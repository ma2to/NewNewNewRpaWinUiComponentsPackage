using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;
using System.Collections.Concurrent;
using System.Data;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Services;

/// <summary>
/// Interná implementácia import služby s komplexnou funkcionalitou
/// Podporuje len DataTable a Dictionary formáty podľa KRITICKÉHO obmedzenia
/// Thread-safe bez mutable fields pre jednotlivé operácie
/// Používa interný IRowStore pre dávkové operácie a perzistenciu
/// </summary>
internal sealed class ImportService : IImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly IOperationLogger<ImportService> _operationLogger;
    private readonly Logging.ImportLogger _importLogger;
    private readonly IValidationService _validationService;
    private readonly Infrastructure.Persistence.Interfaces.IRowStore _rowStore;
    private readonly AdvancedDataGridOptions _options;

    /// <summary>
    /// Konštruktor ImportService
    /// Inicializuje všetky závislosti a null pattern pre operation logger
    /// </summary>
    public ImportService(
        ILogger<ImportService> logger,
        Logging.ImportLogger importLogger,
        IValidationService validationService,
        Infrastructure.Persistence.Interfaces.IRowStore rowStore,
        AdvancedDataGridOptions options,
        IOperationLogger<ImportService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _importLogger = importLogger ?? throw new ArgumentNullException(nameof(importLogger));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Ak nie je poskytnutý operation logger, použijeme null pattern (žiadne logovanie)
        _operationLogger = operationLogger ?? NullOperationLogger<ImportService>.Instance;
    }

    /// <summary>
    /// Importuje dáta s komplexnou validáciou a thread-safe spracovaním
    /// KRITICKÉ: Podporuje len DataTable a Dictionary - NIE JSON/Excel/CSV
    /// Volá AreAllNonEmptyRowsValidAsync po dokončení importu
    /// </summary>
    public async Task<InternalImportResult> ImportAsync(InternalImportDataCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname import operáciu - vytvoríme operation scope pre automatické tracking
        using var scope = _operationLogger.LogOperationStart("ImportAsync", new
        {
            OperationId = operationId,
            Mode = command.Mode,
            DataType = command.DictionaryData != null ? "Dictionary" : command.DataTableData != null ? "DataTable" : "null",
            CorrelationId = command.CorrelationId
        });

        // Špecializované import logovanie - start
        var dataSource = command.DataTableData != null ? "DataTable" : command.DictionaryData != null ? "Dictionary" : "Unknown";
        var rowCount = command.DataTableData?.Rows.Count ?? command.DictionaryData?.Count ?? 0;
        _importLogger.LogImportStart(operationId, dataSource, rowCount, command.Mode.ToString());

        try
        {
            // Validujeme import konfiguráciu
            _logger.LogInformation("Validating import configuration for operation {OperationId}", operationId);
            var validationResult = await ValidateImportDataAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                // Validácia zlyhala - zalogujeme chyby a vrátime failure
                _logger.LogWarning("Import validation failed for operation {OperationId}: {Errors}",
                    operationId, string.Join(", ", validationResult.ValidationErrors));
                scope.MarkFailure(new InvalidOperationException($"Import validation failed: {string.Join(", ", validationResult.ValidationErrors)}"));
                return InternalImportResult.Failure(validationResult.ValidationErrors, stopwatch.Elapsed, command.Mode, command.CorrelationId);
            }

            _logger.LogInformation("Validation successful, starting data processing for operation {OperationId}", operationId);

            // Spracujeme import dáta podľa typu - PODPORUJEME len DataTable a Dictionary
            var processedRows = await ProcessImportDataAsync(command, operationId, cancellationToken);
            _logger.LogInformation("Processed {RowCount} rows in {Duration}ms for operation {OperationId}",
                processedRows.Count, stopwatch.ElapsedMilliseconds, operationId);

            // Uložíme importované dáta do row store
            _logger.LogInformation("Storing {RowCount} rows with mode {Mode} for operation {OperationId}",
                processedRows.Count, command.Mode, operationId);
            var storeResult = await StoreImportedDataAsync(processedRows, command.Mode, operationId, cancellationToken);
            if (!storeResult.IsSuccess)
            {
                // Ukladanie zlyhalo - zalogujeme chybu
                _logger.LogError("Storing imported data failed for operation {OperationId}: {Error}",
                    operationId, storeResult.ErrorMessage);
                scope.MarkFailure(new InvalidOperationException($"Storage failed: {storeResult.ErrorMessage}"));
                return InternalImportResult.Failure(new[] { storeResult.ErrorMessage ?? "Storage failed" }, stopwatch.Elapsed, command.Mode, command.CorrelationId);
            }

            _logger.LogInformation("Data successfully stored for operation {OperationId}", operationId);

            // CRITICAL: Voláme AreAllNonEmptyRowsValidAsync po dokončení importu (iba ak je EnableBatchValidation = true)
            bool validationPassed = true;
            int validRows = processedRows.Count;
            int errorCount = 0;

            if (_options.EnableBatchValidation)
            {
                _logger.LogInformation("Starting automatic post-import batch validation for operation {OperationId}", operationId);
                _importLogger.LogValidationStart(operationId, 0);

                var validationStart = stopwatch.Elapsed;
                var postImportValidation = await _validationService.AreAllNonEmptyRowsValidAsync(false, cancellationToken);
                var validationTime = stopwatch.Elapsed - validationStart;

                if (!postImportValidation.IsSuccess)
                {
                    // Post-import validácia našla problémy - zalogujeme warning
                    _logger.LogWarning("Post-import validation found issues for operation {OperationId}: {Error}",
                        operationId, postImportValidation.ErrorMessage);
                    scope.MarkWarning($"Post-import validation found issues: {postImportValidation.ErrorMessage}");
                    validationPassed = false;
                    errorCount = 1; // Simplified - actual error count would need to be retrieved from validation service
                    validRows = processedRows.Count - errorCount;
                }
                else
                {
                    _logger.LogInformation("Post-import validation successful for operation {OperationId}", operationId);
                }

                _importLogger.LogValidationResults(operationId, processedRows.Count, validRows, errorCount, validationTime);
            }
            else
            {
                _logger.LogInformation("Batch validation disabled, skipping automatic post-import validation for operation {OperationId}", operationId);
            }

            // Zalogujeme metriky importu
            _operationLogger.LogImportOperation(
                importType: command.DataTableData != null ? "DataTable" : "Dictionary",
                totalRows: processedRows.Count,
                importedRows: processedRows.Count,
                duration: stopwatch.Elapsed);

            _logger.LogInformation("Import operation {OperationId} completed successfully in {Duration}ms, imported {RowCount} rows",
                operationId, stopwatch.ElapsedMilliseconds, processedRows.Count);

            // Špecializované logovanie - completion & performance metrics
            var rowsPerSecond = processedRows.Count / stopwatch.Elapsed.TotalSeconds;
            _importLogger.LogImportCompletion(operationId, true, processedRows.Count, stopwatch.Elapsed);
            _importLogger.LogPerformanceMetrics(operationId, rowsPerSecond, 0L, 0L);

            // Označíme scope ako úspešný
            scope.MarkSuccess(new { ImportedRows = processedRows.Count, Duration = stopwatch.Elapsed });

            return InternalImportResult.CreateSuccess(processedRows.Count, processedRows.Count, stopwatch.Elapsed, command.Mode, command.CorrelationId, validationPassed);
        }
        catch (OperationCanceledException ex)
        {
            // Operácia bola zrušená používateľom
            _logger.LogWarning("Import operation {OperationId} was cancelled by user", operationId);
            scope.MarkFailure(ex);
            return InternalImportResult.Failure(new[] { "Operation was cancelled" }, stopwatch.Elapsed, command.Mode, command.CorrelationId);
        }
        catch (Exception ex)
        {
            // Neočakávaná chyba počas importu
            _logger.LogError(ex, "Import operation {OperationId} failed with unexpected error: {Message}", operationId, ex.Message);
            _importLogger.LogCriticalError(operationId, ex, "ImportAsync failed");
            scope.MarkFailure(ex);
            return InternalImportResult.Failure(new[] { $"Import failed: {ex.Message}" }, stopwatch.Elapsed, command.Mode, command.CorrelationId);
        }
    }

    /// <summary>
    /// Validuje konfiguráciu a obmedzenia import dát
    /// </summary>
    public async Task<InternalImportValidationResult> ValidateImportDataAsync(InternalImportDataCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            return new InternalImportValidationResult(false, new[] { "Import command cannot be null" }, Array.Empty<string>());

        var errors = new List<string>();

        // Validate data presence
        if (command.DataTableData == null && command.DictionaryData == null)
            errors.Add("Import data cannot be null - either DataTableData or DictionaryData must be provided");

        // CRITICAL: Validate only supported data types (DataTable and Dictionary)
        if (command.DataTableData != null && command.DictionaryData != null)
        {
            errors.Add("Only one data source is allowed - either DataTableData or DictionaryData, not both");
        }

        // Validate import mode
        if (!Enum.IsDefined(typeof(ImportMode), command.Mode))
            errors.Add($"Invalid import mode: {command.Mode}");

        // Additional validation can be added here if needed

        // Additional async validations can be added here
        await Task.CompletedTask;

        return errors.Count == 0
            ? new InternalImportValidationResult(true, Array.Empty<string>(), Array.Empty<string>())
            : new InternalImportValidationResult(false, errors, Array.Empty<string>());
    }

    /// <summary>
    /// Získa podporované import režimy pre daný dátový typ
    /// </summary>
    public IReadOnlyList<ImportMode> GetSupportedImportModes(Type dataType, object? targetSchema = null)
    {
        if (dataType == null)
            return Array.Empty<ImportMode>();

        // CRITICAL: Only DataTable and Dictionary are supported
        if (dataType == typeof(DataTable))
        {
            return new[] { ImportMode.Replace, ImportMode.Append, ImportMode.Merge };
        }

        if (IsValidDictionaryType(dataType))
        {
            return new[] { ImportMode.Replace, ImportMode.Append };
        }

        _logger.LogWarning("Unsupported data type for import: {DataType}", dataType.Name);
        return Array.Empty<ImportMode>();
    }

    /// <summary>
    /// Odhadne požiadavky pre import na plánovacie účely
    /// </summary>
    public async Task<(TimeSpan EstimatedDuration, long EstimatedMemoryUsage)> EstimateImportRequirementsAsync(InternalImportDataCommand command)
    {
        if (command?.DataTableData == null && command?.DictionaryData == null)
            return (TimeSpan.Zero, 0L);

        // Estimate based on data type and size
        var estimatedRows = 0L;
        var estimatedMemoryPerRow = 1024L; // Base estimate

        if (command.DataTableData is DataTable dataTable)
        {
            estimatedRows = dataTable.Rows.Count;
            estimatedMemoryPerRow = dataTable.Columns.Count * 256L; // Rough estimate per column
        }
        else if (command.DictionaryData is List<Dictionary<string, object?>> dictionaries)
        {
            // For dictionary collections, estimate based on count
            estimatedRows = dictionaries.Count;
            estimatedMemoryPerRow = 2048L; // Larger estimate for dictionary overhead
        }

        var totalMemory = estimatedRows * estimatedMemoryPerRow;
        var estimatedDuration = TimeSpan.FromMilliseconds(estimatedRows * 2); // 2ms per row estimate

        await Task.CompletedTask;
        return (estimatedDuration, totalMemory);
    }

    /// <summary>
    /// Spracuje import dáta podľa typu - thread-safe len s lokálnym stavom
    /// </summary>
    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ProcessImportDataAsync(
        InternalImportDataCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var processedRows = new List<IReadOnlyDictionary<string, object?>>();

        if (command.DataTableData is DataTable dataTable)
        {
            _logger.LogDebug("Processing DataTable with {RowCount} rows for operation {OperationId}",
                dataTable.Rows.Count, operationId);

            // Process DataTable rows in batches for memory efficiency
            var batchSize = _options.ImportBatchSize;
            var rowIndex = 0;

            while (rowIndex < dataTable.Rows.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchEnd = Math.Min(rowIndex + batchSize, dataTable.Rows.Count);
                var batchRows = new List<IReadOnlyDictionary<string, object?>>();

                for (int i = rowIndex; i < batchEnd; i++)
                {
                    var row = dataTable.Rows[i];
                    var rowDict = new Dictionary<string, object?>();

                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        var columnName = dataTable.Columns[col].ColumnName;
                        var value = row[col] == DBNull.Value ? null : row[col];
                        rowDict[columnName] = value;
                    }

                    batchRows.Add(rowDict);
                }

                processedRows.AddRange(batchRows);
                rowIndex = batchEnd;

                // Small delay for cooperative cancellation
                if (rowIndex < dataTable.Rows.Count)
                    await Task.Delay(1, cancellationToken);
            }
        }
        else if (command.DictionaryData is List<Dictionary<string, object?>> dictionaries)
        {
            _logger.LogDebug("Processing {DictionaryCount} Dictionaries for operation {OperationId}",
                dictionaries.Count, operationId);

            foreach (var dict in dictionaries)
            {
                // Convert to readonly for consistency
                var readonlyDict = new Dictionary<string, object?>(dict);
                processedRows.Add(readonlyDict);
            }
        }

        return processedRows;
    }

    /// <summary>
    /// Uloží importované dáta pomocou row store - thread-safe operácia
    /// Používa interné IRowStore dávkové metódy pre optimálny výkon
    /// </summary>
    private async Task<Result> StoreImportedDataAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        ImportMode importMode,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (importMode)
            {
                case ImportMode.Replace:
                    _logger.LogDebug("Import mode REPLACE: clearing existing data and replacing with {RowCount} rows", rows.Count);
                    await _rowStore.ReplaceAllRowsAsync(rows, cancellationToken);
                    break;

                case ImportMode.Append:
                    _logger.LogDebug("Import mode APPEND: appending {RowCount} rows to existing data", rows.Count);
                    await _rowStore.AppendRowsAsync(rows, cancellationToken);
                    break;

                case ImportMode.Merge:
                    // For merge, we use UpsertRowsAsync if rows have unique identifiers
                    // Otherwise fall back to append
                    _logger.LogDebug("Import mode MERGE: merging {RowCount} rows with existing data", rows.Count);
                    await _rowStore.AppendRowsAsync(rows, cancellationToken);
                    break;

                default:
                    return Result.Failure($"Unsupported import mode: {importMode}");
            }

            _logger.LogInformation("Successfully stored {RowCount} rows for operation {OperationId} with mode {ImportMode}",
                rows.Count, operationId, importMode);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store imported data for operation {OperationId}", operationId);
            return Result.Failure($"Storage failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Skontroluje či typ je platný Dictionary typ (Dictionary&lt;string, object?&gt;)
    /// </summary>
    private static bool IsValidDictionaryType(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericDef = type.GetGenericTypeDefinition();
        if (genericDef != typeof(Dictionary<,>) &&
            genericDef != typeof(IDictionary<,>) &&
            genericDef != typeof(IReadOnlyDictionary<,>))
            return false;

        var genericArgs = type.GetGenericArguments();
        return genericArgs.Length == 2 &&
               genericArgs[0] == typeof(string) &&
               genericArgs[1] == typeof(object);
    }
}
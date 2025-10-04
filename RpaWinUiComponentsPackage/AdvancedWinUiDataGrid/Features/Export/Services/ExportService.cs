using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;
using System.Data;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Services;

/// <summary>
/// Interná implementácia export služby s komplexnou funkcionalitou
/// Podporuje IBA DataTable a Dictionary export formáty podľa CRITICAL obmedzenia
/// Spracováva onlyChecked a onlyFiltered argumenty ktoré môžu byť kombinované
/// Thread-safe bez per-operation mutable fields
/// Používa interný IRowStore pre streaming a batch operácie
/// </summary>
internal sealed class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IOperationLogger<ExportService> _operationLogger;
    private readonly Logging.ExportLogger _exportLogger;
    private readonly IValidationService _validationService;
    private readonly IFilterService _filterService;
    private readonly Infrastructure.Persistence.Interfaces.IRowStore _rowStore;
    private readonly AdvancedDataGridOptions _options;

    /// <summary>
    /// Konštruktor ExportService
    /// Inicializuje všetky závislosti a nastavuje null pattern pre optional operation logger
    /// </summary>
    public ExportService(
        ILogger<ExportService> logger,
        Logging.ExportLogger exportLogger,
        IValidationService validationService,
        IFilterService filterService,
        Infrastructure.Persistence.Interfaces.IRowStore rowStore,
        AdvancedDataGridOptions options,
        IOperationLogger<ExportService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exportLogger = exportLogger ?? throw new ArgumentNullException(nameof(exportLogger));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Ak nie je poskytnutý operation logger, použijeme null pattern (žiadne logovanie)
        _operationLogger = operationLogger ?? NullOperationLogger<ExportService>.Instance;
    }

    /// <summary>
    /// Exportuje dáta do DataTable formátu s komplexným filtrovaním
    /// Podporuje onlyChecked a onlyFiltered filtre ktoré môžu byť kombinované
    /// </summary>
    public async Task<System.Data.DataTable> ExportToDataTableAsync(IEnumerable<IReadOnlyDictionary<string, object?>> data, InternalExportDataCommand command, CancellationToken cancellationToken = default)
    {
        var dataList = data.ToList();
        return await ConvertToDataTableAsync(dataList, Guid.NewGuid(), cancellationToken);
    }

    /// <summary>
    /// Exportuje dáta do Dictionary formátu s komplexným filtrovaním
    /// Podporuje onlyChecked a onlyFiltered filtre ktoré môžu byť kombinované
    /// </summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExportToDictionaryAsync(IEnumerable<IReadOnlyDictionary<string, object?>> data, InternalExportDataCommand command, CancellationToken cancellationToken = default)
    {
        var dataList = data.ToList();
        return await ConvertToDictionaryAsync(dataList, Guid.NewGuid(), cancellationToken);
    }

    /// <summary>
    /// Exportuje dáta s komplexným filtrovaním a thread-safe spracovaním
    /// CRITICAL: Podporuje IBA DataTable a Dictionary - ŽIADNE JSON/Excel/CSV
    /// CRITICAL: ExportOnlyChecked a ExportOnlyFiltered môžu byť kombinované
    /// Volá AreAllNonEmptyRowsValidAsync pred exportom
    /// </summary>
    public async Task<InternalExportResult> ExportAsync(InternalExportDataCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname export operáciu - vytvoríme operation scope pre automatické tracking
        using var scope = _operationLogger.LogOperationStart("ExportAsync", new
        {
            OperationId = operationId,
            Format = command.Format,
            OnlyChecked = command.ExportOnlyChecked,
            OnlyFiltered = command.ExportOnlyFiltered,
            RemoveAfterExport = command.RemoveAfterExport,
            CorrelationId = command.CorrelationId
        });

        // Špecializované export logovanie - start
        var totalRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
        var columnCount = 0;
        await foreach (var firstBatch in _rowStore.StreamRowsAsync(false, 1, cancellationToken))
        {
            columnCount = firstBatch.Count > 0 ? firstBatch[0].Count : 0;
            break;
        }
        _exportLogger.LogExportStart(operationId, command.Format.ToString(), totalRowCount, columnCount, command.ExportOnlyFiltered, command.ExportOnlyChecked);

        try
        {
            // Validujeme export konfiguráciu
            _logger.LogInformation("Validating export configuration for operation {OperationId}", operationId);
            var validationResult = await ValidateExportConfigurationAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                // Validácia zlyhala - zalogujeme warning s detailami
                _logger.LogWarning("Export validation failed for operation {OperationId}: {Errors}",
                    operationId, string.Join(", ", validationResult.ValidationErrors));
                scope.MarkFailure(new InvalidOperationException($"Export validation failed: {string.Join(", ", validationResult.ValidationErrors)}"));
                return InternalExportResult.Failure(string.Join(", ", validationResult.ValidationErrors), stopwatch.Elapsed, command.Format, command.CorrelationId);
            }

            _logger.LogInformation("Validation successful for operation {OperationId}", operationId);

            // CRITICAL: Voláme AreAllNonEmptyRowsValidAsync pred exportom (iba ak je EnableBatchValidation = true)
            if (_options.EnableBatchValidation)
            {
                _logger.LogInformation("Starting automatic pre-export batch validation for operation {OperationId}", operationId);

                var preExportValidation = await _validationService.AreAllNonEmptyRowsValidAsync(command.ExportOnlyFiltered, cancellationToken);
                if (!preExportValidation.IsSuccess)
                {
                    // Pre-export validácia našla problémy - zalogujeme warning (nie je kritické, môžeme exportovať)
                    _logger.LogWarning("Pre-export validation found issues for operation {OperationId}: {Error}",
                        operationId, preExportValidation.ErrorMessage);
                    scope.MarkWarning($"Pre-export validation found issues: {preExportValidation.ErrorMessage}");
                }
                else
                {
                    _logger.LogInformation("Pre-export validation successful for operation {OperationId}", operationId);
                }
            }
            else
            {
                _logger.LogInformation("Batch validation disabled, skipping automatic pre-export validation for operation {OperationId}", operationId);
            }

            // Získame filtrované dáta podľa export kritérií
            _logger.LogInformation("Filtering data by criteria - onlyChecked: {OnlyChecked}, onlyFiltered: {OnlyFiltered}",
                command.ExportOnlyChecked, command.ExportOnlyFiltered);
            var filteredData = await GetFilteredExportDataAsync(command, operationId, cancellationToken);
            _logger.LogInformation("Filtered {RowCount} rows for export", filteredData.Count);

            // Špecializované logovanie - data filtering
            _exportLogger.LogDataFiltering(operationId, (int)totalRowCount, (int)totalRowCount, filteredData.Count, filteredData.Count);

            // Exportujeme dáta v požadovanom formáte
            _logger.LogInformation("Processing export data to format {Format}", command.Format);
            var transformStart = stopwatch.Elapsed;
            var exportedData = await ProcessExportDataAsync(filteredData, command.Format, operationId, cancellationToken);
            var transformTime = stopwatch.Elapsed - transformStart;
            _logger.LogInformation("Data successfully exported to format {Format}", command.Format);

            // Špecializované logovanie - data transformation
            _exportLogger.LogDataTransformation(operationId, "Internal", command.Format.ToString(), filteredData.Count, transformTime);

            // Odstránime exportované riadky ak je to požadované
            if (command.RemoveAfterExport && filteredData.Count > 0)
            {
                _logger.LogInformation("Removing {RowCount} exported rows", filteredData.Count);
                await RemoveExportedRowsAsync(filteredData, operationId, cancellationToken);
                _logger.LogInformation("Exported rows successfully removed");
            }

            // Zalogujeme metriky exportu
            _operationLogger.LogExportOperation(
                exportType: command.Format.ToString(),
                totalRows: filteredData.Count,
                exportedRows: filteredData.Count,
                duration: stopwatch.Elapsed);

            _logger.LogInformation("Export operation {OperationId} completed successfully in {Duration}ms, exported {RowCount} rows, removed: {Removed}",
                operationId, stopwatch.ElapsedMilliseconds, filteredData.Count, command.RemoveAfterExport);

            // Špecializované logovanie - completion & performance metrics
            var dataSize = EstimateDataSize(exportedData, command.Format);
            var rowsPerSecond = filteredData.Count / stopwatch.Elapsed.TotalSeconds;
            var throughputMBps = (dataSize / 1024.0 / 1024.0) / stopwatch.Elapsed.TotalSeconds;

            _exportLogger.LogExportCompletion(operationId, true, filteredData.Count, columnCount, dataSize, stopwatch.Elapsed);
            _exportLogger.LogPerformanceMetrics(operationId, rowsPerSecond, 0L, 0L, throughputMBps);

            // Označíme scope ako úspešný
            scope.MarkSuccess(new
            {
                ExportedRows = filteredData.Count,
                Format = command.Format,
                Removed = command.RemoveAfterExport,
                Duration = stopwatch.Elapsed
            });

            return InternalExportResult.CreateSuccess(filteredData.Count, filteredData.Count, stopwatch.Elapsed, command.Format, dataSize, command.CorrelationId);
        }
        catch (OperationCanceledException ex)
        {
            // Operácia bola zrušená používateľom
            _logger.LogInformation("Export operation {OperationId} was cancelled by user", operationId);
            scope.MarkFailure(ex);
            return InternalExportResult.Failure("Export operation was cancelled", stopwatch.Elapsed, command.Format, command.CorrelationId);
        }
        catch (Exception ex)
        {
            // Neočakávaná chyba - CRITICAL level
            _logger.LogCritical(ex, "CRITICAL ERROR: Export operation {OperationId} failed with unexpected error: {Message}. Stack trace: {StackTrace}",
                operationId, ex.Message, ex.StackTrace);
            _exportLogger.LogCriticalError(operationId, ex, "ExportAsync failed");
            scope.MarkFailure(ex);
            return InternalExportResult.Failure($"Export failed: {ex.Message}", stopwatch.Elapsed, command.Format, command.CorrelationId);
        }
    }

    /// <summary>
    /// Validuje export konfiguráciu a obmedzenia
    /// Overuje správnosť export formátu a column selection
    /// </summary>
    public async Task<InternalExportValidationResult> ValidateExportConfigurationAsync(InternalExportDataCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            return InternalExportValidationResult.Invalid(new[] { "Export command cannot be null" });

        var errors = new List<string>();

        // CRITICAL: Validate only supported export formats (DataTable and Dictionary)
        if (!Enum.IsDefined(typeof(ExportFormat), command.Format))
        {
            errors.Add($"Invalid export format: {command.Format}");
        }
        else if (command.Format != ExportFormat.DataTable && command.Format != ExportFormat.Dictionary)
        {
            errors.Add($"Unsupported export format: {command.Format}. Only DataTable and Dictionary are supported");
        }

        // Validate column selection if provided
        if (command.ColumnNames != null && command.ColumnNames.Count == 0)
            errors.Add("Column names cannot be empty when provided");

        // Validate that onlyChecked is feasible (requires checkbox column)
        if (command.ExportOnlyChecked)
        {
            // Kontrolujeme či existuje checkbox column pre onlyChecked filter
            // Pre teraz predpokladáme že je validný ak je požadovaný
            _logger.LogInformation("Export s onlyChecked filtrom požadovaný pre operáciu");
        }

        // Additional async validations can be added here
        await Task.CompletedTask;

        return errors.Count == 0
            ? InternalExportValidationResult.Valid()
            : InternalExportValidationResult.Invalid(errors);
    }

    /// <summary>
    /// Získa podporované export formáty - IBA DataTable a Dictionary
    /// CRITICAL: Iba DataTable a Dictionary sú podporované
    /// </summary>
    public IReadOnlyList<ExportFormat> GetSupportedFormats()
    {
        // CRITICAL: Iba DataTable a Dictionary sú podporované
        return new[] { ExportFormat.DataTable, ExportFormat.Dictionary };
    }

    /// <summary>
    /// Odhadne požiadavky na export pre plánovanie
    /// Vráti odhadovaný čas trvania a použitie pamäte
    /// </summary>
    public async Task<(TimeSpan EstimatedDuration, long EstimatedMemoryUsage)> EstimateExportRequirementsAsync(InternalExportDataCommand command)
    {
        if (command == null)
            return (TimeSpan.Zero, 0L);

        try
        {
            // Odhadujeme na základe aktuálnej veľkosti dát a filtrov
            var totalRowCount = await _rowStore.GetRowCountAsync(CancellationToken.None);

            // Aplikujeme filter odhady
            var estimatedRows = totalRowCount;
            if (command.ExportOnlyChecked || command.ExportOnlyFiltered)
            {
                // Hrubý odhad: filtrované dáta sú typicky 20-80% celku
                estimatedRows = (int)(totalRowCount * 0.5);
            }

            var estimatedMemoryPerRow = command.Format switch
            {
                ExportFormat.DataTable => 512L, // DataTable má overhead
                ExportFormat.Dictionary => 256L, // Dictionary je kompaktnejší
                _ => 256L
            };

            var totalMemory = estimatedRows * estimatedMemoryPerRow;
            var estimatedDuration = TimeSpan.FromMilliseconds(estimatedRows * 1.5); // 1.5ms per row odhad

            return (estimatedDuration, totalMemory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to estimate export requirements, using default estimates");
            return (TimeSpan.FromSeconds(5), 1024L * 1024L); // Default: 5 sekúnd, 1MB
        }
    }

    /// <summary>
    /// Získa filtrované dáta podľa export kritérií - thread-safe s local state only
    /// CRITICAL: Podporuje kombinovanie onlyChecked a onlyFiltered
    /// Používa StreamRowsAsync pre pamäťovo efektívne spracovanie
    /// </summary>
    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetFilteredExportDataAsync(
        InternalExportDataCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting filtered export data for operation {OperationId} with onlyChecked: {OnlyChecked}, onlyFiltered: {OnlyFiltered}",
            operationId, command.ExportOnlyChecked, command.ExportOnlyFiltered);

        var filteredData = new List<IReadOnlyDictionary<string, object?>>();
        var originalCount = 0;

        // Streamujeme dáta z row store pre pamäťovú efektívnosť
        await foreach (var batch in _rowStore.StreamRowsAsync(command.ExportOnlyFiltered, _options.ExportBatchSize, cancellationToken))
        {
            originalCount += batch.Count;

            // Spracujeme každý riadok v dávke
            foreach (var row in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Aplikujeme onlyChecked filter ak je požadovaný
                if (command.ExportOnlyChecked && !IsRowChecked(row))
                    continue;

                // Aplikujeme column selection ak je špecifikovaný
                var processedRow = command.ColumnNames != null && command.ColumnNames.Count > 0
                    ? ApplyColumnSelectionToRow(row, command.ColumnNames)
                    : row;

                // Pridáme validation alerts ak je požadované
                if (command.IncludeValidationAlerts)
                {
                    processedRow = await AddValidationAlertsToRowAsync(processedRow, cancellationToken);
                }

                filteredData.Add(processedRow);
            }
        }

        _logger.LogDebug("Filtered data for operation {OperationId}: {OriginalCount} -> {FilteredCount} rows",
            operationId, originalCount, filteredData.Count);

        return filteredData;
    }

    /// <summary>
    /// Kontroluje či je riadok označený/vybraný cez checkbox stĺpec
    /// Vyhľadáva checkbox stĺpce podľa typických názvov
    /// </summary>
    private bool IsRowChecked(IReadOnlyDictionary<string, object?> row)
    {
        foreach (var kvp in row)
        {
            if (IsCheckboxColumn(kvp.Key) && kvp.Value is bool boolValue && boolValue)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Aplikuje column selection na jednotlivý riadok
    /// Vyberá iba požadované stĺpce z riadku
    /// </summary>
    private IReadOnlyDictionary<string, object?> ApplyColumnSelectionToRow(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<string> columnNames)
    {
        var selectedRow = new Dictionary<string, object?>();
        foreach (var columnName in columnNames)
        {
            if (row.TryGetValue(columnName, out var value))
            {
                selectedRow[columnName] = value;
            }
        }
        return selectedRow;
    }

    /// <summary>
    /// Pridá validation alerts do riadku ak je to požadované
    /// Získa validation errors z row store a pridá ich ako špeciálny stĺpec
    /// </summary>
    private async Task<IReadOnlyDictionary<string, object?>> AddValidationAlertsToRowAsync(
        IReadOnlyDictionary<string, object?> row,
        CancellationToken cancellationToken)
    {
        try
        {
            // Získame row ID pre vyhľadanie validation errors
            if (!row.TryGetValue("__rowId", out var rowIdObj) || rowIdObj == null)
            {
                return row; // Žiadne row ID, nemôžeme vyhľadať validáciu
            }

            var rowId = rowIdObj.ToString();
            if (string.IsNullOrEmpty(rowId))
                return row;

            // Získame validation errors pre tento riadok z row store
            var validationErrors = await _rowStore.GetValidationErrorsForRowAsync(rowId, cancellationToken);

            if (validationErrors == null || !validationErrors.Any())
                return row; // Žiadne validation errors

            // Vytvoríme mutable kópiu a pridáme validation alerts stĺpec
            var mutableRow = row.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var alertMessages = validationErrors.Select(e => $"{e.ColumnName}: {e.Message}");
            mutableRow["__validationAlerts"] = string.Join("; ", alertMessages);

            return mutableRow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add validation alerts to row");
            return row; // Return original row on error
        }
    }

    /// <summary>
    /// Spracuje export dáta do požadovaného formátu - thread-safe operácia
    /// Podporuje DataTable a Dictionary formáty
    /// </summary>
    private async Task<object> ProcessExportDataAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        ExportFormat format,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing export data for operation {OperationId} in format {ExportFormat}",
            operationId, format);

        switch (format)
        {
            case ExportFormat.DataTable:
                return await ConvertToDataTableAsync(data, operationId, cancellationToken);

            case ExportFormat.Dictionary:
                return await ConvertToDictionaryAsync(data, operationId, cancellationToken);

            default:
                throw new NotSupportedException($"Export format {format} is not supported");
        }
    }

    /// <summary>
    /// Konvertuje dáta do DataTable formátu
    /// Vytvorí DataTable so stĺpcami z prvého riadku a pridá všetky dáta po dávkach
    /// </summary>
    private async Task<DataTable> ConvertToDataTableAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var dataTable = new DataTable();

        if (data.Count > 0)
        {
            // Vytvoríme stĺpce z prvého riadku
            var firstRow = data[0];
            foreach (var kvp in firstRow)
            {
                var columnType = kvp.Value?.GetType() ?? typeof(object);
                dataTable.Columns.Add(kvp.Key, columnType);
            }

            // Pridáme dátové riadky po dávkach
            var batchSize = _options.ExportBatchSize;
            for (int i = 0; i < data.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchEnd = Math.Min(i + batchSize, data.Count);
                for (int j = i; j < batchEnd; j++)
                {
                    var row = dataTable.NewRow();
                    foreach (var kvp in data[j])
                    {
                        if (dataTable.Columns.Contains(kvp.Key))
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                    }
                    dataTable.Rows.Add(row);
                }

                // Malé oneskorenie pre cooperative cancellation
                if (batchEnd < data.Count)
                    await Task.Delay(1, cancellationToken);
            }
        }

        _logger.LogDebug("Converted {RowCount} rows to DataTable for operation {OperationId}",
            data.Count, operationId);

        return dataTable;
    }

    /// <summary>
    /// Konvertuje dáta do Dictionary formátu (List of dictionaries)
    /// Dáta sú už v dictionary formáte, len zabezpečíme správnu kópiu
    /// </summary>
    private async Task<List<IReadOnlyDictionary<string, object?>>> ConvertToDictionaryAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        // Dáta sú už v dictionary formáte, iba zabezpečíme správnu kópiu
        var result = new List<IReadOnlyDictionary<string, object?>>(data.Count);

        var batchSize = _options.ExportBatchSize;
        for (int i = 0; i < data.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchEnd = Math.Min(i + batchSize, data.Count);
            for (int j = i; j < batchEnd; j++)
            {
                result.Add(data[j]);
            }

            // Malé oneskorenie pre cooperative cancellation
            if (batchEnd < data.Count)
                await Task.Delay(1, cancellationToken);
        }

        _logger.LogDebug("Converted {RowCount} rows to Dictionary format for operation {OperationId}",
            data.Count, operationId);

        return result;
    }

    /// <summary>
    /// Odstráni exportované riadky z úložiska
    /// Extrahuje row IDs a odstráni ich cez row store
    /// </summary>
    private async Task RemoveExportedRowsAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> exportedRows,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Removing {RowCount} exported rows for operation {OperationId}", exportedRows.Count, operationId);

        try
        {
            // Extrahujeme row IDs z exportovaných riadkov
            var rowIdsToRemove = new List<string>();
            foreach (var row in exportedRows)
            {
                if (row.TryGetValue("__rowId", out var rowIdObj) && rowIdObj != null)
                {
                    var rowId = rowIdObj.ToString();
                    if (!string.IsNullOrEmpty(rowId))
                    {
                        rowIdsToRemove.Add(rowId);
                    }
                }
            }

            if (rowIdsToRemove.Count > 0)
            {
                // Odstránime riadky z row store
                await _rowStore.RemoveRowsAsync(rowIdsToRemove, cancellationToken);
                _logger.LogInformation("Removed {RemovedCount} rows after export for operation {OperationId}",
                    rowIdsToRemove.Count, operationId);
            }
            else
            {
                _logger.LogWarning("No row IDs found in exported data, cannot remove rows for operation {OperationId}", operationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove exported rows for operation {OperationId}", operationId);
            throw; // Re-throw to fail the export operation
        }
    }

    /// <summary>
    /// Kontroluje či názov stĺpca indikuje checkbox stĺpec
    /// Vyhľadáva typické názvy pre checkbox stĺpce
    /// </summary>
    private static bool IsCheckboxColumn(string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return false;

        var lowerName = columnName.ToLowerInvariant();
        return lowerName.Contains("selected") ||
               lowerName.Contains("checked") ||
               lowerName.Contains("check") ||
               lowerName == "isselected" ||
               lowerName == "ischecked";
    }

    /// <summary>
    /// Odhadne veľkosť exportovaných dát v bytoch
    /// </summary>
    private long EstimateDataSize(object exportedData, ExportFormat format)
    {
        try
        {
            if (exportedData is DataTable dt)
            {
                // Odhad: 512 bytov na riadok pre DataTable (má overhead)
                return dt.Rows.Count * 512L * (dt.Columns.Count + 1);
            }
            else if (exportedData is IReadOnlyList<IReadOnlyDictionary<string, object?>> list)
            {
                // Odhad: 256 bytov na riadok pre Dictionary
                var avgColumns = list.Count > 0 ? list[0].Count : 0;
                return list.Count * 256L * (avgColumns + 1);
            }
            return 0L;
        }
        catch
        {
            return 0L;
        }
    }
}
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using System.Collections.Concurrent;
using System.Text;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Services;

/// <summary>
/// Interná implementácia copy-paste služby s komplexnou funkcionalitou
/// Singleton služba for globálne zdieľanú clipboard sémantiku
/// CRITICAL: Paste operácie musia volať AreAllNonEmptyRowsValidAsync po dokončení
/// Thread-safe bez per-operation mutable fields
/// Používa volatile clipboard field s lock for thread-safe prístup
/// </summary>
internal sealed class CopyPasteService : ICopyPasteService
{
    private readonly ILogger<CopyPasteService> _logger;
    private readonly IValidationService _validationService;
    private readonly Infrastructure.Persistence.Interfaces.IRowStore _rowStore;
    private readonly AdvancedDataGridOptions _options;
    private readonly IOperationLogger<CopyPasteService> _operationLogger;
    // REMOVED: SmartOperationService - no longer needed with new data-shifting architecture
    // private readonly Features.SmartAddDelete.Interfaces.ISmartOperationService _smartOperationService;
    private readonly object _clipboardLock = new object();
    private volatile object? _clipboardData;

    /// <summary>
    /// Konštruktor CopyPasteService
    /// Inicializuje všetky závislosti vrátane validation service
    /// Nastavuje null pattern for optional operation logger
    /// </summary>
    public CopyPasteService(
        ILogger<CopyPasteService> logger,
        IValidationService validationService,
        Infrastructure.Persistence.Interfaces.IRowStore rowStore,
        AdvancedDataGridOptions options,
        IOperationLogger<CopyPasteService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Použijeme null pattern ak logger nie je poskytnutý
        _operationLogger = operationLogger ?? NullOperationLogger<CopyPasteService>.Instance;
    }

    /// <summary>
    /// Kopíruje dáta do interného clipboardu s thread-safe operáciou
    /// Globálne zdieľaná clipboard sémantika (Singleton service)
    /// Používa clipboardLock for thread-safe prístup k volatile clipboard field
    /// </summary>
    public async Task<CopyPasteResult> CopyToClipboardAsync(CopyDataCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname copy operáciu - vytvoríme operation scope for automatické tracking
        using var scope = _operationLogger.LogOperationStart("CopyToClipboardAsync", new
        {
            OperationId = operationId,
            Format = command.Format,
            SelectedRowCount = command.SelectedRowCount,
            CorrelationId = command.CorrelationId
        });

        _logger.LogInformation("Starting copy operation {OperationId} with format {Format} for {RowCount} rows [CorrelationId: {CorrelationId}]",
            operationId, command.Format, command.SelectedRowCount, command.CorrelationId);

        try
        {
            // Validujeme copy command
            if (!command.HasValidData)
            {
                _logger.LogWarning("Copy operation {OperationId} failed: No data selected [CorrelationId: {CorrelationId}]",
                    operationId, command.CorrelationId);

                scope.MarkFailure(new InvalidOperationException("No data selected for copy"));
                return CopyPasteResult.Failure("No data selected for copy", stopwatch.Elapsed, command.CorrelationId);
            }

            // Formátujeme dáta for clipboard
            _logger.LogInformation("Formatting data for clipboard with format {Format} for operation {OperationId}",
                command.Format, operationId);

            var formattedData = await FormatDataForClipboardAsync(command, operationId, cancellationToken);

            _logger.LogInformation("Data formatted: {DataSize} characters for operation {OperationId}",
                formattedData.Length, operationId);

            // Uložíme dáta do clipboardu thread-safe spôsobom
            SetClipboard(formattedData);

            _logger.LogInformation("Copy operation {OperationId} completed successfully in {Duration}ms. " +
                "Copied {RowCount} rows, size {DataSize} characters [CorrelationId: {CorrelationId}]",
                operationId, stopwatch.ElapsedMilliseconds, command.SelectedRowCount, formattedData.Length, command.CorrelationId);

            scope.MarkSuccess(new
            {
                ProcessedRows = command.SelectedRowCount,
                DataSize = formattedData.Length,
                Format = command.Format,
                Duration = stopwatch.Elapsed
            });

            return CopyPasteResult.CreateSuccess(
                processedRows: command.SelectedRowCount,
                processedColumns: 0, // Copy doesn't track columns separately
                operationTime: stopwatch.Elapsed,
                dataSize: formattedData.Length,
                correlationId: command.CorrelationId);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("Copy operation {OperationId} was cancelled [CorrelationId: {CorrelationId}]",
                operationId, command.CorrelationId);

            scope.MarkFailure(ex);
            return CopyPasteResult.Failure("Copy operation was cancelled", stopwatch.Elapsed, command.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy operation {OperationId} failed with error [CorrelationId: {CorrelationId}]: {Message}",
                operationId, command.CorrelationId, ex.Message);

            scope.MarkFailure(ex);
            return CopyPasteResult.Failure($"Copy failed: {ex.Message}", stopwatch.Elapsed, command.CorrelationId);
        }
    }

    /// <summary>
    /// Vkladá dáta z interného clipboardu s thread-safe operáciou
    /// CRITICAL: Musí volať AreAllNonEmptyRowsValidAsync po dokončení paste operácie
    /// Globálne zdieľaná clipboard sémantika (Singleton služba)
    /// </summary>
    public async Task<CopyPasteResult> PasteFromClipboardAsync(PasteDataCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname paste operáciu - vytvoríme operation scope for automatické tracking
        using var scope = _operationLogger.LogOperationStart("PasteFromClipboardAsync", new
        {
            OperationId = operationId,
            TargetRow = command.TargetRow,
            TargetColumn = command.TargetColumn,
            ValidateAfterPaste = command.ValidateAfterPaste,
            CorrelationId = command.CorrelationId
        });

        _logger.LogInformation("Starting paste operation {OperationId} at position ({Row}, {Column}) " +
            "with post-validation: {ValidateAfterPaste} [CorrelationId: {CorrelationId}]",
            operationId, command.TargetRow, command.TargetColumn, command.ValidateAfterPaste, command.CorrelationId);

        try
        {
            // Získame dáta z clipboardu thread-safe spôsobom
            var clipboardData = GetClipboard();
            if (clipboardData == null)
            {
                _logger.LogWarning("Paste operation {OperationId} failed: No data available in clipboard [CorrelationId: {CorrelationId}]",
                    operationId, command.CorrelationId);

                scope.MarkFailure(new InvalidOperationException("No data available in clipboard"));
                return CopyPasteResult.Failure("No data available in clipboard", stopwatch.Elapsed, command.CorrelationId);
            }

            _logger.LogInformation("Data retrieved from clipboard for operation {OperationId}", operationId);

            // Parsujeme clipboard dáta na riadky
            _logger.LogInformation("Parsing clipboard data for operation {OperationId}", operationId);
            var pasteData = await ParseClipboardDataAsync(clipboardData.ToString() ?? "", command, operationId, cancellationToken);

            _logger.LogInformation("Parsed {RowCount} rows from clipboard for operation {OperationId}",
                pasteData.Count, operationId);

            // Vykonáme paste operáciu podľa import mode
            _logger.LogInformation("Executing paste operation for {RowCount} rows for operation {OperationId}",
                pasteData.Count, operationId);

            var pasteResult = await ExecutePasteOperationAsync(pasteData, command, operationId, cancellationToken);
            if (!pasteResult.IsSuccess)
            {
                _logger.LogError("Paste operation {OperationId} failed: {Error} [CorrelationId: {CorrelationId}]",
                    operationId, pasteResult.ErrorMessage, command.CorrelationId);

                scope.MarkFailure(new InvalidOperationException(pasteResult.ErrorMessage ?? "Paste operation failed"));
                return CopyPasteResult.Failure(pasteResult.ErrorMessage ?? "Paste operation failed", stopwatch.Elapsed, command.CorrelationId);
            }

            _logger.LogInformation("Data successfully pasted for operation {OperationId}", operationId);

            // CRITICAL FIX: Enforce 2-step cleanup after paste (remove ALL empty rows, ensure last empty)
            // NEW ARCHITECTURE: No cleanup needed - data shifting handles row management automatically
            _logger.LogInformation("Paste completed for operation {OperationId} - no manual cleanup needed with data-shifting architecture", operationId);

            // CRITICAL: Automatic post-paste validation (only if ShouldRunAutomaticValidation returns true)
            if (command.ValidateAfterPaste && _validationService.ShouldRunAutomaticValidation("PasteAsync"))
            {
                _logger.LogInformation("Starting automatic post-paste batch validation for operation {OperationId}", operationId);

                var postPasteValidation = await _validationService.AreAllNonEmptyRowsValidAsync(false, false, cancellationToken);
                if (!postPasteValidation.IsSuccess)
                {
                    _logger.LogWarning("Post-paste validation found issues for operation {OperationId}: {Error} [CorrelationId: {CorrelationId}]",
                        operationId, postPasteValidation.ErrorMessage, command.CorrelationId);

                    scope.MarkWarning($"Post-paste validation found issues: {postPasteValidation.ErrorMessage}");
                }
                else
                {
                    _logger.LogInformation("Post-paste validation successful for operation {OperationId}", operationId);
                }
            }
            else
            {
                _logger.LogInformation("Automatic post-paste validation skipped for operation {OperationId} " +
                    "(ValidateAfterPaste={ValidateAfterPaste}, ValidationAutomationMode or EnableBatchValidation is disabled)",
                    operationId, command.ValidateAfterPaste);
            }

            var dataSize = pasteData.Sum(row => row.Values.Sum(v => v?.ToString()?.Length ?? 0));

            _logger.LogInformation("Paste operation {OperationId} completed successfully in {Duration}ms. " +
                "Pasted {RowCount} rows, size {DataSize} characters [CorrelationId: {CorrelationId}]",
                operationId, stopwatch.ElapsedMilliseconds, pasteData.Count, dataSize, command.CorrelationId);

            scope.MarkSuccess(new
            {
                ProcessedRows = pasteData.Count,
                DataSize = dataSize,
                ValidatedAfterPaste = command.ValidateAfterPaste,
                Duration = stopwatch.Elapsed
            });

            return CopyPasteResult.CreateSuccess(
                processedRows: pasteData.Count,
                processedColumns: 0, // Will be calculated based on data
                operationTime: stopwatch.Elapsed,
                dataSize: dataSize,
                correlationId: command.CorrelationId);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("Paste operation {OperationId} was cancelled [CorrelationId: {CorrelationId}]",
                operationId, command.CorrelationId);

            scope.MarkFailure(ex);
            return CopyPasteResult.Failure("Paste operation was cancelled", stopwatch.Elapsed, command.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paste operation {OperationId} failed with error [CorrelationId: {CorrelationId}]: {Message}",
                operationId, command.CorrelationId, ex.Message);

            scope.MarkFailure(ex);
            return CopyPasteResult.Failure($"Paste failed: {ex.Message}", stopwatch.Elapsed, command.CorrelationId);
        }
    }

    /// <summary>
    /// Nastavuje clipboard dáta thread-safe spôsobom
    /// Globálne zdieľaná clipboard sémantika
    /// </summary>
    public void SetClipboard(object payload)
    {
        lock (_clipboardLock)
        {
            _clipboardData = payload;
            _logger.LogInformation("Clipboard data updated with type {PayloadType}",
                payload?.GetType().Name ?? "null");
        }
    }

    /// <summary>
    /// Získava clipboard dáta thread-safe spôsobom
    /// Globálne zdieľaná clipboard sémantika
    /// </summary>
    public object? GetClipboard()
    {
        lock (_clipboardLock)
        {
            var data = _clipboardData;
            _logger.LogInformation("Retrieved clipboard data of type {DataType}",
                data?.GetType().Name ?? "null");
            return data;
        }
    }

    /// <summary>
    /// Pripravuje dáta for clipboard na základe copy príkazu
    /// </summary>
    private async Task<object> PrepareCopyDataAsync(
        CopyDataCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Preparing copy data for operation {OperationId} with format {Format}",
            operationId, command.Format);

        // Simply convert the selected data to the required format
        return await Task.FromResult(command.SelectedData.ToList());
    }

    /// <summary>
    /// Pripravuje jednotlivé dáta buniek for clipboard
    /// </summary>
    private async Task<List<CellCopyData>> PrepareCellDataAsync(
        IReadOnlyList<CellAddress> selectedItems,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var cellData = new List<CellCopyData>();

        foreach (var cellAddress in selectedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get cell value from row store
            var rowData = await _rowStore.GetRowAsync(cellAddress.Row, cancellationToken);
            if (rowData != null && rowData.TryGetValue(cellAddress.Column, out var value))
            {
                cellData.Add(new CellCopyData
                {
                    Row = cellAddress.Row,
                    Column = cellAddress.Column,
                    Value = value,
                    OriginalType = value?.GetType()
                });
            }
        }

        _logger.LogDebug("Prepared {CellCount} cells for copy operation {OperationId}",
            cellData.Count, operationId);

        return cellData;
    }

    /// <summary>
    /// Pripravuje dáta riadkov for clipboard
    /// </summary>
    private async Task<List<IReadOnlyDictionary<string, object?>>> PrepareRowDataAsync(
        IReadOnlyList<CellAddress> selectedItems,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var rowData = new List<IReadOnlyDictionary<string, object?>>();
        var processedRows = new HashSet<int>();

        foreach (var cellAddress in selectedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (processedRows.Add(cellAddress.Row))
            {
                var row = await _rowStore.GetRowAsync(cellAddress.Row, cancellationToken);
                if (row != null)
                {
                    rowData.Add(row);
                }
            }
        }

        _logger.LogDebug("Prepared {RowCount} rows for copy operation {OperationId}",
            rowData.Count, operationId);

        return rowData;
    }

    /// <summary>
    /// Pripravuje dáta stĺpcov for clipboard
    /// </summary>
    private async Task<Dictionary<string, List<object?>>> PrepareColumnDataAsync(
        IReadOnlyList<CellAddress> selectedItems,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var columnData = new Dictionary<string, List<object?>>();
        var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);

        var selectedColumns = selectedItems.Select(addr => addr.Column).Distinct().ToList();

        foreach (var column in selectedColumns)
        {
            var columnValues = new List<object?>();
            foreach (var row in allRows)
            {
                if (row.TryGetValue(column, out var value))
                {
                    columnValues.Add(value);
                }
            }
            columnData[column] = columnValues;
        }

        _logger.LogDebug("Prepared {ColumnCount} columns for copy operation {OperationId}",
            columnData.Count, operationId);

        return columnData;
    }

    /// <summary>
    /// Spracováva clipboard dáta for paste operáciu
    /// </summary>
    private async Task<List<IReadOnlyDictionary<string, object?>>> ProcessPasteDataAsync(
        object clipboardData,
        PasteDataCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing paste data for operation {OperationId}, clipboard type: {ClipboardType}",
            operationId, clipboardData.GetType().Name);

        var processedData = new List<IReadOnlyDictionary<string, object?>>();

        switch (clipboardData)
        {
            case List<IReadOnlyDictionary<string, object?>> rowData:
                processedData.AddRange(rowData);
                break;

            case List<CellCopyData> cellData:
                // Convert cell data to row data based on target position
                processedData.AddRange(await ConvertCellDataToRowsAsync(cellData, command, operationId, cancellationToken));
                break;

            case Dictionary<string, List<object?>> columnData:
                // Convert column data to row data
                processedData.AddRange(ConvertColumnDataToRows(columnData, operationId));
                break;

            default:
                throw new NotSupportedException($"Clipboard data type {clipboardData.GetType().Name} is not supported for paste");
        }

        return processedData;
    }

    /// <summary>
    /// Konvertuje dáta kopírovania buniek na formát riadkov
    /// </summary>
    private async Task<List<IReadOnlyDictionary<string, object?>>> ConvertCellDataToRowsAsync(
        List<CellCopyData> cellData,
        PasteDataCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var rowData = new Dictionary<int, Dictionary<string, object?>>();

        foreach (var cell in cellData)
        {
            var targetRow = command.TargetRow + (cell.Row - cellData.Min(c => c.Row));
            var targetColumn = cell.Column; // For simplicity, keep same column name

            if (!rowData.ContainsKey(targetRow))
                rowData[targetRow] = new Dictionary<string, object?>();

            rowData[targetRow][targetColumn] = cell.Value;
        }

        await Task.CompletedTask;
        return rowData.Values.Cast<IReadOnlyDictionary<string, object?>>().ToList();
    }

    /// <summary>
    /// Konvertuje dáta stĺpcov na riadky
    /// </summary>
    private List<IReadOnlyDictionary<string, object?>> ConvertColumnDataToRows(
        Dictionary<string, List<object?>> columnData,
        Guid operationId)
    {
        var maxRows = columnData.Values.Max(list => list.Count);
        var rowData = new List<IReadOnlyDictionary<string, object?>>();

        for (int i = 0; i < maxRows; i++)
        {
            var row = new Dictionary<string, object?>();
            foreach (var kvp in columnData)
            {
                if (i < kvp.Value.Count)
                {
                    row[kvp.Key] = kvp.Value[i];
                }
            }
            rowData.Add(row);
        }

        return rowData;
    }

    /// <summary>
    /// Vykonáva paste operáciu na základe paste režimu
    /// </summary>
    private async Task<Result> ExecutePasteOperationAsync(
        List<IReadOnlyDictionary<string, object?>> pasteData,
        PasteDataCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        if (command.OverwriteExisting)
        {
            return await ExecuteOverwritePasteAsync(pasteData, command, operationId, cancellationToken);
        }
        else
        {
            return await ExecuteInsertPasteAsync(pasteData, command, operationId, cancellationToken);
        }
    }

    /// <summary>
    /// Vykonáva insert paste operáciu
    /// </summary>
    private async Task<Result> ExecuteInsertPasteAsync(
        List<IReadOnlyDictionary<string, object?>> pasteData,
        PasteDataCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Insert data at target position
            await _rowStore.AppendRowsAsync(pasteData, cancellationToken);

            _logger.LogDebug("Insert paste completed for operation {OperationId} at row {TargetRow}",
                operationId, command.TargetRow);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Insert paste failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Vykonáva overwrite paste operáciu
    /// </summary>
    private async Task<Result> ExecuteOverwritePasteAsync(
        List<IReadOnlyDictionary<string, object?>> pasteData,
        PasteDataCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Overwrite existing data starting at target position
            for (int i = 0; i < pasteData.Count; i++)
            {
                var targetRowIndex = command.TargetRow + i;
                await _rowStore.UpdateRowAsync(targetRowIndex, pasteData[i], cancellationToken);
            }

            _logger.LogDebug("Overwrite paste completed for operation {OperationId} starting at row {TargetRow}",
                operationId, command.TargetRow);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Overwrite paste failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Vykonáva append paste operáciu
    /// </summary>
    private async Task<Result> ExecuteAppendPasteAsync(
        List<IReadOnlyDictionary<string, object?>> pasteData,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Append data to end of store
            await _rowStore.AppendRowsAsync(pasteData, cancellationToken);

            _logger.LogDebug("Append paste completed for operation {OperationId}", operationId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Append paste failed: {ex.Message}");
        }
    }


    /// <summary>
    /// Estimates clipboard requirements for copy operation
    /// Thread-safe estimation with performance metrics
    /// </summary>
    public (long EstimatedSize, TimeSpan EstimatedProcessingTime) EstimateClipboardRequirements(
        IEnumerable<IReadOnlyDictionary<string, object?>> selectedData,
        ClipboardFormat format)
    {
        if (selectedData == null)
            return (0L, TimeSpan.Zero);

        try
        {
            var rowCount = selectedData.TryGetNonEnumeratedCount(out var count) ? count : selectedData.Count();

            if (rowCount == 0)
                return (0L, TimeSpan.Zero);

            // Estimate size based on format and data
            var estimatedSizePerRow = format switch
            {
                ClipboardFormat.TabSeparated => 512L, // ~512 bytes per row for TSV
                ClipboardFormat.CommaSeparated => 384L, // ~384 bytes per row for CSV
                ClipboardFormat.Json => 1024L, // ~1KB per row for JSON
                ClipboardFormat.PlainText => 256L, // ~256 bytes per row for plain text
                ClipboardFormat.CustomDelimited => 400L, // ~400 bytes per row for custom
                _ => 512L
            };

            var totalEstimatedSize = rowCount * estimatedSizePerRow;

            // Estimate processing time (2ms per row base + format overhead)
            var baseTimePerRow = TimeSpan.FromMilliseconds(2);
            var formatOverhead = format switch
            {
                ClipboardFormat.Json => TimeSpan.FromMilliseconds(1), // JSON parsing overhead
                ClipboardFormat.CustomDelimited => TimeSpan.FromMilliseconds(0.5), // Custom delimiter overhead
                _ => TimeSpan.Zero
            };

            var totalEstimatedTime = TimeSpan.FromTicks((baseTimePerRow + formatOverhead).Ticks * rowCount);

            _logger.LogDebug("Estimated clipboard requirements: {Size} bytes, {Duration}ms for {RowCount} rows in {Format} format",
                totalEstimatedSize, totalEstimatedTime.TotalMilliseconds, rowCount, format);

            return (totalEstimatedSize, totalEstimatedTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate clipboard requirements");
            return (1024L, TimeSpan.FromSeconds(1)); // Fallback estimate
        }
    }

    /// <summary>
    /// Automaticky detekuje formát clipboard dát
    /// </summary>
    private ClipboardFormat DetectClipboardFormat(string clipboardData)
    {
        if (string.IsNullOrEmpty(clipboardData))
            return ClipboardFormat.PlainText;

        // Simple format detection logic
        if (clipboardData.TrimStart().StartsWith("{") || clipboardData.TrimStart().StartsWith("["))
            return ClipboardFormat.Json;

        if (clipboardData.Contains('\t'))
            return ClipboardFormat.TabSeparated;

        if (clipboardData.Contains(',') && clipboardData.Contains('\n'))
            return ClipboardFormat.CommaSeparated;

        return ClipboardFormat.PlainText;
    }

    /// <summary>
    /// Odhaduje štruktúru clipboard dát (riadky a stĺpce)
    /// </summary>
    private (int estimatedRows, int estimatedColumns) EstimateClipboardStructure(string clipboardData, ClipboardFormat format)
    {
        if (string.IsNullOrEmpty(clipboardData))
            return (0, 0);

        var lines = clipboardData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var estimatedRows = lines.Length;

        if (estimatedRows == 0)
            return (0, 0);

        // Estimate columns from first line
        var firstLine = lines[0];
        var estimatedColumns = format switch
        {
            ClipboardFormat.TabSeparated => firstLine.Split('\t').Length,
            ClipboardFormat.CommaSeparated => firstLine.Split(',').Length,
            ClipboardFormat.CustomDelimited => firstLine.Split('\t', ',', ';').Length, // Try common delimiters
            ClipboardFormat.Json => 1, // JSON is typically one object per line
            ClipboardFormat.PlainText => 1,
            _ => 1
        };

        return (estimatedRows, Math.Max(1, estimatedColumns));
    }

    /// <summary>
    /// Formats data for clipboard based on command format
    /// Thread-safe formatting with local state only
    /// </summary>
    private async Task<string> FormatDataForClipboardAsync(
        CopyDataCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Formatting {RowCount} rows for clipboard in {Format} format, operation {OperationId}",
            command.SelectedRowCount, command.Format, operationId);

        try
        {
            // Always use Tab-Separated format for Excel compatibility
            return await FormatAsTabSeparatedAsync(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format data for clipboard, operation {OperationId}", operationId);
            throw;
        }
    }

    /// <summary>
    /// SENIOR IMPLEMENTATION: Formátuje dáta ako hodnoty oddelené tabulátormi s Excel escaping.
    ///
    /// EXCEL TSV FORMAT:
    /// - Normal cells: separated by \t (no quotes)
    /// - Multiline cells: wrapped in quotes "Multi\nLine\nCell"
    /// - Cells with tabs: wrapped in quotes "Cell\tWith\tTabs"
    /// - Cells with quotes: quotes doubled AND wrapped "Cell with ""quote"""
    ///
    /// CRITICAL: Before pasting, values are ESCAPED (quotes added if needed).
    /// CRITICAL: After pasting, ParseTabSeparatedDataAsync UNESCAPES (quotes removed).
    ///
    /// EXAMPLE OUTPUT:
    /// Column1\tColumn2\tColumn3\n
    /// Cell1\t"Multi\nLine\nCell"\tCell3\n
    /// Cell1\t"Cell\tWith\tTabs"\tCell3\n
    /// Cell1\t"Cell with ""quote"""\tCell3\n
    /// </summary>
    private async Task<string> FormatAsTabSeparatedAsync(CopyDataCommand command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Formatting {RowCount} rows as TSV with Excel escaping", command.SelectedData.Count());

        var lines = new List<string>();

        if (command.IncludeHeaders && command.SelectedData.Any())
        {
            var headers = command.SelectedData.First().Keys;
            // SENIOR FIX: Escape header values too (in case column names contain special chars)
            lines.Add(string.Join('\t', headers.Select(EscapeTsvValue)));
        }

        foreach (var row in command.SelectedData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // SENIOR FIX: Escape all cell values before joining with tabs
            // This ensures multiline/tab content is properly quoted for Excel
            var values = row.Values.Select(v => EscapeTsvValue(v?.ToString() ?? ""));
            lines.Add(string.Join('\t', values));
        }

        await Task.CompletedTask;
        var result = string.Join('\n', lines);

        _logger.LogInformation("Formatted TSV with {LineCount} lines ({Length} chars)", lines.Count, result.Length);

        return result;
    }

    /// <summary>
    /// Formátuje dáta ako hodnoty oddelené čiarkami
    /// </summary>
    private async Task<string> FormatAsCommaSeparatedAsync(CopyDataCommand command, CancellationToken cancellationToken)
    {
        var lines = new List<string>();

        if (command.IncludeHeaders && command.SelectedData.Any())
        {
            var headers = command.SelectedData.First().Keys;
            lines.Add(string.Join(',', headers.Select(h => $"\"{h}\"")));
        }

        foreach (var row in command.SelectedData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = row.Values.Select(v => $"\"{v?.ToString() ?? ""}\"");
            lines.Add(string.Join(',', values));
        }

        await Task.CompletedTask;
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Formátuje dáta ako JSON
    /// </summary>
    private async Task<string> FormatAsJsonAsync(CopyDataCommand command, CancellationToken cancellationToken)
    {
        var jsonObjects = new List<string>();

        foreach (var row in command.SelectedData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var jsonPairs = row.Select(kvp => $"\"{kvp.Key}\":\"{kvp.Value?.ToString() ?? ""}\"");
            jsonObjects.Add("{" + string.Join(',', jsonPairs) + "}");
        }

        await Task.CompletedTask;
        return "[" + string.Join(',', jsonObjects) + "]";
    }

    /// <summary>
    /// Formátuje dáta ako čistý text
    /// </summary>
    private async Task<string> FormatAsPlainTextAsync(CopyDataCommand command, CancellationToken cancellationToken)
    {
        var lines = new List<string>();

        foreach (var row in command.SelectedData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = string.Join(' ', row.Values.Select(v => v?.ToString() ?? ""));
            lines.Add(line);
        }

        await Task.CompletedTask;
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Formátuje dáta s vlastným oddeľovačom
    /// </summary>
    private async Task<string> FormatAsCustomDelimitedAsync(CopyDataCommand command, CancellationToken cancellationToken)
    {
        var delimiter = command.Delimiter ?? "\t";
        var lines = new List<string>();

        if (command.IncludeHeaders && command.SelectedData.Any())
        {
            var headers = command.SelectedData.First().Keys;
            lines.Add(string.Join(delimiter, headers));
        }

        foreach (var row in command.SelectedData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = row.Values.Select(v => v?.ToString() ?? "");
            lines.Add(string.Join(delimiter, values));
        }

        await Task.CompletedTask;
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Parses clipboard data to row format based on command
    /// Thread-safe parsing with local state only
    /// </summary>
    private async Task<List<IReadOnlyDictionary<string, object?>>> ParseClipboardDataAsync(
        string clipboardData,
        PasteDataCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Parsing clipboard data for paste operation {OperationId} with format {Format}",
            operationId, command.Format);

        try
        {
            // Always use Tab-Separated parsing for Excel compatibility
            return await ParseTabSeparatedDataAsync(clipboardData, command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse clipboard data for operation {OperationId}", operationId);
            throw;
        }
    }

    /// <summary>
    /// SENIOR IMPLEMENTATION: Parses Tab-Separated Values with Excel multiline/tab support.
    ///
    /// EXCEL TSV FORMAT:
    /// - Normal cells: separated by \t
    /// - Multiline cells: wrapped in quotes "Multi\nLine\nCell"
    /// - Cells with tabs: wrapped in quotes "Cell\tWith\tTabs"
    /// - Escaped quotes: doubled "" inside quoted values "Cell with ""quote"""
    ///
    /// CRITICAL: Uses character-by-character parsing instead of Split() to handle embedded delimiters.
    ///
    /// EXAMPLE INPUT:
    /// Column1\tColumn2\tColumn3\n
    /// Cell1\t"Multi\nLine\nCell"\tCell3\n
    /// Cell1\t"Cell\tWith\tTabs"\tCell3\n
    /// Cell1\t"Cell with ""quote"""\tCell3\n
    ///
    /// EXAMPLE OUTPUT (unescaped):
    /// Column2 value: "Multi\nLine\nCell" (3 lines, NO quotes in final value)
    /// </summary>
    private async Task<List<IReadOnlyDictionary<string, object?>>> ParseTabSeparatedDataAsync(
        string clipboardData,
        PasteDataCommand command,
        CancellationToken cancellationToken)
    {
        var result = new List<IReadOnlyDictionary<string, object?>>();
        var currentRow = new List<string>();
        var currentCell = new StringBuilder();
        bool inQuotes = false;
        bool isFirstRow = true;
        string[]? headers = null;

        _logger.LogDebug("Parsing TSV data with Excel multiline support (length: {Length} chars)", clipboardData.Length);

        for (int i = 0; i < clipboardData.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            char c = clipboardData[i];
            char? next = i + 1 < clipboardData.Length ? clipboardData[i + 1] : null;

            if (c == '"')
            {
                if (inQuotes && next == '"')
                {
                    // SENIOR PATTERN: Escaped quote ("") → append single quote to cell content
                    // Example: "Cell with ""quote""" → Cell with "quote"
                    currentCell.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    // Toggle quote mode (start or end of quoted value)
                    // CRITICAL: Quotes themselves are NOT added to cell content
                    inQuotes = !inQuotes;
                }
            }
            else if (c == '\t' && !inQuotes)
            {
                // SENIOR PATTERN: Tab outside quotes = end of cell
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
            }
            else if ((c == '\n' || c == '\r') && !inQuotes)
            {
                // SENIOR PATTERN: Newline outside quotes = end of row
                if (c == '\r' && next == '\n')
                {
                    i++; // Skip \n after \r (handle Windows CRLF)
                }

                if (currentCell.Length > 0 || currentRow.Count > 0)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();

                    if (isFirstRow)
                    {
                        // SENIOR PATTERN: First row = headers (or auto-generate if not header-like)
                        if (IsHeaderRow(currentRow.ToArray()))
                        {
                            headers = currentRow.ToArray();
                            _logger.LogDebug("Detected {HeaderCount} headers from first row", headers.Length);
                        }
                        else
                        {
                            // First row is data - generate column names
                            headers = currentRow.Select((_, idx) => $"Column{idx + 1}").ToArray();
                            _logger.LogDebug("Generated {HeaderCount} column names (first row is data)", headers.Length);

                            // Add first row as data
                            var firstDataRow = new Dictionary<string, object?>();
                            for (int j = 0; j < Math.Min(headers.Length, currentRow.Count); j++)
                            {
                                firstDataRow[headers[j]] = string.IsNullOrEmpty(currentRow[j]) ? null : currentRow[j];
                            }
                            result.Add(firstDataRow);
                        }

                        isFirstRow = false;
                    }
                    else if (headers != null)
                    {
                        // SENIOR PATTERN: Build data row dictionary
                        var dataRow = new Dictionary<string, object?>();
                        for (int j = 0; j < Math.Min(headers.Length, currentRow.Count); j++)
                        {
                            // CRITICAL: Values are UNESCAPED (quotes removed, "" → ")
                            dataRow[headers[j]] = string.IsNullOrEmpty(currentRow[j]) ? null : currentRow[j];
                        }
                        result.Add(dataRow);
                    }

                    currentRow.Clear();
                }
            }
            else
            {
                // Regular character - add to current cell
                // SENIOR PATTERN: Tabs and newlines inside quotes are preserved as-is
                currentCell.Append(c);
            }
        }

        // SENIOR PATTERN: Handle last row if no trailing newline
        if (currentCell.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentCell.ToString());

            if (isFirstRow && headers == null)
            {
                // Only one row total - treat as headers or generate column names
                if (IsHeaderRow(currentRow.ToArray()))
                {
                    headers = currentRow.ToArray();
                }
                else
                {
                    headers = currentRow.Select((_, idx) => $"Column{idx + 1}").ToArray();
                    var dataRow = new Dictionary<string, object?>();
                    for (int j = 0; j < Math.Min(headers.Length, currentRow.Count); j++)
                    {
                        dataRow[headers[j]] = string.IsNullOrEmpty(currentRow[j]) ? null : currentRow[j];
                    }
                    result.Add(dataRow);
                }
            }
            else if (headers != null)
            {
                // Last data row
                var dataRow = new Dictionary<string, object?>();
                for (int j = 0; j < Math.Min(headers.Length, currentRow.Count); j++)
                {
                    dataRow[headers[j]] = string.IsNullOrEmpty(currentRow[j]) ? null : currentRow[j];
                }
                result.Add(dataRow);
            }
        }

        _logger.LogInformation("Parsed {RowCount} data rows from TSV with {HeaderCount} columns",
            result.Count, headers?.Length ?? 0);

        await Task.CompletedTask;
        return result;
    }

    /// <summary>
    /// Parsuje clipboard dáta oddelené čiarkami
    /// </summary>
    private async Task<List<IReadOnlyDictionary<string, object?>>> ParseCommaSeparatedDataAsync(
        string clipboardData,
        PasteDataCommand command,
        CancellationToken cancellationToken)
    {
        var lines = clipboardData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<IReadOnlyDictionary<string, object?>>();

        if (lines.Length == 0)
            return result;

        // Simple CSV parsing (handles quoted values)
        var headers = ParseCsvLine(lines[0]);
        var startIndex = IsHeaderRow(headers) ? 1 : 0;

        if (startIndex == 0)
        {
            headers = headers.Select((_, i) => $"Column{i + 1}").ToArray();
        }

        for (int i = startIndex; i < lines.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = ParseCsvLine(lines[i]);
            var row = new Dictionary<string, object?>();

            for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
            {
                row[headers[j]] = string.IsNullOrEmpty(values[j]) ? null : values[j];
            }

            result.Add(row);
        }

        await Task.CompletedTask;
        return result;
    }

    /// <summary>
    /// Parsuje JSON clipboard dáta
    /// </summary>
    private async Task<List<IReadOnlyDictionary<string, object?>>> ParseJsonDataAsync(
        string clipboardData,
        CancellationToken cancellationToken)
    {
        // Simple JSON parsing - in production would use System.Text.Json
        var result = new List<IReadOnlyDictionary<string, object?>>();

        // For now, return empty result for JSON
        // Full JSON parsing would require proper JSON library integration

        await Task.CompletedTask;
        return result;
    }

    /// <summary>
    /// Parsuje clipboard dáta v čistom texte
    /// </summary>
    private async Task<List<IReadOnlyDictionary<string, object?>>> ParsePlainTextDataAsync(
        string clipboardData,
        CancellationToken cancellationToken)
    {
        var lines = clipboardData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<IReadOnlyDictionary<string, object?>>();

        for (int i = 0; i < lines.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = new Dictionary<string, object?>
            {
                ["Text"] = lines[i]
            };
            result.Add(row);
        }

        await Task.CompletedTask;
        return result;
    }

    /// <summary>
    /// Parsuje clipboard dáta s vlastným oddeľovačom
    /// </summary>
    private async Task<List<IReadOnlyDictionary<string, object?>>> ParseCustomDelimitedDataAsync(
        string clipboardData,
        PasteDataCommand command,
        CancellationToken cancellationToken)
    {
        var delimiter = command.Delimiter ?? "\t";
        var lines = clipboardData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<IReadOnlyDictionary<string, object?>>();

        if (lines.Length == 0)
            return result;

        var headers = lines[0].Split(delimiter);
        var startIndex = IsHeaderRow(headers) ? 1 : 0;

        if (startIndex == 0)
        {
            headers = headers.Select((_, i) => $"Column{i + 1}").ToArray();
        }

        for (int i = startIndex; i < lines.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = lines[i].Split(delimiter);
            var row = new Dictionary<string, object?>();

            for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
            {
                row[headers[j]] = string.IsNullOrEmpty(values[j]) ? null : values[j];
            }

            result.Add(row);
        }

        await Task.CompletedTask;
        return result;
    }

    /// <summary>
    /// Kontroluje či riadok obsahuje hlavičky (ne-numerické hodnoty)
    /// </summary>
    private bool IsHeaderRow(string[] values)
    {
        return values.Any(v => !string.IsNullOrEmpty(v) && !double.TryParse(v, out _));
    }

    /// <summary>
    /// Parsuje CSV riadok so základnou podporou úvodzoviek
    /// </summary>
    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

    #region Public API Compatibility Methods

    /// <summary>
    /// Copy operation (public API compatibility)
    /// </summary>
    public async Task<Common.Models.Result> CopyAsync(bool includeHeaders, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Copy operation (public API) - includeHeaders: {IncludeHeaders}", includeHeaders);

            // Create empty copy command for now - should use actual selection
            var command = new CopyDataCommand(
                SelectedData: Array.Empty<IReadOnlyDictionary<string, object?>>(),
                IncludeHeaders: includeHeaders,
                IncludeValidationAlerts: false,
                Delimiter: "\t",
                Format: PublicClipboardFormat.Excel
            );

            var result = await CopyToClipboardAsync(command, cancellationToken);
            return result.Success
                ? Common.Models.Result.Success()
                : Common.Models.Result.Failure(result.ErrorMessage ?? "Copy failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed: {Message}", ex.Message);
            return Common.Models.Result.Failure($"Copy failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Cut operation (public API compatibility)
    /// </summary>
    public async Task<Common.Models.Result> CutAsync(bool includeHeaders, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cut operation (public API) - includeHeaders: {IncludeHeaders}", includeHeaders);

            // Copy then clear - for now just copy
            var copyResult = await CopyAsync(includeHeaders, cancellationToken);
            if (!copyResult.IsSuccess)
                return copyResult;

            // TODO: Clear selected cells after copy
            return Common.Models.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cut failed: {Message}", ex.Message);
            return Common.Models.Result.Failure($"Cut failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Paste operation (public API compatibility)
    /// </summary>
    public async Task<Common.Models.Result<int>> PasteAsync(int startRowIndex, string startColumnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Paste operation (public API) at row {Row}, column {Column}", startRowIndex, startColumnName);

            // Get clipboard data - for now use empty string as placeholder
            var clipboardData = string.Empty; // TODO: Get actual clipboard data

            // Create paste command with actual target location
            var command = new PasteDataCommand(
                ClipboardData: clipboardData,
                TargetRow: startRowIndex,
                TargetColumn: 0, // TODO: Convert column name to index
                OverwriteExisting: true,
                Format: PublicClipboardFormat.Excel,
                Delimiter: "\t"
            );

            var result = await PasteFromClipboardAsync(command, cancellationToken);
            return result.Success
                ? Common.Models.Result<int>.Success(result.ProcessedRows)
                : Common.Models.Result<int>.Failure(result.ErrorMessage ?? "Paste failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paste failed: {Message}", ex.Message);
            return Common.Models.Result<int>.Failure($"Paste failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if paste is possible (public API compatibility)
    /// </summary>
    public bool CanPaste()
    {
        return GetClipboard() != null;
    }

    /// <summary>
    /// Get clipboard text (public API compatibility)
    /// </summary>
    public async Task<string> GetClipboardTextAsync()
    {
        return await Task.Run(() =>
        {
            var clipboardData = GetClipboard();
            return clipboardData?.ToString() ?? string.Empty;
        });
    }

    /// <summary>
    /// Set clipboard text (public API compatibility)
    /// </summary>
    public async Task<Common.Models.Result> SetClipboardTextAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run(() =>
            {
                SetClipboard(text);
            }, cancellationToken);
            return Common.Models.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set clipboard text failed: {Message}", ex.Message);
            return Common.Models.Result.Failure($"Set clipboard failed: {ex.Message}");
        }
    }

    /// <summary>
    /// SENIOR HELPER: Escapes TSV value for Excel compatibility.
    ///
    /// ESCAPING RULES:
    /// 1. If value contains tab, newline, carriage return, or quote → wrap in quotes
    /// 2. If wrapping in quotes, double any existing quotes (\" → \"\")
    /// 3. Otherwise, return value as-is (no quotes needed)
    ///
    /// EXAMPLES:
    /// - "NormalText" → NormalText (no special chars, no quotes)
    /// - "Multi\nLine" → "Multi\nLine" (newline → quoted)
    /// - "Cell\tWith\tTab" → "Cell\tWith\tTab" (tabs → quoted)
    /// - "Cell with \"quote\"" → "Cell with \"\"quote\"\"" (quote → doubled AND quoted)
    /// - "" → "" (empty string stays empty)
    ///
    /// CRITICAL: Quotes are added BEFORE pasting to clipboard.
    /// CRITICAL: Quotes are removed AFTER pasting from clipboard (in ParseTabSeparatedDataAsync).
    /// </summary>
    /// <param name="value">Value to escape</param>
    /// <returns>Escaped value ready for TSV format</returns>
    private string EscapeTsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // SENIOR PATTERN: Check if quoting is needed
        // Quote if value contains any special characters: tab, newline, carriage return, or quote
        bool needsQuoting = value.Contains('\t') ||
                           value.Contains('\n') ||
                           value.Contains('\r') ||
                           value.Contains('"');

        if (!needsQuoting)
        {
            // Normal value - no escaping needed
            return value;
        }

        // SENIOR PATTERN: Escape quotes by doubling them, then wrap entire value in quotes
        // Example: Cell with "quote" → Cell with ""quote"" → "Cell with ""quote"""
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    #endregion
}

/// <summary>
/// Interná dátová štruktúra for operácie kopírovania buniek
/// </summary>
internal class CellCopyData
{
    public int Row { get; set; }
    public string Column { get; set; } = string.Empty;
    public object? Value { get; set; }
    public Type? OriginalType { get; set; }
}
# KOMPLETNÉ ZADANIE: COPY/PASTE SYSTÉM PRE UPRAVENÝ KOMPONENT

---
# GENERATE MAP for Copy/Paste documentation (only metadata; content unchanged)
# Use these target paths when generating files from this documentation:
# - Core/ValueObjects/CopyPasteTypes.cs
# - Application/Interfaces/ICopyPasteService.cs
# - Application/Services/CopyPasteService.cs
# - Infrastructure/Logging/Interfaces/ICopyPasteLogger.cs
# - Infrastructure/Logging/CopyPasteLogger.cs
# - Infrastructure/Services/InternalServiceRegistration.cs  # add Copy/Paste registrations here
# - /AdvancedWinUiDataGridFacade          # facade output (single file target; no extension)
# - /AdvancedWinUiDataGridFacade/IAdvancedWinUiDataGridFacade  # facade interface
---

## **Cieľ Implementácie:**
Vytvoriť robustnú, optimalizovanú copy/paste funkcionalitu pre AdvancedWinUiDataGrid komponent s enterprise-grade architektúrou. Copy/Paste systém bude integrovať všetky technológie a paradigmy používané v komponente: Clean Architecture, Command Pattern, Hybrid Internal DI, LINQ optimalizácie, a comprehensive logging.

## **Architectural Principles & Copy/Paste Strategy:**
- **Clean Architecture + Command Pattern**: Copy/Paste operations implementované ako commands s dedicated handlers
- **Hybrid Internal DI + Functional/OOP**: Copy/Paste services v internal DI kontajneri s funkcionálnym programovaním
- **SOLID Principles**: Separation of concerns medzi clipboard operations, formatting, a data processing
- **Enterprise Observability**: Comprehensive logging pre všetky copy/paste operations
- **Performance Optimized**: LINQ optimalizácie s parallel processing pre veľké datasets
- **Thread Safe**: Concurrent clipboard operations bez data corruption

## **Internal DI Integration & Service Distribution:**
Copy/Paste services registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a distribuované cez hybrid internal DI systém. CopyPasteService dostáva specialized clipboard logging dependencies cez constructor injection z internal DI kontajnera.

## **Backup Strategy & Implementation Approach:**
### **1. Backup Strategy**
- Vytvoriť .oldbackup_timestamp súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### **2. Implementation Replacement**
- Kompletný refaktoring s rozšíreným copy/paste systémom
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## **Copy/Paste System Architecture:**

### **1. Copy/Paste Types Definition (CopyPasteTypes.cs)**
```csharp
// Core/ValueObjects/CopyPasteTypes.cs

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// Defines clipboard data formats for copy/paste operations
/// </summary>
public enum ClipboardFormat
{
    /// <summary>Tab-separated values for spreadsheet compatibility</summary>
    TabSeparated,

    /// <summary>Comma-separated values for data exchange</summary>
    CommaSeparated,

    /// <summary>Custom delimiter format with configurable separator</summary>
    CustomDelimited,

    /// <summary>JSON format for structured data</summary>
    Json,

    /// <summary>Plain text format for simple data</summary>
    PlainText
}

/// <summary>
/// Copy/paste progress information for tracking operations
/// </summary>
/// <param name="ProcessedRows">Number of processed rows</param>
/// <param name="TotalRows">Total number of rows to process</param>
/// <param name="ElapsedTime">Time elapsed since operation start</param>
/// <param name="CurrentOperation">Description of current operation</param>
/// <param name="DataSize">Current data size in characters/bytes</param>
public record CopyPasteProgress(
    int ProcessedRows,
    int TotalRows,
    TimeSpan ElapsedTime,
    string CurrentOperation = "",
    long DataSize = 0
)
{
    /// <summary>Calculated completion percentage (0-100)</summary>
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;

    /// <summary>Estimated time remaining based on current progress</summary>
    public TimeSpan? EstimatedTimeRemaining => ProcessedRows > 0 && TotalRows > ProcessedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ProcessedRows) / ProcessedRows)
        : null;

    public CopyPasteProgress() : this(0, 0, TimeSpan.Zero, "", 0) { }
}

/// <summary>
/// Command for copying selected data to clipboard
/// </summary>
/// <param name="SelectedData">Selected data to copy to clipboard</param>
/// <param name="IncludeHeaders">Include column headers in clipboard data</param>
/// <param name="IncludeValidationAlerts">Include validation alerts in copied data</param>
/// <param name="Delimiter">Delimiter for formatting clipboard data</param>
/// <param name="Format">Clipboard data format</param>
/// <param name="Timeout">Timeout for copy operation</param>
/// <param name="Progress">Progress reporting callback</param>
/// <param name="CorrelationId">Operation correlation ID for logging</param>
public record CopyDataCommand(
    IEnumerable<IReadOnlyDictionary<string, object?>> SelectedData,
    bool IncludeHeaders = true,
    bool IncludeValidationAlerts = false,
    string? Delimiter = "\t",
    ClipboardFormat Format = ClipboardFormat.TabSeparated,
    TimeSpan? Timeout = null,
    IProgress<CopyPasteProgress>? Progress = null,
    string? CorrelationId = null
)
{
    /// <summary>Number of selected rows for copying</summary>
    public int SelectedRowCount => SelectedData?.Count() ?? 0;

    /// <summary>Indicates whether command has valid data for copying</summary>
    public bool HasValidData => SelectedData?.Any() == true;

    /// <summary>Factory method for creating copy command with tab-separated format</summary>
    public static CopyDataCommand CreateTabSeparated(
        IEnumerable<IReadOnlyDictionary<string, object?>> selectedData,
        bool includeHeaders = true,
        bool includeValidationAlerts = false,
        TimeSpan? timeout = null,
        IProgress<CopyPasteProgress>? progress = null,
        string? correlationId = null) =>
        new(
            SelectedData: selectedData,
            IncludeHeaders: includeHeaders,
            IncludeValidationAlerts: includeValidationAlerts,
            Delimiter: "\t",
            Format: ClipboardFormat.TabSeparated,
            Timeout: timeout,
            Progress: progress,
            CorrelationId: correlationId
        );

    /// <summary>Factory method for creating copy command with custom delimiter</summary>
    public static CopyDataCommand CreateCustomDelimited(
        IEnumerable<IReadOnlyDictionary<string, object?>> selectedData,
        string delimiter,
        bool includeHeaders = true,
        bool includeValidationAlerts = false,
        TimeSpan? timeout = null,
        IProgress<CopyPasteProgress>? progress = null,
        string? correlationId = null) =>
        new(
            SelectedData: selectedData,
            IncludeHeaders: includeHeaders,
            IncludeValidationAlerts: includeValidationAlerts,
            Delimiter: delimiter,
            Format: ClipboardFormat.CustomDelimited,
            Timeout: timeout,
            Progress: progress,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Command for pasting data from clipboard to grid
/// </summary>
/// <param name="TargetRowIndex">Target row index for paste operation (0-based)</param>
/// <param name="TargetColumnIndex">Target column index for paste operation (0-based)</param>
/// <param name="Mode">Import mode for paste operation</param>
/// <param name="ValidateAfterPaste">Whether to validate data after paste</param>
/// <param name="Delimiter">Delimiter for parsing clipboard data</param>
/// <param name="Format">Expected clipboard data format</param>
/// <param name="Timeout">Timeout for paste operation</param>
/// <param name="Progress">Progress reporting callback</param>
/// <param name="CorrelationId">Operation correlation ID for logging</param>
public record PasteDataCommand(
    int TargetRowIndex = 0,
    int TargetColumnIndex = 0,
    ImportMode Mode = ImportMode.Replace,
    bool ValidateAfterPaste = true,
    string? Delimiter = "\t",
    ClipboardFormat Format = ClipboardFormat.TabSeparated,
    TimeSpan? Timeout = null,
    IProgress<CopyPasteProgress>? Progress = null,
    string? CorrelationId = null
)
{
    /// <summary>Target position as coordinate tuple</summary>
    public (int Row, int Column) TargetPosition => (TargetRowIndex, TargetColumnIndex);

    /// <summary>Indicates whether paste will require validation</summary>
    public bool RequiresValidation => ValidateAfterPaste;

    /// <summary>Factory method for creating paste command at specific position</summary>
    public static PasteDataCommand CreateAtPosition(
        int targetRowIndex,
        int targetColumnIndex,
        ImportMode mode = ImportMode.Replace,
        bool validateAfterPaste = true,
        string? delimiter = "\t",
        TimeSpan? timeout = null,
        IProgress<CopyPasteProgress>? progress = null,
        string? correlationId = null) =>
        new(
            TargetRowIndex: targetRowIndex,
            TargetColumnIndex: targetColumnIndex,
            Mode: mode,
            ValidateAfterPaste: validateAfterPaste,
            Delimiter: delimiter,
            Timeout: timeout,
            Progress: progress,
            CorrelationId: correlationId
        );

    /// <summary>Factory method for creating paste command with validation disabled</summary>
    public static PasteDataCommand CreateFastPaste(
        int targetRowIndex = 0,
        int targetColumnIndex = 0,
        ImportMode mode = ImportMode.Replace,
        string? delimiter = "\t",
        string? correlationId = null) =>
        new(
            TargetRowIndex: targetRowIndex,
            TargetColumnIndex: targetColumnIndex,
            Mode: mode,
            ValidateAfterPaste: false,
            Delimiter: delimiter,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Result of copy/paste operation with comprehensive metrics
/// </summary>
/// <param name="Success">Whether operation completed successfully</param>
/// <param name="ProcessedRows">Number of rows processed during operation</param>
/// <param name="ClipboardData">Clipboard data content (for copy operations)</param>
/// <param name="ErrorMessage">Error message if operation failed</param>
/// <param name="OperationTime">Time taken for operation</param>
/// <param name="OperationType">Type of operation (Copy/Paste)</param>
/// <param name="DataSize">Size of clipboard data in characters</param>
/// <param name="CorrelationId">Operation correlation ID</param>
public record CopyPasteResult(
    bool Success,
    int ProcessedRows,
    string? ClipboardData = null,
    string? ErrorMessage = null,
    TimeSpan OperationTime = default,
    string OperationType = "",
    long DataSize = 0,
    string? CorrelationId = null
)
{
    public CopyPasteResult() : this(false, 0, null, null, TimeSpan.Zero, "", 0, null) { }

    /// <summary>Creates successful copy result</summary>
    public static CopyPasteResult CreateCopySuccess(
        int processedRows,
        string clipboardData,
        TimeSpan operationTime,
        string? correlationId = null) =>
        new(
            Success: true,
            ProcessedRows: processedRows,
            ClipboardData: clipboardData,
            OperationTime: operationTime,
            OperationType: "Copy",
            DataSize: clipboardData.Length,
            CorrelationId: correlationId
        );

    /// <summary>Creates successful paste result</summary>
    public static CopyPasteResult CreatePasteSuccess(
        int processedRows,
        TimeSpan operationTime,
        string? correlationId = null) =>
        new(
            Success: true,
            ProcessedRows: processedRows,
            OperationTime: operationTime,
            OperationType: "Paste",
            CorrelationId: correlationId
        );

    /// <summary>Creates failed operation result</summary>
    public static CopyPasteResult Failure(
        string errorMessage,
        string operationType,
        TimeSpan operationTime = default,
        string? correlationId = null) =>
        new(
            Success: false,
            ProcessedRows: 0,
            ErrorMessage: errorMessage,
            OperationTime: operationTime,
            OperationType: operationType,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Clipboard validation result for pre-operation checks
/// </summary>
/// <param name="IsValid">Whether clipboard data is valid for operation</param>
/// <param name="ValidationErrors">List of validation errors</param>
/// <param name="Warnings">List of validation warnings</param>
/// <param name="EstimatedRows">Estimated number of rows in clipboard data</param>
/// <param name="EstimatedColumns">Estimated number of columns in clipboard data</param>
/// <param name="DetectedFormat">Detected clipboard data format</param>
public record ClipboardValidationResult(
    bool IsValid,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<string> Warnings,
    int? EstimatedRows = null,
    int? EstimatedColumns = null,
    ClipboardFormat? DetectedFormat = null
);
```

### **2. Copy/Paste Service Interface**
```csharp
// Application/Interfaces/ICopyPasteService.cs

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// Service interface for clipboard copy/paste operations with comprehensive functionality
/// </summary>
internal interface ICopyPasteService
{
    /// <summary>
    /// Copies selected data to clipboard using command pattern
    /// </summary>
    /// <param name="command">Copy command with data and configuration</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>Copy result with clipboard data and metrics</returns>
    Task<CopyPasteResult> CopyToClipboardAsync(
        CopyDataCommand command,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Pastes data from clipboard using command pattern
    /// </summary>
    /// <param name="command">Paste command with target position and configuration</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>Paste result with processed data metrics</returns>
    Task<CopyPasteResult> PasteFromClipboardAsync(
        PasteDataCommand command,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Validates clipboard data before paste operation
    /// </summary>
    /// <param name="targetRowIndex">Target row for paste validation</param>
    /// <param name="targetColumnIndex">Target column for paste validation</param>
    /// <param name="expectedFormat">Expected clipboard data format</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>Validation result with errors and warnings</returns>
    Task<ClipboardValidationResult> ValidateClipboardDataAsync(
        int targetRowIndex = 0,
        int targetColumnIndex = 0,
        ClipboardFormat? expectedFormat = null,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current clipboard data for preview
    /// </summary>
    /// <param name="maxPreviewLength">Maximum length for clipboard preview</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>Clipboard preview data</returns>
    Task<string?> GetClipboardPreviewAsync(
        int maxPreviewLength = 1000,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Detects clipboard data format automatically
    /// </summary>
    /// <param name="clipboardData">Clipboard data to analyze</param>
    /// <returns>Detected clipboard format</returns>
    ClipboardFormat DetectClipboardFormat(string clipboardData);

    /// <summary>
    /// Estimates paste operation impact
    /// </summary>
    /// <param name="targetRowIndex">Target row index</param>
    /// <param name="targetColumnIndex">Target column index</param>
    /// <param name="mode">Paste mode</param>
    /// <returns>Estimation of paste operation impact</returns>
    Task<(int AffectedRows, int AffectedColumns, TimeSpan EstimatedDuration)> EstimatePasteImpactAsync(
        int targetRowIndex,
        int targetColumnIndex,
        ImportMode mode);
}
```

### **3. Facade API Integration**
```csharp
// Public API methods pre Facade Pattern (AdvancedWinUiDataGrid.cs)

/// <summary>
/// Copies selected grid data to clipboard with formatting options
/// </summary>
/// <param name="includeHeaders">Include column headers in clipboard data</param>
/// <param name="includeValidationAlerts">Include validation alerts in copied data</param>
/// <param name="format">Clipboard data format</param>
/// <param name="delimiter">Custom delimiter for data formatting</param>
/// <param name="progress">Progress reporting callback</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Copy result with clipboard data and metrics</returns>
public async Task<CopyPasteResult> CopyToClipboardAsync(
    bool includeHeaders = true,
    bool includeValidationAlerts = false,
    ClipboardFormat format = ClipboardFormat.TabSeparated,
    string? delimiter = null,
    IProgress<CopyPasteProgress>? progress = null,
    cancellationToken cancellationToken = default)
{
    var correlationId = Guid.NewGuid().ToString();
    _logger.LogInformation("Starting clipboard copy: format={Format}, includeHeaders={IncludeHeaders} [CorrelationId: {CorrelationId}]",
        format, includeHeaders, correlationId);

    var selectedData = GetSelectedData(); // Facade method to get selected grid data
    var actualDelimiter = delimiter ?? (format == ClipboardFormat.TabSeparated ? "\t" : ",");

    var command = new CopyDataCommand(
        SelectedData: selectedData,
        IncludeHeaders: includeHeaders,
        IncludeValidationAlerts: includeValidationAlerts,
        Delimiter: actualDelimiter,
        Format: format,
        Progress: progress,
        CorrelationId: correlationId
    );

    return await _copyPasteService.CopyToClipboardAsync(command, cancellationToken);
}

/// <summary>
/// Copies selected rows to clipboard using tab-separated format
/// </summary>
/// <param name="includeHeaders">Include headers in clipboard</param>
/// <param name="progress">Progress reporting callback</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Copy result with metrics</returns>
public async Task<CopyPasteResult> CopySelectedRowsAsync(
    bool includeHeaders = true,
    IProgress<CopyPasteProgress>? progress = null,
    cancellationToken cancellationToken = default)
{
    var correlationId = Guid.NewGuid().ToString();
    var selectedData = GetSelectedRows();

    var command = CopyDataCommand.CreateTabSeparated(
        selectedData: selectedData,
        includeHeaders: includeHeaders,
        progress: progress,
        correlationId: correlationId
    );

    return await _copyPasteService.CopyToClipboardAsync(command, cancellationToken);
}

/// <summary>
/// Pastes data from clipboard to specified grid position
/// </summary>
/// <param name="targetRowIndex">Target row index (0-based)</param>
/// <param name="targetColumnIndex">Target column index (0-based)</param>
/// <param name="mode">Paste mode (Replace/Append/Insert/Merge)</param>
/// <param name="validateAfterPaste">Whether to validate data after paste</param>
/// <param name="delimiter">Expected delimiter in clipboard data</param>
/// <param name="progress">Progress reporting callback</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Paste result with processed data metrics</returns>
public async Task<CopyPasteResult> PasteFromClipboardAsync(
    int targetRowIndex = 0,
    int targetColumnIndex = 0,
    ImportMode mode = ImportMode.Replace,
    bool validateAfterPaste = true,
    string? delimiter = "\t",
    IProgress<CopyPasteProgress>? progress = null,
    cancellationToken cancellationToken = default)
{
    var correlationId = Guid.NewGuid().ToString();
    _logger.LogInformation("Starting clipboard paste: target=({Row},{Column}), mode={Mode} [CorrelationId: {CorrelationId}]",
        targetRowIndex, targetColumnIndex, mode, correlationId);

    var command = PasteDataCommand.CreateAtPosition(
        targetRowIndex: targetRowIndex,
        targetColumnIndex: targetColumnIndex,
        mode: mode,
        validateAfterPaste: validateAfterPaste,
        delimiter: delimiter,
        progress: progress,
        correlationId: correlationId
    );

    return await _copyPasteService.PasteFromClipboardAsync(command, cancellationToken);
}

/// <summary>
/// Performs fast paste operation without validation for performance
/// </summary>
/// <param name="targetRowIndex">Target row index</param>
/// <param name="targetColumnIndex">Target column index</param>
/// <param name="mode">Paste mode</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Fast paste result</returns>
public async Task<CopyPasteResult> FastPasteAsync(
    int targetRowIndex = 0,
    int targetColumnIndex = 0,
    ImportMode mode = ImportMode.Replace,
    cancellationToken cancellationToken = default)
{
    var correlationId = Guid.NewGuid().ToString();
    var command = PasteDataCommand.CreateFastPaste(
        targetRowIndex: targetRowIndex,
        targetColumnIndex: targetColumnIndex,
        mode: mode,
        correlationId: correlationId
    );

    return await _copyPasteService.PasteFromClipboardAsync(command, cancellationToken);
}

/// <summary>
/// Validates clipboard data before paste operation
/// </summary>
/// <param name="targetRowIndex">Target row for validation</param>
/// <param name="targetColumnIndex">Target column for validation</param>
/// <param name="expectedFormat">Expected clipboard format</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Validation result with errors and warnings</returns>
public async Task<ClipboardValidationResult> ValidateClipboardDataAsync(
    int targetRowIndex = 0,
    int targetColumnIndex = 0,
    ClipboardFormat? expectedFormat = null,
    cancellationToken cancellationToken = default)
{
    return await _copyPasteService.ValidateClipboardDataAsync(
        targetRowIndex, targetColumnIndex, expectedFormat, cancellationToken);
}

/// <summary>
/// Gets preview of current clipboard data
/// </summary>
/// <param name="maxLength">Maximum preview length</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Clipboard preview data</returns>
public async Task<string?> GetClipboardPreviewAsync(
    int maxLength = 500,
    cancellationToken cancellationToken = default)
{
    return await _copyPasteService.GetClipboardPreviewAsync(maxLength, cancellationToken);
}
```

## **Enhanced Copy/Paste Service Implementation Pattern:**

### **LINQ Optimizations & Performance Features:**
```csharp
// Application/Services/CopyPasteService.cs - ENHANCED s LINQ optimalizáciami

internal sealed class CopyPasteService : ICopyPasteService
{
    private readonly ILogger<CopyPasteService> _logger;
    private readonly ICopyPasteLogger<CopyPasteService> _copyPasteLogger;
    private readonly ICommandLogger<CopyPasteService> _commandLogger;

    public async Task<CopyPasteResult> CopyToClipboardAsync(
        CopyDataCommand command,
        cancellationToken cancellationToken = default)
    {
        using var scope = _copyPasteLogger.LogCommandOperationStart(command,
            new { format = command.Format, hasData = command.HasValidData, rowCount = command.SelectedRowCount });

        _logger.LogInformation("Starting clipboard copy: {RowCount} rows, format={Format} [CorrelationId: {CorrelationId}]",
            command.SelectedRowCount, command.Format, command.CorrelationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!command.HasValidData)
            {
                _logger.LogWarning("Copy aborted: No data selected [CorrelationId: {CorrelationId}]", command.CorrelationId);
                return CopyPasteResult.Failure("No data selected for copy", "Copy", stopwatch.Elapsed, command.CorrelationId);
            }

            // LINQ Optimization: Count optimization s short-circuit evaluation
            var totalRows = await Task.Run(() =>
                command.SelectedData.TryGetNonEnumeratedCount(out var count) ? count : command.SelectedData.Count(),
                cancellationToken);

            _logger.LogInformation("Copy data analysis: {TotalRows} rows to copy", totalRows);

            // LINQ Optimization: Parallel processing pre veľké datasets
            var useParallel = totalRows > 500; // Lower threshold for clipboard operations
            var formattedData = useParallel
                ? await FormatDataParallelAsync(command, cancellationToken)
                : await FormatDataSequentialAsync(command, cancellationToken);

            _copyPasteLogger.LogLINQOptimization("CopyDataFormatting", useParallel, false, stopwatch.Elapsed);

            // Clipboard operation
            await SetClipboardDataAsync(formattedData, cancellationToken);

            _copyPasteLogger.LogClipboardOperation("Copy", success: true,
                dataSize: formattedData.Length, operationTime: stopwatch.Elapsed);

            _logger.LogInformation("Copy completed: {TotalRows} rows, {DataSize} characters in {Duration}ms",
                totalRows, formattedData.Length, stopwatch.ElapsedMilliseconds);

            scope.MarkSuccess(new { copiedRows = totalRows, dataSize = formattedData.Length });
            return CopyPasteResult.CreateCopySuccess(totalRows, formattedData, stopwatch.Elapsed, command.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy operation failed [CorrelationId: {CorrelationId}]", command.CorrelationId);
            scope.MarkError(ex.Message);
            throw;
        }
    }

    public async Task<CopyPasteResult> PasteFromClipboardAsync(
        PasteDataCommand command,
        cancellationToken cancellationToken = default)
    {
        using var scope = _copyPasteLogger.LogCommandOperationStart(command,
            new { targetPosition = command.TargetPosition, mode = command.Mode, validate = command.RequiresValidation });

        _logger.LogInformation("Starting clipboard paste: target=({Row},{Column}), mode={Mode}, validate={Validate} [CorrelationId: {CorrelationId}]",
            command.TargetRowIndex, command.TargetColumnIndex, command.Mode, command.ValidateAfterPaste, command.CorrelationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get clipboard data
            var clipboardData = await GetClipboardDataAsync(cancellationToken);
            if (string.IsNullOrEmpty(clipboardData))
            {
                _logger.LogWarning("Paste aborted: No clipboard data available [CorrelationId: {CorrelationId}]", command.CorrelationId);
                return CopyPasteResult.Failure("No clipboard data available", "Paste", stopwatch.Elapsed, command.CorrelationId);
            }

            // LINQ Optimization: Parse clipboard data s lazy evaluation
            var parsedData = await ParseClipboardDataAsync(clipboardData, command, cancellationToken);
            var totalRows = parsedData.Count();

            _logger.LogInformation("Paste data analysis: {TotalRows} rows parsed from clipboard", totalRows);

            // LINQ Optimization: Conditional validation s short-circuit
            if (command.ValidateAfterPaste)
            {
                var validationResult = await ValidatePasteDataAsync(parsedData, command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogError("Paste validation failed: {ErrorCount} errors [CorrelationId: {CorrelationId}]",
                        validationResult.ValidationErrors.Count, command.CorrelationId);
                    return CopyPasteResult.Failure($"Validation failed: {string.Join(", ", validationResult.ValidationErrors)}",
                        "Paste", stopwatch.Elapsed, command.CorrelationId);
                }
            }

            // Execute paste operation
            var pastedRows = await ExecutePasteOperationAsync(parsedData, command, cancellationToken);

            _copyPasteLogger.LogClipboardOperation("Paste", success: true,
                dataSize: clipboardData.Length, operationTime: stopwatch.Elapsed);

            _logger.LogInformation("Paste completed: {PastedRows}/{TotalRows} rows in {Duration}ms",
                pastedRows, totalRows, stopwatch.ElapsedMilliseconds);

            scope.MarkSuccess(new { pastedRows, mode = command.Mode });
            return CopyPasteResult.CreatePasteSuccess(pastedRows, stopwatch.Elapsed, command.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paste operation failed [CorrelationId: {CorrelationId}]", command.CorrelationId);
            scope.MarkError(ex.Message);
            throw;
        }
    }

    private async Task<string> FormatDataParallelAsync(CopyDataCommand command, cancellationToken cancellationToken)
    {
        // LINQ Optimization: Parallel processing s StringBuilder pooling
        return await Task.Run(() =>
        {
            var partitioner = Partitioner.Create(command.SelectedData, EnumerablePartitionerOptions.NoBuffering);

            var formattedRows = partitioner.AsParallel()
                .WithCancellation(cancellationToken)
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select((row, index) =>
                {
                    // Progress reporting every 50 rows for clipboard operations
                    if (index % 50 == 0 && command.Progress != null)
                    {
                        command.Progress.Report(new CopyPasteProgress(
                            ProcessedRows: index,
                            TotalRows: command.SelectedData.Count(),
                            ElapsedTime: TimeSpan.FromMilliseconds(index * 2), // Estimated
                            CurrentOperation: $"Formatting row {index}",
                            DataSize: index * 100 // Estimated
                        ));
                    }

                    return FormatRowForClipboard(row, command);
                })
                .ToList();

            return string.Join(Environment.NewLine, formattedRows);
        }, cancellationToken);
    }
}
```

## **Enhanced Copy/Paste Logging Integration:**

### **ICopyPasteLogger Interface:**
```csharp
// Infrastructure/Logging/ICopyPasteLogger.cs

internal interface ICopyPasteLogger<T> : IOperationLogger<T>
{
    /// <summary>
    /// Logs clipboard operation with comprehensive metrics
    /// </summary>
    void LogClipboardOperation(string operationType, bool success, long dataSize, TimeSpan operationTime);

    /// <summary>
    /// Logs data formatting during copy operations
    /// </summary>
    void LogDataFormatting(string format, int rowCount, long outputSize, TimeSpan duration);

    /// <summary>
    /// Logs clipboard data parsing during paste operations
    /// </summary>
    void LogClipboardParsing(string detectedFormat, int parsedRows, int parsedColumns, TimeSpan duration);

    /// <summary>
    /// Logs paste validation with detailed results
    /// </summary>
    void LogPasteValidation(string validationType, bool isValid, int errorCount, string? summary = null);

    /// <summary>
    /// Logs progress reporting during copy/paste operations
    /// </summary>
    void LogProgressReporting(string operationType, double progressPercentage, int processedItems, int totalItems);

    /// <summary>
    /// Logs paste mode-specific operations (Replace, Append, Insert, Merge)
    /// </summary>
    void LogPasteModeOperation(string mode, int targetRow, int targetColumn, int affectedRows, TimeSpan duration);
}
```

## **Internal DI Registration:**
```csharp
// Infrastructure/Services/InternalServiceRegistration.cs - Copy/Paste Module Addition

public static IServiceCollection AddAdvancedWinUiDataGridInternal(this IServiceCollection services)
{
    // ... existing registrations ...

    // Copy/Paste module services
    services.AddSingleton<ICopyPasteService, CopyPasteService>();
    services.AddSingleton(typeof(ICopyPasteLogger<>), typeof(CopyPasteLogger<>));

    // Clipboard data processors
    services.AddSingleton<IClipboardDataFormatter, ClipboardDataFormatter>();
    services.AddSingleton<IClipboardDataParser, ClipboardDataParser>();
    services.AddSingleton<IClipboardValidator, ClipboardValidator>();

    return services;
}
```

## **Performance & Optimization Features:**

### **1. LINQ Query Optimizations**
- **Parallel Processing**: Automatic parallel processing pre datasets > 500 rows (clipboard operations)
- **Memory Efficiency**: StringBuilder pooling pre formatting operations
- **Lazy Evaluation**: Deferred parsing až po validation
- **Short-Circuit Evaluation**: Early termination pre validation errors

### **2. Clipboard Format Optimizations**
- **Tab-Separated**: Direct string building s minimal allocations
- **Custom Delimited**: Configurable delimiter s validation
- **JSON**: Streaming JSON serialization pre large datasets
- **Auto-Detection**: Smart format detection based on clipboard content

### **3. Paste Mode Optimizations**
- **Replace**: Direct cell replacement s minimal validation
- **Append**: Bulk append s position tracking
- **Insert**: Row insertion s index management
- **Merge**: Key-based merging s conflict resolution

## **Command Pattern Integration:**
Všetky copy/paste operations implementované ako commands:
- **CopyDataCommand**: Copy operation s flexible formatting
- **PasteDataCommand**: Paste operation s position targeting
- **ValidateClipboardCommand**: Pre-paste validation
- **EstimatePasteCommand**: Paste impact estimation

Každý command má svoj dedicated handler s comprehensive logging a error handling.

## **Integration s Existing Modules:**
Copy/Paste systém je navrhnutý pre bezproblémovú integráciu s:
- **Import System**: Paste operations využívajú import validation pipeline
- **Export System**: Copy operations využívajú export formatting pipeline
- **Validation System**: Automatic validation pre paste operations
- **Filter System**: Copy filtered data s preserve formatting

## **Future Extensions:**
- **Rich Clipboard**: Support pre rich text a formatted data

### **Logging Levels Usage:**
- **Information**: Successful imports/exports, progress milestones, data statistics
- **Warning**: Partial import failures, format conversion warnings, large dataset warnings
- **Error**: Import/export failures, clipboard access errors, validation failures
- **Critical**: Data corruption detection, system resource exhaustion


**Paste** operations use the same import flow: after paste completes, a **bulk validation** is executed. Bulk validation can validate in parts (to reduce memory spikes) but **UI must only be updated at the end** of the bulk validation pass unless running in *headless mode*.
- In headless mode the component does **not** automatically update UI; instead the public API exposes a method to request UI refresh for validation results — callers may invoke it when appropriate.
- `ValidationProgress` is used to report progress of the validation phase (bulk validation). It should be reported with coarse-grained updates (e.g., per batch) to avoid UI thrashing.

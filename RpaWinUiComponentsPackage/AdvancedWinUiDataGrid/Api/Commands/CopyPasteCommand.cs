namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

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
    PublicClipboardFormat Format = PublicClipboardFormat.Excel,
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
            Format: PublicClipboardFormat.Excel,
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
            Format: PublicClipboardFormat.Excel,
            Timeout: timeout,
            Progress: progress,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Command for pasting data from clipboard
/// </summary>
/// <param name="ClipboardData">Data from clipboard to paste</param>
/// <param name="TargetRow">Target row index for pasting (0-based)</param>
/// <param name="TargetColumn">Target column index for pasting (0-based)</param>
/// <param name="OverwriteExisting">Whether to overwrite existing data</param>
/// <param name="Format">Format of clipboard data</param>
/// <param name="Delimiter">Delimiter used in clipboard data</param>
/// <param name="Timeout">Timeout for paste operation</param>
/// <param name="Progress">Progress reporting callback</param>
/// <param name="CorrelationId">Operation correlation ID for logging</param>
public record PasteDataCommand(
    string ClipboardData,
    int TargetRow = 0,
    int TargetColumn = 0,
    bool OverwriteExisting = true,
    PublicClipboardFormat Format = PublicClipboardFormat.Excel,
    string? Delimiter = "\t",
    bool ValidateAfterPaste = true,
    TimeSpan? Timeout = null,
    IProgress<CopyPasteProgress>? Progress = null,
    string? CorrelationId = null
)
{
    /// <summary>Indicates whether command has valid clipboard data</summary>
    public bool HasValidData => !string.IsNullOrWhiteSpace(ClipboardData);

    /// <summary>Factory method for creating paste command with tab-separated format</summary>
    public static PasteDataCommand CreateTabSeparated(
        string clipboardData,
        int targetRow = 0,
        int targetColumn = 0,
        bool overwriteExisting = true,
        TimeSpan? timeout = null,
        IProgress<CopyPasteProgress>? progress = null,
        string? correlationId = null) =>
        new(
            ClipboardData: clipboardData,
            TargetRow: targetRow,
            TargetColumn: targetColumn,
            OverwriteExisting: overwriteExisting,
            Format: PublicClipboardFormat.Excel,
            Delimiter: "\t",
            Timeout: timeout,
            Progress: progress,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Result of copy/paste operation
/// </summary>
/// <param name="Success">Whether operation completed successfully</param>
/// <param name="ProcessedRows">Number of rows processed</param>
/// <param name="ProcessedColumns">Number of columns processed</param>
/// <param name="OperationTime">Time taken for operation</param>
/// <param name="DataSize">Size of data processed</param>
/// <param name="ErrorMessage">Error message if operation failed</param>
/// <param name="CorrelationId">Operation correlation ID</param>
public record CopyPasteResult(
    bool Success,
    int ProcessedRows,
    int ProcessedColumns,
    TimeSpan OperationTime,
    long DataSize,
    string? ErrorMessage = null,
    string? CorrelationId = null
)
{
    public CopyPasteResult() : this(false, 0, 0, TimeSpan.Zero, 0) { }

    /// <summary>Creates successful copy/paste result</summary>
    public static CopyPasteResult CreateSuccess(
        int processedRows,
        int processedColumns,
        TimeSpan operationTime,
        long dataSize,
        string? correlationId = null) =>
        new(
            Success: true,
            ProcessedRows: processedRows,
            ProcessedColumns: processedColumns,
            OperationTime: operationTime,
            DataSize: dataSize,
            CorrelationId: correlationId
        );

    /// <summary>Creates failed copy/paste result</summary>
    public static CopyPasteResult Failure(
        string errorMessage,
        TimeSpan operationTime,
        string? correlationId = null) =>
        new(
            Success: false,
            ProcessedRows: 0,
            ProcessedColumns: 0,
            OperationTime: operationTime,
            DataSize: 0,
            ErrorMessage: errorMessage,
            CorrelationId: correlationId
        );
}
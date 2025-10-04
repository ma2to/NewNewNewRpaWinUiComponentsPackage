namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Progress information for validation operations
/// </summary>
internal class ValidationProgress
{
    /// <summary>
    /// Gets the number of rows processed
    /// </summary>
    public int ProcessedRows { get; init; }

    /// <summary>
    /// Gets the total number of rows to process
    /// </summary>
    public int TotalRows { get; init; }

    /// <summary>
    /// Gets the progress percentage (0-100)
    /// </summary>
    public double ProgressPercent { get; init; }

    /// <summary>
    /// Gets the current error count
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Gets the current warning count
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Gets the current status message
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Gets whether the operation is complete
    /// </summary>
    public bool IsComplete => ProcessedRows >= TotalRows;
}

/// <summary>
/// Context information for validation operations
/// </summary>
internal class ValidationContext
{
    /// <summary>
    /// Gets the row index being validated
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Gets the column names to validate (if specific columns)
    /// </summary>
    public IReadOnlyList<string>? ColumnNames { get; init; }

    /// <summary>
    /// Gets additional context data
    /// </summary>
    public IReadOnlyDictionary<string, object?> AdditionalData { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets the operation ID for correlation
    /// </summary>
    public string? OperationId { get; init; }
}
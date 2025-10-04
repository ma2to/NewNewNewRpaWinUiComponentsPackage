namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Results;

/// <summary>
/// Result of column operations
/// </summary>
internal record ColumnOperationResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    string? ColumnName = null,
    TimeSpan? Duration = null
)
{
    /// <summary>
    /// Creates successful column operation result
    /// </summary>
    public static ColumnOperationResult Success(string columnName, TimeSpan duration) =>
        new(true, null, columnName, duration);

    /// <summary>
    /// Creates failed column operation result
    /// </summary>
    public static ColumnOperationResult Failed(string errorMessage, TimeSpan? duration = null) =>
        new(false, errorMessage, null, duration);

    /// <summary>
    /// Creates cancelled column operation result
    /// </summary>
    public static ColumnOperationResult Cancelled(TimeSpan? duration = null) =>
        new(false, "Operation was cancelled", null, duration);
};
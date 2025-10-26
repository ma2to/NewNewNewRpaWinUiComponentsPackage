namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public cell update descriptor for batch operations
/// BREAKING CHANGE v3.0: Now includes RowId for stable row identification.
/// Use RowId when possible for stability across sort/filter/delete operations.
/// </summary>
public sealed class PublicCellUpdate
{
    /// <summary>
    /// Row index (UNSTABLE - changes on sort/filter/delete)
    /// WARNING: Prefer using RowId for stability.
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Stable row identifier (from __rowId field)
    /// STABLE: Persists across sort/filter/delete operations.
    /// When both RowId and RowIndex are provided, RowId takes precedence.
    /// </summary>
    public string? RowId { get; init; }

    /// <summary>
    /// Column name
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// New value
    /// </summary>
    public object? NewValue { get; init; }
}

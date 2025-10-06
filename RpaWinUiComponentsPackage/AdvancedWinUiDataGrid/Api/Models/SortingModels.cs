namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

/// <summary>
/// Public sort descriptor defining sort operation for a column
/// </summary>
public sealed class PublicSortDescriptor
{
    /// <summary>
    /// Column name to sort by
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Sort direction
    /// </summary>
    public PublicSortDirection Direction { get; init; } = PublicSortDirection.Ascending;

    /// <summary>
    /// Sort priority for multi-column sorting (lower = higher priority)
    /// </summary>
    public int Priority { get; init; } = 0;
}

/// <summary>
/// Public sort direction enumeration
/// </summary>
public enum PublicSortDirection
{
    /// <summary>
    /// No sorting
    /// </summary>
    None = 0,

    /// <summary>
    /// Ascending order (A-Z, 0-9)
    /// </summary>
    Ascending = 1,

    /// <summary>
    /// Descending order (Z-A, 9-0)
    /// </summary>
    Descending = 2
}

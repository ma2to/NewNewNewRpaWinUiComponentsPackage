namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

/// <summary>
/// Public cell update descriptor for batch operations
/// </summary>
public sealed class PublicCellUpdate
{
    /// <summary>
    /// Row index
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Column name
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// New value
    /// </summary>
    public object? NewValue { get; init; }
}

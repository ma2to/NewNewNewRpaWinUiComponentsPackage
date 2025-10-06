namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

/// <summary>
/// Public event args for cell edit notifications
/// </summary>
public sealed class PublicCellEditEventArgs
{
    /// <summary>
    /// Row index of edited cell
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Column name of edited cell
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Old value before edit
    /// </summary>
    public object? OldValue { get; init; }

    /// <summary>
    /// New value after edit
    /// </summary>
    public object? NewValue { get; init; }

    /// <summary>
    /// Time when edit occurred
    /// </summary>
    public DateTime EditTime { get; init; }
}

/// <summary>
/// Public event args for selection change notifications
/// </summary>
public sealed class PublicSelectionChangedEventArgs
{
    /// <summary>
    /// Previously selected row indices
    /// </summary>
    public IReadOnlyList<int> PreviousSelection { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Currently selected row indices
    /// </summary>
    public IReadOnlyList<int> CurrentSelection { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Number of selected rows
    /// </summary>
    public int SelectedCount { get; init; }

    /// <summary>
    /// Time when selection changed
    /// </summary>
    public DateTime SelectionTime { get; init; }
}

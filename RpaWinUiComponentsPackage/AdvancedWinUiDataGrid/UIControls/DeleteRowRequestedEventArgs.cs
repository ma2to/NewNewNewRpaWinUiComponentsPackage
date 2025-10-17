using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Event arguments for delete row requests.
/// Contains both rowId (PRIMARY - stable identifier) and rowIndex (for display only).
/// </summary>
public class DeleteRowRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the stable row ID (unique identifier from storage layer) - PRIMARY.
    /// USE THIS for all delete operations - it remains stable across sort/filter/delete operations.
    /// This value never changes for a row, even when rows are added/deleted/sorted.
    /// Can be null for rows without assigned ID (rare edge cases).
    /// </summary>
    public string? RowId { get; }

    /// <summary>
    /// Gets the row index (zero-based position in current grid view) - Optional (for display only).
    /// UNSTABLE: This value changes when rows are added/deleted/sorted.
    /// Do NOT use this for delete operations - use RowId instead.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Creates new delete row request event arguments.
    /// </summary>
    /// <param name="rowId">The stable row ID from storage (PRIMARY)</param>
    /// <param name="rowIndex">The zero-based row index in the current view (optional, for display only)</param>
    public DeleteRowRequestedEventArgs(int rowIndex, string? rowId)
    {
        RowId = rowId;
        RowIndex = rowIndex;
    }
}

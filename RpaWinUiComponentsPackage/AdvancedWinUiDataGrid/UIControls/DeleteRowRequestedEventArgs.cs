using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Event arguments for delete row requests.
/// Contains both rowIndex (for backward compatibility and display) and rowId (for stable row identification).
/// </summary>
public class DeleteRowRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the row index (zero-based position in current grid view).
    /// This value changes when rows are added/deleted/sorted.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Gets the stable row ID (unique identifier from storage layer).
    /// This value never changes for a row, even when rows are added/deleted/sorted.
    /// Can be null for rows without assigned ID (rare edge cases).
    /// </summary>
    public string? RowId { get; }

    /// <summary>
    /// Creates new delete row request event arguments.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index in the current view</param>
    /// <param name="rowId">The stable row ID from storage, or null if not available</param>
    public DeleteRowRequestedEventArgs(int rowIndex, string? rowId)
    {
        RowIndex = rowIndex;
        RowId = rowId;
    }
}

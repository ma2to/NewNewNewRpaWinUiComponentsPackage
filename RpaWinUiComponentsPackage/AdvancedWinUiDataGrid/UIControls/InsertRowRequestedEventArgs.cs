using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Event arguments for insert row requested event (FÁZA 4 - Context Menu)
/// Contains row index, row ID, and insert position for precise row identification
/// </summary>
public sealed class InsertRowRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Zero-based row index where insert was requested
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Unique row ID (from __rowId column) - used for precise row identification
    /// </summary>
    public string? RowId { get; }

    /// <summary>
    /// Insert position: "Above" or "Below" (FÁZA 4)
    /// Determines whether to call IRowStore.InsertRowBeforeAsync() or InsertRowAfterAsync()
    /// </summary>
    public string Position { get; }

    public InsertRowRequestedEventArgs(int rowIndex, string? rowId, string position = "Below")
    {
        RowIndex = rowIndex;
        RowId = rowId;
        Position = position;
    }
}

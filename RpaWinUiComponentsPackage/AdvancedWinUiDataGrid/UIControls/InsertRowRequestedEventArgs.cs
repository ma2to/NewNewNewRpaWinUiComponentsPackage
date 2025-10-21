using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Event arguments for insert row requested event
/// Contains row index and row ID for precise row identification
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

    public InsertRowRequestedEventArgs(int rowIndex, string? rowId)
    {
        RowIndex = rowIndex;
        RowId = rowId;
    }
}

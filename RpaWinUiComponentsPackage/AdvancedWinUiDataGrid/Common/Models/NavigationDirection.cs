namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Keyboard navigation direction for cell selection in the data grid.
/// Used for arrow key navigation in NORMAL mode (when cell is selected but not editing).
/// </summary>
public enum NavigationDirection
{
    /// <summary>
    /// Navigate to the cell above the current cell (Arrow Up key).
    /// </summary>
    Up,

    /// <summary>
    /// Navigate to the cell below the current cell (Arrow Down key).
    /// </summary>
    Down,

    /// <summary>
    /// Navigate to the cell to the left of the current cell (Arrow Left key).
    /// </summary>
    Left,

    /// <summary>
    /// Navigate to the cell to the right of the current cell (Arrow Right key).
    /// </summary>
    Right,

    /// <summary>
    /// Navigate to the next cell (Tab key) - moves right, wraps to next row at end.
    /// Excel-like behavior: Tab moves across columns, then to first column of next row.
    /// </summary>
    TabForward,

    /// <summary>
    /// Navigate to the previous cell (Shift+Tab key) - moves left, wraps to previous row at start.
    /// Excel-like behavior: Shift+Tab moves across columns backwards, then to last column of previous row.
    /// </summary>
    TabBackward
}

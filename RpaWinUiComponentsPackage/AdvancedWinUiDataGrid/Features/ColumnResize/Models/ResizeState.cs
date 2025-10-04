namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.ColumnResize.Models;

/// <summary>
/// Internal state tracking for active column resize operation
/// </summary>
internal sealed class ResizeState
{
    /// <summary>
    /// Gets or sets the index of the column being resized
    /// </summary>
    internal int ColumnIndex { get; set; }

    /// <summary>
    /// Gets or sets the initial mouse X position when resize started
    /// </summary>
    internal double StartClientX { get; set; }

    /// <summary>
    /// Gets or sets the initial column width when resize started
    /// </summary>
    internal double StartWidth { get; set; }

    /// <summary>
    /// Gets or sets whether a resize operation is currently active
    /// </summary>
    internal bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp (for debouncing)
    /// </summary>
    internal DateTime LastUpdateTime { get; set; }

    /// <summary>
    /// Gets or sets the current width during resize
    /// </summary>
    internal double CurrentWidth { get; set; }
}

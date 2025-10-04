namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

/// <summary>
/// PUBLIC: Auto row height mode configuration
/// Controls automatic row height calculation behavior
/// </summary>
internal enum AutoRowHeightMode
{
    /// <summary>
    /// Auto row height is disabled - use fixed row heights
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Auto row height is enabled for all rows
    /// </summary>
    Enabled = 1,

    /// <summary>
    /// Auto mode - enable only when needed (performance optimization)
    /// </summary>
    Auto = 2
}

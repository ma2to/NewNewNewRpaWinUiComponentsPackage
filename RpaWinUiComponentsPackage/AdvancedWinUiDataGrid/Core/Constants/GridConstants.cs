namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Constants;

/// <summary>
/// CORE: Constants for data grid configuration
/// ENTERPRISE: Centralized grid behavior parameters
/// </summary>
internal static class GridConstants
{
    /// <summary>Default minimum number of rows to maintain</summary>
    public const int DefaultMinimumRows = 1;

    /// <summary>Maximum number of rows for optimal performance</summary>
    public const int MaxRecommendedRows = 100000;

    /// <summary>Default column width in pixels</summary>
    public const double DefaultColumnWidth = 100.0;

    /// <summary>Minimum column width in pixels</summary>
    public const double MinColumnWidth = 20.0;

    /// <summary>Maximum column width in pixels</summary>
    public const double MaxColumnWidth = 2000.0;

    /// <summary>Default row height in pixels</summary>
    public const double DefaultRowHeight = 32.0;

    /// <summary>Minimum row height in pixels</summary>
    public const double MinRowHeight = 20.0;

    /// <summary>Maximum row height in pixels</summary>
    public const double MaxRowHeight = 200.0;

    /// <summary>Default grid border thickness</summary>
    public const double DefaultBorderThickness = 1.0;

    /// <summary>Default scroll sensitivity</summary>
    public const double DefaultScrollSensitivity = 1.0;

    /// <summary>Number of virtual rows to render outside viewport</summary>
    public const int VirtualizationBuffer = 5;
}
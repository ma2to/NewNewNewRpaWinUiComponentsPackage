namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// CORE: Defines operation modes for the data grid component
/// ENTERPRISE: Support for both UI and headless scenarios
/// </summary>
public enum DataGridOperationMode
{
    /// <summary>UI mode - automatic UI updates and user interaction enabled</summary>
    UI,

    /// <summary>Headless mode - manual UI updates only, for automated scenarios</summary>
    Headless
}
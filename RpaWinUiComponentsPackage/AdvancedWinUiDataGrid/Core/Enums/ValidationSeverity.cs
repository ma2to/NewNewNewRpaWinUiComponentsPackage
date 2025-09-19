namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

/// <summary>
/// CORE: Defines validation error severity levels
/// ENTERPRISE: Professional error classification system
/// </summary>
public enum ValidationSeverity
{
    /// <summary>Informational message - no action required</summary>
    Info = 0,

    /// <summary>Warning - user should be aware but can continue</summary>
    Warning = 1,

    /// <summary>Error - prevents normal operation</summary>
    Error = 2,

    /// <summary>Critical error - system stability at risk</summary>
    Critical = 3
}
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

/// <summary>
/// CORE: Defines validation error severity levels
/// ENTERPRISE: Professional error classification system with timeout support
/// </summary>
internal enum ValidationSeverity
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

/// <summary>
/// CORE: Validation trigger modes for smart validation decision making
/// ENTERPRISE: Controls when validation occurs for optimal performance
/// </summary>
internal enum ValidationTrigger
{
    /// <summary>Manual validation only</summary>
    Manual = 0,

    /// <summary>Validate on cell value change</summary>
    OnCellChanged = 1,

    /// <summary>Validate on text input while typing</summary>
    OnTextChanged = 2,

    /// <summary>Validate when cell loses focus</summary>
    OnCellExit = 3,

    /// <summary>Validate on row completion</summary>
    OnRowComplete = 4,

    /// <summary>Bulk validation for import/paste operations</summary>
    Bulk = 5
}

/// <summary>
/// CORE: Validation deletion modes for row removal based on validation state
/// ENTERPRISE: Professional row management based on validation criteria
/// </summary>
internal enum ValidationDeletionMode
{
    /// <summary>Delete rows that fail validation</summary>
    DeleteInvalidRows = 0,

    /// <summary>Delete rows that pass validation</summary>
    DeleteValidRows = 1,

    /// <summary>Delete based on custom predicate</summary>
    DeleteByCustomRule = 2,

    /// <summary>Delete rows with specific severity levels</summary>
    DeleteBySeverity = 3,

    /// <summary>Delete rows failing specific named rules</summary>
    DeleteByRuleName = 4
}
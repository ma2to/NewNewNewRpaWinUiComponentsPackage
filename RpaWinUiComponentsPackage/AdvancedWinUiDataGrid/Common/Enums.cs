namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

/// <summary>
/// Defines the operation mode for the data grid
/// </summary>
internal enum DataGridOperationMode
{
    /// <summary>UI mode with full WinUI integration</summary>
    UI,

    /// <summary>Headless mode for backend operations</summary>
    Headless
}

/// <summary>
/// Defines validation severity levels
/// </summary>
internal enum ValidationSeverity
{
    /// <summary>Informational message</summary>
    Info,

    /// <summary>Warning message</summary>
    Warning,

    /// <summary>Error message</summary>
    Error,

    /// <summary>Critical error message</summary>
    Critical
}

/// <summary>
/// Defines validation logic operators
/// </summary>
internal enum ValidationLogic
{
    /// <summary>All rules must pass</summary>
    And,

    /// <summary>At least one rule must pass</summary>
    Or,

    /// <summary>All rules must pass with short-circuit evaluation</summary>
    AndAlso,

    /// <summary>At least one rule must pass with short-circuit evaluation</summary>
    OrElse
}

/// <summary>
/// Defines import modes for data operations
/// </summary>
internal enum ImportMode
{
    /// <summary>Replace all existing data</summary>
    Replace,

    /// <summary>Append to existing data</summary>
    Append,

    /// <summary>Insert at specific position</summary>
    Insert,

    /// <summary>Merge with existing data</summary>
    Merge
}

/// <summary>
/// Defines export formats
/// </summary>
internal enum ExportFormat
{
    /// <summary>Dictionary format</summary>
    Dictionary,

    /// <summary>DataTable format</summary>
    DataTable
}

/// <summary>
/// Defines clipboard formats for copy/paste operations
/// </summary>
internal enum ClipboardFormat
{
    /// <summary>Tab-separated values</summary>
    TabSeparated,

    /// <summary>Comma-separated values</summary>
    CommaSeparated,

    /// <summary>Custom delimiter</summary>
    CustomDelimited,

    /// <summary>JSON format</summary>
    Json,

    /// <summary>Plain text</summary>
    PlainText
}

/// <summary>
/// Defines validation strategy types
/// </summary>
internal enum ValidationStrategy
{
    /// <summary>Automatic validation</summary>
    Automatic,

    /// <summary>Manual validation on demand</summary>
    Manual,

    /// <summary>Real-time validation on each change</summary>
    RealTime,

    /// <summary>Batch validation</summary>
    Batch
}

/// <summary>
/// Defines validation priority levels
/// </summary>
internal enum ValidationPriority
{
    /// <summary>Low priority validation</summary>
    Low,

    /// <summary>Normal priority validation</summary>
    Normal,

    /// <summary>High priority validation</summary>
    High,

    /// <summary>Critical priority validation</summary>
    Critical
}

/// <summary>
/// Defines validation rule types
/// </summary>
internal enum ValidationRuleType
{
    /// <summary>Required field validation</summary>
    Required,

    /// <summary>Range validation</summary>
    Range,

    /// <summary>Regular expression validation</summary>
    Regex,

    /// <summary>Custom function validation</summary>
    CustomFunction,

    /// <summary>Cross-column validation</summary>
    CrossColumn,

    /// <summary>Conditional validation</summary>
    Conditional,

    /// <summary>Async validation</summary>
    Async,

    /// <summary>Group validation</summary>
    Group
}

/// <summary>
/// Defines logical operators for validation rule groups
/// </summary>
internal enum ValidationLogicalOperator
{
    /// <summary>All conditions must be true</summary>
    And,

    /// <summary>At least one condition must be true</summary>
    Or,

    /// <summary>All conditions must be true with short-circuit</summary>
    AndAlso,

    /// <summary>At least one condition must be true with short-circuit</summary>
    OrElse
}

/// <summary>
/// Defines sort directions
/// </summary>
internal enum SortDirection
{
    /// <summary>No sorting</summary>
    None,

    /// <summary>Ascending order</summary>
    Ascending,

    /// <summary>Descending order</summary>
    Descending
}

/// <summary>
/// Defines filter operators
/// </summary>
internal enum FilterOperator
{
    /// <summary>Equals</summary>
    Equals,

    /// <summary>Not equals</summary>
    NotEquals,

    /// <summary>Contains</summary>
    Contains,

    /// <summary>Does not contain</summary>
    NotContains,

    /// <summary>Starts with</summary>
    StartsWith,

    /// <summary>Ends with</summary>
    EndsWith,

    /// <summary>Greater than</summary>
    GreaterThan,

    /// <summary>Greater than or equal</summary>
    GreaterThanOrEqual,

    /// <summary>Less than</summary>
    LessThan,

    /// <summary>Less than or equal</summary>
    LessThanOrEqual,

    /// <summary>Is null</summary>
    IsNull,

    /// <summary>Is not null</summary>
    IsNotNull,

    /// <summary>Is empty</summary>
    IsEmpty,

    /// <summary>Is not empty</summary>
    IsNotEmpty
}

/// <summary>
/// Defines special column types
/// </summary>
internal enum SpecialColumnType
{
    /// <summary>Regular data column</summary>
    None,

    /// <summary>Normal data column</summary>
    Normal,

    /// <summary>Row number column</summary>
    RowNumber,

    /// <summary>Checkbox column for selection</summary>
    Checkbox,

    /// <summary>Delete row button column</summary>
    DeleteRow,

    /// <summary>Validation alerts column</summary>
    ValidationAlerts
}
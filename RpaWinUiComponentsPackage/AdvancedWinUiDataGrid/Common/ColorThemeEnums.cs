namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

/// <summary>
/// Defines the UI element types that can be themed
/// </summary>
public enum UIElementType
{
    /// <summary>Column headers</summary>
    Header,

    /// <summary>Data cells</summary>
    Cell,

    /// <summary>Buttons (e.g., delete button)</summary>
    Button,

    /// <summary>Scrollbars</summary>
    ScrollBar,

    /// <summary>Filter row</summary>
    FilterRow,

    /// <summary>Search panel</summary>
    SearchPanel,

    /// <summary>Pagination controls</summary>
    PaginationPanel,

    /// <summary>Validation alert icons</summary>
    ValidationAlert,

    /// <summary>Special columns (checkbox, row number, delete, etc.)</summary>
    SpecialColumn,

    /// <summary>Grid container</summary>
    Grid,

    /// <summary>Row element</summary>
    Row
}

/// <summary>
/// Defines the states that UI elements can be in
/// </summary>
public enum UIElementState
{
    /// <summary>Default state</summary>
    Normal,

    /// <summary>Mouse over element</summary>
    Hover,

    /// <summary>Element is pressed</summary>
    Pressed,

    /// <summary>Element is selected</summary>
    Selected,

    /// <summary>Element has keyboard focus</summary>
    Focused,

    /// <summary>Cell is being edited</summary>
    Editing,

    /// <summary>Element is disabled</summary>
    Disabled,

    /// <summary>Element is read-only</summary>
    ReadOnly,

    /// <summary>Validation error</summary>
    Error,

    /// <summary>Validation warning</summary>
    Warning,

    /// <summary>Validation success</summary>
    Success,

    /// <summary>Info message</summary>
    Info
}

/// <summary>
/// Defines the color properties that can be set
/// </summary>
public enum ColorProperty
{
    /// <summary>Background color</summary>
    Background,

    /// <summary>Foreground/text color</summary>
    Foreground,

    /// <summary>Border color</summary>
    Border
}

/// <summary>
/// Defines theme categories for built-in themes
/// </summary>
public enum ThemeCategory
{
    /// <summary>Light theme (white background)</summary>
    Light,

    /// <summary>Dark theme (black background)</summary>
    Dark,

    /// <summary>High contrast theme (accessibility)</summary>
    HighContrast,

    /// <summary>Custom user-defined theme</summary>
    Custom
}

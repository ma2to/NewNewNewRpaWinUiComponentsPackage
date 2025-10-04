namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Enumeration of available grid features that can be enabled or disabled
/// </summary>
public enum GridFeature
{
    /// <summary>Sort functionality (single and multi-column sorting)</summary>
    Sort,

    /// <summary>Search functionality (6 search modes including fuzzy and regex)</summary>
    Search,

    /// <summary>Filter functionality (complex filtering with multiple criteria)</summary>
    Filter,

    /// <summary>Import functionality (DataTable and Dictionary import)</summary>
    Import,

    /// <summary>Export functionality (DataTable and Dictionary export)</summary>
    Export,

    /// <summary>Validation functionality (batch and real-time validation with custom rules)</summary>
    Validation,

    /// <summary>Copy/Paste functionality (clipboard operations)</summary>
    CopyPaste,

    /// <summary>Cell editing functionality (real-time cell updates with validation)</summary>
    CellEdit,

    /// <summary>Row/Column operations (add, delete, update rows and columns)</summary>
    RowColumnOperations,

    /// <summary>Column resize via drag & drop</summary>
    ColumnResize,

    /// <summary>Performance monitoring and optimization</summary>
    Performance,

    /// <summary>Color and conditional formatting</summary>
    Color,

    /// <summary>Keyboard shortcuts</summary>
    Shortcuts,

    /// <summary>Smart operations (intelligent row management)</summary>
    SmartOperations,

    /// <summary>Auto row height calculation</summary>
    AutoRowHeight,

    /// <summary>Row numbering</summary>
    RowNumbering,

    /// <summary>Selection management</summary>
    Selection,

    /// <summary>Special columns (validAlerts, rowNumber, checkbox, delete)</summary>
    SpecialColumns,

    /// <summary>UI operations and rendering</summary>
    UI,

    /// <summary>Security and access control</summary>
    Security,

    /// <summary>Advanced logging</summary>
    Logging,

    /// <summary>Exception handling</summary>
    ExceptionHandling,

    /// <summary>Configuration management</summary>
    Configuration
}

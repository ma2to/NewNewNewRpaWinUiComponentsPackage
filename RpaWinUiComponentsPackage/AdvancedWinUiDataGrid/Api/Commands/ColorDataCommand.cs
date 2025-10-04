namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Color mode for applying colors
/// </summary>
public enum PublicColorMode
{
    Cell,
    Row,
    Column,
    Conditional
}

/// <summary>
/// Conditional formatting rule type
/// </summary>
public enum PublicConditionalFormattingRule
{
    Equals,
    NotEquals,
    Contains,
    GreaterThan,
    LessThan,
    Between,
    IsEmpty,
    IsNotEmpty
}

/// <summary>
/// Command for applying cell/row/column color
/// </summary>
/// <param name="Mode">Color mode (Cell/Row/Column)</param>
/// <param name="BackgroundColor">Background color (hex format #RRGGBB)</param>
/// <param name="ForegroundColor">Foreground color (hex format #RRGGBB)</param>
/// <param name="RowIndex">Row index for cell/row coloring</param>
/// <param name="ColumnIndex">Column index for cell coloring</param>
/// <param name="ColumnName">Column name for column coloring</param>
public record ApplyColorDataCommand(
    PublicColorMode Mode = PublicColorMode.Cell,
    string? BackgroundColor = null,
    string? ForegroundColor = null,
    int? RowIndex = null,
    int? ColumnIndex = null,
    string? ColumnName = null
);

/// <summary>
/// Conditional formatting rule configuration
/// </summary>
/// <param name="ColumnName">Column to apply rule</param>
/// <param name="Rule">Conditional rule type</param>
/// <param name="Value">Comparison value</param>
/// <param name="BackgroundColor">Background color when condition met</param>
/// <param name="ForegroundColor">Foreground color when condition met</param>
public record ConditionalFormatRuleConfig(
    string ColumnName,
    PublicConditionalFormattingRule Rule,
    object? Value = null,
    string? BackgroundColor = null,
    string? ForegroundColor = null
);

/// <summary>
/// Command for applying conditional formatting
/// </summary>
/// <param name="Rules">List of conditional formatting rules</param>
public record ApplyConditionalFormattingDataCommand(
    IReadOnlyList<ConditionalFormatRuleConfig> Rules
)
{
    public ApplyConditionalFormattingDataCommand() : this(Array.Empty<ConditionalFormatRuleConfig>()) { }
}

/// <summary>
/// Command for clearing color
/// </summary>
/// <param name="Mode">Color mode to clear</param>
/// <param name="RowIndex">Row index for cell/row clearing</param>
/// <param name="ColumnIndex">Column index for cell clearing</param>
/// <param name="ColumnName">Column name for column clearing</param>
public record ClearColorDataCommand(
    PublicColorMode Mode = PublicColorMode.Cell,
    int? RowIndex = null,
    int? ColumnIndex = null,
    string? ColumnName = null
);

/// <summary>
/// Result of color operation
/// </summary>
/// <param name="IsSuccess">Whether operation succeeded</param>
/// <param name="AffectedCells">Number of cells affected</param>
/// <param name="Duration">Operation duration</param>
/// <param name="ErrorMessage">Error message if failed</param>
public record ColorDataResult(
    bool IsSuccess,
    int AffectedCells,
    TimeSpan Duration,
    string? ErrorMessage = null
)
{
    public ColorDataResult() : this(false, 0, TimeSpan.Zero, null) { }
}

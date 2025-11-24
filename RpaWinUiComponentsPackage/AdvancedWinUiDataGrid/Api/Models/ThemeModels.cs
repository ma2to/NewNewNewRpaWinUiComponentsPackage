namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public grid theme configuration
/// </summary>
public sealed class PublicGridTheme
{
    /// <summary>
    /// Theme name
    /// </summary>
    public string ThemeName { get; init; } = "Default";

    /// <summary>
    /// Cell colors for different states
    /// </summary>
    public PublicCellColors CellColors { get; init; } = new();

    /// <summary>
    /// Row colors for different states
    /// </summary>
    public PublicRowColors RowColors { get; init; } = new();

    /// <summary>
    /// Column header colors
    /// </summary>
    public PublicHeaderColors HeaderColors { get; init; } = new();

    /// <summary>
    /// Validation colors for errors and warnings
    /// </summary>
    public PublicValidationColors ValidationColors { get; init; } = new();

    /// <summary>
    /// Selection colors
    /// </summary>
    public PublicSelectionColors SelectionColors { get; init; } = new();

    /// <summary>
    /// Border colors
    /// </summary>
    public PublicBorderColors BorderColors { get; init; } = new();

    /// <summary>
    /// Special column colors (RowNumber, Checkbox, DeleteRow, InsertRow, ValidationAlerts)
    /// </summary>
    public PublicSpecialColumnColors SpecialColumnColors { get; init; } = new();

    /// <summary>
    /// UI control colors (ResizeGrip, Menus, Dialogs, Placeholders)
    /// </summary>
    public PublicUIControlColors UIControlColors { get; init; } = new();
}

/// <summary>
/// Cell color configuration for different states
/// </summary>
public sealed class PublicCellColors
{
    /// <summary>
    /// Default cell background color
    /// </summary>
    public string DefaultBackground { get; init; } = "#FFFFFF";

    /// <summary>
    /// Default cell foreground color
    /// </summary>
    public string DefaultForeground { get; init; } = "#000000";

    /// <summary>
    /// Cell background when hovered
    /// </summary>
    public string HoverBackground { get; init; } = "#F5F5F5";

    /// <summary>
    /// Cell foreground when hovered
    /// </summary>
    public string HoverForeground { get; init; } = "#000000";

    /// <summary>
    /// Cell background when focused
    /// </summary>
    public string FocusedBackground { get; init; } = "#E3F2FD";

    /// <summary>
    /// Cell foreground when focused
    /// </summary>
    public string FocusedForeground { get; init; } = "#000000";

    /// <summary>
    /// Cell background when disabled
    /// </summary>
    public string DisabledBackground { get; init; } = "#F0F0F0";

    /// <summary>
    /// Cell foreground when disabled
    /// </summary>
    public string DisabledForeground { get; init; } = "#A0A0A0";

    /// <summary>
    /// Cell background when read-only
    /// </summary>
    public string ReadOnlyBackground { get; init; } = "#FAFAFA";

    /// <summary>
    /// Cell foreground when read-only
    /// </summary>
    public string ReadOnlyForeground { get; init; } = "#000000";
}

/// <summary>
/// Row color configuration for different states
/// </summary>
public sealed class PublicRowColors
{
    /// <summary>
    /// Even row background color
    /// </summary>
    public string EvenRowBackground { get; init; } = "#FFFFFF";

    /// <summary>
    /// Odd row background color (for alternating rows)
    /// </summary>
    public string OddRowBackground { get; init; } = "#F9F9F9";

    /// <summary>
    /// Row background when hovered
    /// </summary>
    public string HoverBackground { get; init; } = "#F0F0F0";

    /// <summary>
    /// Row background when selected
    /// </summary>
    public string SelectedBackground { get; init; } = "#0078D4";

    /// <summary>
    /// Row foreground when selected
    /// </summary>
    public string SelectedForeground { get; init; } = "#FFFFFF";

    /// <summary>
    /// Row background when selected and inactive
    /// </summary>
    public string SelectedInactiveBackground { get; init; } = "#CCCCCC";

    /// <summary>
    /// Row foreground when selected and inactive
    /// </summary>
    public string SelectedInactiveForeground { get; init; } = "#000000";
}

/// <summary>
/// Header color configuration
/// </summary>
public sealed class PublicHeaderColors
{
    /// <summary>
    /// Header background color
    /// </summary>
    public string Background { get; init; } = "#F5F5F5";

    /// <summary>
    /// Header foreground color
    /// </summary>
    public string Foreground { get; init; } = "#000000";

    /// <summary>
    /// Header background when hovered
    /// </summary>
    public string HoverBackground { get; init; } = "#E0E0E0";

    /// <summary>
    /// Header background when pressed
    /// </summary>
    public string PressedBackground { get; init; } = "#D0D0D0";

    /// <summary>
    /// Sort indicator color
    /// </summary>
    public string SortIndicatorColor { get; init; } = "#0078D4";
}

/// <summary>
/// Validation color configuration
/// </summary>
public sealed class PublicValidationColors
{
    /// <summary>
    /// Error background color
    /// </summary>
    public string ErrorBackground { get; init; } = "#FFEBEE";

    /// <summary>
    /// Error foreground color
    /// </summary>
    public string ErrorForeground { get; init; } = "#D32F2F";

    /// <summary>
    /// Error border color
    /// </summary>
    public string ErrorBorder { get; init; } = "#F44336";

    /// <summary>
    /// Warning background color
    /// </summary>
    public string WarningBackground { get; init; } = "#FFF3E0";

    /// <summary>
    /// Warning foreground color
    /// </summary>
    public string WarningForeground { get; init; } = "#F57C00";

    /// <summary>
    /// Warning border color
    /// </summary>
    public string WarningBorder { get; init; } = "#FF9800";

    /// <summary>
    /// Info background color
    /// </summary>
    public string InfoBackground { get; init; } = "#E3F2FD";

    /// <summary>
    /// Info foreground color
    /// </summary>
    public string InfoForeground { get; init; } = "#1976D2";

    /// <summary>
    /// Info border color
    /// </summary>
    public string InfoBorder { get; init; } = "#2196F3";
}

/// <summary>
/// Selection color configuration
/// </summary>
public sealed class PublicSelectionColors
{
    /// <summary>
    /// Selection rectangle border color
    /// </summary>
    public string SelectionBorder { get; init; } = "#0078D4";

    /// <summary>
    /// Selection rectangle fill color
    /// </summary>
    public string SelectionFill { get; init; } = "#0078D433";

    /// <summary>
    /// Multi-selection background color
    /// </summary>
    public string MultiSelectionBackground { get; init; } = "#CCE5FF";

    /// <summary>
    /// Multi-selection foreground color
    /// </summary>
    public string MultiSelectionForeground { get; init; } = "#000000";
}

/// <summary>
/// Border color configuration
/// </summary>
public sealed class PublicBorderColors
{
    /// <summary>
    /// Cell border color
    /// </summary>
    public string CellBorder { get; init; } = "#E0E0E0";

    /// <summary>
    /// Row border color
    /// </summary>
    public string RowBorder { get; init; } = "#E0E0E0";

    /// <summary>
    /// Column border color
    /// </summary>
    public string ColumnBorder { get; init; } = "#E0E0E0";

    /// <summary>
    /// Grid outer border color
    /// </summary>
    public string GridBorder { get; init; } = "#CCCCCC";

    /// <summary>
    /// Focused cell border color
    /// </summary>
    public string FocusedCellBorder { get; init; } = "#0078D4";
}

/// <summary>
/// Special column color configuration
/// </summary>
public sealed class PublicSpecialColumnColors
{
    /// <summary>
    /// RowNumber column background color
    /// </summary>
    public string RowNumberBackground { get; init; } = "#14808080"; // Light gray with alpha

    /// <summary>
    /// RowNumber column foreground color
    /// </summary>
    public string RowNumberForeground { get; init; } = "#808080"; // Gray

    /// <summary>
    /// Checkbox border color
    /// </summary>
    public string CheckboxBorder { get; init; } = "#A9A9A9"; // DarkGray

    /// <summary>
    /// Checkbox background color
    /// </summary>
    public string CheckboxBackground { get; init; } = "#FFFFFF"; // White

    /// <summary>
    /// Checkbox foreground (checkmark) color
    /// </summary>
    public string CheckboxForeground { get; init; } = "#000000"; // Black

    /// <summary>
    /// DeleteRow button background color
    /// </summary>
    public string DeleteRowBackground { get; init; } = "#00000000"; // Transparent

    /// <summary>
    /// DeleteRow button foreground color
    /// </summary>
    public string DeleteRowForeground { get; init; } = "#8B0000"; // DarkRed

    /// <summary>
    /// DeleteRow button hover background color
    /// </summary>
    public string DeleteRowHoverBackground { get; init; } = "#FFEBEE"; // Light red

    /// <summary>
    /// InsertRow button background color
    /// </summary>
    public string InsertRowBackground { get; init; } = "#C8E6C8"; // Light green (200, 230, 200)

    /// <summary>
    /// InsertRow button foreground color
    /// </summary>
    public string InsertRowForeground { get; init; } = "#006400"; // DarkGreen

    /// <summary>
    /// InsertRow button border color
    /// </summary>
    public string InsertRowBorder { get; init; } = "#008000"; // Green

    /// <summary>
    /// InsertRow button hover background color
    /// </summary>
    public string InsertRowHoverBackground { get; init; } = "#96DC96"; // Darker green (150, 220, 150)

    /// <summary>
    /// InsertRow button hover foreground color
    /// </summary>
    public string InsertRowHoverForeground { get; init; } = "#FFFFFF"; // White

    /// <summary>
    /// ValidationAlerts background when has alert
    /// ✅ CRITICAL FIX #24.1: Changed from "#1EFF0000" (alpha 30 transparent red) to "#FFFEBEE" (opaque light red)
    /// ROOT CAUSE: Alpha 30 (11.7% opacity) on dark theme background appeared as BLACK-RED instead of light red
    /// USER COMPLAINT: "meni farbu background na ciernu (background cierny a text cerveny)" (30th fix attempt!)
    /// SOLUTION: Use SAME opaque light red as data cell validation errors (#FFFEBEE = 255, 254, 238)
    /// RESULT: Consistent light red background across validation errors (data cells + ValidationAlerts column)
    /// </summary>
    public string ValidationAlertsErrorBackground { get; init; } = "#FFFEBEE"; // Light red (same as data cell validation)

    /// <summary>
    /// ValidationAlerts foreground when has alert
    /// </summary>
    public string ValidationAlertsErrorForeground { get; init; } = "#FF0000"; // Red
}

/// <summary>
/// UI control color configuration (ResizeGrip, Menus, Dialogs, Containers)
/// </summary>
public sealed class PublicUIControlColors
{
    /// <summary>
    /// Resize grip default background color
    /// </summary>
    public string ResizeGripBackground { get; init; } = "#B3A9A9A9"; // DarkGray with opacity 0.7

    /// <summary>
    /// Resize grip hover background color
    /// </summary>
    public string ResizeGripHoverBackground { get; init; } = "#E60000FF"; // Blue with opacity 0.9

    /// <summary>
    /// Resize preview line color
    /// </summary>
    public string ResizePreviewLine { get; init; } = "#0000FF"; // Blue

    /// <summary>
    /// Context menu destructive action foreground (Delete)
    /// </summary>
    public string MenuDestructiveActionForeground { get; init; } = "#FF0000"; // Red

    /// <summary>
    /// Dialog error message foreground
    /// </summary>
    public string DialogErrorForeground { get; init; } = "#FF0000"; // Red

    /// <summary>
    /// Loading placeholder foreground
    /// </summary>
    public string PlaceholderForeground { get; init; } = "#FF0000"; // Red

    /// <summary>
    /// Loading placeholder background
    /// </summary>
    public string PlaceholderBackground { get; init; } = "#FFFFE0"; // LightYellow

    /// <summary>
    /// Search panel container border
    /// </summary>
    public string SearchPanelBorder { get; init; } = "#D3D3D3"; // LightGray

    /// <summary>
    /// Filter row container border
    /// </summary>
    public string FilterRowBorder { get; init; } = "#D3D3D3"; // LightGray

    /// <summary>
    /// Headers row container border
    /// </summary>
    public string HeadersRowBorder { get; init; } = "#808080"; // Gray

    /// <summary>
    /// Pagination panel container border
    /// </summary>
    public string PaginationPanelBorder { get; init; } = "#D3D3D3"; // LightGray
}

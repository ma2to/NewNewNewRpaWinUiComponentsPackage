namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

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

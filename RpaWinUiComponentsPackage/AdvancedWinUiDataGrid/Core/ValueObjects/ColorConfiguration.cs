using Microsoft.UI;
using Windows.UI;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// PRESENTATION: Color configuration for grid theming
/// IMMUTABLE: Value object for consistent color theming across UI components
/// </summary>
internal sealed record ColorConfiguration
{
    // Grid colors
    /// <summary>Grid background color</summary>
    public Color GridBackgroundColor { get; init; } = Colors.White;

    /// <summary>Grid line color</summary>
    public Color GridLineColor { get; init; } = Colors.LightGray;

    /// <summary>Grid border color</summary>
    public Color GridBorderColor { get; init; } = Colors.LightGray;

    // Header colors
    /// <summary>Header background color</summary>
    public Color HeaderBackgroundColor { get; init; } = Colors.LightGray;

    /// <summary>Header foreground color</summary>
    public Color HeaderForegroundColor { get; init; } = Colors.Black;

    /// <summary>Header border color</summary>
    public Color HeaderBorderColor { get; init; } = Colors.Gray;

    // Cell colors
    /// <summary>Cell selected background color</summary>
    public Color CellSelectedBackgroundColor { get; init; } = Colors.LightBlue;

    /// <summary>Cell editing background color</summary>
    public Color CellEditingBackgroundColor { get; init; } = Colors.LightYellow;

    /// <summary>Cell error background color</summary>
    public Color CellErrorBackgroundColor { get; init; } = Colors.LightPink;

    /// <summary>Cell error border color</summary>
    public Color CellErrorBorderColor { get; init; } = Colors.Red;

    /// <summary>Cell selected border color</summary>
    public Color CellSelectedBorderColor { get; init; } = Colors.Blue;

    // Special control colors
    /// <summary>Delete button background color</summary>
    public Color DeleteButtonBackgroundColor { get; init; } = Colors.LightCoral;

    /// <summary>Delete button foreground color</summary>
    public Color DeleteButtonForegroundColor { get; init; } = Colors.DarkRed;

    /// <summary>Checkbox border color</summary>
    public Color CheckBoxBorderColor { get; init; } = Colors.Gray;

    /// <summary>Validation error text color</summary>
    public Color ValidationErrorTextColor { get; init; } = Colors.DarkRed;

    /// <summary>Validation warning text color</summary>
    public Color ValidationWarningTextColor { get; init; } = Colors.Orange;

    /// <summary>Validation info text color</summary>
    public Color ValidationInfoTextColor { get; init; } = Colors.Blue;

    /// <summary>Border color for focused cells</summary>
    public Color FocusBorderColor { get; init; } = Colors.Blue;

    /// <summary>Create default color configuration</summary>
    public static ColorConfiguration Default => new();

    /// <summary>
    /// ENTERPRISE: Create default color configuration for backward compatibility
    /// LOGGING: Factory method creates standard color configuration with comprehensive theming
    /// </summary>
    public static ColorConfiguration CreateDefault()
    {
        return new ColorConfiguration();
    }

    /// <summary>Create light theme color configuration</summary>
    public static ColorConfiguration LightTheme => new()
    {
        GridBackgroundColor = Colors.White,
        HeaderBackgroundColor = Colors.LightGray,
        HeaderForegroundColor = Colors.Black,
        CellSelectedBackgroundColor = Colors.LightBlue,
        GridLineColor = Colors.LightGray
    };

    /// <summary>Create dark theme color configuration</summary>
    public static ColorConfiguration DarkTheme => new()
    {
        GridBackgroundColor = Colors.DarkGray,
        HeaderBackgroundColor = Colors.Gray,
        HeaderForegroundColor = Colors.White,
        CellSelectedBackgroundColor = Colors.DarkBlue,
        GridLineColor = Colors.Gray,
        ValidationErrorTextColor = Colors.OrangeRed
    };
}
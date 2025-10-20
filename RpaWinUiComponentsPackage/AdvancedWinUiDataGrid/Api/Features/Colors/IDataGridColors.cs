using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// PUBLIC API: Interface for direct color management without using themes.
/// Provides granular control over individual UI element colors.
/// This is part of BOD 6: Direct color changes API.
/// </summary>
public interface IDataGridColors
{
    /// <summary>
    /// Sets a single color property for a specific element and state.
    /// Example: Set header background color when hovered to purple.
    /// </summary>
    /// <param name="elementType">The type of UI element (Header, Cell, Button, etc.)</param>
    /// <param name="state">The state of the element (Normal, Hover, Pressed, etc.)</param>
    /// <param name="property">The color property to set (Background, Foreground, Border)</param>
    /// <param name="hexColor">Hex color value (e.g., "#FF0000", "#AARRGGBB")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task completing when color is set</returns>
    Task SetElementColorAsync(
        UIElementType elementType,
        UIElementState state,
        ColorProperty property,
        string hexColor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets multiple color properties for a specific element and state at once.
    /// Example: Set header normal state background, foreground, and border simultaneously.
    /// </summary>
    /// <param name="elementType">The type of UI element</param>
    /// <param name="state">The state of the element</param>
    /// <param name="colorSet">ColorSet containing Background, Foreground, Border (null values are ignored)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task completing when colors are set</returns>
    Task SetElementColorsAsync(
        UIElementType elementType,
        UIElementState state,
        ColorSet colorSet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update multiple element colors in a single operation.
    /// Example: Set all header states, cell error colors, button hover colors in one call.
    /// </summary>
    /// <param name="colorUpdates">Dictionary mapping (ElementType, State, Property) to hex color</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task completing when all colors are set</returns>
    Task SetMultipleElementColorsAsync(
        IReadOnlyDictionary<(UIElementType ElementType, UIElementState State, ColorProperty Property), string> colorUpdates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current color value for a specific element, state, and property.
    /// Returns hex color string (e.g., "#FF0000").
    /// </summary>
    /// <param name="elementType">The type of UI element</param>
    /// <param name="state">The state of the element</param>
    /// <param name="property">The color property to get</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hex color string or null if not set</returns>
    Task<string?> GetElementColorAsync(
        UIElementType elementType,
        UIElementState state,
        ColorProperty property,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all colors for a specific element and state.
    /// Returns ColorSet with Background, Foreground, Border.
    /// </summary>
    /// <param name="elementType">The type of UI element</param>
    /// <param name="state">The state of the element</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ColorSet or null if not set</returns>
    Task<ColorSet?> GetElementColorsAsync(
        UIElementType elementType,
        UIElementState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a specific element and state to default colors.
    /// Default colors are built-in based on theme category (Light, Dark, HighContrast).
    /// </summary>
    /// <param name="elementType">The type of UI element</param>
    /// <param name="state">The state of the element</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task completing when reset is done</returns>
    Task ResetElementToDefaultAsync(
        UIElementType elementType,
        UIElementState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all elements and states to default colors.
    /// This effectively applies the default theme for current category (Light, Dark, HighContrast).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task completing when reset is done</returns>
    Task ResetAllToDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current comprehensive color theme (includes all customizations).
    /// This can be used to export current colors or save custom theme.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current color theme</returns>
    Task<ComprehensiveColorTheme> GetCurrentThemeAsync(CancellationToken cancellationToken = default);
}

using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color;

/// <summary>
/// Internal service for managing granular color customizations.
/// Provides direct color manipulation API without theme abstraction.
/// Thread-safe for concurrent color updates.
/// </summary>
internal sealed class ColorManagementService
{
    private readonly ILogger<ColorManagementService> _logger;
    private ComprehensiveColorTheme _currentTheme = ComprehensiveColorTheme.DefaultLight;
    private readonly object _themeLock = new();

    /// <summary>
    /// Event fired when theme changes (for UI synchronization)
    /// </summary>
    public event EventHandler<ComprehensiveColorTheme>? ThemeChanged;

    public ColorManagementService(ILogger<ColorManagementService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("ColorManagementService initialized with DefaultLight theme");
    }

    /// <summary>
    /// Sets a single color property for an element and state.
    /// Thread-safe via lock on theme updates.
    /// </summary>
    public async Task SetElementColorAsync(
        UIElementType elementType,
        UIElementState state,
        ColorProperty property,
        string hexColor,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make async-compliant

        _logger.LogDebug("Setting {Property} color for {Element}:{State} to {Color}",
            property, elementType, state, hexColor);

        lock (_themeLock)
        {
            // Clone current theme and update specific property
            _currentTheme = UpdateColorProperty(_currentTheme, elementType, state, property, hexColor);
            RaiseThemeChanged();
        }

        _logger.LogInformation("Color updated: {Element}:{State}.{Property} = {Color}",
            elementType, state, property, hexColor);
    }

    /// <summary>
    /// Sets multiple color properties for an element and state at once.
    /// More efficient than multiple SetElementColorAsync calls.
    /// </summary>
    public async Task SetElementColorsAsync(
        UIElementType elementType,
        UIElementState state,
        ColorSet colorSet,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        _logger.LogDebug("Setting colors for {Element}:{State}", elementType, state);

        lock (_themeLock)
        {
            var updatedTheme = _currentTheme;

            if (colorSet.Background != null)
                updatedTheme = UpdateColorProperty(updatedTheme, elementType, state, ColorProperty.Background, colorSet.Background);

            if (colorSet.Foreground != null)
                updatedTheme = UpdateColorProperty(updatedTheme, elementType, state, ColorProperty.Foreground, colorSet.Foreground);

            if (colorSet.Border != null)
                updatedTheme = UpdateColorProperty(updatedTheme, elementType, state, ColorProperty.Border, colorSet.Border);

            _currentTheme = updatedTheme;
            RaiseThemeChanged();
        }

        _logger.LogInformation("Colors updated for {Element}:{State}", elementType, state);
    }

    /// <summary>
    /// Bulk update multiple element colors in single operation.
    /// Atomic update - all changes applied together.
    /// </summary>
    public async Task SetMultipleElementColorsAsync(
        IReadOnlyDictionary<(UIElementType ElementType, UIElementState State, ColorProperty Property), string> colorUpdates,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        _logger.LogDebug("Bulk updating {Count} color properties", colorUpdates.Count);

        lock (_themeLock)
        {
            var updatedTheme = _currentTheme;

            foreach (var kvp in colorUpdates)
            {
                var (elementType, state, property) = kvp.Key;
                var hexColor = kvp.Value;
                updatedTheme = UpdateColorProperty(updatedTheme, elementType, state, property, hexColor);
            }

            _currentTheme = updatedTheme;
            RaiseThemeChanged();
        }

        _logger.LogInformation("Bulk color update completed: {Count} properties updated", colorUpdates.Count);
    }

    /// <summary>
    /// Gets the current color value for a specific element, state, and property.
    /// </summary>
    public async Task<string?> GetElementColorAsync(
        UIElementType elementType,
        UIElementState state,
        ColorProperty property,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_themeLock)
        {
            return GetColorProperty(_currentTheme, elementType, state, property);
        }
    }

    /// <summary>
    /// Gets all colors for a specific element and state.
    /// </summary>
    public async Task<ColorSet?> GetElementColorsAsync(
        UIElementType elementType,
        UIElementState state,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_themeLock)
        {
            return new ColorSet
            {
                Background = GetColorProperty(_currentTheme, elementType, state, ColorProperty.Background),
                Foreground = GetColorProperty(_currentTheme, elementType, state, ColorProperty.Foreground),
                Border = GetColorProperty(_currentTheme, elementType, state, ColorProperty.Border)
            };
        }
    }

    /// <summary>
    /// Resets element and state to default colors based on current theme category.
    /// </summary>
    public async Task ResetElementToDefaultAsync(
        UIElementType elementType,
        UIElementState state,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        _logger.LogDebug("Resetting {Element}:{State} to default", elementType, state);

        lock (_themeLock)
        {
            // Get default theme based on current category
            var defaultTheme = _currentTheme.Category switch
            {
                ThemeCategory.Light => ComprehensiveColorTheme.DefaultLight,
                ThemeCategory.Dark => ComprehensiveColorTheme.DefaultDark,
                ThemeCategory.HighContrast => ComprehensiveColorTheme.DefaultHighContrast,
                _ => ComprehensiveColorTheme.DefaultLight
            };

            // Copy colors from default theme for this element/state
            var defaultColors = GetColorSet(defaultTheme, elementType, state);
            if (defaultColors != null)
            {
                _currentTheme = SetColorSet(_currentTheme, elementType, state, defaultColors);
                RaiseThemeChanged();
            }
        }

        _logger.LogInformation("Reset {Element}:{State} to default", elementType, state);
    }

    /// <summary>
    /// Resets all elements to default colors for current theme category.
    /// </summary>
    public async Task ResetAllToDefaultAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        _logger.LogDebug("Resetting all colors to default for category {Category}", _currentTheme.Category);

        lock (_themeLock)
        {
            _currentTheme = _currentTheme.Category switch
            {
                ThemeCategory.Light => ComprehensiveColorTheme.DefaultLight,
                ThemeCategory.Dark => ComprehensiveColorTheme.DefaultDark,
                ThemeCategory.HighContrast => ComprehensiveColorTheme.DefaultHighContrast,
                _ => ComprehensiveColorTheme.DefaultLight
            };
            RaiseThemeChanged();
        }

        _logger.LogInformation("All colors reset to default");
    }

    /// <summary>
    /// Gets the current comprehensive theme (includes all customizations).
    /// </summary>
    public async Task<ComprehensiveColorTheme> GetCurrentThemeAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_themeLock)
        {
            return _currentTheme;
        }
    }

    /// <summary>
    /// Sets the entire theme (used by ThemeManagementService).
    /// </summary>
    internal async Task SetThemeAsync(ComprehensiveColorTheme theme, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (theme == null) throw new ArgumentNullException(nameof(theme));

        _logger.LogInformation("Setting theme: {ThemeName} ({Category})", theme.Name, theme.Category);

        lock (_themeLock)
        {
            _currentTheme = theme;
            RaiseThemeChanged();
        }
    }

    /// <summary>
    /// Helper: Updates a single color property in a theme (immutable pattern).
    /// </summary>
    private static ComprehensiveColorTheme UpdateColorProperty(
        ComprehensiveColorTheme theme,
        UIElementType elementType,
        UIElementState state,
        ColorProperty property,
        string hexColor)
    {
        // Get current ColorSet for this element/state
        var currentColorSet = GetColorSet(theme, elementType, state) ?? ColorSet.Default;

        // Create new ColorSet with updated property
        var newColorSet = property switch
        {
            ColorProperty.Background => currentColorSet with { Background = hexColor },
            ColorProperty.Foreground => currentColorSet with { Foreground = hexColor },
            ColorProperty.Border => currentColorSet with { Border = hexColor },
            _ => currentColorSet
        };

        // Set updated ColorSet back to theme
        return SetColorSet(theme, elementType, state, newColorSet);
    }

    /// <summary>
    /// Helper: Gets ColorSet for element/state from theme.
    /// </summary>
    private static ColorSet? GetColorSet(
        ComprehensiveColorTheme theme,
        UIElementType elementType,
        UIElementState state)
    {
        return (elementType, state) switch
        {
            (UIElementType.Header, UIElementState.Normal) => theme.HeaderColors.Normal,
            (UIElementType.Header, UIElementState.Hover) => theme.HeaderColors.Hover,
            (UIElementType.Header, UIElementState.Pressed) => theme.HeaderColors.Pressed,
            (UIElementType.Header, UIElementState.Selected) => theme.HeaderColors.Selected,
            (UIElementType.Header, UIElementState.Disabled) => theme.HeaderColors.Disabled,

            (UIElementType.Cell, UIElementState.Normal) => theme.CellColors.Normal,
            (UIElementType.Cell, UIElementState.Hover) => theme.CellColors.Hover,
            (UIElementType.Cell, UIElementState.Focused) => theme.CellColors.Focused,
            (UIElementType.Cell, UIElementState.Editing) => theme.CellColors.Editing,
            (UIElementType.Cell, UIElementState.Error) => theme.CellColors.Error,
            (UIElementType.Cell, UIElementState.Warning) => theme.CellColors.Warning,
            (UIElementType.Cell, UIElementState.Success) => theme.CellColors.Success,
            (UIElementType.Cell, UIElementState.ReadOnly) => theme.CellColors.ReadOnly,
            (UIElementType.Cell, UIElementState.Disabled) => theme.CellColors.Disabled,
            (UIElementType.Cell, UIElementState.Selected) => theme.CellColors.Selected,

            (UIElementType.Button, UIElementState.Normal) => theme.ButtonColors.Normal,
            (UIElementType.Button, UIElementState.Hover) => theme.ButtonColors.Hover,
            (UIElementType.Button, UIElementState.Pressed) => theme.ButtonColors.Pressed,
            (UIElementType.Button, UIElementState.Disabled) => theme.ButtonColors.Disabled,

            (UIElementType.Row, UIElementState.Normal) => theme.RowColors.Normal,
            (UIElementType.Row, UIElementState.Hover) => theme.RowColors.Hover,
            (UIElementType.Row, UIElementState.Selected) => theme.RowColors.Selected,

            _ => null
        };
    }

    /// <summary>
    /// Helper: Sets ColorSet for element/state in theme (immutable pattern).
    /// </summary>
    private static ComprehensiveColorTheme SetColorSet(
        ComprehensiveColorTheme theme,
        UIElementType elementType,
        UIElementState state,
        ColorSet colorSet)
    {
        return (elementType, state) switch
        {
            (UIElementType.Header, UIElementState.Normal) => theme with { HeaderColors = theme.HeaderColors with { Normal = colorSet } },
            (UIElementType.Header, UIElementState.Hover) => theme with { HeaderColors = theme.HeaderColors with { Hover = colorSet } },
            (UIElementType.Header, UIElementState.Pressed) => theme with { HeaderColors = theme.HeaderColors with { Pressed = colorSet } },
            (UIElementType.Header, UIElementState.Selected) => theme with { HeaderColors = theme.HeaderColors with { Selected = colorSet } },
            (UIElementType.Header, UIElementState.Disabled) => theme with { HeaderColors = theme.HeaderColors with { Disabled = colorSet } },

            (UIElementType.Cell, UIElementState.Normal) => theme with { CellColors = theme.CellColors with { Normal = colorSet } },
            (UIElementType.Cell, UIElementState.Hover) => theme with { CellColors = theme.CellColors with { Hover = colorSet } },
            (UIElementType.Cell, UIElementState.Focused) => theme with { CellColors = theme.CellColors with { Focused = colorSet } },
            (UIElementType.Cell, UIElementState.Editing) => theme with { CellColors = theme.CellColors with { Editing = colorSet } },
            (UIElementType.Cell, UIElementState.Error) => theme with { CellColors = theme.CellColors with { Error = colorSet } },
            (UIElementType.Cell, UIElementState.Warning) => theme with { CellColors = theme.CellColors with { Warning = colorSet } },
            (UIElementType.Cell, UIElementState.Success) => theme with { CellColors = theme.CellColors with { Success = colorSet } },
            (UIElementType.Cell, UIElementState.ReadOnly) => theme with { CellColors = theme.CellColors with { ReadOnly = colorSet } },
            (UIElementType.Cell, UIElementState.Disabled) => theme with { CellColors = theme.CellColors with { Disabled = colorSet } },
            (UIElementType.Cell, UIElementState.Selected) => theme with { CellColors = theme.CellColors with { Selected = colorSet } },

            (UIElementType.Button, UIElementState.Normal) => theme with { ButtonColors = theme.ButtonColors with { Normal = colorSet } },
            (UIElementType.Button, UIElementState.Hover) => theme with { ButtonColors = theme.ButtonColors with { Hover = colorSet } },
            (UIElementType.Button, UIElementState.Pressed) => theme with { ButtonColors = theme.ButtonColors with { Pressed = colorSet } },
            (UIElementType.Button, UIElementState.Disabled) => theme with { ButtonColors = theme.ButtonColors with { Disabled = colorSet } },

            (UIElementType.Row, UIElementState.Normal) => theme with { RowColors = theme.RowColors with { Normal = colorSet } },
            (UIElementType.Row, UIElementState.Hover) => theme with { RowColors = theme.RowColors with { Hover = colorSet } },
            (UIElementType.Row, UIElementState.Selected) => theme with { RowColors = theme.RowColors with { Selected = colorSet } },

            _ => theme
        };
    }

    /// <summary>
    /// Helper: Gets specific color property from theme.
    /// </summary>
    private static string? GetColorProperty(
        ComprehensiveColorTheme theme,
        UIElementType elementType,
        UIElementState state,
        ColorProperty property)
    {
        var colorSet = GetColorSet(theme, elementType, state);
        if (colorSet == null) return null;

        return property switch
        {
            ColorProperty.Background => colorSet.Background,
            ColorProperty.Foreground => colorSet.Foreground,
            ColorProperty.Border => colorSet.Border,
            _ => null
        };
    }

    /// <summary>
    /// Raises the ThemeChanged event (must be called inside lock).
    /// </summary>
    private void RaiseThemeChanged()
    {
        try
        {
            ThemeChanged?.Invoke(this, _currentTheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing ThemeChanged event");
        }
    }
}

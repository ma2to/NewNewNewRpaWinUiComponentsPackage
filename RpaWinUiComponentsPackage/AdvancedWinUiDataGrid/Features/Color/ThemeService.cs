using Microsoft.Extensions.Logging;
using System.Text.Json;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color;

/// <summary>
/// Internal service for theme management
/// Stores current theme and provides theme manipulation
/// </summary>
internal sealed class ThemeService : IThemeService
{
    private readonly ILogger? _logger;
    private GridTheme _currentTheme;
    private readonly Dictionary<string, GridTheme> _themes;

    public ThemeService(ILogger? logger = null)
    {
        _logger = logger;
        _currentTheme = GridTheme.Default;
        _themes = new Dictionary<string, GridTheme>
        {
            ["Default"] = GridTheme.Default,
            ["Dark"] = GridTheme.Dark,
            ["Light"] = GridTheme.Light,
            ["HighContrast"] = GridTheme.HighContrast
        };
    }

    /// <summary>
    /// Gets the current active theme
    /// </summary>
    public GridTheme GetCurrentTheme() => _currentTheme;

    /// <summary>
    /// Applies a new theme
    /// </summary>
    public void ApplyTheme(GridTheme theme)
    {
        if (theme == null)
            throw new ArgumentNullException(nameof(theme));

        _currentTheme = theme;
        _logger?.LogInformation("Applied theme: {ThemeName}", theme.ThemeName);
    }

    /// <summary>
    /// Resets to default theme
    /// </summary>
    public void ResetToDefault()
    {
        _currentTheme = GridTheme.Default;
        _logger?.LogInformation("Reset theme to default");
    }

    /// <summary>
    /// Updates cell colors in current theme
    /// </summary>
    public void UpdateCellColors(CellColors cellColors)
    {
        if (cellColors == null)
            throw new ArgumentNullException(nameof(cellColors));

        _currentTheme = _currentTheme with { CellColors = cellColors };
        _logger?.LogDebug("Updated cell colors in theme");
    }

    /// <summary>
    /// Updates row colors in current theme
    /// </summary>
    public void UpdateRowColors(RowColors rowColors)
    {
        if (rowColors == null)
            throw new ArgumentNullException(nameof(rowColors));

        _currentTheme = _currentTheme with { RowColors = rowColors };
        _logger?.LogDebug("Updated row colors in theme");
    }

    /// <summary>
    /// Updates validation colors in current theme
    /// </summary>
    public void UpdateValidationColors(ValidationColors validationColors)
    {
        if (validationColors == null)
            throw new ArgumentNullException(nameof(validationColors));

        _currentTheme = _currentTheme with { ValidationColors = validationColors };
        _logger?.LogDebug("Updated validation colors in theme");
    }

    public async Task CreateThemeAsync(string themeName, IEnumerable<PublicElementStatePropertyColor> colorDefinitions, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var theme = new GridTheme { ThemeName = themeName };
            _themes[themeName] = theme;
            _logger?.LogInformation("Created theme: {ThemeName}", themeName);
        }, cancellationToken);
    }

    public async Task ApplyThemeAsync(string themeName, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (_themes.TryGetValue(themeName, out var theme))
            {
                ApplyTheme(theme);
            }
            else
            {
                throw new InvalidOperationException($"Theme '{themeName}' not found");
            }
        }, cancellationToken);
    }

    public async Task SaveThemeAsync(string themeName, string filePath, CancellationToken cancellationToken = default)
    {
        if (_themes.TryGetValue(themeName, out var theme))
        {
            var json = JsonSerializer.Serialize(theme);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            _logger?.LogInformation("Saved theme '{ThemeName}' to {FilePath}", themeName, filePath);
        }
        else
        {
            throw new InvalidOperationException($"Theme '{themeName}' not found");
        }
    }

    public async Task LoadThemeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var theme = JsonSerializer.Deserialize<GridTheme>(json);
        if (theme != null)
        {
            _themes[theme.ThemeName] = theme;
            _logger?.LogInformation("Loaded theme '{ThemeName}' from {FilePath}", theme.ThemeName, filePath);
        }
    }

    public IReadOnlyList<string> GetAvailableThemes()
    {
        return _themes.Keys.ToList();
    }

    string IThemeService.GetCurrentTheme()
    {
        return _currentTheme.ThemeName;
    }

    public async Task ResetToDefaultThemeAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            ResetToDefault();
        }, cancellationToken);
    }

    public async Task DeleteThemeAsync(string themeName, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (themeName == "Default" || themeName == "Dark" || themeName == "Light" || themeName == "HighContrast")
            {
                throw new InvalidOperationException($"Cannot delete built-in theme '{themeName}'");
            }

            if (_themes.Remove(themeName))
            {
                _logger?.LogInformation("Deleted theme: {ThemeName}", themeName);
            }
            else
            {
                throw new InvalidOperationException($"Theme '{themeName}' not found");
            }
        }, cancellationToken);
    }
}

/// <summary>
/// Internal grid theme
/// </summary>
internal sealed record GridTheme
{
    internal string ThemeName { get; init; } = "Default";
    internal CellColors CellColors { get; init; } = new();
    internal RowColors RowColors { get; init; } = new();
    internal HeaderColors HeaderColors { get; init; } = new();
    internal ValidationColors ValidationColors { get; init; } = new();
    internal SelectionColors SelectionColors { get; init; } = new();
    internal BorderColors BorderColors { get; init; } = new();

    internal static GridTheme Default { get; } = new();

    internal static GridTheme Dark { get; } = new()
    {
        ThemeName = "Dark",
        CellColors = new CellColors
        {
            DefaultBackground = "#1E1E1E",
            DefaultForeground = "#FFFFFF",
            HoverBackground = "#2D2D2D",
            HoverForeground = "#FFFFFF",
            FocusedBackground = "#264F78",
            FocusedForeground = "#FFFFFF",
            DisabledBackground = "#2D2D2D",
            DisabledForeground = "#858585",
            ReadOnlyBackground = "#252525",
            ReadOnlyForeground = "#CCCCCC"
        },
        RowColors = new RowColors
        {
            EvenRowBackground = "#1E1E1E",
            OddRowBackground = "#252525",
            HoverBackground = "#2D2D2D",
            SelectedBackground = "#0E639C",
            SelectedForeground = "#FFFFFF",
            SelectedInactiveBackground = "#3F3F46",
            SelectedInactiveForeground = "#CCCCCC"
        },
        HeaderColors = new HeaderColors
        {
            Background = "#252525",
            Foreground = "#FFFFFF",
            HoverBackground = "#2D2D2D",
            PressedBackground = "#3F3F46",
            SortIndicatorColor = "#0E639C"
        },
        ValidationColors = new ValidationColors
        {
            ErrorBackground = "#5A1D1D",
            ErrorForeground = "#F48771",
            ErrorBorder = "#F14C4C",
            WarningBackground = "#5C4E1D",
            WarningForeground = "#FFD68F",
            WarningBorder = "#FFC700",
            InfoBackground = "#1E3A5F",
            InfoForeground = "#89D4FF",
            InfoBorder = "#3794FF"
        },
        SelectionColors = new SelectionColors
        {
            SelectionBorder = "#0E639C",
            SelectionFill = "#0E639C33",
            MultiSelectionBackground = "#264F78",
            MultiSelectionForeground = "#FFFFFF"
        },
        BorderColors = new BorderColors
        {
            CellBorder = "#3F3F46",
            RowBorder = "#3F3F46",
            ColumnBorder = "#3F3F46",
            GridBorder = "#555555",
            FocusedCellBorder = "#0E639C"
        }
    };

    internal static GridTheme Light { get; } = Default;

    internal static GridTheme HighContrast { get; } = new()
    {
        ThemeName = "HighContrast",
        CellColors = new CellColors
        {
            DefaultBackground = "#FFFFFF",
            DefaultForeground = "#000000",
            HoverBackground = "#FFFF00",
            HoverForeground = "#000000",
            FocusedBackground = "#00FFFF",
            FocusedForeground = "#000000",
            DisabledBackground = "#808080",
            DisabledForeground = "#000000",
            ReadOnlyBackground = "#E0E0E0",
            ReadOnlyForeground = "#000000"
        },
        RowColors = new RowColors
        {
            EvenRowBackground = "#FFFFFF",
            OddRowBackground = "#FFFFFF",
            HoverBackground = "#FFFF00",
            SelectedBackground = "#0000FF",
            SelectedForeground = "#FFFFFF",
            SelectedInactiveBackground = "#808080",
            SelectedInactiveForeground = "#FFFFFF"
        },
        HeaderColors = new HeaderColors
        {
            Background = "#000000",
            Foreground = "#FFFFFF",
            HoverBackground = "#0000FF",
            PressedBackground = "#00FF00",
            SortIndicatorColor = "#00FFFF"
        },
        ValidationColors = new ValidationColors
        {
            ErrorBackground = "#FF0000",
            ErrorForeground = "#FFFFFF",
            ErrorBorder = "#FFFFFF",
            WarningBackground = "#FFFF00",
            WarningForeground = "#000000",
            WarningBorder = "#000000",
            InfoBackground = "#0000FF",
            InfoForeground = "#FFFFFF",
            InfoBorder = "#FFFFFF"
        },
        SelectionColors = new SelectionColors
        {
            SelectionBorder = "#00FFFF",
            SelectionFill = "#00FFFF80",
            MultiSelectionBackground = "#0000FF",
            MultiSelectionForeground = "#FFFFFF"
        },
        BorderColors = new BorderColors
        {
            CellBorder = "#000000",
            RowBorder = "#000000",
            ColumnBorder = "#000000",
            GridBorder = "#000000",
            FocusedCellBorder = "#00FFFF"
        }
    };
}

internal sealed record CellColors
{
    internal string DefaultBackground { get; init; } = "#FFFFFF";
    internal string DefaultForeground { get; init; } = "#000000";
    internal string HoverBackground { get; init; } = "#F5F5F5";
    internal string HoverForeground { get; init; } = "#000000";
    internal string FocusedBackground { get; init; } = "#E3F2FD";
    internal string FocusedForeground { get; init; } = "#000000";
    internal string DisabledBackground { get; init; } = "#F0F0F0";
    internal string DisabledForeground { get; init; } = "#A0A0A0";
    internal string ReadOnlyBackground { get; init; } = "#FAFAFA";
    internal string ReadOnlyForeground { get; init; } = "#000000";
}

internal sealed record RowColors
{
    internal string EvenRowBackground { get; init; } = "#FFFFFF";
    internal string OddRowBackground { get; init; } = "#F9F9F9";
    internal string HoverBackground { get; init; } = "#F0F0F0";
    internal string SelectedBackground { get; init; } = "#0078D4";
    internal string SelectedForeground { get; init; } = "#FFFFFF";
    internal string SelectedInactiveBackground { get; init; } = "#CCCCCC";
    internal string SelectedInactiveForeground { get; init; } = "#000000";
}

internal sealed record HeaderColors
{
    internal string Background { get; init; } = "#F5F5F5";
    internal string Foreground { get; init; } = "#000000";
    internal string HoverBackground { get; init; } = "#E0E0E0";
    internal string PressedBackground { get; init; } = "#D0D0D0";
    internal string SortIndicatorColor { get; init; } = "#0078D4";
}

internal sealed record ValidationColors
{
    internal string ErrorBackground { get; init; } = "#FFEBEE";
    internal string ErrorForeground { get; init; } = "#D32F2F";
    internal string ErrorBorder { get; init; } = "#F44336";
    internal string WarningBackground { get; init; } = "#FFF3E0";
    internal string WarningForeground { get; init; } = "#F57C00";
    internal string WarningBorder { get; init; } = "#FF9800";
    internal string InfoBackground { get; init; } = "#E3F2FD";
    internal string InfoForeground { get; init; } = "#1976D2";
    internal string InfoBorder { get; init; } = "#2196F3";
}

internal sealed record SelectionColors
{
    internal string SelectionBorder { get; init; } = "#0078D4";
    internal string SelectionFill { get; init; } = "#0078D433";
    internal string MultiSelectionBackground { get; init; } = "#CCE5FF";
    internal string MultiSelectionForeground { get; init; } = "#000000";
}

internal sealed record BorderColors
{
    internal string CellBorder { get; init; } = "#E0E0E0";
    internal string RowBorder { get; init; } = "#E0E0E0";
    internal string ColumnBorder { get; init; } = "#E0E0E0";
    internal string GridBorder { get; init; } = "#CCCCCC";
    internal string FocusedCellBorder { get; init; } = "#0078D4";
}

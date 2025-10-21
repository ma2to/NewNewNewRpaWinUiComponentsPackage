using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// Centralized theme manager for the DataGrid.
/// Manages all color schemes and provides SolidColorBrush instances for UI binding.
/// Supports runtime theme changes and partial theme updates (cell colors, row colors, validation colors separately).
/// </summary>
public sealed class ThemeManager : ViewModelBase
{
    private readonly ILogger<ThemeManager>? _logger;
    private PublicGridTheme _currentTheme = new();

    /// <summary>
    /// Adds theme changed event handler using WeakEventManager.
    /// Prevents memory leaks - handler can be GC'd even without explicit removal.
    /// </summary>
    /// <param name="handler">Event handler to add</param>
    public void AddThemeChangedHandler(EventHandler<EventArgs> handler)
    {
        Features.Optimization.WeakEventManager<ThemeManager, EventArgs>.AddHandler(
            this,
            nameof(ThemeChanged),
            handler);
    }

    /// <summary>
    /// Removes theme changed event handler.
    /// </summary>
    /// <param name="handler">Event handler to remove</param>
    public void RemoveThemeChangedHandler(EventHandler<EventArgs> handler)
    {
        Features.Optimization.WeakEventManager<ThemeManager, EventArgs>.RemoveHandler(
            this,
            nameof(ThemeChanged),
            handler);
    }

    /// <summary>
    /// Raises theme changed event to all subscribers.
    /// Uses WeakEventManager to prevent memory leaks.
    /// </summary>
    private void RaiseThemeChanged()
    {
        Features.Optimization.WeakEventManager<ThemeManager, EventArgs>.RaiseEvent(
            this,
            EventArgs.Empty);
    }

    // Backward compatibility property (for code that checks event != null)
    private const string ThemeChanged = "ThemeChanged";

    /// <summary>
    /// Gets or sets the grid options for accessing checkbox styling and other configuration.
    /// Used by UI controls to get checkbox appearance settings.
    /// </summary>
    public AdvancedDataGridOptions? Options { get; set; }

    /// <summary>
    /// Creates a new ThemeManager instance.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics and troubleshooting</param>
    public ThemeManager(ILogger<ThemeManager>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation("ThemeManager created with default theme");
    }

    /// <summary>
    /// Event handler for ColorManagementService theme changes.
    /// Converts comprehensive theme to public theme and applies it.
    /// This should be wired up externally via event subscription.
    /// </summary>
    internal void OnColorServiceThemeChanged(object? sender, ComprehensiveColorTheme comprehensiveTheme)
    {
        try
        {
            _logger?.LogDebug("ColorManagementService theme changed, updating UI theme");
            ApplyComprehensiveTheme(comprehensiveTheme);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying comprehensive theme from ColorManagementService");
        }
    }

    /// <summary>
    /// Gets or sets the current theme configuration.
    /// When set, fires the ThemeChanged event to notify all subscribers.
    /// </summary>
    public PublicGridTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (SetProperty(ref _currentTheme, value))
            {
                _logger?.LogInformation("Theme changed to: {ThemeName}", value.ThemeName ?? "unnamed");
                RaiseThemeChanged();
            }
        }
    }

    // Cell colors
    public SolidColorBrush CellDefaultBackground => ParseColor(_currentTheme.CellColors.DefaultBackground);
    public SolidColorBrush CellDefaultForeground => ParseColor(_currentTheme.CellColors.DefaultForeground);
    public SolidColorBrush CellHoverBackground => ParseColor(_currentTheme.CellColors.HoverBackground);
    public SolidColorBrush CellHoverForeground => ParseColor(_currentTheme.CellColors.HoverForeground);
    public SolidColorBrush CellFocusedBackground => ParseColor(_currentTheme.CellColors.FocusedBackground);
    public SolidColorBrush CellFocusedForeground => ParseColor(_currentTheme.CellColors.FocusedForeground);

    // Header colors
    public SolidColorBrush HeaderBackground => ParseColor(_currentTheme.HeaderColors.Background);
    public SolidColorBrush HeaderForeground => ParseColor(_currentTheme.HeaderColors.Foreground);
    public SolidColorBrush HeaderHoverBackground => ParseColor(_currentTheme.HeaderColors.HoverBackground);
    public SolidColorBrush HeaderPressedBackground => ParseColor(_currentTheme.HeaderColors.PressedBackground);

    // Validation colors
    public SolidColorBrush ValidationErrorBackground => ParseColor(_currentTheme.ValidationColors.ErrorBackground);
    public SolidColorBrush ValidationErrorForeground => ParseColor(_currentTheme.ValidationColors.ErrorForeground);
    public SolidColorBrush ValidationErrorBorder => ParseColor(_currentTheme.ValidationColors.ErrorBorder);
    public SolidColorBrush ValidationWarningBackground => ParseColor(_currentTheme.ValidationColors.WarningBackground);
    public SolidColorBrush ValidationWarningForeground => ParseColor(_currentTheme.ValidationColors.WarningForeground);
    public SolidColorBrush ValidationWarningBorder => ParseColor(_currentTheme.ValidationColors.WarningBorder);

    // Selection colors
    public SolidColorBrush SelectionBorder => ParseColor(_currentTheme.SelectionColors.SelectionBorder);
    public SolidColorBrush SelectionFill => ParseColor(_currentTheme.SelectionColors.SelectionFill);
    public SolidColorBrush MultiSelectionBackground => ParseColor(_currentTheme.SelectionColors.MultiSelectionBackground);
    public SolidColorBrush MultiSelectionForeground => ParseColor(_currentTheme.SelectionColors.MultiSelectionForeground);

    // Border colors
    public SolidColorBrush CellBorder => ParseColor(_currentTheme.BorderColors.CellBorder);
    public SolidColorBrush RowBorder => ParseColor(_currentTheme.BorderColors.RowBorder);
    public SolidColorBrush ColumnBorder => ParseColor(_currentTheme.BorderColors.ColumnBorder);
    public SolidColorBrush GridBorder => ParseColor(_currentTheme.BorderColors.GridBorder);
    public SolidColorBrush FocusedCellBorder => ParseColor(_currentTheme.BorderColors.FocusedCellBorder);

    // Row colors
    public SolidColorBrush EvenRowBackground => ParseColor(_currentTheme.RowColors.EvenRowBackground);
    public SolidColorBrush OddRowBackground => ParseColor(_currentTheme.RowColors.OddRowBackground);
    public SolidColorBrush SelectedRowBackground => ParseColor(_currentTheme.RowColors.SelectedBackground);
    public SolidColorBrush SelectedRowForeground => ParseColor(_currentTheme.RowColors.SelectedForeground);

    /// <summary>
    /// Parses hex color string to SolidColorBrush using BrushPool for deduplication.
    /// Supports formats: #RGB, #RRGGBB, #AARRGGBB
    /// Memory optimization: Colors with same value share single brush instance.
    /// </summary>
    private SolidColorBrush ParseColor(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || !hexColor.StartsWith("#"))
        {
            return Features.Optimization.BrushPool.GetBrush(Colors.Transparent);
        }

        try
        {
            var hex = hexColor.TrimStart('#');

            // Handle different hex formats
            byte a = 255, r = 0, g = 0, b = 0;

            if (hex.Length == 3) // #RGB
            {
                r = Convert.ToByte(hex.Substring(0, 1) + hex.Substring(0, 1), 16);
                g = Convert.ToByte(hex.Substring(1, 1) + hex.Substring(1, 1), 16);
                b = Convert.ToByte(hex.Substring(2, 1) + hex.Substring(2, 1), 16);
            }
            else if (hex.Length == 6) // #RRGGBB
            {
                r = Convert.ToByte(hex.Substring(0, 2), 16);
                g = Convert.ToByte(hex.Substring(2, 2), 16);
                b = Convert.ToByte(hex.Substring(4, 2), 16);
            }
            else if (hex.Length == 8) // #AARRGGBB
            {
                a = Convert.ToByte(hex.Substring(0, 2), 16);
                r = Convert.ToByte(hex.Substring(2, 2), 16);
                g = Convert.ToByte(hex.Substring(4, 2), 16);
                b = Convert.ToByte(hex.Substring(6, 2), 16);
            }

            // Use BrushPool for deduplication (memory optimization)
            return Features.Optimization.BrushPool.GetBrush(Color.FromArgb(a, r, g, b));
        }
        catch
        {
            // Fallback to transparent if parsing fails
            return Features.Optimization.BrushPool.GetBrush(Colors.Transparent);
        }
    }

    /// <summary>
    /// Updates only the cell colors without changing other theme aspects.
    /// This is useful for fine-tuning cell appearance without affecting headers, validation, etc.
    /// </summary>
    /// <param name="cellColors">The new cell color configuration</param>
    /// <exception cref="ArgumentNullException">Thrown when cellColors is null</exception>
    public void UpdateCellColors(PublicCellColors cellColors)
    {
        if (cellColors == null) throw new ArgumentNullException(nameof(cellColors));

        _logger?.LogInformation("Updating cell colors");

        // Create a new theme with updated cell colors but preserve other colors
        _currentTheme = new PublicGridTheme
        {
            ThemeName = _currentTheme.ThemeName,
            CellColors = cellColors,
            RowColors = _currentTheme.RowColors,
            HeaderColors = _currentTheme.HeaderColors,
            ValidationColors = _currentTheme.ValidationColors,
            SelectionColors = _currentTheme.SelectionColors,
            BorderColors = _currentTheme.BorderColors
        };

        NotifyCellColorsChanged();
    }

    /// <summary>
    /// Updates only the row colors without changing other theme aspects.
    /// This affects row backgrounds (even/odd alternating rows, selected rows, etc.).
    /// </summary>
    /// <param name="rowColors">The new row color configuration</param>
    /// <exception cref="ArgumentNullException">Thrown when rowColors is null</exception>
    public void UpdateRowColors(PublicRowColors rowColors)
    {
        if (rowColors == null) throw new ArgumentNullException(nameof(rowColors));

        _logger?.LogInformation("Updating row colors");

        // Create a new theme with updated row colors but preserve other colors
        _currentTheme = new PublicGridTheme
        {
            ThemeName = _currentTheme.ThemeName,
            CellColors = _currentTheme.CellColors,
            RowColors = rowColors,
            HeaderColors = _currentTheme.HeaderColors,
            ValidationColors = _currentTheme.ValidationColors,
            SelectionColors = _currentTheme.SelectionColors,
            BorderColors = _currentTheme.BorderColors
        };

        NotifyRowColorsChanged();
    }

    /// <summary>
    /// Updates only the validation colors without changing other theme aspects.
    /// This affects how validation errors and warnings are displayed.
    /// </summary>
    /// <param name="validationColors">The new validation color configuration</param>
    /// <exception cref="ArgumentNullException">Thrown when validationColors is null</exception>
    public void UpdateValidationColors(PublicValidationColors validationColors)
    {
        if (validationColors == null) throw new ArgumentNullException(nameof(validationColors));

        _logger?.LogInformation("Updating validation colors");

        // Create a new theme with updated validation colors but preserve other colors
        _currentTheme = new PublicGridTheme
        {
            ThemeName = _currentTheme.ThemeName,
            CellColors = _currentTheme.CellColors,
            RowColors = _currentTheme.RowColors,
            HeaderColors = _currentTheme.HeaderColors,
            ValidationColors = validationColors,
            SelectionColors = _currentTheme.SelectionColors,
            BorderColors = _currentTheme.BorderColors
        };

        NotifyValidationColorsChanged();
    }

    /// <summary>
    /// Applies a complete theme to the grid, replacing all color settings.
    /// This notifies all UI elements to refresh their appearance.
    /// </summary>
    /// <param name="theme">The theme to apply</param>
    /// <exception cref="ArgumentNullException">Thrown when theme is null</exception>
    public void ApplyTheme(PublicGridTheme theme)
    {
        CurrentTheme = theme ?? throw new ArgumentNullException(nameof(theme));

        _logger?.LogInformation("Applied full theme: {ThemeName}", theme.ThemeName ?? "unnamed");

        // Notify all properties changed so UI refreshes completely
        NotifyAllColorsChanged();
    }

    private void NotifyCellColorsChanged()
    {
        OnPropertyChanged(nameof(CellDefaultBackground));
        OnPropertyChanged(nameof(CellDefaultForeground));
        OnPropertyChanged(nameof(CellHoverBackground));
        OnPropertyChanged(nameof(CellHoverForeground));
        OnPropertyChanged(nameof(CellFocusedBackground));
        OnPropertyChanged(nameof(CellFocusedForeground));
    }

    private void NotifyRowColorsChanged()
    {
        OnPropertyChanged(nameof(EvenRowBackground));
        OnPropertyChanged(nameof(OddRowBackground));
        OnPropertyChanged(nameof(SelectedRowBackground));
        OnPropertyChanged(nameof(SelectedRowForeground));
    }

    private void NotifyValidationColorsChanged()
    {
        OnPropertyChanged(nameof(ValidationErrorBackground));
        OnPropertyChanged(nameof(ValidationErrorForeground));
        OnPropertyChanged(nameof(ValidationErrorBorder));
        OnPropertyChanged(nameof(ValidationWarningBackground));
        OnPropertyChanged(nameof(ValidationWarningForeground));
        OnPropertyChanged(nameof(ValidationWarningBorder));
    }

    private void NotifyAllColorsChanged()
    {
        NotifyCellColorsChanged();
        NotifyRowColorsChanged();
        NotifyValidationColorsChanged();

        OnPropertyChanged(nameof(HeaderBackground));
        OnPropertyChanged(nameof(HeaderForeground));
        OnPropertyChanged(nameof(HeaderHoverBackground));
        OnPropertyChanged(nameof(HeaderPressedBackground));
        OnPropertyChanged(nameof(SelectionBorder));
        OnPropertyChanged(nameof(SelectionFill));
        OnPropertyChanged(nameof(MultiSelectionBackground));
        OnPropertyChanged(nameof(MultiSelectionForeground));
        OnPropertyChanged(nameof(CellBorder));
        OnPropertyChanged(nameof(RowBorder));
        OnPropertyChanged(nameof(ColumnBorder));
        OnPropertyChanged(nameof(GridBorder));
        OnPropertyChanged(nameof(FocusedCellBorder));

        RaiseThemeChanged();
    }

    /// <summary>
    /// Applies a comprehensive color theme from the new color API.
    /// Converts ComprehensiveColorTheme to PublicGridTheme and applies it.
    /// </summary>
    /// <param name="comprehensiveTheme">The comprehensive theme to apply</param>
    /// <exception cref="ArgumentNullException">Thrown when comprehensiveTheme is null</exception>
    public void ApplyComprehensiveTheme(ComprehensiveColorTheme comprehensiveTheme)
    {
        if (comprehensiveTheme == null) throw new ArgumentNullException(nameof(comprehensiveTheme));

        _logger?.LogInformation("Converting and applying comprehensive theme: {ThemeName}", comprehensiveTheme.Name);

        var publicTheme = ConvertToPublicGridTheme(comprehensiveTheme);
        ApplyTheme(publicTheme);

        _logger?.LogInformation("Comprehensive theme applied successfully: {ThemeName}", comprehensiveTheme.Name);
    }

    /// <summary>
    /// Converts ComprehensiveColorTheme to PublicGridTheme.
    /// Maps the comprehensive state-based colors to the existing theme model.
    /// </summary>
    /// <param name="source">Source comprehensive theme</param>
    /// <returns>Converted PublicGridTheme</returns>
    private PublicGridTheme ConvertToPublicGridTheme(ComprehensiveColorTheme source)
    {
        return new PublicGridTheme
        {
            ThemeName = source.Name,

            // Map cell colors
            CellColors = new PublicCellColors
            {
                DefaultBackground = source.CellColors.Normal.Background ?? "#FFFFFF",
                DefaultForeground = source.CellColors.Normal.Foreground ?? "#000000",
                HoverBackground = source.CellColors.Hover.Background ?? "#F5F5F5",
                HoverForeground = source.CellColors.Hover.Foreground ?? "#000000",
                FocusedBackground = source.CellColors.Focused.Background ?? "#E3F2FD",
                FocusedForeground = source.CellColors.Focused.Foreground ?? "#000000",
                DisabledBackground = source.CellColors.Disabled.Background ?? "#F0F0F0",
                DisabledForeground = source.CellColors.Disabled.Foreground ?? "#A0A0A0",
                ReadOnlyBackground = source.CellColors.ReadOnly.Background ?? "#FAFAFA",
                ReadOnlyForeground = source.CellColors.ReadOnly.Foreground ?? "#000000"
            },

            // Map row colors
            RowColors = new PublicRowColors
            {
                EvenRowBackground = source.RowColors.Normal.Background ?? "#FFFFFF",
                OddRowBackground = source.RowColors.Alternate?.Background ?? "#F9F9F9",
                HoverBackground = source.RowColors.Hover.Background ?? "#F0F0F0",
                SelectedBackground = source.RowColors.Selected.Background ?? "#0078D4",
                SelectedForeground = source.RowColors.Selected.Foreground ?? "#FFFFFF",
                SelectedInactiveBackground = source.RowColors.SelectedInactive?.Background ?? "#CCCCCC",
                SelectedInactiveForeground = source.RowColors.SelectedInactive?.Foreground ?? "#000000"
            },

            // Map header colors
            HeaderColors = new PublicHeaderColors
            {
                Background = source.HeaderColors.Normal.Background ?? "#F5F5F5",
                Foreground = source.HeaderColors.Normal.Foreground ?? "#000000",
                HoverBackground = source.HeaderColors.Hover.Background ?? "#E0E0E0",
                PressedBackground = source.HeaderColors.Pressed.Background ?? "#D0D0D0",
                SortIndicatorColor = source.HeaderColors.Selected.Background ?? "#0078D4"
            },

            // Map validation colors from cell states
            ValidationColors = new PublicValidationColors
            {
                ErrorBackground = source.CellColors.Error.Background ?? "#FFEBEE",
                ErrorForeground = source.CellColors.Error.Foreground ?? "#D32F2F",
                ErrorBorder = source.CellColors.Error.Border ?? "#F44336",
                WarningBackground = source.CellColors.Warning.Background ?? "#FFF3E0",
                WarningForeground = source.CellColors.Warning.Foreground ?? "#F57C00",
                WarningBorder = source.CellColors.Warning.Border ?? "#FF9800",
                // Use Success colors for Info (no dedicated Info state in CellElementColors)
                InfoBackground = source.CellColors.Success.Background ?? "#E3F2FD",
                InfoForeground = source.CellColors.Success.Foreground ?? "#1976D2",
                InfoBorder = source.CellColors.Success.Border ?? "#2196F3"
            },

            // Map selection colors from cell/row selected states
            SelectionColors = new PublicSelectionColors
            {
                SelectionBorder = source.CellColors.Selected.Border ?? "#0078D4",
                SelectionFill = source.CellColors.Selected.Background ?? "#0078D433",
                MultiSelectionBackground = source.RowColors.Selected.Background ?? "#CCE5FF",
                MultiSelectionForeground = source.RowColors.Selected.Foreground ?? "#000000"
            },

            // Map border colors from grid/cell borders
            BorderColors = new PublicBorderColors
            {
                CellBorder = source.CellColors.Normal.Border ?? "#E0E0E0",
                RowBorder = source.RowColors.Normal.Border ?? "#E0E0E0",
                ColumnBorder = source.HeaderColors.Normal.Border ?? "#E0E0E0",
                GridBorder = source.GridColors.Normal.Border ?? "#CCCCCC",
                FocusedCellBorder = source.CellColors.Focused.Border ?? "#0078D4"
            }
        };
    }
}

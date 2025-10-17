using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

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
    /// Event fired when the theme changes.
    /// UI elements should subscribe to this to refresh their appearance.
    /// </summary>
    public event EventHandler? ThemeChanged;

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
                ThemeChanged?.Invoke(this, EventArgs.Empty);
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
    /// Parses hex color string to SolidColorBrush
    /// Supports formats: #RGB, #RRGGBB, #AARRGGBB
    /// </summary>
    private SolidColorBrush ParseColor(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || !hexColor.StartsWith("#"))
        {
            return new SolidColorBrush(Colors.Transparent);
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

            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }
        catch
        {
            // Fallback to transparent if parsing fails
            return new SolidColorBrush(Colors.Transparent);
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

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}

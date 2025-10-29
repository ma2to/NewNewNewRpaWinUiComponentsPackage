using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls.Converters;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Custom control for rendering special column cells (RowNumber, Checkbox, ValidationAlerts, DeleteRow)
/// Each special column type has its own visual representation and interaction behavior
/// </summary>
internal sealed class SpecialColumnCellControl : UserControl
{
    private readonly CellViewModel _viewModel;
    private readonly ILogger<SpecialColumnCellControl>? _logger;

    // DEBOUNCE FIX: Prevent rapid-fire delete clicks
    private DateTime _lastDeleteClick = DateTime.MinValue;
    private const int DELETE_DEBOUNCE_MS = 300; // 300ms debounce

    // DEBOUNCE FIX: Prevent rapid-fire insert clicks
    private DateTime _lastInsertClick = DateTime.MinValue;
    private const int INSERT_DEBOUNCE_MS = 300; // 300ms debounce

    /// <summary>
    /// Event fired when row selection changes via checkbox (rowIndex, isSelected)
    /// </summary>
    public event Action<int, bool>? OnRowSelectionChanged;

    /// <summary>
    /// Event fired when delete row button is clicked (contains both rowIndex and rowId)
    /// </summary>
    public event EventHandler<DeleteRowRequestedEventArgs>? OnDeleteRowRequested;

    /// <summary>
    /// Event fired when insert row button is clicked (contains both rowIndex and rowId)
    /// </summary>
    public event EventHandler<InsertRowRequestedEventArgs>? OnInsertRowRequested;

    public SpecialColumnCellControl(CellViewModel viewModel, ILogger<SpecialColumnCellControl>? logger = null)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger;

        _logger?.LogTrace("SpecialColumnCellControl created for type {SpecialType}, RowIndex={RowIndex}, ColumnName={ColumnName}",
            _viewModel.SpecialType, _viewModel.RowIndex, _viewModel.ColumnName);

        BuildControl();

        _logger?.LogTrace("SpecialColumnCellControl BuildControl completed, Content type={ContentType}",
            Content?.GetType().Name ?? "null");
    }

    private void BuildControl()
    {
        _logger?.LogInformation("BUILD CONTROL: SpecialType={SpecialType}, RowIndex={RowIndex}, OldContent={OldContentType}",
            _viewModel.SpecialType, _viewModel.RowIndex, Content?.GetType().Name ?? "null");

        // CRITICAL: Force clear old content before creating new (prevents UI virtualization recycling issues)
        Content = null;

        Content = _viewModel.SpecialType switch
        {
            SpecialColumnType.RowNumber => CreateRowNumberControl(),
            SpecialColumnType.Checkbox => CreateCheckboxControl(),
            SpecialColumnType.ValidationAlerts => CreateValidationAlertsControl(),
            SpecialColumnType.DeleteRow => CreateDeleteRowControl(),
            SpecialColumnType.InsertRow => CreateInsertRowControl(),
            _ => new TextBlock { Text = "?", HorizontalAlignment = HorizontalAlignment.Center }
        };

        _logger?.LogInformation("BUILD CONTROL DONE: Created {ContentType} for {SpecialType}",
            Content?.GetType().Name ?? "null", _viewModel.SpecialType);
    }

    #region RowNumber Column

    /// <summary>
    /// Creates read-only row number display (centered, gray text, light background)
    /// </summary>
    private UIElement CreateRowNumberControl()
    {
        var textBlock = new TextBlock
        {
            Text = _viewModel.DisplayRowNumber.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(1),
            Foreground = _viewModel.Theme?.RowNumberForeground ?? Features.Optimization.BrushPool.GetBrush(Colors.Gray),
            FontSize = 12,
            IsTextSelectionEnabled = false
        };

        var border = new Border
        {
            Child = textBlock,
            Background = _viewModel.Theme?.RowNumberBackground ?? Features.Optimization.BrushPool.GetBrush(Color.FromArgb(20, 128, 128, 128)),
            BorderBrush = _viewModel.Theme?.CellBorder ?? Features.Optimization.BrushPool.GetBrush(Colors.LightGray),
            BorderThickness = new Thickness(1, 1, 0, 1), // ✅ FIX: Right=0 (ResizeGripControl adds 12px spacing between columns)
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(1)
        };

        return border;
    }

    #endregion

    #region Checkbox Column

    /// <summary>
    /// Creates checkbox for row selection (fires OnRowSelectionChanged event)
    /// CRITICAL FIX: Uses TwoWay binding to keep checkbox synchronized with ViewModel
    /// PROFESSIONAL SOLUTION: Custom ControlTemplate for complete visibility control
    /// </summary>
    private UIElement CreateCheckboxControl()
    {
        // Get checkbox styling from Options (with defaults)
        var options = _viewModel.Theme?.Options ?? new AdvancedDataGridOptions();

        // Parse hex colors to WinUI colors
        var borderColor = ParseHexColor(options.CheckboxBorderColor, Colors.DimGray);
        var backgroundColor = ParseHexColor(options.CheckboxBackgroundColor, Colors.White);

        // ✅ PROFESSIONAL SOLUTION: Direct Grid-based checkbox implementation
        // WinUI 3 doesn't support FrameworkElementFactory (that's WPF), so we build it directly
        var checkboxGrid = new Grid
        {
            Width = 16,
            Height = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Checkbox box border
        var checkboxBorder = new Border
        {
            Width = 16,
            Height = 16,
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(borderColor),
            Background = new SolidColorBrush(backgroundColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Checkmark icon (hidden by default)
        var checkmarkIcon = new FontIcon
        {
            Glyph = "\uE73E", // Checkmark glyph
            FontSize = 10,
            Foreground = new SolidColorBrush(borderColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = _viewModel.IsRowSelected ? Visibility.Visible : Visibility.Collapsed
        };

        checkboxBorder.Child = checkmarkIcon;
        checkboxGrid.Children.Add(checkboxBorder);

        // Make it interactive - handle Tapped event
        checkboxGrid.IsTapEnabled = true;
        checkboxGrid.Tapped += (s, e) =>
        {
            // Toggle selection
            _viewModel.IsRowSelected = !_viewModel.IsRowSelected;

            // Update visual state
            checkmarkIcon.Visibility = _viewModel.IsRowSelected ? Visibility.Visible : Visibility.Collapsed;

            // Fire events
            OnRowSelectionChanged?.Invoke(_viewModel.RowIndex, _viewModel.IsRowSelected);

            e.Handled = true;
        };

        // Subscribe to ViewModel property changes to update visual state
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CellViewModel.IsRowSelected))
            {
                checkmarkIcon.Visibility = _viewModel.IsRowSelected ? Visibility.Visible : Visibility.Collapsed;
            }
        };

        var border = new Border
        {
            Child = checkboxGrid,
            Background = _viewModel.Theme?.CellDefaultBackground ?? new SolidColorBrush(Colors.White),
            BorderBrush = _viewModel.Theme?.CellBorder ?? new SolidColorBrush(Colors.LightGray),
            BorderThickness = new Thickness(1, 1, 0, 1), // ✅ FIX: Right=0 (ResizeGripControl adds 12px spacing between columns)
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            // ✅ FIX: Increased padding from 2px to 6px to properly center 16px checkbox in row
            // QUALITY: Checkbox 16px + Border padding 12px (6+6) + Border thickness 2px (1+1) = 30px total (fits in ~32px row)
            Padding = new Thickness(6)
        };

        _logger?.LogInformation("Checkbox created for RowIndex={RowIndex}, Size={Width}x{Height}",
            _viewModel.RowIndex, checkboxGrid.Width, checkboxGrid.Height);

        return border;
    }

    #endregion

    #region ValidationAlerts Column

    /// <summary>
    /// Creates validation alerts display (red text if has alert, tooltip with full message).
    /// PROFESSIONAL IMPLEMENTATION: Uses WinUI value converters and data binding for reactive updates.
    /// </summary>
    private UIElement CreateValidationAlertsControl()
    {
        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(4),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        };

        // PROFESSIONAL: Bind Text property to ValidationAlertMessage for automatic updates
        var textBinding = new Microsoft.UI.Xaml.Data.Binding
        {
            Source = _viewModel,
            Path = new PropertyPath(nameof(CellViewModel.ValidationAlertMessage)),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay,
            TargetNullValue = ""
        };
        textBlock.SetBinding(TextBlock.TextProperty, textBinding);

        // PROFESSIONAL: Use value converter for Foreground color based on HasValidationAlert
        var foregroundBinding = new Microsoft.UI.Xaml.Data.Binding
        {
            Source = _viewModel,
            Path = new PropertyPath(nameof(CellViewModel.HasValidationAlert)),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay,
            Converter = new ValidationAlertForegroundConverter(_viewModel.Theme)
        };
        textBlock.SetBinding(TextBlock.ForegroundProperty, foregroundBinding);

        var border = new Border
        {
            Child = textBlock,
            BorderBrush = _viewModel.Theme?.CellBorder ?? new SolidColorBrush(Colors.LightGray),
            BorderThickness = new Thickness(1, 1, 0, 1), // ✅ FIX: Right=0 (ResizeGripControl adds 12px spacing between columns)
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(1)
        };

        // PROFESSIONAL: Use value converter for Background color based on HasValidationAlert
        var backgroundBinding = new Microsoft.UI.Xaml.Data.Binding
        {
            Source = _viewModel,
            Path = new PropertyPath(nameof(CellViewModel.HasValidationAlert)),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay,
            Converter = new ValidationAlertBackgroundConverter(_viewModel.Theme)
        };
        border.SetBinding(Border.BackgroundProperty, backgroundBinding);

        // PROFESSIONAL: Bind tooltip to ValidationAlertMessage with automatic updates
        var tooltip = new ToolTip();
        var tooltipBinding = new Microsoft.UI.Xaml.Data.Binding
        {
            Source = _viewModel,
            Path = new PropertyPath(nameof(CellViewModel.ValidationAlertMessage)),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay
        };
        tooltip.SetBinding(ToolTip.ContentProperty, tooltipBinding);
        ToolTipService.SetToolTip(border, tooltip);

        return border;
    }

    #endregion

    #region DeleteRow Column

    /// <summary>
    /// Creates delete button (fires OnDeleteRowRequested event)
    /// </summary>
    private UIElement CreateDeleteRowControl()
    {
        // CRITICAL FIX: Use SymbolIcon instead of emoji for better rendering
        var icon = new SymbolIcon(Symbol.Delete)
        {
            Foreground = _viewModel.Theme?.DeleteRowForeground ?? Features.Optimization.BrushPool.GetBrush(Colors.DarkRed)
        };

        var button = new Button
        {
            Content = icon,
            Padding = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 0,
            MinHeight = 0,
            Background = _viewModel.Theme?.DeleteRowBackground ?? Features.Optimization.BrushPool.GetBrush(Colors.Transparent)
        };

        // Event: delete button clicked
        // DEBOUNCE FIX: Prevent rapid-fire delete clicks causing row count restoration bug
        button.Click += (s, e) =>
        {
            var now = DateTime.Now;
            if ((now - _lastDeleteClick).TotalMilliseconds < DELETE_DEBOUNCE_MS)
            {
                return; // Ignore rapid clicks within 300ms window
            }
            _lastDeleteClick = now;

            OnDeleteRowRequested?.Invoke(this, new DeleteRowRequestedEventArgs(_viewModel.RowIndex, _viewModel.RowId));
        };

        var border = new Border
        {
            Child = button,
            Background = _viewModel.Theme?.CellDefaultBackground ?? new SolidColorBrush(Colors.White),
            BorderBrush = _viewModel.Theme?.CellBorder ?? new SolidColorBrush(Colors.LightGray),
            BorderThickness = new Thickness(1, 1, 0, 1), // ✅ FIX: Right=0 (ResizeGripControl adds 12px spacing between columns)
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(1)
        };

        _logger?.LogInformation("DeleteRow button created for RowIndex={RowIndex} with SymbolIcon (Symbol.Delete)",
            _viewModel.RowIndex);

        return border;
    }

    #endregion

    #region InsertRow Column

    /// <summary>
    /// Creates insert row button (fires OnInsertRowRequested event)
    /// Similar to delete button but with + icon
    /// </summary>
    private UIElement CreateInsertRowControl()
    {
        var button = new Button
        {
            Content = "+", // Plus icon for insert
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Padding = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 24,
            MinHeight = 24,
            Background = _viewModel.Theme?.InsertRowBackground ?? Features.Optimization.BrushPool.GetBrush(Color.FromArgb(255, 200, 230, 200)),
            Foreground = _viewModel.Theme?.InsertRowForeground ?? Features.Optimization.BrushPool.GetBrush(Colors.DarkGreen),
            BorderBrush = _viewModel.Theme?.InsertRowBorder ?? Features.Optimization.BrushPool.GetBrush(Colors.Green),
            BorderThickness = new Thickness(1),
            // ✅ FIX: Disable built-in visual state transitions pre Hover (zabráni collision)
            UseSystemFocusVisuals = false
        };

        // ✅ FIX: Použiť ResourceDictionary pre hover colors (WinUI-friendly approach)
        // Namiesto PointerEntered/Exited handlers, definujeme custom VisualState resources
        var hoverBg = _viewModel.Theme?.InsertRowHoverBackground ?? Features.Optimization.BrushPool.GetBrush(Color.FromArgb(255, 150, 220, 150));
        var hoverFg = _viewModel.Theme?.InsertRowHoverForeground ?? Features.Optimization.BrushPool.GetBrush(Colors.White);

        // Použiť ControlTemplate resource keys pre custom VisualStates
        button.Resources["ButtonBackgroundPointerOver"] = hoverBg;
        button.Resources["ButtonForegroundPointerOver"] = hoverFg;

        // Event: insert button clicked
        // DEBOUNCE FIX: Prevent rapid-fire insert clicks
        button.Click += (s, e) =>
        {
            var now = DateTime.Now;
            if ((now - _lastInsertClick).TotalMilliseconds < INSERT_DEBOUNCE_MS)
            {
                return; // Ignore rapid clicks within 300ms window
            }
            _lastInsertClick = now;

            // ✅ DEBUGGING: Log RowId to diagnose insert position issues
            _logger?.LogInformation("INSERT BUTTON CLICKED: RowIndex={RowIndex}, RowId={RowId}, ColumnName={ColumnName}",
                _viewModel.RowIndex, _viewModel.RowId ?? "(NULL)", _viewModel.ColumnName);

            OnInsertRowRequested?.Invoke(this, new InsertRowRequestedEventArgs(_viewModel.RowIndex, _viewModel.RowId));
        };

        var border = new Border
        {
            Child = button,
            Background = _viewModel.Theme?.CellDefaultBackground ?? new SolidColorBrush(Colors.White),
            BorderBrush = _viewModel.Theme?.CellBorder ?? new SolidColorBrush(Colors.LightGray),
            BorderThickness = new Thickness(1, 1, 0, 1), // ✅ FIX: Right=0 (ResizeGripControl adds 12px spacing between columns)
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(1)
        };

        return border;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Parses hex color string to WinUI Color
    /// Supports formats: #RGB, #RRGGBB, #AARRGGBB
    /// </summary>
    private static Color ParseHexColor(string hexColor, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || !hexColor.StartsWith("#"))
        {
            return fallback;
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

            return Color.FromArgb(a, r, g, b);
        }
        catch
        {
            return fallback;
        }
    }

    #endregion
}

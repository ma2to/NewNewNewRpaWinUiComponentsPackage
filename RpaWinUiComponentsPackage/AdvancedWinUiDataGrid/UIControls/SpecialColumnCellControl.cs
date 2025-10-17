using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls.Converters;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Custom control for rendering special column cells (RowNumber, Checkbox, ValidationAlerts, DeleteRow)
/// Each special column type has its own visual representation and interaction behavior
/// </summary>
internal sealed class SpecialColumnCellControl : UserControl
{
    private readonly CellViewModel _viewModel;

    // DEBOUNCE FIX: Prevent rapid-fire delete clicks
    private DateTime _lastDeleteClick = DateTime.MinValue;
    private const int DELETE_DEBOUNCE_MS = 300; // 300ms debounce

    /// <summary>
    /// Event fired when row selection changes via checkbox (rowIndex, isSelected)
    /// </summary>
    public event Action<int, bool>? OnRowSelectionChanged;

    /// <summary>
    /// Event fired when delete row button is clicked (contains both rowIndex and rowId)
    /// </summary>
    public event EventHandler<DeleteRowRequestedEventArgs>? OnDeleteRowRequested;

    public SpecialColumnCellControl(CellViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BuildControl();
    }

    private void BuildControl()
    {
        Content = _viewModel.SpecialType switch
        {
            SpecialColumnType.RowNumber => CreateRowNumberControl(),
            SpecialColumnType.Checkbox => CreateCheckboxControl(),
            SpecialColumnType.ValidationAlerts => CreateValidationAlertsControl(),
            SpecialColumnType.DeleteRow => CreateDeleteRowControl(),
            _ => new TextBlock { Text = "?", HorizontalAlignment = HorizontalAlignment.Center }
        };
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
            Foreground = new SolidColorBrush(Colors.Gray),
            FontSize = 12,
            IsTextSelectionEnabled = false
        };

        var border = new Border
        {
            Child = textBlock,
            Background = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)), // Light gray bg
            BorderBrush = _viewModel.Theme?.CellBorder ?? new SolidColorBrush(Colors.LightGray),
            BorderThickness = new Thickness(0, 0, 1, 1),
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
    /// VISIBILITY FIX: Sets Foreground to make unchecked checkbox visible
    /// </summary>
    private UIElement CreateCheckboxControl()
    {
        // Get checkbox styling from Options (with defaults)
        var options = _viewModel.Theme?.Options ?? new AdvancedDataGridOptions();

        // Parse hex colors to WinUI colors
        var borderColor = ParseHexColor(options.CheckboxBorderColor, Colors.DimGray);
        var backgroundColor = ParseHexColor(options.CheckboxBackgroundColor, Colors.White);

        var checkbox = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = options.CheckboxMinWidth,
            MinHeight = options.CheckboxMinHeight,
            BorderBrush = new SolidColorBrush(borderColor),
            BorderThickness = new Thickness(options.CheckboxBorderThickness),
            Background = new SolidColorBrush(backgroundColor),
            Foreground = new SolidColorBrush(borderColor), // VISIBILITY FIX: Make unchecked box visible
            Padding = new Thickness(0),
            Visibility = Visibility.Visible
        };

        // CRITICAL FIX: Use TwoWay binding instead of event handlers
        // This ensures checkbox stays synchronized when ViewModel changes (e.g., from header SelectAll)
        var binding = new Microsoft.UI.Xaml.Data.Binding
        {
            Source = _viewModel,
            Path = new PropertyPath(nameof(CellViewModel.IsRowSelected)),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
            UpdateSourceTrigger = Microsoft.UI.Xaml.Data.UpdateSourceTrigger.PropertyChanged
        };
        checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);

        // Keep event handlers for notification to parent components
        checkbox.Checked += (s, e) =>
        {
            OnRowSelectionChanged?.Invoke(_viewModel.RowIndex, true);
        };

        checkbox.Unchecked += (s, e) =>
        {
            OnRowSelectionChanged?.Invoke(_viewModel.RowIndex, false);
        };

        var border = new Border
        {
            Child = checkbox,
            Background = _viewModel.Theme?.CellDefaultBackground ?? new SolidColorBrush(Colors.White),
            BorderBrush = _viewModel.Theme?.CellBorder ?? new SolidColorBrush(Colors.LightGray),
            BorderThickness = new Thickness(0, 0, 1, 1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(1)
        };

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
            BorderThickness = new Thickness(0, 0, 1, 1),
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
        var button = new Button
        {
            Content = "🗑", // Trash icon
            FontSize = 14,
            Padding = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 0,
            MinHeight = 0
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
            BorderThickness = new Thickness(0, 0, 1, 1),
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

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Custom control for rendering special column cells (RowNumber, Checkbox, ValidationAlerts, DeleteRow)
/// Each special column type has its own visual representation and interaction behavior
/// </summary>
internal sealed class SpecialColumnCellControl : UserControl
{
    private readonly CellViewModel _viewModel;

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
    /// </summary>
    private UIElement CreateCheckboxControl()
    {
        var checkbox = new CheckBox
        {
            IsChecked = _viewModel.IsRowSelected,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 0,
            Padding = new Thickness(0),
            Visibility = Visibility.Visible // Vždy viditeľný, aj keď nie je checked
        };

        // Event: checkbox checked/unchecked
        checkbox.Checked += (s, e) =>
        {
            _viewModel.IsRowSelected = true;
            OnRowSelectionChanged?.Invoke(_viewModel.RowIndex, true);
        };

        checkbox.Unchecked += (s, e) =>
        {
            _viewModel.IsRowSelected = false;
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
    /// Creates validation alerts display (red text if has alert, tooltip with full message)
    /// </summary>
    private UIElement CreateValidationAlertsControl()
    {
        var textBlock = new TextBlock
        {
            Text = _viewModel.ValidationAlertMessage ?? "",
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(4),
            FontSize = 11,
            Foreground = _viewModel.HasValidationAlert
                ? new SolidColorBrush(Colors.Red)
                : (_viewModel.Theme?.CellDefaultForeground ?? new SolidColorBrush(Colors.Black)),
            VerticalAlignment = VerticalAlignment.Center
        };

        var border = new Border
        {
            Child = textBlock,
            Background = _viewModel.HasValidationAlert
                ? new SolidColorBrush(Color.FromArgb(30, 255, 0, 0)) // Light red bg
                : (_viewModel.Theme?.CellDefaultBackground ?? new SolidColorBrush(Colors.White)),
            BorderBrush = _viewModel.Theme?.CellBorder ?? new SolidColorBrush(Colors.LightGray),
            BorderThickness = new Thickness(0, 0, 1, 1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(1)
        };

        // Tooltip for full message
        if (_viewModel.HasValidationAlert)
        {
            ToolTipService.SetToolTip(border, new ToolTip
            {
                Content = _viewModel.ValidationAlertMessage
            });
        }

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
        button.Click += (s, e) =>
        {
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
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Single cell control with selection, editing, and validation support.
/// Displays cell value as read-only text normally, switches to editable text box on double-click.
/// Automatically reflects cell state (selected, validation error, search match) through visual styling.
/// Built programmatically without XAML for maximum flexibility.
/// </summary>
public sealed class CellControl : UserControl
{
    /// <summary>
    /// Gets the view model that manages this cell's data and state.
    /// </summary>
    public CellViewModel ViewModel { get; }

    /// <summary>
    /// Fired when this cell is selected (single-click or Ctrl+click).
    /// </summary>
    public event EventHandler<CellSelectionEventArgs>? CellSelected;

    /// <summary>
    /// Fired when the user starts editing this cell (double-click).
    /// </summary>
    public event EventHandler<CellViewModel>? CellEditStarted;

    /// <summary>
    /// Fired when the user finishes editing this cell (Enter key or focus lost).
    /// </summary>
    public event EventHandler<CellViewModel>? CellEditCompleted;

    /// <summary>
    /// Fired when the mouse pointer enters this cell (used for drag selection).
    /// </summary>
    public event EventHandler<CellViewModel>? CellPointerEntered;

    /// <summary>
    /// Fired when the cell value changes during editing (real-time as user types).
    /// </summary>
    public event EventHandler<CellValueChangedEventArgs>? CellValueChanged;

    private readonly Border _rootBorder;
    private readonly Grid _rootGrid;
    private readonly TextBlock _displayTextBlock;
    private readonly TextBox _editTextBox;

    /// <summary>
    /// Creates a new cell control bound to the specified view model.
    /// </summary>
    /// <param name="viewModel">The view model that manages this cell's data and state</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public CellControl(CellViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Create UI programmatically
        _rootBorder = new Border
        {
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // Bind border properties to ViewModel
        var borderBrushBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.BorderBrush)),
            Mode = BindingMode.OneWay
        };
        _rootBorder.SetBinding(Border.BorderBrushProperty, borderBrushBinding);

        var backgroundBrushBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.BackgroundBrush)),
            Mode = BindingMode.OneWay
        };
        _rootBorder.SetBinding(Border.BackgroundProperty, backgroundBrushBinding);

        // Create Grid to hold display and edit controls
        _rootGrid = new Grid();

        // Display TextBlock (visible when not editing)
        _displayTextBlock = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        var displayTextBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.Value)),
            Mode = BindingMode.OneWay
        };
        _displayTextBlock.SetBinding(TextBlock.TextProperty, displayTextBinding);

        var foregroundBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.ForegroundBrush)),
            Mode = BindingMode.OneWay
        };
        _displayTextBlock.SetBinding(TextBlock.ForegroundProperty, foregroundBinding);

        var displayVisibilityBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.IsEditing)),
            Mode = BindingMode.OneWay,
            Converter = new InverseBoolToVisibilityConverter()
        };
        _displayTextBlock.SetBinding(UIElement.VisibilityProperty, displayVisibilityBinding);

        // Edit TextBox (visible when editing)
        _editTextBox = new TextBox
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        var editTextBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.Value)),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        _editTextBox.SetBinding(TextBox.TextProperty, editTextBinding);

        var editVisibilityBinding = new Binding
        {
            Source = ViewModel,
            Path = new PropertyPath(nameof(ViewModel.IsEditing)),
            Mode = BindingMode.OneWay,
            Converter = new BoolToVisibilityConverter()
        };
        _editTextBox.SetBinding(UIElement.VisibilityProperty, editVisibilityBinding);

        _editTextBox.LostFocus += OnEditTextBoxLostFocus;
        _editTextBox.KeyDown += OnEditTextBoxKeyDown;
        _editTextBox.TextChanged += OnEditTextBoxTextChanged;

        // Add controls to Grid
        _rootGrid.Children.Add(_displayTextBlock);
        _rootGrid.Children.Add(_editTextBox);

        // Set Grid as Border child
        _rootBorder.Child = _rootGrid;

        // Event handlers for interaction
        _rootBorder.Tapped += OnCellTapped;
        _rootBorder.DoubleTapped += OnCellDoubleTapped;
        _rootBorder.PointerEntered += OnPointerEntered;

        // Set Border as UserControl content
        Content = _rootBorder;
    }

    private void OnCellTapped(object sender, TappedRoutedEventArgs e)
    {
        // Check if Ctrl key is pressed
        var isCtrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        // Fire selection event with Ctrl state
        CellSelected?.Invoke(this, new CellSelectionEventArgs
        {
            Cell = ViewModel,
            IsCtrlPressed = isCtrlPressed
        });
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Fire pointer entered event for range selection
        CellPointerEntered?.Invoke(this, ViewModel);
    }

    private void OnCellDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // Double tap - enter edit mode
        ViewModel.IsEditing = true;
        CellEditStarted?.Invoke(this, ViewModel);

        // Focus the edit TextBox
        _editTextBox.Focus(FocusState.Programmatic);
        _editTextBox.SelectAll();
    }

    private void OnEditTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        // Exit edit mode when focus lost
        if (ViewModel.IsEditing)
        {
            ViewModel.IsEditing = false;
            CellEditCompleted?.Invoke(this, ViewModel);
        }
    }

    private void OnEditTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            // Enter key - complete edit
            ViewModel.IsEditing = false;
            CellEditCompleted?.Invoke(this, ViewModel);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            // Escape key - cancel edit (restore original value would go here)
            ViewModel.IsEditing = false;
            e.Handled = true;
        }
    }

    private void OnEditTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        // Real-time validation during edit
        if (ViewModel.IsEditing && sender is TextBox textBox)
        {
            CellValueChanged?.Invoke(this, new CellValueChangedEventArgs
            {
                Cell = ViewModel,
                OldValue = ViewModel.Value,
                NewValue = textBox.Text
            });
        }
    }
}

/// <summary>
/// Converter that converts boolean values to Visibility (true becomes Visible, false becomes Collapsed).
/// Used for showing/hiding UI elements based on boolean properties.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to Visibility.
    /// </summary>
    /// <param name="value">Boolean value to convert</param>
    /// <param name="targetType">Target type (not used)</param>
    /// <param name="parameter">Converter parameter (not used)</param>
    /// <param name="language">Language (not used)</param>
    /// <returns>Visibility.Visible if true, Visibility.Collapsed if false</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue && boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Converts Visibility back to boolean.
    /// </summary>
    /// <param name="value">Visibility value to convert</param>
    /// <param name="targetType">Target type (not used)</param>
    /// <param name="parameter">Converter parameter (not used)</param>
    /// <param name="language">Language (not used)</param>
    /// <returns>True if Visible, false otherwise</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility visibility && visibility == Visibility.Visible;
    }
}

/// <summary>
/// Converter that converts boolean values to inverse Visibility (true becomes Collapsed, false becomes Visible).
/// Used for showing UI elements when a boolean property is false.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to inverse Visibility.
    /// </summary>
    /// <param name="value">Boolean value to convert</param>
    /// <param name="targetType">Target type (not used)</param>
    /// <param name="parameter">Converter parameter (not used)</param>
    /// <param name="language">Language (not used)</param>
    /// <returns>Visibility.Collapsed if true, Visibility.Visible if false</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool boolValue && boolValue ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Converts Visibility back to inverse boolean.
    /// </summary>
    /// <param name="value">Visibility value to convert</param>
    /// <param name="targetType">Target type (not used)</param>
    /// <param name="parameter">Converter parameter (not used)</param>
    /// <param name="language">Language (not used)</param>
    /// <returns>True if Collapsed, false otherwise</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility visibility && visibility == Visibility.Collapsed;
    }
}

/// <summary>
/// Event arguments for cell selection events, including information about modifier keys.
/// </summary>
public class CellSelectionEventArgs : EventArgs
{
    /// <summary>
    /// Gets the cell that was selected.
    /// </summary>
    public CellViewModel Cell { get; init; } = null!;

    /// <summary>
    /// Gets whether the Ctrl key was pressed during selection (for multi-select).
    /// </summary>
    public bool IsCtrlPressed { get; init; }
}

/// <summary>
/// Event arguments for cell value change events during editing.
/// </summary>
public class CellValueChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the cell whose value changed.
    /// </summary>
    public CellViewModel Cell { get; init; } = null!;

    /// <summary>
    /// Gets the old value before the change.
    /// </summary>
    public object? OldValue { get; init; }

    /// <summary>
    /// Gets the new value after the change.
    /// </summary>
    public object? NewValue { get; init; }
}

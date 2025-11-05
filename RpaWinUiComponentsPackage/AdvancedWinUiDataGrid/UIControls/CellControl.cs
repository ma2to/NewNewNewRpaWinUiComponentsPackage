using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

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
    public event EventHandler<CellPointerEnteredEventArgs>? CellPointerEntered;

    /// <summary>
    /// Fired when the cell value changes during editing (real-time as user types).
    /// </summary>
    public event EventHandler<CellValueChangedEventArgs>? CellValueChanged;

    /// <summary>
    /// Fired when the user requests navigation to another cell via arrow keys in NORMAL mode.
    /// </summary>
    public event EventHandler<NavigationDirection>? NavigationRequested;

    private readonly Border _rootBorder;
    private readonly Grid _rootGrid;
    private readonly TextBlock _displayTextBlock;
    private readonly TextBox _editTextBox;
    private readonly ILogger<CellControl>? _logger;
    private object? _originalValue; // Store original value before editing for cancel support

    /// <summary>
    /// Creates a new cell control bound to the specified view model.
    /// </summary>
    /// <param name="viewModel">The view model that manages this cell's data and state</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public CellControl(CellViewModel viewModel, ILogger<CellControl>? logger = null)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger;

        // Create UI programmatically
        _rootBorder = new Border
        {
            BorderThickness = new Thickness(1, 1, 0, 1), // ✅ FIX: Right=0 (ResizeGripControl adds 12px spacing between columns)
            Padding = new Thickness(1), // 1px padding
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
            VerticalAlignment = VerticalAlignment.Center,
            AcceptsReturn = false,          // ✅ CRITICAL FIX: Disable auto-newline (prevent Enter from inserting \n before handler)
            TextWrapping = TextWrapping.Wrap // ✅ CRITICAL FIX: Wrap long text for visibility
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

        // ✅ CRITICAL FIX: Block right-click during edit mode
        // USER REQUIREMENT: Right-click v edit mode by NEMAL ROBIŤ NIČ
        // REASON 1: User explicitly requested no action on right-click in edit mode
        // REASON 2: Zabráni zobrazeniu default TextBox context menu (Cut/Copy/Paste)
        // REASON 3: Zabráni COMException z missing WinUI theme resources
        _editTextBox.RightTapped += OnEditTextBoxRightTapped;
        _editTextBox.PointerPressed += OnEditTextBoxPointerPressed;

        // Add controls to Grid
        _rootGrid.Children.Add(_displayTextBlock);
        _rootGrid.Children.Add(_editTextBox);

        // Set Grid as Border child
        _rootBorder.Child = _rootGrid;

        // Event handlers for interaction
        _rootBorder.PointerPressed += OnCellPointerPressed;
        _rootBorder.DoubleTapped += OnCellDoubleTapped;
        _rootBorder.PointerEntered += OnPointerEntered;

        // Enable keyboard navigation (Tab/Shift+Tab)
        _rootBorder.IsTabStop = true;
        _rootBorder.KeyDown += OnCellKeyDown;

        // ✅ PROFESSIONAL FIX: Subscribe to ViewModel property changes for FocusRequested handling
        // ARCHITECTURE: Arrow key navigation sets FocusRequested=true → this handler applies focus
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Set Border as UserControl content
        Content = _rootBorder;
    }

    private void OnCellPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // ✅ PROBLEM 3 FIX: Detect right-click and skip selection modification
        // Right-click should show context menu WITHOUT changing current selection state
        // This preserves multi-cell selection when user right-clicks to access context menu
        var pointerPoint = e.GetCurrentPoint(this);
        if (pointerPoint.Properties.IsRightButtonPressed)
        {
            _logger?.LogTrace("CellControl[{Row},{Col}]: Right-click detected, preserving selection state",
                ViewModel.RowIndex, ViewModel.ColumnIndex);
            // Right-click detected - do NOT modify selection state
            // Let RightTapped handler show context menu without changing selection
            return;
        }

        // ✅ CRITICAL FIX: Set keyboard focus to _rootBorder IMMEDIATELY after click
        // REASON: Without explicit Focus(), keyboard focus remains on ScrollViewer or parent
        //         ScrollViewer arrow keys = scroll, CellControl arrow keys = navigation
        // ARCHITECTURE:
        //   - Click = select cell + focus cell for keyboard navigation
        //   - FocusState.Pointer = appropriate for mouse/touch input
        // USER REQUIREMENT:
        //   - Tab/Arrow keys must work IMMEDIATELY after click (before first edit)
        //   - After edit, focus is already set (_editTextBox.Focus), so this maintains consistency
        // PROBLEM SOLVED: Tab/Arrow keys now work correctly before first edit (navigation instead of scroll)
        _rootBorder.Focus(FocusState.Pointer);

        _logger?.LogTrace("CellControl[{Row},{Col}]: Keyboard focus set to _rootBorder after click",
            ViewModel.RowIndex, ViewModel.ColumnIndex);

        // Check if Ctrl key is pressed
        var isCtrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        _logger?.LogTrace("CellControl[{Row},{Col}]: PointerPressed, LeftButton={Left}, Ctrl={Ctrl}",
            ViewModel.RowIndex, ViewModel.ColumnIndex,
            pointerPoint.Properties.IsLeftButtonPressed, isCtrlPressed);

        // Fire selection event with Ctrl state and pointer args (for drag selection capture)
        CellSelected?.Invoke(this, new CellSelectionEventArgs
        {
            Cell = ViewModel,
            IsCtrlPressed = isCtrlPressed,
            PointerEventArgs = e  // ✅ CRITICAL FIX: Pass pointer args for capture support
        });

        // ✅ PROFESSIONAL FIX (CHYBA 4): ALLOW event bubbling to ScrollViewer for drag selection
        // REASON: WinUI 3 pointer capture does NOT redirect PointerMoved to capture target (unlike WPF)
        //         PointerMoved is still delivered to visual element under pointer (CellControl)
        //         By NOT handling the event, it bubbles to ScrollViewer → OnScrollViewerPointerMoved fires
        //         This enables smooth drag-and-drop multiselect (click and drag across cells)
        // BEFORE: e.Handled = true → PointerMoved blocked → drag selection doesn't work
        // AFTER: Event bubbles → OnScrollViewerPointerMoved receives events → drag selection works
        // e.Handled = true;  // ❌ COMMENTED OUT - blocks drag selection
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Fire pointer entered event for range selection with pointer args for button state checking
        CellPointerEntered?.Invoke(this, new CellPointerEnteredEventArgs
        {
            Cell = ViewModel,
            PointerEventArgs = e
        });
    }

    /// <summary>
    /// Handles ViewModel property changes for focus management.
    /// CRITICAL: Applies programmatic focus when FocusRequested property is set to true.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CellViewModel.FocusRequested))
        {
            if (ViewModel.FocusRequested)
            {
                _rootBorder.Focus(FocusState.Keyboard);
                ViewModel.FocusRequested = false; // Reset to prevent infinite loops

                _logger?.LogTrace("CellControl[{Row},{Col}]: Focus applied via FocusRequested property",
                    ViewModel.RowIndex, ViewModel.ColumnIndex);
            }
        }
    }

    private void OnCellKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // ✅ MEDIUM FIX: Enter key starts edit mode when cell is selected but not editing
        if (e.Key == Windows.System.VirtualKey.Enter && !ViewModel.IsEditing && ViewModel.IsSelected)
        {
            // Block editing for special columns and read-only cells
            if (ViewModel.IsSpecialColumn || ViewModel.IsReadOnly)
            {
                e.Handled = true;
                return;
            }

            // Start edit mode
            _originalValue = ViewModel.Value;
            ViewModel.IsEditing = true;
            CellEditStarted?.Invoke(this, ViewModel);

            // Focus the edit TextBox
            _editTextBox.Focus(FocusState.Programmatic);
            _editTextBox.SelectAll();

            e.Handled = true;
            return;
        }

        // ✅ PROFESSIONAL FIX: Arrow key navigation in NORMAL mode (when cell is selected but NOT editing)
        // CRITICAL: Only navigate when NOT editing - editing mode blocks arrow keys (see OnEditTextBoxKeyDown)
        // ARCHITECTURE:
        //   - NORMAL MODE: Arrow keys navigate between cells → fires NavigationRequested event
        //   - EDIT MODE: Arrow keys blocked → cursor moves within TextBox
        if (!ViewModel.IsEditing && ViewModel.IsSelected)
        {
            NavigationDirection? direction = e.Key switch
            {
                Windows.System.VirtualKey.Up => NavigationDirection.Up,
                Windows.System.VirtualKey.Down => NavigationDirection.Down,
                Windows.System.VirtualKey.Left => NavigationDirection.Left,
                Windows.System.VirtualKey.Right => NavigationDirection.Right,
                _ => null
            };

            if (direction != null)
            {
                _logger?.LogTrace("CellControl[{Row},{Col}]: Arrow key {Direction} in NORMAL mode - requesting navigation",
                    ViewModel.RowIndex, ViewModel.ColumnIndex, direction.Value);

                NavigationRequested?.Invoke(this, direction.Value);
                e.Handled = true;
                return;
            }
        }

        // ✅ PROFESSIONAL FIX: Tab/Shift+Tab for Excel-like cell navigation
        // ARCHITECTURE: Tab moves right and wraps to next row, Shift+Tab moves left and wraps to previous row
        // REQUIREMENT: Prevent WinUI default Tab behavior (jumping to pages/buttons)
        if (e.Key == Windows.System.VirtualKey.Tab && !ViewModel.IsEditing)
        {
            var isShiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            var direction = isShiftPressed ? NavigationDirection.TabBackward : NavigationDirection.TabForward;

            _logger?.LogTrace("CellControl[{Row},{Col}]: Tab key ({Direction}) in NORMAL mode - requesting navigation",
                ViewModel.RowIndex, ViewModel.ColumnIndex, direction);

            NavigationRequested?.Invoke(this, direction);
            e.Handled = true; // CRITICAL: Block default Tab behavior (prevent jumping to pages)
            return;
        }
    }

    private void OnCellDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // ✅ CRITICAL FIX: Block editing for special columns (Checkbox, RowNumber, ValidationAlerts, DeleteRow, InsertRow)
        // Special columns have their own controls (SpecialColumnCellControl) and should NOT be editable via CellControl
        if (ViewModel.IsSpecialColumn)
        {
            // Ignore double-click on special columns - they handle their own interactions
            e.Handled = true;
            return;
        }

        // ✅ ADDITIONAL FIX: Block editing for read-only cells
        if (ViewModel.IsReadOnly)
        {
            e.Handled = true;
            return;
        }

        // Double tap - enter edit mode
        // Store original value before editing to support cancel
        _originalValue = ViewModel.Value;

        ViewModel.IsEditing = true;
        CellEditStarted?.Invoke(this, ViewModel);

        // Focus the edit TextBox
        _editTextBox.Focus(FocusState.Programmatic);
        _editTextBox.SelectAll();
    }

    private void OnEditTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        // Exit edit mode when focus lost - CANCEL changes (restore original value)
        if (ViewModel.IsEditing)
        {
            _logger?.LogTrace("CellControl[{Row},{Col}]: LostFocus - canceling edit, restoring original value",
                ViewModel.RowIndex, ViewModel.ColumnIndex);

            // Restore original value on focus lost (prepnutie do inej bunky = CANCEL)
            if (_originalValue != null || ViewModel.Value != null)
            {
                ViewModel.Value = _originalValue;
            }
            ViewModel.IsEditing = false;
            CellEditCompleted?.Invoke(this, ViewModel);
            _originalValue = null; // Clear stored value

            // ✅ CRITICAL FIX: DO NOT restore focus here!
            // User clicked another cell - let that cell take focus naturally
            // Only restore focus on explicit Enter/Escape key press (see OnEditTextBoxKeyDown)
            // ViewModel.IsSelected = true;  // REMOVED - conflicts with new cell selection
            // _rootBorder.Focus(FocusState.Programmatic);  // REMOVED - conflicts with new cell click

            _logger?.LogTrace("Edit canceled, focus NOT restored (allowing new cell to take focus)");
        }
    }

    private void OnEditTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // ✅ PROFESSIONAL FIX: Block arrow key navigation during edit mode
        // CRITICAL: User must explicitly commit (Enter) or cancel (Escape) before leaving cell
        // REASON: Arrow keys should NEVER navigate to other cells during edit mode
        // REQUIREMENT: User feedback - prevent accidental cell navigation while editing
        if (e.Key == Windows.System.VirtualKey.Left ||
            e.Key == Windows.System.VirtualKey.Right ||
            e.Key == Windows.System.VirtualKey.Up ||
            e.Key == Windows.System.VirtualKey.Down)
        {
            // Always block navigation during edit mode - require explicit commit/cancel
            // NOTE: Cursor movement within TextBox still works (handled by TextBox internally)
            _logger?.LogTrace("CellControl[{Row},{Col}]: Arrow key {Key} blocked during edit mode - use Enter to commit or Escape to cancel",
                ViewModel.RowIndex, ViewModel.ColumnIndex, e.Key);
            e.Handled = true;
            return;
        }

        // ✅ PROFESSIONAL FIX: Tab key inserts tab character instead of navigating
        // CRITICAL: Prevents focus loss and edit cancellation during multiline text editing
        // REASON: Excel-like behavior - Tab during edit = insert tab, NOT navigate
        // ARCHITECTURE:
        //   - NORMAL MODE (OnCellKeyDown line 225): Tab navigates → e.Handled = FALSE
        //   - EDIT MODE (here): Tab inserts \t → e.Handled = TRUE
        if (e.Key == Windows.System.VirtualKey.Tab)
        {
            _logger?.LogTrace("CellControl[{Row},{Col}]: Tab pressed in edit mode - inserting tab character",
                ViewModel.RowIndex, ViewModel.ColumnIndex);

            // Insert tab character at current cursor position
            var selectionStart = _editTextBox.SelectionStart;
            var currentText = _editTextBox.Text ?? string.Empty;
            var newText = currentText.Insert(selectionStart, "\t");
            _editTextBox.Text = newText;
            _editTextBox.SelectionStart = selectionStart + 1; // Move cursor after tab
            e.Handled = true; // CRITICAL: Prevents event bubbling → no navigation
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            // Check if Shift key is pressed
            var isShiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (isShiftPressed)
            {
                // Shift+Enter: Insert newline without committing edit
                _logger?.LogTrace("CellControl[{Row},{Col}]: Shift+Enter pressed - inserting newline",
                    ViewModel.RowIndex, ViewModel.ColumnIndex);

                // Insert newline at current cursor position
                var selectionStart = _editTextBox.SelectionStart;
                var currentText = _editTextBox.Text ?? string.Empty;
                var newText = currentText.Insert(selectionStart, "\n");
                _editTextBox.Text = newText;
                _editTextBox.SelectionStart = selectionStart + 1; // Move cursor after newline
                e.Handled = true;
                return;
            }

            _logger?.LogTrace("CellControl[{Row},{Col}]: Enter pressed - committing edit",
                ViewModel.RowIndex, ViewModel.ColumnIndex);

            // Enter key (without Shift) - commit edit (value already updated via TwoWay binding)
            ViewModel.IsEditing = false;
            CellEditCompleted?.Invoke(this, ViewModel);
            _originalValue = null; // Clear stored value after commit
            e.Handled = true;

            // ✅ HIGH FIX: Restore selection and focus to cell after Enter
            // This is OK here because user explicitly confirmed edit with Enter
            ViewModel.IsSelected = true;
            _rootBorder.Focus(FocusState.Programmatic);

            _logger?.LogTrace("Edit committed, focus restored to cell");
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            _logger?.LogTrace("CellControl[{Row},{Col}]: Escape pressed - canceling edit",
                ViewModel.RowIndex, ViewModel.ColumnIndex);

            // Escape key - cancel edit and restore original value
            if (_originalValue != null || ViewModel.Value != null)
            {
                ViewModel.Value = _originalValue; // Restore original value
            }
            ViewModel.IsEditing = false;
            _originalValue = null; // Clear stored value after cancel
            e.Handled = true;

            // ✅ HIGH FIX: Restore selection and focus to cell after Escape
            // This is OK here because user explicitly canceled edit with Escape
            ViewModel.IsSelected = true;
            _rootBorder.Focus(FocusState.Programmatic);

            _logger?.LogTrace("Edit canceled, focus restored to cell");
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

    /// <summary>
    /// Handles right-click on edit TextBox - SUPPRESS all actions during edit mode.
    /// USER REQUIREMENT: Pravý klik v edit mode by nemal robiť NIČ.
    /// </summary>
    private void OnEditTextBoxRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // ✅ PROFESSIONAL FIX: Completely suppress right-click during edit mode
        // REASON 1: User requirement - right-click should do NOTHING in edit mode
        // REASON 2: Prevents default TextBox context menu (Cut/Copy/Paste)
        // REASON 3: Prevents COMException from missing WinUI theme resources
        _logger?.LogTrace("CellControl[{Row},{Col}]: Right-click SUPPRESSED during edit mode (no action)",
            ViewModel.RowIndex, ViewModel.ColumnIndex);

        e.Handled = true; // Block event propagation
    }

    /// <summary>
    /// Handles pointer press on edit TextBox - detect and suppress right-click.
    /// This catches right-click BEFORE RightTapped event fires.
    /// </summary>
    private void OnEditTextBoxPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(_editTextBox);
        if (pointerPoint.Properties.IsRightButtonPressed)
        {
            // ✅ CRITICAL: Block right-click at PointerPressed level
            // REASON: Prevents TextBox from processing right-click at all
            //         (more aggressive than RightTapped - blocks earlier in event chain)
            _logger?.LogTrace("CellControl[{Row},{Col}]: Right-click PointerPressed BLOCKED in edit mode",
                ViewModel.RowIndex, ViewModel.ColumnIndex);

            e.Handled = true; // Block immediately
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

    /// <summary>
    /// Gets the pointer event args for pointer capture support (drag selection).
    /// </summary>
    public PointerRoutedEventArgs? PointerEventArgs { get; init; }
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

/// <summary>
/// Event arguments for cell pointer entered events, including pointer state for drag selection.
/// </summary>
public class CellPointerEnteredEventArgs : EventArgs
{
    /// <summary>
    /// Gets the cell that the pointer entered.
    /// </summary>
    public CellViewModel Cell { get; init; } = null!;

    /// <summary>
    /// Gets the pointer event args for checking button state (required for drag selection).
    /// </summary>
    public PointerRoutedEventArgs PointerEventArgs { get; init; } = null!;
}

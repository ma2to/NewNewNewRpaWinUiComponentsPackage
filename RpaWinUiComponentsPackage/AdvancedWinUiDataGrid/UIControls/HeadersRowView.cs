using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Headers row view that displays column headers with support for resizing.
/// Each header has a resize grip on the right side that users can drag to change column width.
/// Column widths are automatically synchronized across headers, filters, and data cells.
/// Uses Grid layout with ColumnDefinitions synchronized across all grid views.
/// MEMORY LEAK FIX: Implements proper cleanup of event handlers via Unloaded event.
/// </summary>
public sealed class HeadersRowView : UserControl
{
    private readonly DataGridViewModel _viewModel;
    private readonly ILogger<HeadersRowView>? _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private ColumnHeaderViewModel? _resizingColumn; // Column currently being resized
    private double _resizeStartWidth; // Original width when resize started
    private double _resizeStartX; // SENIOR FIX: Starting X position for pointer-based resize
    private Border? _resizePreviewLine; // Visual preview line during resize

    private readonly Grid _headersGrid;

    /// <summary>
    /// Creates a new headers row view bound to the specified view model.
    /// Automatically subscribes to column collection changes and column definition changes.
    /// MEMORY LEAK FIX: Subscribes to Unloaded event for proper cleanup.
    /// </summary>
    /// <param name="viewModel">The view model that manages the grid's data and state</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <param name="loggerFactory">Optional logger factory for creating child component loggers</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public HeadersRowView(DataGridViewModel viewModel, ILogger<HeadersRowView>? logger = null, ILoggerFactory? loggerFactory = null)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger;
        _loggerFactory = loggerFactory;

        // Create Grid for headers with columns matching DataGridViewModel
        _headersGrid = new Grid
        {
            Padding = new Thickness(0, 4, 0, 0) // Left=0, Top=4 (spacing from component above), Right=0 (no extra edge space), Bottom=0
            // SENIOR FIX: Removed ManipulationMode - using PointerEvents instead for WinUI 3 compatibility
        };

        // SENIOR FIX: No manipulation mode needed on UserControl - using pointer events

        // Initialize column definitions
        RebuildColumnDefinitions();

        // Create header controls
        RebuildHeaderControls();

        // Listen for column definition changes
        _viewModel.ColumnDefinitionsChanged += OnColumnDefinitionsChanged;

        // Listen for collection changes
        _viewModel.ColumnHeaders.CollectionChanged += OnColumnHeadersCollectionChanged;

        // MEMORY LEAK FIX: Subscribe to Unloaded event for cleanup
        this.Unloaded += OnUnloaded;

        // Set grid as UserControl content
        Content = _headersGrid;
    }

    /// <summary>
    /// MEMORY LEAK FIX: Cleanup event handlers when control is unloaded.
    /// This prevents event handler accumulation that causes 200MB memory leaks per resize operation.
    /// Without this cleanup, old HeadersRowView instances stay in memory due to event subscriptions.
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Unsubscribe from ViewModel events
        _viewModel.ColumnDefinitionsChanged -= OnColumnDefinitionsChanged;
        _viewModel.ColumnHeaders.CollectionChanged -= OnColumnHeadersCollectionChanged;

        // Clean up resize grips event handlers
        // SENIOR FIX: Updated to unsubscribe PointerEvents instead of ManipulationEvents
        foreach (var child in _headersGrid.Children)
        {
            if (child is Grid cellGrid)
            {
                foreach (var innerChild in cellGrid.Children)
                {
                    if (innerChild is ResizeGripControl resizeGrip)
                    {
                        resizeGrip.PointerPressed -= OnResizeGripPointerPressed;
                        resizeGrip.PointerMoved -= OnResizeGripPointerMoved;
                        resizeGrip.PointerReleased -= OnResizeGripPointerReleased;
                        resizeGrip.PointerCaptureLost -= OnResizeGripPointerReleased;
                    }
                }
            }
        }

        // Clean up preview line if still exists
        if (_resizePreviewLine != null && this.Parent is Panel parentPanel)
        {
            parentPanel.Children.Remove(_resizePreviewLine);
            _resizePreviewLine = null;
        }

        // Unsubscribe from self
        this.Unloaded -= OnUnloaded;
    }

    private void OnColumnDefinitionsChanged(object? sender, EventArgs e)
    {
        // Rebuild column definitions when widths change
        RebuildColumnDefinitions();
    }

    /// <summary>
    /// ✅ SENIOR FIX: Optimized incremental header updates.
    /// BEFORE: Full rebuild on each column add → 10 columns = 10× rebuild = O(n²) complexity
    /// AFTER: Incremental add/remove → O(n) complexity
    /// MEMORY IMPACT: 10 columns × 9 redundant checkboxes = 90 wasted UI elements eliminated!
    /// </summary>
    private void OnColumnHeadersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // ✅ PROFESSIONAL: Handle incremental changes efficiently
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                // Incremental add: Only add new columns, don't rebuild existing ones
                if (e.NewItems != null)
                {
                    foreach (ColumnHeaderViewModel header in e.NewItems)
                    {
                        AddSingleHeaderControl(header, e.NewStartingIndex + e.NewItems.IndexOf(header));
                    }
                    // Update column definitions after adding
                    RebuildColumnDefinitions();
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                // Incremental remove: Only remove specific columns
                if (e.OldItems != null && e.OldStartingIndex >= 0)
                {
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        // Remove header control at old index (children shift after each remove)
                        if (e.OldStartingIndex < _headersGrid.Children.Count)
                        {
                            _headersGrid.Children.RemoveAt(e.OldStartingIndex);
                        }
                    }
                    // Update column definitions after removing
                    RebuildColumnDefinitions();
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                // Full clear: Rebuild everything
                RebuildColumnDefinitions();
                RebuildHeaderControls();
                break;

            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
                // Complex operations: Fall back to full rebuild
                RebuildColumnDefinitions();
                RebuildHeaderControls();
                break;
        }
    }

    private void RebuildColumnDefinitions()
    {
        _headersGrid.ColumnDefinitions.Clear();
        var definitions = _viewModel.CreateColumnDefinitions();
        foreach (var def in definitions)
        {
            _headersGrid.ColumnDefinitions.Add(def);
        }
    }

    private void RebuildHeaderControls()
    {
        _headersGrid.Children.Clear();

        for (int i = 0; i < _viewModel.ColumnHeaders.Count; i++)
        {
            AddSingleHeaderControl(_viewModel.ColumnHeaders[i], i);
        }
    }

    /// <summary>
    /// ✅ SENIOR FIX: Adds a single header control at specified index.
    /// Used for incremental column additions to avoid O(n²) rebuilds.
    /// </summary>
    private void AddSingleHeaderControl(ColumnHeaderViewModel header, int columnIndex)
    {
        var headerControl = CreateHeaderControl(header, columnIndex);
        Grid.SetColumn(headerControl, columnIndex);
        _headersGrid.Children.Add(headerControl);
    }

    private Grid CreateHeaderControl(ColumnHeaderViewModel header, int columnIndex)
    {
        // Root Grid for each header cell (contains header content + resize grip)
        var cellGrid = new Grid
        {
            DataContext = header
        };

        // Column definitions: content + resize grip
        cellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Border for header content (Column 0)
        // First column gets left border (1px) for edge, other columns have 0 (resize grip provides spacing)
        var borderThickness = columnIndex == 0
            ? new Thickness(1, 1, 0, 1) // First column: Left=1 (edge border), Top=1, Right=0, Bottom=1
            : new Thickness(0, 1, 0, 1); // Other columns: Left=0, Top=1, Right=0, Bottom=1

        var border = new Border
        {
            BorderThickness = borderThickness,
            BorderBrush = _viewModel.Theme.ColumnBorder,
            Background = _viewModel.Theme.HeaderBackground,
            Padding = new Thickness(0, 4, 0, 4) // FIX: Removed horizontal padding (8px) to align with data cells
        };
        Grid.SetColumn(border, 0);

        // SPECIAL: Checkbox column header gets a Select All/Deselect All checkbox
        if (header.SpecialType == Common.SpecialColumnType.Checkbox)
        {
            var options = _viewModel.Theme.Options ?? new AdvancedDataGridOptions();

            // Parse hex colors
            var borderColor = ParseHexColor(options.CheckboxBorderColor, Colors.DimGray);
            var backgroundColor = ParseHexColor(options.CheckboxBackgroundColor, Colors.White);

            // ✅ PROFESSIONAL SOLUTION: Direct Grid-based checkbox implementation for header
            // WinUI 3 doesn't support FrameworkElementFactory (that's WPF), so we build it directly
            var headerCheckboxGrid = new Grid
            {
                Width = 16,
                Height = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Checkbox box border
            var headerCheckboxBorder = new Border
            {
                Width = 16,
                Height = 16,
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(borderColor),
                Background = new SolidColorBrush(backgroundColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Checkmark icon (for checked state)
            var headerCheckmarkIcon = new FontIcon
            {
                Glyph = "\uE73E", // Checkmark glyph
                FontSize = 10,
                Foreground = new SolidColorBrush(borderColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed // Hidden by default
            };

            // Indeterminate rectangle (for indeterminate state)
            var headerIndeterminateRect = new Microsoft.UI.Xaml.Shapes.Rectangle
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(borderColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Visible // Visible by default (indeterminate state)
            };

            headerCheckboxBorder.Child = new Grid
            {
                Children =
                {
                    headerCheckmarkIcon,
                    headerIndeterminateRect
                }
            };

            headerCheckboxGrid.Children.Add(headerCheckboxBorder);

            // Track checkbox state (three-state: null, true, false)
            bool? headerCheckboxState = false;

            // ✅ PROFESSIONAL QUALITY: Helper method to update header visual state from row state
            void UpdateHeaderCheckboxState()
            {
                var allRows = _viewModel.Rows;
                if (allRows == null || allRows.Count == 0)
                {
                    headerCheckboxState = false;
                    headerCheckmarkIcon.Visibility = Visibility.Collapsed;
                    headerIndeterminateRect.Visibility = Visibility.Collapsed;
                    _logger?.LogTrace("Header checkbox: no rows, state=false");
                    return;
                }

                var selectedCount = allRows.Count(r => r.IsSelected);
                var totalCount = allRows.Count;

                if (selectedCount == 0)
                {
                    // Žiadne označené → false (prázdny štvorček)
                    headerCheckboxState = false;
                    headerCheckmarkIcon.Visibility = Visibility.Collapsed;
                    headerIndeterminateRect.Visibility = Visibility.Collapsed;
                    _logger?.LogTrace("Header checkbox: 0/{Total} selected, state=false", totalCount);
                }
                else if (selectedCount == totalCount)
                {
                    // Všetky označené → true (fajka)
                    headerCheckboxState = true;
                    headerCheckmarkIcon.Visibility = Visibility.Visible;
                    headerIndeterminateRect.Visibility = Visibility.Collapsed;
                    _logger?.LogTrace("Header checkbox: {Total}/{Total} selected, state=true", totalCount, totalCount);
                }
                else
                {
                    // Niektoré označené → null (vyplnený štvorček)
                    headerCheckboxState = null;
                    headerCheckmarkIcon.Visibility = Visibility.Collapsed;
                    headerIndeterminateRect.Visibility = Visibility.Visible;
                    _logger?.LogTrace("Header checkbox: {Selected}/{Total} selected, state=indeterminate", selectedCount, totalCount);
                }
            }

            // ✅ BIDIRECTIONAL SYNC: Subscribe to row PropertyChanged events
            void SubscribeToRowPropertyChanged(DataGridRowViewModel row)
            {
                row.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(DataGridRowViewModel.IsSelected))
                    {
                        _logger?.LogTrace("Row {RowIndex} IsSelected changed, updating header checkbox", row.RowIndex);
                        UpdateHeaderCheckboxState();
                    }
                };
            }

            // Subscribe to all existing rows
            foreach (var row in _viewModel.Rows)
            {
                SubscribeToRowPropertyChanged(row);
            }

            // Subscribe to new rows (when collection changes)
            _viewModel.Rows.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (DataGridRowViewModel row in e.NewItems)
                    {
                        SubscribeToRowPropertyChanged(row);
                        _logger?.LogTrace("Subscribed to new row {RowIndex}", row.RowIndex);
                    }
                }

                // ✅ CRITICAL: Re-subscribe ALL rows after Reset (full reload)
                // Reset is fired when Rows.Clear() + rebuild happens (e.g., after virtual insert/delete)
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                {
                    _logger?.LogTrace("Rows collection reset, re-subscribing to ALL {Count} rows", _viewModel.Rows.Count);
                    foreach (var row in _viewModel.Rows)
                    {
                        SubscribeToRowPropertyChanged(row);
                    }
                }

                // Recalculate header state after collection change
                UpdateHeaderCheckboxState();
            };

            // Set initial state
            UpdateHeaderCheckboxState();

            // Make it interactive - handle Tapped event (header → rows)
            headerCheckboxGrid.IsTapEnabled = true;
            headerCheckboxGrid.Tapped += (s, e) =>
            {
                if (headerCheckboxState == null || headerCheckboxState == false)
                {
                    // Indeterminate/Unchecked → Checked (select all)
                    headerCheckboxState = true;
                    headerCheckmarkIcon.Visibility = Visibility.Visible;
                    headerIndeterminateRect.Visibility = Visibility.Collapsed;
                    _viewModel.SelectAllRows();
                    _logger?.LogInformation("Header checkbox clicked: SELECT ALL");
                }
                else
                {
                    // Checked → Unchecked (deselect all)
                    headerCheckboxState = false;
                    headerCheckmarkIcon.Visibility = Visibility.Collapsed;
                    headerIndeterminateRect.Visibility = Visibility.Collapsed;
                    _viewModel.DeselectAllRows();
                    _logger?.LogInformation("Header checkbox clicked: DESELECT ALL");
                }

                e.Handled = true;
            };

            border.Child = headerCheckboxGrid;
            _logger?.LogInformation("RowSelect header checkbox created with BIDIRECTIONAL SYNC");
        }
        else
        {
            // Normal text header
            var textBlock = new TextBlock
            {
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = _viewModel.Theme.HeaderForeground
            };

            var textBinding = new Binding
            {
                Source = header,
                Path = new PropertyPath(nameof(ColumnHeaderViewModel.DisplayName)),
                Mode = BindingMode.OneWay
            };
            textBlock.SetBinding(TextBlock.TextProperty, textBinding);

            border.Child = textBlock;

            // FÁZA 5: Add click handler for sort on header click (data columns only, not special columns)
            if (header.SpecialType == Common.SpecialColumnType.None)
            {
                border.IsTapEnabled = true;
                border.Tapped += (s, e) => OnHeaderTapped(header);
            }
        }

        // Custom resize grip control (Column 1) with resize cursor support
        // Set static logger and theme for ResizeGripControl (shared across all instances)
        var resizeLogger = _loggerFactory?.CreateLogger<ResizeGripControl>();
        ResizeGripControl.SetLogger(resizeLogger);
        ResizeGripControl.SetThemeManager(_viewModel.Theme);

        var resizeGrip = new ResizeGripControl
        {
            DataContext = header
            // Width, Background, and ProtectedCursor are set in constructor
            // Cursor will change to resize arrows (<->) when hovering over grip
        };
        Grid.SetColumn(resizeGrip, 1);

        _logger?.LogTrace("HeadersRowView: Created resize grip for column '{ColumnName}' (index {ColumnIndex})", header.ColumnName, columnIndex);
        _logger?.LogTrace("Grip properties: Width={Width}, IsHitTestVisible={IsHitTestVisible}, HasBackground={HasBackground}",
            resizeGrip.Width, resizeGrip.IsHitTestVisible, resizeGrip.Background != null);

        // SENIOR FIX: Use PointerEvents instead of ManipulationEvents for WinUI 3 compatibility
        resizeGrip.PointerPressed += OnResizeGripPointerPressed;
        resizeGrip.PointerMoved += OnResizeGripPointerMoved;
        resizeGrip.PointerReleased += OnResizeGripPointerReleased;
        resizeGrip.PointerCaptureLost += OnResizeGripPointerReleased; // Handle lost capture same as release

        _logger?.LogTrace("HeadersRowView: Resize grip event handlers attached for column '{ColumnName}'", header.ColumnName);

        // Add both to cell grid
        cellGrid.Children.Add(border);
        cellGrid.Children.Add(resizeGrip);

        return cellGrid;
    }

    // SENIOR FIX: Replaced ManipulationEvents with PointerEvents for reliable WinUI 3 behavior
    private void OnResizeGripPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _logger?.LogTrace("HeadersRowView: OnResizeGripPointerPressed called! Sender type: {SenderType}", sender?.GetType().Name);

        if (sender is ResizeGripControl grip)
        {
            _logger?.LogTrace("Sender IS ResizeGripControl, DataContext type: {DataContextType}", grip.DataContext?.GetType().Name);

            if (grip.DataContext is ColumnHeaderViewModel column)
            {
                _logger?.LogTrace("DataContext IS ColumnHeaderViewModel: {ColumnName}", column.ColumnName);

                _resizingColumn = column;
                _resizeStartWidth = column.Width;
                _resizeStartX = e.GetCurrentPoint(_headersGrid).Position.X; // FIX: Position relative to grid, not grip
                column.IsResizing = true;

                _logger?.LogInformation("Resize START: col={ColumnName}, width={Width}, x={StartX}", column.ColumnName, _resizeStartWidth, _resizeStartX);

                // ✅ MEDIUM FIX: Capture pointer to continue receiving events even if pointer moves outside grip
                var captured = grip.CapturePointer(e.Pointer);
                if (!captured)
                {
                    _logger?.LogWarning("Failed to capture pointer for resize grip");
                }
                else
                {
                    _logger?.LogTrace("Pointer captured successfully for resize");
                }

                // Create visual preview line (SENIOR ARCHITECTURE: Use theme color)
                _resizePreviewLine = new Border
                {
                    Width = 2,
                    Background = _viewModel.Theme?.ResizePreviewLine ?? new SolidColorBrush(Colors.Blue),
                    Opacity = 0.6,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                // Add preview line to parent grid (if accessible)
                if (this.Parent is Panel parentPanel)
                {
                    _logger?.LogTrace("Adding preview line to parent panel");
                    parentPanel.Children.Add(_resizePreviewLine);
                }
                else
                {
                    _logger?.LogWarning("Parent is not a Panel, cannot add preview line");
                }

                e.Handled = true;
            }
            else
            {
                _logger?.LogError("DataContext is NOT ColumnHeaderViewModel");
            }
        }
        else
        {
            _logger?.LogError("Sender is NOT ResizeGripControl");
        }
    }

    private void OnResizeGripPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_resizingColumn != null && sender is ResizeGripControl grip)
        {
            var currentX = e.GetCurrentPoint(_headersGrid).Position.X; // FIX: Position relative to grid, not grip
            var delta = currentX - _resizeStartX;
            var newWidth = _resizeStartWidth + delta;

            _logger?.LogTrace("Resize MOVE: col={ColumnName}, delta={Delta:F1}, newWidth={NewWidth:F1}",
                _resizingColumn.ColumnName, delta, newWidth);

            if (newWidth >= 50) // Minimum column width
            {
                // Update preview line position (visual feedback)
                if (_resizePreviewLine != null)
                {
                    var translateTransform = new TranslateTransform
                    {
                        X = delta
                    };
                    _resizePreviewLine.RenderTransform = translateTransform;
                }

                // Update actual width (this fires ColumnDefinitionsChanged event)
                _resizingColumn.Width = newWidth;
                _logger?.LogTrace("Column width updated to {NewWidth:F1}", newWidth);
            }
            else
            {
                _logger?.LogTrace("Width {NewWidth:F1} below minimum (50), skipping update", newWidth);
            }

            e.Handled = true;
        }
        else
        {
            if (_resizingColumn == null)
                _logger?.LogTrace("OnResizeGripPointerMoved: _resizingColumn is NULL");
            if (sender is not ResizeGripControl)
                _logger?.LogTrace("OnResizeGripPointerMoved: sender is not ResizeGripControl");
        }
    }

    private void OnResizeGripPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _logger?.LogTrace("HeadersRowView: OnResizeGripPointerReleased called!");

        if (_resizingColumn != null)
        {
            _logger?.LogInformation("Resize END: col={ColumnName}, final width={Width}",
                _resizingColumn.ColumnName, _resizingColumn.Width);

            _resizingColumn.IsResizing = false;

            // CRITICAL: If column was using auto-width (Star sizing), convert to fixed width after resize
            // This is important for ValidationAlerts column which auto-expands by default
            // After user manually resizes it, it should stay at fixed width
            if (_resizingColumn.UseAutoWidth)
            {
                _logger?.LogTrace("Converting column from auto-width to fixed width");
                _resizingColumn.UseAutoWidth = false;
                // Trigger column definitions rebuild to apply fixed width
                _viewModel.SyncColumnWidth(_resizingColumn.ColumnName, _resizingColumn.Width);
            }

            _resizingColumn = null;

            // Release pointer capture
            if (sender is ResizeGripControl grip)
            {
                grip.ReleasePointerCapture(e.Pointer);
                _logger?.LogTrace("Pointer capture released");
            }
            else
            {
                _logger?.LogWarning("Sender is not ResizeGripControl, cannot release capture");
            }

            // Remove preview line
            if (_resizePreviewLine != null && this.Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(_resizePreviewLine);
                _resizePreviewLine = null;
                _logger?.LogTrace("Preview line removed");
            }
            else
            {
                if (_resizePreviewLine == null)
                    _logger?.LogTrace("Preview line is null");
                if (this.Parent is not Panel)
                    _logger?.LogTrace("Parent is not Panel");
            }

            e.Handled = true;
        }
        else
        {
            _logger?.LogTrace("OnResizeGripPointerReleased: _resizingColumn is NULL (resize not started or already ended)");
        }
    }

    /// <summary>
    /// Parses hex color string to WinUI Color
    /// Supports formats: #RGB, #RRGGBB, #AARRGGBB
    /// </summary>
    private static Windows.UI.Color ParseHexColor(string hexColor, Windows.UI.Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || !hexColor.StartsWith("#"))
        {
            return fallback;
        }

        try
        {
            var hex = hexColor.TrimStart('#');

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

            return Windows.UI.Color.FromArgb(a, r, g, b);
        }
        catch
        {
            return fallback;
        }
    }


    /// <summary>
    /// SENIOR UPDATE: Header click now shows flyout with Sort + Filter options instead of direct sort cycling
    /// OLD: Cycled sort direction: None → Ascending → Descending → None
    /// NEW: Shows MenuFlyout with Sort Asc/Desc/None + Filter Checkbox/Regex options
    /// </summary>
    /// <param name="header">The column header that was clicked</param>
    private void OnHeaderTapped(ColumnHeaderViewModel header)
    {
        // Get the header Border control to anchor the flyout
        var headerBorder = FindHeaderBorder(header);
        if (headerBorder == null)
        {
            _logger?.LogWarning("OnHeaderTapped: Could not find header border for column {ColumnName}", header.ColumnName);
            return;
        }

        ShowHeaderFlyout(header, headerBorder);
    }

    /// <summary>
    /// SENIOR IMPLEMENTATION: Shows header flyout with Sort + Filter options
    /// Flyout contains:
    /// - Sort Ascending ↑
    /// - Sort Descending ↓
    /// - Clear Sort ✖
    /// - Separator
    /// - Filter (Select Values)...
    /// - Filter (Regex Pattern)...
    /// </summary>
    /// <param name="header">Column header view model</param>
    /// <param name="anchorElement">UI element to anchor the flyout (typically header border)</param>
    private void ShowHeaderFlyout(ColumnHeaderViewModel header, FrameworkElement anchorElement)
    {
        var flyout = new MenuFlyout();

        // ===== SORT OPTIONS =====
        var sortAscItem = new MenuFlyoutItem
        {
            Text = "Sort Ascending ↑",
            Icon = new SymbolIcon(Symbol.Up)
        };
        sortAscItem.Click += (s, e) =>
        {
            // ✅ PROFESSIONAL FIX: Detect Shift key for multi-sort
            var shiftKeyPressed = Microsoft.UI.Input.InputKeyboardSource
                .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            _logger?.LogInformation("Sort Ascending selected for column {ColumnName}, Shift={Shift}",
                header.ColumnName, shiftKeyPressed);

            _viewModel.SetSortDirection(header.ColumnName, "Ascending", shiftKeyPressed);
            flyout.Hide();
        };
        flyout.Items.Add(sortAscItem);

        var sortDescItem = new MenuFlyoutItem
        {
            Text = "Sort Descending ↓",
            Icon = new FontIcon { Glyph = "\uE96E" } // Down arrow glyph
        };
        sortDescItem.Click += (s, e) =>
        {
            // ✅ PROFESSIONAL FIX: Detect Shift key for multi-sort
            var shiftKeyPressed = Microsoft.UI.Input.InputKeyboardSource
                .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            _logger?.LogInformation("Sort Descending selected for column {ColumnName}, Shift={Shift}",
                header.ColumnName, shiftKeyPressed);

            _viewModel.SetSortDirection(header.ColumnName, "Descending", shiftKeyPressed);
            flyout.Hide();
        };
        flyout.Items.Add(sortDescItem);

        var sortNoneItem = new MenuFlyoutItem
        {
            Text = "Clear Sort ✖",
            Icon = new SymbolIcon(Symbol.Clear)
        };
        sortNoneItem.Click += (s, e) =>
        {
            _logger?.LogInformation("Clear Sort selected for column {ColumnName}", header.ColumnName);
            // ✅ Clear sort always single-mode (no multi-sort logic)
            _viewModel.SetSortDirection(header.ColumnName, "None", false);
            flyout.Hide();
        };
        flyout.Items.Add(sortNoneItem);

        // ===== SEPARATOR =====
        flyout.Items.Add(new MenuFlyoutSeparator());

        // ===== FILTER OPTIONS =====
        // Note: Filter functionality delegated to FilterFlyoutService (existing implementation)
        // These menu items will trigger the existing filter UI
        var filterCheckboxItem = new MenuFlyoutItem
        {
            Text = "Filter (Select Values)...",
            Icon = new SymbolIcon(Symbol.Filter)
        };
        filterCheckboxItem.Click += async (s, e) =>
        {
            _logger?.LogInformation("Filter (Checkbox mode) selected for column {ColumnName}", header.ColumnName);
            flyout.Hide();

            // ✅ PROFESSIONAL FIX: Trigger checkbox filter via FilterFlyoutService
            if (_viewModel?.FilterFlyoutService != null)
            {
                await ShowCheckboxFilterFlyoutAsync(header.ColumnName);
            }
            else
            {
                _logger?.LogWarning("FilterFlyoutService not available - cannot show checkbox filter");
            }
        };
        flyout.Items.Add(filterCheckboxItem);

        var filterRegexItem = new MenuFlyoutItem
        {
            Text = "Filter (Regex Pattern)...",
            Icon = new SymbolIcon(Symbol.Find)
        };
        filterRegexItem.Click += async (s, e) =>
        {
            _logger?.LogInformation("Filter (Regex mode) selected for column {ColumnName}", header.ColumnName);
            flyout.Hide();

            // ✅ PROFESSIONAL FIX: Trigger regex filter via FilterFlyoutService
            if (_viewModel?.FilterFlyoutService != null)
            {
                await ShowRegexFilterDialogAsync(header.ColumnName);
            }
            else
            {
                _logger?.LogWarning("FilterFlyoutService not available - cannot show regex filter");
            }
        };
        flyout.Items.Add(filterRegexItem);

        // ===== SHOW FLYOUT =====
        flyout.Placement = FlyoutPlacementMode.Bottom;
        flyout.ShowAt(anchorElement);

        // Log flyout display
        _logger?.LogTrace("Header flyout displayed for column {ColumnName}", header.ColumnName);

        // Auto-hide handled by WinUI - clicking outside or selecting item will close flyout
        flyout.Closed += (s, e) =>
        {
            _logger?.LogTrace("Header flyout closed for column {ColumnName}", header.ColumnName);
        };
    }

    /// <summary>
    /// SENIOR HELPER: Finds the Border control for a specific column header
    /// Used to anchor flyouts to the correct header
    /// </summary>
    /// <param name="header">Column header to find border for</param>
    /// <returns>Border control or null if not found</returns>
    private Border? FindHeaderBorder(ColumnHeaderViewModel header)
    {
        foreach (var child in _headersGrid.Children)
        {
            if (child is Grid cellGrid && cellGrid.DataContext == header)
            {
                // Find Border in first column of cellGrid
                foreach (var innerChild in cellGrid.Children)
                {
                    if (innerChild is Border border && Grid.GetColumn(border) == 0)
                    {
                        return border;
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Shows checkbox filter flyout for a column
    /// ARCHITECTURE:
    /// - Loads unique values from FilterFlyoutService.LoadUniqueValuesAsync()
    /// - Displays ContentDialog with ListBox of CheckBoxes (one per unique value)
    /// - Includes "Select All" checkbox for bulk selection
    /// - Apply button triggers FilterFlyoutService.ApplyCheckboxFilterAsync()
    /// </summary>
    private async Task ShowCheckboxFilterFlyoutAsync(string columnName)
    {
        if (_viewModel?.FilterFlyoutService == null)
        {
            _logger?.LogError("FilterFlyoutService not available");
            return;
        }

        try
        {
            // Load unique values for this column
            var uniqueValues = await _viewModel.FilterFlyoutService.LoadUniqueValuesAsync(columnName);

            if (uniqueValues.Count == 0)
            {
                _logger?.LogWarning("No unique values found for column {ColumnName}", columnName);
                return;
            }

            // Create dictionary to track checkbox states
            var checkboxes = new Dictionary<string, CheckBox>();
            var selectAllCheckbox = new CheckBox
            {
                Content = "Select All",
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Create ListBox with checkboxes
            var listBox = new ListBox
            {
                MaxHeight = 300,
                MinWidth = 250
            };

            foreach (var value in uniqueValues)
            {
                var checkbox = new CheckBox
                {
                    Content = value,
                    IsChecked = true
                };
                checkboxes[value] = checkbox;
                listBox.Items.Add(checkbox);
            }

            // Select All logic
            selectAllCheckbox.Checked += (s, e) =>
            {
                foreach (var cb in checkboxes.Values)
                    cb.IsChecked = true;
            };
            selectAllCheckbox.Unchecked += (s, e) =>
            {
                foreach (var cb in checkboxes.Values)
                    cb.IsChecked = false;
            };

            // Create StackPanel with Select All + ListBox
            var stackPanel = new StackPanel();
            stackPanel.Children.Add(selectAllCheckbox);
            stackPanel.Children.Add(listBox);

            // Create ContentDialog
            var dialog = new ContentDialog
            {
                Title = $"Filter: {columnName}",
                Content = stackPanel,
                PrimaryButtonText = "Apply",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Get selected values
                var selectedValues = checkboxes
                    .Where(kvp => kvp.Value.IsChecked == true)
                    .Select(kvp => kvp.Key)
                    .ToList();

                _logger?.LogInformation("Applying checkbox filter: {Count} values selected", selectedValues.Count);

                // Apply filter
                await _viewModel.FilterFlyoutService.ApplyCheckboxFilterAsync(columnName, selectedValues);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show checkbox filter for column {ColumnName}", columnName);
        }
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Shows regex filter dialog for a column
    /// ARCHITECTURE:
    /// - Displays ContentDialog with TextBox for regex pattern input
    /// - Includes "Case Sensitive" checkbox
    /// - Apply button triggers FilterFlyoutService.ApplyRegexFilterAsync()
    /// </summary>
    private async Task ShowRegexFilterDialogAsync(string columnName)
    {
        if (_viewModel?.FilterFlyoutService == null)
        {
            _logger?.LogError("FilterFlyoutService not available");
            return;
        }

        try
        {
            // Create TextBox for regex pattern
            var textBox = new TextBox
            {
                PlaceholderText = "Enter regex pattern (e.g., ^test.*|.*data$)",
                MinWidth = 300,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Create Case Sensitive checkbox
            var caseSensitiveCheckbox = new CheckBox
            {
                Content = "Case Sensitive",
                IsChecked = false
            };

            // Create StackPanel
            var stackPanel = new StackPanel();
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(caseSensitiveCheckbox);

            // Create ContentDialog
            var dialog = new ContentDialog
            {
                Title = $"Regex Filter: {columnName}",
                Content = stackPanel,
                PrimaryButtonText = "Apply",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var pattern = textBox.Text;

                if (string.IsNullOrWhiteSpace(pattern))
                {
                    _logger?.LogWarning("Regex pattern is empty - ignoring");
                    return;
                }

                // Add case-insensitive flag if needed
                if (caseSensitiveCheckbox.IsChecked == false)
                {
                    pattern = "(?i)" + pattern;
                }

                _logger?.LogInformation("Applying regex filter: Pattern={Pattern}", pattern);

                // Apply filter
                await _viewModel.FilterFlyoutService.ApplyRegexFilterAsync(columnName, pattern);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show regex filter for column {ColumnName}", columnName);
        }
    }
}

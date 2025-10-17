using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

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
    private ColumnHeaderViewModel? _resizingColumn; // Column currently being resized
    private double _resizeStartWidth; // Original width when resize started
    private Border? _resizePreviewLine; // Visual preview line during resize

    private readonly Grid _headersGrid;

    /// <summary>
    /// Creates a new headers row view bound to the specified view model.
    /// Automatically subscribes to column collection changes and column definition changes.
    /// MEMORY LEAK FIX: Subscribes to Unloaded event for proper cleanup.
    /// </summary>
    /// <param name="viewModel">The view model that manages the grid's data and state</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public HeadersRowView(DataGridViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Create Grid for headers with columns matching DataGridViewModel
        _headersGrid = new Grid
        {
            Padding = new Thickness(8, 4, 8, 4)
        };

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
        foreach (var child in _headersGrid.Children)
        {
            if (child is Grid cellGrid)
            {
                foreach (var innerChild in cellGrid.Children)
                {
                    if (innerChild is ResizeGripControl resizeGrip)
                    {
                        resizeGrip.ManipulationStarted -= OnResizeGripManipulationStarted;
                        resizeGrip.ManipulationDelta -= OnResizeGripManipulationDelta;
                        resizeGrip.ManipulationCompleted -= OnResizeGripManipulationCompleted;
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

    private void OnColumnHeadersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Rebuild everything when columns are added/removed
        RebuildColumnDefinitions();
        RebuildHeaderControls();
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
            var header = _viewModel.ColumnHeaders[i];
            var headerControl = CreateHeaderControl(header, i);
            Grid.SetColumn(headerControl, i);
            _headersGrid.Children.Add(headerControl);
        }
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
        var border = new Border
        {
            BorderThickness = new Thickness(1),
            BorderBrush = _viewModel.Theme.ColumnBorder,
            Background = _viewModel.Theme.HeaderBackground,
            Padding = new Thickness(8, 4, 8, 4)
        };
        Grid.SetColumn(border, 0);

        // SPECIAL: Checkbox column header gets a Select All/Deselect All checkbox
        if (header.SpecialType == Common.SpecialColumnType.Checkbox)
        {
            var options = _viewModel.Theme.Options ?? new AdvancedDataGridOptions();

            // Parse hex colors
            var borderColor = ParseHexColor(options.CheckboxBorderColor, Colors.DimGray);
            var backgroundColor = ParseHexColor(options.CheckboxBackgroundColor, Colors.White);

            var headerCheckbox = new CheckBox
            {
                IsThreeState = true, // null = indeterminate, true = all selected, false = none selected
                IsChecked = null, // Start with indeterminate
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = options.CheckboxMinWidth,
                MinHeight = options.CheckboxMinHeight,
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(options.CheckboxBorderThickness),
                Background = new SolidColorBrush(backgroundColor),
                Foreground = new SolidColorBrush(borderColor) // VISIBILITY FIX: Make unchecked checkbox visible in header
            };

            // Event: header checkbox changed
            headerCheckbox.Checked += (s, e) =>
            {
                // Select all rows
                _viewModel.SelectAllRows();
            };

            headerCheckbox.Unchecked += (s, e) =>
            {
                // Deselect all rows
                _viewModel.DeselectAllRows();
            };

            headerCheckbox.Indeterminate += (s, e) =>
            {
                // User clicked indeterminate state - treat as "select all"
                _viewModel.SelectAllRows();
            };

            border.Child = headerCheckbox;
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
        }

        // Custom resize grip control (Column 1) with resize cursor support
        var resizeGrip = new ResizeGripControl
        {
            DataContext = header
            // Width, Background, ManipulationMode, and ProtectedCursor are set in constructor
            // Cursor will change to resize arrows (<->) when hovering over grip
        };
        Grid.SetColumn(resizeGrip, 1);

        resizeGrip.ManipulationStarted += OnResizeGripManipulationStarted;
        resizeGrip.ManipulationDelta += OnResizeGripManipulationDelta;
        resizeGrip.ManipulationCompleted += OnResizeGripManipulationCompleted;

        // Add both to cell grid
        cellGrid.Children.Add(border);
        cellGrid.Children.Add(resizeGrip);

        return cellGrid;
    }

    private void OnResizeGripManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        if (sender is ResizeGripControl grip && grip.DataContext is ColumnHeaderViewModel column)
        {
            _resizingColumn = column;
            _resizeStartWidth = column.Width;
            column.IsResizing = true;

            // Create visual preview line
            _resizePreviewLine = new Border
            {
                Width = 2,
                Background = new SolidColorBrush(Colors.Blue),
                Opacity = 0.5,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Add preview line to parent grid (if accessible)
            if (this.Parent is Panel parentPanel)
            {
                parentPanel.Children.Add(_resizePreviewLine);
            }
        }
    }

    private void OnResizeGripManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (_resizingColumn != null)
        {
            var newWidth = _resizeStartWidth + e.Cumulative.Translation.X;
            if (newWidth >= 50) // Minimum column width
            {
                // Update preview line position (visual feedback)
                if (_resizePreviewLine != null)
                {
                    var translateTransform = new TranslateTransform
                    {
                        X = e.Cumulative.Translation.X
                    };
                    _resizePreviewLine.RenderTransform = translateTransform;
                }

                // Update actual width (this fires ColumnDefinitionsChanged event)
                _resizingColumn.Width = newWidth;
            }
        }
    }

    private void OnResizeGripManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        if (_resizingColumn != null)
        {
            _resizingColumn.IsResizing = false;
            _resizingColumn = null;

            // Remove preview line
            if (_resizePreviewLine != null && this.Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(_resizePreviewLine);
                _resizePreviewLine = null;
            }
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
}

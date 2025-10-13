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
/// </summary>
public sealed class HeadersRowView : UserControl
{
    private readonly DataGridViewModel _viewModel;
    private ColumnHeaderViewModel? _resizingColumn; // Column currently being resized
    private double _resizeStartWidth; // Original width when resize started

    private readonly Grid _headersGrid;

    /// <summary>
    /// Creates a new headers row view bound to the specified view model.
    /// Automatically subscribes to column collection changes and column definition changes.
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

        // Set grid as UserControl content
        Content = _headersGrid;
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

        // TextBlock for header display name
        var textBlock = new TextBlock
        {
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = _viewModel.Theme.HeaderForeground // Explicitly set readable color from theme
        };

        var textBinding = new Binding
        {
            Source = header,
            Path = new PropertyPath(nameof(ColumnHeaderViewModel.DisplayName)),
            Mode = BindingMode.OneWay
        };
        textBlock.SetBinding(TextBlock.TextProperty, textBinding);

        border.Child = textBlock;

        // Border for resize grip (Column 1)
        var resizeGrip = new Border
        {
            Width = 4,
            Background = new SolidColorBrush(Colors.DarkGray),
            DataContext = header,
            ManipulationMode = ManipulationModes.TranslateX
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
        if (sender is Border border && border.DataContext is ColumnHeaderViewModel column)
        {
            _resizingColumn = column;
            _resizeStartWidth = column.Width;
            column.IsResizing = true;
        }
    }

    private void OnResizeGripManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (_resizingColumn != null)
        {
            var newWidth = _resizeStartWidth + e.Cumulative.Translation.X;
            if (newWidth >= 50) // Minimum column width
            {
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
        }
    }
}

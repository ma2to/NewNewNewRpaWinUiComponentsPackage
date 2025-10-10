using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Filter row view that provides column-level filtering with text input boxes.
/// Displays one filter text box per column, allowing users to filter data by typing criteria.
/// Includes "Apply Filters" and "Clear Filters" buttons for user control.
/// Uses Grid layout with ColumnDefinitions synchronized with headers and data cells for proper alignment.
/// </summary>
public sealed class FilterRowView : UserControl
{
    private readonly DataGridViewModel _viewModel;

    /// <summary>
    /// Fired when the user clicks the "Apply Filters" button.
    /// </summary>
    public event EventHandler? ApplyFiltersRequested;

    /// <summary>
    /// Fired when the user clicks the "Clear Filters" button.
    /// </summary>
    public event EventHandler? ClearFiltersRequested;

    private readonly Grid _rootGrid;
    private readonly Grid _filtersGrid; // Grid containing filter text boxes
    private readonly StackPanel _buttonsPanel; // Panel containing action buttons
    private readonly Button _applyButton;
    private readonly Button _clearButton;

    /// <summary>
    /// Creates a new filter row view bound to the specified view model.
    /// Automatically subscribes to column collection changes and column definition changes.
    /// </summary>
    /// <param name="viewModel">The view model that manages the grid's data and state</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public FilterRowView(DataGridViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Create root Grid with two rows: filters and buttons
        _rootGrid = new Grid
        {
            Padding = new Thickness(8, 4, 8, 4)
        };
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Bind root grid visibility
        var visibilityBinding = new Binding
        {
            Source = _viewModel.FilterRow,
            Path = new PropertyPath(nameof(FilterRowViewModel.IsVisible)),
            Mode = BindingMode.OneWay,
            Converter = new BoolToVisibilityConverter()
        };
        _rootGrid.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

        // Filters Grid (Row 0) - Grid with columns matching DataGridViewModel
        _filtersGrid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 4)
        };
        Grid.SetRow(_filtersGrid, 0);

        // Initialize column definitions
        RebuildColumnDefinitions();

        // Create filter controls
        RebuildFilterControls();

        // Listen for column definition changes
        _viewModel.ColumnDefinitionsChanged += OnColumnDefinitionsChanged;

        // Listen for collection changes
        _viewModel.FilterRow.ColumnFilters.CollectionChanged += OnColumnFiltersCollectionChanged;

        // Buttons Panel (Row 1)
        _buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // Apply Filters Button
        _applyButton = new Button
        {
            Content = "Apply Filters",
            VerticalAlignment = VerticalAlignment.Center
        };
        _applyButton.Click += OnApplyFiltersClick;

        // Clear Filters Button
        _clearButton = new Button
        {
            Content = "Clear Filters",
            VerticalAlignment = VerticalAlignment.Center
        };
        _clearButton.Click += OnClearFiltersClick;

        _buttonsPanel.Children.Add(_applyButton);
        _buttonsPanel.Children.Add(_clearButton);

        Grid.SetRow(_buttonsPanel, 1);

        // Add panels to root grid
        _rootGrid.Children.Add(_filtersGrid);
        _rootGrid.Children.Add(_buttonsPanel);

        // Set root grid as UserControl content
        Content = _rootGrid;
    }

    private void OnColumnDefinitionsChanged(object? sender, EventArgs e)
    {
        // Rebuild column definitions when widths change
        RebuildColumnDefinitions();
    }

    private void OnColumnFiltersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Rebuild everything when filters are added/removed
        RebuildColumnDefinitions();
        RebuildFilterControls();
    }

    private void RebuildColumnDefinitions()
    {
        _filtersGrid.ColumnDefinitions.Clear();
        var definitions = _viewModel.CreateColumnDefinitions();
        foreach (var def in definitions)
        {
            _filtersGrid.ColumnDefinitions.Add(def);
        }
    }

    private void RebuildFilterControls()
    {
        _filtersGrid.Children.Clear();

        for (int i = 0; i < _viewModel.FilterRow.ColumnFilters.Count; i++)
        {
            var filter = _viewModel.FilterRow.ColumnFilters[i];
            var filterControl = CreateFilterTextBox(filter);
            Grid.SetColumn(filterControl, i);
            _filtersGrid.Children.Add(filterControl);
        }
    }

    private TextBox CreateFilterTextBox(ColumnFilterViewModel filter)
    {
        var textBox = new TextBox
        {
            PlaceholderText = "Filter...",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2),
            DataContext = filter
        };

        // Bind text
        var textBinding = new Binding
        {
            Source = filter,
            Path = new PropertyPath(nameof(ColumnFilterViewModel.FilterText)),
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        textBox.SetBinding(TextBox.TextProperty, textBinding);

        return textBox;
    }

    private void OnApplyFiltersClick(object sender, RoutedEventArgs e)
    {
        ApplyFiltersRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnClearFiltersClick(object sender, RoutedEventArgs e)
    {
        _viewModel.FilterRow.ClearAllFilters();
        ClearFiltersRequested?.Invoke(this, EventArgs.Empty);
    }
}

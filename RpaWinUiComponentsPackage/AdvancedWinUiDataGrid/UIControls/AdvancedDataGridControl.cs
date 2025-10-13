using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Main AdvancedDataGrid control that orchestrates all sub-views (search panel, filters, headers, and data cells).
/// This is the top-level UI control that you add to your WinUI window.
/// The control is built programmatically without XAML for maximum flexibility.
/// </summary>
public sealed class AdvancedDataGridControl : UserControl
{
    private readonly ILogger<AdvancedDataGridControl>? _logger;

    /// <summary>
    /// Gets the view model that manages the grid's data and state.
    /// Use this to access grid data, selections, filters, etc.
    /// </summary>
    public DataGridViewModel ViewModel { get; }

    private SearchPanelView? _searchPanelView;
    private FilterRowView? _filterRowView;
    private HeadersRowView? _headersRowView;
    private DataGridCellsView? _dataCellsView;

    private readonly Grid _rootGrid;
    private readonly Border _searchPanelContainer;
    private readonly Border _filterRowContainer;
    private readonly Border _headersRowContainer;
    private readonly Border _dataCellsContainer;

    /// <summary>
    /// Creates a new instance of the AdvancedDataGrid control with a new view model.
    /// This constructor is useful when you want the control to create its own view model.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics and troubleshooting</param>
    public AdvancedDataGridControl(ILogger<AdvancedDataGridControl>? logger = null)
    {
        _logger = logger;
        ViewModel = new DataGridViewModel();

        _logger?.LogInformation("AdvancedDataGridControl created with new ViewModel");

        // Initialize UI containers
        _rootGrid = new Grid();
        _searchPanelContainer = new Border();
        _filterRowContainer = new Border();
        _headersRowContainer = new Border();
        _dataCellsContainer = new Border();

        InitializeUI();
        InitializeSubViews();
    }

    /// <summary>
    /// Creates a new instance of the AdvancedDataGrid control with a shared view model.
    /// This constructor is useful when you want to share a view model between multiple components.
    /// </summary>
    /// <param name="viewModel">The view model to use for this control</param>
    /// <param name="logger">Optional logger for diagnostics and troubleshooting</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public AdvancedDataGridControl(DataGridViewModel viewModel, ILogger<AdvancedDataGridControl>? logger = null)
    {
        _logger = logger;
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _logger?.LogInformation("AdvancedDataGridControl created with existing ViewModel");

        // Initialize UI containers
        _rootGrid = new Grid();
        _searchPanelContainer = new Border();
        _filterRowContainer = new Border();
        _headersRowContainer = new Border();
        _dataCellsContainer = new Border();

        InitializeUI();
        InitializeSubViews();
    }

    /// <summary>
    /// Initializes the UI layout with a 4-row grid structure.
    /// Rows from top to bottom: Search Panel, Filter Row, Headers, Data Cells.
    /// The data cells area takes up all remaining vertical space.
    /// </summary>
    private void InitializeUI()
    {
        _logger?.LogInformation("Initializing grid UI layout");

        // Create root Grid with 4 rows: SearchPanel, FilterRow, Headers, DataCells
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // SearchPanel - auto-sized based on content
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // FilterRow - auto-sized based on content
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Headers - auto-sized based on content
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // DataCells - takes remaining space

        // SearchPanel Container (Row 0) - appears at the top
        _searchPanelContainer.BorderThickness = new Thickness(0, 0, 0, 1);
        _searchPanelContainer.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
        Grid.SetRow(_searchPanelContainer, 0);

        // FilterRow Container (Row 1) - appears below search panel
        _filterRowContainer.BorderThickness = new Thickness(0, 0, 0, 1);
        _filterRowContainer.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
        Grid.SetRow(_filterRowContainer, 1);

        // HeadersRow Container (Row 2) - appears above data cells
        _headersRowContainer.BorderThickness = new Thickness(0, 0, 0, 1);
        _headersRowContainer.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        Grid.SetRow(_headersRowContainer, 2);

        // DataCells Container (Row 3) - scrollable area that takes up remaining vertical space
        Grid.SetRow(_dataCellsContainer, 3);

        // Add all containers to root grid in order
        _rootGrid.Children.Add(_searchPanelContainer);
        _rootGrid.Children.Add(_filterRowContainer);
        _rootGrid.Children.Add(_headersRowContainer);
        _rootGrid.Children.Add(_dataCellsContainer);

        // Set root grid as UserControl content
        Content = _rootGrid;

        _logger?.LogInformation("Grid UI layout initialized successfully");
    }

    /// <summary>
    /// Initializes and wires up all sub-views (search panel, filters, headers, data cells).
    /// Each sub-view is connected to the appropriate view model and event handlers are registered.
    /// </summary>
    private void InitializeSubViews()
    {
        _logger?.LogInformation("Initializing sub-views");

        // Create and wire up SearchPanelView - enables searching across grid data
        _searchPanelView = new SearchPanelView(ViewModel.SearchPanel);
        _searchPanelView.SearchRequested += OnSearchRequested;
        _searchPanelView.ClearRequested += OnSearchCleared;
        _searchPanelContainer.Child = _searchPanelView;

        // Create and wire up FilterRowView - provides column-level filtering
        _filterRowView = new FilterRowView(ViewModel);
        _filterRowView.ApplyFiltersRequested += OnApplyFiltersRequested;
        _filterRowView.ClearFiltersRequested += OnClearFiltersRequested;
        _filterRowContainer.Child = _filterRowView;

        // Create and wire up HeadersRowView - displays column headers with resize/sort capabilities
        _headersRowView = new HeadersRowView(ViewModel);
        _headersRowContainer.Child = _headersRowView;

        // Create and wire up DataGridCellsView - the main scrollable data area
        _dataCellsView = new DataGridCellsView(ViewModel);
        _dataCellsContainer.Child = _dataCellsView;

        _logger?.LogInformation("Sub-views initialized successfully");
    }

    /// <summary>
    /// Event handler for search requests from the search panel.
    /// TODO: This should be connected to the Facade API for actual search implementation.
    /// </summary>
    /// <param name="sender">The search panel view</param>
    /// <param name="e">Event arguments</param>
    private void OnSearchRequested(object? sender, EventArgs e)
    {
        _logger?.LogInformation("Search requested with text: {SearchText}", ViewModel.SearchPanel.SearchText);
        // TODO: Implement search via Facade API
        // Call IAdvancedDataGridFacade.SearchAsync with ViewModel.SearchPanel.SearchText
    }

    /// <summary>
    /// Event handler for clearing search results.
    /// TODO: This should be connected to the Facade API to clear search highlights.
    /// </summary>
    /// <param name="sender">The search panel view</param>
    /// <param name="e">Event arguments</param>
    private void OnSearchCleared(object? sender, EventArgs e)
    {
        _logger?.LogInformation("Search cleared");
        // TODO: Clear search highlights
        // Call IAdvancedDataGridFacade.ClearSearchHighlightsAsync
    }

    /// <summary>
    /// Event handler for applying filters from the filter row.
    /// TODO: This should be connected to the Facade API to apply column filters.
    /// </summary>
    /// <param name="sender">The filter row view</param>
    /// <param name="e">Event arguments</param>
    private void OnApplyFiltersRequested(object? sender, EventArgs e)
    {
        _logger?.LogInformation("Apply filters requested");
        // TODO: Apply filters via Facade API
        // Call IAdvancedDataGridFacade.ApplyFilterAsync for each column filter
    }

    /// <summary>
    /// Event handler for clearing all filters.
    /// TODO: This should be connected to the Facade API to clear all column filters.
    /// </summary>
    /// <param name="sender">The filter row view</param>
    /// <param name="e">Event arguments</param>
    private void OnClearFiltersRequested(object? sender, EventArgs e)
    {
        _logger?.LogInformation("Clear filters requested");
        // TODO: Clear filters via Facade API
        // Call IAdvancedDataGridFacade.ClearAllFiltersAsync
    }

    /// <summary>
    /// Loads data into the grid and initializes columns based on the provided column names.
    /// This is the primary method for populating the grid with data.
    /// Supports special columns (RowNumber, Checkbox, ValidationAlerts, DeleteRow) based on options.
    /// </summary>
    /// <param name="data">Collection of rows to display, where each row is a dictionary of column name to value</param>
    /// <param name="columnNames">Names of columns to display in the grid</param>
    /// <param name="options">Optional grid options containing special column configuration</param>
    public void LoadData(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IEnumerable<string> columnNames,
        AdvancedDataGridOptions? options = null)
    {
        var columnList = columnNames.ToList();
        var dataList = data.ToList();

        _logger?.LogInformation("Loading data: {RowCount} rows, {ColumnCount} columns (with special columns support)",
            dataList.Count, columnList.Count);

        // Initialize columns first (including special columns), then load the row data
        ViewModel.InitializeColumns(columnList, options);
        ViewModel.LoadRows(dataList);

        _logger?.LogInformation("Data loaded successfully with {SpecialCount} special columns",
            options != null ?
                (options.EnableRowNumberColumn ? 1 : 0) +
                (options.EnableCheckboxColumn ? 1 : 0) +
                (options.EnableValidationAlertsColumn ? 1 : 0) +
                (options.EnableDeleteRowColumn ? 1 : 0)
                : 0);
    }

    /// <summary>
    /// Clears all data from the grid, including rows, columns, filters, and search criteria.
    /// This resets the grid to an empty state.
    /// </summary>
    public void Clear()
    {
        _logger?.LogInformation("Clearing all grid data");
        ViewModel.Clear();
    }
}

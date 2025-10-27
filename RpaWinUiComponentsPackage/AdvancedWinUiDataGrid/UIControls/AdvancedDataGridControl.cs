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
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Gets the view model that manages the grid's data and state.
    /// Use this to access grid data, selections, filters, etc.
    /// </summary>
    public DataGridViewModel ViewModel { get; }

    /// <summary>
    /// Event fired when the user requests to delete a row via the delete button in special column.
    /// The application should handle this event and call the facade's SmartDelete functionality.
    /// Contains both rowIndex (for display) and rowId (for stable row identification).
    /// </summary>
    public event EventHandler<DeleteRowRequestedEventArgs>? DeleteRowRequested;

    /// <summary>
    /// Event fired when the user requests to insert a row via the insert button in special column.
    /// The application should handle this event and call the facade's SmartOperations.InsertRowAfterAsync functionality.
    /// Contains both rowIndex (for display) and rowId (for stable row identification).
    /// </summary>
    public event EventHandler<InsertRowRequestedEventArgs>? InsertRowRequested;

    /// <summary>
    /// Event fired when the user changes row selection via the checkbox special column.
    /// The application can handle this event to track which rows are selected.
    /// </summary>
    public event EventHandler<(int rowIndex, bool isSelected)>? RowSelectionChanged;

    /// <summary>
    /// Event fired when the user completes editing a cell (presses Enter).
    /// The application can handle this event to trigger auto-expand when the last row is edited.
    /// </summary>
    public event EventHandler<CellViewModel>? CellEditCompleted;

    private SearchPanelView? _searchPanelView;
    private FilterRowView? _filterRowView;
    private HeadersRowView? _headersRowView;
    private DataGridCellsView? _dataCellsView;
    private PaginationPanelView? _paginationPanelView;

    private readonly Grid _rootGrid;
    private readonly Border _searchPanelContainer;
    private readonly Border _filterRowContainer;
    private readonly Border _headersRowContainer;
    private readonly Border _dataCellsContainer;
    private readonly Border _paginationPanelContainer;

    /// <summary>
    /// Creates a new instance of the AdvancedDataGrid control with a new view model.
    /// This constructor is useful when you want the control to create its own view model.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics and troubleshooting</param>
    /// <param name="loggerFactory">Optional logger factory for creating child component loggers</param>
    /// <param name="pageManager">Optional page manager for pagination support (enables virtual row management)</param>
    public AdvancedDataGridControl(
        ILogger<AdvancedDataGridControl>? logger = null,
        ILoggerFactory? loggerFactory = null,
        Features.Pagination.Interfaces.IPageManager? pageManager = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;

        // ✅ CRITICAL FIX: Create PageManager automatically if not provided (interactive mode)
        // This enables virtual row management by default
        if (pageManager == null)
        {
            var pageManagerLogger = loggerFactory?.CreateLogger<Features.Pagination.Services.PageManager>();
            pageManager = new Features.Pagination.Services.PageManager(pageManagerLogger!);
            pageManager.PageSize = 20; // Default page size
            _logger?.LogInformation("Created PageManager automatically with PageSize=20 for interactive mode");
        }

        ViewModel = new DataGridViewModel(null, loggerFactory, this.DispatcherQueue, null, pageManager);

        _logger?.LogInformation("AdvancedDataGridControl created with new ViewModel (PageManager: {HasPageManager})",
            pageManager != null);

        // Initialize UI containers
        _rootGrid = new Grid();
        _searchPanelContainer = new Border();
        _filterRowContainer = new Border();
        _headersRowContainer = new Border();
        _dataCellsContainer = new Border();
        _paginationPanelContainer = new Border();

        InitializeUI();
        InitializeSubViews();
    }

    /// <summary>
    /// Creates a new instance of the AdvancedDataGrid control with a shared view model.
    /// This constructor is useful when you want to share a view model between multiple components.
    /// </summary>
    /// <param name="viewModel">The view model to use for this control</param>
    /// <param name="logger">Optional logger for diagnostics and troubleshooting</param>
    /// <param name="loggerFactory">Optional logger factory for creating child component loggers</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public AdvancedDataGridControl(
        DataGridViewModel viewModel,
        ILogger<AdvancedDataGridControl>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _logger?.LogInformation("AdvancedDataGridControl created with existing ViewModel");

        // Initialize UI containers
        _rootGrid = new Grid();
        _searchPanelContainer = new Border();
        _filterRowContainer = new Border();
        _headersRowContainer = new Border();
        _dataCellsContainer = new Border();
        _paginationPanelContainer = new Border();

        InitializeUI();
        InitializeSubViews();
    }

    /// <summary>
    /// Initializes the UI layout with a 5-row grid structure.
    /// Rows from top to bottom: Search Panel, Filter Row, Headers, Data Cells, Pagination Panel.
    /// The data cells area takes up all remaining vertical space.
    /// </summary>
    private void InitializeUI()
    {
        _logger?.LogInformation("Initializing grid UI layout");

        // Create root Grid with 5 rows: SearchPanel, FilterRow, Headers, DataCells, PaginationPanel
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // SearchPanel - auto-sized based on content
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // FilterRow - auto-sized based on content
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Headers - auto-sized based on content
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // DataCells - takes remaining space
        _rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // PaginationPanel - auto-sized based on content

        // SearchPanel Container (Row 0) - appears at the top (SENIOR ARCHITECTURE: Use theme colors)
        _searchPanelContainer.BorderThickness = new Thickness(0, 0, 0, 1);
        _searchPanelContainer.BorderBrush = ViewModel.Theme?.SearchPanelBorder ?? new SolidColorBrush(Microsoft.UI.Colors.LightGray);
        Grid.SetRow(_searchPanelContainer, 0);

        // FilterRow Container (Row 1) - appears below search panel (SENIOR ARCHITECTURE: Use theme colors)
        _filterRowContainer.BorderThickness = new Thickness(0, 0, 0, 1);
        _filterRowContainer.BorderBrush = ViewModel.Theme?.FilterRowBorder ?? new SolidColorBrush(Microsoft.UI.Colors.LightGray);
        Grid.SetRow(_filterRowContainer, 1);

        // HeadersRow Container (Row 2) - appears above data cells (SENIOR ARCHITECTURE: Use theme colors)
        _headersRowContainer.BorderThickness = new Thickness(0, 0, 0, 1);
        _headersRowContainer.BorderBrush = ViewModel.Theme?.HeadersRowBorder ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);
        Grid.SetRow(_headersRowContainer, 2);

        // DataCells Container (Row 3) - scrollable area that takes up remaining vertical space
        Grid.SetRow(_dataCellsContainer, 3);

        // PaginationPanel Container (Row 4) - appears at the bottom (SENIOR ARCHITECTURE: Use theme colors)
        _paginationPanelContainer.BorderThickness = new Thickness(0, 1, 0, 0);
        _paginationPanelContainer.BorderBrush = ViewModel.Theme?.PaginationPanelBorder ?? new SolidColorBrush(Microsoft.UI.Colors.LightGray);
        Grid.SetRow(_paginationPanelContainer, 4);

        // Add all containers to root grid in order
        _rootGrid.Children.Add(_searchPanelContainer);
        _rootGrid.Children.Add(_filterRowContainer);
        _rootGrid.Children.Add(_headersRowContainer);
        _rootGrid.Children.Add(_dataCellsContainer);
        _rootGrid.Children.Add(_paginationPanelContainer);

        // Set root grid as UserControl content
        Content = _rootGrid;

        _logger?.LogInformation("Grid UI layout initialized successfully");
    }

    /// <summary>
    /// Initializes and wires up all sub-views (search panel, filters, headers, data cells, pagination panel).
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
        _headersRowView = new HeadersRowView(ViewModel, _loggerFactory?.CreateLogger<HeadersRowView>(), _loggerFactory);
        _headersRowContainer.Child = _headersRowView;

        // Create and wire up DataGridCellsView - the main scrollable data area
        _dataCellsView = new DataGridCellsView(ViewModel, _loggerFactory?.CreateLogger<DataGridCellsView>(), _loggerFactory);
        _dataCellsView.DeleteRowRequested += OnDeleteRowRequestedInternal;
        _dataCellsView.InsertRowRequested += OnInsertRowRequestedInternal;
        _dataCellsView.RowSelectionChanged += OnRowSelectionChangedInternal;
        _dataCellsView.CellEditCompleted += OnCellEditCompletedInternal;
        _dataCellsContainer.Child = _dataCellsView;

        // Create and wire up PaginationPanelView - provides page navigation controls
        _paginationPanelView = new PaginationPanelView(ViewModel.PaginationPanel);
        _paginationPanelView.PageChanged += OnPageChangedInternal;
        _paginationPanelContainer.Child = _paginationPanelView;

        _logger?.LogInformation("Sub-views initialized successfully");
    }

    /// <summary>
    /// Internal handler that forwards delete row requests from DataGridCellsView to public event.
    /// </summary>
    private void OnDeleteRowRequestedInternal(object? sender, DeleteRowRequestedEventArgs args)
    {
        _logger?.LogInformation("Delete row requested for row index {RowIndex}, rowId {RowId}", args.RowIndex, args.RowId);
        DeleteRowRequested?.Invoke(this, args);
    }

    /// <summary>
    /// Internal handler that forwards insert row requests from DataGridCellsView to public event.
    /// </summary>
    private void OnInsertRowRequestedInternal(object? sender, InsertRowRequestedEventArgs args)
    {
        _logger?.LogInformation("Insert row requested for row index {RowIndex}, rowId {RowId}", args.RowIndex, args.RowId);
        InsertRowRequested?.Invoke(this, args);
    }

    /// <summary>
    /// Internal handler that forwards row selection changes from DataGridCellsView to public event.
    /// PERFORMANCE: Changed to DEBUG level to reduce log volume (was 30% of all logs in production)
    /// </summary>
    private void OnRowSelectionChangedInternal(object? sender, (int rowIndex, bool isSelected) e)
    {
        _logger?.LogDebug("Row selection changed: row {RowIndex} -> {IsSelected}", e.rowIndex, e.isSelected);
        RowSelectionChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Internal handler that forwards cell edit completion from DataGridCellsView to public event.
    /// This event is used to trigger auto-expand when the last row is edited.
    /// </summary>
    private void OnCellEditCompletedInternal(object? sender, CellViewModel cell)
    {
        _logger?.LogInformation("Cell edit completed: row {RowIndex}, column {ColumnName}", cell.RowIndex, cell.ColumnName);
        CellEditCompleted?.Invoke(this, cell);
    }

    /// <summary>
    /// Internal handler for page change events from PaginationPanelView.
    /// TODO: This should trigger a data reload from the facade with the new page number.
    /// </summary>
    private void OnPageChangedInternal(object? sender, int newPage)
    {
        _logger?.LogInformation("Page changed to {PageNumber}", newPage);
        // TODO: Reload data for the new page via Facade API
        // Call IAdvancedDataGridFacade.GetPagedDataAsync(newPage, pageSize)
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

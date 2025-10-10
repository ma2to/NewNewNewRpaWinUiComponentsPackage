using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Facade wrapper that connects UI (AdvancedDataGridControl) with Facade API.
/// This class provides seamless integration between UI events and backend operations,
/// acting as a bridge between the visual data grid control and the underlying data operations.
/// Use this when you need both the visual grid and programmatic access to grid operations.
/// </summary>
public sealed class AdvancedDataGridFacadeUI : IAsyncDisposable
{
    private readonly IAdvancedDataGridFacade _facade;
    private readonly AdvancedDataGridControl _control;
    private readonly DataGridViewModel _viewModel;
    private readonly ILogger<AdvancedDataGridFacadeUI>? _logger;

    /// <summary>
    /// Gets the UI control that can be added to your WinUI application window.
    /// This is the visual representation of the data grid.
    /// </summary>
    public AdvancedDataGridControl Control => _control;

    /// <summary>
    /// Gets the facade API for programmatic access to grid operations.
    /// Use this to import data, apply filters, run validations, etc.
    /// </summary>
    public IAdvancedDataGridFacade Facade => _facade;

    /// <summary>
    /// Gets the view model that manages the grid's visual state.
    /// This is useful for accessing grid state like selected cells, columns, rows, etc.
    /// </summary>
    public DataGridViewModel ViewModel => _viewModel;

    /// <summary>
    /// Creates a new instance of the AdvancedDataGridFacadeUI.
    /// This sets up the UI control, view model, and wires up all event handlers.
    /// </summary>
    /// <param name="facade">The facade API to use for backend operations</param>
    /// <param name="logger">Optional logger for diagnostics and troubleshooting</param>
    /// <exception cref="ArgumentNullException">Thrown when facade is null</exception>
    public AdvancedDataGridFacadeUI(IAdvancedDataGridFacade facade, ILogger<AdvancedDataGridFacadeUI>? logger = null)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        _logger = logger;
        _viewModel = new DataGridViewModel();
        _control = new AdvancedDataGridControl(_viewModel);

        _logger?.LogInformation("AdvancedDataGridFacadeUI initialized");

        // Wire up event handlers
        WireUpEventHandlers();
    }

    /// <summary>
    /// Wires up event handlers between UI and facade.
    /// Currently a placeholder for future event integration.
    /// </summary>
    private void WireUpEventHandlers()
    {
        // Search events are already wired in AdvancedDataGridControl
        // We can access them via control if needed

        // Note: Event handlers in AdvancedDataGridControl have TODO comments
        // Those should be implemented by the application using this wrapper
        // This wrapper provides the connection point between UI and Facade

        _logger?.LogInformation("Event handlers wired up");
    }

    /// <summary>
    /// Loads data into the grid with automatic column detection.
    /// This method analyzes the first row to determine column names,
    /// then populates the grid with all provided data.
    /// </summary>
    /// <param name="data">Collection of rows to load, where each row is a dictionary of column name to value</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation</param>
    /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
    public async Task LoadDataAsync(IEnumerable<IReadOnlyDictionary<string, object?>> data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        var dataList = data.ToList();

        _logger?.LogInformation("Loading {RowCount} rows into grid", dataList.Count);

        if (dataList.Count == 0)
        {
            _logger?.LogInformation("No data to load, grid remains empty");
            return;
        }

        // Get column names from first row
        var columnNames = dataList.First().Keys.ToList();
        _logger?.LogInformation("Detected {ColumnCount} columns: {Columns}",
            columnNames.Count, string.Join(", ", columnNames));

        // Load into UI
        _control.LoadData(dataList, columnNames);

        _logger?.LogInformation("Data loaded successfully into grid UI");

        await Task.CompletedTask; // Make this truly async if needed in future
    }

    /// <summary>
    /// Applies a theme to the entire grid, changing colors for cells, headers, validation indicators, etc.
    /// The theme is applied both to the backend facade and the UI view model to keep them synchronized.
    /// </summary>
    /// <param name="theme">The theme configuration to apply</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation</param>
    /// <exception cref="ArgumentNullException">Thrown when theme is null</exception>
    public async Task ApplyThemeAsync(PublicGridTheme theme, CancellationToken cancellationToken = default)
    {
        if (theme == null) throw new ArgumentNullException(nameof(theme));

        _logger?.LogInformation("Applying theme: {ThemeName}", theme.ThemeName ?? "unnamed");

        // Apply to facade (backend)
        await _facade.ApplyThemeAsync(theme);

        // Apply to UI ViewModel (visual)
        _viewModel.Theme.ApplyTheme(theme);

        _logger?.LogInformation("Theme applied successfully");
    }

    /// <summary>
    /// Updates only the cell colors without changing the entire theme.
    /// This is useful for fine-tuning cell appearance without affecting other grid elements.
    /// </summary>
    /// <param name="cellColors">The cell color configuration to apply</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation</param>
    /// <exception cref="ArgumentNullException">Thrown when cellColors is null</exception>
    public async Task UpdateCellColorsAsync(PublicCellColors cellColors, CancellationToken cancellationToken = default)
    {
        if (cellColors == null) throw new ArgumentNullException(nameof(cellColors));

        _logger?.LogInformation("Updating cell colors");

        // Apply to facade
        await _facade.UpdateCellColorsAsync(cellColors);

        // Apply to UI ViewModel
        _viewModel.Theme.UpdateCellColors(cellColors);

        _logger?.LogInformation("Cell colors updated successfully");
    }

    /// <summary>
    /// Updates only the row colors without changing the entire theme.
    /// This affects row backgrounds (even/odd alternating rows, selected rows, etc.).
    /// </summary>
    /// <param name="rowColors">The row color configuration to apply</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation</param>
    /// <exception cref="ArgumentNullException">Thrown when rowColors is null</exception>
    public async Task UpdateRowColorsAsync(PublicRowColors rowColors, CancellationToken cancellationToken = default)
    {
        if (rowColors == null) throw new ArgumentNullException(nameof(rowColors));

        _logger?.LogInformation("Updating row colors");

        // Apply to facade
        await _facade.UpdateRowColorsAsync(rowColors);

        // Apply to UI ViewModel
        _viewModel.Theme.UpdateRowColors(rowColors);

        _logger?.LogInformation("Row colors updated successfully");
    }

    /// <summary>
    /// Updates only the validation colors without changing the entire theme.
    /// This affects how validation errors and warnings are displayed in the grid.
    /// </summary>
    /// <param name="validationColors">The validation color configuration to apply</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation</param>
    /// <exception cref="ArgumentNullException">Thrown when validationColors is null</exception>
    public async Task UpdateValidationColorsAsync(PublicValidationColors validationColors, CancellationToken cancellationToken = default)
    {
        if (validationColors == null) throw new ArgumentNullException(nameof(validationColors));

        _logger?.LogInformation("Updating validation colors");

        // Apply to facade
        await _facade.UpdateValidationColorsAsync(validationColors);

        // Apply to UI ViewModel
        _viewModel.Theme.UpdateValidationColors(validationColors);

        _logger?.LogInformation("Validation colors updated successfully");
    }

    /// <summary>
    /// Gets all currently selected cells from the grid.
    /// Useful for operations like copy/paste or bulk editing of selected cells.
    /// </summary>
    /// <returns>List of selected cell view models</returns>
    public List<CellViewModel> GetSelectedCells()
    {
        var selectedCells = _viewModel.GetSelectedCells();
        _logger?.LogInformation("Retrieved {Count} selected cells", selectedCells.Count);
        return selectedCells;
    }

    /// <summary>
    /// Clears all cell selections in the grid.
    /// After calling this, no cells will be highlighted as selected.
    /// </summary>
    public void ClearSelections()
    {
        _logger?.LogInformation("Clearing all selections");
        _viewModel.ClearAllSelections();
    }

    /// <summary>
    /// Clears all data from the grid, removing all rows, columns, and filters.
    /// Use this to reset the grid to an empty state.
    /// </summary>
    public void Clear()
    {
        _logger?.LogInformation("Clearing all grid data");
        _control.Clear();
    }

    /// <summary>
    /// Disposes of the facade UI wrapper and its associated resources.
    /// This will also dispose the underlying facade API.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _logger?.LogInformation("Disposing AdvancedDataGridFacadeUI");
        await _facade.DisposeAsync();
    }
}

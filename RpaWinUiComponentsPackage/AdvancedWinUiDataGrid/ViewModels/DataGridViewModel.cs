using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// Main ViewModel for the entire AdvancedDataGrid control.
/// This is the central orchestrator that manages all sub-ViewModels (search, filter, headers, rows).
/// Think of this as the "brain" of the grid - it coordinates everything.
/// </summary>
public sealed class DataGridViewModel : ViewModelBase
{
    private readonly ILogger<DataGridViewModel>? _logger;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
    private bool _isSearchPanelVisible = true;
    private bool _isFilterRowVisible = true;

    // MEMORY LEAK FIX: Track event handlers to properly unsubscribe them
    private readonly List<(ColumnHeaderViewModel header, PropertyChangedEventHandler handler)> _headerHandlers = new();

    // PERFORMANCE FIX #1: Debounce/Throttle resize events to prevent excessive UI rebuilds
    private System.Threading.Timer? _columnResizeThrottle;

    /// <summary>
    /// ViewModel for the search panel (contains search text, case sensitivity, etc.)
    /// </summary>
    public SearchPanelViewModel SearchPanel { get; } = new();

    /// <summary>
    /// ViewModel for the filter row (contains filter TextBoxes for each column)
    /// </summary>
    public FilterRowViewModel FilterRow { get; } = new();

    /// <summary>
    /// Theme manager that controls all colors in the grid (cells, headers, validation, etc.)
    /// </summary>
    public ThemeManager Theme { get; } = new();

    /// <summary>
    /// Collection of column headers (one per column in the grid)
    /// </summary>
    public ObservableCollection<ColumnHeaderViewModel> ColumnHeaders { get; } = new();

    /// <summary>
    /// Collection of data rows (each row contains cells)
    /// Uses BulkObservableCollection for efficient bulk operations with large datasets
    /// </summary>
    public BulkObservableCollection<DataGridRowViewModel> Rows { get; } = new();

    /// <summary>
    /// Creates a new instance of the DataGridViewModel.
    /// This is the main view model that manages all grid state including columns, rows, filters, and search.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics and troubleshooting</param>
    /// <param name="dispatcherQueue">Optional DispatcherQueue for UI thread marshalling (required for resize operations)</param>
    public DataGridViewModel(ILogger<DataGridViewModel>? logger = null, Microsoft.UI.Dispatching.DispatcherQueue? dispatcherQueue = null)
    {
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
        _logger?.LogInformation("DataGridViewModel created");
    }

    /// <summary>
    /// Event fired when column definitions need to be updated in UI.
    /// UI controls should subscribe to this to rebuild their Grid.ColumnDefinitions.
    /// This ensures column widths stay synchronized across headers, filters, and data cells.
    /// </summary>
    public event EventHandler? ColumnDefinitionsChanged;

    // Selection state - tracks the current cell selection for multi-select and range selection
    private CellViewModel? _lastSelectedCell;
    private bool _isRangeSelecting;
    private CellViewModel? _rangeStartCell;

    /// <summary>
    /// Gets or sets whether the search panel is visible.
    /// When visibility changes, it also updates the "search in filtered only" button visibility.
    /// </summary>
    public bool IsSearchPanelVisible
    {
        get => _isSearchPanelVisible;
        set
        {
            if (SetProperty(ref _isSearchPanelVisible, value))
            {
                SearchPanel.IsVisible = value;
                UpdateSearchInFilteredOnlyButtonVisibility();
                _logger?.LogInformation("Search panel visibility changed to {Visible}", value);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the filter row is visible.
    /// When visibility changes, it also updates the "search in filtered only" button visibility.
    /// </summary>
    public bool IsFilterRowVisible
    {
        get => _isFilterRowVisible;
        set
        {
            if (SetProperty(ref _isFilterRowVisible, value))
            {
                FilterRow.IsVisible = value;
                UpdateSearchInFilteredOnlyButtonVisibility();
                _logger?.LogInformation("Filter row visibility changed to {Visible}", value);
            }
        }
    }

    /// <summary>
    /// Shows the "search in filtered only" button only when both search and filter are enabled.
    /// This makes sense because you can only search in filtered results if filtering is available.
    /// </summary>
    private void UpdateSearchInFilteredOnlyButtonVisibility()
    {
        SearchPanel.ShowSearchInFilteredOnlyButton = IsSearchPanelVisible && IsFilterRowVisible;
    }

    /// <summary>
    /// Initializes columns from the provided column names with support for special columns.
    /// Creates headers and filter inputs for each column, and sets up width synchronization.
    /// Special columns (RowNumber, Checkbox, ValidationAlerts, DeleteRow) are added based on options.
    /// MEMORY LEAK FIX: Properly unsubscribes old event handlers before creating new ones.
    /// </summary>
    /// <param name="columnNames">Names of the data columns to initialize</param>
    /// <param name="options">Grid options containing special column configuration (optional)</param>
    public void InitializeColumns(IEnumerable<string> columnNames, AdvancedDataGridOptions? options = null)
    {
        var columnList = columnNames.ToList();
        _logger?.LogInformation("Initializing {Count} data columns with special columns support", columnList.Count);

        // MEMORY LEAK FIX: Unsubscribe old event handlers BEFORE clearing collections
        foreach (var (header, handler) in _headerHandlers)
        {
            header.PropertyChanged -= handler;
        }
        _headerHandlers.Clear();

        ColumnHeaders.Clear();
        FilterRow.ColumnFilters.Clear();

        int displayOrder = 0;

        // 1. ROW NUMBER COLUMN (if enabled)
        if (options?.EnableRowNumberColumn == true)
        {
            var rowNumHeader = CreateSpecialColumnHeader(
                name: "rowNumber",
                displayName: "#",
                specialType: SpecialColumnType.RowNumber,
                width: 60,
                isResizable: false,
                displayOrder: displayOrder++
            );
            ColumnHeaders.Add(rowNumHeader);
            _logger?.LogInformation("Added RowNumber special column");
            // NO FILTER for RowNumber
        }

        // 2. CHECKBOX COLUMN (if enabled)
        if (options?.EnableCheckboxColumn == true)
        {
            var checkboxHeader = CreateSpecialColumnHeader(
                name: "checkbox",
                displayName: "☑",
                specialType: SpecialColumnType.Checkbox,
                width: 40,
                isResizable: false,
                displayOrder: displayOrder++
            );
            ColumnHeaders.Add(checkboxHeader);
            _logger?.LogInformation("Added Checkbox special column");
            // NO FILTER for Checkbox
        }

        // 3. DATA COLUMNS (all user columns)
        foreach (var columnName in columnList)
        {
            var header = new ColumnHeaderViewModel
            {
                ColumnName = columnName,
                DisplayName = columnName,
                Width = 120,
                IsResizable = true,
                SpecialType = SpecialColumnType.None,
                DisplayOrder = displayOrder++
            };

            // MEMORY LEAK FIX: Store handler reference for later cleanup
            // Subscribe to Width changes to keep filters and cells synchronized
            PropertyChangedEventHandler handler = (s, e) =>
            {
                if (e.PropertyName == nameof(ColumnHeaderViewModel.Width))
                {
                    SyncColumnWidth(header.ColumnName, header.Width);
                }
            };
            header.PropertyChanged += handler;
            _headerHandlers.Add((header, handler));

            ColumnHeaders.Add(header);

            // Add filter for data columns
            var filter = new ColumnFilterViewModel
            {
                ColumnName = columnName,
                Width = 120
            };
            FilterRow.ColumnFilters.Add(filter);
        }

        // 4. VALIDATION ALERTS COLUMN (if enabled)
        if (options?.EnableValidationAlertsColumn == true)
        {
            var validAlertsHeader = CreateSpecialColumnHeader(
                name: "validAlerts",
                displayName: "⚠ Validation",
                specialType: SpecialColumnType.ValidationAlerts,
                width: options.ValidationAlertsColumnMinWidth,
                isResizable: true,
                displayOrder: displayOrder++
            );
            ColumnHeaders.Add(validAlertsHeader);
            _logger?.LogInformation("Added ValidationAlerts special column");
            // NO FILTER for ValidationAlerts
        }

        // 5. DELETE ROW COLUMN (if enabled)
        if (options?.EnableDeleteRowColumn == true)
        {
            var deleteHeader = CreateSpecialColumnHeader(
                name: "deleteRow",
                displayName: "🗑",
                specialType: SpecialColumnType.DeleteRow,
                width: 80,
                isResizable: false,
                displayOrder: displayOrder++
            );
            ColumnHeaders.Add(deleteHeader);
            _logger?.LogInformation("Added DeleteRow special column");
            // NO FILTER for DeleteRow
        }

        _logger?.LogInformation("Columns initialized successfully: {Total} total ({Special} special, {Data} data)",
            ColumnHeaders.Count,
            ColumnHeaders.Count(h => h.IsSpecialColumn),
            columnList.Count);
    }

    /// <summary>
    /// Creates a special column header with specified properties
    /// </summary>
    private ColumnHeaderViewModel CreateSpecialColumnHeader(
        string name,
        string displayName,
        SpecialColumnType specialType,
        double width,
        bool isResizable,
        int displayOrder)
    {
        return new ColumnHeaderViewModel
        {
            ColumnName = name,
            DisplayName = displayName,
            Width = width,
            IsResizable = isResizable,
            SpecialType = specialType,
            DisplayOrder = displayOrder
        };
    }

    /// <summary>
    /// Creates Grid ColumnDefinitions from current ColumnHeaders
    /// This should be called by UI controls to synchronize their Grid layouts
    /// ValidationAlerts column uses Star sizing to fill remaining space
    /// </summary>
    /// <returns>List of ColumnDefinition with widths from ColumnHeaders</returns>
    public List<ColumnDefinition> CreateColumnDefinitions()
    {
        var definitions = new List<ColumnDefinition>();
        foreach (var header in ColumnHeaders)
        {
            // ValidationAlerts stĺpec má vyplniť medzeru medzi dátovými stĺpcami a delete stĺpcom
            if (header.SpecialType == SpecialColumnType.ValidationAlerts)
            {
                definitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star), // Vyplní zostávajúci priestor
                    MinWidth = header.Width // Minimálna šírka z nastavení
                });
            }
            else
            {
                definitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(header.Width, GridUnitType.Pixel)
                });
            }
        }
        return definitions;
    }

    /// <summary>
    /// Synchronizes column width across header, filter, and all cells in that column
    /// Fires ColumnDefinitionsChanged event to notify UI controls to rebuild their layouts
    /// PERFORMANCE FIX #1: Uses debounce/throttle to prevent excessive UI rebuilds during resize
    /// CRITICAL FIX: Dispatches event to UI thread to prevent COMException
    /// </summary>
    /// <param name="columnName">Name of the column</param>
    /// <param name="newWidth">New width to apply</param>
    public void SyncColumnWidth(string columnName, double newWidth)
    {
        // Update filter width immediately (lightweight operation)
        var filter = FilterRow.ColumnFilters.FirstOrDefault(f => f.ColumnName == columnName);
        if (filter != null)
        {
            filter.Width = newWidth;
        }

        // PERFORMANCE FIX #1: Debounce UI notification - only notify AFTER resize is complete
        // This prevents rebuilding ALL views (HeadersRowView, DataGridCellsView, FilterRowView) on every mouse move
        _columnResizeThrottle?.Dispose();
        _columnResizeThrottle = new System.Threading.Timer(_ =>
        {
            // CRITICAL FIX: Dispatch to UI thread to prevent COMException
            // Timer callback runs on background thread, but ColumnDefinitions.Clear() must run on UI thread
            if (_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    ColumnDefinitionsChanged?.Invoke(this, EventArgs.Empty);
                });
            }
            else
            {
                // Fallback if no dispatcher available (should not happen in normal usage)
                ColumnDefinitionsChanged?.Invoke(this, EventArgs.Empty);
            }
        }, null, 150, System.Threading.Timeout.Infinite); // 150ms debounce delay
    }

    /// <summary>
    /// Loads data rows into the grid with support for special columns.
    /// Creates a CellViewModel for each cell, linking it to the theme manager for visual styling.
    /// Special column cells are populated with computed values (RowNumber, Checkbox state, etc.).
    /// If a column value is missing in a row, the cell will have a null value.
    /// PERFORMANCE: Uses BulkObservableCollection.AddRange for efficient bulk loading.
    /// </summary>
    /// <param name="rowsData">Collection of rows to load, where each row is a dictionary of column name to value</param>
    public void LoadRows(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData)
    {
        var dataList = rowsData.ToList();
        _logger?.LogInformation("Loading {RowCount} rows into grid with special columns support", dataList.Count);

        Rows.Clear();

        // PERFORMANCE: Build all rows first, then add in bulk with single notification
        var rowViewModels = new List<DataGridRowViewModel>(dataList.Count);

        int rowIndex = 0;
        foreach (var rowData in dataList)
        {
            // Create a new row view model
            var rowVm = new DataGridRowViewModel
            {
                RowIndex = rowIndex
            };

            // Create a cell for each column (special + data)
            for (int colIndex = 0; colIndex < ColumnHeaders.Count; colIndex++)
            {
                var header = ColumnHeaders[colIndex];
                var cellVm = new CellViewModel(Theme) // Pass ThemeManager to cell for theme-aware colors
                {
                    RowIndex = rowIndex,
                    ColumnIndex = colIndex,
                    ColumnName = header.ColumnName,
                    SpecialType = header.SpecialType,
                    IsReadOnly = header.IsSpecialColumn // Special columns are read-only (except checkbox)
                };

                // Populate cell value based on column type
                if (header.SpecialType == SpecialColumnType.RowNumber)
                {
                    // RowNumber - computed from rowIndex (1-based)
                    cellVm.Value = rowIndex + 1;
                }
                else if (header.SpecialType == SpecialColumnType.Checkbox)
                {
                    // Checkbox - default unchecked
                    cellVm.IsRowSelected = false;
                    cellVm.Value = null; // No text value
                }
                else if (header.SpecialType == SpecialColumnType.ValidationAlerts)
                {
                    // ValidationAlerts - will be populated later via validation system
                    cellVm.ValidationAlertMessage = null; // TODO: Populate from validation results
                    cellVm.Value = null;
                }
                else if (header.SpecialType == SpecialColumnType.DeleteRow)
                {
                    // DeleteRow - no value, just button rendered by UI
                    cellVm.Value = null;
                }
                else
                {
                    // Normal data column - get value from row data
                    cellVm.Value = rowData.TryGetValue(header.ColumnName, out var value) ? value : null;
                }

                rowVm.Cells.Add(cellVm);
            }

            rowViewModels.Add(rowVm);
            rowIndex++;
        }

        // CRITICAL PERFORMANCE: Use AddRange instead of individual Add() calls
        // For 10M rows: 10M events → 1 event = MASSIVE speedup
        Rows.AddRange(rowViewModels);

        _logger?.LogInformation("Rows loaded successfully with {SpecialCount} special columns per row",
            ColumnHeaders.Count(h => h.IsSpecialColumn));
    }

    /// <summary>
    /// Clears all data from the grid, including rows, columns, filters, and search criteria.
    /// This resets the grid to a completely empty state.
    /// MEMORY LEAK FIX: Properly unsubscribes all event handlers before clearing.
    /// </summary>
    public void Clear()
    {
        _logger?.LogInformation("Clearing all grid data");

        // MEMORY LEAK FIX: Unsubscribe all event handlers BEFORE clearing
        foreach (var (header, handler) in _headerHandlers)
        {
            header.PropertyChanged -= handler;
        }
        _headerHandlers.Clear();

        Rows.Clear();
        ColumnHeaders.Clear();
        FilterRow.ColumnFilters.Clear();
        SearchPanel.ClearSearch();

        _logger?.LogInformation("Grid data cleared");
    }

    #region Cell Selection

    /// <summary>
    /// Handles single cell selection with support for multi-select using Ctrl key.
    /// When Ctrl is not pressed, clears all selections and selects only the clicked cell.
    /// When Ctrl is pressed, toggles the clicked cell without affecting other selections.
    /// </summary>
    /// <param name="cell">The cell that was clicked</param>
    /// <param name="isCtrlPressed">Whether the Ctrl key was held during the click</param>
    public void SelectCell(CellViewModel cell, bool isCtrlPressed)
    {
        if (cell == null) return;

        if (isCtrlPressed)
        {
            // Multi-selection mode: toggle this cell (add/remove from selection)
            cell.IsSelected = !cell.IsSelected;
            _lastSelectedCell = cell;
            _logger?.LogInformation("Cell toggled at [{Row}, {Col}], now {Selected}",
                cell.RowIndex, cell.ColumnIndex, cell.IsSelected ? "selected" : "deselected");
        }
        else
        {
            // Single selection mode: clear all and select this one
            ClearAllSelections();
            cell.IsSelected = true;
            _lastSelectedCell = cell;
            _logger?.LogInformation("Cell selected at [{Row}, {Col}]", cell.RowIndex, cell.ColumnIndex);
        }
    }

    /// <summary>
    /// Starts a range selection operation, typically initiated by clicking and dragging.
    /// Clears any existing selections and marks the starting cell as selected.
    /// </summary>
    /// <param name="startCell">The cell where the range selection begins</param>
    public void StartRangeSelection(CellViewModel startCell)
    {
        if (startCell == null) return;

        _isRangeSelecting = true;
        _rangeStartCell = startCell;

        // Clear previous selections before starting new range
        ClearAllSelections();

        // Select the start cell
        startCell.IsSelected = true;

        _logger?.LogInformation("Range selection started at [{Row}, {Col}]",
            startCell.RowIndex, startCell.ColumnIndex);
    }

    /// <summary>
    /// Updates the range selection as the user drags to a different cell.
    /// Selects all cells in the rectangular area between the start cell and current cell.
    /// </summary>
    /// <param name="currentCell">The cell currently being hovered during the drag</param>
    public void UpdateRangeSelection(CellViewModel currentCell)
    {
        if (!_isRangeSelecting || _rangeStartCell == null || currentCell == null) return;

        // Clear all selections first to redraw the selection range
        ClearAllSelections();

        // Calculate range boundaries (handles dragging in any direction)
        int startRow = Math.Min(_rangeStartCell.RowIndex, currentCell.RowIndex);
        int endRow = Math.Max(_rangeStartCell.RowIndex, currentCell.RowIndex);
        int startCol = Math.Min(_rangeStartCell.ColumnIndex, currentCell.ColumnIndex);
        int endCol = Math.Max(_rangeStartCell.ColumnIndex, currentCell.ColumnIndex);

        // Select all cells in the rectangular range
        for (int row = startRow; row <= endRow; row++)
        {
            if (row >= Rows.Count) break;

            var rowVm = Rows[row];
            for (int col = startCol; col <= endCol; col++)
            {
                if (col >= rowVm.Cells.Count) break;

                rowVm.Cells[col].IsSelected = true;
            }
        }

        // Don't log here as this is called repeatedly during drag (performance)
    }

    /// <summary>
    /// Ends the range selection operation, typically when the mouse button is released.
    /// </summary>
    public void EndRangeSelection()
    {
        if (_isRangeSelecting)
        {
            var selectedCount = GetSelectedCells().Count;
            _logger?.LogInformation("Range selection ended, {Count} cells selected", selectedCount);
        }

        _isRangeSelecting = false;
        _rangeStartCell = null;
    }

    /// <summary>
    /// Clears all cell selections in the grid.
    /// After calling this, no cells will be highlighted as selected.
    /// </summary>
    public void ClearAllSelections()
    {
        // Don't log here as this is called frequently and would create log noise
        foreach (var row in Rows)
        {
            foreach (var cell in row.Cells)
            {
                cell.IsSelected = false;
            }
        }
    }

    /// <summary>
    /// Gets all currently selected cells in the grid.
    /// Useful for operations like copy/paste or bulk editing.
    /// </summary>
    /// <returns>List of selected cell view models</returns>
    public List<CellViewModel> GetSelectedCells()
    {
        var selected = new List<CellViewModel>();
        foreach (var row in Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.IsSelected)
                {
                    selected.Add(cell);
                }
            }
        }
        return selected;
    }

    #endregion
}

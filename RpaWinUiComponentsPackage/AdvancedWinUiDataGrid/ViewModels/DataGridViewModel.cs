using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// Main ViewModel for the entire AdvancedDataGrid control.
/// This is the central orchestrator that manages all sub-ViewModels (search, filter, headers, rows).
/// Think of this as the "brain" of the grid - it coordinates everything.
/// </summary>
public sealed class DataGridViewModel : ViewModelBase
{
    private readonly ILogger<DataGridViewModel>? _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
    private readonly Features.Pagination.Interfaces.IPageManager? _pageManager;
    private bool _isSearchPanelVisible = true;
    private bool _isFilterRowVisible = true;

    // MEMORY LEAK FIX: Track event handlers to properly unsubscribe them
    private readonly List<(ColumnHeaderViewModel header, PropertyChangedEventHandler handler)> _headerHandlers = new();

    // PERFORMANCE CACHE: O(1) RowID→Index lookup for efficient RemoveRowsById operations
    private readonly Dictionary<string, int> _rowIdToIndexCache = new();
    private bool _cacheNeedsRebuild = true;

    /// <summary>
    /// ViewModel for the search panel (contains search text, case sensitivity, etc.)
    /// </summary>
    public SearchPanelViewModel SearchPanel { get; } = new();

    /// <summary>
    /// ViewModel for the filter row (contains filter TextBoxes for each column)
    /// </summary>
    public FilterRowViewModel FilterRow { get; } = new();

    /// <summary>
    /// ViewModel for the pagination panel (page navigation, page size, total counts)
    /// </summary>
    public PaginationPanelViewModel PaginationPanel { get; } = new();

    /// <summary>
    /// Theme manager that controls all colors in the grid (cells, headers, validation, etc.)
    /// </summary>
    public ThemeManager Theme { get; }

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
    /// SENIOR FIX: ViewportManager reference for cache invalidation on data reload
    /// Set by DataGridCellsView after creating ViewportManager instance
    /// </summary>
    internal Features.Viewport.ViewportManager? ViewportManager { get; set; }

    /// <summary>
    /// Creates a new instance of the DataGridViewModel.
    /// This is the main view model that manages all grid state including columns, rows, filters, and search.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics and troubleshooting</param>
    /// <param name="loggerFactory">Optional logger factory for creating child component loggers (CellViewModel, etc.)</param>
    /// <param name="dispatcherQueue">Optional DispatcherQueue for UI thread marshalling (required for resize operations)</param>
    /// <param name="themeManager">Optional theme manager for color management (if not provided, creates default instance)</param>
    public DataGridViewModel(
        ILogger<DataGridViewModel>? logger = null,
        ILoggerFactory? loggerFactory = null,
        Microsoft.UI.Dispatching.DispatcherQueue? dispatcherQueue = null,
        ThemeManager? themeManager = null,
        Features.Pagination.Interfaces.IPageManager? pageManager = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _dispatcherQueue = dispatcherQueue;
        _pageManager = pageManager;
        Theme = themeManager ?? new ThemeManager(logger: null); // Fallback to default if not provided

        // Wire up PageManager to PaginationPanelViewModel if provided
        if (_pageManager != null)
        {
            // Sync PageManager changes to PaginationPanel
            _pageManager.PageChanged += OnPageManagerPageChanged;
            _pageManager.PageSizeChanged += OnPageManagerPageSizeChanged;

            // Sync PaginationPanel changes to PageManager
            PaginationPanel.PageChanged += OnPaginationPanelPageChanged;

            _logger?.LogInformation("DataGridViewModel created with PageManager integration");
        }
        else
        {
            _logger?.LogInformation("DataGridViewModel created without PageManager");
        }
    }

    /// <summary>
    /// Event fired when column definitions need to be updated in UI.
    /// UI controls should subscribe to this to rebuild their Grid.ColumnDefinitions.
    /// This ensures column widths stay synchronized across headers, filters, and data cells.
    /// </summary>
    public event EventHandler? ColumnDefinitionsChanged;

    /// <summary>
    /// Event fired when user clicks column header to request sorting (FÁZA 5)
    /// Facade should subscribe to this event and call SortService.SortByColumnAsync()
    /// </summary>
    public event EventHandler<SortRequestedEventArgs>? SortRequested;

    /// <summary>
    /// Event fired when user requests to insert new row via context menu (FÁZA 4)
    /// Facade should subscribe to this event and call IRowStore.InsertRowAfterAsync() or InsertRowBeforeAsync()
    /// </summary>
    public event EventHandler<InsertRowRequestedEventArgs>? InsertRowRequested;

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
    /// MEMORY LEAK FIX: Disposes collection of DataGridRowViewModels.
    /// CRITICAL: Must be called before Rows.Clear() or when removing rows.
    /// Each DataGridRowViewModel contains ~10 CellViewModels → 1000+ objects for 100 rows.
    /// </summary>
    private void DisposeRemovedRows(IEnumerable<DataGridRowViewModel> rowsToDispose)
    {
        var disposedCount = 0;
        foreach (var row in rowsToDispose)
        {
            row.Dispose(); // ← Disposes all CellViewModels in row (10+ cells per row)
            disposedCount++;
        }

        if (disposedCount > 0)
        {
            _logger?.LogInformation("MEMORY: Disposed {Count} DataGridRowViewModels " +
                "(freed ~{EstimatedCells} CellViewModels)",
                disposedCount, disposedCount * 10);
        }
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
        // CRITICAL: Filter out __rowId (double underscore prefix) - internal implementation detail
        // User columns named "rowId" or "RowId" (without __ prefix) are treated as normal data columns
        // Note: __rowId is added automatically by the internal system and must be hidden from UI
        var allColumns = columnNames.ToList();
        var hasInternalRowId = allColumns.Contains("__rowId");
        var columnList = allColumns.Where(c => c != "__rowId").ToList();

        if (hasInternalRowId)
        {
            _logger?.LogDebug("Internal __rowId column detected and filtered from UI display (expected system behavior)");
        }

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

        // 5. INSERT ROW COLUMN (if enabled) - Add row button
        if (options?.EnableInsertRowColumn == true)
        {
            var insertHeader = CreateSpecialColumnHeader(
                name: "insertRow",
                displayName: "➕",
                specialType: SpecialColumnType.InsertRow,
                width: 60,
                isResizable: false,
                displayOrder: displayOrder++
            );
            ColumnHeaders.Add(insertHeader);
            _logger?.LogInformation("Added InsertRow special column");
            // NO FILTER for InsertRow
        }

        // 6. DELETE ROW COLUMN (if enabled)
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
            DisplayOrder = displayOrder,
            // CRITICAL: ValidationAlerts column uses auto-width (Star sizing) by default
            // After user manually resizes it, UseAutoWidth will be set to false
            UseAutoWidth = specialType == SpecialColumnType.ValidationAlerts
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
            // Check if column should use auto-width (Star sizing)
            // This is typically true for ValidationAlerts column UNTIL user manually resizes it
            if (header.UseAutoWidth)
            {
                definitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star), // Auto-expand to fill available space
                    MinWidth = header.Width // Minimum width from settings
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
    /// PERFORMANCE FIX: Uses realtime update with immediate UI rebuild for smooth resize
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

        // REALTIME UPDATE FIX: Trigger UI update immediately for smooth resize experience
        // CRITICAL FIX: Dispatch to UI thread to prevent COMException
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
    }

    #region Performance Cache Management

    /// <summary>
    /// Ensures RowID→Index cache is valid. Rebuilds cache if dirty.
    /// PERFORMANCE: O(n) rebuild cost, but only when cache is invalid.
    /// After rebuild, all lookups are O(1).
    /// </summary>
    private void EnsureCacheIsValid()
    {
        if (!_cacheNeedsRebuild)
        {
            return; // Cache is fresh, no work needed
        }

        _rowIdToIndexCache.Clear();

        for (int i = 0; i < Rows.Count; i++)
        {
            var rowId = Rows[i].Cells.FirstOrDefault()?.RowId;
            if (!string.IsNullOrEmpty(rowId))
            {
                _rowIdToIndexCache[rowId] = i;
            }
        }

        _cacheNeedsRebuild = false;
        _logger?.LogDebug("RowID→Index cache rebuilt with {Count} entries", _rowIdToIndexCache.Count);
    }

    /// <summary>
    /// Invalidates cache after data changes (sort, filter, delete, load).
    /// Next RowID operation will trigger rebuild via EnsureCacheIsValid().
    /// </summary>
    private void InvalidateCache()
    {
        _cacheNeedsRebuild = true;
        _logger?.LogDebug("RowID→Index cache invalidated");
    }

    /// <summary>
    /// Public method to invalidate RowID→Index cache.
    /// Called by InternalUIUpdateHandler after Virtual Insert/Delete operations.
    /// </summary>
    public void InvalidateRowIdCache()
    {
        InvalidateCache();
    }

    /// <summary>
    /// Finds row index by RowID using O(1) cache lookup.
    /// PUBLIC API: Used by wrappers (DataGridRows, DataGridSelection, etc.)
    /// </summary>
    /// <param name="rowId">Unique stable row identifier</param>
    /// <returns>Current row index or null if not found</returns>
    public int? FindRowIndexByRowId(string rowId)
    {
        if (string.IsNullOrEmpty(rowId))
        {
            return null;
        }

        EnsureCacheIsValid();

        return _rowIdToIndexCache.TryGetValue(rowId, out var index) ? index : null;
    }

    #endregion

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

        // ✅ MEMORY LEAK FIX: Dispose old ViewModels before clearing collection
        if (Rows.Any())
        {
            var oldRows = Rows.ToList(); // Copy before clear
            Rows.Clear();
            DisposeRemovedRows(oldRows); // ← FREE MEMORY
        }
        else
        {
            Rows.Clear(); // First time, no need to dispose
        }

        // SENIOR FIX: Invalidate ViewportManager cache to prevent "Loading..." bug on reload
        // ViewportManager cache holds ViewModels pointing to old disposed rows
        // Must invalidate cache AFTER Rows.Clear() to force fresh ViewModel creation
        ViewportManager?.InvalidateCache();

        // PERFORMANCE: Build all rows first, then add in bulk with single notification
        var rowViewModels = new List<DataGridRowViewModel>(dataList.Count);

        int rowIndex = 0;
        foreach (var rowData in dataList)
        {
            // Extract unique row ID from data (stored in __rowId field by InMemoryRowStore)
            string? rowId = null;
            if (rowData.TryGetValue("__rowId", out var rowIdValue))
            {
                rowId = rowIdValue?.ToString();
            }

            // Create a new row view model
            var rowVm = new DataGridRowViewModel
            {
                RowIndex = rowIndex,
                RowId = rowId // CRITICAL: Store stable row ID for row-based operations
            };

            // Create a cell for each column (special + data)
            for (int colIndex = 0; colIndex < ColumnHeaders.Count; colIndex++)
            {
                var header = ColumnHeaders[colIndex];
                var cellVm = new CellViewModel(Theme, _loggerFactory?.CreateLogger<CellViewModel>()) // Pass ThemeManager and logger to cell
                {
                    RowIndex = rowIndex,
                    RowId = rowId, // CRITICAL: Store stable row ID for delete operations
                    ColumnIndex = colIndex,
                    ColumnName = header.ColumnName,
                    SpecialType = header.SpecialType,
                    IsReadOnly = header.IsSpecialColumn // Special columns are read-only (except checkbox)
                };

                // ✅ CRITICAL FIX: Set parent row for checkbox synchronization
                cellVm.SetParentRow(rowVm);

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

        // Invalidate cache after loading new data
        InvalidateCache();

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

    #region Incremental Update Methods (Performance Optimization)

    /// <summary>
    /// Removes rows by their unique RowIDs using O(1) cache lookup.
    /// PUBLIC API: This is the preferred method for row removal operations.
    /// PERFORMANCE: Cache lookup O(1), then delegates to RemoveRowsAtIndices().
    /// STABILITY: RowID never changes, safe to use after sort/filter operations.
    /// </summary>
    /// <param name="rowIds">Collection of unique row identifiers to remove</param>
    public void RemoveRowsById(IReadOnlyList<string> rowIds)
    {
        if (rowIds == null || rowIds.Count == 0)
        {
            return;
        }

        _logger?.LogInformation("Removing {Count} rows by RowID", rowIds.Count);

        EnsureCacheIsValid();

        // Convert RowIDs to indices using O(1) cache lookup
        var indices = new List<int>(rowIds.Count);
        foreach (var rowId in rowIds)
        {
            if (_rowIdToIndexCache.TryGetValue(rowId, out var index))
            {
                indices.Add(index);
            }
            else
            {
                _logger?.LogWarning("RowID not found in cache: {RowId}", rowId);
            }
        }

        if (indices.Count == 0)
        {
            _logger?.LogWarning("No valid RowIDs found, nothing to remove");
            return;
        }

        // Delegate to private implementation that handles ObservableCollection.RemoveAt()
        RemoveRowsAtIndices(indices);
    }

    /// <summary>
    /// DEPRECATED: Removes rows at specified indices using incremental update.
    /// USE RemoveRowsById() instead for RowID-based operations.
    /// This method is kept for backward compatibility but will be removed in future versions.
    /// </summary>
    /// <param name="indices">Indices of rows to remove</param>
    [Obsolete("Use RemoveRowsById() instead. RowIndex-based operations are unstable after sort/filter.")]
    public void RemoveRowsAt(IReadOnlyList<int> indices)
    {
        RemoveRowsAtIndices(indices);
    }

    /// <summary>
    /// PRIVATE: Removes rows at specified indices using incremental update.
    /// PERFORMANCE: 10-50ms instead of 2-3s full reload.
    /// MEMORY: Reuses existing ViewModels instead of creating new ones.
    /// CRITICAL: Indices must be in DESCENDING order to avoid index shifting bugs.
    /// WHY PRIVATE: ObservableCollection.RemoveAt(index) requires index parameter.
    /// PUBLIC API USES: RemoveRowsById() which converts RowID→Index via cache.
    /// </summary>
    /// <param name="indices">Indices of rows to remove (will be sorted descending)</param>
    private void RemoveRowsAtIndices(IReadOnlyList<int> indices)
    {
        if (indices == null || indices.Count == 0)
        {
            return;
        }

        _logger?.LogInformation("Removing {Count} rows incrementally (indices: {Indices})",
            indices.Count, string.Join(", ", indices));

        // CRITICAL: Remove in DESCENDING order to avoid index shifting bugs
        // If indices are [1, 3, 5]:
        // - Remove 5 first → indices 1,3 still valid
        // - Remove 3 second → index 1 still valid
        // - Remove 1 last → all done
        var sortedIndices = indices.OrderByDescending(i => i).ToList();

        foreach (var index in sortedIndices)
        {
            if (index >= 0 && index < Rows.Count)
            {
                Rows.RemoveAt(index);
            }
            else
            {
                _logger?.LogWarning("Invalid row index {Index} (total rows: {Total})", index, Rows.Count);
            }
        }

        // Update row indices for all remaining rows
        for (int i = 0; i < Rows.Count; i++)
        {
            Rows[i].RowIndex = i;
            // Update RowNumber cells (if present)
            var rowNumberCell = Rows[i].Cells.FirstOrDefault(c => c.SpecialType == SpecialColumnType.RowNumber);
            if (rowNumberCell != null)
            {
                rowNumberCell.Value = i + 1; // 1-based row numbers
                rowNumberCell.RowIndex = i;
            }
            // Update RowIndex for all cells
            foreach (var cell in Rows[i].Cells)
            {
                cell.RowIndex = i;
            }
        }

        // Invalidate cache after removal (indices have changed)
        InvalidateCache();

        _logger?.LogInformation("Rows removed successfully, remaining: {Count}", Rows.Count);
    }

    /// <summary>
    /// Updates cell values for specified rows by RowID using O(1) cache lookup.
    /// PUBLIC API: This is the preferred method for row update operations.
    /// PERFORMANCE: Updates only changed cells instead of rebuilding entire grid.
    /// STABILITY: RowID never changes, safe to use after sort/filter operations.
    /// </summary>
    /// <param name="updates">Dictionary of RowID to new row data</param>
    public void UpdateRowsById(IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> updates)
    {
        if (updates == null || updates.Count == 0)
        {
            return;
        }

        _logger?.LogInformation("Updating {Count} rows by RowID", updates.Count);

        EnsureCacheIsValid();

        foreach (var (rowId, newRowData) in updates)
        {
            if (!_rowIdToIndexCache.TryGetValue(rowId, out var rowIndex))
            {
                _logger?.LogWarning("RowID not found in cache: {RowId}", rowId);
                continue;
            }

            if (rowIndex < 0 || rowIndex >= Rows.Count)
            {
                _logger?.LogWarning("Invalid row index {Index} (total rows: {Total})", rowIndex, Rows.Count);
                continue;
            }

            var rowVm = Rows[rowIndex];

            // Update cell values for data columns (skip special columns)
            foreach (var cell in rowVm.Cells.Where(c => c.SpecialType == SpecialColumnType.None))
            {
                if (newRowData.TryGetValue(cell.ColumnName, out var newValue))
                {
                    cell.Value = newValue;
                }
            }
        }

        _logger?.LogInformation("Rows updated successfully");
    }

    /// <summary>
    /// DEPRECATED: Updates cell values for specified rows using incremental update.
    /// USE UpdateRowsById() instead for RowID-based operations.
    /// This method is kept for backward compatibility but will be removed in future versions.
    /// </summary>
    /// <param name="updates">Dictionary of row index to new row data</param>
    [Obsolete("Use UpdateRowsById() instead. RowIndex-based operations are unstable after sort/filter.")]
    public void UpdateRowsData(IReadOnlyDictionary<int, IReadOnlyDictionary<string, object?>> updates)
    {
        if (updates == null || updates.Count == 0)
        {
            return;
        }

        _logger?.LogInformation("Updating {Count} rows incrementally", updates.Count);

        foreach (var (rowIndex, newRowData) in updates)
        {
            if (rowIndex < 0 || rowIndex >= Rows.Count)
            {
                _logger?.LogWarning("Invalid row index {Index} (total rows: {Total})", rowIndex, Rows.Count);
                continue;
            }

            var rowVm = Rows[rowIndex];

            // CRITICAL FIX: Update rowId FIRST (before cell values)
            // This ensures RowViewModel.RowId is synchronized with backend after delete/shift operations
            if (newRowData.TryGetValue("__rowId", out var newRowId))
            {
                var rowIdStr = newRowId?.ToString();
                rowVm.RowId = rowIdStr; // Update RowViewModel RowId
                foreach (var cell in rowVm.Cells)
                {
                    cell.RowId = rowIdStr; // Update Cell RowId
                }
            }

            // Update cell values for data columns (skip special columns)
            foreach (var cell in rowVm.Cells.Where(c => c.SpecialType == SpecialColumnType.None))
            {
                if (newRowData.TryGetValue(cell.ColumnName, out var newValue))
                {
                    cell.Value = newValue;
                }
            }
        }

        _logger?.LogInformation("Rows updated successfully");
    }

    /// <summary>
    /// Clears content of specified rows by RowID using O(1) cache lookup.
    /// PUBLIC API: This is the preferred method for clearing row content.
    /// PERFORMANCE: Updates only affected cells instead of rebuilding entire grid.
    /// STABILITY: RowID never changes, safe to use after sort/filter operations.
    /// </summary>
    /// <param name="rowIds">Collection of unique row identifiers to clear</param>
    public void ClearRowsContentById(IReadOnlyList<string> rowIds)
    {
        if (rowIds == null || rowIds.Count == 0)
        {
            return;
        }

        _logger?.LogInformation("Clearing content of {Count} rows by RowID", rowIds.Count);

        EnsureCacheIsValid();

        foreach (var rowId in rowIds)
        {
            if (!_rowIdToIndexCache.TryGetValue(rowId, out var index))
            {
                _logger?.LogWarning("RowID not found in cache: {RowId}", rowId);
                continue;
            }

            if (index < 0 || index >= Rows.Count)
            {
                _logger?.LogWarning("Invalid row index {Index} (total rows: {Total})", index, Rows.Count);
                continue;
            }

            var rowVm = Rows[index];

            // Clear values for data columns (skip special columns)
            foreach (var cell in rowVm.Cells.Where(c => c.SpecialType == SpecialColumnType.None))
            {
                cell.Value = null;
            }
        }

        _logger?.LogInformation("Rows content cleared successfully");
    }

    /// <summary>
    /// DEPRECATED: Clears content of specified rows (sets all cell values to null).
    /// USE ClearRowsContentById() instead for RowID-based operations.
    /// This method is kept for backward compatibility but will be removed in future versions.
    /// </summary>
    /// <param name="indices">Indices of rows to clear</param>
    [Obsolete("Use ClearRowsContentById() instead. RowIndex-based operations are unstable after sort/filter.")]
    public void ClearRowsContent(IReadOnlyList<int> indices)
    {
        if (indices == null || indices.Count == 0)
        {
            return;
        }

        _logger?.LogInformation("Clearing content of {Count} rows incrementally", indices.Count);

        foreach (var index in indices)
        {
            if (index < 0 || index >= Rows.Count)
            {
                _logger?.LogWarning("Invalid row index {Index} (total rows: {Total})", index, Rows.Count);
                continue;
            }

            var rowVm = Rows[index];

            // Clear values for data columns (skip special columns)
            foreach (var cell in rowVm.Cells.Where(c => c.SpecialType == SpecialColumnType.None))
            {
                cell.Value = null;
            }
        }

        _logger?.LogInformation("Rows content cleared successfully");
    }

    #endregion

    #region Cell Selection

    /// <summary>
    /// Handles single cell selection with support for multi-select using Ctrl key.
    /// When Ctrl is not pressed, deselects all OTHER cells and selects only the clicked cell.
    /// When Ctrl is pressed, toggles the clicked cell without affecting other selections.
    /// SENIOR FIX: Preserves drag-and-drop range selection functionality (uses StartRangeSelection instead).
    /// </summary>
    /// <param name="cell">The cell that was clicked</param>
    /// <param name="isCtrlPressed">Whether the Ctrl key was held during the click</param>
    public void SelectCell(CellViewModel cell, bool isCtrlPressed)
    {
        if (cell == null) return;

        _logger?.LogTrace("DataGridViewModel: SelectCell called for [{Row},{Col}], Ctrl={IsCtrl}",
            cell.RowIndex, cell.ColumnIndex, isCtrlPressed);

        if (isCtrlPressed)
        {
            // Multi-selection mode: toggle this cell (add/remove from selection)
            var wasSelected = cell.IsSelected;
            cell.IsSelected = !cell.IsSelected;
            _lastSelectedCell = cell;

            _logger?.LogTrace("Multi-select TOGGLE: [{Row},{Col}] {Action}",
                cell.RowIndex, cell.ColumnIndex, wasSelected ? "deselected" : "selected");
            _logger?.LogInformation("Cell toggled at [{Row}, {Col}], now {Selected}",
                cell.RowIndex, cell.ColumnIndex, cell.IsSelected ? "selected" : "deselected");
        }
        else
        {
            // SENIOR FIX: Single-select mode - deselect all OTHER cells
            // Note: Range selection uses StartRangeSelection() instead, so this is safe
            // Optimization: Only iterate if we need to clear other selections
            if (!cell.IsSelected || GetSelectedCellsCount() > 1)
            {
                _logger?.LogTrace("Single-select: Deselecting all cells except [{Row},{Col}]",
                    cell.RowIndex, cell.ColumnIndex);

                // ✅ MEDIUM FIX: Deselect all cells except the clicked one
                // Skip special columns (they should not be selectable anyway)
                int deselectedCount = 0;
                foreach (var row in Rows)
                {
                    foreach (var c in row.Cells.Where(c => !c.IsSpecialColumn))
                    {
                        if (c != cell && c.IsSelected)
                        {
                            _logger?.LogTrace("DESELECT: [{Row},{Col}]", c.RowIndex, c.ColumnIndex);
                            c.IsSelected = false;
                            deselectedCount++;
                        }
                    }
                }

                _logger?.LogTrace("Deselected {Count} cells", deselectedCount);
            }
            else
            {
                _logger?.LogTrace("Cell already selected and only one selected - no deselection needed");
            }

            // ✅ Select the clicked cell
            cell.IsSelected = true;
            _lastSelectedCell = cell;

            _logger?.LogInformation("Cell selected at [{Row}, {Col}]", cell.RowIndex, cell.ColumnIndex);
        }

        // ARCHITECTURE CHANGE: No need to refresh viewport - uses canonical ViewModels
        // Selection changes are automatically visible in viewport
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

    /// <summary>
    /// SENIOR FIX: Gets count of currently selected cells in the grid.
    /// Used for optimization in SelectCell() to determine if we need to deselect other cells.
    /// </summary>
    /// <returns>Number of currently selected cells</returns>
    private int GetSelectedCellsCount()
    {
        int count = 0;
        foreach (var row in Rows)
        {
            count += row.Cells.Count(c => c.IsSelected);
        }
        return count;
    }

    /// <summary>
    /// Selects all rows in the grid by setting their checkbox column to checked.
    /// USE CASE: Header checkbox "Select All" clicked.
    /// </summary>
    public void SelectAllRows()
    {
        _logger?.LogInformation("Selecting all {Count} rows", Rows.Count);

        foreach (var row in Rows)
        {
            // Find checkbox cell and set it to selected
            var checkboxCell = row.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.Checkbox);
            if (checkboxCell != null)
            {
                checkboxCell.IsRowSelected = true;
            }
        }

        _logger?.LogInformation("All rows selected");

        // ✅ FIX: Update header checkbox state after selection change
        // QUALITY: Ensures header checkbox reflects actual row selection state
        UpdateCheckboxHeaderStatePublic();
    }

    /// <summary>
    /// Deselects all rows in the grid by setting their checkbox column to unchecked.
    /// USE CASE: Header checkbox "Deselect All" clicked.
    /// </summary>
    public void DeselectAllRows()
    {
        _logger?.LogInformation("Deselecting all {Count} rows", Rows.Count);

        foreach (var row in Rows)
        {
            // Find checkbox cell and set it to deselected
            var checkboxCell = row.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.Checkbox);
            if (checkboxCell != null)
            {
                checkboxCell.IsRowSelected = false;
            }
        }

        _logger?.LogInformation("All rows deselected");

        // ✅ FIX: Update header checkbox state after selection change
        // QUALITY: Ensures header checkbox reflects actual row selection state
        UpdateCheckboxHeaderStatePublic();
    }

    /// <summary>
    /// Updates checkbox column header state based on current row selection.
    /// Called automatically after any row selection change (SelectAll, DeselectAll, or individual row click).
    /// QUALITY: Implements indeterminate state (some checked), checked (all checked), unchecked (none checked).
    /// PUBLIC: Exposed for DataGridCellsView to call after individual row checkbox click.
    /// </summary>
    public void UpdateCheckboxHeaderStatePublic()
    {
        try
        {
            // Find checkbox column header
            var checkboxHeader = ColumnHeaders.FirstOrDefault(h => h.SpecialType == Common.SpecialColumnType.Checkbox);
            if (checkboxHeader == null)
            {
                return; // No checkbox column, skip
            }

            // Calculate selection state
            var selectedCount = Rows.Count(r => r.IsSelected);
            var totalCount = Rows.Count;

            bool? newState;
            if (selectedCount == 0)
            {
                // No rows selected → Unchecked (☐)
                newState = false;
            }
            else if (selectedCount == totalCount)
            {
                // All rows selected → Checked (✓)
                newState = true;
            }
            else
            {
                // Some rows selected → Indeterminate (■)
                newState = null;
            }

            // Update header checkbox state (binding will update UI automatically)
            if (checkboxHeader.IsCheckboxHeaderChecked != newState)
            {
                checkboxHeader.IsCheckboxHeaderChecked = newState;
                _logger?.LogDebug("Updated header checkbox state: {SelectedCount}/{TotalCount} rows selected → {State}",
                    selectedCount, totalCount, newState?.ToString() ?? "Indeterminate");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to update checkbox header state");
        }
    }

    #endregion

    #region Validation Visualization

    /// <summary>
    /// Applies validation errors to cell ViewModels for visual display (red borders).
    /// CRITICAL: This is the bridge between ValidationService (errors in store) and UI (red borders on cells).
    /// Should be called after validation completes to show validation results in UI.
    /// </summary>
    /// <param name="validationErrors">List of validation errors from validation service</param>
    internal void ApplyValidationErrors(IReadOnlyList<Common.Models.ValidationError> validationErrors)
    {
        _logger?.LogTrace("DataGridViewModel: ApplyValidationErrors called with {ErrorCount} errors", validationErrors?.Count ?? 0);

        if (validationErrors == null)
        {
            _logger?.LogWarning("ApplyValidationErrors called with null errors list");
            return;
        }

        _logger?.LogInformation("Applying {ErrorCount} validation errors to grid UI", validationErrors.Count);

        // Group errors by (RowId, ColumnName) for O(1) lookup
        var errorsByCell = validationErrors
            .Where(e => !string.IsNullOrEmpty(e.RowId) && !string.IsNullOrEmpty(e.ColumnName))
            .GroupBy(e => (e.RowId, e.ColumnName))
            .ToDictionary(g => g.Key, g => g.ToList());

        _logger?.LogTrace("Grouped into {CellErrorCount} unique cell errors", errorsByCell.Count);

        // Clear ALL existing validation errors first
        foreach (var row in Rows)
        {
            foreach (var cell in row.Cells)
            {
                cell.IsValidationError = false;
                cell.ValidationMessage = string.Empty;
            }
        }

        _logger?.LogDebug("Cleared all existing validation errors from cells");

        // Apply new validation errors to cells
        int appliedCount = 0;
        foreach (var row in Rows)
        {
            var rowId = row.Cells.FirstOrDefault()?.RowId;
            if (string.IsNullOrEmpty(rowId))
            {
                continue;
            }

            // Apply errors to data cells
            foreach (var cell in row.Cells.Where(c => c.SpecialType == Common.SpecialColumnType.None))
            {
                var key = (rowId, cell.ColumnName);
                if (errorsByCell.TryGetValue(key, out var cellErrors) && cellErrors.Any())
                {
                    _logger?.LogTrace("Setting IsValidationError=true for cell [{RowIndex},{ColumnIndex}] '{ColumnName}'",
                        cell.RowIndex, cell.ColumnIndex, cell.ColumnName);
                    cell.IsValidationError = true;
                    cell.ValidationMessage = string.Join("; ", cellErrors.Select(e => e.Message));
                    appliedCount++;
                    _logger?.LogDebug("Applied validation error to cell [{RowIndex}, {ColumnName}]: {Message}",
                        cell.RowIndex, cell.ColumnName, cell.ValidationMessage);
                }
            }

            // Update ValidationAlerts special column
            var alertsCell = row.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.ValidationAlerts);
            if (alertsCell != null)
            {
                // Get all errors for this row (any column)
                var allRowErrors = errorsByCell
                    .Where(kvp => kvp.Key.RowId == rowId)
                    .SelectMany(kvp => kvp.Value)
                    .ToList();

                if (allRowErrors.Any())
                {
                    // CRITICAL FIX: Format with column names for clarity
                    // Format: "ColumnName: msg1; ColumnName: msg2; ..."
                    // User requirement: Show column name to identify which field has validation error
                    var message = string.Join("; ",
                        allRowErrors.Select(e => $"{e.ColumnName}: {e.Message}"));

                    // CRITICAL: Update on UI thread to ensure PropertyChanged propagates correctly
                    if (_dispatcherQueue != null)
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            alertsCell.ValidationAlertMessage = message;
                            _logger?.LogDebug("Updated ValidationAlerts column for row {RowIndex}: {Alerts}",
                                row.RowIndex, alertsCell.ValidationAlertMessage);
                        });
                    }
                    else
                    {
                        alertsCell.ValidationAlertMessage = message;
                        _logger?.LogDebug("Updated ValidationAlerts column for row {RowIndex}: {Alerts}",
                            row.RowIndex, alertsCell.ValidationAlertMessage);
                    }
                }
                else
                {
                    // CRITICAL: Clear on UI thread to ensure PropertyChanged propagates correctly
                    if (_dispatcherQueue != null)
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            alertsCell.ValidationAlertMessage = null;
                        });
                    }
                    else
                    {
                        alertsCell.ValidationAlertMessage = null;
                    }
                }
            }
        }

        _logger?.LogInformation("Applied {AppliedCount} validation errors to {TotalCells} cells",
            appliedCount, Rows.Sum(r => r.Cells.Count));

        // CRITICAL FIX: Force UI refresh by invalidating viewport cache on UI thread
        // This ensures validation error borders appear immediately in virtualized ItemsRepeater
        if (_dispatcherQueue != null)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                ViewportManager?.InvalidateCache();
                _logger?.LogTrace("ViewportManager cache invalidated to force UI refresh for validation errors");
            });
        }
        else
        {
            ViewportManager?.InvalidateCache();
            _logger?.LogTrace("ViewportManager cache invalidated to force UI refresh for validation errors");
        }
    }

    /// <summary>
    /// Clears all validation errors from grid UI.
    /// Resets IsValidationError flags and validation messages on all cells.
    /// </summary>
    internal void ClearValidationErrors()
    {
        _logger?.LogInformation("Clearing all validation errors from grid UI");

        foreach (var row in Rows)
        {
            foreach (var cell in row.Cells)
            {
                cell.IsValidationError = false;
                cell.ValidationMessage = string.Empty;
            }

            // Clear ValidationAlerts column
            var alertsCell = row.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.ValidationAlerts);
            if (alertsCell != null)
            {
                alertsCell.ValidationAlertMessage = null;
            }
        }

        _logger?.LogInformation("Validation errors cleared from grid UI");
    }

    #endregion

    #region Sort on Header Click (FÁZA 5)

    /// <summary>
    /// Cycles column sort direction: None → Ascending → Descending → None
    /// Clears sort indicators on other columns (single-column sort only)
    /// Fires SortRequested event for facade to handle actual sorting via SortService
    /// </summary>
    /// <param name="columnName">Name of the column to sort</param>
    public void CycleSortDirection(string columnName)
    {
        var header = ColumnHeaders.FirstOrDefault(h => h.ColumnName == columnName);
        if (header == null)
        {
            _logger?.LogWarning("CycleSortDirection: Column {ColumnName} not found", columnName);
            return;
        }

        // Cycle: None → Ascending → Descending → None
        var newDirection = header.SortDirection switch
        {
            "None" => "Ascending",
            "Ascending" => "Descending",
            "Descending" => "None",
            _ => "Ascending" // Fallback for invalid values
        };

        _logger?.LogInformation("Sort direction changed: {Column} {OldDir} → {NewDir}",
            columnName, header.SortDirection, newDirection);

        // Clear sort indicators on other columns (single-column sort)
        foreach (var otherHeader in ColumnHeaders.Where(h => h != header))
        {
            if (otherHeader.SortDirection != "None")
            {
                otherHeader.SortDirection = "None";
            }
        }

        // Update current column
        header.SortDirection = newDirection;

        // Fire event for facade to handle actual sorting
        SortRequested?.Invoke(this, new SortRequestedEventArgs(columnName, newDirection));
    }

    /// <summary>
    /// SENIOR ADDITION: Sets column sort direction to a specific value (used by Header Flyout)
    /// Clears sort indicators on other columns (single-column sort only)
    /// Fires SortRequested event for facade to handle actual sorting via SortService
    /// </summary>
    /// <param name="columnName">Name of the column to sort</param>
    /// <param name="direction">Desired sort direction ("Ascending", "Descending", or "None")</param>
    public void SetSortDirection(string columnName, string direction)
    {
        var header = ColumnHeaders.FirstOrDefault(h => h.ColumnName == columnName);
        if (header == null)
        {
            _logger?.LogWarning("SetSortDirection: Column {ColumnName} not found", columnName);
            return;
        }

        // Validate direction
        var validDirections = new[] { "Ascending", "Descending", "None" };
        var newDirection = validDirections.Contains(direction) ? direction : "None";

        _logger?.LogInformation("Sort direction set: {Column} → {NewDir}",
            columnName, newDirection);

        // Clear sort indicators on other columns (single-column sort)
        foreach (var otherHeader in ColumnHeaders.Where(h => h != header))
        {
            if (otherHeader.SortDirection != "None")
            {
                otherHeader.SortDirection = "None";
            }
        }

        // Update current column
        header.SortDirection = newDirection;

        // Fire event for facade to handle actual sorting
        SortRequested?.Invoke(this, new SortRequestedEventArgs(columnName, newDirection));
    }

    #endregion

    #region Insert Row via Context Menu (FÁZA 4)

    /// <summary>
    /// Requests insert of new empty row above the specified row
    /// Fires InsertRowRequested event for facade to handle via IRowStore.InsertRowBeforeAsync()
    /// </summary>
    /// <param name="rowIndex">Index of the reference row</param>
    public void RequestInsertRowAbove(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
        {
            _logger?.LogWarning("RequestInsertRowAbove: Invalid row index {RowIndex}", rowIndex);
            return;
        }

        var row = Rows[rowIndex];
        _logger?.LogInformation("Insert row above requested: RowIndex={RowIndex}, RowId={RowId}",
            rowIndex, row.RowId);

        InsertRowRequested?.Invoke(this, new InsertRowRequestedEventArgs(
            rowIndex,
            row.RowId,
            "Above"));
    }

    /// <summary>
    /// Requests insert of new empty row below the specified row
    /// Fires InsertRowRequested event for facade to handle via IRowStore.InsertRowAfterAsync()
    /// </summary>
    /// <param name="rowIndex">Index of the reference row</param>
    public void RequestInsertRowBelow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
        {
            _logger?.LogWarning("RequestInsertRowBelow: Invalid row index {RowIndex}", rowIndex);
            return;
        }

        var row = Rows[rowIndex];
        _logger?.LogInformation("Insert row below requested: RowIndex={RowIndex}, RowId={RowId}",
            rowIndex, row.RowId);

        InsertRowRequested?.Invoke(this, new InsertRowRequestedEventArgs(
            rowIndex,
            row.RowId,
            "Below"));
    }

    #endregion

    #region PageManager Integration

    /// <summary>
    /// CRITICAL: Exposes PageManager instance for external components (ViewportManager, UI)
    /// </summary>
    public Features.Pagination.Interfaces.IPageManager? PageManager => _pageManager;

    /// <summary>
    /// Event fired when page data needs to be reloaded from IRowStore
    /// UI components should subscribe to this and fetch the new page data range
    /// </summary>
    public event EventHandler<(long startIndex, int count)>? PageDataReloadRequested;

    private void OnPageManagerPageChanged(object? sender, Features.Pagination.Interfaces.PageChangedEventArgs e)
    {
        _logger?.LogInformation("PageManager page changed: {OldPage} → {NewPage}, StartIndex={StartIndex}, Count={Count}",
            e.OldPage, e.NewPage, e.StartIndex, e.Count);

        // Sync to PaginationPanel (convert 0-based to 1-based)
        PaginationPanel.CurrentPage = e.NewPage + 1;

        // Request data reload for new page
        PageDataReloadRequested?.Invoke(this, (e.StartIndex, e.Count));
    }

    private void OnPageManagerPageSizeChanged(object? sender, int newPageSize)
    {
        _logger?.LogInformation("PageManager page size changed: {NewPageSize}", newPageSize);

        // Sync to PaginationPanel
        PaginationPanel.PageSize = newPageSize;
    }

    private void OnPaginationPanelPageChanged(object? sender, int newPage)
    {
        _logger?.LogTrace("PaginationPanel page changed: {NewPage}", newPage);

        // Sync to PageManager (convert 1-based to 0-based)
        _pageManager?.GoToPage(newPage - 1);
    }

    /// <summary>
    /// Updates total row count and syncs it to both PageManager and PaginationPanel
    /// Call this after data changes (insert, delete, import, filter)
    /// </summary>
    public void UpdateTotalRowCount(long totalRows)
    {
        _logger?.LogDebug("Updating total row count: {TotalRows}", totalRows);

        // Update PageManager (recalculates total pages)
        _pageManager?.SetTotalDataRows(totalRows);

        // Update PaginationPanel
        PaginationPanel.TotalRowCount = totalRows;
    }

    #endregion
}

/// <summary>
/// Event args for SortRequested event (FÁZA 5)
/// Contains column name and new sort direction for facade to process
/// </summary>
public sealed class SortRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Name of the column to sort
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// New sort direction: "None", "Ascending", or "Descending"
    /// </summary>
    public string SortDirection { get; }

    public SortRequestedEventArgs(string columnName, string sortDirection)
    {
        ColumnName = columnName;
        SortDirection = sortDirection;
    }
}


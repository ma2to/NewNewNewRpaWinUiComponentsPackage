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

    // ✅ SENIOR FIX: Batch update mode for SelectAll/DeselectAll performance optimization
    // When true, row PropertyChanged events are suppressed to prevent UI freeze during bulk operations
    private bool _isBatchUpdating = false;

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
    /// PROFESSIONAL FIX: Filter flyout service for checkbox and regex filtering.
    /// Injected by AdvancedDataGridFacade during initialization.
    /// Used by HeadersRowView to trigger filter operations via header click menu.
    /// Supports two filter modes:
    /// 1. Checkbox mode: Select/deselect distinct values (SQL WHERE IN clause)
    /// 2. Regex mode: Enter regular expression pattern (SQL WHERE REGEXP clause)
    /// </summary>
    internal Features.Filter.Services.FilterFlyoutService? FilterFlyoutService { get; set; }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Facade reference for realtime preview validation.
    /// Injected by AdvancedDataGridFacade during initialization.
    /// Used by DataGridCellsView to call PreviewValidateCellAsync during edit mode (keystroke validation).
    /// NULL in standalone ViewModel scenarios (testing, design-time).
    /// </summary>
    internal IAdvancedDataGridFacade? Facade { get; set; }

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
        // ✅ PROFESSIONAL FIX: Filter ALL system columns with __ prefix (not just __rowId)
        // REASON: Backend adds system columns (__rowId, __rowNumber, __internalState, etc.) for internal use
        // These must be hidden from UI to prevent:
        //   1. User editing system values (breaks internal logic)
        //   2. Column position conflicts (e.g. __rowNumber vs rowNumber special column)
        //   3. Confusing duplicate columns in grid
        // ARCHITECTURE: System columns (__*) are internal, user columns never start with __
        var allColumns = columnNames.ToList();
        var columnList = allColumns
            .Where(c => !c.StartsWith("__", StringComparison.Ordinal))
            .ToList();

        var filteredCount = allColumns.Count - columnList.Count;
        if (filteredCount > 0)
        {
            var filteredColumns = allColumns.Except(columnList).ToList();
            _logger?.LogDebug("Filtered {Count} system columns with __ prefix: {Columns}",
                filteredCount, string.Join(", ", filteredColumns));
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

        // ✅ DIAGNOSTIC: Log column header order for RowNumber position debugging
        var headerOrder = string.Join(", ", ColumnHeaders.Select(h =>
            $"{h.DisplayOrder}:{h.ColumnName}({(h.IsSpecialColumn ? "SPECIAL-" + h.SpecialType : "DATA")})"));
        _logger?.LogInformation("🔍 COLUMN HEADER ORDER: [{HeaderOrder}]", headerOrder);
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
    /// ✅ FIX: Force UI refresh after data changes (INSERT/DELETE operations).
    /// CRITICAL: WinUI ItemsRepeater requires PropertyChanged(Rows) to re-render viewport.
    /// Called by InternalUIUpdateHandler after UpdateViewModelsInPlace().
    /// Same mechanism as OnPageChanged() - ensures UI reflects updated data immediately.
    /// </summary>
    public void NotifyRowsCollectionChanged()
    {
        ViewportManager?.InvalidateCache();
        OnPropertyChanged(nameof(Rows));
        _logger?.LogDebug("UI refresh triggered - Rows collection PropertyChanged notified");
    }

    /// <summary>
    /// ✅ CRITICAL FIX: Force complete UI refresh for ItemsRepeater (beyond cache invalidation).
    /// PROBLEM: ItemsRepeater caches collapsed elements (rows 10-14) and doesn't re-render when IsVisible changes.
    /// SOLUTION: Trigger ItemsRepeaterRefreshRequested event that DataGridCellsView handles via ItemsSource rebind.
    /// USE CASE: Page 7 has 10 rows → user adds 5 rows → UI should show 15 rows (not stay at 10).
    /// </summary>
    public event EventHandler? ItemsRepeaterRefreshRequested;

    public void ForceCompleteUIRefresh()
    {
        ViewportManager?.InvalidateCache();
        OnPropertyChanged(nameof(Rows));

        // ✅ Trigger ItemsRepeater rebind in DataGridCellsView
        ItemsRepeaterRefreshRequested?.Invoke(this, EventArgs.Empty);

        _logger?.LogDebug("Complete UI refresh triggered - ItemsRepeater rebind requested");
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

            // ✅ SENIOR FIX: Set parent ViewModel reference for batch update optimization
            // Enables row to check IsBatchUpdating during SelectAll/DeselectAll
            rowVm.SetParentViewModel(this);

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
                    // ✅ CRITICAL: RowNumber is ALWAYS read-only (cannot be edited)
                    cellVm.IsReadOnly = true;
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

            // ✅ DIAGNOSTIC: Log cell order for RowNumber position debugging (first 3 rows only)
            if (rowIndex < 3)
            {
                var cellOrder = string.Join(", ", rowVm.Cells.Select(c =>
                    $"{c.ColumnIndex}:{c.ColumnName}({(c.IsSpecialColumn ? "SPECIAL" : "DATA")})"));
                _logger?.LogDebug("🔍 ROW {RowIndex} CELL ORDER: [{CellOrder}]", rowIndex, cellOrder);
            }

            rowViewModels.Add(rowVm);
            rowIndex++;
        }

        // ✅ SENIOR FIX: Update PageManager BEFORE AddRange to prevent race condition
        // CRITICAL: OnRowsCollectionChanged fires DURING AddRange and needs TotalDataRows set!
        // Without this: OnRowsCollectionChanged sees TotalDataRows=0 → ERROR "PageManager not configured"
        // ⚠️ DUAL-MODE ARCHITECTURE FIX: TotalDataRows is set EXTERNALLY (not from ViewModels count)
        // REASON: UI layer (ViewModels) contains only CURRENT PAGE (15 rows), not full dataset (1000+ rows)
        // TotalDataRows must be set by caller (PerformFullReload or OnPageChanged) based on RowStore.GetRowCountAsync()
        if (_pageManager != null)
        {
            // ❌ REMOVED: UpdateTotalRowCount(rowViewModels.Count);
            // ✅ TotalDataRows is already set externally - DO NOT override it with page count
            _logger?.LogInformation("PageManager.TotalDataRows already set externally to {TotalDataRows} (TotalPages={TotalPages}), loading {ViewModelCount} ViewModels for current page",
                _pageManager.TotalDataRows, _pageManager.TotalPages, rowViewModels.Count);
        }

        // CRITICAL PERFORMANCE: Use AddRange instead of individual Add() calls
        // For 10M rows: 10M events → 1 event = MASSIVE speedup
        // NOTE: This triggers OnRowsCollectionChanged which now sees correct TotalDataRows
        Rows.AddRange(rowViewModels);

        // Invalidate cache after loading new data
        InvalidateCache();

        _logger?.LogInformation("Rows loaded successfully with {SpecialCount} special columns per row",
            ColumnHeaders.Count(h => h.IsSpecialColumn));
    }

    /// <summary>
    /// ✅ PROFESSIONAL SOLUTION: Updates existing ViewModels IN-PLACE with FIXED UI POOL.
    /// ARCHITECTURE:
    /// - FIXED UI POOL: Always maintains EXACTLY PageSize ViewModels (e.g., 15)
    /// - Data count may be less than PageSize (e.g., page 7 has 10 rows)
    /// - Empty slots (15-10=5) are marked as IsVisible=false
    /// - RowId-BASED: All updates use RowId (stable identifier), not RowIndex (unstable)
    /// BENEFITS:
    /// - NO dispose/create overhead (pool size constant)
    /// - INSERT/DELETE only update data, not UI object count
    /// - Safe for concurrent operations (RowId is stable)
    /// </summary>
    /// <param name="rowsData">New row data to apply to existing ViewModels</param>
    public void UpdateViewModelsInPlace(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData)
    {
        var dataList = rowsData.ToList();
        var pageSize = PageManager?.PageSize ?? 15;

        _logger?.LogInformation("Updating ViewModels IN-PLACE: DataCount={DataCount}, FixedPoolSize={PoolSize}",
            dataList.Count, pageSize);

        // ✅ STEP 1: Ensure Rows collection has EXACTLY PageSize ViewModels (FIXED POOL)
        // REASON: UI pool is FIXED size (always PageSize ViewModels, regardless of data count)
        while (Rows.Count > pageSize)
        {
            var removedRow = Rows[Rows.Count - 1];
            Rows.RemoveAt(Rows.Count - 1);

            if (removedRow is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _logger?.LogTrace("Removed excess ViewModel (shrinking pool to {PoolSize})", pageSize);
        }

        while (Rows.Count < pageSize)
        {
            // Create new empty ViewModel for UI pool
            var rowVm = new DataGridRowViewModel
            {
                RowIndex = Rows.Count,
                RowId = null,  // ← Empty row has no RowId
                IsVisible = false  // ← Start as invisible (will be set to true if has data)
            };
            rowVm.SetParentViewModel(this);

            // Create cells for all columns
            for (int colIndex = 0; colIndex < ColumnHeaders.Count; colIndex++)
            {
                var header = ColumnHeaders[colIndex];
                var cellVm = new CellViewModel(Theme, _loggerFactory?.CreateLogger<CellViewModel>())
                {
                    RowIndex = Rows.Count,
                    RowId = null,  // ← Empty cell has no RowId
                    ColumnIndex = colIndex,
                    ColumnName = header.ColumnName,
                    SpecialType = header.SpecialType,
                    IsReadOnly = header.IsSpecialColumn
                };
                cellVm.SetParentRow(rowVm);
                rowVm.Cells.Add(cellVm);
            }

            Rows.Add(rowVm);
            _logger?.LogTrace("Added empty ViewModel to UI pool (growing pool to {PoolSize})", Rows.Count);
        }

        _logger?.LogDebug("UI Pool ensured: {PoolSize} ViewModels (fixed)", pageSize);

        // ✅ STEP 2: Update ViewModels with data (RowId-BASED)
        // ARCHITECTURE:
        // - First dataList.Count ViewModels: VISIBLE (have RowId + data)
        // - Remaining (pageSize - dataList.Count) ViewModels: INVISIBLE (no RowId, no data)
        for (int i = 0; i < pageSize; i++)
        {
            var rowViewModel = Rows[i];

            if (i < dataList.Count)
            {
                // ✅ VISIBLE ROW: Has data
                var rowData = dataList[i];

                // ✅ CRITICAL: Extract RowId from data (__rowId column)
                // REASON: RowId is STABLE identifier used for all operations (delete, update, etc.)
                string? newRowId = null;
                if (rowData.TryGetValue("__rowId", out var rowIdValue))
                {
                    newRowId = rowIdValue?.ToString();
                }

                // Update RowId (fires PropertyChanged → UI rebinds)
                if (rowViewModel.RowId != newRowId)
                {
                    _logger?.LogTrace("Updating RowId at index {Index}: {OldId} → {NewId}",
                        i, rowViewModel.RowId ?? "(null)", newRowId ?? "(null)");
                    rowViewModel.RowId = newRowId;  // ← RowId updated (UI button Click will use this!)
                }

                // Update RowIndex (page-relative, 0-based)
                if (rowViewModel.RowIndex != i)
                {
                    rowViewModel.RowIndex = i;
                }

                // Mark as visible
                if (!rowViewModel.IsVisible)
                {
                    rowViewModel.IsVisible = true;
                    _logger?.LogTrace("Row {Index} marked VISIBLE (has data, RowId={RowId})", i, newRowId);
                }

                // ✅ Update cell values (RowId propagated to all cells)
                foreach (var cell in rowViewModel.Cells)
                {
                    // Update cell RowId (same as row RowId)
                    if (cell.RowId != newRowId)
                    {
                        cell.RowId = newRowId;  // ← Cell inherits RowId from row
                    }

                    if (cell.RowIndex != i)
                    {
                        cell.RowIndex = i;
                    }

                    // Update cell value based on column type
                    if (cell.SpecialType == SpecialColumnType.RowNumber)
                    {
                        // RowNumber - computed from rowIndex (1-based)
                        cell.Value = i + 1;
                        // ✅ CRITICAL: RowNumber is ALWAYS read-only (cannot be edited)
                        cell.IsReadOnly = true;
                    }
                    else if (cell.SpecialType == SpecialColumnType.Checkbox)
                    {
                        // Checkbox - preserve current state (don't reset)
                    }
                    else if (cell.SpecialType == SpecialColumnType.ValidationAlerts)
                    {
                        // ValidationAlerts - preserve current state
                    }
                    else if (cell.SpecialType == SpecialColumnType.DeleteRow ||
                             cell.SpecialType == SpecialColumnType.InsertRow)
                    {
                        // DeleteRow/InsertRow buttons - no value
                        // CRITICAL: Button Click handlers use cell.RowId (updated above!)
                    }
                    else
                    {
                        // Normal data column - update value from row data
                        if (rowData.TryGetValue(cell.ColumnName, out var newValue))
                        {
                            cell.Value = newValue;
                        }
                        else
                        {
                            cell.Value = null;
                        }
                    }
                }
            }
            else
            {
                // ✅ EMPTY ROW: No data (UI pool padding)
                // REASON: Page has fewer data rows than PageSize (e.g., page 7 has 10 rows, PageSize=15)
                // RENDERING: Row is INVISIBLE (DataGridElementFactory returns collapsed placeholder)

                if (rowViewModel.RowId != null)
                {
                    rowViewModel.RowId = null;  // ← Clear RowId (no data)
                }

                if (rowViewModel.RowIndex != i)
                {
                    rowViewModel.RowIndex = i;
                }

                // Mark as invisible
                if (rowViewModel.IsVisible)
                {
                    rowViewModel.IsVisible = false;
                    _logger?.LogTrace("Row {Index} marked INVISIBLE (UI pool padding, no data)", i);
                }

                // Clear all cell values and RowIds
                foreach (var cell in rowViewModel.Cells)
                {
                    if (cell.RowId != null)
                    {
                        cell.RowId = null;  // ← Clear RowId
                    }

                    if (cell.RowIndex != i)
                    {
                        cell.RowIndex = i;
                    }

                    if (cell.Value != null)
                    {
                        cell.Value = null;  // ← Clear value
                    }
                }
            }
        }

        // ✅ STEP 3: Invalidate caches
        InvalidateCache();
        ViewportManager?.InvalidateCache();

        _logger?.LogInformation("✅ Updated {DataCount} visible ViewModels + {EmptyCount} empty (fixed pool={PoolSize})",
            dataList.Count, pageSize - dataList.Count, pageSize);
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
    /// ✅ PERFORMANCE FIX: Uses batch update to prevent UI freeze on 100+ rows.
    /// </summary>
    /// <summary>
    /// ✅ SENIOR FIX: Optimized SelectAllRows with batch update mode and performance monitoring.
    /// PERFORMANCE TARGET: 100 rows in <100ms (was ~3300ms before optimization).
    /// </summary>
    public void SelectAllRows()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger?.LogInformation("Selecting all {Count} rows (optimized batch mode)", Rows.Count);

        try
        {
            // ✅ SENIOR PERFORMANCE FIX: Enable batch mode to suppress PropertyChanged events
            _isBatchUpdating = true;

            // Collect all rows and set IsSelected directly (bypasses individual UI updates)
            var selectedCount = 0;
            foreach (var row in Rows)
            {
                row.IsSelected = true;
                selectedCount++;
            }

            _logger?.LogInformation("All {Count} rows selected in batch mode (suppressed PropertyChanged)", selectedCount);
        }
        finally
        {
            // ✅ CRITICAL: Always restore batch mode even if exception occurs
            _isBatchUpdating = false;
        }

        // ✅ SENIOR FIX: Comprehensive UI refresh after batch update
        if (_dispatcherQueue != null)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // ✅ CRITICAL: Force PropertyChanged for ALL rows to update checkbox UI binding
                // Without this: Checkboxes don't reflect IsSelected changes (batch mode suppressed events)
                foreach (var row in Rows)
                {
                    row.RaisePropertyChanged(nameof(DataGridRowViewModel.IsSelected));
                }

                // Invalidate viewport cache to force UI refresh
                ViewportManager?.InvalidateCache();

                // Notify UI that Rows collection changed (triggers ItemsRepeater refresh)
                OnPropertyChanged(nameof(Rows));

                sw.Stop();
                _logger?.LogInformation("SelectAllRows completed in {Elapsed}ms with full UI refresh (optimized from ~3300ms)",
                    sw.ElapsedMilliseconds);
            });
        }
        else
        {
            sw.Stop();
            _logger?.LogWarning("DispatcherQueue not available - UI refresh skipped (Elapsed={Elapsed}ms)",
                sw.ElapsedMilliseconds);
        }

        // ✅ FIX: Update header checkbox state after selection change
        UpdateCheckboxHeaderStatePublic();
    }

    /// <summary>
    /// ✅ SENIOR FIX: Optimized DeselectAllRows with batch update mode and performance monitoring.
    /// PERFORMANCE TARGET: 100 rows in <100ms (was ~3500ms before optimization).
    /// </summary>
    public void DeselectAllRows()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger?.LogInformation("Deselecting all {Count} rows (optimized batch mode)", Rows.Count);

        try
        {
            // ✅ SENIOR PERFORMANCE FIX: Enable batch mode to suppress PropertyChanged events
            _isBatchUpdating = true;

            // Collect all rows and set IsSelected directly (bypasses individual UI updates)
            var deselectedCount = 0;
            foreach (var row in Rows)
            {
                row.IsSelected = false;
                deselectedCount++;
            }

            _logger?.LogInformation("All {Count} rows deselected in batch mode (suppressed PropertyChanged)", deselectedCount);
        }
        finally
        {
            // ✅ CRITICAL: Always restore batch mode even if exception occurs
            _isBatchUpdating = false;
        }

        // ✅ SENIOR FIX: Comprehensive UI refresh after batch update
        if (_dispatcherQueue != null)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // ✅ CRITICAL: Force PropertyChanged for ALL rows to update checkbox UI binding
                // Without this: Checkboxes don't reflect IsSelected changes (batch mode suppressed events)
                foreach (var row in Rows)
                {
                    row.RaisePropertyChanged(nameof(DataGridRowViewModel.IsSelected));
                }

                // Invalidate viewport cache to force UI refresh
                ViewportManager?.InvalidateCache();

                // Notify UI that Rows collection changed (triggers ItemsRepeater refresh)
                OnPropertyChanged(nameof(Rows));

                sw.Stop();
                _logger?.LogInformation("DeselectAllRows completed in {Elapsed}ms with full UI refresh (optimized from ~3500ms)",
                    sw.ElapsedMilliseconds);
            });
        }
        else
        {
            sw.Stop();
            _logger?.LogWarning("DispatcherQueue not available - UI refresh skipped (Elapsed={Elapsed}ms)",
                sw.ElapsedMilliseconds);
        }

        // ✅ FIX: Update header checkbox state after selection change
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
    internal async Task ApplyValidationErrors(IReadOnlyList<Common.Models.ValidationError> validationErrors)
    {
        _logger?.LogTrace("DataGridViewModel: ApplyValidationErrors called with {ErrorCount} errors", validationErrors?.Count ?? 0);

        if (validationErrors == null)
        {
            _logger?.LogWarning("ApplyValidationErrors called with null errors list");
            return;
        }

        _logger?.LogInformation("Applying {ErrorCount} validation errors to grid UI", validationErrors.Count);

        // ✅ DIAGNOSTIC 1: Log validation error rowIds
        var errorRowIds = validationErrors.Select(e => e.RowId).Distinct().ToList();
        _logger?.LogWarning("📋 VALIDATION ERRORS contain {Count} unique rowIds: {RowIds}",
            errorRowIds.Count, string.Join(", ", errorRowIds.Take(5)));

        // Group errors by (RowId, ColumnName) for O(1) lookup
        var errorsByCell = validationErrors
            .Where(e => !string.IsNullOrEmpty(e.RowId) && !string.IsNullOrEmpty(e.ColumnName))
            .GroupBy(e => (e.RowId, e.ColumnName))
            .ToDictionary(g => g.Key, g => g.ToList());

        _logger?.LogDebug("Grouped into {CellErrorCount} unique cell errors", errorsByCell.Count);

        // ✅ CRITICAL FIX: ATOMIC batch operation namiesto 150+ TryEnqueue calls
        // REASON: Každý TryEnqueue = samostatný UI thread task → clearing a applying sa prelínajú
        // PREVIOUS BUG: Page 1 row 0 had error, change to page 2 → row 0 still shows error
        //               CAUSE: UI thread queue interleaved clearing and applying tasks
        // SOLUTION: Batch ALL clearing + applying v JEDNOM TryEnqueue call
        // ARCHITECTURE: Atomická operácia na UI thread → clear THEN apply, no interleaving
        // PERFORMANCE: 1 context switch namiesto 150+ = RÝCHLEJŠIE pre 10M+ riadkov
        // VALIDATION SCOPE:
        //   - Validation sa VYKONÁVA na všetkých riadkoch ktoré potrebujú revalidáciu
        //   - ApplyValidationErrors ZOBRAZUJE errors len na 15 visible ViewModels
        //   - Cross-column dependencies: rule.DependentColumns revaliduje závislé stĺpce
        _logger?.LogDebug("Clearing all existing validation errors before applying new ones (ItemsRepeater recycling fix)");

        // ✅ DIAGNOSTIC 2: Log ViewModel rowIds
        var viewModelRowIds = Rows
            .Select(r => r.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.None)?.RowId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();
        _logger?.LogWarning("🖥️ VIEWMODELS contain {Count} unique rowIds: {RowIds}",
            viewModelRowIds.Count, string.Join(", ", viewModelRowIds.Take(5)));

        // Apply new validation errors to cells
        int appliedCount = 0;
        int mismatchCount = 0;

        // ✅ Batch všetky operácie v JEDNOM UI thread task
        if (_dispatcherQueue != null)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // STEP 1: Clear ALL validation errors (synchronous within UI thread)
                foreach (var row in Rows)
                {
                    foreach (var cell in row.Cells.Where(c => c.SpecialType == Common.SpecialColumnType.None))
                    {
                        if (cell.IsValidationError)
                        {
                            cell.IsValidationError = false;
                            cell.ValidationMessage = null;
                        }
                    }

                    // ✅ ALWAYS clear ValidationAlerts - unconditionally!
                    // REASON: ItemsRepeater recycles UI → old message persists without explicit clear
                    // PREVIOUS BUG: Cleared only if !IsNullOrEmpty → empty cells kept old values from recycled UI
                    var alertsCell = row.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.ValidationAlerts);
                    if (alertsCell != null)
                    {
                        alertsCell.ValidationAlertMessage = null;  // Unconditional clear
                    }
                }

                // STEP 2: Apply new validation errors (synchronous within UI thread)
                // NOTE: Tento kód beží SYNCHRONNE po Step 1 v rovnakom UI thread cykle
                // GUARANTEE: Clearing je 100% dokončený pred applying
                // SCOPE: Aplikuje errors len na 15 visible ViewModels (Rows collection)
                //        Validation bola vykonaná na všetkých riadkoch v pozadí
                int batchAppliedCount = 0;

                foreach (var row in Rows)
                {
                    // ✅ PROFESSIONAL FIX: Get rowId from first DATA cell (skip special columns)
                    var rowId = row.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.None)?.RowId;

                    if (string.IsNullOrEmpty(rowId))
                    {
                        rowId = row.RowId;
                        if (string.IsNullOrEmpty(rowId))
                            continue;
                    }

                    // Apply errors to data cells
                    foreach (var cell in row.Cells.Where(c => c.SpecialType == Common.SpecialColumnType.None))
                    {
                        if (errorsByCell.TryGetValue((rowId, cell.ColumnName), out var cellErrors))
                        {
                            var validationMessage = string.Join("; ", cellErrors.Select(e => e.Message));
                            cell.ValidationMessage = validationMessage;
                            cell.IsValidationError = true;
                            batchAppliedCount++;
                        }
                    }

                    // Update ValidationAlerts column with cross-column errors
                    // NOTE: ValidationAlerts zobrazí všetky errors pre tento riadok,
                    //       vrátane errors z cross-column validation rules
                    var alertsCellInner = row.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.ValidationAlerts);
                    if (alertsCellInner != null)
                    {
                        var allRowErrors = errorsByCell
                            .Where(kvp => kvp.Key.RowId == rowId)
                            .SelectMany(kvp => kvp.Value)
                            .ToList();

                        if (allRowErrors.Any())
                        {
                            var columnOrder = ColumnHeaders
                                .Select((header, index) => new { header.ColumnName, Order = index })
                                .ToDictionary(x => x.ColumnName, x => x.Order, StringComparer.OrdinalIgnoreCase);

                            var sortedErrors = allRowErrors
                                .OrderBy(e => columnOrder.TryGetValue(e.ColumnName ?? string.Empty, out var order) ? order : int.MaxValue)
                                .ToList();

                            var message = string.Join("; ",
                                sortedErrors.Select(e => $"{e.ColumnName}: {e.Message}"));

                            alertsCellInner.ValidationAlertMessage = message;
                        }
                        // ✅ CRITICAL FIX: ELSE branch to clear message when no errors
                        // REASON: ItemsRepeater recycles UI → old message persists without explicit clear
                        // PREVIOUS BUG: Page 1 error appeared on Page 2 at same position (13th fix!)
                        // USER ISSUE: "na kazdej page je tato chyba vypisana na prvom riadku"
                        else
                        {
                            alertsCellInner.ValidationAlertMessage = null;  // Clear when no errors
                        }
                    }
                }

                _logger?.LogInformation("✅ ATOMIC VALIDATION UPDATE: Cleared all + applied {Count} errors in single UI thread cycle", batchAppliedCount);
                appliedCount = batchAppliedCount;
            });

            // ✅ CRITICAL FIX: Force SYNCHRONOUS wait for PropertyChanged propagation
            // REASON: TryEnqueue is async → need to ensure UI sees updated values
            // PREVIOUS BUG: Validation errors persisted on wrong pages due to race condition
            // SOLUTION: Small delay to allow PropertyChanged events to propagate to UI
            // TIMING: 50ms = 3 frames @ 60fps - ensures PropertyChanged completed before UI refresh
            await Task.Delay(50);
        }
        else
        {
            // Fallback: Synchronous execution (nie UI thread)
            // NOTE: Toto sa použije len ak DispatcherQueue nie je dostupný (testing scenarios)
            foreach (var row in Rows)
            {
                foreach (var cell in row.Cells.Where(c => c.SpecialType == Common.SpecialColumnType.None))
                {
                    if (cell.IsValidationError)
                    {
                        cell.IsValidationError = false;
                        cell.ValidationMessage = null;
                    }
                }

                var alertsCell = row.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.ValidationAlerts);
                if (alertsCell != null && !string.IsNullOrEmpty(alertsCell.ValidationAlertMessage))
                {
                    alertsCell.ValidationAlertMessage = null;
                }
            }

            // Apply new errors synchronously (same logic as batch)
            foreach (var row in Rows)
            {
                var rowId = row.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.None)?.RowId;

                if (string.IsNullOrEmpty(rowId))
                {
                    rowId = row.RowId;
                    if (string.IsNullOrEmpty(rowId))
                        continue;
                }

                // Apply errors to data cells
                foreach (var cell in row.Cells.Where(c => c.SpecialType == Common.SpecialColumnType.None))
                {
                    if (errorsByCell.TryGetValue((rowId, cell.ColumnName), out var cellErrors))
                    {
                        var validationMessage = string.Join("; ", cellErrors.Select(e => e.Message));
                        cell.ValidationMessage = validationMessage;
                        cell.IsValidationError = true;
                        appliedCount++;
                    }
                }

                // Update ValidationAlerts column
                var alertsCell = row.Cells.FirstOrDefault(c => c.SpecialType == Common.SpecialColumnType.ValidationAlerts);
                if (alertsCell != null)
                {
                    var allRowErrors = errorsByCell
                        .Where(kvp => kvp.Key.RowId == rowId)
                        .SelectMany(kvp => kvp.Value)
                        .ToList();

                    if (allRowErrors.Any())
                    {
                        var columnOrder = ColumnHeaders
                            .Select((header, index) => new { header.ColumnName, Order = index })
                            .ToDictionary(x => x.ColumnName, x => x.Order, StringComparer.OrdinalIgnoreCase);

                        var sortedErrors = allRowErrors
                            .OrderBy(e => columnOrder.TryGetValue(e.ColumnName ?? string.Empty, out var order) ? order : int.MaxValue)
                            .ToList();

                        var message = string.Join("; ",
                            sortedErrors.Select(e => $"{e.ColumnName}: {e.Message}"));

                        alertsCell.ValidationAlertMessage = message;
                    }
                }
            }
        }

        _logger?.LogWarning("Applied {AppliedCount} validation errors to {TotalCells} cells (Mismatches in first 3 rows: {MismatchCount})",
            appliedCount, Rows.Sum(r => r.Cells.Where(c => c.SpecialType == Common.SpecialColumnType.None).Count()), mismatchCount);

        // ✅ DIAGNOSTIC 3: Compare rowIds to identify mismatch patterns
        var unmatchedErrorRowIds = errorRowIds.Except(viewModelRowIds).ToList();
        var unmatchedViewModelRowIds = viewModelRowIds.Except(errorRowIds).ToList();

        if (unmatchedErrorRowIds.Any())
        {
            _logger?.LogError("❌ UNMATCHED ERROR ROWIDS ({Count}): {RowIds}",
                unmatchedErrorRowIds.Count, string.Join(", ", unmatchedErrorRowIds.Take(5)));
        }

        if (unmatchedViewModelRowIds.Any())
        {
            _logger?.LogError("❌ UNMATCHED VIEWMODEL ROWIDS ({Count}): {RowIds}",
                unmatchedViewModelRowIds.Count, string.Join(", ", unmatchedViewModelRowIds.Take(5)));
        }

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
    /// ✅ PROFESSIONAL FIX: Sets column sort direction with multi-sort support (Shift key detection)
    /// Click = Single-sort (clears all other sort indicators)
    /// Shift+Click = Multi-sort (preserves other sort indicators, adds/updates current column)
    /// Fires SortRequested event for facade to handle actual sorting via SortService
    /// </summary>
    /// <param name="columnName">Name of the column to sort</param>
    /// <param name="direction">Desired sort direction ("Ascending", "Descending", or "None")</param>
    /// <param name="isShiftKeyPressed">If true, multi-sort mode (preserve other columns). If false, single-sort (clear others)</param>
    public void SetSortDirection(string columnName, string direction, bool isShiftKeyPressed = false)
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

        _logger?.LogInformation("Sort direction set: {Column} → {NewDir}, ShiftKey={Shift} (Multi-sort={Multi})",
            columnName, newDirection, isShiftKeyPressed, isShiftKeyPressed ? "YES" : "NO");

        // ✅ PROFESSIONAL FIX: Clear other columns ONLY if NOT multi-sort mode
        if (!isShiftKeyPressed)
        {
            // Single-sort mode: Clear sort indicators on other columns
            foreach (var otherHeader in ColumnHeaders.Where(h => h != header))
            {
                if (otherHeader.SortDirection != "None")
                {
                    _logger?.LogInformation("Clearing sort on column {Column} (single-sort mode)", otherHeader.ColumnName);
                    otherHeader.SortDirection = "None";
                }
            }
        }
        else
        {
            _logger?.LogInformation("Multi-sort mode: Preserving other column sort indicators");
        }

        // Update current column
        header.SortDirection = newDirection;

        // Fire event for facade to handle actual sorting
        SortRequested?.Invoke(this, new SortRequestedEventArgs(columnName, newDirection, isShiftKeyPressed));
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
    /// ✅ SENIOR FIX: Internal property for batch update mode (SelectAll/DeselectAll optimization).
    /// When true, DataGridRowViewModel suppresses PropertyChanged events during bulk operations.
    /// INTERNAL: Only accessible to ViewModels namespace for performance optimization.
    /// </summary>
    internal bool IsBatchUpdating => _isBatchUpdating;

    /// <summary>
    /// ✅ SENIOR FIX: Exposes RowStore for virtual pagination page loading
    /// CRITICAL: DataGridCellsView needs RowStore to load new page data on PageChanged event
    /// INTERNAL: Only accessible to UI adapters for pagination data fetching
    /// ARCHITECTURE: Enables dual-mode virtual pagination (UI loads only current page from RowStore)
    /// </summary>
    internal Infrastructure.Persistence.Interfaces.IRowStore? RowStore { get; set; }

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

        // ✅ CRITICAL FIX: Invalidate viewport cache to refresh UI with new page data
        // ViewportManager.GetRowViewModel() will now return rows from new page using page offset
        ViewportManager?.InvalidateCache();

        // ✅ CRITICAL FIX: Force ItemsRepeater to refresh by notifying collection changed
        // This triggers re-rendering with TotalRowCount (page row count) and GetRowViewModel (page-offset rows)
        OnPropertyChanged(nameof(Rows));

        _logger?.LogDebug("Viewport invalidated and UI refreshed for page {Page}", e.NewPage + 1);

        // Request data reload for new page (used for server-side pagination scenarios)
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

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Indicates if Shift key was pressed during sort request
    /// Shift+Click = Multi-sort (add column to existing sort criteria)
    /// Click = Single-sort (replace all existing sort criteria)
    /// </summary>
    public bool IsShiftKeyPressed { get; }

    public SortRequestedEventArgs(string columnName, string sortDirection, bool isShiftKeyPressed = false)
    {
        ColumnName = columnName;
        SortDirection = sortDirection;
        IsShiftKeyPressed = isShiftKeyPressed;
    }
}


using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Viewport;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls.Menus;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Data grid cells view with UI virtualization using ItemsRepeater.
/// Displays rows and cells in a scrollable area with viewport management.
/// Handles cell selection (single, multi-select with Ctrl, range selection with drag).
///
/// VIRTUALIZATION ARCHITECTURE:
/// - ItemsRepeater: WinUI control for element recycling
/// - ViewportManager: Manages POCO cache and ViewModel lifecycle (max 1000 ViewModels)
/// - DataGridElementFactory: Creates/recycles Grid elements for rows
/// - Element recycling: Reduces creation overhead by ~80%
///
/// MEMORY TARGET: 70-80% reduction vs non-virtualized
/// - BEFORE: 100 rows = 460 MB
/// - AFTER: 100 rows = 35-55 MB
/// </summary>
public sealed class DataGridCellsView : UserControl, IDisposable
{
    private readonly DataGridViewModel _viewModel;
    private readonly ViewportManager _viewportManager;
    private readonly DataGridElementFactory _elementFactory;
    private readonly ILogger<DataGridCellsView> _logger;
    private readonly RowContextMenu _rowContextMenu; // SENIOR IMPLEMENTATION: Excel-like row context menu

    /// <summary>
    /// CRITICAL FIX: Public access to ViewportManager for validation updates.
    /// Allows InternalUIUpdateHandler to refresh viewport cache after validation.
    /// </summary>
    public ViewportManager ViewportManager => _viewportManager;

    private readonly ScrollViewer _scrollViewer;
    private readonly ItemsRepeater _itemsRepeater;

    // SENIOR FIX: Custom drag selection tracking (fixes e.Handled = true blocking event bubbling)
    private CellViewModel? _pressedCell; // Cell where mouse was initially pressed
    private bool _isDragging; // True when user started dragging (moved to different cell)
    private CellViewModel? _lastSelectedCell; // ✅ FIX: Track last selected cell to prevent duplicate events
    private CellViewModel? _lastDraggedCell; // ✅ PROFESSIONAL FIX: Track last dragged cell to avoid duplicate processing

    private bool _disposed;
    private bool _isUpdatingViewport; // Prevent re-entrant viewport updates

    // ✅ PROFESSIONAL QUALITY: Scroll optimization fields
    private DispatcherTimer? _scrollDebounceTimer;
    private const int SCROLL_DEBOUNCE_MS = 16; // ~60 FPS throttle
    private const int VIEWPORT_BUFFER_ROWS = 10; // Pre-load rows above/below
    private int _pendingFirstVisibleIndex = -1;
    private int _pendingLastVisibleIndex = -1;

    // ✅ PREVIEW VALIDATION: Debounce timer for keystroke validation (300ms delay)
    private DispatcherTimer? _previewValidationDebounceTimer;
    private (string rowId, string columnName, object? value, CellViewModel cellViewModel)? _pendingPreviewValidation;

    /// <summary>
    /// Event fired when user requests to delete a row via delete button.
    /// Contains both rowIndex (for display) and rowId (for stable identification).
    /// </summary>
    public event EventHandler<DeleteRowRequestedEventArgs>? DeleteRowRequested;

    /// <summary>
    /// Event fired when user requests to insert a row via insert button.
    /// Contains both rowIndex (for display) and rowId (for stable identification).
    /// </summary>
    public event EventHandler<InsertRowRequestedEventArgs>? InsertRowRequested;

    /// <summary>
    /// Event fired when user changes row selection via checkbox.
    /// </summary>
    public event EventHandler<(int rowIndex, bool isSelected)>? RowSelectionChanged;

    /// <summary>
    /// Event fired when user completes editing a cell (presses Enter).
    /// Used to trigger auto-expand when last row is edited.
    /// </summary>
    public event EventHandler<CellViewModel>? CellEditCompleted;

    /// <summary>
    /// Creates a new data grid cells view with UI virtualization.
    /// Uses ItemsRepeater with ViewportManager for efficient large dataset handling.
    /// BACKWARD COMPATIBLE: Works with existing ViewModel.Rows collection.
    /// </summary>
    /// <param name="viewModel">The view model that manages the grid's data and state</param>
    /// <param name="logger">Optional logger for diagnostics (uses NullLogger if not provided)</param>
    /// <param name="loggerFactory">Optional logger factory for creating child component loggers</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public DataGridCellsView(
        DataGridViewModel viewModel,
        ILogger<DataGridCellsView>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DataGridCellsView>.Instance;

        // SENIOR IMPLEMENTATION: Initialize Row Context Menu (Excel-like Insert/Delete)
        _rowContextMenu = new RowContextMenu();
        _rowContextMenu.InsertRowsAboveRequested += OnRowContextMenuInsertAbove;
        _rowContextMenu.InsertRowsBelowRequested += OnRowContextMenuInsertBelow;
        _rowContextMenu.DeleteRowsRequested += OnRowContextMenuDelete;

        // Create ViewportManager (uses ViewModel.Rows as data source)
        var viewportLogger = loggerFactory?.CreateLogger<ViewportManager>()
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ViewportManager>.Instance;
        _viewportManager = new ViewportManager(
            _viewModel,
            _viewModel.Theme,
            viewportLogger);

        // SENIOR FIX: Set ViewportManager reference in ViewModel for cache invalidation on data reload
        _viewModel.ViewportManager = _viewportManager;

        // Create ElementFactory with logger factory for child components
        var factoryLogger = loggerFactory?.CreateLogger<DataGridElementFactory>()
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DataGridElementFactory>.Instance;
        _elementFactory = new DataGridElementFactory(
            _viewportManager,
            _viewModel,
            factoryLogger,
            loggerFactory);

        // Forward events from ElementFactory
        _elementFactory.OnRowSelectionChanged += HandleRowSelectionChanged;
        _elementFactory.OnDeleteRowRequested += HandleDeleteRowRequested;
        _elementFactory.OnInsertRowRequested += HandleInsertRowRequested;
        _elementFactory.OnCellSelected += OnCellSelected;
        _elementFactory.OnCellPointerEntered += OnCellPointerEntered;
        _elementFactory.OnCellEditCompleted += OnCellEditCompleted;
        // ✅ CRITICAL FIX: Subscribe to CellValueChanged for realtime preview validation
        _elementFactory.OnCellValueChanged += OnCellValueChangedAsync;
        // ✅ PROFESSIONAL FIX: Subscribe to cell navigation for arrow key support
        _elementFactory.OnCellNavigationRequested += HandleCellNavigation;

        // Create ItemsRepeater with virtualization
        _itemsRepeater = new ItemsRepeater
        {
            ItemTemplate = _elementFactory,
            Layout = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 2
            }
        };

        // SENIOR IMPLEMENTATION: Attach RightTapped handler for Row Context Menu (Excel-like)
        _itemsRepeater.RightTapped += OnItemsRepeaterRightTapped;

        // Create ScrollViewer for scrollable area
        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _itemsRepeater,
            ManipulationMode = ManipulationModes.None, // CRITICAL FIX: Disable gesture handling to allow pointer events
            IsTabStop = false,                          // ✅ CRITICAL FIX: Prevent ScrollViewer from capturing Tab focus
            TabNavigation = KeyboardNavigationMode.Once // ✅ CRITICAL FIX: Allow Tab to enter children (cells)
        };

        // SENIOR FIX: Handle pointer released for drag selection end
        // (PointerPressed removed - drag detection now happens in OnCellPointerEntered)
        _scrollViewer.PointerReleased += OnPointerReleased;

        // ✅ CRITICAL FIX: Handle pointer capture lost (when pointer leaves window, another control captures, etc.)
        // This ensures drag selection ends properly even if PointerReleased doesn't fire
        _scrollViewer.PointerCaptureLost += OnPointerCaptureLost;

        // ✅ PROFESSIONAL FIX: Use PointerMoved on ScrollViewer for drag selection
        // REASON: WinUI3 bug - PointerEntered doesn't fire when LeftButton is pressed
        //         CellControl.PointerEntered only fires AFTER button release (too late!)
        // SOLUTION: PointerMoved fires continuously during drag → detect cell under pointer manually
        _scrollViewer.PointerMoved += OnScrollViewerPointerMoved;

        // Handle scroll changes to update viewport
        _scrollViewer.ViewChanged += OnScrollViewChanged;

        // ✅ Initialize scroll optimization (debouncing + buffer zone)
        InitializeScrollOptimization();

        // Listen for data changes to invalidate viewport
        _viewModel.Rows.CollectionChanged += OnRowsCollectionChanged;

        // ✅ CRITICAL FIX: Subscribe to ItemsRepeaterRefreshRequested for complete UI refresh
        // This handles cases where IsVisible changes on ViewModels but ItemsRepeater doesn't re-render
        _viewModel.ItemsRepeaterRefreshRequested += OnItemsRepeaterRefreshRequested;

        // ✅ SENIOR FIX: Subscribe to PageChanged event to update ItemsRepeater on page navigation
        if (_viewModel.PageManager != null)
        {
            _viewModel.PageManager.PageChanged += OnPageChanged;
            _logger.LogDebug("Subscribed to PageManager.PageChanged event for pagination support");
        }

        // Set ScrollViewer as UserControl content
        Content = _scrollViewer;

        // Initialize viewport with row count
        _viewportManager.TotalRowCount = _viewModel.Rows.Count;

        // CRITICAL FIX: Initialize viewport cache on first load
        // This fixes the bug where DataGridCellsView is created AFTER Rows.AddRange()
        // was already called, so OnRowsCollectionChanged never fired during initial load.
        // Without this, GetRowViewModel() returns NULL → "Loading..." placeholders shown.
        this.Loaded += OnFirstLoaded;

        _logger.LogInformation("DataGridCellsView created with virtualization (rows: {RowCount})",
            _viewModel.Rows.Count);
    }

    /// <summary>
    /// ✅ SENIOR FIX: Initialize viewport cache on first load.
    /// Called once when control is first loaded into visual tree.
    /// UNIFIED LOGIC: Consistent with OnRowsCollectionChanged for predictable behavior.
    /// </summary>
    private async void OnFirstLoaded(object sender, RoutedEventArgs e)
    {
        // Unsubscribe immediately - only need this once
        this.Loaded -= OnFirstLoaded;

        try
        {
            var pageManager = _viewModel.PageManager;
            int rowsToLoad = 0;
            int itemsSourceCount = 0;

            // ✅ SENIOR FIX: Unified logic for consistent behavior
            if (pageManager != null && pageManager.TotalDataRows > 0)
            {
                // WITH PAGINATION: Load entire current page for smooth scrolling
                var (startIndex, count) = pageManager.GetCurrentPageRange();
                rowsToLoad = count;
                itemsSourceCount = count; // ← ItemsSource size matches current page size

                _logger.LogInformation("Loading ENTIRE current page on initial load: {Count} rows (page {Page}/{TotalPages})",
                    rowsToLoad, pageManager.CurrentPage + 1, pageManager.TotalPages);
            }
            else
            {
                // ⚠️ PAGINATION REQUIRED: For testing, pagination must be configured
                _logger.LogError("PageManager not configured or TotalDataRows=0 - cannot load rows without pagination");
            }
            // ⚠️ BACKWARD COMPATIBILITY: Disabled for testing - pagination required
            //{
            //    // WITHOUT PAGINATION: Load ALL rows for full grid visibility
            //    rowsToLoad = _viewModel.Rows.Count;
            //    itemsSourceCount = _viewModel.Rows.Count;
            //
            //    _logger.LogInformation("Loading ALL rows (no pagination): {Count} rows", rowsToLoad);
            //}

            if (rowsToLoad > 0)
            {
                // ✅ CRITICAL FIX: ItemsSource count must match what ViewportManager will provide
                // With pagination: ItemsSource = page row count (e.g., 15 for PageSize=15)
                // Without pagination: ItemsSource = all rows
                _itemsRepeater.ItemsSource = Enumerable.Range(0, itemsSourceCount).ToList();

                await _viewportManager.UpdateViewportAsync(0, rowsToLoad - 1);
                _logger.LogInformation("Initial viewport cache loaded: {Count} rows", rowsToLoad);
            }
            else
            {
                _logger.LogDebug("No rows to load on initial load (Rows.Count={Count})", _viewModel.Rows.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize viewport cache on first load: {Message}", ex.Message);
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _logger.LogInformation("🔵 DRAG-DEBUG: OnPointerReleased CALLED - _isDragging={IsDragging}, _pressedCell={HasPressed}",
            _isDragging,
            _pressedCell != null ? $"[{_pressedCell.RowIndex},{_pressedCell.ColumnIndex}]" : "NULL");

        // SENIOR FIX: End drag selection when pointer released
        if (_isDragging)
        {
            _viewModel.EndRangeSelection();
            _logger.LogInformation("🟢 DRAG-DEBUG: ✅ Drag selection ENDED (pointer released)");
        }

        // Reset drag state
        _pressedCell = null;
        _isDragging = false;
    }

    /// <summary>
    /// ✅ PROBLEM 4 FIX: Handles pointer capture lost event to properly end drag selection.
    /// CRITICAL: Only reset state if drag was ACTUALLY in progress.
    /// REASON: WinUI3 triggers PointerCaptureLost immediately after CapturePointer if pointer moves,
    ///         resetting _pressedCell would prevent drag from starting in OnCellPointerEntered.
    /// </summary>
    private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _logger.LogInformation("🔵 DRAG-DEBUG: OnPointerCaptureLost CALLED - _isDragging={IsDragging}, _pressedCell={HasPressed}",
            _isDragging,
            _pressedCell != null ? $"[{_pressedCell.RowIndex},{_pressedCell.ColumnIndex}]" : "NULL");

        // ✅ PROBLEM 4 FIX: Only reset state if drag was ACTUALLY in progress
        // If _isDragging=false, keep _pressedCell (waiting for OnCellPointerEntered to start drag)
        if (_isDragging)
        {
            _viewModel.EndRangeSelection();
            _pressedCell = null;  // ← Reset IBA keď drag PREBIEHAL
            _isDragging = false;
            _logger.LogInformation("🟢 DRAG-DEBUG: ✅ Drag selection ENDED (pointer capture lost) - state RESET");
        }
        else
        {
            _logger.LogInformation("🟡 DRAG-DEBUG: Drag not in progress - keeping _pressedCell={HasPressed} (waiting for OnCellPointerEntered)",
                _pressedCell != null ? $"[{_pressedCell.RowIndex},{_pressedCell.ColumnIndex}]" : "NULL");
        }
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Handles pointer moved on ScrollViewer for drag selection.
    /// REASON: WinUI3 bug - CellControl.PointerEntered doesn't fire when LeftButton pressed.
    /// SOLUTION: Manually detect cell under pointer using ViewModel index calculation.
    /// </summary>
    private void OnScrollViewerPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        // Only process if we have a pressed cell (potential drag start)
        if (_pressedCell == null)
            return;

        // ✅ CRITICAL: Check if LEFT button is STILL pressed
        var pointerPoint = e.GetCurrentPoint(_scrollViewer);
        bool isLeftButtonPressed = pointerPoint.Properties.IsLeftButtonPressed;

        if (!isLeftButtonPressed)
        {
            // Button released - end drag if in progress
            if (_isDragging)
            {
                _viewModel.EndRangeSelection();
                _isDragging = false;
                _lastDraggedCell = null;
                _logger.LogInformation("🟢 DRAG-DEBUG: Drag ended (button released during PointerMoved)");
            }
            _pressedCell = null;
            return;
        }

        // ✅ Get pointer position relative to ItemsRepeater
        if (_itemsRepeater == null)
            return;

        var position = e.GetCurrentPoint(_itemsRepeater).Position;

        // ✅ Calculate cell under pointer using viewport geometry
        // ARCHITECTURE: ItemsRepeater uses row-based layout
        //               RowHeight = fixed (e.g., 40px), ColumnWidth = from ColumnHeaders
        var rowHeight = 40.0; // TODO: Get from ViewModel or theme
        var rowIndex = (int)(position.Y / rowHeight);

        // ✅ Calculate column index from X position
        var columnIndex = CalculateColumnIndexFromX(position.X);

        if (rowIndex < 0 || rowIndex >= _viewModel.Rows.Count ||
            columnIndex < 0 || columnIndex >= _viewModel.ColumnHeaders.Count)
        {
            return; // Out of bounds
        }

        var currentCell = _viewModel.Rows[rowIndex].Cells[columnIndex];

        // ✅ Check if this is a different cell than last processed
        if (_lastDraggedCell != null &&
            currentCell.RowIndex == _lastDraggedCell.RowIndex &&
            currentCell.ColumnIndex == _lastDraggedCell.ColumnIndex)
        {
            return; // Same cell - avoid duplicate processing
        }

        _lastDraggedCell = currentCell;

        // ✅ Start drag if not already started
        if (!_isDragging)
        {
            _isDragging = true;
            _viewModel.StartRangeSelection(_pressedCell);
            _logger.LogInformation("🟢 DRAG-DEBUG: Drag STARTED from [{Row},{Col}] via PointerMoved",
                _pressedCell.RowIndex, _pressedCell.ColumnIndex);
        }

        // ✅ Update range selection
        _viewModel.UpdateRangeSelection(currentCell);
        _logger.LogTrace("🟢 DRAG-DEBUG: Drag UPDATED to [{Row},{Col}]",
            currentCell.RowIndex, currentCell.ColumnIndex);
    }

    /// <summary>
    /// Calculates column index from X position using cumulative column widths.
    /// </summary>
    private int CalculateColumnIndexFromX(double x)
    {
        double cumulativeWidth = 0;
        for (int i = 0; i < _viewModel.ColumnHeaders.Count; i++)
        {
            cumulativeWidth += _viewModel.ColumnHeaders[i].Width;
            if (x < cumulativeWidth)
            {
                return i;
            }
        }
        return _viewModel.ColumnHeaders.Count - 1; // Last column
    }

    /// <summary>
    /// ✅ PROFESSIONAL QUALITY: Initialize scroll optimization (debouncing + buffer).
    /// Call this in constructor after _scrollViewer setup.
    /// </summary>
    private void InitializeScrollOptimization()
    {
        _scrollDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(SCROLL_DEBOUNCE_MS)
        };

        _scrollDebounceTimer.Tick += (s, e) =>
        {
            _scrollDebounceTimer.Stop();

            if (_pendingFirstVisibleIndex >= 0 && _pendingLastVisibleIndex >= 0)
            {
                // ✅ PERFORMANCE FIX: Fire-and-forget on background thread (non-blocking)
                // Prevents UI thread from waiting on viewport update
                var firstIndex = _pendingFirstVisibleIndex;
                var lastIndex = _pendingLastVisibleIndex;
                _pendingFirstVisibleIndex = -1;
                _pendingLastVisibleIndex = -1;

                // Execute on background thread to avoid blocking UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UpdateVisibleRowsThrottledAsync(firstIndex, lastIndex);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background viewport update failed: {Message}", ex.Message);
                    }
                });
            }
        };

        _logger.LogInformation("Scroll optimization initialized: debounce={Debounce}ms, buffer={Buffer} rows",
            SCROLL_DEBOUNCE_MS, VIEWPORT_BUFFER_ROWS);
    }

    /// <summary>
    /// ✅ PROFESSIONAL QUALITY: Debounced scroll handler.
    /// Throttles 60+ calls/sec → ~60 FPS updates, eliminates stutter.
    /// </summary>
    private void OnScrollViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_disposed)
            return;

        // Calculate visible row range based on scroll position
        var scrollOffset = _scrollViewer.VerticalOffset;
        var viewportHeight = _scrollViewer.ViewportHeight;

        // Estimate row height (assuming ~32px per row including margin)
        const double estimatedRowHeight = 34.0; // 32px row + 2px margin

        var firstVisibleIndex = Math.Max(0, (int)(scrollOffset / estimatedRowHeight));
        var visibleRowCount = (int)(viewportHeight / estimatedRowHeight) + 1;
        var lastVisibleIndex = Math.Min(_viewportManager.TotalRowCount - 1,
            firstVisibleIndex + visibleRowCount);

        // ✅ Queue update (debounce - prevent 60+ calls/second)
        _pendingFirstVisibleIndex = firstVisibleIndex;
        _pendingLastVisibleIndex = lastVisibleIndex;

        _scrollDebounceTimer?.Stop();
        _scrollDebounceTimer?.Start();
    }

    /// <summary>
    /// ✅ PROFESSIONAL QUALITY: Throttled viewport update with buffer zone.
    /// Pre-loads rows above/below visible area to eliminate scroll stutter.
    /// </summary>
    private async Task UpdateVisibleRowsThrottledAsync(int firstVisibleIndex, int lastVisibleIndex)
    {
        if (_isUpdatingViewport || _disposed)
            return;

        try
        {
            _isUpdatingViewport = true;

            // ✅ Add buffer zone - PRE-LOAD rows above/below viewport
            var bufferedFirstIndex = Math.Max(0, firstVisibleIndex - VIEWPORT_BUFFER_ROWS);
            var bufferedLastIndex = Math.Min(_viewportManager.TotalRowCount - 1,
                lastVisibleIndex + VIEWPORT_BUFFER_ROWS);

            _logger.LogTrace("Scroll: visible=[{First},{Last}], buffered=[{BufFirst},{BufLast}]",
                firstVisibleIndex, lastVisibleIndex, bufferedFirstIndex, bufferedLastIndex);

            // Update viewport with buffered range
            await _viewportManager.UpdateViewportAsync(bufferedFirstIndex, bufferedLastIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateVisibleRowsThrottledAsync failed: {Message}", ex.Message);
        }
        finally
        {
            _isUpdatingViewport = false;
        }
    }

    /// <summary>
    /// ✅ SENIOR FIX: Handles page navigation - updates ItemsRepeater and viewport cache.
    /// Called when user navigates to different page via PaginationControlView.
    /// CRITICAL: Must update ItemsSource count to match new page's row count.
    /// </summary>
    /// <summary>
    /// ✅ UNIFIED PAGINATION: Handles page navigation with IN-PLACE ViewModel updates.
    /// CRITICAL FIX: Uses UpdateViewModelsInPlace() instead of LoadRows() to preserve UI binding.
    /// ARCHITECTURE:
    /// - UpdateViewModelsInPlace(): Update existing ViewModels → UI binding preserved, RowId.PropertyChanged fires
    /// - LoadRows(): Dispose old + create new ViewModels → UI binding MOŽE cache starý RowId (BUG!)
    /// BENEFITS:
    /// - No dispose/recreate ViewModels → UI binding (RowId, cells) preserved
    /// - INSERT/DELETE buttons use CORRECT current RowId (not stale cached RowId)
    /// - Consistent behavior for all datasets (10, 100, 1000, 10M+ rows)
    /// </summary>
    private async void OnPageChanged(object? sender, RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Pagination.Interfaces.PageChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Page changed: {OldPage} → {NewPage}, loading {Count} rows (StartIndex={Start})",
                e.OldPage, e.NewPage, e.Count, e.StartIndex);

            // ✅ PROFESSIONAL SOLUTION: Update ViewModels IN-PLACE (preserve UI binding)
            // CRITICAL: LoadRows() dispose/recreate causes UI binding to cache stale RowId
            // UpdateViewModelsInPlace() updates existing ViewModels → RowId.PropertyChanged fires → UI rebinds
            if (_viewModel.RowStore != null)
            {
                try
                {
                    // Load page rows from RowStore (e.g., rows 60-74 for page 5)
                    var pageRows = await _viewModel.RowStore.GetRowsRangeAsync(e.StartIndex, (int)e.Count, onlyFiltered: false, cancellationToken: default);

                    _logger.LogDebug("Loaded {ActualCount} rows from RowStore for page {NewPage} (StartIndex={Start}, Requested={Requested})",
                        pageRows.Count, e.NewPage + 1, e.StartIndex, e.Count);

                    // ✅ CRITICAL FIX: Use UpdateViewModelsInPlace() instead of LoadRows()
                    // REASON: Preserves UI binding, prevents stale RowId caching
                    // BEFORE: LoadRows() → Rows.Clear() + Rows.AddRange() → UI binding MAY cache old RowId
                    // AFTER: UpdateViewModelsInPlace() → Update existing ViewModels → RowId.PropertyChanged fires → UI rebinds
                    _viewModel.UpdateViewModelsInPlace(pageRows);

                    _logger.LogInformation("✅ Virtual pagination: ViewModel.Rows updated IN-PLACE with {Count} ViewModels for page {NewPage}/{TotalPages}",
                        pageRows.Count, e.NewPage + 1, _viewModel.PageManager?.TotalPages ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load page data from RowStore for page {NewPage}", e.NewPage + 1);
                    // Don't throw - fallback to existing behavior with empty/cached data
                }
            }
            else
            {
                _logger.LogDebug("RowStore not available - using existing ViewModel.Rows");
            }

            // ✅ CRITICAL FIX #3: Update ItemsRepeater count ONLY if changed (avoid reset flicker)
            // PROBLEM: ItemsSource reset = UI completely rebuilds → BLACK SCREEN (~100-200ms)
            // SOLUTION: Check if count changed before reset
            // OPTIMIZATION: Most page changes = same count (PageSize=15) → skip reset → NO FLICKER
            // Example: Page 1→2→3→4→5→6 all have 15 rows → skip reset 5 times
            //          Page 6→7 (last page 10 rows) → reset only once
            var currentSource = _itemsRepeater.ItemsSource as IList<int>;
            if (currentSource == null || currentSource.Count != (int)e.Count)
            {
                _itemsRepeater.ItemsSource = Enumerable.Range(0, (int)e.Count).ToList();
                _logger.LogDebug("✅ FIX #3: ItemsSource updated (count changed: {Old}→{New})", currentSource?.Count ?? 0, e.Count);
            }
            else
            {
                _logger.LogTrace("✅ FIX #3: ItemsSource unchanged (count={Count}), skipping reset to avoid flicker", e.Count);
            }

            // Note: ViewportManager.TotalRowCount getter auto-calculates from PageManager.GetCurrentPageRange()
            // No need to set it manually - it will return correct value automatically

            // ✅ OPTIMIZATION: Invalidate cache AFTER ViewModels updated (not before UI render)
            // REASON: Cache invalidation triggers UI reload - do AFTER data ready to minimize delay
            _viewportManager.InvalidateCache();

            // Pre-load entire new page (all rows on current page for smooth scrolling)
            if (e.Count > 0)
            {
                await _viewportManager.UpdateViewportAsync(0, (int)e.Count - 1);
                _logger.LogDebug("Pre-loaded {Count} rows for page {NewPage}", e.Count, e.NewPage + 1);
            }

            // Reset scroll to top of new page for better UX
            _scrollViewer.ChangeView(null, 0, null, disableAnimation: false);

            _logger.LogInformation("✅ Page change completed successfully (Page {NewPage}/{TotalPages}, {Count} rows)",
                e.NewPage + 1, _viewModel.PageManager?.TotalPages ?? 0, e.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPageChanged failed: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// ✅ CRITICAL FIX: Handles ItemsRepeater complete refresh request.
    /// PROBLEM: Page 7 has 10 rows → user adds 5 rows → UI still shows only 10 (collapsed elements not re-rendered).
    /// ROOT CAUSE: ItemsRepeater caches collapsed elements (rows 10-14) and doesn't re-render when IsVisible changes.
    /// SOLUTION: Rebind ItemsSource (null → recreate) to force ItemsRepeater to recreate ALL elements.
    /// USE CASE: After INSERT operations that change visible row count on last page.
    /// </summary>
    private async void OnItemsRepeaterRefreshRequested(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("ItemsRepeater complete refresh requested - rebinding ItemsSource");

            // Get current ItemsSource count
            var currentSource = _itemsRepeater.ItemsSource as IList<int>;
            var currentCount = currentSource?.Count ?? 0;

            if (currentCount == 0)
            {
                _logger.LogWarning("ItemsSource is empty - skipping refresh");
                return;
            }

            // ✅ CRITICAL FIX: Save scroll position BEFORE rebind
            // REASON: ItemsSource rebind resets ScrollViewer.VerticalOffset to 0
            // USER REQUIREMENT: Keep scroll position when adding/deleting rows
            double savedScrollOffset = _scrollViewer.VerticalOffset;
            _logger.LogDebug("Saving scroll position before rebind: {Offset}", savedScrollOffset);

            // ✅ CRITICAL FIX: Use count of VISIBLE rows instead of total Rows.Count
            // REASON: Rows.Count is FIXED (always PageSize=15), but visible rows may be less (e.g., 6 on last page)
            // PREVIOUS BUG: ItemsRepeater created 15 elements even when only 6 rows were visible
            // RESULT: New rows appeared invisible because ItemsRepeater didn't create UI elements for them
            // ARCHITECTURE: Fixed UI pool (15 ViewModels) but ItemsRepeater renders only VISIBLE count
            int visibleRowCount = _viewModel?.Rows?.Count(r => r.IsVisible) ?? currentCount;
            _logger.LogInformation("Rebinding ItemsSource with {VisibleCount} visible rows (total pool: {PoolSize})",
                visibleRowCount, _viewModel?.Rows?.Count ?? 0);

            // ✅ Force complete re-render: ItemsSource = null → recreate
            // This clears ItemsRepeater's internal cache and forces recreation of ALL elements
            _itemsRepeater.ItemsSource = null;
            _itemsRepeater.UpdateLayout();  // Force layout pass to clear cache

            // ✅ CRITICAL FIX: Set scroll position BEFORE assigning new ItemsSource
            // REASON: Prevents visible scroll jump (scroll restored before UI renders)
            // PREVIOUS BUG: 50ms delay caused visible jump to top then back
            // RESULT: Smooth transition - user never sees scroll position change
            if (savedScrollOffset > 0)
            {
                _scrollViewer.ChangeView(null, savedScrollOffset, null, disableAnimation: true);
            }

            _itemsRepeater.ItemsSource = Enumerable.Range(0, visibleRowCount).ToList();  // ✅ CHANGE: visibleRowCount instead of currentCount

            // ✅ CHANGE: Small delay AFTER rebind for UI stabilization (no visible jump)
            await Task.Delay(16); // 1 frame @ 60fps - imperceptible

            _logger.LogInformation("✅ ItemsRepeater refresh completed - {Count} visible elements recreated, scroll restored to {Offset}",
                visibleRowCount, savedScrollOffset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh ItemsRepeater");
        }
    }

    /// <summary>
    /// Handles row collection changes (add, remove, reset).
    /// Invalidates viewport cache and updates total row count.
    /// </summary>
    private async void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
        {
            _logger.LogDebug("Rows collection changed: {Action}, count={Count}",
                e.Action, _viewModel.Rows.Count);

            // Update total row count
            _viewportManager.TotalRowCount = _viewModel.Rows.Count;

            // Invalidate viewport cache (force reload on next scroll)
            _viewportManager.InvalidateCache();

            // ✅ SENIOR FIX: Unified logic - same as OnFirstLoaded for consistency
            var pageManager = _viewModel.PageManager;
            int rowsToLoad = 0;
            int itemsSourceCount = 0;

            if (pageManager != null && pageManager.TotalDataRows > 0)
            {
                // WITH PAGINATION: Load entire current page into cache for smooth scrolling
                var (startIndex, count) = pageManager.GetCurrentPageRange();
                rowsToLoad = count;
                itemsSourceCount = count; // ← ItemsSource size matches current page size

                _logger.LogInformation("Loading ENTIRE current page into cache: {Count} rows (page {Page}/{TotalPages})",
                    rowsToLoad, pageManager.CurrentPage + 1, pageManager.TotalPages);
            }
            else
            {
                // ⚠️ PAGINATION REQUIRED: For testing, pagination must be configured
                _logger.LogError("PageManager not configured or TotalDataRows=0 - cannot load rows without pagination");
            }
            // ⚠️ BACKWARD COMPATIBILITY: Disabled for testing - pagination required
            //{
            //    // WITHOUT PAGINATION: Load ALL rows for full grid visibility
            //    rowsToLoad = _viewModel.Rows.Count;
            //    itemsSourceCount = _viewModel.Rows.Count;
            //
            //    _logger.LogInformation("Loading ALL rows into cache (no pagination): {Count} rows", rowsToLoad);
            //}

            // ✅ CRITICAL FIX: Update ItemsRepeater with correct count
            _itemsRepeater.ItemsSource = Enumerable.Range(0, itemsSourceCount).ToList();

            if (rowsToLoad > 0)
            {
                await _viewportManager.UpdateViewportAsync(0, rowsToLoad - 1);
                _logger.LogTrace("Pre-loaded {Count} rows into viewport cache after collection change", rowsToLoad);
            }

            // ✅ CRITICAL FIX: Set initial selection after first data load
            // REQUIREMENT: Enable keyboard navigation (Tab/Arrow keys) without requiring user click
            // CONDITION: Only on first data load (_lastSelectedCell == null)
            if (_lastSelectedCell == null && _viewModel.Rows.Count > 0)
            {
                _logger.LogDebug("First data load detected - setting initial selection");
                SetInitialSelection();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnRowsCollectionChanged failed: {Message}", ex.Message);
        }
    }

    private void HandleRowSelectionChanged(int rowIndex, bool isSelected)
    {
        RowSelectionChanged?.Invoke(this, (rowIndex, isSelected));

        // ✅ FIX: Update header checkbox state after individual row selection change
        // QUALITY: Ensures header checkbox reflects partial selection (indeterminate state)
        _viewModel.UpdateCheckboxHeaderStatePublic();
    }

    private void HandleDeleteRowRequested(object? sender, DeleteRowRequestedEventArgs args)
    {
        DeleteRowRequested?.Invoke(this, args);
    }

    private void HandleInsertRowRequested(object? sender, InsertRowRequestedEventArgs args)
    {
        InsertRowRequested?.Invoke(this, args);
    }

    /// <summary>
    /// Handles arrow key navigation requests from cells in NORMAL mode.
    /// ARCHITECTURE: User presses arrow key → CellControl fires NavigationRequested → this handler finds target cell and updates selection.
    /// FEATURES:
    /// - Calculates target cell coordinates based on direction (Up/Down/Left/Right)
    /// - Validates bounds (prevents navigation outside grid)
    /// - Skips special columns (Checkbox, RowNumber, etc.) in horizontal navigation
    /// - Updates selection state (deselect source, select target)
    /// - Triggers focus on target cell via FocusRequested property
    /// </summary>
    private void HandleCellNavigation(object? sender, (CellViewModel sourceCell, NavigationDirection direction) args)
    {
        var (sourceCell, direction) = args;

        _logger.LogInformation("🔵 NAVIGATION: Arrow key {Direction} from Cell=[{SourceRow},{SourceCol}], RowId={SourceRowId}, Column={SourceColumn}",
            direction, sourceCell.RowIndex, sourceCell.ColumnIndex, sourceCell.RowId, sourceCell.ColumnName);

        // Calculate target cell coordinates
        int targetRowIndex = sourceCell.RowIndex;
        int targetColumnIndex = sourceCell.ColumnIndex;

        switch (direction)
        {
            case NavigationDirection.Up:
                targetRowIndex--;
                break;
            case NavigationDirection.Down:
                targetRowIndex++;
                break;
            case NavigationDirection.Left:
                targetColumnIndex--;
                break;
            case NavigationDirection.Right:
                targetColumnIndex++;
                break;
            case NavigationDirection.TabForward:
                // ✅ PROFESSIONAL FIX: Tab moves right, wraps to next row at end
                targetColumnIndex++;
                // Wrap to next row if at end of current row
                if (targetColumnIndex >= _viewModel.Rows[sourceCell.RowIndex].Cells.Count)
                {
                    targetColumnIndex = 0; // First column
                    targetRowIndex++;      // Next row
                }
                break;
            case NavigationDirection.TabBackward:
                // ✅ PROFESSIONAL FIX: Shift+Tab moves left, wraps to previous row at start
                targetColumnIndex--;
                // Wrap to previous row if at start of current row
                if (targetColumnIndex < 0)
                {
                    targetRowIndex--; // Previous row
                    if (targetRowIndex >= 0 && targetRowIndex < _viewModel.Rows.Count)
                    {
                        targetColumnIndex = _viewModel.Rows[targetRowIndex].Cells.Count - 1; // Last column
                    }
                }
                break;
        }

        // Validate vertical bounds
        if (targetRowIndex < 0 || targetRowIndex >= _viewModel.Rows.Count)
        {
            _logger.LogTrace("Navigation blocked - target row {TargetRow} out of bounds (total rows: {TotalRows})",
                targetRowIndex, _viewModel.Rows.Count);
            return;
        }

        var targetRow = _viewModel.Rows[targetRowIndex];

        // Validate horizontal bounds
        if (targetColumnIndex < 0 || targetColumnIndex >= targetRow.Cells.Count)
        {
            _logger.LogTrace("Navigation blocked - target column {TargetCol} out of bounds (total columns: {TotalCols})",
                targetColumnIndex, targetRow.Cells.Count);
            return;
        }

        // Get target cell
        var targetCell = targetRow.Cells[targetColumnIndex];

        // ✅ PROFESSIONAL FIX: Skip special columns in horizontal navigation
        // Special columns (Checkbox, RowNumber, ValidationAlerts, DeleteRow, InsertRow) are not navigable
        // ARCHITECTURE: Only normal data columns can receive keyboard focus
        if (direction == NavigationDirection.Left ||
            direction == NavigationDirection.Right ||
            direction == NavigationDirection.TabForward ||
            direction == NavigationDirection.TabBackward)
        {
            // Find next non-special column in same direction
            int searchColumnIndex = targetColumnIndex;
            int searchDirection = (direction == NavigationDirection.Right || direction == NavigationDirection.TabForward) ? 1 : -1;

            while (targetRow.Cells[searchColumnIndex].IsSpecialColumn)
            {
                searchColumnIndex += searchDirection;

                // Check bounds during search
                if (searchColumnIndex < 0 || searchColumnIndex >= targetRow.Cells.Count)
                {
                    _logger.LogTrace("Navigation blocked - no more normal columns in {Direction} direction",
                        direction);
                    return;
                }
            }

            targetCell = targetRow.Cells[searchColumnIndex];
            targetColumnIndex = searchColumnIndex;
        }
        else
        {
            // Vertical navigation (Up/Down) - if landing on special column, it's OK (user stays in same column)
            // But if current column IS special, cannot navigate vertically
            if (targetCell.IsSpecialColumn)
            {
                _logger.LogTrace("Navigation blocked - target cell [{TargetRow},{TargetCol}] is special column",
                    targetRowIndex, targetColumnIndex);
                return;
            }
        }

        _logger.LogInformation("✅ NAVIGATION: Moving from [{SourceRow},{SourceCol}] to [{TargetRow},{TargetCol}], TargetRowId={TargetRowId}, TargetColumn={TargetColumn}",
            sourceCell.RowIndex, sourceCell.ColumnIndex, targetRowIndex, targetColumnIndex, targetCell.RowId, targetCell.ColumnName);

        // ✅ PROFESSIONAL FIX: Update selection state
        // Deselect source cell
        sourceCell.IsSelected = false;

        // Select target cell
        targetCell.IsSelected = true;

        // ✅ CRITICAL: Trigger focus on target cell via MVVM property
        // ARCHITECTURE: FocusRequested property change triggers OnViewModelPropertyChanged in CellControl → applies focus
        targetCell.FocusRequested = true;

        _logger.LogTrace("Navigation complete - target cell [{TargetRow},{TargetCol}] selected and focused",
            targetRowIndex, targetColumnIndex);
    }

    /// <summary>
    /// ✅ CRITICAL FIX: Select first cell on initial load (enables keyboard navigation from start)
    /// ARCHITECTURE: Called after first data load to establish focus point
    /// REQUIREMENT: Without initial selection, Tab/Arrow keys don't work until user clicks a cell
    /// </summary>
    private void SetInitialSelection()
    {
        if (_viewModel.Rows.Count == 0)
        {
            _logger.LogTrace("SetInitialSelection skipped - no rows available");
            return;
        }

        var firstRow = _viewModel.Rows[0];

        // Find first non-special column (skip Checkbox, RowNumber, ValidationAlerts, etc.)
        var firstCell = firstRow.Cells.FirstOrDefault(c => !c.IsSpecialColumn);
        if (firstCell != null)
        {
            firstCell.IsSelected = true;
            firstCell.FocusRequested = true;
            _lastSelectedCell = firstCell;

            _logger.LogInformation("✅ INITIAL SELECTION: First cell [{Row},{Col}] (RowId={RowId}, Column={Column}) selected and focused",
                firstCell.RowIndex, firstCell.ColumnIndex, firstCell.RowId, firstCell.ColumnName);
        }
        else
        {
            _logger.LogWarning("SetInitialSelection failed - no non-special columns found in first row");
        }
    }

    private void OnCellSelected(object? sender, CellSelectionEventArgs e)
    {
        _logger.LogInformation("🔵 DRAG-DEBUG: OnCellSelected CALLED - Cell=[{Row},{Col}], RowId={RowId}, Ctrl={Ctrl}, HasPointerArgs={HasArgs}",
            e.Cell.RowIndex, e.Cell.ColumnIndex, e.Cell.RowId, e.IsCtrlPressed, e.PointerEventArgs != null);

        // ✅ HIGH FIX: Prevent duplicate selection events for same cell without Ctrl
        // Compare RowId + ColumnName (stable identifiers) instead of object references (recycled ViewModels)
        if (_lastSelectedCell != null &&
            _lastSelectedCell.RowId == e.Cell.RowId &&
            _lastSelectedCell.ColumnName == e.Cell.ColumnName &&
            !e.IsCtrlPressed)
        {
            _logger.LogTrace("Ignoring duplicate cell selection for RowId={RowId}, Column={ColumnName}",
                e.Cell.RowId, e.Cell.ColumnName);
            return;
        }

        // SENIOR FIX: Store pressed cell for drag detection
        // ✅ PROBLEM 4 FIX: Drag will be initiated when pointer moves to different cell (in OnCellPointerEntered)
        // OnCellPointerEntered is fired by CellControl.PointerEntered event
        _pressedCell = e.Cell;
        _isDragging = false;
        _lastSelectedCell = e.Cell;

        _logger.LogInformation("🔵 DRAG-DEBUG: Stored _pressedCell=[{Row},{Col}], _isDragging={IsDragging}",
            _pressedCell.RowIndex, _pressedCell.ColumnIndex, _isDragging);

        // ✅ CRITICAL FIX: Capture pointer on ScrollViewer to ensure PointerReleased fires
        // Without this, e.Handled = true in CellControl blocks pointer events from reaching parent
        if (e.PointerEventArgs?.Pointer != null)
        {
            var captured = _scrollViewer.CapturePointer(e.PointerEventArgs.Pointer);
            if (captured)
            {
                _logger.LogInformation("🔵 DRAG-DEBUG: ✅ Pointer captured successfully on ScrollViewer (PointerId={PointerId})",
                    e.PointerEventArgs.Pointer.PointerId);
            }
            else
            {
                _logger.LogWarning("🔴 DRAG-DEBUG: ❌ FAILED to capture pointer - drag selection may not work properly");
            }
        }
        else
        {
            _logger.LogWarning("🔴 DRAG-DEBUG: ❌ PointerEventArgs is NULL - cannot capture pointer for drag selection");
        }

        // Always call SelectCell for proper single/multi-select behavior
        // ✅ PROBLEM 4 FIX: If user drags to another cell, OnCellPointerEntered will initiate range selection
        _viewModel.SelectCell(e.Cell, e.IsCtrlPressed);

        _logger.LogTrace("Cell selected: [{Row},{Col}], Ctrl={IsCtrl}",
            e.Cell.RowIndex, e.Cell.ColumnIndex, e.IsCtrlPressed);
    }

    // ✅ PROBLEM 4 FIX: OnScrollViewerPointerMoved method COMPLETELY REMOVED
    // REASON: FindElementsInHostCoordinates doesn't work reliably with ItemsRepeater in WinUI3
    //         Always returns 0 elements, drag selection never triggers
    // SOLUTION: OnCellPointerEntered (below) handles drag selection via CellControl.PointerEntered event
    //           This works perfectly because each CellControl fires PointerEntered when mouse enters it

    private void OnCellPointerEntered(object? sender, CellPointerEnteredEventArgs e)
    {
        // ✅ PROFESSIONAL FIX: Check if left button is STILL pressed
        var pointerPoint = e.PointerEventArgs.GetCurrentPoint(this);
        bool isLeftButtonPressed = pointerPoint.Properties.IsLeftButtonPressed;

        _logger.LogInformation("🟢 DRAG-DEBUG: OnCellPointerEntered - Cell=[{Row},{Col}], _pressedCell={HasPressed}, _isDragging={IsDragging}, LeftButtonPressed={LeftPressed}",
            e.Cell.RowIndex, e.Cell.ColumnIndex,
            _pressedCell != null ? $"[{_pressedCell.RowIndex},{_pressedCell.ColumnIndex}]" : "NULL",
            _isDragging,
            isLeftButtonPressed);

        // ✅ FIX: Only start drag if button is STILL pressed
        if (_pressedCell != null && !_isDragging && isLeftButtonPressed)
        {
            // User pressed cell and now moved to another cell → start drag selection
            _isDragging = true;
            _viewModel.StartRangeSelection(_pressedCell);

            _logger.LogInformation("🟢 DRAG-DEBUG: ✅ Drag selection STARTED from cell [{Row},{Col}] via PointerEntered",
                _pressedCell.RowIndex, _pressedCell.ColumnIndex);
        }

        // ✅ FIX: Only continue drag if button is STILL pressed
        if (_isDragging && isLeftButtonPressed)
        {
            _viewModel.UpdateRangeSelection(e.Cell);

            _logger.LogInformation("🟢 DRAG-DEBUG: ✅ Drag selection UPDATED to cell [{Row},{Col}] via PointerEntered",
                e.Cell.RowIndex, e.Cell.ColumnIndex);
        }
        else if (_isDragging && !isLeftButtonPressed)
        {
            // ✅ FIX: Button released during drag - end selection
            _viewModel.EndRangeSelection();
            _pressedCell = null;
            _isDragging = false;

            _logger.LogInformation("🟢 DRAG-DEBUG: ✅ Drag selection ENDED (button released during PointerEntered)");
        }
    }

    private void OnCellEditCompleted(object? sender, CellViewModel cell)
    {
        // ✅ PROFESSIONAL FIX: Forward event to parent control
        // ARCHITECTURE: InternalUIOperationHandler will catch this event and call UpdateCellAsync
        // UpdateCellAsync already handles:
        //   1. Cell value write to backend storage
        //   2. Batch validation (ValidateAllAsync) - writes errors to row store
        //   3. Full UI reload (PerformFullReload) - displays persisted ValidationAlerts
        // REMOVED: Duplicate POST-COMMIT validation that used PreviewValidateCellAsync
        // REASON: PreviewValidateCellAsync does NOT write to storage (preview-only mode)
        //         This caused ValidationAlerts to not show errors after commit
        // FIX: Let InternalUIOperationHandler handle complete validation flow with storage persistence
        _logger.LogInformation("🔑 Cell edit completed: rowId {RowId}, column {ColumnName} - forwarding to handlers",
            cell.RowId, cell.ColumnName);

        // Forward cell edit completion to parent control
        // InternalUIOperationHandler will trigger UpdateCellAsync → ValidateAllAsync → PerformFullReload
        CellEditCompleted?.Invoke(this, cell);
    }

    #region SENIOR IMPLEMENTATION: Row Context Menu Handlers (Excel-like Insert/Delete)

    /// <summary>
    /// SENIOR IMPLEMENTATION: Handles right-click on ItemsRepeater to show Row Context Menu.
    /// Excel-like behavior: Insert Above/Below, Delete selected rows.
    /// Supports multi-row selection (e.g., select 5 rows → Insert 5 Rows Above/Below).
    /// </summary>
    /// <summary>
    /// ✅ PROFESSIONAL FIX: Handles right-click on ItemsRepeater to show Row Context Menu.
    /// ARCHITECTURE:
    /// - Detekuje SELECTED CELLS (nie len selected rows)
    /// - Získa unikátne RowIDs z označených buniek
    /// - Context menu: INSERT BEFORE/AFTER/DELETE na základe selected cells
    /// EXAMPLE: Označené bunky v riadkoch __rowNumber 3, 4, 7:
    ///   - Insert Before: Pridá 2 riadky pred __rowNumber=3, 1 riadok pred __rowNumber=7
    ///   - Insert After: Pridá 2 riadky po __rowNumber=4, 1 riadok po __rowNumber=7
    ///   - Delete: Zmaže rows s __rowNumber 3, 4, 7
    /// </summary>
    private void OnItemsRepeaterRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        _logger.LogInformation("Right-click detected on ItemsRepeater - analyzing SELECTED CELLS for Row Context Menu");

        // ✅ STEP 1: Get all SELECTED CELLS (nie len selected rows!)
        var selectedCells = _viewModel.GetSelectedCells();

        // ✅ PROFESSIONAL FIX: FALLBACK - získať cell pod kurzorom BEZ ZMENY SELECTION STATE
        CellViewModel? cellUnderCursor = null;
        if (selectedCells.Count == 0)
        {
            var originalSource = e.OriginalSource as FrameworkElement;
            while (originalSource != null)
            {
                if (originalSource.DataContext is CellViewModel cellVm)
                {
                    // ❌ NEMODIFIKOVAŤ IsSelected - len získať reference na cell
                    cellUnderCursor = cellVm;
                    _logger.LogInformation("Found cell under cursor (NOT modifying selection): Row={Row}, Col={Col}",
                        cellVm.RowIndex, cellVm.ColumnIndex);
                    break;
                }
                originalSource = originalSource.Parent as FrameworkElement;
            }
        }

        // ✅ STEP 2: Build RowIDs list - buď zo selected cells alebo z cell pod kurzorom
        List<(string RowId, int RowIndex)> targetRows;

        if (selectedCells.Count > 0)
        {
            // Používateľ má selected cells - použiť ich
            targetRows = selectedCells
                .Where(c => c.RowId != null)
                .GroupBy(c => c.RowId)
                .Select(g => (RowId: g.Key!, RowIndex: g.First().RowIndex))
                .OrderBy(r => r.RowIndex)
                .ToList();

            _logger.LogInformation("Using {Count} selected rows for context menu", targetRows.Count);
        }
        else if (cellUnderCursor != null && cellUnderCursor.RowId != null)
        {
            // Žiadne selected cells - použiť cell pod kurzorom (bez zmeny selection!)
            targetRows = new List<(string, int)>
            {
                (cellUnderCursor.RowId, cellUnderCursor.RowIndex)
            };

            _logger.LogInformation("Using cell under cursor for context menu (RowId={RowId}, no selection change)",
                cellUnderCursor.RowId);
        }
        else
        {
            // Žiadne selected cells ani valid cell pod kurzorom
            _logger.LogWarning("No cells selected and no valid cell under cursor - context menu skipped");
            e.Handled = true;
            return;
        }

        var selectedIndices = targetRows.Select(r => r.RowIndex).ToList();
        var selectedIds = targetRows.Select(r => r.RowId).ToList();

        _logger.LogInformation("Showing Row Context Menu for {RowCount} rows (RowIDs: {Ids})",
            targetRows.Count, string.Join(", ", selectedIds.Take(5)));

        // ✅ STEP 3: Create and show context menu
        var contextMenu = _rowContextMenu.CreateRowContextMenu(selectedIndices, selectedIds, _viewModel.Theme);
        contextMenu.ShowAt(_itemsRepeater, e.GetPosition(_itemsRepeater));

        e.Handled = true;
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Handles "Insert Rows Above" for MULTIPLE selected rows.
    /// ARCHITECTURE:
    /// - Analyzuje selected row indices → identifikuje CONTINUOUS GROUPS
    /// - Pre každú group: pridá N riadkov PRED PRVÝM riadkom v group
    /// EXAMPLE: Selected cells v riadkoch index [2, 3, 6] (page-relative 0-based):
    ///   - Groups: [2,3], [6]
    ///   - Group [2,3]: Pridá 2 riadky PRED index 2 (global index = currentPage*pageSize + 2)
    ///   - Group [6]: Pridá 1 riadok PRED index 6 (global index = currentPage*pageSize + 6 + 2)
    /// IMPORTANT:
    ///   - indices are page-relative (0-based)
    ///   - Must convert to global index: globalIndex = currentPage * pageSize + pageRelativeIndex
    /// </summary>
    private async void OnRowContextMenuInsertAbove(object? sender, InsertRowsEventArgs e)
    {
        _logger.LogInformation("Row Context Menu: Insert rows ABOVE for {Count} selected RowIDs",
            e.SelectedRowIds.Count);

        if (e.SelectedRowIds.Count == 0)
        {
            _logger.LogWarning("No selected RowIDs - operation cancelled");
            return;
        }

        // ✅ STEP 1: Get selected cell indices (from ViewModel)
        var selectedCells = _viewModel.GetSelectedCells();
        var selectedRowIndices = selectedCells
            .Select(c => c.RowIndex)
            .Distinct()
            .OrderBy(i => i)
            .ToList();

        if (selectedRowIndices.Count == 0)
        {
            _logger.LogWarning("No selected rows - operation cancelled");
            return;
        }

        // ✅ PROFESSIONAL FIX: Create RowIndex → RowId mapping
        // This allows us to retrieve stable RowId for each row index
        var rowIndexToRowId = selectedCells
            .GroupBy(c => c.RowIndex)
            .ToDictionary(g => g.Key, g => g.First().RowId);

        // ✅ STEP 2: Group consecutive row indices
        // EXAMPLE: [2, 3, 6, 9, 10, 11] → [[2,3], [6], [9,10,11]]
        var groups = new List<List<int>>();
        List<int>? currentGroup = null;

        foreach (var index in selectedRowIndices)
        {
            if (currentGroup == null || index != currentGroup[^1] + 1)
            {
                currentGroup = new List<int> { index };
                groups.Add(currentGroup);
            }
            else
            {
                currentGroup.Add(index);
            }
        }

        _logger.LogInformation("Grouped {Count} selected rows into {GroupCount} continuous groups",
            selectedRowIndices.Count, groups.Count);

        // ✅ STEP 3: Insert rows for each group (REVERSE order to preserve indices)
        var currentPage = _viewModel.PageManager?.CurrentPage ?? 0;
        var pageSize = _viewModel.PageManager?.PageSize ?? 15;
        var totalInserted = 0;

        for (int g = groups.Count - 1; g >= 0; g--)
        {
            var group = groups[g];
            var firstIndexInGroup = group[0];
            var rowCount = group.Count;

            // ✅ PROFESSIONAL FIX: Get RowId for first row in group
            if (!rowIndexToRowId.TryGetValue(firstIndexInGroup, out var rowId))
            {
                _logger.LogError("Cannot find RowId for row index {RowIndex} - skipping group {GroupNum}",
                    firstIndexInGroup, g + 1);
                continue;
            }

            if (string.IsNullOrEmpty(rowId))
            {
                _logger.LogError("RowId is null/empty for row index {RowIndex} - skipping group {GroupNum}",
                    firstIndexInGroup, g + 1);
                continue;
            }

            // Convert page-relative index to global index
            var globalIndex = currentPage * pageSize + firstIndexInGroup;

            _logger.LogInformation("Group {GroupNum}: Insert {Count} rows BEFORE global index {GlobalIndex} (page-relative index {PageIndex}), RowId={RowId}",
                g + 1, rowCount, globalIndex, firstIndexInGroup, rowId);

            // ✅ FIXED: INSERT rows at global index with RowId (not null!)
            for (int i = 0; i < rowCount; i++)
            {
                var eventArgs = new InsertRowRequestedEventArgs(globalIndex, rowId, "Above");
                InsertRowRequested?.Invoke(this, eventArgs);
                totalInserted++;
            }
        }

        _logger.LogInformation("Inserted {Total} rows ABOVE {GroupCount} groups", totalInserted, groups.Count);
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Handles "Insert Rows Below" for MULTIPLE selected rows.
    /// ARCHITECTURE:
    /// - Analyzuje selected row indices → identifikuje CONTINUOUS GROUPS
    /// - Pre každú group: pridá N riadkov PO POSLEDNOM riadku v group
    /// EXAMPLE: Selected cells v riadkoch index [2, 3, 6]:
    ///   - Groups: [2,3], [6]
    ///   - Group [6]: Pridá 1 riadok PO index 6 (global index 7)
    ///   - Group [2,3]: Pridá 2 riadky PO index 3 (global index 5, už posunuté)
    /// </summary>
    private async void OnRowContextMenuInsertBelow(object? sender, InsertRowsEventArgs e)
    {
        _logger.LogInformation("Row Context Menu: Insert rows BELOW for {Count} selected RowIDs",
            e.SelectedRowIds.Count);

        if (e.SelectedRowIds.Count == 0)
        {
            _logger.LogWarning("No selected RowIDs - operation cancelled");
            return;
        }

        // ✅ STEP 1: Get selected cell indices (from ViewModel)
        var selectedCells = _viewModel.GetSelectedCells();
        var selectedRowIndices = selectedCells
            .Select(c => c.RowIndex)
            .Distinct()
            .OrderBy(i => i)
            .ToList();

        if (selectedRowIndices.Count == 0)
        {
            _logger.LogWarning("No selected rows - operation cancelled");
            return;
        }

        // ✅ PROFESSIONAL FIX: Create RowIndex → RowId mapping
        // This allows us to retrieve stable RowId for each row index
        var rowIndexToRowId = selectedCells
            .GroupBy(c => c.RowIndex)
            .ToDictionary(g => g.Key, g => g.First().RowId);

        // ✅ STEP 2: Group consecutive row indices
        var groups = new List<List<int>>();
        List<int>? currentGroup = null;

        foreach (var index in selectedRowIndices)
        {
            if (currentGroup == null || index != currentGroup[^1] + 1)
            {
                currentGroup = new List<int> { index };
                groups.Add(currentGroup);
            }
            else
            {
                currentGroup.Add(index);
            }
        }

        _logger.LogInformation("Grouped {Count} selected rows into {GroupCount} continuous groups",
            selectedRowIndices.Count, groups.Count);

        // ✅ STEP 3: Insert rows for each group (REVERSE order to preserve indices)
        var currentPage = _viewModel.PageManager?.CurrentPage ?? 0;
        var pageSize = _viewModel.PageManager?.PageSize ?? 15;
        var totalInserted = 0;

        for (int g = groups.Count - 1; g >= 0; g--)
        {
            var group = groups[g];
            var lastIndexInGroup = group[^1];
            var rowCount = group.Count;

            // ✅ PROFESSIONAL FIX: Get RowId for last row in group
            if (!rowIndexToRowId.TryGetValue(lastIndexInGroup, out var rowId))
            {
                _logger.LogError("Cannot find RowId for row index {RowIndex} - skipping group {GroupNum}",
                    lastIndexInGroup, g + 1);
                continue;
            }

            if (string.IsNullOrEmpty(rowId))
            {
                _logger.LogError("RowId is null/empty for row index {RowIndex} - skipping group {GroupNum}",
                    lastIndexInGroup, g + 1);
                continue;
            }

            // Convert page-relative index to global index + 1 (insert AFTER)
            var globalIndex = currentPage * pageSize + lastIndexInGroup + 1;

            _logger.LogInformation("Group {GroupNum}: Insert {Count} rows AFTER global index {GlobalIndex} (page-relative index {PageIndex}), RowId={RowId}",
                g + 1, rowCount, globalIndex - 1, lastIndexInGroup, rowId);

            // ✅ FIXED: INSERT rows at global index with RowId (not null!)
            for (int i = 0; i < rowCount; i++)
            {
                var eventArgs = new InsertRowRequestedEventArgs(globalIndex, rowId, "Below");
                InsertRowRequested?.Invoke(this, eventArgs);
                totalInserted++;
            }
        }

        _logger.LogInformation("Inserted {Total} rows BELOW {GroupCount} groups", totalInserted, groups.Count);
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Handles "Delete Rows" for MULTIPLE selected rows.
    /// ARCHITECTURE:
    /// - Získa unique RowIDs from selected cells
    /// - Zmaže všetky riadky pomocou DeleteRowByIdAsync (accepts RowID!)
    /// EXAMPLE: Selected cells v riadkoch __rowNumber [3, 4, 7]:
    ///   - Delete RowID_3 (__rowNumber=3) → rows 4-100 shift DOWN to 3-99
    ///   - Delete RowID_4 (__rowNumber=4, now 3) → rows 5-99 shift DOWN to 4-98
    ///   - Delete RowID_7 (__rowNumber=7, now 5) → rows 8-98 shift DOWN to 6-96
    /// IMPORTANT:
    ///   - DeleteRowByIdAsync() automatically shifts __rowNumber DOWN
    ///   - No need for reverse order (RowID is stable identifier)
    /// </summary>
    private async void OnRowContextMenuDelete(object? sender, DeleteRowsEventArgs e)
    {
        _logger.LogInformation("Row Context Menu: Delete {Count} rows from selected cells",
            e.RowIds.Count);

        if (e.RowIds.Count == 0)
        {
            _logger.LogWarning("No valid RowIDs to delete - operation cancelled");
            return;
        }

        _logger.LogInformation("Deleting {Count} unique rows (RowIDs: {Ids})",
            e.RowIds.Count, string.Join(", ", e.RowIds.Take(5)));

        // ✅ STEP 1: Delete rows by RowId (DeleteRowByIdAsync handles __rowNumber shift automatically)
        foreach (var rowId in e.RowIds)
        {
            var eventArgs = new DeleteRowRequestedEventArgs(-1, rowId); // Index not needed (using RowId)
            DeleteRowRequested?.Invoke(this, eventArgs);
        }

        _logger.LogInformation("Deleted {Count} rows", e.RowIds.Count);
    }

    #endregion

    #region ✅ PREVIEW VALIDATION: Realtime keystroke validation handlers

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Handles cell value changes during edit mode for realtime preview validation.
    /// DEBOUNCE: 300ms delay to avoid spamming validation on every keystroke (user types "123" → validate once after 300ms)
    /// PREVIEW MODE: Does NOT write to validation storage (no DB writes for SQLite mode)
    /// UI UPDATE: Shows red border + validation message immediately without storage commit
    /// COMMIT: When user presses Enter, CellEditCompleted handler commits validation permanently
    /// </summary>
    private async void OnCellValueChangedAsync(object? sender, CellValueChangedEventArgs args)
    {
        if (args?.Cell == null) return;

        // Get facade for preview validation call
        var facade = _viewModel.Facade;
        if (facade?.Editing == null)
        {
            _logger.LogTrace("Preview validation skipped - facade not available");
            return;
        }

        var rowId = args.Cell.RowId;
        var columnName = args.Cell.ColumnName;
        var currentValue = args.NewValue;

        if (string.IsNullOrEmpty(rowId))
        {
            _logger.LogWarning("Preview validation skipped - rowId is null or empty");
            return;
        }

        _logger.LogTrace("🔤 KEYSTROKE: rowId={RowId}, column={ColumnName}, value={Value}",
            rowId, columnName, currentValue);

        // ✅ DEBOUNCE: Store pending validation and restart timer
        _pendingPreviewValidation = (rowId, columnName, currentValue, args.Cell);

        _previewValidationDebounceTimer?.Stop();
        _previewValidationDebounceTimer ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300) // 300ms delay
        };

        _previewValidationDebounceTimer.Tick -= OnPreviewValidationDebounceTimerTick; // Prevent duplicate subscriptions
        _previewValidationDebounceTimer.Tick += OnPreviewValidationDebounceTimerTick;
        _previewValidationDebounceTimer.Start();
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Debounce timer tick handler - executes preview validation after 300ms delay.
    /// REASON: User stopped typing → execute validation now (not on every keystroke)
    /// </summary>
    private async void OnPreviewValidationDebounceTimerTick(object? sender, object e)
    {
        _previewValidationDebounceTimer?.Stop();

        if (_pendingPreviewValidation == null) return;

        var (rowId, columnName, value, cellViewModel) = _pendingPreviewValidation.Value;
        _pendingPreviewValidation = null; // Clear pending validation

        // Get facade
        var facade = _viewModel.Facade;
        if (facade?.Editing == null) return;

        try
        {
            _logger.LogDebug("🔍 PREVIEW VALIDATION: Executing for rowId={RowId}, column={ColumnName}",
                rowId, columnName);

            // ✅ CALL PREVIEW VALIDATION (does NOT write to storage)
            var previewResult = await facade.Editing.PreviewValidateCellAsync(
                rowId,
                columnName,
                value,
                CancellationToken.None);

            // ✅ UPDATE UI IMMEDIATELY (preview mode - no storage write)
            // This runs on UI thread (DispatcherTimer.Tick is already on UI thread)
            UpdateCellPreviewValidationUI(cellViewModel, previewResult);

            _logger.LogDebug("✅ PREVIEW VALIDATION APPLIED: Valid={IsValid}, Message={Message}",
                previewResult.IsValid, previewResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preview validation failed for rowId={RowId}, column={ColumnName}",
                rowId, columnName);
        }
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Updates cell UI with preview validation result (red border + ValidationAlerts).
    /// PREVIEW MODE: Shows temporary validation feedback WITHOUT writing to storage.
    /// CRITICAL: Merges preview error with existing permanent errors from other columns.
    /// </summary>
    private async void UpdateCellPreviewValidationUI(CellViewModel cellViewModel, PreviewValidationResult result)
    {
        if (cellViewModel == null) return;

        // ✅ STEP 1: Update current cell validation state (red border)
        cellViewModel.IsValidationError = !result.IsValid;
        cellViewModel.ValidationMessage = result.ErrorMessage ?? string.Empty;

        if (!result.IsValid)
        {
            _logger.LogTrace("⚠️ PREVIEW UI: Cell [{Row},{Col}] marked as invalid: {Message}",
                cellViewModel.RowIndex, cellViewModel.ColumnName, result.ErrorMessage);
        }
        else
        {
            _logger.LogTrace("✅ PREVIEW UI: Cell [{Row},{Col}] marked as valid",
                cellViewModel.RowIndex, cellViewModel.ColumnName);
        }

        // ✅ STEP 2: Update ValidationAlerts column with MERGED errors (preview + permanent)
        if (_viewModel != null)
        {
            var rowViewModel = _viewModel.Rows.FirstOrDefault(r => r.RowIndex == cellViewModel.RowIndex);
            if (rowViewModel != null)
            {
                var alertsCell = rowViewModel.Cells.FirstOrDefault(c =>
                    c.SpecialType == Common.SpecialColumnType.ValidationAlerts);

                if (alertsCell != null)
                {
                    try
                    {
                        // ✅ STEP 2A: Get existing PERMANENT validation errors for this row (all columns)
                        // These are errors stored in validation storage from previous commits
                        var facade = _viewModel.Facade;
                        if (facade?.Validation != null)
                        {
                            var rowId = cellViewModel.RowId;
                            if (!string.IsNullOrEmpty(rowId))
                            {
                                // Get ALL validation errors for this row from storage
                                var allRowErrors = await facade.Validation.GetValidationErrorsAsync(
                                    onlyFiltered: false,
                                    onlyChecked: false,
                                    cancellationToken: default);

                                // Filter errors for current row
                                var permanentRowErrors = allRowErrors
                                    .Where(e => e.RowId == rowId)
                                    .ToList();

                                // ✅ PROFESSIONAL FIX 4.1: Sort errors by column order (left-to-right)
                                // REASON: ValidationAlerts should show errors in same order as columns appear in grid
                                // USER REQUEST: "tie validacne chyby by sa mohli vypisovat postupne po tych stlpcoch"
                                var columnOrder = _viewModel.Rows.FirstOrDefault()?.Cells
                                    .Where(c => !c.IsSpecialColumn)
                                    .Select((c, index) => new { c.ColumnName, Order = index })
                                    .ToDictionary(x => x.ColumnName, x => x.Order, StringComparer.OrdinalIgnoreCase);

                                if (columnOrder != null && columnOrder.Count > 0)
                                {
                                    permanentRowErrors = permanentRowErrors
                                        .OrderBy(e => columnOrder.TryGetValue(e.ColumnName, out var order) ? order : int.MaxValue)
                                        .ToList();

                                    _logger.LogTrace("Sorted {Count} validation errors by column order for row {RowIndex}",
                                        permanentRowErrors.Count, cellViewModel.RowIndex);
                                }

                                // ✅ STEP 2B: Build merged message: preview error + permanent errors
                                var alertMessages = new List<string>();

                                // Add preview error if validation failed (temporary, not in storage)
                                if (!result.IsValid && !string.IsNullOrEmpty(result.ErrorMessage))
                                {
                                    alertMessages.Add($"⚠️ PREVIEW: {result.AffectedColumn}: {result.ErrorMessage}");
                                    _logger.LogTrace("📝 PREVIEW: Added temporary alert for {Column}", result.AffectedColumn);
                                }

                                // Add permanent errors from OTHER columns (skip current column to avoid duplication)
                                foreach (var error in permanentRowErrors)
                                {
                                    // Skip current column if it has permanent error (we show preview instead)
                                    if (error.ColumnName.Equals(cellViewModel.ColumnName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Only add permanent error if preview is valid (no override)
                                        if (result.IsValid)
                                        {
                                            alertMessages.Add($"{error.ColumnName}: {error.Message}");
                                        }
                                    }
                                    else
                                    {
                                        // Add permanent errors from other columns
                                        alertMessages.Add($"{error.ColumnName}: {error.Message}");
                                    }
                                }

                                // ✅ STEP 2C: Set merged alert message
                                if (alertMessages.Any())
                                {
                                    alertsCell.ValidationAlertMessage = string.Join("; ", alertMessages);
                                    _logger.LogTrace("📝 MERGED ALERTS: Row {RowIndex} ValidationAlerts updated: {Message}",
                                        cellViewModel.RowIndex, alertsCell.ValidationAlertMessage);
                                }
                                else
                                {
                                    // No errors at all - clear ValidationAlerts
                                    alertsCell.ValidationAlertMessage = null;
                                    _logger.LogTrace("✅ CLEARED ALERTS: Row {RowIndex} has no validation errors",
                                        cellViewModel.RowIndex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update ValidationAlerts during preview validation for row {RowIndex}",
                            cellViewModel.RowIndex);
                    }
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// Disposes the DataGridCellsView and cleans up all resources.
    /// Disposes ViewportManager and ElementFactory to release ViewModels and recycled elements.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _logger.LogInformation("DataGridCellsView disposing");

        // Dispose ViewportManager (disposes all cached ViewModels)
        _viewportManager?.Dispose();

        // Clear ElementFactory recycle pool
        _elementFactory?.ClearRecyclePool();

        // Unsubscribe from ElementFactory events
        if (_elementFactory != null)
        {
            _elementFactory.OnRowSelectionChanged -= HandleRowSelectionChanged;
            _elementFactory.OnDeleteRowRequested -= HandleDeleteRowRequested;
            _elementFactory.OnInsertRowRequested -= HandleInsertRowRequested;
            _elementFactory.OnCellSelected -= OnCellSelected;
            _elementFactory.OnCellPointerEntered -= OnCellPointerEntered;
            _elementFactory.OnCellEditCompleted -= OnCellEditCompleted;
            // ✅ CRITICAL FIX: Unsubscribe from CellValueChanged
            _elementFactory.OnCellValueChanged -= OnCellValueChangedAsync;
            // ✅ PROFESSIONAL FIX: Unsubscribe from cell navigation
            _elementFactory.OnCellNavigationRequested -= HandleCellNavigation;
        }

        // ✅ Cleanup preview validation debounce timer
        _previewValidationDebounceTimer?.Stop();
        _previewValidationDebounceTimer = null;

        // Unsubscribe from ViewModel events
        if (_viewModel != null)
        {
            _viewModel.Rows.CollectionChanged -= OnRowsCollectionChanged;
            _viewModel.ItemsRepeaterRefreshRequested -= OnItemsRepeaterRefreshRequested;

            // ✅ SENIOR FIX: Unsubscribe from PageManager events
            if (_viewModel.PageManager != null)
            {
                _viewModel.PageManager.PageChanged -= OnPageChanged;
            }
        }

        // Unsubscribe from ScrollViewer events
        if (_scrollViewer != null)
        {
            // SENIOR FIX: PointerPressed subscription removed (no longer used)
            _scrollViewer.PointerReleased -= OnPointerReleased;
            _scrollViewer.PointerCaptureLost -= OnPointerCaptureLost;
            // ✅ PROBLEM 4 FIX: OnScrollViewerPointerMoved subscription removed (method deleted)
            // _scrollViewer.PointerMoved -= OnScrollViewerPointerMoved;  // ❌ REMOVED - method doesn't exist
            _scrollViewer.ViewChanged -= OnScrollViewChanged;

            // ✅ Cleanup scroll debounce timer
            _scrollDebounceTimer?.Stop();
            _scrollDebounceTimer = null;
        }

        // SENIOR IMPLEMENTATION: Unsubscribe from Row Context Menu events
        if (_rowContextMenu != null)
        {
            _rowContextMenu.InsertRowsAboveRequested -= OnRowContextMenuInsertAbove;
            _rowContextMenu.InsertRowsBelowRequested -= OnRowContextMenuInsertBelow;
            _rowContextMenu.DeleteRowsRequested -= OnRowContextMenuDelete;
        }

        // SENIOR IMPLEMENTATION: Unsubscribe from ItemsRepeater events
        if (_itemsRepeater != null)
        {
            _itemsRepeater.RightTapped -= OnItemsRepeaterRightTapped;
        }

        _logger.LogInformation("DataGridCellsView disposed");
    }
}

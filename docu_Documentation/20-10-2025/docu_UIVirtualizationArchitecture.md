# UI Virtualization Architecture - ItemsRepeater + ViewportManager

**Priorita:** 🔴 P2 (Kritická)
**Čas:** 18-24 hodín
**Dependencies:** Žiadne

---

## 📋 Prehľad

Implementácia UI virtualizácie pre Advanced DataGrid s podporou 1000+ viditeľných riadkov:
- **ItemsRepeater** miesto StackPanel (element recycling)
- **ElementFactory** (create/recycle pattern)
- **ViewportManager** s POCO cache
- **Dispose Pattern** pre ViewModels

### KĽÚČOVÁ POZNÁMKA

**POCO Cache JE POTREBNÝ aj s SQLite!**
- SQLite = persistence (disk, ALL rows)
- POCO Cache = viewport performance (RAM, ~1000-2000 rows)
- **NIE duplikácia** - rôzne účely, spolupracujú!

---

## 🎯 Ciele

1. **Pamäťová úspora:** 70-80% reduction
   - PRED: 100 rows = 460 MB
   - PO: 100 rows = 35-55 MB

2. **Viewport limit:** Maximálne 1000 ViewModels v pamäti

3. **Element recycling:** Reuse UI elementov (max 50 v pool)

4. **Smooth scrolling:** Buffer zone (±500 rows) pre plynulý scroll

---

## 🔧 Implementačné Úlohy

### FÁZA A: ViewportManager (6-8 hodín)

#### Účel

ViewportManager riadi:
- Lightweight POCO data store pre viewport + buffer (~150-200 bytes/row)
- ViewModels LEN pre viewport (max 1000 rows)
- Dispose() ViewModels mimo viewport
- Load data z HybridRowStore (streaming)

#### Implementácia

**Súbor:** ViewportManager.cs (NOVÝ)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Viewport;

/// <summary>
/// Manages viewport with POCO cache and ViewModel lifecycle.
/// POCO cache (~150-200 bytes/row) is REQUIRED even with SQLite!
/// - SQLite = persistence (disk, ALL rows)
/// - POCO = viewport performance (RAM, ~1000-2000 rows)
/// </summary>
internal sealed class ViewportManager : IDisposable
{
    private readonly IRowStore _rowStore;
    private readonly ThemeManager _themeManager;
    private readonly AdvancedDataGridOptions _options;
    private readonly ILogger<ViewportManager> _logger;

    // POCO cache (viewport + buffer)
    private List<RowData> _cachedRows = new();
    private int _cachedStartIndex = -1;

    // ViewModels (ONLY for visible rows, max 1000)
    private readonly Dictionary<int, DataGridRowViewModel> _viewportCache = new();

    private const int MAX_VIEWPORT_SIZE = 1000;
    private const int BUFFER_SIZE = 500; // Pre smooth scroll

    private int _totalRowCount = 0;
    private bool _disposed = false;

    public ViewportManager(
        IRowStore rowStore,
        ThemeManager themeManager,
        AdvancedDataGridOptions options,
        ILogger<ViewportManager> logger)
    {
        _rowStore = rowStore;
        _themeManager = themeManager;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Updates viewport based on visible row range.
    /// Disposes ViewModels outside viewport, loads POCO data, creates ViewModels.
    /// </summary>
    public async Task UpdateViewportAsync(
        int firstVisibleIndex,
        int lastVisibleIndex,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // STEP 1: Dispose ViewModels mimo viewport
            var toRemove = _viewportCache.Keys
                .Where(idx => idx < firstVisibleIndex || idx > lastVisibleIndex)
                .ToList();

            foreach (var idx in toRemove)
            {
                _viewportCache[idx].Dispose();
                _viewportCache.Remove(idx);
            }

            if (toRemove.Any())
                _logger.LogDebug("Disposed {Count} ViewModels outside viewport", toRemove.Count);

            // STEP 2: Ensure POCO data loaded pre viewport + buffer
            var loadStart = Math.Max(0, firstVisibleIndex - BUFFER_SIZE);
            var loadEnd = Math.Min(_totalRowCount - 1, lastVisibleIndex + BUFFER_SIZE);

            // Check if current POCO cache covers needed range
            var needsReload = _cachedStartIndex == -1
                || loadStart < _cachedStartIndex
                || loadEnd > (_cachedStartIndex + _cachedRows.Count - 1);

            if (needsReload)
            {
                await LoadPOCORangeAsync(loadStart, loadEnd, cancellationToken);
            }

            // STEP 3: Create ViewModels pre viditeľné riadky
            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                if (_viewportCache.ContainsKey(i))
                    continue; // Already cached

                // Get POCO data
                var pocoIndex = i - _cachedStartIndex;
                if (pocoIndex < 0 || pocoIndex >= _cachedRows.Count)
                {
                    _logger.LogWarning("Row index {Index} out of POCO cache range", i);
                    continue;
                }

                var rowData = _cachedRows[pocoIndex];
                var viewModel = CreateViewModelForRow(rowData, i);
                _viewportCache[i] = viewModel;
            }

            // STEP 4: Limit viewport size (max 1000)
            if (_viewportCache.Count > MAX_VIEWPORT_SIZE)
            {
                // Remove excess (keep closest to viewport)
                var excess = _viewportCache.Keys
                    .OrderByDescending(idx => Math.Abs(idx - firstVisibleIndex))
                    .Skip(MAX_VIEWPORT_SIZE)
                    .ToList();

                foreach (var idx in excess)
                {
                    _viewportCache[idx].Dispose();
                    _viewportCache.Remove(idx);
                }

                _logger.LogWarning("Viewport exceeded {Max} rows, disposed {Count} excess",
                    MAX_VIEWPORT_SIZE, excess.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateViewport failed: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Loads POCO data for specified range from HybridRowStore.
    /// </summary>
    private async Task LoadPOCORangeAsync(
        int startIndex,
        int endIndex,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading POCO range [{Start}..{End}]", startIndex, endIndex);

        // Clear current cache
        _cachedRows.Clear();
        _cachedStartIndex = startIndex;

        int currentIndex = 0;
        int targetCount = endIndex - startIndex + 1;

        // Stream rows and load target range
        await foreach (var batch in _rowStore.StreamRowsAsync(false, false, 1000, cancellationToken))
        {
            foreach (var row in batch)
            {
                // Skip rows before startIndex
                if (currentIndex < startIndex)
                {
                    currentIndex++;
                    continue;
                }

                // Stop after endIndex
                if (currentIndex > endIndex)
                    break;

                // Add to POCO cache
                _cachedRows.Add(new RowData
                {
                    RowId = row.TryGetValue("__rowId", out var id) ? id?.ToString() ?? "" : "",
                    Values = new Dictionary<string, object?>(row),
                    LastModified = row.TryGetValue("__lastModified", out var lm) && lm is DateTime dt
                        ? dt
                        : DateTime.MinValue,
                    CreationType = row.TryGetValue("__creationType", out var ct)
                        ? ct?.ToString() ?? "AutoGenerated"
                        : "AutoGenerated"
                });

                currentIndex++;

                // Check if we have enough
                if (_cachedRows.Count >= targetCount)
                    break;
            }

            if (_cachedRows.Count >= targetCount)
                break;
        }

        _logger.LogInformation("Loaded {Count} POCO rows (range [{Start}..{End}])",
            _cachedRows.Count, startIndex, endIndex);
    }

    /// <summary>
    /// Creates DataGridRowViewModel from POCO RowData.
    /// </summary>
    private DataGridRowViewModel CreateViewModelForRow(RowData rowData, int rowIndex)
    {
        // Create row ViewModel
        var rowViewModel = new DataGridRowViewModel(
            rowData.RowId,
            rowIndex,
            _themeManager,
            _options);

        // Populate cells from POCO data
        foreach (var kvp in rowData.Values)
        {
            // Skip metadata
            if (kvp.Key.StartsWith("__"))
                continue;

            // Create cell ViewModel
            var cellViewModel = new CellViewModel(
                rowData.RowId,
                kvp.Key,
                kvp.Value,
                rowIndex,
                _themeManager);

            rowViewModel.Cells.Add(cellViewModel);
        }

        return rowViewModel;
    }

    /// <summary>
    /// Gets ViewModel for specified row index (from cache).
    /// </summary>
    public DataGridRowViewModel? GetRowViewModel(int index)
    {
        return _viewportCache.TryGetValue(index, out var vm) ? vm : null;
    }

    /// <summary>
    /// Total row count (for ItemsRepeater).
    /// </summary>
    public int TotalRowCount
    {
        get => _totalRowCount;
        set => _totalRowCount = value;
    }

    /// <summary>
    /// Invalidates all caches (force reload on next UpdateViewport).
    /// </summary>
    public void InvalidateCache()
    {
        // Dispose all ViewModels
        foreach (var vm in _viewportCache.Values)
            vm.Dispose();

        _viewportCache.Clear();

        // Clear POCO cache
        _cachedRows.Clear();
        _cachedStartIndex = -1;

        _logger.LogInformation("Viewport cache invalidated");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Dispose all ViewModels
        foreach (var vm in _viewportCache.Values)
            vm.Dispose();

        _viewportCache.Clear();
        _cachedRows.Clear();

        _logger.LogInformation("ViewportManager disposed");
    }
}

/// <summary>
/// Lightweight POCO data for viewport cache (~150-200 bytes/row).
/// NOTE: Dynamické stĺpce ZACHOVANÉ! (Dictionary, nie struct)
/// </summary>
internal sealed record RowData
{
    public string RowId { get; init; } = "";
    public Dictionary<string, object?> Values { get; init; } = new();
    public DateTime LastModified { get; init; }
    public string CreationType { get; init; } = "AutoGenerated";
}
```

---

### FÁZA B: ElementFactory (4-6 hodín)

#### Účel

ElementFactory pre ItemsRepeater s element recycling:
- Create/Recycle pattern
- Max 50 elementov v recycle pool
- Automatic cleanup (unbind events)

#### Implementácia

**Súbor:** DataGridElementFactory.cs (NOVÝ)

```csharp
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Viewport;

/// <summary>
/// ElementFactory for ItemsRepeater with element recycling.
/// Reuses Grid elements up to MAX_RECYCLE_POOL (50).
/// </summary>
internal sealed class DataGridElementFactory : ElementFactory
{
    private readonly ViewportManager _viewportManager;
    private readonly DataGridViewModel _viewModel;
    private readonly ILogger<DataGridElementFactory> _logger;

    private readonly Queue<Grid> _recycledElements = new();
    private const int MAX_RECYCLE_POOL = 50;

    public DataGridElementFactory(
        ViewportManager viewportManager,
        DataGridViewModel viewModel,
        ILogger<DataGridElementFactory> logger)
    {
        _viewportManager = viewportManager;
        _viewModel = viewModel;
        _logger = logger;
    }

    protected override UIElement GetElement(ElementFactoryGetArgs args)
    {
        var index = args.Index;

        // Get ViewModel from ViewportManager
        var rowViewModel = _viewportManager.GetRowViewModel(index);
        if (rowViewModel == null)
        {
            // Return empty placeholder if ViewModel not loaded yet
            return CreateEmptyPlaceholder();
        }

        // Get or create row grid
        var grid = GetOrCreateRowGrid();

        // Bind grid to ViewModel
        BindRowGrid(grid, rowViewModel);

        return grid;
    }

    protected override void RecycleElement(ElementFactoryRecycleArgs args)
    {
        if (args.Element is not Grid grid)
            return;

        // Unbind and cleanup events
        UnbindRowGrid(grid);

        // Enqueue for reuse OR dispose if pool full
        if (_recycledElements.Count < MAX_RECYCLE_POOL)
        {
            _recycledElements.Enqueue(grid);
        }
        else
        {
            // Pool full, dispose
            DisposeGrid(grid);
        }
    }

    private Grid GetOrCreateRowGrid()
    {
        // Try to reuse from pool
        if (_recycledElements.Count > 0)
        {
            var recycled = _recycledElements.Dequeue();
            _logger.LogTrace("Recycled grid from pool (remaining: {Count})", _recycledElements.Count);
            return recycled;
        }

        // Create new
        return CreateNewRowGrid();
    }

    private Grid CreateNewRowGrid()
    {
        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Height = 30 // Fixed row height for virtualization
        };

        // Add ColumnDefinitions from DataGridViewModel
        foreach (var columnViewModel in _viewModel.Columns)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(columnViewModel.Width)
            });
        }

        _logger.LogTrace("Created new row grid");
        return grid;
    }

    private void BindRowGrid(Grid grid, DataGridRowViewModel rowViewModel)
    {
        grid.DataContext = rowViewModel;
        grid.Children.Clear();

        // Create RowControlData for cleanup
        var rowData = new RowControlData(rowViewModel);
        grid.Tag = rowData;

        // Create cell controls
        int columnIndex = 0;
        foreach (var cellViewModel in rowViewModel.Cells)
        {
            UIElement control;

            if (cellViewModel.IsSpecialColumn)
            {
                // Special column control
                control = CreateSpecialColumnControl(cellViewModel, rowData);
            }
            else
            {
                // Normal cell control
                control = CreateNormalCellControl(cellViewModel, rowData);
            }

            Grid.SetColumn(control, columnIndex);
            grid.Children.Add(control);

            columnIndex++;
        }
    }

    private void UnbindRowGrid(Grid grid)
    {
        // Cleanup actions (unsubscribe events)
        if (grid.Tag is RowControlData rowData)
        {
            rowData.Cleanup();
        }

        grid.DataContext = null;
        grid.Tag = null;
    }

    private void DisposeGrid(Grid grid)
    {
        UnbindRowGrid(grid);
        grid.Children.Clear();
        grid.ColumnDefinitions.Clear();
    }

    private UIElement CreateEmptyPlaceholder()
    {
        return new TextBlock
        {
            Text = "Loading...",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private UIElement CreateSpecialColumnControl(CellViewModel cellViewModel, RowControlData rowData)
    {
        // Use existing SpecialColumnCellControl
        var specialControl = new UIControls.SpecialColumnCellControl(cellViewModel);

        // Subscribe events and store cleanup actions
        // (Similar to existing DataGridCellsView logic)

        return specialControl;
    }

    private UIElement CreateNormalCellControl(CellViewModel cellViewModel, RowControlData rowData)
    {
        // Use existing NormalCellControl or TextBox
        var textBox = new TextBox
        {
            Text = cellViewModel.DisplayValue,
            BorderBrush = cellViewModel.BorderBrush,
            Background = cellViewModel.BackgroundBrush,
            // ... other bindings ...
        };

        // Subscribe events and store cleanup actions
        // (Similar to existing DataGridCellsView logic)

        return textBox;
    }
}

/// <summary>
/// Stores cleanup actions for row controls (event unsubscribe).
/// </summary>
internal sealed class RowControlData
{
    public DataGridRowViewModel RowViewModel { get; }
    public List<Action> CleanupActions { get; } = new();

    public RowControlData(DataGridRowViewModel rowViewModel)
    {
        RowViewModel = rowViewModel;
    }

    public void Cleanup()
    {
        foreach (var action in CleanupActions)
        {
            try
            {
                action();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        CleanupActions.Clear();
    }
}
```

---

### FÁZA C: DataGridCellsView Rewrite (8-10 hodín)

#### Zmeny

**Súbor:** DataGridCellsView.cs

**ODSTRÁNIŤ:**
- `StackPanel _rowsPanel` (line 93)
- `_rowsPanel.Children.Add(...)` calls

**PRIDAŤ:**
- `ItemsRepeater _itemsRepeater`
- `DataGridElementFactory _elementFactory`
- `ViewportManager _viewportManager`
- `ScrollViewer.ViewChanged` event handler

#### Implementácia

```csharp
// FIELDS (replace _rowsPanel):
private ItemsRepeater? _itemsRepeater;
private DataGridElementFactory? _elementFactory;
private ViewportManager? _viewportManager;
private ScrollViewer? _scrollViewer;

// CONSTRUCTOR update:
public DataGridCellsView(DataGridViewModel viewModel, ViewportManager viewportManager)
{
    _viewModel = viewModel;
    _viewportManager = viewportManager;

    // Create ElementFactory
    _elementFactory = new DataGridElementFactory(
        viewportManager,
        viewModel,
        _logger);

    // Create ItemsRepeater
    _itemsRepeater = new ItemsRepeater
    {
        Layout = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 0
        },
        ItemTemplate = _elementFactory,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Top
    };

    // Wrap in ScrollViewer
    _scrollViewer = new ScrollViewer
    {
        Content = _itemsRepeater,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
    };

    // Subscribe to scroll events
    _scrollViewer.ViewChanged += OnScrollViewChanged;

    // Subscribe to collection changes
    _viewModel.Rows.CollectionChanged += OnRowsCollectionChanged;

    // Set as content
    Content = _scrollViewer;
}

private async void OnScrollViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
{
    if (_scrollViewer == null || _viewportManager == null)
        return;

    const double ROW_HEIGHT = 30.0; // Fixed row height

    var verticalOffset = _scrollViewer.VerticalOffset;
    var viewportHeight = _scrollViewer.ViewportHeight;

    // Calculate visible range
    var firstVisibleIndex = (int)(verticalOffset / ROW_HEIGHT);
    var lastVisibleIndex = (int)((verticalOffset + viewportHeight) / ROW_HEIGHT) + 1;

    // Update viewport (load data + create ViewModels)
    await _viewportManager.UpdateViewportAsync(firstVisibleIndex, lastVisibleIndex);

    // Invalidate ItemsRepeater to refresh
    _itemsRepeater?.InvalidateMeasure();
}

private void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
{
    if (e.Action == NotifyCollectionChangedAction.Reset)
    {
        // Reload viewport
        _viewportManager?.InvalidateCache();
        _itemsRepeater?.InvalidateMeasure();
    }
}

protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        // Unsubscribe
        if (_scrollViewer != null)
            _scrollViewer.ViewChanged -= OnScrollViewChanged;

        if (_viewModel != null)
            _viewModel.Rows.CollectionChanged -= OnRowsCollectionChanged;

        // Dispose viewport
        _viewportManager?.Dispose();
    }

    base.Dispose(disposing);
}
```

---

### FÁZA D: DataGridRowViewModel Dispose (2-3 hodiny)

**Súbor:** DataGridRowViewModel.cs

```csharp
public sealed class DataGridRowViewModel : IDisposable
{
    private bool _disposed = false;

    // ... existing code ...

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Dispose cells
        foreach (var cell in Cells.OfType<IDisposable>())
        {
            cell.Dispose();
        }

        Cells.Clear();

        _logger?.LogTrace("DataGridRowViewModel disposed: {RowId}", RowId);
    }
}
```

---

### FÁZA E: Integration & Testing (2-3 hodiny)

#### Integration

**Súbor:** AdvancedDataGridControl.cs (alebo DataGridViewModel.cs)

```csharp
// Create ViewportManager
_viewportManager = new ViewportManager(
    _rowStore,
    _themeManager,
    _options,
    _logger);

_viewportManager.TotalRowCount = await _rowStore.GetRowCountAsync();

// Pass to DataGridCellsView
_cellsView = new DataGridCellsView(_viewModel, _viewportManager);

// Initial load
await _viewportManager.UpdateViewportAsync(0, 30); // First 30 rows
```

#### Testing

**Test Case 1: Scroll Performance**
```csharp
// 1. Load 10,000 rows
// 2. Scroll to row 5000
// 3. Assert: Memory < 200 MB
// 4. Assert: Scroll is smooth (no lag)
```

**Test Case 2: Viewport Limit**
```csharp
// 1. Scroll rapidly through 10,000 rows
// 2. Assert: _viewportCache.Count <= 1000 at all times
```

**Test Case 3: Element Recycling**
```csharp
// 1. Scroll down 100 rows
// 2. Scroll back up
// 3. Assert: Elements were recycled (check pool count)
```

---

## ✅ Výsledky

Po implementácii:

1. **ItemsRepeater:**
   - ✅ Replace StackPanel
   - ✅ Element recycling (max 50 v pool)
   - ✅ Fixed row height (30px)

2. **ViewportManager:**
   - ✅ POCO cache (viewport + buffer)
   - ✅ ViewModels LEN pre viewport (max 1000)
   - ✅ Dispose mimo viewport

3. **Pamäť:**
   - ✅ 100 rows: 460 MB → 35-55 MB (88-92% úspora)
   - ✅ 1000 rows: ~4.6 GB → 120-160 MB (96-97% úspora)
   - ✅ 10,000 rows: ~46 GB → 500-700 MB (98.5% úspora)

---

## 📝 Dependencies

- **HybridRowStore:** StreamRowsAsync metóda
- **ThemeManager:** Pre ViewModels
- **DataGridViewModel:** Column definitions

---

**Posledná aktualizácia:** 2025-10-20
**Status:** Ready for Implementation

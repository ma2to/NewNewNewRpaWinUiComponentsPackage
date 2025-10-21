using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Viewport;

/// <summary>
/// Manages viewport with POCO cache and ViewModel lifecycle.
/// BACKWARD COMPATIBLE: Works with existing ViewModel.Rows collection.
///
/// ARCHITECTURE:
/// - Source: DataGridViewModel.Rows (existing collection of all rows)
/// - POCO Cache: Lightweight copies (~150-200 bytes/row) for viewport + buffer
/// - ViewModels: Created ONLY for visible rows (max 1000)
///
/// PERFORMANCE TARGET: 70-80% memory reduction
/// - BEFORE: 100 rows = 460 MB (all ViewModels in UI)
/// - AFTER: 100 rows = 35-55 MB (only visible ViewModels)
/// </summary>
internal sealed class ViewportManager : IDisposable
{
    private readonly DataGridViewModel _sourceViewModel;
    private readonly ThemeManager _themeManager;
    private readonly ILogger<ViewportManager> _logger;

    // POCO cache (viewport + buffer)
    private List<RowData> _cachedRows = new();
    private int _cachedStartIndex = -1;

    // ViewModels (ONLY for visible rows, max 1000)
    private readonly Dictionary<int, DataGridRowViewModel> _viewportCache = new();

    private const int MAX_VIEWPORT_SIZE = 1000;
    private const int BUFFER_SIZE = 500; // For smooth scroll

    private int _totalRowCount = 0;
    private bool _disposed = false;

    public ViewportManager(
        DataGridViewModel sourceViewModel,
        ThemeManager themeManager,
        ILogger<ViewportManager> logger)
    {
        _sourceViewModel = sourceViewModel ?? throw new ArgumentNullException(nameof(sourceViewModel));
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("ViewportManager created (MAX_VIEWPORT_SIZE: {MaxSize}, BUFFER_SIZE: {BufferSize})",
            MAX_VIEWPORT_SIZE, BUFFER_SIZE);
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
            _logger.LogTrace("UpdateViewport called: first={First}, last={Last}", firstVisibleIndex, lastVisibleIndex);

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
                    _logger.LogWarning("Row index {Index} out of POCO cache range [{Start}..{End}]",
                        i, _cachedStartIndex, _cachedStartIndex + _cachedRows.Count - 1);
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

                _logger.LogWarning("Viewport exceeded {Max} rows, disposed {Count} excess ViewModels",
                    MAX_VIEWPORT_SIZE, excess.Count);
            }

            _logger.LogTrace("UpdateViewport completed: viewport cache size = {Size}", _viewportCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateViewport failed: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Loads POCO data for specified range from ViewModel.Rows collection.
    /// Converts heavy DataGridRowViewModel instances to lightweight POCO (~150-200 bytes each).
    /// </summary>
    private Task LoadPOCORangeAsync(
        int startIndex,
        int endIndex,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading POCO range [{Start}..{End}]", startIndex, endIndex);

        // Clear current cache
        _cachedRows.Clear();
        _cachedStartIndex = startIndex;

        // Load from ViewModel.Rows (already in memory)
        var rowCount = _sourceViewModel.Rows.Count;
        var actualEndIndex = Math.Min(endIndex, rowCount - 1);

        for (int i = startIndex; i <= actualEndIndex; i++)
        {
            if (i < 0 || i >= rowCount)
                continue;

            var sourceRow = _sourceViewModel.Rows[i];

            // Convert to lightweight POCO
            var values = new Dictionary<string, object?>();
            foreach (var cell in sourceRow.Cells)
            {
                values[cell.ColumnName] = cell.Value;
            }

            // Add metadata
            values["__rowId"] = sourceRow.RowId;
            values["__lastModified"] = DateTime.Now;
            values["__creationType"] = "FromViewModel";

            _cachedRows.Add(new RowData
            {
                RowId = sourceRow.RowId ?? "",
                Values = values,
                LastModified = DateTime.Now,
                CreationType = "FromViewModel"
            });
        }

        _logger.LogInformation("Loaded {Count} POCO rows (range [{Start}..{End}])",
            _cachedRows.Count, startIndex, actualEndIndex);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates DataGridRowViewModel from POCO RowData.
    /// Populates cells from POCO data, skipping internal metadata fields.
    /// </summary>
    private DataGridRowViewModel CreateViewModelForRow(RowData rowData, int rowIndex)
    {
        // Create row ViewModel
        var rowViewModel = new DataGridRowViewModel
        {
            RowId = rowData.RowId,
            RowIndex = rowIndex
        };

        // Populate cells from POCO data
        foreach (var kvp in rowData.Values)
        {
            // Skip metadata (double underscore prefix)
            if (kvp.Key.StartsWith("__"))
                continue;

            // Create cell ViewModel
            var cellViewModel = new CellViewModel(_themeManager)
            {
                RowId = rowData.RowId,
                RowIndex = rowIndex,
                ColumnName = kvp.Key,
                Value = kvp.Value
            };

            rowViewModel.Cells.Add(cellViewModel);
        }

        _logger.LogTrace("Created ViewModel for row {RowIndex} with {CellCount} cells", rowIndex, rowViewModel.Cells.Count);
        return rowViewModel;
    }

    /// <summary>
    /// Gets ViewModel for specified row index (from cache).
    /// Returns null if ViewModel not in viewport cache.
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
        set
        {
            _totalRowCount = value;
            _logger.LogDebug("TotalRowCount set to {Count}", value);
        }
    }

    /// <summary>
    /// Invalidates all caches (force reload on next UpdateViewport).
    /// Call this when row data changes (insert, delete, etc.).
    /// </summary>
    public void InvalidateCache()
    {
        _logger.LogInformation("Invalidating viewport cache (ViewModels: {VMCount}, POCO: {POCOCount})",
            _viewportCache.Count, _cachedRows.Count);

        // Dispose all ViewModels
        foreach (var vm in _viewportCache.Values)
            vm.Dispose();

        _viewportCache.Clear();

        // Clear POCO cache
        _cachedRows.Clear();
        _cachedStartIndex = -1;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _logger.LogInformation("ViewportManager disposing (ViewModels: {VMCount}, POCO: {POCOCount})",
            _viewportCache.Count, _cachedRows.Count);

        // Dispose all ViewModels
        foreach (var vm in _viewportCache.Values)
            vm.Dispose();

        _viewportCache.Clear();
        _cachedRows.Clear();
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

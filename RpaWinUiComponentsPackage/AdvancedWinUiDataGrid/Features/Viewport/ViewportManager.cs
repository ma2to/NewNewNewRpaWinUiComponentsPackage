using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common; // For SpecialColumnType enum

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
public sealed class ViewportManager : IDisposable
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
    /// ARCHITECTURE CHANGE: Viewport cache is now OPTIONAL tracking mechanism.
    /// GetRowViewModel() always returns canonical ViewModels from Rows collection.
    /// This method is kept for backward compatibility but does minimal work.
    /// </summary>
    public async Task UpdateViewportAsync(
        int firstVisibleIndex,
        int lastVisibleIndex,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogTrace("UpdateViewport called: first={First}, last={Last} (viewport cache tracking only)",
                firstVisibleIndex, lastVisibleIndex);

            // OPTIONAL: Track visible range in cache for diagnostics/monitoring
            // GetRowViewModel() doesn't use this cache - it returns directly from Rows collection
            var toRemove = _viewportCache.Keys
                .Where(idx => idx < firstVisibleIndex || idx > lastVisibleIndex)
                .ToList();

            foreach (var idx in toRemove)
            {
                _viewportCache.Remove(idx);
            }

            // Add visible range to cache (for tracking only)
            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                if (!_viewportCache.ContainsKey(i) && i >= 0 && i < _sourceViewModel.Rows.Count)
                {
                    _viewportCache[i] = _sourceViewModel.Rows[i];
                }
            }

            _logger.LogTrace("UpdateViewport completed: tracking {Size} visible rows", _viewportCache.Count);

            await Task.CompletedTask; // Keep async signature for API compatibility
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateViewport failed: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// OBSOLETE: No longer needed - ViewportManager now uses direct references to canonical ViewModels.
    /// Kept as stub for backward compatibility, but does nothing.
    /// </summary>
    [Obsolete("POCO caching removed - ViewportManager now uses canonical ViewModels from Rows collection")]
    private Task LoadPOCORangeAsync(
        int startIndex,
        int endIndex,
        CancellationToken cancellationToken)
    {
        // NO-OP: ViewportManager now uses direct references to Rows collection ViewModels
        _logger.LogDebug("LoadPOCORangeAsync called but POCO caching is disabled (using canonical ViewModels)");
        return Task.CompletedTask;
    }

    /// <summary>
    /// OBSOLETE: No longer creates ViewModels - ViewportManager now uses canonical ViewModels from Rows collection.
    /// This method is kept as stub for backward compatibility but should never be called.
    /// </summary>
    [Obsolete("ViewportManager now uses canonical ViewModels from Rows collection - no longer creates copies")]
    private DataGridRowViewModel CreateViewModelForRow(RowData rowData, int rowIndex)
    {
        throw new InvalidOperationException(
            "CreateViewModelForRow should not be called - ViewportManager uses canonical ViewModels from Rows collection");
    }

    /// <summary>
    /// Gets ViewModel for specified row index.
    /// PAGINATION SUPPORT: Index is relative to CURRENT PAGE.
    ///
    /// ARCHITECTURE:
    /// - WITH PAGINATION: Index is page-relative (0-based within page)
    ///   - Page 0: index 0-99 → Rows[0-99]
    ///   - Page 1: index 0-99 → Rows[100-199]
    ///   - Page 2: index 0-49 → Rows[200-249] (last page, partial)
    ///
    /// - WITHOUT PAGINATION: Index is absolute (0-based in Rows collection)
    ///   - index 0 → Rows[0], index 150 → Rows[150]
    ///
    /// CRITICAL FIX: Adds page offset calculation to support pagination navigation.
    /// This fixes issue where rows beyond PageSize were invisible.
    /// </summary>
    /// <summary>
    /// ✅ SENIOR FIX: Simplified GetRowViewModel with dual-mode architecture support
    /// MODE 1 (Small datasets): Rows contains all data, direct index access
    /// MODE 2 (Large datasets): Rows contains only current page, page-relative index access
    /// CRITICAL: Index is always page-relative (0-14) after OnPageChanged loads new page data
    /// ARCHITECTURE: Rows collection is synced with current page via OnPageChanged → LoadRows
    /// </summary>
    public DataGridRowViewModel? GetRowViewModel(int index)
    {
        // ✅ DUAL-MODE ARCHITECTURE: index is page-relative (0-14 for PageSize=15)
        // Rows collection is synchronized with current page via OnPageChanged event handler
        // Example: Page 5 → OnPageChanged loads rows 60-74 → Rows[0-14] contains those 15 ViewModels
        // User scrolls to row 5 on page → GetRowViewModel(5) → returns Rows[5] (absolute row 65)

        if (index >= 0 && index < _sourceViewModel.Rows.Count)
        {
            var pageManager = _sourceViewModel.PageManager;

            if (pageManager != null)
            {
                _logger.LogTrace("GetRowViewModel: Page-relative index {PageIndex} → Rows[{PageIndex}] (Page {Page}/{TotalPages}, Rows.Count={RowsCount})",
                    index, index, pageManager.CurrentPage + 1, pageManager.TotalPages, _sourceViewModel.Rows.Count);
            }
            else
            {
                _logger.LogTrace("GetRowViewModel: Direct index {Index} → Rows[{Index}] (no pagination, Rows.Count={RowsCount})",
                    index, index, _sourceViewModel.Rows.Count);
            }

            return _sourceViewModel.Rows[index];
        }

        _logger.LogWarning("GetRowViewModel: Page-relative index {Index} out of range (Rows.Count={Count})",
            index, _sourceViewModel.Rows.Count);
        return null;

        // ✅ REMOVED OLD PAGINATION CODE: No longer needed with dual-mode architecture
        // Old behavior: Calculate absoluteIndex = pageStartIndex + index, access Rows[absoluteIndex]
        // Problem: Rows contained ALL data (1000 rows), causing memory issues
        // New behavior: Rows contains ONLY current page (15 rows), direct index access
        // Benefits: 98.5% memory reduction, simpler code, no absolute index calculation
    }

    /// <summary>
    /// Creates empty placeholder ViewModel for virtual rows (when data count < page size)
    /// </summary>
    private DataGridRowViewModel CreateEmptyRowViewModel(int rowIndex)
    {
        var emptyRow = new DataGridRowViewModel
        {
            RowIndex = rowIndex,
            RowId = $"__empty_{rowIndex}"
        };

        // Create empty cells for all columns
        foreach (var header in _sourceViewModel.ColumnHeaders)
        {
            var emptyCell = new CellViewModel(_themeManager, null)
            {
                RowIndex = rowIndex,
                RowId = $"__empty_{rowIndex}",
                ColumnName = header.ColumnName,
                Value = null, // Empty cell
                IsReadOnly = true // Empty rows are read-only
            };

            emptyRow.Cells.Add(emptyCell);
        }

        return emptyRow;
    }

    /// <summary>
    /// Total row count for ItemsRepeater (FIXED UI POOL).
    /// ALWAYS returns PageSize (not actual data count) to maintain fixed UI object pool.
    /// Empty rows (when data count < PageSize) are rendered as invisible placeholders.
    ///
    /// ARCHITECTURE:
    /// - FIXED UI POOL: UI always has EXACTLY PageSize ViewModels (e.g., 15)
    /// - Data count may be less than PageSize (e.g., page 7 has 10 rows)
    /// - Empty slots (15-10=5) are marked as IsVisible=false in UpdateViewModelsInPlace()
    /// - DataGridElementFactory skips invisible rows (returns collapsed placeholder)
    ///
    /// EXAMPLE with 100 total rows, PageSize=15, TotalPages=7:
    /// - Page 1: 15 data rows → UI pool: 15 ViewModels (15 visible + 0 invisible)
    /// - Page 7: 10 data rows → UI pool: 15 ViewModels (10 visible + 5 invisible)
    ///
    /// CRITICAL: ALWAYS return PageSize (not variable count) to prevent UI object creation/disposal!
    /// </summary>
    public int TotalRowCount
    {
        get
        {
            var pageManager = _sourceViewModel.PageManager;
            if (pageManager != null && pageManager.TotalDataRows > 0)
            {
                // ✅ CRITICAL: Always return PageSize (not variable count!)
                // REASON: UI pool is FIXED size
                // EXAMPLE: Page 7 has 10 data rows but UI always has 15 ViewModels (5 empty)
                var pageSize = pageManager.PageSize;

                _logger.LogTrace("TotalRowCount: Fixed UI pool size={PageSize} (Page {Page}/{TotalPages})",
                    pageSize, pageManager.CurrentPage + 1, pageManager.TotalPages);

                return pageSize;  // ← ALWAYS PageSize (15), not variable!
            }

            _logger.LogWarning("PageManager not configured or TotalDataRows=0, returning 0 rows");
            return 0;
        }
        set
        {
            _totalRowCount = value;
            _logger.LogDebug("TotalRowCount set to {Count} (ignored, using PageSize instead)", value);
        }
    }

    /// <summary>
    /// Invalidates viewport cache (clears references to canonical ViewModels).
    /// Call this when row data changes (insert, delete, etc.).
    /// ARCHITECTURE CHANGE: NO DISPOSAL - these are references to canonical ViewModels in Rows collection.
    /// </summary>
    public void InvalidateCache()
    {
        _logger.LogInformation("Invalidating viewport cache (clearing {Count} references to canonical ViewModels)",
            _viewportCache.Count);

        // Just clear references - DON'T dispose (canonical ViewModels are owned by Rows collection)
        _viewportCache.Clear();

        // POCO cache no longer used
        _cachedRows.Clear();
        _cachedStartIndex = -1;
    }

    /// <summary>
    /// OBSOLETE: No longer needed - ViewportManager uses canonical ViewModels which are always in sync.
    /// Kept as no-op stub for backward compatibility.
    /// </summary>
    [Obsolete("RefreshViewModelsFromSource is no longer needed - ViewportManager uses canonical ViewModels")]
    public void RefreshViewModelsFromSource(IEnumerable<string>? rowIds = null)
    {
        // NO-OP: ViewportManager now uses direct references to canonical ViewModels in Rows collection
        // All changes to canonical ViewModels are automatically visible in viewport
        _logger.LogDebug("RefreshViewModelsFromSource called but is no-op (using canonical ViewModels)");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _logger.LogInformation("ViewportManager disposing (clearing {Count} references to canonical ViewModels)",
            _viewportCache.Count);

        // Just clear references - DON'T dispose (canonical ViewModels are owned by Rows collection)
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

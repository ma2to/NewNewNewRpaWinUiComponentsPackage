using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;

/// <summary>
/// PROFESSIONAL QUALITY: Adapts IRowStore to IDataSource interface.
/// Provides async lazy-loading facade over existing synchronous IRowStore.
/// This allows ViewportManager to load data on-demand without modifying IRowStore implementation.
/// </summary>
internal sealed class RowStoreDataSource : IDataSource
{
    private readonly Interfaces.IRowStore _rowStore;
    private readonly ILogger<RowStoreDataSource>? _logger;

    /// <summary>
    /// Creates a new RowStoreDataSource adapter.
    /// </summary>
    /// <param name="rowStore">Underlying row store for data access</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public RowStoreDataSource(Interfaces.IRowStore rowStore, ILogger<RowStoreDataSource>? logger = null)
    {
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _logger = logger;
    }

    /// <summary>
    /// Gets total row count from underlying row store.
    /// </summary>
    public int TotalRowCount => _rowStore.GetRowCount();

    /// <summary>
    /// Loads a single row asynchronously by index.
    /// Wraps synchronous IRowStore.GetRow() in Task for async interface compatibility.
    /// </summary>
    public Task<IReadOnlyDictionary<string, object?>?> LoadRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            // IRowStore is synchronous, wrap in Task for async interface
            var rowData = _rowStore.GetRow(rowIndex);
            return Task.FromResult(rowData);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load row at index {RowIndex}", rowIndex);
            return Task.FromResult<IReadOnlyDictionary<string, object?>?>(null);
        }
    }

    /// <summary>
    /// Loads a range of rows asynchronously (batch optimization).
    /// More efficient than multiple LoadRowAsync calls for large ranges.
    /// </summary>
    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> LoadRowRangeAsync(
        int startIndex,
        int endIndex,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new List<IReadOnlyDictionary<string, object?>>();

            for (int i = startIndex; i <= endIndex; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowData = _rowStore.GetRow(i);
                if (rowData != null)
                {
                    result.Add(rowData);
                }
            }

            _logger?.LogDebug("Loaded row range {Start}-{End}, count={Count}", startIndex, endIndex, result.Count);
            return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load row range {Start}-{End}", startIndex, endIndex);
            return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(
                Array.Empty<IReadOnlyDictionary<string, object?>>());
        }
    }

    /// <summary>
    /// Invalidates cache for specific row index.
    /// IRowStore doesn't cache, so this is a no-op for now.
    /// Future: If we add caching layer, implement here.
    /// </summary>
    public void InvalidateRow(int rowIndex)
    {
        _logger?.LogDebug("Row invalidated: {RowIndex}", rowIndex);
        // IRowStore doesn't cache, no-op for now
    }

    /// <summary>
    /// Invalidates entire cache.
    /// IRowStore doesn't cache, so this is a no-op for now.
    /// </summary>
    public void InvalidateAll()
    {
        _logger?.LogDebug("All rows invalidated");
        // IRowStore doesn't cache, no-op for now
    }
}

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// INTERNAL SERVICE: Performance monitoring operations implementation
/// CLEAN ARCHITECTURE: Application layer service for performance operations
/// </summary>
internal sealed class PerformanceService : IPerformanceService
{
    private readonly ConcurrentDictionary<int, DataPage> _pageCache = new();
    private VirtualizationConfiguration _currentConfiguration = VirtualizationConfiguration.Default;
    private long _totalOperations = 0;
    private readonly Stopwatch _totalTimeStopwatch = Stopwatch.StartNew();
    private double _currentMemoryUsage = 0;
    private DateTime _lastReset = DateTime.UtcNow;

    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(
        TimeSpan? timeWindow = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var virtualizationStats = GetVirtualizationStatisticsInternal();

            return PerformanceMetrics.Create(
                _totalOperations,
                _totalTimeStopwatch.Elapsed,
                _currentMemoryUsage,
                virtualizationStats);
        }, cancellationToken);
    }

    public async Task ResetPerformanceCountersAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _totalOperations = 0;
            _totalTimeStopwatch.Restart();
            _currentMemoryUsage = 0;
            _lastReset = DateTime.UtcNow;
            _pageCache.Clear();
        }, cancellationToken);
    }

    public async Task<DataPage> LoadPageAsync(
        int pageIndex,
        VirtualizationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Check cache first
                if (_pageCache.TryGetValue(pageIndex, out var cachedPage))
                {
                    stopwatch.Stop();
                    return DataPage.Create(
                        pageIndex,
                        cachedPage.StartRowIndex,
                        cachedPage.EndRowIndex,
                        cachedPage.Data,
                        stopwatch.Elapsed,
                        fromCache: true);
                }

                // Simulate page loading
                var startRowIndex = pageIndex * configuration.PageSize;
                var endRowIndex = Math.Min(startRowIndex + configuration.PageSize - 1, startRowIndex + configuration.PageSize);

                // In a real implementation, would load actual data
                var pageData = Array.Empty<IReadOnlyDictionary<string, object?>>();

                var page = DataPage.Create(
                    pageIndex,
                    startRowIndex,
                    endRowIndex,
                    pageData,
                    stopwatch.Elapsed,
                    fromCache: false);

                // Cache the page if enabled
                if (configuration.IsEnabled)
                {
                    _pageCache.TryAdd(pageIndex, page);

                    // Clean up old pages if cache is full
                    if (_pageCache.Count > configuration.MaxCachedPages)
                    {
                        CleanupOldPages(configuration.MaxCachedPages / 2);
                    }
                }

                Interlocked.Increment(ref _totalOperations);
                _currentMemoryUsage = EstimateMemoryUsage();

                stopwatch.Stop();
                return page;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                // Return empty page on error
                return DataPage.Create(pageIndex, 0, 0, Array.Empty<IReadOnlyDictionary<string, object?>>(), stopwatch.Elapsed);
            }
        }, cancellationToken);
    }

    public async Task<PerformanceStatistics> GetVirtualizationStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => GetVirtualizationStatisticsInternal(), cancellationToken);
    }

    public async Task OptimizeMemoryUsageAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            // Clear half of the cached pages to free memory
            var targetCount = Math.Max(1, _pageCache.Count / 2);
            CleanupOldPages(targetCount);

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _currentMemoryUsage = EstimateMemoryUsage();
        }, cancellationToken);
    }

    public async Task<double> GetCurrentMemoryUsageMBAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _currentMemoryUsage = EstimateMemoryUsage();
            return _currentMemoryUsage;
        }, cancellationToken);
    }

    public async Task ApplyVirtualizationConfigurationAsync(
        VirtualizationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _currentConfiguration = configuration;

            // Clear cache if virtualization is disabled
            if (!configuration.IsEnabled)
            {
                _pageCache.Clear();
            }
            // Adjust cache size if needed
            else if (_pageCache.Count > configuration.MaxCachedPages)
            {
                CleanupOldPages(configuration.MaxCachedPages);
            }

            _currentMemoryUsage = EstimateMemoryUsage();
        }, cancellationToken);
    }

    public VirtualizationConfiguration GetCurrentConfiguration()
    {
        return _currentConfiguration;
    }

    private PerformanceStatistics GetVirtualizationStatisticsInternal()
    {
        var totalRows = _pageCache.Count * _currentConfiguration.PageSize;
        var memoryUsage = EstimateMemoryUsage();

        return PerformanceStatistics.Create(
            totalRows,
            _pageCache.Count,
            _pageCache.Count,
            memoryUsage);
    }

    private double EstimateMemoryUsage()
    {
        // Simple estimation based on cached pages
        // In a real implementation, would use more sophisticated memory tracking
        var estimatedBytesPerPage = 1024 * 100; // 100KB per page estimate
        var totalBytes = _pageCache.Count * estimatedBytesPerPage;
        return totalBytes / (1024.0 * 1024.0); // Convert to MB
    }

    private void CleanupOldPages(int targetCacheSize)
    {
        var currentCount = _pageCache.Count;
        var toRemove = Math.Max(0, currentCount - targetCacheSize);

        if (toRemove <= 0)
            return;

        // Remove oldest pages (simple FIFO strategy)
        // In a real implementation, would use LRU or other sophisticated strategies
        var removed = 0;
        foreach (var kvp in _pageCache)
        {
            if (removed >= toRemove)
                break;

            if (_pageCache.TryRemove(kvp.Key, out _))
            {
                removed++;
            }
        }
    }
}
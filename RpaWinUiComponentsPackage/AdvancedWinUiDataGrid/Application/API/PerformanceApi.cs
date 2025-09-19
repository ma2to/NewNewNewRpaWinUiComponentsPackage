using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.API;

/// <summary>
/// PUBLIC API: Virtualization configuration for large datasets
/// </summary>
public sealed record VirtualizationConfiguration
{
    /// <summary>Enable data virtualization</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Number of rows to load per page</summary>
    public int PageSize { get; init; } = 1000;

    /// <summary>Maximum number of pages to keep in memory</summary>
    public int MaxCachedPages { get; init; } = 10;

    /// <summary>Number of rows to buffer before and after visible area</summary>
    public int BufferSize { get; init; } = 500;

    /// <summary>Enable preemptive loading of adjacent pages</summary>
    public bool EnablePreemptiveLoading { get; init; } = true;

    /// <summary>Maximum memory usage in MB before forcing cleanup</summary>
    public int MaxMemoryUsageMB { get; init; } = 500;

    /// <summary>Enable background data loading to reduce UI blocking</summary>
    public bool EnableBackgroundLoading { get; init; } = true;

    /// <summary>Timeout for data loading operations</summary>
    public TimeSpan LoadingTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Create default configuration optimized for 100k rows</summary>
    public static VirtualizationConfiguration Default => new();

    /// <summary>Create configuration optimized for 1M+ rows</summary>
    public static VirtualizationConfiguration LargeDataset => new()
    {
        PageSize = 2000,
        MaxCachedPages = 15,
        BufferSize = 1000,
        MaxMemoryUsageMB = 1000
    };

    /// <summary>Create configuration optimized for 10M+ rows</summary>
    public static VirtualizationConfiguration MassiveDataset => new()
    {
        PageSize = 5000,
        MaxCachedPages = 20,
        BufferSize = 2500,
        MaxMemoryUsageMB = 2000
    };
}

/// <summary>
/// PUBLIC API: Performance statistics for monitoring
/// </summary>
public sealed record PerformanceStatistics
{
    public long TotalRows { get; init; }
    public int LoadedPages { get; init; }
    public int CachedPages { get; init; }
    public double MemoryUsageMB { get; init; }
    public TimeSpan AveragePageLoadTime { get; init; }
    public int CacheHitRatio { get; init; }
    public DateTime LastCleanup { get; init; }
    public int BackgroundLoadingQueueSize { get; init; }

    public static PerformanceStatistics Create(long totalRows, int loadedPages, int cachedPages, double memoryUsage) =>
        new()
        {
            TotalRows = totalRows,
            LoadedPages = loadedPages,
            CachedPages = cachedPages,
            MemoryUsageMB = memoryUsage,
            LastCleanup = DateTime.UtcNow
        };
}

/// <summary>
/// PUBLIC API: Data page for virtualization
/// </summary>
public sealed record DataPage
{
    public int PageIndex { get; init; }
    public int StartRowIndex { get; init; }
    public int EndRowIndex { get; init; }
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Data { get; init; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
    public DateTime LoadedAt { get; init; }
    public TimeSpan LoadTime { get; init; }
    public bool IsFromCache { get; init; }

    public static DataPage Create(int pageIndex, int startRow, int endRow, IReadOnlyList<IReadOnlyDictionary<string, object?>> data, TimeSpan loadTime = default, bool fromCache = false) =>
        new()
        {
            PageIndex = pageIndex,
            StartRowIndex = startRow,
            EndRowIndex = endRow,
            Data = data,
            LoadedAt = DateTime.UtcNow,
            LoadTime = loadTime,
            IsFromCache = fromCache
        };
}

/// <summary>
/// PUBLIC API: Progress information for large data operations
/// </summary>
public sealed record LargeDataProgress
{
    public long ProcessedRows { get; init; }
    public long TotalRows { get; init; }
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    public TimeSpan ElapsedTime { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }
    public string CurrentOperation { get; init; } = string.Empty;
    public double ThroughputRowsPerSecond { get; init; }

    public static LargeDataProgress Create(long processed, long total, TimeSpan elapsed, string operation = "") =>
        new()
        {
            ProcessedRows = processed,
            TotalRows = total,
            ElapsedTime = elapsed,
            CurrentOperation = operation,
            ThroughputRowsPerSecond = elapsed.TotalSeconds > 0 ? processed / elapsed.TotalSeconds : 0
        };
}
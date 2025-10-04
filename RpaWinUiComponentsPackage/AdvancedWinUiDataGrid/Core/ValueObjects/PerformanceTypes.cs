using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// Performance metric types
/// </summary>
internal enum PerformanceMetric
{
    OperationCount,
    ErrorCount,
    MemoryUsage,
    CpuTime,
    Duration
}

/// <summary>
/// Performance threshold levels
/// </summary>
internal enum PerformanceThreshold
{
    Normal,
    Warning,
    Critical
}

/// <summary>
/// Performance snapshot with metrics
/// </summary>
internal sealed record PerformanceSnapshot
{
    public long TotalOperations { get; init; }
    public long TotalErrors { get; init; }
    public double ErrorRate { get; init; }
    public long CurrentMemoryUsage { get; init; }
    public TimeSpan CpuTime { get; init; }
    public int ThreadCount { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static PerformanceSnapshot Create(long operations, long errors, long memoryUsage, TimeSpan cpuTime, int threadCount) =>
        new()
        {
            TotalOperations = operations,
            TotalErrors = errors,
            ErrorRate = operations > 0 ? (double)errors / operations * 100 : 0,
            CurrentMemoryUsage = memoryUsage,
            CpuTime = cpuTime,
            ThreadCount = threadCount
        };
}

/// <summary>
/// Performance report with analysis
/// </summary>
internal sealed record PerformanceReport
{
    public required PerformanceSnapshot Snapshot { get; init; }
    public IReadOnlyList<string> Bottlenecks { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
    public PerformanceThreshold Threshold { get; init; } = PerformanceThreshold.Normal;
    public TimeSpan AnalysisDuration { get; init; }

    public static PerformanceReport Create(PerformanceSnapshot snapshot, IReadOnlyList<string> bottlenecks, PerformanceThreshold threshold) =>
        new()
        {
            Snapshot = snapshot,
            Bottlenecks = bottlenecks,
            Threshold = threshold
        };
}

/// <summary>
/// Performance statistics aggregation
/// </summary>
internal sealed record PerformanceStatistics
{
    public long TotalOperations { get; init; }
    public long TotalErrors { get; init; }
    public double AverageOperationTime { get; init; }
    public long PeakMemoryUsage { get; init; }
    public long CurrentMemoryUsage { get; init; }
    public int ActivePoolTypes { get; init; }
    public long ObjectsInPool { get; init; }
    public TimeSpan TotalUptime { get; init; }

    public static PerformanceStatistics Create(long operations, long errors, double avgTime, long peakMemory, long currentMemory) =>
        new()
        {
            TotalOperations = operations,
            TotalErrors = errors,
            AverageOperationTime = avgTime,
            PeakMemoryUsage = peakMemory,
            CurrentMemoryUsage = currentMemory
        };
}

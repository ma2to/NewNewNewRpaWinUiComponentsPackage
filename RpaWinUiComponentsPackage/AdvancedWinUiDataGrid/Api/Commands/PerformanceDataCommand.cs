namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Performance threshold levels
/// </summary>
public enum PublicPerformanceThreshold
{
    Normal,
    Warning,
    Critical
}

/// <summary>
/// Command to start performance monitoring
/// </summary>
/// <param name="MonitoringWindow">Duration to monitor performance</param>
/// <param name="IncludeSystemMetrics">Include system-level metrics</param>
/// <param name="IncludeMemoryMetrics">Include memory usage metrics</param>
public record StartPerformanceMonitoringCommand(
    TimeSpan MonitoringWindow = default,
    bool IncludeSystemMetrics = true,
    bool IncludeMemoryMetrics = true
)
{
    public StartPerformanceMonitoringCommand() : this(TimeSpan.FromMinutes(5), true, true) { }
}

/// <summary>
/// Performance snapshot data
/// </summary>
/// <param name="TotalOperations">Total operations executed</param>
/// <param name="TotalErrors">Total errors encountered</param>
/// <param name="ErrorRate">Error rate percentage</param>
/// <param name="CurrentMemoryUsage">Current memory usage in bytes</param>
/// <param name="CpuTime">Total CPU time</param>
/// <param name="ThreadCount">Active thread count</param>
/// <param name="Timestamp">Snapshot timestamp</param>
public record PerformanceSnapshotData(
    long TotalOperations,
    long TotalErrors,
    double ErrorRate,
    long CurrentMemoryUsage,
    TimeSpan CpuTime,
    int ThreadCount,
    DateTime Timestamp
)
{
    public PerformanceSnapshotData() : this(0, 0, 0, 0, TimeSpan.Zero, 0, DateTime.UtcNow) { }
}

/// <summary>
/// Performance report with analysis
/// </summary>
/// <param name="Snapshot">Current performance snapshot</param>
/// <param name="Bottlenecks">Identified performance bottlenecks</param>
/// <param name="Recommendations">Performance recommendations</param>
/// <param name="Threshold">Current performance threshold level</param>
/// <param name="AnalysisDuration">Time taken to analyze</param>
public record PerformanceReportData(
    PerformanceSnapshotData Snapshot,
    IReadOnlyList<string> Bottlenecks,
    IReadOnlyList<string> Recommendations,
    PublicPerformanceThreshold Threshold,
    TimeSpan AnalysisDuration
)
{
    public PerformanceReportData() : this(
        new PerformanceSnapshotData(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        PublicPerformanceThreshold.Normal,
        TimeSpan.Zero
    ) { }
}

/// <summary>
/// Performance statistics aggregation
/// </summary>
/// <param name="TotalOperations">Total operations</param>
/// <param name="TotalErrors">Total errors</param>
/// <param name="AverageOperationTime">Average operation time in ms</param>
/// <param name="PeakMemoryUsage">Peak memory usage in bytes</param>
/// <param name="CurrentMemoryUsage">Current memory usage in bytes</param>
public record PerformanceStatisticsData(
    long TotalOperations,
    long TotalErrors,
    double AverageOperationTime,
    long PeakMemoryUsage,
    long CurrentMemoryUsage
)
{
    public PerformanceStatisticsData() : this(0, 0, 0, 0, 0) { }
}

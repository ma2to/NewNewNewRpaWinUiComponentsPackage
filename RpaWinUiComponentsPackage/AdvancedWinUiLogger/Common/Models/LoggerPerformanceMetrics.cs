using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC MODEL: Logger performance metrics
/// ENTERPRISE: Performance tracking for monitoring and optimization
/// </summary>
public sealed record LoggerPerformanceMetrics
{
    public long TotalLogEntries { get; init; }
    public TimeSpan TotalLoggingTime { get; init; }
    public double AverageEntryTime { get; init; }
    public double EntriesPerSecond { get; init; }
    public long MemoryUsageBytes { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime LastReset { get; init; }

    public double MemoryUsageMB => MemoryUsageBytes / (1024.0 * 1024.0);

    public static LoggerPerformanceMetrics Create(
        long totalEntries,
        TimeSpan totalTime,
        long memoryUsage)
    {
        return new LoggerPerformanceMetrics
        {
            TotalLogEntries = totalEntries,
            TotalLoggingTime = totalTime,
            AverageEntryTime = totalEntries > 0 ? totalTime.TotalMilliseconds / totalEntries : 0,
            EntriesPerSecond = totalTime.TotalSeconds > 0 ? totalEntries / totalTime.TotalSeconds : 0,
            MemoryUsageBytes = memoryUsage,
            StartTime = DateTime.UtcNow.Subtract(totalTime),
            LastReset = DateTime.UtcNow
        };
    }
}

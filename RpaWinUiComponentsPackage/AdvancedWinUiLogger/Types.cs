using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC TYPES: Facade types that shadow internal Core types to avoid namespace pollution
/// These types are exactly the same as internal Core types but public in facade namespace
/// </summary>

// Cleanup Types
public sealed record CleanupResult
{
    public bool IsSuccess { get; init; }
    public int FilesDeleted { get; init; }
    public long BytesFreed { get; init; }
    public TimeSpan OperationDuration { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> DeletedFiles { get; init; } = Array.Empty<string>();

    public static CleanupResult Success(int filesDeleted, long bytesFreed, TimeSpan duration, IReadOnlyList<string> deletedFiles) =>
        new()
        {
            IsSuccess = true,
            FilesDeleted = filesDeleted,
            BytesFreed = bytesFreed,
            OperationDuration = duration,
            DeletedFiles = deletedFiles
        };

    public static CleanupResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

// Directory Summary Types
public sealed record LogDirectorySummary
{
    public string DirectoryPath { get; init; } = string.Empty;
    public int TotalFiles { get; init; }
    public long TotalSizeBytes { get; init; }
    public DateTime OldestFileDate { get; init; }
    public DateTime NewestFileDate { get; init; }
    public IReadOnlyList<LogFileInfo> Files { get; init; } = Array.Empty<LogFileInfo>();

    public double TotalSizeMB => TotalSizeBytes / (1024.0 * 1024.0);

    public static LogDirectorySummary Create(string directoryPath, IReadOnlyList<LogFileInfo> files) =>
        new()
        {
            DirectoryPath = directoryPath,
            TotalFiles = files.Count,
            TotalSizeBytes = files.Sum(f => f.SizeBytes),
            OldestFileDate = files.Any() ? files.Min(f => f.CreatedDate) : DateTime.MinValue,
            NewestFileDate = files.Any() ? files.Max(f => f.LastModifiedDate) : DateTime.MinValue,
            Files = files
        };
}

// Performance Metrics Types
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
        long memoryUsage) =>
        new()
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

// Search Types
public sealed record LogSearchCriteria
{
    public string SearchText { get; init; } = string.Empty;
    public LogLevel? MinLevel { get; init; }
    public LogLevel? MaxLevel { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool UseRegex { get; init; }
    public bool CaseSensitive { get; init; }
    public int? MaxResults { get; init; } = 1000;

    public static LogSearchCriteria Create(string searchText) =>
        new() { SearchText = searchText };
}

// Statistics Types
public sealed record LogStatistics
{
    public int TotalEntries { get; init; }
    public Dictionary<LogLevel, int> EntriesByLevel { get; init; } = new();
    public DateTime? FirstEntryDate { get; init; }
    public DateTime? LastEntryDate { get; init; }
    public TimeSpan TimeSpan { get; init; }
    public double AverageEntriesPerDay { get; init; }

    public static LogStatistics Create(
        int totalEntries,
        Dictionary<LogLevel, int> entriesByLevel,
        DateTime? firstEntry,
        DateTime? lastEntry) =>
        new()
        {
            TotalEntries = totalEntries,
            EntriesByLevel = entriesByLevel,
            FirstEntryDate = firstEntry,
            LastEntryDate = lastEntry,
            TimeSpan = lastEntry.HasValue && firstEntry.HasValue ? lastEntry.Value - firstEntry.Value : TimeSpan.Zero,
            AverageEntriesPerDay = CalculateAverageEntriesPerDay(totalEntries, firstEntry, lastEntry)
        };

    private static double CalculateAverageEntriesPerDay(int totalEntries, DateTime? firstEntry, DateTime? lastEntry)
    {
        if (!firstEntry.HasValue || !lastEntry.HasValue || totalEntries == 0)
            return 0;

        var days = (lastEntry.Value - firstEntry.Value).TotalDays;
        return days > 0 ? totalEntries / days : totalEntries;
    }
}
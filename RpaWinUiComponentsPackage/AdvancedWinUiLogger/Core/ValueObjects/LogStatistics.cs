using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

/// <summary>
/// ENTERPRISE VALUE OBJECT: Log statistics information
/// IMMUTABLE: Statistical analysis of log data
/// FUNCTIONAL: Metrics and aggregations for monitoring
/// </summary>
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
            TimeSpan = lastEntry.HasValue && firstEntry.HasValue ? lastEntry.Value - firstEntry.Value : System.TimeSpan.Zero,
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
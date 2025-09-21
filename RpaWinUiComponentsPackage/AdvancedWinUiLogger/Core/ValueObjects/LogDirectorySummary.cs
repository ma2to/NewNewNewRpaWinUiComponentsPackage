using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

/// <summary>
/// ENTERPRISE VALUE OBJECT: Log directory summary information
/// IMMUTABLE: Directory analysis with file statistics
/// FUNCTIONAL: Aggregated metrics for directory monitoring
/// </summary>
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
            OldestFileDate = files.Any() ? files.Min(f => f.CreatedUtc) : DateTime.MinValue,
            NewestFileDate = files.Any() ? files.Max(f => f.ModifiedUtc) : DateTime.MinValue,
            Files = files
        };
}
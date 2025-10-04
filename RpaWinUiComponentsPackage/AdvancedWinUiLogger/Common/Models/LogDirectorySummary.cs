using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC MODEL: Summary of log directory
/// ENTERPRISE: Directory-level statistics for monitoring
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

    public static LogDirectorySummary Create(string directoryPath, IReadOnlyList<LogFileInfo> files)
    {
        return new LogDirectorySummary
        {
            DirectoryPath = directoryPath,
            TotalFiles = files.Count,
            TotalSizeBytes = files.Sum(f => f.SizeBytes),
            OldestFileDate = files.Any() ? files.Min(f => f.CreatedUtc) : DateTime.MinValue,
            NewestFileDate = files.Any() ? files.Max(f => f.ModifiedUtc) : DateTime.MinValue,
            Files = files
        };
    }
}

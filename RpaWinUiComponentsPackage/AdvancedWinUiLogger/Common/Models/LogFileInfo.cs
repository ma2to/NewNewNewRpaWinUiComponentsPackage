using System;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC MODEL: Log file information value object
/// ENTERPRISE: Complete file metadata for management operations
/// </summary>
public sealed record LogFileInfo
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime ModifiedUtc { get; init; }
    public bool IsCompressed { get; init; }
    public LogLevel LogLevel { get; init; }

    public double SizeMB => SizeBytes / (1024.0 * 1024.0);
    public double SizeKB => SizeBytes / 1024.0;
    public double AgeDays => (DateTime.UtcNow - CreatedUtc).TotalDays;

    public static LogFileInfo Create(string filePath, long sizeBytes, DateTime createdUtc, DateTime modifiedUtc)
    {
        return new LogFileInfo
        {
            FilePath = filePath,
            FileName = System.IO.Path.GetFileName(filePath),
            SizeBytes = sizeBytes,
            CreatedUtc = createdUtc,
            ModifiedUtc = modifiedUtc
        };
    }

    public static LogFileInfo FromPath(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
            return new LogFileInfo { FilePath = filePath, FileName = System.IO.Path.GetFileName(filePath) };

        var fileInfo = new System.IO.FileInfo(filePath);
        return new LogFileInfo
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            SizeBytes = fileInfo.Length,
            CreatedUtc = fileInfo.CreationTimeUtc,
            ModifiedUtc = fileInfo.LastWriteTimeUtc,
            IsCompressed = filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
        };
    }
}

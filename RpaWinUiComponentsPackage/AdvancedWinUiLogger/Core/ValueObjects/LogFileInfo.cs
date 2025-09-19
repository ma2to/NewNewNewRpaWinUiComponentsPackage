using System;
using System.IO;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

/// <summary>
/// ENTERPRISE VALUE OBJECT: Log file metadata and information
/// IMMUTABLE: File information with domain-specific operations
/// FUNCTIONAL: Pure functions for file analysis and comparisons
/// </summary>
public sealed record LogFileInfo
{
    /// <summary>Full path to the log file</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>File name without path</summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>File size in bytes</summary>
    public long SizeBytes { get; init; }

    /// <summary>UTC timestamp when file was created</summary>
    public DateTime CreatedUtc { get; init; }

    /// <summary>UTC timestamp when file was last modified</summary>
    public DateTime ModifiedUtc { get; init; }

    /// <summary>Indicates if file is currently in use by logger</summary>
    public bool IsActive { get; init; }

    /// <summary>Indicates if file is archived (compressed or moved)</summary>
    public bool IsArchived { get; init; }

    /// <summary>Number of log entries in the file (estimated)</summary>
    public long EstimatedEntryCount { get; init; }

    /// <summary>
    /// ENTERPRISE: Constructor for manual creation
    /// </summary>
    public LogFileInfo() { }

    /// <summary>
    /// FUNCTIONAL: Create log file info from file system information
    /// ENTERPRISE: Factory method with file system integration
    /// </summary>
    public static Result<LogFileInfo> FromFileInfo(FileInfo fileInfo, bool isActive = false)
    {
        try
        {
            if (!fileInfo.Exists)
                return Result<LogFileInfo>.Failure($"File does not exist: {fileInfo.FullName}");

            return Result<LogFileInfo>.Success(new LogFileInfo
            {
                FilePath = fileInfo.FullName,
                SizeBytes = fileInfo.Length,
                CreatedUtc = fileInfo.CreationTimeUtc,
                ModifiedUtc = fileInfo.LastWriteTimeUtc,
                IsActive = isActive,
                IsArchived = fileInfo.Extension.Equals(".gz", StringComparison.OrdinalIgnoreCase) ||
                           fileInfo.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase),
                EstimatedEntryCount = EstimateLogEntryCount(fileInfo.Length)
            });
        }
        catch (Exception ex)
        {
            return Result<LogFileInfo>.Failure($"Failed to create LogFileInfo from {fileInfo.FullName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// FUNCTIONAL: Create log file info from path
    /// ENTERPRISE: Factory method with path validation
    /// </summary>
    public static Result<LogFileInfo> FromPath(string filePath, bool isActive = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return Result<LogFileInfo>.Failure("File path cannot be null or empty");

            var fileInfo = new FileInfo(filePath);
            return FromFileInfo(fileInfo, isActive);
        }
        catch (Exception ex)
        {
            return Result<LogFileInfo>.Failure($"Failed to create LogFileInfo from path {filePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// FUNCTIONAL: Get file size in megabytes
    /// PERFORMANCE: Cached calculation for display purposes
    /// </summary>
    public double SizeMB => SizeBytes / (1024.0 * 1024.0);

    /// <summary>
    /// FUNCTIONAL: Get file size in kilobytes
    /// </summary>
    public double SizeKB => SizeBytes / 1024.0;

    /// <summary>
    /// FUNCTIONAL: Get file age in days
    /// ENTERPRISE: Age calculation for retention policies
    /// </summary>
    public double AgeDays => (DateTime.UtcNow - CreatedUtc).TotalDays;

    /// <summary>
    /// FUNCTIONAL: Get time since last modification in hours
    /// </summary>
    public double HoursSinceModified => (DateTime.UtcNow - ModifiedUtc).TotalHours;

    /// <summary>
    /// FUNCTIONAL: Check if file exceeds size limit
    /// BUSINESS LOGIC: Size-based rotation criteria
    /// </summary>
    public bool ExceedsSizeLimit(long maxSizeBytes) => SizeBytes > maxSizeBytes;

    /// <summary>
    /// FUNCTIONAL: Check if file exceeds age limit
    /// BUSINESS LOGIC: Age-based cleanup criteria
    /// </summary>
    public bool ExceedsAgeLimit(int maxAgeDays) => AgeDays > maxAgeDays;

    /// <summary>
    /// FUNCTIONAL: Check if file has been inactive for specified duration
    /// BUSINESS LOGIC: Inactivity-based operations
    /// </summary>
    public bool IsInactiveFor(TimeSpan duration) =>
        (DateTime.UtcNow - ModifiedUtc) > duration;

    /// <summary>
    /// FUNCTIONAL: Check if file is eligible for archiving
    /// BUSINESS LOGIC: Complex archiving criteria
    /// </summary>
    public bool IsEligibleForArchiving(long maxSizeBytes, int maxAgeDays, TimeSpan inactivityThreshold) =>
        !IsActive &&
        !IsArchived &&
        (ExceedsSizeLimit(maxSizeBytes) || ExceedsAgeLimit(maxAgeDays) || IsInactiveFor(inactivityThreshold));

    /// <summary>
    /// FUNCTIONAL: Check if file is eligible for deletion
    /// BUSINESS LOGIC: Cleanup criteria evaluation
    /// </summary>
    public bool IsEligibleForDeletion(int maxAgeDays, bool deleteArchivedFiles = false) =>
        !IsActive &&
        ExceedsAgeLimit(maxAgeDays) &&
        (deleteArchivedFiles || !IsArchived);

    /// <summary>
    /// FUNCTIONAL: Get relative path from base directory
    /// UTILITY: Path operations for display and organization
    /// </summary>
    public string GetRelativePath(string baseDirectory)
    {
        try
        {
            var basePath = Path.GetFullPath(baseDirectory);
            var fullPath = Path.GetFullPath(FilePath);

            if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(basePath.Length).TrimStart('\\', '/');

            return FileName;
        }
        catch
        {
            return FileName;
        }
    }

    /// <summary>
    /// FUNCTIONAL: Get human-readable file size
    /// DISPLAY: Formatted size for user interfaces
    /// </summary>
    public string GetFormattedSize()
    {
        const double KB = 1024;
        const double MB = KB * 1024;
        const double GB = MB * 1024;

        return SizeBytes switch
        {
            var size when size < KB => $"{SizeBytes} B",
            var size when size < MB => $"{SizeBytes / KB:F1} KB",
            var size when size < GB => $"{SizeBytes / MB:F1} MB",
            _ => $"{SizeBytes / GB:F2} GB"
        };
    }

    /// <summary>
    /// FUNCTIONAL: Get comprehensive file summary
    /// DISPLAY: Detailed information for monitoring and debugging
    /// </summary>
    public string GetSummary() =>
        $"{FileName} ({GetFormattedSize()}, {AgeDays:F1} days old, " +
        $"{(IsActive ? "active" : "inactive")}{(IsArchived ? ", archived" : "")})";

    /// <summary>
    /// ENTERPRISE: Get telemetry data for monitoring
    /// OBSERVABILITY: Structured data for operational insights
    /// </summary>
    public object GetTelemetryData() => new
    {
        FileName,
        SizeBytes,
        SizeMB,
        AgeDays,
        HoursSinceModified,
        IsActive,
        IsArchived,
        EstimatedEntryCount,
        CreatedUtc,
        ModifiedUtc
    };

    /// <summary>
    /// INTERNAL: Estimate number of log entries based on file size
    /// HEURISTIC: Approximate calculation for performance metrics
    /// </summary>
    private static long EstimateLogEntryCount(long fileSizeBytes)
    {
        // Average log entry size estimate: 150 bytes
        // This is a rough estimate and will vary based on log format
        const int averageEntrySize = 150;
        return fileSizeBytes / averageEntrySize;
    }

    /// <summary>
    /// FUNCTIONAL: Compare files by creation time
    /// UTILITY: Sorting and ordering operations
    /// </summary>
    public static int CompareByCreationTime(LogFileInfo x, LogFileInfo y) =>
        x.CreatedUtc.CompareTo(y.CreatedUtc);

    /// <summary>
    /// FUNCTIONAL: Compare files by size
    /// UTILITY: Size-based sorting operations
    /// </summary>
    public static int CompareBySize(LogFileInfo x, LogFileInfo y) =>
        x.SizeBytes.CompareTo(y.SizeBytes);

    /// <summary>
    /// FUNCTIONAL: Compare files by modification time
    /// UTILITY: Activity-based sorting operations
    /// </summary>
    public static int CompareByModificationTime(LogFileInfo x, LogFileInfo y) =>
        x.ModifiedUtc.CompareTo(y.ModifiedUtc);
}
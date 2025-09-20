using System;
using System.IO;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

/// <summary>
/// ENTERPRISE VALUE OBJECT: Log file rotation operation result
/// IMMUTABLE: Contains detailed rotation operation metrics
/// FUNCTIONAL: Factory methods for different rotation outcomes
/// </summary>
public sealed record RotationResult
{
    /// <summary>Indicates if rotation operation was successful</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Path to the original file that was rotated</summary>
    public string? OldFilePath { get; init; }

    /// <summary>Path to the new file created after rotation</summary>
    public string? NewFilePath { get; init; }

    /// <summary>Size of the rotated file in bytes</summary>
    public long RotatedFileSize { get; init; }

    /// <summary>UTC timestamp when rotation operation completed</summary>
    public DateTime RotationTime { get; init; }

    /// <summary>Error message if rotation failed</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Number of files processed during rotation</summary>
    public int FilesProcessed { get; init; }

    /// <summary>Total bytes moved during rotation operation</summary>
    public long TotalBytesProcessed { get; init; }

    /// <summary>Duration of the rotation operation</summary>
    public TimeSpan OperationDuration { get; init; }

    /// <summary>Type of rotation that was performed</summary>
    public RotationType RotationType { get; init; }

    /// <summary>
    /// ENTERPRISE: Private constructor for controlled instantiation
    /// </summary>
    private RotationResult()
    {
        RotationTime = DateTime.UtcNow;
    }

    /// <summary>
    /// FUNCTIONAL: Create successful rotation result
    /// ENTERPRISE: Factory method for successful operations
    /// </summary>
    public static RotationResult Success(
        string newFilePath,
        long rotatedFileSize,
        string? oldFilePath = null,
        int filesProcessed = 1,
        long totalBytesProcessed = 0,
        TimeSpan operationDuration = default,
        RotationType rotationType = RotationType.SizeBased) => new()
        {
            IsSuccess = true,
            OldFilePath = oldFilePath,
            NewFilePath = newFilePath,
            RotatedFileSize = rotatedFileSize,
            RotationTime = DateTime.UtcNow,
            FilesProcessed = filesProcessed,
            TotalBytesProcessed = totalBytesProcessed == 0 ? rotatedFileSize : totalBytesProcessed,
            OperationDuration = operationDuration,
            RotationType = rotationType
        };

    /// <summary>
    /// FUNCTIONAL: Create failed rotation result
    /// ENTERPRISE: Factory method for failed operations with error context
    /// </summary>
    public static RotationResult Failure(
        string errorMessage,
        RotationType rotationType = RotationType.Unknown,
        TimeSpan operationDuration = default,
        string? attemptedFilePath = null) => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            RotationTime = DateTime.UtcNow,
            OperationDuration = operationDuration,
            RotationType = rotationType,
            NewFilePath = attemptedFilePath
        };

    /// <summary>
    /// FUNCTIONAL: Create result for no rotation needed
    /// ENTERPRISE: Explicit indication when rotation was not necessary
    /// </summary>
    public static RotationResult NotNeeded(string currentFilePath, long currentFileSize) => new()
    {
        IsSuccess = true,
        NewFilePath = currentFilePath,
        RotatedFileSize = currentFileSize,
        RotationTime = DateTime.UtcNow,
        FilesProcessed = 0,
        TotalBytesProcessed = 0,
        RotationType = RotationType.NotNeeded
    };

    /// <summary>
    /// FUNCTIONAL: Get rotated file size in megabytes
    /// PERFORMANCE: Cached calculation for display purposes
    /// </summary>
    public double RotatedFileSizeMB => RotatedFileSize / (1024.0 * 1024.0);

    /// <summary>
    /// FUNCTIONAL: Get total processed bytes in megabytes
    /// </summary>
    public double TotalBytesProcessedMB => TotalBytesProcessed / (1024.0 * 1024.0);

    /// <summary>
    /// FUNCTIONAL: Check if rotation involved file archiving
    /// </summary>
    public bool HasArchivedFile => !string.IsNullOrEmpty(OldFilePath);

    /// <summary>
    /// FUNCTIONAL: Check if this was an actual rotation operation
    /// </summary>
    public bool WasRotationPerformed => IsSuccess && FilesProcessed > 0;

    /// <summary>
    /// FUNCTIONAL: Calculate rotation efficiency (MB/second)
    /// PERFORMANCE: Metrics for operation optimization
    /// </summary>
    public double RotationThroughputMBps =>
        OperationDuration.TotalSeconds > 0 ? TotalBytesProcessedMB / OperationDuration.TotalSeconds : 0;

    /// <summary>
    /// FUNCTIONAL: Get human-readable summary message
    /// ENTERPRISE: Detailed status reporting for monitoring
    /// </summary>
    public string GetSummary()
    {
        if (!IsSuccess)
            return $"Rotation failed ({RotationType}): {ErrorMessage}";

        return RotationType switch
        {
            RotationType.NotNeeded => $"No rotation needed: {Path.GetFileName(NewFilePath)} ({RotatedFileSizeMB:F2} MB)",
            RotationType.SizeBased => $"Size-based rotation: {GetRotationDetails()}",
            RotationType.TimeBased => $"Time-based rotation: {GetRotationDetails()}",
            RotationType.Manual => $"Manual rotation: {GetRotationDetails()}",
            RotationType.Cleanup => $"Cleanup rotation: {FilesProcessed} files processed ({TotalBytesProcessedMB:F2} MB)",
            _ => $"Rotation completed: {GetRotationDetails()}"
        };
    }

    /// <summary>
    /// INTERNAL: Get detailed rotation information
    /// </summary>
    private string GetRotationDetails()
    {
        var details = HasArchivedFile
            ? $"Archived {Path.GetFileName(OldFilePath)}, Created {Path.GetFileName(NewFilePath)}"
            : $"Created {Path.GetFileName(NewFilePath)}";

        details += $" ({RotatedFileSizeMB:F2} MB)";

        if (OperationDuration.TotalMilliseconds > 0)
            details += $", {OperationDuration.TotalMilliseconds:F0}ms";

        return details;
    }

    /// <summary>
    /// ENTERPRISE: Get structured data for monitoring systems
    /// TELEMETRY: Metrics collection for operational insights
    /// </summary>
    public object GetTelemetryData() => new
    {
        IsSuccess,
        RotationType = RotationType.ToString(),
        RotatedFileSizeMB,
        TotalBytesProcessedMB,
        FilesProcessed,
        OperationDurationMs = OperationDuration.TotalMilliseconds,
        ThroughputMBps = RotationThroughputMBps,
        HasArchivedFile,
        RotationTime
    };
}

/// <summary>
/// ENTERPRISE ENUM: Types of rotation operations
/// DOMAIN: Classification of different rotation scenarios
/// </summary>
public enum RotationType
{
    /// <summary>Unknown or unspecified rotation type</summary>
    Unknown = 0,

    /// <summary>Rotation triggered by file size limit</summary>
    SizeBased = 1,

    /// <summary>Rotation triggered by time interval</summary>
    TimeBased = 2,

    /// <summary>Manual rotation requested by user</summary>
    Manual = 3,

    /// <summary>Cleanup operation to remove old files</summary>
    Cleanup = 4,

    /// <summary>No rotation was needed</summary>
    NotNeeded = 5
}
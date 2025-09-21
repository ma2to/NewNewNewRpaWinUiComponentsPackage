using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// INTERNAL CONVERSIONS: Extension methods to convert between public facade types and internal Core types
/// This allows facade to work with internal services while exposing public API
/// </summary>
internal static class TypeExtensions
{
    // CleanupResult Extensions
    public static CleanupResult ToPublic(this CoreTypes.CleanupResult result) => new()
    {
        IsSuccess = result.IsSuccess,
        FilesDeleted = result.FilesDeleted,
        BytesFreed = result.BytesFreed,
        OperationDuration = result.OperationDuration,
        ErrorMessage = result.ErrorMessage,
        DeletedFiles = result.DeletedFiles
    };

    public static CoreTypes.CleanupResult ToInternal(this CleanupResult result) => new()
    {
        IsSuccess = result.IsSuccess,
        FilesDeleted = result.FilesDeleted,
        BytesFreed = result.BytesFreed,
        OperationDuration = result.OperationDuration,
        ErrorMessage = result.ErrorMessage,
        DeletedFiles = result.DeletedFiles
    };

    // LogDirectorySummary Extensions
    public static LogDirectorySummary ToPublic(this CoreTypes.LogDirectorySummary summary) => new()
    {
        DirectoryPath = summary.DirectoryPath,
        TotalFiles = summary.TotalFiles,
        TotalSizeBytes = summary.TotalSizeBytes,
        OldestFileDate = summary.OldestFileDate,
        NewestFileDate = summary.NewestFileDate,
        Files = summary.Files.ToPublicLogFileList()
    };

    // LoggerPerformanceMetrics Extensions
    public static LoggerPerformanceMetrics ToPublic(this CoreTypes.LoggerPerformanceMetrics metrics) => new()
    {
        TotalLogEntries = metrics.TotalLogEntries,
        TotalLoggingTime = metrics.TotalLoggingTime,
        AverageEntryTime = metrics.AverageEntryTime,
        EntriesPerSecond = metrics.EntriesPerSecond,
        MemoryUsageBytes = metrics.MemoryUsageBytes,
        StartTime = metrics.StartTime,
        LastReset = metrics.LastReset
    };

    // LogSearchCriteria Extensions
    public static CoreTypes.LogSearchCriteria ToInternal(this LogSearchCriteria criteria) => new()
    {
        SearchText = criteria.SearchText,
        MinLevel = criteria.MinLevel,
        MaxLevel = criteria.MaxLevel,
        FromDate = criteria.FromDate,
        ToDate = criteria.ToDate,
        UseRegex = criteria.UseRegex,
        CaseSensitive = criteria.CaseSensitive,
        MaxResults = criteria.MaxResults
    };

    // LogStatistics Extensions
    public static LogStatistics ToPublic(this CoreTypes.LogStatistics stats) => new()
    {
        TotalEntries = stats.TotalEntries,
        EntriesByLevel = stats.EntriesByLevel,
        FirstEntryDate = stats.FirstEntryDate,
        LastEntryDate = stats.LastEntryDate,
        TimeSpan = stats.TimeSpan,
        AverageEntriesPerDay = stats.AverageEntriesPerDay
    };

    // RotationResult Extensions - Mapping between different property names
    public static RotationResult ToPublic(this CoreTypes.RotationResult result) => new()
    {
        IsSuccess = result.IsSuccess,
        OriginalFilePath = result.OldFilePath,
        OriginalFileSize = result.RotatedFileSize,
        NewFilePath = result.NewFilePath,
        RotationNumber = result.FilesProcessed,
        CompressedSize = result.TotalBytesProcessed,
        OperationDuration = result.OperationDuration,
        ErrorMessage = result.ErrorMessage
    };

    public static CoreTypes.RotationResult ToInternal(this RotationResult result) =>
        result.IsSuccess
            ? CoreTypes.RotationResult.Success(
                result.NewFilePath ?? "",
                result.OriginalFileSize,
                result.OriginalFilePath,
                result.RotationNumber,
                result.CompressedSize,
                result.OperationDuration)
            : CoreTypes.RotationResult.Failure(result.ErrorMessage ?? "", operationDuration: result.OperationDuration);

    // LogFileInfo Extensions - Core has more properties than public facade
    public static LogFileInfo ToPublic(this CoreTypes.LogFileInfo fileInfo) => new()
    {
        FilePath = fileInfo.FilePath,
        FileName = fileInfo.FileName,
        SizeBytes = fileInfo.SizeBytes,
        CreatedUtc = fileInfo.CreatedUtc,
        ModifiedUtc = fileInfo.ModifiedUtc,
        IsCompressed = fileInfo.IsArchived,
        LogLevel = Microsoft.Extensions.Logging.LogLevel.Information // Default, Core doesn't have LogLevel
    };

    public static CoreTypes.LogFileInfo ToInternal(this LogFileInfo fileInfo)
    {
        // Use factory method from Core to create proper Core instance
        var result = CoreTypes.LogFileInfo.FromPath(fileInfo.FilePath);
        return result.IsSuccess ? result.Value : new CoreTypes.LogFileInfo
        {
            FilePath = fileInfo.FilePath,
            SizeBytes = fileInfo.SizeBytes,
            CreatedUtc = fileInfo.CreatedUtc,
            ModifiedUtc = fileInfo.ModifiedUtc,
            IsArchived = fileInfo.IsCompressed
        };
    }

    // LoggerConfiguration Extensions - Core has different property names
    public static LoggerConfiguration ToPublic(this CoreTypes.LoggerConfiguration config) => new()
    {
        LogDirectory = config.LogDirectory,
        BaseFileName = config.BaseFileName,
        MinimumLevel = config.MinLogLevel,
        MaxFileSize = (config.MaxFileSizeMB ?? 10) * 1024 * 1024, // Convert MB to bytes
        MaxFiles = config.MaxLogFiles,
        EnableCompression = config.EnableCompression,
        EnableStructuredLogging = config.EnableStructuredLogging,
        OutputTemplate = $"{{Timestamp:{config.DateFormat}}} [{{Level:u3}}] {{Message}}{{NewLine}}{{Exception}}",
        EnablePerformanceCounters = config.EnablePerformanceMonitoring
    };

    public static CoreTypes.LoggerConfiguration ToInternal(this LoggerConfiguration config) => new()
    {
        LogDirectory = config.LogDirectory,
        BaseFileName = config.BaseFileName,
        MinLogLevel = config.MinimumLevel,
        MaxFileSizeMB = (int)(config.MaxFileSize / (1024 * 1024)), // Convert bytes to MB
        MaxLogFiles = config.MaxFiles,
        EnableCompression = config.EnableCompression,
        EnableStructuredLogging = config.EnableStructuredLogging,
        EnablePerformanceMonitoring = config.EnablePerformanceCounters
    };

    // Collection conversions
    public static IReadOnlyList<T> ToPublicList<T, TInternal>(this IReadOnlyList<TInternal> internalList, Func<TInternal, T> converter)
    {
        return internalList.Select(converter).ToArray();
    }

    public static IReadOnlyList<TInternal> ToInternalList<T, TInternal>(this IReadOnlyList<T> publicList, Func<T, TInternal> converter)
    {
        return publicList.Select(converter).ToArray();
    }

    // Specific collection conversions
    public static IReadOnlyList<LogFileInfo> ToPublicLogFileList(this IReadOnlyList<CoreTypes.LogFileInfo> internalList)
    {
        return internalList.Select(ToPublic).ToArray();
    }

    public static IReadOnlyList<CoreTypes.LogFileInfo> ToInternalLogFileList(this IReadOnlyList<LogFileInfo> publicList)
    {
        return publicList.Select(ToInternal).ToArray();
    }
}
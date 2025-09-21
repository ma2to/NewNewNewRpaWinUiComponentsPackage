using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC API TYPES: Public DTO and enums with mapping to internal types
/// Internal types remain internal, accessed via facade mapping pattern
/// </summary>

// LogFileInfo Type - Public DTO
public sealed record LogFileInfo
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime ModifiedUtc { get; init; }
    public bool IsCompressed { get; init; }
    public Microsoft.Extensions.Logging.LogLevel LogLevel { get; init; }

    public double SizeMB => SizeBytes / (1024.0 * 1024.0);
    public double SizeKB => SizeBytes / 1024.0;
    public double AgeDays => (DateTime.UtcNow - CreatedUtc).TotalDays;

    public static LogFileInfo Create(string filePath, long sizeBytes, DateTime createdUtc, DateTime modifiedUtc) =>
        new()
        {
            FilePath = filePath,
            FileName = System.IO.Path.GetFileName(filePath),
            SizeBytes = sizeBytes,
            CreatedUtc = createdUtc,
            ModifiedUtc = modifiedUtc
        };
}

// RotationResult Type - Public DTO
public sealed record RotationResult
{
    public bool IsSuccess { get; init; }
    public string? OriginalFilePath { get; init; }
    public long OriginalFileSize { get; init; }
    public string? NewFilePath { get; init; }
    public int RotationNumber { get; init; }
    public long CompressedSize { get; init; }
    public TimeSpan OperationDuration { get; init; }
    public string? ErrorMessage { get; init; }

    public static RotationResult Success(string newFilePath, long fileSize, string? oldFilePath, int rotationNumber, long compressedSize, TimeSpan duration) =>
        new()
        {
            IsSuccess = true,
            OriginalFilePath = oldFilePath,
            OriginalFileSize = fileSize,
            NewFilePath = newFilePath,
            RotationNumber = rotationNumber,
            CompressedSize = compressedSize,
            OperationDuration = duration
        };

    public static RotationResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

// LoggingMode Strategy Enum
public enum LoggingMode
{
    Single,
    Bulk,
    AsyncBatch
}

// LoggerConfiguration Type - Public DTO
public sealed record LoggerConfiguration
{
    public string LogDirectory { get; init; } = string.Empty;
    public string BaseFileName { get; init; } = string.Empty;
    public Microsoft.Extensions.Logging.LogLevel MinimumLevel { get; init; } = Microsoft.Extensions.Logging.LogLevel.Information;
    public long MaxFileSize { get; init; } = 10 * 1024 * 1024; // 10MB
    public int MaxFiles { get; init; } = 10;
    public bool EnableCompression { get; init; } = false;
    public bool EnableStructuredLogging { get; init; } = true;
    public string OutputTemplate { get; init; } = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}";
    public bool EnablePerformanceCounters { get; init; } = false;
    public LoggingMode LoggingMode { get; init; } = LoggingMode.Single;

    public static LoggerConfiguration CreateMinimal(string logDirectory, string baseFileName) =>
        new() { LogDirectory = logDirectory, BaseFileName = baseFileName };

    public static LoggerConfiguration CreateHighPerformance(string logDirectory, string baseFileName) =>
        new()
        {
            LogDirectory = logDirectory,
            BaseFileName = baseFileName,
            LoggingMode = LoggingMode.AsyncBatch,
            EnableCompression = true,
            MaxFileSize = 50 * 1024 * 1024,
            EnablePerformanceCounters = true
        };

    public static LoggerConfiguration CreateDevelopment(string logDirectory, string baseFileName) =>
        new()
        {
            LogDirectory = logDirectory,
            BaseFileName = baseFileName,
            MinimumLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
            LoggingMode = LoggingMode.Single,
            EnableStructuredLogging = true
        };
}

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
            OldestFileDate = files.Any() ? files.Min(f => f.CreatedUtc) : DateTime.MinValue,
            NewestFileDate = files.Any() ? files.Max(f => f.ModifiedUtc) : DateTime.MinValue,
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

// Configuration Builder Pattern - Fluent interface for complex configuration
public sealed class ConfigurationBuilder
{
    private string _logDirectory = "./logs";
    private string _baseFileName = "application";
    private LogLevel _minimumLevel = LogLevel.Information;
    private long _maxFileSize = 10 * 1024 * 1024;
    private int _maxFiles = 10;
    private bool _enableCompression = false;
    private bool _enableStructuredLogging = true;
    private string _outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}";
    private bool _enablePerformanceCounters = false;
    private LoggingMode _loggingMode = LoggingMode.Single;

    public ConfigurationBuilder SetLogDirectory(string logDirectory)
    {
        _logDirectory = logDirectory;
        return this;
    }

    public ConfigurationBuilder SetBaseFileName(string baseFileName)
    {
        _baseFileName = baseFileName;
        return this;
    }

    public ConfigurationBuilder SetMinimumLevel(LogLevel minimumLevel)
    {
        _minimumLevel = minimumLevel;
        return this;
    }

    public ConfigurationBuilder SetMaxFileSize(long maxFileSize)
    {
        _maxFileSize = maxFileSize;
        return this;
    }

    public ConfigurationBuilder SetMaxFiles(int maxFiles)
    {
        _maxFiles = maxFiles;
        return this;
    }

    public ConfigurationBuilder EnableCompression(bool enable = true)
    {
        _enableCompression = enable;
        return this;
    }

    public ConfigurationBuilder EnableStructuredLogging(bool enable = true)
    {
        _enableStructuredLogging = enable;
        return this;
    }

    public ConfigurationBuilder SetOutputTemplate(string outputTemplate)
    {
        _outputTemplate = outputTemplate;
        return this;
    }

    public ConfigurationBuilder EnablePerformanceCounters(bool enable = true)
    {
        _enablePerformanceCounters = enable;
        return this;
    }

    public ConfigurationBuilder SetLoggingMode(LoggingMode loggingMode)
    {
        _loggingMode = loggingMode;
        return this;
    }

    public LoggerConfiguration Build() => new()
    {
        LogDirectory = _logDirectory,
        BaseFileName = _baseFileName,
        MinimumLevel = _minimumLevel,
        MaxFileSize = _maxFileSize,
        MaxFiles = _maxFiles,
        EnableCompression = _enableCompression,
        EnableStructuredLogging = _enableStructuredLogging,
        OutputTemplate = _outputTemplate,
        EnablePerformanceCounters = _enablePerformanceCounters,
        LoggingMode = _loggingMode
    };
}
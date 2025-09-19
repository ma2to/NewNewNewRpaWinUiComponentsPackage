using System;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

/// <summary>
/// ENTERPRISE VALUE OBJECT: Immutable logger configuration
/// FUNCTIONAL: Complete configuration with validation and transformations
/// CLEAN ARCHITECTURE: Core domain configuration without external dependencies
/// </summary>
public sealed record LoggerConfiguration
{
    /// <summary>Directory where log files will be stored</summary>
    public required string LogDirectory { get; init; }

    /// <summary>Base name for log files (without extension)</summary>
    public string BaseFileName { get; init; } = "application";

    /// <summary>Maximum file size in MB before rotation (null = no size limit)</summary>
    public int? MaxFileSizeMB { get; init; } = 10;

    /// <summary>Maximum number of log files to retain</summary>
    public int MaxLogFiles { get; init; } = 10;

    /// <summary>Enable automatic file rotation</summary>
    public bool EnableAutoRotation { get; init; } = true;

    /// <summary>Enable real-time log viewing capabilities</summary>
    public bool EnableRealTimeViewing { get; init; } = false;

    /// <summary>Minimum log level to write</summary>
    public LogLevel MinLogLevel { get; init; } = LogLevel.Information;

    /// <summary>Enable structured JSON logging format</summary>
    public bool EnableStructuredLogging { get; init; } = true;

    /// <summary>Enable asynchronous background logging</summary>
    public bool EnableBackgroundLogging { get; init; } = true;

    /// <summary>Internal buffer size for batching log entries</summary>
    public int BufferSize { get; init; } = 1000;

    /// <summary>Interval for flushing buffered entries to disk</summary>
    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Enable performance metrics collection</summary>
    public bool EnablePerformanceMonitoring { get; init; } = false;

    /// <summary>Date format for timestamp formatting</summary>
    public string DateFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>Enable automatic cleanup of old log files</summary>
    public bool EnableAutoCleanup { get; init; } = true;

    /// <summary>Maximum age of log files before cleanup (days)</summary>
    public int MaxFileAgeDays { get; init; } = 30;

    /// <summary>Enable compression for archived log files</summary>
    public bool EnableCompression { get; init; } = false;

    /// <summary>
    /// FUNCTIONAL: Create minimal configuration for quick setup
    /// ENTERPRISE: Factory method for common scenarios
    /// </summary>
    public static LoggerConfiguration CreateMinimal(string logDirectory, string baseFileName = "application") => new()
    {
        LogDirectory = logDirectory,
        BaseFileName = baseFileName,
        MaxFileSizeMB = 10,
        MaxLogFiles = 5,
        EnableAutoRotation = true,
        EnableRealTimeViewing = false,
        MinLogLevel = LogLevel.Information,
        EnableStructuredLogging = false,
        EnableBackgroundLogging = true,
        BufferSize = 100,
        FlushInterval = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    /// FUNCTIONAL: Create high-performance configuration
    /// ENTERPRISE: Optimized for high-throughput scenarios
    /// </summary>
    public static LoggerConfiguration CreateHighPerformance(string logDirectory, string baseFileName = "application") => new()
    {
        LogDirectory = logDirectory,
        BaseFileName = baseFileName,
        MaxFileSizeMB = 50,
        MaxLogFiles = 20,
        EnableAutoRotation = true,
        EnableRealTimeViewing = false,
        MinLogLevel = LogLevel.Warning,
        EnableStructuredLogging = true,
        EnableBackgroundLogging = true,
        BufferSize = 5000,
        FlushInterval = TimeSpan.FromSeconds(2),
        EnablePerformanceMonitoring = true,
        EnableCompression = true
    };

    /// <summary>
    /// FUNCTIONAL: Create development configuration
    /// ENTERPRISE: Optimized for development scenarios with detailed logging
    /// </summary>
    public static LoggerConfiguration CreateDevelopment(string logDirectory, string baseFileName = "dev") => new()
    {
        LogDirectory = logDirectory,
        BaseFileName = baseFileName,
        MaxFileSizeMB = 5,
        MaxLogFiles = 3,
        EnableAutoRotation = true,
        EnableRealTimeViewing = true,
        MinLogLevel = LogLevel.Trace,
        EnableStructuredLogging = true,
        EnableBackgroundLogging = false,
        BufferSize = 50,
        FlushInterval = TimeSpan.FromSeconds(1),
        EnablePerformanceMonitoring = true,
        MaxFileAgeDays = 7
    };

    /// <summary>
    /// VALIDATION: Comprehensive configuration validation
    /// ENTERPRISE: Business rule validation with detailed error reporting
    /// </summary>
    public Result<bool> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(LogDirectory))
            errors.Add("LogDirectory cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(BaseFileName))
            errors.Add("BaseFileName cannot be null or whitespace");

        if (MaxFileSizeMB.HasValue && MaxFileSizeMB <= 0)
            errors.Add("MaxFileSizeMB must be greater than 0 when specified");

        if (MaxLogFiles <= 0)
            errors.Add("MaxLogFiles must be greater than 0");

        if (BufferSize <= 0)
            errors.Add("BufferSize must be greater than 0");

        if (FlushInterval <= TimeSpan.Zero)
            errors.Add("FlushInterval must be greater than zero");

        if (MaxFileAgeDays <= 0)
            errors.Add("MaxFileAgeDays must be greater than 0");

        if (string.IsNullOrWhiteSpace(DateFormat))
            errors.Add("DateFormat cannot be null or whitespace");

        // Validate date format
        try
        {
            DateTime.Now.ToString(DateFormat);
        }
        catch (FormatException)
        {
            errors.Add($"DateFormat '{DateFormat}' is not a valid date format");
        }

        if (errors.Any())
        {
            return Result<bool>.Failure($"Configuration validation failed: {string.Join(", ", errors)}");
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// FUNCTIONAL: Get maximum file size in bytes
    /// PERFORMANCE: Cached calculation for frequent access
    /// </summary>
    public long? MaxFileSizeBytes => MaxFileSizeMB * 1024L * 1024L;

    /// <summary>
    /// FUNCTIONAL: Get current log file path
    /// ENTERPRISE: Path construction with date-based naming
    /// </summary>
    public string GetCurrentLogFilePath() =>
        Path.Combine(LogDirectory, $"{BaseFileName}_{DateTime.Now:yyyyMMdd}.log");

    /// <summary>
    /// FUNCTIONAL: Get archived log file path
    /// ENTERPRISE: Consistent naming convention for archived files
    /// </summary>
    public string GetArchivedLogFilePath(DateTime date, int sequence = 0) =>
        Path.Combine(LogDirectory, $"{BaseFileName}_{date:yyyyMMdd}_{sequence:D3}.log");

    /// <summary>
    /// FUNCTIONAL: Check if log level meets minimum threshold
    /// PERFORMANCE: Fast level checking for filtering
    /// </summary>
    public bool ShouldLog(LogLevel level) => level >= MinLogLevel;

    /// <summary>
    /// FUNCTIONAL: Transform configuration with new log directory
    /// IMMUTABLE: Returns new instance with updated directory
    /// </summary>
    public LoggerConfiguration WithLogDirectory(string newDirectory) =>
        this with { LogDirectory = newDirectory };

    /// <summary>
    /// FUNCTIONAL: Transform configuration with new minimum log level
    /// IMMUTABLE: Returns new instance with updated level
    /// </summary>
    public LoggerConfiguration WithMinLogLevel(LogLevel newLevel) =>
        this with { MinLogLevel = newLevel };

    /// <summary>
    /// FUNCTIONAL: Transform configuration for production use
    /// ENTERPRISE: Apply production-ready settings
    /// </summary>
    public LoggerConfiguration ForProduction() => this with
    {
        MinLogLevel = LogLevel.Information,
        EnableRealTimeViewing = false,
        EnableBackgroundLogging = true,
        EnablePerformanceMonitoring = true,
        BufferSize = Math.Max(BufferSize, 1000),
        FlushInterval = TimeSpan.FromSeconds(Math.Max(FlushInterval.TotalSeconds, 5))
    };

    /// <summary>
    /// ENTERPRISE: Calculate estimated memory usage
    /// PERFORMANCE: Memory estimation for capacity planning
    /// </summary>
    public long EstimateMemoryUsage() =>
        BufferSize * 512L + // Estimated average log entry size
        1024L * 1024L; // Base overhead for logger infrastructure
}
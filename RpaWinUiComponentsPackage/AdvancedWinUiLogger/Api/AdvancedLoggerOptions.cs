using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC CONFIGURATION: Options for AdvancedWinUiLogger component
/// CLEAN ARCHITECTURE: Public API configuration without internal dependencies
/// ENTERPRISE: Professional configuration for advanced logging scenarios
/// </summary>
public sealed record AdvancedLoggerOptions
{
    /// <summary>Directory where log files will be stored</summary>
    public string LogDirectory { get; init; } = "./logs";

    /// <summary>Base name for log files (without extension)</summary>
    public string BaseFileName { get; init; } = "application";

    /// <summary>Minimum log level to write</summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;

    /// <summary>Maximum file size in bytes before rotation</summary>
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10MB

    /// <summary>Maximum number of log files to retain</summary>
    public int MaxFiles { get; init; } = 10;

    /// <summary>Enable automatic file rotation</summary>
    public bool EnableAutoRotation { get; init; } = true;

    /// <summary>Enable compression for archived log files</summary>
    public bool EnableCompression { get; init; } = false;

    /// <summary>Enable structured JSON logging format</summary>
    public bool EnableStructuredLogging { get; init; } = true;

    /// <summary>Enable performance metrics collection</summary>
    public bool EnablePerformanceMonitoring { get; init; } = false;

    /// <summary>Logging mode strategy (Single, Bulk, AsyncBatch)</summary>
    public LoggingMode LoggingMode { get; init; } = LoggingMode.Single;

    /// <summary>Internal buffer size for batching log entries</summary>
    public int BufferSize { get; init; } = 1000;

    /// <summary>Interval for flushing buffered entries to disk</summary>
    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Enable automatic cleanup of old log files</summary>
    public bool EnableAutoCleanup { get; init; } = true;

    /// <summary>Maximum age of log files before cleanup (days)</summary>
    public int MaxFileAgeDays { get; init; } = 30;

    /// <summary>Date format for timestamp formatting</summary>
    public string DateFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>Enable background logging for async operations</summary>
    public bool EnableBackgroundLogging { get; init; } = true;

    /// <summary>
    /// FACTORY METHOD: Create minimal configuration for quick setup
    /// ENTERPRISE: Factory method for common scenarios
    /// </summary>
    public static AdvancedLoggerOptions CreateMinimal(string logDirectory, string baseFileName = "application")
    {
        return new AdvancedLoggerOptions
        {
            LogDirectory = logDirectory,
            BaseFileName = baseFileName,
            MaxFileSizeBytes = 10 * 1024 * 1024,
            MaxFiles = 5,
            EnableAutoRotation = true,
            MinimumLevel = LogLevel.Information,
            EnableStructuredLogging = false,
            EnableBackgroundLogging = true,
            BufferSize = 100,
            FlushInterval = TimeSpan.FromSeconds(10),
            LoggingMode = LoggingMode.Single
        };
    }

    /// <summary>
    /// FACTORY METHOD: Create high-performance configuration
    /// ENTERPRISE: Optimized for high-throughput scenarios
    /// </summary>
    public static AdvancedLoggerOptions CreateHighPerformance(string logDirectory, string baseFileName = "application")
    {
        return new AdvancedLoggerOptions
        {
            LogDirectory = logDirectory,
            BaseFileName = baseFileName,
            MaxFileSizeBytes = 50 * 1024 * 1024,
            MaxFiles = 20,
            EnableAutoRotation = true,
            MinimumLevel = LogLevel.Warning,
            EnableStructuredLogging = true,
            EnableBackgroundLogging = true,
            BufferSize = 5000,
            FlushInterval = TimeSpan.FromSeconds(2),
            EnablePerformanceMonitoring = true,
            EnableCompression = true,
            LoggingMode = LoggingMode.AsyncBatch
        };
    }

    /// <summary>
    /// FACTORY METHOD: Create development configuration
    /// ENTERPRISE: Optimized for development scenarios with detailed logging
    /// </summary>
    public static AdvancedLoggerOptions CreateDevelopment(string logDirectory, string baseFileName = "dev")
    {
        return new AdvancedLoggerOptions
        {
            LogDirectory = logDirectory,
            BaseFileName = baseFileName,
            MaxFileSizeBytes = 5 * 1024 * 1024,
            MaxFiles = 3,
            EnableAutoRotation = true,
            MinimumLevel = LogLevel.Trace,
            EnableStructuredLogging = true,
            EnableBackgroundLogging = false,
            BufferSize = 50,
            FlushInterval = TimeSpan.FromSeconds(1),
            EnablePerformanceMonitoring = true,
            MaxFileAgeDays = 7,
            LoggingMode = LoggingMode.Single
        };
    }

    /// <summary>
    /// VALIDATION: Comprehensive configuration validation
    /// ENTERPRISE: Business rule validation with detailed error reporting
    /// </summary>
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(LogDirectory))
            return (false, "LogDirectory cannot be null or whitespace");

        if (string.IsNullOrWhiteSpace(BaseFileName))
            return (false, "BaseFileName cannot be null or whitespace");

        if (MaxFileSizeBytes <= 0)
            return (false, "MaxFileSizeBytes must be greater than 0");

        if (MaxFiles <= 0)
            return (false, "MaxFiles must be greater than 0");

        if (BufferSize <= 0)
            return (false, "BufferSize must be greater than 0");

        if (FlushInterval <= TimeSpan.Zero)
            return (false, "FlushInterval must be greater than zero");

        if (MaxFileAgeDays <= 0)
            return (false, "MaxFileAgeDays must be greater than 0");

        if (string.IsNullOrWhiteSpace(DateFormat))
            return (false, "DateFormat cannot be null or whitespace");

        // Validate date format
        try
        {
            DateTime.Now.ToString(DateFormat);
        }
        catch (FormatException)
        {
            return (false, $"DateFormat '{DateFormat}' is not a valid date format");
        }

        return (true, null);
    }

    /// <summary>
    /// FUNCTIONAL: Get current log file path
    /// ENTERPRISE: Path construction with date-based naming
    /// </summary>
    public string GetCurrentLogFilePath()
    {
        return System.IO.Path.Combine(LogDirectory, $"{BaseFileName}_{DateTime.Now:yyyyMMdd}.log");
    }

    /// <summary>
    /// FUNCTIONAL: Check if log level meets minimum threshold
    /// PERFORMANCE: Fast level checking for filtering
    /// </summary>
    public bool ShouldLog(LogLevel level)
    {
        return level >= MinimumLevel;
    }

    /// <summary>
    /// FUNCTIONAL: Transform configuration with new log directory
    /// IMMUTABLE: Returns new instance with updated directory
    /// </summary>
    public AdvancedLoggerOptions WithLogDirectory(string newDirectory)
    {
        return this with { LogDirectory = newDirectory };
    }

    /// <summary>
    /// FUNCTIONAL: Transform configuration with new minimum log level
    /// IMMUTABLE: Returns new instance with updated level
    /// </summary>
    public AdvancedLoggerOptions WithMinLogLevel(LogLevel newLevel)
    {
        return this with { MinimumLevel = newLevel };
    }
}

/// <summary>
/// PUBLIC ENUM: Logging mode strategy
/// STRATEGY PATTERN: Different logging strategies for different scenarios
/// </summary>
public enum LoggingMode
{
    /// <summary>Single entry per write - simple and immediate</summary>
    Single,

    /// <summary>Bulk batching - balanced performance</summary>
    Bulk,

    /// <summary>Asynchronous batching - maximum performance</summary>
    AsyncBatch
}

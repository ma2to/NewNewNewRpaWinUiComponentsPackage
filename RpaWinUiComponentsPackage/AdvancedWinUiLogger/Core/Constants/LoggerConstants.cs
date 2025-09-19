using System;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Constants;

/// <summary>
/// ENTERPRISE CONSTANTS: Core logger configuration and behavior constants
/// IMMUTABLE: Central location for system-wide constants
/// MAINTENANCE: Single source of truth for configurable values
/// </summary>
internal static class LoggerConstants
{
    #region File Management Constants

    /// <summary>Default log file extension</summary>
    public const string DefaultLogFileExtension = ".log";

    /// <summary>Default archived log file extension</summary>
    public const string DefaultArchiveExtension = ".gz";

    /// <summary>Default base name for log files</summary>
    public const string DefaultBaseFileName = "application";

    /// <summary>Default date format for log file names</summary>
    public const string DefaultFileDateFormat = "yyyyMMdd";

    /// <summary>Default timestamp format for log entries</summary>
    public const string DefaultTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>Maximum length for log file base name</summary>
    public const int MaxBaseFileNameLength = 100;

    /// <summary>Maximum number of rotation attempts before failure</summary>
    public const int MaxRotationAttempts = 3;

    #endregion

    #region Size and Capacity Constants

    /// <summary>Default maximum file size in MB</summary>
    public const int DefaultMaxFileSizeMB = 10;

    /// <summary>Minimum allowed file size in MB</summary>
    public const int MinFileSizeMB = 1;

    /// <summary>Maximum allowed file size in MB</summary>
    public const int MaxFileSizeMB = 1024; // 1 GB

    /// <summary>Default maximum number of log files to retain</summary>
    public const int DefaultMaxFileCount = 10;

    /// <summary>Minimum number of files to retain</summary>
    public const int MinFileCount = 1;

    /// <summary>Maximum number of files to retain</summary>
    public const int MaxFileCount = 1000;

    /// <summary>Default buffer size for batching log entries</summary>
    public const int DefaultBufferSize = 1000;

    /// <summary>Minimum buffer size</summary>
    public const int MinBufferSize = 10;

    /// <summary>Maximum buffer size</summary>
    public const int MaxBufferSize = 50000;

    /// <summary>Estimated average log entry size in bytes</summary>
    public const int AverageLogEntrySizeBytes = 150;

    #endregion

    #region Timing Constants

    /// <summary>Default flush interval in seconds</summary>
    public const int DefaultFlushIntervalSeconds = 5;

    /// <summary>Minimum flush interval in seconds</summary>
    public const int MinFlushIntervalSeconds = 1;

    /// <summary>Maximum flush interval in seconds</summary>
    public const int MaxFlushIntervalSeconds = 300; // 5 minutes

    /// <summary>Default maximum file age in days before cleanup</summary>
    public const int DefaultMaxFileAgeDays = 30;

    /// <summary>Minimum file age in days</summary>
    public const int MinFileAgeDays = 1;

    /// <summary>Maximum file age in days</summary>
    public const int MaxFileAgeDays = 3650; // 10 years

    /// <summary>Default session timeout in minutes</summary>
    public const int DefaultSessionTimeoutMinutes = 60;

    /// <summary>File operation timeout in milliseconds</summary>
    public const int FileOperationTimeoutMs = 30000; // 30 seconds

    #endregion

    #region Log Level and Formatting Constants

    /// <summary>Default minimum log level</summary>
    public static readonly LogLevel DefaultMinLogLevel = LogLevel.Information;

    /// <summary>Production recommended minimum log level</summary>
    public static readonly LogLevel ProductionMinLogLevel = LogLevel.Warning;

    /// <summary>Development recommended minimum log level</summary>
    public static readonly LogLevel DevelopmentMinLogLevel = LogLevel.Trace;

    /// <summary>Default log entry separator</summary>
    public const string DefaultLogSeparator = "----------------------------------------";

    /// <summary>JSON log format identifier</summary>
    public const string JsonFormatIdentifier = "JSON";

    /// <summary>Plain text format identifier</summary>
    public const string TextFormatIdentifier = "TEXT";

    #endregion

    #region Error Messages

    /// <summary>Error message for null configuration</summary>
    public const string ErrorNullConfiguration = "Logger configuration cannot be null";

    /// <summary>Error message for invalid log directory</summary>
    public const string ErrorInvalidLogDirectory = "Log directory path is invalid or inaccessible";

    /// <summary>Error message for invalid file name</summary>
    public const string ErrorInvalidFileName = "Log file name contains invalid characters";

    /// <summary>Error message for file in use</summary>
    public const string ErrorFileInUse = "Log file is currently in use by another process";

    /// <summary>Error message for disk space</summary>
    public const string ErrorInsufficientDiskSpace = "Insufficient disk space for logging operations";

    /// <summary>Error message for permission denied</summary>
    public const string ErrorPermissionDenied = "Access denied to log directory or file";

    /// <summary>Error message for session not found</summary>
    public const string ErrorSessionNotFound = "Logger session not found";

    /// <summary>Error message for inactive session</summary>
    public const string ErrorSessionInactive = "Logger session is not active";

    #endregion

    #region Performance Thresholds

    /// <summary>Warning threshold for flush operation duration (ms)</summary>
    public const int FlushWarningThresholdMs = 1000;

    /// <summary>Warning threshold for rotation operation duration (ms)</summary>
    public const int RotationWarningThresholdMs = 5000;

    /// <summary>Critical threshold for memory usage (bytes)</summary>
    public const long MemoryUsageCriticalThresholdBytes = 100 * 1024 * 1024; // 100 MB

    /// <summary>Warning threshold for pending entries count</summary>
    public const int PendingEntriesWarningThreshold = 10000;

    /// <summary>Maximum allowed memory usage for logger (bytes)</summary>
    public const long MaxLoggerMemoryUsageBytes = 500 * 1024 * 1024; // 500 MB

    #endregion

    #region Directory and Path Constants

    /// <summary>Default subdirectory name for archived logs</summary>
    public const string DefaultArchiveSubdirectory = "archived";

    /// <summary>Default subdirectory name for temporary files</summary>
    public const string DefaultTempSubdirectory = "temp";

    /// <summary>Configuration file name</summary>
    public const string ConfigurationFileName = "logger-config.json";

    /// <summary>Session state file name</summary>
    public const string SessionStateFileName = "logger-session.json";

    /// <summary>Metrics file name</summary>
    public const string MetricsFileName = "logger-metrics.json";

    #endregion

    #region Feature Flags

    /// <summary>Default value for auto rotation feature</summary>
    public const bool DefaultEnableAutoRotation = true;

    /// <summary>Default value for background logging feature</summary>
    public const bool DefaultEnableBackgroundLogging = true;

    /// <summary>Default value for structured logging feature</summary>
    public const bool DefaultEnableStructuredLogging = true;

    /// <summary>Default value for performance monitoring feature</summary>
    public const bool DefaultEnablePerformanceMonitoring = false;

    /// <summary>Default value for auto cleanup feature</summary>
    public const bool DefaultEnableAutoCleanup = true;

    /// <summary>Default value for compression feature</summary>
    public const bool DefaultEnableCompression = false;

    /// <summary>Default value for real-time viewing feature</summary>
    public const bool DefaultEnableRealTimeViewing = false;

    #endregion

    #region Validation Patterns

    /// <summary>Regular expression for valid file names</summary>
    public const string ValidFileNamePattern = @"^[a-zA-Z0-9_\-\.]+$";

    /// <summary>Regular expression for valid directory names</summary>
    public const string ValidDirectoryNamePattern = @"^[a-zA-Z0-9_\-\.\\\:\/]+$";

    /// <summary>Maximum path length for cross-platform compatibility</summary>
    public const int MaxPathLength = 260;

    #endregion

    #region Environment Specific

    /// <summary>Environment name for development</summary>
    public const string EnvironmentDevelopment = "Development";

    /// <summary>Environment name for staging</summary>
    public const string EnvironmentStaging = "Staging";

    /// <summary>Environment name for production</summary>
    public const string EnvironmentProduction = "Production";

    /// <summary>Environment name for testing</summary>
    public const string EnvironmentTesting = "Testing";

    #endregion

    #region Version and Metadata

    /// <summary>Logger component version</summary>
    public const string LoggerVersion = "1.0.0";

    /// <summary>Configuration schema version</summary>
    public const string ConfigurationSchemaVersion = "1.0";

    /// <summary>Log format version</summary>
    public const string LogFormatVersion = "1.0";

    #endregion

    #region Helper Methods

    /// <summary>
    /// UTILITY: Get default flush interval as TimeSpan
    /// </summary>
    public static TimeSpan GetDefaultFlushInterval() => TimeSpan.FromSeconds(DefaultFlushIntervalSeconds);

    /// <summary>
    /// UTILITY: Get file operation timeout as TimeSpan
    /// </summary>
    public static TimeSpan GetFileOperationTimeout() => TimeSpan.FromMilliseconds(FileOperationTimeoutMs);

    /// <summary>
    /// UTILITY: Get session timeout as TimeSpan
    /// </summary>
    public static TimeSpan GetDefaultSessionTimeout() => TimeSpan.FromMinutes(DefaultSessionTimeoutMinutes);

    /// <summary>
    /// VALIDATION: Check if file size is within valid range
    /// </summary>
    public static bool IsValidFileSize(int fileSizeMB) =>
        fileSizeMB >= MinFileSizeMB && fileSizeMB <= MaxFileSizeMB;

    /// <summary>
    /// VALIDATION: Check if file count is within valid range
    /// </summary>
    public static bool IsValidFileCount(int fileCount) =>
        fileCount >= MinFileCount && fileCount <= MaxFileCount;

    /// <summary>
    /// VALIDATION: Check if buffer size is within valid range
    /// </summary>
    public static bool IsValidBufferSize(int bufferSize) =>
        bufferSize >= MinBufferSize && bufferSize <= MaxBufferSize;

    /// <summary>
    /// UTILITY: Get environment-specific configuration defaults
    /// </summary>
    public static (LogLevel MinLevel, bool EnablePerformanceMonitoring, int BufferSize) GetEnvironmentDefaults(string environment) =>
        environment?.ToLowerInvariant() switch
        {
            "development" => (DevelopmentMinLogLevel, true, 100),
            "production" => (ProductionMinLogLevel, true, 5000),
            "staging" => (LogLevel.Information, true, 1000),
            "testing" => (LogLevel.Debug, false, 50),
            _ => (DefaultMinLogLevel, DefaultEnablePerformanceMonitoring, DefaultBufferSize)
        };

    #endregion
}
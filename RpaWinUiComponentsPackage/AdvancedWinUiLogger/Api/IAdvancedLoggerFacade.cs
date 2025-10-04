using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC INTERFACE: Single entry point for AdvancedWinUiLogger component
/// FACADE PATTERN: Unified API for all logging operations
/// ENTERPRISE: Professional API for advanced logging configuration
/// </summary>
public interface IAdvancedLoggerFacade
{
    #region Logger Configuration

    /// <summary>
    /// PUBLIC API: Configure and return typed logger for component usage
    /// STRATEGY: Apply internal configuration to external logging system
    /// </summary>
    ILogger<T> ConfigureLogger<T>(AdvancedLoggerOptions? options = null);

    /// <summary>
    /// PUBLIC API: Configure and return named logger for component usage
    /// STRATEGY: Apply internal configuration to external logging system
    /// </summary>
    ILogger ConfigureLogger(string categoryName, AdvancedLoggerOptions? options = null);

    /// <summary>
    /// PUBLIC API: Configure logger factory with advanced settings
    /// STRATEGY: Configure external factory with internal specifications
    /// </summary>
    ILoggerFactory ConfigureLoggerFactory(AdvancedLoggerOptions options);

    #endregion

    #region File Management

    /// <summary>
    /// PUBLIC API: Rotate current log file when size limit is reached
    /// FILE MANAGEMENT: Prevent log files from growing too large
    /// </summary>
    Task<RotationResult> RotateLogFileAsync(
        ILogger logger,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PUBLIC API: Clean up old log files based on age
    /// MAINTENANCE: Automatic cleanup of old logs
    /// </summary>
    Task<CleanupResult> CleanupOldLogFilesAsync(
        string logDirectory,
        int maxAgeDays,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PUBLIC API: Get detailed information about all log files
    /// MONITORING: Understand log file status and sizes
    /// </summary>
    Task<IReadOnlyList<LogFileInfo>> GetLogFilesInfoAsync(
        string logDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PUBLIC API: Get summary statistics for log directory
    /// OVERVIEW: Quick overview of logging status
    /// </summary>
    Task<LogDirectorySummary> GetLogDirectorySummaryAsync(
        string logDirectory,
        CancellationToken cancellationToken = default);

    #endregion

    #region Logging Operations

    /// <summary>
    /// PUBLIC API: Write log entry with specified level
    /// CORE LOGGING: Basic log writing functionality
    /// </summary>
    Task WriteLogEntryAsync(
        ILogger logger,
        LogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PUBLIC API: Write structured log entry with parameters
    /// STRUCTURED LOGGING: Modern logging approach with structured data
    /// </summary>
    Task WriteStructuredLogAsync(
        ILogger logger,
        LogLevel level,
        string messageTemplate,
        object?[] args,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PUBLIC API: Write multiple log entries in a batch
    /// PERFORMANCE: Efficient batch logging for high-throughput scenarios
    /// </summary>
    Task WriteBatchLogEntriesAsync(
        ILogger logger,
        IEnumerable<LogEntry> logEntries,
        CancellationToken cancellationToken = default);

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// PUBLIC API: Get performance metrics for logger monitoring
    /// MONITORING: Track logger performance and health
    /// </summary>
    Task<LoggerPerformanceMetrics> GetPerformanceMetricsAsync(
        ILogger logger,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PUBLIC API: Reset performance counters
    /// MAINTENANCE: Clear performance tracking data
    /// </summary>
    Task ResetPerformanceCountersAsync(
        ILogger logger,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PUBLIC API: Enable or disable performance monitoring
    /// CONFIGURATION: Control performance monitoring overhead
    /// </summary>
    Task<bool> SetPerformanceMonitoringAsync(
        ILogger logger,
        bool enabled,
        CancellationToken cancellationToken = default);

    #endregion

    #region Session Management

    /// <summary>
    /// PUBLIC API: Start a new logging session with specific configuration
    /// SESSION: Organized logging with session boundaries
    /// </summary>
    Task<LoggerSession> StartLoggingSessionAsync(
        AdvancedLoggerOptions options,
        string sessionName = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PUBLIC API: End current logging session
    /// SESSION: Clean session termination
    /// </summary>
    Task<bool> EndLoggingSessionAsync(
        LoggerSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PUBLIC API: Get all active logging sessions
    /// MONITORING: Track active logging sessions
    /// </summary>
    Task<IReadOnlyList<LoggerSession>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default);

    #endregion

    #region Log Analysis

    /// <summary>
    /// PUBLIC API: Search log entries by criteria
    /// ANALYSIS: Find specific log entries
    /// </summary>
    Task<IReadOnlyList<LogEntry>> SearchLogEntriesAsync(
        string logDirectory,
        LogSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PUBLIC API: Get log statistics for analysis
    /// ANALYTICS: Statistical analysis of log data
    /// </summary>
    Task<LogStatistics> GetLogStatisticsAsync(
        string logDirectory,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    #endregion
}

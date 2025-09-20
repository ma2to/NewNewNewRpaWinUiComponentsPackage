using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC FACADE: Main entry point for AdvancedWinUiLogger functionality
/// CLEAN ARCHITECTURE: Facade pattern hiding internal implementation details
/// ENTERPRISE: Professional API for advanced logging operations
/// </summary>
public sealed class AdvancedLoggerFacade
{
    /// <summary>
    /// CONSTRUCTOR: Default parameterless constructor for easy usage
    /// </summary>
    public AdvancedLoggerFacade()
    {
        // Simple facade - delegates directly to internal API classes
    }

    #region Logger Creation

    /// <summary>
    /// PUBLIC API: Create a simple file logger for basic logging needs
    /// FACTORY METHOD: Convenient logger creation
    /// </summary>
    public Result<ILogger> CreateFileLogger(string logDirectory, string baseFileName = "application")
    {
        return LoggerApi.CreateFileLogger(logDirectory, baseFileName);
    }

    /// <summary>
    /// PUBLIC API: Create high-performance logger for high-throughput scenarios
    /// PERFORMANCE: Optimized for enterprise applications
    /// </summary>
    public Result<ILogger> CreateHighPerformanceLogger(string logDirectory, string baseFileName = "application")
    {
        return LoggerApi.CreateHighPerformanceLogger(logDirectory, baseFileName);
    }

    /// <summary>
    /// PUBLIC API: Create development logger with detailed logging for debugging
    /// DEVELOPMENT: Optimized for development and debugging scenarios
    /// </summary>
    public Result<ILogger> CreateDevelopmentLogger(string logDirectory, string baseFileName = "dev")
    {
        return LoggerApi.CreateDevelopmentLogger(logDirectory, baseFileName);
    }

    /// <summary>
    /// PUBLIC API: Create logger with custom configuration
    /// ADVANCED: Full control over logger configuration
    /// </summary>
    public Result<ILogger> CreateCustomLogger(LoggerConfiguration configuration)
    {
        return LoggerApi.CreateCustomLogger(configuration);
    }

    #endregion

    #region Log File Management

    /// <summary>
    /// PUBLIC API: Rotate current log file when size limit is reached
    /// FILE MANAGEMENT: Prevent log files from growing too large
    /// </summary>
    public async Task<RotationResult> RotateLogFileAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.RotateLogFileAsync(logger, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Clean up old log files based on age
    /// MAINTENANCE: Automatic cleanup of old logs
    /// </summary>
    public async Task<CleanupResult> CleanupOldLogFilesAsync(
        string logDirectory,
        int maxAgeDays,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.CleanupOldLogFilesAsync(logDirectory, maxAgeDays, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Get detailed information about all log files
    /// MONITORING: Understand log file status and sizes
    /// </summary>
    public async Task<IReadOnlyList<LogFileInfo>> GetLogFilesInfoAsync(
        string logDirectory,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.GetLogFilesInfoAsync(logDirectory, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Get summary statistics for log directory
    /// OVERVIEW: Quick overview of logging status
    /// </summary>
    public async Task<LogDirectorySummary> GetLogDirectorySummaryAsync(
        string logDirectory,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.GetLogDirectorySummaryAsync(logDirectory, cancellationToken);
    }

    #endregion

    #region Log Writing Operations

    /// <summary>
    /// PUBLIC API: Write log entry with specified level
    /// CORE LOGGING: Basic log writing functionality
    /// </summary>
    public async Task WriteLogEntryAsync(
        ILogger logger,
        LogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        await LoggerApi.WriteLogEntryAsync(logger, level, message, exception, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Write structured log entry with parameters
    /// STRUCTURED LOGGING: Modern logging approach with structured data
    /// </summary>
    public async Task WriteStructuredLogAsync(
        ILogger logger,
        LogLevel level,
        string messageTemplate,
        object?[] args,
        CancellationToken cancellationToken = default)
    {
        await LoggerApi.WriteStructuredLogAsync(logger, level, messageTemplate, args, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Write multiple log entries in a batch
    /// PERFORMANCE: Efficient batch logging for high-throughput scenarios
    /// </summary>
    public async Task WriteBatchLogEntriesAsync(
        ILogger logger,
        IEnumerable<LogEntry> logEntries,
        CancellationToken cancellationToken = default)
    {
        await LoggerApi.WriteBatchLogEntriesAsync(logger, logEntries, cancellationToken);
    }

    #endregion

    #region Configuration Management

    /// <summary>
    /// PUBLIC API: Create minimal logger configuration for simple scenarios
    /// FACTORY METHOD: Pre-configured settings for common use cases
    /// </summary>
    public LoggerConfiguration CreateMinimalConfiguration(string logDirectory, string baseFileName = "application")
    {
        return LoggerConfiguration.CreateMinimal(logDirectory, baseFileName);
    }

    /// <summary>
    /// PUBLIC API: Create high-performance logger configuration
    /// FACTORY METHOD: Optimized for high-throughput scenarios
    /// </summary>
    public LoggerConfiguration CreateHighPerformanceConfiguration(string logDirectory, string baseFileName = "application")
    {
        return LoggerConfiguration.CreateHighPerformance(logDirectory, baseFileName);
    }

    /// <summary>
    /// PUBLIC API: Create development logger configuration
    /// FACTORY METHOD: Optimized for development and debugging
    /// </summary>
    public LoggerConfiguration CreateDevelopmentConfiguration(string logDirectory, string baseFileName = "dev")
    {
        return LoggerConfiguration.CreateDevelopment(logDirectory, baseFileName);
    }

    /// <summary>
    /// PUBLIC API: Validate logger configuration before use
    /// VALIDATION: Ensure configuration is valid before creating logger
    /// </summary>
    public Result<bool> ValidateConfiguration(LoggerConfiguration configuration)
    {
        return LoggerApi.ValidateConfiguration(configuration);
    }

    /// <summary>
    /// PUBLIC API: Update logger configuration at runtime
    /// RUNTIME CONFIG: Dynamic configuration updates
    /// </summary>
    public async Task<Result<bool>> UpdateLoggerConfigurationAsync(
        ILogger logger,
        LoggerConfiguration newConfiguration,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.UpdateLoggerConfigurationAsync(logger, newConfiguration, cancellationToken);
    }

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// PUBLIC API: Get performance metrics for logger monitoring
    /// MONITORING: Track logger performance and health
    /// </summary>
    public async Task<LoggerPerformanceMetrics> GetPerformanceMetricsAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.GetPerformanceMetricsAsync(logger, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Reset performance counters
    /// MAINTENANCE: Clear performance tracking data
    /// </summary>
    public async Task ResetPerformanceCountersAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await LoggerApi.ResetPerformanceCountersAsync(logger, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Enable or disable performance monitoring
    /// CONFIGURATION: Control performance monitoring overhead
    /// </summary>
    public async Task<Result<bool>> SetPerformanceMonitoringAsync(
        ILogger logger,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.SetPerformanceMonitoringAsync(logger, enabled, cancellationToken);
    }

    #endregion

    #region Session Management

    /// <summary>
    /// PUBLIC API: Start a new logging session with specific configuration
    /// SESSION: Organized logging with session boundaries
    /// </summary>
    public async Task<Result<LoggerSession>> StartLoggingSessionAsync(
        LoggerConfiguration configuration,
        string sessionName = "",
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.StartLoggingSessionAsync(configuration, sessionName, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: End current logging session
    /// SESSION: Clean session termination
    /// </summary>
    public async Task<Result<bool>> EndLoggingSessionAsync(
        LoggerSession session,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.EndLoggingSessionAsync(session, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Get all active logging sessions
    /// MONITORING: Track active logging sessions
    /// </summary>
    public async Task<IReadOnlyList<LoggerSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await LoggerApi.GetActiveSessionsAsync(cancellationToken);
    }

    #endregion

    #region Log Analysis and Search

    /// <summary>
    /// PUBLIC API: Search log entries by criteria
    /// ANALYSIS: Find specific log entries
    /// </summary>
    public async Task<IReadOnlyList<LogEntry>> SearchLogEntriesAsync(
        string logDirectory,
        LogSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.SearchLogEntriesAsync(logDirectory, searchCriteria, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Get log statistics for analysis
    /// ANALYTICS: Statistical analysis of log data
    /// </summary>
    public async Task<LogStatistics> GetLogStatisticsAsync(
        string logDirectory,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.GetLogStatisticsAsync(logDirectory, fromDate, toDate, cancellationToken);
    }

    #endregion
}
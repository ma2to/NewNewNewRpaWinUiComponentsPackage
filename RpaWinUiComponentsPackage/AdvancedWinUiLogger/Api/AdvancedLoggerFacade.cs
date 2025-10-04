using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.FileManagement.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Performance.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Session.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC FACADE: Single entry point for AdvancedWinUiLogger component
/// FACADE PATTERN: Unified API for all logging operations
/// CLEAN ARCHITECTURE: Public API delegates to internal feature services
/// ENTERPRISE: Professional API for advanced logging configuration
/// </summary>
public sealed class AdvancedLoggerFacade : IAdvancedLoggerFacade
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AdvancedLoggerOptions _options;
    private readonly ILoggerFactory? _externalLoggerFactory;

    /// <summary>
    /// CONSTRUCTOR: Single entry point accepting external logging system via DI
    /// EXTERNAL INTEGRATION: Accepts ILoggerFactory from consumer (Serilog, NLog, etc.)
    /// NULL SAFETY: Supports null external factory for standalone operation
    /// </summary>
    public AdvancedLoggerFacade(
        IServiceProvider serviceProvider,
        AdvancedLoggerOptions options,
        ILoggerFactory? externalLoggerFactory = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _externalLoggerFactory = externalLoggerFactory;
    }

    #region Logger Configuration

    public ILogger<T> ConfigureLogger<T>(AdvancedLoggerOptions? options = null)
    {
        var config = options ?? _options;
        var factory = _externalLoggerFactory ?? _serviceProvider.GetRequiredService<ILoggerFactory>();
        return factory.CreateLogger<T>();
    }

    public ILogger ConfigureLogger(string categoryName, AdvancedLoggerOptions? options = null)
    {
        var config = options ?? _options;
        var factory = _externalLoggerFactory ?? _serviceProvider.GetRequiredService<ILoggerFactory>();
        return factory.CreateLogger(categoryName);
    }

    public ILoggerFactory ConfigureLoggerFactory(AdvancedLoggerOptions options)
    {
        return _externalLoggerFactory ?? _serviceProvider.GetRequiredService<ILoggerFactory>();
    }

    #endregion

    #region File Management

    public async Task<RotationResult> RotateLogFileAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<IFileManagementService>();
        return await service.RotateLogFileAsync(logger, cancellationToken);
    }

    public async Task<CleanupResult> CleanupOldLogFilesAsync(
        string logDirectory,
        int maxAgeDays,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<IFileManagementService>();
        return await service.CleanupOldLogFilesAsync(logDirectory, maxAgeDays, cancellationToken);
    }

    public async Task<IReadOnlyList<LogFileInfo>> GetLogFilesInfoAsync(
        string logDirectory,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<IFileManagementService>();
        return await service.GetLogFilesInfoAsync(logDirectory, cancellationToken);
    }

    public async Task<LogDirectorySummary> GetLogDirectorySummaryAsync(
        string logDirectory,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<IFileManagementService>();
        return await service.GetLogDirectorySummaryAsync(logDirectory, cancellationToken);
    }

    #endregion

    #region Logging Operations

    public async Task WriteLogEntryAsync(
        ILogger logger,
        LogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<ILoggingService>();
        await service.WriteLogEntryAsync(logger, level, message, exception, cancellationToken);
    }

    public async Task WriteStructuredLogAsync(
        ILogger logger,
        LogLevel level,
        string messageTemplate,
        object?[] args,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<ILoggingService>();
        await service.WriteStructuredLogAsync(logger, level, messageTemplate, args, cancellationToken);
    }

    public async Task WriteBatchLogEntriesAsync(
        ILogger logger,
        IEnumerable<LogEntry> logEntries,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<ILoggingService>();
        await service.WriteBatchLogEntriesAsync(logger, logEntries, cancellationToken);
    }

    #endregion

    #region Performance Monitoring

    public async Task<LoggerPerformanceMetrics> GetPerformanceMetricsAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<IPerformanceService>();
        return await service.GetPerformanceMetricsAsync(logger, cancellationToken);
    }

    public async Task ResetPerformanceCountersAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<IPerformanceService>();
        await service.ResetPerformanceCountersAsync(logger, cancellationToken);
    }

    public async Task<bool> SetPerformanceMonitoringAsync(
        ILogger logger,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<IPerformanceService>();
        return await service.SetPerformanceMonitoringAsync(logger, enabled, cancellationToken);
    }

    #endregion

    #region Session Management

    public async Task<LoggerSession> StartLoggingSessionAsync(
        AdvancedLoggerOptions options,
        string sessionName = "",
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<ISessionService>();
        return await service.StartLoggingSessionAsync(options, sessionName, cancellationToken);
    }

    public async Task<bool> EndLoggingSessionAsync(
        LoggerSession session,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<ISessionService>();
        return await service.EndLoggingSessionAsync(session, cancellationToken);
    }

    public async Task<IReadOnlyList<LoggerSession>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<ISessionService>();
        return await service.GetActiveSessionsAsync(cancellationToken);
    }

    #endregion

    #region Log Analysis

    public async Task<IReadOnlyList<LogEntry>> SearchLogEntriesAsync(
        string logDirectory,
        LogSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<ILoggingService>();
        return await service.SearchLogEntriesAsync(logDirectory, searchCriteria, cancellationToken);
    }

    public async Task<LogStatistics> GetLogStatisticsAsync(
        string logDirectory,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var service = _serviceProvider.GetRequiredService<ILoggingService>();
        return await service.GetLogStatisticsAsync(logDirectory, fromDate, toDate, cancellationToken);
    }

    #endregion
}

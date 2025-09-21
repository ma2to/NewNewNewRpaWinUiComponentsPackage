using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Services;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Entities;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// ENTERPRISE FACADE: Single entry point for configuring external logging systems
/// PATTERN: Configuration Builder + Factory for external ILoggerFactory integration
/// STRATEGY: Accepts external logging system and returns configured ILogger instances
/// ENTERPRISE: Professional API for advanced logging configuration in enterprise applications
/// </summary>
public sealed class AdvancedLoggerFacade
{
    private readonly ILoggerFactory? _externalLoggerFactory;
    private readonly IServiceCollection _services;
    private ILoggerFactory? _configuredLoggerFactory;

    /// <summary>
    /// CONSTRUCTOR: Single entry point accepting external logging system
    /// EXTERNAL INTEGRATION: Accepts ILoggerFactory from consumer (Serilog, NLog, etc.)
    /// NULL SAFETY: Supports null external factory for standalone operation
    /// </summary>
    public AdvancedLoggerFacade(ILoggerFactory? externalLoggerFactory = null)
    {
        _externalLoggerFactory = externalLoggerFactory;
        _services = new ServiceCollection();
        ConfigureInternalServices();
    }

    private void ConfigureInternalServices()
    {
        // Register Infrastructure services
        _services.AddSingleton<Core.Interfaces.ILoggerRepository, Infrastructure.Services.FileLoggerRepository>();

        // Register Application services
        _services.AddSingleton<ILoggerCreationService, LoggerCreationService>();
        _services.AddSingleton<ILoggerConfigurationService, LoggerConfigurationService>();
        _services.AddSingleton<ILoggerPerformanceService, LoggerPerformanceService>();
        _services.AddSingleton<ILoggerComponentInfoService, LoggerComponentInfoService>();

        // Register new Application services (replacing Use Cases)
        _services.AddSingleton<ILoggingService, LoggingService>();
        _services.AddSingleton<IFileManagementService, FileManagementService>();

        if (_externalLoggerFactory != null)
        {
            _services.AddSingleton(_externalLoggerFactory);
        }
        else
        {
            _services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
        }
    }

    #region Enterprise Logger Configuration

    /// <summary>
    /// ENTERPRISE API: Configure and return typed logger for component usage
    /// STRATEGY: Apply internal configuration to external logging system
    /// GENERIC: Strongly typed logger for specific component
    /// </summary>
    public ILogger<T> ConfigureLogger<T>(LoggerConfiguration? configuration = null)
    {
        var config = configuration ?? LoggerConfiguration.CreateMinimal("./logs", typeof(T).Name);
        EnsureConfiguredLoggerFactory(config);

        return _configuredLoggerFactory!.CreateLogger<T>();
    }

    /// <summary>
    /// ENTERPRISE API: Configure and return named logger for component usage
    /// STRATEGY: Apply internal configuration to external logging system
    /// NAMED: Custom category name for logger
    /// </summary>
    public ILogger ConfigureLogger(string categoryName, LoggerConfiguration? configuration = null)
    {
        var config = configuration ?? LoggerConfiguration.CreateMinimal("./logs", categoryName);
        EnsureConfiguredLoggerFactory(config);

        return _configuredLoggerFactory!.CreateLogger(categoryName);
    }

    /// <summary>
    /// ENTERPRISE API: Configure logger factory with advanced settings
    /// STRATEGY: Configure external factory with internal specifications
    /// ADVANCED: Full control over logging behavior and performance
    /// </summary>
    public ILoggerFactory ConfigureLoggerFactory(LoggerConfiguration configuration)
    {
        EnsureConfiguredLoggerFactory(configuration);
        return _configuredLoggerFactory!;
    }

    private void EnsureConfiguredLoggerFactory(LoggerConfiguration configuration)
    {
        if (_configuredLoggerFactory != null) return;

        var serviceProvider = _services.BuildServiceProvider();

        if (_externalLoggerFactory != null)
        {
            _configuredLoggerFactory = CreateConfiguredExternalFactory(configuration);
        }
        else
        {
            _configuredLoggerFactory = CreateInternalLoggerFactory(configuration, serviceProvider);
        }
    }

    private ILoggerFactory CreateConfiguredExternalFactory(LoggerConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_externalLoggerFactory!);

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(configuration.MinimumLevel);

            switch (configuration.LoggingMode)
            {
                case LoggingMode.Single:
                    builder.AddFilter("*", configuration.MinimumLevel);
                    break;
                case LoggingMode.Bulk:
                    builder.AddFilter("*", configuration.MinimumLevel);
                    break;
                case LoggingMode.AsyncBatch:
                    builder.AddFilter("*", configuration.MinimumLevel);
                    break;
            }
        });

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ILoggerFactory>();
    }

    private ILoggerFactory CreateInternalLoggerFactory(LoggerConfiguration configuration, IServiceProvider serviceProvider)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(configuration.MinimumLevel);
            builder.AddConsole(options =>
            {
                options.IncludeScopes = configuration.EnableStructuredLogging;
            });

            if (configuration.MinimumLevel <= LogLevel.Debug)
            {
                builder.AddDebug();
            }
        });

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ILoggerFactory>();
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
        var serviceProvider = _services.BuildServiceProvider();
        var fileManagementService = serviceProvider.GetRequiredService<IFileManagementService>();
        return await fileManagementService.RotateLogFileAsync(logger, cancellationToken);
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
        var serviceProvider = _services.BuildServiceProvider();
        var fileManagementService = serviceProvider.GetRequiredService<IFileManagementService>();
        return await fileManagementService.CleanupOldLogFilesAsync(logDirectory, maxAgeDays, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Get detailed information about all log files
    /// MONITORING: Understand log file status and sizes
    /// </summary>
    public async Task<IReadOnlyList<LogFileInfo>> GetLogFilesInfoAsync(
        string logDirectory,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var fileManagementService = serviceProvider.GetRequiredService<IFileManagementService>();
        return await fileManagementService.GetLogFilesInfoAsync(logDirectory, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Get summary statistics for log directory
    /// OVERVIEW: Quick overview of logging status
    /// </summary>
    public async Task<LogDirectorySummary> GetLogDirectorySummaryAsync(
        string logDirectory,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var fileManagementService = serviceProvider.GetRequiredService<IFileManagementService>();
        return await fileManagementService.GetLogDirectorySummaryAsync(logDirectory, cancellationToken);
    }

    #endregion

    #region Log Writing Operations

    /// <summary>
    /// ENTERPRISE API: Write log entry with specified level
    /// CORE LOGGING: Basic log writing functionality using internal services
    /// </summary>
    public async Task WriteLogEntryAsync(
        ILogger logger,
        LogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var loggingService = serviceProvider.GetRequiredService<ILoggingService>();
        await loggingService.WriteLogEntryAsync(logger, level, message, exception, cancellationToken);
    }

    /// <summary>
    /// ENTERPRISE API: Write structured log entry with parameters
    /// STRUCTURED LOGGING: Modern logging approach with structured data using internal services
    /// </summary>
    public async Task WriteStructuredLogAsync(
        ILogger logger,
        LogLevel level,
        string messageTemplate,
        object?[] args,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var loggingService = serviceProvider.GetRequiredService<ILoggingService>();
        await loggingService.WriteStructuredLogAsync(logger, level, messageTemplate, args, cancellationToken);
    }

    /// <summary>
    /// ENTERPRISE API: Write multiple log entries in a batch
    /// PERFORMANCE: Efficient batch logging for high-throughput scenarios using internal services
    /// </summary>
    public async Task WriteBatchLogEntriesAsync(
        ILogger logger,
        IEnumerable<LogEntry> logEntries,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var loggingService = serviceProvider.GetRequiredService<ILoggingService>();
        await loggingService.WriteBatchLogEntriesAsync(logger, logEntries, cancellationToken);
    }

    #endregion

    #region Configuration Factory Methods

    /// <summary>
    /// ENTERPRISE API: Create minimal logger configuration for simple scenarios
    /// FACTORY METHOD: Pre-configured settings for common use cases
    /// </summary>
    public static LoggerConfiguration CreateMinimalConfiguration(string logDirectory, string baseFileName = "application")
    {
        return LoggerConfiguration.CreateMinimal(logDirectory, baseFileName);
    }

    /// <summary>
    /// ENTERPRISE API: Create high-performance logger configuration
    /// FACTORY METHOD: Optimized for high-throughput scenarios with AsyncBatch mode
    /// </summary>
    public static LoggerConfiguration CreateHighPerformanceConfiguration(string logDirectory, string baseFileName = "application")
    {
        return LoggerConfiguration.CreateHighPerformance(logDirectory, baseFileName);
    }

    /// <summary>
    /// ENTERPRISE API: Create development logger configuration
    /// FACTORY METHOD: Optimized for development and debugging with detailed logging
    /// </summary>
    public static LoggerConfiguration CreateDevelopmentConfiguration(string logDirectory, string baseFileName = "dev")
    {
        return LoggerConfiguration.CreateDevelopment(logDirectory, baseFileName);
    }

    /// <summary>
    /// ENTERPRISE API: Validate logger configuration before use
    /// VALIDATION: Ensure configuration is valid before creating logger
    /// </summary>
    public bool ValidateConfiguration(LoggerConfiguration configuration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(configuration.LogDirectory))
                return false;

            if (string.IsNullOrWhiteSpace(configuration.BaseFileName))
                return false;

            if (configuration.MaxFileSize <= 0)
                return false;

            if (configuration.MaxFiles <= 0)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ENTERPRISE API: Create configuration builder for fluent configuration
    /// BUILDER PATTERN: Fluent interface for complex configuration scenarios
    /// </summary>
    public ConfigurationBuilder CreateConfigurationBuilder()
    {
        return new ConfigurationBuilder();
    }

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// ENTERPRISE API: Get performance metrics for logger monitoring
    /// MONITORING: Track logger performance and health using internal services
    /// </summary>
    public async Task<LoggerPerformanceMetrics> GetPerformanceMetricsAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var performanceService = serviceProvider.GetRequiredService<ILoggerPerformanceService>();
        return await performanceService.GetPerformanceMetricsAsync(logger, cancellationToken);
    }

    /// <summary>
    /// ENTERPRISE API: Reset performance counters
    /// MAINTENANCE: Clear performance tracking data using internal services
    /// </summary>
    public async Task ResetPerformanceCountersAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var performanceService = serviceProvider.GetRequiredService<ILoggerPerformanceService>();
        await performanceService.ResetPerformanceCountersAsync(logger, cancellationToken);
    }

    /// <summary>
    /// ENTERPRISE API: Enable or disable performance monitoring
    /// CONFIGURATION: Control performance monitoring overhead using internal services
    /// </summary>
    public async Task<Result<bool>> SetPerformanceMonitoringAsync(
        ILogger logger,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var performanceService = serviceProvider.GetRequiredService<ILoggerPerformanceService>();
        return await performanceService.SetPerformanceMonitoringAsync(logger, enabled, cancellationToken);
    }

    #endregion

    #region Session Management

    /// <summary>
    /// ENTERPRISE API: Start a new logging session with specific configuration
    /// SESSION: Organized logging with session boundaries using internal services
    /// </summary>
    public async Task<Result<LoggerSession>> StartLoggingSessionAsync(
        LoggerConfiguration configuration,
        string sessionName = "",
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var loggingService = serviceProvider.GetRequiredService<ILoggingService>();
        var internalConfig = configuration.ToInternal();
        return await loggingService.StartLoggingSessionAsync(internalConfig, sessionName, cancellationToken);
    }

    /// <summary>
    /// ENTERPRISE API: End current logging session
    /// SESSION: Clean session termination using internal services
    /// </summary>
    public async Task<Result<bool>> EndLoggingSessionAsync(
        LoggerSession session,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var loggingService = serviceProvider.GetRequiredService<ILoggingService>();
        return await loggingService.EndLoggingSessionAsync(session, cancellationToken);
    }

    /// <summary>
    /// ENTERPRISE API: Get all active logging sessions
    /// MONITORING: Track active logging sessions using internal services
    /// </summary>
    public async Task<IReadOnlyList<LoggerSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var loggingService = serviceProvider.GetRequiredService<ILoggingService>();
        return await loggingService.GetActiveSessionsAsync(cancellationToken);
    }

    #endregion

    #region Log Analysis and Search

    /// <summary>
    /// ENTERPRISE API: Search log entries by criteria
    /// ANALYSIS: Find specific log entries using internal services
    /// </summary>
    public async Task<IReadOnlyList<LogEntry>> SearchLogEntriesAsync(
        string logDirectory,
        LogSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var loggingService = serviceProvider.GetRequiredService<ILoggingService>();
        var internalCriteria = searchCriteria.ToInternal();
        return await loggingService.SearchLogEntriesAsync(logDirectory, internalCriteria, cancellationToken);
    }

    /// <summary>
    /// ENTERPRISE API: Get log statistics for analysis
    /// ANALYTICS: Statistical analysis of log data using internal services
    /// </summary>
    public async Task<LogStatistics> GetLogStatisticsAsync(
        string logDirectory,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = _services.BuildServiceProvider();
        var loggingService = serviceProvider.GetRequiredService<ILoggingService>();
        return await loggingService.GetLogStatisticsAsync(logDirectory, fromDate, toDate, cancellationToken);
    }

    #endregion
}
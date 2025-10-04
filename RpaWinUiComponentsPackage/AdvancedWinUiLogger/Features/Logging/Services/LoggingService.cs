using System;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Logging.Services;

/// <summary>
/// INTERNAL SERVICE: Core logging operations implementation
/// CLEAN ARCHITECTURE: Application layer service for logging business logic
/// ENTERPRISE: Professional logging service with dual persistence
/// </summary>
internal sealed class LoggingService : ILoggingService
{
    private readonly ILoggerRepository _repository;
    private readonly AdvancedLoggerOptions _options;

    public LoggingService(ILoggerRepository repository, AdvancedLoggerOptions options)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Write single log entry with dual persistence
    /// </summary>
    public async Task WriteLogEntryAsync(
        ILogger logger,
        LogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check minimum level
            if (!_options.ShouldLog(level))
                return;

            // Create log entry
            var entry = LogEntry.Create(level, message, exception);

            // Write to external logger (Serilog, NLog, etc.)
            logger.Log(level, exception, message);

            // Also persist to our internal repository for file operations
            await _repository.WriteLogEntryAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            // Fallback to basic logging if repository fails
            logger.LogError(ex, "Failed to write log entry to repository: {Message}", message);
        }
    }

    /// <summary>
    /// Write structured log with template support
    /// </summary>
    public async Task WriteStructuredLogAsync(
        ILogger logger,
        LogLevel level,
        string messageTemplate,
        object?[] args,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check minimum level
            if (!_options.ShouldLog(level))
                return;

            // Format message with template
            var formattedMessage = string.Format(messageTemplate, args);

            // Create structured log entry
            var entry = LogEntry.Create(level, formattedMessage);

            // Write to external logger with structured data
            logger.Log(level, messageTemplate, args);

            // Persist to repository
            await _repository.WriteLogEntryAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write structured log entry");
        }
    }

    /// <summary>
    /// Write multiple log entries in optimized batch
    /// </summary>
    public async Task WriteBatchLogEntriesAsync(
        ILogger logger,
        IEnumerable<LogEntry> logEntries,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = logEntries.Where(e => _options.ShouldLog(e.Level)).ToList();
            if (!entries.Any()) return;

            // Write to external logger
            foreach (var entry in entries)
            {
                logger.Log(entry.Level, entry.Exception, entry.Message);
            }

            // Batch write to repository for better performance
            await _repository.WriteBatchAsync(entries, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write batch log entries");
        }
    }

    /// <summary>
    /// Search log entries by criteria
    /// </summary>
    public async Task<IReadOnlyList<LogEntry>> SearchLogEntriesAsync(
        string logDirectory,
        LogSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Implementation would search through log files
            await Task.CompletedTask;
            return Array.Empty<LogEntry>();
        }
        catch
        {
            return Array.Empty<LogEntry>();
        }
    }

    /// <summary>
    /// Get log statistics for analysis
    /// </summary>
    public async Task<LogStatistics> GetLogStatisticsAsync(
        string logDirectory,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Implementation would analyze log files
            await Task.CompletedTask;
            return LogStatistics.Create(0, new Dictionary<LogLevel, int>(), null, null);
        }
        catch
        {
            return LogStatistics.Create(0, new Dictionary<LogLevel, int>(), null, null);
        }
    }
}

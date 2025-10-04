using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Logging.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Logging service contract
/// CLEAN ARCHITECTURE: Application layer interface for logging operations
/// </summary>
internal interface ILoggingService
{
    Task WriteLogEntryAsync(ILogger logger, LogLevel level, string message, Exception? exception = null, CancellationToken cancellationToken = default);
    Task WriteStructuredLogAsync(ILogger logger, LogLevel level, string messageTemplate, object?[] args, CancellationToken cancellationToken = default);
    Task WriteBatchLogEntriesAsync(ILogger logger, IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogEntry>> SearchLogEntriesAsync(string logDirectory, LogSearchCriteria searchCriteria, CancellationToken cancellationToken = default);
    Task<LogStatistics> GetLogStatisticsAsync(string logDirectory, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}

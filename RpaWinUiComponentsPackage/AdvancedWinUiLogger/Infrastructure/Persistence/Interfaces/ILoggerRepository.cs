using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Infrastructure.Persistence.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Repository for log persistence
/// CLEAN ARCHITECTURE: Infrastructure layer interface for data operations
/// </summary>
internal interface ILoggerRepository
{
    Task<bool> WriteLogEntryAsync(LogEntry entry, CancellationToken cancellationToken = default);
    Task<int> WriteBatchAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default);
    Task<string> InitializeLogFileAsync(string filePath, CancellationToken cancellationToken = default);
}

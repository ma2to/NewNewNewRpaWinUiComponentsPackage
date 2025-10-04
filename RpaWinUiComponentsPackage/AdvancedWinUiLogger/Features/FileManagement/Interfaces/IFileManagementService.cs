using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.FileManagement.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: File management service contract
/// CLEAN ARCHITECTURE: Application layer interface for file operations
/// </summary>
internal interface IFileManagementService
{
    Task<RotationResult> RotateLogFileAsync(ILogger logger, CancellationToken cancellationToken = default);
    Task<CleanupResult> CleanupOldLogFilesAsync(string logDirectory, int maxAgeDays, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogFileInfo>> GetLogFilesInfoAsync(string logDirectory, CancellationToken cancellationToken = default);
    Task<LogDirectorySummary> GetLogDirectorySummaryAsync(string logDirectory, CancellationToken cancellationToken = default);
}

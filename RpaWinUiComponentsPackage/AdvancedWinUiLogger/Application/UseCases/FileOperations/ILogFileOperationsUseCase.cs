using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Functional;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.UseCases.FileOperations;

/// <summary>
/// INTERNAL INTERFACE: Log file operations use case
/// CLEAN ARCHITECTURE: Application layer use case for file management operations
/// </summary>
internal interface ILogFileOperationsUseCase
{
    Task<Result<bool>> RotateLogFileAsync(string currentFilePath, CancellationToken cancellationToken = default);
    Task<Result<int>> CleanupOldLogsAsync(string logDirectory, int maxAgeDays, int maxFileCount, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LogFileInfo>>> GetLogFilesAsync(string logDirectory, CancellationToken cancellationToken = default);
    Task<RotationResult> RotateLogFileAsync(Microsoft.Extensions.Logging.ILogger logger, CancellationToken cancellationToken = default);
    Task<CleanupResult> CleanupOldLogFilesAsync(string logDirectory, int maxAgeDays, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogFileInfo>> GetLogFilesInfoAsync(string logDirectory, CancellationToken cancellationToken = default);
    Task<LogDirectorySummary> GetLogDirectorySummaryAsync(string logDirectory, CancellationToken cancellationToken = default);
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Interfaces;

/// <summary>
/// APPLICATION INTERFACE: File management service contract
/// CLEAN ARCHITECTURE: Application layer interface for file operations
/// </summary>
internal interface IFileManagementService
{
    Task<Result<bool>> RotateLogFileAsync(string currentFilePath, CancellationToken cancellationToken = default);
    Task<Result<int>> CleanupOldLogsAsync(string logDirectory, int maxAgeDays, int maxFileCount, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LogFileInfo>>> GetLogFilesAsync(string logDirectory, CancellationToken cancellationToken = default);
    Task<RotationResult> RotateLogFileAsync(ILogger logger, CancellationToken cancellationToken = default);
    Task<CleanupResult> CleanupOldLogFilesAsync(string logDirectory, int maxAgeDays, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LogFileInfo>> GetLogFilesInfoAsync(string logDirectory, CancellationToken cancellationToken = default);
    Task<LogDirectorySummary> GetLogDirectorySummaryAsync(string logDirectory, CancellationToken cancellationToken = default);
    Task<Result<bool>> ShouldRotateLogFileAsync(string filePath, long maxSizeBytes, CancellationToken cancellationToken = default);
    Task<Result<RotationResult>> PerformAdvancedRotationAsync(string currentFilePath, LoggerConfiguration configuration, CancellationToken cancellationToken = default);
}
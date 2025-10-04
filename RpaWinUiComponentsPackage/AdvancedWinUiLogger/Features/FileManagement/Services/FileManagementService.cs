using System;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.FileManagement.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.FileManagement.Services;

/// <summary>
/// INTERNAL SERVICE: File management operations implementation
/// CLEAN ARCHITECTURE: Application layer service for file business logic
/// </summary>
internal sealed class FileManagementService : IFileManagementService
{
    private readonly ILoggerRepository _repository;
    private readonly AdvancedLoggerOptions _options;

    public FileManagementService(ILoggerRepository repository, AdvancedLoggerOptions options)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<RotationResult> RotateLogFileAsync(ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentFilePath = _options.GetCurrentLogFilePath();

            if (!File.Exists(currentFilePath))
                return RotationResult.Success(currentFilePath, 0, rotationType: RotationType.NotNeeded);

            var fileInfo = new FileInfo(currentFilePath);

            // Check if rotation is needed
            if (_options.MaxFileSizeBytes > 0 && fileInfo.Length < _options.MaxFileSizeBytes)
                return RotationResult.Success(currentFilePath, fileInfo.Length, rotationType: RotationType.NotNeeded);

            // Perform rotation
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var newFilePath = Path.Combine(
                Path.GetDirectoryName(currentFilePath) ?? _options.LogDirectory,
                $"{Path.GetFileNameWithoutExtension(currentFilePath)}_{timestamp}{Path.GetExtension(currentFilePath)}");

            File.Move(currentFilePath, newFilePath);

            logger.LogInformation("Log file rotated: {OldPath} -> {NewPath}", currentFilePath, newFilePath);

            return RotationResult.Success(newFilePath, fileInfo.Length, currentFilePath, rotationType: RotationType.SizeBased);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rotate log file");
            return RotationResult.Failure($"Rotation failed: {ex.Message}");
        }
    }

    public async Task<CleanupResult> CleanupOldLogFilesAsync(
        string logDirectory,
        int maxAgeDays,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(logDirectory))
                return CleanupResult.Success(0, 0, TimeSpan.Zero, Array.Empty<string>());

            var startTime = DateTime.UtcNow;
            var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeDays);
            var files = Directory.GetFiles(logDirectory, "*.log", SearchOption.AllDirectories);

            var deletedFiles = new List<string>();
            long bytesFreed = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoffDate)
                {
                    bytesFreed += fileInfo.Length;
                    File.Delete(file);
                    deletedFiles.Add(file);
                }
            }

            var duration = DateTime.UtcNow - startTime;

            await Task.CompletedTask;
            return CleanupResult.Success(deletedFiles.Count, bytesFreed, duration, deletedFiles);
        }
        catch (Exception ex)
        {
            return CleanupResult.Failure($"Cleanup failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<LogFileInfo>> GetLogFilesInfoAsync(
        string logDirectory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(logDirectory))
                return Array.Empty<LogFileInfo>();

            var files = Directory.GetFiles(logDirectory, "*.log", SearchOption.AllDirectories);
            var fileInfos = files.Select(LogFileInfo.FromPath).ToList();

            await Task.CompletedTask;
            return fileInfos;
        }
        catch
        {
            return Array.Empty<LogFileInfo>();
        }
    }

    public async Task<LogDirectorySummary> GetLogDirectorySummaryAsync(
        string logDirectory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await GetLogFilesInfoAsync(logDirectory, cancellationToken);
            return LogDirectorySummary.Create(logDirectory, files);
        }
        catch
        {
            return LogDirectorySummary.Create(logDirectory, Array.Empty<LogFileInfo>());
        }
    }
}

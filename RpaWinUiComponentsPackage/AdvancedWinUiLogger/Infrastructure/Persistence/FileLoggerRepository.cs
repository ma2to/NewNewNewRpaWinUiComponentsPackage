using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Infrastructure.Persistence;

/// <summary>
/// INTERNAL IMPLEMENTATION: File-based logger repository
/// INFRASTRUCTURE: Direct file system operations for log storage
/// ENTERPRISE: High-performance file operations with error handling
/// </summary>
internal sealed class FileLoggerRepository : ILoggerRepository, IDisposable
{
    private readonly AdvancedLoggerOptions _options;
    private readonly object _lock = new();
    private bool _disposed;

    public FileLoggerRepository(AdvancedLoggerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<bool> WriteLogEntryAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entry == null)
                return false;

            var filePath = _options.GetCurrentLogFilePath();
            var logLine = FormatLogEntry(entry);

            lock (_lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.AppendAllText(filePath, logLine + Environment.NewLine, Encoding.UTF8);
            }

            await Task.CompletedTask;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> WriteBatchAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entries == null)
                return 0;

            var entryList = entries.ToList();
            if (!entryList.Any())
                return 0;

            var filePath = _options.GetCurrentLogFilePath();
            var logLines = entryList.Select(FormatLogEntry);
            var content = string.Join(Environment.NewLine, logLines) + Environment.NewLine;

            lock (_lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.AppendAllText(filePath, content, Encoding.UTF8);
            }

            await Task.CompletedTask;
            return entryList.Count;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<string> InitializeLogFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, "", Encoding.UTF8, cancellationToken);
            }

            return filePath;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string FormatLogEntry(LogEntry entry)
    {
        return $"{entry.Timestamp.ToString(_options.DateFormat)} [{entry.Level}] {entry.Message}{(entry.Exception != null ? $" | Exception: {entry.Exception}" : "")}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}

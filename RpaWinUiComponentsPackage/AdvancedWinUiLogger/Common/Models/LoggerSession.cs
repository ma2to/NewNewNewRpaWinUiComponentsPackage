using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC MODEL: Logger session entity
/// ENTERPRISE: Session management for organized logging
/// </summary>
public sealed class LoggerSession
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString();
    public string SessionName { get; init; } = string.Empty;
    public DateTime StartedUtc { get; init; } = DateTime.UtcNow;
    public DateTime? EndedUtc { get; private set; }
    public bool IsActive => EndedUtc == null;
    public TimeSpan Duration => (EndedUtc ?? DateTime.UtcNow) - StartedUtc;
    public string LogDirectory { get; init; } = string.Empty;
    public string CurrentLogFile { get; init; } = string.Empty;
    public long TotalEntriesWritten { get; private set; }
    public long TotalBytesWritten { get; private set; }

    public static LoggerSession Create(string sessionName, string logDirectory, string currentLogFile)
    {
        return new LoggerSession
        {
            SessionName = sessionName,
            LogDirectory = logDirectory,
            CurrentLogFile = currentLogFile
        };
    }

    public void End()
    {
        EndedUtc = DateTime.UtcNow;
    }

    public void IncrementEntries(int count)
    {
        TotalEntriesWritten += count;
    }

    public void IncrementBytes(long bytes)
    {
        TotalBytesWritten += bytes;
    }
}

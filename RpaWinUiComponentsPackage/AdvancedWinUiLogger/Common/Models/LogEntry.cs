using System;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC MODEL: Immutable log entry value object
/// ENTERPRISE: Complete log entry with all metadata
/// </summary>
public sealed record LogEntry
{
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Exception { get; init; }
    public string? Category { get; init; }
    public int EventId { get; init; }
    public string? StackTrace { get; init; }

    public LogEntry(DateTime timestamp, LogLevel level, string message, string? exception = null)
    {
        Timestamp = timestamp;
        Level = level;
        Message = message;
        Exception = exception;
    }

    public static LogEntry Create(LogLevel level, string message, Exception? exception = null)
    {
        return new LogEntry(DateTime.UtcNow, level, message, exception?.ToString());
    }

    public override string ToString()
    {
        return $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{(Exception != null ? $" | Exception: {Exception}" : "")}";
    }
}

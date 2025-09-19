using System;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

/// <summary>
/// DOMAIN VALUE OBJECT: Immutable log entry representation
/// ENTERPRISE: Rich domain behavior with functional transformations
/// PERFORMANCE: Optimized for high-throughput logging scenarios
/// </summary>
internal sealed record LogEntry
{
    /// <summary>UTC timestamp when log entry was created</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>Log level indicating severity</summary>
    public LogLevel Level { get; init; }

    /// <summary>Primary log message</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Exception details if present</summary>
    public string? Exception { get; init; }

    /// <summary>Log category for filtering and organization</summary>
    public string? Category { get; init; }

    /// <summary>Source component that generated the log</summary>
    public string? Source { get; init; }

    /// <summary>Event identifier for correlation</summary>
    public EventId EventId { get; init; }

    /// <summary>Thread ID for concurrency tracking</summary>
    public int ThreadId { get; init; }

    /// <summary>
    /// ENTERPRISE: Default constructor for serialization
    /// </summary>
    public LogEntry()
    {
        Timestamp = DateTime.UtcNow;
        ThreadId = Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// ENTERPRISE: Primary constructor with essential data
    /// </summary>
    public LogEntry(DateTime timestamp, LogLevel level, string message, string? exception = null)
    {
        Timestamp = timestamp;
        Level = level;
        Message = message ?? string.Empty;
        Exception = exception;
        EventId = new EventId(0);
        ThreadId = Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// FUNCTIONAL: Create log entry from Microsoft.Extensions.Logging parameters
    /// ENTERPRISE: Factory method for integration with standard logging framework
    /// </summary>
    public static LogEntry Create<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        System.Exception? exception,
        Func<TState, System.Exception?, string> formatter) =>
        new()
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            Exception = exception?.ToString(),
            Category = typeof(TState).Name,
            ThreadId = Environment.CurrentManagedThreadId
        };

    /// <summary>
    /// FUNCTIONAL: Format entry for file output with configurable date format
    /// PERFORMANCE: Optimized string concatenation
    /// </summary>
    public string ToFileFormat(string dateFormat = "yyyy-MM-dd HH:mm:ss.fff") =>
        $"[{Timestamp.ToString(dateFormat)}] [{Level.ToString().ToUpperInvariant()}] [T{ThreadId:D3}] {Message}" +
        (Exception != null ? Environment.NewLine + Exception : "") +
        Environment.NewLine;

    /// <summary>
    /// FUNCTIONAL: Format entry for structured JSON output
    /// ENTERPRISE: Support for structured logging systems
    /// </summary>
    public string ToJsonFormat() =>
        $"{{\"timestamp\":\"{Timestamp:O}\",\"level\":\"{Level}\",\"message\":\"{EscapeJson(Message)}\",\"threadId\":{ThreadId}" +
        (Category != null ? $",\"category\":\"{EscapeJson(Category)}\"" : "") +
        (Source != null ? $",\"source\":\"{EscapeJson(Source)}\"" : "") +
        (Exception != null ? $",\"exception\":\"{EscapeJson(Exception)}\"" : "") +
        $",\"eventId\":{EventId.Id}}}";

    /// <summary>
    /// FUNCTIONAL: Check if entry meets minimum log level threshold
    /// PERFORMANCE: Fast level comparison for filtering
    /// </summary>
    public bool MeetsLevel(LogLevel minimumLevel) => Level >= minimumLevel;

    /// <summary>
    /// FUNCTIONAL: Check if entry is from specific category
    /// </summary>
    public bool IsFromCategory(string category) =>
        string.Equals(Category, category, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// FUNCTIONAL: Check if entry contains exception information
    /// </summary>
    public bool HasException => !string.IsNullOrEmpty(Exception);

    /// <summary>
    /// FUNCTIONAL: Transform entry with new message (immutable update)
    /// </summary>
    public LogEntry WithMessage(string newMessage) => this with { Message = newMessage ?? string.Empty };

    /// <summary>
    /// FUNCTIONAL: Transform entry with additional source context
    /// </summary>
    public LogEntry WithSource(string source) => this with { Source = source };

    /// <summary>
    /// FUNCTIONAL: Transform entry with category information
    /// </summary>
    public LogEntry WithCategory(string category) => this with { Category = category };

    /// <summary>
    /// UTILITY: Escape JSON special characters
    /// SECURITY: Prevent JSON injection in log messages
    /// </summary>
    private static string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// ENTERPRISE: Calculate approximate memory footprint
    /// PERFORMANCE: Memory usage monitoring for large log volumes
    /// </summary>
    public int GetApproximateSize() =>
        (Message?.Length ?? 0) +
        (Exception?.Length ?? 0) +
        (Category?.Length ?? 0) +
        (Source?.Length ?? 0) +
        64; // Base overhead for DateTime, LogLevel, etc.

    /// <summary>
    /// ENTERPRISE: Check if entry is within specified time range
    /// FUNCTIONAL: Time-based filtering support
    /// </summary>
    public bool IsWithinTimeRange(DateTime startTime, DateTime endTime) =>
        Timestamp >= startTime && Timestamp <= endTime;
}
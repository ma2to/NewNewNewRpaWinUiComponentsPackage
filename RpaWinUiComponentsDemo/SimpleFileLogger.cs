using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace RpaWinUiComponents.Demo;

/// <summary>
/// Simple file logger for demo app performance analysis
/// Writes all logs to a timestamped file in temp directory
/// </summary>
public sealed class SimpleFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logFilePath;
    private static readonly object _lock = new();

    public SimpleFileLogger(string categoryName, string logFilePath)
    {
        _categoryName = categoryName;
        _logFilePath = logFilePath;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logLevelStr = logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT ",
            _ => "UNKN "
        };

        var message = formatter(state, exception);
        var logEntry = $"{timestamp} [{logLevelStr}] {_categoryName}: {message}";

        if (exception != null)
        {
            logEntry += $"\n{exception}";
        }

        // Thread-safe file write
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Ignore file write errors to prevent logging from crashing the app
            }
        }
    }
}

/// <summary>
/// File logger provider
/// </summary>
public sealed class SimpleFileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;

    public SimpleFileLoggerProvider(string logFilePath)
    {
        _logFilePath = logFilePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SimpleFileLogger(categoryName, _logFilePath);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging;

/// <summary>
/// INFRASTRUCTURE: Comprehensive logging implementation for data grid operations
/// ENTERPRISE: Performance tracking, structured logging, and error handling
/// NULL OBJECT PATTERN: Safe to use without external logger dependencies
/// </summary>
internal sealed class DataGridLogger : IDataGridLogger, IDisposable
{
    private readonly ILogger _baseLogger;
    private readonly string _scopeName;
    private readonly bool _logPerformance;
    private bool _disposed;

    public DataGridLogger(ILogger? logger = null, string scopeName = "DataGrid", bool logPerformance = true)
    {
        _baseLogger = logger ?? NullLogger.Instance;
        _scopeName = scopeName;
        _logPerformance = logPerformance;
    }

    /// <summary>
    /// LOGGING: Log informational message with structured data
    /// </summary>
    public void LogInformation(string message, params object[] args)
    {
        if (_disposed) return;

        try
        {
            _baseLogger.LogInformation($"[{_scopeName}] {message}", args);
        }
        catch (Exception ex)
        {
            // Swallow logging exceptions to prevent application crashes
            Debug.WriteLine($"Logging error in {_scopeName}: {ex.Message}");
        }
    }

    /// <summary>
    /// LOGGING: Log warning message with structured data
    /// </summary>
    public void LogWarning(string message, params object[] args)
    {
        if (_disposed) return;

        try
        {
            _baseLogger.LogWarning($"[{_scopeName}] {message}", args);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Logging error in {_scopeName}: {ex.Message}");
        }
    }

    /// <summary>
    /// LOGGING: Log warning with exception context
    /// </summary>
    public void LogWarning(Exception exception, string message, params object[] args)
    {
        if (_disposed) return;

        try
        {
            _baseLogger.LogWarning(exception, $"[{_scopeName}] {message}", args);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Logging error in {_scopeName}: {ex.Message}");
        }
    }

    /// <summary>
    /// LOGGING: Log error message with structured data
    /// </summary>
    public void LogError(string message, params object[] args)
    {
        if (_disposed) return;

        try
        {
            _baseLogger.LogError($"[{_scopeName}] {message}", args);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Logging error in {_scopeName}: {ex.Message}");
        }
    }

    /// <summary>
    /// LOGGING: Log error with exception context
    /// </summary>
    public void LogError(Exception exception, string message, params object[] args)
    {
        if (_disposed) return;

        try
        {
            _baseLogger.LogError(exception, $"[{_scopeName}] {message}", args);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Logging error in {_scopeName}: {ex.Message}");
        }
    }

    /// <summary>
    /// FUNCTIONAL: Execute operation with automatic logging and Result wrapping
    /// PERFORMANCE: Automatic timing measurement
    /// </summary>
    public Result<T> ExecuteWithLogging<T>(Func<T> operation, string? operationName = null)
    {
        if (_disposed) return Result<T>.Failure("Logger has been disposed");

        var opName = operationName ?? "UnnamedOperation";
        var stopwatch = _logPerformance ? Stopwatch.StartNew() : null;

        try
        {
            LogInformation("OPERATION START: {OperationName}", opName);

            var result = operation();

            stopwatch?.Stop();

            LogInformation("OPERATION SUCCESS: {OperationName} completed in {ElapsedMs}ms",
                opName, stopwatch?.ElapsedMilliseconds ?? 0);

            if (_logPerformance && stopwatch != null)
            {
                LogPerformance(opName, stopwatch.Elapsed, result);
            }

            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();

            LogError(ex, "OPERATION FAILED: {OperationName} failed after {ElapsedMs}ms - {ErrorMessage}",
                opName, stopwatch?.ElapsedMilliseconds ?? 0, ex.Message);

            return Result<T>.Failure($"Operation {opName} failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// FUNCTIONAL: Execute operation with automatic logging
    /// </summary>
    public Result ExecuteWithLogging(Action operation, string? operationName = null)
    {
        if (_disposed) return Result.Failure("Logger has been disposed");

        var opName = operationName ?? "UnnamedOperation";
        var stopwatch = _logPerformance ? Stopwatch.StartNew() : null;

        try
        {
            LogInformation("OPERATION START: {OperationName}", opName);

            operation();

            stopwatch?.Stop();

            LogInformation("OPERATION SUCCESS: {OperationName} completed in {ElapsedMs}ms",
                opName, stopwatch?.ElapsedMilliseconds ?? 0);

            if (_logPerformance && stopwatch != null)
            {
                LogPerformance(opName, stopwatch.Elapsed);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();

            LogError(ex, "OPERATION FAILED: {OperationName} failed after {ElapsedMs}ms - {ErrorMessage}",
                opName, stopwatch?.ElapsedMilliseconds ?? 0, ex.Message);

            return Result.Failure($"Operation {opName} failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// SCOPED LOGGING: Create child logger with additional context
    /// </summary>
    public IDataGridLogger CreateScope(string scopeName)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DataGridLogger));

        var fullScopeName = $"{_scopeName}.{scopeName}";
        return new DataGridLogger(_baseLogger, fullScopeName, _logPerformance);
    }

    /// <summary>
    /// PERFORMANCE: Log operation timing and context
    /// </summary>
    public void LogPerformance(string operation, TimeSpan duration, object? context = null)
    {
        if (_disposed || !_logPerformance) return;

        try
        {
            var milliseconds = duration.TotalMilliseconds;

            if (milliseconds > 1000) // Log slow operations as warnings
            {
                LogWarning("PERFORMANCE: Slow operation {Operation} took {ElapsedMs}ms - Context: {Context}",
                    operation, milliseconds, context?.ToString() ?? "none");
            }
            else if (milliseconds > 100) // Log moderately slow operations as info
            {
                LogInformation("PERFORMANCE: Operation {Operation} took {ElapsedMs}ms - Context: {Context}",
                    operation, milliseconds, context?.ToString() ?? "none");
            }
            // Fast operations (< 100ms) are not logged unless debug logging is enabled
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Performance logging error in {_scopeName}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            LogInformation("LOGGER: Disposing {ScopeName}", _scopeName);
        }
        catch
        {
            // Ignore logging errors during disposal
        }

        _disposed = true;
    }
}

/// <summary>
/// FACTORY: Create logger instances with appropriate configuration
/// </summary>
internal static class DataGridLoggerFactory
{
    /// <summary>Create logger for UI operations</summary>
    public static IDataGridLogger CreateForUI(ILogger? logger = null) =>
        new DataGridLogger(logger, "UI", logPerformance: true);

    /// <summary>Create logger for headless operations</summary>
    public static IDataGridLogger CreateForHeadless(ILogger? logger = null) =>
        new DataGridLogger(logger, "Headless", logPerformance: false);

    /// <summary>Create logger for validation operations</summary>
    public static IDataGridLogger CreateForValidation(ILogger? logger = null) =>
        new DataGridLogger(logger, "Validation", logPerformance: true);

    /// <summary>Create logger for data operations</summary>
    public static IDataGridLogger CreateForData(ILogger? logger = null) =>
        new DataGridLogger(logger, "Data", logPerformance: true);

    /// <summary>Create logger for performance-critical operations</summary>
    public static IDataGridLogger CreateForPerformance(ILogger? logger = null) =>
        new DataGridLogger(logger, "Performance", logPerformance: true);
}
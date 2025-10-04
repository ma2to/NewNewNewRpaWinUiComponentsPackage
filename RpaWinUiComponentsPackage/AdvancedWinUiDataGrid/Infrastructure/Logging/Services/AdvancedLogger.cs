using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Services;

/// <summary>
/// ENTERPRISE: Advanced logger implementation with structured logging
/// THREAD SAFE: Thread-safe correlation ID management
/// </summary>
internal sealed class AdvancedLogger : IAdvancedLogger
{
    private readonly ILogger _logger;
    private readonly AsyncLocal<string?> _correlationId = new();

    public AdvancedLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogStructured(LogLevel level, string correlationId, string messageTemplate, params object?[] args)
    {
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["Timestamp"] = DateTime.UtcNow
        }))
        {
            _logger.Log(level, messageTemplate, args);
        }
    }

    public void LogWithContext(LogLevel level, string message, IDictionary<string, object?> context)
    {
        using (_logger.BeginScope(context))
        {
            _logger.Log(level, message);
        }
    }

    public IDisposable BeginCorrelatedScope(string correlationId, string operationName)
    {
        _correlationId.Value = correlationId;

        return _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = operationName,
            ["StartTime"] = DateTime.UtcNow
        }) ?? new NoOpDisposable();
    }

    public string? GetCurrentCorrelationId()
    {
        return _correlationId.Value;
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

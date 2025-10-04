using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

/// <summary>
/// ENTERPRISE: Advanced logging interface with structured logging and correlation
/// </summary>
internal interface IAdvancedLogger
{
    /// <summary>
    /// Log structured message with correlation ID
    /// </summary>
    void LogStructured(LogLevel level, string correlationId, string messageTemplate, params object?[] args);

    /// <summary>
    /// Log with context dictionary
    /// </summary>
    void LogWithContext(LogLevel level, string message, IDictionary<string, object?> context);

    /// <summary>
    /// Create scoped logger with correlation ID
    /// </summary>
    IDisposable BeginCorrelatedScope(string correlationId, string operationName);

    /// <summary>
    /// Get current correlation ID
    /// </summary>
    string? GetCurrentCorrelationId();
}

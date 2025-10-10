using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Services;

/// <summary>
/// Real implementation of operation scope with comprehensive tracking
/// Implementuje RAII pattern for automatické meranie času a disposal tracking
/// </summary>
internal sealed class OperationScope : IOperationScope
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private readonly string _correlationId;
    private object? _context;
    private object? _result;
    private bool _isCompleted;
    private readonly List<string> _warnings = new();

    public OperationScope(ILogger logger, string operationName, object? context = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        _context = context;

        // Generujeme krátke correlation ID for tracking
        _correlationId = Guid.NewGuid().ToString("N")[..8];
        _stopwatch = Stopwatch.StartNew();

        // Zalogujeme začiatok operácie
        _logger.LogInformation(
            "Operation '{OperationName}' started with correlation ID {CorrelationId}. Context: {@Context}",
            operationName, _correlationId, context);
    }

    public void MarkSuccess(object? result = null)
    {
        // Ak už bola operácia ukončená, ignorujeme duplicitné volanie
        if (_isCompleted) return;

        _stopwatch.Stop();
        _result = result;
        _isCompleted = true;

        // Zalogujeme úspešné dokončenie s časom a výsledkom
        _logger.LogInformation(
            "Operation '{OperationName}' completed successfully in {Duration}ms. CorrelationId: {CorrelationId}. Result: {@Result}",
            _operationName, _stopwatch.ElapsedMilliseconds, _correlationId, result);

        // Ak boli nejaké warnings, zalogujeme ich
        if (_warnings.Count > 0)
        {
            _logger.LogWarning(
                "Operation '{OperationName}' completed with {WarningCount} warnings: {Warnings}",
                _operationName, _warnings.Count, _warnings);
        }
    }

    public void MarkFailure(Exception exception)
    {
        // Ak už bola operácia ukončená, ignorujeme duplicitné volanie
        if (_isCompleted) return;

        _stopwatch.Stop();
        _isCompleted = true;

        // Zalogujeme chybu s exception a context
        _logger.LogError(exception,
            "Operation '{OperationName}' failed after {Duration}ms. CorrelationId: {CorrelationId}. Context: {@Context}",
            _operationName, _stopwatch.ElapsedMilliseconds, _correlationId, _context);
    }

    public void MarkWarning(string warning)
    {
        // Pridáme warning do zoznamu a zalogujeme ho
        _warnings.Add(warning);
        _logger.LogWarning(
            "Operation '{OperationName}' warning: {Warning}. CorrelationId: {CorrelationId}",
            _operationName, warning, _correlationId);
    }

    public void UpdateContext(object additionalContext)
    {
        // Pridáme nový context k existujúcemu
        _context = new { Original = _context, Additional = additionalContext };

        _logger.LogTrace(
            "Operation '{OperationName}' context updated. CorrelationId: {CorrelationId}. New context: {@Context}",
            _operationName, _correlationId, additionalContext);
    }

    public void SetResult(object result)
    {
        _result = result;
    }

    public void Dispose()
    {
        // Ak operácia nebola explicitne ukončená, zalogujeme warning
        if (!_isCompleted)
        {
            _stopwatch.Stop();

            _logger.LogWarning(
                "Operation '{OperationName}' disposed without explicit completion after {Duration}ms. CorrelationId: {CorrelationId}",
                _operationName, _stopwatch.ElapsedMilliseconds, _correlationId);
        }
    }

    // Properties implementation
    public string OperationName => _operationName;
    public DateTime StartTime { get; } = DateTime.UtcNow;
    public TimeSpan Elapsed => _stopwatch.Elapsed;
    public bool IsCompleted => _isCompleted;
    public string? CorrelationId => _correlationId;
}

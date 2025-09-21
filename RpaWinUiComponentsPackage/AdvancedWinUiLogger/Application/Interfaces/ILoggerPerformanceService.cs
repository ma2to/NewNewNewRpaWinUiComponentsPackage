using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Functional;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Logger performance monitoring and health check service
/// CLEAN ARCHITECTURE: Application layer interface for performance operations
/// </summary>
internal interface ILoggerPerformanceService
{
    Task<Result<bool>> CheckHealthAsync(CancellationToken cancellationToken = default);
    Result<object> GetPerformanceMetrics();
    Task<LoggerPerformanceMetrics> GetPerformanceMetricsAsync(ILogger logger, CancellationToken cancellationToken = default);
    Task ResetPerformanceCountersAsync(ILogger logger, CancellationToken cancellationToken = default);
    Task<Result<bool>> SetPerformanceMonitoringAsync(ILogger logger, bool enabled, CancellationToken cancellationToken = default);
}
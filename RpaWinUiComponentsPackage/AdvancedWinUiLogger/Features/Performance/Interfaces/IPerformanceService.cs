using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Performance.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Performance monitoring service contract
/// CLEAN ARCHITECTURE: Application layer interface for performance operations
/// </summary>
internal interface IPerformanceService
{
    Task<LoggerPerformanceMetrics> GetPerformanceMetricsAsync(ILogger logger, CancellationToken cancellationToken = default);
    Task ResetPerformanceCountersAsync(ILogger logger, CancellationToken cancellationToken = default);
    Task<bool> SetPerformanceMonitoringAsync(ILogger logger, bool enabled, CancellationToken cancellationToken = default);
}

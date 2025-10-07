using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Performance.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Performance;

/// <summary>
/// Internal implementation of DataGrid performance operations.
/// Delegates to internal performance service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridPerformance : IDataGridPerformance
{
    private readonly ILogger<DataGridPerformance>? _logger;
    private readonly IPerformanceService _performanceService;

    public DataGridPerformance(
        IPerformanceService performanceService,
        ILogger<DataGridPerformance>? logger = null)
    {
        _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
        _logger = logger;
    }

    public PublicPerformanceMetrics GetPerformanceMetrics()
    {
        try
        {
            var internalMetrics = _performanceService.GetPerformanceMetrics().GetAwaiter().GetResult();
            // Create public metrics from internal
            return new PublicPerformanceMetrics
            {
                TotalOperations = internalMetrics.TotalOperations,
                AverageOperationDuration = TimeSpan.FromMilliseconds(internalMetrics.TotalOperations > 0 ? (double)internalMetrics.Uptime.TotalMilliseconds / internalMetrics.TotalOperations : 0),
                OperationsPerSecond = internalMetrics.Uptime.TotalSeconds > 0 ? internalMetrics.TotalOperations / internalMetrics.Uptime.TotalSeconds : 0,
                CurrentMemoryUsageBytes = internalMetrics.MemoryUsageMB * 1024 * 1024,
                PeakMemoryUsageBytes = internalMetrics.MemoryUsageMB * 1024 * 1024,
                RenderStats = new PublicRenderStats()
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetPerformanceMetrics failed in Performance module");
            throw;
        }
    }

    public PublicResult ResetPerformanceMetrics()
    {
        try
        {
            _logger?.LogInformation("Resetting performance metrics via Performance module");

            _performanceService.ResetPerformanceMetrics().GetAwaiter().GetResult();
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ResetPerformanceMetrics failed in Performance module");
            throw;
        }
    }

    public async Task<PublicResult> EnableVirtualizationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Enabling virtualization via Performance module");

            await _performanceService.EnableVirtualizationAsync(cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "EnableVirtualization failed in Performance module");
            throw;
        }
    }

    public async Task<PublicResult> DisableVirtualizationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Disabling virtualization via Performance module");

            await _performanceService.DisableVirtualizationAsync(cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DisableVirtualization failed in Performance module");
            throw;
        }
    }

    public async Task<PublicResult> OptimizeMemoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Optimizing memory via Performance module");

            await _performanceService.OptimizeMemoryAsync(cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "OptimizeMemory failed in Performance module");
            throw;
        }
    }

    public PublicMemoryUsage GetMemoryUsage()
    {
        try
        {
            var memoryUsage = _performanceService.GetMemoryUsage();
            return new PublicMemoryUsage
            {
                AllocatedBytes = memoryUsage,
                DataMemoryBytes = memoryUsage / 2, // Estimate
                UiMemoryBytes = memoryUsage / 4, // Estimate
                CacheMemoryBytes = memoryUsage / 4 // Estimate
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetMemoryUsage failed in Performance module");
            throw;
        }
    }

    public bool IsVirtualizationEnabled()
    {
        try
        {
            return _performanceService.IsVirtualizationEnabled();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsVirtualizationEnabled check failed in Performance module");
            throw;
        }
    }

    public PublicResult SetRenderingThrottle(int delayMs)
    {
        try
        {
            _logger?.LogInformation("Setting rendering throttle to {DelayMs}ms via Performance module", delayMs);

            _performanceService.SetRenderingThrottle(delayMs).GetAwaiter().GetResult();
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetRenderingThrottle failed in Performance module");
            throw;
        }
    }

    public int GetRenderingThrottle()
    {
        try
        {
            return _performanceService.GetRenderingThrottle();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetRenderingThrottle failed in Performance module");
            throw;
        }
    }
}

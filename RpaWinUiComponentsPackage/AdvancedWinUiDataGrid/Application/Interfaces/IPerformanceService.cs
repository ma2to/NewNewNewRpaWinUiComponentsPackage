using System;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Performance monitoring functionality
/// CLEAN ARCHITECTURE: Application layer interface for performance operations
/// </summary>
internal interface IPerformanceService
{
    // Performance monitoring operations
    Task<PerformanceMetrics> GetPerformanceMetricsAsync(
        TimeSpan? timeWindow = null,
        CancellationToken cancellationToken = default);

    Task ResetPerformanceCountersAsync(CancellationToken cancellationToken = default);

    // Virtualization operations
    Task<DataPage> LoadPageAsync(
        int pageIndex,
        VirtualizationConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<PerformanceStatistics> GetVirtualizationStatisticsAsync(CancellationToken cancellationToken = default);

    // Memory management operations
    Task OptimizeMemoryUsageAsync(CancellationToken cancellationToken = default);
    Task<double> GetCurrentMemoryUsageMBAsync(CancellationToken cancellationToken = default);

    // Configuration operations
    Task ApplyVirtualizationConfigurationAsync(
        VirtualizationConfiguration configuration,
        CancellationToken cancellationToken = default);

    VirtualizationConfiguration GetCurrentConfiguration();
}


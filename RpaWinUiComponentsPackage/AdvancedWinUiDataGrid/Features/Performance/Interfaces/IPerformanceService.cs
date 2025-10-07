using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Performance.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Performance.Interfaces;

/// <summary>
/// Interface for performance monitoring service
/// </summary>
internal interface IPerformanceService
{
    /// <summary>
    /// Start performance monitoring
    /// </summary>
    Task<Result> StartMonitoringAsync(StartMonitoringCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop performance monitoring
    /// </summary>
    Task<Result> StopMonitoringAsync(StopMonitoringCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current performance snapshot
    /// </summary>
    Task<PerformanceSnapshot> GetPerformanceSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get comprehensive performance report
    /// </summary>
    Task<PerformanceReport> GetPerformanceReportAsync(GetPerformanceReportCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze performance bottlenecks
    /// </summary>
    Task<IReadOnlyList<string>> AnalyzeBottlenecksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get performance statistics
    /// </summary>
    PerformanceStatistics GetPerformanceStatistics();

    // Wrapper methods for public API
    Task<PerformanceMetrics> GetPerformanceMetrics();
    Task ResetPerformanceMetrics();
    Task EnableVirtualizationAsync(CancellationToken cancellationToken = default);
    Task DisableVirtualizationAsync(CancellationToken cancellationToken = default);
    Task OptimizeMemoryAsync(CancellationToken cancellationToken = default);
    long GetMemoryUsage();
    bool IsVirtualizationEnabled();
    Task SetRenderingThrottle(int milliseconds);
    int GetRenderingThrottle();
}

/// <summary>
/// Performance metrics for public API
/// </summary>
internal class PerformanceMetrics
{
    public long TotalOperations { get; init; }
    public long TotalErrors { get; init; }
    public long MemoryUsageMB { get; init; }
    public int ThreadCount { get; init; }
    public TimeSpan Uptime { get; init; }
}

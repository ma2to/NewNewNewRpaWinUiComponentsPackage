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
}

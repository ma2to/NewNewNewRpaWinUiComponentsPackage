using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Partial class containing Performance Operations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Performance Operations

    public async Task<PublicResult> StartPerformanceMonitoringAsync(StartPerformanceMonitoringCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var performanceService = scope.ServiceProvider.GetRequiredService<Features.Performance.Interfaces.IPerformanceService>();

            var startCommand = Features.Performance.Commands.StartMonitoringCommand.Create(command.MonitoringWindow);
            var internalResult = await performanceService.StartMonitoringAsync(startCommand, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start performance monitoring");
            return PublicResult.Failure(ex.Message);
        }
    }

    public async Task<PublicResult> StopPerformanceMonitoringAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var performanceService = scope.ServiceProvider.GetRequiredService<Features.Performance.Interfaces.IPerformanceService>();

            var stopCommand = Features.Performance.Commands.StopMonitoringCommand.Create();
            var internalResult = await performanceService.StopMonitoringAsync(stopCommand, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop performance monitoring");
            return PublicResult.Failure(ex.Message);
        }
    }

    public async Task<PerformanceSnapshotData> GetPerformanceSnapshotAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var performanceService = scope.ServiceProvider.GetRequiredService<Features.Performance.Interfaces.IPerformanceService>();

            var snapshot = await performanceService.GetPerformanceSnapshotAsync(cancellationToken);

            return new PerformanceSnapshotData(
                snapshot.TotalOperations,
                snapshot.TotalErrors,
                snapshot.ErrorRate,
                snapshot.CurrentMemoryUsage,
                snapshot.CpuTime,
                snapshot.ThreadCount,
                snapshot.Timestamp
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance snapshot");
            return new PerformanceSnapshotData();
        }
    }

    public async Task<PerformanceReportData> GetPerformanceReportAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var performanceService = scope.ServiceProvider.GetRequiredService<Features.Performance.Interfaces.IPerformanceService>();

            var reportCommand = Features.Performance.Commands.GetPerformanceReportCommand.Create();
            var report = await performanceService.GetPerformanceReportAsync(reportCommand, cancellationToken);

            var snapshot = new PerformanceSnapshotData(
                report.Snapshot.TotalOperations,
                report.Snapshot.TotalErrors,
                report.Snapshot.ErrorRate,
                report.Snapshot.CurrentMemoryUsage,
                report.Snapshot.CpuTime,
                report.Snapshot.ThreadCount,
                report.Snapshot.Timestamp
            );

            return new PerformanceReportData(
                snapshot,
                report.Bottlenecks,
                report.Recommendations,
                (PublicPerformanceThreshold)report.Threshold,
                report.AnalysisDuration
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance report");
            return new PerformanceReportData();
        }
    }

    public PerformanceStatisticsData GetPerformanceStatistics()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var performanceService = scope.ServiceProvider.GetRequiredService<Features.Performance.Interfaces.IPerformanceService>();

            var stats = performanceService.GetPerformanceStatistics();

            return new PerformanceStatisticsData(
                stats.TotalOperations,
                stats.TotalErrors,
                stats.AverageOperationTime,
                stats.PeakMemoryUsage,
                stats.CurrentMemoryUsage
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance statistics");
            return new PerformanceStatisticsData();
        }
    }

    #endregion
}


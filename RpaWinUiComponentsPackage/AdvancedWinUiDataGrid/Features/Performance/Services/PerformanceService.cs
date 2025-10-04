using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Performance.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Performance.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Performance.Services;

/// <summary>
/// Service for performance monitoring and optimization
/// </summary>
internal sealed class PerformanceService : IPerformanceService
{
    private readonly ILogger<PerformanceService> _logger;
    private bool _isMonitoring = false;
    private long _totalOperations = 0;
    private long _totalErrors = 0;
    private readonly Stopwatch _uptime = Stopwatch.StartNew();

    public PerformanceService(ILogger<PerformanceService> logger)
    {
        _logger = logger;
    }

    public async Task<Result> StartMonitoringAsync(StartMonitoringCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _isMonitoring = true;
            _logger.LogInformation("Performance monitoring started: window={Window}min", command.MonitoringWindow.TotalMinutes);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start performance monitoring");
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> StopMonitoringAsync(StopMonitoringCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _isMonitoring = false;
            _logger.LogInformation("Performance monitoring stopped");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop performance monitoring");
            return Result.Failure(ex.Message);
        }
    }

    public async Task<PerformanceSnapshot> GetPerformanceSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var process = Process.GetCurrentProcess();
        var memoryUsage = GC.GetTotalMemory(false);
        var cpuTime = process.TotalProcessorTime;
        var threadCount = process.Threads.Count;

        Interlocked.Increment(ref _totalOperations);

        return PerformanceSnapshot.Create(
            _totalOperations,
            _totalErrors,
            memoryUsage,
            cpuTime,
            threadCount
        );
    }

    public async Task<PerformanceReport> GetPerformanceReportAsync(GetPerformanceReportCommand command, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var snapshot = await GetPerformanceSnapshotAsync(cancellationToken);

        var bottlenecks = command.IncludeBottleneckAnalysis
            ? await AnalyzeBottlenecksAsync(cancellationToken)
            : Array.Empty<string>();

        var threshold = DetermineThreshold(snapshot);

        sw.Stop();

        _logger.LogInformation("Performance report generated: threshold={Threshold}, bottlenecks={Count}",
            threshold, bottlenecks.Count);

        return new PerformanceReport
        {
            Snapshot = snapshot,
            Bottlenecks = bottlenecks,
            Threshold = threshold,
            AnalysisDuration = sw.Elapsed
        };
    }

    public async Task<IReadOnlyList<string>> AnalyzeBottlenecksAsync(CancellationToken cancellationToken = default)
    {
        var bottlenecks = new List<string>();
        var snapshot = await GetPerformanceSnapshotAsync(cancellationToken);

        if (snapshot.ErrorRate > 5.0)
        {
            bottlenecks.Add($"High error rate: {snapshot.ErrorRate:F2}%");
        }

        if (snapshot.CurrentMemoryUsage > 500 * 1024 * 1024) // 500 MB
        {
            bottlenecks.Add($"High memory usage: {snapshot.CurrentMemoryUsage / 1024 / 1024} MB");
        }

        if (snapshot.ThreadCount > 50)
        {
            bottlenecks.Add($"High thread count: {snapshot.ThreadCount}");
        }

        return bottlenecks;
    }

    public PerformanceStatistics GetPerformanceStatistics()
    {
        var memoryUsage = GC.GetTotalMemory(false);

        return PerformanceStatistics.Create(
            _totalOperations,
            _totalErrors,
            avgTime: 0,
            peakMemory: memoryUsage,
            currentMemory: memoryUsage
        );
    }

    private PerformanceThreshold DetermineThreshold(PerformanceSnapshot snapshot)
    {
        if (snapshot.ErrorRate > 10.0 || snapshot.CurrentMemoryUsage > 1024 * 1024 * 1024)
        {
            return PerformanceThreshold.Critical;
        }

        if (snapshot.ErrorRate > 5.0 || snapshot.CurrentMemoryUsage > 500 * 1024 * 1024)
        {
            return PerformanceThreshold.Warning;
        }

        return PerformanceThreshold.Normal;
    }
}

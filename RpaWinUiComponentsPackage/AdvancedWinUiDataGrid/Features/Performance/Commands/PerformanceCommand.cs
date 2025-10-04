using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Performance.Commands;

/// <summary>
/// Command to start performance monitoring
/// </summary>
internal sealed record StartMonitoringCommand
{
    internal TimeSpan MonitoringWindow { get; init; } = TimeSpan.FromMinutes(5);
    internal bool IncludeSystemMetrics { get; init; } = true;
    internal bool IncludeMemoryMetrics { get; init; } = true;
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static StartMonitoringCommand Create(TimeSpan window) =>
        new() { MonitoringWindow = window };
}

/// <summary>
/// Command to stop performance monitoring
/// </summary>
internal sealed record StopMonitoringCommand
{
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static StopMonitoringCommand Create() => new();
}

/// <summary>
/// Command to get performance report
/// </summary>
internal sealed record GetPerformanceReportCommand
{
    internal bool IncludeBottleneckAnalysis { get; init; } = true;
    internal bool IncludeRecommendations { get; init; } = true;
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static GetPerformanceReportCommand Create() => new();

    internal static GetPerformanceReportCommand WithAnalysis(bool includeBottlenecks, bool includeRecommendations) =>
        new()
        {
            IncludeBottleneckAnalysis = includeBottlenecks,
            IncludeRecommendations = includeRecommendations
        };
}

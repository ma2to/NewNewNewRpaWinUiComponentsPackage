using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Models;

/// <summary>
/// Result of sort operation with complete statistics
/// Immutable record with factory methods
/// </summary>
internal sealed record SortResult
{
    internal bool Success { get; init; }
    internal IReadOnlyList<IReadOnlyDictionary<string, object?>> SortedData { get; init; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
    internal IReadOnlyList<SortColumnConfiguration> AppliedSorts { get; init; } = Array.Empty<SortColumnConfiguration>();
    internal int ProcessedRows { get; init; }
    internal TimeSpan SortTime { get; init; }
    internal SortPerformanceMode UsedPerformanceMode { get; init; }
    internal bool UsedParallelProcessing { get; init; }
    internal bool UsedStableSort { get; init; }
    internal IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();
    internal SortStatistics Statistics { get; init; } = new();

    /// <summary>
    /// Creates successful sort result
    /// </summary>
    internal static SortResult CreateSuccess(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> sortedData,
        IReadOnlyList<SortColumnConfiguration> appliedSorts,
        TimeSpan sortTime,
        SortPerformanceMode usedMode = SortPerformanceMode.Auto,
        bool usedParallel = false,
        bool usedStable = true,
        SortStatistics? statistics = null) =>
        new()
        {
            Success = true,
            SortedData = sortedData,
            AppliedSorts = appliedSorts,
            ProcessedRows = sortedData.Count,
            SortTime = sortTime,
            UsedPerformanceMode = usedMode,
            UsedParallelProcessing = usedParallel,
            UsedStableSort = usedStable,
            Statistics = statistics ?? new SortStatistics
            {
                UsedParallelProcessing = usedParallel,
                SelectedMode = usedMode,
                AverageSortTime = sortTime
            }
        };

    /// <summary>
    /// Creates failed sort result
    /// </summary>
    internal static SortResult CreateFailure(
        IReadOnlyList<string> errors,
        TimeSpan sortTime) =>
        new()
        {
            Success = false,
            ErrorMessages = errors,
            SortTime = sortTime
        };

    internal static SortResult Empty => new();
}

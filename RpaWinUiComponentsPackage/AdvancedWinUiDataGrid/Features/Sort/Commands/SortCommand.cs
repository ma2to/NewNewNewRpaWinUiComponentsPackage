using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Commands;

/// <summary>
/// Command for basic single-column sorting
/// Immutable record with factory methods for LINQ optimization
/// </summary>
internal sealed record SortCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required string ColumnName { get; init; }
    internal CoreTypes.SortDirection Direction { get; init; } = CoreTypes.SortDirection.Ascending;
    internal bool CaseSensitive { get; init; } = false;
    internal bool EnableParallelProcessing { get; init; } = true;
    internal CoreTypes.SortPerformanceMode PerformanceMode { get; init; } = CoreTypes.SortPerformanceMode.Auto;
    internal TimeSpan? Timeout { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    /// <summary>
    /// Creates basic sort command
    /// </summary>
    internal static SortCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        CoreTypes.SortDirection direction = CoreTypes.SortDirection.Ascending) =>
        new() { Data = data, ColumnName = columnName, Direction = direction };

    /// <summary>
    /// Creates command with selected performance mode
    /// </summary>
    internal static SortCommand WithPerformanceMode(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        CoreTypes.SortDirection direction,
        CoreTypes.SortPerformanceMode performanceMode) =>
        new()
        {
            Data = data,
            ColumnName = columnName,
            Direction = direction,
            PerformanceMode = performanceMode
        };
}

/// <summary>
/// Command for multi-column sort with advanced configuration
/// </summary>
internal sealed record MultiSortCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required IReadOnlyList<CoreTypes.SortColumnConfiguration> SortColumns { get; init; }
    internal bool EnableParallelProcessing { get; init; } = true;
    internal CoreTypes.SortPerformanceMode PerformanceMode { get; init; } = CoreTypes.SortPerformanceMode.Auto;
    internal CoreTypes.SortStability Stability { get; init; } = CoreTypes.SortStability.Stable;
    internal TimeSpan? Timeout { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    /// <summary>
    /// Creates multi-column sort command
    /// </summary>
    internal static MultiSortCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<CoreTypes.SortColumnConfiguration> sortColumns) =>
        new() { Data = data, SortColumns = sortColumns };

    /// <summary>
    /// Creates command with performance optimization
    /// </summary>
    internal static MultiSortCommand WithPerformanceOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<CoreTypes.SortColumnConfiguration> sortColumns) =>
        new()
        {
            Data = data,
            SortColumns = sortColumns,
            EnableParallelProcessing = true,
            PerformanceMode = CoreTypes.SortPerformanceMode.Optimized
        };
}

/// <summary>
/// Command for advanced sort with business rules
/// Enterprise complex sorting with configuration management
/// </summary>
internal sealed record AdvancedSortCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required CoreTypes.AdvancedSortConfiguration SortConfiguration { get; init; }
    internal bool EnableParallelProcessing { get; init; } = true;
    internal bool UseSmartOptimization { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<SortProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;
    internal CoreTypes.SortContext? Context { get; init; }

    /// <summary>
    /// Creates advanced sort command
    /// </summary>
    internal static AdvancedSortCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.AdvancedSortConfiguration sortConfiguration) =>
        new() { Data = data, SortConfiguration = sortConfiguration };

    /// <summary>
    /// Creates command with optimization
    /// </summary>
    internal static AdvancedSortCommand WithOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.AdvancedSortConfiguration sortConfiguration,
        bool useSmartOptimization = true) =>
        new()
        {
            Data = data,
            SortConfiguration = sortConfiguration,
            UseSmartOptimization = useSmartOptimization,
            EnableParallelProcessing = true
        };

    /// <summary>
    /// Creates command with context
    /// </summary>
    internal static AdvancedSortCommand WithContext(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.AdvancedSortConfiguration sortConfiguration,
        CoreTypes.SortContext context) =>
        new() { Data = data, SortConfiguration = sortConfiguration, Context = context };
}

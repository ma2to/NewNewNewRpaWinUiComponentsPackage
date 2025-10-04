using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Commands;

/// <summary>
/// Command pre základné jednokolónkové triedenie
/// Immutable record s factory methods pre LINQ optimization
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
    /// Vytvorí základný sort command
    /// </summary>
    internal static SortCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        CoreTypes.SortDirection direction = CoreTypes.SortDirection.Ascending) =>
        new() { Data = data, ColumnName = columnName, Direction = direction };

    /// <summary>
    /// Vytvorí command s výberom performance mode
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
/// Command pre multi-column sort s pokročilou konfiguráciou
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
    /// Vytvorí multi-column sort command
    /// </summary>
    internal static MultiSortCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<CoreTypes.SortColumnConfiguration> sortColumns) =>
        new() { Data = data, SortColumns = sortColumns };

    /// <summary>
    /// Vytvorí command s performance optimization
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
/// Command pre advanced sort s business rules
/// Enterprise komplexné triedenie s konfiguračným správaním
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
    /// Vytvorí advanced sort command
    /// </summary>
    internal static AdvancedSortCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.AdvancedSortConfiguration sortConfiguration) =>
        new() { Data = data, SortConfiguration = sortConfiguration };

    /// <summary>
    /// Vytvorí command s optimalizáciou
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
    /// Vytvorí command s kontextom
    /// </summary>
    internal static AdvancedSortCommand WithContext(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.AdvancedSortConfiguration sortConfiguration,
        CoreTypes.SortContext context) =>
        new() { Data = data, SortConfiguration = sortConfiguration, Context = context };
}

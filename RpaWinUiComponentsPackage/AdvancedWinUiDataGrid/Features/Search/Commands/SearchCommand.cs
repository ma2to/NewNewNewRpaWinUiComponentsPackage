using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Commands;

/// <summary>
/// COMMAND PATTERN: Basic search command with LINQ optimization
/// CONSISTENT: Rovnaká štruktúra ako ImportDataCommand a ValidateDataCommand
/// </summary>
internal sealed record SearchCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required string SearchText { get; init; }
    internal string[]? TargetColumns { get; init; }
    internal bool CaseSensitive { get; init; } = false;
    internal SearchScope Scope { get; init; } = SearchScope.AllData;
    internal bool EnableParallelProcessing { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<SearchProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    // FLEXIBLE factory methods s LINQ optimization
    internal static SearchCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        string[]? targetColumns = null) =>
        new() { Data = data, SearchText = searchText, TargetColumns = targetColumns };

    internal static SearchCommand WithScope(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        SearchScope scope,
        string[]? targetColumns = null) =>
        new() { Data = data, SearchText = searchText, Scope = scope, TargetColumns = targetColumns };

    // LINQ optimized factory
    internal static SearchCommand WithLINQOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText) =>
        new()
        {
            Data = data.AsParallel().Where(row => row.Values.Any(v => v != null)),
            SearchText = searchText,
            EnableParallelProcessing = true
        };
}

/// <summary>
/// COMMAND PATTERN: Advanced search command with comprehensive criteria
/// ENTERPRISE: Supports complex search scenarios and business logic
/// </summary>
internal sealed record AdvancedSearchCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required AdvancedSearchCriteria SearchCriteria { get; init; }
    internal bool EnableParallelProcessing { get; init; } = true;
    internal bool UseSmartRanking { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<SearchProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;
    internal SearchContext? Context { get; init; }

    // FLEXIBLE factory methods
    internal static AdvancedSearchCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSearchCriteria searchCriteria) =>
        new() { Data = data, SearchCriteria = searchCriteria };

    internal static AdvancedSearchCommand WithContext(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSearchCriteria searchCriteria,
        SearchContext context) =>
        new() { Data = data, SearchCriteria = searchCriteria, Context = context };

    internal static AdvancedSearchCommand WithPerformanceOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSearchCriteria searchCriteria) =>
        new()
        {
            Data = data,
            SearchCriteria = searchCriteria,
            EnableParallelProcessing = true,
            UseSmartRanking = true
        };
}

/// <summary>
/// COMMAND PATTERN: Smart search command with automatic optimization
/// SMART: Automatically switches between quick and advanced search based on criteria
/// </summary>
internal sealed record SmartSearchCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required string SearchText { get; init; }
    internal string[]? TargetColumns { get; init; }
    internal bool CaseSensitive { get; init; } = false;
    internal SearchScope Scope { get; init; } = SearchScope.AllData;
    internal bool AutoOptimize { get; init; } = true;
    internal bool UseCache { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<SearchProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    // FLEXIBLE factory methods
    internal static SmartSearchCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        string[]? targetColumns = null) =>
        new() { Data = data, SearchText = searchText, TargetColumns = targetColumns };

    internal static SmartSearchCommand WithOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        bool autoOptimize = true,
        bool useCache = true) =>
        new()
        {
            Data = data,
            SearchText = searchText,
            AutoOptimize = autoOptimize,
            UseCache = useCache
        };
}

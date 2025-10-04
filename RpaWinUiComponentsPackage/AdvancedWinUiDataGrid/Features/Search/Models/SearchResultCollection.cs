using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Models;

/// <summary>
/// ENTERPRISE: Search operation result with comprehensive statistics
/// CONSISTENT: Rovnaká štruktúra ako FilterResult a ValidationResult
/// </summary>
internal sealed record SearchResultCollection
{
    internal bool Success { get; init; }
    internal IReadOnlyList<SearchResult> Results { get; init; } = Array.Empty<SearchResult>();
    internal int TotalMatchesFound { get; init; }
    internal int TotalRowsSearched { get; init; }
    internal int TotalColumnsSearched { get; init; }
    internal TimeSpan SearchTime { get; init; }
    internal SearchMode UsedSearchMode { get; init; }
    internal bool UsedParallelProcessing { get; init; }
    internal bool UsedSmartRanking { get; init; }
    internal IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();
    internal SearchStatistics Statistics { get; init; } = new();

    internal static SearchResultCollection CreateSuccess(
        IReadOnlyList<SearchResult> results,
        int totalRowsSearched,
        int totalColumnsSearched,
        TimeSpan searchTime,
        SearchMode usedMode = SearchMode.Contains,
        bool usedParallel = false,
        bool usedRanking = false) =>
        new()
        {
            Success = true,
            Results = results,
            TotalMatchesFound = results.Count,
            TotalRowsSearched = totalRowsSearched,
            TotalColumnsSearched = totalColumnsSearched,
            SearchTime = searchTime,
            UsedSearchMode = usedMode,
            UsedParallelProcessing = usedParallel,
            UsedSmartRanking = usedRanking,
            Statistics = new SearchStatistics
            {
                TotalSearchOperations = 1,
                AverageSearchTime = searchTime,
                UsedParallelProcessing = usedParallel,
                AverageRelevanceScore = results.Any() ? results.Average(r => r.RelevanceScore) : 0.0
            }
        };

    internal static SearchResultCollection CreateFailure(
        IReadOnlyList<string> errors,
        TimeSpan searchTime) =>
        new()
        {
            Success = false,
            ErrorMessages = errors,
            SearchTime = searchTime
        };
}

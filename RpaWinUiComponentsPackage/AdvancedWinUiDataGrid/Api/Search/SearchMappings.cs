using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Internal;

/// <summary>
/// Mapping extensions pre konverziu medzi public a internal search types
/// </summary>
internal static class SearchMappings
{
    #region Public → Internal

    /// <summary>
    /// Konvertuje public search scope na internal
    /// </summary>
    internal static SearchScope ToInternal(this PublicSearchScope scope) => scope switch
    {
        PublicSearchScope.AllData => SearchScope.AllData,
        PublicSearchScope.VisibleData => SearchScope.VisibleData,
        PublicSearchScope.SelectedData => SearchScope.SelectedData,
        PublicSearchScope.FilteredData => SearchScope.FilteredData,
        _ => SearchScope.AllData
    };

    /// <summary>
    /// Konvertuje public search mode na internal
    /// </summary>
    internal static SearchMode ToInternal(this PublicSearchMode mode) => mode switch
    {
        PublicSearchMode.Contains => SearchMode.Contains,
        PublicSearchMode.Exact => SearchMode.Exact,
        PublicSearchMode.StartsWith => SearchMode.StartsWith,
        PublicSearchMode.EndsWith => SearchMode.EndsWith,
        PublicSearchMode.Regex => SearchMode.Regex,
        PublicSearchMode.Fuzzy => SearchMode.Fuzzy,
        _ => SearchMode.Contains
    };

    /// <summary>
    /// Konvertuje public search ranking na internal
    /// </summary>
    internal static SearchRanking ToInternal(this PublicSearchRanking ranking) => ranking switch
    {
        PublicSearchRanking.None => SearchRanking.None,
        PublicSearchRanking.Relevance => SearchRanking.Relevance,
        PublicSearchRanking.Position => SearchRanking.Position,
        PublicSearchRanking.Frequency => SearchRanking.Frequency,
        _ => SearchRanking.None
    };

    /// <summary>
    /// Konvertuje public advanced search criteria na internal
    /// </summary>
    internal static AdvancedSearchCriteria ToInternal(this PublicAdvancedSearchCriteria criteria) =>
        new()
        {
            SearchText = criteria.SearchText,
            TargetColumns = criteria.TargetColumns,
            UseRegex = criteria.UseRegex,
            CaseSensitive = criteria.CaseSensitive,
            WholeWordOnly = criteria.WholeWordOnly,
            Scope = criteria.Scope.ToInternal(),
            Mode = criteria.Mode.ToInternal(),
            Ranking = criteria.Ranking.ToInternal(),
            MaxMatches = criteria.MaxMatches,
            Timeout = criteria.Timeout,
            HighlightMatches = criteria.HighlightMatches,
            IncludeHiddenColumns = criteria.IncludeHiddenColumns,
            FuzzyThreshold = criteria.FuzzyThreshold
        };

    /// <summary>
    /// Konvertuje public search command na internal
    /// </summary>
    internal static SearchCommand ToInternal(this SearchDataCommand command) =>
        new()
        {
            Data = command.Data,
            SearchText = command.SearchText,
            TargetColumns = command.TargetColumns,
            CaseSensitive = command.CaseSensitive,
            Scope = command.Scope.ToInternal(),
            Timeout = command.Timeout,
            ProgressReporter = command.Progress != null ? CreateProgressWrapper(command.Progress) : null
        };

    /// <summary>
    /// Konvertuje public advanced search command na internal
    /// </summary>
    internal static AdvancedSearchCommand ToInternal(this AdvancedSearchDataCommand command) =>
        new()
        {
            Data = command.Data,
            SearchCriteria = command.SearchCriteria.ToInternal(),
            EnableParallelProcessing = command.EnableParallelProcessing,
            UseSmartRanking = command.UseSmartRanking,
            ProgressReporter = command.Progress != null ? CreateProgressWrapper(command.Progress) : null
        };

    /// <summary>
    /// Konvertuje public smart search command na internal
    /// </summary>
    internal static SmartSearchCommand ToInternal(this SmartSearchDataCommand command) =>
        new()
        {
            Data = command.Data,
            SearchText = command.SearchText,
            TargetColumns = command.TargetColumns,
            CaseSensitive = command.CaseSensitive,
            AutoOptimize = command.AutoOptimize,
            ProgressReporter = command.Progress != null ? CreateProgressWrapper(command.Progress) : null
        };

    #endregion

    #region Internal → Public

    /// <summary>
    /// Konvertuje internal search scope na public
    /// </summary>
    internal static PublicSearchScope ToPublic(this SearchScope scope) => scope switch
    {
        SearchScope.AllData => PublicSearchScope.AllData,
        SearchScope.VisibleData => PublicSearchScope.VisibleData,
        SearchScope.SelectedData => PublicSearchScope.SelectedData,
        SearchScope.FilteredData => PublicSearchScope.FilteredData,
        _ => PublicSearchScope.AllData
    };

    /// <summary>
    /// Konvertuje internal search mode na public
    /// </summary>
    internal static PublicSearchMode ToPublic(this SearchMode mode) => mode switch
    {
        SearchMode.Contains => PublicSearchMode.Contains,
        SearchMode.Exact => PublicSearchMode.Exact,
        SearchMode.StartsWith => PublicSearchMode.StartsWith,
        SearchMode.EndsWith => PublicSearchMode.EndsWith,
        SearchMode.Regex => PublicSearchMode.Regex,
        SearchMode.Fuzzy => PublicSearchMode.Fuzzy,
        _ => PublicSearchMode.Contains
    };

    /// <summary>
    /// Konvertuje internal search ranking na public
    /// </summary>
    internal static PublicSearchRanking ToPublic(this SearchRanking ranking) => ranking switch
    {
        SearchRanking.None => PublicSearchRanking.None,
        SearchRanking.Relevance => PublicSearchRanking.Relevance,
        SearchRanking.Position => PublicSearchRanking.Position,
        SearchRanking.Frequency => PublicSearchRanking.Frequency,
        _ => PublicSearchRanking.None
    };

    /// <summary>
    /// Konvertuje internal search progress na public
    /// </summary>
    internal static PublicSearchProgress ToPublic(this SearchProgress progress) =>
        new(
            progress.ProcessedRows,
            progress.TotalRows,
            progress.ElapsedTime,
            progress.CurrentOperation,
            progress.FoundMatches,
            progress.CurrentColumn
        );

    /// <summary>
    /// Konvertuje internal search result na public
    /// </summary>
    internal static PublicSearchResult ToPublic(this SearchResult result) =>
        new(
            RowIndex: result.RowIndex,
            ColumnName: result.ColumnName,
            Value: result.Value,
            MatchedText: result.MatchedText,
            IsExactMatch: result.IsExactMatch,
            MatchScore: result.MatchScore,
            RelevanceScore: result.RelevanceScore,
            CellAddress: result.CellAddress
        );

    /// <summary>
    /// Konvertuje internal search result collection na public
    /// </summary>
    internal static SearchDataResult ToPublic(this SearchResultCollection result) =>
        new(
            IsSuccess: result.Success,
            Results: result.Results.Select(r => r.ToPublic()).ToList(),
            TotalMatchesFound: result.TotalMatchesFound,
            TotalRowsSearched: result.TotalRowsSearched,
            Duration: result.SearchTime,
            UsedSearchMode: result.UsedSearchMode.ToPublic(),
            UsedParallelProcessing: result.UsedParallelProcessing,
            ErrorMessages: result.ErrorMessages
        );

    #endregion

    #region Progress Wrapper

    /// <summary>
    /// Vytvorí progress wrapper pre konverziu medzi public a internal progress
    /// </summary>
    internal static IProgress<SearchProgress> CreateProgressWrapper(IProgress<PublicSearchProgress> publicProgress)
    {
        return new Progress<SearchProgress>(internalProgress =>
        {
            publicProgress.Report(internalProgress.ToPublic());
        });
    }

    #endregion
}

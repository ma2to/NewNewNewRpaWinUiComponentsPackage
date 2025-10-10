using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Interfaces;

/// <summary>
/// Interface for search service with comprehensive searching capabilities
/// Combines basic, advanced and smart search functions
/// </summary>
internal interface ISearchService
{
    // COMMAND PATTERN API

    /// <summary>
    /// Performs basic search (command pattern)
    /// </summary>
    Task<SearchResultCollection> SearchAsync(SearchCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs advanced search with complex criteria (command pattern)
    /// </summary>
    Task<SearchResultCollection> AdvancedSearchAsync(AdvancedSearchCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs smart search with automatic optimization (command pattern)
    /// </summary>
    Task<SearchResultCollection> SmartSearchAsync(SmartSearchCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quick search for immediate results (synchronous)
    /// </summary>
    SearchResultCollection QuickSearch(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        bool caseSensitive = false);

    // UTILITY API

    /// <summary>
    /// Validates search criteria
    /// </summary>
    Task<Result> ValidateSearchCriteriaAsync(
        AdvancedSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of columns that can be searched
    /// </summary>
    IReadOnlyList<string> GetSearchableColumns(
        IEnumerable<IReadOnlyDictionary<string, object?>> data);

    /// <summary>
    /// Recommends suitable search modes for data
    /// </summary>
    IReadOnlyList<SearchMode> GetRecommendedSearchModes(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText);

    /// <summary>
    /// Highlights search matches in data grid
    /// </summary>
    Task<Result> HighlightSearchMatchesAsync(
        SearchResultCollection searchResults,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears search highlights
    /// </summary>
    Task<Result> ClearSearchHighlightsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Goes to next search match
    /// </summary>
    Task<Result> GoToNextMatchAsync(
        SearchResultCollection searchResults,
        int currentMatchIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Goes to previous search match
    /// </summary>
    Task<Result> GoToPreviousMatchAsync(
        SearchResultCollection searchResults,
        int currentMatchIndex,
        CancellationToken cancellationToken = default);
}

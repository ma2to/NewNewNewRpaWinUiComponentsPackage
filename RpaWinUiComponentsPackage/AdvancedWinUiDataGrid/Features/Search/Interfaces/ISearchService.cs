using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Interfaces;

/// <summary>
/// Interface pre search službu s comprehensive searching capabilities
/// Kombinuje základné, pokročilé a smart search funkcie
/// </summary>
internal interface ISearchService
{
    // COMMAND PATTERN API

    /// <summary>
    /// Vykoná základné vyhľadávanie (command pattern)
    /// </summary>
    Task<SearchResultCollection> SearchAsync(SearchCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vykoná pokročilé vyhľadávanie s komplexnými kritériami (command pattern)
    /// </summary>
    Task<SearchResultCollection> AdvancedSearchAsync(AdvancedSearchCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vykoná smart search s automatickou optimalizáciou (command pattern)
    /// </summary>
    Task<SearchResultCollection> SmartSearchAsync(SmartSearchCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quick search pre okamžité výsledky (synchronous)
    /// </summary>
    SearchResultCollection QuickSearch(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        bool caseSensitive = false);

    // UTILITY API

    /// <summary>
    /// Validuje search kritériá
    /// </summary>
    Task<Result> ValidateSearchCriteriaAsync(
        AdvancedSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Získa zoznam stĺpcov, v ktorých možno vyhľadávať
    /// </summary>
    IReadOnlyList<string> GetSearchableColumns(
        IEnumerable<IReadOnlyDictionary<string, object?>> data);

    /// <summary>
    /// Odporúči vhodné search modes pre dáta
    /// </summary>
    IReadOnlyList<SearchMode> GetRecommendedSearchModes(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText);

    /// <summary>
    /// Zvýrazní search matches v data grid
    /// </summary>
    Task<Result> HighlightSearchMatchesAsync(
        SearchResultCollection searchResults,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vymaže search highlights
    /// </summary>
    Task<Result> ClearSearchHighlightsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prejde na ďalší search match
    /// </summary>
    Task<Result> GoToNextMatchAsync(
        SearchResultCollection searchResults,
        int currentMatchIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prejde na predchádzajúci search match
    /// </summary>
    Task<Result> GoToPreviousMatchAsync(
        SearchResultCollection searchResults,
        int currentMatchIndex,
        CancellationToken cancellationToken = default);
}

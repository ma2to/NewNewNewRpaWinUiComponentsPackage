using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Search;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Search;

/// <summary>
/// Internal implementation of DataGrid search operations.
/// Delegates to internal search service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridSearch : IDataGridSearch
{
    private readonly ILogger<DataGridSearch>? _logger;
    private readonly ISearchService _searchService;
    private readonly IRowStore _rowStore;

    public DataGridSearch(
        ISearchService searchService,
        IRowStore rowStore,
        ILogger<DataGridSearch>? logger = null)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _logger = logger;
    }

    public async Task<PublicSearchResult> SearchAsync(string searchText, bool caseSensitive = false, bool wholeWord = false, string[]? columnNames = null, PublicSearchScope searchScope = PublicSearchScope.AllData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Searching for '{SearchText}' via Search module (caseSensitive: {CaseSensitive}, wholeWord: {WholeWord}, scope: {Scope})",
                searchText, caseSensitive, wholeWord, searchScope);

            // Get all data from row store
            var allData = await _rowStore.GetAllRowsAsync(cancellationToken);

            // Create search command with scope
            var searchCommand = new SearchCommand
            {
                Data = allData,
                SearchText = searchText,
                TargetColumns = columnNames,
                CaseSensitive = caseSensitive,
                Scope = searchScope.ToInternal(),
                CancellationToken = cancellationToken
            };

            // Execute search
            var internalResult = await _searchService.SearchAsync(searchCommand, cancellationToken);

            // Map to public result
            return new PublicSearchResult
            {
                MatchCount = internalResult.TotalMatchesFound,
                MatchedRowIndices = internalResult.Results.Select(r => r.RowIndex).Distinct().ToList(),
                MatchedCells = internalResult.Results.Select(r => new PublicCellPosition
                {
                    RowIndex = r.RowIndex,
                    ColumnName = r.ColumnName,
                    CellValue = r.Value
                }).ToList(),
                SearchText = searchText,
                CaseSensitive = caseSensitive,
                WholeWord = wholeWord,
                SearchDuration = internalResult.SearchTime,
                Scope = searchScope
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Search failed in Search module");
            throw;
        }
    }

    public async Task<PublicResult> HighlightSearchMatchesAsync(string searchText, bool caseSensitive = false)
    {
        try
        {
            _logger?.LogInformation("Highlighting search matches for '{SearchText}' via Search module", searchText);

            // TODO: Need SearchResultCollection to pass to HighlightSearchMatchesAsync
            await Task.CompletedTask;
            return PublicResult.Failure("HighlightSearchMatches not yet fully implemented - requires search results");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "HighlightSearchMatches failed in Search module");
            throw;
        }
    }

    public async Task<PublicResult> ClearSearchHighlightsAsync()
    {
        try
        {
            _logger?.LogInformation("Clearing search highlights via Search module");

            var internalResult = await _searchService.ClearSearchHighlightsAsync();
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearSearchHighlights failed in Search module");
            throw;
        }
    }

    public async Task<PublicResult<int>> GoToNextMatchAsync()
    {
        try
        {
            _logger?.LogInformation("Going to next search match via Search module");

            // TODO: Need SearchResultCollection and currentMatchIndex to pass to GoToNextMatchAsync
            await Task.CompletedTask;
            return PublicResult<int>.Failure("GoToNextMatch not yet fully implemented - requires search results");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GoToNextMatch failed in Search module");
            throw;
        }
    }

    public async Task<PublicResult<int>> GoToPreviousMatchAsync()
    {
        try
        {
            _logger?.LogInformation("Going to previous search match via Search module");

            // TODO: Need SearchResultCollection and currentMatchIndex to pass to GoToPreviousMatchAsync
            await Task.CompletedTask;
            return PublicResult<int>.Failure("GoToPreviousMatch not yet fully implemented - requires search results");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GoToPreviousMatch failed in Search module");
            throw;
        }
    }

    public PublicSearchStatistics GetSearchStatistics()
    {
        try
        {
            // TODO: Implement GetSearchStatistics in ISearchService
            return new PublicSearchStatistics
            {
                TotalMatches = 0,
                CurrentMatchPosition = 0,
                CurrentSearchText = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetSearchStatistics failed in Search module");
            throw;
        }
    }
}

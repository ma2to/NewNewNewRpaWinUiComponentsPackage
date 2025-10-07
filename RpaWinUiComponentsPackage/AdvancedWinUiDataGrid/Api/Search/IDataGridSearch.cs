using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Search;

/// <summary>
/// Public interface for DataGrid search operations.
/// Provides comprehensive search functionality including text search, highlighting, and navigation.
/// </summary>
public interface IDataGridSearch
{
    /// <summary>
    /// Searches for text across all columns or specific columns.
    /// </summary>
    /// <param name="searchText">Text to search for</param>
    /// <param name="caseSensitive">Whether search should be case sensitive</param>
    /// <param name="wholeWord">Whether to match whole words only</param>
    /// <param name="columnNames">Optional column names to search in (null = all columns)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Search result with matched rows and cells</returns>
    Task<Api.Models.PublicSearchResult> SearchAsync(string searchText, bool caseSensitive = false, bool wholeWord = false, string[]? columnNames = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Highlights all search matches in the grid.
    /// </summary>
    /// <param name="searchText">Text to highlight</param>
    /// <param name="caseSensitive">Whether highlighting should be case sensitive</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> HighlightSearchMatchesAsync(string searchText, bool caseSensitive = false);

    /// <summary>
    /// Clears all search highlights from the grid.
    /// </summary>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearSearchHighlightsAsync();

    /// <summary>
    /// Navigates to the next search match.
    /// </summary>
    /// <returns>Result with index of next match</returns>
    Task<PublicResult<int>> GoToNextMatchAsync();

    /// <summary>
    /// Navigates to the previous search match.
    /// </summary>
    /// <returns>Result with index of previous match</returns>
    Task<PublicResult<int>> GoToPreviousMatchAsync();

    /// <summary>
    /// Gets current search statistics.
    /// </summary>
    /// <returns>Search statistics including match count and current position</returns>
    PublicSearchStatistics GetSearchStatistics();
}

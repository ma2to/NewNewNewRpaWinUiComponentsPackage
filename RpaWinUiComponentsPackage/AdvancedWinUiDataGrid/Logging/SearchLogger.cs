using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Logging;

/// <summary>
/// Internal specialized logger for search operations
/// Provides detailed logging and performance tracking for search scenarios
/// </summary>
internal sealed class SearchLogger
{
    private readonly ILogger<SearchLogger> _logger;

    public SearchLogger(ILogger<SearchLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log search operation start
    /// </summary>
    public void LogSearchStart(Guid operationId, string searchTerm, int totalRows, bool caseSensitive, bool wholeWord)
    {
        _logger.LogInformation("Search operation started [{OperationId}]: Term={SearchTerm}, TotalRows={TotalRows}, CaseSensitive={CaseSensitive}, WholeWord={WholeWord}",
            operationId, searchTerm, totalRows, caseSensitive, wholeWord);
    }

    /// <summary>
    /// Log search results
    /// </summary>
    public void LogSearchResults(Guid operationId, int matchCount, int totalSearched, TimeSpan duration)
    {
        _logger.LogInformation("Search completed [{OperationId}]: Matches={MatchCount}, Searched={TotalSearched}, Duration={Duration}ms",
            operationId, matchCount, totalSearched, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Log search performance metrics
    /// </summary>
    public void LogPerformanceMetrics(Guid operationId, double rowsPerSecond, long memoryUsed)
    {
        _logger.LogInformation("Search performance [{OperationId}]: Rate={RowsPerSecond:F2} rows/sec, Memory={MemoryUsed:N0} bytes",
            operationId, rowsPerSecond, memoryUsed);
    }

    /// <summary>
    /// Log critical search error
    /// </summary>
    public void LogCriticalError(Guid operationId, Exception exception, string context)
    {
        _logger.LogCritical(exception, "Critical search error [{OperationId}]: Context={Context}",
            operationId, context);
    }
}

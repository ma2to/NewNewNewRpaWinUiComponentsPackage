namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Search scope definition for targeting specific data subsets
/// </summary>
public enum PublicSearchScope
{
    /// <summary>Search all data</summary>
    AllData = 0,
    /// <summary>Search only visible data</summary>
    VisibleData = 1,
    /// <summary>Search only selected data</summary>
    SelectedData = 2,
    /// <summary>Search only filtered data</summary>
    FilteredData = 3
}

/// <summary>
/// Search mode definition for different search strategies
/// </summary>
public enum PublicSearchMode
{
    /// <summary>Contains substring search</summary>
    Contains = 0,
    /// <summary>Exact match search</summary>
    Exact = 1,
    /// <summary>Starts with prefix search</summary>
    StartsWith = 2,
    /// <summary>Ends with suffix search</summary>
    EndsWith = 3,
    /// <summary>Regular expression search</summary>
    Regex = 4,
    /// <summary>Fuzzy approximate matching</summary>
    Fuzzy = 5
}

/// <summary>
/// Search result ranking criteria
/// </summary>
public enum PublicSearchRanking
{
    /// <summary>No ranking applied</summary>
    None = 0,
    /// <summary>Rank by relevance score</summary>
    Relevance = 1,
    /// <summary>Rank by position in data</summary>
    Position = 2,
    /// <summary>Rank by match frequency</summary>
    Frequency = 3
}

/// <summary>
/// Progress information for search operations
/// </summary>
/// <param name="ProcessedRows">Number of processed rows</param>
/// <param name="TotalRows">Total number of rows to search</param>
/// <param name="ElapsedTime">Time elapsed since operation start</param>
/// <param name="CurrentOperation">Description of current operation</param>
/// <param name="FoundMatches">Number of matches found so far</param>
/// <param name="CurrentColumn">Column currently being searched</param>
public record PublicSearchProgress(
    int ProcessedRows,
    int TotalRows,
    TimeSpan ElapsedTime,
    string CurrentOperation = "",
    int FoundMatches = 0,
    string? CurrentColumn = null
)
{
    /// <summary>Calculated completion percentage (0-100)</summary>
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;

    /// <summary>Estimated time remaining based on current progress</summary>
    public TimeSpan? EstimatedTimeRemaining => ProcessedRows > 0 && TotalRows > ProcessedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ProcessedRows) / ProcessedRows)
        : null;

    public PublicSearchProgress() : this(0, 0, TimeSpan.Zero, "", 0, null) { }
}

/// <summary>
/// Enhanced search match with comprehensive match information (represents a single match)
/// </summary>
/// <param name="RowIndex">Row index where match was found</param>
/// <param name="ColumnName">Column name where match was found</param>
/// <param name="Value">Matched cell value</param>
/// <param name="MatchedText">The text that matched</param>
/// <param name="IsExactMatch">Whether this is an exact match</param>
/// <param name="MatchScore">Match quality score (0.0-1.0)</param>
/// <param name="RelevanceScore">Relevance score for ranking (0.0-1.0)</param>
/// <param name="CellAddress">Cell address (e.g., "A1")</param>
public record PublicSearchMatch(
    int RowIndex,
    string ColumnName,
    object? Value,
    string? MatchedText,
    bool IsExactMatch = false,
    double MatchScore = 1.0,
    double RelevanceScore = 1.0,
    string? CellAddress = null
)
{
    public PublicSearchMatch() : this(0, "", null, null, false, 1.0, 1.0, null) { }
}

/// <summary>
/// Command for basic search operation
/// </summary>
/// <param name="Data">Data to search</param>
/// <param name="SearchText">Text to search for</param>
/// <param name="TargetColumns">Columns to search in (null = all columns)</param>
/// <param name="CaseSensitive">Case-sensitive search</param>
/// <param name="Scope">Search scope</param>
/// <param name="Timeout">Operation timeout</param>
/// <param name="Progress">Progress reporting callback</param>
/// <param name="CorrelationId">Operation correlation ID for logging</param>
public record SearchDataCommand(
    IEnumerable<IReadOnlyDictionary<string, object?>> Data,
    string SearchText,
    string[]? TargetColumns = null,
    bool CaseSensitive = false,
    PublicSearchScope Scope = PublicSearchScope.AllData,
    TimeSpan? Timeout = null,
    IProgress<PublicSearchProgress>? Progress = null,
    Guid? CorrelationId = null
)
{
    public SearchDataCommand() : this(Array.Empty<IReadOnlyDictionary<string, object?>>(), "") { }
}

/// <summary>
/// Advanced search criteria with enterprise features
/// </summary>
public class PublicAdvancedSearchCriteria
{
    /// <summary>Text to search for</summary>
    public string SearchText { get; set; } = string.Empty;

    /// <summary>Target columns (null = all columns)</summary>
    public string[]? TargetColumns { get; set; }

    /// <summary>Use regular expression matching</summary>
    public bool UseRegex { get; set; } = false;

    /// <summary>Case-sensitive search</summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>Match whole words only</summary>
    public bool WholeWordOnly { get; set; } = false;

    /// <summary>Search scope</summary>
    public PublicSearchScope Scope { get; set; } = PublicSearchScope.AllData;

    /// <summary>Search mode</summary>
    public PublicSearchMode Mode { get; set; } = PublicSearchMode.Contains;

    /// <summary>Result ranking method</summary>
    public PublicSearchRanking Ranking { get; set; } = PublicSearchRanking.None;

    /// <summary>Maximum number of matches to return</summary>
    public int? MaxMatches { get; set; }

    /// <summary>Operation timeout</summary>
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Highlight matches in results</summary>
    public bool HighlightMatches { get; set; } = true;

    /// <summary>Include hidden columns in search</summary>
    public bool IncludeHiddenColumns { get; set; } = false;

    /// <summary>Fuzzy search threshold (0.0-1.0)</summary>
    public double? FuzzyThreshold { get; set; }
}

/// <summary>
/// Command for advanced search operation
/// </summary>
/// <param name="Data">Data to search</param>
/// <param name="SearchCriteria">Advanced search criteria</param>
/// <param name="EnableParallelProcessing">Enable parallel processing</param>
/// <param name="UseSmartRanking">Use smart ranking</param>
/// <param name="Progress">Progress reporting callback</param>
/// <param name="CorrelationId">Operation correlation ID for logging</param>
public record AdvancedSearchDataCommand(
    IEnumerable<IReadOnlyDictionary<string, object?>> Data,
    PublicAdvancedSearchCriteria SearchCriteria,
    bool EnableParallelProcessing = true,
    bool UseSmartRanking = true,
    IProgress<PublicSearchProgress>? Progress = null,
    Guid? CorrelationId = null
)
{
    public AdvancedSearchDataCommand() : this(Array.Empty<IReadOnlyDictionary<string, object?>>(), new PublicAdvancedSearchCriteria()) { }
}

/// <summary>
/// Command for smart search operation with automatic optimization
/// </summary>
/// <param name="Data">Data to search</param>
/// <param name="SearchText">Text to search for</param>
/// <param name="TargetColumns">Columns to search in (null = all columns)</param>
/// <param name="CaseSensitive">Case-sensitive search</param>
/// <param name="AutoOptimize">Automatically optimize search strategy</param>
/// <param name="Progress">Progress reporting callback</param>
/// <param name="CorrelationId">Operation correlation ID for logging</param>
public record SmartSearchDataCommand(
    IEnumerable<IReadOnlyDictionary<string, object?>> Data,
    string SearchText,
    string[]? TargetColumns = null,
    bool CaseSensitive = false,
    bool AutoOptimize = true,
    IProgress<PublicSearchProgress>? Progress = null,
    Guid? CorrelationId = null
)
{
    public SmartSearchDataCommand() : this(Array.Empty<IReadOnlyDictionary<string, object?>>(), "") { }
}

/// <summary>
/// Result of search operation with statistics
/// </summary>
/// <param name="IsSuccess">Whether operation succeeded</param>
/// <param name="Results">Found search matches</param>
/// <param name="TotalMatchesFound">Total number of matches found</param>
/// <param name="TotalRowsSearched">Total number of rows searched</param>
/// <param name="Duration">Total operation duration</param>
/// <param name="UsedSearchMode">Search mode that was used</param>
/// <param name="UsedParallelProcessing">Whether parallel processing was used</param>
/// <param name="ErrorMessages">Error messages if operation failed</param>
public record SearchDataResult(
    bool IsSuccess,
    IReadOnlyList<PublicSearchMatch> Results,
    int TotalMatchesFound,
    int TotalRowsSearched,
    TimeSpan Duration,
    PublicSearchMode UsedSearchMode = PublicSearchMode.Contains,
    bool UsedParallelProcessing = false,
    IReadOnlyList<string>? ErrorMessages = null
)
{
    public SearchDataResult() : this(false, Array.Empty<PublicSearchMatch>(), 0, 0, TimeSpan.Zero, PublicSearchMode.Contains, false, null) { }
}

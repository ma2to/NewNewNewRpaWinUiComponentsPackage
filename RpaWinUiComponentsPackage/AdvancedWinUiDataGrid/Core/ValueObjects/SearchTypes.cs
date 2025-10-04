namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

#region Enums

/// <summary>
/// Search scope definition for targeting specific data subsets
/// </summary>
internal enum SearchScope
{
    AllData,
    VisibleData,
    SelectedData,
    FilteredData
}

/// <summary>
/// Search mode definition for different search strategies
/// </summary>
internal enum SearchMode
{
    Contains,
    Exact,
    StartsWith,
    EndsWith,
    Regex,
    Fuzzy
}

/// <summary>
/// Search result ranking criteria
/// </summary>
internal enum SearchRanking
{
    None,
    Relevance,
    Position,
    Frequency
}

#endregion

#region Progress & Context Types

/// <summary>
/// Search operation progress reporting
/// Consistent structure with ValidationProgress and ExportProgress
/// </summary>
internal sealed record SearchProgress
{
    internal int ProcessedRows { get; init; }
    internal int TotalRows { get; init; }
    internal double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    internal TimeSpan ElapsedTime { get; init; }
    internal string CurrentOperation { get; init; } = string.Empty;
    internal int FoundMatches { get; init; }
    internal string? CurrentColumn { get; init; }

    internal TimeSpan? EstimatedTimeRemaining => ProcessedRows > 0 && TotalRows > ProcessedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ProcessedRows) / ProcessedRows)
        : null;

    public SearchProgress() : this(0, 0, TimeSpan.Zero, "", 0, null) { }

    public SearchProgress(int processedRows, int totalRows, TimeSpan elapsedTime, string currentOperation, int foundMatches, string? currentColumn)
    {
        ProcessedRows = processedRows;
        TotalRows = totalRows;
        ElapsedTime = elapsedTime;
        CurrentOperation = currentOperation;
        FoundMatches = foundMatches;
        CurrentColumn = currentColumn;
    }
}

/// <summary>
/// Search execution context with DI support
/// Hybrid DI: Provides services for custom search functions
/// </summary>
internal sealed record SearchContext
{
    internal IServiceProvider? ServiceProvider { get; init; }
    internal IReadOnlyDictionary<string, object?> SearchParameters { get; init; } = new Dictionary<string, object?>();
    internal CancellationToken CancellationToken { get; init; } = default;
    internal DateTime ExecutionTime { get; init; } = DateTime.UtcNow;
    internal SearchScope Scope { get; init; } = SearchScope.AllData;
}

#endregion

#region Core Search Definitions

/// <summary>
/// Enhanced search criteria with enterprise features
/// Immutable search criteria with comprehensive options
/// </summary>
internal sealed record AdvancedSearchCriteria
{
    internal string SearchText { get; init; } = string.Empty;
    internal string[]? TargetColumns { get; init; }
    internal bool UseRegex { get; init; } = false;
    internal bool CaseSensitive { get; init; } = false;
    internal bool WholeWordOnly { get; init; } = false;
    internal SearchScope Scope { get; init; } = SearchScope.AllData;
    internal SearchMode Mode { get; init; } = SearchMode.Contains;
    internal SearchRanking Ranking { get; init; } = SearchRanking.None;
    internal int? MaxMatches { get; init; }
    internal TimeSpan? Timeout { get; init; } = TimeSpan.FromSeconds(5);
    internal bool HighlightMatches { get; init; } = true;
    internal bool IncludeHiddenColumns { get; init; } = false;
    internal bool SearchFromCurrentPosition { get; init; } = false;
    internal bool WrapAround { get; init; } = true;
    internal bool ShowProgress { get; init; } = true;
    internal double? FuzzyThreshold { get; init; }

    internal static AdvancedSearchCriteria Create(
        string searchText,
        string[]? targetColumns = null,
        bool caseSensitive = false) =>
        new()
        {
            SearchText = searchText,
            TargetColumns = targetColumns,
            CaseSensitive = caseSensitive
        };

    internal static AdvancedSearchCriteria WithRegex(
        string searchPattern,
        string[]? targetColumns = null,
        bool caseSensitive = false) =>
        new()
        {
            SearchText = searchPattern,
            TargetColumns = targetColumns,
            UseRegex = true,
            CaseSensitive = caseSensitive,
            Mode = SearchMode.Regex
        };

    internal static AdvancedSearchCriteria WithFuzzySearch(
        string searchText,
        double fuzzyThreshold = 0.8,
        string[]? targetColumns = null) =>
        new()
        {
            SearchText = searchText,
            TargetColumns = targetColumns,
            Mode = SearchMode.Fuzzy,
            FuzzyThreshold = fuzzyThreshold
        };

    internal static AdvancedSearchCriteria WithScope(
        string searchText,
        SearchScope scope,
        string[]? targetColumns = null) =>
        new()
        {
            SearchText = searchText,
            TargetColumns = targetColumns,
            Scope = scope
        };
}

/// <summary>
/// Enhanced search result with comprehensive match information
/// </summary>
internal sealed record SearchResult
{
    internal int RowIndex { get; init; }
    internal string ColumnName { get; init; } = string.Empty;
    internal object? Value { get; init; }
    internal object? OriginalValue { get; init; }
    internal string? MatchedText { get; init; }
    internal int MatchStartIndex { get; init; }
    internal int MatchLength { get; init; }
    internal bool IsExactMatch { get; init; }
    internal double MatchScore { get; init; } = 1.0;
    internal double RelevanceScore { get; init; } = 1.0;
    internal string? CellAddress { get; init; }
    internal bool IsHighlighted { get; init; }
    internal SearchMode UsedSearchMode { get; init; } = SearchMode.Contains;

    internal static SearchResult Create(
        int rowIndex,
        string columnName,
        object? value,
        string? matchedText = null) =>
        new()
        {
            RowIndex = rowIndex,
            ColumnName = columnName,
            Value = value,
            OriginalValue = value,
            MatchedText = matchedText,
            CellAddress = $"{columnName}{rowIndex + 1}",
            MatchScore = 1.0,
            RelevanceScore = 1.0
        };

    internal static SearchResult CreateEnhanced(
        int rowIndex,
        string columnName,
        object? value,
        string? matchedText,
        bool isExactMatch,
        double matchScore,
        double relevanceScore,
        SearchMode searchMode,
        bool isHighlighted = false) =>
        new()
        {
            RowIndex = rowIndex,
            ColumnName = columnName,
            Value = value,
            OriginalValue = value,
            MatchedText = matchedText,
            IsExactMatch = isExactMatch,
            MatchScore = matchScore,
            RelevanceScore = relevanceScore,
            CellAddress = $"{columnName}{rowIndex + 1}",
            IsHighlighted = isHighlighted,
            UsedSearchMode = searchMode
        };
}

#endregion

#region Statistics

/// <summary>
/// Search execution statistics
/// </summary>
internal sealed record SearchStatistics
{
    internal int TotalSearchOperations { get; init; }
    internal int RegexOperations { get; init; }
    internal int FuzzyOperations { get; init; }
    internal TimeSpan AverageSearchTime { get; init; }
    internal bool UsedParallelProcessing { get; init; }
    internal bool UsedCaching { get; init; }
    internal int CacheHits { get; init; }
    internal int ObjectPoolHits { get; init; }
    internal double AverageRelevanceScore { get; init; }
}

#endregion

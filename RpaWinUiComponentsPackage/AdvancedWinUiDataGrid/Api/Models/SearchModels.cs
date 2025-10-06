namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

/// <summary>
/// Public search result containing matched rows and cells
/// </summary>
public sealed class PublicSearchResult
{
    /// <summary>
    /// Total number of matches found
    /// </summary>
    public int MatchCount { get; init; }

    /// <summary>
    /// Row indices containing matches
    /// </summary>
    public IReadOnlyList<int> MatchedRowIndices { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Matched cell positions (row index, column name)
    /// </summary>
    public IReadOnlyList<PublicCellPosition> MatchedCells { get; init; } = Array.Empty<PublicCellPosition>();

    /// <summary>
    /// Search text used
    /// </summary>
    public string SearchText { get; init; } = string.Empty;

    /// <summary>
    /// Whether search was case sensitive
    /// </summary>
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// Whether whole word matching was used
    /// </summary>
    public bool WholeWord { get; init; }

    /// <summary>
    /// Time taken for search operation
    /// </summary>
    public TimeSpan SearchDuration { get; init; }
}

/// <summary>
/// Public cell position identifier
/// </summary>
public sealed class PublicCellPosition
{
    /// <summary>
    /// Row index
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Column name
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Cell value
    /// </summary>
    public object? CellValue { get; init; }
}

/// <summary>
/// Public search statistics
/// </summary>
public sealed class PublicSearchStatistics
{
    /// <summary>
    /// Total number of matches
    /// </summary>
    public int TotalMatches { get; init; }

    /// <summary>
    /// Current match position (1-based)
    /// </summary>
    public int CurrentMatchPosition { get; init; }

    /// <summary>
    /// Whether there are any matches
    /// </summary>
    public bool HasMatches => TotalMatches > 0;

    /// <summary>
    /// Current search text
    /// </summary>
    public string CurrentSearchText { get; init; } = string.Empty;
}

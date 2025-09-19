using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.API;

/// <summary>
/// PUBLIC API: Filter operators for data filtering
/// </summary>
public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    IsNull,
    IsNotNull,
    IsEmpty,
    IsNotEmpty,
    Regex
}

/// <summary>
/// PUBLIC API: Logic operators for filter combinations
/// </summary>
public enum FilterLogicOperator
{
    And,
    Or
}

/// <summary>
/// PUBLIC API: Search scope definition
/// </summary>
public enum SearchScope
{
    AllData,
    VisibleData,
    SelectedData
}

/// <summary>
/// PUBLIC API: Filter definition for complex filtering
/// </summary>
public sealed record FilterDefinition
{
    public string? ColumnName { get; init; }
    public FilterOperator Operator { get; init; }
    public object? Value { get; init; }
    public object? SecondValue { get; init; }
    public FilterLogicOperator LogicOperator { get; init; } = FilterLogicOperator.And;
    public string? FilterName { get; init; }

    public static FilterDefinition Equals(string columnName, object value) =>
        new() { ColumnName = columnName, Operator = FilterOperator.Equals, Value = value };

    public static FilterDefinition Contains(string columnName, string value) =>
        new() { ColumnName = columnName, Operator = FilterOperator.Contains, Value = value };

    public static FilterDefinition GreaterThan(string columnName, object value) =>
        new() { ColumnName = columnName, Operator = FilterOperator.GreaterThan, Value = value };

    public static FilterDefinition Regex(string columnName, string pattern) =>
        new() { ColumnName = columnName, Operator = FilterOperator.Regex, Value = pattern };
}

/// <summary>
/// PUBLIC API: Advanced search criteria
/// </summary>
public sealed record AdvancedSearchCriteria
{
    public string SearchText { get; init; } = string.Empty;
    public string[]? TargetColumns { get; init; }
    public bool UseRegex { get; init; }
    public bool CaseSensitive { get; init; }
    public SearchScope Scope { get; init; } = SearchScope.AllData;
    public int? MaxMatches { get; init; }
    public TimeSpan? Timeout { get; init; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// PUBLIC API: Search result information
/// </summary>
public sealed record SearchResult
{
    public int RowIndex { get; init; }
    public string ColumnName { get; init; } = string.Empty;
    public object? Value { get; init; }
    public string? MatchedText { get; init; }
    public int MatchStartIndex { get; init; }
    public int MatchLength { get; init; }

    public static SearchResult Create(int rowIndex, string columnName, object? value, string? matchedText = null) =>
        new()
        {
            RowIndex = rowIndex,
            ColumnName = columnName,
            Value = value,
            MatchedText = matchedText
        };
}

/// <summary>
/// PUBLIC API: Filter result with statistics
/// </summary>
public sealed record FilterResult
{
    public int TotalRowsProcessed { get; init; }
    public int MatchingRows { get; init; }
    public int FilteredOutRows { get; init; }
    public TimeSpan ProcessingTime { get; init; }
    public IReadOnlyList<int> MatchingRowIndices { get; init; } = Array.Empty<int>();

    public static FilterResult Create(int total, int matching, TimeSpan processingTime, IReadOnlyList<int>? matchingIndices = null) =>
        new()
        {
            TotalRowsProcessed = total,
            MatchingRows = matching,
            FilteredOutRows = total - matching,
            ProcessingTime = processingTime,
            MatchingRowIndices = matchingIndices ?? Array.Empty<int>()
        };
}

/// <summary>
/// PUBLIC API: Advanced filter with grouping support
/// ENTERPRISE: Support for complex filter grouping with parentheses logic
/// COMPLEX LOGIC: Enables filters like (Age > 18 AND Department = "IT") OR (Salary > 50000)
/// </summary>
public sealed record AdvancedFilter
{
    /// <summary>Column name to filter</summary>
    public string? ColumnName { get; init; }

    /// <summary>Filter operator</summary>
    public FilterOperator Operator { get; init; }

    /// <summary>Filter value</summary>
    public object? Value { get; init; }

    /// <summary>Second value for range operations</summary>
    public object? SecondValue { get; init; }

    /// <summary>Logic operator (AND/OR)</summary>
    public FilterLogicOperator LogicOperator { get; init; } = FilterLogicOperator.And;

    /// <summary>Start of filter group (opening parenthesis)</summary>
    public bool GroupStart { get; init; }

    /// <summary>End of filter group (closing parenthesis)</summary>
    public bool GroupEnd { get; init; }

    /// <summary>Filter name for identification</summary>
    public string? FilterName { get; init; }

    /// <summary>Create equals filter</summary>
    public static AdvancedFilter Equals(string columnName, object value, FilterLogicOperator logicOperator = FilterLogicOperator.And, bool groupStart = false, bool groupEnd = false) =>
        new()
        {
            ColumnName = columnName,
            Operator = FilterOperator.Equals,
            Value = value,
            LogicOperator = logicOperator,
            GroupStart = groupStart,
            GroupEnd = groupEnd
        };

    /// <summary>Create contains filter</summary>
    public static AdvancedFilter Contains(string columnName, string value, FilterLogicOperator logicOperator = FilterLogicOperator.And, bool groupStart = false, bool groupEnd = false) =>
        new()
        {
            ColumnName = columnName,
            Operator = FilterOperator.Contains,
            Value = value,
            LogicOperator = logicOperator,
            GroupStart = groupStart,
            GroupEnd = groupEnd
        };

    /// <summary>Create greater than filter</summary>
    public static AdvancedFilter GreaterThan(string columnName, object value, FilterLogicOperator logicOperator = FilterLogicOperator.And, bool groupStart = false, bool groupEnd = false) =>
        new()
        {
            ColumnName = columnName,
            Operator = FilterOperator.GreaterThan,
            Value = value,
            LogicOperator = logicOperator,
            GroupStart = groupStart,
            GroupEnd = groupEnd
        };

    /// <summary>Create regex filter</summary>
    public static AdvancedFilter Regex(string columnName, string pattern, FilterLogicOperator logicOperator = FilterLogicOperator.And, bool groupStart = false, bool groupEnd = false) =>
        new()
        {
            ColumnName = columnName,
            Operator = FilterOperator.Regex,
            Value = pattern,
            LogicOperator = logicOperator,
            GroupStart = groupStart,
            GroupEnd = groupEnd
        };
}
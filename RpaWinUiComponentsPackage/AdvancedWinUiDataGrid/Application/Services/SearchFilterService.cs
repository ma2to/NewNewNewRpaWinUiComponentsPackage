using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// INTERNAL SERVICE: Search and filter operations implementation
/// CLEAN ARCHITECTURE: Application layer service for search/filter operations
/// </summary>
internal sealed class SearchFilterService : ISearchFilterService
{
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var results = new List<SearchResult>();
            var dataList = data.ToList();
            var timeout = searchCriteria.Timeout ?? TimeSpan.FromSeconds(5);
            var stopwatch = Stopwatch.StartNew();

            for (int rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (stopwatch.Elapsed > timeout)
                    break;

                if (searchCriteria.MaxMatches.HasValue && results.Count >= searchCriteria.MaxMatches.Value)
                    break;

                var row = dataList[rowIndex];
                var columnsToSearch = searchCriteria.TargetColumns?.ToList() ?? row.Keys.ToList();

                foreach (var columnName in columnsToSearch)
                {
                    if (!row.TryGetValue(columnName, out var value) || value == null)
                        continue;

                    var valueStr = value.ToString() ?? string.Empty;

                    if (IsMatch(valueStr, searchCriteria.SearchText, searchCriteria.UseRegex, searchCriteria.CaseSensitive))
                    {
                        results.Add(SearchResult.Create(rowIndex, columnName, value, searchCriteria.SearchText));
                    }
                }
            }

            return (IReadOnlyList<SearchResult>)results;
        }, cancellationToken);
    }

    public IReadOnlyList<SearchResult> QuickSearch(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        bool caseSensitive = false)
    {
        var results = new List<SearchResult>();
        var dataList = data.ToList();

        for (int rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
        {
            var row = dataList[rowIndex];

            foreach (var kvp in row)
            {
                if (kvp.Value == null)
                    continue;

                var valueStr = kvp.Value.ToString() ?? string.Empty;

                if (IsMatch(valueStr, searchText, useRegex: false, caseSensitive))
                {
                    results.Add(SearchResult.Create(rowIndex, kvp.Key, kvp.Value, searchText));
                }
            }
        }

        return results;
    }

    public FilterResult ApplyFilter(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        FilterDefinition filter)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataList = data.ToList();
        var matchingIndices = new List<int>();

        for (int i = 0; i < dataList.Count; i++)
        {
            if (EvaluateFilter(dataList[i], filter))
            {
                matchingIndices.Add(i);
            }
        }

        stopwatch.Stop();
        return FilterResult.Create(dataList.Count, matchingIndices.Count, stopwatch.Elapsed, matchingIndices);
    }

    public FilterResult ApplyFilters(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<FilterDefinition> filters,
        FilterLogicOperator logicOperator = FilterLogicOperator.And)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataList = data.ToList();
        var matchingIndices = new List<int>();

        for (int i = 0; i < dataList.Count; i++)
        {
            bool matches = logicOperator == FilterLogicOperator.And;

            foreach (var filter in filters)
            {
                bool filterMatches = EvaluateFilter(dataList[i], filter);

                if (logicOperator == FilterLogicOperator.And)
                {
                    matches = matches && filterMatches;
                    if (!matches) break; // Early exit for AND
                }
                else // OR
                {
                    matches = matches || filterMatches;
                    if (matches) break; // Early exit for OR
                }
            }

            if (matches)
            {
                matchingIndices.Add(i);
            }
        }

        stopwatch.Stop();
        return FilterResult.Create(dataList.Count, matchingIndices.Count, stopwatch.Elapsed, matchingIndices);
    }

    public async Task<FilterResult> ApplyAdvancedFilterAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<AdvancedFilter> filters,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var dataList = data.ToList();
            var matchingIndices = new List<int>();

            for (int i = 0; i < dataList.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (EvaluateAdvancedFilters(dataList[i], filters))
                {
                    matchingIndices.Add(i);
                }
            }

            stopwatch.Stop();
            return FilterResult.Create(dataList.Count, matchingIndices.Count, stopwatch.Elapsed, matchingIndices);
        }, cancellationToken);
    }

    public bool ValidateFilter(FilterDefinition filter)
    {
        if (string.IsNullOrEmpty(filter.ColumnName))
            return false;

        if (filter.Operator == FilterOperator.Regex && filter.Value is string pattern)
        {
            try
            {
                _ = new Regex(pattern);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    public IReadOnlyList<string> GetFilterableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data)
    {
        var columns = new HashSet<string>();

        foreach (var row in data)
        {
            foreach (var key in row.Keys)
            {
                columns.Add(key);
            }
        }

        return columns.ToList();
    }

    private static bool IsMatch(string value, string searchText, bool useRegex, bool caseSensitive)
    {
        if (string.IsNullOrEmpty(searchText))
            return true;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (useRegex)
        {
            try
            {
                var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                return Regex.IsMatch(value, searchText, options);
            }
            catch
            {
                return false;
            }
        }

        return value.Contains(searchText, comparison);
    }

    private static bool EvaluateFilter(IReadOnlyDictionary<string, object?> row, FilterDefinition filter)
    {
        if (string.IsNullOrEmpty(filter.ColumnName) || !row.TryGetValue(filter.ColumnName, out var value))
            return false;

        return filter.Operator switch
        {
            FilterOperator.Equals => CompareValues(value, filter.Value) == 0,
            FilterOperator.NotEquals => CompareValues(value, filter.Value) != 0,
            FilterOperator.GreaterThan => CompareValues(value, filter.Value) > 0,
            FilterOperator.GreaterThanOrEqual => CompareValues(value, filter.Value) >= 0,
            FilterOperator.LessThan => CompareValues(value, filter.Value) < 0,
            FilterOperator.LessThanOrEqual => CompareValues(value, filter.Value) <= 0,
            FilterOperator.Contains => value?.ToString()?.Contains(filter.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true,
            FilterOperator.NotContains => value?.ToString()?.Contains(filter.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) != true,
            FilterOperator.StartsWith => value?.ToString()?.StartsWith(filter.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true,
            FilterOperator.EndsWith => value?.ToString()?.EndsWith(filter.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true,
            FilterOperator.IsNull => value == null,
            FilterOperator.IsNotNull => value != null,
            FilterOperator.IsEmpty => string.IsNullOrEmpty(value?.ToString()),
            FilterOperator.IsNotEmpty => !string.IsNullOrEmpty(value?.ToString()),
            FilterOperator.Regex => value != null && filter.Value is string pattern && Regex.IsMatch(value.ToString() ?? string.Empty, pattern, RegexOptions.IgnoreCase),
            _ => false
        };
    }

    private static bool EvaluateAdvancedFilters(IReadOnlyDictionary<string, object?> row, IReadOnlyList<AdvancedFilter> filters)
    {
        if (!filters.Any())
            return true;

        // Simple evaluation without full grouping support
        // For a complete implementation, would need to parse grouping and build expression tree
        bool result = true;
        FilterLogicOperator lastOperator = FilterLogicOperator.And;

        foreach (var filter in filters)
        {
            var filterDefinition = new FilterDefinition
            {
                ColumnName = filter.ColumnName,
                Operator = filter.Operator,
                Value = filter.Value,
                SecondValue = filter.SecondValue
            };

            bool filterResult = EvaluateFilter(row, filterDefinition);

            if (lastOperator == FilterLogicOperator.And)
                result = result && filterResult;
            else
                result = result || filterResult;

            lastOperator = filter.LogicOperator;
        }

        return result;
    }

    private static int CompareValues(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return 0;
        if (value1 == null) return -1;
        if (value2 == null) return 1;

        if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
        {
            try
            {
                // Try direct comparison first
                if (value1.GetType() == value2.GetType())
                    return comparable1.CompareTo(comparable2);

                // Try converting to common types
                if (double.TryParse(value1.ToString(), out var d1) && double.TryParse(value2.ToString(), out var d2))
                    return d1.CompareTo(d2);

                if (DateTime.TryParse(value1.ToString(), out var dt1) && DateTime.TryParse(value2.ToString(), out var dt2))
                    return dt1.CompareTo(dt2);
            }
            catch
            {
                // Fall back to string comparison
            }
        }

        return string.Compare(value1.ToString(), value2.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
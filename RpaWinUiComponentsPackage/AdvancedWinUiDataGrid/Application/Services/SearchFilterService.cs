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
            var dataList = data.ToList();
            var timeout = searchCriteria.Timeout ?? TimeSpan.FromSeconds(5);
            var stopwatch = Stopwatch.StartNew();
            var maxMatches = searchCriteria.MaxMatches ?? int.MaxValue;

            // LINQ OPTIMIZATION: Replace manual loops with functional pipeline
            var results = dataList
                .Select((row, rowIndex) => new { row, rowIndex })
                .TakeWhile(_ => !cancellationToken.IsCancellationRequested && stopwatch.Elapsed <= timeout)
                .SelectMany(x => (searchCriteria.TargetColumns ?? x.row.Keys)
                    .Where(columnName => x.row.TryGetValue(columnName, out var value) && value != null)
                    .Where(columnName => IsMatch(
                        x.row[columnName]?.ToString() ?? "",
                        searchCriteria.SearchText,
                        searchCriteria.UseRegex,
                        searchCriteria.CaseSensitive))
                    .Select(columnName => SearchResult.Create(
                        x.rowIndex,
                        columnName,
                        x.row[columnName],
                        searchCriteria.SearchText)))
                .Take(maxMatches)
                .ToList();

            return (IReadOnlyList<SearchResult>)results;
        }, cancellationToken);
    }

    public IReadOnlyList<SearchResult> QuickSearch(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        bool caseSensitive = false)
    {
        // LINQ OPTIMIZATION: Replace nested loops with functional pipeline
        var results = data
            .Select((row, rowIndex) => new { row, rowIndex })
            .SelectMany(x => x.row
                .Where(kvp => kvp.Value != null)
                .Where(kvp => IsMatch(kvp.Value.ToString() ?? string.Empty, searchText, useRegex: false, caseSensitive))
                .Select(kvp => SearchResult.Create(x.rowIndex, kvp.Key, kvp.Value, searchText)))
            .ToList();

        return (IReadOnlyList<SearchResult>)results;
    }

    public FilterResult ApplyFilter(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        FilterDefinition filter)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataList = data.ToList();

        // LINQ OPTIMIZATION: Replace manual loop with Where().Select() pipeline
        var matchingIndices = dataList
            .Select((row, index) => new { row, index })
            .Where(x => EvaluateFilter(x.row, filter))
            .Select(x => x.index)
            .ToList();

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

        // LINQ OPTIMIZATION: Replace manual loop with functional approach
        var matchingIndices = dataList
            .Select((row, index) => new { row, index })
            .Where(x => EvaluateFiltersForRow(x.row, filters, logicOperator))
            .Select(x => x.index)
            .ToList();

        stopwatch.Stop();
        return FilterResult.Create(dataList.Count, matchingIndices.Count, stopwatch.Elapsed, matchingIndices);
    }

    private static bool EvaluateFiltersForRow(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<FilterDefinition> filters,
        FilterLogicOperator logicOperator)
    {
        if (!filters.Any()) return true;

        // LINQ OPTIMIZATION: Use functional approach with early termination
        return logicOperator == FilterLogicOperator.And
            ? filters.All(filter => EvaluateFilter(row, filter))
            : filters.Any(filter => EvaluateFilter(row, filter));
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

            // LINQ OPTIMIZATION: Replace manual loop with functional pipeline and cancellation support
            var matchingIndices = dataList
                .Select((row, index) => new { row, index })
                .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                .Where(x =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return EvaluateAdvancedFilters(x.row, filters);
                })
                .Select(x => x.index)
                .ToList();

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
        // LINQ OPTIMIZATION: Replace nested loops with functional pipeline
        return data
            .SelectMany(row => row.Keys)
            .Distinct()
            .ToList();
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// INTERNAL SERVICE: Sort operations implementation
/// CLEAN ARCHITECTURE: Application layer service for sort operations
/// </summary>
internal sealed class SortService : ISortService
{
    public async Task<SortResult> SortAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        SortDirection direction = SortDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Sort(data, columnName, direction), cancellationToken);
    }

    public SortResult Sort(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        SortDirection direction = SortDirection.Ascending)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataList = data.ToList();

        if (direction == SortDirection.None)
        {
            stopwatch.Stop();
            return SortResult.Create(
                dataList,
                new[] { SortColumnConfiguration.Create(columnName, direction) },
                stopwatch.Elapsed);
        }

        var sortedData = direction == SortDirection.Ascending
            ? dataList.OrderBy(row => GetSortValue(row, columnName)).ToList()
            : dataList.OrderByDescending(row => GetSortValue(row, columnName)).ToList();

        stopwatch.Stop();

        return SortResult.Create(
            sortedData,
            new[] { SortColumnConfiguration.Create(columnName, direction) },
            stopwatch.Elapsed);
    }

    public async Task<SortResult> MultiSortAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<SortColumnConfiguration> sortConfigurations,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => MultiSort(data, sortConfigurations), cancellationToken);
    }

    public SortResult MultiSort(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<SortColumnConfiguration> sortConfigurations)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataList = data.ToList();

        if (!sortConfigurations.Any())
        {
            stopwatch.Stop();
            return SortResult.Create(dataList, Array.Empty<SortColumnConfiguration>(), stopwatch.Elapsed);
        }

        IOrderedEnumerable<IReadOnlyDictionary<string, object?>>? orderedData = null;

        var sortedConfigurations = sortConfigurations
            .Where(c => c.Direction != SortDirection.None)
            .OrderBy(c => c.Priority)
            .ToList();

        foreach (var config in sortedConfigurations)
        {
            if (orderedData == null)
            {
                orderedData = config.Direction == SortDirection.Ascending
                    ? dataList.OrderBy(row => GetSortValue(row, config.ColumnName))
                    : dataList.OrderByDescending(row => GetSortValue(row, config.ColumnName));
            }
            else
            {
                orderedData = config.Direction == SortDirection.Ascending
                    ? orderedData.ThenBy(row => GetSortValue(row, config.ColumnName))
                    : orderedData.ThenByDescending(row => GetSortValue(row, config.ColumnName));
            }
        }

        var sortedData = orderedData?.ToList() ?? dataList;
        stopwatch.Stop();

        return SortResult.Create(sortedData, sortedConfigurations, stopwatch.Elapsed);
    }

    public async Task<SortResult> SortWithConfigurationAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        SortConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => SortWithConfiguration(data, configuration), cancellationToken);
    }

    public SortResult SortWithConfiguration(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        SortConfiguration configuration)
    {
        var sortColumns = configuration.SortColumns;

        if (!configuration.AllowMultiColumnSort && sortColumns.Count > 1)
        {
            // Take only the first sort column
            sortColumns = sortColumns.Take(1).ToList();
        }
        else if (sortColumns.Count > configuration.MaxSortColumns)
        {
            // Take only the allowed number of sort columns
            sortColumns = sortColumns.Take(configuration.MaxSortColumns).ToList();
        }

        return MultiSort(data, sortColumns);
    }

    public bool CanSort(string columnName, IEnumerable<IReadOnlyDictionary<string, object?>> data)
    {
        if (string.IsNullOrEmpty(columnName))
            return false;

        // Check if column exists in any row
        return data.Any(row => row.ContainsKey(columnName));
    }

    public IReadOnlyList<string> GetSortableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data)
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

    private static object? GetSortValue(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (!row.TryGetValue(columnName, out var value))
            return null;

        // Handle null values - they should sort to the end
        if (value == null)
            return null;

        // For numeric types, try to parse strings as numbers for proper sorting
        if (value is string stringValue)
        {
            if (double.TryParse(stringValue, out var doubleValue))
                return doubleValue;

            if (DateTime.TryParse(stringValue, out var dateValue))
                return dateValue;

            // Return string as-is for string sorting
            return stringValue;
        }

        // Return value as-is if it's already a comparable type
        return value;
    }
}
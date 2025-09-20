using System;
using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// CORE ENUM: Sort direction enumeration
/// </summary>
internal enum SortDirection
{
    None = 0,
    Ascending = 1,
    Descending = 2
}

/// <summary>
/// CORE VALUE OBJECT: Sort column configuration
/// </summary>
internal sealed record SortColumnConfiguration
{
    public string ColumnName { get; init; } = string.Empty;
    public SortDirection Direction { get; init; } = SortDirection.None;
    public int Priority { get; init; } = 0;
    public bool IsPrimary { get; init; } = false;

    public static SortColumnConfiguration Create(string columnName, SortDirection direction, int priority = 0) =>
        new()
        {
            ColumnName = columnName,
            Direction = direction,
            Priority = priority,
            IsPrimary = priority == 0
        };
}

/// <summary>
/// CORE VALUE OBJECT: Sort result with metadata
/// </summary>
internal sealed record SortResult
{
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> SortedData { get; init; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
    public IReadOnlyList<SortColumnConfiguration> AppliedSorts { get; init; } = Array.Empty<SortColumnConfiguration>();
    public TimeSpan SortTime { get; init; }
    public int ProcessedRows { get; init; }

    public static SortResult Create(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> sortedData,
        IReadOnlyList<SortColumnConfiguration> appliedSorts,
        TimeSpan sortTime) =>
        new()
        {
            SortedData = sortedData,
            AppliedSorts = appliedSorts,
            SortTime = sortTime,
            ProcessedRows = sortedData.Count
        };

    public static SortResult Empty => new();
}

/// <summary>
/// CORE VALUE OBJECT: Sort configuration with multiple columns
/// </summary>
internal sealed class SortConfiguration
{
    private readonly List<SortColumnConfiguration> _sortColumns = new();

    public bool AllowMultiColumnSort { get; set; } = true;
    public int MaxSortColumns { get; set; } = 3;
    public SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;
    public bool CaseSensitiveStringSort { get; set; } = false;

    public IReadOnlyList<SortColumnConfiguration> SortColumns => _sortColumns.AsReadOnly();

    public void AddSort(string columnName, SortDirection direction, int priority = 0)
    {
        _sortColumns.Add(SortColumnConfiguration.Create(columnName, direction, priority));
    }

    public void RemoveSort(string columnName)
    {
        _sortColumns.RemoveAll(s => s.ColumnName == columnName);
    }

    public void ClearSorts()
    {
        _sortColumns.Clear();
    }
}
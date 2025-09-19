using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.API;

/// <summary>
/// PUBLIC API: Sort direction enumeration
/// </summary>
public enum SortDirection
{
    None = 0,
    Ascending = 1,
    Descending = 2
}

/// <summary>
/// PUBLIC API: Sort configuration for a column
/// </summary>
public sealed record SortColumnConfiguration
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
/// PUBLIC API: Sort result information
/// </summary>
public sealed record SortResult
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
/// PUBLIC API: Sort configuration for the entire grid
/// </summary>
public sealed class SortConfiguration
{
    private readonly List<SortColumnConfiguration> _sortColumns = new();

    /// <summary>Enable multi-column sorting</summary>
    public bool AllowMultiColumnSort { get; set; } = true;

    /// <summary>Maximum number of columns that can be sorted simultaneously</summary>
    public int MaxSortColumns { get; set; } = 3;

    /// <summary>Default sort direction when clicking unsorted column</summary>
    public SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>Case-sensitive string comparison</summary>
    public bool CaseSensitiveStringSort { get; set; } = false;

    /// <summary>Current sort columns in priority order</summary>
    public IReadOnlyList<SortColumnConfiguration> SortColumns => _sortColumns.AsReadOnly();

    /// <summary>Add or update sort for a column</summary>
    public SortConfiguration SetColumnSort(string columnName, SortDirection direction, bool clearOthers = false)
    {
        if (clearOthers || !AllowMultiColumnSort)
        {
            _sortColumns.Clear();
        }

        var existing = _sortColumns.FirstOrDefault(s => s.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            _sortColumns.Remove(existing);
        }

        if (direction != SortDirection.None)
        {
            var priority = _sortColumns.Count;
            if (priority >= MaxSortColumns)
            {
                _sortColumns.RemoveAt(_sortColumns.Count - 1);
                priority = MaxSortColumns - 1;
            }

            _sortColumns.Insert(0, SortColumnConfiguration.Create(columnName, direction, priority));

            for (int i = 0; i < _sortColumns.Count; i++)
            {
                _sortColumns[i] = _sortColumns[i] with { Priority = i, IsPrimary = i == 0 };
            }
        }

        return this;
    }

    /// <summary>Clear all sorts</summary>
    public SortConfiguration ClearAllSorts()
    {
        _sortColumns.Clear();
        return this;
    }

    /// <summary>Toggle sort direction for column</summary>
    public SortConfiguration ToggleColumnSort(string columnName)
    {
        var existing = _sortColumns.FirstOrDefault(s => s.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        var newDirection = existing?.Direction switch
        {
            null or SortDirection.None => DefaultSortDirection,
            SortDirection.Ascending => SortDirection.Descending,
            SortDirection.Descending => SortDirection.None,
            _ => DefaultSortDirection
        };

        return SetColumnSort(columnName, newDirection, !AllowMultiColumnSort);
    }

    /// <summary>Get sort direction for specific column</summary>
    public SortDirection GetColumnSortDirection(string columnName)
    {
        return _sortColumns.FirstOrDefault(s => s.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))?.Direction ?? SortDirection.None;
    }

    /// <summary>Create default sort configuration</summary>
    public static SortConfiguration Default => new();
}
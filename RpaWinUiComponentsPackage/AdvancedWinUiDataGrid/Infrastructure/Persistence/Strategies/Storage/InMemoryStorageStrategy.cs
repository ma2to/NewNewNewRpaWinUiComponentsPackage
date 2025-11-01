using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Strategies.Storage;

/// <summary>
/// ✅ IN-MEMORY STORAGE STRATEGY (FINAL VERSION)
/// NOW: InsertRowsAsync uses BULK SHIFT optimization (PART 1 fix).
/// EXTRACTED from InMemoryRowStore.cs (data storage logic only).
/// ARCHITECTURE: Uses __rowNumber as PRIMARY sort key, ULID as SECONDARY.
/// </summary>
internal sealed class InMemoryStorageStrategy : IStorageStrategy
{
    // ========== PRIVATE FIELDS (presun z InMemoryRowStore.cs) ==========

    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>> _rows = new();
    private readonly object _modificationLock = new();
    private readonly ILogger<InMemoryStorageStrategy>? _logger;

    // ORDERED ROW ACCESS - Cached sorted row keys for stable ordering
    private List<string>? _sortedRowKeys;
    private bool _sortedRowKeysInvalid = true;
    private readonly object _orderLock = new();

    // SORT CRITERIA
    private string? _sortColumnName;
    private SortDirection _sortDirection = SortDirection.None;

    // ========== CONSTRUCTOR ==========

    public InMemoryStorageStrategy(ILogger<InMemoryStorageStrategy>? logger)
    {
        _logger = logger;
    }

    // ========== METADATA ==========

    public string StrategyName => "InMemory";

    // ========== INSERT OPERATIONS (PART 1 BULK SHIFT - PRESUN Z INMEMORYROWSTORE) ==========

    /// <summary>
    /// ✅ UNIFIED (PART 1): Insert single row at index with __rowNumber shift.
    /// PRESUN Z InMemoryRowStore.cs:1444-1511
    /// </summary>
    public async Task<string> InsertRowAtIndexAsync(
        int index,
        IReadOnlyDictionary<string, object?>? rowData,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                var sortedKeys = GetSortedRowKeys();
                int newRowNumber = index + 1; // 0-based → 1-based

                // ✅ SHIFT: Rows with __rowNumber >= newRowNumber UP by 1
                for (int i = index; i < sortedKeys.Count; i++)
                {
                    var rowId = sortedKeys[i];
                    if (_rows.TryGetValue(rowId, out var row))
                    {
                        var mutableRow = new Dictionary<string, object?>(row);
                        mutableRow["__rowNumber"] = Convert.ToInt32(row["__rowNumber"]) + 1;
                        _rows[rowId] = mutableRow;
                    }
                }

                // ✅ INSERT: New row at exact index
                var newRowId = Ulid.NewUlid().ToString();
                var fullRowData = new Dictionary<string, object?>(rowData ?? new Dictionary<string, object?>());
                fullRowData["__rowId"] = newRowId;
                fullRowData["__rowNumber"] = newRowNumber;

                _rows.TryAdd(newRowId, fullRowData);
                InvalidateSortedRowKeysCache();

                _logger?.LogDebug("Inserted row {RowId} at index {Index} (__rowNumber={RowNumber})",
                    newRowId, index, newRowNumber);

                return newRowId;
            }
        }, ct);
    }

    /// <summary>
    /// ✅ UNIFIED + OPTIMIZED (PART 1 FIX): Insert multiple rows at index with BULK SHIFT.
    /// PRESUN Z InMemoryRowStore.cs:528-616 (AFTER PART 1 implementation)
    /// </summary>
    public async Task InsertRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex,
        CancellationToken ct)
    {
        var rowsList = rows.ToList();
        if (rowsList.Count == 0) return;

        await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                // ✅ STEP 1: BULK SHIFT existing rows UP by rowsList.Count
                int targetRowNumber = startIndex + 1;
                foreach (var kvp in _rows)
                {
                    var row = kvp.Value;
                    if (row.TryGetValue("__rowNumber", out var rnObj) && rnObj != null)
                    {
                        var currentRowNumber = Convert.ToInt32(rnObj);
                        if (currentRowNumber >= targetRowNumber)
                        {
                            var mutableRow = new Dictionary<string, object?>(row);
                            mutableRow["__rowNumber"] = currentRowNumber + rowsList.Count;
                            _rows[kvp.Key] = mutableRow;
                        }
                    }
                }

                // ✅ STEP 2: BULK INSERT new rows with sequential __rowNumber
                for (int i = 0; i < rowsList.Count; i++)
                {
                    var rowData = new Dictionary<string, object?>(rowsList[i]);
                    rowData["__rowNumber"] = startIndex + i + 1;

                    if (!rowData.ContainsKey("__rowId") || rowData["__rowId"] == null)
                    {
                        rowData["__rowId"] = Ulid.NewUlid().ToString();
                    }

                    _rows.TryAdd((string)rowData["__rowId"]!, rowData);
                }

                InvalidateSortedRowKeysCache();
            }
        }, ct);

        _logger?.LogInformation("InsertRowsAsync (bulk): Inserted {Count} rows at index {Index}",
            rowsList.Count, startIndex);
    }

    // ========== DELETE OPERATION (shift __rowNumber DOWN) ==========

    public async Task DeleteRowByIdAsync(string rowId, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                if (!_rows.TryRemove(rowId, out var deletedRow))
                {
                    _logger?.LogWarning("DeleteRowByIdAsync: Row {RowId} not found", rowId);
                    return;
                }

                // Get deleted row's __rowNumber
                if (deletedRow.TryGetValue("__rowNumber", out var rnObj) && rnObj != null)
                {
                    var deletedRowNumber = Convert.ToInt32(rnObj);

                    // Shift DOWN rows with __rowNumber > deletedRowNumber
                    foreach (var kvp in _rows)
                    {
                        var row = kvp.Value;
                        if (row.TryGetValue("__rowNumber", out var currentRnObj) && currentRnObj != null)
                        {
                            var currentRowNumber = Convert.ToInt32(currentRnObj);
                            if (currentRowNumber > deletedRowNumber)
                            {
                                var mutableRow = new Dictionary<string, object?>(row);
                                mutableRow["__rowNumber"] = currentRowNumber - 1;
                                _rows[kvp.Key] = mutableRow;
                            }
                        }
                    }
                }

                InvalidateSortedRowKeysCache();
                _logger?.LogDebug("DeleteRowByIdAsync: Deleted row {RowId}", rowId);
            }
        }, ct);
    }

    // ========== APPEND ROWS (sequential __rowNumber assignment) ==========

    public async Task<int> AddRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var rowsList = rows.ToList();
            if (rowsList.Count == 0) return 0;

            lock (_modificationLock)
            {
                // Get current max __rowNumber
                int maxRowNumber = 0;
                foreach (var row in _rows.Values)
                {
                    if (row.TryGetValue("__rowNumber", out var rnObj) && rnObj != null)
                    {
                        var rn = Convert.ToInt32(rnObj);
                        if (rn > maxRowNumber) maxRowNumber = rn;
                    }
                }

                // Append rows with sequential __rowNumber
                foreach (var row in rowsList)
                {
                    maxRowNumber++;

                    var rowData = new Dictionary<string, object?>(row);
                    rowData["__rowNumber"] = maxRowNumber;

                    if (!rowData.ContainsKey("__rowId") || rowData["__rowId"] == null)
                    {
                        rowData["__rowId"] = Ulid.NewUlid().ToString();
                    }

                    _rows.TryAdd((string)rowData["__rowId"]!, rowData);
                }

                InvalidateSortedRowKeysCache();
                _logger?.LogDebug("AddRowsAsync: Appended {Count} rows", rowsList.Count);
                return rowsList.Count;
            }
        }, ct);
    }

    // ========== CRUD METHODS ==========

    public async Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(
        string rowId,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            _rows.TryGetValue(rowId, out var row);
            return row;
        }, ct);
    }

    public async Task UpdateRowByIdAsync(
        string rowId,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken ct)
    {
        await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                if (_rows.ContainsKey(rowId))
                {
                    // Preserve __rowNumber if not in new data
                    if (!rowData.ContainsKey("__rowNumber") && _rows[rowId].TryGetValue("__rowNumber", out var rn))
                    {
                        var mutableData = new Dictionary<string, object?>(rowData);
                        mutableData["__rowNumber"] = rn;
                        _rows[rowId] = mutableData;
                    }
                    else
                    {
                        _rows[rowId] = rowData;
                    }

                    _logger?.LogDebug("UpdateRowByIdAsync: Updated row {RowId}", rowId);
                }
            }
        }, ct);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsRangeAsync(
        long startIndex,
        int count,
        bool onlyFiltered,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                var sortedKeys = GetSortedRowKeys();
                var result = new List<IReadOnlyDictionary<string, object?>>();

                for (long i = startIndex; i < Math.Min(startIndex + count, sortedKeys.Count); i++)
                {
                    if (_rows.TryGetValue(sortedKeys[(int)i], out var row))
                    {
                        result.Add(row);
                    }
                }

                return (IReadOnlyList<IReadOnlyDictionary<string, object?>>)result;
            }
        }, ct);
    }

    public async Task<long> GetRowCountAsync(bool onlyFiltered, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                var sortedKeys = GetSortedRowKeys();

                // ✅ PROBLEM 2 FIX (InMemoryStorageStrategy): Count only non-empty data rows
                // REASON: Auto-expanded empty rows should not be counted in display statistics
                // BEHAVIOR: Must match HybridRowStore behavior for consistency
                long count = 0;
                foreach (var rowId in sortedKeys)
                {
                    if (_rows.TryGetValue(rowId, out var row))
                    {
                        // Check if row has at least one non-empty data column
                        bool hasData = row.Any(kvp =>
                            !kvp.Key.StartsWith("__") &&  // Skip internal columns (__rowId, __rowNumber, etc.)
                            kvp.Value != null &&
                            !string.IsNullOrWhiteSpace(kvp.Value.ToString()));

                        if (hasData)
                        {
                            count++;
                        }
                    }
                }

                _logger?.LogDebug("✅ PROBLEM 2 FIX (InMemory): GetRowCountAsync returning {Count} non-empty rows (total in store: {Total})",
                    count, sortedKeys.Count);

                return count;
            }
        }, ct);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        bool onlyFiltered,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                var sortedKeys = GetSortedRowKeys();
                var result = new List<IReadOnlyDictionary<string, object?>>();

                foreach (var key in sortedKeys)
                {
                    if (_rows.TryGetValue(key, out var row))
                    {
                        result.Add(row);
                    }
                }

                return (IReadOnlyList<IReadOnlyDictionary<string, object?>>)result;
            }
        }, ct);
    }

    public async Task ReplaceAllRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct)
    {
        await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                _rows.Clear();

                int rowNumber = 0;
                foreach (var row in rows)
                {
                    rowNumber++;

                    var rowData = new Dictionary<string, object?>(row);
                    if (!rowData.ContainsKey("__rowNumber"))
                    {
                        rowData["__rowNumber"] = rowNumber;
                    }

                    if (!rowData.ContainsKey("__rowId") || rowData["__rowId"] == null)
                    {
                        rowData["__rowId"] = Ulid.NewUlid().ToString();
                    }

                    _rows.TryAdd((string)rowData["__rowId"]!, rowData);
                }

                InvalidateSortedRowKeysCache();
                _logger?.LogInformation("ReplaceAllRowsAsync: Replaced with {Count} rows", rowNumber);
            }
        }, ct);
    }

    // ========== SORT CRITERIA (renumber __rowNumber) ==========

    public void SetSortCriteria(string columnName, SortDirection direction)
    {
        lock (_modificationLock)
        {
            _sortColumnName = columnName;
            _sortDirection = direction;

            if (direction == SortDirection.None)
            {
                _logger?.LogDebug("SetSortCriteria: Cleared sort");
                return;
            }

            // Sort rows by column and renumber __rowNumber
            var sorted = _rows.Values
                .OrderBy(row =>
                {
                    if (row.TryGetValue(columnName, out var val))
                        return val?.ToString() ?? "";
                    return "";
                })
                .ToList();

            if (direction == SortDirection.Descending)
            {
                sorted.Reverse();
            }

            for (int i = 0; i < sorted.Count; i++)
            {
                var row = sorted[i];
                var mutableRow = new Dictionary<string, object?>(row);
                mutableRow["__rowNumber"] = i + 1;
                _rows[(string)row["__rowId"]!] = mutableRow;
            }

            InvalidateSortedRowKeysCache();
            _logger?.LogInformation("SetSortCriteria: Sorted by {Column} {Direction}, renumbered {Count} rows",
                columnName, direction, sorted.Count);
        }
    }

    public void ClearSortCriteria()
    {
        _sortColumnName = null;
        _sortDirection = SortDirection.None;
    }

    // ========== SEARCH ==========

    public async Task<IReadOnlyList<string>> SearchAsync(
        string searchText,
        string[]? targetColumns,
        bool caseSensitive,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var result = new List<string>();
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (var kvp in _rows)
            {
                var row = kvp.Value;
                foreach (var column in row)
                {
                    if (targetColumns != null && !targetColumns.Contains(column.Key))
                        continue;

                    var cellValue = column.Value?.ToString() ?? "";
                    if (cellValue.Contains(searchText, comparison))
                    {
                        result.Add(kvp.Key);
                        break;
                    }
                }
            }

            return (IReadOnlyList<string>)result;
        }, ct);
    }

    // ========== UTILITY ==========

    public async Task ClearAsync(CancellationToken ct)
    {
        await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                _rows.Clear();
                InvalidateSortedRowKeysCache();
                _logger?.LogInformation("ClearAsync: Cleared all rows");
            }
        }, ct);
    }

    public string? GetRowIdByIndex(int index)
    {
        lock (_modificationLock)
        {
            var sortedKeys = GetSortedRowKeys();
            return index >= 0 && index < sortedKeys.Count ? sortedKeys[index] : null;
        }
    }

    public int? GetRowIndexById(string rowId)
    {
        lock (_modificationLock)
        {
            var sortedKeys = GetSortedRowKeys();
            var index = sortedKeys.IndexOf(rowId);
            return index >= 0 ? index : null;
        }
    }

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Get sorted row keys (cached for performance).
    /// PRESUN Z InMemoryRowStore.cs:1711-1740
    /// </summary>
    private List<string> GetSortedRowKeys()
    {
        if (!_sortedRowKeysInvalid && _sortedRowKeys != null)
            return _sortedRowKeys;

        lock (_orderLock)
        {
            if (!_sortedRowKeysInvalid && _sortedRowKeys != null)
                return _sortedRowKeys;

            _sortedRowKeys = _rows.Values
                .OrderBy(row => row.TryGetValue("__rowNumber", out var rn)
                    ? Convert.ToInt32(rn)
                    : int.MaxValue) // PRIMARY SORT: __rowNumber
                .ThenBy(row => row["__rowId"]) // SECONDARY SORT: ULID
                .Select(row => (string)row["__rowId"]!)
                .ToList();

            _sortedRowKeysInvalid = false;
            return _sortedRowKeys;
        }
    }

    private void InvalidateSortedRowKeysCache()
    {
        lock (_orderLock)
        {
            _sortedRowKeysInvalid = true;
        }
    }
}

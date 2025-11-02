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

    // ✅ PROBLEM 2 FIX: FILTER SUPPORT (copied from InMemoryRowStore.cs)
    private IReadOnlyList<object>? _filterCriteria;
    private List<string>? _filteredRowIds;  // Cached filtered row IDs (ULID strings)
    private readonly object _filterLock = new();  // Thread-safe filter operations

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

                    // ✅ CRITICAL FIX: Preserve existing __rowId to maintain validation error references
                    // REASON: Validation errors store rowId - if we regenerate rowId after validation,
                    //         the errors become orphaned and cannot be applied to cells
                    string rowId;
                    if (rowData.ContainsKey("__rowId") && rowData["__rowId"] != null && !string.IsNullOrEmpty(rowData["__rowId"].ToString()))
                    {
                        // PRESERVE existing rowId (from previous load or import)
                        rowId = rowData["__rowId"].ToString()!;
                        _logger?.LogTrace("✅ PRESERVED rowId: {RowId} for row {Index}", rowId, i);
                    }
                    else
                    {
                        // Generate NEW rowId ONLY for brand new rows (first import)
                        rowId = Ulid.NewUlid().ToString();
                        rowData["__rowId"] = rowId;
                        _logger?.LogTrace("🆕 GENERATED rowId: {RowId} for row {Index}", rowId, i);
                    }

                    // ⚠️ IMPORTANT: Use indexer instead of TryAdd to UPDATE existing rows with same rowId
                    // REASON: During reload after filter/sort, we want to UPDATE existing row with same rowId
                    _rows[rowId] = rowData;
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
                // ✅ PROBLEM 2 FIX: Use filtered row IDs if filter is active
                IReadOnlyList<string> rowIdsToGet;
                if (onlyFiltered && _filteredRowIds != null)
                {
                    // Filtered view - use cached filtered row IDs
                    rowIdsToGet = _filteredRowIds;
                }
                else
                {
                    // All rows view - use sorted keys
                    rowIdsToGet = GetSortedRowKeys();
                }

                var result = new List<IReadOnlyDictionary<string, object?>>();

                for (long i = startIndex; i < Math.Min(startIndex + count, rowIdsToGet.Count); i++)
                {
                    if (_rows.TryGetValue(rowIdsToGet[(int)i], out var row))
                    {
                        result.Add(row);
                    }
                }

                _logger?.LogDebug("✅ PROBLEM 2 FIX (InMemory): GetRowsRangeAsync returning {Count} rows from index {Start} (onlyFiltered={OnlyFiltered})",
                    result.Count, startIndex, onlyFiltered);

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
                // ✅ PROBLEM 2 FIX: Use filtered row IDs if filter is active
                IEnumerable<string> rowIdsToCount;
                if (onlyFiltered && _filteredRowIds != null)
                {
                    // Filtered view - use cached filtered row IDs
                    rowIdsToCount = _filteredRowIds;
                    _logger?.LogDebug("✅ PROBLEM 2 FIX (InMemory): Using filtered view ({Count} filtered rows)",
                        _filteredRowIds.Count);
                }
                else
                {
                    // All rows view - use sorted keys
                    rowIdsToCount = GetSortedRowKeys();
                }

                // Count only non-empty data rows
                long count = 0;
                foreach (var rowId in rowIdsToCount)
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

                _logger?.LogDebug("✅ PROBLEM 2 FIX (InMemory): GetRowCountAsync returning {Count} non-empty rows (onlyFiltered={OnlyFiltered}, total in store: {Total})",
                    count, onlyFiltered, _rows.Count);

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
                // ✅ PROBLEM 2 FIX: Use filtered row IDs if filter is active
                IEnumerable<string> rowIdsToGet;
                if (onlyFiltered && _filteredRowIds != null)
                {
                    // Filtered view - use cached filtered row IDs
                    rowIdsToGet = _filteredRowIds;
                }
                else
                {
                    // All rows view - use sorted keys
                    rowIdsToGet = GetSortedRowKeys();
                }

                var result = new List<IReadOnlyDictionary<string, object?>>();

                foreach (var key in rowIdsToGet)
                {
                    if (_rows.TryGetValue(key, out var row))
                    {
                        result.Add(row);
                    }
                }

                _logger?.LogDebug("✅ PROBLEM 2 FIX (InMemory): GetAllRowsAsync returning {Count} rows (onlyFiltered={OnlyFiltered})",
                    result.Count, onlyFiltered);

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

    // ========== FILTER CRITERIA (✅ PROBLEM 2 FIX - copied from InMemoryRowStore.cs) ==========

    /// <summary>
    /// ✅ PROBLEM 2 FIX: Set filter criteria and build filtered view index.
    /// IDENTICAL logic to InMemoryRowStore.SetFilterCriteria (lines 742-790).
    /// PERFORMANCE: O(n) where n = total rows. Builds index once, subsequent filtered access is O(1) per row.
    /// </summary>
    public void SetFilterCriteria(IReadOnlyList<object>? filterCriteria)
    {
        lock (_filterLock)
        {
            _filterCriteria = filterCriteria;

            if (filterCriteria == null || filterCriteria.Count == 0)
            {
                // No filters - clear filtered view index
                _filteredRowIds = null;
                _logger?.LogInformation("✅ PROBLEM 2 FIX (InMemoryStrategy): Filter criteria cleared - no active filters");
                return;
            }

            // Build filtered view index - O(n) operation but cached for subsequent O(1) access
            _logger?.LogInformation("✅ PROBLEM 2 FIX (InMemoryStrategy): Building filtered view index for {FilterCount} filters over {TotalRows} rows",
                filterCriteria.Count, _rows.Count);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _filteredRowIds = new List<string>();

            // Get all rows as ordered list (need deterministic ordering for index mapping)
            var sortedKeys = GetSortedRowKeys();

            for (int originalIdx = 0; originalIdx < sortedKeys.Count; originalIdx++)
            {
                var rowId = sortedKeys[originalIdx];
                if (_rows.TryGetValue(rowId, out var row))
                {
                    // Check if row matches ALL filter criteria (AND logic)
                    if (RowMatchesAllFilters(row, filterCriteria))
                    {
                        _filteredRowIds.Add(rowId);
                    }
                }
            }

            stopwatch.Stop();

            _logger?.LogInformation("✅ PROBLEM 2 FIX (InMemoryStrategy): Filtered view index built: {FilteredCount}/{TotalCount} rows match filters (took {Duration}ms)",
                _filteredRowIds.Count, _rows.Count, stopwatch.ElapsedMilliseconds);
        }
    }

    public void ClearFilterCriteria()
    {
        SetFilterCriteria(null);
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

    // ========== FILTER HELPER METHODS (✅ PROBLEM 2 FIX - simplified from InMemoryRowStore.cs) ==========

    /// <summary>
    /// ✅ PROBLEM 2 FIX: Checks if row matches ALL active filter criteria (AND logic).
    /// Simplified version - supports basic operators using reflection (same as InMemoryRowStore).
    /// </summary>
    private bool RowMatchesAllFilters(IReadOnlyDictionary<string, object?> row, IReadOnlyList<object> filterCriteria)
    {
        foreach (var criteriaObj in filterCriteria)
        {
            // Extract filter properties using reflection (duck typing)
            var criteriaType = criteriaObj.GetType();
            var columnNameProp = criteriaType.GetProperty("ColumnName");
            var operatorProp = criteriaType.GetProperty("Operator");
            var valueProp = criteriaType.GetProperty("Value");

            if (columnNameProp == null || operatorProp == null || valueProp == null)
            {
                _logger?.LogWarning("Invalid filter criteria object - missing required properties");
                continue;
            }

            var columnName = columnNameProp.GetValue(criteriaObj) as string;
            var operatorValue = operatorProp.GetValue(criteriaObj); // Enum value
            var filterValue = valueProp.GetValue(criteriaObj);

            if (string.IsNullOrEmpty(columnName))
                continue;

            // Get cell value from row
            if (!row.TryGetValue(columnName, out var cellValue))
            {
                cellValue = null; // Column doesn't exist - treat as null
            }

            // ✅ PROFESSIONAL FIX: Apply filter operator with In and Regex support
            var operatorName = operatorValue?.ToString() ?? "";
            bool matches = operatorName switch
            {
                "Equals" => ValuesAreEqual(cellValue, filterValue),
                "Contains" => StringContains(cellValue, filterValue),
                "IsTrue" => cellValue is bool b && b,
                "IsFalse" => cellValue is bool bf && !bf,
                "In" => ValueInList(cellValue, filterValue),           // ✅ CHECKBOX FILTER FIX
                "Regex" => ValueMatchesRegex(cellValue, filterValue),  // ✅ REGEX FILTER FIX
                _ => false // ✅ FIX: Unknown operator should EXCLUDE row (not include)
            };

            if (!matches)
            {
                return false; // Row doesn't match this filter → exclude row
            }
        }

        return true; // Row matches all filters
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Checks if cell value is in list (for FilterOperator.In / checkbox filter).
    /// Used by checkbox filter mode - filterValue is List<string> of selected values.
    /// </summary>
    private bool ValueInList(object? cellValue, object? filterValue)
    {
        if (filterValue == null)
            return false;

        // filterValue should be List<string> (from checkbox filter)
        if (filterValue is not IEnumerable<string> selectedValues)
        {
            _logger?.LogWarning("FilterOperator.In expects List<string>, got {Type}", filterValue.GetType().Name);
            return false;
        }

        var cellString = cellValue?.ToString() ?? string.Empty;

        // Case-insensitive match for user-friendly behavior
        var selectedSet = new HashSet<string>(selectedValues, StringComparer.OrdinalIgnoreCase);
        return selectedSet.Contains(cellString);
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Checks if cell value matches regex pattern (for FilterOperator.Regex).
    /// Used by regex filter mode - filterValue is string regex pattern.
    /// Supports case-insensitive mode via (?i) prefix.
    /// </summary>
    private bool ValueMatchesRegex(object? cellValue, object? filterValue)
    {
        if (filterValue == null)
            return false;

        var pattern = filterValue.ToString();
        if (string.IsNullOrEmpty(pattern))
            return false;

        var cellString = cellValue?.ToString() ?? string.Empty;

        try
        {
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            return regex.IsMatch(cellString);
        }
        catch (System.Text.RegularExpressions.RegexParseException ex)
        {
            _logger?.LogWarning(ex, "Invalid regex pattern: {Pattern}", pattern);
            return false; // Invalid regex = no match
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Regex matching failed for pattern: {Pattern}", pattern);
            return false;
        }
    }

    private bool ValuesAreEqual(object? cellValue, object? filterValue)
    {
        if (cellValue == null && filterValue == null) return true;
        if (cellValue == null || filterValue == null) return false;
        if (cellValue.Equals(filterValue)) return true;

        // String comparison (case-insensitive)
        var cellStr = cellValue.ToString();
        var filterStr = filterValue.ToString();
        return string.Equals(cellStr, filterStr, StringComparison.OrdinalIgnoreCase);
    }

    private bool StringContains(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null) return false;

        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";
        return cellStr.Contains(filterStr, StringComparison.OrdinalIgnoreCase);
    }
}

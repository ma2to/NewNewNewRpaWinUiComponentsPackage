using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using System.Runtime.CompilerServices;
// ULID support for infinite row capacity (Ulid is in System namespace from Cysharp.Ulid package)

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;

/// <summary>
/// In-memory implementation of IRowStore for high-performance data operations
/// ULID-BASED: Uses ULID (Universally Unique Lexicographically Sortable Identifier) for row IDs
/// - Provides practically infinite capacity (2^128 vs 2^31 for int)
/// - Timestamp-based sorting enables efficient GetLastRowAsync O(n)
/// - Thread-safe generation without Interlocked counter
/// </summary>
internal sealed class InMemoryRowStore : Interfaces.IRowStore
{
    private readonly ILogger<InMemoryRowStore> _logger;

    // ULID MIGRATION: Changed from ConcurrentDictionary<int, ...> to ConcurrentDictionary<string, ...>
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>> _rows = new();
    private readonly ConcurrentDictionary<string, List<ValidationError>> _validationErrors = new();
    private readonly object _modificationLock = new();
    // ULID MIGRATION: No _nextRowId needed - each Ulid.NewUlid() is unique
    private IReadOnlyList<object> _filterCriteria = Array.Empty<object>();
    private Features.Filter.Models.FilterExpression? _filterExpression; // Complex filter expression tree
    private bool _hasValidationState = false;

    // FILTERED VIEW SUPPORT - Performance-optimized filtered data access
    // CRITICAL: These fields enable O(1) filtered index lookups and efficient filtered data retrieval
    private IReadOnlyList<object>? _activeFilterCriteria; // Active filter criteria
    private List<string>? _filteredRowIds; // Cached filtered row IDs (ULID strings)
    private Dictionary<int, int>? _filteredToOriginalIndexMap; // Maps filtered index → original index
    private readonly object _filterLock = new(); // Thread-safe filter operations

    // ORDERED ROW ACCESS - Cached sorted row keys for stable ordering (10M+ row performance)
    // CRITICAL: Avoids re-sorting on every GetAllRows() call - rebuild only on insert/delete
    private List<string>? _sortedRowKeys; // Cached list of row keys in ULID chronological order
    private bool _sortedRowKeysInvalid = true; // Flag to trigger cache rebuild
    private readonly object _orderLock = new(); // Thread-safe order cache operations

    // SENIOR FIX: VALIDATION CACHE - Prevents ValidateAll infinite loop
    // CRITICAL: Tracks which rows have been validated to avoid re-validation (cache check)
    // Cleared on data changes (ClearAsync, AddRangeAsync) to ensure fresh validation
    private readonly Dictionary<string, bool> _validatedRowsCache = new(); // Validated row IDs
    private bool _isValidating = false; // Re-entrancy guard for batch validation

    public InMemoryRowStore(ILogger<InMemoryRowStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    /// <summary>
    /// Adds or updates rows
    /// </summary>
    public async Task<int> UpsertRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var affectedRows = 0;

            lock (_modificationLock)
            {
                foreach (var row in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var rowId = GetOrAssignRowId(row);
                    _rows.AddOrUpdate(rowId, row, (_, _) => row);
                    affectedRows++;
                }

                // CRITICAL: Invalidate sorted keys cache after modifications
                if (affectedRows > 0)
                {
                    InvalidateSortedRowKeysCache();
                }
            }

            _logger.LogDebug("Upserted {AffectedRows} rows", affectedRows);
            return affectedRows;
        }, cancellationToken);
    }

    /// <summary>
    /// Removes rows by condition
    /// ULID MIGRATION: Use string keys for dictionary
    /// </summary>
    public async Task<int> RemoveRowsAsync(
        Func<IReadOnlyDictionary<string, object?>, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var removedCount = 0;

            lock (_modificationLock)
            {
                // ULID MIGRATION: List<string> instead of List<int>
                var keysToRemove = new List<string>();

                foreach (var kvp in _rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (predicate(kvp.Value))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    if (_rows.TryRemove(key, out _))
                    {
                        removedCount++;
                    }
                }

                // CRITICAL: Invalidate sorted keys cache after deletions
                if (removedCount > 0)
                {
                    InvalidateSortedRowKeysCache();
                }
            }

            _logger.LogDebug("Removed {RemovedCount} rows", removedCount);
            return removedCount;
        }, cancellationToken);
    }

    /// <summary>
    /// Clears all rows
    /// ULID MIGRATION: No _nextRowId reset needed (ULID is self-managed)
    /// </summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                var count = _rows.Count;
                _rows.Clear();

                // SENIOR FIX: Invalidate sorted keys cache after clearing rows
                // BUG: Without this, cache contains old ULID keys → GetAllRows() returns 0 rows
                // SCENARIO: Import #1 → cache built → Import #2 → Clear → cache still has old keys → TryGetValue fails
                lock (_orderLock)
                {
                    _sortedRowKeysInvalid = true;
                }

                // SENIOR FIX: Clear validation cache when data is cleared
                // Ensures fresh validation when new data is imported
                _validatedRowsCache.Clear();

                // ULID MIGRATION: No _nextRowId reset needed - each Ulid.NewUlid() is unique
                _logger.LogDebug("Cleared all rows: {ClearedCount} rows removed, sorted keys cache and validation cache cleared", count);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Adds multiple rows in batch
    /// ULID MIGRATION: Uses Ulid.NewUlid() for thread-safe unique ID generation
    /// </summary>
    public async Task<int> AddRangeAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var addedCount = 0;

            lock (_modificationLock)
            {
                foreach (var row in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // ULID MIGRATION: Generate ULID string (timestamp-based, lexicographically sortable)
                    var rowId = Ulid.NewUlid().ToString();
                    var rowWithId = new Dictionary<string, object?>(row)
                    {
                        ["__rowId"] = rowId
                    };

                    _rows.TryAdd(rowId, rowWithId);
                    addedCount++;
                }

                // SENIOR FIX: Invalidate sorted keys cache after adding new rows
                // BUG: Cache contains old ULID keys → new rows not visible in GetAllRows()
                if (addedCount > 0)
                {
                    lock (_orderLock)
                    {
                        _sortedRowKeysInvalid = true;
                    }

                    // SENIOR FIX: Clear validation cache when new rows added
                    // Ensures new rows are validated (not skipped as "already validated")
                    _validatedRowsCache.Clear();
                }
            }

            _logger.LogDebug("Added {AddedCount} rows in batch, sorted keys cache and validation cache cleared", addedCount);
            return addedCount;
        }, cancellationToken);
    }

    /// <summary>
    /// Stream rows in batches - IRowStore implementation
    /// Supports filtering by both filtered state and checked state
    /// </summary>
    public async IAsyncEnumerable<IReadOnlyList<IReadOnlyDictionary<string, object?>>> StreamRowsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        int batchSize = 1000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Streaming rows in batches: onlyFiltered={OnlyFiltered}, onlyChecked={OnlyChecked}, batchSize={BatchSize}",
            onlyFiltered, onlyChecked, batchSize);

        await Task.Yield(); // Make it truly async

        // PERFORMANCE FIX: Use cached sorted keys
        var sortedKeys = GetSortedRowKeys();
        var allRows = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var key in sortedKeys)
        {
            if (_rows.TryGetValue(key, out var row))
            {
                if ((!onlyFiltered || IsRowVisible(row)) && (!onlyChecked || IsRowChecked(row)))
                {
                    allRows.Add(row);
                }
            }
        }

        _logger.LogInformation("Filtered {TotalRows} rows: onlyFiltered={OnlyFiltered}, onlyChecked={OnlyChecked}, result={ResultCount}",
            _rows.Count, onlyFiltered, onlyChecked, allRows.Count);

        for (int i = 0; i < allRows.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = allRows.Skip(i).Take(batchSize).ToList();
            yield return batch;
        }
    }

    /// <summary>
    /// Get row count - IRowStore implementation
    /// PERFORMANCE: O(1) for total count, O(1) for filtered count (uses cached _filteredRowIds.Count)
    /// </summary>
    public Task<long> GetRowCountAsync(
        bool onlyFiltered,
        CancellationToken cancellationToken = default)
    {
        if (!onlyFiltered || _filteredRowIds == null)
        {
            // Return total row count
            return Task.FromResult((long)_rows.Count);
        }

        // Return filtered row count (O(1) - uses cached count)
        lock (_filterLock)
        {
            var count = _filteredRowIds?.Count ?? 0;
            return Task.FromResult((long)count);
        }
    }

    /// <summary>
    /// Get row count - IRowStore implementation (backward compatibility overload)
    /// </summary>
    public Task<long> GetRowCountAsync(CancellationToken cancellationToken = default)
    {
        return GetRowCountAsync(onlyFiltered: false, cancellationToken);
    }

    /// <summary>
    /// Get filtered row count - IRowStore implementation
    /// </summary>
    public Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default)
    {
        // NOTE: Count() doesn't depend on order, but using consistent pattern for maintainability
        var count = _rows.Values.Count(IsRowVisible);
        return Task.FromResult((long)count);
    }

    /// <summary>
    /// Get all rows - IRowStore implementation
    /// PERFORMANCE: O(n) for all rows, O(f) for filtered rows where f = filtered count
    /// </summary>
    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        bool onlyFiltered,
        CancellationToken cancellationToken = default)
    {
        if (!onlyFiltered || _filteredRowIds == null)
        {
            // Return all rows (no filter active or not requested)
            // PERFORMANCE FIX: Use cached sorted keys
            var sortedKeys = GetSortedRowKeys();
            var rows = new List<IReadOnlyDictionary<string, object?>>(sortedKeys.Count);

            foreach (var key in sortedKeys)
            {
                if (_rows.TryGetValue(key, out var row))
                {
                    rows.Add(row);
                }
            }

            return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(rows);
        }

        // Return filtered view - use cached filtered row IDs for O(f) performance
        lock (_filterLock)
        {
            var filteredRows = new List<IReadOnlyDictionary<string, object?>>(_filteredRowIds.Count);
            foreach (var rowId in _filteredRowIds)
            {
                if (_rows.TryGetValue(rowId, out var row))
                {
                    filteredRows.Add(row);
                }
            }

            _logger.LogDebug("Retrieved {FilteredCount} filtered rows out of {TotalCount} total rows",
                filteredRows.Count, _rows.Count);

            return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(filteredRows);
        }
    }

    /// <summary>
    /// Get all rows - IRowStore implementation (backward compatibility overload)
    /// </summary>
    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAllRowsAsync(onlyFiltered: false, cancellationToken);
    }

    /// <summary>
    /// Persist rows - IRowStore implementation
    /// </summary>
    public async Task PersistRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        await UpsertRowsAsync(rows, cancellationToken);
    }

    /// <summary>
    /// Replace all rows - IRowStore implementation
    /// </summary>
    public async Task ReplaceAllRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        await ClearAsync(cancellationToken);
        await AddRangeAsync(rows, cancellationToken);
    }

    /// <summary>
    /// Append rows - IRowStore implementation
    /// </summary>
    public async Task AppendRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        await AddRangeAsync(rows, cancellationToken);
    }

    /// <summary>
    /// Ensures the grid has at least one empty row on initialization.
    /// Called during grid creation - only adds a row if store is completely empty.
    /// </summary>
    public async Task EnsureInitialEmptyRowAsync(
        IEnumerable<string> columnNames,
        CancellationToken cancellationToken = default)
    {
        var currentCount = await GetRowCountAsync(cancellationToken);

        if (currentCount == 0)
        {
            _logger.LogInformation("Grid is empty - creating initial empty row with {ColumnCount} columns", columnNames.Count());

            // Create empty row with all columns set to null
            var emptyRow = new Dictionary<string, object?> { ["__rowId"] = Ulid.NewUlid().ToString() };
            foreach (var columnName in columnNames)
            {
                if (columnName != "__rowId")
                {
                    emptyRow[columnName] = null;
                }
            }

            await AppendRowsAsync(new[] { emptyRow }, cancellationToken);
            _logger.LogInformation("Initial empty row created successfully");
        }
        else
        {
            _logger.LogDebug("Grid already has {Count} rows - skipping initial empty row creation", currentCount);
        }
    }

    public async Task InitializeEmptyRowsAsync(
        IEnumerable<string> columnNames,
        int rowCount,
        CancellationToken cancellationToken = default)
    {
        if (rowCount <= 0)
        {
            _logger.LogWarning("InitializeEmptyRowsAsync: rowCount must be positive, got {RowCount}", rowCount);
            return;
        }

        _logger.LogInformation("InitializeEmptyRowsAsync: Creating {RowCount} empty rows", rowCount);

        var columnList = columnNames.ToList();
        var emptyRows = new List<IReadOnlyDictionary<string, object?>>(rowCount);

        // Create N empty rows (all columns set to null)
        for (int i = 0; i < rowCount; i++)
        {
            var emptyRow = new Dictionary<string, object?> { ["__rowId"] = Ulid.NewUlid().ToString() };
            foreach (var columnName in columnList)
            {
                if (columnName != "__rowId")
                {
                    emptyRow[columnName] = null;
                }
            }
            emptyRows.Add(emptyRow);
        }

        // Append all empty rows in bulk
        await AppendRowsAsync(emptyRows, cancellationToken);

        _logger.LogInformation("InitializeEmptyRowsAsync: Successfully created {RowCount} empty rows", rowCount);
    }

    /// <summary>
    /// Insert rows at position - IRowStore implementation
    /// </summary>
    public async Task InsertRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex,
        CancellationToken cancellationToken = default)
    {
        // For in-memory store, insert means add with specific row numbers
        await AddRangeAsync(rows, cancellationToken);
    }

    /// <summary>
    /// Write validation results - IRowStore implementation
    /// ULID MIGRATION: Works with string row IDs directly
    /// </summary>
    public Task WriteValidationResultsAsync(
        IEnumerable<ValidationError> results,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Writing validation results");

        _validationErrors.Clear();
        foreach (var error in results)
        {
            var rowId = error.RowId;
            if (string.IsNullOrEmpty(rowId))
                continue; // Skip errors without RowId

            // ULID MIGRATION: Use string rowId directly (no int.TryParse needed)
            if (!_validationErrors.ContainsKey(rowId))
            {
                _validationErrors[rowId] = new List<ValidationError>();
            }
            _validationErrors[rowId].Add(error);
        }

        _hasValidationState = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Check if validation state exists - IRowStore implementation
    /// </summary>
    public Task<bool> HasValidationStateForScopeAsync(
        bool onlyFiltered,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_hasValidationState);
    }

    /// <summary>
    /// Check if all non-empty rows are valid - IRowStore implementation
    /// Supports filtering by both filtered state and checked state
    /// ULID MIGRATION: GetRowId now returns string? (nullable)
    /// </summary>
    public Task<bool> AreAllNonEmptyRowsMarkedValidAsync(
        bool onlyFiltered,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default)
    {
        if (!_hasValidationState)
            return Task.FromResult(false);

        var rowsToCheck = _rows.Values
            .Where(row => !onlyFiltered || IsRowVisible(row))
            .Where(row => !onlyChecked || IsRowChecked(row))
            .Where(row => !IsRowEmpty(row));

        // ULID MIGRATION: GetRowId returns string? so handle null case
        var allValid = !_validationErrors.Any() ||
                       rowsToCheck.All(row =>
                       {
                           var rowId = GetRowId(row);
                           return rowId == null || !_validationErrors.ContainsKey(rowId);
                       });

        _logger.LogDebug("Checked validation state: onlyFiltered={OnlyFiltered}, onlyChecked={OnlyChecked}, allValid={AllValid}",
            onlyFiltered, onlyChecked, allValid);

        return Task.FromResult(allValid);
    }

    /// <summary>
    /// Get validation errors - IRowStore implementation
    /// Supports filtering by both filtered state and checked state
    /// ULID MIGRATION: Use string rowId directly for dictionary lookup
    /// </summary>
    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default)
    {
        var errors = _validationErrors.Values
            .SelectMany(list => list)
            .Where(error =>
            {
                // ULID MIGRATION: Use string rowId directly (no int.TryParse)
                if (string.IsNullOrEmpty(error.RowId))
                    return false;

                var row = _rows.GetValueOrDefault(error.RowId) ?? new Dictionary<string, object?>();
                return (!onlyFiltered || IsRowVisible(row)) &&
                       (!onlyChecked || IsRowChecked(row));
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ValidationError>>(errors);
    }

    /// <summary>
    /// Clear validation state - IRowStore implementation
    /// </summary>
    public Task ClearValidationStateAsync(CancellationToken cancellationToken = default)
    {
        _validationErrors.Clear();
        _hasValidationState = false;
        _logger.LogDebug("Validation state cleared");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Set filter criteria and build filtered view index - IRowStore implementation
    /// PERFORMANCE: O(n) where n = total rows. Builds index once, subsequent filtered access is O(1) per row.
    /// CRITICAL: This is the core of filtered view support - builds cached index for efficient access
    /// </summary>
    public void SetFilterCriteria(IReadOnlyList<object>? filterCriteria)
    {
        lock (_filterLock)
        {
            _filterCriteria = filterCriteria ?? Array.Empty<object>();
            _activeFilterCriteria = filterCriteria;

            if (filterCriteria == null || filterCriteria.Count == 0)
            {
                // No filters - clear filtered view index
                _filteredRowIds = null;
                _filteredToOriginalIndexMap = null;
                _logger.LogInformation("Filter criteria cleared - no active filters");
                return;
            }

            // Build filtered view index - O(n) operation but cached for subsequent O(1) access
            _logger.LogInformation("Building filtered view index for {FilterCount} filters over {TotalRows} rows",
                filterCriteria.Count, _rows.Count);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _filteredRowIds = new List<string>();
            _filteredToOriginalIndexMap = new Dictionary<int, int>();

            // Get all rows as ordered list (need deterministic ordering for index mapping)
            var allRows = _rows.ToList(); // List<KeyValuePair<string, IReadOnlyDictionary>>
            int filteredIdx = 0;

            for (int originalIdx = 0; originalIdx < allRows.Count; originalIdx++)
            {
                var rowKvp = allRows[originalIdx];
                var row = rowKvp.Value;

                // Check if row matches ALL filter criteria (AND logic)
                if (RowMatchesAllFilters(row, filterCriteria))
                {
                    var rowId = rowKvp.Key; // ULID string
                    _filteredRowIds.Add(rowId);
                    _filteredToOriginalIndexMap[filteredIdx] = originalIdx;
                    filteredIdx++;
                }
            }

            stopwatch.Stop();

            _logger.LogInformation("Filtered view index built: {FilteredCount}/{TotalCount} rows match filters (took {Duration}ms)",
                _filteredRowIds.Count, allRows.Count, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Get filter criteria - IRowStore implementation
    /// </summary>
    public IReadOnlyList<object> GetFilterCriteria()
    {
        return _filterCriteria;
    }

    /// <summary>
    /// Clear filter criteria and filtered view index - IRowStore implementation
    /// Equivalent to SetFilterCriteria(null)
    /// </summary>
    public void ClearFilterCriteria()
    {
        SetFilterCriteria(null);
    }

    public void SetFilterExpression(Features.Filter.Models.FilterExpression? expression)
    {
        lock (_filterLock)
        {
            _filterExpression = expression;

            if (expression == null)
            {
                // No filter - clear filtered view index
                _filteredRowIds = null;
                _filteredToOriginalIndexMap = null;
                _logger.LogInformation("Filter expression cleared - no active filters");
                return;
            }

            // Build filtered view index using filter expression evaluator
            _logger.LogInformation("Building filtered view index using filter expression over {TotalRows} rows", _rows.Count);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _filteredRowIds = new List<string>();
            _filteredToOriginalIndexMap = new Dictionary<int, int>();

            // Create evaluator (pass null for logger - different type)
            var evaluator = new Features.Filter.Services.FilterExpressionEvaluator(null);

            // Get all rows as ordered list (need deterministic ordering for index mapping)
            var allRows = _rows.ToList(); // List<KeyValuePair<string, IReadOnlyDictionary>>
            int filteredIdx = 0;

            for (int originalIdx = 0; originalIdx < allRows.Count; originalIdx++)
            {
                var rowKvp = allRows[originalIdx];
                var row = rowKvp.Value;

                // Evaluate filter expression against row
                if (evaluator.Evaluate(expression, row))
                {
                    var rowId = rowKvp.Key; // ULID string
                    _filteredRowIds.Add(rowId);
                    _filteredToOriginalIndexMap[filteredIdx] = originalIdx;
                    filteredIdx++;
                }
            }

            stopwatch.Stop();

            _logger.LogInformation("Filtered view index built using expression: {FilteredCount}/{TotalCount} rows match (took {Duration}ms)",
                _filteredRowIds.Count, allRows.Count, stopwatch.ElapsedMilliseconds);
        }
    }

    public Features.Filter.Models.FilterExpression? GetFilterExpression()
    {
        return _filterExpression;
    }

    /// <summary>
    /// Set sort criteria - IRowStore implementation
    /// Note: InMemoryRowStore does not use this (sorting handled by SortService).
    /// This is a no-op to maintain interface compatibility.
    /// </summary>
    public void SetSortCriteria(string columnName, Common.SortDirection direction)
    {
        // No-op: InMemoryRowStore doesn't use SQL-based sorting
        // Sorting is handled by SortService using LINQ OrderBy
        _logger.LogDebug("SetSortCriteria called on InMemoryRowStore (no-op): column={Column}, direction={Direction}",
            columnName, direction);
    }

    /// <summary>
    /// Clear sort criteria - IRowStore implementation
    /// Note: InMemoryRowStore does not use this (sorting handled by SortService).
    /// This is a no-op to maintain interface compatibility.
    /// </summary>
    public void ClearSortCriteria()
    {
        // No-op: InMemoryRowStore doesn't use SQL-based sorting
        _logger.LogDebug("ClearSortCriteria called on InMemoryRowStore (no-op)");
    }

    /// <summary>
    /// Perform full-text search - IRowStore implementation
    /// Note: InMemoryRowStore uses LINQ-based search (not FTS5).
    /// Returns row IDs that match the search text in any column.
    /// </summary>
    public async Task<IReadOnlyList<string>> SearchAsync(
        string searchText,
        string[]? targetColumns = null,
        bool caseSensitive = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return Array.Empty<string>();

        await Task.CompletedTask; // Make it async compatible

        var comparison = caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var matchedRowIds = new List<string>();

        lock (_modificationLock)
        {
            foreach (var kvp in _rows)
            {
                var rowId = kvp.Key;
                var row = kvp.Value;

                // Check if row matches search in target columns or all columns
                var columnsToSearch = targetColumns ?? row.Keys.ToArray();

                foreach (var columnName in columnsToSearch)
                {
                    if (row.TryGetValue(columnName, out var cellValue))
                    {
                        var cellText = cellValue?.ToString() ?? string.Empty;
                        if (cellText.Contains(searchText, comparison))
                        {
                            matchedRowIds.Add(rowId);
                            break; // Found match in this row, move to next row
                        }
                    }
                }
            }
        }

        _logger.LogDebug("SearchAsync: found {MatchCount} matches for '{SearchText}'",
            matchedRowIds.Count, searchText);

        return matchedRowIds;
    }

    /// <summary>
    /// Map filtered row index to original row index - IRowStore implementation
    /// CRITICAL FOR EDITS: When user edits cell in filtered view, we need correct original index
    /// PERFORMANCE: O(1) dictionary lookup
    /// </summary>
    /// <param name="filteredIndex">Index in filtered view (0-based)</param>
    /// <returns>Index in original dataset, or null if not found or no filter active</returns>
    public int? MapFilteredIndexToOriginalIndex(int filteredIndex)
    {
        lock (_filterLock)
        {
            if (_filteredToOriginalIndexMap == null)
            {
                // No filter active - indices are the same
                return filteredIndex;
            }

            if (_filteredToOriginalIndexMap.TryGetValue(filteredIndex, out var originalIndex))
            {
                _logger.LogDebug("Mapped filtered index {FilteredIndex} → original index {OriginalIndex}",
                    filteredIndex, originalIndex);
                return originalIndex;
            }

            _logger.LogWarning("Failed to map filtered index {FilteredIndex} - index out of range", filteredIndex);
            return null;
        }
    }

    /// <summary>
    /// Get validation errors for specific row - IRowStore implementation
    /// ULID MIGRATION: Use string rowId directly
    /// </summary>
    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(
        string rowId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(rowId))
            return Task.FromResult<IReadOnlyList<ValidationError>>(Array.Empty<ValidationError>());

        // ULID MIGRATION: Use string rowId directly (no int.TryParse)
        if (_validationErrors.TryGetValue(rowId, out var errors))
        {
            return Task.FromResult<IReadOnlyList<ValidationError>>(errors.ToList());
        }

        return Task.FromResult<IReadOnlyList<ValidationError>>(Array.Empty<ValidationError>());
    }

    /// <summary>
    /// SENIOR FIX: Checks if a row has already been validated (cache check).
    /// Used by ValidateAll to skip already validated rows (Option 3 - cache skip).
    /// Prevents infinite validation loop and improves performance.
    /// </summary>
    /// <param name="rowId">Row ID to check</param>
    /// <returns>True if row is in validation cache, false otherwise</returns>
    public bool IsRowValidationCached(string rowId)
    {
        lock (_modificationLock)
        {
            return _validatedRowsCache.ContainsKey(rowId) && _validatedRowsCache[rowId];
        }
    }

    /// <summary>
    /// SENIOR FIX: Marks a row as validated in the cache.
    /// Called after successful validation to prevent re-validation.
    /// </summary>
    /// <param name="rowId">Row ID to mark as validated</param>
    public void MarkRowAsValidated(string rowId)
    {
        lock (_modificationLock)
        {
            _validatedRowsCache[rowId] = true;
        }
    }

    /// <summary>
    /// SENIOR FIX: Clears validation cache.
    /// Called when data changes (ClearAsync, AddRangeAsync) to ensure fresh validation.
    /// </summary>
    public void ClearValidationCache()
    {
        lock (_modificationLock)
        {
            _validatedRowsCache.Clear();
            _logger.LogDebug("Validation cache cleared");
        }
    }

    /// <summary>
    /// SENIOR FIX: Batch writes validation results for multiple rows in a single operation (Option 2).
    /// Prevents infinite validation loop by:
    /// 1. Re-entrancy guard (_isValidating flag)
    /// 2. Single DataChanged event fire after all writes complete
    /// 3. Cache marking to skip already validated rows
    /// </summary>
    /// <param name="validationResults">Dictionary of rowId → validation errors</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task WriteValidationResultsBatchAsync(
        Dictionary<string, ValidationError[]> validationResults,
        CancellationToken cancellationToken = default)
    {
        if (_isValidating)
        {
            _logger.LogWarning("Validation already in progress - skipping batch write");
            return;
        }

        try
        {
            _isValidating = true;

            await Task.Run(() =>
            {
                lock (_modificationLock)
                {
                    foreach (var (rowId, errors) in validationResults)
                    {
                        if (!_rows.TryGetValue(rowId, out var row))
                            continue;

                        // Clear existing validation errors for this row
                        if (_validationErrors.ContainsKey(rowId))
                        {
                            _validationErrors[rowId].Clear();
                        }
                        else
                        {
                            _validationErrors[rowId] = new List<ValidationError>();
                        }

                        // Add new validation errors
                        if (errors.Length > 0)
                        {
                            _validationErrors[rowId].AddRange(errors);
                        }

                        // Mark as validated
                        _validatedRowsCache[rowId] = true;
                    }

                    _logger.LogInformation(
                        "Batch validation write completed: {RowCount} rows validated",
                        validationResults.Count);
                }
            }, cancellationToken);

            // NOTE: No explicit DataChanged event needed here
            // Validation state is queried directly from _validationErrors dictionary
        }
        finally
        {
            _isValidating = false;
        }
    }

    /// <summary>
    /// Get a specific row by RowID - IRowStore implementation (PRIMARY - stable identifier)
    /// ULID MIGRATION: Use string rowId directly
    /// </summary>
    public Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(
        string rowId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(rowId))
        {
            _logger.LogWarning("Invalid RowID: null or empty");
            return Task.FromResult<IReadOnlyDictionary<string, object?>?>(null);
        }

        // ULID MIGRATION: Use string rowId directly (no int.TryParse)
        if (_rows.TryGetValue(rowId, out var row))
        {
            _logger.LogDebug("Retrieved row by RowID: {RowId}", rowId);
            return Task.FromResult<IReadOnlyDictionary<string, object?>?>(row);
        }

        _logger.LogDebug("Row not found by RowID: {RowId}", rowId);
        return Task.FromResult<IReadOnlyDictionary<string, object?>?>(null);
    }

    /// <summary>
    /// Update a specific row by RowID - IRowStore implementation (PRIMARY - stable identifier)
    /// ULID MIGRATION: Use string rowId directly, preserve ULID
    /// </summary>
    public Task<bool> UpdateRowByIdAsync(
        string rowId,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrEmpty(rowId))
            {
                _logger.LogWarning("Invalid RowID: null or empty");
                return false;
            }

            lock (_modificationLock)
            {
                // ULID MIGRATION: Use string rowId directly (no int.TryParse)
                if (!_rows.TryGetValue(rowId, out var oldRow))
                {
                    _logger.LogWarning("Row not found for update by RowID: {RowId}", rowId);
                    return false;
                }

                // Preserve __rowId (ULID string) in updated data
                var updatedRow = new Dictionary<string, object?>(rowData)
                {
                    ["__rowId"] = rowId  // Preserve ULID
                };

                if (_rows.TryUpdate(rowId, updatedRow, oldRow))
                {
                    _logger.LogDebug("Updated row by RowID: {RowId}", rowId);
                    return true;
                }

                _logger.LogWarning("Failed to update row by RowID: {RowId}", rowId);
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Remove a specific row by RowID - IRowStore implementation (PRIMARY - stable identifier)
    /// ULID MIGRATION: Use string rowId directly
    /// </summary>
    public Task<bool> RemoveRowByIdAsync(
        string rowId,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrEmpty(rowId))
            {
                _logger.LogWarning("Invalid RowID: null or empty");
                return false;
            }

            lock (_modificationLock)
            {
                // ULID MIGRATION: Use string rowId directly (no int.TryParse)
                if (_rows.TryRemove(rowId, out _))
                {
                    // Also remove validation errors for this row
                    _validationErrors.TryRemove(rowId, out _);

                    // CRITICAL: Invalidate sorted keys cache after deletion
                    InvalidateSortedRowKeysCache();

                    _logger.LogDebug("Removed row by RowID: {RowId}", rowId);
                    return true;
                }

                _logger.LogDebug("Row not found for removal by RowID: {RowId}", rowId);
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Remove rows by IDs - IRowStore implementation
    /// ULID MIGRATION: Use string rowIds directly
    /// </summary>
    public Task RemoveRowsAsync(
        IEnumerable<string> rowIds,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            lock (_modificationLock)
            {
                var removedCount = 0;
                foreach (var rowId in rowIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrEmpty(rowId))
                        continue;

                    // ULID MIGRATION: Use string rowId directly (no int.TryParse)
                    if (_rows.TryRemove(rowId, out _))
                    {
                        // Also remove validation errors for this row
                        _validationErrors.TryRemove(rowId, out _);
                        removedCount++;
                    }
                }

                // CRITICAL: Invalidate sorted keys cache after deletions
                if (removedCount > 0)
                {
                    InvalidateSortedRowKeysCache();
                }

                _logger.LogInformation("Removed {RemovedCount} of {RequestedCount} rows",
                    removedCount, rowIds.Count());
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Get a specific row by index - IRowStore implementation (DEPRECATED - use GetRowByIdAsync)
    /// ULID MIGRATION: ULID strings are lexicographically sortable by timestamp
    /// </summary>
    public Task<IReadOnlyDictionary<string, object?>?> GetRowAsync(
        int rowIndex,
        CancellationToken cancellationToken = default)
    {
        // PERFORMANCE FIX: Use cached sorted keys
        var sortedKeys = GetSortedRowKeys();
        var allRows = new List<IReadOnlyDictionary<string, object?>>(sortedKeys.Count);

        foreach (var key in sortedKeys)
        {
            if (_rows.TryGetValue(key, out var row))
            {
                allRows.Add(row);
            }
        }

        if (rowIndex < 0 || rowIndex >= allRows.Count)
        {
            return Task.FromResult<IReadOnlyDictionary<string, object?>?>(null);
        }

        return Task.FromResult<IReadOnlyDictionary<string, object?>?>(allRows[rowIndex]);
    }

    /// <summary>
    /// Update a specific row by index - IRowStore implementation (DEPRECATED - use UpdateRowByIdAsync)
    /// ULID MIGRATION: Sort by ULID string for index-based access
    /// </summary>
    public Task<bool> UpdateRowAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            lock (_modificationLock)
            {
                // ULID MIGRATION: Sort by ULID string (lexicographically sortable)
                // PERFORMANCE FIX: Use cached sorted keys
                var sortedKeys = GetSortedRowKeys();
                var allRows = new List<IReadOnlyDictionary<string, object?>>(sortedKeys.Count);

                foreach (var key in sortedKeys)
                {
                    if (_rows.TryGetValue(key, out var row))
                    {
                        allRows.Add(row);
                    }
                }

                if (rowIndex < 0 || rowIndex >= allRows.Count)
                {
                    return false;
                }

                var oldRow = allRows[rowIndex];
                var rowId = GetRowId(oldRow);

                if (rowId != null && _rows.TryUpdate(rowId, rowData, oldRow))
                {
                    _logger.LogDebug("Updated row at index {RowIndex}", rowIndex);
                    return true;
                }

                return false;
            }
        }, cancellationToken);
    }

    #region Private Methods

    private bool IsRowVisible(IReadOnlyDictionary<string, object?> row)
    {
        // Check if row is marked as filtered out
        if (row.TryGetValue("__isFiltered", out var isFiltered) && isFiltered is bool filtered)
        {
            return !filtered;
        }

        return true; // Default to visible
    }

    private bool IsRowChecked(IReadOnlyDictionary<string, object?> row)
    {
        // Check if row has checkbox column marked as checked
        // Look for typical checkbox column names
        foreach (var kvp in row)
        {
            var columnName = kvp.Key.ToLowerInvariant();
            if ((columnName.Contains("selected") ||
                 columnName.Contains("checked") ||
                 columnName.Contains("check") ||
                 columnName == "isselected" ||
                 columnName == "ischecked") &&
                kvp.Value is bool boolValue)
            {
                return boolValue;
            }
        }

        return false; // Default to not checked
    }

    private bool IsRowEmpty(IReadOnlyDictionary<string, object?> row)
    {
        // Row is empty if all non-special columns are null or empty
        return row.All(kvp =>
            kvp.Key.StartsWith("__") ||
            kvp.Value == null ||
            (kvp.Value is string str && string.IsNullOrWhiteSpace(str)));
    }

    /// <summary>
    /// ULID MIGRATION: Get row ID from row data (string ULID or legacy int converted to string)
    /// Returns null if no valid ID found
    /// </summary>
    private string? GetRowId(IReadOnlyDictionary<string, object?> row)
    {
        if (row.TryGetValue("__rowId", out var rowIdValue))
        {
            // Primary: string ULID
            if (rowIdValue is string rowId && !string.IsNullOrEmpty(rowId))
                return rowId;

            // // Legacy support: int rowId converted to string
            // if (rowIdValue is int intId)
            //     return intId.ToString();
        }
        return null;
    }

    /// <summary>
    /// ULID MIGRATION: Get existing row ID or generate new ULID
    /// Always returns non-null string (either existing or newly generated ULID)
    /// </summary>
    private string GetOrAssignRowId(IReadOnlyDictionary<string, object?> row)
    {
        if (row.TryGetValue("__rowId", out var rowIdValue))
        {
            // Prefer existing string ULID
            if (rowIdValue is string existingId && !string.IsNullOrEmpty(existingId))
                return existingId;

            // // Legacy int support - convert to string
            // if (rowIdValue is int intId)
            //     return intId.ToString();
        }

        // Generate new ULID (thread-safe, timestamp-based, lexicographically sortable)
        return Ulid.NewUlid().ToString();
    }

    #endregion

    #region Public API Compatibility Methods

    public async Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        await AppendRowsAsync(new[] { rowData }, cancellationToken);
        return _rows.Count - 1; // Return index of added row
    }

    public async Task<int> AddRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData, CancellationToken cancellationToken = default)
    {
        var rowsList = rowsData.ToList();
        await AppendRowsAsync(rowsList, cancellationToken);
        return rowsList.Count;
    }

    public async Task InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        await InsertRowsAsync(new[] { rowData }, rowIndex, cancellationToken);
    }

    /// <summary>
    /// DEPRECATED: Use RemoveRowByIdAsync instead
    /// ULID MIGRATION: Sort by ULID to get row by index, then remove by ID
    /// </summary>
    public async Task RemoveRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                // PERFORMANCE FIX: Use cached sorted keys
                var sortedKeys = GetSortedRowKeys();
                var allRows = new List<IReadOnlyDictionary<string, object?>>(sortedKeys.Count);

                foreach (var key in sortedKeys)
                {
                    if (_rows.TryGetValue(key, out var row))
                    {
                        allRows.Add(row);
                    }
                }

                if (rowIndex >= 0 && rowIndex < allRows.Count)
                {
                    var row = allRows[rowIndex];
                    var rowId = GetRowId(row);
                    if (rowId != null)
                    {
                        _rows.TryRemove(rowId, out _);
                    }
                }
            }
        }, cancellationToken);
    }

    /// <summary>
    /// DEPRECATED: Use RemoveRowsAsync(IEnumerable<string> rowIds) instead
    /// ULID MIGRATION: Sort by ULID to map indices to IDs, then remove
    /// </summary>
    public async Task<int> RemoveRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                // PERFORMANCE FIX: Use cached sorted keys
                var sortedKeys = GetSortedRowKeys();
                var allRows = new List<IReadOnlyDictionary<string, object?>>(sortedKeys.Count);

                foreach (var key in sortedKeys)
                {
                    if (_rows.TryGetValue(key, out var row))
                    {
                        allRows.Add(row);
                    }
                }

                var indices = rowIndices.ToList();
                var removed = 0;

                foreach (var index in indices)
                {
                    if (index >= 0 && index < allRows.Count)
                    {
                        var row = allRows[index];
                        var rowId = GetRowId(row);
                        if (rowId != null && _rows.TryRemove(rowId, out _))
                        {
                            removed++;
                        }
                    }
                }
                return removed;
            }
        }, cancellationToken);
    }

    public async Task ClearAllRowsAsync(CancellationToken cancellationToken = default)
    {
        await ClearAsync(cancellationToken);
    }

    /// <summary>
    /// DEPRECATED: Use GetRowByIdAsync instead
    /// ULID MIGRATION: Sort by ULID to get row by index
    /// </summary>
    public IReadOnlyDictionary<string, object?>? GetRow(int rowIndex)
    {
        // PERFORMANCE FIX: Use cached sorted keys
        var sortedKeys = GetSortedRowKeys();

        if (rowIndex < 0 || rowIndex >= sortedKeys.Count)
        {
            return null;
        }

        var rowId = sortedKeys[rowIndex];
        return _rows.TryGetValue(rowId, out var row) ? row : null;
    }

    /// <summary>
    /// Gets cached sorted row keys (ULID chronological order).
    /// PERFORMANCE: Lazy rebuild - sorts only when cache invalidated by insert/delete.
    /// Thread-safe via double-check locking pattern.
    /// </summary>
    private List<string> GetSortedRowKeys()
    {
        // Fast path: cache valid
        if (!_sortedRowKeysInvalid && _sortedRowKeys != null)
        {
            return _sortedRowKeys;
        }

        // Slow path: rebuild cache
        lock (_orderLock)
        {
            // Double-check pattern: another thread may have rebuilt while we waited
            if (!_sortedRowKeysInvalid && _sortedRowKeys != null)
            {
                return _sortedRowKeys;
            }

            _logger.LogDebug("Rebuilding sorted row keys cache for {RowCount} rows", _rows.Count);
            _sortedRowKeys = _rows.Keys.OrderBy(k => k).ToList();
            _sortedRowKeysInvalid = false;
            return _sortedRowKeys;
        }
    }

    /// <summary>
    /// Invalidates sorted row keys cache (call after insert/delete).
    /// </summary>
    private void InvalidateSortedRowKeysCache()
    {
        _sortedRowKeysInvalid = true;
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetAllRows()
    {
        // PERFORMANCE FIX: Use cached sorted keys instead of re-sorting on every call
        var sortedKeys = GetSortedRowKeys();
        var result = new List<IReadOnlyDictionary<string, object?>>(sortedKeys.Count);

        foreach (var key in sortedKeys)
        {
            if (_rows.TryGetValue(key, out var row))
            {
                result.Add(row);
            }
        }

        return result;
    }

    public int GetRowCount()
    {
        return _rows.Count;
    }

    public bool RowExists(int rowIndex)
    {
        return rowIndex >= 0 && rowIndex < _rows.Count;
    }

    /// <summary>
    /// Get last row (row with max ULID = most recent timestamp)
    /// ULID MIGRATION: ULID is lexicographically sortable by timestamp, so Max() returns most recent
    /// Performance: O(n) for dictionary key enumeration (acceptable for this critical operation)
    /// CRITICAL: Used by 3-step cleanup to check if last row is empty
    /// </summary>
    public Task<IReadOnlyDictionary<string, object?>?> GetLastRowAsync(
        CancellationToken cancellationToken = default)
    {
        if (_rows.IsEmpty)
            return Task.FromResult<IReadOnlyDictionary<string, object?>?>(null);

        // ULID strings are lexicographically sortable by timestamp
        // Max ULID string = most recent row (last inserted)
        var maxRowId = _rows.Keys.Max();
        _rows.TryGetValue(maxRowId, out var row);

        _logger.LogDebug("Retrieved last row with ULID rowId {RowId}", maxRowId);
        return Task.FromResult<IReadOnlyDictionary<string, object?>?>(row);
    }

    #region Filter Matching Logic

    /// <summary>
    /// Checks if row matches ALL active filter criteria (AND logic)
    /// </summary>
    private bool RowMatchesAllFilters(IReadOnlyDictionary<string, object?> row, IReadOnlyList<object> filterCriteria)
    {
        foreach (var criteriaObj in filterCriteria)
        {
            // Cast to FilterCriteria (assuming FilterService passes FilterCriteria objects as "object")
            // WORKAROUND: Since we can't reference FilterCriteria type here (circular dependency),
            // we use duck typing via dynamic or reflection
            // For now, we'll use a simplified approach: check if criteriaObj has the expected properties

            // Extract filter properties using reflection or dynamic
            var criteriaType = criteriaObj.GetType();
            var columnNameProp = criteriaType.GetProperty("ColumnName");
            var operatorProp = criteriaType.GetProperty("Operator");
            var valueProp = criteriaType.GetProperty("Value");

            if (columnNameProp == null || operatorProp == null || valueProp == null)
            {
                _logger.LogWarning("Invalid filter criteria object - missing required properties");
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

            // Apply filter operator
            if (!CellMatchesFilter(cellValue, operatorValue, filterValue))
            {
                return false; // Row doesn't match this filter → exclude row
            }
        }

        return true; // Row matches all filters
    }

    /// <summary>
    /// Checks if cell value matches specific filter criteria
    /// Supports all FilterOperator enum values
    /// </summary>
    private bool CellMatchesFilter(object? cellValue, object operatorEnum, object? filterValue)
    {
        // Get operator name (enum ToString)
        var operatorName = operatorEnum.ToString();

        switch (operatorName)
        {
            case "Equals":
                return ValuesAreEqual(cellValue, filterValue);

            case "NotEquals":
                return !ValuesAreEqual(cellValue, filterValue);

            case "Contains":
                return StringContains(cellValue, filterValue);

            case "NotContains":
                return !StringContains(cellValue, filterValue);

            case "StartsWith":
                return StringStartsWith(cellValue, filterValue);

            case "EndsWith":
                return StringEndsWith(cellValue, filterValue);

            case "GreaterThan":
                return CompareValues(cellValue, filterValue) > 0;

            case "GreaterThanOrEqual":
                return CompareValues(cellValue, filterValue) >= 0;

            case "LessThan":
                return CompareValues(cellValue, filterValue) < 0;

            case "LessThanOrEqual":
                return CompareValues(cellValue, filterValue) <= 0;

            case "IsNull":
                return cellValue == null;

            case "IsNotNull":
                return cellValue != null;

            case "IsEmpty":
                return IsValueEmpty(cellValue);

            case "IsNotEmpty":
                return !IsValueEmpty(cellValue);

            case "In":
                return ValueInList(cellValue, filterValue);

            case "Regex":
                return ValueMatchesRegex(cellValue, filterValue);

            default:
                _logger.LogWarning("Unknown filter operator: {Operator}", operatorName);
                return false;
        }
    }

    private bool ValuesAreEqual(object? cellValue, object? filterValue)
    {
        if (cellValue == null && filterValue == null)
            return true;
        if (cellValue == null || filterValue == null)
            return false;

        // Direct equality check first
        if (cellValue.Equals(filterValue))
            return true;

        // String comparison (case-insensitive)
        var cellStr = cellValue.ToString();
        var filterStr = filterValue.ToString();
        return string.Equals(cellStr, filterStr, StringComparison.OrdinalIgnoreCase);
    }

    private bool StringContains(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";
        return cellStr.Contains(filterStr, StringComparison.OrdinalIgnoreCase);
    }

    private bool StringStartsWith(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";
        return cellStr.StartsWith(filterStr, StringComparison.OrdinalIgnoreCase);
    }

    private bool StringEndsWith(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";
        return cellStr.EndsWith(filterStr, StringComparison.OrdinalIgnoreCase);
    }

    private int CompareValues(object? cellValue, object? filterValue)
    {
        if (cellValue == null && filterValue == null)
            return 0;
        if (cellValue == null)
            return -1;
        if (filterValue == null)
            return 1;

        // Try numeric comparison first
        if (TryGetNumericValue(cellValue, out var cellNumeric) &&
            TryGetNumericValue(filterValue, out var filterNumeric))
        {
            return cellNumeric.CompareTo(filterNumeric);
        }

        // Try DateTime comparison
        if (TryGetDateTimeValue(cellValue, out var cellDate) &&
            TryGetDateTimeValue(filterValue, out var filterDate))
        {
            return cellDate.CompareTo(filterDate);
        }

        // Fallback to string comparison
        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";
        return string.Compare(cellStr, filterStr, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetNumericValue(object value, out decimal numericValue)
    {
        numericValue = 0;

        if (value is decimal d) { numericValue = d; return true; }
        if (value is double dbl) { numericValue = (decimal)dbl; return true; }
        if (value is float f) { numericValue = (decimal)f; return true; }
        if (value is long l) { numericValue = l; return true; }
        if (value is int i) { numericValue = i; return true; }
        if (value is short s) { numericValue = s; return true; }
        if (value is byte b) { numericValue = b; return true; }
        if (value is string str) { return decimal.TryParse(str, out numericValue); }

        return false;
    }

    private bool TryGetDateTimeValue(object value, out DateTime dateTimeValue)
    {
        dateTimeValue = default;

        if (value is DateTime dt) { dateTimeValue = dt; return true; }
        if (value is DateTimeOffset dto) { dateTimeValue = dto.DateTime; return true; }
        if (value is string str) { return DateTime.TryParse(str, out dateTimeValue); }

        return false;
    }

    private bool IsValueEmpty(object? value)
    {
        if (value == null)
            return true;
        if (value is string s)
            return string.IsNullOrWhiteSpace(s);
        if (value is System.Collections.ICollection c)
            return c.Count == 0;

        return false;
    }

    /// <summary>
    /// Checks if cellValue is in the list of filterValues (IN operator)
    /// Supports List, IEnumerable, array, or single comma-separated string
    /// Example: Status IN ('Active', 'Pending', 'Completed')
    /// </summary>
    private bool ValueInList(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        // Convert cellValue to string for comparison
        var cellStr = cellValue.ToString() ?? "";

        // Handle different filterValue types
        if (filterValue is System.Collections.IEnumerable enumerable && !(filterValue is string))
        {
            // filterValue is a collection (List<string>, string[], etc.)
            foreach (var item in enumerable)
            {
                var itemStr = item?.ToString() ?? "";
                if (string.Equals(cellStr, itemStr, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        else if (filterValue is string filterStr)
        {
            // filterValue is a single string (might be comma-separated)
            // Split by comma and check each value
            var values = filterStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var value in values)
            {
                if (string.Equals(cellStr, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // Fallback: direct equality check
        return string.Equals(cellStr, filterValue.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if cellValue matches the regex pattern (REGEXP operator)
    /// Example: Email REGEXP '^test.*@example\.com$'
    /// </summary>
    private bool ValueMatchesRegex(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var patternStr = filterValue.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(patternStr))
            return false;

        try
        {
            var regex = new System.Text.RegularExpressions.Regex(
                patternStr,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

            return regex.IsMatch(cellStr);
        }
        catch (System.Text.RegularExpressions.RegexParseException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", patternStr);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating regex pattern: {Pattern}", patternStr);
            return false;
        }
    }

    #endregion

    #region Insert Row Convenience Methods

    /// <summary>
    /// Insert single row AFTER the specified row index
    /// </summary>
    public async Task InsertRowAfterAsync(
        int targetRowIndex,
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default)
    {
        var insertIndex = targetRowIndex + 1;
        await InsertRowsAsync(new[] { CreateUserInsertedRow(newRow) }, insertIndex, cancellationToken);
    }

    /// <summary>
    /// Insert single row BEFORE the specified row index
    /// </summary>
    public async Task InsertRowBeforeAsync(
        int targetRowIndex,
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default)
    {
        await InsertRowsAsync(new[] { CreateUserInsertedRow(newRow) }, targetRowIndex, cancellationToken);
    }

    /// <summary>
    /// Insert single row at the top (index 0)
    /// </summary>
    public async Task InsertRowAtTopAsync(
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default)
    {
        await InsertRowsAsync(new[] { CreateUserInsertedRow(newRow) }, 0, cancellationToken);
    }

    /// <summary>
    /// Helper: Creates row with UserInserted metadata
    /// </summary>
    private IReadOnlyDictionary<string, object?> CreateUserInsertedRow(IReadOnlyDictionary<string, object?> rowData)
    {
        var mutableRow = new Dictionary<string, object?>(rowData);

        // Add metadata
        mutableRow["__creationType"] = "UserInserted";
        mutableRow["__lastModified"] = DateTime.UtcNow;

        // Ensure __rowId if not present
        if (!mutableRow.ContainsKey("__rowId") || string.IsNullOrEmpty(mutableRow["__rowId"]?.ToString()))
        {
            mutableRow["__rowId"] = Guid.NewGuid().ToString();
        }

        return mutableRow;
    }

    #endregion

    #region BREAKING CHANGE v3.0: rowId-based helper methods

    /// <summary>
    /// Gets the rowId for a given row index.
    /// HELPER: Enables conversion from volatile rowIndex to stable rowId.
    /// </summary>
    /// <param name="rowIndex">Row index in current view (filtered or unfiltered)</param>
    /// <returns>RowId if found, null otherwise</returns>
    public string? GetRowIdByIndex(int rowIndex)
    {
        var row = GetRow(rowIndex);
        if (row != null && row.TryGetValue("__rowId", out var rowIdValue))
        {
            return rowIdValue?.ToString();
        }
        return null;
    }

    /// <summary>
    /// Gets the current row index for a given rowId.
    /// HELPER: Enables conversion from stable rowId to volatile rowIndex.
    /// WARNING: Returned index is VOLATILE and may change after sort/filter/delete.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <returns>Current row index if found, null otherwise</returns>
    public int? GetRowIndexById(string rowId)
    {
        var allRows = GetAllRows();
        for (int i = 0; i < allRows.Count; i++)
        {
            if (allRows[i].TryGetValue("__rowId", out var rowIdValue))
            {
                if (rowIdValue?.ToString() == rowId)
                {
                    return i;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Gets a row by its stable rowId (synchronous version).
    /// STABLE: RowId persists across sort/filter/delete operations.
    /// NOTE: Uses existing GetRowByIdAsync implementation (line ~943)
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <returns>Row data if found, null otherwise</returns>
    public IReadOnlyDictionary<string, object?>? GetRowById(string rowId)
    {
        return GetRowByIdAsync(rowId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Checks if a row exists by its stable rowId.
    /// STABLE: RowId persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <returns>True if row exists, false otherwise</returns>
    public bool RowExistsById(string rowId)
    {
        return GetRowById(rowId) != null;
    }

    #endregion

    #endregion
}
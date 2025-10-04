using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using System.Runtime.CompilerServices;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;

/// <summary>
/// In-memory implementation of IRowStore for high-performance data operations
/// </summary>
internal sealed class InMemoryRowStore : Interfaces.IRowStore
{
    private readonly ILogger<InMemoryRowStore> _logger;
    private readonly ConcurrentDictionary<int, IReadOnlyDictionary<string, object?>> _rows = new();
    private readonly ConcurrentDictionary<int, List<ValidationError>> _validationErrors = new();
    private readonly object _modificationLock = new();
    private volatile int _nextRowId = 0;
    private IReadOnlyList<object> _filterCriteria = Array.Empty<object>();
    private bool _hasValidationState = false;

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
            }

            _logger.LogDebug("Upserted {AffectedRows} rows", affectedRows);
            return affectedRows;
        }, cancellationToken);
    }

    /// <summary>
    /// Removes rows by condition
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
                var keysToRemove = new List<int>();

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
            }

            _logger.LogDebug("Removed {RemovedCount} rows", removedCount);
            return removedCount;
        }, cancellationToken);
    }

    /// <summary>
    /// Clears all rows
    /// </summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                var count = _rows.Count;
                _rows.Clear();
                _nextRowId = 0;
                _logger.LogDebug("Cleared all rows: {ClearedCount} rows removed", count);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Adds multiple rows in batch
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

                    var rowId = Interlocked.Increment(ref _nextRowId);
                    var rowWithId = new Dictionary<string, object?>(row)
                    {
                        ["__rowId"] = rowId
                    };

                    _rows.TryAdd(rowId, rowWithId);
                    addedCount++;
                }
            }

            _logger.LogDebug("Added {AddedCount} rows in batch", addedCount);
            return addedCount;
        }, cancellationToken);
    }

    /// <summary>
    /// Stream rows in batches - IRowStore implementation
    /// </summary>
    public async IAsyncEnumerable<IReadOnlyList<IReadOnlyDictionary<string, object?>>> StreamRowsAsync(
        bool onlyFiltered = false,
        int batchSize = 1000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Streaming rows in batches: onlyFiltered={OnlyFiltered}, batchSize={BatchSize}",
            onlyFiltered, batchSize);

        await Task.Yield(); // Make it truly async

        var allRows = _rows.Values
            .Where(row => !onlyFiltered || IsRowVisible(row))
            .ToList();

        for (int i = 0; i < allRows.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = allRows.Skip(i).Take(batchSize).ToList();
            yield return batch;
        }
    }

    /// <summary>
    /// Get row count - IRowStore implementation
    /// </summary>
    public Task<long> GetRowCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_rows.Count);
    }

    /// <summary>
    /// Get filtered row count - IRowStore implementation
    /// </summary>
    public Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default)
    {
        var count = _rows.Values.Count(IsRowVisible);
        return Task.FromResult((long)count);
    }

    /// <summary>
    /// Get all rows - IRowStore implementation
    /// </summary>
    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = _rows.Values.ToList();
        return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>(rows);
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
    /// </summary>
    public Task WriteValidationResultsAsync(
        IEnumerable<ValidationError> results,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Writing validation results");

        _validationErrors.Clear();
        foreach (var error in results)
        {
            var rowId = error.RowIndex;
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
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_hasValidationState);
    }

    /// <summary>
    /// Check if all non-empty rows are valid - IRowStore implementation
    /// </summary>
    public Task<bool> AreAllNonEmptyRowsMarkedValidAsync(
        bool onlyFiltered,
        CancellationToken cancellationToken = default)
    {
        if (!_hasValidationState)
            return Task.FromResult(false);

        var rowsToCheck = _rows.Values
            .Where(row => !onlyFiltered || IsRowVisible(row))
            .Where(row => !IsRowEmpty(row));

        var allValid = !_validationErrors.Any() ||
                       rowsToCheck.All(row => !_validationErrors.ContainsKey(GetRowId(row)));

        return Task.FromResult(allValid);
    }

    /// <summary>
    /// Get validation errors - IRowStore implementation
    /// </summary>
    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default)
    {
        var errors = _validationErrors.Values
            .SelectMany(list => list)
            .Where(error => !onlyFiltered || IsRowVisible(_rows.GetValueOrDefault(error.RowIndex)
                ?? new Dictionary<string, object?>()))
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
    /// Set filter criteria - IRowStore implementation
    /// </summary>
    public void SetFilterCriteria(IReadOnlyList<object> filterCriteria)
    {
        _filterCriteria = filterCriteria ?? Array.Empty<object>();
        _logger.LogDebug("Filter criteria updated: {Count} filters", _filterCriteria.Count);
    }

    /// <summary>
    /// Get filter criteria - IRowStore implementation
    /// </summary>
    public IReadOnlyList<object> GetFilterCriteria()
    {
        return _filterCriteria;
    }

    /// <summary>
    /// Get validation errors for specific row - IRowStore implementation
    /// </summary>
    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(
        string rowId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(rowId) || !int.TryParse(rowId, out var rowIdInt))
            return Task.FromResult<IReadOnlyList<ValidationError>>(Array.Empty<ValidationError>());

        if (_validationErrors.TryGetValue(rowIdInt, out var errors))
        {
            return Task.FromResult<IReadOnlyList<ValidationError>>(errors.ToList());
        }

        return Task.FromResult<IReadOnlyList<ValidationError>>(Array.Empty<ValidationError>());
    }

    /// <summary>
    /// Remove rows by IDs - IRowStore implementation
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

                    if (!int.TryParse(rowId, out var rowIdInt))
                        continue;

                    if (_rows.TryRemove(rowIdInt, out _))
                    {
                        // Also remove validation errors for this row
                        _validationErrors.TryRemove(rowIdInt, out _);
                        removedCount++;
                    }
                }

                _logger.LogInformation("Removed {RemovedCount} of {RequestedCount} rows", removedCount, rowIds.Count());
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Get a specific row by index - IRowStore implementation
    /// </summary>
    public Task<IReadOnlyDictionary<string, object?>?> GetRowAsync(
        int rowIndex,
        CancellationToken cancellationToken = default)
    {
        var allRows = _rows.Values.OrderBy(r => GetOrAssignRowId(r)).ToList();

        if (rowIndex < 0 || rowIndex >= allRows.Count)
        {
            return Task.FromResult<IReadOnlyDictionary<string, object?>?>(null);
        }

        return Task.FromResult<IReadOnlyDictionary<string, object?>?>(allRows[rowIndex]);
    }

    /// <summary>
    /// Update a specific row by index - IRowStore implementation
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
                var allRows = _rows.Values.OrderBy(r => GetOrAssignRowId(r)).ToList();

                if (rowIndex < 0 || rowIndex >= allRows.Count)
                {
                    return false;
                }

                var oldRow = allRows[rowIndex];
                var rowId = GetRowId(oldRow);

                if (rowId >= 0 && _rows.TryUpdate(rowId, rowData, oldRow))
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

    private bool IsRowEmpty(IReadOnlyDictionary<string, object?> row)
    {
        // Row is empty if all non-special columns are null or empty
        return row.All(kvp =>
            kvp.Key.StartsWith("__") ||
            kvp.Value == null ||
            (kvp.Value is string str && string.IsNullOrWhiteSpace(str)));
    }

    private int GetRowId(IReadOnlyDictionary<string, object?> row)
    {
        if (row.TryGetValue("__rowId", out var rowIdValue) && rowIdValue is int rowId)
        {
            return rowId;
        }
        return -1;
    }

    private int GetOrAssignRowId(IReadOnlyDictionary<string, object?> row)
    {
        // Try to get existing row ID
        if (row.TryGetValue("__rowId", out var rowIdValue) && rowIdValue is int existingId)
        {
            return existingId;
        }

        // Assign new row ID
        return Interlocked.Increment(ref _nextRowId);
    }

    #endregion
}
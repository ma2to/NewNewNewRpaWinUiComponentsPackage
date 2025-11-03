using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Strategies;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;

/// <summary>
/// ✅ UNIFIED ROW STORE (STRATEGY PATTERN - PART 2)
/// THIN ORCHESTRATOR: Delegates storage to IStorageStrategy, validation to IValidationStrategy.
/// ZERO DUPLICATE CODE: All storage logic is in strategies (InMemory vs SQLite).
/// BUSINESS LOGIC ONLY: Filter/sort coordination, event management, metadata handling.
/// </summary>
internal sealed class UnifiedRowStore : IRowStore
{
    // ========== STRATEGY DEPENDENCIES ==========

    private readonly IStorageStrategy _storageStrategy;
    private readonly IValidationStrategy _validationStrategy;
    private readonly ILogger<UnifiedRowStore>? _logger;

    // ========== FILTER/SORT STATE ==========

    private IReadOnlyList<object>? _filterCriteria;
    private Features.Filter.Models.FilterExpression? _filterExpression;

    // ========== CONSTRUCTOR ==========

    public UnifiedRowStore(
        IStorageStrategy storageStrategy,
        IValidationStrategy validationStrategy,
        ILogger<UnifiedRowStore>? logger = null)
    {
        _storageStrategy = storageStrategy ?? throw new ArgumentNullException(nameof(storageStrategy));
        _validationStrategy = validationStrategy ?? throw new ArgumentNullException(nameof(validationStrategy));
        _logger = logger;

        _logger?.LogInformation("UnifiedRowStore created with storage={Storage}, validation={Validation}",
            _storageStrategy.StrategyName, _validationStrategy.StrategyName);
    }

    // ========== IRowStore IMPLEMENTATION - STORAGE DELEGATION ==========

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsRangeAsync(
        long startIndex,
        int count,
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default)
    {
        return await _storageStrategy.GetRowsRangeAsync(startIndex, count, onlyFiltered, cancellationToken);
    }

    public async Task<long> GetRowCountAsync(bool onlyFiltered, CancellationToken cancellationToken = default)
    {
        return await _storageStrategy.GetRowCountAsync(onlyFiltered, cancellationToken);
    }

    public Task<long> GetRowCountAsync(CancellationToken cancellationToken = default)
    {
        return GetRowCountAsync(onlyFiltered: false, cancellationToken);
    }

    public Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default)
    {
        // ✅ PROBLEM 2 FIX: Delegate to storage strategy which implements empty row filtering
        // Both InMemoryStorageStrategy and HybridRowStore implement GetRowCountAsync with empty row exclusion
        // This ensures consistent behavior: InMemory filters in-memory, Hybrid filters via SQL
        return GetRowCountAsync(onlyFiltered: true, cancellationToken);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        bool onlyFiltered,
        CancellationToken cancellationToken = default)
    {
        return await _storageStrategy.GetAllRowsAsync(onlyFiltered, cancellationToken);
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAllRowsAsync(onlyFiltered: false, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(
        string rowId,
        CancellationToken cancellationToken = default)
    {
        return await _storageStrategy.GetRowByIdAsync(rowId, cancellationToken);
    }

    public async Task<bool> UpdateRowByIdAsync(
        string rowId,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default)
    {
        await _storageStrategy.UpdateRowByIdAsync(rowId, rowData, cancellationToken);
        return true;
    }

    public async Task<bool> RemoveRowByIdAsync(
        string rowId,
        CancellationToken cancellationToken = default)
    {
        await _storageStrategy.DeleteRowByIdAsync(rowId, cancellationToken);
        return true;
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _storageStrategy.ClearAsync(cancellationToken);

        // Clear validation state when storage is cleared
        await _validationStrategy.ClearValidationStateAsync(cancellationToken);

        // ✅ FIX: Run ClearValidationCache() asynchronously to avoid UI thread blocking
        await Task.Run(() => _validationStrategy.ClearValidationCache(), cancellationToken);
    }

    public async Task ReplaceAllRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        // ✅ CRITICAL FIX PART 1: PRESERVE validation errors BEFORE replacing rows
        // WHY: Validation errors are tied to rowId (ULID), which is STABLE across sort/filter operations
        // EXAMPLE: Row with rowId=01K9335YYVHEFTDCMG2DG9GTXJ has error "Column_1 is required"
        //          After sort, same rowId is at different position, but error must persist
        // PERFORMANCE: Fast - just reading from cache/storage, no re-validation needed
        IReadOnlyList<ValidationError>? existingErrors = null;
        try
        {
            existingErrors = await _validationStrategy.GetValidationErrorsAsync(
                onlyFiltered: false,
                onlyChecked: false,
                cancellationToken);

            if (existingErrors != null && existingErrors.Any())
            {
                _logger?.LogInformation("✅ VALIDATION PRESERVATION: Captured {Count} existing validation errors before ReplaceAllRowsAsync",
                    existingErrors.Count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to capture existing validation errors - continuing without preservation");
            existingErrors = null; // Continue without preservation on error
        }

        // Replace rows (this may internally clear validation cache via strategy)
        await _storageStrategy.ReplaceAllRowsAsync(rows, cancellationToken);

        // ✅ CRITICAL FIX PART 2: RESTORE validation errors AFTER replacing rows
        // WHY: RowIds are stable (ULID-based), so errors can be re-applied to same logical rows
        // NOTE: This is FAST - no re-validation, just cache restoration (typically <10ms for 1000 errors)
        // REASON: Avoids expensive re-validation after sort/filter operations (saves 1-2 seconds)
        if (existingErrors != null && existingErrors.Any())
        {
            try
            {
                // Group errors by rowId for batch write (more efficient than individual writes)
                var validationDict = existingErrors
                    .GroupBy(e => e.RowId)
                    .ToDictionary(g => g.Key, g => g.ToArray());

                // Batch write all validation errors back to storage
                await _validationStrategy.WriteValidationResultsBatchAsync(validationDict, cancellationToken);

                _logger?.LogInformation("✅ VALIDATION PRESERVATION: Restored {Count} validation errors after ReplaceAllRowsAsync " +
                    "({RowCount} rows affected, 0ms validation overhead - used cached results)",
                    existingErrors.Count, validationDict.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to restore validation errors after ReplaceAllRowsAsync - UI may show incomplete validation state");
                // Don't throw - allow operation to continue even if restoration fails
            }
        }
        else
        {
            // If no errors existed, clear cache as before (for safety and consistency)
            await Task.Run(() => _validationStrategy.ClearValidationCache(), cancellationToken);
            _logger?.LogDebug("No validation errors to preserve - cache cleared normally");
        }
    }

    public Task PersistRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        // PersistRowsAsync = ReplaceAllRowsAsync (legacy compatibility)
        return ReplaceAllRowsAsync(rows, cancellationToken);
    }

    public async Task<int> AddRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rowsData,
        CancellationToken cancellationToken = default)
    {
        var count = await _storageStrategy.AddRowsAsync(rowsData, cancellationToken);

        // ✅ FIX: Run ClearValidationCache() asynchronously to avoid UI thread blocking
        await Task.Run(() => _validationStrategy.ClearValidationCache(), cancellationToken);

        return count;
    }

    public async Task<int> AddRowAsync(
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default)
    {
        await _storageStrategy.AddRowsAsync(new[] { rowData }, cancellationToken);

        var count = await _storageStrategy.GetRowCountAsync(false, cancellationToken);
        return (int)count - 1; // Return index of newly added row
    }

    public async Task InsertRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex,
        CancellationToken cancellationToken = default)
    {
        await _storageStrategy.InsertRowsAsync(rows, startIndex, cancellationToken);

        // ✅ FIX: Run ClearValidationCache() asynchronously to avoid UI thread blocking
        await Task.Run(() => _validationStrategy.ClearValidationCache(), cancellationToken);
    }

    public async Task<string> InsertRowAtIndexAsync(
        int index,
        IReadOnlyDictionary<string, object?>? rowData,
        CancellationToken cancellationToken = default)
    {
        var rowId = await _storageStrategy.InsertRowAtIndexAsync(index, rowData, cancellationToken);

        // ✅ FIX: Run ClearValidationCache() asynchronously to avoid UI thread blocking (DEADLOCK FIX)
        await Task.Run(() => _validationStrategy.ClearValidationCache(), cancellationToken);

        return rowId;
    }

    public Task InsertRowAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default)
    {
        // Insert at index - delegate to InsertRowAtIndexAsync
        return InsertRowAtIndexAsync(rowIndex, rowData, cancellationToken);
    }

    public async Task RemoveRowAsync(
        int rowIndex,
        CancellationToken cancellationToken = default)
    {
        var rowId = _storageStrategy.GetRowIdByIndex(rowIndex);
        if (rowId != null)
        {
            await _storageStrategy.DeleteRowByIdAsync(rowId, cancellationToken);
        }
    }

    public async Task<int> RemoveRowsAsync(
        IEnumerable<int> rowIndices,
        CancellationToken cancellationToken = default)
    {
        var indices = rowIndices.OrderByDescending(i => i).ToList(); // Remove from end to start
        int removedCount = 0;

        foreach (var index in indices)
        {
            var rowId = _storageStrategy.GetRowIdByIndex(index);
            if (rowId != null)
            {
                await _storageStrategy.DeleteRowByIdAsync(rowId, cancellationToken);
                removedCount++;
            }
        }

        return removedCount;
    }

    public async Task RemoveRowsAsync(
        IEnumerable<string> rowIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var rowId in rowIds)
        {
            await _storageStrategy.DeleteRowByIdAsync(rowId, cancellationToken);
        }
    }

    public Task ClearAllRowsAsync(CancellationToken cancellationToken = default)
    {
        return ClearAsync(cancellationToken);
    }

    public async Task EnsureInitialEmptyRowAsync(
        IEnumerable<string> columnNames,
        CancellationToken cancellationToken = default)
    {
        var count = await GetRowCountAsync(cancellationToken);
        if (count > 0)
        {
            _logger?.LogDebug("EnsureInitialEmptyRowAsync: Store not empty, skipping");
            return;
        }

        _logger?.LogInformation("EnsureInitialEmptyRowAsync: Creating initial empty row");

        // Create empty row with all columns set to null
        var emptyRow = columnNames.ToDictionary(col => col, col => (object?)null);

        await AddRowsAsync(new[] { emptyRow }, cancellationToken);
    }

    public async Task InitializeEmptyRowsAsync(
        IEnumerable<string> columnNames,
        int rowCount,
        CancellationToken cancellationToken = default)
    {
        if (rowCount <= 0)
        {
            _logger?.LogWarning("InitializeEmptyRowsAsync: rowCount must be positive, got {RowCount}", rowCount);
            return;
        }

        _logger?.LogInformation("InitializeEmptyRowsAsync: Creating {RowCount} empty rows", rowCount);

        var columnList = columnNames.ToList();
        var emptyRows = new List<IReadOnlyDictionary<string, object?>>(rowCount);

        // Create N empty rows (all columns set to null)
        for (int i = 0; i < rowCount; i++)
        {
            var emptyRow = columnList.ToDictionary(col => col, col => (object?)null);
            emptyRows.Add(emptyRow);
        }

        // Append all empty rows in bulk
        await AddRowsAsync(emptyRows, cancellationToken);

        _logger?.LogInformation("InitializeEmptyRowsAsync: Successfully created {RowCount} empty rows", rowCount);
    }

    // ========== SYNCHRONOUS API (delegates to async) ==========

    public IReadOnlyDictionary<string, object?>? GetRow(int rowIndex)
    {
        return GetRowAsync(rowIndex).GetAwaiter().GetResult();
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetAllRows()
    {
        return GetAllRowsAsync().GetAwaiter().GetResult();
    }

    public int GetRowCount()
    {
        return (int)GetRowCountAsync().GetAwaiter().GetResult();
    }

    public bool RowExists(int rowIndex)
    {
        var count = GetRowCount();
        return rowIndex >= 0 && rowIndex < count;
    }

    public async Task<IReadOnlyDictionary<string, object?>?> GetRowAsync(
        int rowIndex,
        CancellationToken cancellationToken = default)
    {
        var rowId = _storageStrategy.GetRowIdByIndex(rowIndex);
        if (rowId == null)
            return null;

        return await _storageStrategy.GetRowByIdAsync(rowId, cancellationToken);
    }

    public async Task<bool> UpdateRowAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default)
    {
        var rowId = _storageStrategy.GetRowIdByIndex(rowIndex);
        if (rowId == null)
            return false;

        await _storageStrategy.UpdateRowByIdAsync(rowId, rowData, cancellationToken);
        return true;
    }

    // ========== ROW ID HELPER METHODS ==========

    public string? GetRowIdByIndex(int rowIndex)
    {
        return _storageStrategy.GetRowIdByIndex(rowIndex);
    }

    public int? GetRowIndexById(string rowId)
    {
        return _storageStrategy.GetRowIndexById(rowId);
    }

    public IReadOnlyDictionary<string, object?>? GetRowById(string rowId)
    {
        return GetRowByIdAsync(rowId).GetAwaiter().GetResult();
    }

    public bool RowExistsById(string rowId)
    {
        return GetRowById(rowId) != null;
    }

    public async Task<IReadOnlyDictionary<string, object?>?> GetLastRowAsync(
        CancellationToken cancellationToken = default)
    {
        var count = await GetRowCountAsync(cancellationToken);
        if (count == 0)
            return null;

        var rowId = _storageStrategy.GetRowIdByIndex((int)(count - 1));
        if (rowId == null)
            return null;

        return await _storageStrategy.GetRowByIdAsync(rowId, cancellationToken);
    }

    // ========== FILTER & SORT (delegate to storage strategy) ==========

    public void SetSortCriteria(string columnName, SortDirection direction)
    {
        _storageStrategy.SetSortCriteria(columnName, direction);
    }

    public void ClearSortCriteria()
    {
        _storageStrategy.ClearSortCriteria();
    }

    public void SetFilterCriteria(IReadOnlyList<object>? filterCriteria)
    {
        _filterCriteria = filterCriteria;
        _logger?.LogInformation("✅ PROBLEM 2 FIX: SetFilterCriteria - delegating to storage strategy (count: {Count})", filterCriteria?.Count ?? 0);

        // ✅ PROBLEM 2 FIX: Delegate to storage strategy (InMemory builds _filteredRowIds, Hybrid builds SQL WHERE clause)
        _storageStrategy.SetFilterCriteria(filterCriteria);
    }

    public void ClearFilterCriteria()
    {
        _filterCriteria = null;
        _logger?.LogInformation("✅ PROBLEM 2 FIX: ClearFilterCriteria - delegating to storage strategy");

        // ✅ PROBLEM 2 FIX: Delegate to storage strategy
        _storageStrategy.ClearFilterCriteria();
    }

    /// <summary>
    /// ✅ PROBLEM 2 FIX: Check if any filter is currently active
    /// </summary>
    public bool HasActiveFilter()
    {
        bool hasFilter = _filterCriteria != null && _filterCriteria.Count > 0;
        _logger?.LogDebug("✅ PROBLEM 2 FIX (UnifiedRowStore): HasActiveFilter={HasFilter} (criteria count: {Count})",
            hasFilter, _filterCriteria?.Count ?? 0);
        return hasFilter;
    }

    public IReadOnlyList<object> GetFilterCriteria()
    {
        return _filterCriteria ?? Array.Empty<object>();
    }

    public void SetFilterExpression(Features.Filter.Models.FilterExpression? expression)
    {
        _filterExpression = expression;
        _logger?.LogInformation("SetFilterExpression: Setting complex filter expression (null: {IsNull})", expression == null);
        // TODO: Implement filter expression SQL building and delegation to storage strategy
    }

    public Features.Filter.Models.FilterExpression? GetFilterExpression()
    {
        return _filterExpression;
    }

    public int? MapFilteredIndexToOriginalIndex(int filteredIndex)
    {
        // TODO: Implement in future phase (Filter/Sort/Search Integration)
        if (_filterCriteria == null || _filterCriteria.Count == 0)
            return filteredIndex;

        return null;
    }

    // ========== SEARCH (delegate to storage strategy) ==========

    public async Task<IReadOnlyList<string>> SearchAsync(
        string searchText,
        string[]? targetColumns = null,
        bool caseSensitive = false,
        CancellationToken cancellationToken = default)
    {
        return await _storageStrategy.SearchAsync(searchText, targetColumns, caseSensitive, cancellationToken);
    }

    // ========== VALIDATION (delegate to validation strategy) ==========

    public async Task WriteValidationResultsAsync(
        IEnumerable<ValidationError> results,
        CancellationToken cancellationToken = default)
    {
        await _validationStrategy.WriteValidationResultsAsync(results, cancellationToken);
    }

    public async Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default)
    {
        return await _validationStrategy.GetValidationErrorsAsync(onlyFiltered, onlyChecked, cancellationToken);
    }

    public async Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(
        string rowId,
        CancellationToken cancellationToken = default)
    {
        return await _validationStrategy.GetValidationErrorsForRowAsync(rowId, cancellationToken);
    }

    public async Task<bool> HasValidationStateForScopeAsync(
        bool onlyFiltered,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default)
    {
        return await _validationStrategy.HasValidationStateAsync(onlyFiltered, onlyChecked, cancellationToken);
    }

    public async Task<bool> AreAllNonEmptyRowsMarkedValidAsync(
        bool onlyFiltered,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default)
    {
        return await _validationStrategy.AreAllNonEmptyRowsMarkedValidAsync(onlyFiltered, onlyChecked, cancellationToken);
    }

    public async Task ClearValidationStateAsync(CancellationToken cancellationToken = default)
    {
        await _validationStrategy.ClearValidationStateAsync(cancellationToken);
    }

    public bool IsRowValidationCached(string rowId)
    {
        return _validationStrategy.IsRowValidationCached(rowId);
    }

    public void MarkRowAsValidated(string rowId)
    {
        _validationStrategy.MarkRowAsValidated(rowId);
    }

    public void ClearValidationCache()
    {
        _validationStrategy.ClearValidationCache();
    }

    public async Task WriteValidationResultsBatchAsync(
        Dictionary<string, ValidationError[]> validationResults,
        CancellationToken cancellationToken = default)
    {
        await _validationStrategy.WriteValidationResultsBatchAsync(validationResults, cancellationToken);
    }

    // ========== STREAMING (delegate to storage strategy) ==========

    public async IAsyncEnumerable<IReadOnlyList<IReadOnlyDictionary<string, object?>>> StreamRowsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        int batchSize = 1000,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Simple implementation: Get all rows and yield in batches
        var allRows = await GetAllRowsAsync(onlyFiltered, cancellationToken);

        for (int i = 0; i < allRows.Count; i += batchSize)
        {
            var batch = allRows.Skip(i).Take(batchSize).ToList();
            yield return batch;
        }
    }

    // ========== MISSING IROWSTORE METHODS ==========

    public async Task AppendRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        await _storageStrategy.AddRowsAsync(rows, cancellationToken);

        // ✅ FIX: Run ClearValidationCache() asynchronously to avoid UI thread blocking
        await Task.Run(() => _validationStrategy.ClearValidationCache(), cancellationToken);
    }

    public async Task InsertRowAfterAsync(
        int targetRowIndex,
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default)
    {
        // Insert after targetRowIndex means insert at targetRowIndex+1
        await InsertRowAtIndexAsync(targetRowIndex + 1, newRow, cancellationToken);
    }

    public async Task InsertRowBeforeAsync(
        int targetRowIndex,
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default)
    {
        // Insert before targetRowIndex means insert at targetRowIndex
        await InsertRowAtIndexAsync(targetRowIndex, newRow, cancellationToken);
    }

    public async Task InsertRowAtTopAsync(
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default)
    {
        // Insert at top means insert at index 0
        await InsertRowAtIndexAsync(0, newRow, cancellationToken);
    }

    public async Task<int> BulkUpdateRowsAsync(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> updates,
        CancellationToken cancellationToken = default)
    {
        if (updates == null || updates.Count == 0)
        {
            _logger?.LogDebug("BulkUpdateRowsAsync: No updates to perform");
            return 0;
        }

        int updatedCount = 0;
        foreach (var (rowId, rowData) in updates)
        {
            await _storageStrategy.UpdateRowByIdAsync(rowId, rowData, cancellationToken);
            updatedCount++;
        }

        _logger?.LogInformation("BulkUpdateRowsAsync: Updated {Count} rows", updatedCount);
        return updatedCount;
    }

    public async Task DeleteRowByIdAsync(
        string rowId,
        CancellationToken cancellationToken = default)
    {
        await _storageStrategy.DeleteRowByIdAsync(rowId, cancellationToken);
    }
}

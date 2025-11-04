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
        var rowsList = rows.ToList();

        _logger?.LogInformation("✅ VALIDATION PRESERVE: Starting ReplaceAllRowsAsync with {Count} rows", rowsList.Count);

        // ✅ STEP 1: Capture existing validation errors BEFORE replace (with OLD RowIds)
        IReadOnlyList<ValidationError>? existingErrors = null;
        try
        {
            existingErrors = await _validationStrategy.GetValidationErrorsAsync(
                onlyFiltered: false,
                onlyChecked: false,
                cancellationToken);

            if (existingErrors != null && existingErrors.Any())
            {
                _logger?.LogInformation("🔍 VALIDATION PRESERVE: Captured {ErrorCount} errors from {RowCount} unique rows",
                    existingErrors.Count, existingErrors.Select(e => e.RowId).Distinct().Count());
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to capture existing validation errors - continuing without preservation");
            existingErrors = null;
        }

        // ✅ STEP 2: Create mapping from OLD RowId → row content hash (for matching after replace)
        Dictionary<string, string>? oldRowIdToContentHash = null;
        try
        {
            var oldRows = await _storageStrategy.GetAllRowsAsync(onlyFiltered: false, cancellationToken);
            oldRowIdToContentHash = oldRows.ToDictionary(
                row => row.TryGetValue("__rowId", out var id) ? id?.ToString() ?? "" : "",
                row => ComputeRowContentHash(row)
            );

            _logger?.LogDebug("🔍 CONTENT HASH MAP: Created hash map for {Count} old rows", oldRowIdToContentHash.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create old row content hash map - validation preservation may be incomplete");
            oldRowIdToContentHash = null;
        }

        // ✅ STEP 3: Replace rows (NEW RowIds will be generated by storage strategy)
        await _storageStrategy.ReplaceAllRowsAsync(rowsList, cancellationToken);

        // ✅ STEP 4: Create mapping from row content hash → NEW RowId (after replace)
        Dictionary<string, string>? contentHashToNewRowId = null;
        try
        {
            var newRows = await _storageStrategy.GetAllRowsAsync(onlyFiltered: false, cancellationToken);
            contentHashToNewRowId = newRows.ToDictionary(
                row => ComputeRowContentHash(row),
                row => row.TryGetValue("__rowId", out var id) ? id?.ToString() ?? "" : ""
            );

            _logger?.LogDebug("🔍 CONTENT HASH MAP: Created hash map for {Count} new rows", contentHashToNewRowId.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create new row content hash map - validation preservation may be incomplete");
            contentHashToNewRowId = null;
        }

        // ✅ STEP 5: Restore validation errors with UPDATED RowIds
        if (existingErrors != null && existingErrors.Any() &&
            oldRowIdToContentHash != null && contentHashToNewRowId != null)
        {
            try
            {
                var updatedErrors = new Dictionary<string, ValidationError[]>();
                var matchedCount = 0;
                var unmatchedCount = 0;

                // Group errors by OLD RowId
                var errorsByOldRowId = existingErrors.GroupBy(e => e.RowId).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var (oldRowId, errors) in errorsByOldRowId)
                {
                    // Find OLD row's content hash
                    if (oldRowIdToContentHash.TryGetValue(oldRowId, out var contentHash))
                    {
                        // Find NEW RowId for same content
                        if (contentHashToNewRowId.TryGetValue(contentHash, out var newRowId) && !string.IsNullOrEmpty(newRowId))
                        {
                            // Create updated errors with NEW RowId
                            var updatedRowErrors = errors.Select(e => new ValidationError
                            {
                                RowId = newRowId,  // ← Mapped to new RowId
                                ColumnName = e.ColumnName,
                                Message = e.Message,
                                Severity = e.Severity,
                                RuleId = e.RuleId
                            }).ToArray();

                            updatedErrors[newRowId] = updatedRowErrors;
                            matchedCount++;

                            _logger?.LogTrace("✅ MAPPED: Old RowId {OldRowId} → New RowId {NewRowId} ({ErrorCount} errors)",
                                oldRowId, newRowId, updatedRowErrors.Length);
                        }
                        else
                        {
                            unmatchedCount++;
                            _logger?.LogWarning("❌ UNMATCHED: Old RowId {OldRowId} has content hash but no new RowId found", oldRowId);
                        }
                    }
                    else
                    {
                        unmatchedCount++;
                        _logger?.LogWarning("❌ UNMATCHED: Old RowId {OldRowId} not found in old rows", oldRowId);
                    }
                }

                // Write updated errors to validation storage
                if (updatedErrors.Any())
                {
                    await _validationStrategy.WriteValidationResultsBatchAsync(updatedErrors, cancellationToken);
                    _logger?.LogInformation("✅ VALIDATION RESTORE: Restored {MatchedCount} rows with validation errors (Unmatched: {UnmatchedCount})",
                        matchedCount, unmatchedCount);
                }
                else
                {
                    _logger?.LogWarning("⚠️ VALIDATION RESTORE: No errors could be matched/restored (Unmatched: {UnmatchedCount})",
                        unmatchedCount);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to restore validation errors after ReplaceAllRowsAsync - UI may show incomplete validation state");
            }
        }
        else if (existingErrors == null || !existingErrors.Any())
        {
            // No errors to preserve - clear cache
            await Task.Run(() => _validationStrategy.ClearValidationCache(), cancellationToken);
            _logger?.LogDebug("No validation errors to preserve - cache cleared normally");
        }
        else
        {
            _logger?.LogWarning("⚠️ VALIDATION RESTORE: Skipped due to missing content hash maps");
        }
    }

    /// <summary>
    /// ✅ HELPER METHOD: Compute content hash for row matching across ReplaceAllRowsAsync.
    /// Hashes all column values (except internal columns like __rowId, __rowNumber).
    /// Used to match OLD rows to NEW rows when RowIds change during replace operations.
    /// </summary>
    private string ComputeRowContentHash(IReadOnlyDictionary<string, object?> row)
    {
        try
        {
            // Hash all column values (except __rowId, __rowNumber, __createdAt, __isChecked, etc.)
            var contentColumns = row
                .Where(kvp => !kvp.Key.StartsWith("__"))  // Exclude internal columns
                .OrderBy(kvp => kvp.Key)  // Stable order for consistent hashing
                .Select(kvp => $"{kvp.Key}:{kvp.Value}");

            var content = string.Join("|", contentColumns);
            return ComputeHashString(content);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to compute row content hash - returning empty hash");
            return string.Empty;
        }
    }

    /// <summary>
    /// ✅ HELPER METHOD: Compute MD5 hash string from input text.
    /// </summary>
    private string ComputeHashString(string input)
    {
        try
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = md5.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to compute hash string - returning input as-is");
            return input;
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

    public void SetMultiColumnSortCriteria(IReadOnlyList<(string columnName, SortDirection direction)> sortColumns)
    {
        _storageStrategy.SetMultiColumnSortCriteria(sortColumns);
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

    public async Task ClearValidationErrorsForRowAsync(
        string rowId,
        CancellationToken cancellationToken = default)
    {
        await _validationStrategy.ClearValidationErrorsForRowAsync(rowId, cancellationToken);
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

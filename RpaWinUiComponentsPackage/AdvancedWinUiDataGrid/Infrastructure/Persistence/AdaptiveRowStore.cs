using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;

/// <summary>
/// Adaptive Row Store - automatically switches between InMemory and Hybrid storage
/// Strategy pattern with lazy migration:
/// - &lt; DataStorageThreshold: InMemoryRowStore
/// - &gt;= DataStorageThreshold: HybridRowStore (SQLite + WAL + fire-and-forget)
/// Thread-safe, transparent switching without data loss
/// </summary>
internal sealed class AdaptiveRowStore : IRowStore, IAsyncDisposable
{
    private readonly ILogger<AdaptiveRowStore>? _logger;
    private readonly AdvancedDataGridOptions _options;
    private readonly IServiceProvider _serviceProvider;

    private IRowStore _activeStore;
    private StorageStrategy _currentStrategy = StorageStrategy.InMemory;
    private readonly SemaphoreSlim _migrationLock = new(1, 1);
    private bool _isDisposed;

    // Strategy enum
    private enum StorageStrategy
    {
        InMemory,   // < DataStorageThreshold
        Hybrid      // >= DataStorageThreshold
    }

    public AdaptiveRowStore(
        ILogger<AdaptiveRowStore>? logger,
        AdvancedDataGridOptions options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options;
        _serviceProvider = serviceProvider;

        // Start with InMemory (lazy migration on threshold)
        _activeStore = CreateInMemoryStore();

        _logger?.LogInformation(
            "AdaptiveRowStore initialized: DataThreshold={DataThreshold}, ValidationThreshold={ValidationThreshold}",
            _options.DataStorageThreshold, _options.ValidationStorageThreshold);
    }

    /// <summary>
    /// Check if migration needed after row count change
    /// Called after AddRowsAsync, AppendRowsAsync, RemoveRowsAsync
    /// </summary>
    private async Task CheckAndMigrateIfNeededAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.UseAdaptiveStorage)
            return;

        var currentRowCount = await _activeStore.GetRowCountAsync(cancellationToken);
        var shouldUseHybrid = currentRowCount >= _options.DataStorageThreshold;

        // Migration needed?
        if (shouldUseHybrid && _currentStrategy == StorageStrategy.InMemory)
        {
            await MigrateToHybridAsync(cancellationToken);
        }
        else if (!shouldUseHybrid && _currentStrategy == StorageStrategy.Hybrid)
        {
            await MigrateToInMemoryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Migrate from InMemory to Hybrid (SQLite)
    /// THREAD-SAFE: Uses semaphore lock
    /// </summary>
    private async Task MigrateToHybridAsync(CancellationToken cancellationToken)
    {
        await _migrationLock.WaitAsync(cancellationToken);
        try
        {
            _logger?.LogWarning("MIGRATION START: InMemory → Hybrid (SQLite + WAL)");
            var stopwatch = Stopwatch.StartNew();

            // 1. Create new HybridRowStore
            var hybridStore = CreateHybridStore();
            await hybridStore.InitializeAsync(_options.DatabasePath, cancellationToken);

            // 2. Copy all data from InMemory to Hybrid
            var allRows = await _activeStore.GetAllRowsAsync(cancellationToken);
            await hybridStore.AppendRowsAsync(allRows, cancellationToken);

            // 3. Copy validation cache (if exists)
            if (_activeStore is InMemoryRowStore inMemoryStore)
            {
                var validationErrors = await inMemoryStore.GetValidationErrorsAsync(false, false, cancellationToken);
                if (validationErrors.Count > 0)
                {
                    // Group by rowId for batch write
                    var validationDict = validationErrors
                        .GroupBy(e => e.RowId)
                        .Where(g => !string.IsNullOrEmpty(g.Key))
                        .ToDictionary(g => g.Key!, g => g.ToArray());

                    await hybridStore.WriteValidationResultsBatchAsync(validationDict, cancellationToken);
                }
            }

            // 4. Swap stores (atomic)
            var oldStore = _activeStore;
            _activeStore = hybridStore;
            _currentStrategy = StorageStrategy.Hybrid;

            // 5. Dispose old InMemory store
            if (oldStore is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();

            stopwatch.Stop();
            _logger?.LogWarning(
                "MIGRATION COMPLETE: InMemory → Hybrid in {Duration}ms, {RowCount} rows migrated",
                stopwatch.ElapsedMilliseconds, allRows.Count);
        }
        finally
        {
            _migrationLock.Release();
        }
    }

    /// <summary>
    /// Migrate from Hybrid to InMemory (when row count drops below threshold)
    /// </summary>
    private async Task MigrateToInMemoryAsync(CancellationToken cancellationToken)
    {
        await _migrationLock.WaitAsync(cancellationToken);
        try
        {
            _logger?.LogWarning("MIGRATION START: Hybrid → InMemory");
            var stopwatch = Stopwatch.StartNew();

            // 1. Create new InMemoryRowStore
            var inMemoryStore = CreateInMemoryStore();

            // 2. Copy all data from Hybrid to InMemory
            var allRows = await _activeStore.GetAllRowsAsync(cancellationToken);
            await inMemoryStore.ReplaceAllRowsAsync(allRows, cancellationToken);

            // 3. Copy validation cache
            var validationErrors = await _activeStore.GetValidationErrorsAsync(false, false, cancellationToken);
            if (validationErrors.Count > 0)
            {
                var validationDict = validationErrors
                    .GroupBy(e => e.RowId)
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .ToDictionary(g => g.Key!, g => g.ToArray());

                await inMemoryStore.WriteValidationResultsBatchAsync(validationDict, cancellationToken);
            }

            // 4. Swap stores (atomic)
            var oldStore = _activeStore;
            _activeStore = inMemoryStore;
            _currentStrategy = StorageStrategy.InMemory;

            // 5. Dispose old Hybrid store (closes SQLite, deletes file)
            if (oldStore is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();

            stopwatch.Stop();
            _logger?.LogWarning(
                "MIGRATION COMPLETE: Hybrid → InMemory in {Duration}ms",
                stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            _migrationLock.Release();
        }
    }

    /// <summary>
    /// Factory: Create InMemoryRowStore
    /// </summary>
    private InMemoryRowStore CreateInMemoryStore()
    {
        var logger = _serviceProvider.GetService<ILogger<InMemoryRowStore>>();
        return new InMemoryRowStore(logger);
    }

    /// <summary>
    /// Factory: Create HybridRowStore
    /// </summary>
    private HybridRowStore CreateHybridStore()
    {
        var logger = _serviceProvider.GetService<ILogger<HybridRowStore>>();
        var dbLifecycleManager = _serviceProvider.GetRequiredService<IDatabaseLifecycleManager>();
        return new HybridRowStore(logger, dbLifecycleManager, _options.ViewportCacheSize);
    }

    // ========================================================================================
    // IRowStore DELEGATION - All methods delegate to _activeStore
    // ========================================================================================

    public async IAsyncEnumerable<IReadOnlyList<IReadOnlyDictionary<string, object?>>> StreamRowsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        int batchSize = 1000,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var batch in _activeStore.StreamRowsAsync(onlyFiltered, onlyChecked, batchSize, cancellationToken))
        {
            yield return batch;
        }
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(bool onlyFiltered, CancellationToken cancellationToken = default) =>
        _activeStore.GetAllRowsAsync(onlyFiltered, cancellationToken);

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(CancellationToken cancellationToken = default) =>
        _activeStore.GetAllRowsAsync(cancellationToken);

    public Task<long> GetRowCountAsync(bool onlyFiltered, CancellationToken cancellationToken = default) =>
        _activeStore.GetRowCountAsync(onlyFiltered, cancellationToken);

    public Task<long> GetRowCountAsync(CancellationToken cancellationToken = default) =>
        _activeStore.GetRowCountAsync(cancellationToken);

    public Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default) =>
        _activeStore.GetFilteredRowCountAsync(cancellationToken);

    /// <summary>
    /// ✅ SENIOR FIX: Delegate GetRowsRangeAsync to active store (InMemory or Hybrid)
    /// Part of dual-mode virtual pagination architecture
    /// </summary>
    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsRangeAsync(
        long startIndex,
        int count,
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default) =>
        _activeStore.GetRowsRangeAsync(startIndex, count, onlyFiltered, cancellationToken);

    public async Task PersistRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        await _activeStore.PersistRowsAsync(rows, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
    }

    public Task ReplaceAllRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default) =>
        _activeStore.ReplaceAllRowsAsync(rows, cancellationToken);

    public async Task AppendRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        await _activeStore.AppendRowsAsync(rows, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
    }

    public Task EnsureInitialEmptyRowAsync(IEnumerable<string> columnNames, CancellationToken cancellationToken = default) =>
        _activeStore.EnsureInitialEmptyRowAsync(columnNames, cancellationToken);

    public async Task InsertRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, int startIndex, CancellationToken cancellationToken = default)
    {
        await _activeStore.InsertRowsAsync(rows, startIndex, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
    }

    public async Task InsertRowAfterAsync(int targetRowIndex, IReadOnlyDictionary<string, object?> newRow, CancellationToken cancellationToken = default)
    {
        await _activeStore.InsertRowAfterAsync(targetRowIndex, newRow, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
    }

    public async Task InsertRowBeforeAsync(int targetRowIndex, IReadOnlyDictionary<string, object?> newRow, CancellationToken cancellationToken = default)
    {
        await _activeStore.InsertRowBeforeAsync(targetRowIndex, newRow, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
    }

    public async Task InsertRowAtTopAsync(IReadOnlyDictionary<string, object?> newRow, CancellationToken cancellationToken = default)
    {
        await _activeStore.InsertRowAtTopAsync(newRow, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
    }

    public Task WriteValidationResultsAsync(IEnumerable<ValidationError> results, CancellationToken cancellationToken = default) =>
        _activeStore.WriteValidationResultsAsync(results, cancellationToken);

    public Task<bool> HasValidationStateForScopeAsync(bool onlyFiltered, bool onlyChecked = false, CancellationToken cancellationToken = default) =>
        _activeStore.HasValidationStateForScopeAsync(onlyFiltered, onlyChecked, cancellationToken);

    public Task<bool> AreAllNonEmptyRowsMarkedValidAsync(bool onlyFiltered, bool onlyChecked = false, CancellationToken cancellationToken = default) =>
        _activeStore.AreAllNonEmptyRowsMarkedValidAsync(onlyFiltered, onlyChecked, cancellationToken);

    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(bool onlyFiltered = false, bool onlyChecked = false, CancellationToken cancellationToken = default) =>
        _activeStore.GetValidationErrorsAsync(onlyFiltered, onlyChecked, cancellationToken);

    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(string rowId, CancellationToken cancellationToken = default) =>
        _activeStore.GetValidationErrorsForRowAsync(rowId, cancellationToken);

    public async Task RemoveRowsAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default)
    {
        await _activeStore.RemoveRowsAsync(rowIds, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
    }

    public Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(string rowId, CancellationToken cancellationToken = default) =>
        _activeStore.GetRowByIdAsync(rowId, cancellationToken);

    public Task<bool> UpdateRowByIdAsync(string rowId, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default) =>
        _activeStore.UpdateRowByIdAsync(rowId, rowData, cancellationToken);

    public Task<bool> RemoveRowByIdAsync(string rowId, CancellationToken cancellationToken = default) =>
        _activeStore.RemoveRowByIdAsync(rowId, cancellationToken);

    public Task<IReadOnlyDictionary<string, object?>?> GetRowAsync(int rowIndex, CancellationToken cancellationToken = default) =>
        _activeStore.GetRowAsync(rowIndex, cancellationToken);

    public Task<bool> UpdateRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default) =>
        _activeStore.UpdateRowAsync(rowIndex, rowData, cancellationToken);

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        _activeStore.ClearAsync(cancellationToken);

    public Task ClearValidationStateAsync(CancellationToken cancellationToken = default) =>
        _activeStore.ClearValidationStateAsync(cancellationToken);

    public void SetFilterCriteria(IReadOnlyList<object>? filterCriteria) =>
        _activeStore.SetFilterCriteria(filterCriteria);

    public void ClearFilterCriteria() =>
        _activeStore.ClearFilterCriteria();

    public IReadOnlyList<object> GetFilterCriteria() =>
        _activeStore.GetFilterCriteria();

    public void SetFilterExpression(Features.Filter.Models.FilterExpression? expression) =>
        _activeStore.SetFilterExpression(expression);

    public Features.Filter.Models.FilterExpression? GetFilterExpression() =>
        _activeStore.GetFilterExpression();

    public Task InitializeEmptyRowsAsync(IEnumerable<string> columnNames, int rowCount, CancellationToken cancellationToken = default) =>
        _activeStore.InitializeEmptyRowsAsync(columnNames, rowCount, cancellationToken);

    public int? MapFilteredIndexToOriginalIndex(int filteredIndex) =>
        _activeStore.MapFilteredIndexToOriginalIndex(filteredIndex);

    public Task<IReadOnlyDictionary<string, object?>?> GetLastRowAsync(CancellationToken cancellationToken = default) =>
        _activeStore.GetLastRowAsync(cancellationToken);

    public void SetSortCriteria(string columnName, SortDirection direction) =>
        _activeStore.SetSortCriteria(columnName, direction);

    public void ClearSortCriteria() =>
        _activeStore.ClearSortCriteria();

    public Task<IReadOnlyList<string>> SearchAsync(string searchText, string[]? targetColumns = null, bool caseSensitive = false, CancellationToken cancellationToken = default) =>
        _activeStore.SearchAsync(searchText, targetColumns, caseSensitive, cancellationToken);

    public async Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        var result = await _activeStore.AddRowAsync(rowData, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
        return result;
    }

    public async Task<int> AddRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData, CancellationToken cancellationToken = default)
    {
        var result = await _activeStore.AddRowsAsync(rowsData, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
        return result;
    }

    public async Task InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        await _activeStore.InsertRowAsync(rowIndex, rowData, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
    }

    public async Task RemoveRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        await _activeStore.RemoveRowAsync(rowIndex, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
    }

    public async Task<int> RemoveRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default)
    {
        var result = await _activeStore.RemoveRowsAsync(rowIndices, cancellationToken);
        await CheckAndMigrateIfNeededAsync(cancellationToken);
        return result;
    }

    public Task ClearAllRowsAsync(CancellationToken cancellationToken = default) =>
        _activeStore.ClearAllRowsAsync(cancellationToken);

    public IReadOnlyDictionary<string, object?>? GetRow(int rowIndex) =>
        _activeStore.GetRow(rowIndex);

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetAllRows() =>
        _activeStore.GetAllRows();

    public int GetRowCount() =>
        _activeStore.GetRowCount();

    public bool RowExists(int rowIndex) =>
        _activeStore.RowExists(rowIndex);

    public bool IsRowValidationCached(string rowId) =>
        _activeStore.IsRowValidationCached(rowId);

    public void MarkRowAsValidated(string rowId) =>
        _activeStore.MarkRowAsValidated(rowId);

    public void ClearValidationCache() =>
        _activeStore.ClearValidationCache();

    public Task WriteValidationResultsBatchAsync(Dictionary<string, ValidationError[]> validationResults, CancellationToken cancellationToken = default) =>
        _activeStore.WriteValidationResultsBatchAsync(validationResults, cancellationToken);

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> FilterAsync(Func<IReadOnlyDictionary<string, object?>, bool> predicate, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("FilterAsync with predicate is not supported in AdaptiveRowStore. Use SetFilterCriteria instead.");

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> SortAsync(string columnName, bool ascending = true, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("SortAsync is not supported in AdaptiveRowStore. Use SetSortCriteria instead.");

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> SearchAsync(string searchTerm, IEnumerable<string>? columnsToSearch = null, CancellationToken cancellationToken = default) =>
        _activeStore.SearchAsync(searchTerm, columnsToSearch?.ToArray(), false, cancellationToken)
            .ContinueWith(async t =>
            {
                var rowIds = await t;
                var rows = new List<IReadOnlyDictionary<string, object?>>();
                foreach (var rowId in rowIds)
                {
                    var row = await _activeStore.GetRowByIdAsync(rowId, cancellationToken);
                    if (row != null)
                        rows.Add(row);
                }
                return (IReadOnlyList<IReadOnlyDictionary<string, object?>>)rows;
            }, cancellationToken).Unwrap();

    public Task UpdateCellAsync(int rowIndex, string columnName, object? newValue, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("UpdateCellAsync by index is not supported in AdaptiveRowStore. Use UpdateRowAsync instead.");

    public Task UpdateCellByIdAsync(string rowId, string columnName, object? newValue, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("UpdateCellByIdAsync is not supported in AdaptiveRowStore. Use UpdateRowByIdAsync instead.");

    public Task<IReadOnlyDictionary<string, object?>?> GetRowByIndexAsync(int rowIndex, CancellationToken cancellationToken = default) =>
        _activeStore.GetRowAsync(rowIndex, cancellationToken);

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsByIdsAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("GetRowsByIdsAsync is not supported in AdaptiveRowStore.");

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsInRangeAsync(int startIndex, int count, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("GetRowsInRangeAsync is not supported in AdaptiveRowStore.");

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetFilteredRowsAsync(CancellationToken cancellationToken = default) =>
        _activeStore.GetAllRowsAsync(true, cancellationToken);

    public Task ClearValidationCacheAsync(CancellationToken cancellationToken = default)
    {
        ClearValidationCache();
        return Task.CompletedTask;
    }

    #region BREAKING CHANGE v3.0: rowId-based helper methods

    /// <summary>
    /// Gets the rowId for a given row index.
    /// Delegates to active store (InMemoryRowStore or HybridRowStore).
    /// </summary>
    public string? GetRowIdByIndex(int rowIndex) =>
        _activeStore.GetRowIdByIndex(rowIndex);

    /// <summary>
    /// Gets the current row index for a given rowId.
    /// Delegates to active store (InMemoryRowStore or HybridRowStore).
    /// </summary>
    public int? GetRowIndexById(string rowId) =>
        _activeStore.GetRowIndexById(rowId);

    /// <summary>
    /// Gets a row by its stable rowId (synchronous version).
    /// Delegates to active store (InMemoryRowStore or HybridRowStore).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? GetRowById(string rowId) =>
        _activeStore.GetRowById(rowId);

    /// <summary>
    /// Checks if a row exists by its stable rowId.
    /// Delegates to active store (InMemoryRowStore or HybridRowStore).
    /// </summary>
    public bool RowExistsById(string rowId) =>
        _activeStore.RowExistsById(rowId);

    /// <summary>
    /// PROFESSIONAL QUALITY: Bulk update multiple rows in a single operation.
    /// Delegates to active store (InMemoryRowStore or HybridRowStore).
    /// </summary>
    public Task<int> BulkUpdateRowsAsync(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> updates,
        CancellationToken cancellationToken = default) =>
        _activeStore.BulkUpdateRowsAsync(updates, cancellationToken);

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        await _migrationLock.WaitAsync();
        try
        {
            if (_activeStore is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();

            _migrationLock.Dispose();
            _isDisposed = true;
        }
        finally
        {
            if (_migrationLock.CurrentCount == 0)
                _migrationLock.Release();
        }
    }

    // ✅ RIEŠENIE #2 - FIXED UI POOL stubs (delegate to active store)
    public Task<string> InsertRowAtIndexAsync(int index, IReadOnlyDictionary<string, object?>? rowData, CancellationToken cancellationToken = default)
    {
        return _activeStore.InsertRowAtIndexAsync(index, rowData, cancellationToken);
    }

    public Task DeleteRowByIdAsync(string rowId, CancellationToken cancellationToken = default)
    {
        return _activeStore.DeleteRowByIdAsync(rowId, cancellationToken);
    }
}

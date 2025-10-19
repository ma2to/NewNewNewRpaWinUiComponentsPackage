using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;

/// <summary>
/// Hybrid row store combining in-memory cache with SQLite persistence.
/// Uses Writer Queue pattern for thread-safe writes and viewport cache for reads.
/// Supports 10M+ rows with efficient memory usage.
/// </summary>
internal sealed class HybridRowStore : IRowStore, IAsyncDisposable
{
    #region Private Fields

    private readonly ILogger<HybridRowStore> _logger;
    private readonly IDatabaseLifecycleManager _databaseLifecycleManager;

    // In-memory viewport cache (max 1000-5000 rows)
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>> _viewportCache;
    private readonly int _maxViewportSize;

    // Writer Queue for thread-safe writes
    private readonly Channel<WriteOperation> _writerQueue;
    private readonly Task _writerTask;
    private readonly CancellationTokenSource _disposeCts = new();

    // Filter support
    private IReadOnlyList<object>? _filterCriteria;
    private readonly ConcurrentDictionary<int, string> _filteredIndexMap; // filteredIndex -> rowId

    // Validation state cache
    private readonly ConcurrentDictionary<string, ValidationError[]> _validationCache;

    // Row ordering (ULID-based, lexicographically sortable by timestamp)
    private readonly ConcurrentDictionary<string, long> _rowCreatedAtMap; // rowId -> createdAt (for sorting)

    // Disposed flag
    private bool _isDisposed;

    #endregion

    #region Constructor & Initialization

    public HybridRowStore(
        ILogger<HybridRowStore> logger,
        IDatabaseLifecycleManager databaseLifecycleManager,
        int maxViewportSize = 1000)
    {
        _logger = logger ?? NullLogger<HybridRowStore>.Instance;
        _databaseLifecycleManager = databaseLifecycleManager ?? throw new ArgumentNullException(nameof(databaseLifecycleManager));
        _maxViewportSize = maxViewportSize;

        _viewportCache = new ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>();
        _filteredIndexMap = new ConcurrentDictionary<int, string>();
        _validationCache = new ConcurrentDictionary<string, ValidationError[]>();
        _rowCreatedAtMap = new ConcurrentDictionary<string, long>();

        // Create writer queue with bounded capacity (backpressure)
        _writerQueue = Channel.CreateBounded<WriteOperation>(new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait // Block producers when queue is full
        });

        // Start background writer task
        _writerTask = Task.Run(WriterBackgroundTaskAsync, _disposeCts.Token);

        _logger.LogInformation("HybridRowStore created with max viewport size: {MaxViewportSize}", _maxViewportSize);
    }

    /// <summary>
    /// Initialize database (must be called before any data operations)
    /// </summary>
    public async Task InitializeAsync(string? databasePath = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing HybridRowStore database...");

        var result = await _databaseLifecycleManager.InitializeDatabaseAsync(databasePath, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to initialize database: {Error}", result.ErrorMessage);
            throw new InvalidOperationException($"Database initialization failed: {result.ErrorMessage}");
        }

        _logger.LogInformation("HybridRowStore database initialized successfully");
    }

    #endregion

    #region Writer Background Task

    /// <summary>
    /// Background task that processes write operations from the queue.
    /// Single-threaded writer for SQLite thread-safety.
    /// </summary>
    private async Task WriterBackgroundTaskAsync()
    {
        _logger.LogInformation("Writer background task started");

        try
        {
            await foreach (var operation in _writerQueue.Reader.ReadAllAsync(_disposeCts.Token))
            {
                try
                {
                    await ExecuteWriteOperationAsync(operation, _disposeCts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing write operation {OpType} [{OpId}]: {Message}",
                        operation.OperationType, operation.OperationId, ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Writer background task cancelled (shutdown in progress)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Writer background task failed: {Message}", ex.Message);
        }

        _logger.LogInformation("Writer background task stopped");
    }

    /// <summary>
    /// Execute a single write operation against SQLite database
    /// </summary>
    private async Task ExecuteWriteOperationAsync(WriteOperation operation, CancellationToken cancellationToken)
    {
        if (!_databaseLifecycleManager.IsInitialized)
        {
            _logger.LogWarning("Database not initialized - skipping write operation {OpType}", operation.OperationType);
            return;
        }

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
        {
            _logger.LogWarning("Database connection not available - skipping write operation {OpType}", operation.OperationType);
            return;
        }

        var queueTime = DateTime.UtcNow - operation.QueuedAt;
        _logger.LogDebug("Executing write operation {OpType} [{OpId}] (queue time: {QueueTime}ms)",
            operation.OperationType, operation.OperationId, queueTime.TotalMilliseconds);

        switch (operation)
        {
            case InsertRowWriteOp insertOp:
                await ExecuteInsertRowAsync(connection, insertOp, cancellationToken);
                break;

            case UpdateRowWriteOp updateOp:
                await ExecuteUpdateRowAsync(connection, updateOp, cancellationToken);
                break;

            case DeleteRowWriteOp deleteOp:
                await ExecuteDeleteRowAsync(connection, deleteOp, cancellationToken);
                break;

            case BulkInsertWriteOp bulkInsertOp:
                await ExecuteBulkInsertAsync(connection, bulkInsertOp, cancellationToken);
                break;

            case BulkUpdateWriteOp bulkUpdateOp:
                await ExecuteBulkUpdateAsync(connection, bulkUpdateOp, cancellationToken);
                break;

            case BulkDeleteWriteOp bulkDeleteOp:
                await ExecuteBulkDeleteAsync(connection, bulkDeleteOp, cancellationToken);
                break;

            case UpdateValidationStateWriteOp validationOp:
                await ExecuteUpdateValidationStateAsync(connection, validationOp, cancellationToken);
                break;

            case VacuumWriteOp:
                await ExecuteVacuumAsync(connection, cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown write operation type: {OpType}", operation.OperationType);
                break;
        }
    }

    #endregion

    #region SQLite Write Execution Methods (Placeholders)

    private async Task ExecuteInsertRowAsync(SqliteConnection connection, InsertRowWriteOp op, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO grid_rows (__rowId, __createdAt, __modifiedAt, __isDeleted, __validationState, data)
            VALUES (@rowId, @createdAt, @modifiedAt, 0, @validationState, @data)";

        cmd.Parameters.AddWithValue("@rowId", op.RowId);
        cmd.Parameters.AddWithValue("@createdAt", op.CreatedAt);
        cmd.Parameters.AddWithValue("@modifiedAt", op.ModifiedAt);
        cmd.Parameters.AddWithValue("@validationState", (object?)op.ValidationStateJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@data", op.DataJson);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogTrace("Inserted row {RowId}", op.RowId);
    }

    private async Task ExecuteUpdateRowAsync(SqliteConnection connection, UpdateRowWriteOp op, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE grid_rows
            SET data = @data,
                __modifiedAt = @modifiedAt,
                __validationState = @validationState
            WHERE __rowId = @rowId AND __isDeleted = 0";

        cmd.Parameters.AddWithValue("@rowId", op.RowId);
        cmd.Parameters.AddWithValue("@data", op.DataJson);
        cmd.Parameters.AddWithValue("@modifiedAt", op.ModifiedAt);
        cmd.Parameters.AddWithValue("@validationState", (object?)op.ValidationStateJson ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogTrace("Updated row {RowId}", op.RowId);
    }

    private async Task ExecuteDeleteRowAsync(SqliteConnection connection, DeleteRowWriteOp op, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE grid_rows
            SET __isDeleted = 1,
                __modifiedAt = @modifiedAt
            WHERE __rowId = @rowId";

        cmd.Parameters.AddWithValue("@rowId", op.RowId);
        cmd.Parameters.AddWithValue("@modifiedAt", op.ModifiedAt);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogTrace("Soft-deleted row {RowId}", op.RowId);
    }

    private async Task ExecuteBulkInsertAsync(SqliteConnection connection, BulkInsertWriteOp op, CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO grid_rows (__rowId, __createdAt, __modifiedAt, __isDeleted, __validationState, data)
                VALUES (@rowId, @createdAt, @modifiedAt, 0, @validationState, @data)";

            var pRowId = cmd.Parameters.Add("@rowId", SqliteType.Text);
            var pCreatedAt = cmd.Parameters.Add("@createdAt", SqliteType.Integer);
            var pModifiedAt = cmd.Parameters.Add("@modifiedAt", SqliteType.Integer);
            var pValidationState = cmd.Parameters.Add("@validationState", SqliteType.Text);
            var pData = cmd.Parameters.Add("@data", SqliteType.Text);

            foreach (var row in op.Rows)
            {
                pRowId.Value = row.RowId;
                pCreatedAt.Value = row.CreatedAt;
                pModifiedAt.Value = row.ModifiedAt;
                pValidationState.Value = (object?)row.ValidationStateJson ?? DBNull.Value;
                pData.Value = row.DataJson;

                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            transaction.Commit();
            _logger.LogInformation("Bulk inserted {Count} rows", op.Rows.Count);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private async Task ExecuteBulkUpdateAsync(SqliteConnection connection, BulkUpdateWriteOp op, CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE grid_rows
                SET data = @data,
                    __modifiedAt = @modifiedAt,
                    __validationState = @validationState
                WHERE __rowId = @rowId AND __isDeleted = 0";

            var pRowId = cmd.Parameters.Add("@rowId", SqliteType.Text);
            var pData = cmd.Parameters.Add("@data", SqliteType.Text);
            var pModifiedAt = cmd.Parameters.Add("@modifiedAt", SqliteType.Integer);
            var pValidationState = cmd.Parameters.Add("@validationState", SqliteType.Text);

            foreach (var row in op.Rows)
            {
                pRowId.Value = row.RowId;
                pData.Value = row.DataJson;
                pModifiedAt.Value = row.ModifiedAt;
                pValidationState.Value = (object?)row.ValidationStateJson ?? DBNull.Value;

                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            transaction.Commit();
            _logger.LogInformation("Bulk updated {Count} rows", op.Rows.Count);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private async Task ExecuteBulkDeleteAsync(SqliteConnection connection, BulkDeleteWriteOp op, CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE grid_rows
                SET __isDeleted = 1,
                    __modifiedAt = @modifiedAt
                WHERE __rowId = @rowId";

            var pRowId = cmd.Parameters.Add("@rowId", SqliteType.Text);
            var pModifiedAt = cmd.Parameters.Add("@modifiedAt", SqliteType.Integer);

            foreach (var rowId in op.RowIds)
            {
                pRowId.Value = rowId;
                pModifiedAt.Value = op.ModifiedAt;

                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            transaction.Commit();
            _logger.LogInformation("Bulk deleted {Count} rows", op.RowIds.Count);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private async Task ExecuteUpdateValidationStateAsync(SqliteConnection connection, UpdateValidationStateWriteOp op, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE grid_rows
            SET __validationState = @validationState,
                __modifiedAt = @modifiedAt
            WHERE __rowId = @rowId AND __isDeleted = 0";

        cmd.Parameters.AddWithValue("@rowId", op.RowId);
        cmd.Parameters.AddWithValue("@validationState", op.ValidationStateJson);
        cmd.Parameters.AddWithValue("@modifiedAt", op.ModifiedAt);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogTrace("Updated validation state for row {RowId}", op.RowId);
    }

    private async Task ExecuteVacuumAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "VACUUM";

        _logger.LogInformation("Executing VACUUM...");
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("VACUUM completed");
    }

    #endregion

    #region IRowStore Implementation (Placeholders - to be implemented in next phases)

    // Note: All IRowStore methods will be implemented in subsequent phases
    // For now, throwing NotImplementedException to allow compilation

    public IAsyncEnumerable<IReadOnlyList<IReadOnlyDictionary<string, object?>>> StreamRowsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        bool onlyFiltered,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<long> GetRowCountAsync(bool onlyFiltered, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<long> GetRowCountAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task PersistRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task ReplaceAllRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task AppendRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task EnsureInitialEmptyRowAsync(IEnumerable<string> columnNames, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task InsertRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, int startIndex, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task WriteValidationResultsAsync(IEnumerable<ValidationError> results, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<bool> HasValidationStateForScopeAsync(bool onlyFiltered, bool onlyChecked = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<bool> AreAllNonEmptyRowsMarkedValidAsync(bool onlyFiltered, bool onlyChecked = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(bool onlyFiltered = false, bool onlyChecked = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(string rowId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task RemoveRowsAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(string rowId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<bool> UpdateRowByIdAsync(string rowId, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<bool> RemoveRowByIdAsync(string rowId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<IReadOnlyDictionary<string, object?>?> GetRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<bool> UpdateRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task ClearValidationStateAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public void SetFilterCriteria(IReadOnlyList<object>? filterCriteria)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public void ClearFilterCriteria()
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public IReadOnlyList<object> GetFilterCriteria()
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public int? MapFilteredIndexToOriginalIndex(int filteredIndex)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<IReadOnlyDictionary<string, object?>?> GetLastRowAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<int> AddRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task RemoveRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task<int> RemoveRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public Task ClearAllRowsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public IReadOnlyDictionary<string, object?>? GetRow(int rowIndex)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetAllRows()
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public int GetRowCount()
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    public bool RowExists(int rowIndex)
    {
        throw new NotImplementedException("Will be implemented in Fáza 1.4");
    }

    #endregion

    #region Disposal

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _logger.LogInformation("Disposing HybridRowStore...");

        // Signal shutdown
        _disposeCts.Cancel();

        // Complete writer queue (no more writes accepted)
        _writerQueue.Writer.Complete();

        // Wait for writer task to finish processing queue
        try
        {
            await _writerTask;
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error waiting for writer task to complete: {Message}", ex.Message);
        }

        // Dispose resources
        _disposeCts.Dispose();

        _isDisposed = true;
        _logger.LogInformation("HybridRowStore disposed");
    }

    #endregion
}

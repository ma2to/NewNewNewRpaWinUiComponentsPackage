using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Strategies.Storage;

/// <summary>
/// ✅ SQLITE STORAGE STRATEGY (FINAL VERSION)
/// NOW: InsertRowsAsync uses BULK SHIFT optimization (PART 1 fix).
/// EXTRACTED from HybridRowStore.cs (data storage logic only).
/// ARCHITECTURE: Uses __rowNumber as PRIMARY sort key, ULID as SECONDARY (via __createdAt).
/// WRITER QUEUE: Background task processes write operations for thread-safe SQLite access.
/// </summary>
internal sealed class SqliteStorageStrategy : IStorageStrategy, IAsyncDisposable
{
    // ========== PRIVATE FIELDS (extracted from HybridRowStore.cs) ==========

    private readonly ILogger<SqliteStorageStrategy>? _logger;
    private readonly IDatabaseLifecycleManager _databaseLifecycleManager;

    // Writer Queue for thread-safe writes
    private readonly Channel<WriteOperation> _writerQueue;
    private readonly Task _writerTask;
    private readonly CancellationTokenSource _disposeCts = new();

    // Sort support
    private string? _activeSortSql; // SQL ORDER BY clause for active sort

    // Disposed flag
    private bool _isDisposed;

    // ========== CONSTRUCTOR ==========

    public SqliteStorageStrategy(
        IDatabaseLifecycleManager databaseLifecycleManager,
        ILogger<SqliteStorageStrategy>? logger)
    {
        _databaseLifecycleManager = databaseLifecycleManager ?? throw new ArgumentNullException(nameof(databaseLifecycleManager));
        _logger = logger;

        // Create writer queue with bounded capacity (backpressure)
        _writerQueue = Channel.CreateBounded<WriteOperation>(new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait // Block producers when queue is full
        });

        // Start background writer task
        _writerTask = Task.Run(WriterBackgroundTaskAsync, _disposeCts.Token);

        _logger?.LogInformation("SqliteStorageStrategy created");
    }

    // ========== METADATA ==========

    public string StrategyName => "SQLite";

    // ========== WRITER BACKGROUND TASK ==========

    /// <summary>
    /// Background task that processes write operations from the queue.
    /// Single-threaded writer for SQLite thread-safety.
    /// </summary>
    private async Task WriterBackgroundTaskAsync()
    {
        _logger?.LogInformation("SQLite writer background task started");

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
                    _logger?.LogError(ex, "Error executing write operation {OpType} [{OpId}]: {Message}",
                        operation.OperationType, operation.OperationId, ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("SQLite writer background task cancelled (shutdown in progress)");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SQLite writer background task failed: {Message}", ex.Message);
        }

        _logger?.LogInformation("SQLite writer background task stopped");
    }

    /// <summary>
    /// Execute a single write operation against SQLite database
    /// </summary>
    private async Task ExecuteWriteOperationAsync(WriteOperation operation, CancellationToken cancellationToken)
    {
        if (!_databaseLifecycleManager.IsInitialized)
        {
            _logger?.LogWarning("Database not initialized - skipping write operation {OpType}", operation.OperationType);
            return;
        }

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
        {
            _logger?.LogWarning("Database connection not available - skipping write operation {OpType}", operation.OperationType);
            return;
        }

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

            case FlushWriteOp flushOp:
                // Signal flush completion - all previous ops have been processed
                flushOp.CompletionSource.TrySetResult(true);
                _logger?.LogTrace("Flush operation completed [{OpId}]", flushOp.OperationId);
                break;

            default:
                _logger?.LogWarning("Unknown write operation type: {OpType}", operation.OperationType);
                break;
        }
    }

    // ========== SQLite Write Execution Methods ==========

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
        _logger?.LogTrace("Inserted row {RowId}", op.RowId);
    }

    private async Task ExecuteUpdateRowAsync(SqliteConnection connection, UpdateRowWriteOp op, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE grid_rows
            SET data = @data,
                __modifiedAt = @modifiedAt
            WHERE __rowId = @rowId AND __isDeleted = 0";

        cmd.Parameters.AddWithValue("@rowId", op.RowId);
        cmd.Parameters.AddWithValue("@data", op.DataJson);
        cmd.Parameters.AddWithValue("@modifiedAt", op.ModifiedAt);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger?.LogTrace("Updated row {RowId}", op.RowId);
    }

    private async Task ExecuteDeleteRowAsync(SqliteConnection connection, DeleteRowWriteOp op, CancellationToken cancellationToken)
    {
        // HARD DELETE: Physical deletion from DB
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM grid_rows WHERE __rowId = @rowId";

        cmd.Parameters.AddWithValue("@rowId", op.RowId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger?.LogTrace("Hard-deleted row {RowId} (affected: {Rows})", op.RowId, rowsAffected);
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
            _logger?.LogInformation("Bulk inserted {Count} rows", op.Rows.Count);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Convert row data dictionary to JSON string
    /// </summary>
    private string SerializeRowData(IReadOnlyDictionary<string, object?> rowData)
    {
        // Keep __rowNumber in JSON for SQLite json_extract queries
        return JsonSerializer.Serialize(rowData);
    }

    /// <summary>
    /// Convert JSON string to row data dictionary (with __rowId metadata)
    /// </summary>
    private IReadOnlyDictionary<string, object?> DeserializeRowData(string rowId, string dataJson)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(dataJson) ?? new Dictionary<string, object?>();

        // Add __rowId metadata if not present
        if (!data.ContainsKey("__rowId"))
        {
            data["__rowId"] = rowId;
        }

        return data;
    }

    /// <summary>
    /// Generate new ULID-based row ID
    /// </summary>
    private string GenerateRowId()
    {
        return Ulid.NewUlid().ToString();
    }

    /// <summary>
    /// Get current Unix timestamp in milliseconds
    /// </summary>
    private long GetUnixTimestampMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Queue write operation (async - waits if queue is full)
    /// </summary>
    private async Task QueueWriteOperationAsync(WriteOperation operation, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteStorageStrategy));

        await _writerQueue.Writer.WriteAsync(operation, cancellationToken);
    }

    /// <summary>
    /// Flush writer queue - wait for all pending write operations to complete.
    /// Uses sentinel operation with TaskCompletionSource to ensure all previous ops are processed.
    /// </summary>
    private async Task FlushWriterQueueAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            return;

        var tcs = new TaskCompletionSource<bool>();

        // Queue a sentinel flush operation
        var flushOp = new FlushWriteOp
        {
            CompletionSource = tcs,
            OperationId = GenerateRowId()
        };

        await _writerQueue.Writer.WriteAsync(flushOp, cancellationToken);

        // Wait for flush operation to be processed (all previous ops have completed)
        await tcs.Task;

        _logger?.LogTrace("FlushWriterQueueAsync: Writer queue flushed successfully");
    }

    /// <summary>
    /// Gets max __rowNumber from database.
    /// </summary>
    private async Task<int> GetMaxRowNumberAsync(CancellationToken cancellationToken)
    {
        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return 0;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(CAST(json_extract(data, '$.__rowNumber') AS INTEGER)), 0) FROM grid_rows WHERE __isDeleted = 0";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
    }

    // ========== INSERT OPERATIONS (PART 1 BULK SHIFT - EXTRACTED FROM HYBRIDROWSTORE) ==========

    /// <summary>
    /// ✅ UNIFIED (PART 1): Insert single row at index with __rowNumber shift.
    /// EXTRACTED FROM HybridRowStore.cs:2326-2369
    /// </summary>
    public async Task<string> InsertRowAtIndexAsync(
        int index,
        IReadOnlyDictionary<string, object?>? rowData,
        CancellationToken ct)
    {
        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            throw new InvalidOperationException("Database not initialized");

        int targetRowNumber = index + 1; // Convert 0-based to 1-based

        // ✅ STEP 1: Shift rows UP (SQL UPDATE)
        using (var cmdShift = connection.CreateCommand())
        {
            cmdShift.CommandText = $@"
                UPDATE grid_rows
                SET data = json_set(data, '$.__rowNumber', CAST(json_extract(data, '$.__rowNumber') AS INTEGER) + 1)
                WHERE __isDeleted = 0
                  AND CAST(json_extract(data, '$.__rowNumber') AS INTEGER) >= {targetRowNumber}";

            var shiftedCount = await cmdShift.ExecuteNonQueryAsync(ct);
            _logger?.LogDebug("InsertRowAtIndex {Index}: Shifted {Count} rows UP (__rowNumber >= {StartNum})",
                index, shiftedCount, targetRowNumber);
        }

        // ✅ STEP 2: Insert new row with __rowNumber
        var newRowId = GenerateRowId();
        var dataDict = new Dictionary<string, object?>(rowData ?? new Dictionary<string, object?>());
        dataDict["__rowId"] = newRowId;
        dataDict["__rowNumber"] = targetRowNumber;

        var insertOp = new InsertRowWriteOp
        {
            RowId = newRowId,
            DataJson = SerializeRowData(dataDict),
            CreatedAt = GetUnixTimestampMs(),
            ModifiedAt = GetUnixTimestampMs(),
            ValidationStateJson = null,
            OperationId = GenerateRowId()
        };

        await QueueWriteOperationAsync(insertOp, ct);
        await FlushWriterQueueAsync(ct);

        _logger?.LogInformation("InsertRowAtIndex: Added row at index {Index} (RowId={RowId}, __rowNumber={RowNumber})",
            index, newRowId, targetRowNumber);

        return newRowId;
    }

    /// <summary>
    /// ✅ UNIFIED + OPTIMIZED (PART 1 FIX): Insert multiple rows at index with BULK SHIFT.
    /// EXTRACTED FROM HybridRowStore.cs:1231-1311 (AFTER PART 1 implementation)
    /// </summary>
    public async Task InsertRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex,
        CancellationToken ct)
    {
        var rowsList = rows.ToList();
        if (rowsList.Count == 0)
        {
            _logger?.LogDebug("InsertRowsAsync: No rows to insert");
            return;
        }

        _logger?.LogInformation(
            "InsertRowsAsync (bulk): Inserting {Count} rows at index {StartIndex} (SqliteStorageStrategy)",
            rowsList.Count, startIndex);

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            throw new InvalidOperationException("Database not initialized");

        // ✅ STEP 1: BULK SHIFT existing rows UP by rowsList.Count (1 SQL UPDATE)
        int targetRowNumber = startIndex + 1; // 0-based index → 1-based __rowNumber

        using (var cmdShift = connection.CreateCommand())
        {
            cmdShift.CommandText = $@"
                UPDATE grid_rows
                SET data = json_set(data, '$.__rowNumber',
                    CAST(json_extract(data, '$.__rowNumber') AS INTEGER) + {rowsList.Count})
                WHERE __isDeleted = 0
                  AND CAST(json_extract(data, '$.__rowNumber') AS INTEGER) >= {targetRowNumber}";

            var shiftedCount = await cmdShift.ExecuteNonQueryAsync(ct);
            _logger?.LogDebug(
                "InsertRowsAsync: Bulk shifted {Count} rows UP (SQL UPDATE: __rowNumber >= {TargetNum})",
                shiftedCount, targetRowNumber);
        }

        // ✅ STEP 2: BULK INSERT new rows with sequential __rowNumber
        var timestamp = GetUnixTimestampMs();
        var insertData = new List<RowInsertData>();

        for (int i = 0; i < rowsList.Count; i++)
        {
            var rowData = new Dictionary<string, object?>(rowsList[i]);
            rowData["__rowNumber"] = startIndex + i + 1; // Sequential: startIndex+1, startIndex+2, ...

            // Generate new ULID if not present
            if (!rowData.ContainsKey("__rowId") || rowData["__rowId"] == null)
            {
                rowData["__rowId"] = GenerateRowId();
            }

            insertData.Add(new RowInsertData
            {
                RowId = (string)rowData["__rowId"]!,
                DataJson = SerializeRowData(rowData),
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                ValidationStateJson = null
            });
        }

        _logger?.LogDebug(
            "InsertRowsAsync: Prepared {Count} rows for bulk insert (__rowNumber {StartNum}-{EndNum})",
            rowsList.Count, startIndex + 1, startIndex + rowsList.Count);

        // Queue bulk insert operation (processed by writer background task)
        var bulkInsertOp = new BulkInsertWriteOp
        {
            Rows = insertData,
            OperationId = GenerateRowId()
        };

        await QueueWriteOperationAsync(bulkInsertOp, ct);

        _logger?.LogInformation(
            "InsertRowsAsync (bulk): Successfully queued {Count} rows for insertion at index {StartIndex}",
            rowsList.Count, startIndex);
    }

    // ========== DELETE OPERATION (shift __rowNumber DOWN) ==========

    /// <summary>
    /// ✅ PROFESSIONAL: Deletes row by RowID and shifts subsequent rows DOWN in SQLite.
    /// EXTRACTED FROM HybridRowStore.cs:2384-2433
    /// </summary>
    public async Task DeleteRowByIdAsync(string rowId, CancellationToken ct)
    {
        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return;

        int deletedRowNumber;

        // ✅ STEP 1: Get __rowNumber of deleted row
        using (var cmdGet = connection.CreateCommand())
        {
            cmdGet.CommandText = $@"
                SELECT CAST(json_extract(data, '$.__rowNumber') AS INTEGER) as rn
                FROM grid_rows
                WHERE __rowId = @rowId AND __isDeleted = 0";
            cmdGet.Parameters.AddWithValue("@rowId", rowId);

            var result = await cmdGet.ExecuteScalarAsync(ct);

            if (result == null || result == DBNull.Value)
            {
                _logger?.LogWarning("DeleteRowById: Row {RowId} not found", rowId);
                return;
            }

            deletedRowNumber = Convert.ToInt32(result);
        }

        // ✅ STEP 2: Hard delete row (physical deletion)
        using (var cmdDelete = connection.CreateCommand())
        {
            cmdDelete.CommandText = "DELETE FROM grid_rows WHERE __rowId = @rowId";
            cmdDelete.Parameters.AddWithValue("@rowId", rowId);
            await cmdDelete.ExecuteNonQueryAsync(ct);
        }

        // ✅ STEP 3: Shift rows DOWN
        using (var cmdShift = connection.CreateCommand())
        {
            cmdShift.CommandText = $@"
                UPDATE grid_rows
                SET data = json_set(data, '$.__rowNumber', CAST(json_extract(data, '$.__rowNumber') AS INTEGER) - 1)
                WHERE __isDeleted = 0
                  AND CAST(json_extract(data, '$.__rowNumber') AS INTEGER) > {deletedRowNumber}";

            var shiftedCount = await cmdShift.ExecuteNonQueryAsync(ct);
            _logger?.LogInformation("DeleteRowById: Deleted row {RowId} (__rowNumber={RowNumber}), shifted {ShiftCount} rows DOWN",
                rowId, deletedRowNumber, shiftedCount);
        }
    }

    // ========== APPEND ROWS (sequential __rowNumber assignment) ==========

    /// <summary>
    /// ✅ PROFESSIONAL: Appends rows with sequential __rowNumber.
    /// EXTRACTED FROM HybridRowStore.cs:1102-1155
    /// </summary>
    public async Task<int> AddRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct)
    {
        var rowsList = rows.ToList();
        if (rowsList.Count == 0)
        {
            _logger?.LogDebug("AddRowsAsync: No rows to append");
            return 0;
        }

        _logger?.LogInformation("AddRowsAsync: Appending {Count} rows", rowsList.Count);

        var timestamp = GetUnixTimestampMs();
        var maxRowNumber = await GetMaxRowNumberAsync(ct);

        // Create bulk insert operation with __rowNumber
        var insertData = rowsList.Select(row =>
        {
            maxRowNumber++; // Increment for each new row

            var rowId = row.ContainsKey("__rowId") && row["__rowId"] is string existingId
                ? existingId
                : GenerateRowId();

            // Add __rowNumber to row data
            var dataDict = new Dictionary<string, object?>(row);
            dataDict["__rowNumber"] = maxRowNumber;
            dataDict["__rowId"] = rowId;

            var dataJson = SerializeRowData(dataDict);

            return new RowInsertData
            {
                RowId = rowId,
                DataJson = dataJson,
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                ValidationStateJson = null
            };
        }).ToList();

        var bulkInsertOp = new BulkInsertWriteOp
        {
            Rows = insertData,
            OperationId = GenerateRowId()
        };

        await QueueWriteOperationAsync(bulkInsertOp, ct);

        _logger?.LogInformation("AddRowsAsync: Queued {Count} rows for insertion with __rowNumber starting from {StartNum}",
            rowsList.Count, maxRowNumber - rowsList.Count + 1);

        return rowsList.Count;
    }

    // ========== CRUD METHODS ==========

    public async Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(
        string rowId,
        CancellationToken ct)
    {
        if (!_databaseLifecycleManager.IsInitialized)
            return null;

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return null;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT data FROM grid_rows WHERE __rowId = @rowId AND __isDeleted = 0";
        cmd.Parameters.AddWithValue("@rowId", rowId);

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result == null || result == DBNull.Value)
            return null;

        var dataJson = result.ToString();
        var rowData = DeserializeRowData(rowId, dataJson!);

        _logger?.LogTrace("GetRowByIdAsync: Retrieved {RowId} from SQLite", rowId);
        return rowData;
    }

    public async Task UpdateRowByIdAsync(
        string rowId,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken ct)
    {
        _logger?.LogDebug("UpdateRowByIdAsync: Updating {RowId}", rowId);

        // Preserve __rowNumber if not in new data
        var existingRow = await GetRowByIdAsync(rowId, ct);
        var dataDict = new Dictionary<string, object?>(rowData);

        if (existingRow != null && existingRow.TryGetValue("__rowNumber", out var rn))
        {
            if (!dataDict.ContainsKey("__rowNumber"))
            {
                dataDict["__rowNumber"] = rn;
            }
        }

        dataDict["__rowId"] = rowId;

        var dataJson = SerializeRowData(dataDict);

        var updateOp = new UpdateRowWriteOp
        {
            RowId = rowId,
            DataJson = dataJson,
            ModifiedAt = GetUnixTimestampMs(),
            ValidationStateJson = null,
            OperationId = GenerateRowId()
        };

        await QueueWriteOperationAsync(updateOp, ct);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsRangeAsync(
        long startIndex,
        int count,
        bool onlyFiltered,
        CancellationToken ct)
    {
        if (!_databaseLifecycleManager.IsInitialized)
        {
            _logger?.LogWarning("GetRowsRangeAsync called but database not initialized");
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
        {
            _logger?.LogWarning("GetRowsRangeAsync called but database connection is null");
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        try
        {
            using var cmd = connection.CreateCommand();

            // Build WHERE clause
            var whereClause = "__isDeleted = 0";
            // TODO: Add filter support when needed

            // Build ORDER BY clause (primary: __rowNumber, fallback: __createdAt)
            var orderByClause = !string.IsNullOrEmpty(_activeSortSql)
                ? _activeSortSql
                : "CAST(json_extract(data, '$.__rowNumber') AS INTEGER) ASC";

            // SQL with LIMIT/OFFSET for efficient pagination
            cmd.CommandText = $@"
                SELECT __rowId, data
                FROM grid_rows
                WHERE {whereClause}
                ORDER BY {orderByClause}
                LIMIT {count} OFFSET {startIndex}";

            var results = new List<IReadOnlyDictionary<string, object?>>();
            using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var rowId = reader.GetString(0);
                var dataJson = reader.GetString(1);
                var rowData = DeserializeRowData(rowId, dataJson);
                results.Add(rowData);
            }

            _logger?.LogDebug("GetRowsRangeAsync: startIndex={Start}, count={Count}, returned={Returned} rows (SQL LIMIT/OFFSET)",
                startIndex, count, results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetRowsRangeAsync failed: startIndex={Start}, count={Count}",
                startIndex, count);
            throw;
        }
    }

    public async Task<long> GetRowCountAsync(bool onlyFiltered, CancellationToken ct)
    {
        if (!_databaseLifecycleManager.IsInitialized)
            return 0;

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return 0;

        using var cmd = connection.CreateCommand();

        // Build WHERE clause
        var whereClause = "__isDeleted = 0";
        // TODO: Add filter support when needed

        cmd.CommandText = $"SELECT COUNT(*) FROM grid_rows WHERE {whereClause}";

        var result = await cmd.ExecuteScalarAsync(ct);
        var count = result != null ? Convert.ToInt64(result) : 0;

        _logger?.LogDebug("GetRowCountAsync: {Count} rows (onlyFiltered={OnlyFiltered})", count, onlyFiltered);
        return count;
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        bool onlyFiltered,
        CancellationToken ct)
    {
        if (!_databaseLifecycleManager.IsInitialized)
            return Array.Empty<IReadOnlyDictionary<string, object?>>();

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return Array.Empty<IReadOnlyDictionary<string, object?>>();

        using var cmd = connection.CreateCommand();

        // Build WHERE clause
        var whereClause = "__isDeleted = 0";
        // TODO: Add filter support when needed

        // Build ORDER BY clause
        var orderByClause = !string.IsNullOrEmpty(_activeSortSql)
            ? _activeSortSql
            : "CAST(json_extract(data, '$.__rowNumber') AS INTEGER) ASC";

        cmd.CommandText = $"SELECT __rowId, data FROM grid_rows WHERE {whereClause} ORDER BY {orderByClause}";

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var rowId = reader.GetString(0);
            var dataJson = reader.GetString(1);
            var rowData = DeserializeRowData(rowId, dataJson);
            rows.Add(rowData);
        }

        _logger?.LogDebug("GetAllRowsAsync returned {Count} rows (onlyFiltered={OnlyFiltered})", rows.Count, onlyFiltered);
        return rows;
    }

    public async Task ReplaceAllRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct)
    {
        _logger?.LogInformation("ReplaceAllRowsAsync: Clearing all data and inserting new rows");

        // Clear all existing data
        await ClearAsync(ct);

        // Append new rows with sequential __rowNumber
        var rowsList = rows.ToList();
        if (rowsList.Count == 0)
            return;

        var timestamp = GetUnixTimestampMs();
        int rowNumber = 0;

        var insertData = rowsList.Select(row =>
        {
            rowNumber++;

            var rowData = new Dictionary<string, object?>(row);
            if (!rowData.ContainsKey("__rowNumber"))
            {
                rowData["__rowNumber"] = rowNumber;
            }

            if (!rowData.ContainsKey("__rowId") || rowData["__rowId"] == null)
            {
                rowData["__rowId"] = GenerateRowId();
            }

            return new RowInsertData
            {
                RowId = (string)rowData["__rowId"]!,
                DataJson = SerializeRowData(rowData),
                CreatedAt = timestamp,
                ModifiedAt = timestamp,
                ValidationStateJson = null
            };
        }).ToList();

        var bulkInsertOp = new BulkInsertWriteOp
        {
            Rows = insertData,
            OperationId = GenerateRowId()
        };

        await QueueWriteOperationAsync(bulkInsertOp, ct);
        await FlushWriterQueueAsync(ct);

        _logger?.LogInformation("ReplaceAllRowsAsync: Replaced with {Count} rows (writer queue flushed)", rowNumber);
    }

    // ========== SORT CRITERIA (renumber __rowNumber) ==========

    /// <summary>
    /// ✅ PROFESSIONAL: Sorts rows by specified column and RENUMBERS __rowNumber.
    /// EXTRACTED FROM HybridRowStore.cs:673-731
    /// </summary>
    public void SetSortCriteria(string columnName, SortDirection direction)
    {
        _logger?.LogInformation("SetSortCriteria: column={ColumnName}, direction={Direction}", columnName, direction);

        if (direction == SortDirection.None)
        {
            _activeSortSql = null;
            _logger?.LogInformation("Sort cleared - reverted to current __rowNumber order");
            return;
        }

        // Build SQL ORDER BY clause for single column
        var columnPath = $"$.{columnName}";
        var directionStr = direction == SortDirection.Ascending ? "ASC" : "DESC";

        // Type-aware sorting (try numeric first, fallback to text)
        var orderBy = $@"
            CASE
                WHEN json_type(json_extract(data, '{columnPath}')) IN ('integer', 'real')
                THEN CAST(json_extract(data, '{columnPath}') AS REAL)
                ELSE NULL
            END {directionStr},
            json_extract(data, '{columnPath}') {directionStr}";

        // Execute SQL UPDATE to renumber __rowNumber using ROW_NUMBER()
        var connection = _databaseLifecycleManager.GetConnection();
        if (connection != null)
        {
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    WITH sorted_rows AS (
                        SELECT __rowId,
                               ROW_NUMBER() OVER (ORDER BY {orderBy}) as new_rn
                        FROM grid_rows
                        WHERE __isDeleted = 0
                    )
                    UPDATE grid_rows
                    SET data = json_set(data, '$.__rowNumber', (
                        SELECT new_rn
                        FROM sorted_rows
                        WHERE sorted_rows.__rowId = grid_rows.__rowId
                    ))
                    WHERE __isDeleted = 0";

                var updatedCount = cmd.ExecuteNonQuery();
                _logger?.LogInformation("Sort completed: {Count} rows renumbered by column={Column}, direction={Direction}",
                    updatedCount, columnName, direction);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SetSortCriteria failed: column={Column}, direction={Direction}", columnName, direction);
            }
        }

        // Set active sort SQL for future queries
        _activeSortSql = $"CAST(json_extract(data, '$.__rowNumber') AS INTEGER) ASC";
    }

    public void ClearSortCriteria()
    {
        _logger?.LogInformation("ClearSortCriteria: Clearing sort criteria");
        _activeSortSql = null;
    }

    // ========== SEARCH ==========

    /// <summary>
    /// Performs FTS5 full-text search on row data.
    /// EXTRACTED FROM HybridRowStore.cs:751-842
    /// </summary>
    public async Task<IReadOnlyList<string>> SearchAsync(
        string searchText,
        string[]? targetColumns,
        bool caseSensitive,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _logger?.LogWarning("SearchAsync called with empty search text");
            return Array.Empty<string>();
        }

        if (!_databaseLifecycleManager.IsInitialized)
        {
            _logger?.LogWarning("SearchAsync called but database not initialized");
            return Array.Empty<string>();
        }

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
        {
            _logger?.LogWarning("SearchAsync called but database connection is null");
            return Array.Empty<string>();
        }

        _logger?.LogInformation("SearchAsync: searchText='{SearchText}', targetColumns={ColumnCount}, caseSensitive={CaseSensitive}",
            searchText, targetColumns?.Length ?? 0, caseSensitive);

        try
        {
            using var cmd = connection.CreateCommand();

            // Build FTS5 MATCH query
            string ftsQuery;

            if (targetColumns != null && targetColumns.Length > 0)
            {
                // Column-specific search
                var columnQueries = targetColumns
                    .Select(col => $"{{data}}:\"{EscapeFtsQuery(searchText)}\"")
                    .ToArray();
                ftsQuery = string.Join(" OR ", columnQueries);
            }
            else
            {
                // Search all columns
                ftsQuery = EscapeFtsQuery(searchText);
            }

            if (caseSensitive)
            {
                // Case-sensitive search: Use FTS to narrow results, then filter by case
                cmd.CommandText = $@"
                    SELECT gr.__rowId
                    FROM grid_rows gr
                    INNER JOIN grid_rows_fts fts ON gr.rowid = fts.rowid
                    WHERE fts.data MATCH '{ftsQuery}'
                      AND gr.__isDeleted = 0
                      AND instr(gr.data, '{searchText.Replace("'", "''")}') > 0";
            }
            else
            {
                // Case-insensitive search: FTS5 handles this natively
                cmd.CommandText = $@"
                    SELECT gr.__rowId
                    FROM grid_rows gr
                    INNER JOIN grid_rows_fts fts ON gr.rowid = fts.rowid
                    WHERE fts.data MATCH '{ftsQuery}'
                      AND gr.__isDeleted = 0";
            }

            var matchedRowIds = new List<string>();

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                matchedRowIds.Add(reader.GetString(0));
            }

            _logger?.LogInformation("SearchAsync completed: found {MatchCount} matches for '{SearchText}'",
                matchedRowIds.Count, searchText);

            return matchedRowIds;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SearchAsync failed for search text '{SearchText}': {Message}", searchText, ex.Message);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Escapes FTS5 query syntax special characters to prevent query errors.
    /// </summary>
    private string EscapeFtsQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
            return string.Empty;

        // Escape double quotes by doubling them
        var escaped = query.Replace("\"", "\"\"");

        // Wrap in quotes to treat as a phrase and prevent FTS5 syntax issues
        return $"\"{escaped}\"";
    }

    // ========== UTILITY ==========

    public async Task ClearAsync(CancellationToken ct)
    {
        _logger?.LogInformation("ClearAsync: Clearing all data");

        if (!_databaseLifecycleManager.IsInitialized)
        {
            _logger?.LogWarning("ClearAsync: Database not initialized");
            return;
        }

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return;

        // Delete all rows from SQLite
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM grid_rows";
        await cmd.ExecuteNonQueryAsync(ct);

        _logger?.LogInformation("ClearAsync: All data cleared");
    }

    public string? GetRowIdByIndex(int index)
    {
        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return null;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            SELECT __rowId
            FROM grid_rows
            WHERE __isDeleted = 0
            ORDER BY CAST(json_extract(data, '$.__rowNumber') AS INTEGER) ASC
            LIMIT 1 OFFSET {index}";

        var result = cmd.ExecuteScalar();
        return result?.ToString();
    }

    public int? GetRowIndexById(string rowId)
    {
        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return null;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
            WITH numbered_rows AS (
                SELECT __rowId,
                       ROW_NUMBER() OVER (ORDER BY CAST(json_extract(data, '$.__rowNumber') AS INTEGER) ASC) - 1 as row_index
                FROM grid_rows
                WHERE __isDeleted = 0
            )
            SELECT row_index
            FROM numbered_rows
            WHERE __rowId = @rowId";

        cmd.Parameters.AddWithValue("@rowId", rowId);

        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
            return null;

        return Convert.ToInt32(result);
    }

    // ========== DISPOSAL ==========

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _logger?.LogInformation("Disposing SqliteStorageStrategy...");

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
            _logger?.LogError(ex, "Error waiting for writer task to complete: {Message}", ex.Message);
        }

        // Dispose resources
        _disposeCts.Dispose();

        _isDisposed = true;
        _logger?.LogInformation("SqliteStorageStrategy disposed");
    }
}

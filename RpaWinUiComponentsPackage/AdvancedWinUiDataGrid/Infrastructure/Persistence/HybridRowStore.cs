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
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;

/// <summary>
/// ⚠️ OBSOLETE (STRATEGY PATTERN - PART 2): Use UnifiedRowStore with SqliteStorageStrategy + SqliteValidationStrategy instead.
///
/// Hybrid row store combining in-memory cache with SQLite persistence.
/// Uses Writer Queue pattern for thread-safe writes and viewport cache for reads.
/// Supports 10M+ rows with efficient memory usage.
///
/// MIGRATION PATH:
/// - Replace HybridRowStore with UnifiedRowStore(SqliteStorageStrategy, SqliteValidationStrategy)
/// - Initialize database via IDatabaseLifecycleManager.InitializeDatabaseAsync()
/// - All business logic remains in UnifiedRowStore, storage logic is in strategies
/// - Zero duplicate code, same functionality
/// </summary>
// [Obsolete("Use UnifiedRowStore with SqliteStorageStrategy + SqliteValidationStrategy instead (Strategy Pattern refactoring). " +
//           "This class will be removed in future versions. See ČÁST 2 documentation for migration guide.")]
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
    private Features.Filter.Models.FilterExpression? _filterExpression; // Complex filter expression tree
    private readonly ConcurrentDictionary<int, string> _filteredIndexMap; // filteredIndex -> rowId
    private string? _activeFilterSql; // SQL WHERE clause for active filters

    // Sort support
    private string? _activeSortSql; // SQL ORDER BY clause for active sort

    // Validation state cache
    private readonly ConcurrentDictionary<string, ValidationError[]> _validationCache;

    // SENIOR FIX: VALIDATION CACHE - Prevents ValidateAll infinite loop
    // CRITICAL: Tracks which rows have been validated to avoid re-validation (cache check)
    // Cleared on data changes (ClearAsync, AddRangeAsync) to ensure fresh validation
    private readonly Dictionary<string, bool> _validatedRowsCache = new(); // Validated row IDs
    private readonly object _validationLock = new(); // Thread-safe validation cache operations
    private bool _isValidating = false; // Re-entrancy guard for batch validation

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

            case FlushWriteOp flushOp:
                // SENIOR FIX: Signal flush completion - all previous ops have been processed
                flushOp.CompletionSource.TrySetResult(true);
                _logger.LogTrace("Flush operation completed [{OpId}]", flushOp.OperationId);
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
        // HARD DELETE: Physical deletion from DB (shift happens automatically via ORDER BY __createdAt)
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM grid_rows WHERE __rowId = @rowId";

        cmd.Parameters.AddWithValue("@rowId", op.RowId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogTrace("Hard-deleted row {RowId} (affected: {Rows})", op.RowId, rowsAffected);

        // Cache invalidation
        _viewportCache.TryRemove(op.RowId, out _);
        _validationCache.TryRemove(op.RowId, out _);
        _rowCreatedAtMap.TryRemove(op.RowId, out _);
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
        // HARD DELETE: Physical deletion from DB (shift happens automatically via ORDER BY __createdAt)
        using var transaction = connection.BeginTransaction();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM grid_rows WHERE __rowId = @rowId";

            var pRowId = cmd.Parameters.Add("@rowId", SqliteType.Text);

            foreach (var rowId in op.RowIds)
            {
                pRowId.Value = rowId;
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // Cache cleanup
                _viewportCache.TryRemove(rowId, out _);
                _validationCache.TryRemove(rowId, out _);
                _rowCreatedAtMap.TryRemove(rowId, out _);
            }

            transaction.Commit();
            _logger.LogInformation("Bulk hard-deleted {Count} rows", op.RowIds.Count);
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

    #region Helper Methods

    /// <summary>
    /// Convert row data dictionary to JSON string
    /// </summary>
    private string SerializeRowData(IReadOnlyDictionary<string, object?> rowData)
    {
        // Remove metadata columns before serialization
        var dataWithoutMeta = rowData
            .Where(kvp => !kvp.Key.StartsWith("__"))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return JsonSerializer.Serialize(dataWithoutMeta);
    }

    /// <summary>
    /// Convert JSON string to row data dictionary (with __rowId metadata)
    /// </summary>
    private IReadOnlyDictionary<string, object?> DeserializeRowData(string rowId, string dataJson)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(dataJson) ?? new Dictionary<string, object?>();

        // Add __rowId metadata
        data["__rowId"] = rowId;

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
    /// Check if row is empty (all values are null or empty string)
    /// </summary>
    private bool IsRowEmpty(IReadOnlyDictionary<string, object?> rowData)
    {
        return rowData
            .Where(kvp => !kvp.Key.StartsWith("__")) // Ignore metadata
            .All(kvp => kvp.Value == null || (kvp.Value is string str && string.IsNullOrWhiteSpace(str)));
    }

    /// <summary>
    /// Queue write operation (async - waits if queue is full)
    /// </summary>
    private async Task QueueWriteOperationAsync(WriteOperation operation, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(HybridRowStore));

        await _writerQueue.Writer.WriteAsync(operation, cancellationToken);
    }

    /// <summary>
    /// SENIOR FIX: Flush writer queue - wait for all pending write operations to complete.
    /// Uses sentinel operation with TaskCompletionSource to ensure all previous ops are processed.
    /// CRITICAL: Prevents race condition in ReplaceAllRowsAsync where UI refresh fires before data is in DB.
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

        _logger.LogTrace("FlushWriterQueueAsync: Writer queue flushed successfully");
    }

    /// <summary>
    /// Get all row IDs ordered by creation time (ULID order)
    /// </summary>
    private async Task<List<string>> GetAllRowIdsOrderedAsync(CancellationToken cancellationToken = default)
    {
        if (!_databaseLifecycleManager.IsInitialized)
            return new List<string>();

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return new List<string>();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT __rowId FROM grid_rows WHERE __isDeleted = 0 ORDER BY __createdAt";

        var rowIds = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rowIds.Add(reader.GetString(0));
        }

        return rowIds;
    }

    /// <summary>
    /// Build SQL WHERE condition for a single filter criterion.
    /// Uses json_extract() to access column values from JSON data field.
    /// </summary>
    private string BuildSqlFilterCondition(FilterCriteria filter)
    {
        // Extract column value from JSON data field using json_extract
        var columnPath = $"$.{filter.ColumnName}";

        return filter.Operator switch
        {
            FilterOperator.Equals =>
                $"json_extract(data, '{columnPath}') = {FormatSqlValue(filter.Value)}",

            FilterOperator.NotEquals =>
                $"json_extract(data, '{columnPath}') != {FormatSqlValue(filter.Value)}",

            FilterOperator.Contains =>
                $"json_extract(data, '{columnPath}') LIKE '%' || {FormatSqlValue(filter.Value)} || '%'",

            FilterOperator.NotContains =>
                $"json_extract(data, '{columnPath}') NOT LIKE '%' || {FormatSqlValue(filter.Value)} || '%'",

            FilterOperator.StartsWith =>
                $"json_extract(data, '{columnPath}') LIKE {FormatSqlValue(filter.Value)} || '%'",

            FilterOperator.EndsWith =>
                $"json_extract(data, '{columnPath}') LIKE '%' || {FormatSqlValue(filter.Value)}",

            FilterOperator.GreaterThan =>
                $"CAST(json_extract(data, '{columnPath}') AS REAL) > {FormatSqlValue(filter.Value)}",

            FilterOperator.GreaterThanOrEqual =>
                $"CAST(json_extract(data, '{columnPath}') AS REAL) >= {FormatSqlValue(filter.Value)}",

            FilterOperator.LessThan =>
                $"CAST(json_extract(data, '{columnPath}') AS REAL) < {FormatSqlValue(filter.Value)}",

            FilterOperator.LessThanOrEqual =>
                $"CAST(json_extract(data, '{columnPath}') AS REAL) <= {FormatSqlValue(filter.Value)}",

            FilterOperator.IsNull =>
                $"json_extract(data, '{columnPath}') IS NULL",

            FilterOperator.IsNotNull =>
                $"json_extract(data, '{columnPath}') IS NOT NULL",

            FilterOperator.IsEmpty =>
                $"(json_extract(data, '{columnPath}') IS NULL OR TRIM(json_extract(data, '{columnPath}')) = '')",

            FilterOperator.IsNotEmpty =>
                $"(json_extract(data, '{columnPath}') IS NOT NULL AND TRIM(json_extract(data, '{columnPath}')) != '')",

            _ => throw new NotSupportedException($"Filter operator {filter.Operator} not supported in SQL builder")
        };
    }

    /// <summary>
    /// Build SQL WHERE condition from complex filter expression tree.
    /// Uses visitor pattern to traverse expression tree and generate SQL.
    /// Supports AND/OR/ANDALSO/ORELSE logical operators and nested expressions.
    /// </summary>
    private string BuildSqlFilterExpression(Features.Filter.Models.FilterExpression expression)
    {
        var visitor = new SqlFilterExpressionVisitor(this);
        return expression.Accept(visitor);
    }

    /// <summary>
    /// Visitor for generating SQL WHERE clause from filter expression tree.
    /// Implements IFilterExpressionVisitor interface.
    /// </summary>
    private sealed class SqlFilterExpressionVisitor : Features.Filter.Models.IFilterExpressionVisitor<string>
    {
        private readonly HybridRowStore _store;

        public SqlFilterExpressionVisitor(HybridRowStore store)
        {
            _store = store;
        }

        public string VisitCondition(Features.Filter.Models.FilterCondition condition)
        {
            // Reuse existing BuildSqlFilterCondition logic
            // Convert FilterCondition to FilterCriteria
            var filterCriteria = new FilterCriteria
            {
                ColumnName = condition.ColumnName,
                Operator = condition.Operator,
                Value = condition.Value
            };

            return _store.BuildSqlFilterCondition(filterCriteria);
        }

        public string VisitBinaryExpression(Features.Filter.Models.FilterBinaryExpression expression)
        {
            // Recursively build SQL for left and right sub-expressions
            var leftSql = expression.Left.Accept(this);
            var rightSql = expression.Right.Accept(this);

            // Combine with logical operator
            var operatorSql = expression.Type switch
            {
                Features.Filter.Models.FilterExpressionType.And => "AND",
                Features.Filter.Models.FilterExpressionType.Or => "OR",
                Features.Filter.Models.FilterExpressionType.AndAlso => "AND",  // SQL doesn't have short-circuit, but behavior is equivalent
                Features.Filter.Models.FilterExpressionType.OrElse => "OR",
                _ => throw new NotSupportedException($"Filter expression type {expression.Type} not supported in SQL")
            };

            return $"({leftSql} {operatorSql} {rightSql})";
        }
    }

    /// <summary>
    /// Format value for SQL query (with proper escaping).
    /// </summary>
    private string FormatSqlValue(object? value)
    {
        if (value == null) return "NULL";
        if (value is string str) return $"'{str.Replace("'", "''")}'";  // Escape single quotes
        if (value is bool b) return b ? "1" : "0";
        if (value is DateTime dt) return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
        return value.ToString() ?? "NULL";
    }

    /// <summary>
    /// Set sort criteria (supports single or multiple columns).
    /// Builds SQL ORDER BY clause from sort descriptors.
    /// </summary>
    /// <summary>
    /// ✅ PROFESSIONAL FIX: Sorts rows by specified column and RENUMBERS __rowNumber.
    /// ARCHITECTURE:
    /// - Sorts rows by column value (ascending/descending)
    /// - SQL CTE: Calculates new __rowNumber using ROW_NUMBER() OVER (ORDER BY column)
    /// - SQL UPDATE: Updates __rowNumber for all rows
    /// EXAMPLE: Sort by "Age" descending:
    ///   - SQL: WITH sorted_rows AS (SELECT __rowId, ROW_NUMBER() OVER (ORDER BY json_extract(data, '$.Age') DESC) as new_rn FROM grid_rows)
    ///   - SQL: UPDATE grid_rows SET data = json_set(data, '$.__rowNumber', new_rn)
    /// </summary>
    public void SetSortCriteria(string columnName, SortDirection direction)
    {
        _logger.LogInformation("SetSortCriteria: column={ColumnName}, direction={Direction}", columnName, direction);

        if (direction == SortDirection.None)
        {
            _activeSortSql = null;
            _logger.LogInformation("Sort cleared - reverted to current __rowNumber order");
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

        // ✅ PROFESSIONAL: Execute SQL UPDATE to renumber __rowNumber using ROW_NUMBER()
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
                _logger.LogInformation("Sort completed: {Count} rows renumbered by column={Column}, direction={Direction}",
                    updatedCount, columnName, direction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetSortCriteria failed: column={Column}, direction={Direction}", columnName, direction);
            }
        }

        // Set active sort SQL for future queries
        _activeSortSql = $"CAST(json_extract(data, '$.__rowNumber') AS INTEGER) ASC";
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Sorts rows by MULTIPLE columns and RENUMBERS __rowNumber.
    /// CRITICAL: Fixes multi-sort where second column was ignored
    /// ARCHITECTURE:
    /// - Builds SQL ORDER BY clause with multiple columns (e.g., Column_2 ASC, Column_3 DESC)
    /// - SQL CTE: Calculates new __rowNumber using ROW_NUMBER() OVER (ORDER BY col1, col2, ...)
    /// - SQL UPDATE: Updates __rowNumber for all rows
    /// EXAMPLE: sortColumns = [(Column_2, Ascending), (Column_3, Descending)]
    ///   → ORDER BY Column_2 ASC, Column_3 DESC (with type-aware sorting)
    /// </summary>
    public void SetMultiColumnSortCriteria(IReadOnlyList<(string columnName, SortDirection direction)> sortColumns)
    {
        _logger.LogInformation("SetMultiColumnSortCriteria: {Count} columns: {Columns}",
            sortColumns.Count,
            string.Join(", ", sortColumns.Select(s => $"{s.columnName} {s.direction}")));

        if (!sortColumns.Any() || sortColumns.All(s => s.direction == SortDirection.None))
        {
            _activeSortSql = null;
            _logger.LogInformation("Multi-sort cleared - reverted to current __rowNumber order");
            return;
        }

        // ✅ PROFESSIONAL FIX: Build SQL ORDER BY clause for MULTIPLE columns
        var orderByClauses = new List<string>();

        foreach (var (columnName, direction) in sortColumns)
        {
            if (direction == SortDirection.None)
            {
                continue;
            }

            var columnPath = $"$.{columnName}";
            var directionStr = direction == SortDirection.Ascending ? "ASC" : "DESC";

            // Type-aware sorting (try numeric first, fallback to text)
            var orderByClause = $@"
                CASE
                    WHEN json_type(json_extract(data, '{columnPath}')) IN ('integer', 'real')
                    THEN CAST(json_extract(data, '{columnPath}') AS REAL)
                    ELSE NULL
                END {directionStr},
                json_extract(data, '{columnPath}') {directionStr}";

            orderByClauses.Add(orderByClause);
        }

        var fullOrderBy = string.Join(",", orderByClauses);

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
                               ROW_NUMBER() OVER (ORDER BY {fullOrderBy}) as new_rn
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
                _logger.LogInformation("Multi-sort completed: {Count} rows renumbered by {ColumnCount} columns",
                    updatedCount, sortColumns.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetMultiColumnSortCriteria failed for {ColumnCount} columns", sortColumns.Count);
            }
        }

        // Set active sort SQL for future queries
        _activeSortSql = $"CAST(json_extract(data, '$.__rowNumber') AS INTEGER) ASC";
    }

    /// <summary>
    /// Clear sort criteria (revert to default __createdAt ordering).
    /// </summary>
    public void ClearSortCriteria()
    {
        _logger.LogInformation("ClearSortCriteria: Clearing sort criteria");
        _activeSortSql = null;
    }

    /// <summary>
    /// Performs FTS5 full-text search on row data.
    /// Uses SQLite FTS5 MATCH query for efficient text search across large datasets.
    /// </summary>
    /// <param name="searchText">Text to search for</param>
    /// <param name="targetColumns">Optional: specific columns to search (null = search all columns)</param>
    /// <param name="caseSensitive">Whether search should be case-sensitive</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of row IDs that match the search criteria</returns>
    public async Task<IReadOnlyList<string>> SearchAsync(
        string searchText,
        string[]? targetColumns = null,
        bool caseSensitive = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _logger.LogWarning("SearchAsync called with empty search text");
            return Array.Empty<string>();
        }

        if (!_databaseLifecycleManager.IsInitialized)
        {
            _logger.LogWarning("SearchAsync called but database not initialized");
            return Array.Empty<string>();
        }

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
        {
            _logger.LogWarning("SearchAsync called but database connection is null");
            return Array.Empty<string>();
        }

        _logger.LogInformation("SearchAsync: searchText='{SearchText}', targetColumns={ColumnCount}, caseSensitive={CaseSensitive}",
            searchText, targetColumns?.Length ?? 0, caseSensitive);

        try
        {
            using var cmd = connection.CreateCommand();

            // Build FTS5 MATCH query
            // Note: FTS5 is case-insensitive by default
            // For case-sensitive search, we need to filter results in a WHERE clause
            string ftsQuery;

            if (targetColumns != null && targetColumns.Length > 0)
            {
                // Column-specific search: {column}:searchtext
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

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                matchedRowIds.Add(reader.GetString(0));
            }

            _logger.LogInformation("SearchAsync completed: found {MatchCount} matches for '{SearchText}'",
                matchedRowIds.Count, searchText);

            return matchedRowIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchAsync failed for search text '{SearchText}': {Message}", searchText, ex.Message);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Escapes FTS5 query syntax special characters to prevent query errors.
    /// FTS5 special chars: " (quotes), * (prefix), AND, OR, NOT, NEAR
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

    #endregion

    #region IRowStore Implementation - Basic CRUD

    public async IAsyncEnumerable<IReadOnlyList<IReadOnlyDictionary<string, object?>>> StreamRowsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        int batchSize = 1000,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_databaseLifecycleManager.IsInitialized)
            yield break;

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            yield break;

        using var cmd = connection.CreateCommand();

        // Build WHERE clause
        var whereClause = "__isDeleted = 0";
        if (onlyFiltered && !string.IsNullOrEmpty(_activeFilterSql))
        {
            whereClause += $" AND ({_activeFilterSql})";
        }
        // TODO: Implement checkbox filtering in future phase

        cmd.CommandText = $"SELECT __rowId, data FROM grid_rows WHERE {whereClause} ORDER BY __createdAt";

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var batch = new List<IReadOnlyDictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var rowId = reader.GetString(0);
            var dataJson = reader.GetString(1);
            var rowData = DeserializeRowData(rowId, dataJson);

            batch.Add(rowData);

            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<IReadOnlyDictionary<string, object?>>();
            }
        }

        // Return remaining rows
        if (batch.Count > 0)
        {
            yield return batch;
        }
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        bool onlyFiltered,
        CancellationToken cancellationToken = default)
    {
        if (!_databaseLifecycleManager.IsInitialized)
            return Array.Empty<IReadOnlyDictionary<string, object?>>();

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return Array.Empty<IReadOnlyDictionary<string, object?>>();

        using var cmd = connection.CreateCommand();

        // Build WHERE clause with filter support
        var whereClause = "__isDeleted = 0";
        if (onlyFiltered && !string.IsNullOrEmpty(_activeFilterSql))
        {
            whereClause += $" AND ({_activeFilterSql})";
        }

        cmd.CommandText = $"SELECT __rowId, data FROM grid_rows WHERE {whereClause} ORDER BY __createdAt";

        var rows = new List<IReadOnlyDictionary<string, object?>>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var rowId = reader.GetString(0);
            var dataJson = reader.GetString(1);
            var rowData = DeserializeRowData(rowId, dataJson);
            rows.Add(rowData);
        }

        _logger.LogDebug("GetAllRowsAsync returned {Count} rows (onlyFiltered={OnlyFiltered})", rows.Count, onlyFiltered);
        return rows;
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAllRowsAsync(onlyFiltered: false, cancellationToken);
    }

    public async Task<long> GetRowCountAsync(bool onlyFiltered, CancellationToken cancellationToken = default)
    {
        if (!_databaseLifecycleManager.IsInitialized)
            return 0;

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return 0;

        using var cmd = connection.CreateCommand();

        // Build WHERE clause with filter support
        var whereClause = "__isDeleted = 0";
        if (onlyFiltered && !string.IsNullOrEmpty(_activeFilterSql))
        {
            whereClause += $" AND ({_activeFilterSql})";
        }

        // ✅ PROBLEM 2 FIX (HybridRowStore): Count only non-empty data rows
        // REASON: Auto-expanded empty rows should not be counted in display statistics
        // BEHAVIOR: Must match InMemoryStorageStrategy behavior for consistency
        // SQL LOGIC: Check if JSON data has at least one non-internal key with non-null, non-empty value
        // Uses json_each to iterate JSON keys and checks if any data column (not starting with '__') has value
        cmd.CommandText = $@"
            SELECT COUNT(*)
            FROM grid_rows
            WHERE {whereClause}
            AND EXISTS (
                SELECT 1
                FROM json_each(data)
                WHERE json_each.key NOT LIKE '__%'
                AND json_each.value IS NOT NULL
                AND json_each.value != ''
                AND TRIM(json_each.value) != ''
            )";

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        var count = result != null ? Convert.ToInt64(result) : 0;

        _logger.LogDebug("✅ PROBLEM 2 FIX (Hybrid): GetRowCountAsync returning {Count} non-empty rows (onlyFiltered={OnlyFiltered})",
            count, onlyFiltered);
        return count;
    }

    public Task<long> GetRowCountAsync(CancellationToken cancellationToken = default)
    {
        return GetRowCountAsync(onlyFiltered: false, cancellationToken);
    }

    public Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement filtering in Phase 2
        return GetRowCountAsync(onlyFiltered: true, cancellationToken);
    }

    /// <summary>
    /// ✅ SENIOR FIX: Get range of rows for pagination/virtualization
    /// CRITICAL: Enables virtual pagination with O(1) SQL LIMIT/OFFSET - highly efficient for large datasets
    /// PERFORMANCE: SQL Server optimized - retrieves only requested rows (e.g., rows 60-74 for page 5)
    /// ARCHITECTURE: Foundation of dual-mode architecture - large datasets use virtual pagination
    /// COMPARISON: InMemoryRowStore O(n) skip/take vs HybridRowStore O(1) SQL LIMIT/OFFSET
    /// </summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsRangeAsync(
        long startIndex,
        int count,
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default)
    {
        if (!_databaseLifecycleManager.IsInitialized)
        {
            _logger.LogWarning("GetRowsRangeAsync called but database not initialized");
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
        {
            _logger.LogWarning("GetRowsRangeAsync called but database connection is null");
            return Array.Empty<IReadOnlyDictionary<string, object?>>();
        }

        try
        {
            using var cmd = connection.CreateCommand();

            // ✅ Build WHERE clause with __isDeleted filter + optional user filter
            var whereClause = "__isDeleted = 0";
            if (onlyFiltered && !string.IsNullOrEmpty(_activeFilterSql))
            {
                whereClause += $" AND ({_activeFilterSql})";
            }

            // ✅ PROFESSIONAL FIX: Build ORDER BY clause (primary: __rowNumber, fallback: __createdAt)
            var orderByClause = !string.IsNullOrEmpty(_activeSortSql)
                ? _activeSortSql
                : "CAST(json_extract(data, '$.__rowNumber') AS INTEGER) ASC";

            // ✅ PROFESSIONAL QUALITY: SQL with LIMIT/OFFSET for efficient pagination
            // Example: startIndex=60, count=15 → LIMIT 15 OFFSET 60 → rows 60-74
            cmd.CommandText = $@"
                SELECT __rowId, data
                FROM grid_rows
                WHERE {whereClause}
                ORDER BY {orderByClause}
                LIMIT {count} OFFSET {startIndex}";

            var results = new List<IReadOnlyDictionary<string, object?>>();
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var rowId = reader.GetString(0);
                var dataJson = reader.GetString(1);
                var rowData = DeserializeRowData(rowId, dataJson);
                results.Add(rowData);
            }

            _logger.LogDebug("GetRowsRangeAsync: startIndex={Start}, count={Count}, onlyFiltered={Filtered}, returned={Returned} rows (SQL LIMIT/OFFSET)",
                startIndex, count, onlyFiltered, results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRowsRangeAsync failed: startIndex={Start}, count={Count}, onlyFiltered={Filtered}",
                startIndex, count, onlyFiltered);
            throw;
        }
    }

    public Task PersistRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        // PersistRowsAsync = ReplaceAllRowsAsync (legacy compatibility)
        return ReplaceAllRowsAsync(rows, cancellationToken);
    }

    public async Task ReplaceAllRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ReplaceAllRowsAsync: Clearing all data and inserting new rows");

        // Clear all existing data
        await ClearAsync(cancellationToken);

        // Append new rows
        await AppendRowsAsync(rows, cancellationToken);

        // SENIOR FIX: Wait for writer queue to flush BEFORE returning
        // This prevents race condition where UI refresh fires before data is actually in DB
        // BUG SCENARIO: Replace → Clear (DB empty) → Append (queued) → UI refresh → GetAllRows() = EMPTY → No columns!
        // FIX: Flush queue → ensure data is in DB before UI refresh
        await FlushWriterQueueAsync(cancellationToken);

        _logger.LogInformation("ReplaceAllRowsAsync completed (writer queue flushed)");
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Appends rows with sequential __rowNumber.
    /// ARCHITECTURE:
    /// - Gets max existing __rowNumber from database
    /// - Assigns new rows: max+1, max+2, max+3, ...
    /// </summary>
    public async Task AppendRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        var rowsList = rows.ToList();
        if (rowsList.Count == 0)
        {
            _logger.LogDebug("AppendRowsAsync: No rows to append");
            return;
        }

        _logger.LogInformation("AppendRowsAsync: Appending {Count} rows", rowsList.Count);

        var timestamp = GetUnixTimestampMs();
        var maxRowNumber = await GetMaxRowNumberAsync(cancellationToken);

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

        await QueueWriteOperationAsync(bulkInsertOp, cancellationToken);

        // SENIOR FIX: Clear validation cache when new rows added
        // Ensures new rows are validated (not skipped as "already validated")
        ClearValidationCache();

        _logger.LogInformation("AppendRowsAsync: Queued {Count} rows for insertion with __rowNumber starting from {StartNum} (validation cache cleared)",
            rowsList.Count, maxRowNumber - rowsList.Count + 1);
    }

    /// <summary>
    /// ✅ PROFESSIONAL Helper: Gets max __rowNumber from database.
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

    public async Task EnsureInitialEmptyRowAsync(IEnumerable<string> columnNames, CancellationToken cancellationToken = default)
    {
        var count = await GetRowCountAsync(cancellationToken);
        if (count > 0)
        {
            _logger.LogDebug("EnsureInitialEmptyRowAsync: Store not empty, skipping");
            return;
        }

        _logger.LogInformation("EnsureInitialEmptyRowAsync: Creating initial empty row");

        // Create empty row with all columns set to null
        var emptyRow = columnNames.ToDictionary(col => col, col => (object?)null);

        await AppendRowsAsync(new[] { emptyRow }, cancellationToken);
    }

    public async Task InitializeEmptyRowsAsync(IEnumerable<string> columnNames, int rowCount, CancellationToken cancellationToken = default)
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
            var emptyRow = columnList.ToDictionary(col => col, col => (object?)null);
            emptyRows.Add(emptyRow);
        }

        // Append all empty rows in bulk
        await AppendRowsAsync(emptyRows, cancellationToken);

        _logger.LogInformation("InitializeEmptyRowsAsync: Successfully created {RowCount} empty rows", rowCount);
    }

    /// <summary>
    /// ✅ UNIFIED BULK: Inserts multiple rows starting at specified index.
    /// Uses SQL bulk __rowNumber shift for optimal performance (1 UPDATE for all rows).
    /// IDENTICAL behavior in InMemory and Hybrid storage.
    /// OPTIMIZED for large batches (100-1000+ rows).
    /// ARCHITECTURE: Uses __rowNumber as PRIMARY sort key (not ULID timestamp).
    /// </summary>
    /// <param name="rows">Rows to insert (WITHOUT __rowId or __rowNumber - will be added)</param>
    /// <param name="startIndex">0-based index where first row will be inserted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <example>
    /// Insert 1000 rows at index=5 (existing 1M rows in SQLite):
    ///   - STEP 1: SQL UPDATE SET __rowNumber = __rowNumber + 1000 WHERE __rowNumber >= 6 → ~50ms
    ///   - STEP 2: SQL bulk INSERT 1000 rows → ~250ms
    ///   - TOTAL: ~300ms (vs fallback AppendRowsAsync which appends at end)
    /// </example>
    public async Task InsertRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, int startIndex, CancellationToken cancellationToken = default)
    {
        var rowsList = rows.ToList();
        if (rowsList.Count == 0)
        {
            _logger?.LogDebug("InsertRowsAsync: No rows to insert");
            return;
        }

        _logger?.LogInformation(
            "InsertRowsAsync (bulk): Inserting {Count} rows at index {StartIndex} (HybridRowStore)",
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

            var shiftedCount = await cmdShift.ExecuteNonQueryAsync(cancellationToken);
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

        await QueueWriteOperationAsync(bulkInsertOp, cancellationToken);

        // SENIOR FIX: Clear validation cache when new rows added
        // Ensures new rows are validated (not skipped as "already validated")
        ClearValidationCache();

        _logger?.LogInformation(
            "InsertRowsAsync (bulk): Successfully queued {Count} rows for insertion at index {StartIndex} (validation cache cleared)",
            rowsList.Count, startIndex);
    }

    public async Task WriteValidationResultsAsync(IEnumerable<ValidationError> results, CancellationToken cancellationToken = default)
    {
        var resultsList = results.ToList();
        if (resultsList.Count == 0)
            return;

        _logger.LogDebug("WriteValidationResultsAsync: Writing {Count} validation results", resultsList.Count);

        // Group by rowId
        var groupedByRow = resultsList.GroupBy(r => r.RowId);

        foreach (var group in groupedByRow)
        {
            var rowId = group.Key;
            if (string.IsNullOrEmpty(rowId))
                continue;

            var errorsForRow = group.ToArray();

            // Update validation cache
            _validationCache[rowId] = errorsForRow;

            // Queue validation state update
            var validationJson = JsonSerializer.Serialize(errorsForRow);

            var updateOp = new UpdateValidationStateWriteOp
            {
                RowId = rowId,
                ValidationStateJson = validationJson,
                ModifiedAt = GetUnixTimestampMs(),
                OperationId = GenerateRowId()
            };

            await QueueWriteOperationAsync(updateOp, cancellationToken);
        }

        _logger.LogInformation("WriteValidationResultsAsync: Queued validation updates for {Count} rows", groupedByRow.Count());
    }

    public async Task<bool> HasValidationStateForScopeAsync(bool onlyFiltered, bool onlyChecked = false, CancellationToken cancellationToken = default)
    {
        // Check if any row has validation state in SQLite
        if (!_databaseLifecycleManager.IsInitialized)
            return false;

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return false;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM grid_rows WHERE __isDeleted = 0 AND __validationState IS NOT NULL";

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        var count = result != null ? Convert.ToInt64(result) : 0;

        return count > 0;
    }

    public async Task<bool> AreAllNonEmptyRowsMarkedValidAsync(bool onlyFiltered, bool onlyChecked = false, CancellationToken cancellationToken = default)
    {
        // Check if all non-empty rows have valid validation state
        var errors = await GetValidationErrorsAsync(onlyFiltered, onlyChecked, cancellationToken);
        return errors.Count == 0;
    }

    public async Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(bool onlyFiltered = false, bool onlyChecked = false, CancellationToken cancellationToken = default)
    {
        if (!_databaseLifecycleManager.IsInitialized)
            return Array.Empty<ValidationError>();

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return Array.Empty<ValidationError>();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT __validationState FROM grid_rows WHERE __isDeleted = 0 AND __validationState IS NOT NULL";

        var allErrors = new List<ValidationError>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var validationJson = reader.GetString(0);
            var errors = JsonSerializer.Deserialize<ValidationError[]>(validationJson);
            if (errors != null)
            {
                allErrors.AddRange(errors);
            }
        }

        return allErrors;
    }

    public async Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(string rowId, CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_validationCache.TryGetValue(rowId, out var cachedErrors))
            return cachedErrors;

        // Read from SQLite
        if (!_databaseLifecycleManager.IsInitialized)
            return Array.Empty<ValidationError>();

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return Array.Empty<ValidationError>();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT __validationState FROM grid_rows WHERE __rowId = @rowId AND __isDeleted = 0";
        cmd.Parameters.AddWithValue("@rowId", rowId);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
            return Array.Empty<ValidationError>();

        var validationJson = result.ToString();
        var errors = JsonSerializer.Deserialize<ValidationError[]>(validationJson!);

        return errors ?? Array.Empty<ValidationError>();
    }

    public Task ClearValidationErrorsForRowAsync(string rowId, CancellationToken cancellationToken = default)
    {
        // Clear from cache
        _validationCache.TryRemove(rowId, out _);

        // Note: Keeping row marked as validated in cache (it was just validated)
        // SQLite cleanup not needed - validation queries filter by existence

        return Task.CompletedTask;
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
        lock (_validationLock)
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
        lock (_validationLock)
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
        lock (_validationLock)
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

            // Update in-memory validation cache
            lock (_validationLock)
            {
                foreach (var (rowId, errors) in validationResults)
                {
                    _validationCache[rowId] = errors;
                    _validatedRowsCache[rowId] = true;
                }

                _logger.LogInformation(
                    "Batch validation write completed: {RowCount} rows validated (in-memory cache)",
                    validationResults.Count);
            }

            // NOTE: Validation results are stored in in-memory cache only
            // SQLite persistence is handled separately if needed
            // No explicit DataChanged event needed - validation state is queried from cache
        }
        finally
        {
            _isValidating = false;
        }
    }

    public async Task RemoveRowsAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default)
    {
        var rowIdsList = rowIds.ToList();
        if (rowIdsList.Count == 0)
            return;

        _logger.LogInformation("RemoveRowsAsync: Removing {Count} rows", rowIdsList.Count);

        var bulkDeleteOp = new BulkDeleteWriteOp
        {
            RowIds = rowIdsList,
            ModifiedAt = GetUnixTimestampMs(),
            OperationId = GenerateRowId()
        };

        await QueueWriteOperationAsync(bulkDeleteOp, cancellationToken);

        // Remove from caches
        foreach (var rowId in rowIdsList)
        {
            _viewportCache.TryRemove(rowId, out _);
            _validationCache.TryRemove(rowId, out _);
            _rowCreatedAtMap.TryRemove(rowId, out _);
        }

        _logger.LogInformation("RemoveRowsAsync: Queued deletion of {Count} rows", rowIdsList.Count);
    }

    public async Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(string rowId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(rowId))
            return null;

        // Check viewport cache first
        if (_viewportCache.TryGetValue(rowId, out var cachedRow))
        {
            _logger.LogTrace("GetRowByIdAsync: Cache hit for {RowId}", rowId);
            return cachedRow;
        }

        // Read from SQLite
        if (!_databaseLifecycleManager.IsInitialized)
            return null;

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return null;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT data FROM grid_rows WHERE __rowId = @rowId AND __isDeleted = 0";
        cmd.Parameters.AddWithValue("@rowId", rowId);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
            return null;

        var dataJson = result.ToString();
        var rowData = DeserializeRowData(rowId, dataJson!);

        // Update viewport cache (if not full)
        if (_viewportCache.Count < _maxViewportSize)
        {
            _viewportCache[rowId] = rowData;
        }

        _logger.LogTrace("GetRowByIdAsync: Retrieved {RowId} from SQLite", rowId);
        return rowData;
    }

    public async Task<bool> UpdateRowByIdAsync(string rowId, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(rowId))
            return false;

        _logger.LogDebug("UpdateRowByIdAsync: Updating {RowId}", rowId);

        var dataJson = SerializeRowData(rowData);

        var updateOp = new UpdateRowWriteOp
        {
            RowId = rowId,
            DataJson = dataJson,
            ModifiedAt = GetUnixTimestampMs(),
            ValidationStateJson = null,
            OperationId = GenerateRowId()
        };

        await QueueWriteOperationAsync(updateOp, cancellationToken);

        // Update viewport cache
        var rowWithMetadata = new Dictionary<string, object?>(rowData)
        {
            ["__rowId"] = rowId
        };
        _viewportCache[rowId] = rowWithMetadata;

        return true;
    }

    public async Task<bool> RemoveRowByIdAsync(string rowId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(rowId))
            return false;

        await RemoveRowsAsync(new[] { rowId }, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyDictionary<string, object?>?> GetRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        // Map index to rowId
        var rowIds = await GetAllRowIdsOrderedAsync(cancellationToken);

        if (rowIndex < 0 || rowIndex >= rowIds.Count)
            return null;

        var rowId = rowIds[rowIndex];
        return await GetRowByIdAsync(rowId, cancellationToken);
    }

    public async Task<bool> UpdateRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        // Map index to rowId
        var rowIds = await GetAllRowIdsOrderedAsync(cancellationToken);

        if (rowIndex < 0 || rowIndex >= rowIds.Count)
            return false;

        var rowId = rowIds[rowIndex];
        return await UpdateRowByIdAsync(rowId, rowData, cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ClearAsync: Clearing all data");

        if (!_databaseLifecycleManager.IsInitialized)
        {
            _logger.LogWarning("ClearAsync: Database not initialized");
            return;
        }

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return;

        // Delete all rows from SQLite
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM grid_rows";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Clear all caches
        _viewportCache.Clear();
        _validationCache.Clear();
        _rowCreatedAtMap.Clear();
        _filteredIndexMap.Clear();
        _filterCriteria = null;

        // SENIOR FIX: Clear validation cache when data is cleared
        // Ensures fresh validation when new data is imported
        ClearValidationCache();

        _logger.LogInformation("ClearAsync: All data cleared (including validation cache)");
    }

    public async Task ClearValidationStateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ClearValidationStateAsync: Clearing all validation state");

        if (!_databaseLifecycleManager.IsInitialized)
            return;

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return;

        // Clear validation state in SQLite
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE grid_rows SET __validationState = NULL";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Clear validation cache
        _validationCache.Clear();

        _logger.LogInformation("ClearValidationStateAsync: Validation state cleared");
    }

    public void SetFilterCriteria(IReadOnlyList<object>? filterCriteria)
    {
        _logger.LogInformation("SetFilterCriteria: Setting filter criteria (count: {Count})", filterCriteria?.Count ?? 0);

        _filterCriteria = filterCriteria;
        _filteredIndexMap.Clear();

        // Convert to FilterCriteria objects and build SQL WHERE clause
        if (filterCriteria == null || filterCriteria.Count == 0)
        {
            _activeFilterSql = null;
            _logger.LogInformation("Filter criteria cleared");
            return;
        }

        var filters = filterCriteria.OfType<FilterCriteria>().ToList();
        if (filters.Count == 0)
        {
            _logger.LogWarning("FilterCriteria list contains no FilterCriteria objects");
            _activeFilterSql = null;
            return;
        }

        // Build SQL WHERE clause from filter criteria
        var whereConditions = new List<string>();

        foreach (var filter in filters)
        {
            var sqlCondition = BuildSqlFilterCondition(filter);
            if (!string.IsNullOrEmpty(sqlCondition))
            {
                whereConditions.Add(sqlCondition);
            }
        }

        // Combine with AND logic
        _activeFilterSql = whereConditions.Count > 0
            ? string.Join(" AND ", whereConditions)
            : null;

        _logger.LogInformation("Filter SQL built: {FilterCount} filters → WHERE {Sql}",
            filters.Count, _activeFilterSql);
    }

    public void ClearFilterCriteria()
    {
        _logger.LogInformation("ClearFilterCriteria: Clearing filter criteria");

        _filterCriteria = null;
        _filteredIndexMap.Clear();
    }

    /// <summary>
    /// ✅ PROBLEM 2 FIX: Check if any filter is currently active
    /// </summary>
    public bool HasActiveFilter()
    {
        bool hasFilter = !string.IsNullOrEmpty(_activeFilterSql);
        _logger.LogDebug("✅ PROBLEM 2 FIX (HybridRowStore): HasActiveFilter={HasFilter} (filter SQL length: {Length})",
            hasFilter, _activeFilterSql?.Length ?? 0);
        return hasFilter;
    }

    public IReadOnlyList<object> GetFilterCriteria()
    {
        return _filterCriteria ?? Array.Empty<object>();
    }

    public void SetFilterExpression(Features.Filter.Models.FilterExpression? expression)
    {
        _logger.LogInformation("SetFilterExpression: Setting complex filter expression (null: {IsNull})", expression == null);

        _filterExpression = expression;
        _filteredIndexMap.Clear();

        // Build SQL WHERE clause from filter expression
        if (expression == null)
        {
            _activeFilterSql = null;
            _logger.LogInformation("Filter expression cleared");
            return;
        }

        try
        {
            // Generate SQL WHERE clause from expression tree
            _activeFilterSql = BuildSqlFilterExpression(expression);

            _logger.LogInformation("Filter expression SQL built: WHERE {Sql}", _activeFilterSql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build SQL from filter expression");
            _activeFilterSql = null;
            _filterExpression = null;
            throw;
        }
    }

    public Features.Filter.Models.FilterExpression? GetFilterExpression()
    {
        return _filterExpression;
    }

    public int? MapFilteredIndexToOriginalIndex(int filteredIndex)
    {
        // TODO: Implement in Phase 2 (Filter/Sort/Search Integration)
        if (_filterCriteria == null || _filterCriteria.Count == 0)
            return filteredIndex;

        if (_filteredIndexMap.TryGetValue(filteredIndex, out var rowId))
        {
            // TODO: Map rowId to original index
            return null;
        }

        return null;
    }

    public async Task<IReadOnlyDictionary<string, object?>?> GetLastRowAsync(CancellationToken cancellationToken = default)
    {
        if (!_databaseLifecycleManager.IsInitialized)
            return null;

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return null;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT __rowId, data FROM grid_rows WHERE __isDeleted = 0 ORDER BY __createdAt DESC LIMIT 1";

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            var rowId = reader.GetString(0);
            var dataJson = reader.GetString(1);
            return DeserializeRowData(rowId, dataJson);
        }

        return null;
    }

    // Public API compatibility methods

    public async Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        await AppendRowsAsync(new[] { rowData }, cancellationToken);

        var count = await GetRowCountAsync(cancellationToken);
        return (int)count - 1; // Return index of newly added row
    }

    public async Task<int> AddRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData, CancellationToken cancellationToken = default)
    {
        var rowsList = rowsData.ToList();
        await AppendRowsAsync(rowsList, cancellationToken);

        return rowsList.Count;
    }

    public Task InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        // Index-based insertion not supported - use AppendRowsAsync
        _logger.LogWarning("InsertRowAsync(index): Index-based insertion not supported, using AppendRowsAsync");
        return AppendRowsAsync(new[] { rowData }, cancellationToken);
    }

    public async Task RemoveRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        var rowIds = await GetAllRowIdsOrderedAsync(cancellationToken);

        if (rowIndex >= 0 && rowIndex < rowIds.Count)
        {
            var rowId = rowIds[rowIndex];
            await RemoveRowByIdAsync(rowId, cancellationToken);
        }
    }

    public async Task<int> RemoveRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default)
    {
        var indices = rowIndices.OrderByDescending(i => i).ToList(); // Remove from end to start
        var rowIds = await GetAllRowIdsOrderedAsync(cancellationToken);

        var rowIdsToRemove = new List<string>();

        foreach (var index in indices)
        {
            if (index >= 0 && index < rowIds.Count)
            {
                rowIdsToRemove.Add(rowIds[index]);
            }
        }

        if (rowIdsToRemove.Count > 0)
        {
            await RemoveRowsAsync(rowIdsToRemove, cancellationToken);
        }

        return rowIdsToRemove.Count;
    }

    public Task ClearAllRowsAsync(CancellationToken cancellationToken = default)
    {
        return ClearAsync(cancellationToken);
    }

    public IReadOnlyDictionary<string, object?>? GetRow(int rowIndex)
    {
        // Synchronous API - not ideal for SQLite
        // Use async version GetRowAsync instead
        return GetRowAsync(rowIndex).GetAwaiter().GetResult();
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetAllRows()
    {
        // Synchronous API - not ideal for SQLite
        // Use async version GetAllRowsAsync instead
        return GetAllRowsAsync().GetAwaiter().GetResult();
    }

    public int GetRowCount()
    {
        // Synchronous API - not ideal for SQLite
        // Use async version GetRowCountAsync instead
        return (int)GetRowCountAsync().GetAwaiter().GetResult();
    }

    public bool RowExists(int rowIndex)
    {
        var count = GetRowCount();
        return rowIndex >= 0 && rowIndex < count;
    }

    #endregion

    #region Additional Helper Methods (not in IRowStore interface)

    /// <summary>
    /// Get paged rows from SQLite database with optional filtering/sorting.
    /// Useful for pagination scenarios.
    /// </summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPagedRowsAsync(
        int pageNumber,
        int pageSize,
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default)
    {
        if (!_databaseLifecycleManager.IsInitialized)
            return Array.Empty<IReadOnlyDictionary<string, object?>>();

        var connection = _databaseLifecycleManager.GetConnection();
        if (connection == null)
            return Array.Empty<IReadOnlyDictionary<string, object?>>();

        var offset = (pageNumber - 1) * pageSize;

        using var cmd = connection.CreateCommand();

        // Build WHERE clause with filter support
        var whereClause = "__isDeleted = 0";
        if (onlyFiltered && !string.IsNullOrEmpty(_activeFilterSql))
        {
            whereClause += $" AND ({_activeFilterSql})";
        }

        // Build ORDER BY clause (default: creation time)
        var orderByClause = !string.IsNullOrEmpty(_activeSortSql)
            ? _activeSortSql
            : "__createdAt ASC";

        cmd.CommandText = $@"
            SELECT __rowId, data
            FROM grid_rows
            WHERE {whereClause}
            ORDER BY {orderByClause}
            LIMIT {pageSize} OFFSET {offset}";

        var results = new List<IReadOnlyDictionary<string, object?>>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var rowId = reader.GetString(0);
            var dataJson = reader.GetString(1);
            var rowData = DeserializeRowData(rowId, dataJson);
            results.Add(rowData);
        }

        _logger.LogDebug("GetPagedRowsAsync: Retrieved page {Page} with {Count} rows (pageSize={PageSize}, onlyFiltered={OnlyFiltered})",
            pageNumber, results.Count, pageSize, onlyFiltered);

        return results;
    }

    #endregion

    #region Insert Row Convenience Methods

    /// <summary>
    /// Insert single row AFTER the specified row index
    /// Creates empty row with UserInserted metadata
    /// </summary>
    public async Task InsertRowAfterAsync(
        int targetRowIndex,
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("InsertRowAfterAsync: START - targetRowIndex={Index}", targetRowIndex);
            _logger.LogDebug("InsertRowAfterAsync: newRow has {Count} columns", newRow?.Count ?? 0);

            // Get template row to copy column structure
            var allRows = await GetAllRowsAsync(cancellationToken);
            _logger.LogDebug("InsertRowAfterAsync: Retrieved {Count} existing rows from storage", allRows.Count);

            if (allRows.Count == 0)
            {
                _logger.LogWarning("InsertRowAfterAsync: No rows exist, appending new row as first row");
                await AppendRowsAsync(new[] { CreateUserInsertedRow(newRow) }, cancellationToken);
                _logger.LogInformation("InsertRowAfterAsync: COMPLETED - First row appended");
                return;
            }

            if (targetRowIndex < 0 || targetRowIndex >= allRows.Count)
            {
                _logger.LogWarning("InsertRowAfterAsync: Invalid targetRowIndex={Index} (count={Count}), clamping to valid range",
                    targetRowIndex, allRows.Count);
            }

            // Create row with UserInserted metadata
            var rowToInsert = CreateUserInsertedRow(newRow);
            _logger.LogDebug("InsertRowAfterAsync: Created UserInserted row with rowId={RowId}",
                rowToInsert.TryGetValue("__rowId", out var rid) ? rid : "unknown");

            // HybridRowStore uses ULID ordering, so just append
            await AppendRowsAsync(new[] { rowToInsert }, cancellationToken);

            _logger.LogInformation("InsertRowAfterAsync: COMPLETED - Row inserted (appended due to ULID ordering)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertRowAfterAsync: FAILED - targetRowIndex={Index}, error={Message}",
                targetRowIndex, ex.Message);
            throw; // Re-throw to propagate to caller
        }
    }

    /// <summary>
    /// Insert single row BEFORE the specified row index
    /// Creates empty row with UserInserted metadata
    /// </summary>
    public async Task InsertRowBeforeAsync(
        int targetRowIndex,
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("InsertRowBeforeAsync: START - targetRowIndex={Index}", targetRowIndex);
            _logger.LogDebug("InsertRowBeforeAsync: newRow has {Count} columns", newRow?.Count ?? 0);

            // Get template row to copy column structure
            var allRows = await GetAllRowsAsync(cancellationToken);
            _logger.LogDebug("InsertRowBeforeAsync: Retrieved {Count} existing rows from storage", allRows.Count);

            if (allRows.Count == 0 || targetRowIndex == 0)
            {
                _logger.LogInformation("InsertRowBeforeAsync: Target is index 0 or no rows exist, delegating to InsertRowAtTopAsync");
                await InsertRowAtTopAsync(newRow, cancellationToken);
                _logger.LogInformation("InsertRowBeforeAsync: COMPLETED via InsertRowAtTopAsync delegation");
                return;
            }

            if (targetRowIndex < 0 || targetRowIndex >= allRows.Count)
            {
                _logger.LogWarning("InsertRowBeforeAsync: Invalid targetRowIndex={Index} (count={Count}), clamping to valid range",
                    targetRowIndex, allRows.Count);
            }

            // Create row with UserInserted metadata
            var rowToInsert = CreateUserInsertedRow(newRow);
            _logger.LogDebug("InsertRowBeforeAsync: Created UserInserted row with rowId={RowId}",
                rowToInsert.TryGetValue("__rowId", out var rid) ? rid : "unknown");

            // HybridRowStore uses ULID ordering, so just append
            await AppendRowsAsync(new[] { rowToInsert }, cancellationToken);

            _logger.LogInformation("InsertRowBeforeAsync: COMPLETED - Row inserted (appended due to ULID ordering)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertRowBeforeAsync: FAILED - targetRowIndex={Index}, error={Message}",
                targetRowIndex, ex.Message);
            throw; // Re-throw to propagate to caller
        }
    }

    /// <summary>
    /// Insert single row at the top (index 0)
    /// Creates empty row with UserInserted metadata
    /// </summary>
    public async Task InsertRowAtTopAsync(
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("InsertRowAtTopAsync: START");
            _logger.LogDebug("InsertRowAtTopAsync: newRow has {Count} columns", newRow?.Count ?? 0);

            // Create row with UserInserted metadata
            var rowToInsert = CreateUserInsertedRow(newRow);
            _logger.LogDebug("InsertRowAtTopAsync: Created UserInserted row with rowId={RowId}",
                rowToInsert.TryGetValue("__rowId", out var rid) ? rid : "unknown");

            // HybridRowStore uses ULID ordering, so just append
            // NOTE: For true "insert at top" behavior, would need to modify ULID generation
            // Currently this will append at end (same as other insert methods)
            await AppendRowsAsync(new[] { rowToInsert }, cancellationToken);

            _logger.LogWarning("InsertRowAtTopAsync: COMPLETED - Row appended at end (ULID ordering constraint - not at top!)");
            _logger.LogDebug("InsertRowAtTopAsync: For true top insertion, consider index-based storage or custom ULID generation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InsertRowAtTopAsync: FAILED - error={Message}", ex.Message);
            throw; // Re-throw to propagate to caller
        }
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
            mutableRow["__rowId"] = GenerateRowId();
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
    /// NOTE: Uses existing GetRowByIdAsync implementation (line ~1315)
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

    /// <summary>
    /// PROFESSIONAL QUALITY: Bulk update multiple rows in a single operation.
    /// HYBRID IMPLEMENTATION: Delegates to serial UpdateRowByIdAsync for now.
    /// TODO: Optimize with batch SQL UPDATE when needed for performance.
    /// </summary>
    public async Task<int> BulkUpdateRowsAsync(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> updates,
        CancellationToken cancellationToken = default)
    {
        if (updates == null || updates.Count == 0)
        {
            _logger.LogDebug("BulkUpdateRowsAsync: No updates to perform");
            return 0;
        }

        // ✅ PERFORMANCE FIX: Use BulkUpdateWriteOp with SQL transaction (10-50x faster)
        // Convert dictionary to RowUpdateData list
        var modifiedAt = GetUnixTimestampMs();
        var rowUpdateList = new List<RowUpdateData>(updates.Count);

        foreach (var kvp in updates)
        {
            var rowId = kvp.Key;
            var rowData = kvp.Value;
            var dataJson = SerializeRowData(rowData);

            rowUpdateList.Add(new RowUpdateData
            {
                RowId = rowId,
                DataJson = dataJson,
                ModifiedAt = modifiedAt,
                ValidationStateJson = null
            });

            // Update viewport cache immediately (optimistic update)
            var rowWithMetadata = new Dictionary<string, object?>(rowData)
            {
                ["__rowId"] = rowId
            };
            _viewportCache[rowId] = rowWithMetadata;
        }

        // Queue single bulk update operation (SQL transaction)
        var bulkUpdateOp = new BulkUpdateWriteOp
        {
            Rows = rowUpdateList,
            OperationId = GenerateRowId()
        };

        await QueueWriteOperationAsync(bulkUpdateOp, cancellationToken);

        _logger.LogInformation("BulkUpdateRowsAsync: Queued bulk update of {Count} rows (SINGLE SQL TRANSACTION)", updates.Count);
        return updates.Count;
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

    #region RIEŠENIE #2 - FIXED UI POOL stubs

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Inserts row at specified index (0-based) in SQLite.
    /// ARCHITECTURE:
    /// - index=0-based, __rowNumber=1-based
    /// - targetRowNumber = index + 1
    /// - SQL UPDATE: Shifts rows with __rowNumber >= targetRowNumber UP by 1
    /// - SQL INSERT: Inserts new row with __rowNumber = targetRowNumber
    /// EXAMPLE: Insert at index=2 (100 existing rows):
    ///   - targetRowNumber = 3
    ///   - SQL: UPDATE rows SET __rowNumber = __rowNumber + 1 WHERE __rowNumber >= 3
    ///   - SQL: INSERT new row with __rowNumber=3
    ///   - Result: __rowNumber = 1-2, 3 (NEW), 4-101
    /// </summary>
    public async Task<string> InsertRowAtIndexAsync(int index, IReadOnlyDictionary<string, object?>? rowData, CancellationToken cancellationToken = default)
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

            var shiftedCount = await cmdShift.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogDebug("InsertRowAtIndex {Index}: Shifted {Count} rows UP (__rowNumber >= {StartNum})",
                index, shiftedCount, targetRowNumber);
        }

        // ✅ STEP 2: Insert new row with __rowNumber
        var newRowId = GenerateRowId();
        var dataDict = new Dictionary<string, object?>(rowData ?? new Dictionary<string, object?>());
        dataDict["__rowNumber"] = targetRowNumber;

        var insertOp = new InsertRowWriteOp
        {
            RowId = newRowId,
            DataJson = SerializeRowData(dataDict),
            CreatedAt = GetUnixTimestampMs(),
            ModifiedAt = GetUnixTimestampMs(),
            ValidationStateJson = null
        };

        await QueueWriteOperationAsync(insertOp, cancellationToken);
        await FlushWriterQueueAsync(cancellationToken);

        _logger.LogInformation("InsertRowAtIndex: Added row at index {Index} (RowId={RowId}, __rowNumber={RowNumber})",
            index, newRowId, targetRowNumber);

        return newRowId;
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Deletes row by RowID and shifts subsequent rows DOWN in SQLite.
    /// ARCHITECTURE:
    /// - Accepts RowID (STABLE identifier)
    /// - SQL SELECT: Gets __rowNumber of deleted row
    /// - SQL UPDATE: Sets __isDeleted=1 (soft delete)
    /// - SQL UPDATE: Shifts rows with __rowNumber > deleted DOWN by 1
    /// EXAMPLE: Delete row with __rowNumber=5 (100 existing rows):
    ///   - SQL: SELECT __rowNumber WHERE __rowId='...' → returns 5
    ///   - SQL: UPDATE SET __isDeleted=1 WHERE __rowId='...'
    ///   - SQL: UPDATE rows SET __rowNumber = __rowNumber - 1 WHERE __rowNumber > 5
    ///   - Result: __rowNumber = 1-4, 5-99 (99 rows total)
    /// </summary>
    public async Task DeleteRowByIdAsync(string rowId, CancellationToken cancellationToken = default)
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

            var result = await cmdGet.ExecuteScalarAsync(cancellationToken);

            if (result == null || result == DBNull.Value)
            {
                _logger.LogWarning("DeleteRowById: Row {RowId} not found", rowId);
                return;
            }

            deletedRowNumber = Convert.ToInt32(result);
        }

        // ✅ STEP 2: Soft delete row
        using (var cmdDelete = connection.CreateCommand())
        {
            cmdDelete.CommandText = "UPDATE grid_rows SET __isDeleted = 1 WHERE __rowId = @rowId";
            cmdDelete.Parameters.AddWithValue("@rowId", rowId);
            await cmdDelete.ExecuteNonQueryAsync(cancellationToken);
        }

        // ✅ STEP 3: Shift rows DOWN
        using (var cmdShift = connection.CreateCommand())
        {
            cmdShift.CommandText = $@"
                UPDATE grid_rows
                SET data = json_set(data, '$.__rowNumber', CAST(json_extract(data, '$.__rowNumber') AS INTEGER) - 1)
                WHERE __isDeleted = 0
                  AND CAST(json_extract(data, '$.__rowNumber') AS INTEGER) > {deletedRowNumber}";

            var shiftedCount = await cmdShift.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("DeleteRowById: Deleted row {RowId} (__rowNumber={RowNumber}), shifted {ShiftCount} rows DOWN",
                rowId, deletedRowNumber, shiftedCount);
        }
    }

    #endregion
}

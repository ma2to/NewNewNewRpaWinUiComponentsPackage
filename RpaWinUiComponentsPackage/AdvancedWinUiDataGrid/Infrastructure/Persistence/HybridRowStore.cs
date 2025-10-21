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
    private string? _activeFilterSql; // SQL WHERE clause for active filters

    // Sort support
    private string? _activeSortSql; // SQL ORDER BY clause for active sort

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
    public void SetSortCriteria(string columnName, SortDirection direction)
    {
        _logger.LogInformation("SetSortCriteria: column={ColumnName}, direction={Direction}", columnName, direction);

        if (direction == SortDirection.None)
        {
            _activeSortSql = null;
            _logger.LogInformation("Sort cleared");
            return;
        }

        // Build SQL ORDER BY clause for single column
        var columnPath = $"$.{columnName}";
        var directionStr = direction == SortDirection.Ascending ? "ASC" : "DESC";

        // Type-aware sorting (try numeric first, fallback to text)
        _activeSortSql = $@"
            CASE
                WHEN json_type(json_extract(data, '{columnPath}')) IN ('integer', 'real')
                THEN CAST(json_extract(data, '{columnPath}') AS REAL)
                ELSE NULL
            END {directionStr},
            json_extract(data, '{columnPath}') {directionStr}";

        _logger.LogInformation("Sort SQL built: ORDER BY {Sql}", _activeSortSql);
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

        cmd.CommandText = $"SELECT COUNT(*) FROM grid_rows WHERE {whereClause}";

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        var count = result != null ? Convert.ToInt64(result) : 0;

        _logger.LogDebug("GetRowCountAsync: {Count} rows (onlyFiltered={OnlyFiltered})", count, onlyFiltered);
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

        _logger.LogInformation("ReplaceAllRowsAsync completed");
    }

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

        // Create bulk insert operation
        var insertData = rowsList.Select(row =>
        {
            var rowId = row.ContainsKey("__rowId") && row["__rowId"] is string existingId
                ? existingId
                : GenerateRowId();

            var dataJson = SerializeRowData(row);

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

        _logger.LogInformation("AppendRowsAsync: Queued {Count} rows for insertion", rowsList.Count);
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

    public async Task InsertRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, int startIndex, CancellationToken cancellationToken = default)
    {
        // For SQLite-based storage, index-based insertion is not meaningful
        // We use ULID-based ordering (creation time)
        // Simply append the rows - they will be ordered by creation time
        _logger.LogWarning("InsertRowsAsync(startIndex): Index-based insertion not supported in HybridRowStore, using AppendRowsAsync");
        await AppendRowsAsync(rows, cancellationToken);
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

        _logger.LogInformation("ClearAsync: All data cleared");
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

    public IReadOnlyList<object> GetFilterCriteria()
    {
        return _filterCriteria ?? Array.Empty<object>();
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

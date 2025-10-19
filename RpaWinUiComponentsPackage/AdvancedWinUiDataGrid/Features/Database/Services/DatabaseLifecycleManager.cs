using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Services;

/// <summary>
/// Internal service for database lifecycle management.
/// Handles initialization, shutdown, and cleanup operations for SQLite database.
/// Thread-safe implementation with semaphore locking for initialization/shutdown.
/// </summary>
internal sealed class DatabaseLifecycleManager : IDatabaseLifecycleManager
{
    private readonly ILogger<DatabaseLifecycleManager> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized = false;
    private bool _isDisposed = false;

    private SqliteConnection? _connection;
    private string? _databasePath;
    private DateTime? _createdAt;

    public DatabaseLifecycleManager(ILogger<DatabaseLifecycleManager> logger)
    {
        _logger = logger ?? NullLogger<DatabaseLifecycleManager>.Instance;
    }

    public bool IsInitialized
    {
        get
        {
            lock (_initLock)
            {
                return _isInitialized;
            }
        }
    }

    public async Task<Result> InitializeDatabaseAsync(
        string? databasePath = null,
        CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                _logger.LogWarning("Database already initialized at {Path} - skipping re-initialization", _databasePath);
                return Result.Success();
            }

            // Determine database path with smart logic
            var pathResult = ResolveDatabasePath(databasePath);
            if (!pathResult.IsSuccess)
            {
                _logger.LogError("Invalid database path: {Error}", pathResult.ErrorMessage);
                return Result.Failure(pathResult.ErrorMessage);
            }
            _databasePath = pathResult.Value;

            _logger.LogInformation("Initializing database at {Path}", _databasePath);

            // Ensure parent directory exists
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created directory {Directory}", directory);
            }

            // Delete existing file if present (fresh start)
            if (File.Exists(_databasePath))
            {
                _logger.LogWarning("Database file already exists - deleting and recreating: {Path}", _databasePath);
                File.Delete(_databasePath);
            }

            // Create SQLite connection
            var connectionString = $"Data Source={_databasePath}";
            _connection = new SqliteConnection(connectionString);
            await _connection.OpenAsync(cancellationToken);

            _logger.LogInformation("SQLite connection opened: {ConnectionState}", _connection.State);

            // Apply PRAGMA settings for performance
            await ApplyPragmaSettingsAsync(cancellationToken);

            // Create schema (tables, indexes, FTS)
            await CreateSchemaAsync(cancellationToken);

            // Verify database file was actually created
            if (!File.Exists(_databasePath))
            {
                _logger.LogError("Database file was not created: {Path}", _databasePath);
                await _connection.DisposeAsync();
                _connection = null;
                return Result.Failure($"Database file was not created at: {_databasePath}");
            }
            _logger.LogDebug("Database file verified to exist at: {Path}", _databasePath);

            _createdAt = DateTime.UtcNow;
            _isInitialized = true;

            _logger.LogInformation("Database initialized successfully at {Path}", _databasePath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed: {Message}", ex.Message);

            // Cleanup on failure
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            return Result.Failure($"Database initialization failed: {ex.Message}");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<Result> ShutdownDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (!_isInitialized)
            {
                _logger.LogInformation("Database not initialized - nothing to shutdown");
                return Result.Success();
            }

            _logger.LogInformation("Shutting down database at {Path}", _databasePath);

            // Close connection
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
                _logger.LogInformation("Database connection closed");
            }

            // Delete temp file (best effort - don't fail if locked)
            if (!string.IsNullOrEmpty(_databasePath) && File.Exists(_databasePath))
            {
                try
                {
                    File.Delete(_databasePath);
                    _logger.LogInformation("Deleted database file: {Path}", _databasePath);
                }
                catch (IOException ex)
                {
                    // File might be locked by another process - log warning but don't fail
                    _logger.LogWarning(ex, "Could not delete database file {Path} - file may be locked", _databasePath);
                }
            }

            _isInitialized = false;
            _databasePath = null;
            _createdAt = null;

            _logger.LogInformation("Database shutdown completed successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database shutdown failed: {Message}", ex.Message);
            return Result.Failure($"Database shutdown failed: {ex.Message}");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public string? GetDatabasePath() => _databasePath;

    public SqliteConnection? GetConnection() => _connection;

    public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _connection == null)
            throw new InvalidOperationException("Database not initialized - call InitializeDatabaseAsync first");

        var fileSize = File.Exists(_databasePath!) ? new FileInfo(_databasePath!).Length : 0;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT
                COUNT(*) as TotalCount,
                SUM(CASE WHEN __isDeleted = 0 THEN 1 ELSE 0 END) as ActiveCount,
                SUM(CASE WHEN __isDeleted = 1 THEN 1 ELSE 0 END) as DeletedCount
            FROM grid_rows";

        long totalCount = 0, activeCount = 0, deletedCount = 0;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            totalCount = reader.GetInt64(0);
            activeCount = reader.GetInt64(1);
            deletedCount = reader.GetInt64(2);
        }

        _logger.LogDebug("Database statistics: Total={Total}, Active={Active}, Deleted={Deleted}, Size={Size}",
            totalCount, activeCount, deletedCount, fileSize);

        return new DatabaseStatistics
        {
            FilePath = _databasePath,
            FileSizeBytes = fileSize,
            TotalRowCount = totalCount,
            ActiveRowCount = activeCount,
            DeletedRowCount = deletedCount,
            CreatedAt = _createdAt,
            LastModifiedAt = File.Exists(_databasePath!) ? File.GetLastWriteTimeUtc(_databasePath!) : null
        };
    }

    public async Task<Result<int>> DeleteAllDatabasesInPathAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory does not exist: {Path}", directoryPath);
                return Result<int>.Success(0);
            }

            var dbFiles = Directory.GetFiles(directoryPath, "*.db", SearchOption.TopDirectoryOnly);
            _logger.LogInformation("Found {Count} database files in {Path}", dbFiles.Length, directoryPath);

            int deletedCount = 0;

            foreach (var dbFile in dbFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    File.Delete(dbFile);
                    deletedCount++;
                    _logger.LogDebug("Deleted database file: {Path}", dbFile);
                }
                catch (IOException ex)
                {
                    // File might be in use - skip it
                    _logger.LogWarning(ex, "Could not delete database file {Path} - file may be in use", dbFile);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Access denied to delete file {Path}", dbFile);
                }
            }

            _logger.LogInformation("Deleted {DeletedCount}/{TotalCount} database files from {Path}",
                deletedCount, dbFiles.Length, directoryPath);

            return Result<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete databases in path {Path}: {Message}", directoryPath, ex.Message);
            return Result<int>.Failure($"Cleanup failed: {ex.Message}");
        }
    }

    // PRIVATE HELPER METHODS

    /// <summary>
    /// Resolves database path with smart logic:
    /// - null -> default temp path with GUID
    /// - ends with .db -> use exactly as provided
    /// - directory path -> directory + GUID filename
    /// - file with non-.db extension -> ERROR
    /// </summary>
    private Result<string> ResolveDatabasePath(string? databasePath)
    {
        // Case 1: null -> default path
        if (databasePath == null)
        {
            return Result<string>.Success(GenerateDefaultDatabasePath());
        }

        // Case 2: ends with .db -> use as-is (explicit file path)
        if (databasePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Using explicit .db file path: {Path}", databasePath);
            return Result<string>.Success(databasePath);
        }

        // Case 3: directory path (no extension or directory exists)
        if (!Path.HasExtension(databasePath) || Directory.Exists(databasePath))
        {
            var fileName = $"grid_{Guid.NewGuid():N}.db";
            var fullPath = Path.Combine(databasePath, fileName);
            _logger.LogDebug("Generated path from directory: {Path}", fullPath);
            return Result<string>.Success(fullPath);
        }

        // Case 4: file with non-.db extension -> ERROR
        var ext = Path.GetExtension(databasePath);
        _logger.LogError("Invalid database path extension: {Ext} (must be .db)", ext);
        return Result<string>.Failure($"Invalid database path: '{databasePath}'. Must end with '.db' or be a directory path.");
    }

    private string GenerateDefaultDatabasePath()
    {
        var tempPath = Path.GetTempPath();
        var appFolder = Path.Combine(tempPath, "AdvancedDataGrid");

        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
            _logger.LogInformation("Created temp directory: {Path}", appFolder);
        }

        var fileName = $"grid_{Guid.NewGuid():N}.db";
        var fullPath = Path.Combine(appFolder, fileName);

        _logger.LogDebug("Generated default database path: {Path}", fullPath);
        return fullPath;
    }

    private async Task ApplyPragmaSettingsAsync(CancellationToken cancellationToken)
    {
        using var cmd = _connection!.CreateCommand();

        // WAL mode (Write-Ahead Logging) - faster writes, allows concurrent readers
        cmd.CommandText = "PRAGMA journal_mode = WAL;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // NORMAL synchronous mode - balance between safety and performance
        cmd.CommandText = "PRAGMA synchronous = NORMAL;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Temp store in memory
        cmd.CommandText = "PRAGMA temp_store = MEMORY;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Cache size (20MB)
        cmd.CommandText = "PRAGMA cache_size = 20000;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // Memory-mapped I/O (256MB) - faster for large files
        cmd.CommandText = "PRAGMA mmap_size = 268435456;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("PRAGMA settings applied: WAL mode, NORMAL sync, 20MB cache, 256MB mmap");
    }

    private async Task CreateSchemaAsync(CancellationToken cancellationToken)
    {
        using var cmd = _connection!.CreateCommand();

        // Main table
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS grid_rows (
                __rowId TEXT PRIMARY KEY,
                __createdAt INTEGER NOT NULL,
                __modifiedAt INTEGER NOT NULL,
                __isDeleted INTEGER DEFAULT 0,
                __validationState TEXT,
                data TEXT NOT NULL
            );";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Created table: grid_rows");

        // Indexes
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_createdAt ON grid_rows(__createdAt);";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_isDeleted ON grid_rows(__isDeleted);";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Created indexes: idx_createdAt, idx_isDeleted");

        // FTS5 virtual table for full-text search
        cmd.CommandText = @"
            CREATE VIRTUAL TABLE IF NOT EXISTS grid_rows_fts USING fts5(
                __rowId UNINDEXED,
                data,
                content='grid_rows',
                content_rowid='rowid'
            );";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Created FTS5 virtual table: grid_rows_fts");

        // FTS triggers for auto-sync
        cmd.CommandText = @"
            CREATE TRIGGER IF NOT EXISTS grid_rows_ai AFTER INSERT ON grid_rows BEGIN
              INSERT INTO grid_rows_fts(rowid, __rowId, data)
              VALUES (new.rowid, new.__rowId, new.data);
            END;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        cmd.CommandText = @"
            CREATE TRIGGER IF NOT EXISTS grid_rows_au AFTER UPDATE ON grid_rows BEGIN
              UPDATE grid_rows_fts SET data = new.data WHERE rowid = new.rowid;
            END;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        cmd.CommandText = @"
            CREATE TRIGGER IF NOT EXISTS grid_rows_ad AFTER DELETE ON grid_rows BEGIN
              DELETE FROM grid_rows_fts WHERE rowid = old.rowid;
            END;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Database schema created: grid_rows table + FTS5 + triggers");
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        await ShutdownDatabaseAsync();
        _initLock?.Dispose();

        _isDisposed = true;
        _logger.LogDebug("DatabaseLifecycleManager disposed");
    }
}

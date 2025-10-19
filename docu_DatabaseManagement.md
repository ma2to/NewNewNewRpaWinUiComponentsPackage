# DATABASE MANAGEMENT - Špecifikácia správy SQLite databáz

## 📋 METADATA

**Dátum vytvorenia:** 19.10.2025
**Verzia:** 1.0
**Jazyk kódu:** English
**Jazyk dokumentácie:** Slovenčina
**Komponent:** AdvancedDataGrid - Database Module

---

## 🎯 ÚVOD

Tento dokument špecifikuje správu SQLite databáz pre AdvancedDataGrid komponent v hybrid SQLite modeli.

### Kľúčové požiadavky

1. **Per-instance database** - Každá inštancia má vlastný temp SQLite file
2. **User-specified path OR temp folder** - Programátor môže zvoliť umiestnenie
3. **Automatic cleanup** - Pri shutdown alebo app exit
4. **Bulk cleanup method** - Zmazať všetky DB v danej ceste (pre app startup)
5. **Internal + Public API** - Metódy dostupné internal aj cez Facade

---

## 📐 ARCHITEKTÚRA DATABASE LIFECYCLE

### Lifecycle diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│  Application Startup                                                 │
│  ├─ Demo app calls DeleteAllDatabasesInPathAsync(tempPath)          │
│  │  (Clean up old sessions)                                          │
│  └─ Creates 1+ grid instances                                        │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Grid Instance Creation                                              │
│  ├─ AdvancedDataGridBuilder.Build()                                 │
│  │  Creates facade + services                                        │
│  │  HybridRowStore created but DB NOT initialized yet               │
│  └─ Returns IAdvancedDataGridFacade                                 │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Database Initialization (REQUIRED before data operations)          │
│  ├─ facade.Database.InitializeDatabaseAsync(dbPath)                 │
│  │  ├─ Creates SQLite file at specified path                        │
│  │  ├─ Applies PRAGMA settings (WAL, NORMAL sync, etc.)            │
│  │  ├─ Creates tables (grid_rows, grid_rows_fts)                   │
│  │  ├─ Creates indexes                                              │
│  │  ├─ Starts writer queue thread                                   │
│  │  └─ Returns PublicResult (success/failure)                       │
│  └─ Grid ready for data operations                                  │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Normal Operations (Import, Edit, Validate, Search, etc.)           │
│  All data operations work with SQLite + in-memory viewport          │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Grid Instance Shutdown                                              │
│  ├─ facade.Database.ShutdownDatabaseAsync()                         │
│  │  ├─ Stops writer queue thread                                    │
│  │  ├─ Flushes pending writes                                       │
│  │  ├─ Closes SQLite connection                                     │
│  │  ├─ Deletes temp DB file (if possible)                          │
│  │  └─ Returns PublicResult                                         │
│  └─ facade.DisposeAsync()                                           │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 API ŠPECIFIKÁCIA

### 1. IDataGridDatabase (Public API)

```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Database;

/// <summary>
/// Public interface for database lifecycle management.
/// Provides methods for initializing, shutting down, and managing SQLite databases.
/// </summary>
public interface IDataGridDatabase
{
    /// <summary>
    /// Initialize SQLite database for this grid instance.
    /// MUST be called after grid creation, BEFORE any data operations.
    /// Creates temp file at specified path or in system temp folder if path is null.
    /// </summary>
    /// <param name="databasePath">
    /// Full path to database file (e.g., "C:\Temp\MyApp\grid_abc123.db").
    /// If null, uses Path.GetTempPath() + auto-generated GUID filename.
    /// Parent directory must exist.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    /// <remarks>
    /// Thread-safe. Safe to call multiple times (idempotent if already initialized).
    /// If database file already exists, it will be DELETED and recreated.
    /// </remarks>
    Task<PublicResult> InitializeDatabaseAsync(
        string? databasePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shutdown and cleanup database for this grid instance.
    /// Stops writer thread, flushes pending writes, closes connection, deletes temp file.
    /// Safe to call during application shutdown or grid disposal.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    /// <remarks>
    /// Thread-safe. Safe to call multiple times (idempotent).
    /// If database file cannot be deleted (locked by another process), logs warning but returns success.
    /// </remarks>
    Task<PublicResult> ShutdownDatabaseAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current database file path.
    /// Returns null if database not initialized yet.
    /// </summary>
    /// <returns>Full path to database file or null</returns>
    string? GetDatabasePath();

    /// <summary>
    /// Get database statistics (file size, row counts, timestamps).
    /// Useful for monitoring and diagnostics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database statistics</returns>
    /// <exception cref="InvalidOperationException">If database not initialized</exception>
    Task<PublicDatabaseStatistics> GetDatabaseStatisticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete ALL database files (*.db) in specified directory.
    /// USE WITH CAUTION - deletes ALL .db files in directory!
    /// Intended for cleanup on application startup to remove orphaned databases from previous sessions.
    /// </summary>
    /// <param name="directoryPath">Directory to scan for .db files</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with count of deleted files</returns>
    /// <remarks>
    /// Skips files that are locked (in use by another process).
    /// Logs warning for each file that could not be deleted.
    /// Returns success even if some files could not be deleted.
    /// </remarks>
    Task<PublicResult<int>> DeleteAllDatabasesInPathAsync(
        string directoryPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compact (VACUUM) database to reduce file size.
    /// Rebuilds database file, reclaiming unused space.
    /// Can be slow for large databases (10M+ rows).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    /// <remarks>
    /// VACUUM requires exclusive lock - will block all other operations.
    /// Use sparingly, typically only when database has grown significantly after many deletes.
    /// </remarks>
    Task<PublicResult> CompactDatabaseAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Database statistics model
/// </summary>
public record PublicDatabaseStatistics
{
    /// <summary>Full path to database file</summary>
    public string? FilePath { get; init; }

    /// <summary>File size in bytes</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>File size formatted (e.g., "2.5 MB")</summary>
    public string FileSizeFormatted => FormatFileSize(FileSizeBytes);

    /// <summary>Total row count (including soft-deleted rows)</summary>
    public long TotalRowCount { get; init; }

    /// <summary>Active row count (WHERE __isDeleted = 0)</summary>
    public long ActiveRowCount { get; init; }

    /// <summary>Deleted row count (WHERE __isDeleted = 1)</summary>
    public long DeletedRowCount { get; init; }

    /// <summary>Database creation timestamp</summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>Last modification timestamp</summary>
    public DateTime? LastModifiedAt { get; init; }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F2} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
```

---

## 🔨 IMPLEMENTAČNÉ DETAILY

### 1. DatabaseLifecycleManager (Internal Service)

```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Services;

/// <summary>
/// Internal service for database lifecycle management.
/// Handles initialization, shutdown, and cleanup operations.
/// </summary>
internal sealed class DatabaseLifecycleManager : IDatabaseLifecycleManager, IAsyncDisposable
{
    private readonly ILogger<DatabaseLifecycleManager> _logger;
    private readonly AdvancedDataGridOptions _options;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized = false;
    private bool _isDisposed = false;

    private SqliteConnection? _connection;
    private string? _databasePath;
    private DateTime? _createdAt;

    public DatabaseLifecycleManager(
        ILogger<DatabaseLifecycleManager> logger,
        AdvancedDataGridOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<Result> InitializeDatabaseAsync(
        string? databasePath,
        CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                _logger.LogWarning("Database already initialized at {Path} - skipping", _databasePath);
                return Result.Success();
            }

            // Determine database path
            _databasePath = databasePath ?? GenerateDefaultDatabasePath();

            _logger.LogInformation("Initializing database at {Path}", _databasePath);

            // Ensure parent directory exists
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created directory {Directory}", directory);
            }

            // Delete existing file if present
            if (File.Exists(_databasePath))
            {
                _logger.LogWarning("Database file already exists - deleting and recreating");
                File.Delete(_databasePath);
            }

            // Create SQLite connection
            var connectionString = $"Data Source={_databasePath}";
            _connection = new SqliteConnection(connectionString);
            await _connection.OpenAsync(cancellationToken);

            // Apply PRAGMA settings
            await ApplyPragmaSettingsAsync(cancellationToken);

            // Create schema (tables, indexes, FTS)
            await CreateSchemaAsync(cancellationToken);

            _createdAt = DateTime.UtcNow;
            _isInitialized = true;

            _logger.LogInformation("Database initialized successfully at {Path}", _databasePath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed: {Message}", ex.Message);
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
            }

            // Delete temp file
            if (!string.IsNullOrEmpty(_databasePath) && File.Exists(_databasePath))
            {
                try
                {
                    File.Delete(_databasePath);
                    _logger.LogInformation("Deleted database file {Path}", _databasePath);
                }
                catch (IOException ex)
                {
                    // File might be locked - log warning but don't fail
                    _logger.LogWarning(ex, "Could not delete database file {Path} - file may be locked", _databasePath);
                }
            }

            _isInitialized = false;
            _databasePath = null;
            _createdAt = null;

            _logger.LogInformation("Database shutdown completed");
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

    public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _connection == null)
            throw new InvalidOperationException("Database not initialized");

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

    private string GenerateDefaultDatabasePath()
    {
        var tempPath = Path.GetTempPath();
        var appFolder = Path.Combine(tempPath, "AdvancedDataGrid");

        if (!Directory.Exists(appFolder))
            Directory.CreateDirectory(appFolder);

        var fileName = $"grid_{Guid.NewGuid():N}.db";
        return Path.Combine(appFolder, fileName);
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

        _logger.LogInformation("PRAGMA settings applied: WAL, NORMAL sync, 20MB cache, 256MB mmap");
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

        // Indexes
        cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_createdAt ON grid_rows(__createdAt);";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_isDeleted ON grid_rows(__isDeleted);";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // FTS5 virtual table
        cmd.CommandText = @"
            CREATE VIRTUAL TABLE IF NOT EXISTS grid_rows_fts USING fts5(
                __rowId UNINDEXED,
                data,
                content='grid_rows',
                content_rowid='rowid'
            );";
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        // FTS triggers
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
    }
}
```

---

## 📊 USAGE EXAMPLES

### Demo aplikácia - Complete lifecycle

```csharp
// App startup cleanup
public async Task Application_Startup()
{
    var tempPath = Path.Combine(Path.GetTempPath(), "AdvancedDataGrid");

    // Cleanup old databases from previous sessions
    var grid = AdvancedDataGridBuilder.CreateBuilder().Build();  // Temporary instance for cleanup
    var cleanupResult = await grid.Database.DeleteAllDatabasesInPathAsync(tempPath);

    if (cleanupResult.IsSuccess)
    {
        Console.WriteLine($"Cleaned up {cleanupResult.Value} old database files");
    }

    await grid.DisposeAsync();
}

// Create grid instance
public async Task<IAdvancedDataGridFacade> CreateGridInstance()
{
    var grid = AdvancedDataGridBuilder
        .CreateBuilder()
        .WithOperationMode(PublicDataGridOperationMode.Interactive)
        .WithDispatcherQueue(DispatcherQueue.GetForCurrentThread())
        .Build();

    // CRITICAL: Initialize database before any data operations
    var dbPath = Path.Combine(
        Path.GetTempPath(),
        "AdvancedDataGrid",
        $"grid_instance_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.db"
    );

    var initResult = await grid.Database.InitializeDatabaseAsync(dbPath);

    if (!initResult.IsSuccess)
    {
        throw new InvalidOperationException($"Database init failed: {initResult.ErrorMessage}");
    }

    Console.WriteLine($"Database initialized at: {grid.Database.GetDatabasePath()}");

    return grid;
}

// Dispose grid instance
public async Task DisposeGridInstance(IAdvancedDataGridFacade grid)
{
    // Optional: Get stats before shutdown
    var stats = await grid.Database.GetDatabaseStatisticsAsync();
    Console.WriteLine($"Database stats before shutdown:");
    Console.WriteLine($"  File size: {stats.FileSizeFormatted}");
    Console.WriteLine($"  Active rows: {stats.ActiveRowCount:N0}");
    Console.WriteLine($"  Deleted rows: {stats.DeletedRowCount:N0}");

    // Shutdown (deletes temp file)
    var shutdownResult = await grid.Database.ShutdownDatabaseAsync();

    if (!shutdownResult.IsSuccess)
    {
        Console.WriteLine($"Warning: Database shutdown had issues: {shutdownResult.ErrorMessage}");
    }

    // Dispose facade
    await grid.DisposeAsync();

    Console.WriteLine("Grid instance disposed");
}
```

---

Pokračujem s ďalším dokumentom...

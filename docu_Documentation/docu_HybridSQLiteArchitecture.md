# HYBRID SQLITE ARCHITEKTÚRA - Špecifikácia implementácie

## 📋 METADATA

**Dátum vytvorenia:** 19.10.2025
**Verzia:** 1.0
**Jazyk kódu:** English
**Jazyk dokumentácie:** Slovenčina
**Komponent:** AdvancedDataGrid
**Architektúra:** Hybrid SQLite Model

---

## 🎯 ÚVOD A KONTEXTOVÝ ZÁKLAD

### Aktuálny stav komponentu

AdvancedDataGrid je sofistikovaný WinUI3 komponent pre prácu s tabuľkovými dátami s nasledujúcimi charakteristikami:

#### Existujúce vlastnosti
- **In-memory data storage** cez `InMemoryRowStore` (ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>)
- **ULID-based row identification** - časovo sortovateľné unikátne ID
- **Tri operačné módy:**
  - Interactive Mode: Automatický UI update
  - Headless + Manual UI Update: Manuálny refresh cez `RefreshUIAsync()`
  - Pure Headless: Bez UI, len dátové operácie
- **Real-time per-cell validation** s debounce (100-300ms)
- **Batch validation** pre veľké operácie (import, export)
- **Smart operations** (SmartDeleteRow, AutoExpand)
- **Virtualizácia UI** (pagination, max 1000 rows per page)
- **Filter/Sort/Search funkcionality** (čiastočne implementované)

#### Limitácie aktuálneho riešenia
- **Pamäťová náročnosť:** Všetky dáta sú v RAM (ConcurrentDictionary)
- **Škálovateľnosť:** Pri 10M+ riadkoch hrozí OutOfMemoryException
- **Batch validácia:** Nad 1M riadkami je pomalá (nie optimalizovaná pre SQL dotazy)
- **Triedenie/Filtrovanie:** LINQ nad 10M objekt collection je neefektívne
- **Full-text search:** Nad miliónmi riadkov je LINQ Contains/Regex pomalé

### Prečo Hybrid SQLite?

Na základe analýzy požiadaviek (100k-10M+ riadkov) je **hybrid prístup** optimálne riešenie:

#### Výhody hybrid modelu
✅ **Realtime operations in-memory** - Per-cell edit, validation, UI feedback (milisekundy)
✅ **Batch operations in SQLite** - Validation, import/export, FTS, aggregácie (sekundy)
✅ **Škálovateľnosť** - Viewport (1000 rows) in-memory, zvyšok v SQLite
✅ **SQL power** - Indexy, JOIN, agregácie, Full-Text Search (FTS5)
✅ **Jednoduché API** - Microsoft.Data.Sqlite v .NET
✅ **Nízka režia** - File-based DB, žiadny server proces
✅ **Crash safety** - WAL mode, ACID transakcie

#### Alternatívy a ich problémy
❌ **Full in-memory (súčasné riešenie):** OOM pri 10M+ rows
❌ **Full SQLite:** Per-cell edits by mali vysokú I/O latency
❌ **Custom paged store:** Veľa dev práce (indexy, B-trees, compaction, recovery)
❌ **LiteDB:** Menej výkonné SQL, horšia podpora FTS

---

## 📐 ARCHITEKTÚRA HYBRID SQLITE MODELU

### Koncepčný diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         UI LAYER (WinUI3)                                 │
│  AdvancedDataGridControl → DataGridViewModel → BulkObservableCollection  │
│  (max 1000 rows visible, viewport rendering)                             │
└─────────────────────────────┬───────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────┐   ┌──────────────────┐   ┌──────────────────┐
│  Interactive  │   │ Headless+Manual  │   │  Pure Headless   │
│     Mode      │   │    UI Update     │   │      Mode        │
│ (Auto refresh)│   │ (Manual refresh) │   │  (No UI at all)  │
└───────┬───────┘   └────────┬─────────┘   └────────┬─────────┘
        │                    │                      │
        └────────────────────┼──────────────────────┘
                             │
┌────────────────────────────▼─────────────────────────────────────────────┐
│                     FACADE API (Entry Point)                              │
│  IAdvancedDataGridFacade                                                  │
│  ├─ Rows.AddRowAsync() / AddRowWithDialogAsync()                         │
│  ├─ Filtering.ApplyFilterAsync() / ClearFiltersAsync()                   │
│  ├─ Sorting.SortByColumnAsync() / MultiSortAsync()                       │
│  ├─ Search.SearchAsync() / NavigateNextAsync()                           │
│  ├─ Validation.ValidateAllAsync() / DeleteByValidationAsync()            │
│  ├─ IO.ImportAsync() / ExportAsync()                                     │
│  └─ Database.InitializeDatabaseAsync() / ShutdownDatabaseAsync()         │
└────────────────────────────┬─────────────────────────────────────────────┘
                             │
┌────────────────────────────▼─────────────────────────────────────────────┐
│                    HYBRID ROW STORE LAYER                                 │
│  HybridRowStore (NEW - replaces InMemoryRowStore)                        │
│                                                                           │
│  ┌─────────────────────────┐    ┌──────────────────────────────────┐   │
│  │   IN-MEMORY VIEWPORT    │    │      SQLITE PERSISTENCE          │   │
│  │  (Current page: 1000)   │    │  (Full dataset: 10M+ rows)       │   │
│  │                          │    │                                  │   │
│  │ • ViewModel rows         │◄───┤ • Temp file DB per instance    │   │
│  │ • Active edits cache     │    │ • WAL journal mode              │   │
│  │ • Validation results     │───►│ • Prepared statements           │   │
│  │ • Search highlights      │    │ • Batch inserts                 │   │
│  │                          │    │ • Indexed columns               │   │
│  │ Debounce (300ms)         │    │ • FTS5 full-text search         │   │
│  │ ↓                        │    │                                  │   │
│  │ Flush to SQLite          │───►│ Writer Queue (single thread)    │   │
│  └─────────────────────────┘    └──────────────────────────────────┘   │
└───────────────────────────────────────────────────────────────────────────┘
```

### Kľúčové komponenty

#### 1. HybridRowStore (NEW)
Nahradí `InMemoryRowStore`, implementuje `IRowStore` interface.

**Zodpovednosti:**
- Správa in-memory viewport (current page, max 1000 rows)
- Lazy loading from SQLite database
- Write-back cache (debounced flush po 300-500ms)
- Pagination support (`GetPagedRowsAsync`)
- Filter/Sort support (SQL WHERE/ORDER BY)
- Full-text search support (FTS5)

**Kľúčové vlastnosti:**
```csharp
internal sealed class HybridRowStore : IRowStore, IAsyncDisposable
{
    // In-memory viewport cache
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>> _viewportCache;

    // SQLite connection (per-instance temp file DB)
    private SqliteConnection? _sqliteConnection;
    private readonly string _databaseFilePath;

    // Writer queue (single thread for safe writes)
    private readonly Channel<WriteOperation> _writeQueue;
    private Task? _writerTask;

    // Pagination state
    private int _currentPage = 1;
    private int _pageSize = 1000;

    // Filter/Sort state
    private string? _activeFilterSql;  // WHERE clause
    private string? _activeSortSql;    // ORDER BY clause
}
```

#### 2. SQLite Database Schema

**Hlavná tabuľka:**
```sql
CREATE TABLE IF NOT EXISTS grid_rows (
    __rowId TEXT PRIMARY KEY,          -- ULID (lexicographically sortable)
    __createdAt INTEGER NOT NULL,      -- Unix timestamp (ms)
    __modifiedAt INTEGER NOT NULL,     -- Unix timestamp (ms)
    __isDeleted INTEGER DEFAULT 0,     -- Soft delete flag
    __validationState TEXT,            -- JSON: {isValid, errors: [...]}
    data TEXT NOT NULL                 -- JSON: všetky column values
);

-- Performance indexes
CREATE INDEX IF NOT EXISTS idx_rowId ON grid_rows(__rowId);
CREATE INDEX IF NOT EXISTS idx_createdAt ON grid_rows(__createdAt);
CREATE INDEX IF NOT EXISTS idx_isDeleted ON grid_rows(__isDeleted);

-- Full-text search (FTS5)
CREATE VIRTUAL TABLE IF NOT EXISTS grid_rows_fts USING fts5(
    __rowId UNINDEXED,
    data,                              -- Fulltext search nad JSON data
    content='grid_rows',
    content_rowid='rowid'
);

-- FTS5 trigger (auto-update on INSERT/UPDATE)
CREATE TRIGGER IF NOT EXISTS grid_rows_ai AFTER INSERT ON grid_rows BEGIN
  INSERT INTO grid_rows_fts(rowid, __rowId, data)
  VALUES (new.rowid, new.__rowId, new.data);
END;

CREATE TRIGGER IF NOT EXISTS grid_rows_au AFTER UPDATE ON grid_rows BEGIN
  UPDATE grid_rows_fts SET data = new.data WHERE rowid = new.rowid;
END;

CREATE TRIGGER IF NOT EXISTS grid_rows_ad AFTER DELETE ON grid_rows BEGIN
  DELETE FROM grid_rows_fts WHERE rowid = old.rowid;
END;
```

**Dynamické column-specific indexes (optional):**
```sql
-- Vytvorené len pre často filtrované/sortované stĺpce
CREATE INDEX IF NOT EXISTS idx_{columnName} ON grid_rows(
    json_extract(data, '$.{columnName}')
);
```

#### 3. Database Lifecycle Management

**Per-instance temp database:**
- Každá inštancia komponentu má vlastný temp SQLite file
- Umiestnenie: user-specified path OR `Path.GetTempPath()`
- Automatický cleanup pri `Shutdown()` alebo aplikácie exit

**API metódy:**
```csharp
// IDataGridDatabase (Public API - NEW)
public interface IDataGridDatabase
{
    /// <summary>
    /// Initialize SQLite database for this grid instance.
    /// Creates temp file at specified path or in system temp folder.
    /// MUST be called after grid creation, before any data operations.
    /// </summary>
    Task<PublicResult> InitializeDatabaseAsync(
        string? databasePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shutdown and cleanup database for this grid instance.
    /// Deletes temp database file if it exists and is accessible.
    /// Safe to call multiple times (idempotent).
    /// </summary>
    Task<PublicResult> ShutdownDatabaseAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current database file path.
    /// Returns null if database not initialized.
    /// </summary>
    string? GetDatabasePath();

    /// <summary>
    /// Get database statistics (file size, row count, etc.).
    /// </summary>
    Task<PublicDatabaseStatistics> GetDatabaseStatisticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete ALL database files in specified directory.
    /// USE WITH CAUTION - deletes all *.db files!
    /// Intended for cleanup on app startup.
    /// </summary>
    Task<PublicResult<int>> DeleteAllDatabasesInPathAsync(
        string directoryPath,
        CancellationToken cancellationToken = default);
}

public record PublicDatabaseStatistics
{
    public string? FilePath { get; init; }
    public long FileSizeBytes { get; init; }
    public long TotalRowCount { get; init; }
    public long ActiveRowCount { get; init; }  // WHERE __isDeleted = 0
    public long DeletedRowCount { get; init; } // WHERE __isDeleted = 1
    public DateTime? CreatedAt { get; init; }
    public DateTime? LastModifiedAt { get; init; }
}
```

**Lifecycle flow:**
```csharp
// Demo aplikácia - App startup
public async Task OnAppStartup()
{
    var tempPath = Path.Combine(Path.GetTempPath(), "AdvancedDataGrid");

    // CRITICAL: Delete old databases from previous sessions
    await DatabaseCleanupService.DeleteAllDatabasesInPathAsync(tempPath);

    // ... create grid instances ...
}

// Demo aplikácia - Grid initialization
public async Task CreateGridInstance()
{
    var grid = AdvancedDataGridBuilder
        .CreateBuilder()
        .WithOperationMode(PublicDataGridOperationMode.Interactive)
        .WithDispatcherQueue(DispatcherQueue.GetForCurrentThread())
        .Build();

    // CRITICAL: Initialize database BEFORE any data operations
    var dbPath = Path.Combine(Path.GetTempPath(), "AdvancedDataGrid", $"grid_{Guid.NewGuid()}.db");
    var result = await grid.Database.InitializeDatabaseAsync(dbPath);

    if (!result.IsSuccess)
    {
        throw new InvalidOperationException($"Database init failed: {result.ErrorMessage}");
    }

    // Now ready for data operations
    await grid.IO.ImportFromDataTableAsync(myDataTable);
}

// Grid instance shutdown
public async Task DisposeGridInstance(IAdvancedDataGridFacade grid)
{
    // Shutdown deletes temp DB file
    await grid.Database.ShutdownDatabaseAsync();

    await grid.DisposeAsync();
}
```

---

## 🔧 IMPLEMENTAČNÉ DETAILY

### 1. Writer Queue Pattern (Thread-safe zápisy)

**Problém:**
SQLite file-based DB vyžaduje single writer. Concurrent zápisy by spôsobili "database is locked" errors.

**Riešenie:**
Dedicated writer thread konzumuje write operations z thread-safe queue (Channel<T>).

```csharp
// Write operation types
internal abstract record WriteOperation;
internal record InsertRowOp(IReadOnlyDictionary<string, object?> Row) : WriteOperation;
internal record UpdateRowOp(string RowId, IReadOnlyDictionary<string, object?> Row) : WriteOperation;
internal record DeleteRowOp(string RowId) : WriteOperation;
internal record BulkInsertOp(IEnumerable<IReadOnlyDictionary<string, object?>> Rows) : WriteOperation;
internal record ValidationUpdateOp(string RowId, ValidationError[] Errors) : WriteOperation;

// Writer queue
private readonly Channel<WriteOperation> _writeQueue = Channel.CreateUnbounded<WriteOperation>();

// Writer thread (started in InitializeDatabaseAsync)
private async Task WriterTaskAsync(CancellationToken cancellationToken)
{
    await foreach (var op in _writeQueue.Reader.ReadAllAsync(cancellationToken))
    {
        try
        {
            switch (op)
            {
                case InsertRowOp insert:
                    await ExecuteInsertRowAsync(insert.Row, cancellationToken);
                    break;

                case UpdateRowOp update:
                    await ExecuteUpdateRowAsync(update.RowId, update.Row, cancellationToken);
                    break;

                case BulkInsertOp bulk:
                    await ExecuteBulkInsertAsync(bulk.Rows, cancellationToken);
                    break;

                // ... other cases ...
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Writer queue operation failed: {OpType}", op.GetType().Name);
        }
    }
}

// Public API - enqueue write
public async Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken)
{
    // 1. Add to in-memory viewport cache (immediate UI feedback)
    var rowId = Ulid.NewUlid().ToString();
    var rowWithId = new Dictionary<string, object?>(rowData) { ["__rowId"] = rowId };
    _viewportCache[rowId] = rowWithId;

    // 2. Enqueue write to SQLite (async, non-blocking)
    await _writeQueue.Writer.WriteAsync(new InsertRowOp(rowWithId), cancellationToken);

    // 3. Return immediately (UI not blocked)
    return _viewportCache.Count - 1;
}
```

### 2. Bulk Insert Optimization

```csharp
private async Task ExecuteBulkInsertAsync(
    IEnumerable<IReadOnlyDictionary<string, object?>> rows,
    CancellationToken cancellationToken)
{
    var stopwatch = Stopwatch.StartNew();

    using var tx = _sqliteConnection!.BeginTransaction();
    using var cmd = _sqliteConnection.CreateCommand();

    cmd.CommandText = @"
        INSERT INTO grid_rows (__rowId, __createdAt, __modifiedAt, data)
        VALUES ($rowId, $created, $modified, $data)";

    cmd.Parameters.Add(new SqliteParameter("$rowId", DbType.String));
    cmd.Parameters.Add(new SqliteParameter("$created", DbType.Int64));
    cmd.Parameters.Add(new SqliteParameter("$modified", DbType.Int64));
    cmd.Parameters.Add(new SqliteParameter("$data", DbType.String));

    int insertedCount = 0;

    foreach (var row in rows)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rowId = row.TryGetValue("__rowId", out var id)
            ? id?.ToString() ?? Ulid.NewUlid().ToString()
            : Ulid.NewUlid().ToString();

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var dataJson = System.Text.Json.JsonSerializer.Serialize(row);

        cmd.Parameters["$rowId"].Value = rowId;
        cmd.Parameters["$created"].Value = now;
        cmd.Parameters["$modified"].Value = now;
        cmd.Parameters["$data"].Value = dataJson;

        cmd.ExecuteNonQuery();
        insertedCount++;
    }

    await tx.CommitAsync(cancellationToken);

    stopwatch.Stop();

    _logger.LogInformation("Bulk insert completed: {Count} rows in {Duration}ms ({Rate} rows/s)",
        insertedCount, stopwatch.ElapsedMilliseconds,
        (int)(insertedCount / stopwatch.Elapsed.TotalSeconds));
}
```

**Performance target:** 100k rows insert < 5 sekúnd

### 3. Pagination with SQLite

```csharp
public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPagedRowsAsync(
    int pageNumber,
    int pageSize,
    bool onlyFiltered = false,
    CancellationToken cancellationToken = default)
{
    // Build SQL query
    var whereCl clause = "__isDeleted = 0";
    if (onlyFiltered && !string.IsNullOrEmpty(_activeFilterSql))
    {
        whereClause += $" AND ({_activeFilterSql})";
    }

    var orderByClause = !string.IsNullOrEmpty(_activeSortSql)
        ? _activeSortSql
        : "__createdAt ASC";  // Default sort by creation time

    var offset = (pageNumber - 1) * pageSize;

    var sql = $@"
        SELECT __rowId, data
        FROM grid_rows
        WHERE {whereClause}
        ORDER BY {orderByClause}
        LIMIT {pageSize} OFFSET {offset}";

    using var cmd = _sqliteConnection!.CreateCommand();
    cmd.CommandText = sql;

    var results = new List<IReadOnlyDictionary<string, object?>>();

    using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
        var rowId = reader.GetString(0);
        var dataJson = reader.GetString(1);

        var rowData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(dataJson);
        if (rowData != null)
        {
            rowData["__rowId"] = rowId;  // Ensure __rowId is in data
            results.Add(rowData);
        }
    }

    _logger.LogDebug("Retrieved page {Page} with {Count} rows (pageSize={PageSize})",
        pageNumber, results.Count, pageSize);

    return results;
}
```

---

## 📊 MEMORY & PERFORMANCE PROFIL

### Memory footprint comparison

| Dataset Size | In-Memory Only | Hybrid SQLite | Úspora |
|--------------|----------------|---------------|--------|
| 100k rows    | ~50 MB         | ~10 MB + 20 MB file | 40% |
| 1M rows      | ~500 MB        | ~10 MB + 200 MB file | 98% RAM |
| 10M rows     | **OUT OF MEMORY** | ~10 MB + 2 GB file | **Funguje!** |

**Vysvetlenie:**
- In-memory: Všetky riadky v ConcurrentDictionary (C# objects overhead)
- Hybrid: Len viewport (1000 rows) v RAM, zvyšok v SQLite file

### Performance benchmarks (target)

| Operácia | Dataset | Target čas |
|----------|---------|------------|
| Bulk insert (prepared statements) | 100k rows | < 5s |
| Bulk insert | 1M rows | < 30s |
| Page load (SQL LIMIT/OFFSET) | 1000 rows | < 100ms |
| Filter apply (SQL WHERE) | 10M rows | < 3s |
| Sort (SQL ORDER BY) | 10M rows | < 5s |
| Full-text search (FTS5) | 10M rows | < 2s |
| Batch validation (SQL query) | 1M rows | < 10s |

---

Toto je prvá časť dokumentácie. Pokračujem s ďalšími .md súbormi...

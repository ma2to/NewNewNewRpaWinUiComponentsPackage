# ČASŤ 2: STRATEGY PATTERN ARCHITECTURE (PO APLIKOVANÍ PART 1)

**Dátum:** 30.10.2025
**Autor:** Návrh architektúry
**Účel:** Zadanie na refactoring InMemoryRowStore + HybridRowStore na Strategy Pattern architektúru
**Prerekvizita:** ČASŤ 1 (InsertRowsAsync bulk shift) MUSÍ BYŤ IMPLEMENTOVANÁ PRED ZAČATÍM! ⚠️

---

## 📋 OBSAH

1. [Aktuálna architektúra (po PART 1 fix)](#1-aktuálna-architektúra-po-part-1-fix)
2. [Identifikácia duplicitného kódu](#2-identifikácia-duplicitného-kódu)
3. [Navrhovaná architektúra (Strategy Pattern)](#3-navrhovaná-architektúra-strategy-pattern)
4. [Interface definitions](#4-interface-definitions)
5. [InMemoryStorageStrategy implementácia](#5-inmemorystoragestrategy-implementácia)
6. [SqliteStorageStrategy implementácia](#6-sqlitestoragestrategy-implementácia)
7. [UnifiedRowStore implementácia](#7-unifiedrowstore-implementácia)
8. [AdaptiveRowStore refactoring](#8-adaptiverowstore-refactoring)
9. [Porovnanie: PRED vs PO](#9-porovnanie-pred-vs-po)
10. [Implementačný plán](#10-implementačný-plán)

---

## 1. AKTUÁLNA ARCHITEKTÚRA (PO PART 1 FIX)

### 1.1 Súčasný stav

**Po aplikovaní PART 1 (InsertRowsAsync bulk shift):**

```
┌─────────────────────────────────────────────────────────────┐
│ AdaptiveRowStore (wrapper, deleguje na active store)        │
│ - Automatická migrácia InMemory ↔ Hybrid pri 100K rows      │
│ - Transparentné prepínanie bez data loss                    │
└─────────────────────────────────────────────────────────────┘
            │
            ├─→ InMemoryRowStore (< 100K rows)
            │   - ConcurrentDictionary<string, IReadOnlyDict>
            │   - __rowNumber sorting (PRIMARY)
            │   - ✅ InsertRowsAsync: bulk shift (PART 1 fix)
            │   - ✅ InsertRowAtIndexAsync: __rowNumber shift
            │   - ~2300 riadkov kódu
            │
            └─→ HybridRowStore (>= 100K rows)
                - SQLite + WAL + Writer Queue
                - __rowNumber sorting (SQL ORDER BY)
                - ✅ InsertRowsAsync: SQL bulk shift (PART 1 fix)
                - ✅ InsertRowAtIndexAsync: SQL __rowNumber shift
                - ~2348 riadkov kódu
```

**Súbory:**

- `AdvancedWinUiDataGrid/Infrastructure/Persistence/InMemoryRowStore.cs` (~2300 LOC)
- `AdvancedWinUiDataGrid/Infrastructure/Persistence/HybridRowStore.cs` (~2348 LOC)
- `AdvancedWinUiDataGrid/Infrastructure/Persistence/AdaptiveRowStore.cs` (~537 LOC)

**TOTAL:** ~5185 riadkov kódu

---

### 1.2 Funkcionálna parita (po PART 1)

Po aplikovaní PART 1 fix, oba stores majú **100% funkcionálnu paritu:**

| Funkcia                | InMemory                     | Hybrid                       | Parita? |
|------------------------|------------------------------|------------------------------|---------|
| InsertRowAtIndexAsync  | __rowNumber shift (lock)     | __rowNumber shift (SQL)      | ✅ 100% |
| InsertRowsAsync (BULK) | __rowNumber bulk shift (lock)| __rowNumber bulk shift (SQL) | ✅ 100% |
| DeleteRowByIdAsync     | __rowNumber shift DOWN       | SQL __rowNumber shift DOWN   | ✅ 100% |
| SetSortCriteria        | LINQ OrderBy + renumber      | SQL ROW_NUMBER() + UPDATE    | ✅ 100% |
| AddRangeAsync          | Sequential __rowNumber       | SQL bulk INSERT + __rowNumber| ✅ 100% |
| GetSortedRowKeys       | Cache + LINQ OrderBy         | SQL ORDER BY (embedded)      | ✅ 100% |

**Záver:** API je identické, implementácia sa líši (in-memory vs SQL) ✅

---

## 2. IDENTIFIKÁCIA DUPLICITNÉHO KÓDU

### 2.1 Duplicitná business logika

Po analýze kódu, identifikované **~800-1000 riadkov duplicitnej BUSINESS LOGIKY:**

| Kategória                | InMemory (LOC) | Hybrid (LOC) | Duplicita (LOC) | Popis                              |
|--------------------------|----------------|--------------|-----------------|-------------------------------------|
| Insert/Delete shifting   | ~150           | ~160         | ~150            | __rowNumber shift UP/DOWN logika    |
| Sorting + renumbering    | ~100           | ~120         | ~100            | SetSortCriteria __rowNumber renumber|
| __rowNumber management   | ~80            | ~90          | ~80             | Sequential assignment, validation   |
| Filtering logic          | ~120           | ~130         | ~120            | Filter criteria building            |
| Search implementation    | ~100           | ~110         | ~100            | Search logic (in-memory vs FTS5)    |
| Validation cache         | ~150           | ~160         | ~150            | Cache operations, batch writes      |
| CRUD helpers             | ~200           | ~220         | ~200            | GetRowById, UpdateRowById, etc.     |
| **TOTAL**                | **~900**       | **~990**     | **~800-1000**   |                                     |

---

### 2.2 Príklady duplicity

#### Príklad 1: InsertRowAtIndexAsync shifting logic

**InMemoryRowStore.cs (riadky 1444-1511):**

```csharp
public async Task<string> InsertRowAtIndexAsync(int index, ...)
{
    // ✅ BUSINESS LOGIKA: Shift __rowNumber UP by 1
    for (int i = index; i < sortedKeys.Count; i++)
    {
        var rowId = sortedKeys[i];
        var row = _rows[rowId];
        var mutableRow = new Dictionary<string, object?>(row);
        mutableRow["__rowNumber"] = Convert.ToInt32(row["__rowNumber"]) + 1; // ← DUPLICITA
        _rows[rowId] = mutableRow;
    }
    // ✅ BUSINESS LOGIKA: Insert new row with __rowNumber
    var newRowId = Ulid.NewUlid().ToString();
    fullRowData["__rowNumber"] = index + 1; // ← DUPLICITA
}
```

**HybridRowStore.cs (riadky 2237-2280):**

```csharp
public async Task<string> InsertRowAtIndexAsync(int index, ...)
{
    // ✅ BUSINESS LOGIKA: Shift __rowNumber UP by 1 (SQL version)
    cmdShift.CommandText = $@"
        UPDATE grid_rows
        SET data = json_set(data, '$.__rowNumber',
            CAST(json_extract(data, '$.__rowNumber') AS INTEGER) + 1) -- ← DUPLICITA (SQL syntax)
        WHERE __isDeleted = 0
          AND CAST(json_extract(data, '$.__rowNumber') AS INTEGER) >= {targetRowNumber}";

    // ✅ BUSINESS LOGIKA: Insert new row with __rowNumber
    dataDict["__rowNumber"] = index + 1; // ← DUPLICITA
}
```

**DUPLICITA:** Shifting UP by 1, assignment `index + 1` → IDENTICKÁ logika, rozdielna implementácia (lock vs SQL)

---

#### Príklad 2: SetSortCriteria renumbering logic

**InMemoryRowStore.cs (riadky 851-896):**

```csharp
public void SetSortCriteria(string columnName, SortDirection direction)
{
    // ✅ BUSINESS LOGIKA: Sort by column + renumber __rowNumber sequentially
    var sorted = _rows.Values
        .OrderBy(row => row[columnName]) // ← DUPLICITA (LINQ syntax)
        .ToList();

    for (int i = 0; i < sorted.Count; i++)
    {
        var row = sorted[i];
        var mutableRow = new Dictionary<string, object?>(row);
        mutableRow["__rowNumber"] = i + 1; // ← DUPLICITA (sequential renumber)
        _rows[(string)row["__rowId"]!] = mutableRow;
    }
}
```

**HybridRowStore.cs (riadky 674-732):**

```csharp
public void SetSortCriteria(string columnName, SortDirection direction)
{
    // ✅ BUSINESS LOGIKA: Sort by column + renumber __rowNumber sequentially (SQL version)
    cmd.CommandText = $@"
        WITH sorted_rows AS (
            SELECT __rowId,
                   ROW_NUMBER() OVER (ORDER BY json_extract(data, '$.{columnName}') {directionStr}) as new_rn -- ← DUPLICITA (SQL syntax)
            FROM grid_rows
            WHERE __isDeleted = 0
        )
        UPDATE grid_rows
        SET data = json_set(data, '$.__rowNumber', (
            SELECT new_rn FROM sorted_rows WHERE sorted_rows.__rowId = grid_rows.__rowId
        ))
        WHERE __isDeleted = 0";
}
```

**DUPLICITA:** Sequential renumbering po sortovaní → IDENTICKÁ logika, rozdielna implementácia (LINQ vs SQL ROW_NUMBER())

---

### 2.3 Záver

**~800-1000 riadkov duplicitnej BUSINESS LOGIKY:**
- Shifting (__rowNumber UP/DOWN)
- Renumbering (sequential __rowNumber assignment)
- Sorting logika (OrderBy column → renumber)
- Filtering (filter criteria → WHERE clause)
- Validation cache operations

**Rozdielna iba IMPLEMENTÁCIA:**
- InMemory: ConcurrentDictionary, lock, LINQ
- Hybrid: SQLite, SQL queries, Writer Queue

**Príležitosť:** Extrahovať business logiku do zdieľanej triedy, implementáciu do strategies ✅

---

## 3. NAVRHOVANÁ ARCHITEKTÚRA (STRATEGY PATTERN)

### 3.1 High-level design

```
┌───────────────────────────────────────────────────────────────┐
│ UnifiedRowStore (IRowStore implementation)                    │
│ - Zdieľaná business logika (validation, metadata management)  │
│ - Deleguje STORAGE operácie na IStorageStrategy               │
│ - Deleguje VALIDATION storage na IValidationStrategy          │
│ - Žiadna duplicita business logiky                            │
│ - ~500 riadkov kódu                                           │
└───────────────────────────────────────────────────────────────┘
            │
            ├─→ IStorageStrategy (interface)
            │   │
            │   ├─→ InMemoryStorageStrategy (~800 LOC)
            │   │   - ConcurrentDictionary
            │   │   - __rowNumber shift (lock)
            │   │   - ✅ Bulk shift optimalizácia (PART 1)
            │   │   - LINQ OrderBy, filtering
            │   │
            │   └─→ SqliteStorageStrategy (~850 LOC)
            │       - SQLite + WAL + Writer Queue
            │       - __rowNumber shift (SQL)
            │       - ✅ SQL bulk shift optimalizácia (PART 1)
            │       - SQL ORDER BY, FTS5 search
            │
            └─→ IValidationStrategy (interface)
                │
                ├─→ InMemoryValidationStrategy (~200 LOC)
                │   - ConcurrentDictionary validation cache
                │   - In-memory batch writes
                │
                └─→ SqliteValidationStrategy (~250 LOC)
                    - SQLite validation state storage
                    - SQL batch writes

┌───────────────────────────────────────────────────────────────┐
│ AdaptiveRowStore (orchestrator)                               │
│ - Automatická migrácia InMemory ↔ SQLite pri 100K rows        │
│ - Vytvára UnifiedRowStore s príslušnou stratégiou             │
│ - Transparentné prepínanie                                    │
│ - ~100 riadkov kódu (refactored)                              │
└───────────────────────────────────────────────────────────────┘
```

---

### 3.2 Výhody riešenia

| Výhoda                   | Popis                                                                 | Benefit              |
|--------------------------|-----------------------------------------------------------------------|----------------------|
| **Eliminácia duplicity** | ~800-1000 riadkov duplicitnej logiky odstránených                     | ✅ -800 LOC           |
| **Total LOC reduction**  | Z ~5185 LOC na ~2700 LOC                                              | ✅ -2485 LOC (48%)    |
| **Separation of concerns** | Storage logic oddelená od business logiky                           | ✅ Better architecture|
| **Testovateľnosť**       | Izolované strategies → unit testy bez dependencies                    | ✅ Better testing     |
| **Maintainability**      | Zmeny v 1 stratégii namiesto 2 stores                                 | ✅ Easier maintenance |
| **Rozšíriteľnosť**       | Pridať Redis = implementovať interface (nie duplikovať 800 LOC)       | ✅ Future-proof       |
| **Performance preserved**| PART 1 bulk shift optimalizácia zachovaná v strategies                | ✅ No regression      |

---

### 3.3 Filová štruktúra (PO implementácii)

```
Infrastructure/Persistence/
├─ Interfaces/
│  ├─ IRowStore.cs (existujúce - BEZ ZMIEN)
│  ├─ IStorageStrategy.cs (NOVÝ) ⭐
│  └─ IValidationStrategy.cs (NOVÝ) ⭐
│
├─ Strategies/
│  ├─ Storage/
│  │  ├─ InMemoryStorageStrategy.cs (NOVÝ - presun z InMemoryRowStore) ⭐
│  │  └─ SqliteStorageStrategy.cs (NOVÝ - presun z HybridRowStore) ⭐
│  │
│  └─ Validation/
│     ├─ InMemoryValidationStrategy.cs (NOVÝ - presun z InMemoryRowStore) ⭐
│     └─ SqliteValidationStrategy.cs (NOVÝ - presun z HybridRowStore) ⭐
│
├─ UnifiedRowStore.cs (NOVÝ - zdieľaná business logika) ⭐
├─ AdaptiveRowStore.cs (REFACTORED - používa UnifiedRowStore)
│
├─ InMemoryRowStore.cs ([Obsolete] - PART 1 bulk shift applied)
└─ HybridRowStore.cs ([Obsolete] - PART 1 SQL bulk shift applied)
```

---

## 4. INTERFACE DEFINITIONS

### 4.1 IStorageStrategy.cs

**Nový súbor:** `Infrastructure/Persistence/Strategies/IStorageStrategy.cs`

```csharp
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Strategies;

/// <summary>
/// ✅ STORAGE STRATEGY: Interface pre data storage (InMemory vs SQLite).
/// NOW: 100% functional parity + bulk shift optimization (InsertRowsAsync unified in PART 1).
/// SEPARATION: Obsahuje iba STORAGE operácie (CRUD, insert/delete, filter, sort, search).
/// BUSINESS LOGIKA: Je v UnifiedRowStore (validation orchestration, metadata management).
/// </summary>
public interface IStorageStrategy
{
    // ========== METADATA ==========

    /// <summary>
    /// Strategy name for logging/debugging.
    /// </summary>
    string StrategyName { get; }

    // ========== CORE CRUD ==========

    /// <summary>
    /// Get range of rows for pagination/virtualization.
    /// InMemory: LINQ Skip/Take, Hybrid: SQL LIMIT/OFFSET.
    /// </summary>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsRangeAsync(
        long startIndex,
        int count,
        bool onlyFiltered,
        CancellationToken ct);

    /// <summary>
    /// Get total row count (filtered or unfiltered).
    /// </summary>
    Task<long> GetRowCountAsync(bool onlyFiltered, CancellationToken ct);

    /// <summary>
    /// Append rows to end of dataset with sequential __rowNumber.
    /// InMemory: TryAdd to dictionary, Hybrid: SQL bulk INSERT.
    /// </summary>
    Task<int> AddRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct);

    /// <summary>
    /// Get row by stable rowId.
    /// </summary>
    Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(
        string rowId,
        CancellationToken ct);

    /// <summary>
    /// Update row by stable rowId.
    /// </summary>
    Task UpdateRowByIdAsync(
        string rowId,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken ct);

    /// <summary>
    /// Delete row by stable rowId.
    /// InMemory: Remove from dictionary + shift __rowNumber DOWN.
    /// Hybrid: Soft delete (__isDeleted=1) + SQL shift __rowNumber DOWN.
    /// </summary>
    Task DeleteRowByIdAsync(string rowId, CancellationToken ct);

    // ========== INDEX-BASED INSERT OPERATIONS (UNIFIED + OPTIMIZED!) ⭐ ==========

    /// <summary>
    /// ✅ UNIFIED (PART 1): Inserts single row at specified index.
    /// Shifts __rowNumber of rows >= index UP by 1.
    /// IDENTICAL behavior in InMemory and SQLite strategies.
    /// </summary>
    /// <param name="index">0-based index where row will be inserted</param>
    /// <param name="rowData">Row data (WITHOUT __rowId or __rowNumber - will be added)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>RowId of newly inserted row (STABLE identifier)</returns>
    Task<string> InsertRowAtIndexAsync(
        int index,
        IReadOnlyDictionary<string, object?>? rowData,
        CancellationToken ct);

    /// <summary>
    /// ✅ UNIFIED + OPTIMIZED (PART 1 FIX): Inserts multiple rows at specified index.
    /// Uses BULK SHIFT optimization (1 shift for all rows, not N shifts).
    /// IDENTICAL behavior in InMemory and SQLite strategies.
    /// OPTIMIZED for large batches (100-1000+ rows): ~200ms for 1000 rows.
    /// </summary>
    /// <param name="rows">Rows to insert (WITHOUT __rowId or __rowNumber - will be added)</param>
    /// <param name="startIndex">0-based index where first row will be inserted</param>
    /// <param name="ct">Cancellation token</param>
    Task InsertRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex,
        CancellationToken ct);

    // ========== FILTER & SORT ==========

    /// <summary>
    /// Set filter criteria and build filtered view.
    /// InMemory: LINQ Where, Hybrid: SQL WHERE clause.
    /// </summary>
    void SetFilterCriteria(List<FilterCriterion> criteria);

    /// <summary>
    /// Clear filter criteria.
    /// </summary>
    void ClearFilterCriteria();

    /// <summary>
    /// Get current filter criteria.
    /// </summary>
    List<FilterCriterion>? GetFilterCriteria();

    /// <summary>
    /// Set sort criteria and renumber __rowNumber.
    /// InMemory: LINQ OrderBy + renumber loop.
    /// Hybrid: SQL ROW_NUMBER() OVER + UPDATE __rowNumber.
    /// </summary>
    /// <param name="columnName">Column to sort by</param>
    /// <param name="direction">Sort direction (Ascending/Descending/None)</param>
    void SetSortCriteria(string columnName, SortDirection direction);

    /// <summary>
    /// Clear sort criteria (revert to default __rowNumber ordering).
    /// </summary>
    void ClearSortCriteria();

    // ========== SEARCH ==========

    /// <summary>
    /// Full-text search on row data.
    /// InMemory: LINQ Contains/IndexOf search.
    /// Hybrid: SQLite FTS5 MATCH search.
    /// </summary>
    /// <param name="searchText">Text to search for</param>
    /// <param name="targetColumns">Optional: specific columns to search (null = all columns)</param>
    /// <param name="caseSensitive">Whether search should be case-sensitive</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of row IDs that match the search criteria</returns>
    Task<IReadOnlyList<string>> SearchAsync(
        string searchText,
        string[]? targetColumns,
        bool caseSensitive,
        CancellationToken ct);

    // ========== UTILITY ==========

    /// <summary>
    /// Clear all data from storage.
    /// </summary>
    Task ClearAsync(CancellationToken ct);

    /// <summary>
    /// Get rowId by current row index (volatile).
    /// </summary>
    string? GetRowIdByIndex(int index);

    /// <summary>
    /// Get current row index by rowId (volatile).
    /// </summary>
    int? GetRowIndexById(string rowId);
}
```

**Riadky kódu:** ~150 LOC

---

### 4.2 IValidationStrategy.cs

**Nový súbor:** `Infrastructure/Persistence/Strategies/IValidationStrategy.cs`

```csharp
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Strategies;

/// <summary>
/// ✅ VALIDATION STRATEGY: Interface pre validation state storage.
/// SEPARATION: Obsahuje iba VALIDATION STORAGE operácie (write, read, cache).
/// BUSINESS LOGIKA: Validation orchestration je v UnifiedRowStore.
/// </summary>
public interface IValidationStrategy
{
    // ========== METADATA ==========

    /// <summary>
    /// Strategy name for logging/debugging.
    /// </summary>
    string StrategyName { get; }

    // ========== VALIDATION STORAGE ==========

    /// <summary>
    /// Write validation results for rows.
    /// InMemory: Update validation cache dictionary.
    /// Hybrid: Queue validation state write to SQLite.
    /// </summary>
    Task WriteValidationResultsAsync(
        IEnumerable<ValidationError> results,
        CancellationToken ct);

    /// <summary>
    /// Get validation errors for scope (filtered/unfiltered, checked/unchecked).
    /// </summary>
    Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(
        bool onlyFiltered,
        bool onlyChecked,
        CancellationToken ct);

    /// <summary>
    /// Get validation errors for specific row by rowId.
    /// </summary>
    Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(
        string rowId,
        CancellationToken ct);

    /// <summary>
    /// Check if validation state exists for scope.
    /// </summary>
    Task<bool> HasValidationStateAsync(
        bool onlyFiltered,
        bool onlyChecked,
        CancellationToken ct);

    /// <summary>
    /// Clear all validation state.
    /// </summary>
    Task ClearValidationStateAsync(CancellationToken ct);

    // ========== VALIDATION CACHE (SENIOR FIX) ==========

    /// <summary>
    /// Checks if a row has already been validated (cache check).
    /// SENIOR FIX: Prevents ValidateAll infinite loop.
    /// </summary>
    bool IsRowValidationCached(string rowId);

    /// <summary>
    /// Marks a row as validated in the cache.
    /// </summary>
    void MarkRowAsValidated(string rowId);

    /// <summary>
    /// Clears validation cache.
    /// </summary>
    void ClearValidationCache();

    /// <summary>
    /// Batch writes validation results for multiple rows in a single operation.
    /// SENIOR FIX: Prevents infinite validation loop by avoiding multiple DataChanged events.
    /// </summary>
    Task WriteValidationResultsBatchAsync(
        Dictionary<string, ValidationError[]> validationResults,
        CancellationToken ct);
}
```

**Riadky kódu:** ~80 LOC

---

## 5. InMemoryStorageStrategy IMPLEMENTÁCIA

### 5.1 Nový súbor

**Nový súbor:** `Infrastructure/Persistence/Strategies/Storage/InMemoryStorageStrategy.cs`

**Presun kódu z:** `InMemoryRowStore.cs` (~800 riadkov)

---

### 5.2 Implementačný template

```csharp
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Strategies.Storage;

/// <summary>
/// ✅ IN-MEMORY STORAGE STRATEGY (FINAL VERSION)
/// NOW: InsertRowsAsync uses BULK SHIFT optimization (PART 1 fix).
/// EXTRACTED from InMemoryRowStore.cs (data storage logic only).
/// </summary>
public class InMemoryStorageStrategy : IStorageStrategy
{
    // ========== PRIVATE FIELDS (presun z InMemoryRowStore.cs) ==========

    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>> _rows = new();
    private readonly object _modificationLock = new();
    private readonly ILogger? _logger;

    // Cache fields
    private List<string>? _sortedRowKeys;
    private bool _sortedRowKeysInvalid = true;
    private readonly object _orderLock = new();

    // Filter fields
    private List<FilterCriterion>? _filterCriteria;

    // ========== CONSTRUCTOR ==========

    public InMemoryStorageStrategy(ILogger<InMemoryStorageStrategy>? logger)
    {
        _logger = logger;
    }

    // ========== METADATA ==========

    public string StrategyName => "InMemory";

    // ========== INSERT OPERATIONS (PART 1 BULK SHIFT) ==========

    /// <summary>
    /// ✅ UNIFIED (PART 1): Insert single row at index with __rowNumber shift.
    /// PRESUN Z InMemoryRowStore.cs:1444-1511
    /// </summary>
    public async Task<string> InsertRowAtIndexAsync(
        int index,
        IReadOnlyDictionary<string, object?>? rowData,
        CancellationToken ct)
    {
        // PRESUN KÓDU z InMemoryRowStore.cs:1444-1511
        // (kód z PART 1 dokumentácie, sekcia 5.2 analógie)
        return await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                var sortedKeys = GetSortedRowKeys();
                int newRowNumber = index + 1;

                // ✅ SHIFT: Rows with __rowNumber >= newRowNumber UP by 1
                for (int i = index; i < sortedKeys.Count; i++)
                {
                    var rowId = sortedKeys[i];
                    var row = _rows[rowId];
                    var mutableRow = new Dictionary<string, object?>(row);
                    mutableRow["__rowNumber"] = Convert.ToInt32(row["__rowNumber"]) + 1;
                    _rows[rowId] = mutableRow;
                }

                // ✅ INSERT: New row at exact index
                var newRowId = Ulid.NewUlid().ToString();
                var fullRowData = new Dictionary<string, object?>(rowData ?? new Dictionary<string, object?>());
                fullRowData["__rowId"] = newRowId;
                fullRowData["__rowNumber"] = newRowNumber;

                _rows.TryAdd(newRowId, fullRowData);
                InvalidateSortedRowKeysCache();

                _logger?.LogDebug("Inserted row {RowId} at index {Index} (__rowNumber={RowNumber})",
                    newRowId, index, newRowNumber);

                return newRowId;
            }
        }, ct);
    }

    /// <summary>
    /// ✅ UNIFIED + OPTIMIZED (PART 1 FIX): Insert multiple rows at index with BULK SHIFT.
    /// PRESUN Z InMemoryRowStore.cs:533-598 (AFTER PART 1 implementation)
    /// </summary>
    public async Task InsertRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex,
        CancellationToken ct)
    {
        // PRESUN KÓDU z InMemoryRowStore.cs:533-598 (AFTER PART 1)
        // (kód z PART 1 dokumentácie, sekcia 5.2)
        var rowsList = rows.ToList();
        if (rowsList.Count == 0) return;

        await Task.Run(() =>
        {
            lock (_modificationLock)
            {
                // ✅ STEP 1: BULK SHIFT
                int targetRowNumber = startIndex + 1;
                foreach (var kvp in _rows)
                {
                    var row = kvp.Value;
                    if (row.TryGetValue("__rowNumber", out var rnObj) && rnObj != null)
                    {
                        var currentRowNumber = Convert.ToInt32(rnObj);
                        if (currentRowNumber >= targetRowNumber)
                        {
                            var mutableRow = new Dictionary<string, object?>(row);
                            mutableRow["__rowNumber"] = currentRowNumber + rowsList.Count;
                            _rows[kvp.Key] = mutableRow;
                        }
                    }
                }

                // ✅ STEP 2: BULK INSERT
                for (int i = 0; i < rowsList.Count; i++)
                {
                    var rowData = new Dictionary<string, object?>(rowsList[i]);
                    rowData["__rowNumber"] = startIndex + i + 1;

                    if (!rowData.ContainsKey("__rowId") || rowData["__rowId"] == null)
                    {
                        rowData["__rowId"] = Ulid.NewUlid().ToString();
                    }

                    _rows.TryAdd((string)rowData["__rowId"]!, rowData);
                }

                InvalidateSortedRowKeysCache();
            }
        }, ct);

        _logger?.LogInformation("InsertRowsAsync (bulk): Inserted {Count} rows at index {Index}",
            rowsList.Count, startIndex);
    }

    // ========== OTHER CRUD METHODS (presun z InMemoryRowStore.cs) ==========

    public async Task<int> AddRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct)
    {
        // PRESUN KÓDU z InMemoryRowStore.cs:176-234 (AddRangeAsync)
        // ... (append s sequential __rowNumber)
    }

    public async Task DeleteRowByIdAsync(string rowId, CancellationToken ct)
    {
        // PRESUN KÓDU z InMemoryRowStore.cs:1530-1578
        // ... (remove + shift __rowNumber DOWN)
    }

    public async Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(
        string rowId,
        CancellationToken ct)
    {
        // PRESUN KÓDU z InMemoryRowStore.cs (GetRowByIdAsync)
        // ... (TryGetValue)
    }

    public async Task UpdateRowByIdAsync(
        string rowId,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken ct)
    {
        // PRESUN KÓDU z InMemoryRowStore.cs (UpdateRowByIdAsync)
        // ... (update dictionary)
    }

    // ========== FILTER & SORT METHODS (presun z InMemoryRowStore.cs) ==========

    public void SetSortCriteria(string columnName, SortDirection direction)
    {
        // PRESUN KÓDU z InMemoryRowStore.cs:851-896
        // ... (LINQ OrderBy + renumber __rowNumber loop)
    }

    public void SetFilterCriteria(List<FilterCriterion> criteria)
    {
        // PRESUN KÓDU z InMemoryRowStore.cs (SetFilterCriteria)
        // ... (store criteria)
    }

    // ========== SEARCH METHOD (presun z InMemoryRowStore.cs) ==========

    public async Task<IReadOnlyList<string>> SearchAsync(
        string searchText,
        string[]? targetColumns,
        bool caseSensitive,
        CancellationToken ct)
    {
        // PRESUN KÓDU z InMemoryRowStore.cs (SearchAsync)
        // ... (LINQ Where + Contains)
    }

    // ========== HELPER METHODS (presun z InMemoryRowStore.cs) ==========

    private List<string> GetSortedRowKeys()
    {
        // PRESUN KÓDU z InMemoryRowStore.cs:1711-1740
        // ... (cache + OrderBy __rowNumber, ThenBy __rowId)
    }

    private void InvalidateSortedRowKeysCache()
    {
        lock (_orderLock)
        {
            _sortedRowKeysInvalid = true;
        }
    }

    // ... (ostatné helper metódy presun z InMemoryRowStore.cs)
}
```

**Riadky kódu:** ~800 LOC (presun z InMemoryRowStore.cs)

---

### 5.3 Presun kódu - checklist

Presunúť z `InMemoryRowStore.cs` do `InMemoryStorageStrategy.cs`:

- [ ] Private fields: `_rows`, `_modificationLock`, `_sortedRowKeys`, cache fields
- [ ] InsertRowAtIndexAsync (riadky 1444-1511) → už implementované v PART 1
- [ ] InsertRowsAsync (riadky 533-598 AFTER PART 1) → bulk shift version
- [ ] DeleteRowByIdAsync (riadky 1530-1578) → shift DOWN logic
- [ ] AddRangeAsync → AddRowsAsync (riadky 176-234) → append s __rowNumber
- [ ] GetRowByIdAsync, UpdateRowByIdAsync, GetRowAsync, UpdateRowAsync
- [ ] SetSortCriteria (riadky 851-896) → LINQ OrderBy + renumber
- [ ] SetFilterCriteria, ClearFilterCriteria, GetFilterCriteria
- [ ] SearchAsync → LINQ Contains/IndexOf search
- [ ] GetSortedRowKeys (riadky 1711-1740) → cache + OrderBy
- [ ] InvalidateSortedRowKeysCache
- [ ] GetRowIdByIndex, GetRowIndexById
- [ ] ClearAsync

**NEPRESUŇ:**
- Validation cache metódy (idú do InMemoryValidationStrategy)
- WriteValidationResultsAsync (ide do InMemoryValidationStrategy)
- IRowStore interface metódy (zostávajú v UnifiedRowStore ako delegácia)

---

## 6. SqliteStorageStrategy IMPLEMENTÁCIA

### 6.1 Nový súbor

**Nový súbor:** `Infrastructure/Persistence/Strategies/Storage/SqliteStorageStrategy.cs`

**Presun kódu z:** `HybridRowStore.cs` (~850 riadkov)

---

### 6.2 Implementačný template

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Database.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Strategies.Storage;

/// <summary>
/// ✅ SQLITE STORAGE STRATEGY (FINAL VERSION)
/// NOW: InsertRowsAsync uses BULK SHIFT optimization (PART 1 fix).
/// EXTRACTED from HybridRowStore.cs (data storage logic only).
/// </summary>
public class SqliteStorageStrategy : IStorageStrategy
{
    // ========== PRIVATE FIELDS (presun z HybridRowStore.cs) ==========

    private readonly IDatabaseLifecycleManager _databaseLifecycleManager;
    private readonly ILogger? _logger;

    // Writer Queue fields (presun z HybridRowStore.cs)
    private readonly Channel<WriteOperation> _writerQueue;
    private readonly Task _writerTask;
    private readonly CancellationTokenSource _disposeCts = new();

    // Filter fields
    private string? _activeFilterSql;

    // Sort fields
    private string? _activeSortSql;

    // ========== CONSTRUCTOR ==========

    public SqliteStorageStrategy(
        ILogger<SqliteStorageStrategy>? logger,
        IDatabaseLifecycleManager databaseLifecycleManager)
    {
        _logger = logger;
        _databaseLifecycleManager = databaseLifecycleManager ?? throw new ArgumentNullException(nameof(databaseLifecycleManager));

        // Initialize Writer Queue (presun z HybridRowStore.cs:85-92)
        _writerQueue = Channel.CreateBounded<WriteOperation>(new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _writerTask = Task.Run(WriterBackgroundTaskAsync, _disposeCts.Token);
    }

    // ========== METADATA ==========

    public string StrategyName => "SQLite";

    // ========== INSERT OPERATIONS (PART 1 SQL BULK SHIFT) ==========

    /// <summary>
    /// ✅ UNIFIED (PART 1): Insert single row at index with SQL __rowNumber shift.
    /// PRESUN Z HybridRowStore.cs:2237-2280
    /// </summary>
    public async Task<string> InsertRowAtIndexAsync(
        int index,
        IReadOnlyDictionary<string, object?>? rowData,
        CancellationToken ct)
    {
        // PRESUN KÓDU z HybridRowStore.cs:2237-2280
        var connection = _databaseLifecycleManager.GetConnection();
        int targetRowNumber = index + 1;

        // ✅ SHIFT: SQL UPDATE to shift __rowNumber UP
        using (var cmdShift = connection.CreateCommand())
        {
            cmdShift.CommandText = $@"
                UPDATE grid_rows
                SET data = json_set(data, '$.__rowNumber',
                    CAST(json_extract(data, '$.__rowNumber') AS INTEGER) + 1)
                WHERE __isDeleted = 0
                  AND CAST(json_extract(data, '$.__rowNumber') AS INTEGER) >= {targetRowNumber}";

            await cmdShift.ExecuteNonQueryAsync(ct);
        }

        // ✅ INSERT: New row with __rowNumber = targetRowNumber
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
            ValidationStateJson = null
        };

        await QueueWriteOperationAsync(insertOp, ct);
        await FlushWriterQueueAsync(ct);

        _logger?.LogDebug("Inserted row {RowId} at index {Index} (__rowNumber={RowNumber})",
            newRowId, index, targetRowNumber);

        return newRowId;
    }

    /// <summary>
    /// ✅ UNIFIED + OPTIMIZED (PART 1 FIX): Insert multiple rows at index with BULK SHIFT.
    /// PRESUN Z HybridRowStore.cs:1216-1223 (AFTER PART 1 implementation)
    /// </summary>
    public async Task InsertRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex,
        CancellationToken ct)
    {
        // PRESUN KÓDU z HybridRowStore.cs:1216-1223 (AFTER PART 1)
        // (kód z PART 1 dokumentácie, sekcia 6.2)
        var rowsList = rows.ToList();
        if (rowsList.Count == 0) return;

        var connection = _databaseLifecycleManager.GetConnection();

        // ✅ STEP 1: BULK SHIFT (1 SQL UPDATE)
        int targetRowNumber = startIndex + 1;
        using (var cmdShift = connection.CreateCommand())
        {
            cmdShift.CommandText = $@"
                UPDATE grid_rows
                SET data = json_set(data, '$.__rowNumber',
                    CAST(json_extract(data, '$.__rowNumber') AS INTEGER) + {rowsList.Count})
                WHERE __isDeleted = 0
                  AND CAST(json_extract(data, '$.__rowNumber') AS INTEGER) >= {targetRowNumber}";

            await cmdShift.ExecuteNonQueryAsync(ct);
        }

        // ✅ STEP 2: BULK INSERT
        var insertData = new List<RowInsertData>();
        var timestamp = GetUnixTimestampMs();

        for (int i = 0; i < rowsList.Count; i++)
        {
            var rowData = new Dictionary<string, object?>(rowsList[i]);
            rowData["__rowNumber"] = startIndex + i + 1;

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

        var bulkInsertOp = new BulkInsertWriteOp { Rows = insertData, OperationId = GenerateRowId() };
        await QueueWriteOperationAsync(bulkInsertOp, ct);
        await FlushWriterQueueAsync(ct);

        _logger?.LogInformation("InsertRowsAsync (bulk): Inserted {Count} rows at index {Index}",
            rowsList.Count, startIndex);
    }

    // ========== OTHER CRUD METHODS (presun z HybridRowStore.cs) ==========

    public async Task<int> AddRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct)
    {
        // PRESUN KÓDU z HybridRowStore.cs:1103-1156 (AppendRowsAsync)
        // ... (SQL bulk INSERT s sequential __rowNumber)
    }

    public async Task DeleteRowByIdAsync(string rowId, CancellationToken ct)
    {
        // PRESUN KÓDU z HybridRowStore.cs:2295-2344
        // ... (SQL soft delete + shift __rowNumber DOWN)
    }

    public async Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(
        string rowId,
        CancellationToken ct)
    {
        // PRESUN KÓDU z HybridRowStore.cs (GetRowByIdAsync)
        // ... (SQL SELECT)
    }

    public async Task UpdateRowByIdAsync(
        string rowId,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken ct)
    {
        // PRESUN KÓDU z HybridRowStore.cs (UpdateRowByIdAsync)
        // ... (SQL UPDATE via Writer Queue)
    }

    // ========== FILTER & SORT METHODS (presun z HybridRowStore.cs) ==========

    public void SetSortCriteria(string columnName, SortDirection direction)
    {
        // PRESUN KÓDU z HybridRowStore.cs:674-732
        // ... (SQL ROW_NUMBER() OVER + UPDATE __rowNumber)
    }

    public void SetFilterCriteria(List<FilterCriterion> criteria)
    {
        // PRESUN KÓDU z HybridRowStore.cs (SetFilterCriteria)
        // ... (build SQL WHERE clause)
    }

    // ========== SEARCH METHOD (presun z HybridRowStore.cs) ==========

    public async Task<IReadOnlyList<string>> SearchAsync(
        string searchText,
        string[]? targetColumns,
        bool caseSensitive,
        CancellationToken ct)
    {
        // PRESUN KÓDU z HybridRowStore.cs:752-843 (SearchAsync)
        // ... (SQLite FTS5 MATCH)
    }

    // ========== WRITER QUEUE METHODS (presun z HybridRowStore.cs) ==========

    private async Task WriterBackgroundTaskAsync()
    {
        // PRESUN KÓDU z HybridRowStore.cs:123-152 (WriterBackgroundTaskAsync)
        // ... (process write operations from queue)
    }

    private async Task QueueWriteOperationAsync(WriteOperation operation, CancellationToken ct)
    {
        // PRESUN KÓDU z HybridRowStore.cs:473-479
        // ... (queue write operation)
    }

    private async Task FlushWriterQueueAsync(CancellationToken ct)
    {
        // PRESUN KÓDU z HybridRowStore.cs:486-506
        // ... (wait for queue to flush)
    }

    // ========== HELPER METHODS (presun z HybridRowStore.cs) ==========

    private string SerializeRowData(IReadOnlyDictionary<string, object?> rowData)
    {
        // PRESUN KÓDU z HybridRowStore.cs:421-429
        // ... (JSON serialize)
    }

    private IReadOnlyDictionary<string, object?> DeserializeRowData(string rowId, string dataJson)
    {
        // PRESUN KÓDU z HybridRowStore.cs:434-442
        // ... (JSON deserialize)
    }

    private string GenerateRowId() => Ulid.NewUlid().ToString();
    private long GetUnixTimestampMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // ... (ostatné helper metódy presun z HybridRowStore.cs)
}
```

**Riadky kódu:** ~850 LOC (presun z HybridRowStore.cs)

---

### 6.3 Presun kódu - checklist

Presunúť z `HybridRowStore.cs` do `SqliteStorageStrategy.cs`:

- [ ] Private fields: `_databaseLifecycleManager`, Writer Queue fields, cache fields
- [ ] Constructor: Initialize Writer Queue (riadky 71-95)
- [ ] InsertRowAtIndexAsync (riadky 2237-2280) → SQL shift UP
- [ ] InsertRowsAsync (riadky 1216-1223 AFTER PART 1) → SQL bulk shift version
- [ ] DeleteRowByIdAsync (riadky 2295-2344) → SQL soft delete + shift DOWN
- [ ] AppendRowsAsync → AddRowsAsync (riadky 1103-1156) → SQL bulk INSERT
- [ ] GetRowByIdAsync, UpdateRowByIdAsync, GetRowAsync, UpdateRowAsync
- [ ] SetSortCriteria (riadky 674-732) → SQL ROW_NUMBER() + UPDATE
- [ ] SetFilterCriteria → BuildSqlFilterCondition (riadky 537-588)
- [ ] SearchAsync (riadky 752-843) → SQLite FTS5
- [ ] Writer Queue methods: WriterBackgroundTaskAsync (riadky 123-152), ExecuteWriteOperationAsync
- [ ] Helper methods: SerializeRowData, DeserializeRowData, QueueWriteOperationAsync, FlushWriterQueueAsync
- [ ] SQL execution methods: ExecuteInsertRowAsync, ExecuteUpdateRowAsync, ExecuteBulkInsertAsync, etc.
- [ ] GetRowIdByIndex, GetRowIndexById
- [ ] ClearAsync

**NEPRESUŇ:**
- Validation cache metódy (idú do SqliteValidationStrategy)
- WriteValidationResultsAsync (ide do SqliteValidationStrategy)
- IRowStore interface metódy (zostávajú v UnifiedRowStore ako delegácia)

---

## 7. UnifiedRowStore IMPLEMENTÁCIA

### 7.1 Nový súbor

**Nový súbor:** `Infrastructure/Persistence/UnifiedRowStore.cs`

**Riadky kódu:** ~500 LOC (nový kód, nie presun)

---

### 7.2 Implementačný template

```csharp
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Strategies;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;

/// <summary>
/// ✅ UNIFIED ROW STORE - FINAL VERSION
/// NOW: 100% functional parity + bulk shift optimization (InsertRowsAsync unified in PART 1).
/// ARCHITECTURE:
/// - Deleguje STORAGE operácie na IStorageStrategy (InMemory/SQLite)
/// - Deleguje VALIDATION storage na IValidationStrategy (InMemory/SQLite)
/// - Obsahuje BUSINESS LOGIKU (validation orchestration, metadata management)
/// - Žiadna duplicita storage kódu
/// </summary>
public class UnifiedRowStore : IRowStore
{
    // ========== PRIVATE FIELDS ==========

    private readonly IStorageStrategy _storage;
    private readonly IValidationStrategy _validation;
    private readonly ILogger _logger;

    // ========== CONSTRUCTOR ==========

    public UnifiedRowStore(
        IStorageStrategy storageStrategy,
        IValidationStrategy validationStrategy,
        ILogger<UnifiedRowStore> logger)
    {
        _storage = storageStrategy ?? throw new ArgumentNullException(nameof(storageStrategy));
        _validation = validationStrategy ?? throw new ArgumentNullException(nameof(validationStrategy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation(
            "UnifiedRowStore created: storage={Storage}, validation={Validation}",
            _storage.StrategyName, _validation.StrategyName);
    }

    // ========== INSERT OPERATIONS - DELEGÁCIA (PART 1 UNIFIED!) ==========

    /// <summary>
    /// ✅ UNIFIED (PART 1): Inserts single row at index.
    /// Deleguje na IStorageStrategy.InsertRowAtIndexAsync.
    /// IDENTICAL behavior (InMemory: __rowNumber shift in-memory, SQLite: SQL UPDATE).
    /// </summary>
    public Task<string> InsertRowAtIndexAsync(
        int index,
        IReadOnlyDictionary<string, object?>? rowData,
        CancellationToken ct)
    {
        _logger.LogDebug(
            "InsertRowAtIndexAsync: index={Index}, strategy={Strategy}",
            index, _storage.StrategyName);

        // ✅ DELEGATE: InMemory alebo SQLite strategy
        return _storage.InsertRowAtIndexAsync(index, rowData, ct);
    }

    /// <summary>
    /// ✅ UNIFIED + OPTIMIZED (PART 1 FIX): Inserts multiple rows at index.
    /// Deleguje na IStorageStrategy.InsertRowsAsync.
    /// NOW: IDENTICAL behavior in both strategies + BULK SHIFT optimization.
    /// PERFORMANCE: ~200ms for 1000 rows (25x faster than sequential).
    /// </summary>
    public Task InsertRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex,
        CancellationToken ct)
    {
        var rowsList = rows.ToList();
        _logger.LogInformation(
            "InsertRowsAsync: Inserting {Count} rows at index {Index}, strategy={Strategy}",
            rowsList.Count, startIndex, _storage.StrategyName);

        // ✅ DELEGATE: Both strategies now use BULK SHIFT optimization (PART 1 fix)
        return _storage.InsertRowsAsync(rowsList, startIndex, ct);
    }

    // ========== OTHER CORE METHODS - DELEGÁCIA ==========

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsRangeAsync(
        long startIndex,
        int count,
        bool onlyFiltered,
        CancellationToken ct)
    {
        _logger.LogDebug(
            "GetRowsRangeAsync: startIndex={Start}, count={Count}, onlyFiltered={Filtered}, strategy={Strategy}",
            startIndex, count, onlyFiltered, _storage.StrategyName);

        return _storage.GetRowsRangeAsync(startIndex, count, onlyFiltered, ct);
    }

    public Task<long> GetRowCountAsync(CancellationToken ct) =>
        _storage.GetRowCountAsync(onlyFiltered: false, ct);

    public Task<long> GetRowCountAsync(bool onlyFiltered, CancellationToken ct) =>
        _storage.GetRowCountAsync(onlyFiltered, ct);

    public Task<int> AddRangeAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct) =>
        _storage.AddRowsAsync(rows, ct);

    public async Task AppendRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct)
    {
        var rowsList = rows.ToList();
        _logger.LogInformation(
            "AppendRowsAsync: Appending {Count} rows, strategy={Strategy}",
            rowsList.Count, _storage.StrategyName);

        await _storage.AddRowsAsync(rowsList, ct);
    }

    public Task DeleteRowByIdAsync(string rowId, CancellationToken ct)
    {
        _logger.LogDebug("DeleteRowByIdAsync: rowId={RowId}, strategy={Strategy}",
            rowId, _storage.StrategyName);

        return _storage.DeleteRowByIdAsync(rowId, ct);
    }

    public Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(
        string rowId,
        CancellationToken ct) =>
        _storage.GetRowByIdAsync(rowId, ct);

    public Task<bool> UpdateRowByIdAsync(
        string rowId,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken ct)
    {
        _logger.LogDebug("UpdateRowByIdAsync: rowId={RowId}, strategy={Strategy}",
            rowId, _storage.StrategyName);

        // Wrap void Task in bool Task
        return _storage.UpdateRowByIdAsync(rowId, rowData, ct).ContinueWith(_ => true, ct);
    }

    // ========== FILTER & SORT - DELEGÁCIA ==========

    public void SetFilterCriteria(IReadOnlyList<object>? filterCriteria)
    {
        var criteria = filterCriteria?.OfType<FilterCriterion>().ToList() ?? new List<FilterCriterion>();

        _logger.LogInformation(
            "SetFilterCriteria: {Count} criteria, strategy={Strategy}",
            criteria.Count, _storage.StrategyName);

        _storage.SetFilterCriteria(criteria);
    }

    public void ClearFilterCriteria()
    {
        _logger.LogInformation("ClearFilterCriteria: strategy={Strategy}", _storage.StrategyName);
        _storage.ClearFilterCriteria();
    }

    public IReadOnlyList<object> GetFilterCriteria()
    {
        var criteria = _storage.GetFilterCriteria();
        return criteria?.Cast<object>().ToList() ?? new List<object>();
    }

    public void SetSortCriteria(string columnName, SortDirection direction)
    {
        _logger.LogInformation(
            "SetSortCriteria: column={Column}, direction={Direction}, strategy={Strategy}",
            columnName, direction, _storage.StrategyName);

        _storage.SetSortCriteria(columnName, direction);
    }

    public void ClearSortCriteria()
    {
        _logger.LogInformation("ClearSortCriteria: strategy={Strategy}", _storage.StrategyName);
        _storage.ClearSortCriteria();
    }

    // ========== VALIDATION - DELEGÁCIA ==========

    public Task WriteValidationResultsAsync(
        IEnumerable<ValidationError> results,
        CancellationToken ct)
    {
        _logger.LogDebug(
            "WriteValidationResultsAsync: {Count} errors, validation={Validation}",
            results.Count(), _validation.StrategyName);

        return _validation.WriteValidationResultsAsync(results, ct);
    }

    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(
        bool onlyFiltered,
        bool onlyChecked,
        CancellationToken ct) =>
        _validation.GetValidationErrorsAsync(onlyFiltered, onlyChecked, ct);

    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(
        string rowId,
        CancellationToken ct) =>
        _validation.GetValidationErrorsForRowAsync(rowId, ct);

    public Task<bool> HasValidationStateForScopeAsync(
        bool onlyFiltered,
        bool onlyChecked,
        CancellationToken ct) =>
        _validation.HasValidationStateAsync(onlyFiltered, onlyChecked, ct);

    public Task ClearValidationStateAsync(CancellationToken ct) =>
        _validation.ClearValidationStateAsync(ct);

    // SENIOR FIX: Validation cache methods
    public bool IsRowValidationCached(string rowId) =>
        _validation.IsRowValidationCached(rowId);

    public void MarkRowAsValidated(string rowId) =>
        _validation.MarkRowAsValidated(rowId);

    public void ClearValidationCache() =>
        _validation.ClearValidationCache();

    public Task WriteValidationResultsBatchAsync(
        Dictionary<string, ValidationError[]> validationResults,
        CancellationToken ct) =>
        _validation.WriteValidationResultsBatchAsync(validationResults, ct);

    // ========== SEARCH - DELEGÁCIA ==========

    public Task<IReadOnlyList<string>> SearchAsync(
        string searchText,
        string[]? targetColumns,
        bool caseSensitive,
        CancellationToken ct) =>
        _storage.SearchAsync(searchText, targetColumns, caseSensitive, ct);

    // ========== UTILITY - DELEGÁCIA ==========

    public string? GetRowIdByIndex(int index) => _storage.GetRowIdByIndex(index);
    public int? GetRowIndexById(string rowId) => _storage.GetRowIndexById(rowId);
    public Task ClearAsync(CancellationToken ct) => _storage.ClearAsync(ct);

    // ========== METADATA ==========

    public string StorageType => _storage.StrategyName;
    public string ValidationType => _validation.StrategyName;

    // ... (ostatné IRowStore metódy - delegácia na _storage alebo _validation)
}
```

**Riadky kódu:** ~500 LOC (nový kód)

**BUSINESS LOGIKA v UnifiedRowStore:**
- Orchestrácia validation (WriteValidationResultsAsync koordinuje storage + cache)
- Metadata management (StorageType, ValidationType)
- Logging koordinácia
- Delegácia na storage/validation strategies

**ŽIADNA duplicita:** Storage logika je v strategies, nie tu! ✅

---

## 8. AdaptiveRowStore REFACTORING

### 8.1 Súbor na úpravu

**Súbor:** `Infrastructure/Persistence/AdaptiveRowStore.cs`

**Riadky na ZMENU:** ~50-200 (factory methods, migration logic)

---

### 8.2 Zmeny v AdaptiveRowStore

```csharp
/// <summary>
/// Adaptive Row Store - automatically switches between InMemory and SQLite storage
/// NOW: Uses UnifiedRowStore + strategy pattern (eliminates duplicity)
/// REFACTORED: Factory methods vytvárajú UnifiedRowStore s príslušnými strategies
/// </summary>
internal sealed class AdaptiveRowStore : IRowStore, IAsyncDisposable
{
    private readonly ILogger<AdaptiveRowStore>? _logger;
    private readonly AdvancedDataGridOptions _options;
    private readonly IServiceProvider _serviceProvider;

    private UnifiedRowStore _activeStore; // ← CHANGED: Now uses UnifiedRowStore instead of IRowStore
    private StorageStrategy _currentStrategy = StorageStrategy.InMemory;
    private readonly SemaphoreSlim _migrationLock = new(1, 1);
    private bool _isDisposed;

    // ========== FACTORY METHODS (UPDATED to use strategies) ==========

    /// <summary>
    /// ✅ UPDATED: Factory: Create UnifiedRowStore with InMemory strategies
    /// </summary>
    private UnifiedRowStore CreateInMemoryStore()
    {
        _logger?.LogDebug("Creating UnifiedRowStore with InMemory strategies");

        var storageLogger = _serviceProvider.GetService<ILogger<InMemoryStorageStrategy>>();
        var validationLogger = _serviceProvider.GetService<ILogger<InMemoryValidationStrategy>>();
        var unifiedLogger = _serviceProvider.GetService<ILogger<UnifiedRowStore>>();

        var storageStrategy = new InMemoryStorageStrategy(storageLogger);
        var validationStrategy = new InMemoryValidationStrategy(validationLogger);

        return new UnifiedRowStore(storageStrategy, validationStrategy, unifiedLogger);
    }

    /// <summary>
    /// ✅ UPDATED: Factory: Create UnifiedRowStore with SQLite strategies
    /// </summary>
    private UnifiedRowStore CreateHybridStore()
    {
        _logger?.LogDebug("Creating UnifiedRowStore with SQLite strategies");

        var storageLogger = _serviceProvider.GetService<ILogger<SqliteStorageStrategy>>();
        var validationLogger = _serviceProvider.GetService<ILogger<SqliteValidationStrategy>>();
        var unifiedLogger = _serviceProvider.GetService<ILogger<UnifiedRowStore>>();

        var dbLifecycleManager = _serviceProvider.GetRequiredService<IDatabaseLifecycleManager>();

        var storageStrategy = new SqliteStorageStrategy(storageLogger, dbLifecycleManager);
        var validationStrategy = new SqliteValidationStrategy(validationLogger, dbLifecycleManager);

        return new UnifiedRowStore(storageStrategy, validationStrategy, unifiedLogger);
    }

    // ========== MIGRATION METHODS (NO LOGIC CHANGE, just use UnifiedRowStore) ==========

    private async Task MigrateToHybridAsync(CancellationToken cancellationToken)
    {
        await _migrationLock.WaitAsync(cancellationToken);
        try
        {
            _logger?.LogWarning("MIGRATION START: InMemory → SQLite (Strategy Pattern)");
            var stopwatch = Stopwatch.StartNew();

            // 1. Create new UnifiedRowStore with SQLite strategies
            var hybridStore = CreateHybridStore();

            // ✅ Initialize SQLite DB (NOVÝ krok)
            await InitializeHybridStoreAsync(hybridStore, cancellationToken);

            // 2. Copy all data from InMemory to SQLite (IDENTICAL API!)
            var allRows = await _activeStore.GetAllRowsAsync(cancellationToken);
            await hybridStore.AppendRowsAsync(allRows, cancellationToken);

            // 3. Copy validation cache (IDENTICAL API!)
            var validationErrors = await _activeStore.GetValidationErrorsAsync(false, false, cancellationToken);
            if (validationErrors.Count > 0)
            {
                var validationDict = validationErrors
                    .GroupBy(e => e.RowId)
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .ToDictionary(g => g.Key!, g => g.ToArray());

                await hybridStore.WriteValidationResultsBatchAsync(validationDict, cancellationToken);
            }

            // 4. Swap stores (atomic)
            var oldStore = _activeStore;
            _activeStore = hybridStore;
            _currentStrategy = StorageStrategy.Hybrid;

            // 5. Dispose old store (if needed)
            // Note: UnifiedRowStore môže implementovať IAsyncDisposable pre cleanup

            stopwatch.Stop();
            _logger?.LogWarning(
                "MIGRATION COMPLETE: InMemory → SQLite in {Duration}ms, {RowCount} rows migrated (Strategy Pattern)",
                stopwatch.ElapsedMilliseconds, allRows.Count);
        }
        finally
        {
            _migrationLock.Release();
        }
    }

    /// <summary>
    /// ✅ NOVÝ: Initialize SQLite database for HybridStore
    /// </summary>
    private async Task InitializeHybridStoreAsync(UnifiedRowStore hybridStore, CancellationToken ct)
    {
        // Extract IDatabaseLifecycleManager from strategy and initialize
        // (Implementácia závisí od toho, či UnifiedRowStore má prístup k strategies)
        // Alternatíva: Volať InitializeAsync na database manager directly

        var dbManager = _serviceProvider.GetRequiredService<IDatabaseLifecycleManager>();
        var result = await dbManager.InitializeDatabaseAsync(_options.DatabasePath, ct);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Database initialization failed: {result.ErrorMessage}");
        }
    }

    // ========== DELEGATION METHODS (NO CHANGES - all delegate to _activeStore) ==========

    // All IRowStore methods delegate to _activeStore (same as before)
    // ...
}
```

**Zmeny:**

- `IRowStore _activeStore` → `UnifiedRowStore _activeStore` (type change)
- Factory methods `CreateInMemoryStore()`, `CreateHybridStore()` → vytvárajú UnifiedRowStore + strategies
- Migration logic NEZMENENÁ (API je identické!)
- Delegácia na `_activeStore` NEZMENENÁ

**Riadky kódu (AdaptiveRowStore):** ~100 LOC (refactored, z ~537 LOC)

---

## 9. POROVNANIE: PRED vs PO

### 9.1 Štruktúra kódu

#### PRED (súčasná architektúra):

```
┌────────────────────────────────────────────────┐
│ AdaptiveRowStore (~537 LOC)                    │
└────────────────────────────────────────────────┘
      │
      ├─→ InMemoryRowStore (~2300 LOC)
      │   - ConcurrentDictionary
      │   - ✅ InsertRowsAsync (PART 1 bulk shift)
      │   - ❌ 800 LOC DUPLICITNEJ logiky
      │
      └─→ HybridRowStore (~2348 LOC)
          - SQLite + WAL
          - ✅ InsertRowsAsync (PART 1 SQL bulk shift)
          - ❌ 800 LOC DUPLICITNEJ logiky

TOTAL: ~5185 LOC
DUPLICITA: ~800-1000 LOC (35-40%)
```

#### PO (Strategy Pattern):

```
┌────────────────────────────────────────────────┐
│ AdaptiveRowStore (~100 LOC refactored)         │
└────────────────────────────────────────────────┘
      │
      └─→ UnifiedRowStore (~500 LOC)
          - Zdieľaná business logika
          - Delegácia na strategies
          │
          ├─→ InMemoryStorageStrategy (~800 LOC)
          │   - ConcurrentDictionary
          │   - ✅ Bulk shift (PART 1)
          │
          ├─→ SqliteStorageStrategy (~850 LOC)
          │   - SQLite + WAL
          │   - ✅ SQL bulk shift (PART 1)
          │
          ├─→ InMemoryValidationStrategy (~200 LOC)
          └─→ SqliteValidationStrategy (~250 LOC)

TOTAL: ~2700 LOC
DUPLICITA: 0 LOC (0%) ⭐
```

---

### 9.2 Porovnanie metrik

| Metrika                 | PRED                             | PO                                   | Improvement         |
|-------------------------|----------------------------------|--------------------------------------|---------------------|
| **Total LOC**           | ~5185                            | **~2700** ⭐                           | ✅ -2485 LOC (48%)   |
| **Duplicitný kód**      | ~800-1000 LOC (35-40%)           | **0 LOC (0%)** ⭐                      | ✅ -800 LOC          |
| **Počet súborov**       | 3 (AdaptiveRowStore, InMemory, Hybrid) | **8** (Adaptive, Unified, 6 strategies) | ⚠️ +5 súborov       |
| **Max LOC per file**    | ~2348 (HybridRowStore)           | **~850** (SqliteStorageStrategy) ⭐    | ✅ -1498 LOC         |
| **Funkcionálna parita** | ✅ 100% (po PART 1)               | ✅ 100%                               | ✅ Preserved         |
| **InsertRowsAsync performance** | ✅ ~200ms (InMemory), ~300ms (Hybrid) | ✅ **~200ms, ~300ms** (PRESERVED) ⭐ | ✅ No regression     |
| **Testovateľnosť**      | ⚠️ 2 veľké triedy (hard to mock)  | ✅ **6 malých strategies (easy to mock)** ⭐ | ✅ Better            |
| **Maintainability**     | ⚠️ Zmeny v 2 miestach             | ✅ **Zmeny v 1 stratégii** ⭐          | ✅ Better            |
| **Rozšíriteľnosť**      | ⚠️ Redis = duplikovať 800 LOC     | ✅ **Redis = implementovať interface** ⭐ | ✅ Better            |

---

### 9.3 Benefits summary

| Benefit                   | Popis                                                                 | Impact              |
|--------------------------|-----------------------------------------------------------------------|---------------------|
| **Eliminácia duplicity** | ~800-1000 LOC odstránených                                            | ✅ **HIGH**          |
| **LOC reduction**        | 48% redukcia total LOC (5185 → 2700)                                  | ✅ **HIGH**          |
| **Separation of concerns** | Storage logic oddelená od business logiky                           | ✅ **MEDIUM**        |
| **Testovateľnosť**       | Izolované strategies → unit testy bez dependencies                    | ✅ **HIGH**          |
| **Maintainability**      | Zmeny v 1 stratégii namiesto 2 stores                                 | ✅ **MEDIUM**        |
| **Rozšíriteľnosť**       | Redis/MongoDB = implementovať interface (nie duplikovať kód)          | ✅ **HIGH**          |
| **Performance preserved**| PART 1 bulk shift optimalizácia zachovaná                             | ✅ **CRITICAL**      |
| **API compatibility**    | IRowStore interface BEZ ZMIEN → žiadny breaking change               | ✅ **CRITICAL**      |

---

## 10. IMPLEMENTAČNÝ PLÁN

### 10.1 Prerekvizity

- [x] **PART 1 IMPLEMENTOVANÉ:** InsertRowsAsync bulk shift MUSÍ BYŤ HOTOVÉ! ⚠️
- [x] Analýza duplicitného kódu (DONE)
- [x] Návrh Strategy Pattern architektúry (DONE)
- [ ] Code review nového riešenia
- [ ] Schválenie implementačného plánu

---

### 10.2 Implementačné fázy

#### FÁZA 1: Vytvorenie interfaces (⏱️ 1-2 hodiny)

**Úlohy:**

1. **Vytvoriť IStorageStrategy.cs:**
   - Nový súbor: `Infrastructure/Persistence/Strategies/IStorageStrategy.cs`
   - Obsah: Sekcia 4.1 (interface definition)
   - ~150 LOC

2. **Vytvoriť IValidationStrategy.cs:**
   - Nový súbor: `Infrastructure/Persistence/Strategies/IValidationStrategy.cs`
   - Obsah: Sekcia 4.2 (interface definition)
   - ~80 LOC

**Acceptance criteria:**
- [ ] Oba interfaces kompilujú
- [ ] XML komentáre sú kompletné
- [ ] Interface metódy majú správne signatúry (parameters, return types)

---

#### FÁZA 2: InMemoryStorageStrategy implementácia (⏱️ 2-3 hodiny)

**Úlohy:**

1. **Vytvoriť InMemoryStorageStrategy.cs:**
   - Nový súbor: `Infrastructure/Persistence/Strategies/Storage/InMemoryStorageStrategy.cs`
   - Template: Sekcia 5.2
   - ~800 LOC (presun z InMemoryRowStore.cs)

2. **Presunúť kód z InMemoryRowStore.cs:**
   - Private fields: `_rows`, `_modificationLock`, cache fields
   - Methods: InsertRowAtIndexAsync, InsertRowsAsync (PART 1 version), DeleteRowByIdAsync, AddRowsAsync, SetSortCriteria, SearchAsync, etc.
   - Checklist: Sekcia 5.3

3. **Unit testy:**
   - Test: InsertRowsAsync(1000 rows, index=5) → verify positions + performance < 300ms
   - Test: SetSortCriteria → verify __rowNumber renumbering
   - Test: SearchAsync → verify LINQ search

**Acceptance criteria:**
- [ ] InMemoryStorageStrategy implementuje IStorageStrategy
- [ ] Všetky presunúté metódy fungujú
- [ ] Performance: InsertRowsAsync(1000 rows) < 300ms
- [ ] Unit testy prechádzajú (100% coverage)

---

#### FÁZA 3: SqliteStorageStrategy implementácia (⏱️ 3-4 hodiny)

**Úlohy:**

1. **Vytvoriť SqliteStorageStrategy.cs:**
   - Nový súbor: `Infrastructure/Persistence/Strategies/Storage/SqliteStorageStrategy.cs`
   - Template: Sekcia 6.2
   - ~850 LOC (presun z HybridRowStore.cs)

2. **Presunúť kód z HybridRowStore.cs:**
   - Private fields: `_databaseLifecycleManager`, Writer Queue fields
   - Constructor: Initialize Writer Queue
   - Methods: InsertRowAtIndexAsync, InsertRowsAsync (PART 1 SQL version), DeleteRowByIdAsync, AddRowsAsync, SetSortCriteria, SearchAsync (FTS5), etc.
   - Writer Queue: WriterBackgroundTaskAsync, QueueWriteOperationAsync, FlushWriterQueueAsync
   - Checklist: Sekcia 6.3

3. **Unit testy:**
   - Test: InsertRowsAsync(1000 rows, index=5) → verify SQL query + positions + performance < 400ms
   - Test: SetSortCriteria → verify SQL ROW_NUMBER() + UPDATE
   - Test: SearchAsync → verify FTS5 MATCH

**Acceptance criteria:**
- [ ] SqliteStorageStrategy implementuje IStorageStrategy
- [ ] Všetky presunúté metódy fungujú
- [ ] Performance: InsertRowsAsync(1000 rows) < 400ms
- [ ] Writer Queue funguje správne (background task processing)
- [ ] Unit testy prechádzajú (100% coverage)

---

#### FÁZA 4: Validation strategies implementácia (⏱️ 2-3 hodiny)

**Úlohy:**

1. **Vytvoriť InMemoryValidationStrategy.cs:**
   - Nový súbor: `Infrastructure/Persistence/Strategies/Validation/InMemoryValidationStrategy.cs`
   - Presun validation cache metód z InMemoryRowStore.cs
   - ~200 LOC

2. **Vytvoriť SqliteValidationStrategy.cs:**
   - Nový súbor: `Infrastructure/Persistence/Strategies/Validation/SqliteValidationStrategy.cs`
   - Presun validation state SQL metód z HybridRowStore.cs
   - ~250 LOC

3. **Unit testy:**
   - Test: WriteValidationResultsAsync → verify cache/SQL write
   - Test: WriteValidationResultsBatchAsync → verify batch write (SENIOR FIX)

**Acceptance criteria:**
- [ ] Oba validation strategies implementujú IValidationStrategy
- [ ] Validation cache funguje (SENIOR FIX: prevents infinite loop)
- [ ] Unit testy prechádzajú

---

#### FÁZA 5: UnifiedRowStore implementácia (⏱️ 3-4 hodiny)

**Úlohy:**

1. **Vytvoriť UnifiedRowStore.cs:**
   - Nový súbor: `Infrastructure/Persistence/UnifiedRowStore.cs`
   - Template: Sekcia 7.2
   - ~500 LOC (nový kód)

2. **Implementovať delegáciu:**
   - INSERT operations → delegate na IStorageStrategy
   - CRUD operations → delegate na IStorageStrategy
   - Filter/Sort → delegate na IStorageStrategy
   - Validation → delegate na IValidationStrategy

3. **Integration testy:**
   - Test: UnifiedRowStore + InMemoryStorageStrategy → verify IDENTICKÉ správanie ako InMemoryRowStore
   - Test: UnifiedRowStore + SqliteStorageStrategy → verify IDENTICKÉ správanie ako HybridRowStore
   - Test: InsertRowsAsync(1000 rows) → verify performance (no regression)
   - Test: Migration InMemory ↔ SQLite → verify data integrity

**Acceptance criteria:**
- [ ] UnifiedRowStore implementuje IRowStore
- [ ] Všetky IRowStore metódy fungujú (delegácia)
- [ ] Performance: Žiadna regressia oproti InMemoryRowStore/HybridRowStore
- [ ] Integration testy prechádzajú (100% parity)

---

#### FÁZA 6: AdaptiveRowStore refactoring (⏱️ 2-3 hodiny)

**Úlohy:**

1. **Refaktorovať AdaptiveRowStore.cs:**
   - Zmena: `IRowStore _activeStore` → `UnifiedRowStore _activeStore`
   - Factory methods: `CreateInMemoryStore()`, `CreateHybridStore()` → vytvárajú UnifiedRowStore + strategies
   - Template: Sekcia 8.2

2. **Migration testing:**
   - Test: Migrácia InMemory → SQLite (100K rows threshold)
   - Test: Migrácia SQLite → InMemory (drop below threshold)
   - Test: Data integrity po migrácii (všetky riadky, validation state)

**Acceptance criteria:**
- [ ] AdaptiveRowStore používa UnifiedRowStore
- [ ] Migration InMemory ↔ SQLite funguje (no data loss)
- [ ] Performance: Žiadna regressia v migrácii
- [ ] End-to-end testy prechádzajú

---

#### FÁZA 7: Cleanup + Deprecation (⏱️ 1-2 hodiny)

**Úlohy:**

1. **[Obsolete] annotations:**
   - `InMemoryRowStore.cs`: Pridať `[Obsolete("Use UnifiedRowStore with InMemoryStorageStrategy")]`
   - `HybridRowStore.cs`: Pridať `[Obsolete("Use UnifiedRowStore with SqliteStorageStrategy")]`

2. **Documentation update:**
   - Aktualizovať XML komentáre v `IRowStore.cs`
   - Pridať poznámku o Strategy Pattern v README
   - Pridať migration guide (InMemoryRowStore → UnifiedRowStore)

3. **Git commit:**
   - Commit message: `refactor: Implement Strategy Pattern for RowStore (eliminate 800 LOC duplicity)`
   - Body: Link na tento dokumentačný súbor

**Acceptance criteria:**
- [ ] Obsolete warnings vo Visual Studio
- [ ] Documentation aktualizovaná
- [ ] Žiadne compiler warnings (okrem obsolete)

---

#### FÁZA 8: Performance validation (⏱️ 1-2 hodiny)

**Úlohy:**

1. **Benchmark testy:**
   - InsertRowsAsync(1000 rows, index=5): InMemory < 300ms, SQLite < 400ms
   - SetSortCriteria(1M rows): SQLite < 2s
   - GetRowsRangeAsync(1M rows): SQLite < 50ms
   - Migration(100K rows): < 5s

2. **Performance regression tests:**
   - Porovnať PRED (InMemoryRowStore/HybridRowStore) vs PO (UnifiedRowStore + strategies)
   - Verify: Žiadna regressia (max 5% tolerance)

**Acceptance criteria:**
- [ ] Všetky benchmark testy prechádzajú
- [ ] Žiadna performance regressia (< 5% difference)
- [ ] Performance report vytvorený

---

### 10.3 Timeline

| Fáza | Popis                                | Čas        | Dependencies                |
|------|--------------------------------------|------------|-----------------------------|
| 1    | Vytvorenie interfaces                | 1-2 hodiny | PART 1 implementované ⚠️     |
| 2    | InMemoryStorageStrategy              | 2-3 hodiny | Fáza 1                      |
| 3    | SqliteStorageStrategy                | 3-4 hodiny | Fáza 1                      |
| 4    | Validation strategies                | 2-3 hodiny | Fáza 1                      |
| 5    | UnifiedRowStore                      | 3-4 hodiny | Fáza 2, 3, 4                |
| 6    | AdaptiveRowStore refactoring         | 2-3 hodiny | Fáza 5                      |
| 7    | Cleanup + Deprecation                | 1-2 hodiny | Fáza 6                      |
| 8    | Performance validation               | 1-2 hodiny | Fáza 7                      |
| **TOTAL** | **ČASŤ 2 implementácia**          | **15-22 hodín** | **PART 1 prerequisite** ⚠️ |

**CELKOVÝ ČAS (PART 1 + PART 2):** 18-26 hodín (2.5-3.5 dni práce)

---

### 10.4 Rollback plán

Ak implementácia zlyhá:

1. **Revert commit:**
   ```bash
   git revert HEAD~N  # N = počet commitov (FÁZA 1-8)
   ```

2. **Keep InMemoryRowStore/HybridRowStore:**
   - Odstrániť `[Obsolete]` annotations
   - Odstrániť nové súbory (strategies, UnifiedRowStore)
   - AdaptiveRowStore: revert na pôvodnú implementáciu

3. **Debugging:**
   - Kontrola interface implementácie (všetky metódy implementované?)
   - Kontrola delegácie v UnifiedRowStore
   - Kontrola factory methods v AdaptiveRowStore
   - Performance profiling (identify bottlenecks)

---

### 10.5 Deliverables

Po dokončení implementácie:

- [ ] ✅ Pull request s implementačným kódom
- [ ] ✅ Performance benchmark report (InMemory vs SQLite strategies)
- [ ] ✅ Unit tests s 100% coverage pre všetky strategies
- [ ] ✅ Integration tests s 100% coverage pre UnifiedRowStore
- [ ] ✅ Migration guide (InMemoryRowStore → UnifiedRowStore)
- [ ] ✅ Updated architecture documentation
- [ ] ✅ Git commit message s linkom na tento dokument

---

## 📝 POZNÁMKY

### Kritické body na overenie

1. **PART 1 prerequisite:**
   - InsertRowsAsync bulk shift MUSÍ BYŤ implementované PRED začatím PART 2! ⚠️
   - Verify: InMemoryRowStore.cs:533-598 obsahuje bulk shift version
   - Verify: HybridRowStore.cs:1216-1223 obsahuje SQL bulk shift version

2. **Interface implementácia:**
   - Všetky metódy z IStorageStrategy MUSIA byť implementované v oboch strategies
   - Všetky metódy z IValidationStrategy MUSIA byť implementované v oboch strategies

3. **Delegácia v UnifiedRowStore:**
   - Všetky IRowStore metódy MUSIA delegovať na _storage alebo _validation
   - Žiadna business logika v UnifiedRowStore (len orchestrácia)

4. **Performance preservation:**
   - InsertRowsAsync bulk shift MUSÍ byť zachovaný v strategies
   - Žiadna regressia performance (max 5% tolerance)

5. **Migration compatibility:**
   - AdaptiveRowStore MUSÍ používať UnifiedRowStore
   - Migration InMemory ↔ SQLite MUSÍ fungovať bez data loss

---

## ✅ CHECKLIST PRE IMPLEMENTÁCIU

### Pred začatím

- [ ] **PART 1 IMPLEMENTOVANÉ** (InsertRowsAsync bulk shift) ⚠️
- [ ] Prečítaný celý dokument
- [ ] Code review s architektom
- [ ] Schválenie implementačného plánu
- [ ] Backup existujúceho kódu (git branch)

### Počas implementácie (FÁZA 1-8)

- [ ] Interfaces vytvorené (IStorageStrategy, IValidationStrategy)
- [ ] InMemoryStorageStrategy implementovaná + testy prechádzajú
- [ ] SqliteStorageStrategy implementovaná + testy prechádzajú
- [ ] Validation strategies implementované + testy prechádzajú
- [ ] UnifiedRowStore implementovaný + testy prechádzajú
- [ ] AdaptiveRowStore refaktorovaný + testy prechádzajú
- [ ] [Obsolete] annotations pridané
- [ ] Performance benchmark splnený (< 5% regressia)

### Po implementácii

- [ ] Všetky existujúce testy prechádzajú (0 regressions)
- [ ] Nové unit testy prechádzajú (100% coverage strategies)
- [ ] Integration testy prechádzajú (UnifiedRowStore + strategies)
- [ ] Performance report vytvorený (no regressia)
- [ ] Documentation aktualizovaná
- [ ] Pull request vytvorený
- [ ] Code review od seniora
- [ ] Merge do main branch

---

## 🎯 ZÁVER

### Odporúčanie

✅ **IMPLEMENTOVAŤ STRATEGY PATTERN** po aplikovaní PART 1 FIX

**Zdôvodnenie:**

1. **PART 1 je prerequisite:**
   - Bulk shift je kritický pre performance (25x rýchlejšie)
   - Zabezpečí 100% funkcionálnu paritu PRED refactoringom
   - Po PART 1: InsertRowsAsync je IDENTICKÉ v oboch stores → bezpečný základ

2. **Strategy Pattern benefits:**
   - **Eliminuje ~800-1000 LOC duplicity** ⭐
   - **Zníži total LOC z ~5185 na ~2700** (48% redukcia) ⭐
   - Zachováva PART 1 bulk shift optimalizáciu ✅
   - Lepšia testovateľnosť (izolované strategies)
   - Rozšíriteľnosť (Redis/MongoDB = implementovať interface)

3. **Žiadna zmena API:**
   - IRowStore interface NEZMENENÝ (backward compatible)
   - AdaptiveRowStore API NEZMENENÉ (transparent switching)
   - Migration logika NEZMENENÁ (same behavior)
   - Používateľ nevidí rozdiel!

4. **Performance preserved:**
   - InsertRowsAsync(1000 rows): ~200ms (InMemory), ~300ms (SQLite) ⭐
   - Bulk shift optimalizácia je v strategies ✅
   - Žiadna performance regressia (< 5% tolerance)

5. **Effort:** 15-22 hodín (2-3 dni práce) - rozumný investment

---

### Finálna štruktúra (PO implementácii)

```
Infrastructure/Persistence/
├─ Interfaces/
│  ├─ IRowStore.cs (existujúce - BEZ ZMIEN)
│  ├─ IStorageStrategy.cs (NOVÝ) ⭐
│  └─ IValidationStrategy.cs (NOVÝ) ⭐
│
├─ Strategies/
│  ├─ Storage/
│  │  ├─ InMemoryStorageStrategy.cs (NOVÝ - s PART 1 bulk shift) ⭐
│  │  └─ SqliteStorageStrategy.cs (NOVÝ - s PART 1 SQL bulk shift) ⭐
│  │
│  └─ Validation/
│     ├─ InMemoryValidationStrategy.cs (NOVÝ) ⭐
│     └─ SqliteValidationStrategy.cs (NOVÝ) ⭐
│
├─ UnifiedRowStore.cs (NOVÝ - 100% parity + bulk optimization) ⭐
├─ AdaptiveRowStore.cs (REFACTORED - používa UnifiedRowStore)
│
├─ InMemoryRowStore.cs ([Obsolete] - PART 1 bulk shift applied)
└─ HybridRowStore.cs ([Obsolete] - PART 1 SQL bulk shift applied)
```

---

**Koniec dokumentu ČASŤ 2**

**Predchádzalo:** ČASŤ 1 - InsertRowsAsync bulk shift (prerequisite)

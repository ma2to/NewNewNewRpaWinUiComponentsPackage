# MASTER INDEX - Kompletná dokumentácia Hybrid SQLite Migration

## 📋 METADATA

**Dátum vytvorenia:** 19.10.2025
**Verzia:** 1.0
**Projekt:** AdvancedDataGrid - Hybrid SQLite Migration
**Autor:** Senior Developer (30-year experience)

---

## 🎯 PREHĽAD PROJEKTU

Tento projekt predstavuje **kompletnú migráciu** AdvancedDataGrid komponentu z pure in-memory modelu na **hybrid SQLite model** s cieľom:

1. **Škálovateľnosť** - Podpora 10M+ riadkov bez OutOfMemoryException
2. **Performance** - Filter/Sort/Search 15-40x rýchlejšie
3. **Nová funkcionalita** - Add Row Modal Dialog, Virtualizácia, Pagination
4. **Zachovanie existujúcej funkcionality** - Sort/Filter/Search z `SPECIFIKACIA_SORT_FILTER_SEARCH.md`
5. **Tri operačné módy** - Interactive, Headless+ManualUI, Pure Headless

---

## 📚 DOKUMENTÁCIA - ŠTRUKTÚRA

### Hlavné dokumenty (povinné čítanie)

| Dokument | Popis | Dôležitosť | Čas čítania |
|----------|-------|------------|-------------|
| **`docu_MasterIndex.md`** (tento súbor) | Master index, overview projektu | ⭐⭐⭐ | 10 min |
| **`docu_HybridSQLiteArchitecture.md`** | Hlavná architektúra hybrid modelu | ⭐⭐⭐ CRITICAL | 30 min |
| **`docu_DatabaseManagement.md`** | Database lifecycle, cleanup, API | ⭐⭐⭐ CRITICAL | 20 min |
| **`docu_SortFilterSearchIntegration.md`** | SQL WHERE/ORDER BY/FTS5 integrácia | ⭐⭐⭐ CRITICAL | 30 min |
| **`docu_AddRowModalSpecification.md`** | Modal dialog pre pridanie riadku | ⭐⭐ HIGH | 20 min |
| **`docu_ImplementationRoadmap.md`** | Implementačná príručka, fázy, timeline | ⭐⭐⭐ CRITICAL | 40 min |

### Existujúce dokumenty (referenčné)

| Dokument | Popis | Relevancia |
|----------|-------|------------|
| **`SPECIFIKACIA_SORT_FILTER_SEARCH.md`** | Pôvodná špecifikácia Sort/Filter/Search | ⭐⭐⭐ Základ pre Fázu 2 |

---

## 📖 READING ORDER (Odporúčané poradie čítania)

Pre najlepšie pochopenie projektu odporúčame toto poradie:

### Krok 1: Úvod a kontext (30 min)
1. **`docu_MasterIndex.md`** (tento súbor) - Overview
2. **`SPECIFIKACIA_SORT_FILTER_SEARCH.md`** - Pôvodné požiadavky

### Krok 2: Architektúra (1.5 hodiny)
3. **`docu_HybridSQLiteArchitecture.md`** - Hybrid model design
4. **`docu_DatabaseManagement.md`** - Database lifecycle
5. **`docu_SortFilterSearchIntegration.md`** - SQL integrácia

### Krok 3: Features (20 min)
6. **`docu_AddRowModalSpecification.md`** - Modal dialog

### Krok 4: Implementácia (40 min)
7. **`docu_ImplementationRoadmap.md`** - Implementačný plán

**Celkový čas čítania: ~2.5 hodiny**

---

## 🔑 KĽÚČOVÉ KONCEPTY

### 1. Hybrid SQLite Model

**In-memory viewport (1000 rows)** + **SQLite persistence (10M+ rows)**

```
┌────────────────────────────────────────────────────┐
│  UI Layer (WinUI3)                                  │
│  ↓ max 1000 rows visible                           │
├────────────────────────────────────────────────────┤
│  HybridRowStore                                     │
│  ├─ Viewport Cache (in-memory: 1000 rows)         │
│  └─ SQLite DB (on-disk: 10M+ rows)                │
│     ├─ grid_rows table (main data)                 │
│     ├─ grid_rows_fts (FTS5 full-text search)      │
│     └─ Indexes (performance)                       │
└────────────────────────────────────────────────────┘
```

**Výhody:**
- ✅ Realtime per-cell operations in-memory (ms latency)
- ✅ Batch operations v SQLite (sekúndy pre milióny)
- ✅ Nízka pamäťová náročnosť (10 MB viewport vs 500 MB full in-memory pre 1M rows)

### 2. Filter/Sort/Search delegované na SQLite

**Pôvodný prístup (problém):**
- LINQ `OrderBy`, `Where`, `Contains` nad 10M objektmi v RAM
- O(n) complexity pre filter matching
- 20-60 sekúnd pre jednoduché operácie

**Nový prístup (riešenie):**
- SQL `WHERE`, `ORDER BY`, FTS5 `MATCH`
- SQLite engine optimalizácie (B-tree indexes)
- < 2-5 sekúnd pre tie isté operácie

**Example:**
```sql
-- Filter: Age > 30 AND Name LIKE '%John%'
SELECT __rowId, data
FROM grid_rows
WHERE __isDeleted = 0
  AND CAST(json_extract(data, '$.Age') AS REAL) > 30
  AND json_extract(data, '$.Name') LIKE '%John%'
ORDER BY json_extract(data, '$.Name') ASC
LIMIT 1000 OFFSET 0;
```

### 3. Writer Queue Pattern

**Problém:** SQLite file-based DB vyžaduje single writer (concurrent writes = "database is locked")

**Riešenie:** Dedicated writer thread konzumuje write operations z `Channel<WriteOperation>`

```csharp
// UI thread (non-blocking)
await grid.Rows.AddRowAsync(rowData);
  ↓
// Enqueue to writer queue (immediate return)
await _writeQueue.Writer.WriteAsync(new InsertRowOp(rowData));
  ↓
// Writer thread (background)
await ExecuteInsertRowAsync(rowData);  // SQLite INSERT
```

### 4. Tri operačné módy

| Mód | UI Rendering | Auto UI Refresh | Použitie |
|-----|--------------|-----------------|----------|
| **Interactive** | ✅ | ✅ | Štandardná UI aplikácia |
| **Headless + Manual UI** | ✅ | ❌ Manual `RefreshUIAsync()` | Custom UI update timing |
| **Pure Headless** | ❌ | ❌ | Backend service, testing |

### 5. Add Row Modal Dialog

**Features:**
- Dynamicky generované TextBoxy pre každý stĺpec
- Real-time validácia (debounce 300ms)
- Async uniqueness check (SQL query)
- Confirm button enabled len ak všetky hodnoty valid

---

## 📊 PERFORMANCE TARGETS

| Operácia | Dataset | Target | Pôvodné | Zlepšenie |
|----------|---------|--------|---------|-----------|
| Bulk insert | 100k rows | < 5s | N/A | Nová feature |
| Bulk insert | 1M rows | < 30s | N/A | Nová feature |
| Filter (3 conditions) | 10M rows | < 2s | 15-30s | **15x** |
| Sort single column | 10M rows | < 5s | 20-40s | **8x** |
| Sort multi-column | 10M rows | < 8s | 30-60s | **7x** |
| Search (Contains) | 10M rows | < 2s | 30-60s | **30x** |
| Search (Regex) | 10M rows | < 5s | 2-5 min | **40x** |
| Page load (filtered+sorted) | 1000 rows | < 200ms | N/A | Nová feature |
| Memory footprint (viewport) | 10M rows | ~10 MB | OUT OF MEMORY | **Funguje!** |

---

## 🛠️ IMPLEMENTAČNÝ PLÁN - SÚHRN

| Fáza | Popis | Trvanie | Priorita | Dokument |
|------|-------|---------|----------|----------|
| **Fáza 0** | Príprava a analýza | 1 deň | CRITICAL | `docu_ImplementationRoadmap.md` |
| **Fáza 1** | Database Layer (HybridRowStore) | 5-6 dní | CRITICAL | `docu_HybridSQLiteArchitecture.md` + `docu_DatabaseManagement.md` |
| **Fáza 2** | Filter/Sort/Search so SQLite | 4-5 dní | CRITICAL | `docu_SortFilterSearchIntegration.md` |
| **Fáza 3** | Add Row Modal Dialog | 2-3 dni | HIGH | `docu_AddRowModalSpecification.md` |
| **Fáza 4** | Validation System Integration | 2-3 dni | HIGH | `docu_ImplementationRoadmap.md` |
| **Fáza 5** | Virtualization & Pagination | 3-4 dni | CRITICAL | `SPECIFIKACIA_SORT_FILTER_SEARCH.md` |
| **Fáza 6** | Three Operation Modes | 2 dni | CRITICAL | `docu_ImplementationRoadmap.md` |
| **Fáza 7** | Testing & Performance Tuning | 4-5 dní | CRITICAL | `docu_ImplementationRoadmap.md` |
| **Fáza 8** | Documentation & Demo App | 2 dni | MEDIUM | `docu_ImplementationRoadmap.md` |

**Celkový odhad:** 25-33 pracovných dní **(5-7 týždňov)**

---

## 🐛 ZOZNAM NÁJDENÝCH CHÝB A PROBLÉMOV

Počas analýzy komponentu boli identifikované nasledujúce chyby (popri pôvodnej špecifikácii):

### 1. OutOfMemoryException pri 10M+ rows ❌ CRITICAL

**Súbor:** `InMemoryRowStore.cs`

**Problém:**
- `ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>` drží všetky riadky v RAM
- Pri 10M rows: ~5-10 GB RAM
- Aplikácia crashuje

**Riešenie:**
- Hybrid SQLite model (Fáza 1)

---

### 2. Filter len počíta matches, nevracia filtered view ❌ CRITICAL

**Súbor:** `Features/Filter/Services/FilterService.cs:52`

**Problém:**
```csharp
public async Task<int> ApplyFilterAsync(string columnName, FilterOperator @operator, object? value)
{
    _activeFilters.Add(filter);
    var filteredCount = await ApplyFiltersToDataAsync(operationId); // ❌ Len počíta!

    // ❌ CHÝBA: Aplikovať filtered view do IRowStore
    // ❌ CHÝBA: UI refresh event trigger

    return filteredCount; // Vracia len COUNT!
}
```

**Riešenie:**
- Implementované v `SPECIFIKACIA_SORT_FILTER_SEARCH.md` + Fáza 2

---

### 3. Sort funguje ale nevyvoláva UI refresh event ❌ HIGH

**Súbor:** `Features/Sort/Services/SortService.cs:312`

**Problém:**
```csharp
public async Task<bool> SortByColumnAsync(string columnName, SortDirection direction, ...)
{
    var sortedRows = /* LINQ sort */ ;
    await _rowStore.ReplaceAllRowsAsync(sortedRows, cancellationToken);

    // ❌ CHÝBA: UI refresh event trigger
    // ❌ CHÝBA: _uiNotificationService.NotifyDataRefreshAsync()

    return true;
}
```

**Riešenie:**
- Implementované v `SPECIFIKACIA_SORT_FILTER_SEARCH.md` + Fáza 2

---

### 4. Search Highlighting a Navigation sú TODO ⚠️ HIGH

**Súbor:** `Features/Search/Services/SearchService.cs:896-1065`

**Problém:**
```csharp
public async Task<Result> HighlightSearchMatchesAsync(...)
{
    await Task.CompletedTask; // Placeholder
    // TODO: Implement actual highlighting logic when UI layer is connected
    return Result.Success();
}

public async Task<Result> GoToNextMatchAsync(...)
{
    await Task.CompletedTask; // Placeholder
    // TODO: Implement actual navigation when UI layer is connected
    return Result.Success();
}
```

**Riešenie:**
- Implementované v `SPECIFIKACIA_SORT_FILTER_SEARCH.md`

---

### 5. SearchResult neobsahuje RowId (ULID) ⚠️ MEDIUM

**Súbor:** `Features/Search/Models/SearchResult.cs`

**Problém:**
- SearchResult obsahuje len `RowIndex`, nie `RowId` (ULID)
- Pri filtered view: RowIndex sa mení, treba stable identifier

**Riešenie:**
```csharp
public record SearchResult
{
    public int RowIndex { get; init; }
    public string RowId { get; init; } = ""; // ✅ PRIDANÉ
    public string ColumnName { get; init; } = "";
    public object? Value { get; init; }
    // ...
}
```

---

### 6. Virtualizácia a Pagination neexistujú ❌ CRITICAL

**Súbor:** `UIControls/DataGridCellsView.cs`, `ViewModels/DataGridViewModel.cs`

**Problém:**
- `BulkObservableCollection<DataGridRowViewModel> Rows` môže obsahovať 10M+ riadkov
- UI rendering potenciálne renderuje všetky riadky → performance katastrofa

**Riešenie:**
- Pagination state management (Fáza 5)
- Pagination UI controls (page numbers, Next/Back)
- Max 1000 rows per page

---

### 7. UI Controls TODO komentáre všade ⚠️ MEDIUM

**Súbor:** `UIControls/AdvancedDataGridControl.cs:218, 227, 245, 254`

**Problém:**
```csharp
private void OnSearchRequested(object? sender, EventArgs e)
{
    _logger?.LogInformation("Search requested with text: {SearchText}", ViewModel.SearchPanel.SearchText);
    // TODO: Implement search via Facade API
}

private void OnApplyFiltersRequested(object? sender, EventArgs e)
{
    _logger?.LogInformation("Apply filters requested");
    // TODO: Apply filters via Facade API
}
```

**Riešenie:**
- Pripojiť event handlers k Facade API metódam (Fáza 2)

---

### 8. Facade Reference nie je dostupná v AdvancedDataGridControl ⚠️ MEDIUM

**Súbor:** `UIControls/AdvancedDataGridControl.cs:1062`

**Problém:**
```csharp
private async void OnColumnHeaderClicked(object? sender, ColumnHeaderViewModel header)
{
    _logger?.LogInformation("Sort requested for column: {ColumnName}", header.ColumnName);

    // ❌ TODO: Get facade reference (via constructor injection or service provider)
    // await _facade.Sorting.ToggleSortDirectionAsync(header.ColumnName);
}
```

**Riešenie:**
```csharp
public AdvancedDataGridControl(
    DataGridViewModel viewModel,
    IAdvancedDataGridFacade facade, // ✅ Pridať parameter
    ILogger<AdvancedDataGridControl>? logger = null)
{
    _facade = facade ?? throw new ArgumentNullException(nameof(facade));
    // ...
}
```

---

### 9. ValidationDeletionService.cs je unstaged nový súbor ⚠️ LOW

**Súbor:** `Features/Validation/Services/ValidationDeletionService.cs`

**Problém:**
- Git status ukazuje nový súbor ktorý nie je staged
- Neúplná implementácia validation deletion funkcionality?

**Riešenie:**
- Overiť či je funkcionalita dokončená
- Pridať do git (Fáza 4)

---

### 10. InternalUIOperationHandler - Logger null check inconsistency ⚠️ LOW

**Súbor:** `UIAdapters/WinUI/InternalUIOperationHandler.cs:35`

**Problém:**
```csharp
public InternalUIOperationHandler(
    ILogger<InternalUIOperationHandler>? logger = null) // ✅ Optional parameter
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // ❌ But required!
}
```

**Riešenie:**
```csharp
_logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<InternalUIOperationHandler>.Instance;
```

---

### 11. LINQ Performance nad 10M rows ❌ CRITICAL

**Problém:**
- `OrderBy`, `Where`, `Contains` nad 10M `Dictionary<string, object?>` v RAM
- O(n log n) sort v LINQ: 20-40 sekúnd
- O(n * m) filter: 15-30 sekúnd

**Riešenie:**
- Delegovať na SQLite (Fáza 2)

---

## 🎯 QUICK START GUIDE

### Pre implementátora

1. **Prečítaj dokumenty v poradí:**
   - `docu_MasterIndex.md` (tento súbor)
   - `docu_HybridSQLiteArchitecture.md`
   - `docu_DatabaseManagement.md`
   - `docu_SortFilterSearchIntegration.md`
   - `docu_AddRowModalSpecification.md`
   - `docu_ImplementationRoadmap.md`

2. **Vytvor feature branch:**
   ```bash
   git checkout -b feature/hybrid-sqlite-migration
   ```

3. **Install dependencies:**
   ```bash
   dotnet add package Microsoft.Data.Sqlite --version 8.0.0
   ```

4. **Začni s Fázou 0** podľa `docu_ImplementationRoadmap.md`

5. **Postupuj fáza po fáze**, píš unit testy priebežne

---

## 📞 SUPPORT

Pri problémoch alebo otázkach:

1. **Prečítaj znova príslušný dokument**
2. **Skontroluj implementačný plán** (`docu_ImplementationRoadmap.md`)
3. **Run unit testy** pre konkrétny modul
4. **Vytvor issue** s popisom problému

---

## 📄 CHANGELOG

### Version 1.0 (19.10.2025)
- Initial documentation creation
- 5 hlavných dokumentov vytvorených
- Implementačný plán definovaný (25-33 dní)
- 11 chýb/problémov identifikovaných

---

**Koniec Master Index dokumentu**

**ÚSPEŠNÝ ŠTART PROJEKTU!** 🚀

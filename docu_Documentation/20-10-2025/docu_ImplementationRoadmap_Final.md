# Implementation Roadmap - Final Version

**Verzia:** Final v2.0
**Dátum:** 2025-10-20
**Celkový čas:** 81-106 hodín (8-9 týždňov)

---

## 📋 Prehľad

Tento dokument poskytuje detailný plán implementácie všetkých úloh s časovými odhadmi, závislosti a očakávanými výsledkami.

---

## 🎯 Priority a Časové Odhady

### 🔴 PRIORITA 1 - Cleanup + Insert Row (23-30 h)

| Úloha | Čas | Súbory | Dependencies |
|-------|-----|--------|--------------|
| 1.1 Cleanup pred export | 2-3 h | ExportService.cs | 1.2, 1.3 |
| 1.2 Grace Period logika | 6-8 h | SmartOperationService.cs, SmartOperationTypes.cs | Žiadne |
| 1.3 Config (UserEmptyRowGracePeriod) | 1 h | AdvancedDataGridOptions.cs | Žiadne |
| 1.4 InsertRow ➕ special column | 5-7 h | SpecialColumnCellControl.cs, Enums.cs, DataGridViewModel.cs | Žiadne |
| 1.5 Insert Row API + Context Menu | 9-11 h | DataGridRows.cs, IAdvancedDataGridFacade.cs, AdvancedDataGridControl.cs | 1.4 |
| **SUBTOTAL P1** | **23-30 h** | | |

### 🔴 PRIORITA 2 - UI Virtualizácia (25-33 h)

| Úloha | Čas | Súbory | Dependencies |
|-------|-----|--------|--------------|
| 2.1A ViewportManager | 6-8 h | ViewportManager.cs (NOVÝ) | Žiadne |
| 2.1B ElementFactory | 4-6 h | DataGridElementFactory.cs (NOVÝ) | 2.1A |
| 2.1C DataGridCellsView rewrite | 8-10 h | DataGridCellsView.cs | 2.1A, 2.1B |
| 2.1D Dispose Pattern | 2-3 h | DataGridRowViewModel.cs | Žiadne |
| 2.1E Integration & Testing | 2-3 h | AdvancedDataGridControl.cs | 2.1A-D |
| 2.2 BrushPool | 4-5 h | BrushPool.cs (NOVÝ), CellViewModel.cs, ThemeManager.cs | Žiadne |
| 2.3 Dispose Pattern (extra) | 3-4 h | CellViewModel.cs | Žiadne |
| **SUBTOTAL P2** | **25-33 h** | | |

### 🔴 PRIORITA 3 - Hybrid SQLite (26-34 h)

| Úloha | Čas | Súbory | Dependencies |
|-------|-----|--------|--------------|
| 3.1A SQLite Schema | 3-4 h | HybridRowStore.cs (schema) | Žiadne |
| 3.1B HybridRowStore implementation | 14-18 h | HybridRowStore.cs, LRUCache.cs (NOVÉ) | 3.1A |
| 3.1C Refactor GetAllRowsAsync calls | 4-6 h | DataGridRows.cs, SmartOperationService.cs | 3.1B |
| 3.1D Migration (ServiceRegistration) | 2-3 h | ServiceRegistration.cs | 3.1B, 3.1C |
| 3.1E Optimizations | 3-4 h | HybridRowStore.cs (indexes, batch) | 3.1D |
| 3.1F Testing | 2-3 h | Test projects | 3.1E |
| **SUBTOTAL P3** | **26-34 h** | | |

### 🟡 PRIORITA 4 - Ďalšie Optimalizácie (7-9 h)

| Úloha | Čas | Súbory | Dependencies |
|-------|-----|--------|--------------|
| 4.1 ArrayPool | 3-4 h | ImportService.cs, ExportService.cs | Žiadne |
| 4.2 WeakEventManager | 4-5 h | WeakEventManager.cs (NOVÝ), ThemeManager.cs, CellViewModel.cs | Žiadne |
| **SUBTOTAL P4** | **7-9 h** | | |

### 📊 CELKOVÝ SÚHRN

| Priorita | Oblast | Čas | Dopad |
|----------|--------|-----|-------|
| 🔴 P1 | Cleanup + Insert Row | 23-30 h | Funkčnosť |
| 🔴 P2 | UI Virtualizácia | 25-33 h | 70-80% úspora |
| 🔴 P3 | Hybrid SQLite | 26-34 h | 10M rows support |
| 🟡 P4 | Optimalizácie | 7-9 h | 5-10% úspora |
| **SPOLU** | | **81-106 h** | **~90% úspora** |

---

## 🗓️ Implementačné Poradie (8-9 týždňov)

### Týždeň 1-2: UI Virtualizácia (18-24 h)

**Prečo najprv UI virtualizácia?**
- Najväčší dopad na pamäť (70-80% úspora)
- Žiadne dependencies
- Umožňuje testovanie s 1000+ riadkami

**Úlohy:**
1. ViewportManager.cs - POCO cache + ViewModel lifecycle (6-8 h)
2. DataGridElementFactory.cs - Element recycling (4-6 h)
3. DataGridCellsView.cs - ItemsRepeater rewrite (8-10 h)
4. DataGridRowViewModel.cs - Dispose pattern (2-3 h)
5. Integration & Testing (2-3 h)

**Deliverable:**
- ✅ ItemsRepeater funguje
- ✅ Viewport limit 1000 rows
- ✅ Element recycling (max 50 pool)
- ✅ Pamäť: 100 rows = 460 MB → 35-55 MB

**Testing:**
```csharp
// Test: Load 10,000 rows, scroll to 5000
// Assert: Memory < 200 MB
// Assert: Only ~1000 ViewModels exist
```

---

### Týždeň 3: Cleanup + BrushPool (13-17 h)

**Úlohy:**
1. RowManagementConfiguration - Grace Period fields (1 h)
2. SmartOperationService - Grace Period logika (6-8 h)
3. AdvancedDataGridOptions - UserEmptyRowGracePeriod (1 h)
4. ExportService - Cleanup pred export (2-3 h)
5. BrushPool.cs - SolidColorBrush deduplication (4-5 h)
6. CellViewModel.cs - BrushPool integration (1 h)

**Deliverable:**
- ✅ Grace Period funguje (3 min default)
- ✅ Cleanup pred export
- ✅ BrushPool: 1.5 MB → 500 bytes
- ✅ Validation success = default border color (NIE zelená!)

**Testing:**
```csharp
// Test: User inserts empty row
// Wait 2 min → cleanup → Assert: Row exists
// Wait 2 more min → cleanup → Assert: Row deleted
```

---

### Týždeň 4-5: Insert Row (14-18 h)

**Úlohy:**
1. Enums.cs - InsertRow = 4, DeleteRow = 5 (1 h)
2. InsertRowRequestedEventArgs.cs - Event args (1 h)
3. SpecialColumnCellControl.cs - CreateInsertRowControl() (2-3 h)
4. DataGridViewModel.cs - Column order 1,2,3,4,5,6 (2 h)
5. IAdvancedDataGridFacade.cs - Signatúry (1 h)
6. DataGridRows.cs - STREAMING variants (InsertRowAtAsync) (5-7 h)
7. AdvancedDataGridControl.cs - Context Menu + Continuous Block (3-4 h)
8. Toolbar + Keyboard shortcuts (2-3 h)

**Deliverable:**
- ✅ ➕ Special column funguje
- ✅ Context Menu: Continuous block logic
- ✅ ŽIADNY DIALOG pri insert multiple
- ✅ API: InsertRowAfterAsync, InsertRowBeforeAsync, InsertRowAtTopAsync
- ✅ STREAMING variants (nie GetAllRowsAsync!)

**Testing:**
```csharp
// Test: Select [3,5,6], context menu on 5 → Insert Below
// Assert: 2 rows inserted (continuous block [5,6])

// Test: Select [3,5], context menu on 5 → Insert Below
// Assert: 1 row inserted (non-continuous)
```

---

### Týždeň 6-8: Hybrid SQLite (26-34 h)

**Prečo až teraz?**
- Najkomplexnejšia úloha
- Vyžaduje refaktoring existujúceho kódu
- Testovanie s 10M rows trvá dlhšie

**Úlohy:**

**Týždeň 6 (8-10 h):**
1. SQLite Schema design (3-4 h)
2. HybridRowStore.cs - Basic CRUD (5-6 h)

**Týždeň 7 (10-14 h):**
3. LRUCache.cs implementation (2-3 h)
4. HybridRowStore - StreamRowsAsync, InsertRowAtAsync (4-6 h)
5. Refactor GetAllRowsAsync calls (4-6 h)
   - DataGridRows.cs
   - SmartOperationService.cs
   - Všetky ostatné

**Týždeň 8 (8-10 h):**
6. ServiceRegistration.cs - Migration (2-3 h)
7. Optimizations (indexes, batch, cache warming) (3-4 h)
8. Testing s 10M rows (2-3 h)

**Deliverable:**
- ✅ HybridRowStore funguje
- ✅ GetAllRowsAsync ELIMINATED (throws exception!)
- ✅ 10M rows support (~700 MB RAM)
- ✅ LRU cache hit rate >90%
- ✅ POCO cache spolupracuje s SQLite

**Testing:**
```csharp
// Test: Import 10M rows
// Assert: RAM < 1.5 GB
// Assert: SQLite file ~1 TB
// Assert: GetAllRowsAsync() throws NotSupportedException
// Assert: Scroll performance smooth
```

---

### Týždeň 9: Optimalizácie (7-9 h)

**Voliteľné** - Možno presunúť neskôr

**Úlohy:**
1. ArrayPool.cs - Dictionary/DataTable buffering (3-4 h)
2. WeakEventManager.cs - Memory leak prevention (4-5 h)

**Deliverable:**
- ✅ ArrayPool: LOH alokácie minimalizované
- ✅ WeakEventManager: Event leaks predchádzané
- ✅ ~2-5% extra úspora

---

## 📈 Očakávané Výsledky (Postupne)

### Po Týždni 2 (UI Virtualizácia)

| Riadky | PRED | PO | Úspora |
|--------|------|-----|--------|
| 100 | 460 MB | 35-55 MB | 88-92% |
| 1000 | ~4.6 GB | 120-160 MB | 96-97% |
| 10000 | ~46 GB | 500-700 MB | 98.5% |

### Po Týždni 3 (+ BrushPool)

- ✅ Brush deduplikácia: +15-20% úspora
- ✅ Grace Period: Auto cleanup funguje
- ✅ Export: Vždy čisté dáta

### Po Týždni 5 (+ Insert Row)

- ✅ Plná Insert Row funkcionalita
- ✅ Context Menu s continuous block logic
- ✅ Streaming API (nie GetAllRows!)

### Po Týždni 8 (+ Hybrid SQLite)

| Riadky | RAM | Disk |
|--------|-----|------|
| 100 | 40-60 MB | ~10 MB |
| 1000 | 120-160 MB | ~100 MB |
| 10000 | 500-700 MB | ~1 GB |
| 100000 | 600-900 MB | ~10 GB |
| **10M** | **700 MB-1.2 GB** | **~1 TB** |

### Po Týždni 9 (+ Všetky Optimalizácie)

- ✅ **Celková úspora: ~90%**
- ✅ **10M rows: REALIZOVATEĽNÉ** (PRED: NEMOŽNÉ)
- ✅ **Import/Export: 2-5% rýchlejšie**

---

## 🔗 Dependency Graph

```
Týždeň 1-2: UI Virtualizácia
    └─> ViewportManager (6-8h)
        └─> ElementFactory (4-6h)
            └─> DataGridCellsView rewrite (8-10h)
                └─> Dispose Pattern (2-3h)
                    └─> Integration (2-3h)

Týždeň 3: Cleanup + BrushPool
    ├─> Grace Period Config (1h)
    ├─> Grace Period Logic (6-8h)
    │   └─> Cleanup pred export (2-3h)
    └─> BrushPool (4-5h)

Týždeň 4-5: Insert Row
    ├─> Special Column (5-7h)
    └─> API + Context Menu (9-11h)

Týždeň 6-8: Hybrid SQLite
    └─> Schema (3-4h)
        └─> HybridRowStore (14-18h)
            └─> Refactor GetAllRows (4-6h)
                └─> Migration (2-3h)
                    └─> Optimizations (3-4h)
                        └─> Testing (2-3h)

Týždeň 9: Optimalizácie (voliteľné)
    ├─> ArrayPool (3-4h)
    └─> WeakEventManager (4-5h)
```

---

## ⚠️ Kritické Poznámky

### 1. Cleanup Logika

❌ **NESPRÁVNE:** Odstrániť cleanup z Import/Add/Paste
✅ **SPRÁVNE:** ZACHOVAŤ cleanup + PRIDAŤ pred export

### 2. Column Order

❌ **NESPRÁVNE:** 1,2,6,5,3,4
✅ **SPRÁVNE:** 1=RowNumber, 2=Checkbox, 3=DATA, 4=ValidationAlerts(vždy!), 5=InsertRow, 6=DeleteRow

### 3. Validation Success Color

❌ **NESPRÁVNE:** Zelená
✅ **SPRÁVNE:** Rovnaká ako default border (gray)

### 4. Context Menu Insert

❌ **NESPRÁVNE:** Dialog "Insert Multiple Rows..."
✅ **SPRÁVNE:** Automatický count = continuous block size

### 5. GetAllRowsAsync

❌ **NESPRÁVNE:** Mark as TODO for later
✅ **SPRÁVNE:** ELIMINATE immediately (throw exception!)

### 6. POCO Cache s SQLite

❌ **NESPRÁVNE:** Nepotrebujem POCO cache ak mám SQLite
✅ **SPRÁVNE:** ÁNO potrebujem - rôzne účely (SQLite=disk, POCO=viewport)

### 7. BrushPool s API Colors

❌ **NESPRÁVNE:** Nepotrebujem BrushPool ak sú farby nastaviteľné
✅ **SPRÁVNE:** ÁNO potrebujem - farby sa opakovajú v praxi

---

## 🧪 Testing Strategy

### Týždeň 1-2: UI Virtualization Tests

```csharp
[Test] ViewportManager_LoadsCorrectRange()
[Test] ElementFactory_RecyclesElements()
[Test] ItemsRepeater_LimitsTo1000ViewModels()
[Test] Scroll_Performance_SmoothWith10KRows()
```

### Týždeň 3: Cleanup Tests

```csharp
[Test] GracePeriod_UserInserted_WaitsBeforeDelete()
[Test] GracePeriod_AutoGenerated_DeletesImmediately()
[Test] CleanupBeforeExport_RemovesEmptyRows()
[Test] BrushPool_DeduplicatesColors()
```

### Týždeň 4-5: Insert Row Tests

```csharp
[Test] SpecialColumn_InsertsRowBelow()
[Test] ContextMenu_ContinuousBlock_InsertsCorrectCount()
[Test] InsertRowAtAsync_StreamingVariant_NoGetAllRows()
[Test] InsertRowMetadata_IsUserInserted()
```

### Týždeň 6-8: SQLite Tests

```csharp
[Test] HybridRowStore_Loads10MRows_RAMUnder1_5GB()
[Test] StreamRowsAsync_ConstantMemory()
[Test] GetAllRowsAsync_ThrowsException()
[Test] InsertRowAtAsync_UpdatesIndicesCorrectly()
[Test] LRUCache_HitRate_Above90Percent()
```

### Týždeň 9: Optimization Tests

```csharp
[Test] ArrayPool_ReducesLOHAllocations()
[Test] WeakEventManager_PreventsMemoryLeaks()
```

---

## 📋 Checklist pre Každý Týždeň

### Pred Začatím

- [ ] Review dokumentácie pre tento týždeň
- [ ] Skontrolovať dependencies (predchádzajúce týždne dokončené?)
- [ ] Vytvoriť feature branch: `feature/week-X-description`

### Počas Implementácie

- [ ] Použiť nomenclatúru z dokumentácie
- [ ] Logovať všetky kritické operácie
- [ ] Pridať XML komentáre k public API
- [ ] Unit testy pre každú úlohu

### Po Dokončení

- [ ] Všetky testy prechádzajú
- [ ] Code review (peer review)
- [ ] Update dokumentácie (ak potrebné)
- [ ] Merge do main branch
- [ ] Tag release: `v2.0.0-week-X`

---

## 🎯 Success Criteria

### Týždeň 2

- ✅ 10K rows load < 200 MB RAM
- ✅ Smooth scroll bez lagov
- ✅ ViewportManager limituje na 1000 ViewModels

### Týždeň 3

- ✅ Grace period funguje (3 min)
- ✅ Cleanup pred export
- ✅ BrushPool: <10 unique colors pre 10K cells

### Týždeň 5

- ✅ ➕ special column funguje
- ✅ Context menu: Continuous block logic správna
- ✅ ŽIADNE GetAllRowsAsync volania v Insert API

### Týždeň 8

- ✅ 10M rows load < 1.5 GB RAM
- ✅ GetAllRowsAsync throws exception
- ✅ SQLite file ~1 TB
- ✅ Scroll performance smooth

### Týždeň 9

- ✅ Všetky optimalizácie implementované
- ✅ Celková úspora ~90%
- ✅ No memory leaks

---

## 📞 Support & Questions

Ak počas implementácie narazíte na problémy:

1. Skontrolovať príslušný .md dokument pre detail
2. Skontrolovať [docu_MasterIndex_Final.md](./docu_MasterIndex_Final.md) pre odkazy
3. Review kritických poznámok v tomto dokumente (sekcia ⚠️)

---

**Posledná aktualizácia:** 2025-10-20
**Status:** Ready for Implementation
**Odhadovaný completion:** 8-9 týždňov (pri 10-12 h/týždeň)

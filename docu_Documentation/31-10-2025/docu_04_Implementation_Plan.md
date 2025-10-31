# 📋 IMPLEMENTAČNÝ PLÁN - PRIORITY A POSTUPNOSŤ

**Dátum:** 31. október 2025
**Projekt:** AdvancedWinUiDataGrid - WinUI 3 Component
**Účel:** Návod na implementáciu všetkých 3 identifikovaných problémov

---

## 🎯 PREHĽAD PROBLÉMOV A PRIORÍT

| #  | Problém                        | Priorita | Zložitosť  | Time Estimate | Risk Level |
|----|--------------------------------|----------|------------|---------------|------------|
| 2  | Sort nefunguje                 | ⚡ HIGH  | SIMPLE     | 1 hod         | LOW        |
| 1  | Insert Above/Below nefunguje   | 🔧 MED   | MEDIUM     | 2 hod         | MEDIUM     |
| 3  | Filter nefunguje               | 🎨 LOW   | COMPLEX    | 4-5 hod       | MED-HIGH   |

**Doporučené poradie:** 2 → 1 → 3 (Sort first, Insert second, Filter last)

---

## ⚡ FÁZA 1: SORT (NAJJEDNODUCHŠIE)

**Cieľ:** Aktivovať sort funkcionalitu pri kliknutí na header stĺpca

### **1.1 Vytvoriť InternalUISortHandler**

**Súbor:** `UIAdapters/WinUI/InternalUISortHandler.cs` (NEW)

**Kroky:**
1. Vytvoriť nový súbor v zložke `UIAdapters/WinUI/`
2. Skopírovať implementáciu z dokumentu `docu_02_Problem_Sort.md` (KROK 1)
3. Overiť using statements:
   ```csharp
   using Microsoft.Extensions.Logging;
   using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
   ```

**Implementácia:**
- Trieda: `InternalUISortHandler : IDisposable`
- Constructor: Subscribe na `_viewModel.SortRequested` event
- Handler: `OnSortRequested()` - volá `_facade.Sorting.SortByColumnAsync()`
- Dispose: Unsubscribe from event

**Čas:** 15 minút

---

### **1.2 Zaregistrovať handler v AdvancedDataGridFacade**

**Súbor:** `AdvancedDataGridFacade.cs` (MODIFY)

**Kroky:**
1. Pridať field:
   ```csharp
   private readonly InternalUISortHandler? _sortHandler;
   ```

2. V constructore, po inicializácii `_updateHandler`:
   ```csharp
   // Initialize automatic sort handler (header click → sort operation)
   if (_uiControl != null)
   {
       var sortLogger = _loggerFactory?.CreateLogger<InternalUISortHandler>();
       _sortHandler = new InternalUISortHandler(this, _viewModel, sortLogger);
       _logger?.LogInformation("InternalUISortHandler initialized");
   }
   ```

3. V `Dispose()` metóde:
   ```csharp
   _sortHandler?.Dispose();
   ```

**Čas:** 5 minút

---

### **1.3 Verifikovať SortService implementáciu**

**Súbor:** `Services/SortService.cs` (VERIFY)

**Kroky:**
1. Otvoriť `SortService.cs`
2. Nájsť metódu `SortByColumnAsync()`
3. Overiť, že na konci volá:
   ```csharp
   await _viewModel.ForceCompleteReloadAsync();
   ```
4. Ak chýba: Pridať pred `return OperationResult.Success();`

**Čas:** 5 minút

---

### **1.4 Build a test**

**Kroky:**
1. Build solution (F6)
2. Overiť: 0 errors
3. Spustiť aplikáciu
4. Načítať dáta
5. Kliknúť na header stĺpca → vybrať "Sort Ascending"
6. **Verifikovať:** Dáta zoradené A→Z
7. Kliknúť znova → vybrať "Sort Descending"
8. **Verifikovať:** Dáta zoradené Z→A
9. Skontrolovať logy:
   ```
   [INFO] InternalUISortHandler activated
   [INFO] AUTO-SORT: Sort requested for column 'Name', direction 'Ascending'
   [INFO] ✅ AUTO-SORT: Sort completed successfully
   ```

**Čas:** 15 minút

**TOTAL FÁZA 1:** ~40 minút

---

## 🔧 FÁZA 2: INSERT ABOVE/BELOW (STREDNE ZLOŽITÉ)

**Cieľ:** Opraviť context menu Insert Above/Below funkcionalitu

### **2.1 Opraviť DataGridCellsView - získať RowId zo SelectedCells**

**Súbor:** `DataGridCellsView.cs` (MODIFY)

**Kroky:**
1. Nájsť metódu `OnRowContextMenuInsertAbove()` (cca line 830)
2. Na začiatku metódy pridať:
   ```csharp
   // Validate selected cells
   if (ViewModel.SelectedCells.Count == 0)
   {
       _logger.LogWarning("Context menu Insert Above - no selected cells");
       return;
   }

   // Get RowId from first selected cell
   var firstSelectedCell = ViewModel.SelectedCells.First();
   string? rowId = firstSelectedCell.RowId;
   int rowIndex = firstSelectedCell.RowIndex;

   if (string.IsNullOrEmpty(rowId))
   {
       _logger.LogError("Context menu Insert Above - selected cell has no RowId");
       return;
   }
   ```

3. V `for` loop, zmeniť:
   ```csharp
   // BEFORE:
   var eventArgs = new InsertRowRequestedEventArgs(globalIndex, null, "Above");

   // AFTER:
   var eventArgs = new InsertRowRequestedEventArgs(rowIndex, rowId, "Above");
   ```

4. **ROVNAKÁ ZMENA** v `OnRowContextMenuInsertBelow()` (line 915):
   - Získať RowId zo SelectedCells
   - Poslať `new InsertRowRequestedEventArgs(rowIndex, rowId, "Below")`

**Čas:** 20 minút

---

### **2.2 Implementovať VirtualInsertEmptyRowBeforeAsync API**

**Súbor:** `IDataGridRows.cs` (MODIFY)

**Kroky:**
1. Otvoriť `Interfaces/IDataGridRows.cs`
2. Pridať metódu signature:
   ```csharp
   /// <summary>
   /// Virtually inserts an empty row BEFORE the specified row (identified by RowId).
   /// </summary>
   Task<OperationResult> VirtualInsertEmptyRowBeforeAsync(
       string rowId,
       CancellationToken cancellationToken = default);
   ```

**Čas:** 2 minúty

---

**Súbor:** `DataGridRows.cs` (MODIFY)

**Kroky:**
1. Otvoriť `Services/DataGridRows.cs`
2. Pridať implementáciu metódy `VirtualInsertEmptyRowBeforeAsync()`
3. Skopírovať kód z dokumentu `docu_01_Problem_Insert_Above_Below.md` (KROK 2)
4. Overiť logiku:
   - Get target row by RowId
   - Get its __rowNumber
   - Find all rows with __rowNumber >= target
   - Shift them down (__rowNumber++)
   - Insert new empty row at target position

**Čas:** 30 minút

---

### **2.3 Upraviť InternalUIOperationHandler - podporiť Position="Above"**

**Súbor:** `InternalUIOperationHandler.cs` (MODIFY)

**Kroky:**
1. Nájsť metódu `OnInsertRowRequested()` (line 102)
2. Nájsť blok s `else if (args.RowIndex >= 0)` (index-based insert DISABLED)
3. **ODSTRÁNIŤ CELÝ BLOK** (lines 131-139)
4. Zmeniť logiku na:
   ```csharp
   if (string.IsNullOrEmpty(args.RowId))
   {
       _logger.LogError("AUTO-INSERT: RowId is null - cannot perform insert");
       return;
   }

   // DUAL POSITION SUPPORT: Above vs Below
   if (args.Position == "Above")
   {
       _logger.LogInformation("AUTO-INSERT (ABOVE): Inserting before RowId={RowId}", args.RowId);
       var result = await _facade.Rows.VirtualInsertEmptyRowBeforeAsync(args.RowId, CancellationToken.None);
       // ... log result ...
   }
   else // Position="Below" or null
   {
       _logger.LogInformation("AUTO-INSERT (BELOW): Inserting after RowId={RowId}", args.RowId);
       var result = await _facade.Rows.VirtualInsertEmptyRowAfterAsync(args.RowId, CancellationToken.None);
       // ... log result ...
   }
   ```

**Čas:** 15 minút

---

### **2.4 Build a test**

**Kroky:**
1. Build solution (F6)
2. Overiť: 0 errors
3. Spustiť aplikáciu, ísť na page 7
4. Pravý klik na riadok 5 → "Insert 2 rows above"
5. **Verifikovať:** 2 prázdne riadky vložené NAD riadok 5
6. Pravý klik na riadok 8 → "Insert 3 rows below"
7. **Verifikovať:** 3 prázdne riadky vložené POD riadok 8
8. Skontrolovať logy:
   ```
   [INFO] Context menu Insert Above triggered for row 5, RowId=01HQX...
   [INFO] AUTO-INSERT (ABOVE): Inserting before RowId=01HQX...
   [INFO] VirtualInsertBefore: Found 15 rows to shift down
   [INFO] ✅ VirtualInsertBefore: New empty row inserted at __rowNumber=5
   ```

**Čas:** 20 minút

**TOTAL FÁZA 2:** ~1.5 hodiny

---

## 🎨 FÁZA 3: FILTER (NAJZLOŽITEJŠIE)

**Cieľ:** Implementovať checkbox a regex filter UI

### **3.1 Pridať FilterFlyoutService do DataGridViewModel**

**Súbor:** `DataGridViewModel.cs` (MODIFY)

**Kroky:**
1. Pridať property:
   ```csharp
   /// <summary>
   /// Filter flyout service for checkbox and regex filtering.
   /// Injected by AdvancedDataGridFacade during initialization.
   /// </summary>
   public IFilterFlyoutService? FilterFlyoutService { get; set; }
   ```

**Čas:** 2 minúty

---

**Súbor:** `AdvancedDataGridFacade.cs` (MODIFY)

**Kroky:**
1. V constructore, po vytvorení `_filterFlyoutService`:
   ```csharp
   // Inject FilterFlyoutService into ViewModel
   _viewModel.FilterFlyoutService = _filterFlyoutService;
   _logger?.LogInformation("FilterFlyoutService injected into ViewModel");
   ```

**Čas:** 2 minúty

---

### **3.2 Implementovať checkbox filter handler v HeadersRowView**

**Súbor:** `HeadersRowView.cs` (MODIFY)

**Kroky:**
1. Nájsť `filterCheckboxItem.Click += ...` handler (line 785)
2. Nahradiť TODO kód:
   ```csharp
   filterCheckboxItem.Click += async (s, e) =>
   {
       _logger?.LogInformation("Filter (Checkbox mode) selected for column {ColumnName}", header.ColumnName);
       flyout.Hide();

       if (_viewModel.FilterFlyoutService == null)
       {
           _logger?.LogError("FilterFlyoutService not available");
           return;
       }

       try
       {
           var uniqueValues = await _viewModel.FilterFlyoutService.LoadUniqueValuesAsync(
               header.ColumnName, CancellationToken.None);

           if (uniqueValues.Count == 0)
           {
               _logger?.LogWarning("No unique values found");
               return;
           }

           await ShowCheckboxFilterFlyoutAsync(header, uniqueValues);
       }
       catch (Exception ex)
       {
           _logger?.LogError(ex, "Failed to load unique values");
       }
   };
   ```

**Čas:** 10 minút

---

### **3.3 Vytvoriť checkbox filter flyout UI**

**Súbor:** `HeadersRowView.cs` (MODIFY)

**Kroky:**
1. Pridať novú metódu `ShowCheckboxFilterFlyoutAsync()`
2. Skopírovať implementáciu z dokumentu `docu_03_Problem_Filter.md` (KROK 3)
3. Overiť komponenty:
   - ContentDialog s checkboxami
   - ScrollViewer (max height 400px)
   - "Select All" checkbox
   - Individual value checkboxes (limit 100)
   - "Apply Filter" primary button
   - "Clear Filter" secondary button

**Čas:** 60 minút (komplexné UI)

---

### **3.4 Implementovať regex filter handler v HeadersRowView**

**Súbor:** `HeadersRowView.cs` (MODIFY)

**Kroky:**
1. Nájsť `filterRegexItem.Click += ...` handler (line 793)
2. Nahradiť TODO kód:
   ```csharp
   filterRegexItem.Click += async (s, e) =>
   {
       _logger?.LogInformation("Filter (Regex mode) selected for column {ColumnName}", header.ColumnName);
       flyout.Hide();

       if (_viewModel.FilterFlyoutService == null)
       {
           _logger?.LogError("FilterFlyoutService not available");
           return;
       }

       await ShowRegexFilterDialogAsync(header);
   };
   ```

**Čas:** 5 minút

---

### **3.5 Vytvoriť regex filter dialog UI**

**Súbor:** `HeadersRowView.cs` (MODIFY)

**Kroky:**
1. Pridať novú metódu `ShowRegexFilterDialogAsync()`
2. Skopírovať implementáciu z dokumentu `docu_03_Problem_Filter.md` (KROK 5)
3. Overiť komponenty:
   - ContentDialog s TextBox
   - Pattern input field
   - Case sensitive checkbox
   - Examples text
   - Regex validation
   - "Apply Filter" primary button
   - "Clear Filter" secondary button

**Čas:** 45 minút

---

### **3.6 Verifikovať FilterFlyoutService implementáciu**

**Súbor:** `FilterFlyoutService.cs` (VERIFY)

**Kroky:**
1. Overiť metódy existujú:
   - ✅ `LoadUniqueValuesAsync()`
   - ✅ `ApplyCheckboxFilterAsync()`
   - ✅ `ApplyRegexFilterAsync()`
   - ⚠️ `ClearAllFiltersAsync()` - možno chýba

2. Ak `ClearAllFiltersAsync()` chýba:
   ```csharp
   public async Task<OperationResult> ClearAllFiltersAsync(CancellationToken cancellationToken = default)
   {
       await _rowStore.ClearFilterAsync(cancellationToken);
       _logger?.LogInformation("✅ All filters cleared");

       if (_viewModel != null)
           await _viewModel.ForceCompleteReloadAsync();

       return OperationResult.Success();
   }
   ```

3. Overiť, že `IRowStore` má metódy:
   - `ApplyFilterAsync(string whereClause, CancellationToken)`
   - `ClearFilterAsync(CancellationToken)`

**Čas:** 15 minút

---

### **3.7 Build a test**

**Kroky:**
1. Build solution (F6)
2. Overiť: 0 errors (možno warnings pre unused parameters)

**Test Checkbox Filter:**
3. Spustiť aplikáciu, načítať dáta
4. Kliknúť na header stĺpca "Status" → "Filter (Checkbox)"
5. **Verifikovať:** Zobrazí sa flyout s distinct hodnotami
6. Odznačiť "Inactive", kliknúť "Apply Filter"
7. **Verifikovať:** Grid zobrazuje len Active/Pending riadky
8. Kliknúť znova, vybrať "Clear Filter"
9. **Verifikovať:** Všetky riadky obnovené

**Test Regex Filter:**
10. Kliknúť na header stĺpca "Name" → "Filter (Regex)"
11. Zadať pattern `^A.*`, kliknúť "Apply"
12. **Verifikovať:** Zobrazené len mená začínajúce na 'A'
13. Zadať invalid pattern `[unclosed`, kliknúť "Apply"
14. **Verifikovať:** Dialog zostane otvorený (validation error)

**Logy:**
```
[INFO] Filter (Checkbox mode) selected for column Status
[INFO] Loaded 3 unique values for column Status
[INFO] Applying checkbox filter: column=Status, selectedValues=2/3
[INFO] ✅ Checkbox filter applied successfully
```

**Čas:** 30 minút

**TOTAL FÁZA 3:** ~3 hodiny

---

## 📊 CELKOVÁ TIMELINE

| Fáza | Úloha                          | Čas       | Kumulatívne |
|------|--------------------------------|-----------|-------------|
| 1    | Sort implementation            | 40 min    | 40 min      |
| 2    | Insert Above/Below             | 1.5 hod   | 2 hod 10 min|
| 3    | Filter implementation          | 3 hod     | 5 hod 10 min|
| -    | **TOTAL (bez testingu)**       | **5 hod** | -           |
| -    | **Testing all features**       | **1 hod** | -           |
| -    | **TOTAL (s testingom)**        | **6 hod** | -           |

---

## ✅ CHECKLIST PRE KAŽDÚ FÁZU

### **Pre-implementation:**
- [ ] Prečítať príslušný dokument (docu_01, docu_02, docu_03)
- [ ] Vytvoriť git branch: `feature/sort-insert-filter-fixes`
- [ ] Backup aktuálneho stavu

### **During implementation:**
- [ ] Implementovať všetky kroky postupne
- [ ] Build po každej väčšej zmene (eliminovať compile errors skoro)
- [ ] Commit po každej dokončenej fáze:
  - `git commit -m "feat: implement sort functionality (#2)"`
  - `git commit -m "feat: fix insert above/below context menu (#1)"`
  - `git commit -m "feat: implement checkbox and regex filters (#3)"`

### **Post-implementation:**
- [ ] Vykonať všetky testy z testing checklist
- [ ] Overiť logy (INFO level) na správne funkcie
- [ ] Vytvoriť pull request s odkazom na dokumentáciu
- [ ] Code review (ak applicable)

---

## 🚨 MOŽNÉ PROBLÉMY A RIEŠENIA

### **Problém 1: Build errors - missing using statements**

**Symptóm:**
```
CS0246: The type or namespace name 'IAdvancedDataGridFacade' could not be found
```

**Riešenie:**
Pridať using:
```csharp
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Interfaces;
```

---

### **Problém 2: FilterFlyoutService.ApplyRegexFilterAsync() - SQLite REGEXP not supported**

**Symptóm:**
```
Exception: SQLite Error 1: 'no such function: REGEXP'
```

**Riešenie:**
Zaregistrovať custom REGEXP funkciu v IRowStore (HybridRowStore):
```csharp
// HybridRowStore.cs - RegisterRegexFunction()
private void RegisterRegexFunction()
{
    _sqliteConnection.CreateFunction(
        "regexp",
        (string pattern, string input) =>
        {
            return Regex.IsMatch(input ?? "", pattern);
        });
}
```

---

### **Problém 3: Context menu RowId is still null**

**Symptóm:**
```
[ERROR] AUTO-INSERT: RowId is null - cannot perform insert
```

**Riešenie:**
1. Overiť, že `DataGridCellsView.OnRowContextMenuInsertAbove()` má kód:
   ```csharp
   var firstSelectedCell = ViewModel.SelectedCells.First();
   string? rowId = firstSelectedCell.RowId;
   ```
2. Overiť, že event sa volá:
   ```csharp
   var eventArgs = new InsertRowRequestedEventArgs(rowIndex, rowId, "Above");
   ```
3. Debug: Pridať breakpoint do handlera, skontrolovať `rowId` hodnotu

---

### **Problém 4: Filter UI nezobrazuje checkboxy**

**Symptóm:**
Dialog sa zobrazí prázdny alebo crashne.

**Riešenie:**
1. Overiť, že `LoadUniqueValuesAsync()` vracia neprázdny list
2. Debug: Pridať log pred `for` loop:
   ```csharp
   _logger?.LogDebug("Creating {Count} checkboxes", uniqueValues.Count);
   ```
3. Overiť XamlRoot:
   ```csharp
   dialog.XamlRoot = this.XamlRoot; // CRITICAL!
   ```

---

### **Problém 5: Sort nereloaduje UI**

**Symptóm:**
Sort sa vykoná (logy OK), ale grid zobrazuje staré dáta.

**Riešenie:**
Overiť, že `SortService.SortByColumnAsync()` volá:
```csharp
await _viewModel.ForceCompleteReloadAsync();
```

Alternatívne, InternalUIUpdateHandler by mal detekovať row changes a triggernúť reload automaticky.

---

## 📚 REFERENCIE NA DOKUMENTY

- **[docu_00_Summary.md](docu_00_Summary.md)** - Celkové zhrnutie
- **[docu_01_Problem_Insert_Above_Below.md](docu_01_Problem_Insert_Above_Below.md)** - Insert detaily
- **[docu_02_Problem_Sort.md](docu_02_Problem_Sort.md)** - Sort detaily
- **[docu_03_Problem_Filter.md](docu_03_Problem_Filter.md)** - Filter detaily

---

## 🎓 LESSONS LEARNED

### **Architektúrne vzory použité:**

1. **Internal Handler Pattern** (Sort, Insert, Delete)
   - Centralizovaná event handling logika
   - Separation of concerns: UI ↔ Business logic
   - Konzistentný prístup pre všetky auto-operations

2. **Facade Pattern** (AdvancedDataGridFacade)
   - Unified API pre všetky grid operácie
   - Hiding complexity of services (SortService, FilterFlyoutService, DataGridRows)

3. **Event-driven architecture**
   - UI events (SortRequested, InsertRowRequested) → Handlers → Services
   - Loose coupling medzi komponentami

4. **ViewModel injection**
   - Services accessible via ViewModel properties
   - HeadersRowView → ViewModel.FilterFlyoutService

### **WinUI 3 Best Practices:**

1. **ContentDialog XamlRoot** - VŽDY nastaviť `dialog.XamlRoot = this.XamlRoot`
2. **Async event handlers** - Použiť `async void` pre UI event handlers
3. **ScrollViewer performance** - Limitovať počet child controls (<100)
4. **Checkbox state management** - Použiť `IsChecked = null` pre indeterminate state

---

**END OF DOCUMENT**

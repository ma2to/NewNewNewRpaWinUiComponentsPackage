# ❌ PROBLÉM 2: SORT (ZORAĎOVANIE) NEFUNGUJE

**Dátum:** 31. október 2025
**Priorita:** ⚡ VYSOKÁ (Najjednoduchšia implementácia)
**Status:** Úplne nefunkčné

---

## 🔍 POPIS PROBLÉMU

### **Symptóm:**
Používateľ klikne na header stĺpca v DataGrid, otvorí sa flyout menu s možnosťami:
- "Sort Ascending"
- "Sort Descending"
- "Clear Sort"

Po výbere jednej z týchto možností **NIC SA NESTANE** - dáta zostanú v pôvodnom poradí.

### **Očakávané správanie:**
- Pri výbere "Sort Ascending" sa má grid zoradiť podľa vybraného stĺpca vzostupne (A→Z, 0→9)
- Pri výbere "Sort Descending" sa má grid zoradiť podľa vybraného stĺpca zostupne (Z→A, 9→0)
- Pri výbere "Clear Sort" sa má obnoviť pôvodné poradie (sorting podľa __rowNumber)

### **Aktuálne správanie:**
- Header flyout menu sa zobrazí správne
- Menu items majú správne ikony a texty
- Event `SortRequested` sa vyvolá
- **ALE:** Žiadny handler tento event nepočúva → dáta sa nezoraďujú

---

## 🐛 IDENTIFIKOVANÉ PROBLÉMY

### **Problém 2.1: SortRequested event NEMÁ ŽIADNEHO SUBSCRIBERA**

**Lokácia:** `DataGridViewModel.cs:1815-1840`

**Kód:**
```csharp
// DataGridViewModel.cs:1815
public event EventHandler<SortRequestedEventArgs>? SortRequested;

// DataGridViewModel.cs:1825-1840
public void SetSortDirection(string columnName, string direction)
{
    var header = ColumnHeaders.FirstOrDefault(h => h.ColumnName == columnName);
    if (header == null)
    {
        _logger?.LogWarning("Cannot set sort direction - column {ColumnName} not found", columnName);
        return;
    }

    // Update header visual state (arrows)
    var newDirection = direction switch
    {
        "Ascending" => SortDirection.Ascending,
        "Descending" => SortDirection.Descending,
        _ => SortDirection.None
    };
    header.SortDirection = newDirection;

    // Fire event
    SortRequested?.Invoke(this, new SortRequestedEventArgs(columnName, newDirection));
    //                   ❌ This event has NO subscribers!
}
```

**Root Cause:**
- Event je definovaný a firuje sa správne
- **ALE:** V celom projekte neexistuje ŽIADNE `SortRequested +=` subscription
- Event sa stratí v nicote

**Verifikácia:**
```bash
# Vyhľadané v celom projekte:
grep -r "SortRequested +=" .
# Výsledok: 0 zhôd
```

**Dôsledok:**
- Sort visual state (arrows na headeri) sa správne aktualizuje
- Ale žiadna logika nevykoná skutočné zoradenie dát v IRowStore

---

### **Problém 2.2: AdvancedDataGridControl NEsubscribuje na event**

**Lokácia:** `AdvancedDataGridControl.cs:1-373`

**Analýza:**
```csharp
// AdvancedDataGridControl.cs:193-239 - InitializeSubViews()
private void InitializeSubViews()
{
    // ...

    // HeadersRowView is created
    _headersRowView = new HeadersRowView(ViewModel, ...);
    _headersRowContainer.Child = _headersRowView;

    // DataGridCellsView subscriptions
    _dataCellsView.DeleteRowRequested += OnDeleteRowRequestedInternal;
    _dataCellsView.InsertRowRequested += OnInsertRowRequestedInternal;
    _dataCellsView.RowSelectionChanged += OnRowSelectionChangedInternal;
    _dataCellsView.CellEditCompleted += OnCellEditCompletedInternal;

    // ❌ MISSING: No subscription to ViewModel.SortRequested!
    // Should be: ViewModel.SortRequested += OnSortRequestedInternal;
}
```

**Root Cause:**
- Control inicializuje všetky sub-views
- Subscribuje na delete, insert, cell edit events
- **ALE:** NEsubscribuje na `ViewModel.SortRequested`

**Dôsledok:**
- Aj keby sme pridali subscription v AdvancedDataGridControl, museli by sme manual forward do facade
- Lepšie riešenie: Vytvoriť dedicated handler (InternalUISortHandler)

---

### **Problém 2.3: SortService nie je zapojený do event flow**

**Lokácia:** `Services/SortService.cs`

**Analýza:**
```csharp
// SortService.cs - Existuje plná implementácia
public class SortService : ISortService
{
    public async Task<OperationResult> SortByColumnAsync(
        string columnName,
        bool ascending,
        CancellationToken cancellationToken = default)
    {
        // ✅ Implementation exists!
        // - Gets all rows from IRowStore
        // - Sorts by column value
        // - Reassigns __rowNumber (1-based sequential)
        // - Updates rows in storage
        // - Triggers full reload
    }
}
```

**Root Cause:**
- `SortService.SortByColumnAsync()` má plnú implementáciu
- **ALE:** Nikto tento service nevolá
- Chýba prepojenie: `SortRequested event → SortService.SortByColumnAsync()`

**Dôsledok:**
- Sort infraštruktúra je pripravená, ale nie je použitá
- Dead code

---

## ✅ PROFESIONÁLNE RIEŠENIE

### **ARCHITEKTÚRNY PATTERN: Internal Handler**

**Dôvod výberu:**
- V projekte už existujú podobné handlery:
  - ✅ `InternalUIOperationHandler` - Delete, Insert, Auto-expand
  - ✅ `InternalUIUpdateHandler` - UI refresh, incremental updates
- Konzistencia: Všetky auto-operations používajú rovnaký pattern
- Separation of concerns: UI events ↔ Business logic

---

### **KROK 1: Vytvoriť InternalUISortHandler**

**Nový súbor:** `UIAdapters/WinUI/InternalUISortHandler.cs`

```csharp
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

/// <summary>
/// Internal handler for automatic sort operations triggered by header clicks.
/// Subscribes to SortRequested event from DataGridViewModel and calls facade sort API.
/// ACTIVE in all operation modes (Interactive, Headless, Readonly).
///
/// ARCHITECTURE:
/// - ViewModel fires SortRequested event when user clicks header sort menu
/// - This handler receives event and calls IAdvancedDataGridFacade.Sorting.SortByColumnAsync()
/// - SortService performs actual sorting in IRowStore
/// - InternalUIUpdateHandler detects row changes and triggers UI refresh
///
/// THREAD SAFETY: Event handlers use async void pattern (fire-and-forget).
/// </summary>
internal sealed class InternalUISortHandler : IDisposable
{
    private readonly ILogger<InternalUISortHandler> _logger;
    private readonly DataGridViewModel _viewModel;
    private readonly IAdvancedDataGridFacade _facade;
    private bool _isDisposed;

    /// <summary>
    /// Creates internal sort handler and subscribes to ViewModel.SortRequested event.
    /// </summary>
    /// <param name="facade">Facade API for sort operations</param>
    /// <param name="viewModel">ViewModel that fires SortRequested event</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public InternalUISortHandler(
        IAdvancedDataGridFacade facade,
        DataGridViewModel viewModel,
        ILogger<InternalUISortHandler>? logger = null)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // ✅ Subscribe to SortRequested event
        _viewModel.SortRequested += OnSortRequested;
        _logger.LogInformation("InternalUISortHandler activated - subscribed to SortRequested event");
    }

    /// <summary>
    /// Handles sort requests from ViewModel (triggered by header menu clicks).
    /// Calls facade sort API and logs success/failure.
    /// </summary>
    private async void OnSortRequested(object? sender, SortRequestedEventArgs args)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot handle sort request - handler is disposed");
            return;
        }

        try
        {
            _logger.LogInformation("AUTO-SORT: Sort requested for column '{ColumnName}', direction '{Direction}'",
                args.ColumnName, args.Direction);

            // Special case: "None" direction means clear sort (restore original order)
            if (args.Direction == SortDirection.None)
            {
                _logger.LogInformation("AUTO-SORT: Clear sort requested - restoring original order (sorting by __rowNumber)");

                // Sort by __rowNumber to restore original insertion order
                var clearResult = await _facade.Sorting.SortByColumnAsync("__rowNumber", ascending: true, CancellationToken.None);

                if (clearResult.IsSuccess)
                {
                    _logger.LogInformation("✅ AUTO-SORT: Sort cleared successfully");
                }
                else
                {
                    _logger.LogError("❌ AUTO-SORT: Clear sort failed - {ErrorMessage}", clearResult.ErrorMessage);
                }
                return;
            }

            // Convert SortDirection enum to boolean (Ascending=true, Descending=false)
            bool ascending = args.Direction == SortDirection.Ascending;

            _logger.LogDebug("AUTO-SORT: Calling facade.Sorting.SortByColumnAsync(column='{ColumnName}', ascending={Ascending})",
                args.ColumnName, ascending);

            // ✅ Call facade sort API
            var result = await _facade.Sorting.SortByColumnAsync(args.ColumnName, ascending, CancellationToken.None);

            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ AUTO-SORT: Sort completed successfully for column '{ColumnName}' ({Direction})",
                    args.ColumnName, ascending ? "Ascending" : "Descending");
            }
            else
            {
                _logger.LogError("❌ AUTO-SORT: Sort failed - {ErrorMessage}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ AUTO-SORT: Exception during sort operation for column '{ColumnName}'", args.ColumnName);
        }
    }

    /// <summary>
    /// Disposes handler and unsubscribes from SortRequested event.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _viewModel.SortRequested -= OnSortRequested;
        _logger.LogInformation("InternalUISortHandler deactivated - unsubscribed from SortRequested event");

        _isDisposed = true;
    }
}
```

---

### **KROK 2: Zaregistrovať InternalUISortHandler v AdvancedDataGridFacade**

**Lokácia:** `AdvancedDataGridFacade.cs` - constructor a Dispose()

**Zmeny:**

```csharp
// AdvancedDataGridFacade.cs - Add field
private readonly InternalUIOperationHandler? _operationHandler;
private readonly InternalUIUpdateHandler? _updateHandler;
private readonly InternalUISortHandler? _sortHandler;  // ✅ NEW FIELD

// AdvancedDataGridFacade.cs - Constructor enhancement
public AdvancedDataGridFacade(
    IRowStore rowStore,
    AdvancedDataGridOptions options,
    AdvancedDataGridControl? uiControl = null,
    ILoggerFactory? loggerFactory = null)
{
    // ... existing initialization ...

    // ===== EXISTING HANDLERS =====

    // Initialize automatic UI operation handler (delete, insert, auto-expand)
    // ONLY active in Interactive mode
    if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiControl != null)
    {
        var opLogger = _loggerFactory?.CreateLogger<InternalUIOperationHandler>();
        _operationHandler = new InternalUIOperationHandler(this, _options, _uiControl, opLogger);
        _logger?.LogInformation("InternalUIOperationHandler initialized for Interactive mode");
    }

    // Initialize automatic UI update handler (incremental updates, reload triggers)
    // ACTIVE in all modes
    if (_uiControl != null)
    {
        var updateLogger = _loggerFactory?.CreateLogger<InternalUIUpdateHandler>();
        _updateHandler = new InternalUIUpdateHandler(_viewModel, _rowStore, _uiControl, updateLogger);
        _logger?.LogInformation("InternalUIUpdateHandler initialized for UI synchronization");
    }

    // ===== NEW HANDLER =====

    // ✅ Initialize automatic sort handler (header click → sort operation)
    // ACTIVE in all modes (sorting is always allowed)
    if (_uiControl != null)
    {
        var sortLogger = _loggerFactory?.CreateLogger<InternalUISortHandler>();
        _sortHandler = new InternalUISortHandler(this, _viewModel, sortLogger);
        _logger?.LogInformation("InternalUISortHandler initialized for automatic sort handling");
    }
}
```

**Dispose enhancement:**

```csharp
// AdvancedDataGridFacade.cs - Dispose() method
public void Dispose()
{
    _logger?.LogInformation("AdvancedDataGridFacade disposing...");

    // Dispose handlers first (they unsubscribe from events)
    _operationHandler?.Dispose();
    _updateHandler?.Dispose();
    _sortHandler?.Dispose();  // ✅ NEW

    // ... rest of dispose logic ...
}
```

---

### **KROK 3: Verifikovať SortService implementáciu**

**Lokácia:** `Services/SortService.cs`

**Skontrolovať, že implementácia obsahuje:**

1. ✅ Načítanie všetkých riadkov z IRowStore
2. ✅ Sorting podľa column value (handle nulls)
3. ✅ Reassign __rowNumber (1-based sequential order)
4. ✅ Update rows v IRowStore
5. ✅ Trigger full reload (`_viewModel.ForceCompleteReloadAsync()`)

**Expected implementation:**

```csharp
// SortService.cs - Verify this implementation exists
public async Task<OperationResult> SortByColumnAsync(
    string columnName,
    bool ascending,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger?.LogInformation("SortService: Starting sort by column '{ColumnName}' ({Direction})",
            columnName, ascending ? "ASC" : "DESC");

        // STEP 1: Get all rows from storage
        var allRows = (await _rowStore.GetAllRowsAsync(cancellationToken)).ToList();

        if (allRows.Count == 0)
        {
            _logger?.LogWarning("SortService: No rows to sort");
            return OperationResult.Success();
        }

        _logger?.LogDebug("SortService: Loaded {Count} rows from storage", allRows.Count);

        // STEP 2: Sort rows by column value (handle nulls - nulls first or last depending on DB)
        IEnumerable<IReadOnlyDictionary<string, object?>> sortedRows;

        if (ascending)
        {
            sortedRows = allRows.OrderBy(row =>
            {
                if (row.TryGetValue(columnName, out var value))
                    return value;
                return null; // Nulls first in ascending
            });
        }
        else
        {
            sortedRows = allRows.OrderByDescending(row =>
            {
                if (row.TryGetValue(columnName, out var value))
                    return value;
                return null; // Nulls last in descending
            });
        }

        _logger?.LogDebug("SortService: Rows sorted in memory");

        // STEP 3: Reassign __rowNumber (1-based sequential)
        int rowNumber = 1;
        foreach (var row in sortedRows)
        {
            var rowId = row["__rowId"]!.ToString()!;

            // Create updated row with new __rowNumber
            var updatedRow = new Dictionary<string, object?>(row)
            {
                ["__rowNumber"] = rowNumber
            };

            // Update in storage
            await _rowStore.UpdateRowAsync(rowId, updatedRow, cancellationToken);

            rowNumber++;
        }

        _logger?.LogInformation("✅ SortService: Sorted {Count} rows by column '{ColumnName}' ({Direction}), __rowNumber reassigned",
            allRows.Count, columnName, ascending ? "ASC" : "DESC");

        // STEP 4: Trigger full reload to refresh UI
        // This will be automatically detected by InternalUIUpdateHandler
        // which subscribes to IRowStore.RowChanged events

        return OperationResult.Success();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "❌ SortService: Sort failed for column '{ColumnName}'", columnName);
        return OperationResult.Failure($"Sort exception: {ex.Message}");
    }
}
```

**DÔLEŽITÉ:** Ak SortService NEvolá `ForceCompleteReloadAsync()`, UI sa nemusí automaticky aktualizovať!

**Fix ak chýba:**
```csharp
// Na konci SortByColumnAsync(), pred return OperationResult.Success():

// ✅ CRITICAL: Trigger full reload to refresh UI
if (_viewModel != null)
{
    await _viewModel.ForceCompleteReloadAsync();
    _logger?.LogDebug("SortService: Full reload triggered");
}
```

---

## 📋 ZHRNUTIE ZMIEN

### **Nové súbory:**

1. **UIAdapters/WinUI/InternalUISortHandler.cs** (NEW)
   - Trieda: `InternalUISortHandler`
   - Účel: Subscribe na `SortRequested` event, volá `facade.Sorting.SortByColumnAsync()`

### **Upravené súbory:**

2. **AdvancedDataGridFacade.cs**
   - Pridať: Field `_sortHandler`
   - Upraviť: Constructor - vytvoriť a zaregistrovať `InternalUISortHandler`
   - Upraviť: Dispose() - zavolať `_sortHandler?.Dispose()`

3. **Services/SortService.cs** (VERIFY ONLY)
   - Overiť: Metóda `SortByColumnAsync()` volá `ForceCompleteReloadAsync()` na konci
   - Ak nie: Pridať call na konci metódy

---

## 🧪 TESTING CHECKLIST

Po implementácii otestovať nasledovné scenáre:

### **Test 1: Sort Ascending - Text Column**
1. ✅ Otvoriť aplikáciu, načítať dáta
2. ✅ Kliknúť na header text stĺpca (napr. "Name")
3. ✅ Vybrať "Sort Ascending"
4. ✅ **Verifikovať:** Dáta zoradené A→Z
5. ✅ **Verifikovať:** Header má up arrow (↑) ikonu

### **Test 2: Sort Descending - Text Column**
1. ✅ Kliknúť znova na rovnaký header
2. ✅ Vybrať "Sort Descending"
3. ✅ **Verifikovať:** Dáta zoradené Z→A
4. ✅ **Verifikovať:** Header má down arrow (↓) ikonu

### **Test 3: Sort Ascending - Numeric Column**
1. ✅ Kliknúť na header numeric stĺpca (napr. "Age", "Price")
2. ✅ Vybrať "Sort Ascending"
3. ✅ **Verifikovať:** Dáta zoradené 0→9 (najmenšie číslo nahor)
4. ✅ **Verifikovať:** NULL hodnoty sú na začiatku alebo konci (podľa DB logiky)

### **Test 4: Sort Descending - Numeric Column**
1. ✅ Kliknúť znova na rovnaký header
2. ✅ Vybrať "Sort Descending"
3. ✅ **Verifikovať:** Dáta zoradené 9→0 (najväčšie číslo nahor)

### **Test 5: Clear Sort**
1. ✅ Po sortovaní kliknúť na header
2. ✅ Vybrať "Clear Sort"
3. ✅ **Verifikovať:** Dáta obnovené v pôvodnom poradí (sorting by __rowNumber)
4. ✅ **Verifikovať:** Header už nemá arrow ikonu

### **Test 6: Sort Different Columns**
1. ✅ Zoradiť podľa stĺpca A (Ascending)
2. ✅ Zoradiť podľa stĺpca B (Descending)
3. ✅ **Verifikovať:** Stĺpec A už nemá arrow, len stĺpec B má down arrow
4. ✅ **Verifikovať:** Dáta zoradené podľa stĺpca B, stĺpec A sa ignoruje

### **Test 7: Sort with Pagination**
1. ✅ Načítať veľký dataset (napr. 100 riadkov, 20 per page → 5 pages)
2. ✅ Zoradiť podľa stĺpca (Ascending)
3. ✅ **Verifikovať:** Page 1 zobrazuje prvých 20 zoradených riadkov
4. ✅ Prepnúť na page 2
5. ✅ **Verifikovať:** Page 2 zobrazuje riadky 21-40 v zoradenom poradí

### **Test 8: Sort Special Columns**
1. ✅ Pokúsiť sa zoradiť podľa RowNumber stĺpca
2. ✅ **Verifikovať:** Funguje (číselné zoradenie)
3. ✅ Pokúsiť sa zoradiť podľa Checkbox stĺpca (ak má boolean hodnoty)
4. ✅ **Verifikovať:** Funguje (false pred true, alebo opačne)

---

## 📊 EXPECTED LOG OUTPUT

Po úspešnej implementácii by logy mali vyzerať takto:

**Startup logs:**
```
[INFO] InternalUISortHandler activated - subscribed to SortRequested event
```

**Sort operation logs:**
```
[INFO] Sort Ascending selected for column Name
[INFO] AUTO-SORT: Sort requested for column 'Name', direction 'Ascending'
[DEBUG] AUTO-SORT: Calling facade.Sorting.SortByColumnAsync(column='Name', ascending=True)
[INFO] SortService: Starting sort by column 'Name' (ASC)
[DEBUG] SortService: Loaded 67 rows from storage
[DEBUG] SortService: Rows sorted in memory
[INFO] ✅ SortService: Sorted 67 rows by column 'Name' (ASC), __rowNumber reassigned
[DEBUG] SortService: Full reload triggered
[INFO] ✅ AUTO-SORT: Sort completed successfully for column 'Name' (Ascending)
[INFO] InternalUIUpdateHandler: Detected large-scale row changes, triggering full reload...
[INFO] Complete UI refresh triggered - ItemsRepeater rebind requested
```

**Clear sort logs:**
```
[INFO] Clear Sort selected for column Name
[INFO] AUTO-SORT: Sort requested for column 'Name', direction 'None'
[INFO] AUTO-SORT: Clear sort requested - restoring original order (sorting by __rowNumber)
[INFO] SortService: Starting sort by column '__rowNumber' (ASC)
[INFO] ✅ SortService: Sorted 67 rows by column '__rowNumber' (ASC), __rowNumber reassigned
[INFO] ✅ AUTO-SORT: Sort cleared successfully
```

---

## ⚠️ ZNÁME OBMEDZENIA

1. **Performance:** Sorting veľkého datasetu (10,000+ riadkov) môže trvať niekoľko sekúnd
   - **Dôvod:** Každý `UpdateRowAsync()` je samostatný SQL UPDATE
   - **Možné zlepšenie:** Batch UPDATE v jednej transakcii

2. **Multi-column sort:** Aktuálna implementácia podporuje len single-column sort
   - **Feature request:** Pridať Shift+Click pre secondary sort (napr. sort by LastName, then FirstName)

3. **Case sensitivity:** Text sorting môže byť case-sensitive alebo case-insensitive podľa SQLite COLLATE settings
   - **Možné zlepšenie:** Pridať option do AdvancedDataGridOptions

4. **NULL handling:** NULL hodnoty sa sortujú na začiatok (Ascending) alebo koniec (Descending) - správanie závisí od SQLite
   - **Možné zlepšenie:** Custom comparator na explicitné NULL handling

---

## 🎯 PREČO JE TOTO NAJJEDNODUCHŠIE RIEŠENIE

1. **Len 1 nový súbor:** InternalUISortHandler.cs (~120 riadkov kódu)
2. **Minimálne zmeny:** AdvancedDataGridFacade.cs (3 riadky - field, constructor, dispose)
3. **Žiadne UI zmeny:** HeadersRowView už má kompletné UI (flyout, menu items)
4. **Service už existuje:** SortService.SortByColumnAsync() je fully implemented
5. **Konzistentný pattern:** Rovnaký ako InternalUIOperationHandler a InternalUIUpdateHandler
6. **Low risk:** Žiadne breaking changes, jen pridanie novej funkcionality

**Time estimate:** 30-45 minút implementácie + 15 minút testovania = **~1 hodina celkom**

---

**END OF DOCUMENT**

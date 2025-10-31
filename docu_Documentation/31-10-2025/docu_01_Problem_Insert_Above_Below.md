# ❌ PROBLÉM 1: KONTEXTOVÉ MENU INSERT ABOVE/BELOW

**Dátum:** 31. október 2025
**Priorita:** 🔧 STREDNÁ (Medium Complexity)
**Status:** Čiastočne funkčné (special column tlačidlo funguje, context menu nie)

---

## 🔍 POPIS PROBLÉMU

### **Symptóm:**
Používateľ klikne pravým tlačidlom na bunku v DataGrid, otvorí sa context menu s možnosťami:
- "Insert rows above"
- "Insert rows below"

Po výbere jednej z týchto možností a zadaní počtu riadkov **NIC SA NESTANE**.

### **Očakávané správanie:**
- Pri výbere "Insert 3 rows above" sa má **nad** aktuálny riadok vložiť 3 prázdne riadky
- Pri výbere "Insert 2 rows below" sa má **pod** aktuálny riadok vložiť 2 prázdne riadky
- Dáta existujúcich riadkov sa posunú nahor/dolu podľa potreby

### **Aktuálne správanie:**
- Context menu sa zobrazí správne
- Dialog na zadanie počtu riadkov funguje
- Event `InsertRowRequested` sa síce vyvolá, ale handler ho ignoruje s warningom v logu

---

## 🐛 IDENTIFIKOVANÉ PROBLÉMY

### **Problém 1.1: Context menu posiela NULL RowId**

**Lokácia:** `DataGridCellsView.cs:830-946`

**Kód:**
```csharp
// DataGridCellsView.cs:893-899 - PROBLEMATIC CODE
private void OnRowContextMenuInsertAbove(object sender, RoutedEventArgs e)
{
    // ... dialog handling ...

    for (int i = 0; i < rowCount; i++)
    {
        var eventArgs = new InsertRowRequestedEventArgs(globalIndex, null, "Above");
        //                                                          ^^^^ ❌ RowId is NULL!
        InsertRowRequested?.Invoke(this, eventArgs);
        totalInserted++;
    }
}
```

**Root Cause:**
- Handler vytvorí event args s `RowId = null`
- Posiela len `globalIndex` (page-relative index)
- `InsertRowRequestedEventArgs(globalIndex, null, "Above")` - druhý parameter je NULL

**Dôsledok:**
- Event síce príde do `InternalUIOperationHandler`, ale handler nedokáže pracovať bez RowId
- Facade API vyžaduje stable RowId identifier, nie nestabilný index

---

### **Problém 1.2: Index-based insert je DISABLED**

**Lokácia:** `InternalUIOperationHandler.cs:131-139`

**Kód:**
```csharp
// InternalUIOperationHandler.cs:131-139
private async void OnInsertRowRequested(object? sender, InsertRowRequestedEventArgs args)
{
    // ...

    else if (args.RowIndex >= 0)
    {
        // ❌ MODE 2 DISABLED: Index-based insert not supported yet
        // TODO: Context menu should send RowId instead of index
        // For now, log warning and skip
        _logger.LogWarning("AUTO-INSERT: Index-based insert not supported. RowIndex={RowIndex}, Position={Position}. " +
            "Context menu needs to send RowId instead of index.",
            args.RowIndex, args.Position ?? "After");
    }
}
```

**Root Cause:**
- Handler má explicitne vypnutý index-based režim
- Aj keby context menu poslal index, nič by sa nestalo

**Dôsledok:**
- Insert operácia sa PRESKOČÍ s warningom v logu
- Používateľ nevidí žiadnu chybovú hlášku

---

### **Problém 1.3: Chýba mapovanie Selected Cells → RowId**

**Root Cause:**
- Context menu je otvorený cez pravý klik na bunke
- V momente kliku je bunka už v `ViewModel.SelectedCells` kolekcii
- `SelectedCells` obsahuje `CellViewModel` objekty, ktoré majú `RowId` property
- Handler **NEKONTROLUJE** SelectedCells a nestará sa o RowId

**Dôsledok:**
- Stráca sa informácia o konkrétnom riadku (RowId)
- Handler nedokáže zavolať `VirtualInsertEmptyRowAfterAsync(rowId)`

---

### **Problém 1.4: Chýba API pre Insert BEFORE (Above)**

**Lokácia:** `IDataGridRows` interface

**Aktuálny stav:**
- ✅ Existuje: `VirtualInsertEmptyRowAfterAsync(rowId)` - vloží riadok POD
- ❌ Neexistuje: `VirtualInsertEmptyRowBeforeAsync(rowId)` - vloží riadok NAD

**Root Cause:**
- API bolo navrhnuté len pre "Insert Below" use case (special column tlačidlo)
- Context menu potrebuje aj "Insert Above" funkcionalitu

**Dôsledok:**
- Aj keby handler dostal RowId, nedokáže spracovať `Position="Above"`

---

## ✅ PROFESIONÁLNE RIEŠENIE

### **KROK 1: Opraviť DataGridCellsView - získať RowId zo SelectedCells**

**Lokácia:** `DataGridCellsView.cs:830-903`

**Zmeny:**
1. Na začiatku handlera skontrolovať `ViewModel.SelectedCells`
2. Získať prvú vybratú bunku: `firstSelectedCell = ViewModel.SelectedCells.First()`
3. Extrahovať `RowId` a `RowIndex` z bunky
4. Validovať, že RowId nie je null/empty
5. Poslať event s RowId namiesto null

**Implementácia:**

```csharp
// DataGridCellsView.cs:830-903 - FIXED VERSION
private void OnRowContextMenuInsertAbove(object sender, RoutedEventArgs e)
{
    // ✅ STEP 1: Validate selected cells
    if (ViewModel.SelectedCells.Count == 0)
    {
        _logger.LogWarning("Context menu Insert Above - no selected cells");
        return;
    }

    // ✅ STEP 2: Get RowId from first selected cell
    var firstSelectedCell = ViewModel.SelectedCells.First();
    string? rowId = firstSelectedCell.RowId;
    int rowIndex = firstSelectedCell.RowIndex;

    if (string.IsNullOrEmpty(rowId))
    {
        _logger.LogError("Context menu Insert Above - selected cell has no RowId (rowIndex={RowIndex})", rowIndex);
        return;
    }

    _logger.LogDebug("Context menu Insert Above triggered for row {RowIndex}, RowId={RowId}", rowIndex, rowId);

    // ✅ STEP 3: Show dialog to get row count
    var insertDialog = new ContentDialog
    {
        Title = "Insert Rows Above",
        Content = new TextBox
        {
            PlaceholderText = "Enter number of rows to insert",
            Text = "1"
        },
        PrimaryButtonText = "Insert",
        CloseButtonText = "Cancel",
        XamlRoot = this.XamlRoot
    };

    insertDialog.PrimaryButtonClick += async (s, args) =>
    {
        var textBox = (TextBox)insertDialog.Content;
        if (int.TryParse(textBox.Text, out int rowCount) && rowCount > 0)
        {
            _logger.LogInformation("User requested insert {RowCount} rows ABOVE row {RowIndex} (RowId={RowId})",
                rowCount, rowIndex, rowId);

            int totalInserted = 0;

            // ✅ STEP 4: Fire events with RowId (not null!)
            for (int i = 0; i < rowCount; i++)
            {
                // ✅ FIXED: Send RowId instead of null
                // Position="Above" means insert BEFORE this row
                var eventArgs = new InsertRowRequestedEventArgs(rowIndex, rowId, "Above");
                InsertRowRequested?.Invoke(this, eventArgs);
                totalInserted++;
            }

            _logger.LogInformation("Context menu insert completed: {TotalInserted} rows inserted ABOVE row {RowIndex}",
                totalInserted, rowIndex);
        }
        else
        {
            _logger.LogWarning("Invalid row count entered: {Text}", textBox.Text);
        }
    };

    _ = insertDialog.ShowAsync();
}
```

**Rovnaká logika pre `OnRowContextMenuInsertBelow()`:**

```csharp
// DataGridCellsView.cs:915-946 - FIXED VERSION
private void OnRowContextMenuInsertBelow(object sender, RoutedEventArgs e)
{
    if (ViewModel.SelectedCells.Count == 0)
    {
        _logger.LogWarning("Context menu Insert Below - no selected cells");
        return;
    }

    // ✅ Get RowId from first selected cell
    var firstSelectedCell = ViewModel.SelectedCells.First();
    string? rowId = firstSelectedCell.RowId;
    int rowIndex = firstSelectedCell.RowIndex;

    if (string.IsNullOrEmpty(rowId))
    {
        _logger.LogError("Context menu Insert Below - selected cell has no RowId (rowIndex={RowIndex})", rowIndex);
        return;
    }

    var insertDialog = new ContentDialog
    {
        Title = "Insert Rows Below",
        Content = new TextBox
        {
            PlaceholderText = "Enter number of rows to insert",
            Text = "1"
        },
        PrimaryButtonText = "Insert",
        CloseButtonText = "Cancel",
        XamlRoot = this.XamlRoot
    };

    insertDialog.PrimaryButtonClick += async (s, args) =>
    {
        var textBox = (TextBox)insertDialog.Content;
        if (int.TryParse(textBox.Text, out int rowCount) && rowCount > 0)
        {
            _logger.LogInformation("User requested insert {RowCount} rows BELOW row {RowIndex} (RowId={RowId})",
                rowCount, rowIndex, rowId);

            int totalInserted = 0;

            for (int i = 0; i < rowCount; i++)
            {
                // ✅ FIXED: Send RowId instead of null
                // Position="Below" means insert AFTER this row
                var eventArgs = new InsertRowRequestedEventArgs(rowIndex, rowId, "Below");
                InsertRowRequested?.Invoke(this, eventArgs);
                totalInserted++;
            }

            _logger.LogInformation("Context menu insert completed: {TotalInserted} rows inserted BELOW row {RowIndex}",
                totalInserted, rowIndex);
        }
    };

    _ = insertDialog.ShowAsync();
}
```

---

### **KROK 2: Implementovať VirtualInsertEmptyRowBeforeAsync API**

**Lokácia:** `IDataGridRows.cs` + `DataGridRows.cs`

**Interface Definition:**

```csharp
// IDataGridRows.cs - Add new method
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Interfaces;

public interface IDataGridRows
{
    // ... existing methods ...

    /// <summary>
    /// Virtually inserts an empty row BEFORE the specified row (identified by RowId).
    /// STRATEGY: Shifts current row and all rows below down by 1 (__rowNumber++).
    /// The new empty row is inserted at the current row's __rowNumber position.
    /// Row count remains unchanged - last row is overwritten.
    /// </summary>
    /// <param name="rowId">RowId of the row BEFORE which to insert (stable identifier)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result with success/failure status</returns>
    Task<OperationResult> VirtualInsertEmptyRowBeforeAsync(
        string rowId,
        CancellationToken cancellationToken = default);
}
```

**Implementation:**

```csharp
// DataGridRows.cs - Implementation
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Services;

public class DataGridRows : IDataGridRows
{
    // ... existing code ...

    /// <summary>
    /// Virtually inserts an empty row BEFORE the specified row.
    /// ALGORITHM:
    /// 1. Find target row by RowId, get its __rowNumber (e.g., 5)
    /// 2. Shift all rows with __rowNumber >= 5 down by 1 (__rowNumber++)
    /// 3. Create new empty row with __rowNumber = 5
    /// 4. Trigger full reload to refresh UI
    /// </summary>
    public async Task<OperationResult> VirtualInsertEmptyRowBeforeAsync(
        string rowId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("VirtualInsertBefore: Starting insert before RowId={RowId}", rowId);

            // STEP 1: Get target row by RowId
            var targetRow = await _rowStore.GetRowAsync(rowId, cancellationToken);
            if (targetRow == null)
            {
                _logger?.LogError("VirtualInsertBefore: Row with RowId={RowId} not found", rowId);
                return OperationResult.Failure($"Row with RowId={rowId} not found");
            }

            // STEP 2: Get __rowNumber of target row
            if (!targetRow.TryGetValue("__rowNumber", out var rowNumberObj) ||
                rowNumberObj == null ||
                !int.TryParse(rowNumberObj.ToString(), out int targetRowNumber))
            {
                _logger?.LogError("VirtualInsertBefore: Row {RowId} has invalid __rowNumber", rowId);
                return OperationResult.Failure($"Row {rowId} has invalid __rowNumber");
            }

            _logger?.LogDebug("VirtualInsertBefore: Target row has __rowNumber={RowNumber}", targetRowNumber);

            // STEP 3: Get all rows and find rows to shift down
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
            var rowsToShift = allRows
                .Where(r => r.TryGetValue("__rowNumber", out var rn) &&
                           int.TryParse(rn?.ToString(), out int num) &&
                           num >= targetRowNumber)
                .OrderByDescending(r => int.Parse(r["__rowNumber"]!.ToString()!)) // Process from bottom to top
                .ToList();

            _logger?.LogInformation("VirtualInsertBefore: Found {Count} rows to shift down (rowNumber >= {TargetRowNumber})",
                rowsToShift.Count, targetRowNumber);

            // STEP 4: Shift rows down (increment __rowNumber)
            foreach (var row in rowsToShift)
            {
                var oldRowNumber = int.Parse(row["__rowNumber"]!.ToString()!);
                var newRowNumber = oldRowNumber + 1;

                var updatedRow = new Dictionary<string, object?>(row)
                {
                    ["__rowNumber"] = newRowNumber
                };

                await _rowStore.UpdateRowAsync(row["__rowId"]!.ToString()!, updatedRow, cancellationToken);

                _logger?.LogDebug("VirtualInsertBefore: Shifted row {RowId}: __rowNumber {Old} → {New}",
                    row["__rowId"], oldRowNumber, newRowNumber);
            }

            // STEP 5: Create new empty row at target position
            var newRowId = Ulid.NewUlid().ToString();
            var newRow = new Dictionary<string, object?>
            {
                ["__rowId"] = newRowId,
                ["__rowNumber"] = targetRowNumber
            };

            // Add null values for all data columns
            foreach (var columnName in _columnNames.Where(c => !c.StartsWith("__")))
            {
                newRow[columnName] = null;
            }

            await _rowStore.AddRowAsync(newRow, cancellationToken);

            _logger?.LogInformation("✅ VirtualInsertBefore: New empty row inserted at __rowNumber={RowNumber}, RowId={NewRowId}, {Count} rows shifted down",
                targetRowNumber, newRowId, rowsToShift.Count);

            // STEP 6: Trigger full reload
            // Note: This will be handled by InternalUIUpdateHandler automatically

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ VirtualInsertBefore failed for RowId={RowId}", rowId);
            return OperationResult.Failure($"Exception: {ex.Message}");
        }
    }
}
```

---

### **KROK 3: Upraviť InternalUIOperationHandler - podporiť Position="Above"**

**Lokácia:** `InternalUIOperationHandler.cs:102-151`

**Zmeny:**
1. Odstrániť index-based disabled režim
2. Pridať podporu pre `Position="Above"` (volá `VirtualInsertEmptyRowBeforeAsync`)
3. Zachovať podporu pre `Position="Below"` (volá `VirtualInsertEmptyRowAfterAsync`)

**Implementácia:**

```csharp
// InternalUIOperationHandler.cs:102-151 - ENHANCED VERSION
private async void OnInsertRowRequested(object? sender, InsertRowRequestedEventArgs args)
{
    if (_isDisposed)
    {
        _logger.LogWarning("Cannot handle insert request - handler is disposed");
        return;
    }

    try
    {
        _logger.LogInformation("AUTO-INSERT: User requested insert at RowIndex={RowIndex}, RowId={RowId}, Position={Position}",
            args.RowIndex, args.RowId ?? "(NULL)", args.Position ?? "Below");

        // ✅ VALIDATION: RowId must be present
        if (string.IsNullOrEmpty(args.RowId))
        {
            _logger.LogError("AUTO-INSERT: RowId is null or empty - cannot perform insert. " +
                "This is a bug in the UI layer - event should always include RowId.");
            return;
        }

        // ✅ DUAL POSITION SUPPORT: Above (before) vs Below (after)
        if (args.Position == "Above")
        {
            // MODE 1: Insert BEFORE (above) the specified row
            _logger.LogInformation("AUTO-INSERT (ABOVE): Inserting empty row BEFORE RowId={RowId}", args.RowId);

            var result = await _facade.Rows.VirtualInsertEmptyRowBeforeAsync(args.RowId, CancellationToken.None);

            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ AUTO-INSERT (ABOVE): Empty row inserted BEFORE RowId={RowId}", args.RowId);
            }
            else
            {
                _logger.LogError("❌ AUTO-INSERT (ABOVE): Insert failed - {ErrorMessage}", result.ErrorMessage);
            }
        }
        else // Default: Position="Below" or null
        {
            // MODE 2: Insert AFTER (below) the specified row
            _logger.LogInformation("AUTO-INSERT (BELOW): Inserting empty row AFTER RowId={RowId}", args.RowId);

            var result = await _facade.Rows.VirtualInsertEmptyRowAfterAsync(args.RowId, CancellationToken.None);

            if (result.IsSuccess)
            {
                _logger.LogInformation("✅ AUTO-INSERT (BELOW): Empty row inserted AFTER RowId={RowId}", args.RowId);
            }
            else
            {
                _logger.LogError("❌ AUTO-INSERT (BELOW): Insert failed - {ErrorMessage}", result.ErrorMessage);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ AUTO-INSERT: Exception during insert operation. RowIndex={RowIndex}, RowId={RowId}, Position={Position}",
            args.RowIndex, args.RowId ?? "(NULL)", args.Position ?? "Below");
    }
}
```

---

## 📋 ZHRNUTIE ZMIEN

### **Súbory na upravenie:**

1. **DataGridCellsView.cs**
   - Metóda: `OnRowContextMenuInsertAbove()` (lines 830-903)
   - Metóda: `OnRowContextMenuInsertBelow()` (lines 915-946)
   - Zmena: Získať RowId z `ViewModel.SelectedCells.First().RowId`

2. **IDataGridRows.cs**
   - Pridať: Metóda `VirtualInsertEmptyRowBeforeAsync(string rowId, ...)`

3. **DataGridRows.cs**
   - Implementovať: `VirtualInsertEmptyRowBeforeAsync()` s row shifting logikou

4. **InternalUIOperationHandler.cs**
   - Metóda: `OnInsertRowRequested()` (lines 102-151)
   - Zmena: Pridať `if (args.Position == "Above")` branch s `VirtualInsertEmptyRowBeforeAsync` call

---

## 🧪 TESTING CHECKLIST

Po implementácii otestovať nasledovné scenáre:

### **Test 1: Insert Above - Single Row**
1. ✅ Otvoriť aplikáciu, načítať dáta (napr. page 7 s 10 riadkami)
2. ✅ Pravý klik na riadok 5
3. ✅ Vybrať "Insert rows above"
4. ✅ Zadať "1", kliknúť "Insert"
5. ✅ **Verifikovať:** Prázdny riadok vložený nad riadok 5, pôvodný riadok 5 je teraz riadok 6

### **Test 2: Insert Above - Multiple Rows**
1. ✅ Pravý klik na riadok 3
2. ✅ Vybrať "Insert rows above"
3. ✅ Zadať "5", kliknúť "Insert"
4. ✅ **Verifikovať:** 5 prázdnych riadkov vložených nad riadok 3

### **Test 3: Insert Below - Single Row**
1. ✅ Pravý klik na riadok 7
2. ✅ Vybrať "Insert rows below"
3. ✅ Zadať "1", kliknúť "Insert"
4. ✅ **Verifikovať:** Prázdny riadok vložený pod riadok 7

### **Test 4: Insert Below - Multiple Rows**
1. ✅ Pravý klik na riadok 2
2. ✅ Vybrať "Insert rows below"
3. ✅ Zadať "3", kliknúť "Insert"
4. ✅ **Verifikovať:** 3 prázdne riadky vložené pod riadok 2

### **Test 5: Insert on Last Row**
1. ✅ Ísť na poslednú stranu (page 7)
2. ✅ Pravý klik na posledný riadok
3. ✅ Vybrať "Insert rows below"
4. ✅ Zadať "2", kliknúť "Insert"
5. ✅ **Verifikovať:** 2 prázdne riadky pridané, PageManager správne aktualizuje TotalPages

### **Test 6: Insert on First Row**
1. ✅ Ísť na prvú stranu (page 1)
2. ✅ Pravý klik na prvý riadok
3. ✅ Vybrať "Insert rows above"
4. ✅ Zadať "1", kliknúť "Insert"
5. ✅ **Verifikovať:** Nový riadok je teraz prvý (__rowNumber=1)

---

## 📊 EXPECTED LOG OUTPUT

Po úspešnej implementácii by logy mali vyzerať takto:

```
[INFO] Context menu Insert Above triggered for row 5, RowId=01HQXYZ123ABC
[INFO] User requested insert 3 rows ABOVE row 5 (RowId=01HQXYZ123ABC)
[INFO] AUTO-INSERT (ABOVE): Inserting empty row BEFORE RowId=01HQXYZ123ABC
[INFO] VirtualInsertBefore: Starting insert before RowId=01HQXYZ123ABC
[DEBUG] VirtualInsertBefore: Target row has __rowNumber=5
[INFO] VirtualInsertBefore: Found 15 rows to shift down (rowNumber >= 5)
[DEBUG] VirtualInsertBefore: Shifted row 01HQABC456DEF: __rowNumber 5 → 6
[DEBUG] VirtualInsertBefore: Shifted row 01HQDEF789GHI: __rowNumber 6 → 7
... (more shift logs) ...
[INFO] ✅ VirtualInsertBefore: New empty row inserted at __rowNumber=5, RowId=01HQNEW789XYZ, 15 rows shifted down
[INFO] ✅ AUTO-INSERT (ABOVE): Empty row inserted BEFORE RowId=01HQXYZ123ABC
[INFO] Context menu insert completed: 1 rows inserted ABOVE row 5
[INFO] (repeat for 2nd and 3rd row inserts)
```

---

## ⚠️ ZNÁME OBMEDZENIA

1. **Performance:** Shifting veľkého počtu riadkov môže byť pomalé (každý UPDATE je samostatný SQL)
   - **Možné zlepšenie:** Batch UPDATE v jednej transakcii

2. **Virtual insert:** Celkový počet riadkov sa nemení (posledný riadok sa prepisuje)
   - **Dôvod:** Zachovanie konzistentnej row count pre PageManager
   - **Alternatíva:** Implementovať "physical insert" s row count zmenu

3. **Concurrent operations:** Ak počas insert operácie iný proces mení riadky, môže dôjsť k race condition
   - **Možné zlepšenie:** Row-level locking v SQLite

---

**END OF DOCUMENT**

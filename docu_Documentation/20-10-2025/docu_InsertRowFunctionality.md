# Insert Row Functionality Specification

**Priorita:** 🔴 P1 (Kritická)
**Čas:** 14-18 hodín
**Dependencies:** Cleanup Logic (pre metadata)

---

## 📋 Prehľad

Implementácia Insert Row funkcionality s:
- ➕ Special Column (kliknutie → insert row BELOW)
- Context Menu s **Continuous Block Logic** (NO DIALOG!)
- API metódy so **STREAMING variantami** (nie GetAllRowsAsync!)
- Toolbar buttons
- Keyboard shortcuts

### KRITICKÁ POŽIADAVKA

❌ **ŽIADNY DIALOG** pri "Insert Multiple Rows"
✅ **Automatický počet** = veľkosť continuous selected block

---

## 🎯 Column Order (FINÁLNE)

**Požadované poradie: 1, 2, 3, 4, 5, 6**

1. **RowNumber** (ak enabled)
2. **Checkbox** (ak enabled)
3. **DATA stĺpce**
4. **ValidationAlerts** (VŽDY enabled AK je feature Validation enabled!)
5. **InsertRow ➕** (NOVÝ, ak enabled)
6. **DeleteRow 🗑** (ak enabled)

**POZNÁMKA:** ValidationAlerts NIE je optional - je enabled AK je feature Validation enabled!

---

## 🔧 Implementačné Úlohy

### Úloha 1.4: Insert Row Special Column (➕)

**Čas:** 5-7 hodín

#### A) Enum Update

**Súbor:** Common/Enums.cs

```csharp
public enum SpecialColumnType
{
    None = 0,
    RowNumber = 1,
    Checkbox = 2,
    ValidationAlerts = 3,
    InsertRow = 4,      // NOVÉ
    DeleteRow = 5       // Zmenené z 4
}
```

#### B) SpecialColumnCellControl.cs - Pridať Case

**Lokácia:** line 42

```csharp
Content = _viewModel.SpecialType switch
{
    SpecialColumnType.RowNumber => CreateRowNumberControl(),
    SpecialColumnType.Checkbox => CreateCheckboxControl(),
    SpecialColumnType.ValidationAlerts => CreateValidationAlertsControl(),
    SpecialColumnType.InsertRow => CreateInsertRowControl(),    // NOVÉ
    SpecialColumnType.DeleteRow => CreateDeleteRowControl(),
    _ => new TextBlock { Text = "?", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center }
};
```

#### C) Event + Args

**1. Event Definition (line ~32):**

```csharp
public event EventHandler<InsertRowRequestedEventArgs>? OnInsertRowRequested;
```

**2. Args Class (nový súbor: InsertRowRequestedEventArgs.cs):**

```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

public sealed class InsertRowRequestedEventArgs : EventArgs
{
    public int RowIndex { get; }
    public string? RowId { get; }

    public InsertRowRequestedEventArgs(int rowIndex, string? rowId)
    {
        RowIndex = rowIndex;
        RowId = rowId;
    }
}
```

#### D) CreateInsertRowControl() Metóda

**Lokácia:** SpecialColumnCellControl.cs (pridať za CreateValidationAlertsControl)

```csharp
// Debounce fields (pridať pri DELETE_DEBOUNCE_MS):
private DateTime _lastInsertClick = DateTime.MinValue;
private const int INSERT_DEBOUNCE_MS = 300;

// Metóda:
private UIElement CreateInsertRowControl()
{
    var button = new Button
    {
        Content = "➕",
        FontSize = 14,
        Padding = new Thickness(4),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        MinWidth = 0,
        MinHeight = 0
    };

    button.Click += (s, e) =>
    {
        var now = DateTime.Now;
        if ((now - _lastInsertClick).TotalMilliseconds < INSERT_DEBOUNCE_MS)
            return; // Debounce protection

        _lastInsertClick = now;

        // Fire event: Insert empty row BELOW current row (no dialog!)
        OnInsertRowRequested?.Invoke(this,
            new InsertRowRequestedEventArgs(_viewModel.RowIndex, _viewModel.RowId));
    };

    var border = new Border
    {
        Child = button,
        Background = _viewModel.Theme?.CellDefaultBackground ?? new SolidColorBrush(Colors.White),
        BorderBrush = _viewModel.Theme?.CellBorder ?? new SolidColorBrush(Colors.LightGray),
        BorderThickness = new Thickness(0, 0, 1, 1),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
        Padding = new Thickness(1)
    };

    return border;
}
```

#### E) AdvancedDataGridOptions.cs - Flag

```csharp
/// <summary>
/// Gets or sets whether the insert row column is enabled
/// </summary>
public bool EnableInsertRowColumn { get; set; } = false;
```

Pridať aj do Clone() metódy:

```csharp
EnableInsertRowColumn = this.EnableInsertRowColumn,
```

#### F) DataGridViewModel.cs - Poradie Stĺpcov

**Lokácia:** RebuildColumns() metóda

```csharp
private void RebuildColumns()
{
    int columnIndex = 0;

    // 1. RowNumber (ak enabled)
    if (_options.EnableRowNumberColumn)
        AddSpecialColumn(SpecialColumnType.RowNumber, columnIndex++, "Row#", 60);

    // 2. Checkbox (ak enabled)
    if (_options.EnableCheckboxColumn)
        AddSpecialColumn(SpecialColumnType.Checkbox, columnIndex++, "☑", 50);

    // 3. DATA stĺpce
    foreach (var dataColumn in _dataColumns)
        AddDataColumn(dataColumn, columnIndex++);

    // 4. ValidationAlerts (VŽDY ak feature Validation enabled!)
    if (_options.IsFeatureEnabled(GridFeature.Validation))
        AddSpecialColumn(SpecialColumnType.ValidationAlerts, columnIndex++, "⚠",
            _options.ValidationAlertsColumnMinWidth);

    // 5. InsertRow ➕ (ak enabled)
    if (_options.EnableInsertRowColumn)
        AddSpecialColumn(SpecialColumnType.InsertRow, columnIndex++, "➕", 50);

    // 6. DeleteRow 🗑 (ak enabled)
    if (_options.EnableDeleteRowColumn)
        AddSpecialColumn(SpecialColumnType.DeleteRow, columnIndex++, "🗑", 50);
}
```

#### G) DataGridCellsView.cs - Event Handling

**Lokácia:** Pri vytváraní SpecialColumnCellControl (line ~240-260)

```csharp
// Existing handlers...
EventHandler<DeleteRowRequestedEventArgs> deleteHandler = ...;
specialControl.OnDeleteRowRequested += deleteHandler;

// NOVÉ: Insert row handler
EventHandler<InsertRowRequestedEventArgs> insertHandler = (sender, args) =>
{
    HandleInsertRowRequested(args);
};
specialControl.OnInsertRowRequested += insertHandler;

// Cleanup actions
rowData.CleanupActions.Add(() =>
{
    specialControl.OnRowSelectionChanged -= selectionHandler;
    specialControl.OnDeleteRowRequested -= deleteHandler;
    specialControl.OnInsertRowRequested -= insertHandler;  // NOVÉ
});
```

**Event definition (line ~73):**

```csharp
public event EventHandler<InsertRowRequestedEventArgs>? InsertRowRequested;
```

**Handler method (po HandleDeleteRowRequested):**

```csharp
private void HandleInsertRowRequested(InsertRowRequestedEventArgs args)
{
    InsertRowRequested?.Invoke(this, args);
}
```

---

### Úloha 1.5: Insert Row API + Context Menu Logic

**Čas:** 9-11 hodín

#### A) IAdvancedDataGridFacade.cs - Signatúry

```csharp
/// <summary>
/// Inserts empty row BEFORE specified row (streaming variant)
/// </summary>
Task<PublicResult<string>> InsertRowBeforeAsync(
    string referenceRowId,
    IReadOnlyDictionary<string, object?>? initialValues = null,
    CancellationToken cancellationToken = default);

/// <summary>
/// Inserts empty row AFTER specified row (streaming variant)
/// </summary>
Task<PublicResult<string>> InsertRowAfterAsync(
    string referenceRowId,
    IReadOnlyDictionary<string, object?>? initialValues = null,
    CancellationToken cancellationToken = default);

/// <summary>
/// Inserts empty row at TOP of grid (streaming variant)
/// </summary>
Task<PublicResult<string>> InsertRowAtTopAsync(
    IReadOnlyDictionary<string, object?>? initialValues = null,
    CancellationToken cancellationToken = default);

/// <summary>
/// Inserts multiple empty rows AFTER specified row
/// </summary>
Task<PublicResult<List<string>>> InsertRowsAfterAsync(
    string referenceRowId,
    int count,
    IReadOnlyDictionary<string, object?>? initialValues = null,
    CancellationToken cancellationToken = default);

/// <summary>
/// Inserts row BEFORE first selected row
/// </summary>
Task<PublicResult<string>> InsertRowBeforeSelectionAsync(
    CancellationToken cancellationToken = default);

/// <summary>
/// Inserts row AFTER last selected row
/// </summary>
Task<PublicResult<string>> InsertRowAfterSelectionAsync(
    CancellationToken cancellationToken = default);
```

#### B) DataGridRows.cs - Implementácia (STREAMING!)

**KRITICKÉ:** NEPOUŽÍVAŤ GetAllRowsAsync! Použiť StreamRowsAsync + InsertRowAtAsync!

```csharp
public async Task<PublicResult<string>> InsertRowAfterAsync(
    string referenceRowId,
    IReadOnlyDictionary<string, object?>? initialValues = null,
    CancellationToken cancellationToken = default)
{
    try
    {
        var operationId = Guid.NewGuid();
        _logger.LogInformation("InsertRowAfter {OpId}: refId={RefId}", operationId, referenceRowId);

        // STREAMING: Nájdi referenčný riadok index
        int refIndex = -1;
        int currentIndex = 0;
        IReadOnlyDictionary<string, object?>? templateRow = null;

        await foreach (var batch in _rowStore.StreamRowsAsync(false, false, 1000, cancellationToken))
        {
            foreach (var row in batch)
            {
                if (row.TryGetValue("__rowId", out var rowId) && rowId?.ToString() == referenceRowId)
                {
                    refIndex = currentIndex;
                    templateRow = row;
                    break;
                }
                currentIndex++;
            }
            if (refIndex != -1) break; // Found!
        }

        if (refIndex == -1)
            return PublicResult<string>.Failure($"Reference row '{referenceRowId}' not found");

        // Vytvor prázdny riadok
        var newRow = CreateEmptyRow(templateRow!);

        // Set metadata: USER-INSERTED
        ((Dictionary<string, object?>)newRow)["__creationType"] = "UserInserted";
        ((Dictionary<string, object?>)newRow)["__lastModified"] = DateTime.UtcNow;

        // Apply initialValues
        if (initialValues != null)
        {
            foreach (var kvp in initialValues)
                ((Dictionary<string, object?>)newRow)[kvp.Key] = kvp.Value;
        }

        // KRITICKÉ: Použiť STREAMING insert (nie GetAllRows!)
        // Toto vyžaduje novú metódu v IRowStore: InsertRowAtAsync(index, row)
        await _rowStore.InsertRowAtAsync(refIndex + 1, newRow, cancellationToken);

        var insertedRowId = newRow["__rowId"]?.ToString() ?? "";
        _logger.LogInformation("InsertRowAfter {OpId} completed: rowId={NewId}", operationId, insertedRowId);

        return PublicResult<string>.Success(insertedRowId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "InsertRowAfter failed: {Message}", ex.Message);
        return PublicResult<string>.Failure($"Insert failed: {ex.Message}");
    }
}

// Helper
private IReadOnlyDictionary<string, object?> CreateEmptyRow(IReadOnlyDictionary<string, object?> templateRow)
{
    var emptyRow = new Dictionary<string, object?>();

    foreach (var key in templateRow.Keys)
    {
        // Skip metadata + __rowId (IRowStore assigns new)
        if (key == "__rowId" || key == "__creationType" || key == "__lastModified")
            continue;

        emptyRow[key] = null; // Empty
    }

    return emptyRow;
}
```

**NOVÁ metóda v IRowStore:**

```csharp
/// <summary>
/// Inserts row at specific index (STREAMING - no GetAllRows!)
/// Updates RowIndex for all rows after insertion point
/// </summary>
Task InsertRowAtAsync(int index, IReadOnlyDictionary<string, object?> row, CancellationToken cancellationToken);
```

**InsertRowBeforeAsync** - podobná implementácia, len `refIndex` miesto `refIndex + 1`

**InsertRowAtTopAsync:**

```csharp
public async Task<PublicResult<string>> InsertRowAtTopAsync(
    IReadOnlyDictionary<string, object?>? initialValues = null,
    CancellationToken cancellationToken = default)
{
    // Get first row as template
    IReadOnlyDictionary<string, object?>? templateRow = null;
    await foreach (var batch in _rowStore.StreamRowsAsync(false, false, 1, cancellationToken))
    {
        templateRow = batch.FirstOrDefault();
        break;
    }

    if (templateRow == null)
        return PublicResult<string>.Failure("No rows to use as template");

    var newRow = CreateEmptyRow(templateRow);
    ((Dictionary<string, object?>)newRow)["__creationType"] = "UserInserted";
    ((Dictionary<string, object?>)newRow)["__lastModified"] = DateTime.UtcNow;

    if (initialValues != null)
    {
        foreach (var kvp in initialValues)
            ((Dictionary<string, object?>)newRow)[kvp.Key] = kvp.Value;
    }

    await _rowStore.InsertRowAtAsync(0, newRow, cancellationToken);

    return PublicResult<string>.Success(newRow["__rowId"]?.ToString() ?? "");
}
```

#### C) Context Menu - Continuous Block Logic

**Súbor:** AdvancedDataGridControl.cs (alebo DataGridCellsView.cs)

**KĽÚČOVÁ LOGIKA:**

```csharp
/// <summary>
/// Vypočíta veľkosť continuous blocku obsahujúceho contextRowIndex.
///
/// PRÍKLADY:
/// - Označené: [3,5,6], context=5 → block=[5,6] → return 2
/// - Označené: [3,5,6], context=6 → block=[5,6] → return 2
/// - Označené: [3,5], context=5 → block=[5] → return 1
/// - Označené: [3,5], context=3 → block=[3] → return 1
/// </summary>
private int GetContinuousBlockSize(int contextRowIndex, List<int> selectedIndices)
{
    if (!selectedIndices.Contains(contextRowIndex))
        return 1; // Ak context row NIE je selected → 1 riadok

    // Nájdi continuous block obsahujúci contextRowIndex
    var sorted = selectedIndices.OrderBy(x => x).ToList();

    int blockStart = contextRowIndex;
    int blockEnd = contextRowIndex;

    // Expand block backward
    for (int i = contextRowIndex - 1; i >= 0; i--)
    {
        if (sorted.Contains(i))
            blockStart = i;
        else
            break; // Discontinuity found
    }

    // Expand block forward
    for (int i = contextRowIndex + 1; i < _totalRows; i++)
    {
        if (sorted.Contains(i))
            blockEnd = i;
        else
            break; // Discontinuity found
    }

    return blockEnd - blockStart + 1;
}
```

**Context Menu Creation:**

```csharp
private void ShowContextMenu(int contextRowIndex, string contextRowId)
{
    var menuFlyout = new MenuFlyout();

    // Insert Row Above
    var insertAbove = new MenuFlyoutItem { Text = "Insert Row Above" };
    insertAbove.Click += async (s, e) =>
    {
        var selectedIndices = GetSelectedRowIndices();
        var count = GetContinuousBlockSize(contextRowIndex, selectedIndices);

        _logger.LogInformation("Insert {Count} rows ABOVE index {Index}", count, contextRowIndex);

        // Insert 'count' empty rows ABOVE contextRowIndex
        for (int i = 0; i < count; i++)
        {
            await _facade.InsertRowAtAsync(contextRowIndex, null);
        }
    };
    menuFlyout.Items.Add(insertAbove);

    // Insert Row Below
    var insertBelow = new MenuFlyoutItem { Text = "Insert Row Below" };
    insertBelow.Click += async (s, e) =>
    {
        var selectedIndices = GetSelectedRowIndices();
        var count = GetContinuousBlockSize(contextRowIndex, selectedIndices);

        _logger.LogInformation("Insert {Count} rows BELOW index {Index}", count, contextRowIndex);

        // Insert 'count' empty rows BELOW contextRowIndex
        for (int i = 0; i < count; i++)
        {
            await _facade.InsertRowAtAsync(contextRowIndex + 1 + i, null);
        }
    };
    menuFlyout.Items.Add(insertBelow);

    menuFlyout.Items.Add(new MenuFlyoutSeparator());

    // Duplicate This Row
    var duplicateRow = new MenuFlyoutItem { Text = "Duplicate This Row" };
    duplicateRow.Click += async (s, e) =>
    {
        var rowData = await GetRowDataAsync(contextRowIndex);
        await _facade.InsertRowAfterAsync(contextRowId, rowData);
    };
    menuFlyout.Items.Add(duplicateRow);

    // Delete Row
    var deleteRow = new MenuFlyoutItem { Text = "Delete Row" };
    deleteRow.Click += async (s, e) => await _facade.DeleteRowAsync(contextRowId);
    menuFlyout.Items.Add(deleteRow);

    // Show menu
    menuFlyout.ShowAt(_cellsView, new Microsoft.UI.Xaml.FlyoutShowOptions
    {
        Position = _lastRightClickPosition
    });
}
```

**ŽIADNY "Insert Multiple Rows..." s dialogom!**

#### D) Toolbar Buttons

```csharp
// Insert Row at Top
var insertAtTopBtn = new Button { Content = "Insert Row at Top" };
insertAtTopBtn.Click += async (s, e) => await _facade.InsertRowAtTopAsync();

// Insert Before Selection (počet = počet označených)
var insertBeforeSelBtn = new Button { Content = "Insert Row(s) Before Selection" };
insertBeforeSelBtn.Click += async (s, e) =>
{
    var selectedIndices = GetSelectedRowIndices();
    if (selectedIndices.Count == 0)
    {
        _logger.LogWarning("No rows selected for insert before");
        return;
    }

    var firstIndex = selectedIndices.Min();
    for (int i = 0; i < selectedIndices.Count; i++)
    {
        await _facade.InsertRowAtAsync(firstIndex + i, null);
    }
};

// Insert After Selection (počet = počet označených)
var insertAfterSelBtn = new Button { Content = "Insert Row(s) After Selection" };
insertAfterSelBtn.Click += async (s, e) =>
{
    var selectedIndices = GetSelectedRowIndices();
    if (selectedIndices.Count == 0)
    {
        _logger.LogWarning("No rows selected for insert after");
        return;
    }

    var lastIndex = selectedIndices.Max();
    for (int i = 0; i < selectedIndices.Count; i++)
    {
        await _facade.InsertRowAtAsync(lastIndex + 1 + i, null);
    }
};
```

#### E) Keyboard Shortcuts

```csharp
// KeyDown handler:
private async void OnKeyDown(object sender, KeyRoutedEventArgs e)
{
    bool ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
    bool shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
    bool altPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);

    if (e.Key == VirtualKey.Add) // Plus key (numpad or main keyboard)
    {
        if (ctrlPressed && shiftPressed)
        {
            // Ctrl+Shift+Plus → Insert AFTER selection
            var selectedIndices = GetSelectedRowIndices();
            if (selectedIndices.Any())
            {
                var lastIndex = selectedIndices.Max();
                for (int i = 0; i < selectedIndices.Count; i++)
                {
                    await _facade.InsertRowAtAsync(lastIndex + 1 + i, null);
                }
            }

            e.Handled = true;
        }
        else if (ctrlPressed && altPressed)
        {
            // Ctrl+Alt+Plus → Insert BEFORE selection
            var selectedIndices = GetSelectedRowIndices();
            if (selectedIndices.Any())
            {
                var firstIndex = selectedIndices.Min();
                for (int i = 0; i < selectedIndices.Count; i++)
                {
                    await _facade.InsertRowAtAsync(firstIndex + i, null);
                }
            }

            e.Handled = true;
        }
    }
}
```

---

## ✅ Výsledky

Po implementácii:

1. **➕ Special Column:**
   - ✅ Poradie: 5 (RowNumber, Checkbox, DATA, ValidationAlerts, **InsertRow**, DeleteRow)
   - ✅ Kliknutie → Insert prázdny riadok BELOW
   - ✅ Metadata: __creationType = "UserInserted"

2. **Context Menu:**
   - ✅ Insert Above/Below
   - ✅ Continuous block logic (označené [3,5,6] → context na 5 → insert 2 riadky)
   - ✅ ŽIADNY DIALOG

3. **API Metódy:**
   - ✅ InsertRowAfterAsync (STREAMING)
   - ✅ InsertRowBeforeAsync (STREAMING)
   - ✅ InsertRowAtTopAsync
   - ✅ InsertRowsAfterAsync (bulk)
   - ✅ NEPOUŽÍVA GetAllRowsAsync!

4. **Toolbar & Shortcuts:**
   - ✅ Insert at Top button
   - ✅ Insert Before/After Selection buttons
   - ✅ Ctrl+Shift+Plus / Ctrl+Alt+Plus

---

## 🧪 Testing

### Test Case 1: Special Column Click
```csharp
// 1. Click ➕ on row 3
// 2. Assert: New empty row inserted at index 4
// 3. Assert: Metadata __creationType = "UserInserted"
```

### Test Case 2: Continuous Block Logic
```csharp
// 1. Select rows [2, 4, 5]
// 2. Right-click on row 4 → "Insert Row Below"
// 3. Assert: 2 rows inserted (continuous block [4,5])

// 4. Select rows [2, 4, 5]
// 5. Right-click on row 2 → "Insert Row Below"
// 6. Assert: 1 row inserted (non-continuous)
```

### Test Case 3: Streaming Insert (No GetAllRows!)
```csharp
// 1. Load 10,000 rows
// 2. Insert row at index 5000
// 3. Assert: Memory usage stays below 1 GB
// 4. Assert: InsertRowAtAsync was called (not GetAllRowsAsync)
```

---

## 📝 Dependencies

- **IRowStore:** Musí implementovať `InsertRowAtAsync` (nová metóda)
- **Cleanup Logic:** Pre metadata (__creationType, __lastModified)

---

**Posledná aktualizácia:** 2025-10-20
**Status:** Ready for Implementation

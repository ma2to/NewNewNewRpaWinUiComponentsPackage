# Phase 13: Internal Services rowId Migration

## IDENTIFIKOVANÉ METÓDY A KÓD POUŽÍVAJÚCI rowIndex/columnIndex

Kompletný audit vnútorných služieb a interných API, ktoré stále používajú volatile `rowIndex`/`columnIndex` namiesto stable `rowId`/`columnName`.

---

## 1. VALIDATION SERVICE - KRITICKÉ ⚠️

**File:** `ValidationService.cs`

### Metódy na refaktoring:

```csharp
// Line 840
Task<ValidationResult> ValidateCellAsync(int rowIndex, string columnName, object? newValue, ...)
// SHOULD BE: ValidateCellAsync(string rowId, string columnName, object? newValue, ...)

// Line 1011
string GetValidationAlertsForRow(int rowIndex)
// SHOULD BE: GetValidationAlertsForRow(string rowId)

// Line 1035
Task<Result> UpdateValidationAlertsAsync(int rowIndex, IReadOnlyList<ValidationResult> results, ...)
// SHOULD BE: UpdateValidationAlertsAsync(string rowId, IReadOnlyList<ValidationResult> results, ...)
```

### Internal storage issue:

```csharp
// Line 514 - ValidationContext
var context = new ValidationContext
{
    RowIndex = absoluteRowIndex,  // PROBLÉM: volatile index
    OperationId = operationId.ToString()
};
// SHOULD ADD: RowId field to ValidationContext
```

### Internal calls using deprecated methods:

```csharp
// Line 852, 1016, 1048, 1055, 1070, 1077
var row = await _rowStore.GetRowAsync(rowIndex, cancellationToken);  // DEPRECATED
// SHOULD USE: _rowStore.GetRowById(rowId)
```

**Priority:** CRITICAL - Validation fails after row delete/sort because rowIndex becomes invalid

---

## 2. COLOR SERVICE - KRITICKÉ ⚠️

**File:** `ColorService.cs` + `IColorService.cs`

### Public API methods:

```csharp
Task SetCellBackgroundColorAsync(int rowIndex, string columnName, string color, ...)
// SHOULD BE: SetCellBackgroundColorAsync(string rowId, string columnName, string color, ...)

Task SetCellForegroundColorAsync(int rowIndex, string columnName, string color, ...)
// SHOULD BE: SetCellForegroundColorAsync(string rowId, string columnName, string color, ...)

Task SetRowBackgroundColorAsync(int rowIndex, string color, ...)
// SHOULD BE: SetRowBackgroundColorAsync(string rowId, string color, ...)

Task ClearCellColorsAsync(int rowIndex, string columnName, ...)
// SHOULD BE: ClearCellColorsAsync(string rowId, string columnName, ...)
```

### Internal storage issue:

```csharp
// Line 88 - ColorConfiguration stores RowIndex and ColumnIndex
ColorMode.Cell => kvp.Value.RowIndex == command.RowIndex && kvp.Value.ColumnIndex == command.ColumnIndex
ColorMode.Row => kvp.Value.RowIndex == command.RowIndex
```

**ColorCommand/ColorConfiguration models need:**
- Replace `int RowIndex` with `string RowId`
- Replace `int ColumnIndex` with `string ColumnName`

**Priority:** CRITICAL - Colors applied to wrong rows after sort/filter/delete operations

---

## 3. SELECTION SERVICE - KRITICKÉ ⚠️

**File:** `SelectionService.cs`

### Internal storage - MAJOR ISSUE:

```csharp
// Line 21 - PROBLÉM: Dictionary key je rowIndex namiesto rowId
private readonly ConcurrentDictionary<int, bool> _selectedRows = new();
// SHOULD BE: private readonly ConcurrentDictionary<string, bool> _selectedRows = new();
```

### Methods to refactor:

```csharp
// Lines 41-72
void SelectCell(int row, int col)
// SHOULD BE: SelectCell(string rowId, string columnName)

void StartDragSelect(int row, int col)
void DragSelectTo(int row, int col)
void EndDragSelect(int row, int col)
void ToggleCellSelection(int row, int col)
void ExtendSelectionTo(int row, int col)
// ALL SHOULD USE: (string rowId, string columnName)
```

**Priority:** CRITICAL - Selection lost/shifted after row delete/sort operations

---

## 4. AUTO ROW HEIGHT SERVICE - KRITICKÉ ⚠️

**File:** `AutoRowHeightService.cs` + `IAutoRowHeightService.cs`

### Interface method:

```csharp
Task<Common.Models.Result<double>> AdjustRowHeightAsync(int rowIndex, CancellationToken cancellationToken = default)
// SHOULD BE: AdjustRowHeightAsync(string rowId, CancellationToken cancellationToken = default)
```

**Note:** Public API (IDataGridAutoRowHeight) already has rowId version. This is the INTERNAL service interface.

**Priority:** CRITICAL - Internal service must match public API pattern

---

## 5. COLUMN SERVICE - STREDNÁ PRIORITA 🔶

**File:** `ColumnService.cs` + `IColumnResizeService.cs`

### Methods using columnIndex:

```csharp
void StartColumnResize(int columnIndex, double clientX)
void UpdateColumnResize(int columnIndex, double clientX)
void EndColumnResize(int columnIndex, double clientX)
Task<Result> ResizeColumnAsync(int columnIndex, double newWidth, ...)
double GetColumnWidth(int columnIndex)
```

**SHOULD USE:** `columnName` (stable identifier) instead of `columnIndex` (changes with column reorder/hide)

**Priority:** MEDIUM - columnIndex is volatile across column reordering operations

---

## 6. IROWSTORE DEPRECATED METHODS - UŽ OZNAČENÉ ✅

**File:** `IRowStore.cs`

### Already marked DEPRECATED in comments:

```csharp
// Line 258 - DEPRECATED (lines 253-254 comment)
Task<IReadOnlyDictionary<string, object?>?> GetRowAsync(int rowIndex, ...)
// Replacement exists: GetRowByIdAsync(string rowId) - lines 226-228

// Line 269 - DEPRECATED (lines 263-264 comment)
Task<bool> UpdateRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, ...)
// Replacement exists: UpdateRowByIdAsync(string rowId) - lines 237-240

// Line 392 - Compatibility API
Task InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, ...)

// Line 394 - Compatibility API
Task<int> RemoveRowsAsync(IEnumerable<int> rowIndices, ...)
// Overload exists: RemoveRowsAsync(IEnumerable<string> rowIds) - lines 216-218
```

**Status:** Already documented as DEPRECATED, but methods still exist for compatibility.

**Priority:** MEDIUM - Can add [Obsolete] attributes in future cleanup

---

## 7. INTERNAL UI OPERATION HANDLER - ČÁSTEČNĚ OPRAVENÉ ⚠️

**File:** `InternalUIOperationHandler.cs`

### Already detected CS0618 warning:

```csharp
// Line 113 - Uses deprecated method
await _dataGridFacade.Rows.InsertRowAsync(insertIndex, null, cancellationToken);
```

**Compiler warning:**
```
CS0618: 'IDataGridRows.InsertRowAsync(int, IReadOnlyDictionary<string, object?>, CancellationToken)'
is obsolete: 'Use InsertRowBeforeIdAsync or InsertRowAfterIdAsync instead.'
```

**Should be:**
```csharp
string? referenceRowId = _dataGridFacade.Rows.GetRowIdByIndex(insertIndex - 1);
if (referenceRowId != null)
    await _dataGridFacade.Rows.InsertRowAfterIdAsync(referenceRowId, null, cancellationToken);
else
    await _dataGridFacade.Rows.AddRowAsync(new Dictionary<string, object?>(), cancellationToken);
```

### Already migrated (GOOD):

```csharp
// Line 194 - používa rowId ✅
await _dataGridFacade.Editing.UpdateCellAsync(rowId, columnName, newValue, cancellationToken);
```

**Priority:** HIGH - Fix the CS0618 warning

---

## 8. CELL EDIT SERVICE - UŽ MIGROVANÉ ✅

**File:** `CellEditService.cs`

**Status:** GOOD EXAMPLE of migration pattern
- Old rowIndex methods COMMENTED OUT (lines 42-100, 164-284, 541-556)
- New rowId methods implemented (lines 108, 292, 563)
- Internal conversion still uses rowIndex for backward compatibility (line 330)

### Remaining internal issue:

```csharp
// Line 330 - Validation context still needs rowIndex
var rowIndex = _rowStore.GetRowIndexById(rowId);  // CONVERTS BACK TO INDEX
```

**Recommendation:** ValidationContext should evolve to accept rowId instead of rowIndex

**Priority:** LOW - Already migrated to public rowId API

---

## SUMMARY TABLE

| Component | File | Methods Count | Priority | Status |
|-----------|------|---------------|----------|--------|
| **ValidationService** | ValidationService.cs | 3 public + ValidationContext | 🔴 CRITICAL | Not started |
| **ColorService** | ColorService.cs | 4+ public + internal storage | 🔴 CRITICAL | Not started |
| **SelectionService** | SelectionService.cs | 1 storage + 6 methods | 🔴 CRITICAL | Not started |
| **AutoRowHeightService** | AutoRowHeightService.cs | 1 interface method | 🔴 CRITICAL | Not started |
| **ColumnService** | ColumnService.cs | 5 methods | 🔶 MEDIUM | Not started |
| **InternalUIOperationHandler** | InternalUIOperationHandler.cs | 1 method (CS0618) | 🟡 HIGH | Detected, not fixed |
| **IRowStore** | IRowStore.cs | 4 deprecated methods | 🔶 MEDIUM | Already marked DEPRECATED |
| **CellEditService** | CellEditService.cs | - | ✅ COMPLETE | Already migrated |

---

## PRIORITA REFAKTORINGU

### KRITICKÉ (blocker pre v3.0) 🔴

1. **ValidationService** - 3 metódy + ValidationContext model
   - `ValidateCellAsync(int rowIndex, ...)` → `ValidateCellAsync(string rowId, ...)`
   - `GetValidationAlertsForRow(int rowIndex)` → `GetValidationAlertsForRow(string rowId)`
   - `UpdateValidationAlertsAsync(int rowIndex, ...)` → `UpdateValidationAlertsAsync(string rowId, ...)`
   - Add `RowId` field to `ValidationContext`, deprecate `RowIndex`

2. **ColorService** - 4+ metódy + internal storage
   - All public methods: rowIndex → rowId
   - ColorCommand/ColorConfiguration models: RowIndex → RowId, ColumnIndex → ColumnName
   - Internal dictionary storage update

3. **SelectionService** - ConcurrentDictionary<int> + 6 metód
   - `ConcurrentDictionary<int, bool>` → `ConcurrentDictionary<string, bool>`
   - All drag-select methods: (int row, int col) → (string rowId, string columnName)

4. **AutoRowHeightService** - 1 internal interface metóda
   - `IAutoRowHeightService.AdjustRowHeightAsync(int rowIndex, ...)` → `AdjustRowHeightAsync(string rowId, ...)`

### VYSOKÁ (pred v3.0) 🟡

5. **InternalUIOperationHandler** - Fix CS0618 warning
   - Replace deprecated `InsertRowAsync(int)` with `InsertRowAfterIdAsync(string)`

### STREDNÁ (môže počkať) 🔶

6. **ColumnService** - columnIndex → columnName (5 metód)
   - Refactor resize methods to use stable columnName

7. **IRowStore** - Compatibility methods cleanup
   - Add [Obsolete] attributes to deprecated methods
   - Eventually remove in final v3.0.0

### AKCEPTOVATEĽNÉ (žiadna zmena) ✅

- **Viewport/rendering kód** - potrebuje visual index pre UI positioning
- **CellEditService** - už migrované

---

## MIGRATION PATTERN

Based on successful CellEditService migration:

### Step 1: Create rowId overload
```csharp
// New method
public async Task<Result> MethodAsync(string rowId, ..., CancellationToken cancellationToken = default)
{
    var rowIndex = _rowStore.GetRowIndexById(rowId);
    if (rowIndex == null)
        return Result.Failure($"Row {rowId} not found");

    // Use existing internal implementation with rowIndex
    return await InternalMethodAsync(rowIndex.Value, ..., cancellationToken);
}
```

### Step 2: Mark old method as Obsolete
```csharp
[Obsolete("Use MethodAsync(string rowId, ...) instead. rowIndex is unstable and changes on sort/filter/delete operations.", false)]
public async Task<Result> MethodAsync(int rowIndex, ..., CancellationToken cancellationToken = default)
{
    // Keep for backward compatibility
}
```

### Step 3: Comment out old implementation (optional)
```csharp
/* COMMENTED OUT - OLD METHOD (rowIndex-based, unstable across sort/filter)
public async Task<Result> MethodAsync(int rowIndex, ...)
{
    // Old implementation kept for reference
}
*/
```

### Step 4: Update internal storage
```csharp
// OLD:
private readonly ConcurrentDictionary<int, ModelData> _storage = new();

// NEW:
private readonly ConcurrentDictionary<string, ModelData> _storage = new();
```

---

## ESTIMATED EFFORT

- ValidationService: 4 hours (3 methods + model + tests)
- ColorService: 6 hours (4 methods + models + storage + tests)
- SelectionService: 8 hours (storage migration + 6 methods + tests)
- AutoRowHeightService: 2 hours (1 method + tests)
- InternalUIOperationHandler: 1 hour (fix CS0618)
- ColumnService: 4 hours (5 methods + tests)

**Total:** ~25 hours

---

## BREAKING CHANGES FOR v3.0.0

### Internal Service Interfaces Changed:
- `IValidationService` - 3 methods signature change
- `IColorService` - 4 methods signature change
- `ISelectionService` - 6 methods signature change
- `IAutoRowHeightService` - 1 method signature change
- `IColumnResizeService` - 5 methods signature change (if refactored)

### Internal Models Changed:
- `ValidationContext` - added RowId, deprecated RowIndex
- `ColorCommand` - RowIndex → RowId, ColumnIndex → ColumnName
- `ColorConfiguration` - RowIndex → RowId, ColumnIndex → ColumnName
- `SelectionService` internal storage - key change from int to string

### Backward Compatibility:
- Old methods marked [Obsolete] - generate CS0618 warnings
- Can be suppressed with `#pragma warning disable CS0618`
- Will be removed in v4.0.0

---

## SUCCESS CRITERIA

✅ All CRITICAL services refactored to use rowId/columnName
✅ No CS0618 warnings in internal codebase (except explicitly suppressed)
✅ All tests passing with rowId-based APIs
✅ Build succeeds with 0 errors
✅ Performance benchmarks show no regression
✅ Documentation updated with migration guide

---

## NEXT STEPS

1. Start with ValidationService (highest impact on data integrity)
2. Then ColorService (user-visible bug - colors on wrong rows)
3. Then SelectionService (user-visible bug - selection lost)
4. Then AutoRowHeightService (align internal with public API)
5. Fix InternalUIOperationHandler CS0618 warning
6. (Optional) ColumnService columnName migration

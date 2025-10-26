# 📋 FINÁLNY KOMPLEXNÝ VÝPIS IMPLEMENTÁCIE - ÚPLNÁ ANALÝZA

**Dátum:** 20.10.2025
**Autor:** Claude (Senior Developer Analysis)
**Účel:** Kompletná technická dokumentácia WinUI DataGrid komponentu s návrhom nových funkcionalít

---

## ✅ 1. INMEMORYROWSTORE vs HYBRIDROWSTORE - FUNKCIONALITA Z POHĽADU POUŽÍVATEĽA

### **ZISTENIA:**

✅ **OBE implementácie majú ROVNAKÚ funkcionalitu** - implementujú interface `IRowStore`:

**Spoločné metódy (relevantné pre validation/deletion):**
- `StreamRowsAsync(onlyFiltered, onlyChecked, batchSize, cancellationToken)` ✅
  - **InMemoryRowStore.cs:221**
  - **HybridRowStore.cs:822**
- `GetAllRowsAsync(onlyFiltered, cancellationToken)` ✅
- `GetRowCountAsync(cancellationToken)` ✅
- `SetFilterCriteria(filterCriteria)` ✅
- `IsRowValidationCached(rowId)` ✅
- `RemoveRowsAsync(rowIds)` ✅
- `InsertRowsAsync(rows, startIndex)` ✅

### **ZÁVER:**

✅ **Implementované zmeny (ValidationService.cs, ValidationDeletionService.cs) fungujú IDENTICKY pre obe implementácie** - používajú len `IRowStore` interface metódy.

---

## 🔄 2. MECHANIZMUS MAZANIA A PRIDÁVANIA - NOVÁ ARCHITEKTÚRA

### **KRITICKÁ ZMENA ARCHITEKTÚRY:**

⚠️ **PO NOVOM: Riadky ako objekty sa NEMAŽÚ ani NEPRIDÁVAJÚ fyzicky**
⚠️ **PO NOVOM: Mažú/Pridávajú sa len DÁTA, riadky sa POSÚVAJÚ**

### **ULID A UI RIADKY - DÔLEŽITÉ VYSVETLENIE:**

✅ **DÁTOVÉ RIADKY (Storage Layer):**
- Každý dátový riadok má svoj **stabilný ULID identifikátor**
- ULID **SA NEMENÍ** pri posune dát medzi UI riadkami
- ULID je **VIAZANÝ NA DÁTA**, nie na UI pozíciu
- Príklad: Dáta "John Doe, 25" majú ULID `01HXABC...005` - tento ULID zostáva pri týchto dátach aj keď sa presunú z UI riadku 5 na UI riadok 3

✅ **UI RIADKY (Display Layer):**
- UI má **rôzny počet riadkov** podľa nastavenia (napr. 100 objektov na stránku)
- Každý UI riadok **ZOBRAZUJE dáta** z dátového riadku
- UI riadok **DOSTANE ULID** dátového riadku ktorý práve zobrazuje
- **ULID SA NEZOBRAZUJE** používateľovi (je to interný identifikátor)
- UI riadok index 0 môže dnes zobrazovať dáta s ULID `01HXABC...001`, zajtra (po posune) dáta s ULID `01HXABC...003`

### **NOVÝ MECHANIZMUS MAZANIA:**

**PO NOVOM (nová implementácia):**
```
MAZANIE RIADKU (napr. UI riadok index 5):
1. UI Riadok 5: Dáta s ULID `01HXABC...006` sa VYMAŽÚ (všetky bunky → null/empty)
2. UI Riadky 6-99: Dáta sa POSUNÚ NAHOR o 1
   - UI riadok 6 dostane dáta z UI riadku 7 (s ich ULID)
   - UI riadok 7 dostane dáta z UI riadku 8 (s ich ULID)
   - atď.
3. UI Riadok 99: Dostane prázdne dáta (null/empty, žiadny ULID alebo nový ULID pre prázdny riadok)
4. FYZICKY: 100 UI riadkov zostáva, len dáta (vrátane ULID) sa presúvajú
```

**ČO TO ZNAMENÁ:**
- ✅ **Počet UI riadkov = KONŠTANTNÝ PER PAGE** (vždy 100 na stránku, ak `options.RowsPerPage = 100`)
- ✅ **UI Riadky ako objekty = ZACHOVANÉ** (Dictionary má stále 100 UI objektov per page)
- ✅ **ULID = VIAZANÝ NA DÁTA** (ULID sa presúva s dátami, nie s UI pozíciou)
- ✅ **Dáta sa presúvajú** medzi UI riadkami (shift up)
- ✅ **Posledný UI riadok** dostane prázdne dáta (recyklácia)

### **NOVÝ MECHANIZMUS PRIDÁVANIA:**

**PO NOVOM (nová implementácia):**
```
PRIDANIE RIADKU (napr. pod UI riadok index 5):
1. UI Riadky 6-99: Dáta sa POSUNÚ DOLE o 1
   - UI riadok 99 dostane dáta z UI riadku 98 (s ich ULID)
   - UI riadok 98 dostane dáta z UI riadku 97 (s ich ULID)
   - atď.
2. UI Riadok 6 (nová pozícia): Dostane PRÁZDNE dáta (null/empty, nový ULID pre prázdny riadok)
3. UI Riadok 99: Stratí svoje pôvodné dáta (prepíšu sa dátami z UI riadku 98)
4. FYZICKY: 100 UI riadkov zostáva, len dáta (vrátane ULID) sa presúvajú
```

**ČO TO ZNAMENÁ:**
- ✅ **Počet UI riadkov = KONŠTANTNÝ PER PAGE** (vždy 100 na stránku)
- ✅ **UI Riadky ako objekty = ZACHOVANÉ**
- ✅ **ULID = VIAZANÝ NA DÁTA** (presúva sa s dátami)
- ✅ **Dáta sa presúvajú** medzi UI riadkami (shift down)
- ✅ **Nový prázdny riadok** sa objaví na požadovanom mieste (UI index 6)

### **PRÍKLAD - MAZANIE UI RIADKU 8 V TABULKE S 150 DÁTOVÝMI RIADKAMI (2 PAGES):**

**PRED MAZANÍM:**
```
PAGE 1 (zobrazuje UI riadky 0-99):
UI Index | Dátový ULID      | Zobrazené Dáta
---------|------------------|------------------
0        | 01HXABC...001   | "Row 0 data"
1        | 01HXABC...002   | "Row 1 data"
2        | 01HXABC...003   | "Row 2 data"
...
7        | 01HXABC...008   | "Row 7 data"
8        | 01HXABC...009   | "Row 8 data" ← MAŽEME TIETO DÁTA
9        | 01HXABC...010   | "Row 9 data"
...
99       | 01HXABC...100   | "Row 99 data"

PAGE 2 (zobrazuje UI riadky 100-149):
UI Index | Dátový ULID      | Zobrazené Dáta
---------|------------------|------------------
100      | 01HXABC...101   | "Row 100 data"
101      | 01HXABC...102   | "Row 101 data"
...
149      | 01HXABC...150   | "Row 149 data"

CELKOVO: 150 dátových riadkov (2 pages × 100 UI riadkov = možnosť zobrazenia až 200)
```

**PO MAZANÍ (nová architektúra - POSUN DÁT NAPRIEČ CELOU TABUĽKOU):**
```
PAGE 1 (zobrazuje UI riadky 0-99):
UI Index | Dátový ULID      | Zobrazené Dáta           | Zmena
---------|------------------|--------------------------|------------------
0        | 01HXABC...001   | "Row 0 data"             | BEZ ZMENY
1        | 01HXABC...002   | "Row 1 data"             | BEZ ZMENY
2        | 01HXABC...003   | "Row 2 data"             | BEZ ZMENY
...
7        | 01HXABC...008   | "Row 7 data"             | BEZ ZMENY
8        | 01HXABC...010   | "Row 9 data"             | ← Dáta + ULID z UI riadku 9 (preskočené row 8)
9        | 01HXABC...011   | "Row 10 data"            | ← Dáta + ULID z UI riadku 10
...
79       | 01HXABC...080   | "Row 79 data"            | ← Dáta + ULID z UI riadku 80
80       | 01HXABC...081   | "Row 80 data"            | ← Dáta + ULID z UI riadku 81
81       | 01HXABC...082   | "Row 81 data"            | ← Dáta + ULID z UI riadku 82
...
99       | 01HXABC...100   | "Row 99 data"            | ← Dáta + ULID z UI riadku 100

PAGE 2 (zobrazuje UI riadky 100-149):
UI Index | Dátový ULID      | Zobrazené Dáta           | Zmena
---------|------------------|--------------------------|------------------
100      | 01HXABC...101   | "Row 100 data"           | ← Dáta + ULID z UI riadku 101
101      | 01HXABC...102   | "Row 101 data"           | ← Dáta + ULID z UI riadku 102
...
148      | 01HXABC...149   | "Row 148 data"           | ← Dáta + ULID z UI riadku 149
149      | (prázdny/nový)  | null (prázdne)           | ← Recyklovaný UI riadok (row 150 stratený)

CELKOVO: 149 dátových riadkov (zmazaný row 8, všetky nasledujúce posunuté hore)
```

**POZNÁMKA:**
- ULID `01HXABC...009` (pôvodne na UI indexe 8) bol VYMAZANÝ spolu s dátami
- Dáta sa posunuli naprieč **CELOU TABUĽKOU** (všetky pages)
- UI riadok 8 dostal dáta z UI riadku 9
- UI riadok 80 dostal dáta z UI riadku 81
- UI riadok 81 dostal dáta z UI riadku 82
- UI riadok 100 dostal dáta z UI riadku 101
- UI riadok 101 dostal dáta z UI riadku 102
- Posledný UI riadok (149) dostal prázdne dáta

### **PRÍKLAD - PRIDANIE RIADKU POD UI INDEX 5:**

**PO PRIDANÍ (nová architektúra):**
```
UI Index | Dátový ULID      | Zobrazené Dáta           | Zmena
---------|------------------|--------------------------|------------------
0        | 01HXABC...001   | "Row 0 data"             | BEZ ZMENY
1        | 01HXABC...002   | "Row 1 data"             | BEZ ZMENY
2        | 01HXABC...003   | "Row 2 data"             | BEZ ZMENY
3        | 01HXABC...004   | "Row 3 data"             | BEZ ZMENY
4        | 01HXABC...005   | "Row 4 data"             | BEZ ZMENY
5        | 01HXABC...006   | "Row 5 data"             | BEZ ZMENY
6        | 01HXNEW...777   | null (prázdne - NOVÝ)    | ← Nový ULID + prázdne dáta
7        | 01HXABC...007   | "Row 6 data"             | ← Dáta + ULID z UI riadku 6
8        | 01HXABC...008   | "Row 7 data"             | ← Dáta + ULID z UI riadku 7
...
99       | 01HXABC...099   | "Row 98 data"            | ← Dáta z UI riadku 98 (row 99 stratený)
```

### **ZÁVER:**

✅ **UI RIADKY AKO OBJEKTY = NEMENNÉ** (vždy fixný počet na stránku, napr. 100)
✅ **ULID = VIAZANÝ NA DÁTA** (presúva sa s dátami, nie s UI pozíciou)
✅ **DÁTA = POHYBLIVÉ** (presúvajú sa medzi UI riadkami NAPRIEČ CELOU TABUĽKOU)
✅ **FYZICKY SA NIC NEMAŽE ANI NEPRIDÁVA** (len dátový obsah sa mení)
✅ **RECYKLÁCIA** = posledný UI riadok dostáva prázdne dáta pri mazaní
✅ **ULID SA NEZOBRAZUJE** používateľovi (je to interný identifikátor pre tracking dát)
✅ **MULTI-PAGE SUPPORT** = dáta sa posúvajú naprieč všetkými stránkami (napr. 150 riadkov = 2 pages)

---

## ⚠️ 3. ROWS FEATURE - NOVÁ METÓDA DELETEROWSBYIDASYNC

### **ZMENA: SmartDeleteRowsByIdAsync → DeleteRowsByIdAsync**

❌ **ZMAZAŤ:** Celý folder `Features/SmartAddDelete/`
✅ **NAHRADIŤ:** Novou metódou v `Features/Rows/`

### **NOVÁ ŠTRUKTÚRA:**

```
Features/
├── Rows/                           ← NOVÝ FOLDER
│   ├── Services/
│   │   └── RowManagementService.cs   ← NOVÁ TRIEDA
│   ├── Commands/
│   │   └── DeleteRowsByIdCommand.cs  ← NOVÝ COMMAND
│   └── Interfaces/
│       └── IRowManagementService.cs   ← NOVÝ INTERFACE
└── SmartAddDelete/                 ← ZMAZAŤ CELÝ FOLDER
    ├── Services/
    │   └── SmartOperationService.cs   ← ZMAZAŤ
    ├── Commands/
    │   │   └── SmartOperationCommand.cs   ← ZMAZAŤ
    └── Interfaces/
        └── ISmartOperationService.cs  ← ZMAZAŤ
```

### **NOVÁ IMPLEMENTÁCIA:**

**RowManagementService.cs** (nová trieda):
```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Services;

/// <summary>
/// Service for managing row operations (delete, insert, update)
/// NEW ARCHITECTURE: Works with data shifting, not physical row deletion
/// Maintains constant row count per page (e.g., 100 rows per page always)
/// </summary>
internal sealed class RowManagementService : IRowManagementService
{
    private readonly ILogger<RowManagementService> _logger;
    private readonly IRowStore _rowStore;

    public RowManagementService(
        ILogger<RowManagementService> logger,
        IRowStore rowStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
    }

    /// <summary>
    /// Delete rows by ID - NEW ARCHITECTURE
    /// Does NOT physically delete row objects
    /// Instead: Clears data, shifts remaining data up across ALL pages, recycles last row
    /// </summary>
    public async Task<RowManagementResult> DeleteRowsByIdAsync(
        DeleteRowsByIdCommand command,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        _logger.LogInformation(
            "Starting delete rows by ID operation {OperationId}: rowIdsToDelete={Count}",
            operationId, command.RowIdsToDelete.Count);

        try
        {
            // STEP 1: Get all rows and find indices of rows to delete
            var allRows = (await _rowStore.GetAllRowsAsync(cancellationToken)).ToList();
            var deletedIndices = new List<int>();

            for (int i = 0; i < allRows.Count; i++)
            {
                if (allRows[i].TryGetValue("__rowId", out var rowIdValue))
                {
                    var rowId = rowIdValue?.ToString();
                    if (rowId != null && command.RowIdsToDelete.Contains(rowId))
                    {
                        deletedIndices.Add(i);
                    }
                }
            }

            if (deletedIndices.Count == 0)
            {
                _logger.LogWarning("No rows found with provided IDs - operation skipped");
                stopwatch.Stop();
                return RowManagementResult.CreateSuccess(
                    allRows.Count,
                    0,
                    RowOperationType.Delete,
                    stopwatch.Elapsed,
                    new RowManagementStatistics());
            }

            // STEP 2: Shift data up (NEW ARCHITECTURE) - across ALL pages
            await _rowStore.ShiftRowDataAfterDeletionAsync(deletedIndices, cancellationToken);

            _logger.LogInformation(
                "Delete rows by ID operation {OperationId} completed: {Count} rows deleted (data cleared + shifted across all pages)",
                operationId, deletedIndices.Count);

            stopwatch.Stop();
            return RowManagementResult.CreateSuccess(
                allRows.Count - deletedIndices.Count, // Row count decreased by deleted count
                deletedIndices.Count,
                RowOperationType.Delete,
                stopwatch.Elapsed,
                new RowManagementStatistics
                {
                    RowsDataCleared = deletedIndices.Count,
                    RowsDataShifted = allRows.Count - deletedIndices.Max() - 1
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete rows by ID operation {OperationId} failed: {Message}",
                operationId, ex.Message);
            return RowManagementResult.CreateFailure(
                RowOperationType.Delete,
                new[] { $"Delete by ID failed: {ex.Message}" },
                stopwatch.Elapsed);
        }
    }
}
```

### **ZMENY V API FACADE:**

**IAdvancedDataGridFacade.cs** - upraviť:
```csharp
// STARÁ VLASTNOSŤ (zmazať):
// IDataGridSmartOperations SmartOperations { get; }

// NOVÁ VLASTNOSŤ (pridať):
IDataGridRows Rows { get; }
```

**IDataGridRows.cs** (nový interface):
```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public interface for row management operations
/// NEW ARCHITECTURE: Data shifting instead of physical deletion
/// </summary>
public interface IDataGridRows
{
    /// <summary>
    /// Delete rows by their IDs
    /// NEW: Does NOT physically delete row objects
    /// Instead: Clears data, shifts remaining data up across ALL pages, recycles last row
    /// Row count decreases (e.g., 150 rows → 142 rows after deleting 8)
    /// </summary>
    Task<RowManagementResult> DeleteRowsByIdAsync(
        IEnumerable<string> rowIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert empty rows at specified index
    /// NEW: Does NOT physically add row objects
    /// Instead: Shifts data down, creates empty data at insert position
    /// </summary>
    Task<RowManagementResult> InsertEmptyRowsAtAsync(
        int insertIndex,
        int rowCount = 1,
        CancellationToken cancellationToken = default);
}
```

### **POUŽITIE V APLIKÁCII:**

```csharp
// STARÉ (zmazať):
// await _gridFacade.SmartOperations.SmartDeleteRowsByIdAsync(...);

// NOVÉ (použiť):
await _gridFacade.Rows.DeleteRowsByIdAsync(rowIds, cancellationToken);
```

### **ČO SA ZMAZALO:**

❌ **SmartAddDelete folder** - celý
❌ `SmartOperationService.cs` - zmazať
❌ `EnsureMinRowsAndLastEmptyAsync` - zmazať
❌ `SmartAddLastEmptyRowAsync` - zmazať
❌ `SmartDeleteRowsByIdAsync` - nahradiť `DeleteRowsByIdAsync`
❌ `IDataGridSmartOperations` interface - nahradiť `IDataGridRows`

### **OVERENIE - ČI SA MÔŽE ZMAZAŤ CELÝ SMARTADDDELETE:**

✅ **ÁNO** - SmartAddDelete obsahuje len metódy ktoré sú nahradené:
- `SmartDeleteRowsByIdAsync` → `DeleteRowsByIdAsync` (nová architektúra)
- `EnsureMinRowsAndLastEmptyAsync` → už nie je potrebná (data shift)
- `SmartAddLastEmptyRowAsync` → už nie je potrebná (data shift)

✅ **ValidationDeletionService.cs:126** - ZMENIŤ:
```csharp
// STARÉ:
var deleteResult = await _smartOperations.SmartDeleteRowsByIdAsync(deleteCommand, cancellationToken);

// NOVÉ:
var deleteResult = await _rowManagementService.DeleteRowsByIdAsync(rowIds, cancellationToken);
```

---

## ✅ 4. VALIDAČNÉ PRAVIDLÁ - KOMPOZITNÉ AND/OR

### **IMPLEMENTÁCIA KOMPOZITNÝCH PRAVIDIEL:**

✅ **IMPLEMENTOVAŤ LEN:**
- `AndValidationRule` - (rule1 AND rule2)
- `OrValidationRule` - (rule1 OR rule2)

❌ **NEIMPLEMENTOVAŤ:**
- ~~`AndAlsoValidationRule`~~ - zbytočné (And už je short-circuit v C#, je to to isté ako AND)
- ~~`OrElseValidationRule`~~ - zbytočné (Or už je short-circuit v C#, je to to isté ako OR)

### **NÁVRH IMPLEMENTÁCIE:**

**Features/Validation/Rules/AndValidationRule.cs** (nová trieda):
```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Rules;

/// <summary>
/// Composite validation rule that combines two rules with AND logic.
/// Both rules must pass for validation to succeed.
/// SHORT-CIRCUIT: If left rule fails, right rule is NOT evaluated.
/// </summary>
public sealed class AndValidationRule : IValidationRule
{
    private readonly IValidationRule _left;
    private readonly IValidationRule _right;
    private readonly string _ruleId;
    private readonly string _ruleName;

    public AndValidationRule(
        IValidationRule left,
        IValidationRule right,
        string? ruleId = null,
        string? ruleName = null)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _ruleId = ruleId ?? $"AND_{left.RuleId}_{right.RuleId}";
        _ruleName = ruleName ?? $"{left.RuleName} AND {right.RuleName}";
    }

    public string RuleId => _ruleId;
    public string RuleName => _ruleName;

    public IReadOnlyList<string> DependentColumns =>
        _left.DependentColumns
            .Union(_right.DependentColumns)
            .Distinct()
            .ToList();

    public ValidationResult Validate(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context)
    {
        // SHORT-CIRCUIT AND: If left fails, return immediately
        var leftResult = _left.Validate(row, context);
        if (!leftResult.IsValid)
        {
            return leftResult; // Left failed → AND fails
        }

        // Left passed, evaluate right
        var rightResult = _right.Validate(row, context);
        if (!rightResult.IsValid)
        {
            return rightResult; // Right failed → AND fails
        }

        // Both passed → AND succeeds
        return ValidationResult.Success();
    }
}
```

**Features/Validation/Rules/OrValidationRule.cs** (nová trieda):
```csharp
/// <summary>
/// Composite validation rule that combines two rules with OR logic.
/// At least one rule must pass for validation to succeed.
/// SHORT-CIRCUIT: If left rule passes, right rule is NOT evaluated.
/// </summary>
public sealed class OrValidationRule : IValidationRule
{
    private readonly IValidationRule _left;
    private readonly IValidationRule _right;
    private readonly string _ruleId;
    private readonly string _ruleName;

    public OrValidationRule(
        IValidationRule left,
        IValidationRule right,
        string? ruleId = null,
        string? ruleName = null)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _ruleId = ruleId ?? $"OR_{left.RuleId}_{right.RuleId}";
        _ruleName = ruleName ?? $"{left.RuleName} OR {right.RuleName}";
    }

    public string RuleId => _ruleId;
    public string RuleName => _ruleName;

    public IReadOnlyList<string> DependentColumns =>
        _left.DependentColumns
            .Union(_right.DependentColumns)
            .Distinct()
            .ToList();

    public ValidationResult Validate(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context)
    {
        // SHORT-CIRCUIT OR: If left passes, return immediately
        var leftResult = _left.Validate(row, context);
        if (leftResult.IsValid)
        {
            return leftResult; // Left passed → OR succeeds
        }

        // Left failed, evaluate right
        var rightResult = _right.Validate(row, context);
        if (rightResult.IsValid)
        {
            return rightResult; // Right passed → OR succeeds
        }

        // Both failed → OR fails (return right error for better message)
        return rightResult;
    }
}
```

### **POUŽITIE V APLIKÁCII:**

```csharp
// PRÍKLAD: (Column_1 required AND Column_2 > 0) OR Column_3 matches regex

// Vytvor základné pravidlá
var rule1 = new SimpleValidationRule(
    ruleId: "rule_column1_required",
    ruleName: "Column_1_Required",
    dependentColumns: new[] { "Column_1" },
    validator: (row, context) =>
    {
        if (!row.TryGetValue("Column_1", out var value))
            return ValidationResult.Success();

        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Error("Column_1 is required");

        return ValidationResult.Success();
    });

var rule2 = new SimpleValidationRule(
    ruleId: "rule_column2_positive",
    ruleName: "Column_2_Positive",
    dependentColumns: new[] { "Column_2" },
    validator: (row, context) =>
    {
        if (!row.TryGetValue("Column_2", out var value))
            return ValidationResult.Success();

        if (value == null || Convert.ToInt32(value) <= 0)
            return ValidationResult.Error("Column_2 must be > 0");

        return ValidationResult.Success();
    });

var rule3 = new SimpleValidationRule(
    ruleId: "rule_column3_regex",
    ruleName: "Column_3_Regex",
    dependentColumns: new[] { "Column_3" },
    validator: (row, context) =>
    {
        if (!row.TryGetValue("Column_3", out var value))
            return ValidationResult.Success();

        if (value == null || !Regex.IsMatch(value.ToString() ?? "", @"^\d{3}-\d{3}$"))
            return ValidationResult.Error("Column_3 must match pattern XXX-XXX");

        return ValidationResult.Success();
    });

// Vytvor kompozitné pravidlo: (rule1 AND rule2) OR rule3
var andRule = new AndValidationRule(rule1, rule2);
var finalRule = new OrValidationRule(andRule, rule3);

// Pridaj jedno kompozitné pravidlo
await _gridFacade.Validation.AddValidationRuleAsync(finalRule);

// ALTERNATÍVA: Pridať tri samostatné pravidlá
// (zachová sa pôvodné správanie - každé pravidlo samostatne vyhodnotené)
await _gridFacade.Validation.AddValidationRuleAsync(rule1);
await _gridFacade.Validation.AddValidationRuleAsync(rule2);
await _gridFacade.Validation.AddValidationRuleAsync(rule3);
```

### **ZACHOVANÉ PÔVODNÉ SPRÁVANIE:**

✅ **Každé pravidlo sa SAMOSTATNE vyhodnotí** (ak sa pridá samostatne)
✅ **Ak jedno pravidlo zlyhá → pridá sa error**
✅ **Ak dve pravidlá zlyhajú → pridajú sa 2 errors**
✅ **PLUS: Možnosť vytvoriť kompozitné pravidlá** `(rule1 AND rule2) OR rule3`

---

## 📌 5. HEADER FLYOUT - SORT + FILTER

### **ZMENA: Header Click → Zobrazí Header Flyout (namiesto priameho sortu)**

❌ **STARÉ SPRÁVANIE:**
- Klik na header → zmení sort (Asc → Desc → None)

✅ **NOVÉ SPRÁVANIE:**
- Klik na header → zobrazí **Header Flyout**
- Flyout obsahuje:
  - **Sort Asc** (vzostupne)
  - **Sort Desc** (zostupne)
  - **Sort None** (bez sortu)
  - **Separator**
  - **Filter (Checkbox mode)** - zoznam unikátnych hodnôt
  - **Filter (Regex mode)** - regex pattern input
- Klik mimo flyout ALEBO klik na header znova → flyout sa **schová**

### **NÁVRH IMPLEMENTÁCIE:**

**HeadersRowView.cs** - upraviť:
```csharp
private void OnHeaderTapped(object sender, TappedRoutedEventArgs e)
{
    if (sender is FrameworkElement element && element.DataContext is ColumnHeaderViewModel header)
    {
        // STARÉ (zmazať):
        // CycleSortDirection(header);

        // NOVÉ (pridať):
        ShowHeaderFlyout(header, element);
    }
}

private void ShowHeaderFlyout(ColumnHeaderViewModel header, FrameworkElement anchorElement)
{
    // Vytvor flyout s možnosťami
    var flyout = new MenuFlyout();

    // SORT OPTIONS
    var sortAscItem = new MenuFlyoutItem
    {
        Text = "Sort Ascending ↑",
        Icon = new SymbolIcon(Symbol.Up)
    };
    sortAscItem.Click += (s, e) =>
    {
        header.SortDirection = "Ascending";
        OnSortChanged(header);
        flyout.Hide();
    };
    flyout.Items.Add(sortAscItem);

    var sortDescItem = new MenuFlyoutItem
    {
        Text = "Sort Descending ↓",
        Icon = new SymbolIcon(Symbol.Down)
    };
    sortDescItem.Click += (s, e) =>
    {
        header.SortDirection = "Descending";
        OnSortChanged(header);
        flyout.Hide();
    };
    flyout.Items.Add(sortDescItem);

    var sortNoneItem = new MenuFlyoutItem
    {
        Text = "Clear Sort ✖",
        Icon = new SymbolIcon(Symbol.Clear)
    };
    sortNoneItem.Click += (s, e) =>
    {
        header.SortDirection = "None";
        OnSortChanged(header);
        flyout.Hide();
    };
    flyout.Items.Add(sortNoneItem);

    // SEPARATOR
    flyout.Items.Add(new MenuFlyoutSeparator());

    // FILTER OPTIONS
    var filterCheckboxItem = new MenuFlyoutItem
    {
        Text = "Filter (Select Values)...",
        Icon = new SymbolIcon(Symbol.Filter)
    };
    filterCheckboxItem.Click += (s, e) =>
    {
        ShowFilterCheckboxFlyout(header, anchorElement);
        flyout.Hide();
    };
    flyout.Items.Add(filterCheckboxItem);

    var filterRegexItem = new MenuFlyoutItem
    {
        Text = "Filter (Regex Pattern)...",
        Icon = new SymbolIcon(Symbol.Find)
    };
    filterRegexItem.Click += (s, e) =>
    {
        ShowFilterRegexFlyout(header, anchorElement);
        flyout.Hide();
    };
    flyout.Items.Add(filterRegexItem);

    // Zobraz flyout POD headerom
    flyout.Placement = FlyoutPlacementMode.Bottom;
    flyout.ShowAt(anchorElement);

    // Auto-hide pri kliknutí mimo alebo na header znova
    flyout.Closed += (s, e) =>
    {
        _logger?.LogTrace("Header flyout closed for column {ColumnName}", header.ColumnName);
    };
}

private void OnSortChanged(ColumnHeaderViewModel header)
{
    _logger?.LogInformation("Sort changed: {ColumnName} → {SortDirection}",
        header.ColumnName, header.SortDirection);

    // Fire event pre DataGridViewModel
    SortChanged?.Invoke(this, header);
}
```

### **FLYOUT SPRÁVANIE:**

✅ **Zobrazenie:**
- Klik na header → flyout sa zobrazí **POD** headerom
- `FlyoutPlacementMode.Bottom` = pod anchorElement

✅ **Schovanie:**
- Klik mimo flyout → flyout sa automaticky schová (WinUI default)
- Klik na header znova → flyout sa schová (toggle)
- Výber položky z menu → flyout sa schová (explicitne `flyout.Hide()`)

✅ **Obsah:**
- Sort Asc / Desc / None
- Separator
- Filter Checkbox / Regex

---

## 📌 6. KONTEXTOVÉ MENU PRE PRIDÁVANIE RIADKOV - EXCEL-LIKE

### **IMPLEMENTÁCIA RIGHT-CLICK KONTEXTOVÉHO MENU:**

✅ **POŽIADAVKY:**
- Right-click na riadok → kontextové menu
- Insert Above / Below
- Viacero označených riadkov → pridá viacero riadkov
- Ako Excel

### **NÁVRH IMPLEMENTÁCIE:**

**UIControls/Menus/RowContextMenu.cs** (nová trieda):
```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls.Menus;

/// <summary>
/// Context menu for row operations (Insert Above, Insert Below, Delete)
/// Excel-like behavior for row management
/// </summary>
internal sealed class RowContextMenu
{
    public event EventHandler<InsertRowsEventArgs>? InsertRowsAboveRequested;
    public event EventHandler<InsertRowsEventArgs>? InsertRowsBelowRequested;
    public event EventHandler<DeleteRowsEventArgs>? DeleteRowsRequested;

    public MenuFlyout CreateRowContextMenu(
        IReadOnlyList<int> selectedRowIndices,
        IReadOnlyList<string> selectedRowIds)
    {
        var menu = new MenuFlyout();

        // INSERT ABOVE
        var insertAboveItem = new MenuFlyoutItem
        {
            Text = selectedRowIndices.Count > 1
                ? $"Insert {selectedRowIndices.Count} Rows Above"
                : "Insert Row Above",
            Icon = new SymbolIcon(Symbol.Add)
        };
        insertAboveItem.Click += (s, e) =>
        {
            InsertRowsAboveRequested?.Invoke(this, new InsertRowsEventArgs
            {
                ReferenceRowIndex = selectedRowIndices.Min(),
                RowCount = selectedRowIndices.Count,
                SelectedRowIds = selectedRowIds
            });
        };
        menu.Items.Add(insertAboveItem);

        // INSERT BELOW
        var insertBelowItem = new MenuFlyoutItem
        {
            Text = selectedRowIndices.Count > 1
                ? $"Insert {selectedRowIndices.Count} Rows Below"
                : "Insert Row Below",
            Icon = new SymbolIcon(Symbol.Add)
        };
        insertBelowItem.Click += (s, e) =>
        {
            InsertRowsBelowRequested?.Invoke(this, new InsertRowsEventArgs
            {
                ReferenceRowIndex = selectedRowIndices.Max(),
                RowCount = selectedRowIndices.Count,
                SelectedRowIds = selectedRowIds
            });
        };
        menu.Items.Add(insertBelowItem);

        // SEPARATOR
        menu.Items.Add(new MenuFlyoutSeparator());

        // DELETE ROWS
        var deleteItem = new MenuFlyoutItem
        {
            Text = selectedRowIndices.Count > 1
                ? $"Delete {selectedRowIndices.Count} Rows"
                : "Delete Row",
            Icon = new SymbolIcon(Symbol.Delete),
            Foreground = new SolidColorBrush(Colors.Red)
        };
        deleteItem.Click += (s, e) =>
        {
            DeleteRowsRequested?.Invoke(this, new DeleteRowsEventArgs
            {
                RowIndices = selectedRowIndices,
                RowIds = selectedRowIds
            });
        };
        menu.Items.Add(deleteItem);

        return menu;
    }
}

public class InsertRowsEventArgs : EventArgs
{
    public int ReferenceRowIndex { get; init; }
    public int RowCount { get; init; }
    public IReadOnlyList<string> SelectedRowIds { get; init; } = Array.Empty<string>();
}

public class DeleteRowsEventArgs : EventArgs
{
    public IReadOnlyList<int> RowIndices { get; init; } = Array.Empty<int>();
    public IReadOnlyList<string> RowIds { get; init; } = Array.Empty<string>();
}
```

### **INTEGRÁCIA DO DataGridCellsView.cs:**

```csharp
// RightTapped handler na riadku
private void OnRowRightTapped(object sender, RightTappedRoutedEventArgs e)
{
    // Získaj označené riadky (cez Checkbox stĺpec)
    var selectedRows = _viewModel.Rows
        .Where(r => r.IsRowSelected)
        .ToList();

    if (selectedRows.Count == 0)
    {
        // Ak nie je nič označené, označ riadok pod kurzorom
        if (sender is FrameworkElement element && element.DataContext is RowViewModel row)
        {
            row.IsRowSelected = true;
            selectedRows.Add(row);
        }
    }

    var selectedIndices = selectedRows.Select(r => r.RowIndex).ToList();
    var selectedIds = selectedRows.Select(r => r.RowId).ToList();

    // Vytvor kontextové menu
    var contextMenu = _rowContextMenu.CreateRowContextMenu(selectedIndices, selectedIds);

    // Zobraz menu na pozícii kurzora
    contextMenu.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));

    e.Handled = true;
}
```

### **EXCEL-LIKE PRÍKLAD:**

```
STAV PRED:
UI Index | Checkbox | Data
---------|----------|-------------
0        | ☐       | "Row 0"
1        | ☐       | "Row 1"
2        | ☑       | "Row 2"  ← OZNAČENÉ
3        | ☑       | "Row 3"  ← OZNAČENÉ
4        | ☑       | "Row 4"  ← OZNAČENÉ
5        | ☐       | "Row 5"
...
99       | ☐       | "Row 99"

RIGHT-CLICK na označené riadky → Kontextové menu:
┌─────────────────────────────┐
│ ➕ Insert 3 Rows Above      │
│ ➕ Insert 3 Rows Below      │
│ ─────────────────────────── │
│ 🗑 Delete 3 Rows            │
└─────────────────────────────┘

KLIK NA "Insert 3 Rows Below" (pod UI index 4):

STAV PO (nová architektúra - data shift):
UI Index | Data
---------|-------------
0        | "Row 0"
1        | "Row 1"
2        | "Row 2"
3        | "Row 3"
4        | "Row 4"
5        | null (NOVÝ PRÁZDNY)  ← Vložený
6        | null (NOVÝ PRÁZDNY)  ← Vložený
7        | null (NOVÝ PRÁZDNY)  ← Vložený
8        | "Row 5"              ← Dáta posunuté dole
9        | "Row 6"              ← Dáta posunuté dole
...
99       | "Row 96"             ← Dáta z row 96 (row 97-99 stratené)
```

---

## 📌 7. ŠPECIÁLNY STĹPEC INSERTROW - FUNKČNOSŤ

### **AKO FUNGUJE INSERTROW ŠPECIÁLNY STĹPEC:**

✅ **SpecialColumnCellControl.cs:309-349 - CreateInsertRowControl:**
- Tlačidlo s ikonou "+" (plus)
- Klik na tlačidlo → fire event `InsertRowRequested`
- Event obsahuje `rowIndex` a `rowId`

✅ **ČO SA STANE PO KLIKNUTÍ:**
```
KLIK NA "+" v UI riadku 5:
1. Event: InsertRowRequested(rowIndex=5, rowId="01HXABC...006")
2. Aplikácia zavolá: facade.Rows.InsertEmptyRowsAtAsync(insertIndex=6, rowCount=1)
3. NOVÁ ARCHITEKTÚRA: Dáta sa posunú dole od UI indexu 6
4. UI Riadok 6 dostane prázdne dáta (nový ULID)
5. Ostatné dáta sa posunú dole (UI riadok 7-99)
```

✅ **ROZDIEL MEDZI INSERTROW STĹPCOM A KONTEXTOVÝM MENU:**

| Feature                  | InsertRow Stĺpec ("+")      | Kontextové Menu (Right-click) |
|--------------------------|----------------------------|-------------------------------|
| Spustenie                | Klik na ikonu "+"          | Right-click → Insert Below    |
| Pozícia vloženia         | **POD** aktuálnym riadkom  | Nad / Pod označené riadky     |
| Počet vložených riadkov  | **Vždy 1** riadok          | **1 až N** (podľa označených) |
| Dialóg                   | Môže otvoriť AddRowModal   | Len vloží prázdne riadky      |
| Use case                 | Rýchle pridanie 1 riadku   | Hromadné pridanie N riadkov   |

---

## 🎯 FINÁLNE ODPOVEDE NA VŠETKY OTÁZKY

### **1. InMemoryRowStore vs HybridRowStore?**
✅ **ÁNO** - rovnaká funkcionalita cez `IRowStore` interface

### **2. Mechanizmus mazania/pridávania?**
✅ **PO NOVOM:**
- UI Riadky ako objekty = NEMENNÉ (konštantný počet per page, napr. 100)
- ULID = VIAZANÝ NA DÁTA (presúva sa s dátami, nie s UI pozíciou)
- UI riadky dostanú ULID tých dátových riadkov ktoré aktuálne zobrazujú
- ULID sa NEZOBRAZUJE používateľovi
- Dáta = POHYBLIVÉ (posúvajú sa medzi UI riadkami naprieč celou tabuľkou)
- Fyzicky sa NIC nemaže ani nepridáva
- Multi-page support: dáta sa posúvajú naprieč všetkými stránkami (napr. 150 riadkov = 2 pages)

### **3. SmartAddDelete - čo zmazať?**
❌ **ZMAZAŤ CELÝ FOLDER SmartAddDelete**:
- `SmartOperationService.cs` - zmazať
- `EnsureMinRowsAndLastEmptyAsync` - zmazať
- `SmartAddLastEmptyRowAsync` - zmazať

✅ **NAHRADIŤ:**
- Nový folder `Features/Rows/`
- `RowManagementService.cs` - nová trieda
- `DeleteRowsByIdAsync` - nová metóda (namiesto SmartDeleteRowsByIdAsync)

### **4. Kompozitné validačné pravidlá?**
✅ **IMPLEMENTOVAŤ LEN:**
- `AndValidationRule` - (rule1 AND rule2)
- `OrValidationRule` - (rule1 OR rule2)

❌ **NEIMPLEMENTOVAŤ:**
- ~~`AndAlsoValidationRule`~~ - zbytočné (je to to isté ako AND, C# aj tak používa short-circuit)
- ~~`OrElseValidationRule`~~ - zbytočné (je to to isté ako OR, C# aj tak používa short-circuit)

✅ **ZACHOVANÉ PÔVODNÉ SPRÁVANIE:**
- Každé pravidlo sa samostatne vyhodnotí (ak sa pridá samostatne)
- Ak jedno pravidlo zlyhá → pridá sa error
- Ak dve pravidlá zlyhajú → pridajú sa 2 errors

### **5. Header klik - sort?**
✅ **ZMENIŤ:**
- **STARÉ:** Klik na header → zmení sort
- **NOVÉ:** Klik na header → zobrazí Header Flyout

✅ **HEADER FLYOUT OBSAHUJE:**
- Sort Asc / Desc / None
- Separator
- Filter Checkbox / Regex

✅ **SCHOVANIE:**
- Klik mimo flyout → schová sa
- Klik na header znova → schová sa

### **6. Kontextové menu pre insert?**
✅ **IMPLEMENTOVAŤ:**
- Right-click na riadok → kontextové menu
- Insert Above / Below
- Excel-like - viacero označených riadkov → pridá viacero

### **7. InsertRow špeciálny stĺpec?**
✅ **FUNGUJE:**
- Ikona "+" (plus)
- Klik → pridá 1 riadok **POD** aktuálnym
- Môže otvoriť AddRowModal dialóg

---

## 📂 SÚHRNNÁ ZMENA ŠTRUKTÚRY

### **ZMENY V FOLDER ŠTRUKTÚRE:**

```
Features/
├── Rows/                           ← NOVÝ FOLDER
│   ├── Services/
│   │   └── RowManagementService.cs   ← NOVÝ
│   ├── Commands/
│   │   └── DeleteRowsByIdCommand.cs  ← NOVÝ
│   └── Interfaces/
│       └── IRowManagementService.cs   ← NOVÝ
│
├── Validation/
│   ├── Rules/                      ← NOVÝ FOLDER
│   │   ├── AndValidationRule.cs      ← NOVÝ
│   │   └── OrValidationRule.cs       ← NOVÝ
│   └── Services/
│       ├── ValidationService.cs       ← UPRAVENÝ (Fáza 2)
│       └── ValidationDeletionService.cs ← UPRAVENÝ (Fáza 2)
│
├── UIControls/
│   ├── Menus/                      ← NOVÝ FOLDER
│   │   └── RowContextMenu.cs         ← NOVÝ
│   ├── HeadersRowView.cs             ← UPRAVENÝ (Header Flyout)
│   └── DataGridCellsView.cs          ← UPRAVENÝ (Right-click menu)
│
└── SmartAddDelete/                 ← ZMAZAŤ CELÝ FOLDER
    ├── Services/
    │   └── SmartOperationService.cs   ← ZMAZAŤ
    ├── Commands/
    │   └── SmartOperationCommand.cs   ← ZMAZAŤ
    └── Interfaces/
        └── ISmartOperationService.cs  ← ZMAZAŤ
```

### **ZMENY V API:**

```csharp
// IAdvancedDataGridFacade.cs

// ZMAZAŤ:
// IDataGridSmartOperations SmartOperations { get; }

// PRIDAŤ:
IDataGridRows Rows { get; }
```

---

## 📊 TABUĽKA IMPLEMENTOVANÝCH ZMIEN

| Feature                           | Status      | Poznámka                                      |
|-----------------------------------|-------------|-----------------------------------------------|
| Validation empty row skipping     | ✅ HOTOVÉ   | Fáza 2 - ValidationService.cs                |
| Validation deletion + trailing    | ✅ HOTOVÉ   | Fáza 2 - ValidationDeletionService.cs        |
| Kompozitné pravidlá (And/Or)      | ⏳ NÁVRH    | AndValidationRule, OrValidationRule          |
| Header Flyout (Sort + Filter)     | ⏳ NÁVRH    | HeadersRowView.cs úprava                     |
| Row Context Menu (Excel-like)     | ⏳ NÁVRH    | RowContextMenu.cs nová trieda                |
| DeleteRowsByIdAsync (data shift)  | ⏳ NÁVRH    | RowManagementService.cs nová trieda          |
| SmartAddDelete removal            | ⏳ NÁVRH    | Zmazať celý folder                           |
| ULID tracking (data-bound)        | ⏳ NÁVRH    | ULID viazaný na dáta, nie UI pozíciu         |
| Konštantný počet UI riadkov       | ⏳ NÁVRH    | Fixný počet per page, dáta sa posúvajú       |
| Multi-page data shift             | ⏳ NÁVRH    | Dáta sa posúvajú naprieč všetkými pages      |

---

## 🔍 KĽÚČOVÉ TECHNICKÉ POJMY

| Pojem                     | Význam                                                                 |
|---------------------------|------------------------------------------------------------------------|
| **UI Riadok**             | Vizuálny objekt v gridu (fixný počet na stránku, napr. 100)           |
| **Dátový Riadok**         | Dáta zobrazené v UI riadku (ULID + hodnoty stĺpcov)                   |
| **ULID**                  | Unique Lexicographically Sortable ID - viazaný NA DÁTA, nie UI pozíciu |
| **Data Shift**            | Posun dát medzi UI riadkami naprieč celou tabuľkou (bez fyzického mazania/pridávania) |
| **Recyklácia**            | Posledný UI riadok dostáva prázdne dáta pri mazaní                    |
| **Kompozitné pravidlo**   | Validačné pravidlo zložené z AND/OR kombinácií                        |
| **Header Flyout**         | Dropdown menu pod hlavičkou stĺpca (Sort + Filter)                    |
| **Row Context Menu**      | Right-click menu na riadku (Insert Above/Below, Delete)               |
| **Multi-Page Support**    | Dáta sa posúvajú naprieč všetkými stránkami (napr. 150 riadkov = 2 pages) |

---

**TENTO DOKUMENT JE KOMPLETNÁ TECHNICKÁ ŠPECIFIKÁCIA PRE IMPLEMENTÁCIU NOVÝCH FUNKCIONALÍT.**

# IMPLEMENTATION ROADMAP - Kompletná implementačná príručka

## 📋 METADATA

**Dátum vytvorenia:** 19.10.2025
**Verzia:** 1.0
**Jazyk kódu:** English
**Jazyk dokumentácie:** Slovenčina
**Komponent:** AdvancedDataGrid - Hybrid SQLite Migration

---

## 🎯 EXECUTIVE SUMMARY

Tento dokument obsahuje kompletnú implementačnú príručku pre migráciu AdvancedDataGrid komponentu z pure in-memory modelu na **hybrid SQLite model**.

### Hlavné ciele migrácie

1. **Škálovateľnosť:** Podpora 10M+ riadkov bez OutOfMemoryException
2. **Performance:** Filter/Sort/Search 15-40x rýchlejšie cez SQLite
3. **Funkcionality z pôvodnej špecifikácie:** Kompletný Sort/Filter/Search z `SPECIFIKACIA_SORT_FILTER_SEARCH.md`
4. **Nová funkcionalita:** Add Row modal dialog s real-time validáciou
5. **Tri operačné módy:** Interactive, Headless+ManualUI, Pure Headless
6. **Resource management:** Optimalizácia CPU/RAM/I/O

---

## 📚 REFERENČNÉ DOKUMENTY

Táto príručka nadväzuje na nasledovné dokumenty (všetky v root folder):

1. **`docu_HybridSQLiteArchitecture.md`**
   - Hlavná architektúra hybrid modelu
   - HybridRowStore design
   - SQLite schéma (grid_rows, FTS5)
   - Memory & Performance profil

2. **`docu_DatabaseManagement.md`**
   - Lifecycle management (Init, Shutdown, Cleanup)
   - Per-instance temp databases
   - API: IDataGridDatabase

3. **`docu_SortFilterSearchIntegration.md`**
   - Integrácia Sort/Filter/Search so SQLite
   - SQL WHERE/ORDER BY/FTS5 implementácia
   - Performance benchmarks

4. **`docu_AddRowModalSpecification.md`**
   - Modal dialog pre pridanie riadku
   - Real-time validácia
   - Podpora troch módov

5. **`SPECIFIKACIA_SORT_FILTER_SEARCH.md`** (pôvodná špecifikácia)
   - Detailná požiadavky na Filter/Sort/Search
   - Virtualizácia a pagination
   - Classic vs Smart metódy

**DÔLEŽITÉ:** Prečítaj si všetky uvedené dokumenty pred začiatkom implementácie!

---

## 🗺️ IMPLEMENTAČNÝ PLÁN - OVERVIEW

### Fázový rozklad

| Fáza | Popis | Trvanie | Priorita |
|------|-------|---------|----------|
| **Fáza 0** | Príprava a analýza | 1 deň | CRITICAL |
| **Fáza 1** | Database Layer (HybridRowStore) | 5-6 dní | CRITICAL |
| **Fáza 2** | Filter/Sort/Search so SQLite | 4-5 dní | CRITICAL |
| **Fáza 3** | Add Row Modal Dialog | 2-3 dni | HIGH |
| **Fáza 4** | Validation System Integration | 2-3 dni | HIGH |
| **Fáza 5** | Virtualization & Pagination | 3-4 dni | CRITICAL |
| **Fáza 6** | Three Operation Modes | 2 dni | CRITICAL |
| **Fáza 7** | Testing & Performance Tuning | 4-5 dní | CRITICAL |
| **Fáza 8** | Documentation & Demo App | 2 dni | MEDIUM |

**Celkový odhad:** 25-33 pracovných dní (5-7 týždňov)

---

## 📋 FÁZA 0: PRÍPRAVA A ANALÝZA (1 deň)

### Ciele
- Prečítať všetky dokumenty
- Pripraviť development environment
- Backup aktuálneho stavu
- Vytvoriť feature branch

### Tasks

**0.1. Prečítať dokumentáciu** ✓
- [x] `docu_HybridSQLiteArchitecture.md`
- [x] `docu_DatabaseManagement.md`
- [x] `docu_SortFilterSearchIntegration.md`
- [x] `docu_AddRowModalSpecification.md`
- [x] `SPECIFIKACIA_SORT_FILTER_SEARCH.md`

**0.2. Backup a Git setup**
```bash
# Create feature branch
git checkout -b feature/hybrid-sqlite-migration
git push -u origin feature/hybrid-sqlite-migration

# Tag current state
git tag v1.0-before-hybrid-sqlite-migration
git push --tags
```

**0.3. Install dependencies**
```bash
# Install Microsoft.Data.Sqlite NuGet package
dotnet add package Microsoft.Data.Sqlite --version 8.0.0
```

**0.4. Create project structure**
```
RpaWinUiComponentsPackage/AdvancedWinUiDataGrid/
├─ Features/
│  ├─ Database/
│  │  ├─ Services/
│  │  │  ├─ DatabaseLifecycleManager.cs (NEW)
│  │  │  └─ HybridRowStore.cs (NEW - replaces InMemoryRowStore)
│  │  ├─ Models/
│  │  │  ├─ WriteOperation.cs (NEW)
│  │  │  └─ DatabaseStatistics.cs (NEW)
│  │  └─ Registration.cs (NEW)
│  ├─ Validation/
│  │  └─ ... (update for SQLite)
│  └─ ... (existing features)
├─ UIControls/
│  └─ Dialogs/
│     ├─ AddRowModalDialog.xaml (NEW)
│     └─ AddRowModalDialog.xaml.cs (NEW)
├─ Api/
│  └─ Features/
│     ├─ Database/
│     │  └─ IDataGridDatabase.cs (NEW)
│     └─ ... (existing)
└─ ...
```

---

## 📋 FÁZA 1: DATABASE LAYER - HybridRowStore (5-6 dní)

### Ciele
- Implementovať HybridRowStore (náhrada InMemoryRowStore)
- Implementovať DatabaseLifecycleManager
- Implementovať Writer Queue pattern
- Testovať bulk insert performance

### Tasks

**1.1. DatabaseLifecycleManager (1 deň)**

Implementovať službu podľa `docu_DatabaseManagement.md`:

```csharp
// Features/Database/Services/DatabaseLifecycleManager.cs
internal sealed class DatabaseLifecycleManager : IDatabaseLifecycleManager, IAsyncDisposable
{
    // PRAGMA settings
    private async Task ApplyPragmaSettingsAsync(...)
    {
        // WAL, NORMAL sync, cache_size, mmap_size
    }

    // Schema creation
    private async Task CreateSchemaAsync(...)
    {
        // CREATE TABLE grid_rows
        // CREATE VIRTUAL TABLE grid_rows_fts USING fts5
        // CREATE TRIGGER grid_rows_ai/au/ad
        // CREATE INDEX idx_createdAt, idx_isDeleted
    }

    public async Task<Result> InitializeDatabaseAsync(string? databasePath, ...)
    public async Task<Result> ShutdownDatabaseAsync(...)
    public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync(...)
    public async Task<Result<int>> DeleteAllDatabasesInPathAsync(string directoryPath, ...)
}
```

**Testing:**
- Unit test: InitializeDatabaseAsync vytvára DB file
- Unit test: Schema creation (tables, indexes, triggers existujú)
- Unit test: ShutdownDatabaseAsync maže temp file
- Unit test: DeleteAllDatabasesInPathAsync maže všetky .db files

**1.2. Write Operation Models (0.5 dňa)**

```csharp
// Features/Database/Models/WriteOperation.cs
internal abstract record WriteOperation;
internal record InsertRowOp(IReadOnlyDictionary<string, object?> Row) : WriteOperation;
internal record UpdateRowOp(string RowId, IReadOnlyDictionary<string, object?> Row) : WriteOperation;
internal record DeleteRowOp(string RowId) : WriteOperation;
internal record BulkInsertOp(IEnumerable<IReadOnlyDictionary<string, object?>> Rows) : WriteOperation;
internal record ValidationUpdateOp(string RowId, ValidationError[] Errors) : WriteOperation;
```

**1.3. HybridRowStore - Core Structure (1 deň)**

```csharp
// Features/Database/Services/HybridRowStore.cs
internal sealed class HybridRowStore : IRowStore, IAsyncDisposable
{
    // In-memory viewport cache
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>> _viewportCache;

    // SQLite connection
    private SqliteConnection? _sqliteConnection;
    private readonly string _databaseFilePath;

    // Writer queue
    private readonly Channel<WriteOperation> _writeQueue;
    private Task? _writerTask;
    private CancellationTokenSource? _writerCts;

    // Pagination/Filter/Sort state
    private int _currentPage = 1;
    private int _pageSize = 1000;
    private string? _activeFilterSql;
    private string? _activeSortSql;

    // Constructor
    public HybridRowStore(
        ILogger<HybridRowStore> logger,
        DatabaseLifecycleManager databaseManager)
    {
        // ...
        _sqliteConnection = databaseManager.GetConnection();

        // Start writer thread
        _writerCts = new CancellationTokenSource();
        _writerTask = WriterTaskAsync(_writerCts.Token);
    }

    // Writer thread loop
    private async Task WriterTaskAsync(CancellationToken cancellationToken)
    {
        await foreach (var op in _writeQueue.Reader.ReadAllAsync(cancellationToken))
        {
            // Process write operations
        }
    }
}
```

**1.4. HybridRowStore - Basic CRUD (1.5 dňa)**

Implementovať základné metódy:

```csharp
// AddRowAsync
public async Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, ...)
{
    // 1. Add to viewport cache
    // 2. Enqueue to writer queue
    // 3. Return index
}

// GetPagedRowsAsync
public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPagedRowsAsync(
    int pageNumber, int pageSize, bool onlyFiltered, ...)
{
    // SELECT __rowId, data FROM grid_rows WHERE ... ORDER BY ... LIMIT ... OFFSET ...
}

// UpdateRowByIdAsync
public async Task<bool> UpdateRowByIdAsync(string rowId, IReadOnlyDictionary<string, object?> rowData, ...)

// RemoveRowByIdAsync
public async Task<bool> RemoveRowByIdAsync(string rowId, ...)
```

**Testing:**
- Unit test: AddRowAsync pridá riadok do DB
- Unit test: GetPagedRowsAsync vracia správnu stránku
- Unit test: UpdateRowByIdAsync updatuje riadok
- Unit test: RemoveRowByIdAsync soft-delete (__isDeleted = 1)

**1.5. HybridRowStore - Bulk Operations (1 deň)**

```csharp
// Bulk insert (prepared statements + transaction)
private async Task ExecuteBulkInsertAsync(
    IEnumerable<IReadOnlyDictionary<string, object?>> rows, ...)
{
    using var tx = _sqliteConnection!.BeginTransaction();
    using var cmd = _sqliteConnection.CreateCommand();

    cmd.CommandText = @"
        INSERT INTO grid_rows (__rowId, __createdAt, __modifiedAt, data)
        VALUES ($rowId, $created, $modified, $data)";

    cmd.Parameters.Add(new SqliteParameter("$rowId", DbType.String));
    cmd.Parameters.Add(new SqliteParameter("$created", DbType.Int64));
    cmd.Parameters.Add(new SqliteParameter("$modified", DbType.Int64));
    cmd.Parameters.Add(new SqliteParameter("$data", DbType.String));

    foreach (var row in rows)
    {
        // ... set parameters, ExecuteNonQuery
    }

    await tx.CommitAsync(cancellationToken);
}
```

**Performance target:** 100k rows insert < 5 sekúnd

**Testing:**
- Performance test: Bulk insert 100k rows < 5s
- Performance test: Bulk insert 1M rows < 30s

**1.6. Service Registration (0.5 dňa)**

```csharp
// Features/Database/Registration.cs
public static class DatabaseServiceRegistration
{
    public static void AddDatabaseServices(this IServiceCollection services)
    {
        services.AddSingleton<DatabaseLifecycleManager>();
        services.AddSingleton<IRowStore, HybridRowStore>();
        // ...
    }
}
```

---

## 📋 FÁZA 2: FILTER/SORT/SEARCH SO SQLITE (4-5 dní)

### Ciele
- Implementovať SQL WHERE pre Filter
- Implementovať SQL ORDER BY pre Sort
- Implementovať FTS5 pre Search
- Integrovať s existujúcimi službami

### Tasks

**2.1. Filter - SQL WHERE Builder (1 deň)**

Implementovať podľa `docu_SortFilterSearchIntegration.md`:

```csharp
// HybridRowStore.cs
public void SetFilterCriteria(IReadOnlyList<FilterCriteria> criteria)
{
    // Build SQL WHERE clause from criteria
    var whereConditions = new List<string>();

    foreach (var filter in criteria)
    {
        var sqlCondition = BuildSqlFilterCondition(filter);
        whereConditions.Add(sqlCondition);
    }

    _activeFilterSql = string.Join(" AND ", whereConditions);
}

private string BuildSqlFilterCondition(FilterCriteria filter)
{
    // json_extract(data, '$.ColumnName') + operator
    // Handle: Equals, NotEquals, Contains, StartsWith, GreaterThan, ...
}
```

**Testing:**
- Unit test: SetFilterCriteria("Age", GreaterThan, 30) → správny SQL
- Unit test: GetPagedRowsAsync(onlyFiltered: true) vracia len matching rows
- Performance test: Filter 10M rows < 2s

**2.2. Sort - SQL ORDER BY Builder (1 deň)**

```csharp
// HybridRowStore.cs
public void SetSortCriteria(IReadOnlyList<SortDescriptor> sortDescriptors)
{
    // Build SQL ORDER BY clause
    // Type-aware sorting (numeric vs text)
}
```

**Testing:**
- Unit test: SetSortCriteria("Name", ASC) → správny SQL
- Unit test: Multi-column sort (Age DESC, Name ASC)
- Performance test: Sort 10M rows < 5s

**2.3. Search - FTS5 Implementation (2 dní)**

```csharp
// HybridRowStore.cs
public async Task<IReadOnlyList<SearchResult>> SearchAsync(
    string searchText, SearchMode searchMode, ...)
{
    // Build FTS5 query
    var ftsQuery = BuildFtsQuery(searchText, searchMode);

    var sql = @"
        SELECT r.__rowId, r.data
        FROM grid_rows_fts fts
        JOIN grid_rows r ON r.rowid = fts.rowid
        WHERE fts.data MATCH $query AND r.__isDeleted = 0
        ORDER BY rank
        LIMIT 1000";

    // Execute + parse results
}

private string BuildFtsQuery(string searchText, SearchMode mode)
{
    // Contains: *text*
    // Exact: "text"
    // StartsWith: text*
    // Fuzzy: text~
}
```

**Testing:**
- Unit test: Search("john") vracia matching rows
- Unit test: Search modes (Contains, Exact, StartsWith)
- Performance test: Search 10M rows < 2s

**2.4. Service Integration (0.5 dňa)**

Update existujúce služby aby volali HybridRowStore:

```csharp
// FilterService.cs
public async Task<int> ApplyFilterAsync(...)
{
    // ... existing code ...

    // NEW: Call HybridRowStore instead of InMemoryRowStore
    _hybridRowStore.SetFilterCriteria(_activeFilters.ToArray());

    // ... trigger UI refresh ...
}

// SortService.cs
public async Task<bool> SortByColumnAsync(...)
{
    // NEW: Call HybridRowStore
    _hybridRowStore.SetSortCriteria(...);

    // ... trigger UI refresh ...
}

// SearchService.cs
public async Task<SearchResultCollection> SearchAsync(...)
{
    // NEW: Call HybridRowStore.SearchAsync (FTS5)
    var results = await _hybridRowStore.SearchAsync(...);

    // ... map to SearchResult objects ...
}
```

---

## 📋 FÁZA 3: ADD ROW MODAL DIALOG (2-3 dni)

### Ciele
- Implementovať AddRowModalDialog UI
- Real-time validácia v textboxoch
- Integrácia s facade API

### Tasks

**3.1. AddRowDialogViewModel (0.5 dňa)**

Podľa `docu_AddRowModalSpecification.md`:

```csharp
// ViewModels/AddRowDialogViewModel.cs
internal sealed class AddRowDialogViewModel : ViewModelBase
{
    public ObservableCollection<AddRowFieldViewModel> ColumnFields { get; }
    public bool IsAllValid => ColumnFields.All(f => !f.HasError);
    // ...
}

internal sealed class AddRowFieldViewModel : ViewModelBase
{
    public string ColumnName { get; }
    public string Value { get; set; }
    public string? ValidationError { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ValidationError);

    public async Task TriggerDebouncedValidationAsync(...)
    {
        // Debounce 300ms
        // Call facade.Rows.ValidateRowDataAsync
    }
}
```

**3.2. AddRowModalDialog XAML + Code-behind (1 deň)**

```xml
<!-- UIControls/Dialogs/AddRowModalDialog.xaml -->
<ContentDialog
    x:Class="...AddRowModalDialog"
    PrimaryButtonText="Add Row"
    SecondaryButtonText="Cancel"
    Title="Add New Row">

    <ScrollViewer MaxHeight="500">
        <StackPanel Spacing="12">
            <!-- For each column: Label + TextBox + ValidationError -->
        </StackPanel>
    </ScrollViewer>
</ContentDialog>
```

```csharp
// AddRowModalDialog.xaml.cs
public sealed partial class AddRowModalDialog : ContentDialog
{
    private void BuildDialogContent()
    {
        // Dynamically create UI for each column
    }

    public async Task<IReadOnlyDictionary<string, object?>> GetRowDataAsync()
    {
        // Return row data after validation
    }
}
```

**3.3. Facade API Integration (0.5 dňa)**

```csharp
// Api/Features/Rows/DataGridRows.cs
public async Task<PublicResult<string?>> AddRowWithDialogAsync(...)
{
    if (_options.OperationMode == PublicDataGridOperationMode.Headless)
    {
        throw new InvalidOperationException("AddRowWithDialogAsync not supported in Pure Headless mode");
    }

    // Open dialog on UI thread
    var dialog = new AddRowModalDialog(_facade, _columnNames);
    var result = await dialog.ShowAsync();

    if (result == ContentDialogResult.Primary)
    {
        var rowData = await dialog.GetRowDataAsync();

        // Add row
        var addResult = await AddRowAsync(rowData, cancellationToken);

        // Return row ID
        var rowId = rowData.TryGetValue("__rowId", out var id) ? id?.ToString() : null;
        return PublicResult<string?>.Success(rowId);
    }

    return PublicResult<string?>.Success(null);  // Cancelled
}
```

**Testing:**
- Manual test: Click Add Row icon → dialog opens
- Manual test: Type invalid email → validation error appears
- Manual test: Confirm → row added to grid
- Unit test: ValidateRowDataAsync calls validation service

---

## 📋 FÁZA 4: VALIDATION SYSTEM INTEGRATION (2-3 dni)

### Ciele
- Update ValidationService pre SQLite
- Batch validácia nad SQLite
- Delete by validation errors

### Tasks

**4.1. ValidationService - SQL Queries (1.5 dňa)**

```csharp
// Features/Validation/Services/ValidationService.cs
public async Task<ValidationResultCollection> ValidateAllAsync(...)
{
    // SQL query for each validation rule
    foreach (var rule in _validationRules)
    {
        var sql = BuildValidationSql(rule);
        var errors = await ExecuteValidationQueryAsync(sql, rule, ...);
        results.AddRange(errors);
    }

    // Write validation results to grid_rows.__validationState
    await _hybridRowStore.WriteValidationResultsAsync(results, ...);

    return results;
}

private string BuildValidationSql(ValidationRule rule)
{
    return rule.RuleType switch
    {
        ValidationRuleType.Unique => $@"
            SELECT __rowId, json_extract(data, '$.{rule.ColumnName}') as value
            FROM grid_rows
            WHERE __isDeleted = 0
            GROUP BY value
            HAVING COUNT(*) > 1",

        ValidationRuleType.Range => $@"
            SELECT __rowId
            FROM grid_rows
            WHERE __isDeleted = 0
            AND CAST(json_extract(data, '$.{rule.ColumnName}') AS REAL) NOT BETWEEN {rule.MinValue} AND {rule.MaxValue}",

        // ... other types ...
    };
}
```

**4.2. Delete By Validation Errors (0.5 dňa)**

```csharp
// Features/Validation/Services/ValidationDeletionService.cs
public async Task<Result<int>> DeleteByValidationErrorsAsync(...)
{
    // SELECT __rowId FROM grid_rows WHERE __validationState LIKE '%"hasErrors":true%'
    var invalidRowIds = await _hybridRowStore.GetInvalidRowIdsAsync(...);

    // Soft delete or physical delete
    await _hybridRowStore.RemoveRowsAsync(invalidRowIds, ...);

    return Result<int>.Success(invalidRowIds.Count);
}
```

**Testing:**
- Unit test: ValidateAllAsync detekuje duplicates (Unique rule)
- Unit test: ValidateAllAsync detekuje range violations
- Performance test: Validate 1M rows < 10s

---

## 📋 FÁZA 5: VIRTUALIZATION & PAGINATION (3-4 dni)

### Ciele
- Implementovať pagination state management
- Pagination UI controls (page numbers, Next/Back)
- Integrácia s Filter/Sort

### Tasks

**5.1. DataGridViewModel - Pagination State (1 deň)**

```csharp
// ViewModels/DataGridViewModel.cs
public sealed class DataGridViewModel : ViewModelBase
{
    private int _currentPage = 1;
    private int _pageSize = 1000;
    private long _totalRowCount = 0;

    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public long TotalRowCount { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalRowCount / PageSize);
    public bool CanGoToNextPage => CurrentPage < TotalPages;
    public bool CanGoToPreviousPage => CurrentPage > 1;

    public void GoToNextPage() { ... }
    public void GoToPreviousPage() { ... }
    public void GoToPage(int pageNumber) { ... }

    public event EventHandler<int>? PageChanged;

    public async Task LoadCurrentPageAsync()
    {
        // Get total count
        bool onlyFiltered = /* check if filter active */;
        TotalRowCount = await _hybridRowStore.GetRowCountAsync(onlyFiltered, ...);

        // Get current page rows
        var pagedRows = await _hybridRowStore.GetPagedRowsAsync(CurrentPage, PageSize, onlyFiltered, ...);

        // Load into ViewModel
        LoadRows(pagedRows);
    }
}
```

**5.2. PaginationPanelView UI (1 deň)**

```xml
<!-- UIControls/PaginationPanelView.xaml -->
<StackPanel Orientation="Horizontal" Spacing="8">
    <!-- Previous button -->
    <Button Content="◀ Previous" Click="OnPreviousClicked" IsEnabled="{x:Bind ViewModel.CanGoToPreviousPage}" />

    <!-- Page numbers (smart rendering: 1 2 3 ... current ... last) -->
    <ItemsRepeater ItemsSource="{x:Bind ViewModel.PageNumbers}">
        <ItemsRepeater.ItemTemplate>
            <DataTemplate>
                <Button Content="{Binding}" Click="OnPageClicked" />
            </DataTemplate>
        </ItemsRepeater.ItemTemplate>
    </ItemsRepeater>

    <!-- Next button -->
    <Button Content="Next ▶" Click="OnNextClicked" IsEnabled="{x:Bind ViewModel.CanGoToNextPage}" />

    <!-- Page info -->
    <TextBlock Text="{x:Bind ViewModel.PageInfo}" />
</StackPanel>
```

**5.3. AdvancedDataGridControl Integration (0.5 dňa)**

```csharp
// UIControls/AdvancedDataGridControl.cs
private void InitializeSubViews()
{
    // ... existing code ...

    // NEW: Add pagination panel above headers
    _paginationPanel = new PaginationPanelView(ViewModel);
    _paginationPanel.PageChanged += OnPaginationPageChanged;

    _rootGrid.Children.Add(_paginationPanel);
    Grid.SetRow(_paginationPanel, 0);  // Row 0: Pagination
    Grid.SetRow(_headersRowContainer, 1);  // Row 1: Headers
    Grid.SetRow(_cellsViewContainer, 2);  // Row 2: Data cells
}

private async void OnPaginationPageChanged(object? sender, int newPage)
{
    _logger?.LogInformation("Page changed to {Page}", newPage);

    await ViewModel.LoadCurrentPageAsync();
}
```

**5.4. Testing (0.5 dňa)**

- Manual test: Load 1M rows → only 1000 displayed, pagination visible
- Manual test: Click Next → page 2 loads
- Manual test: Click page number → correct page loads
- Performance test: Page switch < 100ms

---

## 📋 FÁZA 6: THREE OPERATION MODES (2 dni)

### Ciele
- Verify Interactive Mode (auto UI refresh)
- Verify Headless + Manual UI (manual refresh required)
- Verify Pure Headless (no UI)

### Tasks

**6.1. Interactive Mode Testing (0.5 dňa)**

```csharp
// Demo App - Interactive Mode
var grid = AdvancedDataGridBuilder
    .CreateBuilder()
    .WithOperationMode(PublicDataGridOperationMode.Interactive)
    .WithDispatcherQueue(DispatcherQueue.GetForCurrentThread())
    .Build();

await grid.Database.InitializeDatabaseAsync();

// Filter
await grid.Filtering.ApplyFilterAsync("Age", FilterOperator.GreaterThan, 30);
// ✓ UI automatically refreshes

// Sort
await grid.Sorting.SortByColumnAsync("Name", SortDirection.Ascending);
// ✓ UI automatically refreshes

// Add row with dialog
var result = await grid.Rows.AddRowWithDialogAsync();
// ✓ Dialog opens, user fills in, row added, UI refreshes
```

**6.2. Headless + Manual UI Testing (0.5 dňa)**

```csharp
// Demo App - Headless + Manual UI
var grid = AdvancedDataGridBuilder
    .CreateBuilder()
    .WithOperationMode(PublicDataGridOperationMode.HeadlessWithManualUI)
    .WithDispatcherQueue(DispatcherQueue.GetForCurrentThread())
    .Build();

await grid.Database.InitializeDatabaseAsync();

// Filter
await grid.Filtering.ApplyFilterAsync("Age", FilterOperator.GreaterThan, 30);
// ✗ UI NOT automatically refreshed

await grid.RefreshUIAsync();
// ✓ NOW UI refreshes

// Sort
await grid.Sorting.SortByColumnAsync("Name", SortDirection.Ascending);
await grid.RefreshUIAsync();
// ✓ UI refreshes

// Add row with dialog
var result = await grid.Rows.AddRowWithDialogAsync();
if (result.IsSuccess && result.Value != null)
{
    await grid.RefreshUIAsync();  // Manual refresh required
}
```

**6.3. Pure Headless Testing (0.5 dňa)**

```csharp
// Demo App - Pure Headless
var grid = AdvancedDataGridBuilder
    .CreateBuilder()
    .WithOperationMode(PublicDataGridOperationMode.Headless)
    .Build();  // No DispatcherQueue

await grid.Database.InitializeDatabaseAsync();

// Import data
await grid.IO.ImportFromDataTableAsync(myDataTable);

// Filter
await grid.Filtering.ApplyFilterAsync("Age", FilterOperator.GreaterThan, 30);

// Get filtered data
var filteredData = await grid.Rows.GetAllRowsAsync(onlyFiltered: true);

foreach (var row in filteredData)
{
    Console.WriteLine($"{row["Name"]}, {row["Age"]}");
}

// Add row (NO dialog in Pure Headless)
var rowData = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 };
await grid.Rows.AddRowAsync(rowData);

// Export
await grid.IO.ExportToDataTableAsync();
```

**6.4. Demo App Update (0.5 dňa)**

Update demo app aby demonštroval všetky tri módy:

```csharp
// MainWindow.xaml.cs
private ComboBox _modeSelector;

private async void OnModeChanged(object sender, SelectionChangedEventArgs e)
{
    var selectedMode = _modeSelector.SelectedIndex switch
    {
        0 => PublicDataGridOperationMode.Interactive,
        1 => PublicDataGridOperationMode.HeadlessWithManualUI,
        2 => PublicDataGridOperationMode.Headless,
        _ => PublicDataGridOperationMode.Interactive
    };

    await RecreateGridWithMode(selectedMode);
}
```

---

## 📋 FÁZA 7: TESTING & PERFORMANCE TUNING (4-5 dní)

### Ciele
- Unit tests pre všetky komponenty
- Integration tests pre end-to-end scenáre
- Performance benchmarks
- Memory profiling

### Tasks

**7.1. Unit Tests (2 dni)**

Vytvor unit testy pre:

**Database Layer:**
- `DatabaseLifecycleManager_InitializeDatabaseAsync_CreatesFile`
- `DatabaseLifecycleManager_ShutdownDatabaseAsync_DeletesFile`
- `HybridRowStore_AddRowAsync_InsertsRow`
- `HybridRowStore_GetPagedRowsAsync_ReturnsPaginatedData`
- `HybridRowStore_BulkInsert_100kRows_Under5Seconds`

**Filter/Sort/Search:**
- `HybridRowStore_SetFilterCriteria_BuildsCorrectSQL`
- `HybridRowStore_SetSortCriteria_BuildsCorrectSQL`
- `HybridRowStore_SearchAsync_FTS5_ReturnsMatches`

**Validation:**
- `ValidationService_ValidateAllAsync_DetectsDuplicates`
- `ValidationService_DeleteByValidationErrorsAsync_RemovesInvalidRows`

**7.2. Integration Tests (1 deň)**

End-to-end scenáre:

```csharp
[Fact]
public async Task IntegrationTest_Filter_Sort_Paginate_10MRows()
{
    // Setup
    var grid = CreateGridWithSQLite();
    await grid.Database.InitializeDatabaseAsync();

    // Import 10M rows
    var testData = GenerateTestData(10_000_000);
    await grid.IO.ImportFromDataTableAsync(testData);

    // Apply filter (Age > 30)
    await grid.Filtering.ApplyFilterAsync("Age", FilterOperator.GreaterThan, 30);

    // Sort by Name ASC
    await grid.Sorting.SortByColumnAsync("Name", SortDirection.Ascending);

    // Load first page
    var page1 = await grid.Rows.GetPagedRowsAsync(1, 1000, onlyFiltered: true);

    // Assertions
    Assert.Equal(1000, page1.Count);
    Assert.All(page1, row => Assert.True((int)row["Age"] > 30));
    Assert.True(IsListSorted(page1, "Name"));
}
```

**7.3. Performance Benchmarks (1 deň)**

Run benchmarks z `docu_SortFilterSearchIntegration.md`:

| Operácia | Target | Actual | Status |
|----------|--------|--------|--------|
| Bulk insert 100k rows | < 5s | ? | ⏳ |
| Bulk insert 1M rows | < 30s | ? | ⏳ |
| Filter 10M rows (3 conditions) | < 2s | ? | ⏳ |
| Sort 10M rows (single column) | < 5s | ? | ⏳ |
| Search 10M rows (FTS5) | < 2s | ? | ⏳ |
| Page load (filtered+sorted) | < 200ms | ? | ⏳ |

**7.4. Memory Profiling (0.5 dňa)**

Použiť Visual Studio Diagnostic Tools:

- Load 10M rows
- Verify že pamäť je ~10 MB (viewport) + SQLite file
- Verify že GC sa nevolá často (max 1x za 10 sekúnd)

---

## 📋 FÁZA 8: DOCUMENTATION & DEMO APP (2 dni)

### Ciele
- Finalizovať dokumentáciu
- Vytvoriť demo scenáre
- README update

### Tasks

**8.1. API Documentation (0.5 dňa)**

Pridať XML comments pre všetky public API:

```csharp
/// <summary>
/// Initialize SQLite database for this grid instance.
/// MUST be called after grid creation, BEFORE any data operations.
/// </summary>
/// <param name="databasePath">Full path to database file. If null, uses temp folder.</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Result indicating success or failure</returns>
public Task<PublicResult> InitializeDatabaseAsync(string? databasePath = null, ...)
```

**8.2. Demo App - Complete Scenarios (1 deň)**

Vytvor kompletné demo scenáre v demo app:

**Scenario 1: Import 1M rows + Filter + Sort + Pagination**
```csharp
// Import 1M rows from CSV
await grid.IO.ImportFromCsvAsync("data_1M.csv");

// Apply filter
await grid.Filtering.ApplyFilterAsync("Age", FilterOperator.GreaterThan, 30);

// Sort
await grid.Sorting.SortByColumnAsync("Name", SortDirection.Ascending);

// Navigate to page 5
await grid.Pagination.GoToPageAsync(5);
```

**Scenario 2: Add Row Modal + Validation**
```csharp
// Click Add Row icon → modal opens
var result = await grid.Rows.AddRowWithDialogAsync();

if (result.IsSuccess)
{
    MessageBox.Show($"Row added with ID: {result.Value}");
}
```

**Scenario 3: Batch Validation + Delete Invalid Rows**
```csharp
// Run validation
var validationResult = await grid.Validation.ValidateAllAsync();

if (validationResult.HasErrors)
{
    // Delete invalid rows
    var deleteResult = await grid.Validation.DeleteByValidationErrorsAsync();
    MessageBox.Show($"Deleted {deleteResult.Value} invalid rows");
}
```

**8.3. README Update (0.5 dňa)**

Update `README.md`:

```markdown
# AdvancedDataGrid - Hybrid SQLite Model

## Features

✅ **Hybrid SQLite Architecture** - 10M+ rows support
✅ **Filter/Sort/Search** - 15-40x faster than pure in-memory
✅ **Virtualization & Pagination** - Max 1000 rows per page
✅ **Add Row Modal Dialog** - Real-time validation
✅ **Three Operation Modes** - Interactive, Headless+ManualUI, Pure Headless

## Quick Start

```csharp
// 1. Create grid instance
var grid = AdvancedDataGridBuilder
    .CreateBuilder()
    .WithOperationMode(PublicDataGridOperationMode.Interactive)
    .WithDispatcherQueue(DispatcherQueue.GetForCurrentThread())
    .Build();

// 2. Initialize database
await grid.Database.InitializeDatabaseAsync();

// 3. Import data
await grid.IO.ImportFromDataTableAsync(myDataTable);

// 4. Use features
await grid.Filtering.ApplyFilterAsync("Age", FilterOperator.GreaterThan, 30);
await grid.Sorting.SortByColumnAsync("Name", SortDirection.Ascending);
```

## Performance Benchmarks

- **Filter** 10M rows: < 2s
- **Sort** 10M rows: < 5s
- **Search** 10M rows (FTS5): < 2s
- **Page load**: < 200ms

## Documentation

- [Hybrid SQLite Architecture](docu_HybridSQLiteArchitecture.md)
- [Database Management](docu_DatabaseManagement.md)
- [Sort/Filter/Search Integration](docu_SortFilterSearchIntegration.md)
- [Add Row Modal](docu_AddRowModalSpecification.md)
```

---

## 🎯 ZÁVEREČNÝ CHECKLIST

Pred merge do `master` branch, over:

### Code Quality
- [ ] Všetky unit testy prechádzajú (100%)
- [ ] Integration testy prechádzajú
- [ ] Performance benchmarks spĺňajú targets
- [ ] Memory profiling ukazuje < 50 MB pre 10M rows viewport
- [ ] Code review completed
- [ ] XML documentation pre všetky public API

### Functionality
- [ ] Filter funguje vo všetkých troch módoch
- [ ] Sort funguje vo všetkých troch módoch
- [ ] Search funguje vo všetkých troch módoch
- [ ] Pagination funguje (Next/Previous/Page numbers)
- [ ] Add Row Modal Dialog funguje v Interactive a Headless+ManualUI
- [ ] Real-time validácia v modal dialogu funguje
- [ ] Database lifecycle (Init/Shutdown/Cleanup) funguje

### Documentation
- [ ] README.md updated
- [ ] API documentation complete (XML comments)
- [ ] Demo app má kompletné scenáre
- [ ] Performance benchmarks documented

---

## 📞 SUPPORT & ISSUES

Ak počas implementácie narazíš na problémy:

1. **Prečítaj znova príslušný dokument** (docu_*.md)
2. **Skontroluj TODO list** v tomto dokumente
3. **Run unit testy** pre konkrétny modul
4. **Performance profiling** (Visual Studio Diagnostic Tools)
5. **Ask for clarification** - vytvor issue v projekte

---

**Koniec implementačnej príručky**

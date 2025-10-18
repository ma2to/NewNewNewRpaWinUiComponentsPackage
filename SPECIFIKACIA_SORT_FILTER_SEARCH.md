# ŠPECIFIKÁCIA: Komplexná implementácia Sort, Filter, Search s Virtualizáciou a Pagináciou

## 📋 ÚVOD

Tento dokument obsahuje detailnú špecifikáciu implementácie funkcií **Sort** (triedenie), **Filter** (filtrovanie) a **Search** (vyhľadávanie) pre komponent **AdvancedDataGrid** s podporou všetkých troch operačných módov, virtualizácie a pagination.

**Dátum vytvorenia:** 18.10.2025
**Verzia:** 1.0
**Jazyk kódu:** English
**Jazyk dokumentácie:** Slovenčina

---

## 🎯 CIELE A POŽIADAVKY

### Hlavné ciele
1. **Plná podpora troch operačných módov:**
   - **Interactive Mode**: Automatický UI update, všetko sa deje vo vnútri komponentu
   - **Headless + Manual UI Update Mode**: UI sa updatuje manuálne cez metódy v entry pointe
   - **Pure Headless Mode**: Žiadne UI, všetko cez entry point metódy

2. **Filter funkcionalita:**
   - Vráti subset dát ktoré vyhovujú filtrovacím podmienkam
   - Zachová reálny dataset (aby sa dal vrátiť k všetkým riadkom po zrušení filtrov)
   - Úpravy vo vyfiltrovaných dátach sa prejavia aj v reálnom datasete
   - Rovnaká funkcionalita vo všetkých troch módoch

3. **Search funkcionalita:**
   - Vráti pole/list referencií na bunky kde sa vyskytuje hľadaná fráza
   - Každá referencia obsahuje: `RowIndex`, `ULID (RowId)`, `ColumnName`, `Value`
   - **Interactive Mode & Headless + Manual UI Update:**
     - Zobrazí tlačidlá/šípky **Next** a **Back** pre navigáciu
     - Cirkulárna navigácia (posledný → prvý, prvý → posledný)
   - **Pure Headless Mode:**
     - Vráti celý zoznam výsledkov
     - Entry point metódy pre Next/Back navigáciu

4. **Sort funkcionalita:**
   - Funguje rovnako vo všetkých troch módoch
   - UI integrácia (klik na column header)
   - Podpora multi-column sort

5. **Virtualizácia a Pagination:**
   - UI zobrazuje **maximum 1000 riadkov** na jednu stranu
   - Nad headers: page numbers a šípky Next/Back
   - Metódy sa vykonávajú nad **celým datasetom** (kde je to relevantné)
   - Podpora pre **10M+ riadkov**

6. **Klasické metódy popri Smart metódach:**
   - Classic `DeleteRow` (nie len `SmartDeleteRow`)
   - Classic `AddRow` (priamy insert do datasetu)

7. **Resource Management:**
   - Optimalizácia CPU, pamäte, RAM
   - Profesionálne, stabilné a rýchle riešenie

---

## 📊 SÚČASNÝ STAV - ANALÝZA IMPLEMENTÁCIE

### 1. **Sort Service** ✅ Čiastočne funkčné

**Súbor:** `Features/Sort/Services/SortService.cs`

**Čo funguje:**
- ✅ LINQ implementácia s paralelným spracovaním
- ✅ Single-column sort (`SortAsync`)
- ✅ Multi-column sort (`MultiSortAsync`) s ThenBy chains
- ✅ Type detection (numeric, DateTime, string)
- ✅ Performance modes (Sequential, Parallel, Auto)
- ✅ Custom sort keys a business rules (`AdvancedSortAsync`)
- ✅ Legacy API (`SortByColumnAsync` - modifikuje IRowStore)

**Čo chýba/nefunguje:**
- ❌ **UI integrácia** - UI control obsahuje TODO komentáre pre column header click
- ❌ **Event firing** - Sort nevyvoláva `OnDataRefreshed` event → žiadny automatický UI refresh
- ❌ **Filtered data support** - Sort nerespektuje aktuálne aktívne filtre
- ❌ **Virtualization awareness** - Sort pracuje nad celým datasetom bez ohľadu na virtualizáciu
- ❌ **Public API wrapper** - Neexistuje `IDataGridSorting` wrapper pre facade
- ❌ **Granular metadata** - Sort výsledok neposkytuje metadata pre InternalUIUpdateHandler

**Kód príklad - Sort funguje ale UI sa neupdatuje:**
```csharp
// SortService.cs:312
public async Task<bool> SortByColumnAsync(string columnName, SortDirection direction, ...)
{
    var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
    var sortedRows = /* LINQ sort */ ;

    await _rowStore.ReplaceAllRowsAsync(sortedRows, cancellationToken);
    // ❌ CHÝBA: UI refresh event trigger
    // ❌ CHÝBA: _uiNotificationService.NotifyDataRefreshAsync()
    return true;
}
```

---

### 2. **Filter Service** ❌ Nefunkčné - iba počíta matches

**Súbor:** `Features/Filter/Services/FilterService.cs`

**Čo funguje:**
- ✅ Filter criteria storage (`ConcurrentBag<FilterCriteria>`)
- ✅ Komplexná business logika pre filter operátory (Equals, Contains, GreaterThan, IsNull, ...)
- ✅ Batch processing optimalizácia
- ✅ Thread-safe implementácia
- ✅ `GetFilteredDataAsync()` - vracia filtered dataset

**Čo chýba/nefunguje:**
- ❌ **Filter NIE JE aplikovaný na dáta!** - Metóda `ApplyFilterAsync` iba **počíta matching rows** ale NEupdatuje IRowStore
- ❌ **Žiadna filtered view** - IRowStore neudržiava filtered subset
- ❌ **Žiadny UI update** - Filter nevyvoláva `OnDataRefreshed` event
- ❌ **Edits vo filtered data sa nepremietajú** - Chýba mapovanie filtered row → original row
- ❌ **UI filter controls sú TODO** - AdvancedDataGridControl.cs:245 obsahuje TODO komentáre

**KRITICKÁ CHYBA - Filter len počíta, nedáva filtered view:**
```csharp
// FilterService.cs:52
public async Task<int> ApplyFilterAsync(string columnName, FilterOperator @operator, object? value)
{
    _activeFilters.Add(filter); // ✅ Filter criteria uložené

    var filteredCount = await ApplyFiltersToDataAsync(operationId); // ✅ Spočíta matching rows

    // ❌ CHÝBA: Aplikovať filtered view do IRowStore
    // ❌ CHÝBA: _rowStore.SetFilterCriteria(_activeFilters)
    // ❌ CHÝBA: UI refresh event trigger

    return filteredCount; // ❌ Vracia len COUNT, nie filtered data!
}
```

**Ďalší problém - GetFilteredDataAsync existuje ale sa NEPOUŽÍVA:**
```csharp
// FilterService.cs:548
public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetFilteredDataAsync()
{
    var activeFilters = _activeFilters.ToArray();
    var allData = await _rowStore.GetAllRowsAsync(default);

    if (activeFilters.Length == 0) return allData.ToList();

    var filteredData = new List<IReadOnlyDictionary<string, object?>>();
    foreach (var row in allData)
    {
        if (RowMatchesAllFilters(row, activeFilters))
            filteredData.Add(row);
    }

    return filteredData; // ✅ Táto metóda FUNGUJE ale NIKDE sa nevolá!
}
```

---

### 3. **Search Service** ⚠️ Čiastočne funkčné - chybuje UI integration

**Súbor:** `Features/Search/Services/SearchService.cs`

**Čo funguje:**
- ✅ Basic search (`SearchAsync`) s LINQ optimizáciou
- ✅ Advanced search (`AdvancedSearchAsync`) s regex, fuzzy matching
- ✅ Search modes: Contains, Exact, StartsWith, EndsWith, Regex, Fuzzy
- ✅ Search scopes: AllData, FilteredData, VisibleData, SelectedData
- ✅ Parallel processing pre veľké datasety (>1000 riadkov)
- ✅ Smart ranking (Relevance, Position, Frequency)
- ✅ Výsledky obsahujú: `RowIndex`, `ColumnName`, `Value`, `MatchScore`, `RelevanceScore`

**Čo chýba/nefunguje:**
- ❌ **Highlighting je TODO** - SearchService.cs:896-928 obsahuje iba placeholder
- ❌ **Navigation Next/Previous je TODO** - SearchService.cs:969-1065 iba placeholder
- ❌ **UI integration chýba** - AdvancedDataGridControl.cs:218 obsahuje TODO komentár
- ❌ **Cirkulárna navigácia** - Logika existuje ale NIE JE pripojená k UI
- ❌ **ULID v results** - SearchResult neobsahuje RowId (ULID), iba RowIndex

**TODO Komentáre v kóde:**
```csharp
// SearchService.cs:908
public async Task<Result> HighlightSearchMatchesAsync(...)
{
    await Task.CompletedTask; // Placeholder

    // TODO: Implement actual highlighting logic when UI layer is connected
    // This would typically involve:
    // - Applying visual styles to matched cells
    // - Storing highlight state for rendering
    // - Triggering UI refresh

    return Result.Success();
}

// SearchService.cs:969
public async Task<Result> GoToNextMatchAsync(...)
{
    await Task.CompletedTask; // Placeholder

    // TODO: Implement actual navigation when UI layer is connected
    // This would typically involve:
    // - Scrolling to the next match row/cell
    // - Highlighting the current match
    // - Updating selection state

    return Result.Success();
}
```

---

### 4. **Virtualizácia a Pagination** ❌ Neexistuje

**Súbor:** `UIControls/DataGridCellsView.cs`, `ViewModels/DataGridViewModel.cs`

**Čo existuje:**
- ⚠️ `BulkObservableCollection<DataGridRowViewModel> Rows` - môže obsahovať 10M+ riadkov
- ⚠️ UI rendering potenciálne renderuje všetky riadky (performance problém!)

**Čo chýba:**
- ❌ **Virtualization strategy** - Žiadna logika pre zobrazenie len 1000 riadkov
- ❌ **Pagination UI controls** - Žiadne page numbers, Next/Back šípky nad headers
- ❌ **Pagination state management** - Žiadny `CurrentPage`, `PageSize`, `TotalPages`
- ❌ **Scroll virtualization** - Žiadna `VirtualizingStackPanel` alebo podobný mechanizmus
- ❌ **Virtual row rendering** - UI renderuje všetky riadky namiesto len visible subset

---

### 5. **UI Control Integration** ❌ TODO Komentáre všade

**Súbor:** `UIControls/AdvancedDataGridControl.cs`

**TODO Komentáre v kóde:**
```csharp
// Line 218
private void OnSearchRequested(object? sender, EventArgs e)
{
    _logger?.LogInformation("Search requested with text: {SearchText}", ViewModel.SearchPanel.SearchText);
    // TODO: Implement search via Facade API
    // Call IAdvancedDataGridFacade.SearchAsync with ViewModel.SearchPanel.SearchText
}

// Line 245
private void OnApplyFiltersRequested(object? sender, EventArgs e)
{
    _logger?.LogInformation("Apply filters requested");
    // TODO: Apply filters via Facade API
    // Call IAdvancedDataGridFacade.ApplyFilterAsync for each column filter
}
```

**Problém:**
- UI eventy existujú ale NIE SÚ pripojené k Facade API metódam
- Search/Filter/Sort tlačidlá nerobíc nič (iba logujú)

---

### 6. **Event System - OnDataRefreshed** ⚠️ Funguje len pre CRUD

**Súbor:** `UIAdapters/WinUI/InternalUIUpdateHandler.cs`, `UIAdapters/WinUI/UiNotificationService.cs`

**Čo funguje:**
- ✅ `OnDataRefreshed` event existuje a funguje pre SmartDelete, AddRow, UpdateCell
- ✅ Granular updates (physical delete, content clear, row updates)
- ✅ Automatic UI refresh v Interactive mode

**Čo chýba:**
- ❌ Sort nevyvoláva `OnDataRefreshed` event
- ❌ Filter nevyvoláva `OnDataRefreshed` event
- ❌ Search nevyvoláva UI update (highlighting)

---

## 🔧 TECHNICKÉ RIEŠENIE

### ARCHITEKTÚRA RIEŠENIA

Riešenie sa skladá z nasledujúcich komponentov:

```
┌─────────────────────────────────────────────────────────────┐
│                    FACADE API (Entry Point)                  │
│  facade.Sorting.SortByColumnAsync(...)                       │
│  facade.Filtering.ApplyFilterAsync(...)                      │
│  facade.Search.SearchAsync(...) / NavigateNext/Previous()    │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ├─ Interactive Mode: Automatic UI Update
                  ├─ Headless + Manual UI: Manual RefreshUIAsync()
                  └─ Pure Headless: No UI, data returns only
                  │
┌─────────────────▼───────────────────────────────────────────┐
│              INTERNAL SERVICES LAYER                         │
│  SortService    FilterService    SearchService               │
│  (LINQ logic)   (Criteria eval)  (Regex/Fuzzy)              │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│                    IRowStore (Data Layer)                    │
│  - AllData: Full dataset (10M+ rows)                         │
│  - FilteredView: Subset matching filter criteria             │
│  - VirtualPage: Visible 1000 rows for current page           │
└─────────────────┬───────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────────┐
│                UI NOTIFICATION LAYER                         │
│  UiNotificationService.OnDataRefreshed(eventArgs)            │
│  ↓                                                            │
│  InternalUIUpdateHandler.ApplyGranularUpdates()             │
│  ↓                                                            │
│  DataGridViewModel.LoadRows(virtualizedRows)                │
└──────────────────────────────────────────────────────────────┘
```

---

## 📐 DETAILNÉ RIEŠENIE: FILTER

### Problém
- Filter service len počíta matching rows, nevracia filtered view
- IRowStore neudržiava filtered subset
- UI neupdatuje sa po aplikovaní filtra
- Edits vo filtered data sa nepremietajú do real dataset

### Riešenie

#### 1. **Rozšírenie IRowStore Interface**

Pridať podporu pre filtered view:

```csharp
// IRowStore.cs
internal interface IRowStore
{
    // ... existujúce metódy ...

    /// <summary>
    /// Sets active filter criteria and builds filtered view index
    /// </summary>
    void SetFilterCriteria(IReadOnlyList<FilterCriteria> criteria);

    /// <summary>
    /// Gets all rows (filtered or unfiltered based on criteria)
    /// </summary>
    /// <param name="onlyFiltered">If true, returns only filtered view</param>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets row count (filtered or unfiltered)
    /// </summary>
    Task<long> GetRowCountAsync(
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears filter criteria and filtered view index
    /// </summary>
    void ClearFilterCriteria();

    /// <summary>
    /// Maps filtered row index to original row index
    /// CRITICAL: For edits in filtered view to update real dataset
    /// </summary>
    int? MapFilteredIndexToOriginalIndex(int filteredIndex);
}
```

#### 2. **InMemoryRowStore Implementation**

Implementovať filtered view v InMemoryRowStore:

```csharp
// InMemoryRowStore.cs
internal sealed class InMemoryRowStore : IRowStore
{
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>> _rows;
    private IReadOnlyList<FilterCriteria>? _activeFilterCriteria;
    private List<string>? _filteredRowIds; // Cached filtered row IDs
    private Dictionary<int, int>? _filteredToOriginalIndexMap; // filteredIdx -> originalIdx

    public void SetFilterCriteria(IReadOnlyList<FilterCriteria> criteria)
    {
        _activeFilterCriteria = criteria;

        if (criteria == null || criteria.Count == 0)
        {
            _filteredRowIds = null;
            _filteredToOriginalIndexMap = null;
            _logger.LogInformation("Filter criteria cleared");
            return;
        }

        // Build filtered view index
        _filteredRowIds = new List<string>();
        _filteredToOriginalIndexMap = new Dictionary<int, int>();

        var allRows = GetAllRows(); // Unfiltered
        int filteredIdx = 0;

        for (int originalIdx = 0; originalIdx < allRows.Count; originalIdx++)
        {
            var row = allRows[originalIdx];
            if (RowMatchesAllFilters(row, criteria))
            {
                var rowId = row["__rowId"]?.ToString();
                if (!string.IsNullOrEmpty(rowId))
                {
                    _filteredRowIds.Add(rowId);
                    _filteredToOriginalIndexMap[filteredIdx] = originalIdx;
                    filteredIdx++;
                }
            }
        }

        _logger.LogInformation("Filter criteria set: {FilterCount} filters, {MatchCount} matching rows",
            criteria.Count, _filteredRowIds.Count);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default)
    {
        if (!onlyFiltered || _filteredRowIds == null)
        {
            // Return all rows
            return _rows.Values.ToList();
        }

        // Return filtered view
        var filteredRows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var rowId in _filteredRowIds)
        {
            if (_rows.TryGetValue(rowId, out var row))
            {
                filteredRows.Add(row);
            }
        }

        return await Task.FromResult(filteredRows);
    }

    public int? MapFilteredIndexToOriginalIndex(int filteredIndex)
    {
        if (_filteredToOriginalIndexMap == null)
            return filteredIndex; // No filter active, indices are the same

        return _filteredToOriginalIndexMap.TryGetValue(filteredIndex, out var originalIndex)
            ? originalIndex
            : null;
    }

    private bool RowMatchesAllFilters(IReadOnlyDictionary<string, object?> row, IReadOnlyList<FilterCriteria> criteria)
    {
        // DELEGATED to FilterService - avoid code duplication
        // FilterService already has RowMatchesFilter logic
        foreach (var filter in criteria)
        {
            if (!FilterService.MatchesFilter(row, filter))
                return false;
        }
        return true;
    }
}
```

#### 3. **FilterService Update**

Upraviť FilterService aby aplikoval filter do IRowStore:

```csharp
// FilterService.cs
public async Task<int> ApplyFilterAsync(string columnName, FilterOperator @operator, object? value)
{
    // ... existujúci kód ...

    _activeFilters.Add(filter);

    // ✅ NOVÉ: Aplikovať filter criteria do IRowStore
    _rowStore.SetFilterCriteria(_activeFilters.ToArray());

    // Get filtered count from IRowStore
    var filteredCount = (int)await _rowStore.GetRowCountAsync(onlyFiltered: true, default);

    // ✅ NOVÉ: Trigger UI refresh event
    if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
    {
        var eventArgs = new PublicDataRefreshEventArgs
        {
            AffectedRows = filteredCount,
            OperationType = "ApplyFilter",
            RefreshTime = DateTime.UtcNow,
            // Full reload needed because filter changes entire view
            RequiresFullReload = true
        };

        await _uiNotificationService.NotifyDataRefreshWithMetadataAsync(eventArgs);
    }

    return filteredCount;
}

public async Task<int> ClearFiltersAsync()
{
    // ... existujúci kód ...

    while (_activeFilters.TryTake(out _)) { }

    // ✅ NOVÉ: Clear filter criteria in IRowStore
    _rowStore.ClearFilterCriteria();

    var totalRows = await _rowStore.GetRowCountAsync(onlyFiltered: false, default);

    // ✅ NOVÉ: Trigger UI refresh event
    if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
    {
        var eventArgs = new PublicDataRefreshEventArgs
        {
            AffectedRows = (int)totalRows,
            OperationType = "ClearFilters",
            RefreshTime = DateTime.UtcNow,
            RequiresFullReload = true
        };

        await _uiNotificationService.NotifyDataRefreshWithMetadataAsync(eventArgs);
    }

    return (int)totalRows;
}
```

#### 4. **Editing Filtered Data**

Upraviť editing API aby mapoval filtered index → original index:

```csharp
// DataGridEditing.cs
public async Task<PublicResult> UpdateCellAsync(int rowIndex, string columnName, object? value, ...)
{
    // ✅ NOVÉ: Map filtered index to original index if filter is active
    var originalIndex = _rowStore.MapFilteredIndexToOriginalIndex(rowIndex) ?? rowIndex;

    // Update cell in original dataset using originalIndex
    var row = await _rowStore.GetRowAsync(originalIndex, cancellationToken);
    if (row == null)
        return PublicResult.Failure("Row not found");

    var updatedRow = new Dictionary<string, object?>(row);
    updatedRow[columnName] = value;

    await _rowStore.UpdateRowAsync(originalIndex, updatedRow, cancellationToken);

    // UI update (filtered view will automatically reflect change)
    ...
}
```

#### 5. **Public API Wrapper**

```csharp
// IDataGridFiltering.cs (Public API)
public interface IDataGridFiltering
{
    Task<PublicResult> ApplyFilterAsync(string columnName, PublicFilterOperator @operator, object? value, CancellationToken cancellationToken = default);
    Task<PublicResult> ApplyMultipleFiltersAsync(IEnumerable<PublicFilterCriteria> filters, CancellationToken cancellationToken = default);
    Task<PublicResult> RemoveFilterAsync(string columnName, CancellationToken cancellationToken = default);
    Task<PublicResult> ClearAllFiltersAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<PublicFilterCriteria> GetActiveFilters();
    bool IsColumnFiltered(string columnName);
    int GetFilterCount();
    Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default);
    Task<long> GetTotalRowCountAsync(CancellationToken cancellationToken = default);
}
```

---

## 📐 DETAILNÉ RIEŠENIE: SEARCH

### Problém
- Highlighting a Next/Previous navigation sú TODO
- SearchResult neobsahuje RowId (ULID)
- UI integrácia chýba
- Cirkulárna navigácia nie je implementovaná

### Riešenie

#### 1. **Rozšírenie SearchResult**

Pridať RowId do SearchResult:

```csharp
// SearchResult.cs
public record SearchResult
{
    public int RowIndex { get; init; }
    public string RowId { get; init; } = ""; // ✅ NOVÉ: ULID stable identifier
    public string ColumnName { get; init; } = "";
    public object? Value { get; init; }
    public string MatchedText { get; init; } = "";
    public bool IsExactMatch { get; init; }
    public double MatchScore { get; init; }
    public double RelevanceScore { get; init; }
    public SearchMode SearchMode { get; init; }
    public bool ShouldHighlight { get; init; }

    public static SearchResult Create(int rowIndex, string rowId, string columnName, object? value, string matchedText)
    {
        return new SearchResult
        {
            RowIndex = rowIndex,
            RowId = rowId, // ✅ NOVÉ
            ColumnName = columnName,
            Value = value,
            MatchedText = matchedText,
            MatchScore = 1.0,
            RelevanceScore = 0.8,
            SearchMode = SearchMode.Contains,
            ShouldHighlight = true
        };
    }
}
```

#### 2. **SearchService Update - Extract RowId**

Upraviť SearchService aby extrahoval RowId z dát:

```csharp
// SearchService.cs
public async Task<SearchResultCollection> SearchAsync(SearchCommand command, ...)
{
    // ... existujúci kód ...

    for (var rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
    {
        var row = dataList[rowIndex];

        // ✅ NOVÉ: Extract RowId
        var rowId = row.TryGetValue("__rowId", out var rowIdValue)
            ? rowIdValue?.ToString() ?? ""
            : "";

        foreach (var columnName in searchColumns)
        {
            if (row.TryGetValue(columnName, out var value))
            {
                var text = value?.ToString() ?? string.Empty;
                if (SearchFilterAlgorithms.IsMatch(text, command.SearchText, false, command.CaseSensitive))
                {
                    results.Add(SearchResult.Create(rowIndex, rowId, columnName, value, text)); // ✅ NOVÉ: RowId parameter
                }
            }
        }
    }

    // ...
}
```

#### 3. **Search Navigation State Management**

Vytvoriť search navigation state:

```csharp
// SearchNavigationState.cs
internal sealed class SearchNavigationState
{
    public SearchResultCollection? Results { get; set; }
    public int CurrentMatchIndex { get; set; } = 0;
    public bool IsActive => Results != null && Results.Results.Count > 0;

    public SearchResult? GetCurrentMatch()
    {
        if (!IsActive) return null;
        return Results.Results[CurrentMatchIndex];
    }

    public SearchResult? NavigateNext()
    {
        if (!IsActive) return null;

        // Circular navigation: last → first
        CurrentMatchIndex = (CurrentMatchIndex + 1) % Results.Results.Count;
        return GetCurrentMatch();
    }

    public SearchResult? NavigatePrevious()
    {
        if (!IsActive) return null;

        // Circular navigation: first → last
        CurrentMatchIndex = CurrentMatchIndex - 1;
        if (CurrentMatchIndex < 0)
            CurrentMatchIndex = Results.Results.Count - 1;

        return GetCurrentMatch();
    }

    public void Clear()
    {
        Results = null;
        CurrentMatchIndex = 0;
    }
}
```

#### 4. **Highlighting Implementation**

Implementovať highlighting v CellViewModel:

```csharp
// CellViewModel.cs
public sealed class CellViewModel : ViewModelBase
{
    // ... existujúce properties ...

    private bool _isSearchHighlighted;
    public bool IsSearchHighlighted
    {
        get => _isSearchHighlighted;
        set => SetProperty(ref _isSearchHighlighted, value);
    }

    private string? _searchHighlightText;
    public string? SearchHighlightText
    {
        get => _searchHighlightText;
        set => SetProperty(ref _searchHighlightText, value);
    }
}
```

Implementovať highlighting v SearchService:

```csharp
// SearchService.cs
public async Task<Result> HighlightSearchMatchesAsync(SearchResultCollection searchResults, ...)
{
    if (_viewModel == null)
        return Result.Failure("ViewModel not available");

    // Clear previous highlights
    foreach (var row in _viewModel.Rows)
    {
        foreach (var cell in row.Cells)
        {
            cell.IsSearchHighlighted = false;
            cell.SearchHighlightText = null;
        }
    }

    // Apply new highlights
    foreach (var result in searchResults.Results)
    {
        if (result.RowIndex >= 0 && result.RowIndex < _viewModel.Rows.Count)
        {
            var row = _viewModel.Rows[result.RowIndex];
            var cell = row.Cells.FirstOrDefault(c => c.ColumnName == result.ColumnName);
            if (cell != null)
            {
                cell.IsSearchHighlighted = true;
                cell.SearchHighlightText = result.MatchedText;
            }
        }
    }

    _logger.LogInformation("Highlighted {Count} search matches", searchResults.Results.Count);
    return Result.Success();
}
```

#### 5. **Navigation Implementation**

```csharp
// SearchService.cs
private readonly SearchNavigationState _navigationState = new();

public async Task<Result<SearchResult?>> GoToNextMatchAsync(CancellationToken cancellationToken = default)
{
    var nextMatch = _navigationState.NavigateNext();
    if (nextMatch == null)
        return Result<SearchResult?>.Failure("No search matches available");

    // Scroll to match in UI
    if (_viewModel != null && _dispatcherQueue != null)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // CRITICAL: Scroll to row and highlight
            ScrollToRow(nextMatch.RowIndex);
            HighlightCell(nextMatch.RowIndex, nextMatch.ColumnName);
        });
    }

    _logger.LogInformation("Navigated to next match: {RowIndex}, {ColumnName} ({Current}/{Total})",
        nextMatch.RowIndex, nextMatch.ColumnName,
        _navigationState.CurrentMatchIndex + 1, _navigationState.Results.Results.Count);

    return Result<SearchResult?>.Success(nextMatch);
}

public async Task<Result<SearchResult?>> GoToPreviousMatchAsync(CancellationToken cancellationToken = default)
{
    var prevMatch = _navigationState.NavigatePrevious();
    if (prevMatch == null)
        return Result<SearchResult?>.Failure("No search matches available");

    // Scroll to match in UI
    if (_viewModel != null && _dispatcherQueue != null)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            ScrollToRow(prevMatch.RowIndex);
            HighlightCell(prevMatch.RowIndex, prevMatch.ColumnName);
        });
    }

    _logger.LogInformation("Navigated to previous match: {RowIndex}, {ColumnName} ({Current}/{Total})",
        prevMatch.RowIndex, prevMatch.ColumnName,
        _navigationState.CurrentMatchIndex + 1, _navigationState.Results.Results.Count);

    return Result<SearchResult?>.Success(prevMatch);
}
```

#### 6. **UI Controls - Next/Previous Buttons**

Pridať Next/Previous buttons do SearchPanelView:

```csharp
// SearchPanelView.cs
private Button? _nextButton;
private Button? _previousButton;
private TextBlock? _matchCountText;

private void InitializeUI()
{
    // ... existujúci search box ...

    // ✅ NOVÉ: Next/Previous buttons
    _previousButton = new Button
    {
        Content = "◀ Previous",
        IsEnabled = false,
        Margin = new Thickness(5, 0, 0, 0)
    };
    _previousButton.Click += OnPreviousClicked;

    _nextButton = new Button
    {
        Content = "Next ▶",
        IsEnabled = false,
        Margin = new Thickness(5, 0, 0, 0)
    };
    _nextButton.Click += OnNextClicked;

    _matchCountText = new TextBlock
    {
        Text = "",
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(10, 0, 0, 0)
    };

    // Add to layout
    // ...
}

private async void OnNextClicked(object sender, RoutedEventArgs e)
{
    NextMatchRequested?.Invoke(this, EventArgs.Empty);
}

private async void OnPreviousClicked(object sender, RoutedEventArgs e)
{
    PreviousMatchRequested?.Invoke(this, EventArgs.Empty);
}

public void UpdateNavigationState(int currentIndex, int totalMatches)
{
    bool hasMatches = totalMatches > 0;
    _nextButton.IsEnabled = hasMatches;
    _previousButton.IsEnabled = hasMatches;
    _matchCountText.Text = hasMatches
        ? $"{currentIndex + 1} / {totalMatches}"
        : "";
}
```

#### 7. **Public API Wrapper**

```csharp
// IDataGridSearch.cs (Public API)
public interface IDataGridSearch
{
    Task<PublicSearchResult> SearchAsync(string searchText, PublicSearchOptions? options = null, CancellationToken cancellationToken = default);
    Task<PublicResult<PublicSearchMatch?>> NavigateToNextMatchAsync(CancellationToken cancellationToken = default);
    Task<PublicResult<PublicSearchMatch?>> NavigateToPreviousMatchAsync(CancellationToken cancellationToken = default);
    Task<PublicResult> ClearSearchHighlightsAsync(CancellationToken cancellationToken = default);

    int GetCurrentMatchIndex();
    int GetTotalMatchCount();
    bool HasActiveSearch();
}

public record PublicSearchMatch
{
    public int RowIndex { get; init; }
    public string RowId { get; init; } = ""; // ULID
    public string ColumnName { get; init; } = "";
    public object? Value { get; init; }
}

public record PublicSearchResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<PublicSearchMatch> Matches { get; init; } = Array.Empty<PublicSearchMatch>();
    public int TotalMatches => Matches.Count;
    public string? ErrorMessage { get; init; }
}
```

---

## 📐 DETAILNÉ RIEŠENIE: SORT

### Problém
- Sort funguje ale nevyvoláva UI refresh event
- Nerespektuje aktívne filtre
- UI integrácia (column header click) chýba
- Sort ignoruje virtualizáciu

### Riešenie

#### 1. **SortService - Trigger UI Refresh Event**

Upraviť SortService aby vyvolával UI refresh:

```csharp
// SortService.cs
public async Task<bool> SortByColumnAsync(string columnName, SortDirection direction, ...)
{
    var allRows = await _rowStore.GetAllRowsAsync(onlyFiltered: false, cancellationToken);

    var sortedRows = direction == SortDirection.Ascending
        ? allRows.OrderBy(r => SortAlgorithms.GetSortValue(r, columnName)).ToList()
        : allRows.OrderByDescending(r => SortAlgorithms.GetSortValue(r, columnName)).ToList();

    await _rowStore.ReplaceAllRowsAsync(sortedRows, cancellationToken);
    _currentSort = new() { (columnName, direction) };

    // ✅ NOVÉ: Trigger UI refresh event
    if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
    {
        var eventArgs = new PublicDataRefreshEventArgs
        {
            AffectedRows = sortedRows.Count,
            OperationType = "Sort",
            RefreshTime = DateTime.UtcNow,
            RequiresFullReload = true // Sort changes entire order
        };

        await _uiNotificationService.NotifyDataRefreshWithMetadataAsync(eventArgs);
    }

    _logger.LogInformation("Sort completed: {RowCount} rows sorted by {ColumnName} {Direction}",
        sortedRows.Count, columnName, direction);

    return true;
}
```

#### 2. **Sort with Filter Support**

Pridať podporu pre sort filtered view:

```csharp
// SortService.cs
public async Task<bool> SortByColumnAsync(string columnName, SortDirection direction,
    bool onlyFiltered = false, // ✅ NOVÉ
    CancellationToken cancellationToken = default)
{
    // Get rows (filtered or all)
    var rows = await _rowStore.GetAllRowsAsync(onlyFiltered, cancellationToken);

    var sortedRows = direction == SortDirection.Ascending
        ? rows.OrderBy(r => SortAlgorithms.GetSortValue(r, columnName)).ToList()
        : rows.OrderByDescending(r => SortAlgorithms.GetSortValue(r, columnName)).ToList();

    if (onlyFiltered)
    {
        // CRITICAL: When sorting filtered view, we need to update filtered row order
        // but preserve the filtering criteria and original dataset
        await _rowStore.UpdateFilteredViewOrder(sortedRows, cancellationToken);
    }
    else
    {
        // Sort entire dataset
        await _rowStore.ReplaceAllRowsAsync(sortedRows, cancellationToken);
    }

    // ... trigger UI refresh ...
}
```

#### 3. **IRowStore - UpdateFilteredViewOrder**

Pridať metódu do IRowStore pre update filtered view order:

```csharp
// IRowStore.cs
internal interface IRowStore
{
    // ... existujúce metódy ...

    /// <summary>
    /// Updates the order of filtered view without changing filter criteria
    /// </summary>
    Task UpdateFilteredViewOrder(IReadOnlyList<IReadOnlyDictionary<string, object?>> sortedFilteredRows, CancellationToken cancellationToken = default);
}
```

#### 4. **UI Integration - Column Header Click**

Implementovať column header click pre sort:

```csharp
// HeadersRowView.cs
private void OnColumnHeaderClicked(object? sender, ColumnHeaderViewModel header)
{
    if (header.IsSpecialColumn)
        return; // Special columns are not sortable

    _logger?.LogInformation("Column header clicked: {ColumnName}", header.ColumnName);

    // Fire event for facade to handle
    ColumnHeaderClicked?.Invoke(this, header);
}

public event EventHandler<ColumnHeaderViewModel>? ColumnHeaderClicked;
```

Pripojiť event v AdvancedDataGridControl:

```csharp
// AdvancedDataGridControl.cs
private void InitializeSubViews()
{
    // ... existujúci kód ...

    _headersRowView = new HeadersRowView(ViewModel);
    _headersRowView.ColumnHeaderClicked += OnColumnHeaderClicked; // ✅ NOVÉ
    _headersRowContainer.Child = _headersRowView;
}

private async void OnColumnHeaderClicked(object? sender, ColumnHeaderViewModel header)
{
    _logger?.LogInformation("Sort requested for column: {ColumnName}", header.ColumnName);

    // ✅ NOVÉ: Call facade API
    // TODO: Get facade reference (via constructor injection or service provider)
    // await _facade.Sorting.ToggleSortDirectionAsync(header.ColumnName);
}
```

#### 5. **Public API Wrapper**

```csharp
// IDataGridSorting.cs (Public API)
public interface IDataGridSorting
{
    Task<PublicResult> SortByColumnAsync(string columnName, PublicSortDirection direction = PublicSortDirection.Ascending, CancellationToken cancellationToken = default);
    Task<PublicResult> SortByMultipleColumnsAsync(IReadOnlyList<PublicSortDescriptor> sortDescriptors, CancellationToken cancellationToken = default);
    Task<PublicResult> ClearSortingAsync(CancellationToken cancellationToken = default);
    Task<PublicResult<PublicSortDirection>> ToggleSortDirectionAsync(string columnName, CancellationToken cancellationToken = default);

    IReadOnlyList<PublicSortDescriptor> GetCurrentSortDescriptors();
    bool IsColumnSorted(string columnName);
    PublicSortDirection GetColumnSortDirection(string columnName);
}

public enum PublicSortDirection
{
    None,
    Ascending,
    Descending
}

public record PublicSortDescriptor
{
    public string ColumnName { get; init; } = "";
    public PublicSortDirection Direction { get; init; }
    public int Priority { get; init; } = 0;
    public bool IsEnabled { get; init; } = true;
}
```

---

## 📐 DETAILNÉ RIEŠENIE: VIRTUALIZÁCIA A PAGINATION

### Problém
- UI potenciálne renderuje všetky riadky (10M+) → performance katastrofa
- Žiadna pagination UI (page numbers, Next/Back)
- Žiadny virtualization state management
- Scroll virtualization neexistuje

### Riešenie

#### 1. **Pagination State Management**

Vytvoriť pagination state v DataGridViewModel:

```csharp
// DataGridViewModel.cs
public sealed class DataGridViewModel : ViewModelBase
{
    // ... existujúce properties ...

    // ✅ NOVÉ: Pagination state
    private int _currentPage = 1;
    private int _pageSize = 1000; // Max 1000 rows per page
    private long _totalRowCount = 0;

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    public long TotalRowCount
    {
        get => _totalRowCount;
        set
        {
            if (SetProperty(ref _totalRowCount, value))
            {
                OnPropertyChanged(nameof(TotalPages));
            }
        }
    }

    public int TotalPages => (int)Math.Ceiling((double)TotalRowCount / PageSize);
    public bool CanGoToNextPage => CurrentPage < TotalPages;
    public bool CanGoToPreviousPage => CurrentPage > 1;

    public void GoToNextPage()
    {
        if (CanGoToNextPage)
        {
            CurrentPage++;
            PageChanged?.Invoke(this, CurrentPage);
        }
    }

    public void GoToPreviousPage()
    {
        if (CanGoToPreviousPage)
        {
            CurrentPage--;
            PageChanged?.Invoke(this, CurrentPage);
        }
    }

    public void GoToPage(int pageNumber)
    {
        if (pageNumber >= 1 && pageNumber <= TotalPages)
        {
            CurrentPage = pageNumber;
            PageChanged?.Invoke(this, CurrentPage);
        }
    }

    public event EventHandler<int>? PageChanged;
}
```

#### 2. **IRowStore - Paginated Data Retrieval**

Rozšíriť IRowStore o pagination support:

```csharp
// IRowStore.cs
internal interface IRowStore
{
    // ... existujúce metódy ...

    /// <summary>
    /// Gets paginated rows for current page
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of rows per page</param>
    /// <param name="onlyFiltered">If true, paginates filtered view</param>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPagedRowsAsync(
        int pageNumber,
        int pageSize,
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total row count for pagination calculation
    /// </summary>
    Task<long> GetTotalRowCountForPaginationAsync(
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default);
}
```

Implementovať v InMemoryRowStore:

```csharp
// InMemoryRowStore.cs
public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPagedRowsAsync(
    int pageNumber,
    int pageSize,
    bool onlyFiltered = false,
    CancellationToken cancellationToken = default)
{
    var allRows = await GetAllRowsAsync(onlyFiltered, cancellationToken);

    // Calculate skip and take
    var skip = (pageNumber - 1) * pageSize;
    var take = pageSize;

    var pagedRows = allRows.Skip(skip).Take(take).ToList();

    _logger.LogInformation("Retrieved page {PageNumber} with {RowCount} rows (pageSize={PageSize}, total={Total})",
        pageNumber, pagedRows.Count, pageSize, allRows.Count);

    return pagedRows;
}

public async Task<long> GetTotalRowCountForPaginationAsync(bool onlyFiltered = false, ...)
{
    return await GetRowCountAsync(onlyFiltered, cancellationToken);
}
```

#### 3. **LoadRows with Pagination**

Upraviť LoadRows metódu aby loadla len current page:

```csharp
// DataGridViewModel.cs
public async Task LoadCurrentPageAsync()
{
    if (_rowStore == null)
    {
        _logger?.LogWarning("RowStore not available for pagination");
        return;
    }

    _logger?.LogInformation("Loading page {PageNumber} (pageSize={PageSize})", CurrentPage, PageSize);

    // Get total count for pagination calculation
    bool onlyFiltered = _options.FilteringEnabled && HasActiveFilters();
    TotalRowCount = await _rowStore.GetTotalRowCountForPaginationAsync(onlyFiltered);

    // Get current page rows
    var pagedRows = await _rowStore.GetPagedRowsAsync(CurrentPage, PageSize, onlyFiltered);

    // Load rows into ViewModel (same as before, but with paged data)
    LoadRows(pagedRows);

    _logger?.LogInformation("Page {PageNumber}/{TotalPages} loaded with {RowCount} rows",
        CurrentPage, TotalPages, pagedRows.Count);
}
```

#### 4. **Pagination UI Controls**

Vytvoriť pagination panel (detaily vynechané kvôli dĺžke - viď vyššie detailný kód)

#### 5. **Performance - Scroll Virtualization**

Pre dodatočnú performance optimalizáciu, použiť `ItemsRepeater` s virtualizáciou namiesto Grid.

---

## 📐 CLASSIC METÓDY (popri Smart metódach)

### Požiadavka
Pridať "klasické" metódy popri existujúcich Smart metódach:
- Classic `DeleteRow` - priamo zmaže riadok bez smart logiky
- Classic `AddRow` - priamo pridá riadok do datasetu bez smart expand logiky

### Riešenie

#### 1. **Classic DeleteRow**

```csharp
// DataGridRows.cs (Public API)
public interface IDataGridRows
{
    // ... existujúce Smart metódy ...

    /// <summary>
    /// Classic delete: Directly removes row by index WITHOUT smart logic
    /// No "always keep last empty" logic, no content clearing - just direct delete
    /// </summary>
    Task<PublicResult> DeleteRowAsync(int rowIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Classic delete: Directly removes row by RowId WITHOUT smart logic
    /// </summary>
    Task<PublicResult> DeleteRowByIdAsync(string rowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Classic delete: Directly removes multiple rows by indices WITHOUT smart logic
    /// </summary>
    Task<PublicResult> DeleteRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default);

    /// <summary>
    /// Classic delete: Directly removes multiple rows by RowIds WITHOUT smart logic
    /// </summary>
    Task<PublicResult> DeleteRowsByIdAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default);
}
```

Implementácia:

```csharp
// DataGridRows.cs implementation
public async Task<PublicResult> DeleteRowByIdAsync(string rowId, CancellationToken cancellationToken = default)
{
    try
    {
        _logger?.LogInformation("Classic delete: Removing row by RowId {RowId}", rowId);

        // Direct delete WITHOUT smart logic
        var removed = await _rowStore.RemoveRowByIdAsync(rowId, cancellationToken);

        if (!removed)
            return PublicResult.Failure($"Row with RowId {rowId} not found");

        // Trigger UI refresh
        if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
        {
            await _uiNotificationService.NotifyDataRefreshAsync(1, "ClassicDelete");
        }

        _logger?.LogInformation("Classic delete completed: Row {RowId} removed", rowId);
        return PublicResult.Success();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Classic delete failed for RowId {RowId}", rowId);
        return PublicResult.Failure($"Classic delete failed: {ex.Message}");
    }
}
```

#### 2. **Classic AddRow**

```csharp
// DataGridRows.cs (Public API)
public interface IDataGridRows
{
    // ... existujúce Smart metódy ...

    /// <summary>
    /// Classic add: Directly appends row to dataset WITHOUT smart logic
    /// No "remove last empty" logic - just direct append
    /// </summary>
    Task<PublicResult<int>> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Classic add: Directly appends multiple rows to dataset WITHOUT smart logic
    /// </summary>
    Task<PublicResult<int>> AddRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Classic insert: Directly inserts row at specific index WITHOUT smart logic
    /// </summary>
    Task<PublicResult> InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);
}
```

Implementácia:

```csharp
// DataGridRows.cs implementation
public async Task<PublicResult<int>> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
{
    try
    {
        _logger?.LogInformation("Classic add: Appending row to dataset");

        // Direct append WITHOUT smart logic (no removal of last empty row)
        var newRowIndex = await _rowStore.AddRowAsync(rowData, cancellationToken);

        // Trigger UI refresh
        if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
        {
            await _uiNotificationService.NotifyDataRefreshAsync(1, "ClassicAdd");
        }

        _logger?.LogInformation("Classic add completed: New row added at index {RowIndex}", newRowIndex);
        return PublicResult<int>.Success(newRowIndex);
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Classic add failed");
        return PublicResult<int>.Failure($"Classic add failed: {ex.Message}");
    }
}
```

---

## 📋 IMPLEMENTAČNÝ PLÁN

### Fáza 1: FILTER IMPLEMENTATION (Priorita: KRITICKÁ)

**Časový odhad:** 3-4 dni

1. ✅ **IRowStore rozšírenie**
   - Pridať metódy: `SetFilterCriteria`, `ClearFilterCriteria`, `MapFilteredIndexToOriginalIndex`
   - Implementovať filtered view index v InMemoryRowStore
   - Unit testy pre filtered view

2. ✅ **FilterService Update**
   - Upraviť `ApplyFilterAsync` aby volal `_rowStore.SetFilterCriteria()`
   - Pridať UI refresh event trigger
   - Upraviť `ClearFiltersAsync` podobne

3. ✅ **Editing API Update**
   - Upraviť `UpdateCellAsync`, `UpdateRowAsync` aby mapovali filtered → original index
   - Zabezpečiť že edity vo filtered view sa premietajú do real dataset

4. ✅ **Public API Wrapper**
   - Vytvoriť `IDataGridFiltering` interface
   - Implementovať `DataGridFiltering` wrapper class
   - Pridať do Facade API

5. ✅ **UI Integration**
   - Pripojiť filter UI controls k Facade API metódam
   - Implementovať event handlery v AdvancedDataGridControl

6. ✅ **Testing**
   - Unit testy pre filtered view logic
   - Integration testy pre all 3 modes
   - Performance test s 10M rows + filter

---

### Fáza 2: SEARCH IMPLEMENTATION (Priorita: VYSOKÁ)

**Časový odhad:** 2-3 dni

1. ✅ **SearchResult Rozšírenie**
   - Pridať `RowId` property do SearchResult
   - Upraviť SearchService aby extrahoval RowId z dát

2. ✅ **Search Navigation State**
   - Vytvoriť `SearchNavigationState` class
   - Implementovať `NavigateNext`, `NavigatePrevious` s cirkulárnou logikou

3. ✅ **Highlighting Implementation**
   - Pridať properties do CellViewModel: `IsSearchHighlighted`, `SearchHighlightText`
   - Implementovať `HighlightSearchMatchesAsync` v SearchService
   - Upraviť UI rendering aby zvýrazňoval highlighted cells

4. ✅ **Navigation Implementation**
   - Implementovať `GoToNextMatchAsync`, `GoToPreviousMatchAsync`
   - Scroll to match logic
   - Update current match highlighting

5. ✅ **UI Controls - Next/Previous Buttons**
   - Upraviť SearchPanelView
   - Pridať Next/Previous buttons
   - Pridať match count display (e.g., "3 / 15")

6. ✅ **Public API Wrapper**
   - Vytvoriť `IDataGridSearch` interface
   - Implementovať `DataGridSearch` wrapper class
   - Pridať do Facade API

7. ✅ **Testing**
   - Unit testy pre search navigation
   - Integration testy pre highlighting
   - Performance test s 10M rows + regex search

---

### Fáza 3: SORT IMPLEMENTATION (Priorita: VYSOKÁ)

**Časový odhad:** 1-2 dni

1. ✅ **SortService Update**
   - Pridať UI refresh event trigger do `SortByColumnAsync`
   - Pridať `onlyFiltered` parameter pre sort filtered view
   - Implementovať `UpdateFilteredViewOrder` v IRowStore

2. ✅ **UI Integration - Column Header Click**
   - Implementovať click handler v HeadersRowView
   - Pripojiť event k AdvancedDataGridControl
   - Volať Facade API metódu

3. ✅ **Public API Wrapper**
   - Vytvoriť `IDataGridSorting` interface
   - Implementovať `DataGridSorting` wrapper class
   - Pridať do Facade API

4. ✅ **Visual Indicators**
   - Pridať sort direction indicators (▲▼) do column headers
   - Update indicators po sort

5. ✅ **Testing**
   - Unit testy pre sort + filter interaction
   - Integration testy pre all 3 modes
   - Performance test s 10M rows multi-column sort

---

### Fáza 4: VIRTUALIZATION & PAGINATION (Priorita: KRITICKÁ)

**Časový odhad:** 4-5 dní

1. ✅ **Pagination State Management**
   - Rozšíriť DataGridViewModel o pagination properties
   - Implementovať `GoToNextPage`, `GoToPreviousPage`, `GoToPage`
   - Event `PageChanged`

2. ✅ **IRowStore Pagination Support**
   - Pridať metódy: `GetPagedRowsAsync`, `GetTotalRowCountForPaginationAsync`
   - Implementovať v InMemoryRowStore

3. ✅ **LoadCurrentPageAsync**
   - Upraviť DataGridViewModel aby loadla len current page
   - Integrácia s filter (paginuje filtered view ak je filter aktívny)

4. ✅ **Pagination UI Controls**
   - Vytvoriť `PaginationPanelView`
   - Implementovať Previous/Next buttons
   - Implementovať smart page numbers rendering (1, 2, 3, ..., current, ..., last)
   - Page info display

5. ✅ **AdvancedDataGridControl Update**
   - Pridať pagination panel nad headers
   - Subscribe na `PageChanged` event
   - Trigger `LoadCurrentPageAsync` na page change

6. ✅ **ItemsRepeater Virtualization**
   - Replace Grid s ItemsRepeater v DataGridCellsView
   - Implementovať scroll virtualization
   - Row recycling

7. ✅ **Testing**
   - Unit testy pre pagination logic
   - Integration testy s filter + sort + pagination
   - **CRITICAL:** Performance test s 10M rows → verify UI renderuje len 1000 rows

---

### Fáza 5: CLASSIC METÓDY (Priorita: STREDNÁ)

**Časový odhad:** 1 deň

1. ✅ **Classic Delete Metódy**
   - Implementovať `DeleteRowAsync`, `DeleteRowByIdAsync`, `DeleteRowsAsync`, `DeleteRowsByIdAsync`
   - Bez smart logic - priame volanie IRowStore.RemoveRowAsync

2. ✅ **Classic Add Metódy**
   - Implementovať `AddRowAsync`, `AddRowsAsync`, `InsertRowAsync`
   - Bez smart logic - priame volanie IRowStore.AppendRowsAsync

3. ✅ **Public API Update**
   - Pridať classic metódy do `IDataGridRows`
   - Documentation comments pre rozlíšenie Smart vs Classic

4. ✅ **Testing**
   - Unit testy pre classic metódy
   - Verify že classic metódy NEAKTIVUJÚ smart logic

---

### Fáza 6: THREE OPERATION MODES VERIFICATION (Priorita: KRITICKÁ)

**Časový odhad:** 2 dni

1. ✅ **Interactive Mode Testing**
   - Verify Sort/Filter/Search automaticky updatujú UI
   - Verify Search Next/Previous buttons fungujú
   - Verify Pagination funguje
   - Verify Filter UI controls fungujú

2. ✅ **Headless + Manual UI Update Mode Testing**
   - Verify Sort/Filter/Search NEAKTIVUJÚ automatický UI update
   - Verify `RefreshUIAsync()` manuálne updatuje UI
   - Verify Search Next/Previous buttons fungujú (po manual refresh)
   - Verify Pagination funguje

3. ✅ **Pure Headless Mode Testing**
   - Verify žiadne UI rendering
   - Verify Sort/Filter/Search vracajú data results
   - Verify Entry point metódy fungujú
   - Verify Search vracia full list matches (bez UI navigation)

4. ✅ **Mode Switching Tests**
   - Verify že prepnutie módu počas runtime funguje správne

---

### Fáza 7: RESOURCE MANAGEMENT & OPTIMIZATION (Priorita: VYSOKÁ)

**Časový odhad:** 2-3 dni

1. ✅ **Memory Optimization**
   - Profiling s 10M rows
   - Verify pagination limituje UI memory footprint
   - Verify filtered view používa indices (nie duplicate data)

2. ✅ **CPU Optimization**
   - Verify Sort používa parallel processing pre >1000 rows
   - Verify Filter používa batch processing
   - Verify Search používa parallel processing

3. ✅ **Threading & Concurrency**
   - Verify všetky async operations sú thread-safe
   - Verify UI updates sú dispatched na UI thread
   - Verify ConcurrentDictionary usage v IRowStore

4. ✅ **Performance Benchmarks**
   - Sort 10M rows: target <5s
   - Filter 10M rows: target <3s
   - Search 10M rows: target <2s (simple), <10s (regex)
   - Pagination page switch: target <100ms
   - UI refresh after filter: target <500ms

---

## 🐛 ĎALŠIE CHYBY A PROBLÉMY NÁJDENÉ PRI ANALÝZE

### 1. **InMemoryRowStore.GetAllRowsAsync už má parameter `onlyFiltered`**

**Súbor:** `InMemoryRowStore.cs`

**Popis:**
IRowStore interface už má parameter `onlyFiltered` v metóde `StreamRowsAsync` (line 23-27) a tiež existuje `GetFilteredRowCountAsync` (line 49). To naznačuje, že filtered support už čiastočne existuje, ale NIE JE dokončený.

**Problém:**
- `StreamRowsAsync(onlyFiltered: true)` je implementované ale `onlyFiltered` parameter v `GetAllRowsAsync` chýba
- Nekonzistentné API

**Riešenie:**
Rozšíriť všetky relevantné metódy o `onlyFiltered` parameter a implementovať logiku.

---

### 2. **ValidationDeletionService.cs je ďalší nový súbor (unstaged)**

**Súbor:** `Features/Validation/Services/ValidationDeletionService.cs`

**Popis:**
Git status ukazuje nový súbor `ValidationDeletionService.cs` ktorý nie je staged. To naznačuje neúplnú implementáciu validation deletion funkcionality.

**Odporúčanie:**
Overiť či je táto funkcionalita dokončená a či je správne integrovaná.

---

### 3. **TODO Komentáre v AdvancedDataGridControl.cs (multiple locations)**

**Súbor:** `AdvancedDataGridControl.cs`

**Line 218:**
```csharp
private void OnSearchRequested(object? sender, EventArgs e)
{
    _logger?.LogInformation("Search requested with text: {SearchText}", ViewModel.SearchPanel.SearchText);
    // TODO: Implement search via Facade API
}
```

**Line 227:**
```csharp
private void OnClearRequested(object? sender, EventArgs e)
{
    _logger?.LogInformation("Clear search requested");
    // TODO: Implement clear search via Facade API
}
```

**Line 245:**
```csharp
private void OnApplyFiltersRequested(object? sender, EventArgs e)
{
    _logger?.LogInformation("Apply filters requested");
    // TODO: Apply filters via Facade API
}
```

**Line 254:**
```csharp
private void OnClearFiltersRequested(object? sender, EventArgs e)
{
    _logger?.LogInformation("Clear filters requested");
    // TODO: Clear filters via Facade API
}
```

**Problém:**
Všetky tieto UI event handlers sú TODO placeholders. UI controls nefungujú.

**Riešenie:**
Implementovať event handlers aby volali Facade API metódy.

---

### 4. **Facade Reference nie je dostupná v AdvancedDataGridControl**

**Súbor:** `AdvancedDataGridControl.cs`

**Problém:**
AdvancedDataGridControl potrebuje volať Facade API metódy (Sort, Filter, Search) ale nemá referenciu na Facade.

**Súčasné riešenie:**
Komentár na line 1062: `// TODO: Get facade reference (via constructor injection or service provider)`

**Odporúčané riešenie:**
```csharp
// AdvancedDataGridControl.cs
public sealed class AdvancedDataGridControl : UserControl
{
    private readonly IAdvancedDataGridFacade _facade; // ✅ NOVÉ

    public AdvancedDataGridControl(
        DataGridViewModel viewModel,
        IAdvancedDataGridFacade facade, // ✅ NOVÉ parameter
        ILogger<AdvancedDataGridControl>? logger = null)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        // ...
    }

    private async void OnSearchRequested(object? sender, EventArgs e)
    {
        var searchText = ViewModel.SearchPanel.SearchText;
        if (string.IsNullOrWhiteSpace(searchText))
            return;

        // ✅ NOVÉ: Call facade API
        await _facade.Search.SearchAsync(searchText);
    }

    private async void OnApplyFiltersRequested(object? sender, EventArgs e)
    {
        // ✅ NOVÉ: Apply filters from FilterRow TextBoxes
        var filters = ViewModel.FilterRow.ColumnFilters
            .Where(f => !string.IsNullOrWhiteSpace(f.FilterText))
            .Select(f => new PublicFilterCriteria
            {
                ColumnName = f.ColumnName,
                Operator = PublicFilterOperator.Contains,
                Value = f.FilterText
            });

        await _facade.Filtering.ApplyMultipleFiltersAsync(filters);
    }
}
```

---

### 5. **SmartOperationService - Chýbajúca metóda `EnsureMinRowsAndLastEmptyPublicAsync`**

**Súbor:** `DataGridSmartOperations.cs` line 321

**Problém:**
DataGridSmartOperations volá metódu `_smartOperationService.EnsureMinRowsAndLastEmptyPublicAsync()` ktorá pravdepodobne neexistuje (odkaz z summary notes).

**Riešenie:**
Overiť či táto metóda existuje v SmartOperationService. Ak nie, implementovať ju.

---

### 6. **InternalUIOperationHandler - Missing Logger Null Check**

**Súbor:** `InternalUIOperationHandler.cs` line 35

**Problém:**
```csharp
_logger = logger ?? throw new ArgumentNullException(nameof(logger));
```

Logger by mal byť optional (podľa konštruktora parameter je `ILogger<InternalUIOperationHandler>? logger = null`) ale kód ho vyžaduje (throw exception).

**Riešenie:**
```csharp
_logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<InternalUIOperationHandler>.Instance;
```

---

## 📊 ZÁVER A SÚHRN

### Čo je potrebné implementovať

1. **Filter** - KRITICKÉ
   - Filtered view v IRowStore
   - Filtered → Original index mapping
   - UI refresh events
   - Public API

2. **Search** - VYSOKÁ PRIORITA
   - RowId v SearchResult
   - Navigation state (Next/Previous)
   - Highlighting implementation
   - UI controls (Next/Previous buttons)
   - Public API

3. **Sort** - VYSOKÁ PRIORITA
   - UI refresh events
   - Filter support
   - Column header click integration
   - Public API

4. **Virtualization & Pagination** - KRITICKÉ
   - Pagination state management
   - IRowStore paging support
   - Pagination UI (page numbers, Next/Back)
   - ItemsRepeater virtualization
   - **Cieľ:** UI renderuje max 1000 rows naraz pre 10M+ dataset

5. **Classic Metódy** - STREDNÁ PRIORITA
   - Classic Delete (bez smart logic)
   - Classic Add (bez smart logic)

6. **Three Operation Modes** - KRITICKÉ
   - Interactive: Automatic UI update
   - Headless + Manual UI: Manual RefreshUIAsync()
   - Pure Headless: No UI, data returns only

7. **Resource Management** - VYSOKÁ PRIORITA
   - Memory optimization (pagination, indices)
   - CPU optimization (parallel processing, batch processing)
   - Performance benchmarks verification

### Časový odhad celej implementácie

**Celkový čas:** 15-20 dní (3-4 týždne)

- Fáza 1 (Filter): 3-4 dni
- Fáza 2 (Search): 2-3 dni
- Fáza 3 (Sort): 1-2 dni
- Fáza 4 (Virtualization/Pagination): 4-5 dní
- Fáza 5 (Classic metódy): 1 deň
- Fáza 6 (3 Modes Verification): 2 dni
- Fáza 7 (Resource Management): 2-3 dni

### Kľúčové riziká

1. **Performance s 10M+ rows** - Virtualization MUSÍ fungovať inak UI zamrzne
2. **Filtered view complexity** - Mapovanie filtered → original index je kritické pre správnu funkcionalitu
3. **Thread safety** - ConcurrentDictionary a async operations musia byť thread-safe
4. **UI refresh events** - Všetky operácie musia správne triggerovať UI updates v Interactive mode

### Odporúčania

1. **Implementovať vo fázach** podľa priority
2. **Dôkladné testovanie** po každej fáze
3. **Performance profiling** s realistickými datasetmi (1M, 5M, 10M rows)
4. **Code review** pred merge do master
5. **Documentation** pre Public API metódy

---

**Koniec špecifikácie**


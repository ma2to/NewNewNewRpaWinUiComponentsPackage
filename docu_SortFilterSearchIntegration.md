# SORT/FILTER/SEARCH INTEGRÁCIA SO SQLITE - Špecifikácia

## 📋 METADATA

**Dátum vytvorenia:** 19.10.2025
**Verzia:** 1.0
**Jazyk kódu:** English
**Jazyk dokumentácie:** Slovenčina
**Komponent:** AdvancedDataGrid - Sort/Filter/Search Module
**Základ:** Existujúca špecifikácia `SPECIFIKACIA_SORT_FILTER_SEARCH.md` + Hybrid SQLite Model

---

## 🎯 PREHĽAD

Tento dokument rozširuje existujúcu špecifikáciu `SPECIFIKACIA_SORT_FILTER_SEARCH.md` o integráciu so SQLite databázou v hybrid modeli.

### Kľúčové zmeny oproti pôvodnej špecifikácii

**V pôvodnej špecifikácii (`SPECIFIKACIA_SORT_FILTER_SEARCH.md`):**
- Filter/Sort/Search fungovali nad in-memory ConcurrentDictionary
- LINQ operácie pre filter matching, sort, search
- Filtered view index v InMemoryRowStore

**V novom hybrid SQLite modeli:**
- Filter/Sort/Search delegované na SQLite (WHERE, ORDER BY, FTS5)
- SQL queries namiesto LINQ
- Viewport (1000 rows) sa loaduje z SQLite už filtrovaný/sortovaný
- Lepšia performance pri 10M+ rows

---

## 📐 FILTER IMPLEMENTÁCIA SO SQLITE

### Pôvodný prístup (in-memory)

```csharp
// InMemoryRowStore.cs - pôvodné riešenie
public void SetFilterCriteria(IReadOnlyList<FilterCriteria> criteria)
{
    // Build filtered row IDs list - O(n) scan cez všetky riadky
    _filteredRowIds = new List<string>();

    var allRows = GetAllRows();
    for (int i = 0; i < allRows.Count; i++)
    {
        if (RowMatchesAllFilters(allRows[i], criteria))
        {
            _filteredRowIds.Add(allRows[i]["__rowId"]);
        }
    }
}

private bool RowMatchesAllFilters(IReadOnlyDictionary<string, object?> row, IReadOnlyList<FilterCriteria> criteria)
{
    foreach (var filter in criteria)
    {
        var cellValue = row.TryGetValue(filter.ColumnName, out var val) ? val : null;

        switch (filter.Operator)
        {
            case FilterOperator.Equals:
                if (!ValuesAreEqual(cellValue, filter.Value)) return false;
                break;
            // ... 15+ operators ...
        }
    }
    return true;
}
```

**Problémy:**
- O(n) scan cez všetky riadky pri každom filteri
- Pri 10M rows to je 10M porovnaní v C# kóde
- Neskaluje dobre

### Nový prístup (SQLite WHERE clause)

```csharp
// HybridRowStore.cs - nové riešenie
public void SetFilterCriteria(IReadOnlyList<FilterCriteria> criteria)
{
    if (criteria == null || criteria.Count == 0)
    {
        _activeFilterSql = null;
        _logger.LogInformation("Filter criteria cleared");
        return;
    }

    // Build SQL WHERE clause from filter criteria
    var whereConditions = new List<string>();

    foreach (var filter in criteria)
    {
        var sqlCondition = BuildSqlFilterCondition(filter);
        if (!string.IsNullOrEmpty(sqlCondition))
        {
            whereConditions.Add(sqlCondition);
        }
    }

    // Combine with AND logic
    _activeFilterSql = whereConditions.Count > 0
        ? string.Join(" AND ", whereConditions)
        : null;

    _logger.LogInformation("Filter SQL built: {Filters} filters → WHERE {Sql}",
        criteria.Count, _activeFilterSql);
}

private string BuildSqlFilterCondition(FilterCriteria filter)
{
    // Extract column value from JSON data field using json_extract
    var columnPath = $"$.{filter.ColumnName}";

    return filter.Operator switch
    {
        FilterOperator.Equals =>
            $"json_extract(data, '{columnPath}') = {FormatSqlValue(filter.Value)}",

        FilterOperator.NotEquals =>
            $"json_extract(data, '{columnPath}') != {FormatSqlValue(filter.Value)}",

        FilterOperator.Contains =>
            $"json_extract(data, '{columnPath}') LIKE '%' || {FormatSqlValue(filter.Value)} || '%'",

        FilterOperator.StartsWith =>
            $"json_extract(data, '{columnPath}') LIKE {FormatSqlValue(filter.Value)} || '%'",

        FilterOperator.EndsWith =>
            $"json_extract(data, '{columnPath}') LIKE '%' || {FormatSqlValue(filter.Value)}",

        FilterOperator.GreaterThan =>
            $"CAST(json_extract(data, '{columnPath}') AS REAL) > {FormatSqlValue(filter.Value)}",

        FilterOperator.GreaterThanOrEqual =>
            $"CAST(json_extract(data, '{columnPath}') AS REAL) >= {FormatSqlValue(filter.Value)}",

        FilterOperator.LessThan =>
            $"CAST(json_extract(data, '{columnPath}') AS REAL) < {FormatSqlValue(filter.Value)}",

        FilterOperator.LessThanOrEqual =>
            $"CAST(json_extract(data, '{columnPath}') AS REAL) <= {FormatSqlValue(filter.Value)}",

        FilterOperator.IsNull =>
            $"json_extract(data, '{columnPath}') IS NULL",

        FilterOperator.IsNotNull =>
            $"json_extract(data, '{columnPath}') IS NOT NULL",

        FilterOperator.IsEmpty =>
            $"(json_extract(data, '{columnPath}') IS NULL OR TRIM(json_extract(data, '{columnPath}')) = '')",

        FilterOperator.IsNotEmpty =>
            $"(json_extract(data, '{columnPath}') IS NOT NULL AND TRIM(json_extract(data, '{columnPath}')) != '')",

        _ => throw new NotSupportedException($"Filter operator {filter.Operator} not supported")
    };
}

private string FormatSqlValue(object? value)
{
    if (value == null) return "NULL";
    if (value is string str) return $"'{str.Replace("'", "''")}'";  // Escape single quotes
    if (value is bool b) return b ? "1" : "0";
    return value.ToString() ?? "NULL";
}
```

**Výhody:**
- Filter sa aplikuje na SQL úrovni (SQLite engine)
- Indexed columns môžu využiť B-tree indexy (rýchlejšie)
- Pri 10M rows + index: < 1 sekunda namiesto 10+ sekúnd

### GetPagedRowsAsync s aktívnym filtrom

```csharp
public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetPagedRowsAsync(
    int pageNumber,
    int pageSize,
    bool onlyFiltered = false,
    CancellationToken cancellationToken = default)
{
    var whereClause = "__isDeleted = 0";

    if (onlyFiltered && !string.IsNullOrEmpty(_activeFilterSql))
    {
        whereClause += $" AND ({_activeFilterSql})";
    }

    var orderByClause = !string.IsNullOrEmpty(_activeSortSql)
        ? _activeSortSql
        : "__createdAt ASC";

    var offset = (pageNumber - 1) * pageSize;

    var sql = $@"
        SELECT __rowId, data
        FROM grid_rows
        WHERE {whereClause}
        ORDER BY {orderByClause}
        LIMIT {pageSize} OFFSET {offset}";

    using var cmd = _sqliteConnection!.CreateCommand();
    cmd.CommandText = sql;

    var results = new List<IReadOnlyDictionary<string, object?>>();

    using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
        var rowId = reader.GetString(0);
        var dataJson = reader.GetString(1);

        var rowData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(dataJson);
        if (rowData != null)
        {
            rowData["__rowId"] = rowId;
            results.Add(rowData);
        }
    }

    _logger.LogInformation("GetPagedRowsAsync: page={Page}, pageSize={PageSize}, filtered={OnlyFiltered}, results={Count}",
        pageNumber, pageSize, onlyFiltered, results.Count);

    return results;
}
```

**Performance target:**
- 10M rows, complex filter (3 conditions): < 2 sekundy na prvý page load
- Subsequent pages (same filter): < 200ms (SQLite query cache)

---

## 📐 SORT IMPLEMENTÁCIA SO SQLITE

### Pôvodný prístup (LINQ)

```csharp
// SortService.cs - pôvodné riešenie
public async Task<bool> SortByColumnAsync(string columnName, SortDirection direction, ...)
{
    var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);

    var sortedRows = direction == SortDirection.Ascending
        ? allRows.OrderBy(r => GetSortValue(r, columnName)).ToList()
        : allRows.OrderByDescending(r => GetSortValue(r, columnName)).ToList();

    await _rowStore.ReplaceAllRowsAsync(sortedRows, cancellationToken);
    return true;
}
```

**Problémy:**
- Pri 10M rows: LINQ OrderBy loaduje všetky riadky do pamäte
- QuickSort v LINQ: O(n log n) ale nad objektmi v RAM
- Pri 10M rows: 10-30 sekúnd

### Nový prístup (SQLite ORDER BY)

```csharp
// HybridRowStore.cs
public void SetSortCriteria(IReadOnlyList<SortDescriptor> sortDescriptors)
{
    if (sortDescriptors == null || sortDescriptors.Count == 0)
    {
        _activeSortSql = null;
        _logger.LogInformation("Sort criteria cleared - using default __createdAt ASC");
        return;
    }

    // Build SQL ORDER BY clause
    var orderByClauses = new List<string>();

    foreach (var sort in sortDescriptors)
    {
        var columnPath = $"$.{sort.ColumnName}";
        var direction = sort.Direction == SortDirection.Ascending ? "ASC" : "DESC";

        // Type-aware sorting (try numeric first, fallback to text)
        var orderClause = $@"
            CASE
                WHEN json_type(json_extract(data, '{columnPath}')) IN ('integer', 'real')
                THEN CAST(json_extract(data, '{columnPath}') AS REAL)
                ELSE NULL
            END {direction},
            json_extract(data, '{columnPath}') {direction}";

        orderByClauses.Add(orderClause);
    }

    _activeSortSql = string.Join(", ", orderByClauses);

    _logger.LogInformation("Sort SQL built: {Sorts} sorts → ORDER BY {Sql}",
        sortDescriptors.Count, _activeSortSql);
}
```

**Výhody:**
- Sort sa deje na SQLite úrovni (B-tree index využitie)
- Viewport (1000 rows) sa loaduje už sortovaný
- Pri 10M rows: < 5 sekúnd (s indexom)

**Multi-column sort:**
```sql
-- Example: Sort by Age (numeric DESC), then Name (text ASC)
ORDER BY
    CASE WHEN json_type(json_extract(data, '$.Age')) IN ('integer', 'real')
         THEN CAST(json_extract(data, '$.Age') AS REAL)
         ELSE NULL END DESC,
    json_extract(data, '$.Age') DESC,
    CASE WHEN json_type(json_extract(data, '$.Name')) IN ('integer', 'real')
         THEN CAST(json_extract(data, '$.Name') AS REAL)
         ELSE NULL END ASC,
    json_extract(data, '$.Name') ASC
```

---

## 📐 SEARCH IMPLEMENTÁCIA SO SQLITE FTS5

### Pôvodný prístup (LINQ Contains/Regex)

```csharp
// SearchService.cs - pôvodné riešenie
public async Task<SearchResultCollection> SearchAsync(SearchCommand command, ...)
{
    var dataList = await GetSearchDataAsync(command.SearchScope, cancellationToken);

    for (var rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
    {
        var row = dataList[rowIndex];

        foreach (var columnName in searchColumns)
        {
            if (row.TryGetValue(columnName, out var value))
            {
                var text = value?.ToString() ?? string.Empty;

                // LINQ Contains (O(n*m) string scan)
                if (text.Contains(command.SearchText, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(SearchResult.Create(rowIndex, rowId, columnName, value, text));
                }
            }
        }
    }
}
```

**Problémy:**
- O(n * m * k) complexity: n rows × m columns × k characters per value
- Pri 10M rows: 20-60 sekúnd pre simple Contains
- Regex search: 2-5 minút

### Nový prístup (SQLite FTS5)

**FTS5 (Full-Text Search 5)** je built-in SQLite modul pre rýchle fulltext vyhľadávanie.

**Setup (už v schéme):**
```sql
-- FTS5 virtual table
CREATE VIRTUAL TABLE grid_rows_fts USING fts5(
    __rowId UNINDEXED,
    data,
    content='grid_rows',
    content_rowid='rowid'
);

-- Auto-sync triggers (INSERT/UPDATE/DELETE)
-- ... (už v DatabaseLifecycleManager.CreateSchemaAsync)
```

**Search query:**
```csharp
// HybridRowStore.cs
public async Task<IReadOnlyList<SearchResult>> SearchAsync(
    string searchText,
    SearchMode searchMode,
    IEnumerable<string>? searchColumns = null,
    CancellationToken cancellationToken = default)
{
    var results = new List<SearchResult>();

    // Build FTS5 query
    var ftsQuery = BuildFtsQuery(searchText, searchMode);

    var sql = @"
        SELECT r.__rowId, r.data
        FROM grid_rows_fts fts
        JOIN grid_rows r ON r.rowid = fts.rowid
        WHERE fts.data MATCH $query
        AND r.__isDeleted = 0
        ORDER BY rank
        LIMIT 1000";  // Max 1000 results

    using var cmd = _sqliteConnection!.CreateCommand();
    cmd.CommandText = sql;
    cmd.Parameters.AddWithValue("$query", ftsQuery);

    using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
    while (await reader.ReadAsync(cancellationToken))
    {
        var rowId = reader.GetString(0);
        var dataJson = reader.GetString(1);

        var rowData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(dataJson);
        if (rowData == null) continue;

        // Find matching columns
        var matchingColumns = FindMatchingColumns(rowData, searchText, searchColumns);

        foreach (var (columnName, value) in matchingColumns)
        {
            results.Add(new SearchResult
            {
                RowId = rowId,
                ColumnName = columnName,
                Value = value,
                MatchedText = searchText,
                MatchScore = 1.0,
                ShouldHighlight = true
            });
        }
    }

    _logger.LogInformation("FTS5 search '{Text}' returned {Count} results", searchText, results.Count);
    return results;
}

private string BuildFtsQuery(string searchText, SearchMode mode)
{
    return mode switch
    {
        SearchMode.Contains => $"*{EscapeFtsQuery(searchText)}*",
        SearchMode.Exact => $"\"{EscapeFtsQuery(searchText)}\"",
        SearchMode.StartsWith => $"{EscapeFtsQuery(searchText)}*",
        SearchMode.EndsWith => $"*{EscapeFtsQuery(searchText)}",
        SearchMode.Fuzzy => $"{EscapeFtsQuery(searchText)}~",  // Levenshtein distance
        _ => EscapeFtsQuery(searchText)
    };
}

private string EscapeFtsQuery(string text)
{
    // Escape FTS5 special characters: " * : ( ) [ ] { } - + AND OR NOT
    return text
        .Replace("\"", "\"\"")
        .Replace("*", "\\*")
        .Replace(":", "\\:");
}
```

**Performance:**
- FTS5 používa inverted index (word → document mapping)
- Search cez 10M rows: < 2 sekundy (simple), < 5 sekúnd (regex/fuzzy)
-Rank automaticky prioritizuje relevantné výsledky

**Highlighting:**
FTS5 poskytuje `snippet()` funkciu pre zvýraznenie matchov:

```sql
SELECT
    r.__rowId,
    snippet(grid_rows_fts, 1, '<mark>', '</mark>', '...', 50) AS highlighted_text
FROM grid_rows_fts fts
JOIN grid_rows r ON r.rowid = fts.rowid
WHERE fts.data MATCH 'search_term';
```

---

## 🔄 TRI OPERAČNÉ MÓDY - INTEGRÁCIA

### Interactive Mode

**Behavior:**
- Filter/Sort/Search aplikované cez Facade API automaticky triggerujú UI refresh
- `UiNotificationService.OnDataRefreshed` event → `InternalUIUpdateHandler` → ViewModel reload

**Flow:**
```
User clicks Filter button in UI
  ↓
FilterRowView raises ApplyFiltersRequested event
  ↓
AdvancedDataGridControl.OnApplyFiltersRequested()
  ↓
facade.Filtering.ApplyFilterAsync(columnName, operator, value)
  ↓
FilterService.ApplyFilterAsync()
  ├─ _hybridRowStore.SetFilterCriteria(criteria)  // Builds SQL WHERE
  └─ _uiNotificationService.NotifyDataRefreshWithMetadataAsync()
     ↓
InternalUIUpdateHandler.OnDataRefreshed()
  ↓
viewModel.LoadCurrentPageAsync()  // Loads viewport from SQLite with filter
  ↓
UI updates automatically (ObservableCollection change)
```

### Headless + Manual UI Update Mode

**Behavior:**
- Filter/Sort/Search aplikované cez Facade API NEtriggerujú automatický UI refresh
- Programátor musí explicitne zavolať `RefreshUIAsync()`

**Flow:**
```
await facade.Filtering.ApplyFilterAsync("Age", FilterOperator.GreaterThan, 30);
// UI sa NEUPDATUJE

await facade.RefreshUIAsync();  // Manual refresh
// Teraz sa UI aktualizuje
```

### Pure Headless Mode

**Behavior:**
- Žiadne UI, žiadne eventy
- Filter/Sort/Search vracajú dátové výsledky

**Flow:**
```
await facade.Filtering.ApplyFilterAsync("Age", FilterOperator.GreaterThan, 30);

var filteredData = await facade.Rows.GetAllRowsAsync(onlyFiltered: true);
// Returns IReadOnlyList<IReadOnlyDictionary<string, object?>>

foreach (var row in filteredData)
{
    Console.WriteLine($"Name: {row["Name"]}, Age: {row["Age"]}");
}
```

---

## 📊 PERFORMANCE BENCHMARKS

### Target performance (10M rows dataset)

| Operácia | In-Memory (pôvodné) | Hybrid SQLite (nové) | Zlepšenie |
|----------|---------------------|----------------------|-----------|
| Filter apply (3 conditions) | 15-30s | < 2s | **15x rýchlejšie** |
| Sort single column | 20-40s | < 5s | **8x rýchlejšie** |
| Sort multi-column (3 cols) | 30-60s | < 8s | **7x rýchlejšie** |
| Search (simple Contains) | 30-60s | < 2s | **30x rýchlejšie** |
| Search (regex) | 2-5 min | < 5s | **40x rýchlejšie** |
| Pagination page load (filtered+sorted) | N/A | < 200ms | **Nová feature** |

### Indexy pre performance boost

**Ak stĺpec je často filtrovaný/sortovaný, pridaj index:**
```sql
-- Create index for "Age" column
CREATE INDEX IF NOT EXISTS idx_age ON grid_rows(
    CAST(json_extract(data, '$.Age') AS INTEGER)
);

-- Create index for "Name" column
CREATE INDEX IF NOT EXISTS idx_name ON grid_rows(
    json_extract(data, '$.Name')
);
```

**Kedy vytvoriť indexy:**
- Programátor môže volať `facade.Database.CreateColumnIndexAsync("Age")`
- Automaticky pri prvom filteri/sorte na danom stĺpci (lazy index creation)

---

Koniec tretieho dokumentu. Pokračujem s ďalšími...

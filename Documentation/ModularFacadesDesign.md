# Modular Facades Design

## 🎯 Cieľ
Rozdeliť monolitický `IAdvancedDataGridFacade` do **logických modulov** pre lepšiu organizáciu a použiteľnosť.

## 📦 Moduly

### 1. **IO Module** (`RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.IO`)
**Zodpovednosť**: Import/Export dát

```csharp
public interface IDataGridIO
{
    // Import
    Task<ImportResult> ImportAsync(ImportDataCommand command, CancellationToken cancellationToken = default);

    // Export
    Task<ExportResult> ExportAsync(ExportDataCommand command, CancellationToken cancellationToken = default);
    Task<DataTable> GetCurrentDataAsDataTableAsync(CancellationToken cancellationToken = default);
}
```

### 2. **Validation Module** (`RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Validation`)
**Zodpovednosť**: Validácie

```csharp
public interface IDataGridValidation
{
    // Validation operations
    Task<PublicResult<bool>> ValidateAllAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default);
    Task<PublicValidationResultWithStatistics> ValidateAllWithStatisticsAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default);
    Task<PublicResult> AddValidationRuleAsync(IValidationRule rule);
    Task<PublicResult> RemoveValidationRulesAsync(params string[] columnNames);
    Task<PublicResult> RemoveValidationRuleAsync(string ruleName);
    Task<PublicResult> ClearAllValidationRulesAsync();

    // Validation queries
    string GetValidationAlerts(int rowIndex);
    bool HasValidationErrors(int rowIndex);

    // UI refresh
    void RefreshValidationResultsToUI();
}
```

### 3. **Operations Module** (`RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Operations`)
**Zodpovednosť**: Sort, Filter, Search, CellEdit

```csharp
// Sort
public interface IDataGridSort
{
    Task<SortDataResult> SortAsync(SortDataCommand command, CancellationToken cancellationToken = default);
    Task<SortDataResult> MultiSortAsync(MultiSortDataCommand command, CancellationToken cancellationToken = default);
    SortDataResult QuickSort(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName, PublicSortDirection direction = PublicSortDirection.Ascending);
    IReadOnlyList<string> GetSortableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data);
    Task<PublicResult> SortByColumnAsync(string columnName, PublicSortDirection direction);
    Task<PublicResult> ClearSortingAsync();
}

// Filter
public interface IDataGridFilter
{
    Task<int> ApplyFilterAsync(string columnName, PublicFilterOperator @operator, object? value);
    Task<int> ClearFiltersAsync();
}

// Search
public interface IDataGridSearch
{
    Task<SearchDataResult> SearchAsync(SearchDataCommand command, CancellationToken cancellationToken = default);
    Task<SearchDataResult> AdvancedSearchAsync(AdvancedSearchDataCommand command, CancellationToken cancellationToken = default);
    Task<SearchDataResult> SmartSearchAsync(SmartSearchDataCommand command, CancellationToken cancellationToken = default);
    SearchDataResult QuickSearch(IEnumerable<IReadOnlyDictionary<string, object?>> data, string searchText, bool caseSensitive = false);
    Task<PublicResult> ValidateSearchCriteriaAsync(PublicAdvancedSearchCriteria searchCriteria);
    IReadOnlyList<string> GetSearchableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data);
}

// Cell Edit
public interface IDataGridCellEdit
{
    Task<CellEditResult> BeginEditAsync(BeginEditDataCommand command, CancellationToken cancellationToken = default);
    Task<CellEditResult> UpdateCellAsync(UpdateCellDataCommand command, CancellationToken cancellationToken = default);
    Task<CellEditResult> CommitEditAsync(CancellationToken cancellationToken = default);
    Task<CellEditResult> CancelEditAsync(CancellationToken cancellationToken = default);
}
```

### 4. **Data Module** (`RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Data`)
**Zodpovednosť**: Row/Column operations, Data access

```csharp
// Rows
public interface IDataGridRows
{
    Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData);
    Task<bool> RemoveRowAsync(int rowIndex);
    Task<bool> UpdateRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData);
    IReadOnlyDictionary<string, object?>? GetRow(int rowIndex);
    int GetRowCount();
    int GetVisibleRowCount();
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GetCurrentData();
}

// Columns
public interface IDataGridColumns
{
    IReadOnlyList<PublicColumnDefinition> GetColumnDefinitions();
    bool AddColumn(PublicColumnDefinition columnDefinition);
    bool RemoveColumn(string columnName);
    bool UpdateColumn(PublicColumnDefinition columnDefinition);
    double ResizeColumn(int columnIndex, double newWidth);
    void StartColumnResize(int columnIndex, double clientX);
    void UpdateColumnResize(double clientX);
    void EndColumnResize();
    double GetColumnWidth(int columnIndex);
    bool IsResizing();
}
```

### 5. **UI Module** (`RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UI`)
**Zodpovednosť**: Selection, CopyPaste, Colors, Themes

```csharp
// Selection
public interface IDataGridSelection
{
    void SelectCell(int row, int col);
    void ToggleCellSelection(int row, int col);
    void ExtendSelectionTo(int row, int col);
    void StartDragSelect(int row, int col);
    void DragSelectTo(int row, int col);
    void EndDragSelect(int row, int col);
}

// CopyPaste
public interface IDataGridClipboard
{
    void SetClipboard(object payload);
    object? GetClipboard();
    Task<CopyPasteResult> CopyAsync(CopyDataCommand command, CancellationToken cancellationToken = default);
    Task<CopyPasteResult> PasteAsync(PasteDataCommand command, CancellationToken cancellationToken = default);
}

// Colors & Themes
public interface IDataGridTheme
{
    Task<ColorDataResult> ApplyColorAsync(ApplyColorDataCommand command, CancellationToken cancellationToken = default);
    Task<ColorDataResult> ApplyConditionalFormattingAsync(ApplyConditionalFormattingDataCommand command, CancellationToken cancellationToken = default);
    Task<ColorDataResult> ClearColorAsync(ClearColorDataCommand command, CancellationToken cancellationToken = default);
    Task<PublicResult> ApplyThemeAsync(PublicGridTheme theme);
    PublicGridTheme GetCurrentTheme();
    Task<PublicResult> ResetThemeToDefaultAsync();
    Task<PublicResult> UpdateCellColorsAsync(PublicCellColors cellColors);
    Task<PublicResult> UpdateRowColorsAsync(PublicRowColors rowColors);
    Task<PublicResult> UpdateValidationColorsAsync(PublicValidationColors validationColors);
    PublicGridTheme CreateDarkTheme();
    PublicGridTheme CreateLightTheme();
    PublicGridTheme CreateHighContrastTheme();
}
```

### 6. **Features Module** (`RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features`)
**Zodpovednosť**: AutoRowHeight, Keyboard Shortcuts, SmartOperations

```csharp
// Auto Row Height
public interface IDataGridAutoRowHeight
{
    Task<PublicAutoRowHeightResult> EnableAutoRowHeightAsync(PublicAutoRowHeightConfiguration configuration, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicRowHeightCalculationResult>> CalculateOptimalRowHeightsAsync(IProgress<PublicBatchCalculationProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<PublicRowHeightCalculationResult> CalculateRowHeightAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, PublicRowHeightCalculationOptions? options = null, CancellationToken cancellationToken = default);
    Task<PublicTextMeasurementResult> MeasureTextAsync(string text, string fontFamily, double fontSize, double maxWidth, bool textWrapping = true, CancellationToken cancellationToken = default);
    Task<PublicAutoRowHeightResult> ApplyAutoRowHeightConfigurationAsync(PublicAutoRowHeightConfiguration configuration, CancellationToken cancellationToken = default);
    Task<bool> InvalidateHeightCacheAsync(CancellationToken cancellationToken = default);
    PublicAutoRowHeightStatistics GetAutoRowHeightStatistics();
    PublicCacheStatistics GetCacheStatistics();
}

// Keyboard Shortcuts
public interface IDataGridKeyboardShortcuts
{
    Task<ShortcutDataResult> ExecuteShortcutAsync(ExecuteShortcutDataCommand command, CancellationToken cancellationToken = default);
    Task<bool> RegisterShortcutAsync(PublicShortcutDefinition shortcut);
    IReadOnlyList<PublicShortcutDefinition> GetRegisteredShortcuts();
}

// Smart Operations
public interface IDataGridSmartOperations
{
    Task<SmartOperationDataResult> SmartAddRowsAsync(SmartAddRowsDataCommand command, CancellationToken cancellationToken = default);
    Task<SmartOperationDataResult> SmartDeleteRowsAsync(SmartDeleteRowsDataCommand command, CancellationToken cancellationToken = default);
    Task<SmartOperationDataResult> AutoExpandEmptyRowAsync(AutoExpandEmptyRowDataCommand command, CancellationToken cancellationToken = default);
    Task<PublicResult> ValidateRowManagementConfigurationAsync(PublicRowManagementConfiguration configuration);
    PublicRowManagementStatistics GetRowManagementStatistics();
}
```

### 7. **Performance Module** (`RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Performance`)
**Zodpovednosť**: Performance monitoring, Batch operations

```csharp
// Performance Monitoring
public interface IDataGridPerformance
{
    Task<PublicResult> StartPerformanceMonitoringAsync(StartPerformanceMonitoringCommand command, CancellationToken cancellationToken = default);
    Task<PublicResult> StopPerformanceMonitoringAsync(CancellationToken cancellationToken = default);
    Task<PerformanceSnapshotData> GetPerformanceSnapshotAsync(CancellationToken cancellationToken = default);
    Task<PerformanceReportData> GetPerformanceReportAsync(CancellationToken cancellationToken = default);
    PerformanceStatisticsData GetPerformanceStatistics();
}

// Batch Operations
public interface IDataGridBatchOperations
{
    Task<BatchOperationResult> BatchUpdateCellsAsync(BatchUpdateCellsDataCommand command, CancellationToken cancellationToken = default);
    Task<BatchOperationResult> BatchRowOperationsAsync(BatchRowOperationsDataCommand command, CancellationToken cancellationToken = default);
    Task<BatchOperationResult> BatchColumnOperationsAsync(BatchColumnOperationsDataCommand command, CancellationToken cancellationToken = default);
}
```

### 8. **MVVM Module** (`RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.MVVM`)
**Zodpovednosť**: ViewModels, UI Notifications, Presets

```csharp
public interface IDataGridMVVM
{
    // Transformations
    PublicRowViewModel AdaptToRowViewModel(IReadOnlyDictionary<string, object?> rowData, int rowIndex);
    IReadOnlyList<PublicRowViewModel> AdaptToRowViewModels(IEnumerable<IReadOnlyDictionary<string, object?>> rows, int startIndex = 0);
    PublicColumnViewModel AdaptToColumnViewModel(PublicColumnDefinition columnDefinition);
    IReadOnlyList<PublicValidationErrorViewModel> AdaptValidationErrors(IReadOnlyList<PublicValidationErrorViewModel> errors);

    // Subscriptions
    IDisposable SubscribeToValidationRefresh(Action<PublicValidationRefreshEventArgs> handler);
    IDisposable SubscribeToDataRefresh(Action<PublicDataRefreshEventArgs> handler);
    IDisposable SubscribeToOperationProgress(Action<PublicOperationProgressEventArgs> handler);

    // Business Presets
    PublicSortConfiguration CreateEmployeeHierarchySortPreset(string departmentColumn = "Department", string positionColumn = "Position", string salaryColumn = "Salary");
    PublicSortConfiguration CreateCustomerPrioritySortPreset(string tierColumn = "CustomerTier", string valueColumn = "TotalValue", string joinDateColumn = "JoinDate");
    PublicAutoRowHeightConfiguration GetResponsiveHeightPreset();
    PublicAutoRowHeightConfiguration GetCompactHeightPreset();
    PublicAutoRowHeightConfiguration GetPerformanceHeightPreset();

    // UI Operations
    Task RefreshUIAsync(string operationType = "ManualRefresh", int affectedRows = 0);
}
```

## 🏗️ Implementácia

### Krok 1: Vytvoriť interface pre každý modul
### Krok 2: Implementovať každý modul ako samostatnú triedu
### Krok 3: Vytvoriť Factory pre registráciu modulov
### Krok 4: Update dokumentácie a príkladov

## 📝 Príklad použitia

```csharp
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.IO;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Validation;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Operations;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Data;

// Vytvorenie modulov
var io = DataGridModuleFactory.CreateIO(options);
var validation = DataGridModuleFactory.CreateValidation(options);
var sort = DataGridModuleFactory.CreateSort(options);
var rows = DataGridModuleFactory.CreateRows(options);

// Použitie
await io.ImportAsync(importCommand);
await validation.ValidateAllAsync();
await sort.SortByColumnAsync("Name", PublicSortDirection.Ascending);
var rowData = rows.GetRow(0);
```

## ⚠️ Backward Compatibility

Pre zachovanie backward compatibility môžeme:
1. Ponechať `IAdvancedDataGridFacade` ako **deprecated**
2. Implementovať facade ako **wrapper** okolo modulov
3. Postupná migrácia v budúcich verziách

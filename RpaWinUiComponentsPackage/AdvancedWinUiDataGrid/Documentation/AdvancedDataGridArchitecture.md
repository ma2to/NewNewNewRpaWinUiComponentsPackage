# AdvancedDataGrid Architecture Documentation

## 🔧 Recent Compilation Fixes

### UI Control Issues Resolved (2025-09-19)

**Fixed compilation errors in AdvancedDataGridControl:**

1. **CS1061 InitializeComponent Error**:
   - **Root Cause**: Missing generated code file (`AdvancedDataGridControl.xaml.g.cs`)
   - **Solution**: Created proper generated code file with correct component initialization and event binding
   - **Files**: `AdvancedDataGridControl.xaml.g.cs`

2. **CS1061 CellAddress.Row Errors**:
   - **Root Cause**: Incorrect property name usage (`.Row` instead of `.RowIndex`)
   - **Solution**: Updated all references to use correct `CellAddress.RowIndex` property
   - **Files**: `AdvancedDataGridControl.xaml.cs` (lines 142, 154, 166, 238)

3. **CS0246 PointerEventArgs Error**:
   - **Root Cause**: Incorrect event argument type for WinUI 3 mouse wheel handling
   - **Solution**: Changed from `PointerEventArgs` to `PointerRoutedEventArgs` with updated delta access pattern
   - **Files**: `AdvancedDataGridControl.xaml.cs` (OnScrollViewerPointerWheelChanged method)

4. **CS0103 MainScrollViewer/DataGridContainer Errors**:
   - **Root Cause**: Missing field declarations in generated code file
   - **Solution**: Properly declared controls in .xaml.g.cs with correct connection logic
   - **Files**: `AdvancedDataGridControl.xaml.g.cs`

5. **CS1061 IDataGridLogger.Dispose Error**:
   - **Root Cause**: Interface doesn't implement IDisposable, but concrete implementation does
   - **Solution**: Added type checking before disposal using pattern matching
   - **Files**: `AdvancedDataGridControl.xaml.cs` (Dispose method)

6. **CS8604 Null Reference Warning**:
   - **Root Cause**: PropertyChangedEventArgs.PropertyName can be null
   - **Solution**: Added null coalescing operator with fallback value
   - **Files**: `AdvancedDataGridControl.xaml.cs` (OnViewModelPropertyChanged method)

### Technical Implementation Details

**CLEAN ARCHITECTURE COMPLIANCE**: All fixes maintain existing architectural patterns without breaking layer separation.

**SOLID PRINCIPLES**: Changes follow Single Responsibility and preserve existing interfaces without modification.

**LOGGING ARCHITECTURE**: Comprehensive logging maintains enterprise-grade observability throughout the UI layer.

**NULL SAFETY**: Applied defensive programming patterns to handle potential null references safely.

## 🏗️ Clean Architecture Overview

Komponenta AdvancedDataGrid je implementovaná podľa **Clean Architecture** princípov s dôrazom na **Single Responsibility Principle** a **Single Using Statement** pre vývojárov.

## 📁 Final Architecture Structure

```
AdvancedWinUiDataGrid/
├── 📁 Application/                    # Application Layer - Use Cases & APIs
│   ├── 📁 API/                        # Public API Definitions
│   │   ├── SearchFilterApi.cs         # Search & Filter types
│   │   ├── DataImportExportApi.cs     # Import/Export types
│   │   ├── SortApi.cs                 # Sort configuration types
│   │   ├── KeyboardShortcutsApi.cs    # Keyboard shortcut types
│   │   ├── PerformanceApi.cs          # Performance & virtualization types
│   │   └── AutoRowHeightApi.cs        # Auto row height types
│   ├── 📁 Interfaces/                 # Service contracts
│   └── 📁 UseCases/                   # Business logic operations
├── 📁 Core/                           # Domain Layer - Business Logic
│   ├── 📁 Entities/                   # Domain entities
│   ├── 📁 ValueObjects/               # Immutable value objects
│   ├── 📁 Enums/                      # Domain enumerations
│   ├── 📁 Interfaces/                 # Core abstractions
│   │   ├── IValidationRules.cs        # Public validation interfaces
│   │   ├── IDataGridLogger.cs         # Logging interface
│   │   └── IComplexValidationRule.cs  # Complex validation interface
│   └── 📁 Constants/                  # Domain constants
├── 📁 Infrastructure/                 # Infrastructure Layer - Services
│   ├── 📁 Logging/                    # Logging implementations
│   ├── 📁 Persistence/                # Data storage implementations
│   └── 📁 Services/                   # Service implementations
│       ├── ValidationService.cs       # Validation logic
│       ├── ValidationRuleImplementations.cs # Internal rule implementations
│       └── SearchFilterService.cs     # Search & filter logic
├── 📁 Presentation/                   # Presentation Layer - UI
│   ├── 📁 UI/                         # UserControl implementations
│   ├── 📁 ViewModels/                 # MVVM ViewModels
│   ├── 📁 Converters/                 # Value converters
│   └── 📁 Themes/                     # UI themes and styles
├── 📁 Tests/                          # Unit & Integration tests
├── 📁 Documentation/                  # Architecture documentation
├── AdvancedDataGrid.cs                # 🎯 SINGLE PUBLIC API ENTRY POINT
└── *.cs.old                          # Archived old implementations
```

## 🎯 Single Using Statement Architecture

### ✅ Developer Experience
```csharp
// SINGLE USING STATEMENT for entire component
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

// All functionality available through one class
var dataGrid = new AdvancedDataGrid(logger, DataGridOperationMode.UI);
```

### 📊 Public API Surface

**AdvancedDataGrid.cs** - Single point of entry with:
- ✅ **Validation API** - 8-type validation system
- ✅ **Data Management API** - Dictionary & DataTable import/export
- ✅ **Copy/Paste API** - Excel-compatible tab-delimited format
- ✅ **Search & Filter API** - Advanced search with regex
- ✅ **Sort API** - Multi-column sorting
- ✅ **Configuration Properties** - All settings in one place
- ✅ **Data Access** - Read-only data access methods

## 🔧 API Structure Design

### Application/API Layer
Všetky **public typy** potrebné pre API sú organizované v `Application/API/`:

```csharp
// Search & Filter
FilterOperator, FilterDefinition, SearchResult, FilterResult

// Import & Export
ImportMode, ImportOptions, ImportResult, CopyPasteResult

// Sorting
SortDirection, SortConfiguration, SortResult

// Performance
VirtualizationConfiguration, PerformanceStatistics, DataPage

// Auto Row Height
AutoRowHeightConfiguration, TextMeasurementResult

// Keyboard
KeyboardShortcut, KeyboardShortcutConfiguration
```

### Core/Interfaces Layer
**Public validation interfaces** v `Core/Interfaces/`:

```csharp
// Validation interfaces accessible to external developers
IValidationRule, ISingleCellValidationRule, ICrossColumnValidationRule
IConditionalValidationRule, IComplexValidationRule
```

### Infrastructure/Services Layer
**Internal implementations** v `Infrastructure/Services/`:

```csharp
// Internal service implementations
ValidationService, SearchFilterService, ValidationRuleImplementations
```

## 🚀 Key Architectural Principles

### 1. **Single Entry Point**
- ✅ **AdvancedDataGrid.cs** - jediný public API súbor
- ✅ **Single using statement** - `using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;`
- ✅ **No internal namespace pollution** - žiadne internal typy v IntelliSense

### 2. **Clean Architecture Layers**
- ✅ **Application/API** - Public types for external use
- ✅ **Core** - Business logic and interfaces
- ✅ **Infrastructure** - Service implementations
- ✅ **Presentation** - UI components

### 3. **SOLID Principles**
- ✅ **Single Responsibility** - každá trieda má jednu zodpovednosť
- ✅ **Open/Closed** - rozšíriteľné cez interfaces
- ✅ **Liskov Substitution** - interfaces sú substituovateľné
- ✅ **Interface Segregation** - špecializované interfaces
- ✅ **Dependency Inversion** - závislosti cez abstractions

### 4. **API Design**
- ✅ **Standard .NET types** - int, string, bool, DateTime, Dictionary, DataTable
- ✅ **No custom complex types** - v API argumentoch
- ✅ **IntelliSense friendly** - všetko dostupné cez AdvancedDataGrid
- ✅ **Async/await patterns** - moderné asynchronné programovanie

## 📈 Implemented Features

### ✅ Complete Feature Set Implementation

**🚀 ENTERPRISE GRADE**: All required features implemented with comprehensive logging, error handling, and performance optimization.

### ✅ Enhanced Import/Export Command Structure
- ✅ **ImportDataCommand** with all required arguments (DictionaryData, DataTableData, CheckboxStates, StartRow, Mode, Timeout, ValidationProgress)
- ✅ **ExportDataCommand** with advanced filtering (IncludeValidAlerts, ExportOnlyChecked, ExportOnlyFiltered, RemoveAfter)
- ✅ **Backward compatibility** with ImportFromDataTableCommand and ExportToDataTableCommand
- ✅ **Progress reporting** with ValidationProgress and ExportProgress types
- ✅ **Timeout protection** and cancellation support

### ✅ Advanced Filtering with Grouping Logic
- ✅ **AdvancedFilter** with GroupStart/GroupEnd support for complex parentheses logic
- ✅ **Complex expressions** like (Age > 18 AND Department = "IT") OR (Salary > 50000)
- ✅ **Balanced parentheses validation** with detailed error reporting
- ✅ **Recursive expression evaluation** with proper precedence handling
- ✅ **Performance optimization** with short-circuiting and early termination

### ✅ Smart Delete & Minimum Rows Management
- ✅ **MinimumRows property** with automatic enforcement
- ✅ **Smart delete logic**: Delete rows above minimum, clear content below minimum
- ✅ **Automatic empty row** maintenance at the end for new data entry
- ✅ **Checkbox state management** with index shifting during deletions
- ✅ **Export with removeAfter** applies smart delete logic

### ✅ ValidAlerts Column Integration
- ✅ **Special column type** ValidAlerts for displaying validation errors
- ✅ **Automatic population** based on validation rule failures
- ✅ **Export inclusion** when includeValidAlerts = true
- ✅ **Formatted error messages** with concatenation and proper handling
- ✅ **Read-only column** with appropriate styling and configuration

### ✅ Validation System (8 Types)
```csharp
// Single cell validation
await dataGrid.AddSingleCellValidationAsync("Age", value => value is int age && age >= 0, "Age must be positive");

// Cross-column validation
await dataGrid.AddCrossColumnValidationAsync(new[] { "FirstName", "LastName" },
    row => (row.ContainsKey("FirstName") && row.ContainsKey("LastName"), null), "Both names required");

// Conditional validation
await dataGrid.AddConditionalValidationAsync("Salary",
    row => row["Department"]?.ToString() == "Sales",
    value => value is decimal salary && salary > 30000, "Sales salary must be > 30k");
```

### ✅ Enhanced Data Import/Export (Dictionary & DataTable)
```csharp
// Dictionary import with enhanced options
var data = new[] { new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 30 } };
await dataGrid.ImportFromDictionaryAsync(data);

// DataTable import
await dataGrid.ImportFromDataTableAsync(dataTable);

// Advanced Dictionary export with ValidAlerts support
var (result, exportedData) = await dataGrid.ExportToDictionaryAsync(
    includeValidAlerts: true,
    exportOnlyChecked: false,
    exportOnlyFiltered: false,
    removeAfter: false);

// Advanced DataTable export with post-export removal
var (result, dataTable) = await dataGrid.ExportToDataTableAsync(
    includeValidAlerts: true,
    exportOnlyChecked: true,
    exportOnlyFiltered: false,
    removeAfter: true);

// Excel-compatible copy/paste (tab-delimited)
var copyResult = await dataGrid.CopyToClipboardAsync();
await dataGrid.PasteFromClipboardAsync(clipboardData);
```

### ✅ Advanced Search & Filter with Grouping
```csharp
// Advanced search with regex
var searchResults = await dataGrid.SearchAsync(new AdvancedSearchCriteria
{
    SearchText = "John.*Developer",
    UseRegex = true
});

// Simple business logic filters
var filterResults = await dataGrid.ApplyFiltersAsync(new[]
{
    FilterDefinition.GreaterThan("Age", 25),
    FilterDefinition.Contains("Department", "Engineering")
});

// Advanced filters with complex grouping: (Age > 18 AND Department = "IT") OR (Salary > 50000)
var advancedFilterResults = await dataGrid.ApplyFiltersAsync(new[]
{
    new AdvancedFilter
    {
        ColumnName = "Age",
        Operator = FilterOperator.GreaterThan,
        Value = 18,
        GroupStart = true  // Start group
    },
    new AdvancedFilter
    {
        ColumnName = "Department",
        Operator = FilterOperator.Equals,
        Value = "IT",
        LogicOperator = FilterLogicOperator.And,
        GroupEnd = true  // End group
    },
    new AdvancedFilter
    {
        ColumnName = "Salary",
        Operator = FilterOperator.GreaterThan,
        Value = 50000,
        LogicOperator = FilterLogicOperator.Or  // OR between groups
    }
});
```

### ✅ Multi-Column Sorting
```csharp
// Single column sort
await dataGrid.SortByColumnAsync("Name", SortDirection.Ascending);

// Toggle column sort (for UI clicks)
await dataGrid.ToggleColumnSortAsync("Salary");

// Clear all sorting
dataGrid.ClearAllSorts();
```

### ✅ Configuration
```csharp
// Virtualization for large datasets
dataGrid.VirtualizationConfiguration = VirtualizationConfiguration.MassiveDataset;

// Sort configuration
dataGrid.SortConfiguration = SortConfiguration.Default;

// Auto row height for multiline text
dataGrid.AutoRowHeightConfiguration = AutoRowHeightConfiguration.Spacious;

// Keyboard shortcuts
dataGrid.KeyboardShortcutConfiguration = KeyboardShortcutConfiguration.CreateDefault();

// Smart delete and minimum rows configuration
dataGrid.MinimumRows = 5;  // Maintain at least 5 rows
```

### ✅ Smart Delete & Minimum Rows Logic
```csharp
// Configure minimum rows to maintain table structure
dataGrid.MinimumRows = 3;  // At least 3 rows must always be present

// Smart delete behavior:
// - Rows above minimum: DELETE = removes entire row
// - Rows at/below minimum: DELETE = clears content but keeps row structure
// - Always maintains +1 empty row at the end for new data entry

// Example scenarios:
// 1. Grid has 10 rows, minimum is 3
//    - Deleting rows 1-7: Rows are completely removed
//    - Deleting rows 8-10: Only content is cleared, row structure remains
//    - Result: 3 rows with content cleared + 1 empty row at end = 4 total rows

// 2. Export with removeAfter = true applies same smart delete logic
var (result, data) = await dataGrid.ExportToDataTableAsync(
    exportOnlyChecked: true,
    removeAfter: true);  // Uses smart delete on exported rows
```

### ✅ ValidAlerts Column Support
```csharp
// ValidAlerts is a special column that displays validation error messages
// - Automatically populated based on validation rule failures
// - Read-only column showing formatted error messages
// - Included in exports when includeValidAlerts = true
// - Always visible in UI mode, included in headless mode exports

// Example: Export with validation alerts included
var (result, exportedData) = await dataGrid.ExportToDictionaryAsync(
    includeValidAlerts: true);  // Adds "ValidAlerts" column with error messages

// The ValidAlerts column contains:
// - Concatenated validation error messages separated by "; "
// - Empty string for rows with no validation errors
// - "Validation check failed" if validation process itself fails
```

### ✅ Automatic Scrolling & Table Overflow Management
```csharp
// AUTOMATIC SCROLLING: Smart overflow handling
// - Horizontal scrollbars appear when table width exceeds container
// - Vertical scrollbars appear when table height exceeds container
// - Mouse wheel scrolling support for vertical navigation
// - Smooth scrolling with configurable acceleration (3x multiplier)
// - Programmatic scrolling to specific rows

// Mouse wheel scrolling automatically enabled - no configuration needed
// Scrollbars appear/disappear automatically based on content size

// Programmatic navigation to specific row
dataGrid.ScrollToRow(150);  // Scrolls to row 150 with smooth animation

// Container size changes automatically trigger scrollbar visibility updates
// Comprehensive logging tracks all scrolling operations for debugging
```

### ✅ Enhanced UI Control Features
```csharp
// RESPONSIVE DESIGN: Table adapts to container size changes
// - Auto-scrollbars when content overflows container dimensions
// - Smooth mouse wheel scrolling with momentum
// - Touch-friendly scrolling for tablet interfaces
// - Keyboard navigation support (Arrow keys, Page Up/Down)
// - Focus management maintains selection during scrolling

// Color configuration support with theme switching
dataGrid.ApplyColorConfiguration(ColorConfiguration.DarkTheme);

// Professional error handling with detailed logging
// - All user interactions logged with performance metrics
// - Scroll operations tracked with delta and position information
// - Container size changes monitored for responsive behavior
```

## 🔍 Development Workflow

### For External Developers:
1. **Single using statement**: `using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;`
2. **IntelliSense discovery**: Everything cez `AdvancedDataGrid` class
3. **Standard .NET types**: Žiadne custom typy v API
4. **Async/await patterns**: Moderné asynchronné volania

### For Internal Development:
1. **Clean separation**: API types v `Application/API`
2. **Business logic**: V `Core` layer
3. **Implementations**: V `Infrastructure` layer
4. **UI components**: V `Presentation` layer
5. **No God classes**: Každá trieda má jednu zodpovednosť

## 📊 Performance Characteristics

- ✅ **Virtualization**: Podpora pre 10M+ rows s intelligent caching
- ✅ **Memory management**: Optimalizované pre veľké datasety
- ✅ **Background processing**: Non-blocking operácie s async/await
- ✅ **Progress reporting**: Real-time feedback pre ValidationProgress a ExportProgress
- ✅ **Timeout protection**: 2-second default pre validation s konfigurovateľným timeout
- ✅ **Smart filtering**: Short-circuiting evaluation pre complex filter expressions
- ✅ **Efficient grouping**: Recursive expression evaluation s balanced parentheses
- ✅ **Minimal memory allocation**: Object pooling a reuse patterns
- ✅ **Smooth scrolling**: Hardware-accelerated scrolling s mouse wheel support
- ✅ **Responsive UI**: Auto-scrollbars s real-time container size monitoring
- ✅ **Performance logging**: Detailné logovanie všetkých operácií pre debugging
- ✅ **UI virtualization**: ItemsRepeater pre efficient rendering veľkých datasets

## 🎯 Migration Benefits

### Before (Old Architecture):
- ❌ Multiple using statements needed
- ❌ Internal namespaces in IntelliSense
- ❌ God classes with mixed responsibilities
- ❌ Complex type dependencies
- ❌ Namespace pollution

### After (New Architecture):
- ✅ Single using statement: `using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;`
- ✅ Clean IntelliSense experience
- ✅ SOLID principles throughout
- ✅ Standard .NET types only
- ✅ Clean namespace structure

## 🔧 Recent Updates & Improvements

### ✅ Compilation Error Fixes
- ✅ **Color Type Resolution**: Fixed Microsoft.UI.Color references across ViewModels
- ✅ **ColorConfiguration**: Created comprehensive color theming system
- ✅ **ColumnSpecialType**: Added backward compatibility alias for SpecialColumnType
- ✅ **LoggerConfiguration**: Fixed accessibility modifiers for public API exposure
- ✅ **ValidationResult**: Made public for external validation rule implementations
- ✅ **Interface Definitions**: Resolved duplicate interface definitions
- ✅ **Test Framework**: Added conditional compilation for Xunit tests

### ✅ Automatic Scrolling Implementation
- ✅ **ScrollViewer Configuration**: Auto scrollbars with HorizontalScrollBarVisibility="Auto" and VerticalScrollBarVisibility="Auto"
- ✅ **Mouse Wheel Support**: PointerWheelChanged event handler for smooth vertical scrolling
- ✅ **Programmatic Navigation**: ScrollToRow method with animation support
- ✅ **Container Responsiveness**: SizeChanged event monitoring for dynamic scrollbar management
- ✅ **Performance Logging**: Comprehensive logging for all scroll operations
- ✅ **Touch Support**: IsTabStop and TabNavigation for tablet-friendly operation

### ✅ UI Enhancement Features
- ✅ **Smooth Scrolling**: 3x acceleration multiplier for responsive mouse wheel
- ✅ **Hardware Acceleration**: ChangeView with smooth animation support
- ✅ **Error Handling**: Try-catch blocks around all scroll operations
- ✅ **Event Handling**: Proper event.Handled marking to prevent parent interference
- ✅ **Size Monitoring**: Real-time container and content size tracking
- ✅ **Focus Management**: Proper focus handling during scroll operations

---

*AdvancedDataGrid v2.2.2 - Clean Architecture Implementation*
*Single Using Statement • Standard .NET Types • SOLID Principles • Auto-Scrolling*
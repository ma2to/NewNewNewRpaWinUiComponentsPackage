# KOMPLETNÁ ŠPECIFIKÁCIA: ROW, COLUMN & CELL MANAGEMENT SYSTÉM PRE ADVANCEDWINUIDATAGRID

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY & ŠTRUKTÚRA

### Clean Architecture + Entity Pattern (Jednotná s ostatnými časťami)
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Entity services, data validation (internal)
- **Core Layer**: Domain entities, value objects, addressing (internal)
- **Infrastructure Layer**: Performance monitoring, thread safety (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každá entita má jednu zodpovednosť (Row/Column/Cell)
- **Open/Closed**: Rozšíriteľné pre nové typy entities bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky entity implementujú spoločné patterns
- **Interface Segregation**: Špecializované interfaces pre rôzne typy operations
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained (Jednotné s ostatnými časťami)
- **Clean Architecture**: Entities v Core layer, services v Application layer
- **Hybrid DI**: Entity factory methods s dependency injection support
- **Functional/OOP**: Immutable value objects + encapsulated entity behavior
- **SOLID**: Single responsibility pre každý entity type
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable addressing, atomic entity operations
- **Internal DI Registration**: Všetky entity services budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť `.oldbackup_timestamp` súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s entity pattern a thread-safe operations
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## 📋 CORE ENTITIES & VALUE OBJECTS

### 1. **DataRow.cs** - Core Row Entity

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;

/// <summary>
/// CORE ENTITY: Main row entity with thread-safe operations
/// ENTERPRISE: Professional row management with change tracking and validation
/// THREAD-SAFE: Atomic operations for concurrent access scenarios
/// </summary>
internal sealed class DataRow
{
    // Core Properties
    public int RowIndex { get; private set; }
    public int RowNumber { get; private set; }
    public bool IsEmpty { get; private set; }
    public bool HasUnsavedChanges { get; private set; }
    public bool HasValidationErrors { get; private set; }
    public IReadOnlyDictionary<string, Cell> Cells { get; private set; }
    public DateTime LastModified { get; private set; }
    public int Version { get; private set; }

    // Key Methods
    public void SetCell(string columnName, object? value)
    public Cell? GetCell(string columnName)
    public object? GetCellValue(string columnName)
    public void SetCellValue(string columnName, object? value)
    public void RemoveCell(string columnName)

    // Change Management
    public void CommitChanges()
    public void RevertChanges()

    // Validation
    public void ClearValidationResults()
    public IReadOnlyDictionary<string, object?> GetRowData()
    public ValidationSeverity GetHighestSeverity()

    // Events
    public event EventHandler<RowStateChangedEventArgs>? StateChanged;
    public event EventHandler<CellValueChangedEventArgs>? CellValueChanged;
}

/// <summary>
/// CORE ENUM: Row state change tracking
/// ENTERPRISE: Professional state change categorization
/// </summary>
internal enum RowStateChangeType
{
    BecameEmpty,
    BecameNotEmpty,
    ChangesDetected,
    ChangesCommitted,
    ChangesReverted,
    ValidationStatusChanged
}
```

### 2. **DataColumn.cs** - Core Column Entity

```csharp
using System;
using System.ComponentModel;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;

/// <summary>
/// CORE ENTITY: Main column entity with configuration management
/// ENTERPRISE: Professional column configuration with dynamic behavior
/// THREAD-SAFE: Immutable configuration with atomic updates
/// </summary>
internal sealed class DataColumn
{
    // Core Properties
    public string Name { get; private set; }
    public string OriginalName { get; private set; }
    public string DisplayName { get; private set; }
    public object? DefaultValue { get; private set; }

    // Layout Properties
    public double Width { get; private set; }
    public double MinWidth { get; private set; }
    public double MaxWidth { get; private set; }

    // Type & Behavior Properties
    public Type DataType { get; private set; }
    public SpecialColumnType SpecialType { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsReadOnly { get; private set; }
    public bool IsVisible { get; private set; }
    public bool IsResizable { get; private set; }
    public bool IsSortable { get; private set; }

    // Key Methods
    public static DataColumn CreateWithUniqueNameResolution(string name, IEnumerable<string> existingNames)
    public void AutoFitWidth(IEnumerable<string> sampleData)
    public void ConfigureAsSpecialColumn(SpecialColumnType specialType)
    public void Rename(string newName)
    public void UpdateDisplayName(string newDisplayName)
    public void SetDefaultValue(object? defaultValue)

    // Events
    public event EventHandler<PropertyChangedEventArgs>? NameChanged;
    public event EventHandler<PropertyChangedEventArgs>? WidthChanged;
}
```

### 3. **Cell.cs** - Core Cell Entity

```csharp
using System;
using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;

/// <summary>
/// CORE ENTITY: Main cell entity with value and validation tracking
/// ENTERPRISE: Professional cell data management with change detection
/// VALIDATION: Integrated validation results with severity tracking
/// </summary>
internal sealed class Cell
{
    // Core Properties
    public CellAddress Address { get; private set; }
    public string ColumnName { get; private set; }
    public object? Value { get; private set; }
    public object? OriginalValue { get; private set; }
    public bool IsReadOnly { get; private set; }

    // State Properties
    public bool HasUnsavedChanges => !Equals(Value, OriginalValue);
    public bool IsEmpty => Value == null || string.IsNullOrWhiteSpace(Value.ToString());
    public bool HasValidationErrors { get; private set; }
    public IReadOnlyList<ValidationResult> ValidationResults { get; private set; }

    // Key Methods
    public void CommitChanges()
    public void RevertChanges()
    public void SetValidationResults(IEnumerable<ValidationResult> results)
    public void AddValidationResult(ValidationResult result)
    public void ClearValidationResults()
    public ValidationSeverity GetHighestSeverity()

    // Events
    public event EventHandler<CellValueChangedEventArgs>? ValueChanged;
}

/// <summary>
/// ENTERPRISE: Cell value change event arguments
/// COMPREHENSIVE: Complete change tracking with context
/// </summary>
internal sealed class CellValueChangedEventArgs : EventArgs
{
    public object? OldValue { get; }
    public object? NewValue { get; }
    public string ColumnName { get; }
    public CellAddress Address { get; }
    public DateTime ChangedAt { get; }
}
```

## 🎯 VALUE OBJECTS & ADDRESSING

### 1. **CellAddress.cs** - Cell Positioning System

```csharp
using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// CORE VALUE OBJECT: Cell addressing with navigation capabilities
/// IMMUTABLE: Thread-safe cell positioning with Excel-style addressing
/// NAVIGATION: Built-in movement and adjacency detection
/// </summary>
internal readonly record struct CellAddress
{
    public int RowIndex { get; }
    public int ColumnIndex { get; }

    // Backward Compatibility
    public int Row => RowIndex;

    public CellAddress(int rowIndex, int columnIndex)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
    }

    // Navigation Methods
    public bool IsAdjacentTo(CellAddress other)
    public CellAddress Offset(int rowOffset, int columnOffset)
    public CellAddress NextInRow()
    public CellAddress PreviousInRow()
    public CellAddress Below()
    public CellAddress Above()

    // Utility Methods
    public string ToExcelAddress()  // Returns A1, B2, etc. format

    // Static Factory Methods
    public static CellAddress Create(int row, int column) => new(row, column);
    public static CellAddress FromExcel(string excelAddress);  // Parse "A1" format
}
```

### 2. **ColumnDefinition.cs** - Column Configuration

```csharp
using System;
using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// CORE VALUE OBJECT: Comprehensive column configuration
/// IMMUTABLE: Thread-safe column definition with factory methods
/// ENTERPRISE: Professional column setup with validation integration
/// </summary>
internal sealed record ColumnDefinition
{
    // Basic Properties
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public Type DataType { get; init; } = typeof(string);

    // Behavior Properties
    public bool IsVisible { get; init; } = true;
    public bool IsReadOnly { get; init; } = false;
    public bool IsSortable { get; init; } = true;
    public bool IsFilterable { get; init; } = true;
    public bool IsResizable { get; init; } = true;

    // Layout Properties
    public double Width { get; init; } = 100;
    public double MinWidth { get; init; } = 50;
    public double MaxWidth { get; init; } = 500;

    // Data Properties
    public string? Format { get; init; }
    public object? DefaultValue { get; init; }
    public IReadOnlyList<IValidationRule>? ValidationRules { get; init; }
    public string? Tooltip { get; init; }
    public string? PlaceholderText { get; init; }

    // Special Properties
    public SpecialColumnType SpecialType { get; init; } = SpecialColumnType.None;
    public bool IsRowNumberColumn => SpecialType == SpecialColumnType.RowNumber;

    // Factory Methods
    public static ColumnDefinition CreateText(string name, string? displayName = null) =>
        new() { Name = name, DisplayName = displayName ?? name, DataType = typeof(string) };

    public static ColumnDefinition CreateNumber(string name, string? displayName = null) =>
        new() { Name = name, DisplayName = displayName ?? name, DataType = typeof(decimal) };

    public static ColumnDefinition CreateDate(string name, string? displayName = null) =>
        new() { Name = name, DisplayName = displayName ?? name, DataType = typeof(DateTime) };

    public static ColumnDefinition CreateBoolean(string name, string? displayName = null) =>
        new() { Name = name, DisplayName = displayName ?? name, DataType = typeof(bool) };

    public static ColumnDefinition CreateReadOnly(string name, Type dataType) =>
        new() { Name = name, DisplayName = name, DataType = dataType, IsReadOnly = true };

    public static ColumnDefinition CreateHidden(string name, Type dataType) =>
        new() { Name = name, DisplayName = name, DataType = dataType, IsVisible = false };

    public static ColumnDefinition CreateWithValidation(string name, Type dataType, IReadOnlyList<IValidationRule> validationRules) =>
        new() { Name = name, DisplayName = name, DataType = dataType, ValidationRules = validationRules };

    public static ColumnDefinition CreateRowNumberColumn() =>
        new() { Name = "RowNumber", DisplayName = "#", DataType = typeof(int),
                SpecialType = SpecialColumnType.RowNumber, IsReadOnly = true, Width = 50 };
}
```

## 🔧 SPECIAL COLUMN TYPES

### **SpecialColumnType.cs** - Column Type Classification

```csharp
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

/// <summary>
/// ENTERPRISE: Special column types for grid functionality
/// PREDEFINED: Built-in column behaviors with specialized handling
/// </summary>
internal enum SpecialColumnType
{
    None = 0,           // Regular data column
    CheckBox = 1,       // Boolean checkbox column
    DeleteRow = 2,      // Row deletion action column
    ValidAlerts = 3,    // Validation error display column
    RowNumber = 4       // Row number display column (core property)
}

// Backward Compatibility Alias
internal enum ColumnSpecialType
{
    None = SpecialColumnType.None,
    CheckBox = SpecialColumnType.CheckBox,
    DeleteRow = SpecialColumnType.DeleteRow,
    ValidAlerts = SpecialColumnType.ValidAlerts,
    RowNumber = SpecialColumnType.RowNumber
}
```

## 🎯 FACADE API METÓDY

### Row, Column & Cell Management API

```csharp
#region Row, Column & Cell Management Operations

/// <summary>
/// PUBLIC API: Get row entity by index
/// ENTERPRISE: Thread-safe row access with validation
/// </summary>
DataRow? GetRow(int rowIndex);

/// <summary>
/// PUBLIC API: Get column entity by name
/// ENTERPRISE: Column configuration access with metadata
/// </summary>
DataColumn? GetColumn(string columnName);

/// <summary>
/// PUBLIC API: Get cell entity by address
/// ENTERPRISE: Cell data access with validation state
/// </summary>
Cell? GetCell(CellAddress address);

/// <summary>
/// PUBLIC API: Set cell value with validation
/// ENTERPRISE: Thread-safe cell value updates with change tracking
/// </summary>
Task<Result<bool>> SetCellValueAsync(CellAddress address, object? value);

/// <summary>
/// PUBLIC API: Bulk cell updates with transaction support
/// PERFORMANCE: Efficient batch cell operations with rollback capability
/// </summary>
Task<Result<int>> SetCellValuesAsync(
    IEnumerable<(CellAddress Address, object? Value)> cellUpdates,
    bool validateBeforeCommit = true);

/// <summary>
/// PUBLIC API: Add new column with configuration
/// ENTERPRISE: Dynamic column addition with conflict resolution
/// </summary>
Task<Result<DataColumn>> AddColumnAsync(ColumnDefinition columnDefinition);

/// <summary>
/// PUBLIC API: Remove column with data handling
/// ENTERPRISE: Safe column removal with data preservation options
/// </summary>
Task<Result<bool>> RemoveColumnAsync(string columnName, bool preserveData = false);

/// <summary>
/// PUBLIC API: Reorder columns with validation
/// ENTERPRISE: Column reordering with layout optimization
/// </summary>
Task<Result<bool>> ReorderColumnsAsync(IReadOnlyList<string> newColumnOrder);

#endregion
```

## ⚡ PERFORMANCE OPTIMIZATIONS & THREAD SAFETY

### Entity Management Optimizations

```csharp
internal sealed class EntityManager
{
    // PERFORMANCE: Object pooling for DataRow entities
    private readonly ObjectPool<DataRow> _rowPool = new();

    // THREAD-SAFETY: Concurrent collections for entity access
    private readonly ConcurrentDictionary<int, DataRow> _rows = new();
    private readonly ConcurrentDictionary<string, DataColumn> _columns = new();

    // PERFORMANCE: Efficient cell addressing with spatial indexing
    private readonly Dictionary<CellAddress, Cell> _cellIndex = new();

    // ATOMIC OPERATIONS: Thread-safe entity operations
    public async Task<DataRow> CreateRowAsync(int rowIndex)
    {
        return await Task.Run(() =>
        {
            var row = _rowPool.Get();
            row.Initialize(rowIndex);
            _rows.TryAdd(rowIndex, row);
            return row;
        });
    }

    // LINQ OPTIMIZATION: Efficient entity queries
    public IEnumerable<Cell> GetCellsInRange(CellAddress start, CellAddress end)
    {
        return _cellIndex
            .Where(kvp => IsInRange(kvp.Key, start, end))
            .Select(kvp => kvp.Value)
            .AsParallel();
    }
}
```

## 🔍 GRID BEHAVIOR CONFIGURATION

### Entity-Specific Configuration

```csharp
/// <summary>
/// ENTERPRISE: Grid behavior configuration for entities
/// COMPREHENSIVE: Complete entity behavior customization
/// </summary>
internal sealed record GridEntityConfiguration
{
    // Row Configuration
    public bool EnableRowSelection { get; init; } = true;
    public bool EnableMultiRowSelection { get; init; } = true;
    public bool EnableRowReordering { get; init; } = false;
    public bool EnableRowGrouping { get; init; } = false;

    // Column Configuration
    public bool EnableColumnResizing { get; init; } = true;
    public bool EnableColumnReordering { get; init; } = true;
    public bool EnableColumnHiding { get; init; } = true;
    public bool EnableColumnAutoFit { get; init; } = true;

    // Cell Configuration
    public bool EnableCellSelection { get; init; } = true;
    public bool EnableCellEditing { get; init; } = true;
    public bool EnableCellValidation { get; init; } = true;
    public bool EnableCellFormatting { get; init; } = true;

    // Factory Methods
    public static GridEntityConfiguration Default => new();
    public static GridEntityConfiguration ReadOnly => new()
    {
        EnableRowReordering = false,
        EnableColumnReordering = false,
        EnableCellEditing = false
    };

    public static GridEntityConfiguration Minimal => new()
    {
        EnableRowSelection = false,
        EnableColumnResizing = false,
        EnableColumnReordering = false,
        EnableCellSelection = false,
        EnableCellEditing = false
    };
}
```

## **🔍 LOGGING ŠPECIFIKÁCIA PRE ENTITY MANAGEMENT**

### **Internal DI Registration & Service Distribution**
Všetky entity management logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`**:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<IEntityLogger<EntityManager>, EntityLogger<EntityManager>>();
services.AddSingleton<IOperationLogger<EntityManager>, OperationLogger<EntityManager>>();
services.AddSingleton<ICommandLogger<EntityManager>, CommandLogger<EntityManager>>();
```

### **Entity Operations Logging**

```csharp
// Row operations logging
_entityLogger.LogRowOperation("Create", rowIndex, operationTime);
_logger.LogInformation("Row created: index={RowIndex}, time={Duration}ms", rowIndex, operationTime.TotalMilliseconds);

// Column operations logging
_entityLogger.LogColumnOperation("Add", columnName, columnType, operationTime);
_logger.LogInformation("Column added: name={ColumnName}, type={DataType}, time={Duration}ms",
    columnName, columnType, operationTime.TotalMilliseconds);

// Cell operations logging
_entityLogger.LogCellOperation("Update", cellAddress.ToString(), oldValue, newValue, operationTime);
_logger.LogInformation("Cell updated: address={Address}, oldValue={OldValue}, newValue={NewValue}, time={Duration}ms",
    cellAddress, oldValue, newValue, operationTime.TotalMilliseconds);
```

### **Validation Integration Logging**

```csharp
// Cell validation logging
_logger.LogInformation("Cell validation completed: address={Address}, isValid={IsValid}, errorCount={ErrorCount}",
    cellAddress, validationResult.IsValid, validationResult.ErrorCount);

// Row validation logging
_logger.LogInformation("Row validation completed: rowIndex={RowIndex}, validCells={ValidCells}, invalidCells={InvalidCells}",
    rowIndex, validCellCount, invalidCellCount);
```

### **Logging Levels Usage:**
- **Information**: Successful entity operations, configuration changes, performance metrics
- **Warning**: Validation failures, performance issues, configuration conflicts
- **Error**: Entity operation failures, data integrity issues, constraint violations
- **Critical**: Entity system failures, severe data corruption, thread safety violations

Táto špecifikácia poskytuje kompletný, enterprise-ready entity management systém pre rows, columns a cells s pokročilými performance optimalizáciami, thread safety a jednotnou architektúrou s ostatnými časťami komponentu.
# KOMPLETNÁ ŠPECIFIKÁCIA: SMART ROW MANAGEMENT SYSTÉM PRE ADVANCEDWINUIDATAGRID

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY & ŠTRUKTÚRA

### Clean Architecture + Command Pattern (Jednotná s ostatnými časťami)
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Command handlers, row management services (internal)
- **Core Layer**: Row management entities, smart algorithms (internal)
- **Infrastructure Layer**: Row number tracking, performance monitoring (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý row management command má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové typy row operations bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky row commands implementujú spoločné interface
- **Interface Segregation**: Špecializované interfaces pre rôzne typy row operations
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained (Jednotné s ostatnými časťami)
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable commands + encapsulated behavior
- **SOLID**: Single responsibility pre každý command type
- **LINQ Optimization**: Lazy evaluation, parallel processing pre row operations
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable commands, atomic row operations
- **Internal DI Registration**: Všetky row management časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť `.oldbackup_timestamp` súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a smart row logic
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## 📋 CORE VALUE OBJECTS & COMMAND PATTERN

### 1. **RowManagementTypes.cs** - Core Layer

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

#region Enums

/// <summary>
/// ENTERPRISE: Row operation types for smart management
/// </summary>
internal enum RowOperationType
{
    Add,
    Delete,
    Clear,
    AutoExpand,
    SmartDelete
}

/// <summary>
/// ENTERPRISE: Row state for tracking empty/filled rows
/// </summary>
internal enum RowState
{
    Empty,
    Filled,
    Partial,
    LastEmpty
}

#endregion

#region Progress & Context Types

/// <summary>
/// ENTERPRISE: Row management progress reporting
/// CONSISTENT: Rovnaká štruktúra ako SortProgress a ValidationProgress
/// </summary>
internal sealed record RowManagementProgress
{
    internal int ProcessedRows { get; init; }
    internal int TotalRows { get; init; }
    internal double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    internal TimeSpan ElapsedTime { get; init; }
    internal string CurrentOperation { get; init; } = string.Empty;
    internal RowOperationType CurrentOperationType { get; init; } = RowOperationType.Add;

    /// <summary>Estimated time remaining based on current progress</summary>
    internal TimeSpan? EstimatedTimeRemaining => ProcessedRows > 0 && TotalRows > ProcessedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ProcessedRows) / ProcessedRows)
        : null;

    public RowManagementProgress() : this(0, 0, TimeSpan.Zero, "", RowOperationType.Add) { }

    public RowManagementProgress(int processedRows, int totalRows, TimeSpan elapsedTime, string currentOperation, RowOperationType operationType)
    {
        ProcessedRows = processedRows;
        TotalRows = totalRows;
        ElapsedTime = elapsedTime;
        CurrentOperation = currentOperation;
        CurrentOperationType = operationType;
    }
}

/// <summary>
/// ENTERPRISE: Row management configuration
/// CORE: Definuje minimálny počet riadkov a smart behavior
/// </summary>
internal sealed record RowManagementConfiguration
{
    internal int MinimumRows { get; init; } = 1;
    internal bool EnableAutoExpand { get; init; } = true;
    internal bool EnableSmartDelete { get; init; } = true;
    internal bool AlwaysKeepLastEmpty { get; init; } = true;
    internal bool EnableRowShifting { get; init; } = true;

    internal static RowManagementConfiguration Default => new();

    internal static RowManagementConfiguration Create(
        int minimumRows = 1,
        bool enableAutoExpand = true,
        bool enableSmartDelete = true) =>
        new()
        {
            MinimumRows = minimumRows,
            EnableAutoExpand = enableAutoExpand,
            EnableSmartDelete = enableSmartDelete
        };
}

#endregion

#region Command Objects

/// <summary>
/// COMMAND PATTERN: Smart add rows command
/// CONSISTENT: Rovnaká štruktúra ako ImportCommand a ValidationCommand
/// </summary>
internal sealed record SmartAddRowsCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> DataToAdd { get; init; }
    internal required RowManagementConfiguration Configuration { get; init; }
    internal bool PreserveRowNumbers { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<RowManagementProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    internal static SmartAddRowsCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> dataToAdd,
        RowManagementConfiguration configuration) =>
        new() { DataToAdd = dataToAdd, Configuration = configuration };

    internal static SmartAddRowsCommand WithProgress(
        IEnumerable<IReadOnlyDictionary<string, object?>> dataToAdd,
        RowManagementConfiguration configuration,
        IProgress<RowManagementProgress> progressReporter) =>
        new() { DataToAdd = dataToAdd, Configuration = configuration, ProgressReporter = progressReporter };
}

/// <summary>
/// COMMAND PATTERN: Smart delete rows command
/// ENTERPRISE: Inteligentné mazanie s context-aware logic
/// </summary>
internal sealed record SmartDeleteRowsCommand
{
    internal required IReadOnlyList<int> RowIndexesToDelete { get; init; }
    internal required RowManagementConfiguration Configuration { get; init; }
    internal bool ForcePhysicalDelete { get; init; } = false;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<RowManagementProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    internal static SmartDeleteRowsCommand Create(
        IReadOnlyList<int> rowIndexesToDelete,
        RowManagementConfiguration configuration) =>
        new() { RowIndexesToDelete = rowIndexesToDelete, Configuration = configuration };

    internal static SmartDeleteRowsCommand WithForceDelete(
        IReadOnlyList<int> rowIndexesToDelete,
        RowManagementConfiguration configuration,
        bool forcePhysicalDelete = true) =>
        new()
        {
            RowIndexesToDelete = rowIndexesToDelete,
            Configuration = configuration,
            ForcePhysicalDelete = forcePhysicalDelete
        };
}

/// <summary>
/// COMMAND PATTERN: Auto-expand empty row command
/// SMART: Automatické udržiavanie prázdneho riadku na konci
/// </summary>
internal sealed record AutoExpandEmptyRowCommand
{
    internal required RowManagementConfiguration Configuration { get; init; }
    internal int CurrentRowCount { get; init; }
    internal bool TriggerExpansion { get; init; } = true;
    internal cancellationToken cancellationToken { get; init; } = default;

    internal static AutoExpandEmptyRowCommand Create(
        RowManagementConfiguration configuration,
        int currentRowCount) =>
        new() { Configuration = configuration, CurrentRowCount = currentRowCount };
}

#endregion

#region Result Objects

/// <summary>
/// ENTERPRISE: Row management operation result
/// CONSISTENT: Rovnaká štruktúra ako SortResult a ValidationResult
/// </summary>
internal sealed record RowManagementResult
{
    internal bool Success { get; init; }
    internal int FinalRowCount { get; init; }
    internal int ProcessedRows { get; init; }
    internal RowOperationType OperationType { get; init; }
    internal TimeSpan OperationTime { get; init; }
    internal IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
    internal RowManagementStatistics Statistics { get; init; } = new();

    internal static RowManagementResult CreateSuccess(
        int finalRowCount,
        int processedRows,
        RowOperationType operationType,
        TimeSpan operationTime) =>
        new()
        {
            Success = true,
            FinalRowCount = finalRowCount,
            ProcessedRows = processedRows,
            OperationType = operationType,
            OperationTime = operationTime
        };

    internal static RowManagementResult CreateFailure(
        RowOperationType operationType,
        IReadOnlyList<string> messages,
        TimeSpan operationTime) =>
        new()
        {
            Success = false,
            OperationType = operationType,
            Messages = messages,
            OperationTime = operationTime
        };

    internal static RowManagementResult Empty => new();
}

/// <summary>
/// ENTERPRISE: Row management statistics
/// PERFORMANCE: Monitoring and optimization metrics
/// </summary>
internal sealed record RowManagementStatistics
{
    internal int EmptyRowsCreated { get; init; }
    internal int RowsPhysicallyDeleted { get; init; }
    internal int RowsContentCleared { get; init; }
    internal int RowsShifted { get; init; }
    internal bool MinimumRowsEnforced { get; init; }
    internal bool LastEmptyRowMaintained { get; init; }
}

#endregion
```

## 🎯 FACADE API METÓDY

### Základné Row Management API (Consistent s existujúcimi metódami)

```csharp
#region Smart Row Management Operations with Command Pattern

/// <summary>
/// PUBLIC API: Smart add rows using command pattern
/// ENTERPRISE: Inteligentné pridávanie riadkov s minimum rows management
/// LOGIKA: Import ≥ minimumRows → všetky + 1 prázdny | Import < minimumRows → minimumRows + 1 prázdny
/// </summary>
Task<RowManagementResult> SmartAddRowsAsync(
    SmartAddRowsCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Smart delete rows with context-aware logic
/// ENTERPRISE: Inteligentné mazanie s min rows enforcement
/// LOGIKA: Riadky ≤ min → clear content + shift | Riadky > min → physical delete
/// </summary>
Task<RowManagementResult> SmartDeleteRowsAsync(
    SmartDeleteRowsCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Auto-expand empty row maintenance
/// SMART: Automatické udržiavanie prázdneho riadku na konci
/// TRIGGER: Aktivuje sa pri zadaní do posledného prázdneho riadku
/// </summary>
Task<RowManagementResult> AutoExpandEmptyRowAsync(
    AutoExpandEmptyRowCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Validate row management configuration
/// ENTERPRISE: Comprehensive row management validation
/// </summary>
Task<Result<bool>> ValidateRowManagementConfigurationAsync(
    RowManagementConfiguration configuration);

/// <summary>
/// PUBLIC API: Get current row management statistics
/// MONITORING: Real-time row management metrics
/// </summary>
RowManagementStatistics GetRowManagementStatistics();

#endregion
```

## 🚀 SMART ROW MANAGEMENT LOGIKA

### 1. **MINIMUM ROWS CONFIGURATION**
```csharp
// Pri inicializácii nastaviť minimumRows (default = 1)
var config = RowManagementConfiguration.Create(
    minimumRows: 14,
    enableAutoExpand: true,
    enableSmartDelete: true
);

// Komponent nikdy nemôže mať menej riadkov ako minimumRows
```

### 2. **SMART ADD LOGIKA - Príklady:**
```csharp
// minimumRows=14, import=20 → 20 dátových + 1 prázdny = 21 riadkov
// minimumRows=14, import=13 → 13 dátových + 1 prázdny = 14 riadkov
// minimumRows=14, import=6 → 6 dátových + 8 prázdnych = 14 riadkov
// minimumRows=14, import=14 → 14 dátových + 1 prázdny = 15 riadkov

var addCommand = SmartAddRowsCommand.Create(importedData, config);
var result = await facade.SmartAddRowsAsync(addCommand);
```

### 3. **AUTO-EXPAND PRÁZDNY RIADOK**
```csharp
// Vždy jeden prázdny riadok na konci datasetu
// Pri zadaní do prázdneho riadku → vytvorí nový prázdny na konci
// Posledný prázdny riadok sa nikdy nedá zmazať

var expandCommand = AutoExpandEmptyRowCommand.Create(config, currentRowCount);
await facade.AutoExpandEmptyRowAsync(expandCommand);
```

### 4. **SMART DELETE LOGIKA**
```csharp
// A) Riadky ≤ minimumRows (zachová štruktúru):
// - Zmaže iba obsah buniek
// - Všetky riadky od mazaného sa posunú nahor
// - Celkový počet riadkov zostane nezmenený

// B) Riadky > minimumRows (reálne mazanie):
// - Zmaže celý riadok fyzicky
// - Počet riadkov sa zníži o 1
// - Vždy zostane 1 prázdny riadok na konci

var deleteCommand = SmartDeleteRowsCommand.Create(rowIndexesToDelete, config);
var result = await facade.SmartDeleteRowsAsync(deleteCommand);

// C) Mazanie prázdnych riadkov:
// - Posledný prázdny riadok sa nikdy nezmáže (pri pokuse sa vytvorí nový)
```

## ⚡ PERFORMANCE & LINQ OPTIMIZATIONS

### Smart Algorithm Implementation
```csharp
internal sealed class RowManagementService
{
    // LINQ OPTIMIZATION: Efficient row state analysis
    private async Task<RowState[]> AnalyzeRowStatesAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        cancellationToken cancellationToken)
    {
        return await Task.Run(() =>
            data.AsParallel()
                .WithCancellation(cancellationToken)
                .Select((row, index) => new { row, index })
                .Select(item => AnalyzeRowState(item.row, item.index, data.Count))
                .ToArray(),
            cancellationToken);
    }

    // PERFORMANCE: Smart row shifting with minimal operations
    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ShiftRowsUpAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        int deletedRowIndex,
        cancellationToken cancellationToken)
    {
        return await Task.Run(() =>
            data.Take(deletedRowIndex)
                .Concat(data.Skip(deletedRowIndex + 1))
                .Concat(new[] { CreateEmptyRow(data.FirstOrDefault()) })
                .ToList(),
            cancellationToken);
    }
}
```

## 🎯 KĽÚČOVÉ VYLEPŠENIA & ROZŠÍRENIA

### 1. **Smart Row State Management**
- **Empty Row Detection**: Automatická detekcia prázdnych riadkov
- **Last Row Protection**: Posledný prázdny riadok sa nikdy nezmáže
- **Minimum Enforcement**: Vynútenie minimálneho počtu riadkov
- **Auto-Expansion**: Automatické rozširovanie pri potrebe

### 2. **Context-Aware Delete Logic**
- **Structural Preservation**: Zachovanie štruktúry pri ≤ minimumRows
- **Physical Deletion**: Reálne mazanie pri > minimumRows
- **Row Shifting**: Efektívne posúvanie riadkov nahor
- **Empty Row Maintenance**: Udržiavanie prázdneho riadku na konci

### 3. **Performance Optimizations**
- **LINQ Parallel Processing**: Pre analýzu stavu riadkov
- **Lazy Evaluation**: Deferred execution pre row operations
- **Atomic Operations**: Thread-safe row management
- **Memory Efficiency**: Minimálne alokácie pri row operations

### 4. **Comprehensive Monitoring**
- **Real-time Statistics**: Detailed row management metrics
- **Progress Reporting**: Real-time progress s estimated completion
- **Operation Tracking**: Comprehensive logging všetkých operations
- **Performance Metrics**: Row operation timing a throughput

### 5. **Enterprise Features**
- **Command Pattern**: Konzistentné API s ostatnými modulmi
- **Clean Architecture**: Separation of concerns
- **Internal DI**: Dependency injection support
- **Error Handling**: Graceful degradation s detailed error messages

## **🔍 LOGGING ŠPECIFIKÁCIA PRE ROW MANAGEMENT**

### **Internal DI Registration & Service Distribution**
Všetky row management logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`**:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<IRowManagementLogger<RowManagementService>, RowManagementLogger<RowManagementService>>();
services.AddSingleton<IOperationLogger<RowManagementService>, OperationLogger<RowManagementService>>();
services.AddSingleton<ICommandLogger<RowManagementService>, CommandLogger<RowManagementService>>();
```

### **Command Pattern Row Management Logging**
```csharp
// Smart add logging
_rowLogger.LogSmartAddOperation(command.DataToAdd.Count(),
    result.FinalRowCount, result.OperationTime);

_logger.LogInformation("Smart add completed: added={AddedRows}, final={FinalRows}, time={Duration}ms",
    result.ProcessedRows, result.FinalRowCount, result.OperationTime.TotalMilliseconds);

// Smart delete logging
_rowLogger.LogSmartDeleteOperation(command.RowIndexesToDelete.Count,
    deleteType: result.ProcessedRows > config.MinimumRows ? "Physical" : "ContentOnly",
    result.OperationTime);

// Auto-expand logging
_logger.LogInformation("Auto-expand triggered: currentRows={CurrentRows}, minRows={MinRows}, expanded={Expanded}",
    command.CurrentRowCount, config.MinimumRows, result.ProcessedRows > 0);
```

### **Logging Levels Usage:**
- **Information**: Successful row operations, configuration changes, statistics
- **Warning**: Minimum rows enforcement, auto-expansion triggers, performance issues
- **Error**: Row operation failures, configuration validation errors
- **Critical**: Data integrity issues, severe row management failures

Táto špecifikácia poskytuje kompletný, enterprise-ready smart row management systém s pokročilou logikou, performance optimalizáciami a jednotnou architektúrou s ostatnými časťami komponentu.
# KOMPLETNÁ ŠPECIFIKÁCIA: POKROČILÝ SORT SYSTÉM PRE ADVANCEDWINUIDATAGRID

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY & ŠTRUKTÚRA

### Clean Architecture + Command Pattern (Jednotná s ostatnými časťami)
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Command handlers, services (internal)
- **Core Layer**: Domain entities, sort algorithms, value objects (internal)
- **Infrastructure Layer**: Performance monitoring, resilience patterns (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý sort command má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové typy sort bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky sort commands implementujú spoločné interface
- **Interface Segregation**: Špecializované interfaces pre rôzne typy sort
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained (Jednotné s ostatnými časťami)
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable commands + encapsulated behavior
- **SOLID**: Single responsibility pre každý command type
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable commands, atomic operations
- **Internal DI Registration**: Všetky sort časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť `.oldbackup_timestamp` súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a LINQ optimizations
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## 📋 CORE VALUE OBJECTS & COMMAND PATTERN

### 1. **SortTypes.cs** - Core Layer

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

#region Enums

/// <summary>
/// ENTERPRISE: Sort direction enumeration for comprehensive sorting scenarios
/// </summary>
internal enum SortDirection
{
    None = 0,
    Ascending = 1,
    Descending = 2
}

/// <summary>
/// ENTERPRISE: Sort performance modes for different dataset sizes
/// </summary>
internal enum SortPerformanceMode
{
    Auto,           // Automatic selection based on data size
    Sequential,     // Single-threaded sorting
    Parallel,       // Multi-threaded sorting
    Optimized       // Advanced optimizations with caching
}

/// <summary>
/// ENTERPRISE: Sort stability options for consistent ordering
/// </summary>
internal enum SortStability
{
    Stable,         // Preserve relative order of equal elements
    Unstable        // Allow reordering of equal elements for performance
}

#endregion

#region Progress & Context Types

/// <summary>
/// ENTERPRISE: Sort operation progress reporting
/// CONSISTENT: Rovnaká štruktúra ako ValidationProgress a ExportProgress
/// </summary>
internal sealed record SortProgress
{
    internal int ProcessedRows { get; init; }
    internal int TotalRows { get; init; }
    internal double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    internal TimeSpan ElapsedTime { get; init; }
    internal string CurrentOperation { get; init; } = string.Empty;
    internal string? CurrentColumn { get; init; }
    internal SortDirection CurrentDirection { get; init; } = SortDirection.None;

    /// <summary>Estimated time remaining based on current progress</summary>
    internal TimeSpan? EstimatedTimeRemaining => ProcessedRows > 0 && TotalRows > ProcessedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ProcessedRows) / ProcessedRows)
        : null;

    public SortProgress() : this(0, 0, TimeSpan.Zero, "", null, SortDirection.None) { }

    public SortProgress(int processedRows, int totalRows, TimeSpan elapsedTime, string currentOperation, string? currentColumn, SortDirection currentDirection)
    {
        ProcessedRows = processedRows;
        TotalRows = totalRows;
        ElapsedTime = elapsedTime;
        CurrentOperation = currentOperation;
        CurrentColumn = currentColumn;
        CurrentDirection = currentDirection;
    }
}

/// <summary>
/// ENTERPRISE: Sort execution context with DI support
/// HYBRID DI: Poskytuje services pre custom sort functions
/// </summary>
internal sealed record SortContext
{
    internal IServiceProvider? ServiceProvider { get; init; }
    internal IReadOnlyDictionary<string, object?> SortParameters { get; init; } = new Dictionary<string, object?>();
    internal cancellationToken cancellationToken { get; init; } = default;
    internal DateTime ExecutionTime { get; init; } = DateTime.UtcNow;
    internal SortPerformanceMode PerformanceMode { get; init; } = SortPerformanceMode.Auto;
}

#endregion

#region Core Sort Definitions

/// <summary>
/// ENTERPRISE: Enhanced sort column configuration with comprehensive options
/// FUNCTIONAL: Immutable sort configuration with flexible composition
/// FLEXIBLE: Nie hardcoded factory methods, ale flexible object creation
/// </summary>
internal sealed record SortColumnConfiguration
{
    internal string ColumnName { get; init; } = string.Empty;
    internal SortDirection Direction { get; init; } = SortDirection.Ascending;
    internal int Priority { get; init; } = 0;
    internal bool IsPrimary { get; init; } = false;
    internal bool IsEnabled { get; init; } = true;
    internal bool CaseSensitive { get; init; } = false;
    internal bool NullsFirst { get; init; } = false;
    internal Func<object?, object?, int>? CustomComparer { get; init; }

    // FLEXIBLE factory methods namiesto hardcoded
    internal static SortColumnConfiguration Create(
        string columnName,
        SortDirection direction,
        int priority = 0) =>
        new()
        {
            ColumnName = columnName,
            Direction = direction,
            Priority = priority,
            IsPrimary = priority == 0
        };

    internal static SortColumnConfiguration WithCustomComparer(
        string columnName,
        SortDirection direction,
        Func<object?, object?, int> comparer,
        int priority = 0) =>
        new()
        {
            ColumnName = columnName,
            Direction = direction,
            Priority = priority,
            IsPrimary = priority == 0,
            CustomComparer = comparer
        };

    internal static SortColumnConfiguration CaseInsensitive(
        string columnName,
        SortDirection direction,
        int priority = 0) =>
        new()
        {
            ColumnName = columnName,
            Direction = direction,
            Priority = priority,
            IsPrimary = priority == 0,
            CaseSensitive = false
        };
}

/// <summary>
/// ENTERPRISE: Advanced sort configuration with business rule support
/// COMPLEX LOGIC: Supports multi-column sorting with performance optimization
/// </summary>
internal sealed record AdvancedSortConfiguration
{
    internal string ConfigurationName { get; init; } = string.Empty;
    internal IReadOnlyList<SortColumnConfiguration> SortColumns { get; init; } = Array.Empty<SortColumnConfiguration>();
    internal SortPerformanceMode PerformanceMode { get; init; } = SortPerformanceMode.Auto;
    internal SortStability Stability { get; init; } = SortStability.Stable;
    internal bool AllowMultiColumnSort { get; init; } = true;
    internal int MaxSortColumns { get; init; } = 5;
    internal TimeSpan? ExecutionTimeout { get; init; }
    internal bool EnableParallelProcessing { get; init; } = true;
    internal Func<IReadOnlyDictionary<string, object?>, SortContext, object?>? CustomSortKey { get; init; }

    // Business rule factories - FLEXIBLE nie hardcoded
    internal static AdvancedSortConfiguration CreateEmployeeHierarchy(
        string departmentColumn = "Department",
        string positionColumn = "Position",
        string salaryColumn = "Salary") =>
        new()
        {
            ConfigurationName = "EmployeeHierarchy",
            SortColumns = new[]
            {
                SortColumnConfiguration.Create(departmentColumn, SortDirection.Ascending, 0),
                SortColumnConfiguration.Create(positionColumn, SortDirection.Ascending, 1),
                SortColumnConfiguration.Create(salaryColumn, SortDirection.Descending, 2)
            },
            PerformanceMode = SortPerformanceMode.Auto,
            MaxSortColumns = 3
        };

    internal static AdvancedSortConfiguration CreateCustomerPriority(
        string tierColumn = "CustomerTier",
        string valueColumn = "TotalValue",
        string joinDateColumn = "JoinDate") =>
        new()
        {
            ConfigurationName = "CustomerPriority",
            SortColumns = new[]
            {
                SortColumnConfiguration.Create(tierColumn, SortDirection.Ascending, 0),
                SortColumnConfiguration.Create(valueColumn, SortDirection.Descending, 1),
                SortColumnConfiguration.Create(joinDateColumn, SortDirection.Ascending, 2)
            },
            PerformanceMode = SortPerformanceMode.Optimized,
            Stability = SortStability.Stable
        };
}

#endregion

#region Command Objects

/// <summary>
/// COMMAND PATTERN: Basic sort command with LINQ optimization
/// CONSISTENT: Rovnaká štruktúra ako ImportDataCommand a ValidateDataCommand
/// </summary>
internal sealed record SortCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required string ColumnName { get; init; }
    internal SortDirection Direction { get; init; } = SortDirection.Ascending;
    internal bool CaseSensitive { get; init; } = false;
    internal bool EnableParallelProcessing { get; init; } = true;
    internal SortPerformanceMode PerformanceMode { get; init; } = SortPerformanceMode.Auto;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<SortProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    // FLEXIBLE factory methods s LINQ optimization
    internal static SortCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        SortDirection direction = SortDirection.Ascending) =>
        new() { Data = data, ColumnName = columnName, Direction = direction };

    internal static SortCommand WithPerformanceMode(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        SortDirection direction,
        SortPerformanceMode performanceMode) =>
        new() { Data = data, ColumnName = columnName, Direction = direction, PerformanceMode = performanceMode };

    // LINQ optimized factory
    internal static SortCommand WithLINQOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        SortDirection direction) =>
        new()
        {
            Data = data.AsParallel().Where(row => row.ContainsKey(columnName)),
            ColumnName = columnName,
            Direction = direction,
            EnableParallelProcessing = true
        };
}

/// <summary>
/// COMMAND PATTERN: Multi-column sort command with comprehensive configuration
/// ENTERPRISE: Supports complex sorting scenarios and business logic
/// </summary>
internal sealed record MultiSortCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required IReadOnlyList<SortColumnConfiguration> SortColumns { get; init; }
    internal bool EnableParallelProcessing { get; init; } = true;
    internal SortPerformanceMode PerformanceMode { get; init; } = SortPerformanceMode.Auto;
    internal SortStability Stability { get; init; } = SortStability.Stable;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<SortProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;
    internal SortContext? Context { get; init; }

    // FLEXIBLE factory methods
    internal static MultiSortCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<SortColumnConfiguration> sortColumns) =>
        new() { Data = data, SortColumns = sortColumns };

    internal static MultiSortCommand WithContext(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<SortColumnConfiguration> sortColumns,
        SortContext context) =>
        new() { Data = data, SortColumns = sortColumns, Context = context };

    internal static MultiSortCommand WithPerformanceOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<SortColumnConfiguration> sortColumns) =>
        new()
        {
            Data = data,
            SortColumns = sortColumns,
            EnableParallelProcessing = true,
            PerformanceMode = SortPerformanceMode.Optimized
        };
}

/// <summary>
/// COMMAND PATTERN: Advanced sort command with business rules
/// ENTERPRISE: Complex sorting with configuration-based behavior
/// </summary>
internal sealed record AdvancedSortCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required AdvancedSortConfiguration SortConfiguration { get; init; }
    internal bool EnableParallelProcessing { get; init; } = true;
    internal bool UseSmartOptimization { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<SortProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;
    internal SortContext? Context { get; init; }

    // FLEXIBLE factory methods
    internal static AdvancedSortCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSortConfiguration sortConfiguration) =>
        new() { Data = data, SortConfiguration = sortConfiguration };

    internal static AdvancedSortCommand WithOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSortConfiguration sortConfiguration,
        bool useSmartOptimization = true) =>
        new()
        {
            Data = data,
            SortConfiguration = sortConfiguration,
            UseSmartOptimization = useSmartOptimization,
            EnableParallelProcessing = true
        };
}

#endregion

#region Result Objects

/// <summary>
/// ENTERPRISE: Sort operation result with comprehensive statistics
/// CONSISTENT: Rovnaká štruktúra ako FilterResult a ValidationResult
/// </summary>
internal sealed record SortResult
{
    internal bool Success { get; init; }
    internal IReadOnlyList<IReadOnlyDictionary<string, object?>> SortedData { get; init; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
    internal IReadOnlyList<SortColumnConfiguration> AppliedSorts { get; init; } = Array.Empty<SortColumnConfiguration>();
    internal int ProcessedRows { get; init; }
    internal TimeSpan SortTime { get; init; }
    internal SortPerformanceMode UsedPerformanceMode { get; init; }
    internal bool UsedParallelProcessing { get; init; }
    internal bool UsedStableSort { get; init; }
    internal IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();
    internal SortStatistics Statistics { get; init; } = new();

    internal static SortResult CreateSuccess(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> sortedData,
        IReadOnlyList<SortColumnConfiguration> appliedSorts,
        TimeSpan sortTime,
        SortPerformanceMode usedMode = SortPerformanceMode.Auto,
        bool usedParallel = false,
        bool usedStable = true) =>
        new()
        {
            Success = true,
            SortedData = sortedData,
            AppliedSorts = appliedSorts,
            ProcessedRows = sortedData.Count,
            SortTime = sortTime,
            UsedPerformanceMode = usedMode,
            UsedParallelProcessing = usedParallel,
            UsedStableSort = usedStable
        };

    internal static SortResult CreateFailure(
        IReadOnlyList<string> errors,
        TimeSpan sortTime) =>
        new()
        {
            Success = false,
            ErrorMessages = errors,
            SortTime = sortTime
        };

    internal static SortResult Empty => new();
}

/// <summary>
/// ENTERPRISE: Sort execution statistics
/// PERFORMANCE: Monitoring and optimization metrics
/// </summary>
internal sealed record SortStatistics
{
    internal int TotalComparisons { get; init; }
    internal int TotalSwaps { get; init; }
    internal TimeSpan AverageSortTime { get; init; }
    internal bool UsedParallelProcessing { get; init; }
    internal bool UsedObjectPooling { get; init; }
    internal int ObjectPoolHits { get; init; }
    internal int CacheHits { get; init; }
    internal double ComparisonsPerSecond { get; init; }
    internal SortPerformanceMode SelectedMode { get; init; }
}

#endregion
```

## 🎯 FACADE API METÓDY

### Základné Sort API (Consistent s existujúcimi metódami)

```csharp
#region Sort Operations with Command Pattern

/// <summary>
/// PUBLIC API: Single column sort using command pattern
/// ENTERPRISE: Professional sorting with progress tracking
/// CONSISTENT: Rovnaká štruktúra ako ImportAsync a ValidateAsync
/// </summary>
Task<SortResult> SortAsync(
    SortCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Multi-column sort with comprehensive configuration
/// ENTERPRISE: Complex sorting scenarios with performance optimization
/// LINQ OPTIMIZED: Parallel processing with stable sorting algorithms
/// </summary>
Task<SortResult> MultiSortAsync(
    MultiSortCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Advanced sort with business rules
/// ENTERPRISE: Complex business logic with hierarchical sorting
/// SUPPORTS: Employee hierarchy, customer priority, custom business rules
/// </summary>
Task<SortResult> AdvancedSortAsync(
    AdvancedSortCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Quick sort for immediate results
/// PERFORMANCE: Optimized for small datasets and simple criteria
/// </summary>
SortResult QuickSort(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    string columnName,
    SortDirection direction = SortDirection.Ascending);

#endregion

#region Sort Validation and Utilities

/// <summary>
/// PUBLIC API: Validate sort configuration
/// ENTERPRISE: Comprehensive sort validation
/// </summary>
Task<Result<bool>> ValidateSortConfigurationAsync(AdvancedSortConfiguration sortConfiguration);

/// <summary>
/// PUBLIC API: Get sortable columns
/// DYNAMIC: Automatically discovers sortable columns from data
/// </summary>
IReadOnlyList<string> GetSortableColumns(
    IEnumerable<IReadOnlyDictionary<string, object?>> data);

/// <summary>
/// PUBLIC API: Get optimal sort performance mode
/// SMART: Recommends performance mode based on data characteristics
/// </summary>
SortPerformanceMode GetRecommendedPerformanceMode(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    IReadOnlyList<SortColumnConfiguration> sortColumns);

/// <summary>
/// PUBLIC API: Analyze sort complexity
/// PERFORMANCE: Estimates sort operation complexity and duration
/// </summary>
SortComplexityAnalysis AnalyzeSortComplexity(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    IReadOnlyList<SortColumnConfiguration> sortColumns);

#endregion
```

## 🔧 COMPLEX BUSINESS LOGIC EXAMPLES

### Employee Hierarchy Sorting

```csharp
// BUSINESS RULE: Department → Position → Salary (DESC) → Hire Date
var employeeSort = AdvancedSortConfiguration.CreateEmployeeHierarchy(
    departmentColumn: "Department",
    positionColumn: "Position",
    salaryColumn: "Salary"
);

// Pridanie dodatočných critérií
var extendedEmployeeSort = employeeSort with
{
    SortColumns = employeeSort.SortColumns.Concat(new[]
    {
        SortColumnConfiguration.Create("HireDate", SortDirection.Ascending, 3)
    }).ToArray(),
    PerformanceMode = SortPerformanceMode.Optimized
};

var command = AdvancedSortCommand.Create(employeeData, extendedEmployeeSort);
var result = await facade.AdvancedSortAsync(command);
```

### Custom Comparer s Business Logic

```csharp
// CUSTOM BUSINESS LOGIC: Priority-based customer sorting
var customerPriorityComparer = (object? value1, object? value2) =>
{
    var tier1 = value1?.ToString();
    var tier2 = value2?.ToString();

    var priorities = new Dictionary<string, int>
    {
        ["Platinum"] = 1, ["Gold"] = 2, ["Silver"] = 3, ["Bronze"] = 4
    };

    var priority1 = priorities.GetValueOrDefault(tier1 ?? "", int.MaxValue);
    var priority2 = priorities.GetValueOrDefault(tier2 ?? "", int.MaxValue);

    return priority1.CompareTo(priority2);
};

var customerSort = new AdvancedSortConfiguration
{
    ConfigurationName = "CustomerPriority",
    SortColumns = new[]
    {
        SortColumnConfiguration.WithCustomComparer("CustomerTier", SortDirection.Ascending, customerPriorityComparer, 0),
        SortColumnConfiguration.Create("TotalSpent", SortDirection.Descending, 1),
        SortColumnConfiguration.Create("JoinDate", SortDirection.Ascending, 2)
    },
    PerformanceMode = SortPerformanceMode.Optimized,
    Stability = SortStability.Stable
};
```

### Dynamic Performance Optimization

```csharp
// SMART PERFORMANCE: Automatic mode selection based on data characteristics
var smartSort = new AdvancedSortConfiguration
{
    ConfigurationName = "SmartPerformanceSort",
    SortColumns = sortColumns,
    PerformanceMode = SortPerformanceMode.Auto, // Automatic selection
    CustomSortKey = (row, context) =>
    {
        // Custom sort key generation s DI support
        var performanceService = context.ServiceProvider?.GetService<IPerformanceOptimizationService>();
        return performanceService?.GenerateOptimalSortKey(row) ?? row.Values.FirstOrDefault();
    }
};

var command = AdvancedSortCommand.WithOptimization(data, smartSort, useSmartOptimization: true);
var result = await facade.AdvancedSortAsync(command);
```

## ⚡ PERFORMANCE & LINQ OPTIMIZATIONS

### Parallel Processing s Smart Algorithm Selection

```csharp
// LINQ optimized sort service implementation
internal sealed class AdvancedSortService
{
    private const int ParallelProcessingThreshold = 1000;

    public async Task<SortResult> SortAsync(SortCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataList = command.Data.ToList();

        // PERFORMANCE: Choose optimal sorting strategy
        var sortStrategy = DetermineOptimalStrategy(dataList, command);
        var sortedData = sortStrategy switch
        {
            SortStrategy.QuickSort => dataList.OrderBy(row => GetSortValue(row, command.ColumnName)),
            SortStrategy.ParallelQuickSort => dataList
                .AsParallel()
                .WithCancellation(command.cancellationToken)
                .OrderBy(row => GetSortValue(row, command.ColumnName)),
            SortStrategy.StableSort => dataList.OrderBy(row => GetSortValue(row, command.ColumnName)),
            SortStrategy.CustomOptimized => ApplyCustomOptimizedSort(dataList, command),
            _ => dataList.OrderBy(row => GetSortValue(row, command.ColumnName))
        };

        var resultList = sortedData.ToList();
        stopwatch.Stop();

        // LOGGING: Performance metrics
        _sortLogger.LogSortOperation("SingleColumnSort", command.ColumnName,
            dataList.Count, resultList.Count, stopwatch.Elapsed);

        return SortResult.CreateSuccess(
            resultList,
            new[] { SortColumnConfiguration.Create(command.ColumnName, command.Direction) },
            stopwatch.Elapsed,
            command.PerformanceMode,
            usedParallel: command.EnableParallelProcessing && dataList.Count > ParallelProcessingThreshold);
    }

    // PERFORMANCE: Multi-column sort with LINQ chain optimization
    public async Task<SortResult> MultiSortAsync(MultiSortCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataList = command.Data.ToList();

        // LINQ OPTIMIZATION: Build efficient ordering chain
        IOrderedEnumerable<IReadOnlyDictionary<string, object?>> orderedData = null;

        var enabledSorts = command.SortColumns
            .Where(s => s.IsEnabled && s.Direction != SortDirection.None)
            .OrderBy(s => s.Priority);

        foreach (var sortConfig in enabledSorts)
        {
            if (orderedData == null)
            {
                // Primary sort
                orderedData = sortConfig.Direction == SortDirection.Ascending
                    ? (command.EnableParallelProcessing && dataList.Count > ParallelProcessingThreshold
                        ? dataList.AsParallel().OrderBy(row => GetSortValue(row, sortConfig))
                        : dataList.OrderBy(row => GetSortValue(row, sortConfig)))
                    : (command.EnableParallelProcessing && dataList.Count > ParallelProcessingThreshold
                        ? dataList.AsParallel().OrderByDescending(row => GetSortValue(row, sortConfig))
                        : dataList.OrderByDescending(row => GetSortValue(row, sortConfig)));
            }
            else
            {
                // Secondary sorts (ThenBy chain)
                orderedData = sortConfig.Direction == SortDirection.Ascending
                    ? orderedData.ThenBy(row => GetSortValue(row, sortConfig))
                    : orderedData.ThenByDescending(row => GetSortValue(row, sortConfig));
            }
        }

        var resultList = orderedData?.ToList() ?? dataList;
        stopwatch.Stop();

        return SortResult.CreateSuccess(
            resultList,
            command.SortColumns.ToList(),
            stopwatch.Elapsed,
            command.PerformanceMode,
            usedParallel: command.EnableParallelProcessing);
    }

    // PERFORMANCE: Smart algorithm selection
    private SortStrategy DetermineOptimalStrategy(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        SortCommand command)
    {
        var dataSize = data.Count;
        var performanceMode = command.PerformanceMode;

        return performanceMode switch
        {
            SortPerformanceMode.Auto => dataSize switch
            {
                < 100 => SortStrategy.QuickSort,
                < ParallelProcessingThreshold => SortStrategy.StableSort,
                _ => SortStrategy.ParallelQuickSort
            },
            SortPerformanceMode.Sequential => SortStrategy.QuickSort,
            SortPerformanceMode.Parallel => SortStrategy.ParallelQuickSort,
            SortPerformanceMode.Optimized => SortStrategy.CustomOptimized,
            _ => SortStrategy.QuickSort
        };
    }
}

internal enum SortStrategy
{
    QuickSort,
    ParallelQuickSort,
    StableSort,
    CustomOptimized
}
```

## 🎯 KĽÚČOVÉ VYLEPŠENIA & ROZŠÍRENIA

### 1. **Enhanced Performance Modes**
- **Auto**: Intelligent selection based on data characteristics
- **Sequential**: Single-threaded for small datasets
- **Parallel**: Multi-threaded for large datasets
- **Optimized**: Advanced optimizations with caching and object pooling

### 2. **Enterprise Business Rules**
- **Flexible Configuration**: Nie hardcoded factory methods
- **Multi-Level Sorting**: Hierarchical sorting s custom comparers
- **Custom Sort Keys**: DI-enabled custom sort key generation
- **Business Templates**: Built-in templates pre common scenarios

### 3. **Advanced LINQ Optimizations**
- **Parallel Processing**: AsParallel() pre veľké datasets (>1000 rows)
- **Stable Sorting**: Preserves relative order of equal elements
- **Chain Optimization**: Efficient OrderBy → ThenBy chains
- **Lazy Evaluation**: Deferred execution pre streaming scenarios

### 4. **Consistent Architecture**
- **Command Pattern**: Rovnaká štruktúra ako Import/Export a Validation
- **Clean Architecture**: Core → Application → Infrastructure layers
- **Hybrid DI**: Internal DI s functional programming support
- **Thread Safety**: Immutable commands a atomic operations

### 5. **Comprehensive Monitoring**
- **Performance Metrics**: Detailed statistics a timing information
- **Progress Reporting**: Real-time progress s estimated completion
- **Error Handling**: Graceful degradation s comprehensive logging
- **Smart Recommendations**: Performance mode suggestions

## **🔍 LOGGING ŠPECIFIKÁCIA PRE SORT OPERÁCIE**

### **Internal DI Registration & Service Distribution**
Všetky sort logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a injektované do `SortService` cez internal DI systém:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<ISortLogger<SortService>, SortLogger<SortService>>();
services.AddSingleton<IOperationLogger<SortService>, OperationLogger<SortService>>();
services.AddSingleton<ICommandLogger<SortService>, CommandLogger<SortService>>();

// V SortService constructor
public SortService(
    ILogger<SortService> logger,
    ISortLogger<SortService> sortLogger,
    IOperationLogger<SortService> operationLogger,
    ICommandLogger<SortService> commandLogger)
```

### **Command Pattern Sort Logging**
Sort systém implementuje pokročilé logovanie pre všetky typy sort operácií vrátane performance optimizations a business rules.

### **Basic Sort Operations Logging**
```csharp
// Single column sort
_sortLogger.LogSortOperation("SingleColumnSort", columnName,
    originalRowCount, sortedRowCount, sortDuration);

_logger.LogInformation("Sort applied: column='{ColumnName}' direction={Direction} rows={RowCount} duration={Duration}ms",
    columnName, direction, sortedRowCount, sortDuration.TotalMilliseconds);

// Multi-column sort
_sortLogger.LogMultiColumnSort(command.SortColumns.Count,
    command.PerformanceMode, command.EnableParallelProcessing);

_logger.LogInformation("Multi-column sort: {ColumnCount} columns, mode={PerformanceMode}, parallel={Parallel}, rows={RowCount}",
    command.SortColumns.Count, command.PerformanceMode, command.EnableParallelProcessing, result.ProcessedRows);
```

### **Advanced Sort & Business Rules Logging**
```csharp
// Business rule execution logging
_sortLogger.LogBusinessRuleSorting(advancedConfig.ConfigurationName,
    advancedConfig.SortColumns.Count, success: result.Success, executionDuration);

_logger.LogInformation("Business rule sort '{ConfigName}': columns={ColumnCount}, stability={Stability}, success={Success}",
    advancedConfig.ConfigurationName, advancedConfig.SortColumns.Count,
    advancedConfig.Stability, result.Success);

// Custom comparer execution
if (sortConfig.CustomComparer != null)
{
    _sortLogger.LogCustomComparerExecution(sortConfig.ColumnName,
        comparerResult, comparerDuration, comparerError);

    _logger.LogInformation("Custom comparer executed for '{ColumnName}': success={Success}, duration={Duration}ms",
        sortConfig.ColumnName, comparerResult, comparerDuration.TotalMilliseconds);
}
```

### **Performance & LINQ Optimization Logging**
```csharp
// LINQ parallel processing logging
_logger.LogInformation("LINQ sort processing: parallel={UseParallel}, partitions={PartitionCount}, rows={RowCount}, time={Duration}ms",
    enableParallelProcessing, partitionCount, totalRows, processingTime.TotalMilliseconds);

// Performance mode selection logging
_sortLogger.LogPerformanceModeSelection(selectedMode, dataCharacteristics, selectionReason);

_logger.LogInformation("Sort performance mode selected: {SelectedMode} for {RowCount} rows, reason: {Reason}",
    selectedMode, rowCount, selectionReason);

// Algorithm optimization logging
_sortLogger.LogSortAlgorithmOptimization(usedAlgorithm, comparisons, swaps, duration);

if (sortDuration > PerformanceThresholds.SortWarningThreshold)
{
    _logger.LogWarning("Sort performance warning: {Duration}ms for {RowCount} rows with {ColumnCount} columns",
        sortDuration.TotalMilliseconds, rowCount, columnCount);
}
```

### **Sort Validation & Recommendations Logging**
```csharp
// Sort configuration validation
_sortLogger.LogSortValidation(configurationName,
    isValid: validationResult.IsValid, errorMessage: validationResult.ErrorMessage);

// Performance recommendations
_logger.LogInformation("Sort recommendations: mode={RecommendedMode} for {DataType} data with {RowCount} rows",
    recommendedMode, detectedDataType, rowCount);

// Sortable columns discovery
_logger.LogInformation("Sortable columns discovered: {ColumnCount} columns, types: {DataTypes}",
    sortableColumns.Count, string.Join(",", detectedTypes));
```

### **Logging Levels Usage:**
- **Information**: Sort executions, business rule success, performance metrics, LINQ optimizations
- **Warning**: Performance degradation, large dataset processing, complex sort warnings
- **Error**: Sort validation failures, business rule execution errors, custom comparer failures
- **Critical**: Sort system failures, memory exhaustion during large operations

---

## 🧠 CORE SORTING ALGORITHMS & IMPLEMENTATIONS

### **SortingAlgorithms.cs** - Core Layer Infrastructure

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Algorithms;

/// <summary>
/// ENTERPRISE: High-performance sorting algorithms with LINQ optimizations
/// THREAD SAFE: All algorithms support parallel processing and stable sorting
/// PERFORMANCE: Object pooling, minimal allocations, adaptive algorithm selection
/// </summary>
internal static class SortingAlgorithms
{
    private const int ParallelProcessingThreshold = 1000;
    private const int SmallDatasetThreshold = 100;

    #region Single Column Sorting Algorithms

    /// <summary>
    /// LINQ OPTIMIZED: Single column sort with intelligent algorithm selection
    /// PERFORMANCE: Adaptive algorithm based on data size and characteristics
    /// </summary>
    internal static IOrderedEnumerable<IReadOnlyDictionary<string, object?>> ApplySingleColumnSort(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        SortColumnConfiguration sortConfig,
        SortPerformanceMode performanceMode = SortPerformanceMode.Auto)
    {
        var dataList = data as IReadOnlyList<IReadOnlyDictionary<string, object?>> ?? data.ToList();
        var algorithm = SelectOptimalAlgorithm(dataList.Count, performanceMode);

        return algorithm switch
        {
            SortAlgorithm.QuickSort => ApplyQuickSort(dataList, sortConfig),
            SortAlgorithm.ParallelQuickSort => ApplyParallelQuickSort(dataList, sortConfig),
            SortAlgorithm.StableSort => ApplyStableSort(dataList, sortConfig),
            SortAlgorithm.OptimizedSort => ApplyOptimizedSort(dataList, sortConfig),
            _ => ApplyQuickSort(dataList, sortConfig)
        };
    }

    /// <summary>
    /// PERFORMANCE: Quick sort for small to medium datasets
    /// COMPLEXITY: O(n log n) average case
    /// </summary>
    private static IOrderedEnumerable<IReadOnlyDictionary<string, object?>> ApplyQuickSort(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        SortColumnConfiguration sortConfig)
    {
        return sortConfig.Direction == SortDirection.Ascending
            ? data.OrderBy(row => GetSortValue(row, sortConfig))
            : data.OrderByDescending(row => GetSortValue(row, sortConfig));
    }

    /// <summary>
    /// PERFORMANCE: Parallel quick sort for large datasets
    /// LINQ OPTIMIZED: Uses PLINQ for automatic parallelization
    /// </summary>
    private static IOrderedEnumerable<IReadOnlyDictionary<string, object?>> ApplyParallelQuickSort(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        SortColumnConfiguration sortConfig)
    {
        var parallelQuery = data.AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount);

        return sortConfig.Direction == SortDirection.Ascending
            ? parallelQuery.OrderBy(row => GetSortValue(row, sortConfig))
            : parallelQuery.OrderByDescending(row => GetSortValue(row, sortConfig));
    }

    /// <summary>
    /// ENTERPRISE: Stable sort that preserves relative order of equal elements
    /// PERFORMANCE: Uses .NET's stable sorting implementation
    /// </summary>
    private static IOrderedEnumerable<IReadOnlyDictionary<string, object?>> ApplyStableSort(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        SortColumnConfiguration sortConfig)
    {
        // LINQ OrderBy is already stable, but we add explicit indexing for guarantee
        var indexedData = data.Select((row, index) => new { row, index });

        var sortedData = sortConfig.Direction == SortDirection.Ascending
            ? indexedData.OrderBy(x => GetSortValue(x.row, sortConfig)).ThenBy(x => x.index)
            : indexedData.OrderByDescending(x => GetSortValue(x.row, sortConfig)).ThenBy(x => x.index);

        return sortedData.Select(x => x.row).Cast<IReadOnlyDictionary<string, object?>>().OrderBy(_ => 0);
    }

    /// <summary>
    /// ENTERPRISE: Optimized sort with caching and object pooling
    /// PERFORMANCE: Pre-computes sort keys for better performance
    /// </summary>
    private static IOrderedEnumerable<IReadOnlyDictionary<string, object?>> ApplyOptimizedSort(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        SortColumnConfiguration sortConfig)
    {
        // Pre-compute sort keys to avoid repeated value extraction
        var keyValuePairs = data
            .AsParallel()
            .Select(row => new KeyValuePair<object?, IReadOnlyDictionary<string, object?>>(
                GetSortValue(row, sortConfig), row))
            .ToArray();

        var sortedPairs = sortConfig.Direction == SortDirection.Ascending
            ? keyValuePairs.OrderBy(kvp => kvp.Key, GetOptimizedComparer(sortConfig))
            : keyValuePairs.OrderByDescending(kvp => kvp.Key, GetOptimizedComparer(sortConfig));

        return sortedPairs.Select(kvp => kvp.Value).Cast<IReadOnlyDictionary<string, object?>>().OrderBy(_ => 0);
    }

    #endregion

    #region Multi-Column Sorting Algorithms

    /// <summary>
    /// LINQ OPTIMIZED: Multi-column sort with efficient ThenBy chains
    /// PERFORMANCE: Builds optimal OrderBy → ThenBy chains for multiple columns
    /// </summary>
    internal static IOrderedEnumerable<IReadOnlyDictionary<string, object?>> ApplyMultiColumnSort(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<SortColumnConfiguration> sortColumns,
        bool enableParallel = true,
        SortStability stability = SortStability.Stable)
    {
        var dataList = data as IReadOnlyList<IReadOnlyDictionary<string, object?>> ?? data.ToList();
        var enabledSorts = sortColumns.Where(s => s.IsEnabled).OrderBy(s => s.Priority).ToList();

        if (!enabledSorts.Any())
            return dataList.OrderBy(_ => 0);

        // Start with primary sort
        var primarySort = enabledSorts.First();
        IOrderedEnumerable<IReadOnlyDictionary<string, object?>> orderedData;

        if (enableParallel && dataList.Count > ParallelProcessingThreshold)
        {
            orderedData = primarySort.Direction == SortDirection.Ascending
                ? dataList.AsParallel().OrderBy(row => GetSortValue(row, primarySort))
                : dataList.AsParallel().OrderByDescending(row => GetSortValue(row, primarySort));
        }
        else
        {
            orderedData = primarySort.Direction == SortDirection.Ascending
                ? dataList.OrderBy(row => GetSortValue(row, primarySort))
                : dataList.OrderByDescending(row => GetSortValue(row, primarySort));
        }

        // Apply secondary sorts with ThenBy chain
        foreach (var sortConfig in enabledSorts.Skip(1))
        {
            orderedData = sortConfig.Direction == SortDirection.Ascending
                ? orderedData.ThenBy(row => GetSortValue(row, sortConfig))
                : orderedData.ThenByDescending(row => GetSortValue(row, sortConfig));
        }

        // Apply stability guarantee if required
        if (stability == SortStability.Stable)
        {
            var indexedResult = orderedData
                .Select((row, index) => new { row, originalIndex = dataList.ToList().IndexOf(row) })
                .OrderBy(x => x.originalIndex);
            return indexedResult.Select(x => x.row).Cast<IReadOnlyDictionary<string, object?>>().OrderBy(_ => 0);
        }

        return orderedData;
    }

    /// <summary>
    /// ENTERPRISE: Advanced multi-column sort with business rule support
    /// PERFORMANCE: Optimized for complex business scenarios
    /// </summary>
    internal static async Task<IOrderedEnumerable<IReadOnlyDictionary<string, object?>>> ApplyAdvancedMultiColumnSortAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSortConfiguration sortConfiguration,
        SortContext context)
    {
        var dataList = data as IReadOnlyList<IReadOnlyDictionary<string, object?>> ?? data.ToList();

        // Apply custom sort key generation if available
        if (sortConfiguration.CustomSortKey != null)
        {
            var keyedData = await ApplyCustomSortKeyAsync(dataList, sortConfiguration, context);
            return keyedData.OrderBy(x => x.sortKey).Select(x => x.row)
                .Cast<IReadOnlyDictionary<string, object?>>().OrderBy(_ => 0);
        }

        return dataList.OrderBy(_ => 0);
    }
```

## 🧮 CORE SORTING ALGORITHMS INFRASTRUCTURE

### **SortAlgorithms.cs** - Pure Functional Sorting Engine

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Utilities;

/// <summary>
/// ENTERPRISE: Pure functional sorting algorithms for maximum performance and testability
/// FUNCTIONAL PARADIGM: Stateless algorithms without side effects
/// HYBRID APPROACH: Functional algorithms within OOP service architecture
/// THREAD SAFE: Immutable functions suitable for concurrent execution
/// </summary>
internal static class SortAlgorithms
{
    /// <summary>
    /// PURE FUNCTION: Extract sortable value with intelligent type conversion
    /// PERFORMANCE: Optimized type handling for data grid scenarios
    /// TYPE COERCION: Automatic conversion for mixed-type data
    /// </summary>
    public static object? GetSortValue(IReadOnlyDictionary<string, object?> row, string columnName)
    {
        if (row == null) throw new ArgumentNullException(nameof(row));
        if (string.IsNullOrEmpty(columnName)) throw new ArgumentException("Column name cannot be null or empty", nameof(columnName));

        if (!row.TryGetValue(columnName, out var value))
            return null;

        // Handle null values - they should sort to the end
        if (value == null)
            return null;

        // Return comparable types as-is for optimal performance
        if (value is IComparable)
            return value;

        // For string values, try intelligent type conversion
        if (value is string stringValue)
        {
            return ConvertStringToComparableType(stringValue);
        }

        // Return original value for custom IComparable implementations
        return value;
    }

    /// <summary>
    /// PURE FUNCTION: Generate multi-column sort keys for complex sorting
    /// ENTERPRISE: Optimized for large dataset multi-column scenarios
    /// PERFORMANCE: Efficient key generation with minimal allocations
    /// </summary>
    public static IReadOnlyList<object?> GenerateSortKeys(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<string> columnNames)
    {
        if (row == null) throw new ArgumentNullException(nameof(row));
        if (columnNames == null) throw new ArgumentNullException(nameof(columnNames));

        var keys = new object?[columnNames.Count];
        for (int i = 0; i < columnNames.Count; i++)
        {
            keys[i] = GetSortValue(row, columnNames[i]);
        }
        return keys;
    }

    /// <summary>
    /// PURE FUNCTION: Compare sort key arrays with direction support
    /// FUNCTIONAL: Stateless comparison with early termination optimization
    /// PERFORMANCE: Efficient multi-column comparison logic
    /// </summary>
    public static int CompareSortKeys(
        IReadOnlyList<object?> keys1,
        IReadOnlyList<object?> keys2,
        IReadOnlyList<bool> ascendingDirections)
    {
        if (keys1 == null) throw new ArgumentNullException(nameof(keys1));
        if (keys2 == null) throw new ArgumentNullException(nameof(keys2));
        if (ascendingDirections == null) throw new ArgumentNullException(nameof(ascendingDirections));

        var minLength = Math.Min(Math.Min(keys1.Count, keys2.Count), ascendingDirections.Count);

        for (int i = 0; i < minLength; i++)
        {
            var comparison = CompareValues(keys1[i], keys2[i]);
            if (comparison != 0)
            {
                return ascendingDirections[i] ? comparison : -comparison;
            }
        }

        return 0; // All compared keys are equal
    }

    /// <summary>
    /// PURE FUNCTION: Intelligent sortability detection for dynamic data
    /// DATA ANALYSIS: Sample-based sortability assessment
    /// PERFORMANCE: Limited sampling for large datasets
    /// </summary>
    public static bool IsColumnSortable(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrEmpty(columnName)) return false;

        // Check if column exists and has comparable values
        var sampleValues = data
            .Take(100) // Sample first 100 rows for performance
            .Select(row => row.TryGetValue(columnName, out var value) ? value : null)
            .Where(value => value != null)
            .Take(10) // Only need a few non-null samples
            .ToList();

        if (!sampleValues.Any()) return false;

        // Check if values are comparable
        return sampleValues.All(value =>
            value is IComparable ||
            (value is string str && CanConvertToComparableType(str)));
    }

    /// <summary>
    /// PURE FUNCTION: Detect optimal sort data type for performance optimization
    /// TYPE ANALYSIS: Statistical type detection from data samples
    /// ENTERPRISE: Intelligent type detection for sorting strategy selection
    /// </summary>
    public static Type? DetectSortDataType(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrEmpty(columnName)) return null;

        var sampleValues = data
            .Take(100)
            .Select(row => row.TryGetValue(columnName, out var value) ? value : null)
            .Where(value => value != null)
            .Take(20)
            .ToList();

        if (!sampleValues.Any()) return typeof(string);

        // Count type occurrences
        var typeCounts = sampleValues
            .GroupBy(value => GetEffectiveType(value))
            .ToDictionary(g => g.Key, g => g.Count());

        // Return most common type
        return typeCounts.OrderByDescending(kvp => kvp.Value).First().Key;
    }

    /// <summary>
    /// PURE FUNCTION: Type-aware value comparison with null-safe logic
    /// TYPE COERCION: Automatic type coercion for mixed-type comparisons
    /// PERFORMANCE: Optimized comparison paths for common scenarios
    /// </summary>
    private static int CompareValues(object? value1, object? value2)
    {
        // Handle null cases - nulls sort to end
        if (value1 == null && value2 == null) return 0;
        if (value1 == null) return 1; // null sorts after non-null
        if (value2 == null) return -1; // non-null sorts before null

        // Fast path for same type comparisons
        if (value1.GetType() == value2.GetType() && value1 is IComparable comparable1)
        {
            return comparable1.CompareTo(value2);
        }

        // Type coercion for mixed types
        if (TryCoerceToSameType(value1, value2, out var coerced1, out var coerced2))
        {
            if (coerced1 is IComparable coercedComparable)
            {
                return coercedComparable.CompareTo(coerced2);
            }
        }

        // Final fallback to string comparison
        var str1 = value1.ToString() ?? string.Empty;
        var str2 = value2.ToString() ?? string.Empty;
        return string.Compare(str1, str2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// PURE FUNCTION: Intelligent string to comparable type conversion
    /// TYPE CONVERSION: Performance-optimized conversion paths
    /// DATA GRID OPTIMIZED: Optimized for common data grid data types
    /// </summary>
    private static object ConvertStringToComparableType(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        // Try numeric conversions first (most common in data grids)
        if (double.TryParse(value, out var doubleValue))
            return doubleValue;

        // Try date conversion
        if (DateTime.TryParse(value, out var dateValue))
            return dateValue;

        // Try boolean conversion
        if (bool.TryParse(value, out var boolValue))
            return boolValue;

        // Return as string if no conversion possible
        return value;
    }

    /// <summary>
    /// PURE FUNCTION: Check convertibility to comparable type
    /// TYPE CHECKING: Exception-free type conversion checking
    /// </summary>
    private static bool CanConvertToComparableType(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;

        return double.TryParse(value, out _) ||
               DateTime.TryParse(value, out _) ||
               bool.TryParse(value, out _);
    }

    /// <summary>
    /// PURE FUNCTION: Get effective type for sorting purposes
    /// TYPE ANALYSIS: Determine best type for sorting optimization
    /// </summary>
    private static Type GetEffectiveType(object? value)
    {
        if (value == null) return typeof(object);

        if (value is string str)
        {
            // Return the type this string can be converted to
            if (double.TryParse(str, out _)) return typeof(double);
            if (DateTime.TryParse(str, out _)) return typeof(DateTime);
            if (bool.TryParse(str, out _)) return typeof(bool);
        }

        return value.GetType();
    }

    /// <summary>
    /// PURE FUNCTION: Type coercion for heterogeneous data
    /// TYPE COERCION: Safe type coercion without exceptions
    /// PERFORMANCE: Optimized coercion paths for common type combinations
    /// </summary>
    private static bool TryCoerceToSameType(object value1, object value2, out object? coerced1, out object? coerced2)
    {
        coerced1 = value1;
        coerced2 = value2;

        // Try numeric coercion
        if (TryConvertToDouble(value1, out var d1) && TryConvertToDouble(value2, out var d2))
        {
            coerced1 = d1;
            coerced2 = d2;
            return true;
        }

        // Try DateTime coercion
        if (TryConvertToDateTime(value1, out var dt1) && TryConvertToDateTime(value2, out var dt2))
        {
            coerced1 = dt1;
            coerced2 = dt2;
            return true;
        }

        return false;
    }

    /// <summary>
    /// PURE FUNCTION: Safe numeric conversion with comprehensive type support
    /// EXCEPTION-FREE: No exceptions thrown during conversion attempts
    /// </summary>
    private static bool TryConvertToDouble(object? value, out double result)
    {
        result = 0;

        return value switch
        {
            double d => (result = d) == d,
            float f => (result = f) == f,
            decimal dec => (result = (double)dec) == (double)dec,
            int i => (result = i) == i,
            long l => (result = l) == l,
            short s => (result = s) == s,
            byte b => (result = b) == b,
            string str => double.TryParse(str, out result),
            _ => false
        };
    }

    /// <summary>
    /// PURE FUNCTION: Safe DateTime conversion with format tolerance
    /// EXCEPTION-FREE: No exceptions thrown during conversion attempts
    /// </summary>
    private static bool TryConvertToDateTime(object? value, out DateTime result)
    {
        result = default;

        return value switch
        {
            DateTime dt => (result = dt) == dt,
            DateTimeOffset dto => (result = dto.DateTime) == dto.DateTime,
            string str => DateTime.TryParse(str, out result),
            _ => false
        };
    }
}
```

## 🎯 SORT ALGORITHMS INTEGRATION PATTERNS

### **Application Layer Integration**
```csharp
// SortService.cs - Integration with pure functional algorithms
internal sealed class SortService : ISortService
{
    public async Task<SortResult> SortAsync(SortCommand command, cancellationToken cancellationToken = default)
    {
        // Sortability validation using pure algorithms
        var isSortable = SortAlgorithms.IsColumnSortable(command.Data, command.ColumnName);
        if (!isSortable)
        {
            return SortResult.Failure($"Column '{command.ColumnName}' is not sortable");
        }

        // Optimal type detection for performance
        var detectedType = SortAlgorithms.DetectSortDataType(command.Data, command.ColumnName);
        _logger.LogInformation("Detected sort type: {DetectedType} for column {ColumnName}",
            detectedType?.Name ?? "Unknown", command.ColumnName);

        // Apply pure functional sorting
        var sortedData = command.Direction == SortDirection.Ascending
            ? command.Data.OrderBy(row => SortAlgorithms.GetSortValue(row, command.ColumnName))
            : command.Data.OrderByDescending(row => SortAlgorithms.GetSortValue(row, command.ColumnName));

        return SortResult.CreateSuccess(sortedData.ToList(), command);
    }

    public async Task<SortResult> MultiSortAsync(MultiSortCommand command, cancellationToken cancellationToken = default)
    {
        // Multi-column sorting using pure functional key generation
        var columnNames = command.SortColumns.Select(s => s.ColumnName).ToList();
        var ascendingDirections = command.SortColumns.Select(s => s.Direction == SortDirection.Ascending).ToList();

        var sortedData = command.Data.OrderBy(row =>
        {
            var keys = SortAlgorithms.GenerateSortKeys(row, columnNames);
            return keys;
        }, Comparer<IReadOnlyList<object?>>.Create((keys1, keys2) =>
            SortAlgorithms.CompareSortKeys(keys1, keys2, ascendingDirections)));

        return SortResult.CreateSuccess(sortedData.ToList(), command.SortColumns);
    }
}
        }

        // Standard multi-column sort
        return ApplyMultiColumnSort(
            dataList,
            sortConfiguration.SortColumns,
            sortConfiguration.EnableParallelProcessing,
            sortConfiguration.Stability);
    }

    /// <summary>
    /// ENTERPRISE: Custom sort key generation with DI support
    /// ASYNC: Supports async sort key generation
    /// </summary>
    private static async Task<IEnumerable<(object? sortKey, IReadOnlyDictionary<string, object?> row)>> ApplyCustomSortKeyAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> data,
        AdvancedSortConfiguration sortConfiguration,
        SortContext context)
    {
        var tasks = data.Select(async row =>
        {
            var sortKey = sortConfiguration.CustomSortKey!(row, context);
            return (sortKey, row);
        });

        return await Task.WhenAll(tasks);
    }

    #endregion

    #region Performance Optimization Helpers

    /// <summary>
    /// PERFORMANCE: Intelligent algorithm selection based on data characteristics
    /// </summary>
    private static SortAlgorithm SelectOptimalAlgorithm(int dataSize, SortPerformanceMode performanceMode)
    {
        return performanceMode switch
        {
            SortPerformanceMode.Auto => dataSize switch
            {
                < SmallDatasetThreshold => SortAlgorithm.QuickSort,
                < ParallelProcessingThreshold => SortAlgorithm.StableSort,
                _ => SortAlgorithm.ParallelQuickSort
            },
            SortPerformanceMode.Sequential => SortAlgorithm.QuickSort,
            SortPerformanceMode.Parallel => SortAlgorithm.ParallelQuickSort,
            SortPerformanceMode.Optimized => SortAlgorithm.OptimizedSort,
            _ => SortAlgorithm.QuickSort
        };
    }

    /// <summary>
    /// PERFORMANCE: Type-optimized sort value extraction
    /// </summary>
    private static object? GetSortValue(IReadOnlyDictionary<string, object?> row, SortColumnConfiguration sortConfig)
    {
        if (!row.TryGetValue(sortConfig.ColumnName, out var value))
            return sortConfig.NullsFirst ? null : new object(); // Special null handling

        // Custom comparer takes precedence
        if (sortConfig.CustomComparer != null)
            return value;

        // Handle null values
        if (value == null)
            return sortConfig.NullsFirst ? null : new object();

        // Case sensitivity for strings
        if (value is string stringValue && !sortConfig.CaseSensitive)
            return stringValue.ToUpperInvariant();

        return value;
    }

    /// <summary>
    /// PERFORMANCE: Optimized comparer factory for different data types
    /// </summary>
    private static IComparer<object?> GetOptimizedComparer(SortColumnConfiguration sortConfig)
    {
        if (sortConfig.CustomComparer != null)
            return Comparer<object?>.Create(sortConfig.CustomComparer);

        return Comparer<object?>.Create((x, y) =>
        {
            if (x == null && y == null) return 0;
            if (x == null) return sortConfig.NullsFirst ? -1 : 1;
            if (y == null) return sortConfig.NullsFirst ? 1 : -1;

            // Type-specific optimizations
            return (x, y) switch
            {
                (string s1, string s2) => string.Compare(s1, s2,
                    sortConfig.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase),
                (IComparable c1, IComparable c2) => c1.CompareTo(c2),
                _ => Comparer<object>.Default.Compare(x, y)
            };
        });
    }

    #endregion

    #region Algorithm Performance Tracking

    /// <summary>
    /// ENTERPRISE: Sort performance metrics for optimization analysis
    /// </summary>
    internal static SortStatistics TrackSortPerformance(
        int dataSize,
        TimeSpan sortTime,
        SortAlgorithm usedAlgorithm,
        bool usedParallel)
    {
        return new SortStatistics
        {
            TotalComparisons = EstimateComparisons(dataSize, usedAlgorithm),
            TotalSwaps = EstimateSwaps(dataSize, usedAlgorithm),
            AverageSortTime = sortTime,
            UsedParallelProcessing = usedParallel,
            ComparisonsPerSecond = EstimateComparisons(dataSize, usedAlgorithm) / sortTime.TotalSeconds,
            SelectedMode = MapAlgorithmToMode(usedAlgorithm)
        };
    }

    private static int EstimateComparisons(int dataSize, SortAlgorithm algorithm)
    {
        return algorithm switch
        {
            SortAlgorithm.QuickSort => (int)(dataSize * Math.Log2(dataSize)),
            SortAlgorithm.ParallelQuickSort => (int)(dataSize * Math.Log2(dataSize) / Environment.ProcessorCount),
            SortAlgorithm.StableSort => dataSize * dataSize / 4, // Conservative estimate
            SortAlgorithm.OptimizedSort => (int)(dataSize * Math.Log2(dataSize) * 0.8), // Optimized estimate
            _ => dataSize * dataSize / 2
        };
    }

    private static int EstimateSwaps(int dataSize, SortAlgorithm algorithm)
    {
        return algorithm switch
        {
            SortAlgorithm.QuickSort => dataSize / 2,
            SortAlgorithm.ParallelQuickSort => dataSize / (2 * Environment.ProcessorCount),
            SortAlgorithm.StableSort => dataSize / 3, // Stable sorts generally need fewer swaps
            SortAlgorithm.OptimizedSort => dataSize / 4, // Pre-computed keys reduce swaps
            _ => dataSize
        };
    }

    private static SortPerformanceMode MapAlgorithmToMode(SortAlgorithm algorithm)
    {
        return algorithm switch
        {
            SortAlgorithm.QuickSort => SortPerformanceMode.Sequential,
            SortAlgorithm.ParallelQuickSort => SortPerformanceMode.Parallel,
            SortAlgorithm.StableSort => SortPerformanceMode.Auto,
            SortAlgorithm.OptimizedSort => SortPerformanceMode.Optimized,
            _ => SortPerformanceMode.Auto
        };
    }

    #endregion
}

/// <summary>
/// ENTERPRISE: Sort algorithm enumeration for performance tracking
/// </summary>
internal enum SortAlgorithm
{
    QuickSort,
    ParallelQuickSort,
    StableSort,
    OptimizedSort
}
```

Táto špecifikácia poskytuje kompletný, enterprise-ready sort systém s pokročilou business logic, optimalizáciami a jednotnou architektúrou s ostatnými časťami komponentu.
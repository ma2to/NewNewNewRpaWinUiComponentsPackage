# KOMPLETNÁ ŠPECIFIKÁCIA: POKROČILÝ FILTER SYSTÉM S BUSINESS LOGIC

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY & ŠTRUKTÚRA

### Clean Architecture + Command Pattern (Jednotná s Import/Export a Validation)
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Command handlers, services (internal)
- **Core Layer**: Domain entities, filter rules, algorithms (internal)
- **Infrastructure Layer**: Performance monitoring, resilience patterns (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý filter má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové typy filtrov bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky filter definitions implementujú spoločné interface
- **Interface Segregation**: Špecializované interfaces pre rôzne typy filtrovania
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained (Jednotné s ostatnými časťami)
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable commands + encapsulated behavior
- **SOLID**: Single responsibility pre každý command type
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable commands, atomic operations
- **Internal DI Registration**: Všetky filter časti budú registrované v InternalServiceRegistration.cs

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

### 1. **FilterTypes.cs** - Core Layer

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

#region Enums

/// <summary>
/// ENTERPRISE: Filter operators supporting comprehensive business scenarios
/// EXTENDED: Pridané pokročilé operátory pre enterprise použitie
/// </summary>
internal enum FilterOperator
{
    // Basic comparison operators
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,

    // String operators
    Contains,
    NotContains,
    StartsWith,
    EndsWith,

    // Null checking
    IsNull,
    IsNotNull,
    IsEmpty,
    IsNotEmpty,

    // Collection operators - EXTENDED
    In,
    NotIn,
    Between,
    NotBetween,

    // Advanced operators - EXTENDED
    Regex,
    Custom,

    // Logical operators for grouping - EXTENDED
    And,
    Or,
    Not
}

/// <summary>
/// ENTERPRISE: Logic operators for complex filter combinations
/// EXTENDED: Pridané AndAlso a OrElse podľa požiadavky
/// </summary>
internal enum FilterLogicOperator
{
    And,         // Standard AND
    Or,          // Standard OR
    AndAlso,     // Short-circuit AND evaluation
    OrElse       // Short-circuit OR evaluation
}

/// <summary>
/// ENTERPRISE: Filter scope for targeting specific data subsets
/// </summary>
internal enum FilterScope
{
    AllData,
    VisibleData,
    SelectedData,
    FilteredData
}

/// <summary>
/// ENTERPRISE: Filter grouping types for complex business logic
/// </summary>
internal enum FilterGroupType
{
    Simple,      // Basic filter group
    Business,    // Business rule group
    Conditional, // Conditional filter group
    Hierarchical // Nested filter groups
}

#endregion

#region Progress & Context Types

/// <summary>
/// ENTERPRISE: Filter operation progress reporting
/// CONSISTENT: Rovnaká štruktúra ako ValidationProgress a ExportProgress
/// </summary>
internal sealed record FilterProgress
{
    internal int ProcessedRows { get; init; }
    internal int TotalRows { get; init; }
    internal double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    internal TimeSpan ElapsedTime { get; init; }
    internal string CurrentOperation { get; init; } = string.Empty;
    internal int AppliedFilters { get; init; }
    internal int MatchingRows { get; init; }
}

/// <summary>
/// ENTERPRISE: Filter execution context with DI support
/// HYBRID DI: Poskytuje services pre custom filter functions
/// </summary>
internal sealed record FilterContext
{
    internal IServiceProvider? ServiceProvider { get; init; }
    internal IReadOnlyDictionary<string, object?> GlobalParameters { get; init; } = new Dictionary<string, object?>();
    internal cancellationToken cancellationToken { get; init; } = default;
    internal DateTime ExecutionTime { get; init; } = DateTime.UtcNow;
}

#endregion

#region Core Filter Definitions

/// <summary>
/// DDD: Basic filter definition with immutable design
/// FUNCTIONAL: Immutable filter with functional composition support
/// FLEXIBLE: Nie hardcoded factory methods, ale flexible object creation
/// </summary>
internal sealed record FilterDefinition
{
    internal string ColumnName { get; init; } = string.Empty;
    internal FilterOperator Operator { get; init; } = FilterOperator.Equals;
    internal object? Value { get; init; }
    internal object? SecondValue { get; init; } // Pre Between operátor
    internal bool IsCaseSensitive { get; init; } = false;
    internal string? FilterName { get; init; }
    internal bool IsEnabled { get; init; } = true;

    // Grouping properties pre complex logic
    internal bool GroupStart { get; init; } = false;
    internal bool GroupEnd { get; init; } = false;
    internal FilterLogicOperator LogicOperator { get; init; } = FilterLogicOperator.And;

    // FLEXIBLE factory methods namiesto hardcoded
    internal static FilterDefinition Create(
        string columnName,
        FilterOperator filterOperator,
        object? value,
        bool caseSensitive = false) =>
        new()
        {
            ColumnName = columnName,
            Operator = filterOperator,
            Value = value,
            IsCaseSensitive = caseSensitive
        };

    internal static FilterDefinition WithGrouping(
        string columnName,
        FilterOperator filterOperator,
        object? value,
        bool groupStart = false,
        bool groupEnd = false,
        FilterLogicOperator logicOperator = FilterLogicOperator.And) =>
        new()
        {
            ColumnName = columnName,
            Operator = filterOperator,
            Value = value,
            GroupStart = groupStart,
            GroupEnd = groupEnd,
            LogicOperator = logicOperator
        };

    internal static FilterDefinition Between(
        string columnName,
        object minValue,
        object maxValue,
        bool caseSensitive = false) =>
        new()
        {
            ColumnName = columnName,
            Operator = FilterOperator.Between,
            Value = minValue,
            SecondValue = maxValue,
            IsCaseSensitive = caseSensitive
        };
}

/// <summary>
/// ENTERPRISE: Advanced filter with business rule support
/// COMPLEX LOGIC: Supports hierarchical grouping and business rules
/// DDD: Rich domain object s behavioral methods
/// </summary>
internal sealed record AdvancedFilterDefinition
{
    internal string FilterName { get; init; } = string.Empty;
    internal FilterGroupType GroupType { get; init; } = FilterGroupType.Simple;
    internal FilterLogicOperator RootOperator { get; init; } = FilterLogicOperator.And;
    internal IReadOnlyList<FilterDefinition> Filters { get; init; } = Array.Empty<FilterDefinition>();
    internal IReadOnlyList<AdvancedFilterDefinition> ChildGroups { get; init; } = Array.Empty<AdvancedFilterDefinition>();
    internal Func<IReadOnlyDictionary<string, object?>, FilterContext, bool>? CustomLogic { get; init; }
    internal bool IsEnabled { get; init; } = true;
    internal TimeSpan? ExecutionTimeout { get; init; }

    #region Business Rule Factories - FLEXIBLE nie hardcoded

    /// <summary>
    /// Create business rule filter for "Active Customer" criteria
    /// FLEXIBLE: Konfigurovateľné business rule
    /// </summary>
    internal static AdvancedFilterDefinition CreateActiveCustomerRule(
        DateTime? cutoffDate = null,
        string statusColumn = "Status",
        string lastOrderColumn = "LastOrderDate") =>
        new()
        {
            FilterName = "ActiveCustomer",
            GroupType = FilterGroupType.Business,
            RootOperator = FilterLogicOperator.And,
            Filters = new[]
            {
                FilterDefinition.Create(lastOrderColumn, FilterOperator.GreaterThan, cutoffDate ?? DateTime.Now.AddDays(-90)),
                FilterDefinition.Create(statusColumn, FilterOperator.Equals, "Active"),
                FilterDefinition.Create("IsDeleted", FilterOperator.NotEquals, true)
            }
        };

    /// <summary>
    /// Create business rule filter for "High Value Transaction"
    /// FLEXIBLE: Konfigurovateľné thresholds
    /// </summary>
    internal static AdvancedFilterDefinition CreateHighValueTransactionRule(
        decimal highThreshold = 1000m,
        decimal mediumThreshold = 500m,
        string amountColumn = "Amount",
        string tierColumn = "CustomerTier") =>
        new()
        {
            FilterName = "HighValueTransaction",
            GroupType = FilterGroupType.Business,
            RootOperator = FilterLogicOperator.Or,
            Filters = new[]
            {
                FilterDefinition.Create(amountColumn, FilterOperator.GreaterThan, highThreshold)
            },
            ChildGroups = new[]
            {
                new AdvancedFilterDefinition
                {
                    FilterName = "MediumValuePremium",
                    RootOperator = FilterLogicOperator.And,
                    Filters = new[]
                    {
                        FilterDefinition.Create(amountColumn, FilterOperator.GreaterThan, mediumThreshold),
                        FilterDefinition.Create(tierColumn, FilterOperator.Equals, "Premium")
                    }
                }
            }
        };

    /// <summary>
    /// Create flexible risk assessment filter
    /// FLEXIBLE: Konfigurovateľné risk criteria
    /// </summary>
    internal static AdvancedFilterDefinition CreateRiskAssessmentRule(
        string riskLevel,
        Dictionary<string, object>? customThresholds = null) =>
        riskLevel.ToUpperInvariant() switch
        {
            "LOW" => CreateLowRiskFilter(customThresholds),
            "MEDIUM" => CreateMediumRiskFilter(customThresholds),
            "HIGH" => CreateHighRiskFilter(customThresholds),
            _ => throw new ArgumentException($"Unknown risk level: {riskLevel}")
        };

    private static AdvancedFilterDefinition CreateLowRiskFilter(Dictionary<string, object>? thresholds) =>
        new()
        {
            FilterName = "LowRisk",
            GroupType = FilterGroupType.Business,
            RootOperator = FilterLogicOperator.And,
            Filters = new[]
            {
                FilterDefinition.Create("TransactionAmount", FilterOperator.LessThan,
                    thresholds?.GetValueOrDefault("MaxAmount") ?? 100m),
                FilterDefinition.Create("CustomerVerified", FilterOperator.Equals, true),
                FilterDefinition.Create("CustomerSSN", FilterOperator.IsNotNull, null)
            }
        };

    private static AdvancedFilterDefinition CreateMediumRiskFilter(Dictionary<string, object>? thresholds) =>
        new()
        {
            FilterName = "MediumRisk",
            GroupType = FilterGroupType.Business,
            RootOperator = FilterLogicOperator.And,
            Filters = new[]
            {
                FilterDefinition.Create("TransactionAmount", FilterOperator.Between,
                    100m, thresholds?.GetValueOrDefault("MaxAmount") ?? 10000m)
            },
            ChildGroups = new[]
            {
                new AdvancedFilterDefinition
                {
                    FilterName = "MediumRiskVerification",
                    RootOperator = FilterLogicOperator.Or,
                    Filters = new[]
                    {
                        FilterDefinition.Create("CustomerVerified", FilterOperator.Equals, true),
                        FilterDefinition.Create("HasPreviousTransactions", FilterOperator.Equals, true)
                    }
                }
            }
        };

    private static AdvancedFilterDefinition CreateHighRiskFilter(Dictionary<string, object>? thresholds) =>
        new()
        {
            FilterName = "HighRisk",
            GroupType = FilterGroupType.Business,
            RootOperator = FilterLogicOperator.Or,
            Filters = new[]
            {
                FilterDefinition.Create("TransactionAmount", FilterOperator.GreaterThan,
                    thresholds?.GetValueOrDefault("MinHighAmount") ?? 10000m),
                FilterDefinition.Create("CustomerVerified", FilterOperator.Equals, false),
                FilterDefinition.Create("CustomerSSN", FilterOperator.IsNull, null)
            }
        };

    #endregion

    #region Logical Combination Methods

    /// <summary>
    /// Combine filters with AND logic (including AndAlso)
    /// EXTENDED: Podporuje AndAlso short-circuit evaluation
    /// </summary>
    internal static AdvancedFilterDefinition And(
        params FilterDefinition[] filters) =>
        new()
        {
            FilterName = $"AndGroup_{Guid.NewGuid():N}",
            RootOperator = FilterLogicOperator.And,
            Filters = filters
        };

    /// <summary>
    /// Combine filters with OR logic (including OrElse)
    /// EXTENDED: Podporuje OrElse short-circuit evaluation
    /// </summary>
    internal static AdvancedFilterDefinition Or(
        params FilterDefinition[] filters) =>
        new()
        {
            FilterName = $"OrGroup_{Guid.NewGuid():N}",
            RootOperator = FilterLogicOperator.Or,
            Filters = filters
        };

    /// <summary>
    /// Combine filters with AndAlso short-circuit logic
    /// EXTENDED: Nová funkcionalita podľa požiadavky
    /// </summary>
    internal static AdvancedFilterDefinition AndAlso(
        params FilterDefinition[] filters) =>
        new()
        {
            FilterName = $"AndAlsoGroup_{Guid.NewGuid():N}",
            RootOperator = FilterLogicOperator.AndAlso,
            Filters = filters
        };

    /// <summary>
    /// Combine filters with OrElse short-circuit logic
    /// EXTENDED: Nová funkcionalita podľa požiadavky
    /// </summary>
    internal static AdvancedFilterDefinition OrElse(
        params FilterDefinition[] filters) =>
        new()
        {
            FilterName = $"OrElseGroup_{Guid.NewGuid():N}",
            RootOperator = FilterLogicOperator.OrElse,
            Filters = filters
        };

    /// <summary>
    /// Create grouped filter with explicit parentheses logic
    /// ENTERPRISE: Supports complex business rules with grouping
    /// </summary>
    internal static AdvancedFilterDefinition Group(
        string groupName,
        FilterLogicOperator logicOperator,
        params FilterDefinition[] filters) =>
        new()
        {
            FilterName = groupName,
            GroupType = FilterGroupType.Simple,
            RootOperator = logicOperator,
            Filters = filters.Select((f, i) => f with
            {
                GroupStart = i == 0,
                GroupEnd = i == filters.Length - 1
            }).ToArray()
        };

    #endregion
}

#endregion

#region Command Objects

/// <summary>
/// COMMAND PATTERN: Apply filter command with LINQ optimization
/// CONSISTENT: Rovnaká štruktúra ako ImportDataCommand a ValidateDataCommand
/// </summary>
internal sealed record ApplyFilterCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required FilterDefinition Filter { get; init; }
    internal FilterScope Scope { get; init; } = FilterScope.AllData;
    internal bool EnableParallelProcessing { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<FilterProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    // FLEXIBLE factory methods s LINQ optimization
    internal static ApplyFilterCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        FilterDefinition filter) =>
        new() { Data = data, Filter = filter };

    internal static ApplyFilterCommand WithScope(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        FilterDefinition filter,
        FilterScope scope) =>
        new() { Data = data, Filter = filter, Scope = scope };

    // LINQ optimized factory
    internal static ApplyFilterCommand WithLINQOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        FilterDefinition filter) =>
        new()
        {
            Data = data.AsParallel().Where(row => row.Values.Any(v => v != null)),
            Filter = filter,
            EnableParallelProcessing = true
        };
}

/// <summary>
/// COMMAND PATTERN: Apply multiple filters command
/// EXTENDED: Podporuje AndAlso a OrElse operátory
/// </summary>
internal sealed record ApplyFiltersCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required IReadOnlyList<FilterDefinition> Filters { get; init; }
    internal FilterLogicOperator LogicOperator { get; init; } = FilterLogicOperator.And;
    internal FilterScope Scope { get; init; } = FilterScope.AllData;
    internal bool EnableParallelProcessing { get; init; } = true;
    internal bool UseShortCircuitEvaluation { get; init; } = true; // Pre AndAlso/OrElse
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<FilterProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    // FLEXIBLE factory methods
    internal static ApplyFiltersCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<FilterDefinition> filters,
        FilterLogicOperator logicOperator = FilterLogicOperator.And) =>
        new() { Data = data, Filters = filters, LogicOperator = logicOperator };

    // EXTENDED: Podpora pre AndAlso
    internal static ApplyFiltersCommand WithAndAlso(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<FilterDefinition> filters) =>
        new()
        {
            Data = data,
            Filters = filters,
            LogicOperator = FilterLogicOperator.AndAlso,
            UseShortCircuitEvaluation = true
        };

    // EXTENDED: Podpora pre OrElse
    internal static ApplyFiltersCommand WithOrElse(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<FilterDefinition> filters) =>
        new()
        {
            Data = data,
            Filters = filters,
            LogicOperator = FilterLogicOperator.OrElse,
            UseShortCircuitEvaluation = true
        };
}

/// <summary>
/// COMMAND PATTERN: Apply advanced filter command with business rules
/// ENTERPRISE: Supports complex business logic and hierarchical grouping
/// </summary>
internal sealed record ApplyAdvancedFilterCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required AdvancedFilterDefinition AdvancedFilter { get; init; }
    internal FilterScope Scope { get; init; } = FilterScope.AllData;
    internal bool EnableParallelProcessing { get; init; } = true;
    internal bool UseShortCircuitEvaluation { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<FilterProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;
    internal FilterContext? Context { get; init; }

    // FLEXIBLE factory methods
    internal static ApplyAdvancedFilterCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedFilterDefinition advancedFilter) =>
        new() { Data = data, AdvancedFilter = advancedFilter };

    internal static ApplyAdvancedFilterCommand WithContext(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedFilterDefinition advancedFilter,
        FilterContext context) =>
        new() { Data = data, AdvancedFilter = advancedFilter, Context = context };
}

#endregion

#region Result Objects

/// <summary>
/// ENTERPRISE: Filter operation result with comprehensive statistics
/// CONSISTENT: Rovnaká štruktúra ako ImportResult a ValidationResult
/// </summary>
internal sealed record FilterResult
{
    internal bool Success { get; init; }
    internal IReadOnlyList<IReadOnlyDictionary<string, object?>> FilteredData { get; init; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
    internal IReadOnlyList<int> MatchingRowIndices { get; init; } = Array.Empty<int>();
    internal int OriginalRowCount { get; init; }
    internal int FilteredRowCount { get; init; }
    internal int ProcessedRowCount { get; init; }
    internal TimeSpan FilterTime { get; init; }
    internal FilterLogicOperator UsedLogicOperator { get; init; }
    internal bool UsedParallelProcessing { get; init; }
    internal bool UsedShortCircuitEvaluation { get; init; }
    internal IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();
    internal IReadOnlyList<FilterDefinition> AppliedFilters { get; init; } = Array.Empty<FilterDefinition>();

    internal static FilterResult CreateSuccess(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> filteredData,
        IReadOnlyList<int> matchingIndices,
        int originalCount,
        TimeSpan filterTime,
        IReadOnlyList<FilterDefinition> appliedFilters,
        FilterLogicOperator usedOperator = FilterLogicOperator.And,
        bool usedParallel = false,
        bool usedShortCircuit = false) =>
        new()
        {
            Success = true,
            FilteredData = filteredData,
            MatchingRowIndices = matchingIndices,
            OriginalRowCount = originalCount,
            FilteredRowCount = filteredData.Count,
            ProcessedRowCount = originalCount,
            FilterTime = filterTime,
            AppliedFilters = appliedFilters,
            UsedLogicOperator = usedOperator,
            UsedParallelProcessing = usedParallel,
            UsedShortCircuitEvaluation = usedShortCircuit
        };

    internal static FilterResult CreateFailure(
        IReadOnlyList<string> errors,
        TimeSpan filterTime) =>
        new()
        {
            Success = false,
            ErrorMessages = errors,
            FilterTime = filterTime
        };
}

/// <summary>
/// ENTERPRISE: Advanced filter result with business rule execution details
/// </summary>
internal sealed record AdvancedFilterResult
{
    internal bool Success { get; init; }
    internal IReadOnlyList<IReadOnlyDictionary<string, object?>> FilteredData { get; init; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
    internal int OriginalRowCount { get; init; }
    internal int FilteredRowCount { get; init; }
    internal TimeSpan FilterTime { get; init; }
    internal string ExecutedBusinessRule { get; init; } = string.Empty;
    internal IReadOnlyList<string> ExecutedFilterGroups { get; init; } = Array.Empty<string>();
    internal bool UsedCustomLogic { get; init; }
    internal FilterStatistics Statistics { get; init; } = new();
    internal IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();

    internal static AdvancedFilterResult CreateSuccess(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> filteredData,
        int originalCount,
        TimeSpan filterTime,
        string businessRule,
        FilterStatistics statistics) =>
        new()
        {
            Success = true,
            FilteredData = filteredData,
            OriginalRowCount = originalCount,
            FilteredRowCount = filteredData.Count,
            FilterTime = filterTime,
            ExecutedBusinessRule = businessRule,
            Statistics = statistics
        };
}

/// <summary>
/// ENTERPRISE: Filter execution statistics
/// PERFORMANCE: Monitoring and optimization metrics
/// </summary>
internal sealed record FilterStatistics
{
    internal int TotalFiltersExecuted { get; init; }
    internal int BusinessRulesExecuted { get; init; }
    internal int CustomLogicExecutions { get; init; }
    internal TimeSpan AverageFilterExecutionTime { get; init; }
    internal bool UsedParallelProcessing { get; init; }
    internal bool UsedShortCircuitEvaluation { get; init; }
    internal int ObjectPoolHits { get; init; }
    internal int CacheHits { get; init; }
}

#endregion
```

## 🎯 FACADE API METÓDY

### Základné Filter API (Consistent s existujúcimi metódami)

```csharp
#region Filter Operations with Command Pattern

/// <summary>
/// PUBLIC API: Apply single filter using command pattern
/// ENTERPRISE: Professional filtering with progress tracking
/// CONSISTENT: Rovnaká štruktúra ako ImportAsync a ValidateAsync
/// </summary>
Task<FilterResult> ApplyFilterAsync(
    ApplyFilterCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Apply multiple filters with logic operators
/// EXTENDED: Supports And, Or, AndAlso, OrElse operators
/// LINQ OPTIMIZED: Parallel processing with short-circuit evaluation
/// </summary>
Task<FilterResult> ApplyFiltersAsync(
    ApplyFiltersCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Apply advanced filter with business rules
/// ENTERPRISE: Complex business logic with hierarchical grouping
/// SUPPORTS: (Age > 18 AND Department = "IT") OR (Salary > 50000)
/// </summary>
Task<AdvancedFilterResult> ApplyAdvancedFilterAsync(
    ApplyAdvancedFilterCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Quick filter for immediate results
/// PERFORMANCE: Optimized for small datasets and simple criteria
/// </summary>
FilterResult QuickFilter(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    string columnName,
    FilterOperator filterOperator,
    object? value);

#endregion

#region Filter Validation and Utilities

/// <summary>
/// PUBLIC API: Validate filter definition
/// ENTERPRISE: Comprehensive filter validation with business rule checking
/// </summary>
Task<Result<bool>> ValidateFilterAsync(FilterDefinition filter);

/// <summary>
/// PUBLIC API: Validate advanced filter with business rules
/// ENTERPRISE: Complex validation including custom logic verification
/// </summary>
Task<Result<bool>> ValidateAdvancedFilterAsync(AdvancedFilterDefinition advancedFilter);

/// <summary>
/// PUBLIC API: Get filterable columns
/// DYNAMIC: Automatically discovers filterable columns from data
/// </summary>
IReadOnlyList<string> GetFilterableColumns(
    IEnumerable<IReadOnlyDictionary<string, object?>> data);

/// <summary>
/// PUBLIC API: Get recommended filter operators for column
/// SMART: Suggests appropriate operators based on data type and content
/// </summary>
IReadOnlyList<FilterOperator> GetRecommendedFilterOperators(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    string columnName);

#endregion
```

## 🔧 COMPLEX BUSINESS LOGIC EXAMPLES

### Multi-Level Grouping s AndAlso/OrElse

```csharp
// COMPLEX BUSINESS RULE: (Age > 18 AND Department = "IT") OR (Salary > 50000 AND Experience > 5)
var complexFilter = AdvancedFilterDefinition.OrElse(
    // Prvá skupina s AndAlso
    AdvancedFilterDefinition.AndAlso(
        FilterDefinition.Create("Age", FilterOperator.GreaterThan, 18),
        FilterDefinition.Create("Department", FilterOperator.Equals, "IT")
    ).Filters.ToArray(),

    // Druhá skupina s AndAlso
    AdvancedFilterDefinition.AndAlso(
        FilterDefinition.Create("Salary", FilterOperator.GreaterThan, 50000m),
        FilterDefinition.Create("Experience", FilterOperator.GreaterThan, 5)
    ).Filters.ToArray()
);

var command = ApplyAdvancedFilterCommand.Create(data, complexFilter);
var result = await facade.ApplyAdvancedFilterAsync(command);
```

### Hierarchical Business Rules

```csharp
// BUSINESS RULE: Active Premium Customer Filter
var activePremiumCustomer = new AdvancedFilterDefinition
{
    FilterName = "ActivePremiumCustomer",
    GroupType = FilterGroupType.Business,
    RootOperator = FilterLogicOperator.And,
    ChildGroups = new[]
    {
        // Active Customer Group
        AdvancedFilterDefinition.CreateActiveCustomerRule(
            cutoffDate: DateTime.Now.AddDays(-30),
            statusColumn: "CustomerStatus",
            lastOrderColumn: "LastOrderDate"
        ),

        // Premium Tier Group
        new AdvancedFilterDefinition
        {
            FilterName = "PremiumTier",
            RootOperator = FilterLogicOperator.Or,
            Filters = new[]
            {
                FilterDefinition.Create("CustomerTier", FilterOperator.Equals, "Premium"),
                FilterDefinition.Create("TotalSpent", FilterOperator.GreaterThan, 10000m),
                FilterDefinition.Create("YearsAsMember", FilterOperator.GreaterThan, 5)
            }
        }
    }
};
```

### Risk Assessment s Custom Logic

```csharp
// ENTERPRISE RISK ASSESSMENT FILTER
var riskAssessment = new AdvancedFilterDefinition
{
    FilterName = "RiskAssessment",
    GroupType = FilterGroupType.Business,
    RootOperator = FilterLogicOperator.AndAlso, // Short-circuit pre performance
    CustomLogic = (row, context) =>
    {
        // Custom business logic s DI support
        var riskService = context.ServiceProvider?.GetService<IRiskAssessmentService>();
        if (riskService == null) return true;

        var riskScore = riskService.CalculateRiskScore(row);
        return riskScore <= 0.7; // 70% risk threshold
    },
    ChildGroups = new[]
    {
        AdvancedFilterDefinition.CreateRiskAssessmentRule("MEDIUM", new Dictionary<string, object>
        {
            ["MaxAmount"] = 5000m,
            ["MinHighAmount"] = 15000m
        })
    }
};
```

## ⚡ PERFORMANCE & LINQ OPTIMIZATIONS

### Parallel Processing s Short-Circuit Evaluation

```csharp
// LINQ optimized filter service implementation
internal sealed class AdvancedFilterService
{
    public async Task<FilterResult> ApplyFiltersAsync(ApplyFiltersCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataList = command.Data.ToList();

        // LINQ OPTIMIZATION: Parallel processing s short-circuit evaluation
        var matchingIndices = command.LogicOperator switch
        {
            FilterLogicOperator.And => dataList
                .AsParallel()
                .WithCancellation(command.cancellationToken)
                .Select((row, index) => new { row, index })
                .Where(x => command.Filters.All(filter => EvaluateFilter(x.row, filter)))
                .Select(x => x.index)
                .ToList(),

            FilterLogicOperator.Or => dataList
                .AsParallel()
                .WithCancellation(command.cancellationToken)
                .Select((row, index) => new { row, index })
                .Where(x => command.Filters.Any(filter => EvaluateFilter(x.row, filter)))
                .Select(x => x.index)
                .ToList(),

            // EXTENDED: AndAlso s short-circuit evaluation
            FilterLogicOperator.AndAlso => dataList
                .AsParallel()
                .WithCancellation(command.cancellationToken)
                .Select((row, index) => new { row, index })
                .Where(x => EvaluateFiltersWithShortCircuit(x.row, command.Filters, true))
                .Select(x => x.index)
                .ToList(),

            // EXTENDED: OrElse s short-circuit evaluation
            FilterLogicOperator.OrElse => dataList
                .AsParallel()
                .WithCancellation(command.cancellationToken)
                .Select((row, index) => new { row, index })
                .Where(x => EvaluateFiltersWithShortCircuit(x.row, command.Filters, false))
                .Select(x => x.index)
                .ToList(),

            _ => throw new ArgumentException($"Unsupported logic operator: {command.LogicOperator}")
        };

        var filteredData = matchingIndices.Select(i => dataList[i]).ToList();
        stopwatch.Stop();

        return FilterResult.CreateSuccess(
            filteredData,
            matchingIndices,
            dataList.Count,
            stopwatch.Elapsed,
            command.Filters.ToList(),
            command.LogicOperator,
            usedParallel: command.EnableParallelProcessing,
            usedShortCircuit: command.UseShortCircuitEvaluation);
    }

    // EXTENDED: Short-circuit evaluation for AndAlso/OrElse
    private bool EvaluateFiltersWithShortCircuit(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<FilterDefinition> filters,
        bool isAndLogic)
    {
        foreach (var filter in filters)
        {
            var result = EvaluateFilter(row, filter);

            if (isAndLogic && !result)
                return false; // Short-circuit on first false for AndAlso

            if (!isAndLogic && result)
                return true;  // Short-circuit on first true for OrElse
        }

        return isAndLogic; // AndAlso: true if all passed, OrElse: false if none passed
    }
}
```

## 🎯 KĽÚČOVÉ VYLEPŠENIA & ROZŠÍRENIA

### 1. **Extended Logic Operators**
- **AndAlso**: Short-circuit AND evaluation pre performance
- **OrElse**: Short-circuit OR evaluation pre performance
- **Backward Compatible**: Zachované And a Or pre existujúce use cases

### 2. **Enterprise Business Rules**
- **Flexible Configuration**: Nie hardcoded factory methods
- **Hierarchical Grouping**: Nested filter groups s complex logic
- **Custom Logic Support**: DI-enabled custom business rule functions
- **Risk Assessment**: Built-in business rule templates

### 3. **Performance Optimizations**
- **LINQ Parallel Processing**: AsParallel() pre veľké datasets
- **Short-Circuit Evaluation**: Early termination pre AndAlso/OrElse
- **Object Pooling**: FilterContext pooling pre memory efficiency
- **Lazy Evaluation**: Deferred execution pre streaming scenarios

### 4. **Consistent Architecture**
- **Command Pattern**: Rovnaká štruktúra ako Import/Export a Validation
- **Clean Architecture**: Core → Application → Infrastructure layers
- **Hybrid DI**: Internal DI s functional programming support
- **Thread Safety**: Immutable commands a atomic operations

### 5. **Backup Strategy**
- **`.oldbackup_timestamp`**: Všetky modifikované súbory
- **Complete Replacement**: Žiadna backward compatibility
- **DI Preservation**: Zachované interface contracts a registrácie

Táto špecifikácia poskytuje kompletný, enterprise-ready filter systém s pokročilou business logic, optimalizáciami a jednotnou architektúrou s ostatnými časťami komponentu.

---

## 🧠 CORE FILTERING ALGORITHMS & IMPLEMENTATIONS

### **FilteringAlgorithms.cs** - Core Layer Infrastructure

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Algorithms;

/// <summary>
/// ENTERPRISE: High-performance filtering algorithms with LINQ optimizations
/// THREAD SAFE: All algorithms are thread-safe and support parallel processing
/// PERFORMANCE: Object pooling, minimal allocations, lazy evaluation
/// </summary>
internal static class FilteringAlgorithms
{
    #region Single Filter Algorithms

    /// <summary>
    /// LINQ OPTIMIZED: Single filter with parallel processing support
    /// PERFORMANCE: O(n) complexity with early termination optimization
    /// </summary>
    internal static IEnumerable<(IReadOnlyDictionary<string, object?> row, int index)> ApplySingleFilter(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        FilterDefinition filter,
        bool enableParallel = true)
    {
        var dataList = data as IReadOnlyList<IReadOnlyDictionary<string, object?>> ?? data.ToList();

        return enableParallel && dataList.Count > 1000
            ? dataList.AsParallel()
                .Select((row, index) => new { row, index })
                .Where(x => EvaluateFilterCondition(x.row, filter))
                .Select(x => (x.row, x.index))
            : dataList
                .Select((row, index) => new { row, index })
                .Where(x => EvaluateFilterCondition(x.row, filter))
                .Select(x => (x.row, x.index));
    }

    /// <summary>
    /// PERFORMANCE: Optimized filter condition evaluation with type-specific optimizations
    /// ENTERPRISE: Supports all FilterOperator types with custom logic
    /// </summary>
    internal static bool EvaluateFilterCondition(
        IReadOnlyDictionary<string, object?> row,
        FilterDefinition filter)
    {
        if (!row.TryGetValue(filter.ColumnName, out var cellValue))
            return filter.Operator == FilterOperator.IsNull;

        return filter.Operator switch
        {
            FilterOperator.Equals => CompareValues(cellValue, filter.Value, filter.IsCaseSensitive) == 0,
            FilterOperator.NotEquals => CompareValues(cellValue, filter.Value, filter.IsCaseSensitive) != 0,
            FilterOperator.GreaterThan => CompareValues(cellValue, filter.Value, filter.IsCaseSensitive) > 0,
            FilterOperator.GreaterThanOrEqual => CompareValues(cellValue, filter.Value, filter.IsCaseSensitive) >= 0,
            FilterOperator.LessThan => CompareValues(cellValue, filter.Value, filter.IsCaseSensitive) < 0,
            FilterOperator.LessThanOrEqual => CompareValues(cellValue, filter.Value, filter.IsCaseSensitive) <= 0,
            FilterOperator.Contains => ContainsValue(cellValue, filter.Value, filter.IsCaseSensitive),
            FilterOperator.NotContains => !ContainsValue(cellValue, filter.Value, filter.IsCaseSensitive),
            FilterOperator.StartsWith => StartsWithValue(cellValue, filter.Value, filter.IsCaseSensitive),
            FilterOperator.EndsWith => EndsWithValue(cellValue, filter.Value, filter.IsCaseSensitive),
            FilterOperator.IsNull => cellValue == null,
            FilterOperator.IsNotNull => cellValue != null,
            FilterOperator.IsEmpty => IsEmptyValue(cellValue),
            FilterOperator.IsNotEmpty => !IsEmptyValue(cellValue),
            FilterOperator.Between => IsBetweenValues(cellValue, filter.Value, filter.SecondValue, filter.IsCaseSensitive),
            FilterOperator.In => IsInValues(cellValue, filter.Value, filter.IsCaseSensitive),
            FilterOperator.Regex => MatchesRegex(cellValue, filter.Value, filter.IsCaseSensitive),
            _ => false
        };
    }

    #endregion

    #region Multi-Filter Algorithms with Short-Circuit Evaluation

    /// <summary>
    /// LINQ OPTIMIZED: Multi-filter with AndAlso/OrElse short-circuit evaluation
    /// PERFORMANCE: Early termination saves significant processing time
    /// </summary>
    internal static IEnumerable<(IReadOnlyDictionary<string, object?> row, int index)> ApplyMultipleFilters(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<FilterDefinition> filters,
        FilterLogicOperator logicOperator,
        bool useShortCircuit = true,
        bool enableParallel = true)
    {
        var dataList = data as IReadOnlyList<IReadOnlyDictionary<string, object?>> ?? data.ToList();

        return enableParallel && dataList.Count > 1000
            ? dataList.AsParallel()
                .Select((row, index) => new { row, index })
                .Where(x => EvaluateMultipleFilters(x.row, filters, logicOperator, useShortCircuit))
                .Select(x => (x.row, x.index))
            : dataList
                .Select((row, index) => new { row, index })
                .Where(x => EvaluateMultipleFilters(x.row, filters, logicOperator, useShortCircuit))
                .Select(x => (x.row, x.index));
    }

    /// <summary>
    /// EXTENDED: Short-circuit evaluation for AndAlso/OrElse performance
    /// ENTERPRISE: Optimized logical operations with early termination
    /// </summary>
    internal static bool EvaluateMultipleFilters(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<FilterDefinition> filters,
        FilterLogicOperator logicOperator,
        bool useShortCircuit)
    {
        return logicOperator switch
        {
            FilterLogicOperator.And => filters.All(filter => EvaluateFilterCondition(row, filter)),
            FilterLogicOperator.Or => filters.Any(filter => EvaluateFilterCondition(row, filter)),
            FilterLogicOperator.AndAlso => useShortCircuit
                ? EvaluateWithShortCircuitAnd(row, filters)
                : filters.All(filter => EvaluateFilterCondition(row, filter)),
            FilterLogicOperator.OrElse => useShortCircuit
                ? EvaluateWithShortCircuitOr(row, filters)
                : filters.Any(filter => EvaluateFilterCondition(row, filter)),
            _ => false
        };
    }

    /// <summary>
    /// PERFORMANCE: AndAlso with manual short-circuit for maximum efficiency
    /// </summary>
    private static bool EvaluateWithShortCircuitAnd(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<FilterDefinition> filters)
    {
        foreach (var filter in filters)
        {
            if (!EvaluateFilterCondition(row, filter))
                return false; // Short-circuit on first false
        }
        return true;
    }

    /// <summary>
    /// PERFORMANCE: OrElse with manual short-circuit for maximum efficiency
    /// </summary>
    private static bool EvaluateWithShortCircuitOr(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<FilterDefinition> filters)
    {
        foreach (var filter in filters)
        {
            if (EvaluateFilterCondition(row, filter))
                return true; // Short-circuit on first true
        }
        return false;
    }

    #endregion

    #region Advanced Business Rule Algorithms

    /// <summary>
    /// ENTERPRISE: Advanced filter with hierarchical business rules
    /// COMPLEX LOGIC: Supports nested groups and custom business logic
    /// </summary>
    internal static async Task<IEnumerable<(IReadOnlyDictionary<string, object?> row, int index)>> ApplyAdvancedFilterAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedFilterDefinition advancedFilter,
        FilterContext context,
        bool enableParallel = true)
    {
        var dataList = data as IReadOnlyList<IReadOnlyDictionary<string, object?>> ?? data.ToList();

        var evaluationTasks = enableParallel && dataList.Count > 1000
            ? dataList.AsParallel()
                .Select(async (row, index) => new
                {
                    row,
                    index,
                    matches = await EvaluateAdvancedFilterAsync(row, advancedFilter, context)
                })
                .ToArray()
            : dataList
                .Select(async (row, index) => new
                {
                    row,
                    index,
                    matches = await EvaluateAdvancedFilterAsync(row, advancedFilter, context)
                })
                .ToArray();

        var results = await Task.WhenAll(evaluationTasks);
        return results.Where(r => r.matches).Select(r => (r.row, r.index));
    }

    /// <summary>
    /// ENTERPRISE: Advanced filter evaluation with custom business logic
    /// ASYNC: Supports async custom logic with timeout handling
    /// </summary>
    private static async Task<bool> EvaluateAdvancedFilterAsync(
        IReadOnlyDictionary<string, object?> row,
        AdvancedFilterDefinition advancedFilter,
        FilterContext context)
    {
        // Evaluate basic filters first
        var basicFiltersResult = EvaluateMultipleFilters(
            row, advancedFilter.Filters, advancedFilter.RootOperator, useShortCircuit: true);

        if (!basicFiltersResult && advancedFilter.RootOperator == FilterLogicOperator.AndAlso)
            return false; // Short-circuit if basic filters fail and using AND logic

        // Evaluate child groups
        var childGroupResults = new List<bool>();
        foreach (var childGroup in advancedFilter.ChildGroups)
        {
            var childResult = await EvaluateAdvancedFilterAsync(row, childGroup, context);
            childGroupResults.Add(childResult);

            // Short-circuit evaluation for child groups
            if (advancedFilter.RootOperator == FilterLogicOperator.AndAlso && !childResult)
                return false;
            if (advancedFilter.RootOperator == FilterLogicOperator.OrElse && childResult)
                return true;
        }

        // Evaluate custom logic if present
        var customLogicResult = true;
        if (advancedFilter.CustomLogic != null)
        {
            try
            {
                using var timeoutCts = new cancellationTokenSource(advancedFilter.ExecutionTimeout ?? TimeSpan.FromSeconds(30));
                var combinedCts = cancellationTokenSource.CreateLinkedTokenSource(context.cancellationToken, timeoutCts.Token);

                var customContext = context with { cancellationToken = combinedCts.Token };
                customLogicResult = advancedFilter.CustomLogic(row, customContext);
            }
            catch (OperationCanceledException)
            {
                customLogicResult = false; // Timeout or cancellation
            }
        }

        // Combine all results based on root operator
        return advancedFilter.RootOperator switch
        {
            FilterLogicOperator.And => basicFiltersResult && childGroupResults.All(r => r) && customLogicResult,
            FilterLogicOperator.Or => basicFiltersResult || childGroupResults.Any(r => r) || customLogicResult,
            FilterLogicOperator.AndAlso => basicFiltersResult && childGroupResults.All(r => r) && customLogicResult,
            FilterLogicOperator.OrElse => basicFiltersResult || childGroupResults.Any(r => r) || customLogicResult,
            _ => false
        };
    }

    #endregion

    #region Performance-Optimized Helper Methods

    /// <summary>
    /// PERFORMANCE: Type-aware value comparison with minimal boxing
    /// </summary>
    private static int CompareValues(object? value1, object? value2, bool caseSensitive)
    {
        if (value1 == null && value2 == null) return 0;
        if (value1 == null) return -1;
        if (value2 == null) return 1;

        // Optimize for common types to avoid reflection
        return (value1, value2) switch
        {
            (string s1, string s2) => string.Compare(s1, s2,
                caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase),
            (int i1, int i2) => i1.CompareTo(i2),
            (decimal d1, decimal d2) => d1.CompareTo(d2),
            (DateTime dt1, DateTime dt2) => dt1.CompareTo(dt2),
            (bool b1, bool b2) => b1.CompareTo(b2),
            _ => Comparer<object>.Default.Compare(value1, value2)
        };
    }

    /// <summary>
    /// PERFORMANCE: Optimized string containment check
    /// </summary>
    private static bool ContainsValue(object? cellValue, object? filterValue, bool caseSensitive)
    {
        var cellString = cellValue?.ToString() ?? "";
        var filterString = filterValue?.ToString() ?? "";

        return cellString.Contains(filterString,
            caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// PERFORMANCE: Optimized string prefix check
    /// </summary>
    private static bool StartsWithValue(object? cellValue, object? filterValue, bool caseSensitive)
    {
        var cellString = cellValue?.ToString() ?? "";
        var filterString = filterValue?.ToString() ?? "";

        return cellString.StartsWith(filterString,
            caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// PERFORMANCE: Optimized string suffix check
    /// </summary>
    private static bool EndsWithValue(object? cellValue, object? filterValue, bool caseSensitive)
    {
        var cellString = cellValue?.ToString() ?? "";
        var filterString = filterValue?.ToString() ?? "";

        return cellString.EndsWith(filterString,
            caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// PERFORMANCE: Optimized empty value check
    /// </summary>
    private static bool IsEmptyValue(object? cellValue)
    {
        return cellValue switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            System.Collections.ICollection collection => collection.Count == 0,
            _ => false
        };
    }

    /// <summary>
    /// PERFORMANCE: Optimized between range check
    /// </summary>
    private static bool IsBetweenValues(object? cellValue, object? minValue, object? maxValue, bool caseSensitive)
    {
        if (cellValue == null || minValue == null || maxValue == null)
            return false;

        var minComparison = CompareValues(cellValue, minValue, caseSensitive);
        var maxComparison = CompareValues(cellValue, maxValue, caseSensitive);

        return minComparison >= 0 && maxComparison <= 0;
    }

    /// <summary>
    /// PERFORMANCE: Optimized IN operator check
    /// </summary>
    private static bool IsInValues(object? cellValue, object? filterValue, bool caseSensitive)
    {
        if (filterValue is not System.Collections.IEnumerable enumerable)
            return false;

        foreach (var value in enumerable)
        {
            if (CompareValues(cellValue, value, caseSensitive) == 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// PERFORMANCE: Optimized regex matching with caching
    /// </summary>
    private static bool MatchesRegex(object? cellValue, object? filterValue, bool caseSensitive)
    {
        var cellString = cellValue?.ToString() ?? "";
        var pattern = filterValue?.ToString() ?? "";

        if (string.IsNullOrEmpty(pattern))
            return false;

        try
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            return System.Text.RegularExpressions.Regex.IsMatch(cellString, pattern, options);
        }
        catch
        {
            return false; // Invalid regex pattern
        }
    }

    #endregion
}
```

---

## **🔍 LOGGING ŠPECIFIKÁCIA PRE FILTER OPERÁCIE**

### **Internal DI Registration & Service Distribution**
Všetky filter logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a injektované do `FilterService` cez internal DI systém:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<IFilterLogger<FilterService>, FilterLogger<FilterService>>();
services.AddSingleton<IOperationLogger<FilterService>, OperationLogger<FilterService>>();
services.AddSingleton<ICommandLogger<FilterService>, CommandLogger<FilterService>>();

// V FilterService constructor
public FilterService(
    ILogger<FilterService> logger,
    IFilterLogger<FilterService> filterLogger,
    IOperationLogger<FilterService> operationLogger,
    ICommandLogger<FilterService> commandLogger)
```

### **Command Pattern Filter Logging**
Filter systém implementuje pokročilé logovanie pre všetky typy filtrov vrátane AndAlso/OrElse operátorov a enterprise business rules.

### **Basic Filter Operations Logging**
```csharp
// Single filter application
_filterLogger.LogFilterOperation("SingleFilter", filter.FilterName ?? "Unnamed",
    originalRowCount, matchingRowCount, filterDuration);

_logger.LogInformation("Filter applied: '{FilterName}' operator={Operator} column='{ColumnName}' matched {MatchingRows}/{TotalRows} rows in {Duration}ms",
    filter.FilterName ?? "Unnamed", filter.Operator, filter.ColumnName,
    matchingRowCount, originalRowCount, filterDuration.TotalMilliseconds);

// Multiple filters with logic operators
_filterLogger.LogFilterCombination(command.LogicOperator, command.Filters.Count,
    command.UseShortCircuitEvaluation);

_logger.LogInformation("Multiple filters applied: {FilterCount} filters with {LogicOperator} logic, short-circuit={ShortCircuit}, matched {MatchingRows}/{TotalRows}",
    command.Filters.Count, command.LogicOperator, command.UseShortCircuitEvaluation,
    result.FilteredRowCount, result.OriginalRowCount);
```

### **Advanced Filter & Business Rules Logging**
```csharp
// Business rule execution logging
_filterLogger.LogBusinessRuleExecution(advancedFilter.FilterName,
    advancedFilter.GroupType.ToString(), success: result.Success, executionDuration);

_logger.LogInformation("Business rule '{RuleName}' executed: type={GroupType}, filters={FilterCount}, childGroups={ChildCount}, success={Success}",
    advancedFilter.FilterName, advancedFilter.GroupType,
    advancedFilter.Filters.Count, advancedFilter.ChildGroups.Count, result.Success);

// Custom logic execution logging
if (advancedFilter.CustomLogic != null)
{
    _filterLogger.LogCustomLogicExecution(advancedFilter.FilterName,
        customLogicResult, customLogicDuration, customLogicError);

    _logger.LogInformation("Custom filter logic executed for '{FilterName}': success={Success}, duration={Duration}ms",
        advancedFilter.FilterName, customLogicResult, customLogicDuration.TotalMilliseconds);
}
```

### **AndAlso/OrElse Short-Circuit Logging**
```csharp
// Short-circuit evaluation logging
_filterLogger.LogLINQOptimization("FilterEvaluation",
    usedParallel: command.EnableParallelProcessing,
    usedShortCircuit: command.UseShortCircuitEvaluation,
    duration: evaluationTime);

// AndAlso short-circuit logging
if (command.LogicOperator == FilterLogicOperator.AndAlso)
{
    _logger.LogInformation("AndAlso evaluation: short-circuited on filter {FilterIndex}/{TotalFilters}, early termination saved {SavedEvaluations} operations",
        shortCircuitIndex, totalFilters, savedEvaluations);
}

// OrElse short-circuit logging
if (command.LogicOperator == FilterLogicOperator.OrElse)
{
    _logger.LogInformation("OrElse evaluation: short-circuited on filter {FilterIndex}/{TotalFilters}, early success after {EvaluatedFilters} operations",
        shortCircuitIndex, totalFilters, evaluatedFilters);
}
```

### **Performance & LINQ Optimization Logging**
```csharp
// LINQ parallel processing logging
_logger.LogInformation("LINQ filter processing: parallel={UseParallel}, partitions={PartitionCount}, total rows={RowCount}, processing time={Duration}ms",
    enableParallelProcessing, partitionCount, totalRows, processingTime.TotalMilliseconds);

// Object pooling and memory optimization
_logger.LogInformation("Filter context pooling: pool hits={PoolHits}, cache hits={CacheHits}, memory saved={MemorySaved}KB",
    filterStatistics.ObjectPoolHits, filterStatistics.CacheHits, memorySaved);

// Performance thresholds monitoring
if (filterDuration > PerformanceThresholds.FilterWarningThreshold)
{
    _logger.LogWarning("Filter performance warning: operation took {Duration}ms, consider optimization for {FilterType} with {RowCount} rows",
        filterDuration.TotalMilliseconds, filterType, rowCount);
}
```

### **Filter Validation & Recommendations Logging**
```csharp
// Filter validation logging
_filterLogger.LogFilterValidation(filter.FilterName ?? "Unnamed",
    isValid: validationResult.IsValid, errorMessage: validationResult.ErrorMessage);

// Filter recommendations logging
_logger.LogInformation("Filter recommendations for column '{ColumnName}': {RecommendedOperators} based on data type {DataType} and {SampleCount} samples",
    columnName, string.Join(",", recommendedOperators), detectedDataType, analyzedSamples);

// Dynamic column discovery logging
_logger.LogInformation("Filterable columns discovered: {ColumnCount} columns from {RowCount} rows, types detected: {DataTypes}",
    filterableColumns.Count, dataRowCount, string.Join(",", detectedTypes));
```

### **Logging Levels Usage:**
- **Information**: Filter executions, business rule success, performance metrics, LINQ optimizations
- **Warning**: Performance degradation, large dataset processing, complex business rule warnings
- **Error**: Filter validation failures, business rule execution errors, custom logic failures
- **Critical**: Filter system failures, memory exhaustion during large operations
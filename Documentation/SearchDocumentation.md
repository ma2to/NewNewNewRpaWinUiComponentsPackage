# KOMPLETNÉ ZADANIE: POKROČILÝ SEARCH SYSTÉM PRE ADVANCEDWINUIDATAGRID

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY & ŠTRUKTÚRA

### Clean Architecture + Command Pattern (Jednotná s ostatnými časťami)
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Command handlers, services (internal)
- **Core Layer**: Domain entities, search algorithms, value objects (internal)
- **Infrastructure Layer**: Performance monitoring, resilience patterns (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý search command má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové typy search bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky search commands implementujú spoločné interface
- **Interface Segregation**: Špecializované interfaces pre rôzne typy search
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained (Jednotné s ostatnými časťami)
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable commands + encapsulated behavior
- **SOLID**: Single responsibility pre každý command type
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable commands, atomic operations
- **Internal DI Registration**: Všetky search časti budú registrované v InternalServiceRegistration.cs

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

### 1. **SearchTypes.cs** - Core Layer

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

#region Enums

/// <summary>
/// ENTERPRISE: Search scope definition for targeting specific data subsets
/// </summary>
internal enum SearchScope
{
    AllData,
    VisibleData,
    SelectedData,
    FilteredData
}

/// <summary>
/// ENTERPRISE: Search mode definition for different search strategies
/// </summary>
internal enum SearchMode
{
    Contains,
    Exact,
    StartsWith,
    EndsWith,
    Regex,
    Fuzzy
}

/// <summary>
/// ENTERPRISE: Search result ranking criteria
/// </summary>
internal enum SearchRanking
{
    None,
    Relevance,
    Position,
    Frequency
}

#endregion

#region Progress & Context Types

/// <summary>
/// ENTERPRISE: Search operation progress reporting
/// CONSISTENT: Rovnaká štruktúra ako ValidationProgress a ExportProgress
/// </summary>
internal sealed record SearchProgress
{
    internal int ProcessedRows { get; init; }
    internal int TotalRows { get; init; }
    internal double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    internal TimeSpan ElapsedTime { get; init; }
    internal string CurrentOperation { get; init; } = string.Empty;
    internal int FoundMatches { get; init; }
    internal string? CurrentColumn { get; init; }
}

/// <summary>
/// ENTERPRISE: Search execution context with DI support
/// HYBRID DI: Poskytuje services pre custom search functions
/// </summary>
internal sealed record SearchContext
{
    internal IServiceProvider? ServiceProvider { get; init; }
    internal IReadOnlyDictionary<string, object?> SearchParameters { get; init; } = new Dictionary<string, object?>();
    internal cancellationToken cancellationToken { get; init; } = default;
    internal DateTime ExecutionTime { get; init; } = DateTime.UtcNow;
    internal SearchScope Scope { get; init; } = SearchScope.AllData;
}

#endregion

#region Core Search Definitions

/// <summary>
/// DDD: Enhanced search criteria with enterprise features
/// FUNCTIONAL: Immutable search criteria with comprehensive options
/// FLEXIBLE: Nie hardcoded factory methods, ale flexible object creation
/// </summary>
internal sealed record AdvancedSearchCriteria
{
    internal string SearchText { get; init; } = string.Empty;
    internal string[]? TargetColumns { get; init; }
    internal bool UseRegex { get; init; } = false;
    internal bool CaseSensitive { get; init; } = false;
    internal bool WholeWordOnly { get; init; } = false;
    internal SearchScope Scope { get; init; } = SearchScope.AllData;
    internal SearchMode Mode { get; init; } = SearchMode.Contains;
    internal SearchRanking Ranking { get; init; } = SearchRanking.None;
    internal int? MaxMatches { get; init; }
    internal TimeSpan? Timeout { get; init; } = TimeSpan.FromSeconds(5);

    // Enhanced search properties
    internal bool HighlightMatches { get; init; } = true;
    internal bool IncludeHiddenColumns { get; init; } = false;
    internal bool SearchFromCurrentPosition { get; init; } = false;
    internal bool WrapAround { get; init; } = true;
    internal bool ShowProgress { get; init; } = true;
    internal double? FuzzyThreshold { get; init; } // Pre fuzzy search (0.0-1.0)

    // FLEXIBLE factory methods namiesto hardcoded
    internal static AdvancedSearchCriteria Create(
        string searchText,
        string[]? targetColumns = null,
        bool caseSensitive = false) =>
        new()
        {
            SearchText = searchText,
            TargetColumns = targetColumns,
            CaseSensitive = caseSensitive
        };

    internal static AdvancedSearchCriteria WithRegex(
        string searchPattern,
        string[]? targetColumns = null,
        bool caseSensitive = false) =>
        new()
        {
            SearchText = searchPattern,
            TargetColumns = targetColumns,
            UseRegex = true,
            CaseSensitive = caseSensitive,
            Mode = SearchMode.Regex
        };

    internal static AdvancedSearchCriteria WithFuzzySearch(
        string searchText,
        double fuzzyThreshold = 0.8,
        string[]? targetColumns = null) =>
        new()
        {
            SearchText = searchText,
            TargetColumns = targetColumns,
            Mode = SearchMode.Fuzzy,
            FuzzyThreshold = fuzzyThreshold
        };

    internal static AdvancedSearchCriteria WithScope(
        string searchText,
        SearchScope scope,
        string[]? targetColumns = null) =>
        new()
        {
            SearchText = searchText,
            TargetColumns = targetColumns,
            Scope = scope
        };
}

/// <summary>
/// ENTERPRISE: Enhanced search result with comprehensive match information
/// PERFORMANCE: Optimized result structure with lazy evaluation support
/// </summary>
internal sealed record SearchResult
{
    internal int RowIndex { get; init; }
    internal string ColumnName { get; init; } = string.Empty;
    internal object? Value { get; init; }
    internal object? OriginalValue { get; init; }
    internal string? MatchedText { get; init; }
    internal int MatchStartIndex { get; init; }
    internal int MatchLength { get; init; }

    // Enhanced search result properties
    internal bool IsExactMatch { get; init; }
    internal double MatchScore { get; init; } = 1.0;
    internal double RelevanceScore { get; init; } = 1.0;
    internal string? CellAddress { get; init; }
    internal bool IsHighlighted { get; init; }
    internal SearchMode UsedSearchMode { get; init; } = SearchMode.Contains;

    internal static SearchResult Create(
        int rowIndex,
        string columnName,
        object? value,
        string? matchedText = null) =>
        new()
        {
            RowIndex = rowIndex,
            ColumnName = columnName,
            Value = value,
            OriginalValue = value,
            MatchedText = matchedText,
            CellAddress = $"{columnName}{rowIndex + 1}",
            MatchScore = 1.0,
            RelevanceScore = 1.0
        };

    internal static SearchResult CreateEnhanced(
        int rowIndex,
        string columnName,
        object? value,
        string? matchedText,
        bool isExactMatch,
        double matchScore,
        double relevanceScore,
        SearchMode searchMode,
        bool isHighlighted = false) =>
        new()
        {
            RowIndex = rowIndex,
            ColumnName = columnName,
            Value = value,
            OriginalValue = value,
            MatchedText = matchedText,
            IsExactMatch = isExactMatch,
            MatchScore = matchScore,
            RelevanceScore = relevanceScore,
            CellAddress = $"{columnName}{rowIndex + 1}",
            IsHighlighted = isHighlighted,
            UsedSearchMode = searchMode
        };
}

#endregion

#region Command Objects

/// <summary>
/// COMMAND PATTERN: Basic search command with LINQ optimization
/// CONSISTENT: Rovnaká štruktúra ako ImportDataCommand a ValidateDataCommand
/// </summary>
internal sealed record SearchCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required string SearchText { get; init; }
    internal string[]? TargetColumns { get; init; }
    internal bool CaseSensitive { get; init; } = false;
    internal SearchScope Scope { get; init; } = SearchScope.AllData;
    internal bool EnableParallelProcessing { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<SearchProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    // FLEXIBLE factory methods s LINQ optimization
    internal static SearchCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        string[]? targetColumns = null) =>
        new() { Data = data, SearchText = searchText, TargetColumns = targetColumns };

    internal static SearchCommand WithScope(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        SearchScope scope,
        string[]? targetColumns = null) =>
        new() { Data = data, SearchText = searchText, Scope = scope, TargetColumns = targetColumns };

    // LINQ optimized factory
    internal static SearchCommand WithLINQOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText) =>
        new()
        {
            Data = data.AsParallel().Where(row => row.Values.Any(v => v != null)),
            SearchText = searchText,
            EnableParallelProcessing = true
        };
}

/// <summary>
/// COMMAND PATTERN: Advanced search command with comprehensive criteria
/// ENTERPRISE: Supports complex search scenarios and business logic
/// </summary>
internal sealed record AdvancedSearchCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required AdvancedSearchCriteria SearchCriteria { get; init; }
    internal bool EnableParallelProcessing { get; init; } = true;
    internal bool UseSmartRanking { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<SearchProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;
    internal SearchContext? Context { get; init; }

    // FLEXIBLE factory methods
    internal static AdvancedSearchCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSearchCriteria searchCriteria) =>
        new() { Data = data, SearchCriteria = searchCriteria };

    internal static AdvancedSearchCommand WithContext(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSearchCriteria searchCriteria,
        SearchContext context) =>
        new() { Data = data, SearchCriteria = searchCriteria, Context = context };

    internal static AdvancedSearchCommand WithPerformanceOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSearchCriteria searchCriteria) =>
        new()
        {
            Data = data,
            SearchCriteria = searchCriteria,
            EnableParallelProcessing = true,
            UseSmartRanking = true
        };
}

/// <summary>
/// COMMAND PATTERN: Smart search command with automatic optimization
/// SMART: Automatically switches between quick and advanced search based on criteria
/// </summary>
internal sealed record SmartSearchCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> Data { get; init; }
    internal required string SearchText { get; init; }
    internal string[]? TargetColumns { get; init; }
    internal bool CaseSensitive { get; init; } = false;
    internal SearchScope Scope { get; init; } = SearchScope.AllData;
    internal bool AutoOptimize { get; init; } = true;
    internal bool UseCache { get; init; } = true;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<SearchProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    // FLEXIBLE factory methods
    internal static SmartSearchCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        string[]? targetColumns = null) =>
        new() { Data = data, SearchText = searchText, TargetColumns = targetColumns };

    internal static SmartSearchCommand WithOptimization(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        bool autoOptimize = true,
        bool useCache = true) =>
        new()
        {
            Data = data,
            SearchText = searchText,
            AutoOptimize = autoOptimize,
            UseCache = useCache
        };
}

#endregion

#region Result Objects

/// <summary>
/// ENTERPRISE: Search operation result with comprehensive statistics
/// CONSISTENT: Rovnaká štruktúra ako FilterResult a ValidationResult
/// </summary>
internal sealed record SearchResultCollection
{
    internal bool Success { get; init; }
    internal IReadOnlyList<SearchResult> Results { get; init; } = Array.Empty<SearchResult>();
    internal int TotalMatchesFound { get; init; }
    internal int TotalRowsSearched { get; init; }
    internal int TotalColumnsSearched { get; init; }
    internal TimeSpan SearchTime { get; init; }
    internal SearchMode UsedSearchMode { get; init; }
    internal bool UsedParallelProcessing { get; init; }
    internal bool UsedSmartRanking { get; init; }
    internal IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();
    internal SearchStatistics Statistics { get; init; } = new();

    internal static SearchResultCollection CreateSuccess(
        IReadOnlyList<SearchResult> results,
        int totalRowsSearched,
        int totalColumnsSearched,
        TimeSpan searchTime,
        SearchMode usedMode = SearchMode.Contains,
        bool usedParallel = false,
        bool usedRanking = false) =>
        new()
        {
            Success = true,
            Results = results,
            TotalMatchesFound = results.Count,
            TotalRowsSearched = totalRowsSearched,
            TotalColumnsSearched = totalColumnsSearched,
            SearchTime = searchTime,
            UsedSearchMode = usedMode,
            UsedParallelProcessing = usedParallel,
            UsedSmartRanking = usedRanking
        };

    internal static SearchResultCollection CreateFailure(
        IReadOnlyList<string> errors,
        TimeSpan searchTime) =>
        new()
        {
            Success = false,
            ErrorMessages = errors,
            SearchTime = searchTime
        };
}

/// <summary>
/// ENTERPRISE: Search execution statistics
/// PERFORMANCE: Monitoring and optimization metrics
/// </summary>
internal sealed record SearchStatistics
{
    internal int TotalSearchOperations { get; init; }
    internal int RegexOperations { get; init; }
    internal int FuzzyOperations { get; init; }
    internal TimeSpan AverageSearchTime { get; init; }
    internal bool UsedParallelProcessing { get; init; }
    internal bool UsedCaching { get; init; }
    internal int CacheHits { get; init; }
    internal int ObjectPoolHits { get; init; }
    internal double AverageRelevanceScore { get; init; }
}

#endregion
```

## 🎯 FACADE API METÓDY

### Základné Search API (Consistent s existujúcimi metódami)

```csharp
#region Search Operations with Command Pattern

/// <summary>
/// PUBLIC API: Basic search using command pattern
/// ENTERPRISE: Professional search with progress tracking
/// CONSISTENT: Rovnaká štruktúra ako ApplyFilterAsync a ValidateAsync
/// </summary>
Task<IReadOnlyList<SearchResult>> SearchAsync(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    string searchText,
    string[]? targetColumns = null,
    bool caseSensitive = false,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Advanced search with comprehensive criteria
/// ENTERPRISE: Complex search with regex, fuzzy matching, and smart ranking
/// SUPPORTS: Multi-column search with highlighting and relevance scoring
/// </summary>
Task<IReadOnlyList<SearchResult>> AdvancedSearchAsync(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    AdvancedSearchCriteria searchCriteria,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Smart search with automatic optimization
/// ENTERPRISE: Intelligent search with performance optimization
/// SMART: Automatically switches between quick and advanced search based on criteria
/// </summary>
Task<IReadOnlyList<SearchResult>> SmartSearchAsync(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    string searchText,
    string[]? targetColumns = null,
    bool caseSensitive = false,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Quick search for immediate results
/// PERFORMANCE: Optimized for small datasets and simple criteria
/// </summary>
IReadOnlyList<SearchResult> QuickSearch(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    string searchText,
    bool caseSensitive = false);

#endregion

#region Search Validation and Utilities

/// <summary>
/// PUBLIC API: Validate search criteria
/// ENTERPRISE: Comprehensive search criteria validation
/// </summary>
Task<Result<bool>> ValidateSearchCriteriaAsync(AdvancedSearchCriteria searchCriteria);

/// <summary>
/// PUBLIC API: Get searchable columns
/// DYNAMIC: Automatically discovers searchable columns from data
/// </summary>
IReadOnlyList<string> GetSearchableColumns(
    IEnumerable<IReadOnlyDictionary<string, object?>> data);

/// <summary>
/// PUBLIC API: Get recommended search modes for data
/// SMART: Suggests appropriate search modes based on data type and content
/// </summary>
IReadOnlyList<SearchMode> GetRecommendedSearchModes(
    IEnumerable<IReadOnlyDictionary<string, object?>> data,
    string searchText);

#endregion
```

## 🔧 IMPLEMENTÁCIA EXAMPLE USAGE

### Basic Search Example
```csharp
// BASIC SEARCH - vyhovuje pôvodnému zadaniu
var searchResults = await dataGrid.SearchAsync(
    data,
    "John Smith",
    targetColumns: new[] { "FirstName", "LastName" },
    caseSensitive: false);
```

### Advanced Search Example
```csharp
// ADVANCED SEARCH - vyhovuje pôvodnému zadaniu
var advancedResults = await dataGrid.AdvancedSearchAsync(data, new AdvancedSearchCriteria
{
    SearchText = ".*@company\\.com$",  // Regex support
    TargetColumns = new[] { "Email" },
    UseRegex = true,
    CaseSensitive = false,
    Scope = SearchScope.AllData,  // AllData, VisibleData, SelectedData
    MaxMatches = null  // Default null = find all matches
});
```

### Smart Search Example
```csharp
// SMART SEARCH - nová funkcionalita
var smartResults = await dataGrid.SmartSearchAsync(
    data,
    "john@company.com",
    targetColumns: new[] { "Email", "ContactInfo" },
    caseSensitive: false);
```

### Enhanced Advanced Search with Fuzzy Matching
```csharp
// ENTERPRISE FUZZY SEARCH
var fuzzyResults = await dataGrid.AdvancedSearchAsync(data, new AdvancedSearchCriteria
{
    SearchText = "Jon Smith",  // Typos will still match "John Smith"
    Mode = SearchMode.Fuzzy,
    FuzzyThreshold = 0.8,  // 80% similarity threshold
    TargetColumns = new[] { "FullName", "CustomerName" },
    Ranking = SearchRanking.Relevance,
    HighlightMatches = true,
    ShowProgress = true
});
```

## ⚡ PERFORMANCE & LINQ OPTIMIZATIONS

### Parallel Processing s Smart Ranking

```csharp
// LINQ optimized search service implementation
internal sealed class AdvancedSearchService
{
    public async Task<SearchResultCollection> SearchAsync(AdvancedSearchCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataList = command.Data.ToList();

        // LINQ OPTIMIZATION: Parallel processing s relevance scoring
        var results = command.SearchCriteria.Mode switch
        {
            SearchMode.Contains => dataList
                .AsParallel()
                .WithCancellation(command.cancellationToken)
                .SelectMany((row, rowIndex) => SearchInRow(row, rowIndex, command.SearchCriteria))
                .Where(result => result != null)
                .OrderByDescending(result => result.RelevanceScore)
                .Take(command.SearchCriteria.MaxMatches ?? int.MaxValue)
                .ToList(),

            SearchMode.Regex => dataList
                .AsParallel()
                .WithCancellation(command.cancellationToken)
                .SelectMany((row, rowIndex) => SearchInRowWithRegex(row, rowIndex, command.SearchCriteria))
                .Where(result => result != null)
                .OrderByDescending(result => result.MatchScore)
                .ToList(),

            SearchMode.Fuzzy => dataList
                .AsParallel()
                .WithCancellation(command.cancellationToken)
                .SelectMany((row, rowIndex) => SearchInRowWithFuzzy(row, rowIndex, command.SearchCriteria))
                .Where(result => result != null && result.MatchScore >= (command.SearchCriteria.FuzzyThreshold ?? 0.8))
                .OrderByDescending(result => result.MatchScore)
                .ToList(),

            _ => throw new ArgumentException($"Unsupported search mode: {command.SearchCriteria.Mode}")
        };

        stopwatch.Stop();

        return SearchResultCollection.CreateSuccess(
            results,
            dataList.Count,
            GetSearchedColumnsCount(command.SearchCriteria, dataList),
            stopwatch.Elapsed,
            command.SearchCriteria.Mode,
            usedParallel: command.EnableParallelProcessing,
            usedRanking: command.UseSmartRanking);
    }
}
```

## 🎯 KĽÚČOVÉ VYLEPŠENIA & ROZŠÍRENIA

### 1. **Enhanced Search Modes**
- **Contains**: Standard substring search
- **Exact**: Exact match search
- **StartsWith/EndsWith**: Positional matching
- **Regex**: Regular expression support (ako v pôvodnom zadaní)
- **Fuzzy**: Approximate string matching s configurable threshold

### 2. **Smart Search Capabilities**
- **Auto-optimization**: Automatically chooses best search strategy
- **Relevance scoring**: Intelligent result ranking
- **Progress reporting**: Real-time search progress
- **Scope filtering**: Search in AllData, VisibleData, SelectedData, FilteredData

### 3. **Performance Optimizations**
- **LINQ Parallel Processing**: AsParallel() pre veľké datasets
- **Smart caching**: Results caching pre repeated searches
- **Object pooling**: SearchContext pooling pre memory efficiency
- **Lazy evaluation**: Deferred execution pre streaming scenarios

### 4. **Consistent Architecture**
- **Command Pattern**: Rovnaká štruktúra ako Import/Export a Validation
- **Clean Architecture**: Core → Application → Infrastructure layers
- **Hybrid DI**: Internal DI s functional programming support
- **Thread Safety**: Immutable commands a atomic operations

### 5. **Enhanced User Experience**
- **Match highlighting**: Visual indication of search matches
- **Progress reporting**: Real-time feedback pre long-running searches
- **Smart recommendations**: Automatic search mode suggestions
- **Comprehensive validation**: Search criteria validation before execution

### 6. **Backward Compatibility & New Features**
- **Zachované API**: Pôvodné `SearchAsync` a `QuickSearch` metódy
- **Nové rozšírenia**: `AdvancedSearchAsync` a `SmartSearchAsync`
- **Enhanced Results**: Comprehensive search result information
- **Smart defaults**: Intelligent default values pre všetky search options

Táto špecifikácia poskytuje kompletný, enterprise-ready search systém ktorý vyhovuje pôvodnému zadaniu ale je rozšírený o pokročilé funkcie a je jednotný s ostatnými časťami komponentu (Filter, Validation, Import/Export) čo sa týka architektúry, command pattern, hybrid DI, SOLID princípov a performance optimizations.

---

## **🔍 LOGGING ŠPECIFIKÁCIA PRE SEARCH OPERÁCIE**

### **Internal DI Registration & Service Distribution**
Všetky search logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a distribuované cez internal DI do `SearchService`:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<ISearchLogger<SearchService>, SearchLogger<SearchService>>();
services.AddSingleton<IOperationLogger<SearchService>, OperationLogger<SearchService>>();
services.AddSingleton<ICommandLogger<SearchService>, CommandLogger<SearchService>>();

// V SearchService constructor
public SearchService(
    ILogger<SearchService> logger,
    ISearchLogger<SearchService> searchLogger,
    IOperationLogger<SearchService> operationLogger,
    ICommandLogger<SearchService> commandLogger)
```

### **Command Pattern Search Logging Integration**
Search systém implementuje pokročilé logovanie pre všetky typy search operácií vrátane regex, fuzzy matching a smart ranking s performance optimization tracking.

### **Basic & Advanced Search Operations Logging**
```csharp
// Search operation execution logging
await _operationLogger.LogCommandOperationStart(searchCommand, new {
    searchText = searchCommand.SearchCriteria.SearchText,
    targetColumns = searchCommand.SearchCriteria.TargetColumns?.Length ?? 0,
    searchMode = searchCommand.SearchCriteria.Mode,
    useRegex = searchCommand.SearchCriteria.UseRegex
});

_logger.LogInformation("Search started: '{SearchText}' in {ColumnCount} columns, mode={Mode}, regex={UseRegex}, scope={Scope}",
    searchCriteria.SearchText, targetColumns?.Length ?? 0,
    searchCriteria.Mode, searchCriteria.UseRegex, searchCriteria.Scope);

// Search execution and performance
_searchLogger.LogSearchOperation("AdvancedSearch", searchCriteria.SearchText,
    totalRowsSearched, result.TotalMatchesFound, searchTime);

_logger.LogInformation("Search completed: {MatchCount} matches found in {RowCount} rows, duration={Duration}ms, mode={Mode}",
    result.TotalMatchesFound, result.TotalRowsSearched, searchTime.TotalMilliseconds, result.UsedSearchMode);
```

### **Smart Search & Fuzzy Matching Logging**
```csharp
// Smart search optimization decisions
_logger.LogInformation("Smart search optimization: selected {Strategy} strategy for '{SearchText}' based on data size={RowCount} and complexity",
    selectedStrategy, searchText, dataRowCount);

// Fuzzy search threshold and scoring
if (searchCriteria.Mode == SearchMode.Fuzzy)
{
    _logger.LogInformation("Fuzzy search executed: threshold={Threshold}, average score={AverageScore}, matches above threshold={MatchCount}",
        searchCriteria.FuzzyThreshold, averageMatchScore, matchesAboveThreshold);
}

// Relevance scoring and ranking
if (useSmartRanking)
{
    _logger.LogInformation("Search ranking applied: {RankingType}, top result score={TopScore}, score distribution=[{ScoreDistribution}]",
        searchCriteria.Ranking, topResultScore, string.Join(",", scoreDistribution));
}
```

### **Search Performance & LINQ Optimization Logging**
```csharp
// LINQ parallel processing performance
_searchLogger.LogLINQOptimization("SearchExecution",
    usedParallel: enableParallelProcessing,
    usedShortCircuit: false, // Search doesn't use short-circuit
    duration: executionTime);

_logger.LogInformation("Search performance: parallel={UseParallel}, cache hits={CacheHits}, object pool hits={PoolHits}, memory efficiency={MemoryEfficiency}%",
    usedParallelProcessing, searchStatistics.CacheHits,
    searchStatistics.ObjectPoolHits, memoryEfficiency);

// Search scope and filtering impact
_logger.LogInformation("Search scope processing: scope={Scope}, filtered from {OriginalRows} to {ProcessedRows} rows, scope filtering time={ScopeTime}ms",
    searchScope, originalRowCount, processedRowCount, scopeFilteringTime.TotalMilliseconds);
```

### **Search Validation & Recommendations Logging**
```csharp
// Search criteria validation
_logger.LogInformation("Search criteria validation: valid={IsValid}, regex pattern valid={RegexValid}, fuzzy threshold valid={FuzzyValid}",
    validationResult.IsValid, regexPatternValid, fuzzyThresholdValid);

// Column recommendations and data analysis
_logger.LogInformation("Searchable columns analysis: {TotalColumns} columns, {SearchableColumns} searchable, recommended modes=[{RecommendedModes}]",
    totalColumns, searchableColumns.Count, string.Join(",", recommendedModes));

// Search pattern analysis and suggestions
_logger.LogInformation("Search pattern analysis for '{SearchText}': detected pattern type={PatternType}, suggested mode={SuggestedMode}, confidence={Confidence}%",
    searchText, detectedPatternType, suggestedMode, confidence);
```

### **Search Result Processing & Highlighting Logging**
```csharp
// Match highlighting and result processing
_logger.LogInformation("Search results processing: {TotalMatches} matches processed, highlighting enabled={HighlightingEnabled}, average relevance={AvgRelevance}",
    searchResults.Count, highlightingEnabled, averageRelevance);

// Result ranking and filtering
if (searchCriteria.MaxMatches.HasValue)
{
    _logger.LogInformation("Search results limited: {FoundMatches} matches found, limited to {MaxMatches}, ranking by {RankingCriteria}",
        totalMatchesFound, searchCriteria.MaxMatches.Value, searchCriteria.Ranking);
}
```

### **Logging Levels Usage:**
- **Information**: Search executions, match statistics, performance metrics, smart optimization decisions
- **Warning**: Performance degradation, large dataset warnings, regex timeout warnings, fuzzy search threshold adjustments
- **Error**: Search validation failures, regex pattern errors, search execution failures, timeout exceeded
- **Critical**: Search system failures, memory exhaustion during large search operations, data corruption detection

## 🧮 CORE SEARCH & FILTER ALGORITHMS INFRASTRUCTURE

### **SearchFilterAlgorithms.cs** - Pure Functional Search Engine

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Utilities;

/// <summary>
/// ENTERPRISE: Pure functional search and filter algorithms for maximum performance and testability
/// FUNCTIONAL PARADIGM: Stateless algorithms without side effects
/// HYBRID APPROACH: Functional algorithms within OOP service architecture
/// THREAD SAFE: Immutable functions suitable for concurrent execution
/// </summary>
internal static class SearchFilterAlgorithms
{
    /// <summary>
    /// PURE FUNCTION: Evaluate filter criteria against row data with comprehensive operator support
    /// PERFORMANCE: Optimized comparison logic without state dependencies
    /// ENTERPRISE: Complete filter operator implementation for business scenarios
    /// </summary>
    public static bool EvaluateFilter(
        IReadOnlyDictionary<string, object?> row,
        FilterCriteria filter)
    {
        if (row == null) throw new ArgumentNullException(nameof(row));
        if (filter == null) throw new ArgumentNullException(nameof(filter));

        if (!row.TryGetValue(filter.ColumnName, out var value))
            return false;

        return filter.Operator switch
        {
            FilterOperator.Equals => CompareValues(value, filter.Value) == 0,
            FilterOperator.NotEquals => CompareValues(value, filter.Value) != 0,
            FilterOperator.GreaterThan => CompareValues(value, filter.Value) > 0,
            FilterOperator.GreaterThanOrEqual => CompareValues(value, filter.Value) >= 0,
            FilterOperator.LessThan => CompareValues(value, filter.Value) < 0,
            FilterOperator.LessThanOrEqual => CompareValues(value, filter.Value) <= 0,
            FilterOperator.Contains => ContainsValue(value, filter.Value, filter.CaseSensitive),
            FilterOperator.StartsWith => StartsWithValue(value, filter.Value, filter.CaseSensitive),
            FilterOperator.EndsWith => EndsWithValue(value, filter.Value, filter.CaseSensitive),
            FilterOperator.Regex => IsRegexMatch(value, filter.Value, filter.CaseSensitive),
            FilterOperator.IsNull => value == null,
            FilterOperator.IsNotNull => value != null,
            FilterOperator.IsEmpty => IsEmptyValue(value),
            FilterOperator.IsNotEmpty => !IsEmptyValue(value),
            _ => false
        };
    }

    /// <summary>
    /// PURE FUNCTION: Type-safe value comparison with intelligent type coercion
    /// ENTERPRISE: Comprehensive type handling for business data scenarios
    /// PERFORMANCE: Optimized comparison paths for common data types
    /// </summary>
    public static int CompareValues(object? value1, object? value2)
    {
        // Handle null cases first
        if (value1 == null && value2 == null) return 0;
        if (value1 == null) return -1;
        if (value2 == null) return 1;

        // Direct equality check for performance
        if (value1.Equals(value2)) return 0;

        // Type-specific comparisons for optimal performance
        if (value1 is IComparable comparable1 && value2 is IComparable comparable2)
        {
            // Attempt direct comparison if types match
            if (value1.GetType() == value2.GetType())
            {
                return comparable1.CompareTo(comparable2);
            }

            // Numeric type conversions
            if (TryConvertToDouble(value1, out var double1) && TryConvertToDouble(value2, out var double2))
            {
                return double1.CompareTo(double2);
            }

            // DateTime conversions
            if (TryConvertToDateTime(value1, out var date1) && TryConvertToDateTime(value2, out var date2))
            {
                return date1.CompareTo(date2);
            }
        }

        // Final fallback to string comparison
        var str1 = value1.ToString() ?? string.Empty;
        var str2 = value2.ToString() ?? string.Empty;
        return string.Compare(str1, str2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// PURE FUNCTION: Advanced pattern matching with regex and text search support
    /// PERFORMANCE: Optimized string operations with early returns
    /// FLEXIBILITY: Support for both regex and simple text matching
    /// </summary>
    public static bool IsMatch(
        string text,
        string searchText,
        bool useRegex = false,
        bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(text)) return string.IsNullOrEmpty(searchText);
        if (string.IsNullOrEmpty(searchText)) return true;

        if (useRegex)
        {
            return IsRegexMatch(text, searchText, caseSensitive);
        }

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return text.Contains(searchText, comparison);
    }

    /// <summary>
    /// PURE FUNCTION: Production-safe regex matching with timeout and fallback
    /// ENTERPRISE: Comprehensive error handling for production scenarios
    /// RESILIENCE: Automatic fallback strategies for regex failures
    /// </summary>
    public static bool IsRegexMatch(object? value, object? pattern, bool caseSensitive = false)
    {
        if (value == null || pattern == null) return false;

        var text = value.ToString() ?? string.Empty;
        var regexPattern = pattern.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(regexPattern)) return true;

        try
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            // Add timeout for production safety (100ms timeout)
            return Regex.IsMatch(text, regexPattern, options, TimeSpan.FromMilliseconds(100));
        }
        catch (RegexMatchTimeoutException)
        {
            // Fallback to simple contains for timeout scenarios
            return IsMatch(text, regexPattern, false, caseSensitive);
        }
        catch (ArgumentException)
        {
            // Invalid regex pattern - fallback to literal matching
            return IsMatch(text, regexPattern, false, caseSensitive);
        }
    }

    /// <summary>
    /// PURE FUNCTION: String containment check with case sensitivity support
    /// TEXT PROCESSING: Efficient substring matching for search operations
    /// </summary>
    private static bool ContainsValue(object? value, object? searchValue, bool caseSensitive)
    {
        if (value == null || searchValue == null) return false;

        var text = value.ToString() ?? string.Empty;
        var search = searchValue.ToString() ?? string.Empty;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return text.Contains(search, comparison);
    }

    /// <summary>
    /// PURE FUNCTION: String prefix matching with case sensitivity support
    /// TEXT PROCESSING: Efficient prefix matching for filter operations
    /// </summary>
    private static bool StartsWithValue(object? value, object? prefix, bool caseSensitive)
    {
        if (value == null || prefix == null) return false;

        var text = value.ToString() ?? string.Empty;
        var prefixText = prefix.ToString() ?? string.Empty;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return text.StartsWith(prefixText, comparison);
    }

    /// <summary>
    /// PURE FUNCTION: String suffix matching with case sensitivity support
    /// TEXT PROCESSING: Efficient suffix matching for filter operations
    /// </summary>
    private static bool EndsWithValue(object? value, object? suffix, bool caseSensitive)
    {
        if (value == null || suffix == null) return false;

        var text = value.ToString() ?? string.Empty;
        var suffixText = suffix.ToString() ?? string.Empty;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return text.EndsWith(suffixText, comparison);
    }

    /// <summary>
    /// PURE FUNCTION: Comprehensive emptiness evaluation for different data types
    /// BUSINESS LOGIC: Enterprise-grade empty value detection
    /// </summary>
    private static bool IsEmptyValue(object? value)
    {
        return value switch
        {
            null => true,
            string str => string.IsNullOrWhiteSpace(str),
            Array array => array.Length == 0,
            System.Collections.ICollection collection => collection.Count == 0,
            _ => false
        };
    }

    /// <summary>
    /// PURE FUNCTION: Safe numeric conversion with comprehensive type support
    /// TYPE SAFETY: Exception-free numeric type conversion for comparisons
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
    /// TYPE SAFETY: Exception-free DateTime type conversion for comparisons
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

## 🎯 SEARCH & FILTER ALGORITHMS INTEGRATION PATTERNS

### **Application Layer Integration - FilterService**
```csharp
// FilterService.cs - Integration with pure functional algorithms
internal sealed class FilterService : IFilterService
{
    public async Task<FilterResult> ApplyFilterAsync(FilterCommand command, cancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Apply filter using pure functional algorithms
        var filteredData = command.Data.Where(row =>
            SearchFilterAlgorithms.EvaluateFilter(row, command.FilterCriteria)).ToList();

        stopwatch.Stop();

        _logger.LogInformation("Filter applied: column={Column}, operator={Operator}, found {MatchCount}/{TotalCount} matches in {Duration}ms",
            command.FilterCriteria.ColumnName, command.FilterCriteria.Operator,
            filteredData.Count, command.Data.Count(), stopwatch.ElapsedMilliseconds);

        return FilterResult.CreateSuccess(filteredData, command.FilterCriteria, stopwatch.Elapsed);
    }

    public async Task<FilterResult> ApplyAdvancedFilterAsync(AdvancedFilterCommand command, cancellationToken cancellationToken = default)
    {
        // Complex filter logic using multiple criteria
        var filteredData = command.Data.Where(row =>
        {
            return command.FilterCriteria.All(criteria =>
                SearchFilterAlgorithms.EvaluateFilter(row, criteria));
        }).ToList();

        return FilterResult.CreateSuccess(filteredData, command.FilterCriteria, stopwatch.Elapsed);
    }
}
```

### **Application Layer Integration - SearchService**
```csharp
// SearchService.cs - Integration with pure functional algorithms
internal sealed class SearchService : ISearchService
{
    public async Task<SearchResult> SearchAsync(SearchCommand command, cancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Apply search using pure functional algorithms
        var searchResults = new List<SearchMatch>();

        foreach (var row in command.Data)
        {
            foreach (var columnName in command.SearchableColumns)
            {
                if (row.TryGetValue(columnName, out var value))
                {
                    var text = value?.ToString() ?? string.Empty;

                    // Use pure functional search algorithm
                    if (SearchFilterAlgorithms.IsMatch(text, command.SearchText,
                        command.UseRegex, command.CaseSensitive))
                    {
                        searchResults.Add(new SearchMatch
                        {
                            Row = row,
                            ColumnName = columnName,
                            MatchedText = text,
                            RelevanceScore = CalculateRelevanceScore(text, command.SearchText)
                        });
                    }
                }
            }
        }

        stopwatch.Stop();

        return SearchResult.CreateSuccess(searchResults, command.SearchText, stopwatch.Elapsed);
    }

    public async Task<SearchResult> SmartSearchAsync(SmartSearchCommand command, cancellationToken cancellationToken = default)
    {
        // Intelligent search using pattern detection
        var detectedPattern = AnalyzeSearchPattern(command.SearchText);
        var optimizedSearchText = OptimizeSearchPattern(command.SearchText, detectedPattern);

        // Use optimized search with pure algorithms
        var results = await SearchAsync(new SearchCommand
        {
            SearchText = optimizedSearchText,
            UseRegex = detectedPattern == SearchPattern.Regex,
            CaseSensitive = command.CaseSensitive,
            Data = command.Data,
            SearchableColumns = command.SearchableColumns
        }, cancellationToken);

        return results;
    }

    private double CalculateRelevanceScore(string text, string searchText)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchText))
            return 0.0;

        // Exact match gets highest score
        if (text.Equals(searchText, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        // Starts with gets high score
        if (text.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
            return 0.8;

        // Contains gets medium score
        if (text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            return 0.6;

        // Partial matches get lower scores based on similarity
        return CalculateSimilarityScore(text, searchText);
    }
}
```

### **Advanced Filter Operations with Pure Algorithms**
```csharp
// Advanced filtering scenarios using SearchFilterAlgorithms
public static class AdvancedFilterOperations
{
    public static IEnumerable<IReadOnlyDictionary<string, object?>> ApplyComplexFilter(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<FilterCriteria> criteria,
        FilterLogic logic = FilterLogic.And)
    {
        return data.Where(row =>
        {
            var results = criteria.Select(filter =>
                SearchFilterAlgorithms.EvaluateFilter(row, filter)).ToList();

            return logic switch
            {
                FilterLogic.And => results.All(r => r),
                FilterLogic.Or => results.Any(r => r),
                FilterLogic.Xor => results.Count(r => r) == 1,
                _ => results.All(r => r)
            };
        });
    }

    public static IEnumerable<IReadOnlyDictionary<string, object?>> ApplyMultiColumnSearch(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        IReadOnlyList<string> columns,
        bool useRegex = false,
        bool caseSensitive = false)
    {
        return data.Where(row =>
            columns.Any(columnName =>
            {
                if (row.TryGetValue(columnName, out var value))
                {
                    var text = value?.ToString() ?? string.Empty;
                    return SearchFilterAlgorithms.IsMatch(text, searchText, useRegex, caseSensitive);
                }
                return false;
            }));
    }
}
```
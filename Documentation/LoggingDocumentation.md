# KOMPLETNÁ ŠPECIFIKÁCIA INFRASTRUCTURE LOGGING SYSTÉMU

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY

### Clean Architecture + Command Pattern
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Logging handlers, operation services (internal)
- **Core Layer**: Logging domain entities, operation rules (internal)
- **Infrastructure Layer**: Log rendering, persistence services (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý logger má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové typy logov bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky loggers implementujú `IOperationLogger<T>`
- **Interface Segregation**: Špecializované interfaces pre rôzne typy operácií
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable log commands + encapsulated behavior
- **SOLID**: Single responsibility pre každý logger type
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable log commands, atomic operation updates
- **Internal DI Registration**: Všetky logging časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť .oldbackup_timestamp súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a LINQ optimizations
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované a bezpečné a stabilné riešenie

## **🎯 LOGGING LEVELS STRATEGY (UNIFIED FOR DEBUG & RELEASE):**
- **Information**: Successful operations, state changes, performance metrics
- **Warning**: Performance degradation, fallback scenarios, validation warnings
- **Error**: Operation failures, recoverable errors, invalid inputs
- **Critical**: System failures, unrecoverable errors, security issues
- **NO Debug Level**: Všetky debug informácie na Information level

## 🎯 TYPY LOGGERS A ICH ŠPECIALIZÁCIE

### 1. **IOperationLogger<T> - Universal Operation Logging**
```csharp
// OPERATION SCOPE PATTERN s automatic timing a disposal
public interface IOperationLogger<T>
{
    IOperationScope LogOperationStart(string operationName, object? context = null);
    Task LogOperationStartAsync(string operationName, object? context = null);
    void LogOperationSuccess(string operationName, object? result = null, TimeSpan? duration = null);
    void LogOperationFailure(string operationName, Exception exception, object? context = null);
    void LogOperationWarning(string operationName, string warning, object? context = null);

    // EXTENDED for command pattern
    IOperationScope LogCommandOperationStart<TCommand>(TCommand command, object? parameters = null);
    void LogCommandSuccess<TCommand>(string commandType, TCommand command, TimeSpan duration);
    void LogCommandFailure<TCommand>(string commandType, TCommand command, Exception exception, TimeSpan duration);

    // Filter operations - EXTENDED
    void LogFilterOperation(string filterType, string filterName, int totalRows, int matchingRows, TimeSpan duration);
    void LogAdvancedFilterOperation(string businessRule, int totalFilters, int totalRows, int matchingRows, TimeSpan duration);

    // Import/Export operations - CONSISTENT
    void LogImportOperation(string importType, int totalRows, int importedRows, TimeSpan duration);
    void LogExportOperation(string exportType, int totalRows, int exportedRows, TimeSpan duration);

    // Validation operations - CONSISTENT
    void LogValidationOperation(string validationType, int totalRows, int validRows, int ruleCount, TimeSpan duration);

    // Performance monitoring - EXTENDED
    void LogPerformanceMetrics(string operationType, object metrics);
    void LogLINQOptimization(string operationType, bool usedParallel, bool usedShortCircuit, TimeSpan duration);
}

// 🔄 AUTOMATICKÉ OPERATION TRACKING:
// Pri začatí operácie sa automaticky spustí timing a context tracking
// Toto platí pre VŠETKY service operations, nie len ako príklad
```

### 2. **IUIInteractionLogger - UI Interaction Tracking**
```csharp
public interface IUIInteractionLogger
{
    void LogUserAction(string actionType, string elementId, object? parameters = null);
    void LogThemeChange(string themeName, string previousTheme);
    void LogColorUpdate(string elementType, string elementState, string newColor);
    void LogPerformanceMetric(string metricName, double value, string unit);
    void LogValidationResult(string ruleName, bool isValid, string? errorMessage = null);

    // 🔄 AUTOMATICKÉ UI TRACKING pre VŠETKY user interactions
    // Pri zmene UI state sa automaticky loguje user action s context
    DependentElements = new[] { "ThemeName", "ElementId", "ValidationRuleName" }
}
```

### 3. **IValidationLogger<T> - Validation-Specific Logging**
```csharp
public interface IValidationLogger<T> : IOperationLogger<T>
{
    Task LogRuleExecution(string ruleName, string ruleType, bool isValid, TimeSpan duration);
    Task LogAsyncValidation(string ruleName, bool isValid, TimeSpan duration, bool wasTimedOut);
    Task LogDuplicateDetection(string ruleName, int groupCount, int totalDuplicates, TimeSpan duration);
    Task LogAutomaticRevalidation(string columnName, int affectedRulesCount, TimeSpan duration);
    void LogValidationStrategy(string strategy, int rowCount, int ruleCount, string performanceInfo);

    // ENHANCED methods
    void LogRuleExecution(string ruleName, string ruleType, bool success, TimeSpan duration);
    void LogAutomaticRevalidation(string columnName, int affectedRules, TimeSpan duration);
    void LogValidationStrategy(string strategy, int rowCount, int ruleCount, string reason);
    void LogAsyncValidation(string ruleName, bool success, TimeSpan duration, bool wasTimedOut = false);

    // 🔄 AUTOMATICKÉ VALIDATION TRACKING pre VŠETKY validation rules
    // Pri zmene validation state sa automaticky loguje validation result
    ValidationTimeout = TimeSpan.FromSeconds(2); // default timeout
}
```

### 4. **IFilterLogger<T> - Filter-Specific Logging**
```csharp
public interface IFilterLogger<T> : IOperationLogger<T>
{
    void LogFilterCombination(FilterLogicOperator logicOperator, int filterCount, bool useShortCircuit);
    void LogBusinessRuleExecution(string ruleName, string ruleType, bool success, TimeSpan duration);
    void LogCustomLogicExecution(string filterName, bool success, TimeSpan duration, string? errorMessage = null);
    void LogFilterValidation(string filterName, bool isValid, string? errorMessage = null);

    // 🔄 AUTOMATICKÉ FILTER TRACKING pre VŠETKY filter operations
    DependentFilters = new[] { "FilterName", "BusinessRule", "CustomLogic" }
}
```

### 5. **IImportExportLogger<T> - Import/Export Logging**
```csharp
public interface IImportExportLogger<T> : IOperationLogger<T>
{
    void LogDataConversion(string fromFormat, string toFormat, int rowCount, TimeSpan duration);
    void LogClipboardOperation(string operationType, bool success, int dataSize, TimeSpan duration);
    void LogProgressReporting(string operationType, double progressPercentage, int processedItems, int totalItems);

    // 🔄 AUTOMATICKÉ IMPORT/EXPORT TRACKING pre VŠETKY data operations
    DependentOperations = new[] { "ImportType", "ExportFormat", "DataSize" }
}
```

### 6. **IExceptionLogger<T> - Exception Tracking**
```csharp
public interface IExceptionLogger<T>
{
    Task LogUnhandledException(Exception exception, string? context = null);
    Task<TResult?> LogServiceException<TResult>(Exception exception, string serviceName, string operationName, TResult? fallbackValue = default);
    Task LogUIException(Exception exception, string uiContext);
    Task LogAsyncOperationException(Exception exception, string operationName, bool wasCancelled = false);

    // 🔄 AUTOMATICKÉ EXCEPTION TRACKING pre VŠETKY service operations
    DependentOperations = new[] { "ServiceName", "OperationName", "UIContext" }
}
```

### 7. **IPerformanceLogger<T> - Performance Metrics Logging**
```csharp
public interface IPerformanceLogger<T>
{
    void LogPerformanceMetric(string metricName, double value, string unit, object? context = null);
    void LogMemoryUsage(long memoryBytes, string context);
    void LogOperationTiming(string operationName, TimeSpan duration, int itemCount = 0);
    void LogThroughput(string operationName, int itemsProcessed, TimeSpan duration);
    void LogResourceUtilization(string resourceType, double utilizationPercentage);

    // 🔄 AUTOMATICKÉ PERFORMANCE TRACKING pre VŠETKY operations
    // Pri dokončení operation sa automaticky loguje performance data
    DependentMetrics = new[] { "MemoryUsage", "CpuUsage", "OperationDuration" }
}
```

### 8. **ICommandLogger<T> - Command Pattern Logging**
```csharp
public interface ICommandLogger<T>
{
    void LogCommandExecution<TCommand>(TCommand command, bool success, TimeSpan duration) where TCommand : class;
    void LogCommandValidation<TCommand>(TCommand command, bool isValid, IReadOnlyList<string> validationErrors) where TCommand : class;
    void LogCommandRollback<TCommand>(TCommand command, string rollbackReason) where TCommand : class;
    void LogBatchCommandExecution<TCommand>(IReadOnlyList<TCommand> commands, int successCount, int failureCount) where TCommand : class;

    // ENHANCED methods
    void LogCommandExecution<TCommand>(TCommand command, string operationType);
    void LogCommandValidation<TCommand>(TCommand command, bool isValid, IReadOnlyList<string>? errors = null);
    void LogCommandProgress<TCommand>(TCommand command, double progressPercentage, string currentOperation);

    // 🔄 AUTOMATICKÉ COMMAND TRACKING pre VŠETKY command executions
    DependentCommands = new[] { "ValidationCommands", "DataCommands", "UICommands" }
}
```

## 📋 COMMAND PATTERN PRE LOGGING OPERATIONS

### LogOperationCommand
```csharp
public sealed record LogOperationCommand<T>
{
    public required string OperationName { get; init; }
    public required LogLevel Level { get; init; }
    public object? Context { get; init; }
    public Exception? Exception { get; init; }
    public TimeSpan? Duration { get; init; }
    public object? Result { get; init; }

    // Factory methods pre FLEXIBLE creation s DI support
    public static LogOperationCommand<T> Success(string operationName, object? result = null, TimeSpan? duration = null) =>
        new() { OperationName = operationName, Level = LogLevel.Information, Result = result, Duration = duration };

    public static LogOperationCommand<T> Failure(string operationName, Exception exception, object? context = null) =>
        new() { OperationName = operationName, Level = LogLevel.Error, Exception = exception, Context = context };

    public static LogOperationCommand<T> Warning(string operationName, string warning, object? context = null) =>
        new() { OperationName = operationName, Level = LogLevel.Warning, Context = new { Warning = warning, Context = context } };

    // DI factory method
    public static LogOperationCommand<T> CreateWithDI(string operationName, LogLevel level, IServiceProvider services) =>
        new() { OperationName = operationName, Level = level };
}
```

### CreateLogScopeCommand
```csharp
public sealed record CreateLogScopeCommand
{
    public required string ScopeName { get; init; }
    public required Type ServiceType { get; init; }
    public object? InitialContext { get; init; }
    public bool EnableAutoTiming { get; init; } = true;
    public TimeSpan? WarningThreshold { get; init; }
    public LogLevel MinimumLevel { get; init; } = LogLevel.Debug;

    // FLEXIBLE factory methods s DI support
    public static CreateLogScopeCommand Create(string scopeName, Type serviceType) =>
        new() { ScopeName = scopeName, ServiceType = serviceType };

    public static CreateLogScopeCommand WithTiming(string scopeName, Type serviceType, TimeSpan warningThreshold) =>
        new() { ScopeName = scopeName, ServiceType = serviceType, WarningThreshold = warningThreshold };
}
```

### StructuredLogCommand
```csharp
public sealed record StructuredLogCommand
{
    public required LogLevel Level { get; init; }
    public required string MessageTemplate { get; init; }
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }
    public Exception? Exception { get; init; }
    public string? CorrelationId { get; init; }
    public object? Scope { get; init; }

    // FLEXIBLE factory methods s LINQ optimization
    public static StructuredLogCommand Create(LogLevel level, string messageTemplate, params object?[] args) =>
        new() { Level = level, MessageTemplate = messageTemplate, Parameters = CreateParameterDictionary(messageTemplate, args) };

    public static StructuredLogCommand WithCorrelation(LogLevel level, string messageTemplate, string correlationId, params object?[] args) =>
        new() { Level = level, MessageTemplate = messageTemplate, CorrelationId = correlationId, Parameters = CreateParameterDictionary(messageTemplate, args) };

    // LINQ optimized factory pre bulk logging
    public static IEnumerable<StructuredLogCommand> CreateBulk(
        IEnumerable<(LogLevel level, string message, object?[] args)> logEntries) =>
        logEntries.Select(entry => Create(entry.level, entry.message, entry.args));
}
```

## 🎯 FAÇADE API METÓDY

### Universal Logging API
```csharp
// FLEXIBLE generic approach - nie hardcoded factory methods
Task<Result<IOperationScope>> StartOperationAsync<T>(string operationName, object? context = null);

// Príklady použitia:
using var scope = await facade.StartOperationAsync<ValidationService>("ValidateAllRows", new { RowCount = 1000 });
using var scope = await facade.StartOperationAsync<ImportService>("ImportDataFromFile", new { FileName = "data.xlsx" });
using var scope = await facade.StartOperationAsync<ExportService>("ExportToJson", new { Format = "JSON" });
```

### Structured Logging API
```csharp
// Log with structured data
Task<Result<bool>> LogStructuredAsync(LogLevel level, string messageTemplate, params object?[] parameters);
Task<Result<bool>> LogStructuredAsync(LogLevel level, string messageTemplate, IReadOnlyDictionary<string, object?> parameters);

// Log with correlation
Task<Result<bool>> LogWithCorrelationAsync(string correlationId, LogLevel level, string messageTemplate, params object?[] parameters);

// Bulk logging s LINQ optimization
Task<Result<int>> LogBulkAsync(IEnumerable<StructuredLogCommand> logCommands);
```

### Performance Logging API
```csharp
/// <summary>
/// PUBLIC API: Log performance metrics with automatic aggregation
/// ENTERPRISE: Performance tracking with statistical analysis
/// AUTOMATIC: Tracks performance trends and alerts on degradation
/// </summary>
Task<Result<bool>> LogPerformanceMetricAsync(string metricName, double value, string unit, object? context = null);

/// <summary>
/// PUBLIC API: Create performance scope with automatic timing
/// ENTERPRISE: High-precision timing with threshold alerts
/// RAII PATTERN: Automatic timing calculation on disposal
/// </summary>
Task<Result<IPerformanceScope>> StartPerformanceTrackingAsync(string operationName, TimeSpan? warningThreshold = null);

/// <summary>
/// PUBLIC API: Log operation throughput for capacity planning
/// ENTERPRISE: Throughput analysis for scaling decisions
/// </summary>
Task<Result<bool>> LogThroughputAsync(string operationName, int itemsProcessed, TimeSpan duration);
```

### Exception Logging API
```csharp
Task<Result<bool>> LogExceptionAsync(Exception exception, string context, LogLevel level = LogLevel.Error);
Task<Result<T?>> LogServiceExceptionAsync<T>(Exception exception, string serviceName, string operationName, T? fallbackValue = default);
Task<Result<bool>> LogUnhandledExceptionAsync(Exception exception, string? context = null);
```

## ⚡ OPERATION SCOPE PATTERN S RAII

### IOperationScope Interface
```csharp
public interface IOperationScope : IDisposable
{
    string OperationName { get; }
    DateTime StartTime { get; }
    TimeSpan Elapsed { get; }
    bool IsCompleted { get; }
    string? CorrelationId { get; }

    void MarkSuccess(object? result = null);
    void MarkFailure(Exception exception);
    void MarkWarning(string warning);
    void UpdateContext(object additionalContext);
    void SetResult(object result);

    // 🔄 AUTOMATICKÉ DISPOSAL TRACKING:
    // Pri disposal sa automaticky loguje operation completion
    // Ak nie je explicitly marked, loguje sa ako incomplete
}
```

### Real Implementation s Advanced Features
```csharp
internal sealed class OperationScope : IOperationScope
{
    private readonly ILogger _logger;
    private readonly IPerformanceLogger _perfLogger;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private readonly string _correlationId;
    private object? _context;
    private object? _result;
    private bool _isCompleted;
    private readonly List<string> _warnings = new();

    public OperationScope(ILogger logger, IPerformanceLogger perfLogger, string operationName, object? context = null)
    {
        _logger = logger;
        _perfLogger = perfLogger;
        _operationName = operationName;
        _context = context;
        _correlationId = Guid.NewGuid().ToString("N")[..8];
        _stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Operation '{OperationName}' started with correlation ID {CorrelationId}. Context: {@Context}",
            operationName, _correlationId, context);

        // Performance tracking start
        _perfLogger.LogOperationStart(operationName, context);
    }

    public void MarkSuccess(object? result = null)
    {
        if (_isCompleted) return;

        _stopwatch.Stop();
        _result = result;
        _isCompleted = true;

        _logger.LogInformation("Operation '{OperationName}' completed successfully in {Duration}ms. CorrelationId: {CorrelationId}. Result: {@Result}",
            _operationName, _stopwatch.ElapsedMilliseconds, _correlationId, result);

        // Performance metrics
        _perfLogger.LogOperationTiming(_operationName, _stopwatch.Elapsed);

        // Log warnings if any
        if (_warnings.Count > 0)
        {
            _logger.LogWarning("Operation '{OperationName}' completed with {WarningCount} warnings: {Warnings}",
                _operationName, _warnings.Count, _warnings);
        }
    }

    public void MarkFailure(Exception exception)
    {
        if (_isCompleted) return;

        _stopwatch.Stop();
        _isCompleted = true;

        _logger.LogError(exception, "Operation '{OperationName}' failed after {Duration}ms. CorrelationId: {CorrelationId}. Context: {@Context}",
            _operationName, _stopwatch.ElapsedMilliseconds, _correlationId, _context);

        // Performance metrics for failures
        _perfLogger.LogOperationFailure(_operationName, _stopwatch.Elapsed, exception);
    }

    public void MarkWarning(string warning)
    {
        _warnings.Add(warning);
        _logger.LogWarning("Operation '{OperationName}' warning: {Warning}. CorrelationId: {CorrelationId}",
            _operationName, warning, _correlationId);
    }

    public void UpdateContext(object additionalContext)
    {
        _context = new { Original = _context, Additional = additionalContext };
        _logger.LogTrace("Operation '{OperationName}' context updated. CorrelationId: {CorrelationId}",
            _operationName, _correlationId);
    }

    public void SetResult(object result)
    {
        _result = result;
    }

    public void Dispose()
    {
        if (!_isCompleted)
        {
            _stopwatch.Stop();
            _logger.LogWarning("Operation '{OperationName}' disposed without explicit completion after {Duration}ms. CorrelationId: {CorrelationId}",
                _operationName, _stopwatch.ElapsedMilliseconds, _correlationId);

            // Track incomplete operations
            _perfLogger.LogIncompleteOperation(_operationName, _stopwatch.Elapsed);
        }
    }

    // Properties implementation
    public string OperationName => _operationName;
    public DateTime StartTime { get; } = DateTime.UtcNow;
    public TimeSpan Elapsed => _stopwatch.Elapsed;
    public bool IsCompleted => _isCompleted;
    public string? CorrelationId => _correlationId;
}
```

## 🔍 NULL PATTERN IMPLEMENTATIONS

### NullOperationLogger<T>
```csharp
internal sealed class NullOperationLogger<T> : IOperationLogger<T>
{
    public static readonly NullOperationLogger<T> Instance = new();

    private NullOperationLogger() { }

    public IOperationScope LogOperationStart(string operationName, object? context = null)
        => NullOperationScope.Instance;

    public Task LogOperationStartAsync(string operationName, object? context = null)
        => Task.CompletedTask;

    public void LogOperationSuccess(string operationName, object? result = null, TimeSpan? duration = null) { }
    public void LogOperationFailure(string operationName, Exception exception, object? context = null) { }
    public void LogOperationWarning(string operationName, string warning, object? context = null) { }

    // Extended methods - all no-op
    public IOperationScope LogCommandOperationStart<TCommand>(TCommand command, object? parameters = null) => NullOperationScope.Instance;
    public void LogCommandSuccess<TCommand>(string commandType, TCommand command, TimeSpan duration) { }
    public void LogCommandFailure<TCommand>(string commandType, TCommand command, Exception exception, TimeSpan duration) { }
    public void LogFilterOperation(string filterType, string filterName, int totalRows, int matchingRows, TimeSpan duration) { }
    public void LogAdvancedFilterOperation(string businessRule, int totalFilters, int totalRows, int matchingRows, TimeSpan duration) { }
    public void LogImportOperation(string importType, int totalRows, int importedRows, TimeSpan duration) { }
    public void LogExportOperation(string exportType, int totalRows, int exportedRows, TimeSpan duration) { }
    public void LogValidationOperation(string validationType, int totalRows, int validRows, int ruleCount, TimeSpan duration) { }
    public void LogPerformanceMetrics(string operationType, object metrics) { }
    public void LogLINQOptimization(string operationType, bool usedParallel, bool usedShortCircuit, TimeSpan duration) { }

    // 🔄 AUTOMATICKÉ NULL PATTERN:
    // Pre scenarios kde logging nie je potrebný (testing, headless mode)
    // Poskytuje zero-overhead implementation s full interface compliance
}
```

### NullOperationScope
```csharp
internal sealed class NullOperationScope : IOperationScope
{
    public static readonly NullOperationScope Instance = new();

    private NullOperationScope() { }

    public string OperationName => "";
    public DateTime StartTime => DateTime.MinValue;
    public TimeSpan Elapsed => TimeSpan.Zero;
    public bool IsCompleted => true;
    public string? CorrelationId => null;

    public void MarkSuccess(object? result = null) { }
    public void MarkFailure(Exception exception) { }
    public void MarkWarning(string warning) { }
    public void UpdateContext(object additionalContext) { }
    public void SetResult(object result) { }
    public void Dispose() { }
}
```

## 📊 ENHANCED SERVICE LOGGING PATTERNS

### Filter Service Integration
```csharp
// Application/Services/FilterService.cs - ENHANCED LOGGING
internal sealed class FilterService : IFilterService
{
    private readonly ILogger<FilterService> _logger;
    private readonly IFilterLogger<FilterService> _filterLogger;
    private readonly ICommandLogger<FilterService> _commandLogger;

    public async Task<FilterResult> ApplyFilterAsync(ApplyFilterCommand command)
    {
        using var scope = _filterLogger.LogCommandOperationStart(command,
            new { filterName = command.Filter.FilterName, scope = command.Scope });

        _logger.LogInformation("Applying filter '{FilterName}' to {RowCount} rows with operator {Operator}",
            command.Filter.FilterName ?? "Unnamed",
            command.Data.Count(),
            command.Filter.Operator);

        try
        {
            var result = await ExecuteFilterCommandAsync(command);

            _filterLogger.LogFilterOperation("SingleFilter",
                command.Filter.FilterName ?? "Unnamed",
                result.OriginalRowCount,
                result.FilteredRowCount,
                result.FilterTime);

            _logger.LogInformation("Filter applied successfully: {FilteredRows}/{OriginalRows} rows matched in {Duration}ms",
                result.FilteredRowCount, result.OriginalRowCount, result.FilterTime.TotalMilliseconds);

            scope.MarkSuccess(new { filteredCount = result.FilteredRowCount });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Filter application failed for filter '{FilterName}'",
                command.Filter.FilterName ?? "Unnamed");
            scope.MarkFailure(ex);
            throw;
        }
    }
}
```

### Service Integration Patterns
```csharp
// V každom service (príklad SmartOperationsService):
public async Task<SmartDeleteResult> AnalyzeSmartDeleteAsync(
    IReadOnlyList<Dictionary<string, object?>> data,
    IReadOnlyList<ColumnDefinition> columns,
    GridBehaviorConfiguration behavior,
    cancellationToken cancellationToken = default)
{
    using var scope = _operationLogger.LogOperationStart("AnalyzeSmartDelete",
        new { rowCount = data.Count, enableSmartDelete = behavior.EnableSmartDelete });

    if (!behavior.EnableSmartDelete || data.Count == 0)
    {
        _logger.LogInformation("Smart delete analysis skipped: EnableSmartDelete={EnableSmartDelete}, RowCount={RowCount}",
            behavior.EnableSmartDelete, data.Count);
        scope.MarkSuccess(SmartDeleteResult.NoAction());
        return SmartDeleteResult.NoAction();
    }

    try
    {
        _logger.LogInformation("Starting smart delete analysis for {RowCount} rows", data.Count);

        // Business logic...
        var result = SmartDeleteResult.WithSuggestions(deleteSuggestions);

        _logger.LogInformation("Smart delete analysis completed successfully. Found {SuggestionCount} suggestions",
            deleteSuggestions.Count);
        scope.MarkSuccess(result);
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during smart delete analysis");
        var errorResult = SmartDeleteResult.Error($"Smart delete analysis failed: {ex.Message}");
        scope.MarkFailure(ex);
        return errorResult;
    }
}
```

### Exception Handler Integration
```csharp
// V ExceptionHandlerService:
public async Task<T?> HandleServiceExceptionAsync<T>(Exception exception, string serviceName, string operationName, T? fallbackValue = default)
{
    using var scope = _operationLogger.LogOperationStart("HandleServiceException",
        new { serviceName, operationName, fallbackValueType = typeof(T).Name });

    try
    {
        await RecordExceptionAsync(exception, "Service", $"{serviceName}.{operationName}");
        Interlocked.Increment(ref _serviceExceptions);

        _logger.LogError(exception,
            "Service exception in {ServiceName}.{OperationName}. Returning fallback value: {@FallbackValue}",
            serviceName, operationName, fallbackValue);

        scope.MarkSuccess(fallbackValue);
        return fallbackValue;
    }
    catch (Exception ex)
    {
        scope.MarkFailure(ex);
        _logger.LogCritical(ex, "CRITICAL: Exception handler failed while processing service exception");
        return fallbackValue;
    }
}
```

## **📋 INTERNAL DI REGISTRATION PATTERN:**
```csharp
// Infrastructure/Services/InternalServiceRegistration.cs
public static IServiceCollection AddAdvancedWinUiDataGridInternal(this IServiceCollection services)
{
    // Core logging infrastructure
    services.AddSingleton(typeof(IOperationLogger<>), typeof(OperationLogger<>));
    services.AddSingleton(typeof(IUIInteractionLogger<>), typeof(UIInteractionLogger<>));
    services.AddSingleton(typeof(ICommandLogger<>), typeof(CommandLogger<>));

    // Module-specific loggers
    services.AddSingleton(typeof(IFilterLogger<>), typeof(FilterLogger<>));
    services.AddSingleton(typeof(IValidationLogger<>), typeof(ValidationLogger<>));
    services.AddSingleton(typeof(IImportExportLogger<>), typeof(ImportExportLogger<>));
    services.AddSingleton(typeof(IExceptionLogger<>), typeof(ExceptionLogger<>));
    services.AddSingleton(typeof(IPerformanceLogger<>), typeof(PerformanceLogger<>));

    // Null pattern implementations
    services.AddSingleton(typeof(NullOperationLogger<>));

    // Future module loggers (will be added as modules are implemented)
    // services.AddSingleton(typeof(ISortLogger<>), typeof(SortLogger<>));
    // services.AddSingleton(typeof(ISearchLogger<>), typeof(SearchLogger<>));
    // services.AddSingleton(typeof(IUILogger<>), typeof(UILogger<>));

    return services;
}
```

## ⚡ AUTOMATICKÉ LOGGING PRE VŠETKY SLUŽBY

```csharp
// 🔄 AUTOMATICKÉ LOGGING platí pre VŠETKY services:

// 1. SmartOperationsService - pri analyse operations
// 2. ValidationService - pri validation rule executions
// 3. PerformanceService - pri performance monitoring
// 4. ExceptionHandlerService - pri exception handling
// 5. ImportService - pri import operations
// 6. ExportService - pri export operations
// 7. SearchFilterService - pri search/filter operations
// 8. AutoRowHeightService - pri height calculations

// Implementácia automatického loggingu s LINQ optimization:
internal sealed class AutomaticLoggingService
{
    private readonly ConcurrentDictionary<Type, IOperationLogger> _loggers = new();
    private readonly ObjectPool<LogContext> _contextPool;

    public void RegisterLogger<T>(IOperationLogger<T> logger)
    {
        _loggers.TryAdd(typeof(T), logger);
    }

    // LINQ optimized + thread safe operation logging
    public async Task<TResult> ExecuteWithLoggingAsync<TService, TResult>(
        string operationName,
        Func<Task<TResult>> operation,
        object? context = null)
    {
        if (!_loggers.TryGetValue(typeof(TService), out var logger))
        {
            return await operation();
        }

        using var scope = logger.LogOperationStart(operationName, context);
        try
        {
            var result = await operation();
            scope.MarkSuccess(result);
            return result;
        }
        catch (Exception ex)
        {
            scope.MarkFailure(ex);
            throw;
        }
    }

    // Parallel LINQ processing pre bulk operations
    public async Task LogBulkOperationsAsync<T>(IEnumerable<(string operation, object? context)> operations)
    {
        if (!_loggers.TryGetValue(typeof(T), out var logger)) return;

        var logTasks = operations.AsParallel()
            .Select(async op =>
            {
                using var scope = logger.LogOperationStart(op.operation, op.context);
                // Bulk operation logic
                scope.MarkSuccess();
            })
            .ToArray();

        await Task.WhenAll(logTasks);
    }
}
```

## 🧠 SMART LOGGING STRATEGIES

```csharp
public enum LogLevel
{
    Trace = 0,      // Detailed diagnostic information
    Debug = 1,      // Debugging information
    Information = 2, // General information
    Warning = 3,    // Warning information
    Error = 4,      // Error information
    Critical = 5,   // Critical error information
    None = 6        // No logging
}

public enum LoggingStrategy
{
    Minimal,         // Errors and Critical only
    Standard,        // Information, Warning, Error, Critical
    Verbose,         // All levels including Debug
    Performance,     // Focus on performance metrics
    Diagnostic       // Maximum detail for troubleshooting
}

// Smart decision making algoritmus s LINQ optimization:
public LoggingStrategy GetRecommendedLoggingStrategy(
    int operationComplexity,
    bool isProduction,
    PerformanceImpact performanceRequirements)
{
    // Prahy pre rozhodovanie s performance optimization:
    if (isProduction)
    {
        return performanceRequirements switch
        {
            PerformanceImpact.Critical => LoggingStrategy.Minimal,
            PerformanceImpact.High => LoggingStrategy.Standard,
            _ => LoggingStrategy.Verbose
        };
    }

    // Development mode - more detailed logging
    return operationComplexity switch
    {
        var complexity when complexity > 1000 => LoggingStrategy.Performance,
        var complexity when complexity > 100 => LoggingStrategy.Verbose,
        _ => LoggingStrategy.Diagnostic
    };
}
```

## **📊 FUTURE MODULE EXTENSIONS:**
Ako sa budú pridávať nové moduly do dokumentácií (Search, Sort, UI, Performance, Security, atď.), logging systém sa rozšíri o:

- **ISearchLogger<T>**: Pre search operations, query optimization, result ranking
- **ISortLogger<T>**: Pre sort operations, column sorting, multi-column sorting
- **IUILogger<T>**: Pre UI interactions, user events, performance metrics
- **IPerformanceLogger<T>**: Pre performance monitoring, bottleneck detection
- **ISecurityLogger<T>**: Pre security events, access control, audit trails

Každý nový modul bude mať svoj špecializovaný logger interface registrovaný v `InternalServiceRegistration.cs` a injektovaný cez constructor dependency injection do príslušného service.

## 🎯 PERFORMANCE & OPTIMIZATION

### LINQ Optimizations
- **Lazy evaluation** pre log processing
- **Parallel processing** pre bulk logging operations
- **Streaming** pre real-time log analysis
- **Object pooling** pre LogContext
- **Minimal allocations** s immutable log commands
- **Hash-based correlation lookup** pre performance pri veľkých log volumes

### Thread Safety
- **Immutable log commands** a value objects
- **Atomic log operations**
- **ConcurrentDictionary** pre logger mappings
- **Thread-safe collections** pre log buffers
- **Concurrent log processing** s parallel LINQ

### DI Integration
- **Command factory methods** s dependency injection support
- **Service provider integration** pre external logging services
- **Interface contracts preservation** pri refactoringu

## 🎯 KĽÚČOVÉ VYLEPŠENIA PODĽA KOREKCIÍ

1. **🔄 Automatické operation logging** - platí pre **VŠETKY** services a operations
2. **📋 Structured logging** - queryable log data s consistent format
3. **🔧 RAII pattern** - automatic resource management s operation scopes
4. **📊 Performance tracking** - integrated performance metrics collection
5. **⚡ Performance optimization** - LINQ, parallel processing, object pooling, thread safety
6. **🏗️ Clean Architecture** - Commands v Core, processing v Application, hybrid DI support
7. **🔄 Complete replacement** - .oldbackup_timestamp files, žiadna backward compatibility
8. **🎯 Universal logging interface** - support for any service type
9. **🔍 Advanced diagnostics** - correlation IDs, context tracking, exception analysis
10. **📋 Module-specific loggers** - specialized interfaces pre každý modul
11. **🔄 Enhanced command logging** - comprehensive command pattern integration

---

## **🔍 LOGGING ŠPECIFIKÁCIA PRE INFRASTRUCTURE OPERÁCIE**

### **Internal DI Registration & Service Distribution**
Všetky infrastructure logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a distribuované cez internal DI do všetkých services:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton(typeof(IOperationLogger<>), typeof(OperationLogger<>));
services.AddSingleton<IUIInteractionLogger, UIInteractionLogger>();
services.AddSingleton(typeof(IValidationLogger<>), typeof(ValidationLogger<>));
services.AddSingleton(typeof(IExceptionLogger<>), typeof(ExceptionLogger<>));
services.AddSingleton(typeof(IPerformanceLogger<>), typeof(PerformanceLogger<>));
services.AddSingleton(typeof(ICommandLogger<>), typeof(CommandLogger<>));
services.AddSingleton(typeof(IFilterLogger<>), typeof(FilterLogger<>));
services.AddSingleton(typeof(IImportExportLogger<>), typeof(ImportExportLogger<>));

// Null pattern implementations
services.AddSingleton(typeof(NullOperationLogger<>));

// V každom Service constructor
public ServiceName(
    ILogger<ServiceName> logger,
    IOperationLogger<ServiceName> operationLogger,
    IPerformanceLogger<ServiceName> performanceLogger,
    IExceptionLogger<ServiceName> exceptionLogger)
```

### **Universal Service Logging Integration**
Infrastructure logging systém implementuje comprehensive logging pre všetky service types s automatickým operation tracking a performance monitoring.

### **Operation Execution Logging**
```csharp
// Universal operation logging
using var scope = _operationLogger.LogOperationStart("OperationName",
    new { parameter1 = value1, parameter2 = value2 });

try
{
    var result = await ExecuteOperationAsync();

    _logger.LogInformation("Operation '{OperationName}' completed successfully. Result: {@Result}",
        operationName, result);

    scope.MarkSuccess(result);
    return result;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation '{OperationName}' failed with exception: {Exception}",
        operationName, ex.Message);

    scope.MarkFailure(ex);
    throw;
}
```

### **Performance Metrics Logging**
```csharp
// Performance tracking integration
_performanceLogger.LogOperationTiming("DataValidation", operationDuration, rowCount);
_performanceLogger.LogMemoryUsage(GC.GetTotalMemory(false), "After validation");
_performanceLogger.LogThroughput("RowProcessing", processedRows, totalDuration);

_logger.LogInformation("Performance metrics: Duration={Duration}ms, Memory={Memory}MB, Throughput={Throughput}rows/sec",
    operationDuration.TotalMilliseconds, memoryMB, throughput);
```

### **Exception Handling Logging**
```csharp
// Service exception logging
var fallbackResult = await _exceptionLogger.LogServiceException(exception,
    nameof(ValidationService), "ValidateAllRows", defaultResult);

_logger.LogWarning("Service operation failed, using fallback result: {@FallbackResult}", fallbackResult);

// Unhandled exception logging
await _exceptionLogger.LogUnhandledException(exception,
    $"Context: {operationContext}, CorrelationId: {correlationId}");
```

### **UI Interaction Logging**
```csharp
// User action tracking
_uiLogger.LogUserAction("CellEdit", cellId, new { OldValue = oldValue, NewValue = newValue });
_uiLogger.LogThemeChange("DarkTheme", "LightTheme");
_uiLogger.LogValidationResult("EmailValidation", isValid, validationError);

_logger.LogInformation("UI interaction logged: Action={Action}, Element={Element}, Success={Success}",
    actionType, elementId, wasSuccessful);
```

### **Correlation & Context Tracking**
```csharp
// Correlation ID tracking across operations
var correlationId = Guid.NewGuid().ToString("N")[..8];

using var scope = _operationLogger.LogOperationStart("MultiStepOperation",
    new { CorrelationId = correlationId, InitialContext = context });

_logger.LogInformation("Multi-step operation started with correlation ID {CorrelationId}", correlationId);

// Context updates during operation
scope.UpdateContext(new { Step = "DataValidation", Progress = 50 });
scope.UpdateContext(new { Step = "DataProcessing", Progress = 80 });

_logger.LogInformation("Operation progress: {Progress}%, CorrelationId: {CorrelationId}",
    progress, correlationId);
```

### **Bulk Operations Logging**
```csharp
// Bulk operation logging s parallel processing
var bulkCommands = operations.Select(op => new StructuredLogCommand
{
    Level = LogLevel.Information,
    MessageTemplate = "Bulk operation executed: {OperationName} with result {Result}",
    Parameters = new Dictionary<string, object?> { ["OperationName"] = op.Name, ["Result"] = op.Result }
});

var loggedCount = await facade.LogBulkAsync(bulkCommands);
_logger.LogInformation("Bulk logging completed: {LoggedCount} entries processed", loggedCount);
```

### **Logging Levels Usage:**
- **Information**: General operation information (successful completions, state changes, performance metrics)
- **Warning**: Potentially problematic situations (performance degradation, fallback usage, validation warnings)
- **Error**: Handled errors (service exceptions, validation failures, recoverable errors)
- **Critical**: System-threatening errors (unhandled exceptions, system failures, data corruption)
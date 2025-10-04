# KOMPLETNÁ ŠPECIFIKÁCIA INFRASTRUCTURE EXCEPTION HANDLING SYSTÉMU

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY

### Clean Architecture + Command Pattern
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Exception handlers, recovery services (internal)
- **Core Layer**: Exception domain entities, handling rules (internal)
- **Infrastructure Layer**: Exception tracking, persistence services (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý exception handler má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové typy exceptions bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky exception handlers implementujú `IExceptionHandler`
- **Interface Segregation**: Špecializované interfaces pre rôzne typy exceptions
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable exception commands + encapsulated behavior
- **SOLID**: Single responsibility pre každý exception type
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable exception commands, atomic operation updates
- **Internal DI Registration**: Všetky exception handling časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť .oldbackup_timestamp súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a LINQ optimizations
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované a bezpečné a stabilné riešenie

## 🎯 8 TYPOV EXCEPTION HANDLING STRATÉGIÍ

### 1. **ServiceExceptionHandlingRule**
```csharp
// FLEXIBLE RULE CREATION - nie hardcoded factory methods
var rule = new ServiceExceptionHandlingRule
{
    ServiceName = "ValidationService",
    OperationName = "ValidateAllRows",
    RecoveryStrategy = RecoveryStrategy.RetryWithDefaults,
    MaxRetryAttempts = 3,
    RetryDelay = TimeSpan.FromSeconds(1),
    FallbackValue = default(bool),
    EnableLogging = true
};

// 🔄 AUTOMATICKÉ EXCEPTION RECOVERY:
// Pri výskyte exception v service sa automaticky spustí recovery strategy
// Toto platí pre VŠETKY exception handling rules, nie len ako príklad
```

### 2. **UIExceptionHandlingRule**
```csharp
var rule = new UIExceptionHandlingRule
{
    UIContext = "DataGridCellEditor",
    ExceptionType = typeof(InvalidOperationException),
    UserNotificationLevel = NotificationLevel.Warning,
    RecoveryAction = UIRecoveryAction.RevertToPreviousState,
    ShowErrorDialog = true,
    LogLevel = LogLevel.Warning,

    // 🔄 AUTOMATICKÉ UI EXCEPTION RECOVERY:
    // Pri UI exception sa automaticky revertuje na previous state
    DependentElements = new[] { "CellEditor", "ValidationState" }
};
```

### 3. **AsyncExceptionHandlingRule**
```csharp
var rule = new AsyncExceptionHandlingRule
{
    OperationName = "ImportDataAsync",
    CancellationStrategy = CancellationStrategy.GracefulShutdown,
    TimeoutHandling = TimeoutHandling.ExtendWithWarning,
    ProgressRecovery = ProgressRecovery.RestoreFromCheckpoint,
    CleanupActions = new[] { "TempFileCleanup", "ConnectionCleanup" },

    // 🔄 AUTOMATICKÉ ASYNC EXCEPTION RECOVERY pre VŠETKY pravidlá
    DependentOperations = new[] { "ImportDataAsync", "ExportDataAsync" }
};
```

### 4. **ValidationExceptionHandlingRule**
```csharp
var rule = new ValidationExceptionHandlingRule
{
    ValidationRuleName = "EmailValidation",
    OnValidationFailure = ValidationFailureAction.MarkAsInvalid,
    OnValidationException = ValidationExceptionAction.DisableRule,
    RetryValidation = true,
    MaxValidationRetries = 2,

    RecoveryFunction = (exception, context) =>
    {
        // Custom recovery logic for validation exceptions
        return ValidationResult.CreateWarning("Validation temporarily unavailable");
    },

    // 🔄 AUTOMATICKÉ VALIDATION EXCEPTION RECOVERY pre VŠETKY pravidlá
    DependentRules = new[] { "EmailValidation", "PhoneValidation" }
};
```

### 5. **DataAccessExceptionHandlingRule**
```csharp
var rule = new DataAccessExceptionHandlingRule
{
    DataSource = "FileSystem",
    OperationType = "ReadData",
    ConnectionRecovery = ConnectionRecovery.RetryWithBackoff,
    DataIntegrityCheck = true,
    BackupDataSource = "MemoryCache",

    // 🔄 AUTOMATICKÉ DATA ACCESS RECOVERY pre VŠETKY pravidlá
    DependentDataSources = new[] { "FileSystem", "MemoryCache", "NetworkStorage" },

    RecoveryFunction = (exception, context) =>
    {
        // Switch to backup data source
        return DataAccessResult.SwitchToBackup(context.BackupDataSource);
    }
};
```

### 6. **CriticalSystemExceptionHandlingRule**
```csharp
var rule = new CriticalSystemExceptionHandlingRule
{
    ExceptionCategory = ExceptionCategory.OutOfMemory,
    EmergencyAction = EmergencyAction.ForceGarbageCollection,
    SystemRecovery = SystemRecovery.RestartComponent,
    NotifySystemAdministrator = true,
    CreateDumpFile = true,

    // 🔄 AUTOMATICKÉ CRITICAL SYSTEM RECOVERY pre VŠETKY pravidlá
    DependentSystemComponents = new[] { "DataGrid", "ValidationEngine", "ImportExport" },

    EmergencyRecoveryFunction = (exception, context) =>
    {
        // Emergency system recovery
        GC.Collect();
        GC.WaitForPendingFinalizers();
        return SystemRecoveryResult.ComponentRestart();
    }
};
```

### 7. **PerformanceExceptionHandlingRule**
```csharp
var rule = new PerformanceExceptionHandlingRule
{
    PerformanceThreshold = TimeSpan.FromSeconds(10),
    MemoryThresholdMB = 1000,
    OnPerformanceDegradation = PerformanceDegradationAction.EnableOptimizations,
    OnMemoryPressure = MemoryPressureAction.ClearCaches,

    // 🔄 AUTOMATICKÉ PERFORMANCE RECOVERY pre VŠETKY pravidlá
    DependentPerformanceMetrics = new[] { "ResponseTime", "MemoryUsage", "CPUUsage" },

    OptimizationFunction = (performanceData, context) =>
    {
        return PerformanceOptimizationResult.EnableParallelProcessing();
    }
};
```

### 8. **SecurityExceptionHandlingRule**
```csharp
var rule = new SecurityExceptionHandlingRule
{
    SecurityContext = "DataAccess",
    ThreatLevel = ThreatLevel.Medium,
    SecurityAction = SecurityAction.LogAndContinue,
    AuditRequired = true,
    IsolateOperation = false,

    // 🔄 AUTOMATICKÉ SECURITY EXCEPTION HANDLING pre VŠETKY pravidlá
    DependentSecurityContexts = new[] { "DataAccess", "FileAccess", "NetworkAccess" },

    SecurityResponseFunction = (securityException, context) =>
    {
        // Log security event and continue with restricted permissions
        return SecurityResponseResult.ContinueWithRestrictions();
    }
};
```

## ⚡ PRIORITY-BASED EXCEPTION HANDLING

### **Priority-based exception handling príklady:**

```csharp
// Logické poradie exception rules pre jeden service
var criticalRule = new ExceptionHandlingRule("OutOfMemoryException", SystemRecovery.Restart, Priority: 1, RuleName: "CriticalMemory");
var serviceRule = new ExceptionHandlingRule("ServiceException", RecoveryStrategy.Retry, Priority: 2, RuleName: "ServiceRecovery");
var validationRule = new ExceptionHandlingRule("ValidationException", RecoveryStrategy.Fallback, Priority: 3, RuleName: "ValidationFallback");

// Cross-service exception handling s prioritou
var globalExceptionRule = new GlobalExceptionHandlingRule(
    new[] { "ValidationService", "ImportService", "ExportService" },
    exception => HandleGlobalException(exception),
    "Global exception recovery",
    Priority: 10,
    RuleName: "GlobalExceptionHandler"
);
```

## 📋 COMMAND PATTERN PRE EXCEPTION HANDLING

### HandleExceptionCommand
```csharp
public sealed record HandleExceptionCommand<T> where T : Exception
{
    public required T Exception { get; init; }
    public required string Context { get; init; }
    public RecoveryStrategy Strategy { get; init; } = RecoveryStrategy.Automatic;
    public object? FallbackValue { get; init; }
    public bool EnableRetry { get; init; } = true;
    public int MaxRetryAttempts { get; init; } = 3;

    // Factory methods pre FLEXIBLE creation s DI support
    public static HandleExceptionCommand<T> Create(T exception, string context) =>
        new() { Exception = exception, Context = context };

    public static HandleExceptionCommand<T> WithStrategy(T exception, string context, RecoveryStrategy strategy) =>
        new() { Exception = exception, Context = context, Strategy = strategy };

    // DI factory method
    public static HandleExceptionCommand<T> CreateWithDI(T exception, string context, IServiceProvider services) =>
        new() { Exception = exception, Context = context };
}
```

### RecoverFromExceptionCommand
```csharp
public sealed record RecoverFromExceptionCommand
{
    public required Exception Exception { get; init; }
    public required RecoveryStrategy Strategy { get; init; }
    public object? RecoveryContext { get; init; }
    public TimeSpan? RetryDelay { get; init; }
    public Func<Exception, object?, Task<object?>>? CustomRecoveryFunction { get; init; }

    // FLEXIBLE factory methods s DI support
    public static RecoverFromExceptionCommand Create(Exception exception, RecoveryStrategy strategy) =>
        new() { Exception = exception, Strategy = strategy };

    public static RecoverFromExceptionCommand WithCustomRecovery(
        Exception exception,
        Func<Exception, object?, Task<object?>> recoveryFunction) =>
        new() { Exception = exception, Strategy = RecoveryStrategy.Custom, CustomRecoveryFunction = recoveryFunction };
}
```

### LogExceptionCommand
```csharp
public sealed record LogExceptionCommand
{
    public required Exception Exception { get; init; }
    public required LogLevel Level { get; init; }
    public required string Context { get; init; }
    public string? CorrelationId { get; init; }
    public bool IncludeStackTrace { get; init; } = true;
    public bool NotifyAdministrators { get; init; } = false;

    // FLEXIBLE factory methods s LINQ optimization
    public static LogExceptionCommand Create(Exception exception, LogLevel level, string context) =>
        new() { Exception = exception, Level = level, Context = context };

    public static LogExceptionCommand WithCorrelation(Exception exception, LogLevel level, string context, string correlationId) =>
        new() { Exception = exception, Level = level, Context = context, CorrelationId = correlationId };

    // LINQ optimized factory pre bulk exception logging
    public static IEnumerable<LogExceptionCommand> CreateBulk(
        IEnumerable<(Exception exception, LogLevel level, string context)> exceptions) =>
        exceptions.Select(ex => Create(ex.exception, ex.level, ex.context));
}
```

## 🎯 FAÇADE API METÓDY

### Universal Exception Handling API
```csharp
// FLEXIBLE generic approach - nie hardcoded factory methods
Task<Result<T?>> HandleExceptionAsync<T>(Exception exception, string context, T? fallbackValue = default);

// Príklady použitia:
var result = await facade.HandleExceptionAsync(validationException, "DataValidation", false);
var data = await facade.HandleExceptionAsync(importException, "DataImport", Array.Empty<DataRow>());
var config = await facade.HandleExceptionAsync(configException, "Configuration", defaultConfiguration);
```

### Exception Recovery Management
```csharp
Task<Result<bool>> AddExceptionHandlingRuleAsync<T>(T rule) where T : IExceptionHandlingRule;
Task<Result<bool>> RemoveExceptionHandlingRuleAsync(string ruleName);
Task<Result<bool>> RemoveExceptionHandlingRulesAsync(params Type[] exceptionTypes);
Task<Result<bool>> ClearAllExceptionHandlingRulesAsync();
```

### Exception Analytics & Monitoring
```csharp
/// <summary>
/// PUBLIC API: Get comprehensive exception statistics
/// ENTERPRISE: Exception analytics for system health monitoring
/// AUTOMATIC: Real-time exception tracking and pattern analysis
/// LINQ OPTIMIZED: Parallel processing s lazy evaluation
/// THREAD SAFE: Atomic operations, immutable statistics
/// </summary>
Task<Result<ExceptionStatistics>> GetExceptionStatisticsAsync(TimeSpan? timeWindow = null);

/// <summary>
/// PUBLIC API: Get system health assessment based on exceptions
/// ENTERPRISE: Proactive system health monitoring
/// BEHAVIORAL LOGIC: Health levels based on exception patterns
/// </summary>
Task<Result<SystemHealthReport>> GetSystemHealthAsync();

/// <summary>
/// PUBLIC API: Get exception handling recommendations
/// ENTERPRISE: Smart recovery strategy suggestions
/// AUTOMATIC: Analysis-based optimization recommendations
/// </summary>
Task<Result<IReadOnlyList<ExceptionHandlingRecommendation>>> GetHandlingRecommendationsAsync();
```

## 🔄 EXCEPTION RECOVERY STRATEGIES

### Recovery Strategy Enumeration
```csharp
public enum RecoveryStrategy
{
    Automatic,           // Let system decide best recovery
    Retry,              // Retry operation with backoff
    RetryWithDefaults,  // Retry with default parameters
    Fallback,           // Use fallback value/method
    Rollback,           // Rollback to previous state
    Skip,               // Skip failed operation
    Terminate,          // Terminate operation gracefully
    RestartComponent,   // Restart affected component
    Custom              // Use custom recovery function
}

public enum RecoveryResult
{
    Success,            // Recovery successful
    PartialRecovery,    // Partial recovery achieved
    RecoveryFailed,     // Recovery attempt failed
    RequiresManualIntervention, // Manual intervention needed
    SystemRestart       // System restart required
}
```

### Automated Recovery Implementation
```csharp
// FLEXIBLE recovery engine - nie hardcoded recovery methods
internal sealed class AutomaticRecoveryEngine
{
    private readonly ConcurrentDictionary<Type, List<IExceptionHandlingRule>> _exceptionRules = new();
    private readonly ObjectPool<RecoveryContext> _contextPool;

    public async Task<RecoveryResult> ExecuteRecoveryAsync(Exception exception, string context)
    {
        var exceptionType = exception.GetType();
        if (!_exceptionRules.TryGetValue(exceptionType, out var rules))
        {
            // Try base types and interfaces
            rules = FindApplicableRules(exceptionType);
        }

        // Priority-based rule execution with LINQ optimization
        var applicableRules = rules
            .OrderBy(rule => rule.Priority)
            .Where(rule => rule.IsApplicable(exception, context))
            .ToList();

        foreach (var rule in applicableRules)
        {
            try
            {
                var recoveryResult = await ExecuteRecoveryRuleAsync(rule, exception, context);
                if (recoveryResult != RecoveryResult.RecoveryFailed)
                {
                    return recoveryResult;
                }
            }
            catch (Exception recoveryException)
            {
                // Log recovery failure and try next rule
                _logger.LogError(recoveryException,
                    "Recovery rule '{RuleName}' failed for exception {ExceptionType}",
                    rule.RuleName, exceptionType.Name);
            }
        }

        return RecoveryResult.RecoveryFailed;
    }
}
```

## 📊 EXCEPTION MONITORING & ANALYTICS

### ExceptionStatistics Record
```csharp
public sealed record ExceptionStatistics
{
    public int TotalExceptions { get; init; }
    public int UnhandledExceptions { get; init; }
    public int ServiceExceptions { get; init; }
    public int UIExceptions { get; init; }
    public int AsyncExceptions { get; init; }
    public int CriticalExceptions { get; init; }
    public DateTime? LastExceptionTime { get; init; }
    public string? MostFrequentExceptionType { get; init; }
    public TimeSpan MonitoringWindow { get; init; }
    public double ExceptionsPerMinute { get; init; }
    public IReadOnlyDictionary<string, int> ExceptionsByType { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> ExceptionsByService { get; init; } = new Dictionary<string, int>();
    public RecoveryEffectiveness RecoveryStats { get; init; }
}

public sealed record RecoveryEffectiveness
{
    public int TotalRecoveryAttempts { get; init; }
    public int SuccessfulRecoveries { get; init; }
    public int PartialRecoveries { get; init; }
    public int FailedRecoveries { get; init; }
    public double RecoverySuccessRate => TotalRecoveryAttempts > 0
        ? (double)SuccessfulRecoveries / TotalRecoveryAttempts * 100
        : 0;
    public TimeSpan AverageRecoveryTime { get; init; }
}
```

### SystemHealthReport Record
```csharp
public sealed record SystemHealthReport
{
    public SystemHealthLevel OverallHealth { get; init; }
    public IReadOnlyList<HealthIssue> Issues { get; init; } = Array.Empty<HealthIssue>();
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
    public ExceptionTrend ExceptionTrend { get; init; }
    public PerformanceImpact PerformanceImpact { get; init; }
    public DateTime AssessmentTime { get; init; } = DateTime.UtcNow;
    public TimeSpan AssessmentPeriod { get; init; }
    public ComponentHealthStatus ComponentHealth { get; init; }
}

public enum SystemHealthLevel
{
    Healthy,            // System operating normally
    Fair,               // Minor issues present
    Warning,            // Attention needed
    Critical,           // Immediate action required
    SystemFailure       // System compromised
}

public sealed record HealthIssue
{
    public string ComponentName { get; init; } = "";
    public HealthIssueType IssueType { get; init; }
    public string Description { get; init; } = "";
    public HealthIssueSeverity Severity { get; init; }
    public IReadOnlyList<string> SuggestedActions { get; init; } = Array.Empty<string>();
    public DateTime FirstOccurrence { get; init; }
    public int OccurrenceCount { get; init; }
}
```

## ⚡ AUTOMATICKÉ EXCEPTION DETECTION & RECOVERY

```csharp
// 🔄 AUTOMATICKÉ EXCEPTION HANDLING platí pre VŠETKY service operations:

// 1. ServiceExceptionHandlingRule - pre service layer exceptions
// 2. UIExceptionHandlingRule - pre UI interaction exceptions
// 3. AsyncExceptionHandlingRule - pre async operation exceptions
// 4. ValidationExceptionHandlingRule - pre validation exceptions
// 5. DataAccessExceptionHandlingRule - pre data access exceptions
// 6. CriticalSystemExceptionHandlingRule - pre system-level exceptions
// 7. PerformanceExceptionHandlingRule - pre performance degradation
// 8. SecurityExceptionHandlingRule - pre security-related exceptions

// Implementácia automatického exception handling s LINQ optimization:
internal sealed class AutomaticExceptionDetectionService
{
    private readonly ConcurrentDictionary<string, HashSet<IExceptionHandlingRule>> _contextToRulesMap = new();
    private readonly ObjectPool<ExceptionContext> _contextPool;
    private readonly ICircuitBreaker _circuitBreaker;

    public void RegisterRule(IExceptionHandlingRule rule)
    {
        var dependentContexts = GetDependentContexts(rule);
        foreach (var context in dependentContexts)
        {
            _contextToRulesMap.AddOrUpdate(
                context,
                new HashSet<IExceptionHandlingRule> { rule },
                (key, existing) => { existing.Add(rule); return existing; });
        }
    }

    // LINQ optimized + thread safe exception handling
    public async Task<T?> HandleExceptionAsync<T>(
        Exception exception,
        string operationContext,
        T? fallbackValue = default)
    {
        // Circuit breaker check
        if (_circuitBreaker.IsOpen(operationContext))
        {
            throw new CircuitBreakerOpenException($"Circuit breaker open for context: {operationContext}");
        }

        if (_contextToRulesMap.TryGetValue(operationContext, out var rules))
        {
            // Parallel LINQ processing s priority ordering
            var applicableRules = rules.AsParallel()
                .Where(rule => rule.CanHandle(exception))
                .OrderBy(rule => rule.Priority)
                .ToArray();

            foreach (var rule in applicableRules)
            {
                try
                {
                    var recoveryResult = await rule.ExecuteRecoveryAsync(exception, operationContext);
                    if (recoveryResult.IsSuccess)
                    {
                        return recoveryResult.RecoveredValue is T recovered ? recovered : fallbackValue;
                    }
                }
                catch (Exception recoveryException)
                {
                    // Log recovery failure but continue with next rule
                    _logger.LogError(recoveryException,
                        "Exception recovery failed for rule {RuleName}", rule.RuleName);
                }
            }
        }

        // No recovery possible, record failure and return fallback
        await RecordUnrecoverableExceptionAsync(exception, operationContext);
        return fallbackValue;
    }

    // Special handling pre critical system exceptions
    public async Task HandleCriticalSystemExceptionAsync(Exception exception, string systemContext)
    {
        var criticalRules = _contextToRulesMap.Values
            .SelectMany(rules => rules)
            .OfType<CriticalSystemExceptionHandlingRule>()
            .Where(rule => rule.CanHandle(exception))
            .OrderBy(rule => rule.Priority);

        foreach (var rule in criticalRules)
        {
            try
            {
                await rule.ExecuteEmergencyRecoveryAsync(exception, systemContext);
                break; // Stop after first successful critical recovery
            }
            catch (Exception emergencyRecoveryException)
            {
                _logger.LogCritical(emergencyRecoveryException,
                    "CRITICAL: Emergency recovery failed for rule {RuleName}", rule.RuleName);
            }
        }
    }
}
```

## 🧠 SMART EXCEPTION ANALYSIS

```csharp
public enum ExceptionCategory
{
    Transient,          // Temporary, likely to resolve on retry
    Configuration,      // Configuration-related issues
    Resource,           // Resource exhaustion (memory, disk, network)
    Security,           // Security violations
    Data,               // Data-related issues
    Network,            // Network connectivity issues
    Permission,         // Permission/authorization issues
    SystemFailure,      // System-level failures
    ApplicationLogic    // Application logic errors
}

public enum ExceptionSeverity
{
    Low,                // Minor impact, system continues normally
    Medium,             // Moderate impact, some functionality affected
    High,               // Significant impact, major functionality affected
    Critical,           // Severe impact, system stability threatened
    Fatal               // System cannot continue
}

// Smart decision making algoritmus s LINQ optimization:
public async Task<ExceptionHandlingRecommendation> AnalyzeExceptionAsync(
    Exception exception,
    string context,
    ExceptionStatistics currentStats)
{
    var category = ClassifyException(exception);
    var severity = DetermineSeverity(exception, context, currentStats);
    var trend = AnalyzeExceptionTrend(exception.GetType(), currentStats);

    var recommendedStrategy = (category, severity, trend) switch
    {
        (ExceptionCategory.Transient, ExceptionSeverity.Low, _) => RecoveryStrategy.Retry,
        (ExceptionCategory.Resource, ExceptionSeverity.High, _) => RecoveryStrategy.RestartComponent,
        (ExceptionCategory.Security, _, _) => RecoveryStrategy.Terminate,
        (ExceptionCategory.Configuration, _, _) => RecoveryStrategy.RetryWithDefaults,
        (_, ExceptionSeverity.Fatal, _) => RecoveryStrategy.RestartComponent,
        _ => RecoveryStrategy.Automatic
    };

    return new ExceptionHandlingRecommendation
    {
        Exception = exception,
        Context = context,
        Category = category,
        Severity = severity,
        RecommendedStrategy = recommendedStrategy,
        EstimatedRecoveryTime = EstimateRecoveryTime(category, severity),
        AlternativeStrategies = GetAlternativeStrategies(category, severity),
        PreventionSuggestions = GetPreventionSuggestions(exception, currentStats)
    };
}
```

## 🎯 PERFORMANCE & OPTIMIZATION

### LINQ Optimizations
- **Lazy evaluation** pre exception rule processing
- **Parallel processing** pre bulk exception handling
- **Streaming** pre real-time exception monitoring
- **Object pooling** pre ExceptionContext
- **Minimal allocations** s immutable exception commands
- **Hash-based exception type lookup** pre performance pri veľkých rule sets

### Thread Safety
- **Immutable exception commands** a value objects
- **Atomic exception tracking**
- **ConcurrentDictionary** pre rule mappings
- **Thread-safe collections** pre exception statistics
- **Concurrent exception handling** s parallel LINQ

### DI Integration
- **Command factory methods** s dependency injection support
- **Service provider integration** pre external exception services
- **Interface contracts preservation** pri refactoringu

## 🎯 KĽÚČOVÉ VYLEPŠENIA PODĽA KOREKCIÍ

1. **🔄 Automatické exception handling** - platí pre **VŠETKY** service operations a exception types
2. **📋 Comprehensive recovery strategies** - multiple recovery approaches based on exception analysis
3. **🔧 Flexibilné exception rules** - nie hardcoded factory methods, ale flexible object creation
4. **📊 Real-time exception analytics** - continuous monitoring and health assessment
5. **⚡ Performance optimization** - LINQ, parallel processing, object pooling, thread safety
6. **🏗️ Clean Architecture** - Commands v Core, processing v Application, hybrid DI support
7. **🔄 Complete replacement** - .oldbackup_timestamp files, žiadna backward compatibility
8. **🎯 Universal exception handling interface** - support for any exception type
9. **🧠 Smart exception analysis** - automatic categorization and recovery recommendations
10. **📈 Predictive exception management** - trend analysis and proactive prevention

---

## **🔍 LOGGING ŠPECIFIKÁCIA PRE EXCEPTION OPERÁCIE**

### **Internal DI Registration & Service Distribution**
Všetky exception logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a distribuované cez internal DI do `ExceptionHandlerService`:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<IExceptionLogger<ExceptionHandlerService>, ExceptionLogger<ExceptionHandlerService>>();
services.AddSingleton<IOperationLogger<ExceptionHandlerService>, OperationLogger<ExceptionHandlerService>>();
services.AddSingleton<ICommandLogger<ExceptionHandlerService>, CommandLogger<ExceptionHandlerService>>();

// V ExceptionHandlerService constructor
public ExceptionHandlerService(
    ILogger<ExceptionHandlerService> logger,
    IExceptionLogger<ExceptionHandlerService> exceptionLogger,
    IOperationLogger<ExceptionHandlerService> operationLogger,
    ICommandLogger<ExceptionHandlerService> commandLogger)
```

### **Exception Handling Logging Integration**
Exception systém implementuje comprehensive logging pre všetky typy exceptions s automatickým recovery tracking a smart analysis reporting.

### **Exception Occurrence Logging**
```csharp
// Universal exception logging
await _exceptionLogger.LogUnhandledException(exception, context);

_logger.LogError(exception, "Unhandled exception occurred: type={ExceptionType}, context={Context}, stackTrace={StackTrace}",
    exception.GetType().Name, context, exception.StackTrace);

// Service exception specific logging
var fallbackResult = await _exceptionLogger.LogServiceException(exception, serviceName, operationName, fallbackValue);

_logger.LogWarning("Service exception handled: service={ServiceName}, operation={OperationName}, fallback={FallbackValue}",
    serviceName, operationName, fallbackResult);
```

### **Recovery Strategy Logging**
```csharp
// Recovery attempt logging
_logger.LogInformation("Executing recovery strategy: exception={ExceptionType}, strategy={Strategy}, context={Context}",
    exception.GetType().Name, recoveryStrategy, context);

// Recovery result logging
_logger.LogInformation("Recovery completed: success={Success}, strategy={Strategy}, duration={Duration}ms, result={RecoveryResult}",
    wasSuccessful, recoveryStrategy, recoveryTime.TotalMilliseconds, recoveryResult);

// Recovery failure logging
if (!wasSuccessful)
{
    _logger.LogError("Recovery failed: exception={ExceptionType}, strategy={Strategy}, attempts={AttemptCount}, context={Context}",
        exception.GetType().Name, recoveryStrategy, attemptCount, context);
}
```

### **Exception Analytics Logging**
```csharp
// Exception statistics logging
_logger.LogInformation("Exception statistics updated: total={Total}, unhandled={Unhandled}, rate={Rate}exceptions/min, trend={Trend}",
    statistics.TotalExceptions, statistics.UnhandledExceptions, statistics.ExceptionsPerMinute, trend);

// Health assessment logging
_logger.LogInformation("System health assessed: level={HealthLevel}, issues={IssueCount}, recommendations={RecommendationCount}",
    healthReport.OverallHealth, healthReport.Issues.Count, healthReport.Recommendations.Count);

// Critical health alerts
if (healthReport.OverallHealth >= SystemHealthLevel.Critical)
{
    _logger.LogCritical("CRITICAL SYSTEM HEALTH: level={HealthLevel}, critical_issues={CriticalIssues}, immediate_action_required=true",
        healthReport.OverallHealth, healthReport.Issues.Count(i => i.Severity == HealthIssueSeverity.Critical));
}
```

### **Circuit Breaker Logging**
```csharp
// Circuit breaker state logging
_logger.LogWarning("Circuit breaker opened: context={Context}, failure_threshold_exceeded=true, failures={FailureCount}",
    operationContext, currentFailureCount);

_logger.LogInformation("Circuit breaker half-open: context={Context}, testing_recovery=true",
    operationContext);

_logger.LogInformation("Circuit breaker closed: context={Context}, recovery_successful=true, test_duration={Duration}ms",
    operationContext, testDuration.TotalMilliseconds);
```

### **Exception Pattern Analysis Logging**
```csharp
// Exception pattern detection
_logger.LogWarning("Exception pattern detected: type={ExceptionType}, frequency={Frequency}, context={Context}, requires_investigation=true",
    exceptionType, occurrenceFrequency, context);

// Recommendation logging
_logger.LogInformation("Exception handling recommendation: exception={ExceptionType}, category={Category}, severity={Severity}, recommended_strategy={Strategy}",
    exception.GetType().Name, recommendation.Category, recommendation.Severity, recommendation.RecommendedStrategy);
```

### **Logging Levels Usage:**
- **Information**: Successful recoveries, health assessments, statistics updates, pattern analysis
- **Warning**: Exception occurrences, circuit breaker activations, degraded system health, recovery retries
- **Error**: Recovery failures, unhandled exceptions, service failures, component errors
- **Critical**: System-threatening exceptions, critical health issues, emergency recovery activations, fatal system errors
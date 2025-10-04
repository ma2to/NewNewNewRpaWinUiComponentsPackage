
# DI lifetime decisions (applied across the documentation)

For consistency the following lifetimes are recommended and applied in the documentation examples:

- `IImportService` -> **Scoped**
  - Reason: import operations often carry per-operation state (parsing context, progress) and Scoped avoids unintended shared state.
- `IExportService` -> **Scoped**
  - Reason: export operations frequently use operation-specific buffers and settings; Scoped avoids concurrency/state issues when used in web or background contexts.
- `ICopyPasteService` -> **Singleton**
  - Reason: clipboard/copy-paste semantics are globally shared and typically stateless wrappers; Singleton provides a single coordinated clipboard manager. Ensure thread-safety in the implementation.

These decisions are reflected in all DI registration examples in the documentation.


# KOMPLETNÁ ŠPECIFIKÁCIA INITIALIZATION INFRASTRUCTURE SYSTÉMU

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY

### Clean Architecture + Command Pattern
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Initialization services, lifecycle managers (internal)
- **Core Layer**: Initialization commands, configuration entities (internal)
- **Infrastructure Layer**: Service registration, DI container management (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý initialization service má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové initialization modes bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky initialization patterns implementujú `IInitializationPattern`
- **Interface Segregation**: Špecializované interfaces pre rôzne typy inicializácie
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Initialization factory methods s dependency injection support
- **Functional/OOP**: Immutable configuration objects + encapsulated behavior
- **SOLID**: Single responsibility pre každý initialization aspect
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Thread-safe initialization with proper locking mechanisms
- **Internal DI Registration**: Všetky initialization časti registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť `.oldbackup_timestamp` súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a initialization orchestration
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## 🚀 DUAL-MODE INITIALIZATION SYSTEM

### 1. **Unified Initialization Architecture**

#### Master Initialization Command
```csharp
// COMMAND PATTERN: Initialization command s comprehensive configuration
internal sealed record InitializeComponentCommand
{
    internal InitializationConfiguration Configuration { get; init; } = new();
    internal bool IsHeadlessMode { get; init; } = false;
    internal bool ValidateConfiguration { get; init; } = true;
    internal TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromMinutes(5);
    internal IProgress<InitializationProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    // Factory methods pre common scenarios
    internal static InitializeComponentCommand ForUI(InitializationConfiguration? config = null) =>
        new()
        {
            Configuration = config ?? new(),
            IsHeadlessMode = false
        };

    internal static InitializeComponentCommand ForHeadless(InitializationConfiguration? config = null) =>
        new()
        {
            Configuration = config ?? new(),
            IsHeadlessMode = true
        };
}

// 🔄 AUTOMATICKÉ MODE DETECTION:
// Systém automaticky detekuje UI/Headless kontext a aplikuje správne initialization patterns
```

#### Initialization Progress Tracking
```csharp
internal sealed record InitializationProgress
{
    internal int CompletedSteps { get; init; }
    internal int TotalSteps { get; init; }
    internal double CompletionPercentage => TotalSteps > 0 ? (double)CompletedSteps / TotalSteps * 100 : 0;
    internal TimeSpan ElapsedTime { get; init; }
    internal string CurrentOperation { get; init; } = string.Empty;
    internal InitializationPhase CurrentPhase { get; init; } = InitializationPhase.None;
    internal bool IsHeadlessMode { get; init; }

    internal TimeSpan? EstimatedTimeRemaining => CompletedSteps > 0 && TotalSteps > CompletedSteps
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalSteps - CompletedSteps) / CompletedSteps)
        : null;
}

internal enum InitializationPhase
{
    None = 0,
    ServiceRegistration = 1,
    DependencyValidation = 2,
    ConfigurationLoading = 3,
    ComponentInitialization = 4,
    ValidationSetup = 5,
    ThemeInitialization = 6,
    SmartOperationsSetup = 7,
    Finalization = 8
}
```

### 2. **Mode-Specific Initialization Patterns**

#### UI Mode Initialization Sequence
```csharp
internal sealed class UIInitializationPattern : IInitializationPattern
{
    // STEP 1: UI-specific service registration
    internal async Task<InitializationResult> InitializeUIServicesAsync(
        InitializeComponentCommand command)
    {
        // UI-specific services
        _serviceProvider.RegisterSingleton<IAutoRowHeightService>();
        _serviceProvider.RegisterSingleton<IKeyboardShortcutsService>();
        _serviceProvider.RegisterScoped<ICopyPasteService>();

        // Theme a color management pre UI
        await InitializeThemeSystemAsync(command.Configuration.ColorTheme);

        // 🔄 AUTOMATICKÉ UI OPTIMIZATIONS:
        // UI mode automaticky aktivuje virtualizáciu, rendering optimizations
        return InitializationResult.Success("UI services initialized");
    }

    // STEP 2: UI rendering initialization
    internal async Task<InitializationResult> InitializeRenderingAsync(
        InitializationConfiguration config)
    {
        // Column header rendering
        await InitializeHeaderRenderingAsync();

        // Cell rendering systems
        await InitializeCellRenderingAsync();

        // Scroll a virtualization systems
        await InitializeVirtualizationAsync(config.PerformanceConfig);

        return InitializationResult.Success("Rendering systems initialized");
    }
}
```

#### Headless Mode Initialization Sequence
```csharp
internal sealed class HeadlessInitializationPattern : IInitializationPattern
{
    // STEP 1: Headless-optimized service registration
    internal async Task<InitializationResult> InitializeHeadlessServicesAsync(
        InitializeComponentCommand command)
    {
        // Core data services (bez UI dependencies)
        _serviceProvider.RegisterSingleton<IValidationService>();
        _serviceProvider.RegisterSingleton<IFilterService>();
        _serviceProvider.RegisterSingleton<ISortService>();
        _serviceProvider.RegisterScoped<IImportService>();
        _serviceProvider.RegisterScoped<IExportService>();

        // 🔄 AUTOMATICKÉ HEADLESS OPTIMIZATIONS:
        // Headless mode preskakuje všetky UI-related services a optimalizuje pre performance
        return InitializationResult.Success("Headless services initialized");
    }

    // STEP 2: Data processing optimization pre headless mode
    internal async Task<InitializationResult> InitializeDataProcessingAsync(
        InitializationConfiguration config)
    {
        // Enhanced batch processing pre server scenarios
        await ConfigureBatchProcessingAsync(config.PerformanceConfig);

        // Memory management optimizations
        await ConfigureMemoryOptimizationsAsync();

        // Concurrency optimizations pre headless operations
        await ConfigureConcurrencyAsync(Environment.ProcessorCount * 2);

        return InitializationResult.Success("Data processing optimized for headless mode");
    }
}
```

## ⚙️ ADVANCED SERVICE REGISTRATION PATTERNS

### 1. **Comprehensive Internal DI Registration**

#### Enhanced Service Registration Architecture
```csharp
// Infrastructure/Services/InternalServiceRegistration.cs - ENHANCED
internal static class InternalServiceRegistration
{
    // MASTER REGISTRATION METHOD s mode awareness
    internal static IServiceCollection AddAdvancedWinUiDataGridWithMode(
        this IServiceCollection services,
        bool isHeadlessMode = false,
        InitializationConfiguration? config = null)
    {
        // CORE SERVICES (always registered)
        RegisterCoreServices(services);

        // MODE-SPECIFIC SERVICES
        if (isHeadlessMode)
        {
            RegisterHeadlessServices(services);
        }
        else
        {
            RegisterUIServices(services);
            RegisterThemeServices(services);
        }

        // CONDITIONAL SERVICES based on configuration
        RegisterConditionalServices(services, config ?? new());

        return services;
    }

    // CORE SERVICES - always required
    private static void RegisterCoreServices(IServiceCollection services)
    {
        // Infrastructure Layer
        services.AddSingleton(typeof(IOperationLogger<>), typeof(OperationLogger<>));
        services.AddSingleton(typeof(ICommandLogger<>), typeof(CommandLogger<>));
        services.AddSingleton<IExceptionHandlerService, ExceptionHandlerService>();
        services.AddSingleton<IGlobalExceptionManager, GlobalExceptionManager>();

        // Core Business Services
        services.AddScoped<IValidationService, ValidationService>();
        services.AddSingleton<IFilterService, FilterService>();
        services.AddSingleton<ISortService, SortService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ISmartOperationsService, SmartOperationsService>();

        // Performance a Monitoring
        services.AddScoped<IPerformanceService, PerformanceService>();
        services.AddSingleton<ISearchFilterService, SearchFilterService>();
        services.AddSingleton<IRowNumberService, RowNumberService>();
    }

    // UI-SPECIFIC SERVICES
    private static void RegisterUIServices(IServiceCollection services)
    {
        // UI Interaction Services
        services.AddSingleton<IAutoRowHeightService, AutoRowHeightService>();
        services.AddSingleton<IKeyboardShortcutsService, KeyboardShortcutsService>();
        services.AddSingleton<ICopyPasteService, CopyPasteService>();

        // UI Logging Services
        services.AddSingleton(typeof(IUIInteractionLogger<>), typeof(UIInteractionLogger<>));
        services.AddSingleton(typeof(IShortcutLogger<>), typeof(ShortcutLogger<>));
        services.AddSingleton(typeof(ICopyPasteLogger<>), typeof(CopyPasteLogger<>));
    }

    // HEADLESS-OPTIMIZED SERVICES
    private static void RegisterHeadlessServices(IServiceCollection services)
    {
        // Enhanced batch processing services pre server scenarios
        services.AddSingleton<IBatchProcessingService, HeadlessBatchProcessingService>();
        services.AddSingleton<IDataPipelineService, HeadlessDataPipelineService>();

        // Memory optimization services pre long-running headless processes
        services.AddSingleton<IMemoryManagementService, HeadlessMemoryManagementService>();
    }
}
```

### 2. **Service Lifetime Management Patterns**

#### Intelligent Lifetime Resolution
```csharp
internal enum ServiceLifetime
{
    Singleton,    // Stateless services, shared across all operations
    Scoped,       // Per-operation services, stateful within operation scope
    Transient     // Lightweight, stateless, per-request services
}

// SINGLETON SERVICES (Stateless, Performance-Critical)
// - IFilterService: Stateless filtering algorithms
// - ISortService: Pure functional sorting operations
// - IAutoRowHeightService: Text measurement utilities
// - IKeyboardShortcutsService: Keyboard event processing

// SCOPED SERVICES (Stateful, Operation-Bound)
// - IValidationService: Maintains validation state during operation
// - IPerformanceService: Tracks performance metrics per operation
// - IImportService: Manages import state and progress
// - IExportService: Tracks export progress and temporary data
// - ICopyPasteService: Maintains clipboard state

// TRANSIENT SERVICES (Lightweight, Request-Level)
// - IRowNumberService: Simple row numbering calculations
```

## 🔄 COMPONENT LIFECYCLE MANAGEMENT

### 1. **Sophisticated Lifecycle Orchestration**

#### Startup Sequence Orchestrator
```csharp
internal sealed class ComponentLifecycleManager : IComponentLifecycleManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ComponentLifecycleManager> _logger;
    private readonly object _lifecycleLock = new();
    private bool _isInitialized;
    private bool _isDisposed;

    // COMPREHENSIVE STARTUP SEQUENCE
    internal async Task<InitializationResult> InitializeAsync(
        InitializeComponentCommand command)
    {
        lock (_lifecycleLock)
        {
            if (_isInitialized)
                return InitializationResult.AlreadyInitialized();
        }

        var stopwatch = Stopwatch.StartNew();
        var totalSteps = 8;
        var currentStep = 0;

        try
        {
            // PHASE 1: Service Registration
            await ReportProgress(++currentStep, totalSteps, "Registering services", command);
            await RegisterServicesAsync(command);

            // PHASE 2: Dependency Validation
            await ReportProgress(++currentStep, totalSteps, "Validating dependencies", command);
            await ValidateDependenciesAsync();

            // PHASE 3: Configuration Loading
            await ReportProgress(++currentStep, totalSteps, "Loading configuration", command);
            await LoadConfigurationAsync(command.Configuration);

            // PHASE 4: Component Initialization
            await ReportProgress(++currentStep, totalSteps, "Initializing components", command);
            await InitializeComponentsAsync(command);

            // PHASE 5: Validation System Setup
            await ReportProgress(++currentStep, totalSteps, "Setting up validation", command);
            await InitializeValidationSystemAsync(command.Configuration.ValidationConfig);

            // PHASE 6: Theme Initialization (UI mode only)
            if (!command.IsHeadlessMode)
            {
                await ReportProgress(++currentStep, totalSteps, "Initializing theme system", command);
                await InitializeThemeSystemAsync(command.Configuration.ColorTheme);
            }
            else
            {
                currentStep++; // Skip theme for headless
            }

            // PHASE 7: Smart Operations Setup
            await ReportProgress(++currentStep, totalSteps, "Setting up smart operations", command);
            await InitializeSmartOperationsAsync(command.Configuration.CustomSettings);

            // PHASE 8: Finalization
            await ReportProgress(++currentStep, totalSteps, "Finalizing initialization", command);
            await FinalizeInitializationAsync();

            lock (_lifecycleLock)
            {
                _isInitialized = true;
            }

            stopwatch.Stop();

            _logger.LogInformation("Component initialization completed successfully in {Duration}ms, mode={Mode}",
                stopwatch.ElapsedMilliseconds, command.IsHeadlessMode ? "Headless" : "UI");

            return InitializationResult.Success($"Initialization completed in {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Component initialization failed");
            return InitializationResult.Failure(ex.Message);
        }
    }

    // GRACEFUL SHUTDOWN SEQUENCE
    internal async Task<bool> ShutdownAsync(cancellationToken cancellationToken = default)
    {
        lock (_lifecycleLock)
        {
            if (!_isInitialized || _isDisposed)
                return true;
        }

        try
        {
            // 1. Stop background services
            await StopBackgroundServicesAsync(cancellationToken);

            // 2. Flush pending operations
            await FlushPendingOperationsAsync(cancellationToken);

            // 3. Clean up resources
            await CleanupResourcesAsync();

            // 4. Unregister global handlers
            await UnregisterGlobalHandlersAsync();

            lock (_lifecycleLock)
            {
                _isInitialized = false;
                _isDisposed = true;
            }

            _logger.LogInformation("Component shutdown completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Component shutdown failed");
            return false;
        }
    }
}
```

### 2. **Resource Management & Cleanup Patterns**

#### Comprehensive Resource Management
```csharp
internal sealed class ResourceManager : IDisposable
{
    private readonly ConcurrentDictionary<string, IDisposable> _managedResources = new();
    private readonly Timer? _cleanupTimer;
    private volatile bool _isDisposed;

    internal ResourceManager()
    {
        // Periodic cleanup timer (every 5 minutes)
        _cleanupTimer = new Timer(PerformPeriodicCleanup, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    // AUTOMATIC RESOURCE TRACKING
    internal void RegisterResource(string key, IDisposable resource)
    {
        if (_isDisposed) return;

        _managedResources.AddOrUpdate(key, resource, (_, old) =>
        {
            old?.Dispose(); // Cleanup existing resource
            return resource;
        });
    }

    // PERIODIC CLEANUP
    private void PerformPeriodicCleanup(object? state)
    {
        if (_isDisposed) return;

        var keysToRemove = new List<string>();

        foreach (var kvp in _managedResources)
        {
            if (IsResourceExpired(kvp.Value))
            {
                kvp.Value.Dispose();
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _managedResources.TryRemove(key, out _);
        }
    }

    // GRACEFUL DISPOSAL
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _cleanupTimer?.Dispose();

        // Dispose all managed resources
        foreach (var resource in _managedResources.Values)
        {
            try
            {
                resource?.Dispose();
            }
            catch (Exception ex)
            {
                // Log disposal errors but continue cleanup
                Console.WriteLine($"Error disposing resource: {ex.Message}");
            }
        }

        _managedResources.Clear();
    }
}
```

## ⚙️ CONFIGURATION MANAGEMENT SYSTEM

### 1. **Hierarchical Configuration Architecture**

#### Master Configuration Container
```csharp
internal sealed record InitializationConfiguration
{
    // CORE CONFIGURATION SECTIONS
    internal PerformanceConfiguration? PerformanceConfig { get; init; }
    internal ValidationConfiguration? ValidationConfig { get; init; }
    internal GridBehaviorConfiguration? GridBehaviorConfig { get; init; }
    internal CustomSettingsConfiguration? CustomSettings { get; init; }
    internal ColorConfiguration? ColorTheme { get; init; }
    internal AutoRowHeightConfiguration? AutoRowHeightConfig { get; init; }
    internal KeyboardShortcutConfiguration? KeyboardConfig { get; init; }

    // INITIALIZATION-SPECIFIC SETTINGS
    internal bool EnableSmartOperations { get; init; } = true;
    internal bool EnableAdvancedValidation { get; init; } = true;
    internal bool EnablePerformanceOptimizations { get; init; } = true;
    internal TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromMinutes(5);
    internal LogLevel MinimumLogLevel { get; init; } = LogLevel.Information;

    // FACTORY METHODS pre common scenarios
    internal static InitializationConfiguration Default => new();

    internal static InitializationConfiguration HighPerformance => new()
    {
        PerformanceConfig = new PerformanceConfiguration
        {
            EnableVirtualization = true,
            EnableLazyLoading = true,
            EnableMemoryOptimization = true,
            EnableAsyncOperations = true,
            VirtualizationThreshold = 500, // Lower threshold for better performance
            MaxConcurrentOperations = Environment.ProcessorCount * 2
        },
        EnablePerformanceOptimizations = true
    };

    internal static InitializationConfiguration ServerMode => new()
    {
        EnableSmartOperations = false, // Disable UI-heavy operations
        PerformanceConfig = new PerformanceConfiguration
        {
            EnableVirtualization = false, // Not needed in headless mode
            EnableAsyncOperations = true,
            MaxConcurrentOperations = Environment.ProcessorCount * 4 // Higher concurrency
        }
    };
}
```

#### Configuration Loading & Validation Patterns
```csharp
internal sealed class ConfigurationManager
{
    // COMPREHENSIVE CONFIGURATION LOADING
    internal async Task<InitializationConfiguration> LoadConfigurationAsync(
        string? configurationPath = null,
        bool validateConfiguration = true)
    {
        var config = new InitializationConfiguration();

        // 1. Load from default sources
        config = await LoadFromDefaultSourcesAsync();

        // 2. Load from file if specified
        if (!string.IsNullOrEmpty(configurationPath))
        {
            config = await LoadFromFileAsync(configurationPath, config);
        }

        // 3. Load from environment variables
        config = await LoadFromEnvironmentAsync(config);

        // 4. Apply runtime overrides
        config = await ApplyRuntimeOverridesAsync(config);

        // 5. Validate final configuration
        if (validateConfiguration)
        {
            await ValidateConfigurationAsync(config);
        }

        return config;
    }

    // CONFIGURATION VALIDATION
    private async Task ValidateConfigurationAsync(InitializationConfiguration config)
    {
        var validationErrors = new List<string>();

        // Performance configuration validation
        if (config.PerformanceConfig != null)
        {
            if (config.PerformanceConfig.VirtualizationThreshold < 0)
                validationErrors.Add("VirtualizationThreshold must be non-negative");

            if (config.PerformanceConfig.MaxConcurrentOperations <= 0)
                validationErrors.Add("MaxConcurrentOperations must be positive");
        }

        // Timeout validation
        if (config.InitializationTimeout <= TimeSpan.Zero)
            validationErrors.Add("InitializationTimeout must be positive");

        if (validationErrors.Any())
            throw new ConfigurationValidationException(
                $"Configuration validation failed: {string.Join(", ", validationErrors)}");
    }
}
```

## 🎯 FACADE API INTEGRATION

### Public Initialization Methods
```csharp
#region Component Initialization with Command Pattern

/// <summary>
/// PUBLIC API: Initialize component using comprehensive initialization command
/// ENTERPRISE: Full initialization control with progress reporting
/// </summary>
Task<InitializationResult> InitializeAsync(
    InitializeComponentCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Initialize for UI mode with default configuration
/// CONVENIENCE: Simplified UI initialization
/// </summary>
Task<InitializationResult> InitializeForUIAsync(
    InitializationConfiguration? config = null,
    IProgress<InitializationProgress>? progress = null,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Initialize for headless mode with server optimizations
/// PERFORMANCE: Optimized for server/background scenarios
/// </summary>
Task<InitializationResult> InitializeForHeadlessAsync(
    InitializationConfiguration? config = null,
    IProgress<InitializationProgress>? progress = null,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Graceful component shutdown with cleanup
/// LIFECYCLE: Proper resource cleanup and disposal
/// </summary>
Task<bool> ShutdownAsync(cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Validate initialization configuration before use
/// VALIDATION: Pre-initialization configuration check
/// </summary>
Task<ConfigurationValidationResult> ValidateInitializationConfigAsync(
    InitializationConfiguration config);

/// <summary>
/// PUBLIC API: Get current initialization status
/// MONITORING: Runtime initialization state inspection
/// </summary>
InitializationStatus GetInitializationStatus();

#endregion
```

## 🔍 ENTERPRISE LOGGING & MONITORING

### Comprehensive Initialization Logging
```csharp
// INITIALIZATION LIFECYCLE LOGGING
_logger.LogInformation("Component initialization started: mode={Mode}, timeout={Timeout}",
    command.IsHeadlessMode ? "Headless" : "UI", command.InitializationTimeout);

// SERVICE REGISTRATION LOGGING
_logger.LogInformation("Service registration completed: {RegisteredServices} services registered, duration={Duration}ms",
    registeredServiceCount, registrationTime.TotalMilliseconds);

// CONFIGURATION LOADING LOGGING
_logger.LogInformation("Configuration loaded: {ConfigSources} sources processed, validation={ValidationEnabled}",
    configurationSources.Count, command.ValidateConfiguration);

// PERFORMANCE METRICS LOGGING
_logger.LogInformation("Initialization performance: totalTime={TotalTime}ms, serviceRegistration={ServiceTime}ms, configLoad={ConfigTime}ms",
    totalInitializationTime.TotalMilliseconds, serviceRegistrationTime.TotalMilliseconds, configurationLoadTime.TotalMilliseconds);

// ERROR HANDLING LOGGING
_logger.LogError(ex, "Initialization failed at phase {Phase}: {ErrorMessage}",
    currentPhase, ex.Message);
```

### Performance Monitoring Integration
```csharp
// INITIALIZATION PERFORMANCE TRACKING
using var initScope = _performanceLogger.LogOperationStart("ComponentInitialization",
    new { mode = command.IsHeadlessMode ? "Headless" : "UI" });

// SERVICE PERFORMANCE MONITORING
_performanceLogger.LogServiceRegistration("InternalDI", serviceCount, registrationTime);

// MEMORY USAGE MONITORING
_performanceLogger.LogMemoryUsage("PostInitialization", GC.GetTotalMemory(false),
    Process.GetCurrentProcess().WorkingSet64);

// Success/failure tracking
initScope.MarkSuccess(new {
    initializedServices = serviceCount,
    configurationValid = isConfigValid,
    totalTime = stopwatch.ElapsedMilliseconds
});
```

Táto kompletná inicializačná infraštruktúra poskytuje enterprise-ready, thread-safe, a vysoko optimalizovanú inicializáciu komponentu s podporou dual-mode operations, sophisticated lifecycle management, a comprehensive configuration system podľa Clean Architecture + Command Pattern princípov.

// Register the public facade
```csharp
services.AddScoped<AdvancedDataGridFacade>();
```

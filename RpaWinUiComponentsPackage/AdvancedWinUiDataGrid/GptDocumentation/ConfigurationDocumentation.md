# KOMPLETNÁ ŠPECIFIKÁCIA CONFIGURATION MANAGEMENT SYSTÉMU

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY

### Clean Architecture + Command Pattern
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Configuration services, validation handlers (internal)
- **Core Layer**: Configuration entities, validation commands (internal)
- **Infrastructure Layer**: Configuration persistence, file watchers (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý configuration service má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové configuration types bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky configuration handlers implementujú spoločné interface
- **Interface Segregation**: Špecializované interfaces pre rôzne typy konfigurácií
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Configuration factory methods s dependency injection support
- **Functional/OOP**: Immutable configuration objects + encapsulated behavior
- **SOLID**: Single responsibility pre každý configuration aspect
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable configurations, atomic updates
- **Internal DI Registration**: Všetky configuration časti registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť `.oldbackup_timestamp` súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a configuration orchestration
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## ⚙️ DYNAMIC CONFIGURATION SYSTEM

### 1. **ConfigurationManagementService** - Runtime Configuration Control

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Configuration;

/// <summary>
/// ENTERPRISE: Dynamic configuration management with hot-reload capabilities
/// THREAD SAFE: Concurrent configuration updates with atomic operations
/// VALIDATION: Comprehensive configuration validation before application
/// HOT-RELOAD: Real-time configuration updates without restart
/// </summary>
internal sealed class ConfigurationManagementService : IConfigurationManagementService, IDisposable
{
    private readonly ConcurrentDictionary<string, IConfigurationProvider> _providers = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastUpdates = new();
    private readonly ILogger<ConfigurationManagementService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private volatile bool _isDisposed = false;
    private readonly ReaderWriterLockSlim _configurationLock = new();

    public ConfigurationManagementService(
        ILogger<ConfigurationManagementService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        _logger.LogInformation("ConfigurationManagementService initialized");
    }

    /// <summary>
    /// COMMAND PATTERN: Update configuration using command with validation
    /// HOT-RELOAD: Apply configuration changes without restart
    /// </summary>
    public async Task<ConfigurationUpdateResult> UpdateConfigurationAsync(
        UpdateConfigurationCommand command,
        cancellationToken cancellationToken = default)
    {
        _configurationLock.EnterWriteLock();

        try
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Configuration update started: {ConfigurationType}, validateFirst={ValidateFirst}",
                command.ConfigurationType, command.ValidateBeforeApply);

            // Step 1: Validate configuration if requested
            if (command.ValidateBeforeApply)
            {
                var validationResult = await ValidateConfigurationAsync(command.NewConfiguration, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Configuration validation failed: {Errors}",
                        string.Join(", ", validationResult.ValidationErrors));

                    return ConfigurationUpdateResult.Failure(
                        $"Validation failed: {string.Join(", ", validationResult.ValidationErrors)}",
                        stopwatch.Elapsed);
                }
            }

            // Step 2: Backup current configuration
            var backupResult = await BackupCurrentConfigurationAsync(command.ConfigurationType);
            if (!backupResult.Success)
            {
                _logger.LogError("Configuration backup failed: {Error}", backupResult.ErrorMessage);
                return ConfigurationUpdateResult.Failure("Backup failed", stopwatch.Elapsed);
            }

            // Step 3: Apply new configuration
            var applyResult = await ApplyConfigurationAsync(command, cancellationToken);
            if (!applyResult.Success)
            {
                _logger.LogError("Configuration application failed: {Error}", applyResult.ErrorMessage);

                // Rollback to backup
                await RollbackConfigurationAsync(command.ConfigurationType, backupResult.BackupId!);
                return ConfigurationUpdateResult.Failure("Application failed, rolled back", stopwatch.Elapsed);
            }

            // Step 4: Update tracking information
            _lastUpdates[command.ConfigurationType] = DateTime.UtcNow;

            stopwatch.Stop();

            _logger.LogInformation("Configuration update completed: {ConfigurationType}, duration={Duration}ms",
                command.ConfigurationType, stopwatch.ElapsedMilliseconds);

            // Step 5: Notify configuration change
            await NotifyConfigurationChangeAsync(command.ConfigurationType, command.NewConfiguration);

            return ConfigurationUpdateResult.Success(stopwatch.Elapsed, applyResult.AffectedComponents!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration update failed: {ConfigurationType}", command.ConfigurationType);
            return ConfigurationUpdateResult.Failure(ex.Message, TimeSpan.Zero);
        }
        finally
        {
            _configurationLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// VALIDATION: Comprehensive configuration validation
    /// </summary>
    private async Task<ConfigurationValidationResult> ValidateConfigurationAsync(
        object configuration,
        cancellationToken cancellationToken)
    {
        var validationErrors = new List<string>();

        // Validate based on configuration type
        switch (configuration)
        {
            case ValidationConfiguration validationConfig:
                await ValidateValidationConfigurationAsync(validationConfig, validationErrors);
                break;

            case PerformanceConfiguration performanceConfig:
                await ValidatePerformanceConfigurationAsync(performanceConfig, validationErrors);
                break;

            case ColorConfiguration colorConfig:
                await ValidateColorConfigurationAsync(colorConfig, validationErrors);
                break;

            case GridBehaviorConfiguration behaviorConfig:
                await ValidateBehaviorConfigurationAsync(behaviorConfig, validationErrors);
                break;

            default:
                validationErrors.Add($"Unknown configuration type: {configuration.GetType().Name}");
                break;
        }

        return new ConfigurationValidationResult
        {
            IsValid = !validationErrors.Any(),
            ValidationErrors = validationErrors,
            ConfigurationType = configuration.GetType().Name
        };
    }
}

/// <summary>
/// COMMAND PATTERN: Configuration update command
/// </summary>
internal sealed record UpdateConfigurationCommand
{
    internal required string ConfigurationType { get; init; }
    internal required object NewConfiguration { get; init; }
    internal object? PreviousConfiguration { get; init; }
    internal bool ValidateBeforeApply { get; init; } = true;
    internal bool CreateBackup { get; init; } = true;
    internal TimeSpan? UpdateTimeout { get; init; }
    internal string? UpdateSource { get; init; }
    internal IProgress<ConfigurationUpdateProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    // Factory methods
    internal static UpdateConfigurationCommand ForValidation(ValidationConfiguration config, string? source = null) =>
        new()
        {
            ConfigurationType = nameof(ValidationConfiguration),
            NewConfiguration = config,
            UpdateSource = source ?? "Manual"
        };

    internal static UpdateConfigurationCommand ForPerformance(PerformanceConfiguration config, string? source = null) =>
        new()
        {
            ConfigurationType = nameof(PerformanceConfiguration),
            NewConfiguration = config,
            UpdateSource = source ?? "Manual"
        };
}
```

### 2. **FileWatcherConfigurationProvider** - File-Based Hot-Reload

```csharp
/// <summary>
/// ENTERPRISE: File-based configuration provider with hot-reload support
/// FILE WATCHER: Automatic configuration reload on file changes
/// RESILIENT: Error handling and fallback mechanisms
/// </summary>
internal sealed class FileWatcherConfigurationProvider : IConfigurationProvider, IDisposable
{
    private readonly FileSystemWatcher _fileWatcher;
    private readonly ILogger<FileWatcherConfigurationProvider> _logger;
    private readonly string _configurationFilePath;
    private readonly IConfigurationManagementService _configurationService;

    private volatile bool _isDisposed = false;
    private DateTime _lastFileWriteTime;
    private readonly object _reloadLock = new();

    public FileWatcherConfigurationProvider(
        string configurationFilePath,
        IConfigurationManagementService configurationService,
        ILogger<FileWatcherConfigurationProvider> logger)
    {
        _configurationFilePath = configurationFilePath;
        _configurationService = configurationService;
        _logger = logger;

        // Initialize file watcher
        var directory = Path.GetDirectoryName(configurationFilePath);
        var fileName = Path.GetFileName(configurationFilePath);

        _fileWatcher = new FileSystemWatcher(directory!, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        _fileWatcher.Changed += OnConfigurationFileChanged;
        _fileWatcher.Error += OnFileWatcherError;

        _lastFileWriteTime = File.GetLastWriteTime(configurationFilePath);

        _logger.LogInformation("FileWatcherConfigurationProvider initialized: {FilePath}", configurationFilePath);
    }

    /// <summary>
    /// FILE CHANGE HANDLER: Process configuration file changes
    /// </summary>
    private async void OnConfigurationFileChanged(object sender, FileSystemEventArgs e)
    {
        lock (_reloadLock)
        {
            // Debounce rapid file changes
            var currentWriteTime = File.GetLastWriteTime(_configurationFilePath);
            if (currentWriteTime <= _lastFileWriteTime.AddSeconds(1))
                return;

            _lastFileWriteTime = currentWriteTime;
        }

        try
        {
            _logger.LogInformation("Configuration file change detected: {FilePath}", e.FullPath);

            // Wait briefly for file to be fully written
            await Task.Delay(100);

            // Load and apply new configuration
            var loadResult = await LoadConfigurationFromFileAsync();
            if (loadResult.Success)
            {
                var updateCommand = new UpdateConfigurationCommand
                {
                    ConfigurationType = loadResult.ConfigurationType!,
                    NewConfiguration = loadResult.Configuration!,
                    ValidateBeforeApply = true,
                    CreateBackup = true,
                    UpdateSource = "FileWatcher"
                };

                var updateResult = await _configurationService.UpdateConfigurationAsync(updateCommand);

                if (updateResult.Success)
                {
                    _logger.LogInformation("Configuration hot-reload completed: {ConfigurationType}",
                        loadResult.ConfigurationType);
                }
                else
                {
                    _logger.LogError("Configuration hot-reload failed: {Error}", updateResult.ErrorMessage);
                }
            }
            else
            {
                _logger.LogError("Failed to load configuration from file: {Error}", loadResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration file change processing failed");
        }
    }

    /// <summary>
    /// FILE LOADING: Load configuration from JSON file
    /// </summary>
    private async Task<ConfigurationLoadResult> LoadConfigurationFromFileAsync()
    {
        try
        {
            if (!File.Exists(_configurationFilePath))
            {
                return ConfigurationLoadResult.Failure("Configuration file not found");
            }

            var jsonContent = await File.ReadAllTextAsync(_configurationFilePath);
            var configurationWrapper = JsonSerializer.Deserialize<ConfigurationWrapper>(jsonContent);

            if (configurationWrapper == null)
            {
                return ConfigurationLoadResult.Failure("Failed to deserialize configuration");
            }

            return ConfigurationLoadResult.Success(
                configurationWrapper.ConfigurationType,
                configurationWrapper.Configuration);
        }
        catch (Exception ex)
        {
            return ConfigurationLoadResult.Failure($"File loading failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _fileWatcher?.Dispose();

        _logger.LogInformation("FileWatcherConfigurationProvider disposed");
    }
}

/// <summary>
/// Configuration file wrapper for JSON serialization
/// </summary>
internal sealed class ConfigurationWrapper
{
    public required string ConfigurationType { get; init; }
    public required object Configuration { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    public string? UpdateSource { get; init; }
    public int Version { get; init; } = 1;
}
```

## 🔧 ENVIRONMENT-SPECIFIC CONFIGURATION

### 1. **EnvironmentConfigurationService** - Multi-Environment Support

```csharp
/// <summary>
/// ENTERPRISE: Environment-specific configuration management
/// MULTI-ENVIRONMENT: Development, Staging, Production configurations
/// FLEXIBLE: Environment variable integration and override support
/// </summary>
internal sealed class EnvironmentConfigurationService : IEnvironmentConfigurationService
{
    private readonly ILogger<EnvironmentConfigurationService> _logger;
    private readonly Dictionary<string, IConfigurationProfile> _profiles = new();
    private string _currentEnvironment = "Development";

    public EnvironmentConfigurationService(ILogger<EnvironmentConfigurationService> logger)
    {
        _logger = logger;

        // Register built-in profiles
        RegisterBuiltInProfiles();

        // Detect current environment
        _currentEnvironment = DetectEnvironment();

        _logger.LogInformation("EnvironmentConfigurationService initialized for environment: {Environment}",
            _currentEnvironment);
    }

    /// <summary>
    /// ENVIRONMENT DETECTION: Automatic environment detection
    /// </summary>
    private string DetectEnvironment()
    {
        // Check environment variable first
        var environment = Environment.GetEnvironmentVariable("DATAGRID_ENVIRONMENT");
        if (!string.IsNullOrEmpty(environment))
            return environment;

        // Check command line arguments
        var args = Environment.GetCommandLineArgs();
        var envArg = args.FirstOrDefault(a => a.StartsWith("--environment="));
        if (envArg != null)
            return envArg.Substring(14);

        // Debug builds default to Development
#if DEBUG
        return "Development";
#else
        return "Production";
#endif
    }

    /// <summary>
    /// BUILT-IN PROFILES: Register default environment configurations
    /// </summary>
    private void RegisterBuiltInProfiles()
    {
        // Development Profile
        _profiles["Development"] = new DevelopmentConfigurationProfile();
        _profiles["Dev"] = _profiles["Development"]; // Alias

        // Staging Profile
        _profiles["Staging"] = new StagingConfigurationProfile();
        _profiles["Stage"] = _profiles["Staging"]; // Alias

        // Production Profile
        _profiles["Production"] = new ProductionConfigurationProfile();
        _profiles["Prod"] = _profiles["Production"]; // Alias

        // High-Performance Profile
        _profiles["HighPerformance"] = new HighPerformanceConfigurationProfile();
        _profiles["Performance"] = _profiles["HighPerformance"]; // Alias
    }

    /// <summary>
    /// CONFIGURATION FACTORY: Create environment-specific configuration
    /// </summary>
    public async Task<TConfiguration> GetConfigurationForEnvironmentAsync<TConfiguration>(
        string? environmentOverride = null)
        where TConfiguration : class
    {
        var environment = environmentOverride ?? _currentEnvironment;

        if (!_profiles.TryGetValue(environment, out var profile))
        {
            _logger.LogWarning("Unknown environment profile: {Environment}, falling back to Development",
                environment);
            profile = _profiles["Development"];
        }

        var configuration = await profile.CreateConfigurationAsync<TConfiguration>();

        // Apply environment variable overrides
        await ApplyEnvironmentVariableOverridesAsync(configuration);

        _logger.LogDebug("Configuration created for environment: {Environment}, type: {ConfigurationType}",
            environment, typeof(TConfiguration).Name);

        return configuration;
    }

    /// <summary>
    /// ENVIRONMENT VARIABLES: Apply environment variable overrides
    /// </summary>
    private async Task ApplyEnvironmentVariableOverridesAsync<TConfiguration>(TConfiguration configuration)
        where TConfiguration : class
    {
        await Task.Run(() =>
        {
            // Apply environment-specific overrides based on configuration type
            switch (configuration)
            {
                case PerformanceConfiguration perf:
                    ApplyPerformanceEnvironmentOverrides(perf);
                    break;

                case ValidationConfiguration validation:
                    ApplyValidationEnvironmentOverrides(validation);
                    break;

                case GridBehaviorConfiguration behavior:
                    ApplyBehaviorEnvironmentOverrides(behavior);
                    break;
            }
        });
    }

    /// <summary>
    /// PERFORMANCE OVERRIDES: Environment-specific performance configuration
    /// </summary>
    private void ApplyPerformanceEnvironmentOverrides(PerformanceConfiguration config)
    {
        // DATAGRID_MAX_CONCURRENT_OPS
        if (int.TryParse(Environment.GetEnvironmentVariable("DATAGRID_MAX_CONCURRENT_OPS"), out var maxOps))
        {
            config = config with { MaxConcurrentOperations = maxOps };
            _logger.LogDebug("Environment override applied: MaxConcurrentOperations = {Value}", maxOps);
        }

        // DATAGRID_VIRTUALIZATION_THRESHOLD
        if (int.TryParse(Environment.GetEnvironmentVariable("DATAGRID_VIRTUALIZATION_THRESHOLD"), out var threshold))
        {
            config = config with { VirtualizationThreshold = threshold };
            _logger.LogDebug("Environment override applied: VirtualizationThreshold = {Value}", threshold);
        }

        // DATAGRID_ENABLE_MEMORY_OPTIMIZATION
        if (bool.TryParse(Environment.GetEnvironmentVariable("DATAGRID_ENABLE_MEMORY_OPTIMIZATION"), out var memOpt))
        {
            config = config with { EnableMemoryOptimization = memOpt };
            _logger.LogDebug("Environment override applied: EnableMemoryOptimization = {Value}", memOpt);
        }
    }
}

/// <summary>
/// CONFIGURATION PROFILES: Environment-specific configuration templates
/// </summary>
internal interface IConfigurationProfile
{
    Task<TConfiguration> CreateConfigurationAsync<TConfiguration>() where TConfiguration : class;
    string ProfileName { get; }
    string Description { get; }
}

internal sealed class DevelopmentConfigurationProfile : IConfigurationProfile
{
    public string ProfileName => "Development";
    public string Description => "Development environment with detailed logging and validation";

    public async Task<TConfiguration> CreateConfigurationAsync<TConfiguration>()
        where TConfiguration : class
    {
        return await Task.FromResult(typeof(TConfiguration).Name switch
        {
            nameof(PerformanceConfiguration) => (TConfiguration)(object)new PerformanceConfiguration
            {
                EnableVirtualization = false, // Disable for easier debugging
                EnableLazyLoading = false,
                EnableMemoryOptimization = false,
                VirtualizationThreshold = 10000, // Higher threshold
                BatchSize = 50, // Smaller batches for debugging
                OperationTimeout = TimeSpan.FromMinutes(10), // Longer timeout
                MaxConcurrentOperations = 2 // Lower concurrency
            },

            nameof(ValidationConfiguration) => (TConfiguration)(object)ValidationConfiguration.Responsive,

            _ => throw new ArgumentException($"Unknown configuration type: {typeof(TConfiguration).Name}")
        });
    }
}
```

## 🎯 FACADE API INTEGRATION

### Public Configuration Management Methods
```csharp
#region Configuration Management with Command Pattern

/// <summary>
/// PUBLIC API: Update component configuration dynamically
/// HOT-RELOAD: Apply configuration changes without restart
/// </summary>
Task<ConfigurationUpdateResult> UpdateConfigurationAsync(
    UpdateConfigurationCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Validate configuration before application
/// VALIDATION: Comprehensive configuration validation
/// </summary>
Task<ConfigurationValidationResult> ValidateConfigurationAsync(
    object configuration,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Get current configuration for specific type
/// INTROSPECTION: Runtime configuration inspection
/// </summary>
TConfiguration GetCurrentConfiguration<TConfiguration>() where TConfiguration : class;

/// <summary>
/// PUBLIC API: Reset configuration to default values
/// RESET: Restore default configuration with backup
/// </summary>
Task<ConfigurationUpdateResult> ResetConfigurationToDefaultAsync<TConfiguration>(
    cancellationToken cancellationToken = default) where TConfiguration : class;

/// <summary>
/// PUBLIC API: Load configuration from external file
/// FILE LOADING: External configuration file support
/// </summary>
Task<ConfigurationUpdateResult> LoadConfigurationFromFileAsync(
    string filePath,
    bool enableHotReload = false,
    cancellationToken cancellationToken = default);

#endregion
```

Táto konfiguračná infraštruktúra poskytuje enterprise-ready, thread-safe, a vysoko flexibilný configuration management system s podporou hot-reload, environment-specific settings, comprehensive validation, a automatic file watching podľa Clean Architecture + Command Pattern princípov.
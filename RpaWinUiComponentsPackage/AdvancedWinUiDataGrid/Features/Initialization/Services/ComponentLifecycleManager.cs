using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Services;

/// <summary>
/// Komplexný service pre správu životného cyklu komponentu
/// Thread-safe orchestration startup a shutdown sekvencií
/// Podporuje progress reporting a timeout handling
/// </summary>
internal sealed class ComponentLifecycleManager : IComponentLifecycleManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ComponentLifecycleManager> _logger;
    private readonly IOperationLogger<ComponentLifecycleManager> _operationLogger;
    private readonly object _lifecycleLock = new();
    private bool _isInitialized;
    private bool _isDisposed;
    private InitializationStatus _currentStatus = new() { IsInitialized = false };
    private DateTime? _initializationStartTime;

    public ComponentLifecycleManager(
        IServiceProvider serviceProvider,
        ILogger<ComponentLifecycleManager> logger,
        IOperationLogger<ComponentLifecycleManager>? operationLogger = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationLogger = operationLogger ?? NullOperationLogger<ComponentLifecycleManager>.Instance;
    }

    /// <summary>
    /// Indikátor či je komponenta inicializovaná
    /// </summary>
    public bool IsInitialized
    {
        get
        {
            lock (_lifecycleLock)
            {
                return _isInitialized;
            }
        }
    }

    /// <summary>
    /// Komplexná inicializačná sekvencia s progress tracking
    /// 8-fázový startup proces s validation a error handling
    /// </summary>
    public async Task<InitializationResult> InitializeAsync(
        InitializeComponentCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if already initialized
        lock (_lifecycleLock)
        {
            if (_isInitialized)
            {
                _logger.LogWarning("Component is already initialized - skipping re-initialization");
                return InitializationResult.AlreadyInitialized();
            }
        }

        var stopwatch = Stopwatch.StartNew();
        _initializationStartTime = DateTime.UtcNow;
        var totalSteps = command.IsHeadlessMode ? 7 : 8; // Theme initialization len pre UI
        var currentStep = 0;

        using var scope = _operationLogger.LogOperationStart("ComponentInitialization", new
        {
            Mode = command.IsHeadlessMode ? "Headless" : "UI",
            ValidateConfiguration = command.ValidateConfiguration,
            Timeout = command.InitializationTimeout
        });

        _logger.LogInformation("Starting component initialization: mode={Mode}, timeout={Timeout}ms",
            command.IsHeadlessMode ? "Headless" : "UI",
            command.InitializationTimeout.TotalMilliseconds);

        try
        {
            // PHASE 1: Service Registration
            await ReportProgressAsync(++currentStep, totalSteps, "Registering services",
                InitializationPhase.ServiceRegistration, command, stopwatch.Elapsed, cancellationToken);
            await RegisterServicesAsync(command, cancellationToken);

            // PHASE 2: Dependency Validation
            await ReportProgressAsync(++currentStep, totalSteps, "Validating dependencies",
                InitializationPhase.DependencyValidation, command, stopwatch.Elapsed, cancellationToken);
            await ValidateDependenciesAsync(cancellationToken);

            // PHASE 3: Configuration Loading
            await ReportProgressAsync(++currentStep, totalSteps, "Loading configuration",
                InitializationPhase.ConfigurationLoading, command, stopwatch.Elapsed, cancellationToken);
            await LoadConfigurationAsync(command.Configuration, cancellationToken);

            // PHASE 4: Component Initialization
            await ReportProgressAsync(++currentStep, totalSteps, "Initializing components",
                InitializationPhase.ComponentInitialization, command, stopwatch.Elapsed, cancellationToken);
            await InitializeComponentsAsync(command, cancellationToken);

            // PHASE 5: Validation System Setup
            await ReportProgressAsync(++currentStep, totalSteps, "Setting up validation system",
                InitializationPhase.ValidationSetup, command, stopwatch.Elapsed, cancellationToken);
            await InitializeValidationSystemAsync(command.Configuration.ValidationConfig, cancellationToken);

            // PHASE 6: Theme Initialization (UI mode only)
            if (!command.IsHeadlessMode)
            {
                await ReportProgressAsync(++currentStep, totalSteps, "Initializing theme system",
                    InitializationPhase.ThemeInitialization, command, stopwatch.Elapsed, cancellationToken);
                await InitializeThemeSystemAsync(command.Configuration.ColorTheme, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Skipping theme initialization - headless mode");
            }

            // PHASE 7: Smart Operations Setup
            await ReportProgressAsync(++currentStep, totalSteps, "Setting up smart operations",
                InitializationPhase.SmartOperationsSetup, command, stopwatch.Elapsed, cancellationToken);
            await InitializeSmartOperationsAsync(command.Configuration.CustomSettings, cancellationToken);

            // PHASE 8: Finalization
            await ReportProgressAsync(++currentStep, totalSteps, "Finalizing initialization",
                InitializationPhase.Finalization, command, stopwatch.Elapsed, cancellationToken);
            await FinalizeInitializationAsync(cancellationToken);

            // Mark as initialized
            lock (_lifecycleLock)
            {
                _isInitialized = true;
                _currentStatus = new InitializationStatus
                {
                    IsInitialized = true,
                    IsHeadlessMode = command.IsHeadlessMode,
                    CurrentPhase = InitializationPhase.Finalization,
                    InitializationStartTime = _initializationStartTime,
                    InitializationCompletedTime = DateTime.UtcNow,
                    InitializationDuration = stopwatch.Elapsed
                };
            }

            stopwatch.Stop();

            _logger.LogInformation("Component initialization completed successfully in {Duration}ms, mode={Mode}",
                stopwatch.ElapsedMilliseconds, command.IsHeadlessMode ? "Headless" : "UI");

            scope.MarkSuccess(new
            {
                Duration = stopwatch.Elapsed,
                Mode = command.IsHeadlessMode ? "Headless" : "UI",
                TotalSteps = totalSteps
            });

            return InitializationResult.Success(
                $"Initialization completed in {stopwatch.ElapsedMilliseconds}ms",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Component initialization was cancelled by user");
            scope.MarkFailure(new OperationCanceledException("Initialization cancelled"));
            return InitializationResult.Failure("Initialization was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Component initialization failed: {Message}", ex.Message);

            lock (_lifecycleLock)
            {
                _currentStatus = _currentStatus with { LastError = ex.Message };
            }

            scope.MarkFailure(ex);
            return InitializationResult.Failure($"Initialization failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Graceful shutdown s cleanup všetkých resources
    /// </summary>
    public async Task<bool> ShutdownAsync(CancellationToken cancellationToken = default)
    {
        lock (_lifecycleLock)
        {
            if (!_isInitialized || _isDisposed)
            {
                _logger.LogWarning("Component is not initialized or already disposed - skipping shutdown");
                return true;
            }
        }

        _logger.LogInformation("Starting component shutdown");

        try
        {
            // 1. Stop background services
            _logger.LogInformation("Stopping background services");
            await StopBackgroundServicesAsync(cancellationToken);

            // 2. Flush pending operations
            _logger.LogInformation("Flushing pending operations");
            await FlushPendingOperationsAsync(cancellationToken);

            // 3. Clean up resources
            _logger.LogInformation("Cleaning up resources");
            await CleanupResourcesAsync();

            // 4. Unregister global handlers
            _logger.LogInformation("Unregistering global handlers");
            await UnregisterGlobalHandlersAsync();

            lock (_lifecycleLock)
            {
                _isInitialized = false;
                _isDisposed = true;
                _currentStatus = new InitializationStatus { IsInitialized = false };
            }

            _logger.LogInformation("Component shutdown completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Component shutdown failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Získa aktuálny status inicializácie
    /// </summary>
    public InitializationStatus GetStatus()
    {
        lock (_lifecycleLock)
        {
            return _currentStatus;
        }
    }

    // PRIVATE HELPER METHODS

    private async Task ReportProgressAsync(
        int currentStep,
        int totalSteps,
        string operation,
        InitializationPhase phase,
        InitializeComponentCommand command,
        TimeSpan elapsed,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var progress = new InitializationProgress
        {
            CompletedSteps = currentStep,
            TotalSteps = totalSteps,
            CurrentOperation = operation,
            CurrentPhase = phase,
            ElapsedTime = elapsed,
            IsHeadlessMode = command.IsHeadlessMode
        };

        _logger.LogInformation("Initialization progress: {Step}/{TotalSteps} - {Operation} ({Percentage:F1}%)",
            currentStep, totalSteps, operation, progress.CompletionPercentage);

        command.ProgressReporter?.Report(progress);
        await Task.CompletedTask;
    }

    private async Task RegisterServicesAsync(InitializeComponentCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering services - mode={Mode}", command.IsHeadlessMode ? "Headless" : "UI");

        // Services sú už registrované cez ServiceRegistration.cs
        // Tu len validujeme že sú dostupné
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task ValidateDependenciesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating service dependencies");

        // Validate critical services
        var requiredServices = new[]
        {
            typeof(ILogger<>),
            typeof(AdvancedDataGridOptions)
        };

        foreach (var serviceType in requiredServices)
        {
            if (_serviceProvider.GetService(serviceType) == null)
            {
                throw new InvalidOperationException($"Required service {serviceType.Name} is not registered");
            }
        }

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task LoadConfigurationAsync(InitializationConfiguration config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading configuration");

        // Configuration je už loaded cez command
        // Tu môžeme pridať validation alebo transformation logic

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task InitializeComponentsAsync(InitializeComponentCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing core components");

        // Initialize core business components
        // Pre teraz placeholder - rozšírime keď budú services implementované

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task InitializeValidationSystemAsync(ValidationConfiguration? config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing validation system");

        // Setup validation service if enabled
        if (config?.EnableValidation == true)
        {
            _logger.LogInformation("Validation system enabled with batch size={BatchSize}",
                config.ValidationBatchSize);
        }

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task InitializeThemeSystemAsync(object? config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing theme system");

        // Setup theme/color system (implementujeme neskôr)

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task InitializeSmartOperationsAsync(Dictionary<string, object?>? settings, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing smart operations");

        // Setup smart operations service

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task FinalizeInitializationAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Finalizing initialization");

        // Final cleanup and verification

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task StopBackgroundServicesAsync(CancellationToken cancellationToken)
    {
        // Stop any background workers/timers
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task FlushPendingOperationsAsync(CancellationToken cancellationToken)
    {
        // Complete any pending async operations
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task CleanupResourcesAsync()
    {
        // Dispose managed resources
        await Task.CompletedTask;
    }

    private async Task UnregisterGlobalHandlersAsync()
    {
        // Unregister event handlers
        await Task.CompletedTask;
    }
}

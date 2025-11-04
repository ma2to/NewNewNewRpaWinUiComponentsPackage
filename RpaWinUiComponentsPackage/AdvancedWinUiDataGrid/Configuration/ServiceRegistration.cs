using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Services;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Progress;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

/// <summary>
/// Central service registration for AdvancedWinUiDataGrid component
/// Follows feature-based structure from StructureDocumentation.md
/// </summary>
internal static class ServiceRegistration
{
    /// <summary>
    /// Registers all services for the AdvancedWinUiDataGrid component
    /// Calls per-feature Registration.Register methods as per documentation
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Service collection for chaining</returns>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options = null)
    {
        options ??= new AdvancedDataGridOptions();

        // Register shared options
        services.AddSingleton(options);

        // Cross-cutting singletons
        RegisterCrossCuttingServices(services, options);

        // RowStore: use host-provided factory if present
        RegisterRowStore(services, options);

        // Register features (each feature provides its own Registration.Register)
        Features.Import.Registration.Register(services, options);
        Features.Export.Registration.Register(services, options);
        Features.Validation.Registration.Register(services, options);
        Features.Filter.Registration.Register(services, options);
        Features.CopyPaste.Registration.Register(services, options);
        Features.Column.Registration.Register(services, options);
        Features.Selection.Registration.Register(services, options);
        Features.AutoRowHeight.Registration.Register(services, options);
        Features.RowNumber.Registration.Register(services, options);
        Features.Sort.Registration.Register(services, options);
        Features.Search.Registration.Register(services, options);
        Features.Initialization.Registration.Register(services, options);
        Features.Shortcuts.Registration.Register(services, options);
        // NEW ARCHITECTURE: RowManagement replaces SmartAddDelete
        Features.Rows.Registration.AddRowManagementServices(services);

        // NEW FEATURES - MEDIUM PRIORITY
        Features.Color.Registration.Register(services, options);
        Features.Schema.Registration.Register(services, options);
        Features.Performance.Registration.Register(services, options);
        Features.RowColumnCell.Registration.Register(services, options);

        // NEW FEATURES - LOW PRIORITY
        Features.UI.Registration.Register(services, options);
        Features.Security.Registration.Register(services, options);

        // SMART VALIDATION SYSTEM FEATURES
        Features.CellEdit.Registration.AddCellEditFeature(services);
        Infrastructure.SpecialColumns.Registration.AddSpecialColumnsInfrastructure(services);
        Features.SpecialColumns.Registration.AddSpecialColumnsFeature(services);

        // COLUMN RESIZE FEATURE
        Features.ColumnResize.Registration.AddColumnResizeFeature(services);

        // CONFIGURATION FEATURE
        Features.Configuration.Registration.Register(services, options);

        // PAGINATION FEATURE
        Features.Pagination.Registration.AddPaginationServices(services);

        // Facade implementation (internal)
        services.AddScoped<AdvancedDataGridFacade>();
        services.AddScoped<IAdvancedDataGridFacade>(sp => sp.GetRequiredService<AdvancedDataGridFacade>());

        // Register modular API facades (public interfaces with namespace separation)
        services.AddScoped<Columns.IDataGridColumns, Columns.DataGridColumns>();
        services.AddScoped<Editing.IDataGridEditing, Editing.DataGridEditing>();
        services.AddScoped<Filtering.IDataGridFiltering, Filtering.DataGridFiltering>();
        services.AddScoped<Selection.IDataGridSelection, Selection.DataGridSelection>();
        services.AddScoped<Sorting.IDataGridSorting, Sorting.DataGridSorting>();
        services.AddScoped<Configuration.IDataGridConfiguration, Configuration.DataGridConfiguration>();
        services.AddScoped<Rows.IDataGridRows, Rows.DataGridRows>();
        services.AddScoped<Batch.IDataGridBatch, Batch.DataGridBatch>();
        services.AddScoped<IO.IDataGridIO, IO.DataGridIO>();
        services.AddScoped<Clipboard.IDataGridClipboard, Clipboard.DataGridClipboard>();
        services.AddScoped<Search.IDataGridSearch, Search.DataGridSearch>();
        services.AddScoped<Validation.IDataGridValidation, Validation.DataGridValidation>();
        services.AddScoped<Performance.IDataGridPerformance, Performance.DataGridPerformance>();
        services.AddScoped<Theming.IDataGridTheming, Theming.DataGridTheming>();
        services.AddScoped<Notifications.IDataGridNotifications, Notifications.DataGridNotifications>();
        services.AddScoped<AutoRowHeight.IDataGridAutoRowHeight, AutoRowHeight.DataGridAutoRowHeight>();
        services.AddScoped<Shortcuts.IDataGridShortcuts, Shortcuts.DataGridShortcuts>();
        services.AddScoped<MVVM.IDataGridMVVM, MVVM.DataGridMVVM>();
        // REMOVED: SmartOperations - replaced by RowManagement feature
        // services.AddScoped<SmartOperations.IDataGridSmartOperations, SmartOperations.DataGridSmartOperations>();
        services.AddScoped<Environments.IEnvironmentConfiguration, Environments.EnvironmentConfiguration>();

        // Register NEW comprehensive color/theme facades (BOD 4-7)
        services.AddScoped<IDataGridColors, DataGridColors>();
        services.AddScoped<IDataGridTheme, DataGridTheme>();

        return services;
    }

    /// <summary>
    /// Registers cross-cutting services like logging and shared infrastructure
    /// </summary>
    private static void RegisterCrossCuttingServices(IServiceCollection services, AdvancedDataGridOptions options)
    {
        // Configure logging if provided
        if (options.LoggerFactory != null)
        {
            services.AddSingleton(options.LoggerFactory);

            // Register generic ILogger<T> using the ILoggerFactory
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        }

        // Register dispatcher queue for UI mode
        if (options.DispatcherQueue != null)
        {
            services.AddSingleton(options.DispatcherQueue);
        }

        // Register UI adapters based on DispatcherQueue availability (not OperationMode)
        // This allows RefreshUIAsync() to work in both Interactive and Headless modes
        if (options.DispatcherQueue != null)
        {
            // UI notification service available for BOTH modes (if DispatcherQueue provided)
            services.TryAddSingleton<UIAdapters.WinUI.UiNotificationService>();
            services.TryAddSingleton<UIAdapters.WinUI.GridViewModelAdapter>();

            // CRITICAL: Register DataGridViewModel and AdvancedDataGridControl for Interactive mode
            // This enables InternalUIUpdateHandler to access the ViewModel and auto-update UI
            if (options.OperationMode == PublicDataGridOperationMode.Interactive)
            {
                // Register ThemeManager with ColorManagementService integration
                services.TryAddSingleton(sp =>
                {
                    var logger = sp.GetService<ILogger<ViewModels.ThemeManager>>();
                    var themeManager = new ViewModels.ThemeManager(logger);

                    // Wire up ColorManagementService events if available
                    var colorService = sp.GetService<Features.Color.ColorManagementService>();
                    if (colorService != null)
                    {
                        colorService.ThemeChanged += themeManager.OnColorServiceThemeChanged;
                        logger?.LogInformation("ThemeManager subscribed to ColorManagementService events");
                    }

                    return themeManager;
                });

                // Register shared ViewModel (singleton - one ViewModel per facade instance)
                services.TryAddSingleton(sp =>
                {
                    var dispatcher = sp.GetRequiredService<Microsoft.UI.Dispatching.DispatcherQueue>();
                    var logger = sp.GetService<ILogger<ViewModels.DataGridViewModel>>();
                    var loggerFactory = sp.GetService<ILoggerFactory>();
                    var themeManager = sp.GetService<ViewModels.ThemeManager>();

                    // ✅ CRITICAL FIX: Inject PageManager for virtual row management and pagination
                    var pageManager = sp.GetRequiredService<Features.Pagination.Interfaces.IPageManager>();

                    // ✅ READ PageSize from AdvancedDataGridOptions (configurable via public API, default=100)
                    pageManager.PageSize = options.PageSize;
                    logger?.LogInformation("DataGridViewModel created with PageManager (PageSize={PageSize}) for virtual row management", options.PageSize);

                    return new ViewModels.DataGridViewModel(logger, loggerFactory, dispatcher, themeManager, pageManager);
                });

                // Register UI control (singleton - one UI control per facade instance)
                services.TryAddSingleton(sp =>
                {
                    var viewModel = sp.GetRequiredService<ViewModels.DataGridViewModel>();
                    var logger = sp.GetService<ILogger<UIControls.AdvancedDataGridControl>>();
                    var loggerFactory = sp.GetService<ILoggerFactory>();
                    return new UIControls.AdvancedDataGridControl(viewModel, logger, loggerFactory);
                });

                // CRITICAL: InternalUIUpdateHandler for automatic granular updates (10M+ row performance)
                // Now it has access to the shared ViewModel from DI
                services.TryAddSingleton<UIAdapters.WinUI.InternalUIUpdateHandler>();
            }
        }

        // Register logging infrastructure (OPTIONAL - uses null object pattern if ILoggerFactory is not available)
        // Uses TryAddSingleton for optional registration - does not replace existing registrations
        services.TryAddSingleton(typeof(IOperationLogger<>), typeof(OperationLogger<>));

        // Register specialized loggers for features
        services.TryAddSingleton<Logging.ExportLogger>();
        services.TryAddSingleton<Logging.ImportLogger>();
        services.TryAddSingleton<Logging.ValidationLogger>();
        services.TryAddSingleton<Logging.SearchLogger>();
        services.TryAddSingleton<Logging.SortLogger>();
        services.TryAddSingleton<Logging.FilterLogger>();
        services.TryAddSingleton<Logging.CopyPasteLogger>();

        // Register advanced logging (LOW PRIORITY enhancement)
        services.TryAddSingleton(typeof(Infrastructure.Logging.Interfaces.IAdvancedLogger), sp =>
        {
            var logger = sp.GetService<ILogger>();
            return logger != null
                ? new Infrastructure.Logging.Services.AdvancedLogger(logger)
                : null!;
        });

        // Register progress hub
        services.TryAddSingleton<IProgressHub, ProgressHub>();
    }

    /// <summary>
    /// Registers row store with adaptive storage support
    /// PRIORITY:
    ///   1. User-provided factory (if options.RowStoreFactory != null)
    ///   2. Adaptive storage (if options.UseAdaptiveStorage == true) [DEFAULT]
    ///   3. Legacy InMemory (if options.UseAdaptiveStorage == false)
    /// </summary>
    private static void RegisterRowStore(IServiceCollection services, AdvancedDataGridOptions options)
    {
        // Register DatabaseLifecycleManager (required for HybridRowStore in adaptive mode)
        services.TryAddSingleton<Features.Database.Interfaces.IDatabaseLifecycleManager>(sp =>
        {
            var logger = sp.GetService<ILogger<Features.Database.Services.DatabaseLifecycleManager>>();
            return new Features.Database.Services.DatabaseLifecycleManager(logger);
        });

        // DECISION TREE: 3 možnosti

        if (options.RowStoreFactory != null)
        {
            // ═══════════════════════════════════════════════════════════════════
            // VETVA 1: USER-PROVIDED FACTORY (HIGHEST PRIORITY)
            // ═══════════════════════════════════════════════════════════════════
            // Používateľ má vlastnú implementáciu IRowStore (Redis, MongoDB, custom...)
            // PRÍKLAD:
            //   options.RowStoreFactory = sp => new RedisRowStore(redis);

            services.AddSingleton(sp => options.RowStoreFactory!(sp));

            // LOG: Informuj o použití custom factory
            var sp = services.BuildServiceProvider();
            var logger = sp.GetService<ILogger<object>>();
            logger?.LogInformation("RowStore: Using custom user-provided factory");
            sp.Dispose();
        }
        // else if (options.UseAdaptiveStorage)
        else
        {
            // ═══════════════════════════════════════════════════════════════════
            // VETVA 2: ADAPTIVE STORAGE (DEFAULT)
            // ═══════════════════════════════════════════════════════════════════
            // Automatické prepínanie InMemory ↔ Hybrid na základe row count
            // THRESHOLDS:
            //   - DataStorageThreshold (default 100,000)
            //   - ValidationStorageThreshold (default 1,000,000)
            // PRÍKLAD:
            //   < 100K rows: InMemory + InMemory
            //   100K-1M rows: SQLite + InMemory
            //   1M and more rows: SQLite + SQLite

            services.AddSingleton<Infrastructure.Persistence.Interfaces.IRowStore>(sp =>
            {
                var logger = sp.GetService<ILogger<AdaptiveRowStore>>();
                var adaptiveStore = new AdaptiveRowStore(logger, options, sp);

                logger?.LogInformation(
                    "RowStore: Adaptive storage enabled (DataThreshold={DataThreshold}, ValidationThreshold={ValidationThreshold}, DatabasePath={Path})",
                    options.DataStorageThreshold,
                    options.ValidationStorageThreshold,
                    options.DatabasePath ?? "(temp)");

                return adaptiveStore;
            });
        }
        // else
        // {
        //     // ═══════════════════════════════════════════════════════════════════
        //     // VETVA 3: LEGACY INMEMORY (STATIC)
        //     // ═══════════════════════════════════════════════════════════════════
        //     // Vždy InMemoryRowStore (žiadne prepínanie)
        //     // Use-case:
        //     //   - Malé datasety (< 100K rows guaranteed)
        //     //   - Backward compatibility
        //     //   - Testing/development
        //     // PRÍKLAD:
        //     //   options.UseAdaptiveStorage = false;
        //
        //     services.AddSingleton<Infrastructure.Persistence.Interfaces.IRowStore, InMemoryRowStore>();
        //
        //     var sp = services.BuildServiceProvider();
        //     var logger = sp.GetService<ILogger<object>>();
        //     logger?.LogInformation("RowStore: Using legacy InMemoryRowStore (no adaptive switching)");
        //     sp.Dispose();
        // }
    }

    /// <summary>
    /// Validates service registrations
    /// </summary>
    /// <param name="serviceProvider">Service provider to validate</param>
    /// <returns>True if all required services are registered</returns>
    internal static bool ValidateServiceRegistrations(IServiceProvider serviceProvider)
    {
        try
        {
            // Validate core facade is registered
            _ = serviceProvider.GetRequiredService<IAdvancedDataGridFacade>();
            _ = serviceProvider.GetRequiredService<AdvancedDataGridOptions>();
            _ = serviceProvider.GetRequiredService<Infrastructure.Persistence.Interfaces.IRowStore>();

            return true;
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetService<ILogger>();
            logger?.LogError(ex, "Service registration validation failed");
            return false;
        }
    }

    /// <summary>
    /// Registers UI-specific services when in UI mode
    /// </summary>
    internal static void RegisterUIServices(IServiceCollection services, AdvancedDataGridOptions options)
    {
        if (options.OperationMode == PublicDataGridOperationMode.Interactive && options.DispatcherQueue != null)
        {
            services.AddSingleton(options.DispatcherQueue);
        }
    }

    /// <summary>
    /// Registers performance monitoring services
    /// </summary>
    internal static void RegisterPerformanceServices(IServiceCollection services, AdvancedDataGridOptions options)
    {
        if (options.EnablePerformanceMetrics)
        {
            // Register performance monitoring services
            // This would include metrics collection, timing services, etc.
        }
    }

    /// <summary>
    /// Creates an operation scope for services that need scoped lifetime
    /// </summary>
    /// <param name="serviceProvider">Service provider to create scope from</param>
    /// <returns>Service scope for operation</returns>
    internal static IServiceScope CreateOperationScope(IServiceProvider serviceProvider)
    {
        if (serviceProvider is IServiceScopeFactory scopeFactory)
        {
            return scopeFactory.CreateScope();
        }

        // If the service provider doesn't support scopes, return a no-op scope
        return new NoOpServiceScope(serviceProvider);
    }

    /// <summary>
    /// No-operation service scope for providers that don't support scoping
    /// </summary>
    private class NoOpServiceScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; }

        public NoOpServiceScope(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public void Dispose()
        {
            // No-op
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public factory for creating AdvancedDataGrid facade instances
/// Supports both standalone and host-integrated scenarios
/// </summary>
public static class AdvancedDataGridFacadeFactory
{
    /// <summary>
    /// Creates a standalone facade instance with internal DI container
    /// For applications that want to use the component without host DI integration
    /// </summary>
    /// <param name="options">Configuration options for the component</param>
    /// <param name="loggerFactory">Optional logger factory for logging</param>
    /// <param name="dispatcher">Optional dispatcher queue for UI operations</param>
    /// <returns>Configured facade instance</returns>
    public static IAdvancedDataGridFacade CreateStandalone(
        AdvancedDataGridOptions? options = null,
        ILoggerFactory? loggerFactory = null,
        DispatcherQueue? dispatcher = null)
    {
        options ??= new AdvancedDataGridOptions();

        var services = new ServiceCollection();

        // Register optional host dependencies
        if (loggerFactory != null)
        {
            services.AddSingleton(loggerFactory);
            options.LoggerFactory = loggerFactory;
        }

        if (dispatcher != null)
        {
            services.AddSingleton(dispatcher);
            options.DispatcherQueue = dispatcher;
        }

        // Register all component services
        ServiceRegistration.Register(services, options);

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Return the facade instance
        return serviceProvider.GetRequiredService<IAdvancedDataGridFacade>();
    }

    /// <summary>
    /// Creates a facade instance using existing host services
    /// For applications that want to integrate with their existing DI container
    /// </summary>
    /// <param name="hostServices">Host service provider</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Configured facade instance</returns>
    public static IAdvancedDataGridFacade CreateUsingHostServices(
        IServiceProvider hostServices,
        AdvancedDataGridOptions? options = null)
    {
        options ??= new AdvancedDataGridOptions();

        var services = new ServiceCollection();

        // Copy relevant services from host
        var hostLoggerFactory = hostServices.GetService<ILoggerFactory>();
        if (hostLoggerFactory != null)
        {
            services.AddSingleton(hostLoggerFactory);
            options.LoggerFactory = hostLoggerFactory;
        }

        var hostDispatcher = hostServices.GetService<DispatcherQueue>();
        if (hostDispatcher != null)
        {
            services.AddSingleton(hostDispatcher);
            options.DispatcherQueue = hostDispatcher;
        }

        // Register all component services
        ServiceRegistration.Register(services, options);

        // Build service provider with fallback to host
        var serviceProvider = services.BuildServiceProvider();

        // Return the facade instance
        return serviceProvider.GetRequiredService<IAdvancedDataGridFacade>();
    }
}
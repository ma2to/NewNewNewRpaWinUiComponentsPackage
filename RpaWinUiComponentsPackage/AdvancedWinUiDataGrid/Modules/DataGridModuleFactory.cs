using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Modules;

/// <summary>
/// Public factory for creating DataGrid modules
/// Each module represents a specific domain of functionality (IO, Validation, Operations, etc.)
/// </summary>
public static class DataGridModuleFactory
{
    /// <summary>
    /// Creates all modules as a complete set with shared configuration
    /// Returns a ModuleSet containing all initialized modules
    /// </summary>
    public static DataGridModules CreateAll(
        AdvancedDataGridOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        options ??= new AdvancedDataGridOptions();

        var services = new ServiceCollection();

        // Register logger factory
        if (loggerFactory != null)
        {
            services.AddSingleton(loggerFactory);
            options.LoggerFactory = loggerFactory;
        }

        // Register all component services
        ServiceRegistration.Register(services, options);

        // Register modular facades
        services.AddScoped<IO.IDataGridIO, IO.DataGridIO>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Create and return all modules
        return new DataGridModules(serviceProvider);
    }

    /// <summary>
    /// Creates individual IO module
    /// </summary>
    public static IO.IDataGridIO CreateIO(
        AdvancedDataGridOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        var modules = CreateAll(options, loggerFactory);
        return modules.IO;
    }
}

/// <summary>
/// Container for all DataGrid modules
/// Provides organized access to all functional areas
/// </summary>
public sealed class DataGridModules : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;

    internal DataGridModules(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // Initialize IO module
        IO = serviceProvider.GetRequiredService<IO.IDataGridIO>();
    }

    /// <summary>
    /// IO module for Import/Export operations
    /// </summary>
    public IO.IDataGridIO IO { get; }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

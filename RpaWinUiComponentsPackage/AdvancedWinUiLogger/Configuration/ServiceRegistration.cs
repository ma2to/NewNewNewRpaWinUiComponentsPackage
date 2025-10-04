using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Infrastructure.Persistence;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Configuration;

/// <summary>
/// INTERNAL CONFIGURATION: Central service registration for AdvancedWinUiLogger
/// CLEAN ARCHITECTURE: Feature-based DI registration pattern
/// ENTERPRISE: Professional service registration with proper lifetime management
/// </summary>
internal static class ServiceRegistration
{
    /// <summary>
    /// Registers all services for the AdvancedWinUiLogger component
    /// Calls per-feature Registration.Register methods as per architecture
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Service collection for chaining</returns>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedLoggerOptions? options = null)
    {
        options ??= AdvancedLoggerOptions.CreateMinimal("./logs");

        // Register shared options
        services.AddSingleton(options);

        // Register Infrastructure (Repository pattern)
        RegisterInfrastructure(services);

        // Register Features (each feature provides its own Registration.Register)
        Features.Logging.Registration.Register(services, options);
        Features.FileManagement.Registration.Register(services, options);
        Features.Session.Registration.Register(services, options);
        Features.Performance.Registration.Register(services, options);

        // Register Facade implementation (internal)
        services.AddScoped<AdvancedLoggerFacade>();
        services.AddScoped<IAdvancedLoggerFacade>(sp => sp.GetRequiredService<AdvancedLoggerFacade>());

        return services;
    }

    /// <summary>
    /// Registers infrastructure services (persistence, external dependencies)
    /// </summary>
    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddSingleton<ILoggerRepository, FileLoggerRepository>();
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
            _ = serviceProvider.GetRequiredService<IAdvancedLoggerFacade>();
            _ = serviceProvider.GetRequiredService<AdvancedLoggerOptions>();
            _ = serviceProvider.GetRequiredService<ILoggerRepository>();

            return true;
        }
        catch
        {
            return false;
        }
    }
}

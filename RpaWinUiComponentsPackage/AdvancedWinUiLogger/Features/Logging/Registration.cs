using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Logging.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Logging;

/// <summary>
/// INTERNAL REGISTRATION: DI registration for Logging feature
/// </summary>
internal static class Registration
{
    internal static IServiceCollection Register(IServiceCollection services, AdvancedLoggerOptions options)
    {
        services.AddScoped<ILoggingService, LoggingService>();
        return services;
    }
}

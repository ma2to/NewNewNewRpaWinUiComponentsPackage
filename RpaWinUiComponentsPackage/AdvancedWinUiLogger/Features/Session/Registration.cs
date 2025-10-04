using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Session.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Session.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Session;

/// <summary>
/// INTERNAL REGISTRATION: DI registration for Session feature
/// </summary>
internal static class Registration
{
    internal static IServiceCollection Register(IServiceCollection services, AdvancedLoggerOptions options)
    {
        services.AddSingleton<ISessionService, SessionService>();
        return services;
    }
}

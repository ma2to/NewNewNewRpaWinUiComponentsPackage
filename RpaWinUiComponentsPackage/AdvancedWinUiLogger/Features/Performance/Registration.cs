using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Performance.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Performance.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.Performance;

/// <summary>
/// INTERNAL REGISTRATION: DI registration for Performance feature
/// </summary>
internal static class Registration
{
    internal static IServiceCollection Register(IServiceCollection services, AdvancedLoggerOptions options)
    {
        services.AddSingleton<IPerformanceService, PerformanceService>();
        return services;
    }
}

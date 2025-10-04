using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.FileManagement.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.FileManagement.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Features.FileManagement;

/// <summary>
/// INTERNAL REGISTRATION: DI registration for FileManagement feature
/// </summary>
internal static class Registration
{
    internal static IServiceCollection Register(IServiceCollection services, AdvancedLoggerOptions options)
    {
        services.AddScoped<IFileManagementService, FileManagementService>();
        return services;
    }
}

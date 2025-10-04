using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color;

/// <summary>
/// Service registration for Color/Theme feature
/// </summary>
internal static class Registration
{
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions options)
    {
        // Register ThemeService as singleton (shared theme across grid instance)
        services.TryAddSingleton<ThemeService>();

        return services;
    }
}

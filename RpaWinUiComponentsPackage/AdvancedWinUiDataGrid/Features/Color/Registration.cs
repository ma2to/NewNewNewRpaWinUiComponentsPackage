using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Services;

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
        services.TryAddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());

        // Register ColorService as scoped
        services.TryAddScoped<IColorService, ColorService>();

        return services;
    }
}

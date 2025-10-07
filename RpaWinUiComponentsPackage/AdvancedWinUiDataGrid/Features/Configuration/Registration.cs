using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration;

/// <summary>
/// Service registration for Configuration feature
/// </summary>
internal static class Registration
{
    internal static IServiceCollection Register(IServiceCollection services, global::RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.AdvancedDataGridOptions options)
    {
        // Register DataGridConfigurationService as singleton
        services.TryAddSingleton<IDataGridConfiguration, DataGridConfigurationService>();

        return services;
    }
}

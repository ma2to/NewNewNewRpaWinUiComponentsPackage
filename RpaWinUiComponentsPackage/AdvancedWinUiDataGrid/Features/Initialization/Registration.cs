using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization;

/// <summary>
/// Service registration for Initialization feature
/// Provides ComponentLifecycleManager as singleton
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers all services required for the Initialization feature
    /// </summary>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions options)
    {
        // ComponentLifecycleManager is singleton - one lifecycle manager for the entire component
        services.AddSingleton<IComponentLifecycleManager, ComponentLifecycleManager>();

        return services;
    }
}

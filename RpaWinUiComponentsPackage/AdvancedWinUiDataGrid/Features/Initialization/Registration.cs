using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization;

/// <summary>
/// Registrácia služieb pre Initialization feature
/// Provides ComponentLifecycleManager as singleton
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registruje všetky služby pre Initialization feature
    /// </summary>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions options)
    {
        // ComponentLifecycleManager je singleton - jeden lifecycle manager pre celý component
        services.AddSingleton<IComponentLifecycleManager, ComponentLifecycleManager>();

        return services;
    }
}

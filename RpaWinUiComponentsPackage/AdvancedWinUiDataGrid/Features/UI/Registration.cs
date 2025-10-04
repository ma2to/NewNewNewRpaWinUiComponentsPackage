using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI;

/// <summary>
/// Internal service registration for UI feature module
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers UI feature services with correct lifetimes
    /// IUIService -> Scoped (per-operation state isolation)
    /// </summary>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        services.AddScoped<IUIService, UIService>();
        return services;
    }
}

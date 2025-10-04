using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight;

/// <summary>
/// Internal service registration for AutoRowHeight feature module
/// Per SERVICE_REGISTRATION.md pattern
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers AutoRowHeight feature services with correct lifetimes per DI_DECISIONS.md
    /// IAutoRowHeightService -> Scoped (per-operation state isolation)
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Service collection for chaining</returns>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        // AutoRowHeight service - Scoped per DI_DECISIONS.md
        services.AddScoped<IAutoRowHeightService, AutoRowHeightService>();

        return services;
    }
}
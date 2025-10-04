using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter;

/// <summary>
/// Internal service registration for Filter feature module
/// Per SERVICE_REGISTRATION.md pattern
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers Filter feature services with correct lifetimes per DI_DECISIONS.md
    /// IFilterService -> Scoped (per-operation state isolation)
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Service collection for chaining</returns>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        // Filter service - Scoped per DI_DECISIONS.md
        services.AddScoped<IFilterService, FilterService>();

        return services;
    }
}
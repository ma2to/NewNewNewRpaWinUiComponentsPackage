using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column;

/// <summary>
/// Internal service registration for Column feature module
/// Per SERVICE_REGISTRATION.md pattern
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers Column feature services with correct lifetimes per DI_DECISIONS.md
    /// IColumnService -> Scoped (per-operation state isolation)
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Service collection for chaining</returns>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        // Column service - Scoped per DI_DECISIONS.md
        services.AddScoped<IColumnService, ColumnService>();

        return services;
    }
}
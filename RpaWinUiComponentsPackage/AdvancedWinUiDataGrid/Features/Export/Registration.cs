using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export;

/// <summary>
/// Internal service registration for Export feature module
/// Per SERVICE_REGISTRATION.md pattern
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers Export feature services with correct lifetimes per DI_DECISIONS.md
    /// IExportService -> Scoped (per-operation state isolation)
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Service collection for chaining</returns>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        // Export service - Scoped per DI_DECISIONS.md
        services.AddScoped<IExportService, ExportService>();

        return services;
    }
}
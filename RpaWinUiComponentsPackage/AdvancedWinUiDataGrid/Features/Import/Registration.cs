using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import;

/// <summary>
/// Internal service registration for Import feature module
/// Per SERVICE_REGISTRATION.md pattern
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers Import feature services with correct lifetimes per DI_DECISIONS.md
    /// IImportService -> Scoped (per-operation state isolation)
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Service collection for chaining</returns>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        // Import service - Scoped per DI_DECISIONS.md
        services.AddScoped<IImportService, ImportService>();

        return services;
    }
}
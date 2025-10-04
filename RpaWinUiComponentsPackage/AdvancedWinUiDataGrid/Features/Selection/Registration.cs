using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection;

/// <summary>
/// Internal service registration for Selection feature module
/// Per SERVICE_REGISTRATION.md pattern
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers Selection feature services with correct lifetimes per DI_DECISIONS.md
    /// ISelectionService -> Scoped (per-operation state isolation)
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Service collection for chaining</returns>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        // Selection service - Scoped per DI_DECISIONS.md
        services.AddScoped<ISelectionService, SelectionService>();

        return services;
    }
}
using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CellEdit.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CellEdit.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CellEdit;

/// <summary>
/// Registration for CellEdit feature services
/// Per DI_DECISIONS.md - Scoped lifetime for per-operation state isolation
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers CellEdit feature services
    /// </summary>
    internal static IServiceCollection AddCellEditFeature(this IServiceCollection services)
    {
        // Register cell edit service with Scoped lifetime
        services.AddScoped<ICellEditService, CellEditService>();

        return services;
    }
}

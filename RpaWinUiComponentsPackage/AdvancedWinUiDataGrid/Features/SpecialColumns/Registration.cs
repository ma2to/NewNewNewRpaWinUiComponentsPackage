using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SpecialColumns.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SpecialColumns;

/// <summary>
/// Registration for SpecialColumns feature services
/// Per DI_DECISIONS.md - Scoped lifetime for per-operation state isolation
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers SpecialColumns feature services
    /// </summary>
    internal static IServiceCollection AddSpecialColumnsFeature(this IServiceCollection services)
    {
        // Register checkbox header service with Scoped lifetime
        // Scoped = per-operation state isolation (each grid operation gets fresh instance)
        services.AddScoped<CheckboxHeaderService>();

        return services;
    }
}

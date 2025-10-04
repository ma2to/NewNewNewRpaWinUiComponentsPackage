using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.SpecialColumns.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.SpecialColumns.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.SpecialColumns;

/// <summary>
/// Registration for SpecialColumns infrastructure services
/// Per DI_DECISIONS.md - Scoped lifetime for per-operation state isolation
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers SpecialColumns infrastructure services
    /// </summary>
    internal static IServiceCollection AddSpecialColumnsInfrastructure(this IServiceCollection services)
    {
        // Register special column service with Scoped lifetime
        services.AddScoped<ISpecialColumnService, SpecialColumnService>();

        return services;
    }
}

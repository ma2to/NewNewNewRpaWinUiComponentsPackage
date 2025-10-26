using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows;

/// <summary>
/// Registration for row management services
/// NEW ARCHITECTURE: Replaces SmartAddDelete with simpler data-shifting approach
/// </summary>
internal static class Registration
{
    public static IServiceCollection AddRowManagementServices(this IServiceCollection services)
    {
        // Row management service - Scoped (per-operation)
        services.AddScoped<IRowManagementService, RowManagementService>();

        return services;
    }
}

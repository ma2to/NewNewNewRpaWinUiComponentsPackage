using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowNumber.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowNumber.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowNumber;

/// <summary>
/// INTERNAL: Registration helper for RowNumber feature
/// DI: Registers all RowNumber-related services
/// </summary>
internal static class Registration
{
    internal static void Register(IServiceCollection services, object? options = null)
    {
        // Register RowNumber service as Scoped
        services.AddScoped<IRowNumberService, RowNumberService>();
    }
}

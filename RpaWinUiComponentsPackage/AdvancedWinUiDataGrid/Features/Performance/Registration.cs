using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Performance.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Performance.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Performance;

internal static class Registration
{
    internal static void Register(IServiceCollection services, object? options = null)
    {
        services.AddScoped<IPerformanceService, PerformanceService>();
    }
}

using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort;

internal static class Registration
{
    internal static void Register(IServiceCollection services, object? options = null)
    {
        services.AddScoped<ISortService, SortService>();
    }
}

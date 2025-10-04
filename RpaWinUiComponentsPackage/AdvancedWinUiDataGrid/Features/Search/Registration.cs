using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Search;

internal static class Registration
{
    internal static void Register(IServiceCollection services, object? options = null)
    {
        services.AddScoped<ISearchService, SearchService>();
    }
}

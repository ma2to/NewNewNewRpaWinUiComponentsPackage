using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Pagination.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Pagination.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Pagination;

/// <summary>
/// Registration for Pagination feature
/// </summary>
internal static class Registration
{
    internal static IServiceCollection AddPaginationServices(this IServiceCollection services)
    {
        services.AddSingleton<IPageManager, PageManager>();
        return services;
    }
}

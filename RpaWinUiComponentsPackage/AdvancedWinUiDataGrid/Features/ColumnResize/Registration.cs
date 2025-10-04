using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.ColumnResize.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.ColumnResize.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.ColumnResize;

/// <summary>
/// Dependency injection registration for ColumnResize feature
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Adds column resize feature services to DI container
    /// </summary>
    internal static IServiceCollection AddColumnResizeFeature(this IServiceCollection services)
    {
        services.AddScoped<IColumnResizeService, ColumnResizeService>();
        return services;
    }
}

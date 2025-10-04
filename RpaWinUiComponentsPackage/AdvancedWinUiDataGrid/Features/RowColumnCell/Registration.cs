using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowColumnCell.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowColumnCell.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowColumnCell;

internal static class Registration
{
    internal static void Register(IServiceCollection services, object? options = null)
    {
        services.AddScoped<IRowColumnCellService, RowColumnCellService>();
    }
}

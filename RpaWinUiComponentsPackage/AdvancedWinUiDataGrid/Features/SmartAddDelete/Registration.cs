using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete;

internal static class Registration
{
    internal static void Register(IServiceCollection services, object? options = null)
    {
        services.AddScoped<ISmartOperationService, SmartOperationService>();
    }
}

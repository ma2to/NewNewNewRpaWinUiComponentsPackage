using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts;

internal static class Registration
{
    internal static void Register(IServiceCollection services, object? options = null)
    {
        services.AddScoped<IShortcutService, ShortcutService>();
    }
}

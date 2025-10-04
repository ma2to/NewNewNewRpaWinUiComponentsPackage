using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Security.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Security.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Security;

/// <summary>
/// Internal service registration for Security feature module
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers Security feature services with correct lifetimes
    /// ISecurityService -> Scoped (per-operation state isolation)
    /// </summary>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        services.AddScoped<ISecurityService, SecurityService>();
        return services;
    }
}

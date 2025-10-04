using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste;

/// <summary>
/// Internal service registration for CopyPaste feature module
/// Per SERVICE_REGISTRATION.md pattern
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers CopyPaste feature services with correct lifetimes per DI_DECISIONS.md
    /// ICopyPasteService -> Singleton (globally shared clipboard semantics)
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Service collection for chaining</returns>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        // CopyPaste service - Singleton per DI_DECISIONS.md
        services.AddSingleton<ICopyPasteService, CopyPasteService>();

        return services;
    }
}
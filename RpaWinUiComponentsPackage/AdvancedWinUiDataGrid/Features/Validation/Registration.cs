using Microsoft.Extensions.DependencyInjection;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Services;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation;

/// <summary>
/// Internal service registration for Validation feature module
/// Per SERVICE_REGISTRATION.md pattern
/// </summary>
internal static class Registration
{
    /// <summary>
    /// Registers Validation feature services with correct lifetimes per DI_DECISIONS.md
    /// IValidationService -> Scoped (per-operation state isolation)
    /// </summary>
    /// <param name="services">Service collection to register services in</param>
    /// <param name="options">Configuration options for the component</param>
    /// <returns>Service collection for chaining</returns>
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions? options)
    {
        // Validation service - Scoped per DI_DECISIONS.md
        services.AddScoped<IValidationService, ValidationService>();

        // PERFORMANCE: Debounced validation service - Singleton (shared state for debouncing)
        services.AddSingleton<DebouncedValidationService>();

        return services;
    }
}
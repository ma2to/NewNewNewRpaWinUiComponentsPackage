using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Schema;

/// <summary>
/// Service registration for Schema feature (column schema definition and type validation)
/// </summary>
internal static class Registration
{
    internal static IServiceCollection Register(IServiceCollection services, AdvancedDataGridOptions options)
    {
        // Register ColumnSchemaService as singleton (shared schema across grid instance)
        services.TryAddSingleton<ColumnSchemaService>();

        return services;
    }
}

using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Partial class containing Column Management APIs
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Column Management APIs

    /// <summary>
    /// Gets column definitions
    /// </summary>
    public IReadOnlyList<PublicColumnDefinition> GetColumnDefinitions()
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var columnService = scope.ServiceProvider.GetRequiredService<IColumnService>();
        return columnService.GetColumnDefinitions().ToPublicList();
    }

    /// <summary>
    /// Adds new column definition
    /// </summary>
    public bool AddColumn(PublicColumnDefinition columnDefinition)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var columnService = scope.ServiceProvider.GetRequiredService<IColumnService>();
        return columnService.AddColumn(columnDefinition.ToInternal());
    }

    /// <summary>
    /// Removes column by name
    /// </summary>
    public bool RemoveColumn(string columnName)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var columnService = scope.ServiceProvider.GetRequiredService<IColumnService>();
        return columnService.RemoveColumn(columnName);
    }

    /// <summary>
    /// Updates existing column definition
    /// </summary>
    public bool UpdateColumn(PublicColumnDefinition columnDefinition)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var columnService = scope.ServiceProvider.GetRequiredService<IColumnService>();
        return columnService.UpdateColumn(columnDefinition.ToInternal());
    }

    /// <summary>
    /// Gets a column definition by name
    /// </summary>
    public PublicColumnDefinition? GetColumn(string columnName)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.RowColumnOperations, nameof(GetColumn));

        try
        {
            var columnService = _serviceProvider.GetRequiredService<IColumnService>();
            var columns = columnService.GetColumnDefinitions();
            var column = columns.FirstOrDefault(c => c.Name == columnName);
            return column?.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get column {ColumnName}", columnName);
            return null;
        }
    }

    #endregion
}


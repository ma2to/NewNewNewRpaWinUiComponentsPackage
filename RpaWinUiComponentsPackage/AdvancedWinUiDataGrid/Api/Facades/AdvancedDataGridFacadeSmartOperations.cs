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
/// Partial class containing Smart Row Management Operations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Smart Row Management Operations

    public async Task<SmartOperationDataResult> SmartAddRowsAsync(SmartAddRowsDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var smartOpService = scope.ServiceProvider.GetRequiredService<Features.SmartAddDelete.Interfaces.ISmartOperationService>();
            var rowStore = scope.ServiceProvider.GetRequiredService<IRowStore>();

            var currentRowCount = await rowStore.GetRowCountAsync(cancellationToken);

            var internalConfig = new Core.ValueObjects.RowManagementConfiguration
            {
                MinimumRows = command.Configuration.MinimumRows,
                EnableAutoExpand = command.Configuration.EnableAutoExpand,
                EnableSmartDelete = command.Configuration.EnableSmartDelete,
                AlwaysKeepLastEmpty = command.Configuration.AlwaysKeepLastEmpty
            };

            var internalCommand = new Features.SmartAddDelete.Commands.SmartAddRowsInternalCommand
            {
                DataToAdd = command.DataToAdd,
                Configuration = internalConfig,
                CancellationToken = cancellationToken
            };

            var internalResult = await smartOpService.SmartAddRowsAsync(internalCommand, cancellationToken);

            return new SmartOperationDataResult
            {
                Success = internalResult.Success,
                FinalRowCount = internalResult.FinalRowCount,
                ProcessedRows = internalResult.ProcessedRows,
                OperationTime = internalResult.OperationTime,
                Messages = internalResult.Messages,
                Statistics = new PublicRowManagementStatistics
                {
                    EmptyRowsCreated = internalResult.Statistics.EmptyRowsCreated,
                    RowsPhysicallyDeleted = internalResult.Statistics.RowsPhysicallyDeleted,
                    RowsContentCleared = internalResult.Statistics.RowsContentCleared,
                    RowsShifted = internalResult.Statistics.RowsShifted,
                    MinimumRowsEnforced = internalResult.Statistics.MinimumRowsEnforced,
                    LastEmptyRowMaintained = internalResult.Statistics.LastEmptyRowMaintained
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart add rows failed: {Message}", ex.Message);
            return new SmartOperationDataResult
            {
                Success = false,
                Messages = new[] { $"Smart add failed: {ex.Message}" }
            };
        }
    }

    public async Task<SmartOperationDataResult> SmartDeleteRowsAsync(SmartDeleteRowsDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var smartOpService = scope.ServiceProvider.GetRequiredService<Features.SmartAddDelete.Interfaces.ISmartOperationService>();

            var internalConfig = new Core.ValueObjects.RowManagementConfiguration
            {
                MinimumRows = command.Configuration.MinimumRows,
                EnableAutoExpand = command.Configuration.EnableAutoExpand,
                EnableSmartDelete = command.Configuration.EnableSmartDelete,
                AlwaysKeepLastEmpty = command.Configuration.AlwaysKeepLastEmpty
            };

            var internalCommand = new Features.SmartAddDelete.Commands.SmartDeleteRowsInternalCommand
            {
                RowIndexesToDelete = command.RowIndexesToDelete,
                Configuration = internalConfig,
                CurrentRowCount = command.CurrentRowCount,
                ForcePhysicalDelete = command.ForcePhysicalDelete,
                CancellationToken = cancellationToken
            };

            var internalResult = await smartOpService.SmartDeleteRowsAsync(internalCommand, cancellationToken);

            return new SmartOperationDataResult
            {
                Success = internalResult.Success,
                FinalRowCount = internalResult.FinalRowCount,
                ProcessedRows = internalResult.ProcessedRows,
                OperationTime = internalResult.OperationTime,
                Messages = internalResult.Messages,
                Statistics = new PublicRowManagementStatistics
                {
                    EmptyRowsCreated = internalResult.Statistics.EmptyRowsCreated,
                    RowsPhysicallyDeleted = internalResult.Statistics.RowsPhysicallyDeleted,
                    RowsContentCleared = internalResult.Statistics.RowsContentCleared,
                    RowsShifted = internalResult.Statistics.RowsShifted,
                    MinimumRowsEnforced = internalResult.Statistics.MinimumRowsEnforced,
                    LastEmptyRowMaintained = internalResult.Statistics.LastEmptyRowMaintained
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart delete rows failed: {Message}", ex.Message);
            return new SmartOperationDataResult
            {
                Success = false,
                Messages = new[] { $"Smart delete failed: {ex.Message}" }
            };
        }
    }

    public async Task<SmartOperationDataResult> AutoExpandEmptyRowAsync(AutoExpandEmptyRowDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var smartOpService = scope.ServiceProvider.GetRequiredService<Features.SmartAddDelete.Interfaces.ISmartOperationService>();

            var internalConfig = new Core.ValueObjects.RowManagementConfiguration
            {
                MinimumRows = command.Configuration.MinimumRows,
                EnableAutoExpand = command.Configuration.EnableAutoExpand,
                EnableSmartDelete = command.Configuration.EnableSmartDelete,
                AlwaysKeepLastEmpty = command.Configuration.AlwaysKeepLastEmpty
            };

            var internalCommand = new Features.SmartAddDelete.Commands.AutoExpandEmptyRowInternalCommand
            {
                Configuration = internalConfig,
                CurrentRowCount = command.CurrentRowCount,
                CancellationToken = cancellationToken
            };

            var internalResult = await smartOpService.AutoExpandEmptyRowAsync(internalCommand, cancellationToken);

            return new SmartOperationDataResult
            {
                Success = internalResult.Success,
                FinalRowCount = internalResult.FinalRowCount,
                ProcessedRows = internalResult.ProcessedRows,
                OperationTime = internalResult.OperationTime,
                Messages = internalResult.Messages,
                Statistics = new PublicRowManagementStatistics
                {
                    EmptyRowsCreated = internalResult.Statistics.EmptyRowsCreated,
                    LastEmptyRowMaintained = internalResult.Statistics.LastEmptyRowMaintained
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-expand empty row failed: {Message}", ex.Message);
            return new SmartOperationDataResult
            {
                Success = false,
                Messages = new[] { $"Auto-expand failed: {ex.Message}" }
            };
        }
    }

    public async Task<PublicResult> ValidateRowManagementConfigurationAsync(PublicRowManagementConfiguration configuration)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var smartOpService = scope.ServiceProvider.GetRequiredService<Features.SmartAddDelete.Interfaces.ISmartOperationService>();

            var internalConfig = new Core.ValueObjects.RowManagementConfiguration
            {
                MinimumRows = configuration.MinimumRows,
                EnableAutoExpand = configuration.EnableAutoExpand,
                EnableSmartDelete = configuration.EnableSmartDelete,
                AlwaysKeepLastEmpty = configuration.AlwaysKeepLastEmpty
            };

            var internalResult = await smartOpService.ValidateRowManagementConfigurationAsync(internalConfig);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Row management configuration validation failed: {Message}", ex.Message);
            return PublicResult.Failure($"Validation failed: {ex.Message}");
        }
    }

    public PublicRowManagementStatistics GetRowManagementStatistics()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var smartOpService = scope.ServiceProvider.GetRequiredService<Features.SmartAddDelete.Interfaces.ISmartOperationService>();

            var stats = smartOpService.GetRowManagementStatistics();
            return new PublicRowManagementStatistics
            {
                EmptyRowsCreated = stats.EmptyRowsCreated,
                RowsPhysicallyDeleted = stats.RowsPhysicallyDeleted,
                RowsContentCleared = stats.RowsContentCleared,
                RowsShifted = stats.RowsShifted,
                MinimumRowsEnforced = stats.MinimumRowsEnforced,
                LastEmptyRowMaintained = stats.LastEmptyRowMaintained
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get row management statistics: {Message}", ex.Message);
            return new PublicRowManagementStatistics();
        }
    }

    #endregion
}


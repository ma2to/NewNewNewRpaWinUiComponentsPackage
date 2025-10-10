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
/// Partial class containing Row/Column/Cell Batch Operations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Row/Column/Cell Batch Operations

    public async Task<BatchOperationResult> BatchUpdateCellsAsync(BatchUpdateCellsDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var rcService = scope.ServiceProvider.GetRequiredService<Features.RowColumnCell.Interfaces.IRowColumnCellService>();

            var operations = command.Operations.Select(op => new Core.ValueObjects.BatchCellOperation
            {
                RowIndex = op.RowIndex,
                ColumnIndex = op.ColumnIndex,
                Value = op.Value,
                OperationType = (Core.ValueObjects.CellOperationType)op.OperationType
            }).ToList();

            var batchCommand = Features.RowColumnCell.Commands.BatchUpdateCellsCommand.Create(operations);
            var result = await rcService.BatchUpdateCellsAsync(batchCommand, cancellationToken);

            return new BatchOperationResult(result.Success, result.AffectedItems, result.Errors, result.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch update cells");
            return new BatchOperationResult(false, 0, new[] { ex.Message }, TimeSpan.Zero);
        }
    }

    public async Task<BatchOperationResult> BatchRowOperationsAsync(BatchRowOperationsDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var rcService = scope.ServiceProvider.GetRequiredService<Features.RowColumnCell.Interfaces.IRowColumnCellService>();

            var operations = command.Operations.Select(op => new Core.ValueObjects.BatchRowOperation
            {
                RowIndex = op.RowIndex,
                RowData = op.RowData,
                OperationType = (Core.ValueObjects.BatchRowOperationType)op.OperationType
            }).ToList();

            if (operations.All(op => op.OperationType == Core.ValueObjects.BatchRowOperationType.Insert))
            {
                var insertCommand = Features.RowColumnCell.Commands.BatchInsertRowsCommand.Create(operations);
                var result = await rcService.BatchInsertRowsAsync(insertCommand, cancellationToken);
                return new BatchOperationResult(result.Success, result.AffectedItems, result.Errors, result.Duration);
            }
            else if (operations.All(op => op.OperationType == Core.ValueObjects.BatchRowOperationType.Delete))
            {
                var rowIndices = operations.Select(op => op.RowIndex).ToList();
                var deleteCommand = Features.RowColumnCell.Commands.BatchDeleteRowsCommand.Create(rowIndices);
                var result = await rcService.BatchDeleteRowsAsync(deleteCommand, cancellationToken);
                return new BatchOperationResult(result.Success, result.AffectedItems, result.Errors, result.Duration);
            }
            else
            {
                return new BatchOperationResult(false, 0, new[] { "Mixed operation types not supported" }, TimeSpan.Zero);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch row operations");
            return new BatchOperationResult(false, 0, new[] { ex.Message }, TimeSpan.Zero);
        }
    }

    public async Task<BatchOperationResult> BatchColumnOperationsAsync(BatchColumnOperationsDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var rcService = scope.ServiceProvider.GetRequiredService<Features.RowColumnCell.Interfaces.IRowColumnCellService>();

            var operations = command.Operations.Select(op => new Core.ValueObjects.BatchColumnOperation
            {
                ColumnName = op.ColumnName,
                Width = op.Width,
                NewPosition = op.NewPosition,
                NewName = op.NewName,
                OperationType = (Core.ValueObjects.ColumnOperationType)op.OperationType
            }).ToList();

            var batchCommand = Features.RowColumnCell.Commands.BatchUpdateColumnsCommand.Create(operations);
            var result = await rcService.BatchUpdateColumnsAsync(batchCommand, cancellationToken);

            return new BatchOperationResult(result.Success, result.AffectedItems, result.Errors, result.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch column operations");
            return new BatchOperationResult(false, 0, new[] { ex.Message }, TimeSpan.Zero);
        }
    }

    #endregion
}


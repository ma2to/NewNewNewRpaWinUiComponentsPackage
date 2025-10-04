using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowColumnCell.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowColumnCell.Interfaces;

/// <summary>
/// Interface for row, column, and cell operations service
/// </summary>
internal interface IRowColumnCellService
{
    /// <summary>
    /// Execute batch cell updates
    /// </summary>
    Task<OperationResult> BatchUpdateCellsAsync(BatchUpdateCellsCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute batch row insertions
    /// </summary>
    Task<OperationResult> BatchInsertRowsAsync(BatchInsertRowsCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute batch row deletions
    /// </summary>
    Task<OperationResult> BatchDeleteRowsAsync(BatchDeleteRowsCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute batch column updates
    /// </summary>
    Task<OperationResult> BatchUpdateColumnsAsync(BatchUpdateColumnsCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate batch operation before execution
    /// </summary>
    Task<Result> ValidateBatchOperationAsync(object command, CancellationToken cancellationToken = default);
}

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

    /// <summary>
    /// Begin batch update mode (suspend UI updates)
    /// </summary>
    void BeginBatchUpdate();

    /// <summary>
    /// End batch update mode (resume UI updates)
    /// </summary>
    void EndBatchUpdate();

    /// <summary>
    /// Check if batch update mode is active
    /// </summary>
    bool IsInBatchUpdate();

    /// <summary>
    /// Batch update column values for specified rows
    /// </summary>
    Task<int> BatchUpdateColumnAsync(IEnumerable<int> rowIndices, string columnName, object? newValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch transform column values using a transformation function
    /// </summary>
    Task<int> BatchTransformAsync(IEnumerable<int> rowIndices, string columnName, Func<object?, object?> transformFunc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch delete rows by indices
    /// </summary>
    Task<int> BatchDeleteRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch update cells
    /// </summary>
    Task<int> BatchUpdateCellsAsync(IEnumerable<PublicCellUpdate> cellUpdates, CancellationToken cancellationToken = default);
}

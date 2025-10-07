using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Batch;

/// <summary>
/// Public interface for DataGrid batch operations.
/// Provides high-performance bulk operations for multiple rows and cells.
/// </summary>
public interface IDataGridBatch
{
    /// <summary>
    /// Begins a batch update operation (disables UI updates).
    /// </summary>
    /// <returns>Result of the operation</returns>
    PublicResult BeginBatchUpdate();

    /// <summary>
    /// Ends a batch update operation (re-enables UI updates).
    /// </summary>
    /// <returns>Result of the operation</returns>
    PublicResult EndBatchUpdate();

    /// <summary>
    /// Updates multiple cells in a single operation.
    /// </summary>
    /// <param name="cellUpdates">Collection of cell updates</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with count of updated cells</returns>
    Task<PublicResult<int>> BatchUpdateCellsAsync(IEnumerable<PublicCellUpdate> cellUpdates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a column value for multiple rows.
    /// </summary>
    /// <param name="rowIndices">Row indices to update</param>
    /// <param name="columnName">Column name to update</param>
    /// <param name="newValue">New value to set</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with count of updated cells</returns>
    Task<PublicResult<int>> BatchUpdateColumnAsync(IEnumerable<int> rowIndices, string columnName, object? newValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple rows in a single operation.
    /// </summary>
    /// <param name="rowIndices">Row indices to delete</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with count of deleted rows</returns>
    Task<PublicResult<int>> BatchDeleteRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a transformation function to multiple cells.
    /// </summary>
    /// <param name="rowIndices">Row indices to transform</param>
    /// <param name="columnName">Column name to transform</param>
    /// <param name="transformFunc">Transformation function</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with count of transformed cells</returns>
    Task<PublicResult<int>> BatchTransformAsync(IEnumerable<int> rowIndices, string columnName, Func<object?, object?> transformFunc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if currently in batch update mode.
    /// </summary>
    /// <returns>True if in batch update mode</returns>
    bool IsInBatchUpdate();
}

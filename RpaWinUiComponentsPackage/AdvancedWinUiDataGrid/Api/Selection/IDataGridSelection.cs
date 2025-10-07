
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Selection;

/// <summary>
/// Public interface for DataGrid selection operations.
/// Provides comprehensive selection functionality including row, cell, and range selection.
/// </summary>
public interface IDataGridSelection
{
    /// <summary>
    /// Selects a specific row by index.
    /// </summary>
    /// <param name="rowIndex">Row index to select</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SelectRowAsync(int rowIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects multiple rows by indices.
    /// </summary>
    /// <param name="rowIndices">Collection of row indices to select</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SelectRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects a range of rows.
    /// </summary>
    /// <param name="startRowIndex">Start row index</param>
    /// <param name="endRowIndex">End row index</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SelectRowRangeAsync(int startRowIndex, int endRowIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects all rows in the grid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SelectAllRowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all row selections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearSelectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets indices of currently selected rows.
    /// </summary>
    /// <returns>Collection of selected row indices</returns>
    IReadOnlyList<int> GetSelectedRowIndices();

    /// <summary>
    /// Gets count of selected rows.
    /// </summary>
    /// <returns>Number of selected rows</returns>
    int GetSelectedRowCount();

    /// <summary>
    /// Checks if a row is selected.
    /// </summary>
    /// <param name="rowIndex">Row index to check</param>
    /// <returns>True if row is selected</returns>
    bool IsRowSelected(int rowIndex);

    /// <summary>
    /// Gets data from selected rows.
    /// </summary>
    /// <returns>Collection of row data for selected rows</returns>
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GetSelectedRowsData();
}

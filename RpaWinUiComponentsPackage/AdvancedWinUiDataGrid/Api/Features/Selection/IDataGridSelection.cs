
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Selection;

/// <summary>
/// Public interface for DataGrid selection operations.
/// Provides comprehensive selection functionality including row, cell, and range selection.
/// </summary>
public interface IDataGridSelection
{
    /// <summary>
    /// Selects a specific row by index.
    /// WARNING: rowIndex is unstable - changes on sort/filter/delete. Use SelectRowAsync(string rowId) instead.
    /// </summary>
    /// <param name="rowIndex">Row index to select</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    [Obsolete("Use SelectRowAsync(string rowId) instead. rowIndex is unstable and changes on sort/filter/delete operations.", false)]
    Task<PublicResult> SelectRowAsync(int rowIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects a specific row by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SelectRowAsync(string rowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects multiple rows by indices.
    /// WARNING: rowIndices are unstable - change on sort/filter/delete. Use SelectRowsAsync(IEnumerable<string> rowIds) instead.
    /// </summary>
    /// <param name="rowIndices">Collection of row indices to select</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    [Obsolete("Use SelectRowsAsync(IEnumerable<string> rowIds) instead. rowIndices are unstable and change on sort/filter/delete operations.", false)]
    Task<PublicResult> SelectRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects multiple rows by stable row IDs.
    /// STABLE: Uses rowIds which persist across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowIds">Collection of stable row identifiers (from __rowId field)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SelectRowsAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default);

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
    /// Checks if a row is selected by index.
    /// WARNING: rowIndex is unstable - changes on sort/filter/delete. Use IsRowSelected(string rowId) instead.
    /// </summary>
    /// <param name="rowIndex">Row index to check</param>
    /// <returns>True if row is selected</returns>
    [Obsolete("Use IsRowSelected(string rowId) instead. rowIndex is unstable and changes on sort/filter/delete operations.", false)]
    bool IsRowSelected(int rowIndex);

    /// <summary>
    /// Checks if a row is selected by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <returns>True if row is selected</returns>
    bool IsRowSelected(string rowId);

    /// <summary>
    /// Gets data from selected rows.
    /// </summary>
    /// <returns>Collection of row data for selected rows</returns>
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GetSelectedRowsData();
}

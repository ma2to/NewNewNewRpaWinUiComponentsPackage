
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Rows;

/// <summary>
/// Public interface for DataGrid row operations.
/// Provides comprehensive row management including adding, removing, updating, and querying rows.
/// </summary>
public interface IDataGridRows
{
    /// <summary>
    /// Adds a new row to the grid.
    /// </summary>
    /// <param name="rowData">Row data as dictionary</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with index of added row</returns>
    Task<PublicResult<int>> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple rows to the grid.
    /// </summary>
    /// <param name="rowsData">Collection of row data</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with count of added rows</returns>
    Task<PublicResult<int>> AddRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a row at a specific index.
    /// </summary>
    /// <param name="rowIndex">Index to insert at</param>
    /// <param name="rowData">Row data as dictionary</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a row at a specific index.
    /// </summary>
    /// <param name="rowIndex">Row index to update</param>
    /// <param name="rowData">New row data</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> UpdateRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a row at a specific index.
    /// </summary>
    /// <param name="rowIndex">Row index to remove</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> RemoveRowAsync(int rowIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple rows by indices.
    /// </summary>
    /// <param name="rowIndices">Collection of row indices to remove</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with count of removed rows</returns>
    Task<PublicResult<int>> RemoveRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all rows from the grid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearAllRowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets row data at a specific index.
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <returns>Row data as dictionary or null if not found</returns>
    IReadOnlyDictionary<string, object?>? GetRow(int rowIndex);

    /// <summary>
    /// Gets all row data.
    /// </summary>
    /// <returns>Collection of all row data</returns>
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GetAllRows();

    /// <summary>
    /// Gets row count.
    /// </summary>
    /// <returns>Total number of rows</returns>
    int GetRowCount();

    /// <summary>
    /// Checks if a row exists at index.
    /// </summary>
    /// <param name="rowIndex">Row index to check</param>
    /// <returns>True if row exists</returns>
    bool RowExists(int rowIndex);

    /// <summary>
    /// Duplicates a row at a specific index.
    /// </summary>
    /// <param name="rowIndex">Row index to duplicate</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with index of new row</returns>
    Task<PublicResult<int>> DuplicateRowAsync(int rowIndex, CancellationToken cancellationToken = default);
}

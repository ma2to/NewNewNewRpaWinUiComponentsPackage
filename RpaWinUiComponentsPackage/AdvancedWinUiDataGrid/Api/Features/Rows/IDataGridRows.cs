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

    // /// <summary>
    // /// Inserts a row at a specific index.
    // /// WARNING: rowIndex is unstable - changes on sort/filter/delete. Use InsertRowBeforeId or InsertRowAfterId instead.
    // /// </summary>
    // /// <param name="rowIndex">Index to insert at</param>
    // /// <param name="rowData">Row data as dictionary</param>
    // /// <param name="cancellationToken">Cancellation token for operation</param>
    // /// <returns>Result of the operation</returns>
    // [Obsolete("Use InsertRowBeforeIdAsync or InsertRowAfterIdAsync instead. rowIndex is unstable and changes on sort/filter/delete operations.", false)]
    // Task<PublicResult> InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a row before a specific row by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="referenceRowId">Stable row identifier to insert before</param>
    /// <param name="rowData">Row data as dictionary (null creates empty row)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> InsertRowBeforeIdAsync(string referenceRowId, IReadOnlyDictionary<string, object?>? rowData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a row after a specific row by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="referenceRowId">Stable row identifier to insert after</param>
    /// <param name="rowData">Row data as dictionary (null creates empty row)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> InsertRowAfterIdAsync(string referenceRowId, IReadOnlyDictionary<string, object?>? rowData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a row by its unique identifier.
    /// </summary>
    /// <param name="rowId">Unique stable row identifier</param>
    /// <param name="rowData">New row data</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> UpdateRowAsync(string rowId, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a row by its unique identifier.
    /// </summary>
    /// <param name="rowId">Unique stable row identifier</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> RemoveRowAsync(string rowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple rows by their unique identifiers.
    /// </summary>
    /// <param name="rowIds">Collection of unique row identifiers to remove</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with count of removed rows</returns>
    Task<PublicResult<int>> RemoveRowsAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all rows from the grid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearAllRowsAsync(CancellationToken cancellationToken = default);

    // /// <summary>
    // /// Gets row data at a specific index.
    // /// WARNING: rowIndex is unstable - changes on sort/filter/delete. Use GetRow(string rowId) instead.
    // /// </summary>
    // /// <param name="rowIndex">Row index</param>
    // /// <returns>Row data as dictionary or null if not found</returns>
    // [Obsolete("Use GetRow(string rowId) instead. rowIndex is unstable and changes on sort/filter/delete operations.", false)]
    // IReadOnlyDictionary<string, object?>? GetRow(int rowIndex);

    /// <summary>
    /// Gets row data by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <returns>Row data as dictionary or null if not found</returns>
    IReadOnlyDictionary<string, object?>? GetRow(string rowId);

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

    // /// <summary>
    // /// Checks if a row exists at index.
    // /// WARNING: rowIndex is unstable - changes on sort/filter/delete. Use RowExists(string rowId) instead.
    // /// </summary>
    // /// <param name="rowIndex">Row index to check</param>
    // /// <returns>True if row exists</returns>
    // [Obsolete("Use RowExists(string rowId) instead. rowIndex is unstable and changes on sort/filter/delete operations.", false)]
    // bool RowExists(int rowIndex);

    /// <summary>
    /// Checks if a row exists by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <returns>True if row exists</returns>
    bool RowExists(string rowId);

    // /// <summary>
    // /// Duplicates a row at a specific index.
    // /// WARNING: rowIndex is unstable - changes on sort/filter/delete. Use DuplicateRowAsync(string rowId) instead.
    // /// </summary>
    // /// <param name="rowIndex">Row index to duplicate</param>
    // /// <param name="cancellationToken">Cancellation token for operation</param>
    // /// <returns>Result with index of new row</returns>
    // [Obsolete("Use DuplicateRowAsync(string rowId) instead. rowIndex is unstable and changes on sort/filter/delete operations.", false)]
    // Task<PublicResult<int>> DuplicateRowAsync(int rowIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates a row by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with rowId of new row</returns>
    Task<PublicResult<string>> DuplicateRowAsync(string rowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the unique row ID for a row at the specified index.
    /// USE CASE: User clicks on row in UI, UI event provides RowIndex, need to convert to stable RowID.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index</param>
    /// <returns>The unique row ID (from __rowId field) or null if not found</returns>
    string? GetRowIdByIndex(int rowIndex);

    /// <summary>
    /// Gets the current row index for a row with the specified ID.
    /// USE CASE: Have RowID from database, want to scroll/highlight row in UI.
    /// </summary>
    /// <param name="rowId">Unique row identifier</param>
    /// <returns>Current zero-based row index or null if not found</returns>
    int? GetRowIndexById(string rowId);

    /// <summary>
    /// Gets the row ID of the currently selected row (if single selection).
    /// USE CASE: Shortcut to avoid manual conversion in single-select scenarios.
    /// </summary>
    /// <returns>Row ID or null if no row selected</returns>
    string? GetSelectedRowId();

    /// <summary>
    /// Gets the row IDs of all currently selected rows.
    /// USE CASE: Shortcut for multi-select delete/update operations.
    /// </summary>
    /// <returns>Array of row IDs (empty array if no selection)</returns>
    string[] GetSelectedRowIds();

    /// <summary>
    /// Validate row data without adding to grid.
    /// Useful for pre-validating data before calling AddRowAsync.
    /// </summary>
    /// <param name="rowData">Row data to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with errors (if any)</returns>
    Task<PublicValidationResult> ValidateRowDataAsync(
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default);
}

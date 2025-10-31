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
    /// Inserts an empty row after a specific row by stable row ID.
    /// Convenience method for Interactive mode where empty rows are inserted via UI button.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// PUBLIC API: Can be called from application code for manual empty row insertion.
    /// This method creates an empty row with all column values set to null.
    /// </summary>
    /// <param name="referenceRowId">Stable row identifier to insert after</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> InsertEmptyRowAfterAsync(string referenceRowId, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// PROFESSIONAL SOLUTION: Virtuálne vloží prázdny riadok na pozíciu (posunie data smerom nadol v rámci page).
    /// NEZMENÍ celkový počet riadkov - posledný riadok page sa prepisuje prázdnými hodnotami.
    /// USE CASE: Fixed page size grids where insert should shift data down without changing row count.
    /// VIRTUAL OPERATION: Does not physically add row to dataset, only shifts existing data.
    /// </summary>
    /// <param name="referenceRowId">Stable row identifier to insert after</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> VirtualInsertEmptyRowAfterAsync(string referenceRowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// PROFESSIONAL SOLUTION: Virtuálne vloží prázdny riadok PRED zadaný riadok (posunie data smerom nadol v rámci page).
    /// NEZMENÍ celkový počet riadkov - posledný riadok page sa prepisuje prázdnymi hodnotami.
    /// USE CASE: Fixed page size grids where "Insert Above" should shift data down without changing row count.
    /// VIRTUAL OPERATION: Does not physically add row to dataset, only shifts existing data.
    /// ALGORITHM:
    ///   1. Find target row by RowId, get its __rowNumber (e.g., 5)
    ///   2. Shift all rows with __rowNumber >= 5 down by 1 (__rowNumber++)
    ///   3. Create new empty row with __rowNumber = 5
    ///   4. Trigger UI refresh to update DataGridViewModel
    /// </summary>
    /// <param name="referenceRowId">Stable row identifier to insert before (this row will be shifted down)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> VirtualInsertEmptyRowBeforeAsync(string referenceRowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// PROFESSIONAL SOLUTION: Virtuálne zmaže riadok na pozícii (posunie data smerom nahor v rámci page).
    /// NEZMENÍ celkový počet riadkov - dáta sa posunú nahor a posledný riadok ostane prázdny.
    /// USE CASE: Fixed page size grids where delete should shift data up without changing row count.
    /// VIRTUAL OPERATION: Does not physically remove row from dataset, only shifts existing data.
    /// EFFECT: Row data is completely deleted (all subsequent rows shift up), last row becomes empty.
    /// </summary>
    /// <param name="rowId">Stable row identifier to delete</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> VirtualDeleteRowAsync(string rowId, CancellationToken cancellationToken = default);
}

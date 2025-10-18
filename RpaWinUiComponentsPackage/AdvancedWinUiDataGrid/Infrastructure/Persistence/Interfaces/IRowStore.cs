using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

/// <summary>
/// Internal interface for row data persistence and retrieval
/// Supports streaming, filtering, and validation state management
/// </summary>
internal interface IRowStore
{
    /// <summary>
    /// Stream rows in batches for memory-efficient processing
    /// Supports filtered and unfiltered data retrieval, with optional checkbox filtering
    /// </summary>
    /// <param name="onlyFiltered">If true, returns only rows that match active filters</param>
    /// <param name="onlyChecked">If true, returns only rows where checkbox column is checked</param>
    /// <param name="batchSize">Number of rows per batch</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Async enumerable of row batches</returns>
    /// <remarks>
    /// When both onlyFiltered and onlyChecked are true, returns rows that match BOTH criteria (AND logic)
    /// </remarks>
    IAsyncEnumerable<IReadOnlyList<IReadOnlyDictionary<string, object?>>> StreamRowsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        int batchSize = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all rows as a collection
    /// Use StreamRowsAsync for large datasets
    /// </summary>
    /// <param name="onlyFiltered">If true, returns only filtered view; otherwise returns all rows</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>All rows in the store (filtered or unfiltered based on parameter)</returns>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        bool onlyFiltered,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all rows as a collection (backward compatibility overload)
    /// Use StreamRowsAsync for large datasets
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>All rows in the store</returns>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total row count
    /// </summary>
    /// <param name="onlyFiltered">If true, returns count of filtered view; otherwise returns count of all rows</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Total number of rows (filtered or unfiltered based on parameter)</returns>
    Task<long> GetRowCountAsync(
        bool onlyFiltered,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total row count (backward compatibility overload)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Total number of rows</returns>
    Task<long> GetRowCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get row count for filtered data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of rows that match active filters</returns>
    Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persist rows to the store
    /// Supports batch operations for better performance
    /// </summary>
    /// <param name="rows">Rows to persist</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task PersistRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replace all data in the store
    /// </summary>
    /// <param name="rows">New rows to store</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task ReplaceAllRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Append rows to existing data
    /// </summary>
    /// <param name="rows">Rows to append</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task AppendRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the grid has at least one empty row on initialization.
    /// Called during grid creation to establish the initial state.
    /// Only adds a row if the store is completely empty.
    /// </summary>
    /// <param name="columnNames">Column names for the initial empty row</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task EnsureInitialEmptyRowAsync(
        IEnumerable<string> columnNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert rows at specific position
    /// </summary>
    /// <param name="rows">Rows to insert</param>
    /// <param name="startIndex">Starting index for insertion</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task InsertRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Write validation results for rows
    /// Used by validation service to store validation state
    /// </summary>
    /// <param name="results">Validation results to store</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task WriteValidationResultsAsync(
        IEnumerable<ValidationError> results,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if validation state exists for current scope
    /// </summary>
    /// <param name="onlyFiltered">Whether to check filtered or all data</param>
    /// <param name="onlyChecked">Whether to check only checked rows</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if validation state exists</returns>
    Task<bool> HasValidationStateForScopeAsync(
        bool onlyFiltered,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if all non-empty rows are marked as valid
    /// Fast path for validation operations
    /// </summary>
    /// <param name="onlyFiltered">Whether to check filtered or all data</param>
    /// <param name="onlyChecked">Whether to check only checked rows</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if all non-empty rows are valid</returns>
    Task<bool> AreAllNonEmptyRowsMarkedValidAsync(
        bool onlyFiltered,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get validation errors for rows
    /// </summary>
    /// <param name="onlyFiltered">Whether to get errors for filtered or all data</param>
    /// <param name="onlyChecked">Whether to get errors for only checked rows</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Validation errors found</returns>
    Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get validation errors for a specific row
    /// </summary>
    /// <param name="rowId">Row ID to get validation errors for</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Validation errors for the row</returns>
    Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(
        string rowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove specific rows by their IDs
    /// </summary>
    /// <param name="rowIds">Row IDs to remove</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task RemoveRowsAsync(
        IEnumerable<string> rowIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific row by RowID (PRIMARY - stable identifier)
    /// </summary>
    /// <param name="rowId">Unique stable row identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Row data or null if not found</returns>
    Task<IReadOnlyDictionary<string, object?>?> GetRowByIdAsync(
        string rowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a specific row by RowID (PRIMARY - stable identifier)
    /// </summary>
    /// <param name="rowId">Unique stable row identifier</param>
    /// <param name="rowData">New row data</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateRowByIdAsync(
        string rowId,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a specific row by RowID (PRIMARY - stable identifier)
    /// </summary>
    /// <param name="rowId">Unique stable row identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if removed successfully</returns>
    Task<bool> RemoveRowByIdAsync(
        string rowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific row by index (DEPRECATED - use GetRowByIdAsync for stability)
    /// </summary>
    /// <param name="rowIndex">Row index (0-based)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Row data or null if not found</returns>
    Task<IReadOnlyDictionary<string, object?>?> GetRowAsync(
        int rowIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a specific row by index (DEPRECATED - use UpdateRowByIdAsync for stability)
    /// </summary>
    /// <param name="rowIndex">Row index (0-based)</param>
    /// <param name="rowData">New row data</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateRowAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all data from the store
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear validation state
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    Task ClearValidationStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set filter criteria for the store and build filtered view index
    /// CRITICAL: This builds the filtered row index for efficient filtered data access
    /// Used by filter service to update what constitutes "filtered" data
    /// Performance: O(n) where n = total rows. Builds index once, subsequent filtered access is O(1) per row.
    /// </summary>
    /// <param name="filterCriteria">Active filter criteria (null or empty to clear filters)</param>
    void SetFilterCriteria(IReadOnlyList<object>? filterCriteria);

    /// <summary>
    /// Clears filter criteria and filtered view index
    /// Equivalent to calling SetFilterCriteria(null)
    /// </summary>
    void ClearFilterCriteria();

    /// <summary>
    /// Get current filter criteria
    /// </summary>
    /// <returns>Current filter criteria (empty list if no filters active)</returns>
    IReadOnlyList<object> GetFilterCriteria();

    /// <summary>
    /// Maps filtered row index to original row index
    /// CRITICAL FOR EDITS: When user edits a cell in filtered view, we need to update the correct row in original dataset
    /// Example: Filtered view shows rows [5, 12, 23]. User edits filteredIndex=1 (original row 12) → returns 12
    /// </summary>
    /// <param name="filteredIndex">Index in filtered view (0-based)</param>
    /// <returns>Index in original dataset, or null if not found or no filter active</returns>
    int? MapFilteredIndexToOriginalIndex(int filteredIndex);

    /// <summary>
    /// Get last row in the store (optimized - O(1) if possible, O(n) for concurrent dictionary)
    /// Returns row with highest __rowId (ULID timestamp-based, lexicographically sortable)
    /// CRITICAL: Used by 3-step cleanup to check if last row is empty
    /// Performance: For 10M+ rows, O(n) key enumeration is acceptable for this operation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Last row data or null if store is empty</returns>
    /// <remarks>
    /// ULID is lexicographically sortable by timestamp, so Max(Keys) returns most recent row.
    /// This is critical for ensuring "always keep last empty row" functionality.
    /// </remarks>
    Task<IReadOnlyDictionary<string, object?>?> GetLastRowAsync(
        CancellationToken cancellationToken = default);

    // Public API synchronous compatibility methods
    Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);
    Task<int> AddRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData, CancellationToken cancellationToken = default);
    Task InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);
    Task RemoveRowAsync(int rowIndex, CancellationToken cancellationToken = default);
    Task<int> RemoveRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default);
    Task ClearAllRowsAsync(CancellationToken cancellationToken = default);
    IReadOnlyDictionary<string, object?>? GetRow(int rowIndex);
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GetAllRows();
    int GetRowCount();
    bool RowExists(int rowIndex);
}
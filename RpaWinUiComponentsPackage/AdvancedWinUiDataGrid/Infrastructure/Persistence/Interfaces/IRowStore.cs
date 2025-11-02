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
    /// ✅ SENIOR FIX: Get a range of rows for pagination/virtualization
    /// CRITICAL: Enables virtual pagination - UI loads only visible page instead of entire dataset
    /// PERFORMANCE: O(n) for skip/take in InMemoryRowStore, O(1) for SQL LIMIT/OFFSET in HybridRowStore
    /// ARCHITECTURE: This is the foundation of dual-mode architecture:
    /// - Small datasets (<1000): Load all rows into UI (backward compatible)
    /// - Large datasets (>=1000): Load only current page into UI (virtual pagination)
    /// BUSINESS LAYER: Validations, search, filter, smart delete still use GetAllRowsAsync/StreamRowsAsync (full dataset access)
    /// </summary>
    /// <param name="startIndex">Starting row index (0-based, absolute position in dataset)</param>
    /// <param name="count">Number of rows to retrieve (page size, typically 15-100)</param>
    /// <param name="onlyFiltered">If true, returns range from filtered view; otherwise from all rows</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Range of rows (may be fewer than count if near end of dataset)</returns>
    /// <example>
    /// Dataset: 1000 rows, PageSize: 15
    /// Page 1: GetRowsRangeAsync(0, 15)   → rows 0-14
    /// Page 5: GetRowsRangeAsync(60, 15)  → rows 60-74
    /// Page 67: GetRowsRangeAsync(990, 15) → rows 990-999 (only 10 rows returned)
    /// </example>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsRangeAsync(
        long startIndex,
        int count,
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get row count for filtered data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Number of rows that match active filters</returns>
    Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ✅ PROBLEM 2 FIX: Check if any filter is currently active
    /// Used by UI to determine whether to show total count or filtered count
    /// </summary>
    /// <returns>True if filter criteria is set and active; false otherwise</returns>
    bool HasActiveFilter();

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
    /// Insert single row AFTER the specified row index (convenience method)
    /// </summary>
    /// <param name="targetRowIndex">Index of row after which to insert</param>
    /// <param name="newRow">Row data to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InsertRowAfterAsync(
        int targetRowIndex,
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert single row BEFORE the specified row index (convenience method)
    /// </summary>
    /// <param name="targetRowIndex">Index of row before which to insert</param>
    /// <param name="newRow">Row data to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InsertRowBeforeAsync(
        int targetRowIndex,
        IReadOnlyDictionary<string, object?> newRow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert single row at the top (index 0) (convenience method)
    /// </summary>
    /// <param name="newRow">Row data to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InsertRowAtTopAsync(
        IReadOnlyDictionary<string, object?> newRow,
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

    /// <summary>
    /// Set sort criteria for the store.
    /// In HybridRowStore: Builds SQL ORDER BY clause for efficient database sorting.
    /// In InMemoryRowStore: No-op (sorting handled by SortService).
    /// </summary>
    /// <param name="columnName">Column to sort by</param>
    /// <param name="direction">Sort direction (Ascending/Descending/None)</param>
    void SetSortCriteria(string columnName, Common.SortDirection direction);

    /// <summary>
    /// Clear sort criteria (revert to default ordering).
    /// </summary>
    void ClearSortCriteria();

    /// <summary>
    /// Perform full-text search on row data.
    /// In HybridRowStore: Uses SQLite FTS5 for efficient search.
    /// In InMemoryRowStore: Uses LINQ-based in-memory search.
    /// </summary>
    /// <param name="searchText">Text to search for</param>
    /// <param name="targetColumns">Optional: specific columns to search (null = all columns)</param>
    /// <param name="caseSensitive">Whether search should be case-sensitive</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of row IDs that match the search criteria</returns>
    Task<IReadOnlyList<string>> SearchAsync(
        string searchText,
        string[]? targetColumns = null,
        bool caseSensitive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Set complex filter expression for the store
    /// NEW API: Supports complex logical expressions with AND/OR/ANDALSO/ORELSE and nested conditions
    /// Example: ((Age > 18 AND City = 'Bratislava') OR (Status = 'Active'))
    /// Replaces SetFilterCriteria for advanced filtering scenarios
    /// </summary>
    /// <param name="expression">Filter expression tree (null to clear filter)</param>
    void SetFilterExpression(Features.Filter.Models.FilterExpression? expression);

    /// <summary>
    /// Get current filter expression
    /// </summary>
    /// <returns>Current filter expression (null if no filter active)</returns>
    Features.Filter.Models.FilterExpression? GetFilterExpression();

    /// <summary>
    /// Initialize store with fixed number of empty rows (for page-based initialization)
    /// Replaces EnsureInitialEmptyRowAsync - creates exactly N empty rows instead of ensuring minimum 1
    /// Used during grid initialization to pre-allocate rows for first page
    /// </summary>
    /// <param name="columnNames">Column names for empty rows</param>
    /// <param name="rowCount">Number of empty rows to create (typically page size)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeEmptyRowsAsync(
        IEnumerable<string> columnNames,
        int rowCount,
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

    // SENIOR FIX: Validation cache methods (prevents ValidateAll infinite loop)
    /// <summary>
    /// Checks if a row has already been validated (cache check).
    /// </summary>
    bool IsRowValidationCached(string rowId);

    /// <summary>
    /// Marks a row as validated in the cache.
    /// </summary>
    void MarkRowAsValidated(string rowId);

    /// <summary>
    /// Clears validation cache.
    /// </summary>
    void ClearValidationCache();

    /// <summary>
    /// Batch writes validation results for multiple rows in a single operation.
    /// Prevents infinite validation loop by avoiding multiple DataChanged events.
    /// </summary>
    Task WriteValidationResultsBatchAsync(
        Dictionary<string, ValidationError[]> validationResults,
        CancellationToken cancellationToken = default);

    // ========== BREAKING CHANGE v3.0: rowId-based helper methods ==========

    /// <summary>
    /// Gets the stable rowId for a row at the specified index.
    /// HELPER: Converts volatile rowIndex to stable rowId.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index (current position in view)</param>
    /// <returns>Stable rowId (from __rowId field) or null if row not found</returns>
    string? GetRowIdByIndex(int rowIndex);

    /// <summary>
    /// Gets the current rowIndex for a row with the specified rowId.
    /// HELPER: Converts stable rowId to volatile rowIndex (current position in view).
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <returns>Current zero-based row index or null if row not found</returns>
    int? GetRowIndexById(string rowId);

    /// <summary>
    /// Gets a row by stable rowId.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <returns>Row data or null if not found</returns>
    IReadOnlyDictionary<string, object?>? GetRowById(string rowId);

    /// <summary>
    /// Checks if a row exists by stable rowId.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <returns>True if row exists, false otherwise</returns>
    bool RowExistsById(string rowId);

    /// <summary>
    /// PROFESSIONAL QUALITY: Bulk update multiple rows in a single operation.
    /// Suppresses individual PropertyChanged notifications - caller triggers single batch notification.
    /// PERFORMANCE: 10-50x faster than serial UpdateRowByIdAsync calls for virtual operations.
    /// CRITICAL: This solves slowness EVEN FOR SINGLE ROW operations because virtual insert/delete
    /// must shift ALL subsequent rows (e.g., insert 1 row at position 5 → shift 15 rows = 15 updates).
    /// With bulk update: 15 serial updates (750ms) → 1 batch update (50ms).
    /// </summary>
    /// <param name="updates">Dictionary of rowId → rowData mappings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of successfully updated rows</returns>
    Task<int> BulkUpdateRowsAsync(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> updates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ✅ RIEŠENIE #2: Physically inserts row at specified index (FIXED UI POOL).
    /// Uses ULID timestamp interpolation to control sort order.
    /// RowCount increases by 1.
    /// </summary>
    /// <param name="index">Target position (0-based)</param>
    /// <param name="rowData">Column values (WITHOUT __rowId - system column added automatically)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>RowId of newly inserted row (STABLE identifier)</returns>
    Task<string> InsertRowAtIndexAsync(int index, IReadOnlyDictionary<string, object?>? rowData, CancellationToken cancellationToken = default);

    /// <summary>
    /// ✅ RIEŠENIE #2: Physically deletes row by RowId (FIXED UI POOL).
    /// RowCount decreases by 1.
    /// Rows after deleted row automatically SHIFT UP via sort order.
    /// </summary>
    /// <param name="rowId">STABLE identifier (not RowIndex!)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteRowByIdAsync(string rowId, CancellationToken cancellationToken = default);
}
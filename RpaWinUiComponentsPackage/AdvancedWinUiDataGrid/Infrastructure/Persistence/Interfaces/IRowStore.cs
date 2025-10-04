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
    /// Supports filtered and unfiltered data retrieval
    /// </summary>
    /// <param name="onlyFiltered">If true, returns only rows that match active filters</param>
    /// <param name="batchSize">Number of rows per batch</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Async enumerable of row batches</returns>
    IAsyncEnumerable<IReadOnlyList<IReadOnlyDictionary<string, object?>>> StreamRowsAsync(
        bool onlyFiltered = false,
        int batchSize = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all rows as a collection
    /// Use StreamRowsAsync for large datasets
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>All rows in the store</returns>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllRowsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total row count
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
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if validation state exists</returns>
    Task<bool> HasValidationStateForScopeAsync(
        bool onlyFiltered,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if all non-empty rows are marked as valid
    /// Fast path for validation operations
    /// </summary>
    /// <param name="onlyFiltered">Whether to check filtered or all data</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if all non-empty rows are valid</returns>
    Task<bool> AreAllNonEmptyRowsMarkedValidAsync(
        bool onlyFiltered,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get validation errors for rows
    /// </summary>
    /// <param name="onlyFiltered">Whether to get errors for filtered or all data</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Validation errors found</returns>
    Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(
        bool onlyFiltered = false,
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
    /// Get a specific row by index
    /// </summary>
    /// <param name="rowIndex">Row index (0-based)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Row data or null if not found</returns>
    Task<IReadOnlyDictionary<string, object?>?> GetRowAsync(
        int rowIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a specific row by index
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
    /// Set filter criteria for the store
    /// Used by filter service to update what constitutes "filtered" data
    /// </summary>
    /// <param name="filterCriteria">Active filter criteria</param>
    void SetFilterCriteria(IReadOnlyList<object> filterCriteria);

    /// <summary>
    /// Get current filter criteria
    /// </summary>
    /// <returns>Current filter criteria</returns>
    IReadOnlyList<object> GetFilterCriteria();
}
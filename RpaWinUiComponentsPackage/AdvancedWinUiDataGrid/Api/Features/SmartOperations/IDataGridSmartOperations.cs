namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.SmartOperations;

/// <summary>
/// Public interface for smart row management operations.
/// Provides intelligent add/delete operations with automatic minimum rows management.
/// </summary>
public interface IDataGridSmartOperations
{
    /// <summary>
    /// Smart delete rows - clears content or physically removes rows based on minimum rows configuration.
    ///
    /// Behavior:
    /// - If currentRows &lt;= minimumRows: Clears content and shifts rows up (keeps grid at minimum size)
    /// - If currentRows &gt; minimumRows: Physically removes rows (but keeps 1 empty at end)
    /// </summary>
    /// <param name="rowIndices">Indices of rows to delete</param>
    /// <param name="config">Smart operations configuration (null uses default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with statistics about the operation</returns>
    Task<PublicSmartOperationResult> SmartDeleteRowsAsync(
        IEnumerable<int> rowIndices,
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Smart delete single row - convenience method for deleting one row.
    /// </summary>
    /// <param name="rowIndex">Index of row to delete</param>
    /// <param name="config">Smart operations configuration (null uses default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with statistics about the operation</returns>
    Task<PublicSmartOperationResult> SmartDeleteRowAsync(
        int rowIndex,
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Smart delete rows by stable row IDs - recommended to avoid index shifting bugs.
    /// Uses unique row IDs instead of indices, preventing race conditions during rapid deletes.
    ///
    /// Behavior:
    /// - Directly removes rows by ID (no GetAll/ReplaceAll overhead)
    /// - Maintains minimum rows requirement
    /// - Adds empty row at end if AlwaysKeepLastEmpty is enabled
    /// </summary>
    /// <param name="rowIds">Unique row IDs to delete</param>
    /// <param name="config">Smart operations configuration (null uses default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with statistics about the operation</returns>
    Task<PublicSmartOperationResult> SmartDeleteRowsByIdAsync(
        IEnumerable<string> rowIds,
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Smart delete single row by ID - convenience method for deleting one row by its unique ID.
    /// </summary>
    /// <param name="rowId">Unique row ID to delete</param>
    /// <param name="config">Smart operations configuration (null uses default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with statistics about the operation</returns>
    Task<PublicSmartOperationResult> SmartDeleteRowByIdAsync(
        string rowId,
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Smart add rows - adds data rows and maintains minimum rows + empty row at end.
    ///
    /// Behavior:
    /// - If adding data &gt;= minimumRows: Adds all data + 1 empty row
    /// - If adding data &lt; minimumRows: Adds data + fills to minimumRows + 1 empty row
    /// </summary>
    /// <param name="rowsData">Row data to add</param>
    /// <param name="config">Smart operations configuration (null uses default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with statistics about the operation</returns>
    Task<PublicSmartOperationResult> SmartAddRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rowsData,
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-expand empty row - automatically adds empty row at end if needed.
    /// Checks if last row is empty, and adds one if not.
    /// </summary>
    /// <param name="config">Smart operations configuration (null uses default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with statistics about the operation</returns>
    Task<PublicSmartOperationResult> AutoExpandEmptyRowAsync(
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current smart operations configuration.
    /// </summary>
    /// <returns>Current configuration</returns>
    PublicSmartOperationsConfig GetCurrentConfig();

    /// <summary>
    /// Updates the smart operations configuration.
    /// </summary>
    /// <param name="config">New configuration</param>
    /// <returns>Result of the update</returns>
    Task<PublicResult> UpdateConfigAsync(PublicSmartOperationsConfig config);

    /// <summary>
    /// Manually triggers 3-step row cleanup and maintenance.
    /// STEP 1: Remove ALL empty rows from anywhere in dataset (streaming O(n))
    /// STEP 2: Ensure minimum rows by adding empty rows at end
    /// STEP 3: Ensure last row is empty (independent of minRows check)
    ///
    /// Use cases:
    /// - After custom bulk operations
    /// - After validation-based deletions (when implemented)
    /// - Manual data cleanup trigger
    /// - Before export to ensure clean dataset
    /// </summary>
    /// <param name="config">Smart operations configuration (null uses current default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with statistics about cleanup operation</returns>
    /// <remarks>
    /// Performance: O(n) streaming + O(k) delete + O(m) add
    /// Safe for 10M+ rows datasets
    /// </remarks>
    Task<PublicSmartOperationResult> EnsureMinRowsAndLastEmptyAsync(
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default);
}

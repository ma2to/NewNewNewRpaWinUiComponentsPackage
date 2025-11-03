using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Editing;

/// <summary>
/// Public interface for DataGrid editing operations.
/// Provides cell editing functionality with validation and change tracking.
/// BREAKING CHANGE v3.0: All methods now use rowId instead of rowIndex for stable row identification.
/// </summary>
public interface IDataGridEditing
{
    /// <summary>
    /// Begins editing a cell by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="columnName">Column name</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> BeginEditAsync(string rowId, string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current cell edit.
    /// </summary>
    /// <param name="newValue">New value to commit</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> CommitEditAsync(object? newValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the current cell edit.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> CancelEditAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a cell value directly by stable row ID (without begin/commit).
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="columnName">Column name</param>
    /// <param name="newValue">New value</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> UpdateCellAsync(string rowId, string columnName, object? newValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a cell is currently being edited.
    /// </summary>
    /// <returns>True if editing is in progress</returns>
    bool IsEditing();

    /// <summary>
    /// Gets current edit cell position.
    /// </summary>
    /// <returns>Current edit position or null if not editing</returns>
    PublicCellPosition? GetCurrentEditPosition();

    /// <summary>
    /// Enables or disables editing for the entire grid.
    /// </summary>
    /// <param name="enabled">True to enable editing</param>
    /// <returns>Result of the operation</returns>
    PublicResult SetEditingEnabled(bool enabled);

    /// <summary>
    /// Checks if editing is enabled.
    /// </summary>
    /// <returns>True if editing is enabled</returns>
    bool IsEditingEnabled();

    /// <summary>
    /// ✅ NEW: Preview validation during live cell editing (keystroke validation).
    /// PREVIEW MODE: Does NOT write to validation storage - only returns result for UI preview.
    /// PERFORMANCE: Fast, no DB writes (critical for SQLite mode with 300ms debounce).
    /// USE CASE: TextBox.TextChanged event → validate immediately → show red border + alert message.
    /// COMMIT: When user presses Enter, UpdateCellAsync writes to storage and commits validation permanently.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="columnName">Column name being edited</param>
    /// <param name="currentValue">Current value in TextBox (not yet committed)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview validation result (NOT written to storage)</returns>
    Task<PreviewValidationResult> PreviewValidateCellAsync(string rowId, string columnName, object? currentValue, CancellationToken cancellationToken = default);
}

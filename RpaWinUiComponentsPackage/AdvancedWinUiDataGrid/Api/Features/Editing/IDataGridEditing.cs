
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Editing;

/// <summary>
/// Public interface for DataGrid editing operations.
/// Provides cell editing functionality with validation and change tracking.
/// </summary>
public interface IDataGridEditing
{
    /// <summary>
    /// Begins editing a cell.
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="columnName">Column name</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> BeginEditAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default);

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
    /// Updates a cell value directly (without begin/commit).
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="columnName">Column name</param>
    /// <param name="newValue">New value</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> UpdateCellAsync(int rowIndex, string columnName, object? newValue, CancellationToken cancellationToken = default);

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
}

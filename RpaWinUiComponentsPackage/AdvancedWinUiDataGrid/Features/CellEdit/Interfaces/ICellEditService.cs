using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CellEdit.Interfaces;

/// <summary>
/// Service interface for cell editing operations with real-time validation
/// Implements Scoped lifetime per DI_DECISIONS.md - per-operation state isolation
/// BREAKING CHANGE v3.0: All methods now use rowId instead of rowIndex for stable row identification.
/// </summary>
internal interface ICellEditService
{
    /// <summary>
    /// Begins an edit session for a specific cell by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="columnName">Column name to edit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Edit result with session information</returns>
    Task<EditResult> BeginEditAsync(string rowId, string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the value of a cell being edited by stable row ID (with real-time validation).
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="columnName">Column name</param>
    /// <param name="newValue">New value for the cell</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Edit result with validation information</returns>
    Task<EditResult> UpdateCellAsync(string rowId, string columnName, object? newValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current edit session
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Edit result indicating success or failure</returns>
    Task<EditResult> CommitEditAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the current edit session and reverts to original value
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Edit result indicating success or failure</returns>
    Task<EditResult> CancelEditAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active edit session
    /// </summary>
    /// <returns>Active edit session or null if none</returns>
    EditSession? GetCurrentEditSession();

    /// <summary>
    /// Checks if there is an active edit session
    /// </summary>
    /// <returns>True if there is an active edit session</returns>
    bool HasActiveEditSession();

    /// <summary>
    /// Checks if currently editing (alias for HasActiveEditSession)
    /// </summary>
    bool IsEditing();

    /// <summary>
    /// Gets the current edit position (rowId and column)
    /// STABLE: Returns rowId which persists across sort/filter/delete operations.
    /// </summary>
    (string rowId, string columnName)? GetCurrentEditPosition();

    /// <summary>
    /// Sets whether editing is enabled globally
    /// </summary>
    void SetEditingEnabled(bool enabled);

    /// <summary>
    /// Checks if editing is enabled globally
    /// </summary>
    bool IsEditingEnabled();
}

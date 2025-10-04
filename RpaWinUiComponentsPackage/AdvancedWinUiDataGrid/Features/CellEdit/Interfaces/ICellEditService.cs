using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CellEdit.Interfaces;

/// <summary>
/// Service interface for cell editing operations with real-time validation
/// Implements Scoped lifetime per DI_DECISIONS.md - per-operation state isolation
/// </summary>
internal interface ICellEditService
{
    /// <summary>
    /// Begins an edit session for a specific cell
    /// </summary>
    /// <param name="rowIndex">Row index to edit</param>
    /// <param name="columnName">Column name to edit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Edit result with session information</returns>
    Task<EditResult> BeginEditAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the value of a cell being edited (with real-time validation)
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="columnName">Column name</param>
    /// <param name="newValue">New value for the cell</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Edit result with validation information</returns>
    Task<EditResult> UpdateCellAsync(int rowIndex, string columnName, object? newValue, CancellationToken cancellationToken = default);

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
}

using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Commands;

/// <summary>
/// Command for deleting rows by their stable row IDs
/// NEW ARCHITECTURE: Uses rowId (ULID) instead of row indices for stability
/// </summary>
internal sealed class DeleteRowsByIdCommand
{
    /// <summary>
    /// Row IDs to delete (stable ULID identifiers)
    /// </summary>
    public IReadOnlyList<string> RowIdsToDelete { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Current total row count before deletion
    /// </summary>
    public int CurrentRowCount { get; init; }

    /// <summary>
    /// Creates a new DeleteRowsByIdCommand
    /// </summary>
    public static DeleteRowsByIdCommand Create(
        IEnumerable<string> rowIdsToDelete,
        int currentRowCount)
    {
        return new DeleteRowsByIdCommand
        {
            RowIdsToDelete = rowIdsToDelete.ToList(),
            CurrentRowCount = currentRowCount
        };
    }
}

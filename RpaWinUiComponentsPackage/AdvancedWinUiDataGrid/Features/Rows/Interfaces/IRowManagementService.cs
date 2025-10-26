using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Interfaces;

/// <summary>
/// Internal service interface for row management operations
/// NEW ARCHITECTURE: Works with data shifting via RemoveRowsAsync (no physical row object deletion)
/// </summary>
internal interface IRowManagementService
{
    /// <summary>
    /// Delete rows by their stable row IDs
    /// Uses RemoveRowsAsync internally which removes ULIDs from storage
    /// This automatically "shifts" data as remaining ULIDs maintain lexicographic order
    /// </summary>
    Task<RowManagementResult> DeleteRowsByIdAsync(
        DeleteRowsByIdCommand command,
        CancellationToken cancellationToken = default);
}

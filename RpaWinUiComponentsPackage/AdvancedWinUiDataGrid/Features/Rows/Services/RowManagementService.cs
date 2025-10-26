using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Services;

/// <summary>
/// Service for managing row operations (delete, insert, update)
/// NEW ARCHITECTURE: Works with data shifting via IRowStore.RemoveRowsAsync
/// - RemoveRowsAsync physically removes ULIDs from storage dictionary
/// - Remaining ULIDs automatically maintain correct lexicographic order
/// - Works identically for InMemoryRowStore and HybridRowStore
/// </summary>
internal sealed class RowManagementService : IRowManagementService
{
    private readonly ILogger<RowManagementService> _logger;
    private readonly IRowStore _rowStore;

    public RowManagementService(
        ILogger<RowManagementService> logger,
        IRowStore rowStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
    }

    /// <summary>
    /// Delete rows by ID - NEW ARCHITECTURE
    /// Calls IRowStore.RemoveRowsAsync which:
    /// - Physically removes ULIDs from storage (InMemoryRowStore: ConcurrentDictionary.TryRemove)
    /// - Remaining ULIDs maintain lexicographic sort order automatically
    /// - "Data shifting" happens naturally as ULIDs are lexicographically sorted
    /// </summary>
    public async Task<RowManagementResult> DeleteRowsByIdAsync(
        DeleteRowsByIdCommand command,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        _logger.LogInformation(
            "Starting delete rows by ID operation {OperationId}: rowIdsToDelete={Count}",
            operationId, command.RowIdsToDelete.Count);

        try
        {
            if (command.RowIdsToDelete.Count == 0)
            {
                _logger.LogWarning("No row IDs provided - operation skipped");
                stopwatch.Stop();
                return RowManagementResult.CreateSuccess(
                    command.CurrentRowCount,
                    0,
                    RowOperationType.Delete,
                    stopwatch.Elapsed,
                    new RowManagementStatistics());
            }

            var initialRowCount = await _rowStore.GetRowCountAsync(cancellationToken);

            // Call IRowStore.RemoveRowsAsync - works for both InMemoryRowStore and HybridRowStore
            // This physically removes ULIDs from storage, achieving "data shift" effect
            await _rowStore.RemoveRowsAsync(command.RowIdsToDelete, cancellationToken);

            var finalRowCount = await _rowStore.GetRowCountAsync(cancellationToken);
            var actualDeleted = (int)(initialRowCount - finalRowCount);

            _logger.LogInformation(
                "Delete rows by ID operation {OperationId} completed: {Count} rows deleted, final count={Final}",
                operationId, actualDeleted, finalRowCount);

            stopwatch.Stop();
            return RowManagementResult.CreateSuccess(
                (int)finalRowCount,
                actualDeleted,
                RowOperationType.Delete,
                stopwatch.Elapsed,
                new RowManagementStatistics
                {
                    RowsDataCleared = actualDeleted,
                    RowsDataShifted = 0, // Not applicable - ULIDs maintain order automatically
                    EmptyRowsCreated = 0  // Not applicable - NEW ARCHITECTURE uses variable row count
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete rows by ID operation {OperationId} failed: {Message}",
                operationId, ex.Message);
            stopwatch.Stop();
            return RowManagementResult.CreateFailure(
                RowOperationType.Delete,
                new[] { $"Delete by ID failed: {ex.Message}" },
                stopwatch.Elapsed);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Services;

/// <summary>
/// Internal implementation of smart row add/delete operations
/// Thread-safe with logging support and minimum rows enforcement
/// </summary>
internal sealed class SmartOperationService : ISmartOperationService
{
    private readonly ILogger<SmartOperationService> _logger;
    private readonly IOperationLogger<SmartOperationService> _operationLogger;
    private readonly IRowStore _rowStore;
    private RowManagementStatistics _statistics = new();

    public SmartOperationService(
        ILogger<SmartOperationService> logger,
        IRowStore rowStore,
        IOperationLogger<SmartOperationService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _operationLogger = operationLogger ?? NullOperationLogger<SmartOperationService>.Instance;
    }

    public async Task<RowManagementResult> SmartAddRowsAsync(SmartAddRowsInternalCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("SmartAddRowsAsync", new
        {
            OperationId = operationId,
            DataCount = command.DataToAdd.Count(),
            MinimumRows = command.Configuration.MinimumRows
        });

        _logger.LogInformation("Starting smart add rows operation {OperationId}: dataToAdd={Count}, minRows={MinRows}",
            operationId, command.DataToAdd.Count(), command.Configuration.MinimumRows);

        try
        {
            var dataList = command.DataToAdd.ToList();
            var currentRows = await _rowStore.GetRowCountAsync(cancellationToken);

            // SMART ADD LOGIC:
            // If adding data >= minimumRows: add all data + 1 empty row
            // If adding data < minimumRows: add data + fill to minimumRows + 1 empty row

            var minRows = command.Configuration.MinimumRows;
            var totalDataRows = dataList.Count;
            var emptyRowsToAdd = 0;

            if (totalDataRows >= minRows)
            {
                // Scenario: import >= minimumRows → all data + 1 empty
                emptyRowsToAdd = 1;
            }
            else
            {
                // Scenario: import < minimumRows → data + fill to minRows + 1 empty
                emptyRowsToAdd = (minRows - totalDataRows) + 1;
            }

            // Add data rows
            var allRows = dataList.ToList();

            // Add empty rows
            var emptyRow = CreateEmptyRow(dataList.FirstOrDefault());
            for (int i = 0; i < emptyRowsToAdd; i++)
            {
                allRows.Add(emptyRow);
            }

            await _rowStore.AppendRowsAsync(allRows, cancellationToken);

            var finalRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
            stopwatch.Stop();

            var statistics = new RowManagementStatistics
            {
                EmptyRowsCreated = emptyRowsToAdd,
                MinimumRowsEnforced = totalDataRows < minRows,
                LastEmptyRowMaintained = command.Configuration.AlwaysKeepLastEmpty
            };

            _statistics = statistics;

            _logger.LogInformation("Smart add rows operation {OperationId} completed in {Duration}ms: added {DataRows} data rows + {EmptyRows} empty rows = {FinalCount} total",
                operationId, stopwatch.ElapsedMilliseconds, totalDataRows, emptyRowsToAdd, finalRowCount);

            scope.MarkSuccess(new { Duration = stopwatch.Elapsed, FinalRowCount = finalRowCount });

            return RowManagementResult.CreateSuccess(
                finalRowCount,
                totalDataRows,
                RowOperationType.Add,
                stopwatch.Elapsed,
                statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart add rows operation {OperationId} failed: {Message}", operationId, ex.Message);
            scope.MarkFailure(ex);
            return RowManagementResult.CreateFailure(
                RowOperationType.Add,
                new[] { $"Smart add failed: {ex.Message}" },
                stopwatch.Elapsed);
        }
    }

    public async Task<RowManagementResult> SmartDeleteRowsAsync(SmartDeleteRowsInternalCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("SmartDeleteRowsAsync", new
        {
            OperationId = operationId,
            RowsToDelete = command.RowIndexesToDelete.Count,
            CurrentRowCount = command.CurrentRowCount,
            MinimumRows = command.Configuration.MinimumRows
        });

        _logger.LogInformation("Starting smart delete rows operation {OperationId}: rowsToDelete={Count}, currentRows={CurrentRows}, minRows={MinRows}",
            operationId, command.RowIndexesToDelete.Count, command.CurrentRowCount, command.Configuration.MinimumRows);

        try
        {
            var minRows = command.Configuration.MinimumRows;
            var currentRows = command.CurrentRowCount;
            var rowsPhysicallyDeleted = 0;
            var rowsContentCleared = 0;
            var rowsShifted = 0;

            // SMART DELETE LOGIC:
            // A) If currentRows <= minimumRows: clear content only + shift rows up
            // B) If currentRows > minimumRows: physical delete (but keep 1 empty at end)

            if (currentRows <= minRows || !command.Configuration.EnableSmartDelete)
            {
                // Scenario A: Clear content + shift up
                var allRows = (await _rowStore.GetAllRowsAsync(cancellationToken)).ToList();
                var rowsToModify = allRows.ToList();

                foreach (var rowIndex in command.RowIndexesToDelete.OrderByDescending(i => i))
                {
                    if (rowIndex < rowsToModify.Count)
                    {
                        // Clear content of this row and shift
                        rowsContentCleared++;

                        // Remove the row and add empty at end
                        rowsToModify.RemoveAt(rowIndex);
                        var emptyRow = CreateEmptyRow(allRows.FirstOrDefault());
                        rowsToModify.Add(emptyRow);
                        rowsShifted++;
                    }
                }

                await _rowStore.ReplaceAllRowsAsync(rowsToModify, cancellationToken);
            }
            else
            {
                // Scenario B: Physical delete
                var allRows = (await _rowStore.GetAllRowsAsync(cancellationToken)).ToList();
                var rowsToKeep = allRows.ToList();

                foreach (var rowIndex in command.RowIndexesToDelete.OrderByDescending(i => i))
                {
                    if (rowIndex < rowsToKeep.Count)
                    {
                        rowsToKeep.RemoveAt(rowIndex);
                        rowsPhysicallyDeleted++;
                    }
                }

                // Ensure at least 1 empty row at end
                var lastRow = rowsToKeep.LastOrDefault();
                if (lastRow != null && !IsEmptyRow(lastRow))
                {
                    var emptyRow = CreateEmptyRow(lastRow);
                    rowsToKeep.Add(emptyRow);
                }

                await _rowStore.ReplaceAllRowsAsync(rowsToKeep, cancellationToken);
            }

            var finalRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
            stopwatch.Stop();

            var statistics = new RowManagementStatistics
            {
                RowsPhysicallyDeleted = rowsPhysicallyDeleted,
                RowsContentCleared = rowsContentCleared,
                RowsShifted = rowsShifted,
                MinimumRowsEnforced = currentRows <= minRows,
                LastEmptyRowMaintained = command.Configuration.AlwaysKeepLastEmpty
            };

            _statistics = statistics;

            _logger.LogInformation("Smart delete rows operation {OperationId} completed in {Duration}ms: physicallyDeleted={Physical}, contentCleared={Cleared}, shifted={Shifted}, finalCount={FinalCount}",
                operationId, stopwatch.ElapsedMilliseconds, rowsPhysicallyDeleted, rowsContentCleared, rowsShifted, finalRowCount);

            scope.MarkSuccess(new { Duration = stopwatch.Elapsed, FinalRowCount = finalRowCount });

            return RowManagementResult.CreateSuccess(
                finalRowCount,
                command.RowIndexesToDelete.Count,
                RowOperationType.SmartDelete,
                stopwatch.Elapsed,
                statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart delete rows operation {OperationId} failed: {Message}", operationId, ex.Message);
            scope.MarkFailure(ex);
            return RowManagementResult.CreateFailure(
                RowOperationType.SmartDelete,
                new[] { $"Smart delete failed: {ex.Message}" },
                stopwatch.Elapsed);
        }
    }

    public async Task<RowManagementResult> AutoExpandEmptyRowAsync(AutoExpandEmptyRowInternalCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("AutoExpandEmptyRowAsync", new
        {
            OperationId = operationId,
            CurrentRowCount = command.CurrentRowCount
        });

        _logger.LogInformation("Starting auto-expand empty row operation {OperationId}: currentRows={CurrentRows}",
            operationId, command.CurrentRowCount);

        try
        {
            if (!command.Configuration.EnableAutoExpand || !command.TriggerExpansion)
            {
                _logger.LogInformation("Auto-expand skipped: enableAutoExpand={Enabled}, trigger={Trigger}",
                    command.Configuration.EnableAutoExpand, command.TriggerExpansion);
                stopwatch.Stop();
                return RowManagementResult.CreateSuccess(command.CurrentRowCount, 0, RowOperationType.AutoExpand, stopwatch.Elapsed);
            }

            // Check if last row is empty
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
            var lastRow = allRows.LastOrDefault();

            if (lastRow != null && !IsEmptyRow(lastRow))
            {
                // Add new empty row
                var emptyRow = CreateEmptyRow(lastRow);
                await _rowStore.AppendRowsAsync(new[] { emptyRow }, cancellationToken);

                var finalRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
                stopwatch.Stop();

                var statistics = new RowManagementStatistics
                {
                    EmptyRowsCreated = 1,
                    LastEmptyRowMaintained = true
                };

                _logger.LogInformation("Auto-expand empty row operation {OperationId} completed in {Duration}ms: added 1 empty row, finalCount={FinalCount}",
                    operationId, stopwatch.ElapsedMilliseconds, finalRowCount);

                scope.MarkSuccess(new { Duration = stopwatch.Elapsed, EmptyRowAdded = true });

                return RowManagementResult.CreateSuccess(
                    finalRowCount,
                    1,
                    RowOperationType.AutoExpand,
                    stopwatch.Elapsed,
                    statistics);
            }

            stopwatch.Stop();
            _logger.LogInformation("Auto-expand skipped: last row is already empty");
            scope.MarkSuccess(new { Duration = stopwatch.Elapsed, EmptyRowAdded = false });

            return RowManagementResult.CreateSuccess(command.CurrentRowCount, 0, RowOperationType.AutoExpand, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-expand empty row operation {OperationId} failed: {Message}", operationId, ex.Message);
            scope.MarkFailure(ex);
            return RowManagementResult.CreateFailure(
                RowOperationType.AutoExpand,
                new[] { $"Auto-expand failed: {ex.Message}" },
                stopwatch.Elapsed);
        }
    }

    public async Task<Result> ValidateRowManagementConfigurationAsync(RowManagementConfiguration configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating row management configuration: minRows={MinRows}", configuration.MinimumRows);

            if (configuration.MinimumRows < 1)
            {
                return Result.Failure("MinimumRows must be at least 1");
            }

            if (configuration.MinimumRows > 1000)
            {
                return Result.Failure("MinimumRows cannot exceed 1000");
            }

            _logger.LogInformation("Row management configuration validation passed");
            return await Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Row management configuration validation failed: {Message}", ex.Message);
            return Result.Failure($"Validation error: {ex.Message}");
        }
    }

    public RowManagementStatistics GetRowManagementStatistics()
    {
        return _statistics;
    }

    #region Private Helper Methods

    private IReadOnlyDictionary<string, object?> CreateEmptyRow(IReadOnlyDictionary<string, object?>? templateRow)
    {
        if (templateRow == null)
            return new Dictionary<string, object?>();

        var emptyRow = new Dictionary<string, object?>();
        foreach (var key in templateRow.Keys)
        {
            emptyRow[key] = null;
        }
        return emptyRow;
    }

    private bool IsEmptyRow(IReadOnlyDictionary<string, object?> row)
    {
        return row.Values.All(v => v == null || string.IsNullOrWhiteSpace(v?.ToString()));
    }

    #endregion
}

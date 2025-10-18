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
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Services;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Services;

/// <summary>
/// Internal implementation of smart row add/delete operations
/// Thread-safe with logging support and minimum rows enforcement
/// PERFORMANCE: Uses debounced validation instead of synchronous blocking validation
/// </summary>
internal sealed class SmartOperationService : ISmartOperationService, IDisposable
{
    private readonly ILogger<SmartOperationService> _logger;
    private readonly IOperationLogger<SmartOperationService> _operationLogger;
    private readonly IRowStore _rowStore;
    private readonly IValidationService _validationService;
    private readonly DebouncedValidationService? _debouncedValidation;
    private RowManagementStatistics _statistics = new();

    // CRITICAL: Semaphore to prevent concurrent delete operations (fixes race condition)
    private readonly SemaphoreSlim _deleteOperationLock = new(1, 1);

    public SmartOperationService(
        ILogger<SmartOperationService> logger,
        IRowStore rowStore,
        IValidationService validationService,
        IOperationLogger<SmartOperationService>? operationLogger = null,
        DebouncedValidationService? debouncedValidation = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _operationLogger = operationLogger ?? NullOperationLogger<SmartOperationService>.Instance;
        _debouncedValidation = debouncedValidation; // Optional - for performance optimization
    }

    public async Task<RowManagementResult> SmartAddRowsAsync(SmartAddRowsInternalCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("SmartAddRowsAsync", new
        {
            OperationId = operationId,
            DataCount = command.DataToAdd.Count()
        });

        _logger.LogInformation("Starting smart add rows operation {OperationId}: dataToAdd={Count}",
            operationId, command.DataToAdd.Count());

        try
        {
            var dataList = command.DataToAdd.ToList();
            var currentRows = await _rowStore.GetRowCountAsync(cancellationToken);
            var totalDataRows = dataList.Count;

            _logger.LogInformation("Adding {Count} data rows to store (currentRows={Current})",
                totalDataRows, currentRows);

            // STEP 1: Add all data rows (WITHOUT empty rows - 3-step cleanup handles that)
            await _rowStore.AppendRowsAsync(dataList, cancellationToken);

            _logger.LogInformation("Data rows appended, applying 3-step cleanup...");

            // STEP 2: Apply 3-step cleanup (remove empty from middle, ensure min, ensure last empty)
            var templateRow = dataList.FirstOrDefault();
            var countBeforeCleanup = (int)await _rowStore.GetRowCountAsync(cancellationToken);

            await EnsureMinRowsAndLastEmptyAsync(command.Configuration, templateRow, cancellationToken);

            var finalRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
            var emptyRowsCreated = Math.Max(0, finalRowCount - countBeforeCleanup);

            stopwatch.Stop();

            var statistics = new RowManagementStatistics
            {
                EmptyRowsCreated = emptyRowsCreated,
                LastEmptyRowMaintained = command.Configuration.AlwaysKeepLastEmpty
            };

            _statistics = statistics;

            _logger.LogInformation("Smart add rows operation {OperationId} completed in {Duration}ms: added {DataRows} data rows + {EmptyRows} empty rows = {FinalCount} total",
                operationId, stopwatch.ElapsedMilliseconds, totalDataRows, emptyRowsCreated, finalRowCount);

            // CRITICAL: Automatic post-operation validation (only if ShouldRunAutomaticValidation returns true)
            if (_validationService.ShouldRunAutomaticValidation("SmartAddRowsAsync"))
            {
                _logger.LogInformation("Starting automatic post-SmartAdd batch validation for operation {OperationId}", operationId);

                var postAddValidation = await _validationService.AreAllNonEmptyRowsValidAsync(false, false, cancellationToken);
                if (!postAddValidation.IsSuccess)
                {
                    _logger.LogWarning("Post-SmartAdd validation found issues for operation {OperationId}: {Error}",
                        operationId, postAddValidation.ErrorMessage);
                    scope.MarkWarning($"Post-SmartAdd validation found issues: {postAddValidation.ErrorMessage}");
                }
                else
                {
                    _logger.LogInformation("Post-SmartAdd validation successful for operation {OperationId}", operationId);
                }
            }
            else
            {
                _logger.LogInformation("Automatic post-SmartAdd validation skipped for operation {OperationId} " +
                    "(ValidationAutomationMode or EnableBatchValidation is disabled)", operationId);
            }

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
        // CRITICAL: Acquire lock to prevent concurrent delete operations (fixes race condition)
        // Race condition: Multiple deletes can execute concurrently, causing one delete to see currentRows=0
        // because another delete called GetAllRowsAsync()->Clear()->ReplaceAllRowsAsync() in between
        await _deleteOperationLock.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid();

            using var scope = _operationLogger.LogOperationStart("SmartDeleteRowsAsync", new
            {
                OperationId = operationId,
                RowsToDelete = command.RowIndexesToDelete.Count,
                CurrentRowCount = command.CurrentRowCount
            });

            _logger.LogInformation("Starting smart delete rows operation {OperationId}: rowsToDelete={Count}, currentRows={CurrentRows}",
                operationId, command.RowIndexesToDelete.Count, command.CurrentRowCount);

            try
        {
            var currentRows = command.CurrentRowCount;
            var rowsPhysicallyDeleted = 0;
            var rowsContentCleared = 0;
            var rowsShifted = 0;

            // PERFORMANCE OPTIMIZATION: Track affected indices for granular UI updates (10M+ row support)
            var physicallyDeletedList = new List<int>();
            var contentClearedList = new List<int>();
            var updatedRowDataDict = new Dictionary<int, IReadOnlyDictionary<string, object?>>();
            var affectedRowIndicesList = new List<int>();

            // SMART DELETE LOGIC:
            // A) If currentRows <= minimumRows: clear content only + shift rows up
            // B) If currentRows > minimumRows AND EnableSmartDelete: physical delete (but keep 1 empty at end)
            // C) If EnableSmartDelete disabled: clear content instead of physical delete
            // Physical delete + 2-step cleanup
            // All deletes now use same logic - physical delete followed by 2-step cleanup

            if (command.Configuration.EnableSmartDelete)
            {
                // SCENARIO: PHYSICAL DELETE + 2-STEP CLEANUP
                _logger.LogDebug("SmartDelete: Physical delete + 2-step cleanup");

                var allRows = (await _rowStore.GetAllRowsAsync(cancellationToken)).ToList();
                var sortedIndicesToDelete = command.RowIndexesToDelete.OrderByDescending(i => i).ToList();

                // STEP: Physical delete rows by removing from list
                foreach (var rowIndex in sortedIndicesToDelete)
                {
                    if (rowIndex < allRows.Count)
                    {
                        physicallyDeletedList.Add(rowIndex);
                        affectedRowIndicesList.Add(rowIndex);
                        rowsPhysicallyDeleted++;
                    }
                }

                // Remove rows (build new list without deleted rows)
                var rowsToKeep = new List<IReadOnlyDictionary<string, object?>>();
                for (int i = 0; i < allRows.Count; i++)
                {
                    if (!sortedIndicesToDelete.Contains(i))
                    {
                        rowsToKeep.Add(allRows[i]);
                    }
                }

                _logger.LogInformation("Physically deleted {Count} rows by index in Scenario A", rowsPhysicallyDeleted);

                // Replace with remaining rows
                await _rowStore.ReplaceAllRowsAsync(rowsToKeep, cancellationToken);

                // STEP: 3-step cleanup (remove empty from middle, ensure min, ensure last empty)
                var templateRow = allRows.FirstOrDefault();
                var countBeforeCleanup = (int)await _rowStore.GetRowCountAsync(cancellationToken);

                await EnsureMinRowsAndLastEmptyAsync(command.Configuration, templateRow, cancellationToken);

                var countAfterCleanup = (int)await _rowStore.GetRowCountAsync(cancellationToken);
                var emptyRowsCreated = Math.Max(0, countAfterCleanup - countBeforeCleanup);

                _logger.LogDebug("Scenario A metadata: PhysicallyDeleted={Deleted}, EmptyRowsCreated={Empty}",
                    rowsPhysicallyDeleted, emptyRowsCreated);
            }
            else
            {
                // Scenario: SmartDelete disabled - clear content instead of physical delete
                _logger.LogDebug("SmartDelete: Clear content only (EnableSmartDelete=false)");

                var allRows = (await _rowStore.GetAllRowsAsync(cancellationToken)).ToList();
                var rowsToModify = allRows.ToList();

                // Sort descending to avoid index shifting issues during iteration
                var sortedIndicesToDelete = command.RowIndexesToDelete.OrderByDescending(i => i).ToList();

                // CRITICAL FIX: Remove rows first WITHOUT adding empty rows in the loop
                foreach (var rowIndex in sortedIndicesToDelete)
                {
                    if (rowIndex < rowsToModify.Count)
                    {
                        // Track content cleared
                        contentClearedList.Add(rowIndex);
                        affectedRowIndicesList.Add(rowIndex);
                        rowsContentCleared++;

                        // Remove the row (DON'T add empty row here!)
                        rowsToModify.RemoveAt(rowIndex);
                        rowsShifted++;
                    }
                }

                // CRITICAL FIX: After removing, add back empty rows to maintain current count
                // (since EnableSmartDelete=false means we clear content, not physically delete)
                var rowsRemoved = sortedIndicesToDelete.Count(i => i < allRows.Count);
                var templateRow = allRows.FirstOrDefault() ?? rowsToModify.FirstOrDefault();

                for (int i = 0; i < rowsRemoved; i++)
                {
                    var emptyRow = CreateEmptyRow(templateRow);
                    rowsToModify.Add(emptyRow);
                }

                _logger.LogDebug("Added {Count} empty rows to maintain original count (clear content mode)", rowsRemoved);

                // Track shifted rows for granular UI update
                for (int shiftedIdx = 0; shiftedIdx < rowsToModify.Count; shiftedIdx++)
                {
                    updatedRowDataDict[shiftedIdx] = rowsToModify[shiftedIdx];
                }

                _logger.LogDebug("Scenario C metadata: ContentCleared={Cleared}, EmptyRowsAdded={Empty}, UpdatedRows={Updated}",
                    contentClearedList.Count, rowsRemoved, updatedRowDataDict.Count);

                await _rowStore.ReplaceAllRowsAsync(rowsToModify, cancellationToken);
            }

            var finalRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
            stopwatch.Stop();

            var statistics = new RowManagementStatistics
            {
                RowsPhysicallyDeleted = rowsPhysicallyDeleted,
                RowsContentCleared = rowsContentCleared,
                RowsShifted = rowsShifted,
                LastEmptyRowMaintained = command.Configuration.AlwaysKeepLastEmpty
            };

            _statistics = statistics;

            _logger.LogInformation("Smart delete rows operation {OperationId} completed in {Duration}ms: physicallyDeleted={Physical}, contentCleared={Cleared}, shifted={Shifted}, finalCount={FinalCount}",
                operationId, stopwatch.ElapsedMilliseconds, rowsPhysicallyDeleted, rowsContentCleared, rowsShifted, finalRowCount);

            // PERFORMANCE: Debounced async validation (non-blocking) instead of synchronous blocking validation
            // This changes 100 deletes × 100ms blocking = 10s → 500ms debounced async = instant UI
            if (!command.SkipAutomaticValidation && _validationService.ShouldRunAutomaticValidation("SmartDeleteRowsAsync"))
            {
                if (_debouncedValidation != null)
                {
                    // Use debounced validation (non-blocking, fires after 500ms delay)
                    _debouncedValidation.ScheduleValidation("SmartDeleteRowsAsync", delayMs: 500);
                    _logger.LogInformation("Scheduled debounced validation for operation {OperationId} (non-blocking)", operationId);
                }
                else
                {
                    // Fallback to synchronous validation if DebouncedValidationService not available
                    _logger.LogWarning("DebouncedValidationService not available - using synchronous validation (SLOW)");

                    var postDeleteValidation = await _validationService.AreAllNonEmptyRowsValidAsync(false, false, cancellationToken);
                    if (!postDeleteValidation.IsSuccess)
                    {
                        _logger.LogWarning("Post-SmartDelete validation found issues for operation {OperationId}: {Error}",
                            operationId, postDeleteValidation.ErrorMessage);
                        scope.MarkWarning($"Post-SmartDelete validation found issues: {postDeleteValidation.ErrorMessage}");
                    }
                }
            }
            else
            {
                _logger.LogInformation("Automatic post-SmartDelete validation skipped for operation {OperationId} " +
                    "(SkipAutomaticValidation={Skip}, ShouldRun={ShouldRun})",
                    operationId, command.SkipAutomaticValidation,
                    _validationService.ShouldRunAutomaticValidation("SmartDeleteRowsAsync"));
            }

            scope.MarkSuccess(new { Duration = stopwatch.Elapsed, FinalRowCount = finalRowCount });

            // Create result with granular metadata for 10M+ row performance
            var result = RowManagementResult.CreateSuccess(
                finalRowCount,
                command.RowIndexesToDelete.Count,
                RowOperationType.SmartDelete,
                stopwatch.Elapsed,
                statistics);

            // Add granular update metadata (using record 'with' expression for immutability)
            return result with
            {
                PhysicallyDeletedIndices = physicallyDeletedList.AsReadOnly(),
                ContentClearedIndices = contentClearedList.AsReadOnly(),
                UpdatedRowData = updatedRowDataDict,
                AffectedRowIndices = affectedRowIndicesList.Distinct().OrderBy(i => i).ToList().AsReadOnly()
            };
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
        finally
        {
            // CRITICAL: Always release the lock, even if operation fails
            _deleteOperationLock.Release();
        }
    }

    public async Task<RowManagementResult> SmartDeleteRowsByIdAsync(SmartDeleteRowsByIdInternalCommand command, CancellationToken cancellationToken = default)
    {
        // CRITICAL: Acquire lock to prevent concurrent delete operations
        await _deleteOperationLock.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid();

            using var scope = _operationLogger.LogOperationStart("SmartDeleteRowsByIdAsync", new
            {
                OperationId = operationId,
                RowsToDelete = command.RowIdsToDelete.Count,
                CurrentRowCount = command.CurrentRowCount
            });

            _logger.LogInformation("Starting smart delete rows by ID operation {OperationId}: rowIdsToDelete={Count}, currentRows={CurrentRows}",
                operationId, command.RowIdsToDelete.Count, command.CurrentRowCount);

            try
            {
                var currentRows = command.CurrentRowCount;

                // CRITICAL: Get indices BEFORE delete for metadata tracking
                var allRowsBeforeDelete = (await _rowStore.GetAllRowsAsync(cancellationToken)).ToList();
                var deletedIndices = new List<int>();

                for (int i = 0; i < allRowsBeforeDelete.Count; i++)
                {
                    if (allRowsBeforeDelete[i].TryGetValue("__rowId", out var rowIdValue))
                    {
                        var rowId = rowIdValue?.ToString();
                        if (rowId != null && command.RowIdsToDelete.Contains(rowId))
                        {
                            deletedIndices.Add(i);
                        }
                    }
                }

                _logger.LogDebug("Mapped {Count} rowIds to indices: {Indices}",
                    deletedIndices.Count, string.Join(", ", deletedIndices));

                // CRITICAL FIX: Use ACTUAL deleted count (not command count which may include non-existent IDs)
                var actualDeletedCount = deletedIndices.Count;

                // CRITICAL FIX: Early exit if no rows found - prevent adding empty rows for non-existent deletes
                if (actualDeletedCount == 0)
                {
                    _logger.LogWarning("No rows found with provided IDs - operation skipped (no changes made)");
                    stopwatch.Stop();
                    scope.MarkSuccess(new { Duration = stopwatch.Elapsed, RowsDeleted = 0, Message = "No matching rows found" });

                    return RowManagementResult.CreateSuccess(
                        currentRows,
                        0,
                        RowOperationType.SmartDelete,
                        stopwatch.Elapsed,
                        new RowManagementStatistics());
                }

                // CRITICAL FIX: Calculate newRowCount using ACTUAL deleted count
                var newRowCount = currentRows - actualDeletedCount;
                var rowsPhysicallyDeleted = 0;
                var rowsContentCleared = 0;
                var emptyRowsCreated = 0;
                var contentClearedList = new List<int>();
                var updatedRowDataDict = new Dictionary<int, IReadOnlyDictionary<string, object?>>();

                // Physical delete + 2-step cleanup
                {
                    // PHYSICAL DELETE + 2-STEP CLEANUP
                    _logger.LogDebug("SmartDeleteById: Physical delete + 2-step cleanup " +
                        "(newRowCount={New})", newRowCount);

                    // STEP: Physical delete rows by ID
                    await _rowStore.RemoveRowsAsync(command.RowIdsToDelete, cancellationToken);
                    rowsPhysicallyDeleted = actualDeletedCount;
                    _logger.LogInformation("Physically deleted {Count} rows by ID in operation {OperationId}",
                        actualDeletedCount, operationId);

                    // Track deleted indices for UI update
                    foreach (var index in deletedIndices)
                    {
                        contentClearedList.Add(index); // Track as content cleared for UI compatibility
                    }

                    // STEP: 3-step cleanup (remove empty from middle, ensure min, ensure last empty)
                    var templateRow = allRowsBeforeDelete.FirstOrDefault();
                    var countBeforeCleanup = (int)await _rowStore.GetRowCountAsync(cancellationToken);

                    await EnsureMinRowsAndLastEmptyAsync(command.Configuration, templateRow, cancellationToken);

                    var countAfterCleanup = (int)await _rowStore.GetRowCountAsync(cancellationToken);
                    emptyRowsCreated = Math.Max(0, countAfterCleanup - countBeforeCleanup);

                    // Track all updated rows for UI (full reload needed after cleanup)
                    var allRowsAfterCleanup = await _rowStore.GetAllRowsAsync(cancellationToken);
                    for (int i = 0; i < allRowsAfterCleanup.Count; i++)
                    {
                        updatedRowDataDict[i] = allRowsAfterCleanup[i];
                    }

                    _logger.LogDebug("Scenario A metadata: PhysicallyDeleted={Deleted}, EmptyRowsCreated={Empty}, UpdatedRows={Updated}",
                        rowsPhysicallyDeleted, emptyRowsCreated, updatedRowDataDict.Count);
                }

                var finalRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
                stopwatch.Stop();

                var statistics = new RowManagementStatistics
                {
                    RowsPhysicallyDeleted = rowsPhysicallyDeleted,
                    RowsContentCleared = rowsContentCleared,
                    RowsShifted = 0,
                    EmptyRowsCreated = emptyRowsCreated,
                    LastEmptyRowMaintained = command.Configuration.AlwaysKeepLastEmpty
                };

                _statistics = statistics;

                _logger.LogInformation("Smart delete rows by ID operation {OperationId} completed in {Duration}ms: physicallyDeleted={Physical}, contentCleared={Cleared}, emptyRowsCreated={Empty}, finalCount={FinalCount}",
                    operationId, stopwatch.ElapsedMilliseconds, rowsPhysicallyDeleted, rowsContentCleared, emptyRowsCreated, finalRowCount);

                // PERFORMANCE: Debounced async validation (non-blocking)
                if (!command.SkipAutomaticValidation && _validationService.ShouldRunAutomaticValidation("SmartDeleteRowsByIdAsync"))
                {
                    if (_debouncedValidation != null)
                    {
                        _debouncedValidation.ScheduleValidation("SmartDeleteRowsByIdAsync", delayMs: 500);
                        _logger.LogInformation("Scheduled debounced validation for operation {OperationId} (non-blocking)", operationId);
                    }
                    else
                    {
                        _logger.LogWarning("DebouncedValidationService not available - using synchronous validation (SLOW)");
                        var postDeleteValidation = await _validationService.AreAllNonEmptyRowsValidAsync(false, false, cancellationToken);
                        if (!postDeleteValidation.IsSuccess)
                        {
                            _logger.LogWarning("Post-SmartDeleteById validation found issues for operation {OperationId}: {Error}",
                                operationId, postDeleteValidation.ErrorMessage);
                            scope.MarkWarning($"Post-SmartDeleteById validation found issues: {postDeleteValidation.ErrorMessage}");
                        }
                    }
                }

                scope.MarkSuccess(new { Duration = stopwatch.Elapsed, FinalRowCount = finalRowCount });

                // CRITICAL: Return metadata for incremental UI update (10M+ row performance)
                var result = RowManagementResult.CreateSuccess(
                    finalRowCount,
                    actualDeletedCount,
                    RowOperationType.SmartDelete,
                    stopwatch.Elapsed,
                    statistics);

                // Add granular metadata
                return result with
                {
                    PhysicallyDeletedIndices = Array.Empty<int>(),
                    ContentClearedIndices = contentClearedList.AsReadOnly(),
                        UpdatedRowData = updatedRowDataDict,
                        AffectedRowIndices = contentClearedList.AsReadOnly()
                    };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Smart delete rows by ID operation {OperationId} failed: {Message}", operationId, ex.Message);
                scope.MarkFailure(ex);
                return RowManagementResult.CreateFailure(
                    RowOperationType.SmartDelete,
                    new[] { $"Smart delete by ID failed: {ex.Message}" },
                    stopwatch.Elapsed);
            }
        }
        finally
        {
            // CRITICAL: Always release the lock
            _deleteOperationLock.Release();
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
            _logger.LogInformation("Validating row management configuration");

            // No MinimumRows validation needed - removed from configuration

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

    public async Task<RowManagementResult> AutoFillAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> currentData,
        int startRowIndex,
        int endRowIndex,
        string columnName,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("AutoFillAsync", new
        {
            OperationId = operationId,
            StartRowIndex = startRowIndex,
            EndRowIndex = endRowIndex,
            ColumnName = columnName
        });

        _logger.LogInformation("Starting auto-fill operation {OperationId}: column={Column}, startRow={StartRow}, endRow={EndRow}",
            operationId, columnName, startRowIndex, endRowIndex);

        try
        {
            var dataList = currentData.ToList();

            if (startRowIndex < 0 || endRowIndex >= dataList.Count || startRowIndex >= endRowIndex)
            {
                var error = $"Invalid row range: start={startRowIndex}, end={endRowIndex}, dataCount={dataList.Count}";
                _logger.LogWarning("AutoFillAsync {OperationId} validation failed: {Error}", operationId, error);
                scope.MarkFailure(new ArgumentException(error));
                return RowManagementResult.CreateFailure(
                    RowOperationType.Clear,
                    new[] { error },
                    stopwatch.Elapsed);
            }

            // Check if column exists
            var firstRow = dataList.FirstOrDefault();
            if (firstRow == null || !firstRow.ContainsKey(columnName))
            {
                var error = $"Column '{columnName}' not found in data";
                _logger.LogWarning("AutoFillAsync {OperationId} validation failed: {Error}", operationId, error);
                scope.MarkFailure(new ArgumentException(error));
                return RowManagementResult.CreateFailure(
                    RowOperationType.Clear,
                    new[] { error },
                    stopwatch.Elapsed);
            }

            // TODO: Implement actual auto-fill logic when UI layer is connected
            // This would typically involve:
            // - Detecting pattern in source cells (numeric sequence, date sequence, text pattern)
            // - Applying pattern to target range
            // - Handling different data types (numbers, dates, strings)
            // - Smart increment/decrement detection
            // - Custom pattern support (e.g., "Item 1", "Item 2", "Item 3")

            var cellsAffected = endRowIndex - startRowIndex + 1;

            await Task.CompletedTask; // Placeholder for async implementation

            stopwatch.Stop();

            _logger.LogInformation("AutoFillAsync {OperationId} completed in {Duration}ms: filled {CellCount} cells in column '{Column}'",
                operationId, stopwatch.ElapsedMilliseconds, cellsAffected, columnName);

            scope.MarkSuccess(new { Duration = stopwatch.Elapsed, CellsAffected = cellsAffected });

            return RowManagementResult.CreateSuccess(
                dataList.Count,
                cellsAffected,
                RowOperationType.Clear,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoFillAsync {OperationId} failed: {Message}", operationId, ex.Message);
            scope.MarkFailure(ex);
            return RowManagementResult.CreateFailure(
                RowOperationType.Clear,
                new[] { $"Auto-fill failed: {ex.Message}" },
                stopwatch.Elapsed);
        }
    }

    #region Private Helper Methods

    private IReadOnlyDictionary<string, object?> CreateEmptyRow(IReadOnlyDictionary<string, object?>? templateRow)
    {
        if (templateRow == null)
            return new Dictionary<string, object?>();

        var emptyRow = new Dictionary<string, object?>();
        foreach (var key in templateRow.Keys)
        {
            // CRITICAL FIX: Skip __rowId field - let InMemoryRowStore assign new unique ID
            // Problem: If __rowId is set to null, subsequent deletes on this row will fail
            // because CellViewModel.RowId will be null → fallback to index-based delete → Scenario A (clear content)
            if (key == "__rowId")
            {
                continue; // Skip __rowId, InMemoryRowStore.AddRangeAsync will assign new ID
            }

            emptyRow[key] = null;
        }
        return emptyRow;
    }

    private bool IsEmptyRow(IReadOnlyDictionary<string, object?> row)
    {
        // CRITICAL: Ignore __rowId field - it's an identifier, not data
        // A row is empty if ALL data fields (excluding __rowId) are null or whitespace
        return row
            .Where(kvp => kvp.Key != "__rowId")
            .All(kvp => kvp.Value == null || string.IsNullOrWhiteSpace(kvp.Value?.ToString()));
    }

    /// <summary>
    /// UNIVERSAL 3-STEP CLEANUP: Remove empty rows from middle, ensure minRows, ensure last empty
    /// STEP 1: Remove ALL empty rows (they will be re-added at end in correct quantity)
    /// STEP 2: Ensure minRows (fill with empty rows at end)
    /// STEP 3: Ensure last row is empty (independent of minRows check)
    ///
    /// PERFORMANCE: O(n) streaming + O(k) delete + O(m) add = O(n) total
    /// Safe for 10M+ rows datasets using StreamRowsAsync
    ///
    /// Called after ALL operations that modify row count:
    /// - SmartDeleteRowsByIdAsync (both scenarios)
    /// - SmartDeleteRowsAsync (all scenarios)
    /// - SmartAddRowsAsync
    /// - ImportService
    /// - CopyPasteService
    /// </summary>
    public async Task EnsureMinRowsAndLastEmptyAsync(
        RowManagementConfiguration config,
        IReadOnlyDictionary<string, object?>? templateRow = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting 2-step row cleanup: (1) remove ALL empty rows, (2) ensure last empty");

        // STEP 1: Remove ALL empty rows (will be re-added at end if needed)
        // Use streaming to avoid loading all 10M rows to memory
        var emptyRowIds = new List<string>();
        var totalRowsScanned = 0;

        _logger.LogDebug("STEP 1: Scanning for empty rows to remove...");
        await foreach (var batch in _rowStore.StreamRowsAsync(
            onlyFiltered: false,
            onlyChecked: false,
            batchSize: 1000,
            cancellationToken))
        {
            foreach (var row in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totalRowsScanned++;

                if (IsEmptyRow(row) && row.TryGetValue("__rowId", out var rowIdObj))
                {
                    var rowId = rowIdObj?.ToString();
                    if (!string.IsNullOrEmpty(rowId))
                    {
                        emptyRowIds.Add(rowId);
                    }
                }
            }
        }

        if (emptyRowIds.Count > 0)
        {
            _logger.LogInformation("STEP 1: Removing {Count} empty rows (scanned {Total} total rows)",
                emptyRowIds.Count, totalRowsScanned);
            await _rowStore.RemoveRowsAsync(emptyRowIds, cancellationToken);
        }
        else
        {
            _logger.LogDebug("STEP 1: No empty rows to remove (scanned {Total} rows)", totalRowsScanned);
        }

        // STEP 2: Ensure last row is empty (always, regardless of current count)
        var currentCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
        if (config.AlwaysKeepLastEmpty)
        {
            // After removing all empty rows, add exactly one empty row at end
            var newEmptyRow = CreateEmptyRow(templateRow);
            await _rowStore.AppendRowsAsync(new[] { newEmptyRow }, cancellationToken);
            _logger.LogInformation(
                "STEP 2: Added 1 empty row at end (AlwaysKeepLastEmpty=true, finalCount={Count})",
                currentCount + 1
            );
        }

        var finalCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
        _logger.LogInformation("2-step cleanup completed: initial={Initial}, final={Final}",
            totalRowsScanned, finalCount);
    }

    /// <summary>
    /// PUBLIC API VERSION: 2-step cleanup that can be called from public API facade
    /// Wraps internal helper with proper command pattern and result mapping
    /// </summary>
    public async Task<RowManagementResult> EnsureMinRowsAndLastEmptyPublicAsync(
        RowManagementConfiguration config,
        CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Starting PUBLIC 2-step cleanup for operation {OperationId}", operationId);

        try
        {
            var initialCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);

            // Call internal 2-step cleanup helper
            await EnsureMinRowsAndLastEmptyAsync(config, templateRow: null, cancellationToken);

            var finalCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
            var emptyRowsCreated = Math.Max(0, finalCount - initialCount);

            var statistics = new RowManagementStatistics
            {
                EmptyRowsCreated = emptyRowsCreated,
                RowsPhysicallyDeleted = 0,
                RowsContentCleared = 0,
                RowsShifted = 0,
                LastEmptyRowMaintained = config.AlwaysKeepLastEmpty
            };

            return RowManagementResult.CreateSuccess(
                finalRowCount: finalCount,
                processedRows: emptyRowsCreated,
                operationType: RowOperationType.AutoExpand,
                operationTime: stopwatch.Elapsed,
                statistics: statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUBLIC 2-step cleanup failed for operation {OperationId}", operationId);
            return RowManagementResult.CreateFailure(
                operationType: RowOperationType.AutoExpand,
                messages: new[] { $"2-step cleanup failed: {ex.Message}" },
                operationTime: stopwatch.Elapsed);
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        // Release semaphore resources
        _deleteOperationLock?.Dispose();
    }

    #endregion
}

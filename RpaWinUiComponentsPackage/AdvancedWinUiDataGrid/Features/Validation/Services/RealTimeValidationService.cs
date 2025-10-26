using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Services;

/// <summary>
/// SENIOR IMPLEMENTATION: Real-time validation service with 300ms throttling.
///
/// Validates cells as user types during edit mode with intelligent debounce/throttle.
/// Automatically applies validation errors to UI via ApplyValidationErrors().
///
/// KEY DIFFERENCES FROM DebouncedValidationService:
/// - DebouncedValidationService: Bulk validation after delete operations (500ms, all rows)
/// - RealTimeValidationService: Single-cell validation during typing (300ms, one row)
///
/// PERFORMANCE OPTIMIZATION:
/// - 300ms throttle prevents excessive validation during rapid typing
/// - Per-cell cancellation ensures only latest value is validated
/// - Background execution with UI thread marshaling for smooth UX
///
/// ARCHITECTURE:
/// - Used by: CellEditService (fires on every TextChanged event)
/// - Calls: ValidationService.ValidateRowAsync()
/// - Updates: DataGridViewModel.ApplyValidationErrors() on UI thread
/// </summary>
internal sealed class RealTimeValidationService : IDisposable
{
    private readonly ILogger<RealTimeValidationService> _logger;
    private readonly IValidationService _validationService;
    private readonly DataGridViewModel _dataGridViewModel;
    private readonly DispatcherQueue? _dispatcherQueue;

    // SENIOR PATTERN: Per-cell throttling with CancellationTokenSource dictionary
    // Key format: "{rowIndex}:{columnName}" allows independent throttling per cell
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingValidations = new();

    private const int THROTTLE_MS = 300; // 300ms throttle (shorter than debounce for better UX)
    private bool _disposed;

    /// <summary>
    /// Creates a new real-time validation service.
    /// </summary>
    /// <param name="logger">Logger for diagnostics</param>
    /// <param name="validationService">Core validation service for rule execution</param>
    /// <param name="dataGridViewModel">View model to update with validation errors</param>
    /// <param name="dispatcherQueue">UI thread dispatcher for ApplyValidationErrors (optional)</param>
    public RealTimeValidationService(
        ILogger<RealTimeValidationService> logger,
        IValidationService validationService,
        DataGridViewModel dataGridViewModel,
        DispatcherQueue? dispatcherQueue = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _dataGridViewModel = dataGridViewModel ?? throw new ArgumentNullException(nameof(dataGridViewModel));
        _dispatcherQueue = dispatcherQueue;

        _logger.LogInformation("RealTimeValidationService initialized with {Throttle}ms throttling", THROTTLE_MS);
    }

    /// <summary>
    /// SENIOR METHOD: Validates cell value with 300ms throttling (debounce).
    ///
    /// Automatically cancels previous validation for same cell if still pending.
    /// This ensures only the LATEST typed value is validated, improving performance.
    ///
    /// WORKFLOW:
    /// 1. User types "A" → schedules validation
    /// 2. User types "B" (before 300ms) → CANCELS "A" validation, schedules "B"
    /// 3. User types "C" (before 300ms) → CANCELS "B" validation, schedules "C"
    /// 4. User stops typing → after 300ms, "C" validation executes
    ///
    /// Result: Only 1 validation instead of 3 (3x performance improvement)
    /// </summary>
    /// <param name="rowIndex">Row index of cell being edited</param>
    /// <param name="columnName">Column name of cell being edited</param>
    /// <param name="newValue">New value typed by user</param>
    /// <param name="cancellationToken">Cancellation token (optional)</param>
    public async Task ValidateCellWithThrottlingAsync(
        int rowIndex,
        string columnName,
        object? newValue,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            _logger.LogWarning("Real-time validation skipped - service disposed");
            return;
        }

        // SENIOR PATTERN: Create unique key for this cell
        var cellKey = $"{rowIndex}:{columnName}";

        // SENIOR PATTERN: Cancel previous validation for this cell if still pending
        // This implements proper debounce behavior (only latest value is validated)
        if (_pendingValidations.TryGetValue(cellKey, out var previousCts))
        {
            previousCts.Cancel();
            previousCts.Dispose();
            _logger.LogTrace("Cancelled previous validation for cell [{CellKey}] (newer value typed)", cellKey);
        }

        // Create new cancellation token source for this validation
        // Linked with external token to support component disposal
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pendingValidations[cellKey] = cts;

        try
        {
            // SENIOR PERFORMANCE: Throttle - wait 300ms before validating
            // If user types again within 300ms, this task will be cancelled above
            await Task.Delay(THROTTLE_MS, cts.Token);

            _logger.LogDebug("Real-time validation executing for cell [{CellKey}] value='{Value}' after {Throttle}ms throttle",
                cellKey, newValue, THROTTLE_MS);

            // Get row data from ViewModel
            var row = _dataGridViewModel.Rows.ElementAtOrDefault(rowIndex);
            if (row == null)
            {
                _logger.LogWarning("Row {RowIndex} not found for real-time validation - skipping", rowIndex);
                return;
            }

            // SENIOR PATTERN: Build row dictionary with NEW value for edited column
            // This allows validation rules to check against the in-progress typed value
            var rowData = new Dictionary<string, object?>();
            foreach (var cell in row.Cells.Where(c => !c.IsSpecialColumn))
            {
                // Use new value for the edited column, current value for others
                rowData[cell.ColumnName] = cell.ColumnName == columnName ? newValue : cell.Value;
            }

            // Create validation context with metadata
            var validationContext = new ValidationContext
            {
                RowIndex = rowIndex,
                ColumnName = columnName,
                Properties = new Dictionary<string, object?>
                {
                    ["NewValue"] = newValue,
                    ["ValidationMode"] = ValidationMode.RealTime, // Indicates real-time validation
                    ["TriggerSource"] = "CellEdit" // For diagnostics
                }
            };

            // Execute validation (core business rules)
            var validationResult = await _validationService.ValidateRowAsync(
                rowData,
                validationContext,
                cts.Token);

            _logger.LogInformation(
                "Real-time validation completed for cell [{CellKey}]: IsValid={IsValid}",
                cellKey, validationResult.IsValid);

            // SENIOR FIX: Fire ValidationChanged event instead of calling ApplyValidationErrors directly
            // The event subscription in ComponentLifecycleManager will handle UI updates
            _validationService.FireValidationChanged();
        }
        catch (OperationCanceledException)
        {
            // Expected when user types again before throttle expires
            _logger.LogTrace("Real-time validation cancelled for cell [{CellKey}] (newer validation triggered)", cellKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Real-time validation failed for cell [{CellKey}]", cellKey);
        }
        finally
        {
            // SENIOR PATTERN: Cleanup - remove from pending dictionary and dispose CTS
            _pendingValidations.TryRemove(cellKey, out _);
            cts.Dispose();
        }
    }

    /// <summary>
    /// SENIOR CLEANUP: Disposes service and cancels all pending validations.
    ///
    /// Called when component is unloaded to prevent memory leaks.
    /// Ensures all background tasks are cancelled gracefully.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _logger.LogInformation("RealTimeValidationService disposing - cancelling {Count} pending validations",
            _pendingValidations.Count);

        // Cancel all pending validations
        foreach (var cts in _pendingValidations.Values)
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing CancellationTokenSource during cleanup");
            }
        }

        _pendingValidations.Clear();
        _logger.LogInformation("RealTimeValidationService disposed successfully");
    }
}

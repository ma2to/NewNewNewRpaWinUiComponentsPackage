using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CellEdit.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Services;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Schema;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.SpecialColumns.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CellEdit.Services;

/// <summary>
/// Service for cell editing operations with real-time validation support
/// Thread-safe implementation with edit session management
/// </summary>
internal sealed class CellEditService : ICellEditService
{
    private readonly ILogger<CellEditService> _logger;
    private readonly IRowStore _rowStore;
    private readonly IValidationService _validationService;
    private readonly ISpecialColumnService _specialColumnService;
    private readonly AdvancedDataGridOptions _options;
    private readonly ColumnSchemaService _columnSchemaService; // PHASE 3: Schema access
    private readonly TypeValidationService _typeValidationService; // PHASE 3: Type validation
    private EditSession? _currentEditSession;
    private readonly object _sessionLock = new();
    private bool _editingEnabled = true;

    /// <summary>
    /// Constructor for CellEditService
    /// PHASE 3: Now includes ColumnSchemaService and TypeValidationService for type enforcement
    /// </summary>
    public CellEditService(
        ILogger<CellEditService> logger,
        IRowStore rowStore,
        IValidationService validationService,
        ISpecialColumnService specialColumnService,
        AdvancedDataGridOptions options,
        ColumnSchemaService columnSchemaService,
        TypeValidationService typeValidationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _specialColumnService = specialColumnService ?? throw new ArgumentNullException(nameof(specialColumnService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _columnSchemaService = columnSchemaService ?? throw new ArgumentNullException(nameof(columnSchemaService));
        _typeValidationService = typeValidationService ?? throw new ArgumentNullException(nameof(typeValidationService));
    }

    /// <summary>
    /// Begins an edit session for a specific cell by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// BREAKING CHANGE v3.0: Replaces rowIndex-based BeginEditAsync.
    /// </summary>
    public async Task<EditResult> BeginEditAsync(string rowId, string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Beginning edit session for rowId {RowId}, column {ColumnName}", rowId, columnName);

            lock (_sessionLock)
            {
                // Check if there's already an active session
                if (_currentEditSession != null && _currentEditSession.IsActive)
                {
                    _logger.LogWarning("Edit session already active for rowId {RowId}, column {ColumnName}",
                        _currentEditSession.RowId, _currentEditSession.ColumnName);
                    return EditResult.Failure("An edit session is already active. Please commit or cancel it first.");
                }
            }

            // Get current row data by rowId
            var row = await _rowStore.GetRowByIdAsync(rowId, cancellationToken);
            if (row == null)
            {
                _logger.LogWarning("Row {RowId} not found when beginning edit", rowId);
                return EditResult.Failure($"Row {rowId} not found");
            }

            // Get current value
            var currentValue = row.TryGetValue(columnName, out var value) ? value : null;

            // Create new edit session
            lock (_sessionLock)
            {
                _currentEditSession = new EditSession
                {
                    SessionId = Guid.NewGuid(),
                    RowId = rowId,  // CHANGED: Use RowId instead of RowIndex
                    ColumnName = columnName,
                    OriginalValue = currentValue,
                    CurrentValue = currentValue,
                    StartedAt = DateTime.UtcNow,
                    IsActive = true
                };
            }

            _logger.LogInformation("Edit session {SessionId} started for rowId {RowId}, column {ColumnName}",
                _currentEditSession.SessionId, rowId, columnName);

            return EditResult.Success(_currentEditSession.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin edit session for rowId {RowId}, column {ColumnName}: {Message}",
                rowId, columnName, ex.Message);
            return EditResult.Failure($"Failed to begin edit: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the value of a cell being edited by stable row ID (with real-time validation).
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// BREAKING CHANGE v3.0: Replaces rowIndex-based UpdateCellAsync.
    /// </summary>
    public async Task<EditResult> UpdateCellAsync(string rowId, string columnName, object? newValue, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating cell for rowId {RowId}, column {ColumnName} with value: {Value}",
                rowId, columnName, newValue);

            // Get current row by rowId
            var row = await _rowStore.GetRowByIdAsync(rowId, cancellationToken);
            if (row == null)
            {
                _logger.LogWarning("Row {RowId} not found when updating cell", rowId);
                return EditResult.Failure($"Row {rowId} not found");
            }

            // Get old value
            var oldValue = row.TryGetValue(columnName, out var value) ? value : null;

            // PHASE 3: Type validation and enforcement (if schema defined)
            object? validatedValue = newValue;
            var columnDef = _columnSchemaService.GetColumnByName(columnName);

            if (columnDef != null && columnDef.DataType != typeof(object))
            {
                _logger.LogDebug("PHASE 3: Type validation for column {ColumnName} (DataType={DataType}, AllowNull={AllowNull})",
                    columnName, columnDef.DataType.Name, columnDef.AllowNull);

                // ✅ CHECK 1: Null validation
                if (newValue == null && !columnDef.AllowNull)
                {
                    var errorMsg = $"Column '{columnName}' does not allow null values";
                    _logger.LogWarning("Type validation failed for rowId {RowId}, column {ColumnName}: {Error}",
                        rowId, columnName, errorMsg);
                    return EditResult.Failure(errorMsg);
                }

                // ✅ CHECK 2: Type compatibility and conversion
                if (newValue != null)
                {
                    var valueType = newValue.GetType();
                    if (!_typeValidationService.IsCompatibleType(valueType, columnDef.DataType))
                    {
                        // Attempt type conversion
                        var convertedValue = _typeValidationService.ConvertValue(newValue, columnDef.DataType);
                        if (convertedValue == null && newValue != null)
                        {
                            // Conversion failed
                            var errorMsg = $"Column '{columnName}': cannot convert '{newValue}' ({valueType.Name}) to {columnDef.DataType.Name}";
                            _logger.LogWarning("Type validation/conversion failed for rowId {RowId}, column {ColumnName}: {Error}",
                                rowId, columnName, errorMsg);
                            return EditResult.Failure(errorMsg);
                        }

                        // Use converted value
                        validatedValue = convertedValue;
                        _logger.LogDebug("Type conversion successful: {From} → {To} for column {ColumnName}",
                            valueType.Name, columnDef.DataType.Name, columnName);
                    }
                }

                _logger.LogDebug("✓ Type validation passed for column {ColumnName}", columnName);
            }
            else if (columnDef != null)
            {
                _logger.LogTrace("Column {ColumnName} has DataType=object - type validation skipped", columnName);
            }

            // Create updated row with validated value
            var updatedRow = new Dictionary<string, object?>(row)
            {
                [columnName] = validatedValue
            };

            // Update the row in store by rowId
            var updated = await _rowStore.UpdateRowByIdAsync(rowId, updatedRow, cancellationToken);
            if (!updated)
            {
                return EditResult.Failure($"Failed to update row {rowId}");
            }

            // Perform real-time validation for this cell (only if ShouldRunAutomaticValidation returns true)
            ValidationResult validationResult;
            string? validationAlerts = null;

            if (_validationService.ShouldRunAutomaticValidation("UpdateCellAsync"))
            {
                // Get current rowIndex for validation context (validation service still needs it)
                var rowIndex = _rowStore.GetRowIndexById(rowId);

                _logger.LogDebug("Performing automatic real-time validation for rowId {RowId}, column {ColumnName}", rowId, columnName);

                var validationContext = new ValidationContext
                {
                    RowIndex = rowIndex ?? -1,  // Use -1 if not found (shouldn't happen)
                    ColumnName = columnName,
                    Properties = new Dictionary<string, object?>
                    {
                        ["OldValue"] = oldValue,
                        ["NewValue"] = newValue,
                        ["ValidationMode"] = ValidationMode.RealTime
                    }
                };

                validationResult = await _validationService.ValidateRowAsync(updatedRow, validationContext, cancellationToken);

                // Update validation alerts column
                if (!validationResult.IsValid)
                {
                    // Format validation alert message
                    var severity = validationResult.Severity.ToString();
                    validationAlerts = $"{severity}: {validationResult.ErrorMessage}";

                    // Update validation alerts (SpecialColumnService may need rowIndex - use GetRowIndexById)
                    if (rowIndex.HasValue)
                    {
                        await _specialColumnService.UpdateValidationAlertsAsync(rowIndex.Value, validationAlerts, cancellationToken);
                    }

                    // ✅ CRITICAL FIX: Write realtime validation errors to validation store
                    // REASON: HandleValidationChanged reads from validation store and clears UI if empty
                    //         If realtime errors are not persisted, ValidationAlerts will be cleared
                    // ARCHITECTURE: Realtime validation creates single-row error → batch validation aggregates
                    // USER REQUIREMENT: Preview validation shows errors immediately during typing,
                    //                   Commit validation writes errors to ValidationAlerts for persistence
                    var errorsList = new List<ValidationError>();

                    // Parse combined error message to extract per-column errors
                    // Format: "Column_1: msg1; Column_4: msg2"
                    if (!string.IsNullOrEmpty(validationResult.ErrorMessage))
                    {
                        var errorParts = validationResult.ErrorMessage.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in errorParts)
                        {
                            var colonIndex = part.IndexOf(':');
                            if (colonIndex > 0)
                            {
                                var colName = part.Substring(0, colonIndex).Trim();
                                var message = part.Substring(colonIndex + 1).Trim();

                                errorsList.Add(new ValidationError
                                {
                                    RowId = rowId,
                                    ColumnName = colName,
                                    Message = message,
                                    Severity = validationResult.Severity == PublicValidationSeverity.Error
                                        ? ValidationSeverity.Error
                                        : ValidationSeverity.Warning,
                                    RuleId = $"realtime_{colName}"
                                });
                            }
                            else
                            {
                                // Fallback: single error for edited column
                                errorsList.Add(new ValidationError
                                {
                                    RowId = rowId,
                                    ColumnName = validationResult.AffectedColumn ?? columnName,
                                    Message = part,
                                    Severity = validationResult.Severity == PublicValidationSeverity.Error
                                        ? ValidationSeverity.Error
                                        : ValidationSeverity.Warning,
                                    RuleId = $"realtime_{columnName}"
                                });
                            }
                        }
                    }

                    // Write to validation store (replaces old errors for this row)
                    // Get existing errors for OTHER rows (preserve them)
                    var allErrors = await _rowStore.GetValidationErrorsAsync(false, false, cancellationToken);
                    var otherRowErrors = allErrors.Where(e => e.RowId != rowId).ToList();

                    // Combine: other rows' errors + this row's new errors
                    var combinedErrors = otherRowErrors.Concat(errorsList).ToList();
                    await _rowStore.WriteValidationResultsAsync(combinedErrors, cancellationToken);

                    _logger.LogDebug("Wrote {Count} realtime validation errors to store for rowId {RowId}",
                        errorsList.Count, rowId);

                    // ✅ PROFESSIONAL FIX: Log message clarity improvement
                    // NOTE: ErrorMessage contains ALL row errors (cross-cell dependencies)
                    // Format: "Column_4: msg1; Column_3: msg2; Column_1: msg3"
                    // This is CORRECT behavior (cross-cell validation), not a bug
                    _logger.LogWarning("Cell update validation failed for rowId {RowId}, editing column {ColumnName} (ALL row validation errors): {Message}",
                        rowId, columnName, validationResult.ErrorMessage);

                    // SENIOR FIX: Fire ValidationChanged event to update cell borders (red) and ValidationAlerts
                    _validationService.FireValidationChanged();
                }
                else
                {
                    // Clear validation alerts for this row
                    if (rowIndex.HasValue)
                    {
                        await _specialColumnService.ClearValidationAlertsAsync(rowIndex.Value, cancellationToken);
                    }

                    // ✅ CRITICAL FIX: Clear realtime validation errors from store when validation passes
                    // Get existing errors and remove errors for this row
                    var allErrors = await _rowStore.GetValidationErrorsAsync(false, false, cancellationToken);
                    var remainingErrors = allErrors.Where(e => e.RowId != rowId).ToList();
                    await _rowStore.WriteValidationResultsAsync(remainingErrors, cancellationToken);

                    _logger.LogDebug("Cleared realtime validation errors from store for rowId {RowId}", rowId);

                    // SENIOR FIX: Fire ValidationChanged event to clear cell borders and ValidationAlerts
                    _validationService.FireValidationChanged();
                }
            }
            else
            {
                _logger.LogDebug("Automatic real-time validation skipped for rowId {RowId}, column {ColumnName} " +
                    "(ValidationAutomationMode or EnableRealTimeValidation is disabled)", rowId, columnName);

                // Create a default success validation result when validation is disabled
                validationResult = new ValidationResult
                {
                    IsValid = true,
                    Severity = PublicValidationSeverity.Info,
                    ErrorMessage = null,
                    AffectedColumn = columnName
                };
            }

            // Update edit session if active
            lock (_sessionLock)
            {
                if (_currentEditSession != null &&
                    _currentEditSession.RowId == rowId &&
                    _currentEditSession.ColumnName == columnName)
                {
                    _currentEditSession = _currentEditSession with { CurrentValue = newValue };
                }
            }

            _logger.LogInformation("Cell updated for rowId {RowId}, column {ColumnName}. Valid: {IsValid}",
                rowId, columnName, validationResult.IsValid);

            return new EditResult
            {
                IsSuccess = true,
                ValidationResult = validationResult,
                ValidationAlerts = validationAlerts,
                SessionId = _currentEditSession?.SessionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cell for rowId {RowId}, column {ColumnName}: {Message}",
                rowId, columnName, ex.Message);
            return EditResult.Failure($"Failed to update cell: {ex.Message}");
        }
    }

    /// <summary>
    /// Commits the current edit session
    /// </summary>
    public async Task<EditResult> CommitEditAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EditSession? session;
            lock (_sessionLock)
            {
                session = _currentEditSession;
                if (session == null || !session.IsActive)
                {
                    _logger.LogWarning("No active edit session to commit");
                    return EditResult.Failure("No active edit session to commit");
                }
            }

            _logger.LogInformation("Committing edit session {SessionId} for rowId {RowId}, column {ColumnName}",
                session.SessionId, session.RowId, session.ColumnName);

            // ✅ PROFESSIONAL FIX: Revalidate and save errors to row store on COMMIT (user confirms cell)
            if (_validationService.ShouldRunAutomaticValidation("CommitEditAsync"))
            {
                _logger.LogInformation("🔍 COMMIT VALIDATION: Starting validation for rowId {RowId}, column {ColumnName}",
                    session.RowId, session.ColumnName);

                // Get current row
                var row = await _rowStore.GetRowByIdAsync(session.RowId, cancellationToken);
                if (row != null)
                {
                    // Revalidate entire row (for cross-cell dependencies)
                    // IMPORTANT: Use stopOnFirstError=false to collect ALL errors (per test requirements)
                    var allRowErrors = await _validationService.GetAllRowErrorsAsync(
                        session.RowId,
                        stopOnFirstError: false, // ✅ Collect ALL errors for testing
                        cancellationToken);

                    _logger.LogInformation("🔍 COMMIT VALIDATION: GetAllRowErrorsAsync returned {ErrorCount} errors for rowId {RowId}",
                        allRowErrors.Count, session.RowId);

                    if (allRowErrors.Any())
                    {
                        _logger.LogInformation("🔍 COMMIT VALIDATION: Error details: {Errors}",
                            string.Join("; ", allRowErrors.Select(e => $"{e.ColumnName}: {e.Message}")));
                    }

                    // Save all errors to row store
                    if (allRowErrors.Any())
                    {
                        await _rowStore.WriteValidationResultsAsync(allRowErrors, cancellationToken);

                        _logger.LogInformation("✅ COMMIT VALIDATION: Saved {ErrorCount} validation errors to row store for rowId {RowId}",
                            allRowErrors.Count, session.RowId);
                    }
                    else
                    {
                        // Clear all errors for this row (validation passed)
                        await _rowStore.ClearValidationErrorsForRowAsync(session.RowId, cancellationToken);

                        _logger.LogInformation("✅ COMMIT VALIDATION: Cleared validation errors for rowId {RowId} (all valid)",
                            session.RowId);
                    }

                    // Fire ValidationChanged to update UI
                    _validationService.FireValidationChanged();

                    _logger.LogInformation("✅ COMMIT VALIDATION: FireValidationChanged called to update UI");
                }
                else
                {
                    _logger.LogWarning("⚠️ COMMIT VALIDATION: Row not found for rowId {RowId}", session.RowId);
                }
            }
            else
            {
                _logger.LogDebug("COMMIT VALIDATION: Skipped (automatic validation disabled)");
            }

            // End the session
            lock (_sessionLock)
            {
                _currentEditSession = session with { IsActive = false };
            }

            _logger.LogInformation("Edit session {SessionId} committed successfully", session.SessionId);

            return EditResult.Success(session.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit edit session: {Message}", ex.Message);
            return EditResult.Failure($"Failed to commit edit: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels the current edit session and reverts to original value
    /// </summary>
    public async Task<EditResult> CancelEditAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EditSession? session;
            lock (_sessionLock)
            {
                session = _currentEditSession;
                if (session == null || !session.IsActive)
                {
                    _logger.LogWarning("No active edit session to cancel");
                    return EditResult.Failure("No active edit session to cancel");
                }
            }

            _logger.LogInformation("Canceling edit session {SessionId} for rowId {RowId}, column {ColumnName}",
                session.SessionId, session.RowId, session.ColumnName);

            // Revert to original value using stable rowId
            var row = await _rowStore.GetRowByIdAsync(session.RowId, cancellationToken);
            if (row != null)
            {
                var revertedRow = new Dictionary<string, object?>(row)
                {
                    [session.ColumnName] = session.OriginalValue
                };
                await _rowStore.UpdateRowByIdAsync(session.RowId, revertedRow, cancellationToken);
            }

            // End the session
            lock (_sessionLock)
            {
                _currentEditSession = session with { IsActive = false };
            }

            _logger.LogInformation("Edit session {SessionId} canceled successfully", session.SessionId);
            return EditResult.Success(session.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel edit session: {Message}", ex.Message);
            return EditResult.Failure($"Failed to cancel edit: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current active edit session
    /// </summary>
    public EditSession? GetCurrentEditSession()
    {
        lock (_sessionLock)
        {
            return _currentEditSession;
        }
    }

    /// <summary>
    /// Checks if there is an active edit session
    /// </summary>
    public bool HasActiveEditSession()
    {
        lock (_sessionLock)
        {
            return _currentEditSession != null && _currentEditSession.IsActive;
        }
    }

    /// <summary>
    /// Checks if currently editing (alias for HasActiveEditSession)
    /// </summary>
    public bool IsEditing()
    {
        return HasActiveEditSession();
    }

    /// <summary>
    /// Gets the current edit position (rowId and column).
    /// STABLE: Returns rowId which persists across sort/filter/delete operations.
    /// BREAKING CHANGE v3.0: Returns rowId instead of rowIndex.
    /// </summary>
    public (string rowId, string columnName)? GetCurrentEditPosition()
    {
        lock (_sessionLock)
        {
            if (_currentEditSession == null || !_currentEditSession.IsActive)
            {
                return null;
            }
            return (_currentEditSession.RowId, _currentEditSession.ColumnName);
        }
    }

    /// <summary>
    /// Sets whether editing is enabled globally
    /// </summary>
    public void SetEditingEnabled(bool enabled)
    {
        _editingEnabled = enabled;
        _logger.LogInformation("Cell editing globally {Status}", enabled ? "enabled" : "disabled");
    }

    /// <summary>
    /// Checks if editing is enabled globally
    /// </summary>
    public bool IsEditingEnabled()
    {
        return _editingEnabled;
    }

    /// <summary>
    /// ✅ NEW: Preview validation during live cell editing (keystroke validation).
    /// PREVIEW MODE: Does NOT write to validation storage - only returns result for UI preview.
    /// PERFORMANCE: Fast, no DB writes (critical for SQLite mode with 300ms debounce).
    /// ARCHITECTURE: Validates in-memory preview row WITHOUT committing to IRowStore.
    /// USE CASE: User types "123" in TextBox → validate immediately → show red border + message → no storage write.
    /// COMMIT: When user presses Enter, UpdateCellAsync writes to storage and commits validation permanently.
    /// </summary>
    public async Task<PreviewValidationResult> PreviewValidateCellAsync(
        string rowId,
        string columnName,
        object? currentValue,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogTrace("🔍 PREVIEW VALIDATION: rowId={RowId}, column={ColumnName}, value={Value}",
                rowId, columnName, currentValue);

            // Check if validation is enabled
            if (!_validationService.ShouldRunAutomaticValidation("PreviewValidateCellAsync"))
            {
                _logger.LogTrace("Preview validation skipped - automatic validation disabled");
                return PreviewValidationResult.Success(columnName);
            }

            // Get current row by rowId
            var row = await _rowStore.GetRowByIdAsync(rowId, cancellationToken);
            if (row == null)
            {
                _logger.LogWarning("Preview validation failed - row {RowId} not found", rowId);
                return PreviewValidationResult.Error("Row not found", columnName);
            }

            // ✅ CRITICAL: Create preview row with new value (WITHOUT writing to storage)
            // This is in-memory only - no IRowStore.UpdateRowByIdAsync call
            var previewRow = new Dictionary<string, object?>(row)
            {
                [columnName] = currentValue
            };

            // Get row index for validation context (validation service still needs it for rule evaluation)
            var rowIndex = _rowStore.GetRowIndexById(rowId);

            // Create validation context for preview mode
            var validationContext = new ValidationContext
            {
                RowIndex = rowIndex ?? -1,
                ColumnName = columnName,
                Properties = new Dictionary<string, object?>
                {
                    ["OldValue"] = row.TryGetValue(columnName, out var oldVal) ? oldVal : null,
                    ["NewValue"] = currentValue,
                    ["ValidationMode"] = ValidationMode.PreviewRealTime  // ← NEW enum value
                }
            };

            // ✅ VALIDATE (in-memory only, NO storage writes)
            var validationResult = await _validationService.ValidateRowAsync(
                previewRow,
                validationContext,
                cancellationToken);

            // ✅ CRITICAL: DO NOT write validation result to storage
            // DO NOT call _validationService.CommitValidationErrorAsync()
            // DO NOT fire ValidationChanged event
            // REASON: This is preview mode - validation is only for UI feedback, not persistence

            // Return preview result
            if (!validationResult.IsValid)
            {
                _logger.LogTrace("⚠️ PREVIEW VALIDATION FAILED: {ErrorMessage}", validationResult.ErrorMessage);
                return PreviewValidationResult.Error(
                    validationResult.ErrorMessage ?? "Validation failed",
                    columnName,
                    validationResult.Severity);
            }
            else
            {
                _logger.LogTrace("✅ PREVIEW VALIDATION PASSED");
                return PreviewValidationResult.Success(columnName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preview validation failed for rowId {RowId}, column {ColumnName}: {Message}",
                rowId, columnName, ex.Message);
            return PreviewValidationResult.Error($"Validation error: {ex.Message}", columnName);
        }
    }
}

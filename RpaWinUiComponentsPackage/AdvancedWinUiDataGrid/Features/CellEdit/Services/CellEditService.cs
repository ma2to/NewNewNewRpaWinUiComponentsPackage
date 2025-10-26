using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CellEdit.Interfaces;
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
    private EditSession? _currentEditSession;
    private readonly object _sessionLock = new();
    private bool _editingEnabled = true;

    /// <summary>
    /// Constructor for CellEditService
    /// </summary>
    public CellEditService(
        ILogger<CellEditService> logger,
        IRowStore rowStore,
        IValidationService validationService,
        ISpecialColumnService specialColumnService,
        AdvancedDataGridOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _specialColumnService = specialColumnService ?? throw new ArgumentNullException(nameof(specialColumnService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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

            // Create updated row
            var updatedRow = new Dictionary<string, object?>(row)
            {
                [columnName] = newValue
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

                    _logger.LogWarning("Cell update validation failed for rowId {RowId}, column {ColumnName}: {Message}",
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

            // End the session
            lock (_sessionLock)
            {
                _currentEditSession = session with { IsActive = false };
            }

            _logger.LogInformation("Edit session {SessionId} committed successfully", session.SessionId);

            await Task.CompletedTask;
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
}

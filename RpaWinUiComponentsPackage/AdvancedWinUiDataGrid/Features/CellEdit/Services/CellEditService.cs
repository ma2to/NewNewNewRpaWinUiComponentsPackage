using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CellEdit.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.SpecialColumns.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

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
    /// Begins an edit session for a specific cell
    /// </summary>
    public async Task<EditResult> BeginEditAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Beginning edit session for row {RowIndex}, column {ColumnName}", rowIndex, columnName);

            lock (_sessionLock)
            {
                // Check if there's already an active session
                if (_currentEditSession != null && _currentEditSession.IsActive)
                {
                    _logger.LogWarning("Edit session already active for row {RowIndex}, column {ColumnName}",
                        _currentEditSession.RowIndex, _currentEditSession.ColumnName);
                    return EditResult.Failure("An edit session is already active. Please commit or cancel it first.");
                }
            }

            // Get current row data
            var row = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
            if (row == null)
            {
                _logger.LogWarning("Row {RowIndex} not found when beginning edit", rowIndex);
                return EditResult.Failure($"Row {rowIndex} not found");
            }

            // Get current value
            var currentValue = row.TryGetValue(columnName, out var value) ? value : null;

            // Create new edit session
            lock (_sessionLock)
            {
                _currentEditSession = new EditSession
                {
                    SessionId = Guid.NewGuid(),
                    RowIndex = rowIndex,
                    ColumnName = columnName,
                    OriginalValue = currentValue,
                    CurrentValue = currentValue,
                    StartedAt = DateTime.UtcNow,
                    IsActive = true
                };
            }

            _logger.LogInformation("Edit session {SessionId} started for row {RowIndex}, column {ColumnName}",
                _currentEditSession.SessionId, rowIndex, columnName);

            return EditResult.Success(_currentEditSession.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin edit session for row {RowIndex}, column {ColumnName}: {Message}",
                rowIndex, columnName, ex.Message);
            return EditResult.Failure($"Failed to begin edit: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the value of a cell being edited (with real-time validation)
    /// </summary>
    public async Task<EditResult> UpdateCellAsync(int rowIndex, string columnName, object? newValue, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating cell for row {RowIndex}, column {ColumnName} with value: {Value}",
                rowIndex, columnName, newValue);

            // Get current row
            var row = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
            if (row == null)
            {
                _logger.LogWarning("Row {RowIndex} not found when updating cell", rowIndex);
                return EditResult.Failure($"Row {rowIndex} not found");
            }

            // Get old value
            var oldValue = row.TryGetValue(columnName, out var value) ? value : null;

            // Create updated row
            var updatedRow = new Dictionary<string, object?>(row)
            {
                [columnName] = newValue
            };

            // Update the row in store
            await _rowStore.UpdateRowAsync(rowIndex, updatedRow, cancellationToken);

            // Perform real-time validation for this cell (only if EnableRealTimeValidation = true)
            ValidationResult validationResult;
            string? validationAlerts = null;

            if (_options.EnableRealTimeValidation)
            {
                _logger.LogDebug("Performing automatic real-time validation for row {RowIndex}, column {ColumnName}", rowIndex, columnName);

                var validationContext = new ValidationContext
                {
                    RowIndex = rowIndex,
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

                    await _specialColumnService.UpdateValidationAlertsAsync(rowIndex, validationAlerts, cancellationToken);

                    _logger.LogWarning("Cell update validation failed for row {RowIndex}, column {ColumnName}: {Message}",
                        rowIndex, columnName, validationResult.ErrorMessage);
                }
                else
                {
                    // Clear validation alerts for this row
                    await _specialColumnService.ClearValidationAlertsAsync(rowIndex, cancellationToken);
                }
            }
            else
            {
                _logger.LogDebug("Real-time validation disabled, skipping validation for row {RowIndex}, column {ColumnName}", rowIndex, columnName);

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
                    _currentEditSession.RowIndex == rowIndex &&
                    _currentEditSession.ColumnName == columnName)
                {
                    _currentEditSession = _currentEditSession with { CurrentValue = newValue };
                }
            }

            _logger.LogInformation("Cell updated for row {RowIndex}, column {ColumnName}. Valid: {IsValid}",
                rowIndex, columnName, validationResult.IsValid);

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
            _logger.LogError(ex, "Failed to update cell for row {RowIndex}, column {ColumnName}: {Message}",
                rowIndex, columnName, ex.Message);
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

            _logger.LogInformation("Committing edit session {SessionId} for row {RowIndex}, column {ColumnName}",
                session.SessionId, session.RowIndex, session.ColumnName);

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

            _logger.LogInformation("Canceling edit session {SessionId} for row {RowIndex}, column {ColumnName}",
                session.SessionId, session.RowIndex, session.ColumnName);

            // Revert to original value
            var row = await _rowStore.GetRowAsync(session.RowIndex, cancellationToken);
            if (row != null)
            {
                var revertedRow = new Dictionary<string, object?>(row)
                {
                    [session.ColumnName] = session.OriginalValue
                };
                await _rowStore.UpdateRowAsync(session.RowIndex, revertedRow, cancellationToken);
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
}

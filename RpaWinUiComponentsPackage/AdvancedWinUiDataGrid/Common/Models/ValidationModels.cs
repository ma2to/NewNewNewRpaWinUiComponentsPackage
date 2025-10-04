namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Validation mode for determining batch vs real-time validation strategy
/// </summary>
internal enum ValidationMode
{
    /// <summary>Batch validation - for import, export, paste operations</summary>
    Batch,

    /// <summary>Real-time validation - for cell editing operations</summary>
    RealTime
}

/// <summary>
/// Validation alert for a specific row with message and severity
/// </summary>
internal sealed record ValidationAlert
{
    /// <summary>
    /// Gets the row index for this alert
    /// </summary>
    internal int RowIndex { get; init; }

    /// <summary>
    /// Gets the validation message
    /// </summary>
    internal string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the validation severity level
    /// </summary>
    internal ValidationSeverity Severity { get; init; }

    /// <summary>
    /// Gets the affected column name (optional)
    /// </summary>
    internal string? ColumnName { get; init; }

    /// <summary>
    /// Gets the rule ID that generated this alert
    /// </summary>
    internal string? RuleId { get; init; }
}

/// <summary>
/// Edit session information for cell editing operations
/// </summary>
internal sealed record EditSession
{
    /// <summary>
    /// Gets the session ID
    /// </summary>
    internal Guid SessionId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the row index being edited
    /// </summary>
    internal int RowIndex { get; init; }

    /// <summary>
    /// Gets the column name being edited
    /// </summary>
    internal string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the original value before editing
    /// </summary>
    internal object? OriginalValue { get; init; }

    /// <summary>
    /// Gets the current value during editing
    /// </summary>
    internal object? CurrentValue { get; init; }

    /// <summary>
    /// Gets the timestamp when editing started
    /// </summary>
    internal DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets whether this session is active
    /// </summary>
    internal bool IsActive { get; init; } = true;
}

/// <summary>
/// Result of a cell edit operation
/// </summary>
internal sealed record EditResult
{
    /// <summary>
    /// Gets whether the edit was successful
    /// </summary>
    internal bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if edit failed
    /// </summary>
    internal string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the validation result for the edited cell
    /// </summary>
    internal ValidationResult? ValidationResult { get; init; }

    /// <summary>
    /// Gets the updated validation alerts for the row
    /// </summary>
    internal string? ValidationAlerts { get; init; }

    /// <summary>
    /// Gets the session ID
    /// </summary>
    internal Guid? SessionId { get; init; }

    /// <summary>
    /// Creates a successful edit result
    /// </summary>
    internal static EditResult Success(Guid? sessionId = null, string? validationAlerts = null) =>
        new() { IsSuccess = true, SessionId = sessionId, ValidationAlerts = validationAlerts };

    /// <summary>
    /// Creates a failed edit result
    /// </summary>
    internal static EditResult Failure(string errorMessage, ValidationResult? validationResult = null) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, ValidationResult = validationResult };
}

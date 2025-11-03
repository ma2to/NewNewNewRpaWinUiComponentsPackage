namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Validation mode for determining batch vs real-time validation strategy
/// </summary>
internal enum ValidationMode
{
    /// <summary>Batch validation - for import, export, paste operations</summary>
    Batch,

    /// <summary>Real-time validation - for cell editing operations (committed to storage)</summary>
    RealTime,

    /// <summary>Preview real-time validation - for live keystroke validation (NOT committed to storage)</summary>
    PreviewRealTime
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
    /// Gets the stable row ID being edited (from __rowId field).
    /// STABLE: Persists across sort/filter/delete operations.
    /// </summary>
    internal string RowId { get; init; } = string.Empty;

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

/// <summary>
/// ✅ NEW: Result of preview validation during live cell editing (keystroke validation).
/// PREVIEW MODE: Does NOT write to validation storage - only returns result for UI preview.
/// USE CASE: User types in TextBox → validate on keystroke → show red border + message → no DB write.
/// PERFORMANCE: Designed for high-frequency calls (300ms debounced) without storage overhead.
/// </summary>
public sealed record PreviewValidationResult
{
    /// <summary>
    /// Gets whether the preview validation passed
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation error message (null if valid)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the validation severity
    /// </summary>
    public PublicValidationSeverity Severity { get; init; } = PublicValidationSeverity.Error;

    /// <summary>
    /// Gets the affected column name
    /// </summary>
    public string? AffectedColumn { get; init; }

    /// <summary>
    /// Gets the timestamp when preview validation was performed
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful preview validation result
    /// </summary>
    public static PreviewValidationResult Success(string? affectedColumn = null) =>
        new()
        {
            IsValid = true,
            AffectedColumn = affectedColumn,
            Severity = PublicValidationSeverity.Info
        };

    /// <summary>
    /// Creates a failed preview validation result with error message
    /// </summary>
    public static PreviewValidationResult Error(string errorMessage, string? affectedColumn = null, PublicValidationSeverity severity = PublicValidationSeverity.Error) =>
        new()
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            AffectedColumn = affectedColumn,
            Severity = severity
        };
}

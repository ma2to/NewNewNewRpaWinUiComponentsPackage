using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Represents a validation error with detailed information
/// </summary>
internal class ValidationError
{
    /// <summary>
    /// Gets or sets the unique stable row identifier (PRIMARY - use this for all operations)
    /// This ID remains stable across sort/filter/delete operations
    /// </summary>
    public string RowId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule ID that triggered the error
    /// </summary>
    public string RuleId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the error code (alias for RuleId)
    /// </summary>
    public string ErrorCode
    {
        get => RuleId;
        init => RuleId = value;
    }

    /// <summary>
    /// Gets or sets the error message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the affected column name
    /// </summary>
    public string? ColumnName { get; init; }

    /// <summary>
    /// Gets or sets the validation severity
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Gets or sets the timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional context information
    /// </summary>
    public Dictionary<string, object?> Context { get; init; } = new();

    /// <summary>
    /// Creates a new validation error
    /// </summary>
    /// <param name="rowId">Unique stable row identifier</param>
    /// <param name="ruleId">Rule ID</param>
    /// <param name="message">Error message</param>
    /// <param name="columnName">Column name</param>
    /// <param name="severity">Severity level</param>
    /// <returns>New validation error instance</returns>
    public static ValidationError Create(
        string rowId,
        string ruleId,
        string message,
        string? columnName = null,
        ValidationSeverity severity = ValidationSeverity.Error)
    {
        return new ValidationError
        {
            RowId = rowId,
            RuleId = ruleId,
            Message = message,
            ColumnName = columnName,
            Severity = severity,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Represents a validation warning with detailed information
/// </summary>
internal class ValidationWarning
{
    /// <summary>
    /// Gets or sets the unique stable row identifier (PRIMARY - use this for all operations)
    /// This ID remains stable across sort/filter/delete operations
    /// </summary>
    public string RowId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule ID that triggered the warning
    /// </summary>
    public string RuleId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the warning message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the affected column name
    /// </summary>
    public string? ColumnName { get; init; }

    /// <summary>
    /// Gets or sets the validation severity
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Warning;

    /// <summary>
    /// Gets or sets the timestamp when the warning occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional context information
    /// </summary>
    public Dictionary<string, object?> Context { get; init; } = new();

    /// <summary>
    /// Creates a new validation warning
    /// </summary>
    /// <param name="rowId">Unique stable row identifier</param>
    /// <param name="ruleId">Rule ID</param>
    /// <param name="message">Warning message</param>
    /// <param name="columnName">Column name</param>
    /// <returns>New validation warning instance</returns>
    public static ValidationWarning Create(
        string rowId,
        string ruleId,
        string message,
        string? columnName = null)
    {
        return new ValidationWarning
        {
            RowId = rowId,
            RuleId = ruleId,
            Message = message,
            ColumnName = columnName,
            Timestamp = DateTime.UtcNow
        };
    }
}
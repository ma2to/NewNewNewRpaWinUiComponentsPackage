
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public models for DataGrid Validation operations.
/// Contains enums, DTOs, and statistics models used by IDataGridValidation interface.
/// </summary>

/// <summary>
/// Public validation severity levels
/// </summary>
public enum PublicValidationSeverity
{
    /// <summary>Informational message</summary>
    Info = 0,

    /// <summary>Warning message</summary>
    Warning = 1,

    /// <summary>Error message</summary>
    Error = 2,

    /// <summary>Critical error message</summary>
    Critical = 3
}

/// <summary>
/// Public validation rule types
/// </summary>
public enum PublicValidationRuleType
{
    /// <summary>Required field validation</summary>
    Required = 0,

    /// <summary>Regular expression validation</summary>
    Regex = 1,

    /// <summary>Range validation</summary>
    Range = 2,

    /// <summary>Custom validation</summary>
    Custom = 3
}

/// <summary>
/// Public validation rule definition
/// </summary>
public class PublicValidationRule
{
    /// <summary>
    /// Type of validation rule
    /// </summary>
    public PublicValidationRuleType RuleType { get; set; } = PublicValidationRuleType.Required;

    /// <summary>
    /// Error message to display when validation fails
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Regular expression pattern (for Regex rule type)
    /// </summary>
    public string? RegexPattern { get; set; }

    /// <summary>
    /// Minimum value (for Range rule type)
    /// </summary>
    public object? MinValue { get; set; }

    /// <summary>
    /// Maximum value (for Range rule type)
    /// </summary>
    public object? MaxValue { get; set; }

    /// <summary>
    /// Custom validation function (for Custom rule type)
    /// </summary>
    public Func<object?, bool>? CustomValidator { get; set; }

    /// <summary>
    /// Validation severity level
    /// </summary>
    public PublicValidationSeverity Severity { get; set; } = PublicValidationSeverity.Error;
}

/// <summary>
/// Public statistics for validation rule execution
/// Provides performance insights for individual validation rules
/// </summary>
public sealed class PublicRuleStatistics
{
    /// <summary>
    /// Name of the validation rule
    /// </summary>
    public string RuleName { get; init; } = string.Empty;

    /// <summary>
    /// Number of times this rule was executed
    /// </summary>
    public int ExecutionCount { get; init; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs { get; init; }

    /// <summary>
    /// Total number of errors found by this rule
    /// </summary>
    public int ErrorsFound { get; init; }

    /// <summary>
    /// Total time spent executing this rule
    /// </summary>
    public TimeSpan TotalExecutionTime { get; init; }

    /// <summary>
    /// Throughput in executions per second
    /// </summary>
    public double ExecutionsPerSecond => TotalExecutionTime.TotalSeconds > 0
        ? ExecutionCount / TotalExecutionTime.TotalSeconds
        : 0;

    /// <summary>
    /// Error rate (percentage of executions that found errors)
    /// </summary>
    public double ErrorRate => ExecutionCount > 0
        ? (double)ErrorsFound / ExecutionCount * 100
        : 0;
}

/// <summary>
/// Extended validation result with detailed statistics
/// Used for batch validation operations that track performance
/// </summary>
public sealed class PublicValidationResultWithStatistics
{
    /// <summary>
    /// Whether all validations passed
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Total number of rows validated
    /// </summary>
    public int TotalRows { get; init; }

    /// <summary>
    /// Number of valid rows
    /// </summary>
    public int ValidRows { get; init; }

    /// <summary>
    /// Total number of validation errors
    /// </summary>
    public int TotalErrors { get; init; }

    /// <summary>
    /// Number of errors by severity
    /// </summary>
    public IReadOnlyDictionary<string, int> ErrorsBySeverity { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Total validation duration
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Validation throughput (rows per second)
    /// </summary>
    public double RowsPerSecond => Duration.TotalSeconds > 0
        ? TotalRows / Duration.TotalSeconds
        : 0;

    /// <summary>
    /// Statistics for each validation rule that was executed
    /// </summary>
    public IReadOnlyList<PublicRuleStatistics> RuleStatistics { get; init; } = Array.Empty<PublicRuleStatistics>();

    /// <summary>
    /// List of all validation errors found
    /// </summary>
    public IReadOnlyList<PublicValidationErrorViewModel> ValidationErrors { get; init; } = Array.Empty<PublicValidationErrorViewModel>();

    /// <summary>
    /// Creates a successful validation result with statistics
    /// </summary>
    public static PublicValidationResultWithStatistics Success(
        int totalRows,
        TimeSpan duration,
        IReadOnlyList<PublicRuleStatistics>? ruleStatistics = null) =>
        new()
        {
            IsValid = true,
            TotalRows = totalRows,
            ValidRows = totalRows,
            TotalErrors = 0,
            Duration = duration,
            RuleStatistics = ruleStatistics ?? Array.Empty<PublicRuleStatistics>()
        };

    /// <summary>
    /// Creates a failed validation result with statistics
    /// </summary>
    public static PublicValidationResultWithStatistics Failure(
        int totalRows,
        int validRows,
        int totalErrors,
        IReadOnlyDictionary<string, int> errorsBySeverity,
        TimeSpan duration,
        IReadOnlyList<PublicRuleStatistics> ruleStatistics,
        IReadOnlyList<PublicValidationErrorViewModel> validationErrors) =>
        new()
        {
            IsValid = false,
            TotalRows = totalRows,
            ValidRows = validRows,
            TotalErrors = totalErrors,
            ErrorsBySeverity = errorsBySeverity,
            Duration = duration,
            RuleStatistics = ruleStatistics,
            ValidationErrors = validationErrors
        };
}

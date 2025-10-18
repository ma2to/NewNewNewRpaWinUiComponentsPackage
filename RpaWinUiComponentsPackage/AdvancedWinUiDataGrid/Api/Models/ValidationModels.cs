
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

/// <summary>
/// Deletion mode for validation-based deletion
/// </summary>
public enum PublicValidationDeletionMode
{
    /// <summary>Delete rows that FAIL the specified validation rules</summary>
    DeleteInvalid = 0,

    /// <summary>Delete rows that PASS the specified validation rules</summary>
    DeleteValid = 1
}

/// <summary>
/// Strategy for handling duplicate rows
/// </summary>
public enum PublicDuplicateDeletionStrategy
{
    /// <summary>Keep first occurrence, delete all subsequent duplicates</summary>
    KeepFirst = 0,

    /// <summary>Keep last occurrence, delete all previous duplicates</summary>
    KeepLast = 1,

    /// <summary>Delete ALL occurrences (keep none)</summary>
    KeepNone = 2
}

/// <summary>
/// Criteria for validation-based row deletion.
/// CRITICAL: Applies ONLY the validation rules provided in this criteria,
/// NOT all validation rules registered in the system.
/// </summary>
public sealed class PublicValidationDeletionCriteria
{
    /// <summary>
    /// Validation rules to apply for this deletion operation.
    /// IMPORTANT: Only these rules are evaluated, not all system rules.
    /// Dictionary key: column name, value: validation rule
    /// </summary>
    public IReadOnlyDictionary<string, PublicValidationRule> ValidationRules { get; init; }
        = new Dictionary<string, PublicValidationRule>();

    /// <summary>
    /// Deletion mode: delete invalid or delete valid rows
    /// </summary>
    public PublicValidationDeletionMode DeletionMode { get; init; } = PublicValidationDeletionMode.DeleteInvalid;

    /// <summary>
    /// Apply deletion only to filtered rows (true) or all rows (false)
    /// </summary>
    public bool OnlyFiltered { get; init; } = false;

    /// <summary>
    /// Apply deletion only to checked rows (true) or all rows (false)
    /// </summary>
    public bool OnlyChecked { get; init; } = false;

    /// <summary>
    /// Creates criteria for deleting rows that fail specific validation rules
    /// </summary>
    public static PublicValidationDeletionCriteria DeleteInvalidRows(
        IReadOnlyDictionary<string, PublicValidationRule> rules,
        bool onlyFiltered = false,
        bool onlyChecked = false) =>
        new()
        {
            ValidationRules = rules,
            DeletionMode = PublicValidationDeletionMode.DeleteInvalid,
            OnlyFiltered = onlyFiltered,
            OnlyChecked = onlyChecked
        };

    /// <summary>
    /// Creates criteria for deleting rows that pass specific validation rules
    /// </summary>
    public static PublicValidationDeletionCriteria DeleteValidRows(
        IReadOnlyDictionary<string, PublicValidationRule> rules,
        bool onlyFiltered = false,
        bool onlyChecked = false) =>
        new()
        {
            ValidationRules = rules,
            DeletionMode = PublicValidationDeletionMode.DeleteValid,
            OnlyFiltered = onlyFiltered,
            OnlyChecked = onlyChecked
        };
}

/// <summary>
/// Criteria for duplicate row deletion
/// </summary>
public sealed class PublicDuplicateDeletionCriteria
{
    /// <summary>
    /// Column names to use for duplicate detection.
    /// Rows are considered duplicates if ALL specified columns have matching values.
    /// Empty list = consider all columns
    /// </summary>
    public IReadOnlyList<string> ComparisonColumns { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Duplicate handling strategy (keep first, keep last, keep none)
    /// </summary>
    public PublicDuplicateDeletionStrategy Strategy { get; init; } = PublicDuplicateDeletionStrategy.KeepFirst;

    /// <summary>
    /// Apply deletion only to filtered rows (true) or all rows (false)
    /// </summary>
    public bool OnlyFiltered { get; init; } = false;

    /// <summary>
    /// Apply deletion only to checked rows (true) or all rows (false)
    /// </summary>
    public bool OnlyChecked { get; init; } = false;

    /// <summary>
    /// Case-sensitive comparison (default: false)
    /// </summary>
    public bool CaseSensitive { get; init; } = false;

    /// <summary>
    /// Creates criteria for deleting duplicates (keep first occurrence)
    /// </summary>
    public static PublicDuplicateDeletionCriteria KeepFirst(
        IReadOnlyList<string>? comparisonColumns = null,
        bool onlyFiltered = false,
        bool onlyChecked = false,
        bool caseSensitive = false) =>
        new()
        {
            ComparisonColumns = comparisonColumns ?? Array.Empty<string>(),
            Strategy = PublicDuplicateDeletionStrategy.KeepFirst,
            OnlyFiltered = onlyFiltered,
            OnlyChecked = onlyChecked,
            CaseSensitive = caseSensitive
        };

    /// <summary>
    /// Creates criteria for deleting duplicates (keep last occurrence)
    /// </summary>
    public static PublicDuplicateDeletionCriteria KeepLast(
        IReadOnlyList<string>? comparisonColumns = null,
        bool onlyFiltered = false,
        bool onlyChecked = false,
        bool caseSensitive = false) =>
        new()
        {
            ComparisonColumns = comparisonColumns ?? Array.Empty<string>(),
            Strategy = PublicDuplicateDeletionStrategy.KeepLast,
            OnlyFiltered = onlyFiltered,
            OnlyChecked = onlyChecked,
            CaseSensitive = caseSensitive
        };

    /// <summary>
    /// Creates criteria for deleting ALL duplicate rows (keep none)
    /// </summary>
    public static PublicDuplicateDeletionCriteria KeepNone(
        IReadOnlyList<string>? comparisonColumns = null,
        bool onlyFiltered = false,
        bool onlyChecked = false,
        bool caseSensitive = false) =>
        new()
        {
            ComparisonColumns = comparisonColumns ?? Array.Empty<string>(),
            Strategy = PublicDuplicateDeletionStrategy.KeepNone,
            OnlyFiltered = onlyFiltered,
            OnlyChecked = onlyChecked,
            CaseSensitive = caseSensitive
        };
}

/// <summary>
/// Result of validation-based or duplicate deletion operation
/// </summary>
public sealed class PublicValidationDeletionResult
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Number of rows deleted
    /// </summary>
    public int RowsDeleted { get; init; }

    /// <summary>
    /// Final row count after deletion
    /// </summary>
    public int FinalRowCount { get; init; }

    /// <summary>
    /// Number of empty rows created during 3-step cleanup
    /// </summary>
    public int EmptyRowsCreated { get; init; }

    /// <summary>
    /// Total duration of the operation
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Additional details about the operation
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Creates a successful deletion result
    /// </summary>
    public static PublicValidationDeletionResult Success(
        int rowsDeleted,
        int finalRowCount,
        int emptyRowsCreated,
        TimeSpan duration,
        string? details = null) =>
        new()
        {
            IsSuccess = true,
            RowsDeleted = rowsDeleted,
            FinalRowCount = finalRowCount,
            EmptyRowsCreated = emptyRowsCreated,
            Duration = duration,
            Details = details
        };

    /// <summary>
    /// Creates a failed deletion result
    /// </summary>
    public static PublicValidationDeletionResult Failure(
        string errorMessage,
        TimeSpan duration) =>
        new()
        {
            IsSuccess = false,
            RowsDeleted = 0,
            FinalRowCount = 0,
            EmptyRowsCreated = 0,
            Duration = duration,
            ErrorMessage = errorMessage
        };
}

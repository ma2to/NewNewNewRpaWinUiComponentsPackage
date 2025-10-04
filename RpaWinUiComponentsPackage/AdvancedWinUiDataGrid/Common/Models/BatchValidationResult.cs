using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Result of batch validation operation with comprehensive metrics
/// </summary>
internal class BatchValidationResult
{
    /// <summary>
    /// Gets whether all validations passed
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the number of rows validated
    /// </summary>
    public int ValidatedRows { get; init; }

    /// <summary>
    /// Gets the total error count
    /// </summary>
    public int TotalErrorCount { get; init; }

    /// <summary>
    /// Gets the total warning count
    /// </summary>
    public int TotalWarningCount { get; init; }

    /// <summary>
    /// Gets the total number of rows processed
    /// </summary>
    public int TotalRows { get; init; }

    /// <summary>
    /// Gets the time taken for validation
    /// </summary>
    public TimeSpan ValidationTime { get; init; }

    /// <summary>
    /// Gets individual row validation results
    /// </summary>
    public IReadOnlyList<ValidationResult> RowResults { get; init; } = Array.Empty<ValidationResult>();

    /// <summary>
    /// Gets summary of validation errors
    /// </summary>
    public IReadOnlyList<string> ErrorSummary { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets validation errors
    /// </summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; init; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Gets validation warnings
    /// </summary>
    public IReadOnlyList<ValidationWarning> ValidationWarnings { get; init; } = Array.Empty<ValidationWarning>();

    /// <summary>
    /// Gets error message if failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Success rate as percentage
    /// </summary>
    public double SuccessRate => ValidatedRows > 0 ? (double)(ValidatedRows - TotalErrorCount) / ValidatedRows * 100 : 0;

    /// <summary>
    /// Creates successful batch validation result
    /// </summary>
    public static BatchValidationResult Success(
        int validatedRows,
        int totalErrorCount,
        int totalWarningCount,
        TimeSpan validationTime,
        IReadOnlyList<ValidationResult>? rowResults = null,
        IReadOnlyList<ValidationError>? validationErrors = null,
        IReadOnlyList<ValidationWarning>? validationWarnings = null)
    {
        return new BatchValidationResult
        {
            IsSuccess = totalErrorCount == 0,
            ValidatedRows = validatedRows,
            TotalErrorCount = totalErrorCount,
            TotalWarningCount = totalWarningCount,
            TotalRows = validatedRows,
            ValidationTime = validationTime,
            RowResults = rowResults ?? Array.Empty<ValidationResult>(),
            ValidationErrors = validationErrors ?? Array.Empty<ValidationError>(),
            ValidationWarnings = validationWarnings ?? Array.Empty<ValidationWarning>(),
            ErrorSummary = Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates failed batch validation result
    /// </summary>
    public static BatchValidationResult Failed(
        string errorMessage,
        TimeSpan validationTime,
        int validatedRows = 0,
        int totalErrorCount = 0,
        int totalWarningCount = 0,
        IReadOnlyList<ValidationResult>? rowResults = null,
        IReadOnlyList<string>? errorSummary = null)
    {
        return new BatchValidationResult
        {
            IsSuccess = false,
            ValidatedRows = validatedRows,
            TotalErrorCount = totalErrorCount,
            TotalWarningCount = totalWarningCount,
            TotalRows = validatedRows,
            ValidationTime = validationTime,
            RowResults = rowResults ?? Array.Empty<ValidationResult>(),
            ErrorSummary = errorSummary ?? new[] { errorMessage },
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates successful batch validation result for convenience
    /// </summary>
    public static BatchValidationResult CreateSuccess(
        int validatedRows,
        int totalRows,
        TimeSpan validationTime,
        IReadOnlyList<ValidationResult> rowResults)
    {
        var errorCount = rowResults.Count(r => !r.IsValid && r.Severity == PublicValidationSeverity.Error);
        var warningCount = rowResults.Count(r => !r.IsValid && r.Severity == PublicValidationSeverity.Warning);

        return new BatchValidationResult
        {
            IsSuccess = errorCount == 0,
            ValidatedRows = validatedRows,
            TotalErrorCount = errorCount,
            TotalWarningCount = warningCount,
            TotalRows = totalRows,
            ValidationTime = validationTime,
            RowResults = rowResults,
            ErrorSummary = Array.Empty<string>()
        };
    }

    /// <summary>
    /// Creates failed batch validation result for convenience
    /// </summary>
    public static BatchValidationResult CreateFailure(
        int validatedRows,
        int failedRows,
        int totalRows,
        TimeSpan validationTime,
        IReadOnlyList<ValidationResult> rowResults,
        IReadOnlyList<string> errorSummary)
    {
        return new BatchValidationResult
        {
            IsSuccess = false,
            ValidatedRows = validatedRows,
            TotalErrorCount = failedRows,
            TotalWarningCount = 0,
            TotalRows = totalRows,
            ValidationTime = validationTime,
            RowResults = rowResults,
            ErrorSummary = errorSummary
        };
    }
}
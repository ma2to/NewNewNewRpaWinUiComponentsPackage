namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public validation result for row data validation.
/// Used by AddRowWithDialogAsync and ValidateRowDataAsync API methods.
/// </summary>
public record PublicValidationResult
{
    /// <summary>
    /// True if all validations passed
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors (empty if valid)
    /// </summary>
    public IReadOnlyList<PublicCellValidationError> Errors { get; init; } = Array.Empty<PublicCellValidationError>();

    /// <summary>
    /// Create successful validation result
    /// </summary>
    public static PublicValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Create failed validation result with errors
    /// </summary>
    public static PublicValidationResult Failure(IReadOnlyList<PublicCellValidationError> errors) =>
        new() { IsValid = false, Errors = errors };
}

/// <summary>
/// Validation error for a single cell
/// </summary>
public record PublicCellValidationError
{
    /// <summary>
    /// Column name where error occurred
    /// </summary>
    public string ColumnName { get; init; } = "";

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; init; } = "";

    /// <summary>
    /// Severity of the validation error
    /// </summary>
    public PublicValidationSeverity Severity { get; init; } = PublicValidationSeverity.Error;
}

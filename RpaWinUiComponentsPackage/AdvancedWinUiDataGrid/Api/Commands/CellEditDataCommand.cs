using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Command for beginning a cell edit operation
/// </summary>
public sealed record BeginEditDataCommand
{
    /// <summary>
    /// Gets the row index to edit
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Gets the column name to edit
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;
}

/// <summary>
/// Command for updating a cell value
/// </summary>
public sealed record UpdateCellDataCommand
{
    /// <summary>
    /// Gets the row index to update
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Gets the column name to update
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new value for the cell
    /// </summary>
    public object? NewValue { get; init; }

    /// <summary>
    /// Gets whether to validate the cell immediately
    /// </summary>
    public bool ValidateImmediately { get; init; } = true;
}

/// <summary>
/// Result of a cell edit operation
/// </summary>
public sealed record CellEditResult
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets whether the cell is valid after editing
    /// </summary>
    public bool IsValid { get; init; } = true;

    /// <summary>
    /// Gets the validation message if cell is invalid
    /// </summary>
    public string? ValidationMessage { get; init; }

    /// <summary>
    /// Gets the validation severity
    /// </summary>
    public PublicValidationSeverity ValidationSeverity { get; init; } = PublicValidationSeverity.Info;

    /// <summary>
    /// Gets the validation alerts for the row
    /// </summary>
    public string? ValidationAlerts { get; init; }

    /// <summary>
    /// Gets the session ID for the edit
    /// </summary>
    public Guid? SessionId { get; init; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static CellEditResult Success(Guid? sessionId = null, string? validationAlerts = null) =>
        new() { IsSuccess = true, SessionId = sessionId, ValidationAlerts = validationAlerts };

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static CellEditResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };

    /// <summary>
    /// Creates a result with validation error
    /// </summary>
    public static CellEditResult ValidationError(string validationMessage, PublicValidationSeverity severity, string? validationAlerts = null) =>
        new()
        {
            IsSuccess = true,
            IsValid = false,
            ValidationMessage = validationMessage,
            ValidationSeverity = severity,
            ValidationAlerts = validationAlerts
        };
}

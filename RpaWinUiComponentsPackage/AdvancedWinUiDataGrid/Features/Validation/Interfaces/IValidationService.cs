using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;

/// <summary>
/// Service interface for validation operations with comprehensive validation functionality
/// Implements Scoped lifetime per DI_DECISIONS.md - per-operation state isolation
/// </summary>
internal interface IValidationService
{
    /// <summary>
    /// CRITICAL: Validates all non-empty rows with batched, thread-safe processing
    /// CRITICAL: Must be batched, thread-safe and work with streams (in-memory / cache / disk)
    /// Called by Import & Paste & Export operations automatically
    /// </summary>
    /// <param name="onlyFiltered">Whether to validate only filtered rows</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result indicating if all non-empty rows are valid</returns>
    Task<Result<bool>> AreAllNonEmptyRowsValidAsync(
        bool onlyFiltered = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a validation rule to the system
    /// </summary>
    /// <param name="rule">Validation rule to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> AddValidationRuleAsync(
        IValidationRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes validation rules by column names
    /// </summary>
    /// <param name="columnNames">Column names to remove rules for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> RemoveValidationRulesAsync(
        string[] columnNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a validation rule by name
    /// </summary>
    /// <param name="ruleName">Name of rule to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> RemoveValidationRuleAsync(
        string ruleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all validation rules
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> ClearAllValidationRulesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a single row against all applicable rules
    /// </summary>
    /// <param name="row">Row data to validate</param>
    /// <param name="context">Validation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateRowAsync(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates multiple rows in batch with streaming support
    /// </summary>
    /// <param name="rows">Rows to validate</param>
    /// <param name="progress">Progress reporting callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch validation results</returns>
    Task<BatchValidationResult> ValidateRowsBatchAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        IProgress<ValidationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently configured validation rules
    /// </summary>
    /// <returns>Read-only collection of validation rules</returns>
    IReadOnlyList<IValidationRule> GetValidationRules();

    /// <summary>
    /// Gets validation rules for specific columns
    /// </summary>
    /// <param name="columnNames">Column names to get rules for</param>
    /// <returns>Validation rules for specified columns</returns>
    IReadOnlyList<IValidationRule> GetValidationRulesForColumns(params string[] columnNames);

    /// <summary>
    /// Validates a single cell with real-time validation mode
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="columnName">Column name</param>
    /// <param name="newValue">New value for the cell</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result for the cell</returns>
    Task<ValidationResult> ValidateCellAsync(
        int rowIndex,
        string columnName,
        object? newValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines validation mode based on operation name
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>Validation mode to use</returns>
    ValidationMode DetermineValidationMode(string operationName);

    /// <summary>
    /// Determines if automatic validation should run for a specific operation
    /// Implements ValidationAutomationMode logic
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>True if automatic validation should run, false otherwise</returns>
    bool ShouldRunAutomaticValidation(string operationName);

    /// <summary>
    /// Gets validation alerts message for a specific row
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <returns>Formatted validation alerts string</returns>
    string GetValidationAlertsForRow(int rowIndex);

    /// <summary>
    /// Updates validation alerts for a specific row
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="results">Validation results for the row</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> UpdateValidationAlertsAsync(
        int rowIndex,
        IReadOnlyList<ValidationResult> results,
        CancellationToken cancellationToken = default);

    // Public API compatibility methods
    Task<Result<bool>> ValidateAllAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default);
    Task<PublicValidationResultWithStatistics> ValidateAllWithStatisticsAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default);
    void RefreshValidationResultsToUI();
    string GetValidationAlerts(int rowIndex);
    bool HasValidationErrors(int rowIndex);
}
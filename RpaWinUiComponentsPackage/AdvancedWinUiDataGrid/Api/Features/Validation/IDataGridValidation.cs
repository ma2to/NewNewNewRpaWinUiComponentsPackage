
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Validation;

/// <summary>
/// Public interface for DataGrid validation operations.
/// Provides comprehensive validation functionality including rule management and validation execution.
/// </summary>
public interface IDataGridValidation
{
    /// <summary>
    /// Validates all non-empty rows with batched, thread-safe processing.
    /// </summary>
    /// <param name="onlyFiltered">Whether to validate only filtered rows</param>
    /// <param name="onlyChecked">Whether to validate only checked rows (checkbox column = true)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Validation result indicating if all rows are valid</returns>
    /// <remarks>
    /// When both onlyFiltered and onlyChecked are true, validates rows that match BOTH criteria (AND logic).
    /// This is commonly used for export scenarios where user wants to export only filtered AND checked rows.
    /// </remarks>
    Task<PublicResult<bool>> ValidateAllAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates all non-empty rows with detailed statistics tracking.
    /// Provides performance insights including rule execution times and error rates.
    /// </summary>
    /// <param name="onlyFiltered">Whether to validate only filtered rows</param>
    /// <param name="onlyChecked">Whether to validate only checked rows (checkbox column = true)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Validation result with detailed rule statistics and performance metrics</returns>
    Task<PublicValidationResultWithStatistics> ValidateAllWithStatisticsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all non-empty rows are valid (quick check without detailed statistics).
    /// </summary>
    /// <param name="onlyFiltered">Whether to validate only filtered rows</param>
    /// <param name="onlyChecked">Whether to validate only checked rows (checkbox column = true)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>True if all non-empty rows are valid, false otherwise</returns>
    Task<bool> AreAllNonEmptyRowsValidAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes validation results to UI (no-op in headless mode).
    /// </summary>
    void RefreshValidationResultsToUI();

    /// <summary>
    /// Adds a validation rule to the grid.
    /// </summary>
    /// <param name="rule">Validation rule to add</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> AddValidationRuleAsync(IValidationRule rule);

    /// <summary>
    /// Removes validation rules by column names.
    /// </summary>
    /// <param name="columnNames">Column names to remove rules for</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> RemoveValidationRulesAsync(params string[] columnNames);

    /// <summary>
    /// Removes a validation rule by name.
    /// </summary>
    /// <param name="ruleName">Name of rule to remove</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> RemoveValidationRuleAsync(string ruleName);

    /// <summary>
    /// Clears all validation rules from the grid.
    /// </summary>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearAllValidationRulesAsync();

    /// <summary>
    /// Gets validation alerts for a specific row.
    /// </summary>
    /// <param name="rowId">Unique stable row identifier</param>
    /// <returns>Validation alerts string</returns>
    string GetValidationAlerts(string rowId);

    /// <summary>
    /// Checks if a row has validation errors.
    /// </summary>
    /// <param name="rowId">Unique stable row identifier</param>
    /// <returns>True if row has validation errors</returns>
    bool HasValidationErrors(string rowId);

    /// <summary>
    /// Gets all validation errors from the most recent validation operation.
    /// CRITICAL: Use this after validation to apply errors to UI ViewModels.
    /// </summary>
    /// <param name="onlyFiltered">Whether to get errors only for filtered rows</param>
    /// <param name="onlyChecked">Whether to get errors only for checked rows</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>List of validation errors with RowId and ColumnName for UI mapping</returns>
    Task<IReadOnlyList<PublicValidationErrorViewModel>> GetValidationErrorsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete rows based on specific validation rules.
    /// CRITICAL: Applies ONLY the rules provided in criteria, NOT all system rules.
    /// Applies 3-step cleanup after deletion (remove empty from middle, ensure minRows, ensure last empty).
    /// </summary>
    /// <param name="criteria">Validation deletion criteria (rules, mode, scope)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with rows deleted count and final statistics</returns>
    /// <example>
    /// // Delete rows where column "Age" is required but missing
    /// var rules = new Dictionary&lt;string, PublicValidationRule&gt;
    /// {
    ///     ["Age"] = new PublicValidationRule
    ///     {
    ///         RuleType = PublicValidationRuleType.Required,
    ///         ErrorMessage = "Age is required"
    ///     }
    /// };
    /// var criteria = PublicValidationDeletionCriteria.DeleteInvalidRows(rules);
    /// var result = await grid.Validation.DeleteRowsByValidationAsync(criteria);
    /// </example>
    Task<PublicValidationDeletionResult> DeleteRowsByValidationAsync(
        PublicValidationDeletionCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete duplicate rows based on column comparison.
    /// Applies 3-step cleanup after deletion (remove empty from middle, ensure minRows, ensure last empty).
    /// </summary>
    /// <param name="criteria">Duplicate deletion criteria (columns, strategy, scope)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with rows deleted count and final statistics</returns>
    /// <example>
    /// // Delete duplicate rows based on "Email" column, keep first occurrence
    /// var criteria = PublicDuplicateDeletionCriteria.KeepFirst(
    ///     comparisonColumns: new[] { "Email" }
    /// );
    /// var result = await grid.Validation.DeleteDuplicateRowsAsync(criteria);
    /// </example>
    Task<PublicValidationDeletionResult> DeleteDuplicateRowsAsync(
        PublicDuplicateDeletionCriteria criteria,
        CancellationToken cancellationToken = default);
}

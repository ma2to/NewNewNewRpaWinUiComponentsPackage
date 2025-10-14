
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
    /// <param name="rowIndex">Row index</param>
    /// <returns>Validation alerts string</returns>
    string GetValidationAlerts(int rowIndex);

    /// <summary>
    /// Checks if a row has validation errors.
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <returns>True if row has validation errors</returns>
    bool HasValidationErrors(int rowIndex);
}

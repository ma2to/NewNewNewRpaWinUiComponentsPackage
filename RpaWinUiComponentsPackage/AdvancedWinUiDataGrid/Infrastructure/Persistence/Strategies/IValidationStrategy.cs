using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Strategies;

/// <summary>
/// ✅ VALIDATION STRATEGY: Interface for validation state storage.
/// SEPARATION: Contains only VALIDATION STORAGE operations (write, read, cache).
/// BUSINESS LOGIC: Validation orchestration is in UnifiedRowStore.
/// </summary>
internal interface IValidationStrategy
{
    // ========== METADATA ==========

    /// <summary>
    /// Strategy name for logging/debugging.
    /// </summary>
    string StrategyName { get; }

    // ========== VALIDATION STORAGE ==========

    /// <summary>
    /// Write validation results for rows.
    /// InMemory: Update validation cache dictionary.
    /// Hybrid: Queue validation state write to SQLite.
    /// </summary>
    Task WriteValidationResultsAsync(
        IEnumerable<ValidationError> results,
        CancellationToken ct);

    /// <summary>
    /// Get validation errors for scope (filtered/unfiltered, checked/unchecked).
    /// </summary>
    Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(
        bool onlyFiltered,
        bool onlyChecked,
        CancellationToken ct);

    /// <summary>
    /// Get validation errors for specific row by rowId.
    /// </summary>
    Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(
        string rowId,
        CancellationToken ct);

    /// <summary>
    /// Check if validation state exists for scope.
    /// </summary>
    Task<bool> HasValidationStateAsync(
        bool onlyFiltered,
        bool onlyChecked,
        CancellationToken ct);

    /// <summary>
    /// Check if all non-empty rows are marked as valid.
    /// </summary>
    Task<bool> AreAllNonEmptyRowsMarkedValidAsync(
        bool onlyFiltered,
        bool onlyChecked,
        CancellationToken ct);

    /// <summary>
    /// Clear all validation state.
    /// </summary>
    Task ClearValidationStateAsync(CancellationToken ct);

    // ========== VALIDATION CACHE (SENIOR FIX) ==========

    /// <summary>
    /// Checks if a row has already been validated (cache check).
    /// SENIOR FIX: Prevents ValidateAll infinite loop.
    /// </summary>
    bool IsRowValidationCached(string rowId);

    /// <summary>
    /// Marks a row as validated in the cache.
    /// </summary>
    void MarkRowAsValidated(string rowId);

    /// <summary>
    /// Clears validation cache.
    /// </summary>
    void ClearValidationCache();

    /// <summary>
    /// Batch writes validation results for multiple rows in a single operation.
    /// SENIOR FIX: Prevents infinite validation loop by avoiding multiple DataChanged events.
    /// </summary>
    Task WriteValidationResultsBatchAsync(
        Dictionary<string, ValidationError[]> validationResults,
        CancellationToken ct);

    /// <summary>
    /// Clears all validation errors for a specific row.
    /// Used after revalidation when all errors have been fixed.
    /// </summary>
    Task ClearValidationErrorsForRowAsync(
        string rowId,
        CancellationToken ct);
}

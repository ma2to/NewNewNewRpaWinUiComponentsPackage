using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Strategies.Validation;

/// <summary>
/// ✅ IN-MEMORY VALIDATION STRATEGY (FINAL VERSION)
/// EXTRACTED from InMemoryRowStore.cs (validation logic only).
/// ARCHITECTURE: Stores validation state in memory using ConcurrentDictionary.
/// SENIOR FIX: Includes validation cache to prevent infinite validation loop.
/// </summary>
internal sealed class InMemoryValidationStrategy : IValidationStrategy
{
    // ========== PRIVATE FIELDS (extracted from InMemoryRowStore.cs) ==========

    private readonly ConcurrentDictionary<string, List<ValidationError>> _validationErrors = new();
    private readonly object _modificationLock = new();
    private bool _hasValidationState = false;

    // SENIOR FIX: VALIDATION CACHE - Prevents ValidateAll infinite loop
    // CRITICAL: Tracks which rows have been validated to avoid re-validation (cache check)
    // Cleared on data changes (ClearAsync, AddRangeAsync) to ensure fresh validation
    // ✅ THREAD-SAFE: ConcurrentDictionary for lock-free access (DEADLOCK FIX)
    private readonly ConcurrentDictionary<string, bool> _validatedRowsCache = new(); // Validated row IDs
    private bool _isValidating = false; // Re-entrancy guard for batch validation

    private readonly ILogger<InMemoryValidationStrategy>? _logger;

    // ========== CONSTRUCTOR ==========

    public InMemoryValidationStrategy(ILogger<InMemoryValidationStrategy>? logger)
    {
        _logger = logger;
    }

    // ========== METADATA ==========

    public string StrategyName => "InMemory";

    // ========== VALIDATION STORAGE ==========

    /// <summary>
    /// Write validation results - IValidationStrategy implementation.
    /// EXTRACTED FROM InMemoryRowStore.cs:622-645
    /// </summary>
    public Task WriteValidationResultsAsync(
        IEnumerable<ValidationError> results,
        CancellationToken ct)
    {
        _logger?.LogDebug("Writing validation results");

        _validationErrors.Clear();
        foreach (var error in results)
        {
            var rowId = error.RowId;
            if (string.IsNullOrEmpty(rowId))
                continue; // Skip errors without RowId

            if (!_validationErrors.ContainsKey(rowId))
            {
                _validationErrors[rowId] = new List<ValidationError>();
            }
            _validationErrors[rowId].Add(error);
        }

        _hasValidationState = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get validation errors for scope (filtered/unfiltered, checked/unchecked).
    /// EXTRACTED FROM InMemoryRowStore.cs:695-715
    /// </summary>
    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsAsync(
        bool onlyFiltered,
        bool onlyChecked,
        CancellationToken ct)
    {
        var errors = _validationErrors.Values
            .SelectMany(list => list)
            .Where(error =>
            {
                // TODO: Implement filtering by onlyFiltered and onlyChecked when needed
                // For now, return all errors
                return true;
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ValidationError>>(errors);
    }

    /// <summary>
    /// Get validation errors for specific row.
    /// EXTRACTED FROM InMemoryRowStore.cs:1013-1027
    /// </summary>
    public Task<IReadOnlyList<ValidationError>> GetValidationErrorsForRowAsync(
        string rowId,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rowId))
            return Task.FromResult<IReadOnlyList<ValidationError>>(Array.Empty<ValidationError>());

        if (_validationErrors.TryGetValue(rowId, out var errors))
        {
            return Task.FromResult<IReadOnlyList<ValidationError>>(errors.ToList());
        }

        return Task.FromResult<IReadOnlyList<ValidationError>>(Array.Empty<ValidationError>());
    }

    /// <summary>
    /// Check if validation state exists for scope.
    /// EXTRACTED FROM InMemoryRowStore.cs:650-656
    /// </summary>
    public Task<bool> HasValidationStateAsync(
        bool onlyFiltered,
        bool onlyChecked,
        CancellationToken ct)
    {
        return Task.FromResult(_hasValidationState);
    }

    /// <summary>
    /// Check if all non-empty rows are marked as valid.
    /// EXTRACTED FROM InMemoryRowStore.cs:658-688
    /// NOTE: Requires access to row data for IsRowEmpty check - simplified for now.
    /// </summary>
    public Task<bool> AreAllNonEmptyRowsMarkedValidAsync(
        bool onlyFiltered,
        bool onlyChecked,
        CancellationToken ct)
    {
        if (!_hasValidationState)
            return Task.FromResult(false);

        // Simplified: Check if there are any validation errors
        var allValid = !_validationErrors.Any();

        _logger?.LogDebug("Checked validation state: onlyFiltered={OnlyFiltered}, onlyChecked={OnlyChecked}, allValid={AllValid}",
            onlyFiltered, onlyChecked, allValid);

        return Task.FromResult(allValid);
    }

    /// <summary>
    /// Clear all validation state.
    /// EXTRACTED FROM InMemoryRowStore.cs:720-726
    /// </summary>
    public Task ClearValidationStateAsync(CancellationToken ct)
    {
        _validationErrors.Clear();
        _hasValidationState = false;
        _logger?.LogDebug("Validation state cleared");
        return Task.CompletedTask;
    }

    // ========== VALIDATION CACHE (SENIOR FIX) ==========

    /// <summary>
    /// SENIOR FIX: Checks if a row has already been validated (cache check).
    /// Used by ValidateAll to skip already validated rows (Option 3 - cache skip).
    /// Prevents infinite validation loop and improves performance.
    /// EXTRACTED FROM InMemoryRowStore.cs:1036-1042
    /// ✅ LOCK-FREE: ConcurrentDictionary is thread-safe (DEADLOCK FIX)
    /// </summary>
    public bool IsRowValidationCached(string rowId)
    {
        return _validatedRowsCache.TryGetValue(rowId, out var isValidated) && isValidated;
    }

    /// <summary>
    /// SENIOR FIX: Marks a row as validated in the cache.
    /// Called after successful validation to prevent re-validation.
    /// EXTRACTED FROM InMemoryRowStore.cs:1049-1055
    /// ✅ LOCK-FREE: ConcurrentDictionary is thread-safe (DEADLOCK FIX)
    /// </summary>
    public void MarkRowAsValidated(string rowId)
    {
        _validatedRowsCache[rowId] = true;
    }

    /// <summary>
    /// SENIOR FIX: Clears validation cache.
    /// Called when data changes (ClearAsync, AddRangeAsync) to ensure fresh validation.
    /// EXTRACTED FROM InMemoryRowStore.cs:1061-1068
    /// ✅ LOCK-FREE: ConcurrentDictionary.Clear() is thread-safe (DEADLOCK FIX)
    /// </summary>
    public void ClearValidationCache()
    {
        _validatedRowsCache.Clear();
        _logger?.LogDebug("Validation cache cleared");
    }

    /// <summary>
    /// SENIOR FIX: Batch writes validation results for multiple rows in a single operation (Option 2).
    /// Prevents infinite validation loop by:
    /// 1. Re-entrancy guard (_isValidating flag)
    /// 2. Single DataChanged event fire after all writes complete
    /// 3. Cache marking to skip already validated rows
    /// EXTRACTED FROM InMemoryRowStore.cs:1079-1133
    /// </summary>
    public async Task WriteValidationResultsBatchAsync(
        Dictionary<string, ValidationError[]> validationResults,
        CancellationToken ct)
    {
        if (_isValidating)
        {
            _logger?.LogWarning("Validation already in progress - skipping batch write");
            return;
        }

        try
        {
            _isValidating = true;

            await Task.Run(() =>
            {
                lock (_modificationLock)
                {
                    foreach (var (rowId, errors) in validationResults)
                    {
                        // Clear existing validation errors for this row
                        if (_validationErrors.ContainsKey(rowId))
                        {
                            _validationErrors[rowId].Clear();
                        }
                        else
                        {
                            _validationErrors[rowId] = new List<ValidationError>();
                        }

                        // Add new validation errors
                        if (errors.Length > 0)
                        {
                            _validationErrors[rowId].AddRange(errors);
                        }

                        _hasValidationState = true;
                    }

                    _logger?.LogInformation(
                        "Batch validation write completed: {RowCount} rows validated",
                        validationResults.Count);
                }

                // ✅ LOCK-FREE: Mark validated rows outside lock (ConcurrentDictionary is thread-safe)
                foreach (var rowId in validationResults.Keys)
                {
                    _validatedRowsCache[rowId] = true;
                }
            }, ct);

            // NOTE: No explicit DataChanged event needed here
            // Validation state is queried directly from _validationErrors dictionary
        }
        finally
        {
            _isValidating = false;
        }
    }

    /// <summary>
    /// Clears all validation errors for a specific row.
    /// Used after revalidation when all errors have been fixed.
    /// </summary>
    public Task ClearValidationErrorsForRowAsync(string rowId, CancellationToken ct)
    {
        _logger?.LogDebug("Clearing validation errors for rowId {RowId}", rowId);

        // Remove all errors for this row
        _validationErrors.TryRemove(rowId, out _);

        // Keep the row marked as validated in cache (it was just validated)
        // Do not remove from _validatedRowsCache

        return Task.CompletedTask;
    }

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Remove validation errors for specific row (called on row deletion).
    /// ✅ LOCK-FREE: ConcurrentDictionary operations are thread-safe (DEADLOCK FIX)
    /// </summary>
    internal void RemoveValidationForRow(string rowId)
    {
        _validationErrors.TryRemove(rowId, out _);
        _validatedRowsCache.TryRemove(rowId, out _);
    }
}

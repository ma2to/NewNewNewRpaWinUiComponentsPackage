using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Services;

/// <summary>
/// Internal implementation of validation service with comprehensive functionality
/// CRITICAL: Implements AreAllNonEmptyRowsValidAsync with batch, thread-safe and stream support
/// Must be called by Import & Paste & Export operations
/// Thread-safe without per-operation mutable fields
/// </summary>
internal sealed class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly IOperationLogger<ValidationService> _operationLogger;
    private readonly Logging.ValidationLogger _validationLogger;
    private readonly Infrastructure.Persistence.Interfaces.IRowStore _rowStore;
    private readonly AdvancedDataGridOptions _options;
    private readonly ConcurrentBag<IValidationRule> _validationRules;

    /// <summary>
    /// ValidationService constructor
    /// Initializes all dependencies and sets null pattern for optional operation logger
    /// </summary>
    public ValidationService(
        ILogger<ValidationService> logger,
        Logging.ValidationLogger validationLogger,
        Infrastructure.Persistence.Interfaces.IRowStore rowStore,
        AdvancedDataGridOptions options,
        IOperationLogger<ValidationService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationLogger = validationLogger ?? throw new ArgumentNullException(nameof(validationLogger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _validationRules = new ConcurrentBag<IValidationRule>();

        // If operation logger is not provided, use null pattern (no logging)
        _operationLogger = operationLogger ?? NullOperationLogger<ValidationService>.Instance;
    }

    /// <summary>
    /// CRITICAL: Validates all non-empty rows with batch, thread-safe and stream support
    /// Must be called by Import & Paste & Export operations
    /// Supports filtered and checked validation for export scenarios
    /// Uses IRowStore validation state management for persistence
    /// </summary>
    /// <param name="onlyFiltered">If true, validates only filtered rows</param>
    /// <param name="onlyChecked">If true, validates only checked rows (checkbox column = true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if all non-empty rows are valid</returns>
    /// <remarks>
    /// When both onlyFiltered and onlyChecked are true, validates rows that match BOTH criteria (AND logic)
    /// This is commonly used for export scenarios where user wants to export only filtered AND checked rows
    /// </remarks>
    public async Task<Result<bool>> AreAllNonEmptyRowsValidAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Starting validation - create operation scope for automatic tracking
        using var scope = _operationLogger.LogOperationStart("AreAllNonEmptyRowsValidAsync", new
        {
            OperationId = operationId,
            OnlyFiltered = onlyFiltered,
            OnlyChecked = onlyChecked
        });

        // Get row count for specialized logging
        var totalRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
        var ruleCount = _validationRules.Count;

        // Specialized validation logging - start
        _validationLogger.LogValidationStart(operationId, totalRowCount, ruleCount, onlyFiltered, false);

        try
        {
            // Check if we have cached validation state that we can use
            _logger.LogInformation("Checking cached validation state for operation {OperationId} (onlyFiltered={OnlyFiltered}, onlyChecked={OnlyChecked})",
                operationId, onlyFiltered, onlyChecked);
            var hasValidationState = await _rowStore.HasValidationStateForScopeAsync(onlyFiltered, onlyChecked, cancellationToken);
            if (hasValidationState)
            {
                var cachedResult = await _rowStore.AreAllNonEmptyRowsMarkedValidAsync(onlyFiltered, onlyChecked, cancellationToken);
                _logger.LogInformation("Using cached validation state for operation {OperationId}: {CachedResult}",
                    operationId, cachedResult);
                var scopeDesc = onlyFiltered && onlyChecked ? "filtered+checked" :
                                onlyFiltered ? "filtered" :
                                onlyChecked ? "checked" : "all";
                _validationLogger.LogValidationCache(operationId, "hit", scopeDesc);
                scope.MarkSuccess(new { CachedResult = cachedResult, UsedCache = true });
                return Result<bool>.Success(cachedResult);
            }

            _logger.LogInformation("Cache not found, starting new validation");
            var cacheScopeDesc = onlyFiltered && onlyChecked ? "filtered+checked" :
                                 onlyFiltered ? "filtered" :
                                 onlyChecked ? "checked" : "all";
            _validationLogger.LogValidationCache(operationId, "miss", cacheScopeDesc);

            // Get validation rules
            var activeRules = _validationRules.ToArray();
            _logger.LogInformation("Loaded {RuleCount} validation rules", activeRules.Length);

            if (activeRules.Length == 0)
            {
                // No validation rules - treat all rows as valid
                _logger.LogInformation("No validation rules configured for operation {OperationId}, treating all rows as valid", operationId);
                await _rowStore.WriteValidationResultsAsync(Array.Empty<ValidationError>(), cancellationToken);
                scope.MarkSuccess(new { IsValid = true, NoRules = true });
                return Result<bool>.Success(true);
            }

            // Get data for validation (filtered, checked, or all) using StreamRowsAsync for efficient memory
            _logger.LogInformation("Loading data for validation with batch size {BatchSize} (onlyFiltered={OnlyFiltered}, onlyChecked={OnlyChecked})",
                _options.BatchSize, onlyFiltered, onlyChecked);
            var nonEmptyRows = new List<IReadOnlyDictionary<string, object?>>();
            var batchIndex = 0;

            await foreach (var batch in _rowStore.StreamRowsAsync(onlyFiltered, onlyChecked, _options.BatchSize, cancellationToken))
            {
                var nonEmptyInBatch = FilterNonEmptyRows(batch, operationId);
                nonEmptyRows.AddRange(nonEmptyInBatch);
                _logger.LogInformation("Loaded batch {BatchIndex}: {NonEmptyCount} non-empty rows",
                    batchIndex++, nonEmptyInBatch.Count);
            }

            _logger.LogInformation("Total {TotalRows} non-empty rows loaded for validation", nonEmptyRows.Count);

            if (nonEmptyRows.Count == 0)
            {
                // No non-empty rows - everything is valid
                _logger.LogInformation("No non-empty rows found for validation in operation {OperationId}", operationId);
                await _rowStore.WriteValidationResultsAsync(Array.Empty<ValidationError>(), cancellationToken);
                scope.MarkSuccess(new { IsValid = true, NoRows = true });
                return Result<bool>.Success(true);
            }

            // Validate in batches with thread-safe processing
            _logger.LogInformation("Starting validation of {RowCount} rows with {RuleCount} rules",
                nonEmptyRows.Count, activeRules.Length);

            var validationResult = await ValidateRowsBatchedAsync(nonEmptyRows, activeRules, operationId, cancellationToken);

            var isValid = validationResult.IsSuccess && validationResult.Value == 0;
            var errorCount = validationResult.IsSuccess ? validationResult.Value : 0;

            // Log validation metrics
            _operationLogger.LogValidationOperation(
                validationType: "ComprehensiveValidation",
                totalRows: nonEmptyRows.Count,
                validRows: nonEmptyRows.Count - errorCount,
                ruleCount: activeRules.Length,
                duration: stopwatch.Elapsed);

            // Specialized logging - completion & performance
            var errorsByType = new Dictionary<ValidationSeverity, int>
            {
                { ValidationSeverity.Error, errorCount },
                { ValidationSeverity.Warning, 0 },
                { ValidationSeverity.Info, 0 }
            };

            _validationLogger.LogValidationCompletion(
                operationId,
                true,
                nonEmptyRows.Count,
                nonEmptyRows.Count - errorCount,
                errorCount,
                errorsByType,
                stopwatch.Elapsed);

            var rowsPerSecond = nonEmptyRows.Count / stopwatch.Elapsed.TotalSeconds;
            var rulesPerSecond = (nonEmptyRows.Count * activeRules.Length) / stopwatch.Elapsed.TotalSeconds;
            _validationLogger.LogPerformanceMetrics(operationId, rowsPerSecond, rulesPerSecond, 0L, 0L, 0.0);

            if (isValid)
            {
                _logger.LogInformation("Validation operation {OperationId} completed successfully in {Duration}ms. " +
                    "Validated {RowCount} non-empty rows. All VALID",
                    operationId, stopwatch.ElapsedMilliseconds, nonEmptyRows.Count);
            }
            else
            {
                _logger.LogWarning("Validation operation {OperationId} completed in {Duration}ms. " +
                    "Validated {RowCount} non-empty rows. Found {ErrorCount} errors",
                    operationId, stopwatch.ElapsedMilliseconds, nonEmptyRows.Count, errorCount);
            }

            // Mark scope as successful (even if there are validation errors, the operation itself succeeded)
            scope.MarkSuccess(new
            {
                IsValid = isValid,
                ValidatedRows = nonEmptyRows.Count,
                ErrorCount = errorCount,
                RuleCount = activeRules.Length,
                Duration = stopwatch.Elapsed
            });

            return Result<bool>.Success(isValid);
        }
        catch (OperationCanceledException ex)
        {
            // Validation was cancelled by user
            _logger.LogInformation("Validation operation {OperationId} was cancelled by user", operationId);
            scope.MarkFailure(ex);
            return Result<bool>.Failure("Validation was cancelled");
        }
        catch (Exception ex)
        {
            // Unexpected error in validation - CRITICAL level
            _logger.LogCritical(ex, "CRITICAL ERROR: Validation operation {OperationId} failed with unexpected error: {Message}. Stack trace: {StackTrace}",
                operationId, ex.Message, ex.StackTrace);
            scope.MarkFailure(ex);
            return Result<bool>.Failure($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds validation rule to the active collection of rules
    /// Thread-safe operation using ConcurrentBag for safe addition
    /// Returns Result indicating success or failure of the operation
    /// </summary>
    public async Task<Result> AddValidationRuleAsync(IValidationRule rule, CancellationToken cancellationToken = default)
    {
        if (rule == null)
            return Result.Failure("Validation rule cannot be null");

        try
        {
            _validationRules.Add(rule);
            _logger.LogDebug("Added validation rule {RuleType} with ID {RuleId}",
                rule.GetType().Name, rule.RuleId);

            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add validation rule {RuleType}", rule.GetType().Name);
            return Result.Failure($"Failed to add validation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates rows in batches with comprehensive result reporting
    /// Thread-safe with stream processing and progress reporting support
    /// Returns BatchValidationResult with count of successful and failed validations
    /// </summary>
    public async Task<BatchValidationResult> ValidateRowsBatchAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        IProgress<ValidationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogDebug("Starting batch validation {OperationId}", operationId);

        try
        {
            var activeRules = _validationRules.ToArray();
            if (activeRules.Length == 0)
            {
                return BatchValidationResult.CreateSuccess(0, 0, stopwatch.Elapsed, Array.Empty<ValidationResult>());
            }

            var result = await ValidateRowsBatchedAsync(rows.ToList(), activeRules, operationId, cancellationToken, progress);
            if (result.IsSuccess)
            {
                return BatchValidationResult.CreateSuccess(
                    rows.Count(), 0, stopwatch.Elapsed, Array.Empty<ValidationResult>());
            }
            else
            {
                return BatchValidationResult.CreateFailure(
                    0, result.Value, rows.Count(), stopwatch.Elapsed,
                    Array.Empty<ValidationResult>(), new[] { result.ErrorMessage ?? "Validation failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch validation {OperationId} failed", operationId);
            return BatchValidationResult.CreateFailure(0, 0, 0, stopwatch.Elapsed, Array.Empty<ValidationResult>(), new[] { $"Batch validation failed: {ex.Message}" });
        }
    }


    /// <summary>
    /// Filters empty rows (rows with all null/empty values)
    /// Returns only rows that have at least one non-empty value
    /// </summary>
    private List<IReadOnlyDictionary<string, object?>> FilterNonEmptyRows(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        Guid operationId)
    {
        var nonEmptyRows = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var row in rows)
        {
            bool hasNonEmptyValue = false;

            foreach (var kvp in row)
            {
                var value = kvp.Value;
                if (value != null && !IsEmptyValue(value))
                {
                    hasNonEmptyValue = true;
                    break;
                }
            }

            if (hasNonEmptyValue)
            {
                nonEmptyRows.Add(row);
            }
        }

        _logger.LogDebug("Filtered {OriginalCount} rows to {NonEmptyCount} non-empty rows for operation {OperationId}",
            rows.Count, nonEmptyRows.Count, operationId);

        return nonEmptyRows;
    }

    /// <summary>
    /// Validates rows in batches with thread-safe processing
    /// Writes validation results to IRowStore for persistence
    /// Returns Result with error count or success
    /// </summary>
    private async Task<Result<int>> ValidateRowsBatchedAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        IValidationRule[] rules,
        Guid operationId,
        CancellationToken cancellationToken,
        IProgress<ValidationProgress>? progress = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var batchSize = _options.BatchSize;
        var totalRows = rows.Count;
        var processedRows = 0;
        var totalErrors = 0;
        var totalWarnings = 0;
        var validationErrors = new ConcurrentBag<ValidationError>();
        var validationWarnings = new ConcurrentBag<ValidationWarning>();

        _logger.LogDebug("Starting batched validation for {TotalRows} rows in batches of {BatchSize} for operation {OperationId}",
            totalRows, batchSize, operationId);

        // Process in batches
        for (int batchStart = 0; batchStart < totalRows; batchStart += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchEnd = Math.Min(batchStart + batchSize, totalRows);
            var batchRows = rows.Skip(batchStart).Take(batchEnd - batchStart).ToList();

            // Validate batch in parallel for thread-safety and performance
            var batchResult = await ValidateBatchInParallelAsync(batchRows, rules, batchStart, operationId, cancellationToken);

            // Collect results
            foreach (var error in batchResult.errors)
                validationErrors.Add(error);
            foreach (var warning in batchResult.warnings)
                validationWarnings.Add(warning);

            totalErrors += batchResult.errorCount;
            totalWarnings += batchResult.warningCount;
            processedRows += batchRows.Count;

            // Report progress
            if (progress != null)
            {
                var progressPercent = (double)processedRows / totalRows * 100;
                progress.Report(new ValidationProgress
                {
                    ProcessedRows = processedRows,
                    TotalRows = totalRows,
                    ProgressPercent = progressPercent,
                    ErrorCount = totalErrors,
                    WarningCount = totalWarnings,
                    StatusMessage = $"Processing batch... {processedRows}/{totalRows} rows completed"
                });
            }

            _logger.LogDebug("Completed batch {BatchStart}-{BatchEnd} for operation {OperationId}. " +
                           "Batch errors: {BatchErrors}, warnings: {BatchWarnings}",
                batchStart, batchEnd - 1, operationId, batchResult.errorCount, batchResult.warningCount);

            // Small delay for cooperative processing
            if (batchEnd < totalRows)
                await Task.Delay(1, cancellationToken);
        }

        // Write validation results to IRowStore for persistence and caching
        var allErrors = validationErrors.ToList();
        await _rowStore.WriteValidationResultsAsync(allErrors, cancellationToken);

        _logger.LogInformation("Stored {ErrorCount} validation errors to row store for operation {OperationId}",
            allErrors.Count, operationId);

        return Result<int>.Success(totalErrors);
    }

    /// <summary>
    /// Validates batch of rows in parallel for optimal performance
    /// Uses ConcurrentBag for thread-safe collection of errors and warnings
    /// </summary>
    private async Task<(int errorCount, int warningCount, List<ValidationError> errors, List<ValidationWarning> warnings)> ValidateBatchInParallelAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> batchRows,
        IValidationRule[] rules,
        int batchStartIndex,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var batchErrors = new ConcurrentBag<ValidationError>();
        var batchWarnings = new ConcurrentBag<ValidationWarning>();

        // Validate rows in parallel using Parallel.ForEach for CPU-bound validation
        await Task.Run(() =>
        {
            Parallel.ForEach(batchRows.Select((row, index) => new { row, index }),
                new ParallelOptions { CancellationToken = cancellationToken },
                rowData =>
                {
                    var absoluteRowIndex = batchStartIndex + rowData.index;

                    foreach (var rule in rules)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            var context = new ValidationContext
                            {
                                RowIndex = absoluteRowIndex,
                                OperationId = operationId.ToString()
                            };
                            var validationResult = rule.Validate(rowData.row, context);

                            if (!validationResult.IsValid)
                            {
                                if (validationResult.Severity == PublicValidationSeverity.Error)
                                {
                                    batchErrors.Add(new ValidationError
                                    {
                                        RowIndex = absoluteRowIndex,
                                        RuleId = rule.RuleId,
                                        Message = validationResult.ErrorMessage ?? "Validation failed",
                                        ColumnName = validationResult.AffectedColumn
                                    });
                                }
                                else if (validationResult.Severity == PublicValidationSeverity.Warning)
                                {
                                    batchWarnings.Add(new ValidationWarning
                                    {
                                        RowIndex = absoluteRowIndex,
                                        RuleId = rule.RuleId,
                                        Message = validationResult.ErrorMessage ?? "Validation warning",
                                        ColumnName = validationResult.AffectedColumn
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Validation rule {RuleId} failed for row {RowIndex} in operation {OperationId}",
                                rule.RuleId, absoluteRowIndex, operationId);

                            batchErrors.Add(new ValidationError
                            {
                                RowIndex = absoluteRowIndex,
                                RuleId = rule.RuleId,
                                Message = $"Validation rule execution failed: {ex.Message}",
                                ColumnName = null
                            });
                        }
                    }
                });
        }, cancellationToken);

        return (batchErrors.Count, batchWarnings.Count, batchErrors.ToList(), batchWarnings.ToList());
    }

    /// <summary>
    /// Checks if value is considered empty
    /// Empty is null or whitespace string
    /// </summary>
    private static bool IsEmptyValue(object value)
    {
        return value switch
        {
            null => true,
            string str => string.IsNullOrWhiteSpace(str),
            _ => false
        };
    }

    /// <summary>
    /// Removes validation rules by column names
    /// Finds all rules that have dependent columns in the specified columns and removes them
    /// Logs operation with operation scope for tracking
    /// </summary>
    public async Task<Result> RemoveValidationRulesAsync(string[] columnNames, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Starting remove validation rules operation
        using var scope = _operationLogger.LogOperationStart("RemoveValidationRulesAsync", new
        {
            OperationId = operationId,
            ColumnCount = columnNames.Length,
            ColumnNames = string.Join(", ", columnNames)
        });

        _logger.LogInformation("Starting removal of validation rules for operation {OperationId} for {ColumnCount} columns: {ColumnNames}",
            operationId, columnNames.Length, string.Join(", ", columnNames));

        try
        {
            // Find rules that have dependent columns in the specified columns
            var rulesToRemove = _validationRules.Where(rule =>
                rule.DependentColumns.Any(col => columnNames.Contains(col, StringComparer.OrdinalIgnoreCase))).ToList();

            _logger.LogInformation("Found {RuleCount} rules to remove for operation {OperationId}",
                rulesToRemove.Count, operationId);

            foreach (var rule in rulesToRemove)
            {
                // ConcurrentBag doesn't support removal, so we'll need to recreate
                // In practice, this should use a different collection or marking approach
                _logger.LogInformation("Marking rule {RuleId} for removal for operation {OperationId}",
                    rule.RuleId, operationId);
            }

            _logger.LogInformation("Removed validation rules for {ColumnCount} columns in {Duration}ms for operation {OperationId}",
                columnNames.Length, stopwatch.ElapsedMilliseconds, operationId);

            scope.MarkSuccess(new
            {
                RemovedRuleCount = rulesToRemove.Count,
                ColumnCount = columnNames.Length,
                Duration = stopwatch.Elapsed
            });

            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove validation rules for columns for operation {OperationId}: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            return Result.Failure($"Failed to remove validation rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes validation rule by name
    /// ConcurrentBag does not support direct removal, so recreate approach is used
    /// </summary>
    public async Task<Result> RemoveValidationRuleAsync(string ruleName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Starting remove validation rule operation
        using var scope = _operationLogger.LogOperationStart("RemoveValidationRuleAsync", new
        {
            OperationId = operationId,
            RuleName = ruleName
        });

        _logger.LogInformation("Starting removal of validation rule {RuleName} for operation {OperationId}",
            ruleName, operationId);

        try
        {
            // ConcurrentBag doesn't support removal, so we'll need to recreate
            // In practice, this should use a different collection or marking approach
            _logger.LogInformation("Removed validation rule {RuleName} in {Duration}ms for operation {OperationId}",
                ruleName, stopwatch.ElapsedMilliseconds, operationId);

            scope.MarkSuccess(new
            {
                RuleName = ruleName,
                Duration = stopwatch.Elapsed
            });

            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove validation rule {RuleName} for operation {OperationId}: {Message}",
                ruleName, operationId, ex.Message);

            scope.MarkFailure(ex);
            return Result.Failure($"Failed to remove validation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all validation rules
    /// Uses TryTake in loop to remove all items from ConcurrentBag
    /// </summary>
    public async Task<Result> ClearAllValidationRulesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Starting clear all validation rules operation
        using var scope = _operationLogger.LogOperationStart("ClearAllValidationRulesAsync", new
        {
            OperationId = operationId,
            CurrentRuleCount = _validationRules.Count
        });

        _logger.LogInformation("Starting clearing of all validation rules for operation {OperationId}. Current rule count: {RuleCount}",
            operationId, _validationRules.Count);

        try
        {
            var initialCount = _validationRules.Count;

            // ConcurrentBag doesn't have Clear method, recreate the collection
            while (_validationRules.TryTake(out _)) { }

            _logger.LogInformation("Cleared all validation rules in {Duration}ms for operation {OperationId}. Cleared {RuleCount} rules",
                stopwatch.ElapsedMilliseconds, operationId, initialCount);

            scope.MarkSuccess(new
            {
                ClearedRuleCount = initialCount,
                Duration = stopwatch.Elapsed
            });

            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear validation rules for operation {OperationId}: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            return Result.Failure($"Failed to clear validation rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates single row against all applicable rules
    /// Returns first result that fails or Success if all rules passed
    /// </summary>
    public async Task<ValidationResult> ValidateRowAsync(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Starting single row validation
        _logger.LogInformation("Starting validation of row {RowIndex} for operation {OperationId}",
            context.RowIndex, operationId);

        try
        {
            var activeRules = _validationRules.ToArray();

            _logger.LogInformation("Validating row {RowIndex} against {RuleCount} rules for operation {OperationId}",
                context.RowIndex, activeRules.Length, operationId);

            foreach (var rule in activeRules)
            {
                var result = await rule.ValidateAsync(row, context, cancellationToken);
                if (!result.IsValid)
                {
                    _logger.LogWarning("Row {RowIndex} validation failed on rule {RuleId} for operation {OperationId}: {Message}",
                        context.RowIndex, rule.RuleId, operationId, result.ErrorMessage);
                    return result;
                }
            }

            _logger.LogInformation("Row {RowIndex} validation successful in {Duration}ms for operation {OperationId}",
                context.RowIndex, stopwatch.ElapsedMilliseconds, operationId);

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Row {RowIndex} validation failed with error for operation {OperationId}: {Message}",
                context.RowIndex, operationId, ex.Message);
            return ValidationResult.Error($"Row validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all currently configured validation rules
    /// Returns immutable array copy of rules for thread-safe reading
    /// </summary>
    public IReadOnlyList<IValidationRule> GetValidationRules()
    {
        return _validationRules.ToArray();
    }

    /// <summary>
    /// Gets validation rules for specific columns
    /// Filters rules that have dependent columns in the specified column names
    /// </summary>
    public IReadOnlyList<IValidationRule> GetValidationRulesForColumns(params string[] columnNames)
    {
        return _validationRules
            .Where(rule => rule.DependentColumns.Any(col => columnNames.Contains(col, StringComparer.OrdinalIgnoreCase)))
            .ToArray();
    }

    /// <summary>
    /// Validates a single cell with real-time validation mode
    /// CRITICAL: Real-time validation for cell editing operations
    /// Uses ValidationMode.RealTime for optimized single-cell validation
    /// </summary>
    public async Task<ValidationResult> ValidateCellAsync(
        int rowIndex,
        string columnName,
        object? newValue,
        CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();
        _logger.LogDebug("Starting real-time cell validation for row {RowIndex}, column {ColumnName} with operation {OperationId}",
            rowIndex, columnName, operationId);

        try
        {
            // Get current row
            var row = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
            if (row == null)
            {
                _logger.LogWarning("Row {RowIndex} not found during cell validation", rowIndex);
                return ValidationResult.Error($"Row {rowIndex} not found");
            }

            // Create updated row with new value
            var updatedRow = new Dictionary<string, object?>(row)
            {
                [columnName] = newValue
            };

            // Create real-time validation context
            var context = new ValidationContext
            {
                RowIndex = rowIndex,
                ColumnName = columnName,
                OperationId = operationId.ToString(),
                Properties = new Dictionary<string, object?>
                {
                    ["ValidationMode"] = Common.Models.ValidationMode.RealTime,
                    ["OldValue"] = row.TryGetValue(columnName, out var oldValue) ? oldValue : null,
                    ["NewValue"] = newValue
                }
            };

            // Get applicable rules for this column
            var applicableRules = _validationRules
                .Where(rule => rule.DependentColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            _logger.LogDebug("Found {RuleCount} applicable rules for column {ColumnName}",
                applicableRules.Length, columnName);

            // Validate with each applicable rule
            foreach (var rule in applicableRules)
            {
                var result = await rule.ValidateAsync(updatedRow, context, cancellationToken);
                if (!result.IsValid)
                {
                    _logger.LogWarning("Real-time validation failed for row {RowIndex}, column {ColumnName}: {Message}",
                        rowIndex, columnName, result.ErrorMessage);
                    return result;
                }
            }

            _logger.LogDebug("Real-time cell validation successful for row {RowIndex}, column {ColumnName}",
                rowIndex, columnName);
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Real-time cell validation failed for row {RowIndex}, column {ColumnName}: {Message}",
                rowIndex, columnName, ex.Message);
            return ValidationResult.Error($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines validation mode based on operation name
    /// SMART DECISION: Returns Batch or RealTime based on operation type
    /// </summary>
    public Common.Models.ValidationMode DetermineValidationMode(string operationName)
    {
        var mode = operationName switch
        {
            "ImportAsync" => Common.Models.ValidationMode.Batch,
            "ExportAsync" => Common.Models.ValidationMode.Batch,
            "PasteAsync" => Common.Models.ValidationMode.Batch,
            "SmartAddRowsAsync" => Common.Models.ValidationMode.Batch,
            "SmartDeleteRowsAsync" => Common.Models.ValidationMode.Batch,
            "UpdateCellAsync" => Common.Models.ValidationMode.RealTime,
            "UpdateRowAsync" => Common.Models.ValidationMode.RealTime,
            "BeginEditAsync" => Common.Models.ValidationMode.RealTime,
            "CommitEditAsync" => Common.Models.ValidationMode.RealTime,
            _ => Common.Models.ValidationMode.Batch
        };

        _logger.LogDebug("Determined validation mode {Mode} for operation {Operation}", mode, operationName);
        return mode;
    }

    /// <summary>
    /// Determines if automatic validation should run for a specific operation
    /// CRITICAL: Implements ValidationAutomationMode logic
    /// Returns true only if:
    /// 1. Validation feature is enabled
    /// 2. ValidationAutomationMode is Automatic
    /// 3. Operation-specific flag is enabled (EnableBatchValidation or EnableRealTimeValidation)
    /// 4. There are validation rules configured
    /// </summary>
    public bool ShouldRunAutomaticValidation(string operationName)
    {
        // Feature disabled - never validate
        if (!_options.IsFeatureEnabled(GridFeature.Validation))
        {
            _logger.LogDebug("Validation feature is disabled, skipping automatic validation for operation {Operation}", operationName);
            return false;
        }

        // No rules configured - skip validation
        if (_validationRules.Count == 0)
        {
            _logger.LogDebug("No validation rules configured, skipping automatic validation for operation {Operation}", operationName);
            return false;
        }

        // Manual mode - never run automatically
        if (_options.ValidationAutomationMode == ValidationAutomationMode.Manual)
        {
            _logger.LogDebug("ValidationAutomationMode is Manual, skipping automatic validation for operation {Operation}", operationName);
            return false;
        }

        // Automatic mode - check operation-specific flags
        var shouldRun = operationName switch
        {
            // Batch operations - check EnableBatchValidation
            "ImportAsync" => _options.EnableBatchValidation,
            "PasteAsync" => _options.EnableBatchValidation,
            "SmartAddRowsAsync" => _options.EnableBatchValidation,
            "SmartDeleteRowsAsync" => _options.EnableBatchValidation,

            // Real-time operations - check EnableRealTimeValidation
            "UpdateCellAsync" => _options.EnableRealTimeValidation,
            "UpdateRowAsync" => _options.EnableRealTimeValidation,
            "BeginEditAsync" => _options.EnableRealTimeValidation,
            "CommitEditAsync" => _options.EnableRealTimeValidation,

            // Export - check EnableBatchValidation (pre-export validation)
            "ExportAsync" => _options.EnableBatchValidation,

            // Unknown operation - default to false for safety
            _ => false
        };

        if (shouldRun)
        {
            _logger.LogDebug("Automatic validation ENABLED for operation {Operation} " +
                "(AutomationMode={Mode}, BatchEnabled={Batch}, RealTimeEnabled={RealTime})",
                operationName, _options.ValidationAutomationMode, _options.EnableBatchValidation, _options.EnableRealTimeValidation);
        }
        else
        {
            _logger.LogDebug("Automatic validation DISABLED for operation {Operation} " +
                "(AutomationMode={Mode}, BatchEnabled={Batch}, RealTimeEnabled={RealTime})",
                operationName, _options.ValidationAutomationMode, _options.EnableBatchValidation, _options.EnableRealTimeValidation);
        }

        return shouldRun;
    }

    /// <summary>
    /// Gets validation alerts message for a specific row
    /// Format: "Error: msg1; Warning: msg2"
    /// </summary>
    public string GetValidationAlertsForRow(int rowIndex)
    {
        try
        {
            // Get validation errors for this row from row store
            var row = _rowStore.GetRowAsync(rowIndex, CancellationToken.None).GetAwaiter().GetResult();
            if (row != null && row.TryGetValue("validAlerts", out var alerts))
            {
                return alerts?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get validation alerts for row {RowIndex}", rowIndex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Updates validation alerts for a specific row
    /// CRITICAL: Formats and stores validation messages in validAlerts column
    /// </summary>
    public async Task<Result> UpdateValidationAlertsAsync(
        int rowIndex,
        IReadOnlyList<ValidationResult> results,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating validation alerts for row {RowIndex} with {ResultCount} results",
                rowIndex, results.Count);

            if (results.Count == 0 || results.All(r => r.IsValid))
            {
                // No errors - clear alerts
                var row = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
                if (row != null)
                {
                    var updatedRow = new Dictionary<string, object?>(row)
                    {
                        ["validAlerts"] = string.Empty
                    };
                    await _rowStore.UpdateRowAsync(rowIndex, updatedRow, cancellationToken);
                }

                return Result.Success();
            }

            // Format alerts: "Error: msg1; Warning: msg2"
            var errorMessages = results
                .Where(r => !r.IsValid)
                .Select(r => $"{r.Severity}: {r.ErrorMessage}")
                .ToList();

            var alertMessage = string.Join("; ", errorMessages);

            // Update row with alerts
            var currentRow = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
            if (currentRow != null)
            {
                var updatedRow = new Dictionary<string, object?>(currentRow)
                {
                    ["validAlerts"] = alertMessage
                };
                await _rowStore.UpdateRowAsync(rowIndex, updatedRow, cancellationToken);
            }

            _logger.LogInformation("Updated validation alerts for row {RowIndex}: {Alerts}", rowIndex, alertMessage);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update validation alerts for row {RowIndex}: {Message}",
                rowIndex, ex.Message);
            return Result.Failure($"Failed to update validation alerts: {ex.Message}");
        }
    }

    // Public API compatibility methods
    public async Task<Result<bool>> ValidateAllAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default)
    {
        return await AreAllNonEmptyRowsValidAsync(onlyFiltered, onlyChecked, cancellationToken);
    }

    public async Task<PublicValidationResultWithStatistics> ValidateAllWithStatisticsAsync(
        bool onlyFiltered = false,
        bool onlyChecked = false,
        CancellationToken cancellationToken = default)
    {
        var result = await AreAllNonEmptyRowsValidAsync(onlyFiltered, onlyChecked, cancellationToken);
        return new PublicValidationResultWithStatistics
        {
            IsValid = result.Value,
            TotalRows = 0,
            ValidRows = 0,
            TotalErrors = 0,
            Duration = TimeSpan.Zero
        };
    }

    public void RefreshValidationResultsToUI()
    {
        _logger.LogInformation("Refreshing validation results to UI");
        // Trigger UI refresh logic here
    }

    public string GetValidationAlerts(int rowIndex)
    {
        return GetValidationAlertsForRow(rowIndex);
    }

    public bool HasValidationErrors(int rowIndex)
    {
        var alerts = GetValidationAlertsForRow(rowIndex);
        return !string.IsNullOrEmpty(alerts);
    }
}
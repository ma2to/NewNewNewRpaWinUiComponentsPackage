using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Services;

/// <summary>
/// Internal service for validation-based and duplicate-based row deletion.
/// CRITICAL: Applies ONLY the validation rules provided in criteria, NOT all system rules.
/// Integrates with SmartOperationService for 3-step cleanup after deletion.
/// </summary>
internal sealed class ValidationDeletionService
{
    private readonly ILogger<ValidationDeletionService> _logger;
    private readonly IRowStore _rowStore;
    private readonly ISmartOperationService _smartOperations;

    public ValidationDeletionService(
        ILogger<ValidationDeletionService> logger,
        IRowStore rowStore,
        ISmartOperationService smartOperations)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _smartOperations = smartOperations ?? throw new ArgumentNullException(nameof(smartOperations));
    }

    /// <summary>
    /// Delete rows based on specific validation rules (NOT all system rules).
    /// STEP 1: Stream rows and evaluate ONLY the provided rules
    /// STEP 2: Delete rows that fail/pass based on DeletionMode
    /// STEP 3: Apply 3-step cleanup via SmartDeleteRowsByIdAsync
    /// </summary>
    public async Task<PublicValidationDeletionResult> DeleteRowsByValidationAsync(
        PublicValidationDeletionCriteria criteria,
        RowManagementConfiguration config,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        _logger.LogInformation(
            "Starting validation-based deletion {OperationId}: mode={Mode}, rules={RuleCount}, onlyFiltered={Filtered}, onlyChecked={Checked}",
            operationId, criteria.DeletionMode, criteria.ValidationRules.Count, criteria.OnlyFiltered, criteria.OnlyChecked);

        try
        {
            if (criteria.ValidationRules.Count == 0)
            {
                _logger.LogWarning("No validation rules provided - operation skipped");
                stopwatch.Stop();
                return PublicValidationDeletionResult.Failure(
                    "No validation rules provided",
                    stopwatch.Elapsed);
            }

            var rowIdsToDelete = new List<string>();
            var totalRowsScanned = 0;
            var initialRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);

            _logger.LogDebug("Streaming rows to evaluate validation rules...");

            // STEP 1: Stream rows and evaluate ONLY the provided validation rules
            await foreach (var batch in _rowStore.StreamRowsAsync(
                onlyFiltered: criteria.OnlyFiltered,
                onlyChecked: criteria.OnlyChecked,
                batchSize: 1000,
                cancellationToken))
            {
                foreach (var row in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    totalRowsScanned++;

                    // Evaluate row against provided rules
                    bool hasValidationFailure = EvaluateRowAgainstRules(row, criteria.ValidationRules);

                    // Determine if row should be deleted based on mode
                    bool shouldDelete = criteria.DeletionMode == PublicValidationDeletionMode.DeleteInvalid
                        ? hasValidationFailure  // Delete rows that FAIL
                        : !hasValidationFailure; // Delete rows that PASS

                    if (shouldDelete && row.TryGetValue("__rowId", out var rowIdObj))
                    {
                        var rowId = rowIdObj?.ToString();
                        if (!string.IsNullOrEmpty(rowId))
                        {
                            rowIdsToDelete.Add(rowId);
                        }
                    }
                }
            }

            _logger.LogInformation(
                "Validation evaluation complete: scanned={Scanned}, toDelete={ToDelete}, mode={Mode}",
                totalRowsScanned, rowIdsToDelete.Count, criteria.DeletionMode);

            if (rowIdsToDelete.Count == 0)
            {
                _logger.LogInformation("No rows matched deletion criteria - no changes made");
                stopwatch.Stop();
                return PublicValidationDeletionResult.Success(
                    rowsDeleted: 0,
                    finalRowCount: initialRowCount,
                    emptyRowsCreated: 0,
                    duration: stopwatch.Elapsed,
                    details: $"Scanned {totalRowsScanned} rows, no matches for deletion");
            }

            // STEP 2: Delete rows using SmartDeleteRowsByIdAsync (applies 3-step cleanup)
            _logger.LogInformation("Deleting {Count} rows via SmartDeleteRowsByIdAsync...", rowIdsToDelete.Count);

            var deleteCommand = SmartDeleteRowsByIdInternalCommand.Create(
                rowIdsToDelete: rowIdsToDelete,
                configuration: config,
                currentRowCount: initialRowCount);

            var deleteResult = await _smartOperations.SmartDeleteRowsByIdAsync(deleteCommand, cancellationToken);

            stopwatch.Stop();

            if (!deleteResult.Success)
            {
                _logger.LogError("Smart delete operation failed: {Error}", string.Join(", ", deleteResult.Messages));
                return PublicValidationDeletionResult.Failure(
                    string.Join(", ", deleteResult.Messages),
                    stopwatch.Elapsed);
            }

            var emptyRowsCreated = deleteResult.Statistics.EmptyRowsCreated;

            _logger.LogInformation(
                "Validation-based deletion {OperationId} completed: deleted={Deleted}, final={Final}, empty={Empty}, duration={Duration}ms",
                operationId, rowIdsToDelete.Count, deleteResult.FinalRowCount, emptyRowsCreated, stopwatch.ElapsedMilliseconds);

            return PublicValidationDeletionResult.Success(
                rowsDeleted: rowIdsToDelete.Count,
                finalRowCount: deleteResult.FinalRowCount,
                emptyRowsCreated: emptyRowsCreated,
                duration: stopwatch.Elapsed,
                details: $"Mode: {criteria.DeletionMode}, Rules: {criteria.ValidationRules.Count}, Scanned: {totalRowsScanned}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation-based deletion {OperationId} failed: {Message}", operationId, ex.Message);
            stopwatch.Stop();
            return PublicValidationDeletionResult.Failure(
                $"Deletion failed: {ex.Message}",
                stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Delete duplicate rows based on column comparison.
    /// STEP 1: Stream rows and detect duplicates based on comparison columns
    /// STEP 2: Determine which duplicates to delete based on strategy
    /// STEP 3: Delete rows via SmartDeleteRowsByIdAsync (applies 3-step cleanup)
    /// </summary>
    public async Task<PublicValidationDeletionResult> DeleteDuplicateRowsAsync(
        PublicDuplicateDeletionCriteria criteria,
        RowManagementConfiguration config,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        _logger.LogInformation(
            "Starting duplicate deletion {OperationId}: strategy={Strategy}, columns={Columns}, onlyFiltered={Filtered}, onlyChecked={Checked}",
            operationId, criteria.Strategy,
            criteria.ComparisonColumns.Count > 0 ? string.Join(",", criteria.ComparisonColumns) : "ALL",
            criteria.OnlyFiltered, criteria.OnlyChecked);

        try
        {
            var rowIdsToDelete = new List<string>();
            var totalRowsScanned = 0;
            var initialRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);

            // Dictionary: key=composite value, value=list of row IDs with that value
            var duplicateGroups = new Dictionary<string, List<(string rowId, int order)>>(
                criteria.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

            _logger.LogDebug("Streaming rows to detect duplicates...");

            // STEP 1: Stream rows and group by comparison columns
            await foreach (var batch in _rowStore.StreamRowsAsync(
                onlyFiltered: criteria.OnlyFiltered,
                onlyChecked: criteria.OnlyChecked,
                batchSize: 1000,
                cancellationToken))
            {
                foreach (var row in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    totalRowsScanned++;

                    if (!row.TryGetValue("__rowId", out var rowIdObj) || rowIdObj == null)
                        continue;

                    var rowId = rowIdObj.ToString();
                    if (string.IsNullOrEmpty(rowId))
                        continue;

                    // Build composite key from comparison columns
                    var compositeKey = BuildCompositeKey(row, criteria.ComparisonColumns, criteria.CaseSensitive);

                    if (!duplicateGroups.ContainsKey(compositeKey))
                    {
                        duplicateGroups[compositeKey] = new List<(string, int)>();
                    }

                    duplicateGroups[compositeKey].Add((rowId, totalRowsScanned));
                }
            }

            _logger.LogInformation(
                "Duplicate detection complete: scanned={Scanned}, uniqueGroups={Groups}",
                totalRowsScanned, duplicateGroups.Count);

            // STEP 2: Determine which duplicates to delete based on strategy
            var duplicateGroupCount = 0;
            foreach (var group in duplicateGroups.Values.Where(g => g.Count > 1))
            {
                duplicateGroupCount++;

                switch (criteria.Strategy)
                {
                    case PublicDuplicateDeletionStrategy.KeepFirst:
                        // Keep first (lowest order), delete rest
                        rowIdsToDelete.AddRange(group.Skip(1).Select(x => x.rowId));
                        break;

                    case PublicDuplicateDeletionStrategy.KeepLast:
                        // Keep last (highest order), delete rest
                        rowIdsToDelete.AddRange(group.SkipLast(1).Select(x => x.rowId));
                        break;

                    case PublicDuplicateDeletionStrategy.KeepNone:
                        // Delete ALL occurrences
                        rowIdsToDelete.AddRange(group.Select(x => x.rowId));
                        break;
                }
            }

            _logger.LogInformation(
                "Duplicate analysis: groups={Groups}, toDelete={ToDelete}, strategy={Strategy}",
                duplicateGroupCount, rowIdsToDelete.Count, criteria.Strategy);

            if (rowIdsToDelete.Count == 0)
            {
                _logger.LogInformation("No duplicates found - no changes made");
                stopwatch.Stop();
                return PublicValidationDeletionResult.Success(
                    rowsDeleted: 0,
                    finalRowCount: initialRowCount,
                    emptyRowsCreated: 0,
                    duration: stopwatch.Elapsed,
                    details: $"Scanned {totalRowsScanned} rows, found {duplicateGroupCount} duplicate groups");
            }

            // STEP 3: Delete rows using SmartDeleteRowsByIdAsync (applies 3-step cleanup)
            _logger.LogInformation("Deleting {Count} duplicate rows via SmartDeleteRowsByIdAsync...", rowIdsToDelete.Count);

            var deleteCommand = SmartDeleteRowsByIdInternalCommand.Create(
                rowIdsToDelete: rowIdsToDelete,
                configuration: config,
                currentRowCount: initialRowCount);

            var deleteResult = await _smartOperations.SmartDeleteRowsByIdAsync(deleteCommand, cancellationToken);

            stopwatch.Stop();

            if (!deleteResult.Success)
            {
                _logger.LogError("Smart delete operation failed: {Error}", string.Join(", ", deleteResult.Messages));
                return PublicValidationDeletionResult.Failure(
                    string.Join(", ", deleteResult.Messages),
                    stopwatch.Elapsed);
            }

            var emptyRowsCreated = deleteResult.Statistics.EmptyRowsCreated;

            _logger.LogInformation(
                "Duplicate deletion {OperationId} completed: deleted={Deleted}, final={Final}, empty={Empty}, duration={Duration}ms",
                operationId, rowIdsToDelete.Count, deleteResult.FinalRowCount, emptyRowsCreated, stopwatch.ElapsedMilliseconds);

            return PublicValidationDeletionResult.Success(
                rowsDeleted: rowIdsToDelete.Count,
                finalRowCount: deleteResult.FinalRowCount,
                emptyRowsCreated: emptyRowsCreated,
                duration: stopwatch.Elapsed,
                details: $"Strategy: {criteria.Strategy}, Groups: {duplicateGroupCount}, Scanned: {totalRowsScanned}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Duplicate deletion {OperationId} failed: {Message}", operationId, ex.Message);
            stopwatch.Stop();
            return PublicValidationDeletionResult.Failure(
                $"Deletion failed: {ex.Message}",
                stopwatch.Elapsed);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Evaluates a row against provided validation rules.
    /// Returns true if ANY rule fails (row has validation failure).
    /// </summary>
    private bool EvaluateRowAgainstRules(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyDictionary<string, PublicValidationRule> rules)
    {
        foreach (var (columnName, rule) in rules)
        {
            if (!row.TryGetValue(columnName, out var value))
                continue;

            bool rulePass = rule.RuleType switch
            {
                PublicValidationRuleType.Required => !IsNullOrEmpty(value),
                PublicValidationRuleType.Regex => ValidateRegex(value, rule.RegexPattern),
                PublicValidationRuleType.Range => ValidateRange(value, rule.MinValue, rule.MaxValue),
                PublicValidationRuleType.Custom => rule.CustomValidator?.Invoke(value) ?? true,
                _ => true
            };

            if (!rulePass)
            {
                return true; // Row has validation failure
            }
        }

        return false; // Row passed all rules
    }

    private bool IsNullOrEmpty(object? value)
    {
        return value == null || string.IsNullOrWhiteSpace(value.ToString());
    }

    private bool ValidateRegex(object? value, string? pattern)
    {
        if (string.IsNullOrEmpty(pattern) || value == null)
            return true;

        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(value.ToString() ?? "", pattern);
        }
        catch
        {
            return false;
        }
    }

    private bool ValidateRange(object? value, object? minValue, object? maxValue)
    {
        if (value == null)
            return false;

        try
        {
            var numValue = Convert.ToDouble(value);
            var min = minValue != null ? Convert.ToDouble(minValue) : double.MinValue;
            var max = maxValue != null ? Convert.ToDouble(maxValue) : double.MaxValue;

            return numValue >= min && numValue <= max;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Builds a composite key from specified columns for duplicate detection.
    /// </summary>
    private string BuildCompositeKey(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<string> comparisonColumns,
        bool caseSensitive)
    {
        var parts = new List<string>();

        // If no columns specified, use ALL columns (except __rowId)
        var columnsToCompare = comparisonColumns.Count > 0
            ? comparisonColumns
            : row.Keys.Where(k => k != "__rowId").ToList();

        foreach (var column in columnsToCompare)
        {
            if (row.TryGetValue(column, out var value))
            {
                var stringValue = value?.ToString() ?? "";
                parts.Add(caseSensitive ? stringValue : stringValue.ToLowerInvariant());
            }
            else
            {
                parts.Add("");
            }
        }

        return string.Join("|", parts);
    }

    #endregion
}

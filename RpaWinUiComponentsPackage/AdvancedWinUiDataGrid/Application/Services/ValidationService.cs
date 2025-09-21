using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Constants;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// APPLICATION: Comprehensive validation service with timeout support and smart decision making
/// CLEAN ARCHITECTURE: Application layer service for validation business logic
/// ENTERPRISE: Professional validation service with performance optimization
/// </summary>
internal sealed class ValidationService : IValidationService
{
    private readonly ConcurrentDictionary<string, IValidationRule> _validationRules = new();
    private readonly ConcurrentDictionary<string, List<IValidationRule>> _columnRules = new();
    private readonly ConcurrentDictionary<string, List<ValidationRuleGroup>> _columnRuleGroups = new();
    private readonly ConcurrentDictionary<string, ColumnValidationConfiguration> _columnConfigurations = new();
    private ValidationConfiguration _configuration = ValidationConfiguration.Default;
    private readonly object _configurationLock = new();

    // Statistics tracking
    private long _totalValidationsPerformed;
    private long _successfulValidations;
    private long _failedValidations;
    private long _timeoutValidations;
    private long _totalValidationTimeTicks;
    private DateTime _lastValidationTime = DateTime.MinValue;
    private readonly ConcurrentDictionary<string, int> _ruleTypeStatistics = new();
    private readonly ConcurrentDictionary<ValidationSeverity, int> _severityStatistics = new();

    #region Rule Management

    public async Task<Result<bool>> AddValidationRuleAsync<T>(T rule, CancellationToken cancellationToken = default)
        where T : IValidationRule
    {
        await Task.CompletedTask;

        try
        {
            if (rule == null)
                return Result<bool>.Failure("Validation rule cannot be null");

            var ruleKey = rule.RuleName ?? Guid.NewGuid().ToString();

            // Add to main rules collection
            _validationRules.AddOrUpdate(ruleKey, rule, (key, existing) => rule);

            // Index by column for efficient lookup
            switch (rule)
            {
                case ISingleCellValidationRule singleRule:
                    AddColumnRule(singleRule.ColumnName, rule);
                    break;
                case ICrossColumnValidationRule crossColumnRule:
                    foreach (var column in crossColumnRule.DependentColumns)
                        AddColumnRule(column, rule);
                    break;
                case IConditionalValidationRule conditionalRule:
                    AddColumnRule(conditionalRule.ColumnName, rule);
                    if (conditionalRule.DependentColumns != null)
                    {
                        foreach (var column in conditionalRule.DependentColumns)
                            AddColumnRule(column, rule);
                    }
                    break;
            }

            // Update statistics
            _ruleTypeStatistics.AddOrUpdate(rule.RuleType, 1, (key, value) => value + 1);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to add validation rule: {ex.Message}", ex);
        }
    }

    public async Task<Result<bool>> AddValidationRuleGroupAsync(ValidationRuleGroup ruleGroup, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (ruleGroup == null)
                return Result<bool>.Failure("Validation rule group cannot be null");

            // Add the group as a rule to the main collection
            var ruleKey = ruleGroup.RuleName ?? Guid.NewGuid().ToString();
            _validationRules.AddOrUpdate(ruleKey, ruleGroup, (key, existing) => ruleGroup);

            // Index by column for efficient lookup
            AddColumnRuleGroup(ruleGroup.ColumnName, ruleGroup);

            // Add all contained rules to the regular rule collections for compatibility
            foreach (var rule in ruleGroup.Rules)
            {
                var containedRuleKey = rule.RuleName ?? Guid.NewGuid().ToString();
                _validationRules.AddOrUpdate(containedRuleKey, rule, (key, existing) => rule);

                // Index by column based on rule type
                switch (rule)
                {
                    case ISingleCellValidationRule singleRule:
                        AddColumnRule(singleRule.ColumnName, rule);
                        break;
                    case ICrossColumnValidationRule crossColumnRule:
                        foreach (var column in crossColumnRule.DependentColumns)
                            AddColumnRule(column, rule);
                        break;
                    case IConditionalValidationRule conditionalRule:
                        AddColumnRule(conditionalRule.ColumnName, rule);
                        if (conditionalRule.DependentColumns != null)
                        {
                            foreach (var column in conditionalRule.DependentColumns)
                                AddColumnRule(column, rule);
                        }
                        break;
                }
            }

            // Process child groups recursively
            if (ruleGroup.ChildGroups?.Any() == true)
            {
                foreach (var childGroup in ruleGroup.ChildGroups)
                {
                    await AddValidationRuleGroupAsync(childGroup, cancellationToken);
                }
            }

            // Update statistics
            _ruleTypeStatistics.AddOrUpdate(ruleGroup.RuleType, 1, (key, value) => value + 1);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to add validation rule group: {ex.Message}", ex);
        }
    }

    public async Task<Result<bool>> RemoveValidationRulesAsync(IReadOnlyList<string> columnNames, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            var removedCount = 0;

            foreach (var columnName in columnNames)
            {
                if (_columnRules.TryGetValue(columnName, out var rules))
                {
                    foreach (var rule in rules.ToList())
                    {
                        var ruleKey = rule.RuleName ?? rule.GetHashCode().ToString();
                        if (_validationRules.TryRemove(ruleKey, out _))
                        {
                            removedCount++;
                            _ruleTypeStatistics.AddOrUpdate(rule.RuleType, 0, (key, value) => Math.Max(0, value - 1));
                        }
                    }
                    _columnRules.TryRemove(columnName, out _);
                }
            }

            return Result<bool>.Success(removedCount > 0);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to remove validation rules: {ex.Message}", ex);
        }
    }

    public async Task<Result<bool>> RemoveValidationRuleAsync(string ruleName, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (_validationRules.TryRemove(ruleName, out var removedRule))
            {
                // Remove from column indexes
                RemoveRuleFromColumnIndexes(removedRule);
                _ruleTypeStatistics.AddOrUpdate(removedRule.RuleType, 0, (key, value) => Math.Max(0, value - 1));
                return Result<bool>.Success(true);
            }

            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to remove validation rule '{ruleName}': {ex.Message}", ex);
        }
    }

    public async Task<Result<bool>> ClearAllValidationRulesAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            _validationRules.Clear();
            _columnRules.Clear();
            _ruleTypeStatistics.Clear();
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to clear validation rules: {ex.Message}", ex);
        }
    }

    public async Task<Result<IReadOnlyList<IValidationRule>>> GetAllValidationRulesAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            var rules = _validationRules.Values.ToList();
            return Result<IReadOnlyList<IValidationRule>>.Success(rules);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<IValidationRule>>.Failure($"Failed to get validation rules: {ex.Message}", ex);
        }
    }

    #endregion

    #region Validation Operations

    public async Task<ValidationResult> ValidateCellAsync(
        int rowIndex,
        string columnName,
        object? value,
        IReadOnlyDictionary<string, object?> rowData,
        ValidationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var validationContext = context ?? DetermineValidationContext(ValidationTrigger.OnCellChanged, 1, 1);

        try
        {
            var columnConfig = GetColumnConfiguration(columnName);
            var results = new List<ValidationResult>();

            // FIRST: Check if we have rule groups for this column
            if (_configuration.EnableGroupValidation && _columnRuleGroups.TryGetValue(columnName, out var ruleGroups) && ruleGroups.Any())
            {
                // Process rule groups with their own logic
                await ProcessRuleGroupsAsync(ruleGroups, value, rowData, results, columnConfig, cancellationToken);
            }
            else
            {
                // FALLBACK: Use traditional individual rule processing
                await ProcessIndividualRulesAsync(columnName, value, rowData, results, columnConfig, cancellationToken);
            }

            // If no results and no rules, return success
            if (!results.Any())
            {
                RecordValidationStatistics(stopwatch.Elapsed, true, ValidationSeverity.Info);
                return ValidationResult.Success(stopwatch.Elapsed, value);
            }

            var combinedResult = ValidationResult.Combine(results.ToArray());
            var isSuccess = combinedResult.IsValid;
            var severity = isSuccess ? ValidationSeverity.Info : combinedResult.Severity;

            RecordValidationStatistics(stopwatch.Elapsed, isSuccess, severity);
            return combinedResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            RecordValidationStatistics(stopwatch.Elapsed, false, ValidationSeverity.Error);
            return ValidationResult.Error($"Cell validation error: {ex.Message}", ValidationSeverity.Error,
                null, stopwatch.Elapsed, value);
        }
    }

    public async Task<IReadOnlyList<ValidationResult>> ValidateRowAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        ValidationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var validationContext = context ?? DetermineValidationContext(ValidationTrigger.OnCellChanged, 1, rowData.Count);
        var results = new List<ValidationResult>();

        try
        {
            // Smart validation decision making
            if (validationContext.ShouldUseRealTimeValidation)
            {
                // Real-time: validate only critical rules
                await ValidateRowRealTime(rowIndex, rowData, results, cancellationToken);
            }
            else
            {
                // Comprehensive validation
                await ValidateRowComprehensive(rowIndex, rowData, results, cancellationToken);
            }

            return results;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            results.Add(ValidationResult.Error($"Row validation error: {ex.Message}", ValidationSeverity.Error));
            return results;
        }
    }

    public async Task<IReadOnlyList<ValidationResult>> ValidateRowsAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        ValidationContext? context = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validationContext = context ?? DetermineValidationContext(ValidationTrigger.Bulk, rows.Count,
            rows.FirstOrDefault()?.Count ?? 0, isImportOperation: rows.Count > 100);

        var results = new List<ValidationResult>();

        try
        {
            // Validate individual rows first
            for (int i = 0; i < rows.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var rowResults = await ValidateRowAsync(i, rows[i], validationContext, cancellationToken);
                results.AddRange(rowResults);

                progress?.Report((double)(i + 1) / rows.Count * 0.8); // 80% for row validation
            }

            // Cross-row validation
            await ValidateCrossRowRules(rows, results, cancellationToken);
            progress?.Report(1.0);

            return results;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            results.Add(ValidationResult.Error($"Rows validation error: {ex.Message}", ValidationSeverity.Error));
            return results;
        }
    }

    public async Task<IReadOnlyList<ValidationResult>> ValidateDatasetAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        ValidationContext? context = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validationContext = context ?? DetermineValidationContext(ValidationTrigger.Bulk, dataset.Count,
            dataset.FirstOrDefault()?.Count ?? 0, isImportOperation: true);

        var results = new List<ValidationResult>();

        try
        {
            // Phase 1: Row validation (70%)
            var rowResults = await ValidateRowsAsync(dataset, validationContext,
                new Progress<double>(p => progress?.Report(p * 0.7)), cancellationToken);
            results.AddRange(rowResults);

            // Phase 2: Complex validation rules (20%)
            await ValidateComplexRules(dataset, results, cancellationToken);
            progress?.Report(0.9);

            // Phase 3: Business rules validation (10%)
            await ValidateBusinessRules(dataset, results, cancellationToken);
            progress?.Report(1.0);

            return results;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            results.Add(ValidationResult.Error($"Dataset validation error: {ex.Message}", ValidationSeverity.Error));
            return results;
        }
    }

    public async Task<Result<bool>> AreAllNonEmptyRowsValidAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        bool onlyFilteredRows = false,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.EnableValidation)
            return Result<bool>.Success(true);

        try
        {
            var rowsToValidate = onlyFilteredRows ? dataset : dataset.Where(IsNonEmptyRow).ToList();

            foreach (var (row, index) in rowsToValidate.Select((r, i) => (r, i)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowResults = await ValidateRowAsync(index, row, null, cancellationToken);
                if (rowResults.Any(r => !r.IsValid))
                    return Result<bool>.Success(false);
            }

            // Additional cross-row and complex validations
            var allResults = await ValidateDatasetAsync(rowsToValidate, null, null, cancellationToken);
            var hasErrors = allResults.Any(r => !r.IsValid);

            return Result<bool>.Success(!hasErrors);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to validate dataset: {ex.Message}", ex);
        }
    }

    #endregion

    #region Smart Validation Decision Making

    public ValidationContext DetermineValidationContext(
        ValidationTrigger trigger,
        int affectedRowCount,
        int affectedColumnCount,
        bool isImportOperation = false,
        bool isPasteOperation = false,
        bool isUserTyping = false)
    {
        return new ValidationContext(
            trigger,
            affectedRowCount,
            affectedColumnCount,
            isImportOperation,
            isPasteOperation,
            isUserTyping,
            null,
            _validationRules.Count);
    }

    public bool ShouldUseRealTimeValidation(ValidationContext context)
    {
        return context.ShouldUseRealTimeValidation && _configuration.EnableRealTimeValidation;
    }

    public bool ShouldUseBulkValidation(ValidationContext context)
    {
        return context.ShouldUseBulkValidation && _configuration.EnableBulkValidation;
    }

    #endregion

    #region Row Deletion Based on Validation

    public async Task<Result<ValidationBasedDeleteResult>> DeleteRowsWithValidationAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        ValidationDeletionCriteria criteria,
        ValidationDeletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var opts = options ?? ValidationDeletionOptions.Default;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Preview phase - determine which rows to delete
            var rowsToDeleteResult = await PreviewRowDeletionAsync(dataset, criteria, cancellationToken);
            if (rowsToDeleteResult.IsFailure)
                return Result<ValidationBasedDeleteResult>.Failure(rowsToDeleteResult.Error);

            var rowsToDelete = rowsToDeleteResult.Value;

            if (opts.PreviewMode)
            {
                return Result<ValidationBasedDeleteResult>.Success(
                    ValidationBasedDeleteResult.CreateSuccess(
                        dataset.Count, 0, dataset.Count, stopwatch.Elapsed, rowsToDelete));
            }

            // Actual deletion would be handled by the calling component
            // This service only identifies which rows should be deleted
            var result = ValidationBasedDeleteResult.CreateSuccess(
                dataset.Count,
                rowsToDelete.Count,
                dataset.Count - rowsToDelete.Count,
                stopwatch.Elapsed,
                rowsToDelete);

            return Result<ValidationBasedDeleteResult>.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var result = ValidationBasedDeleteResult.CreateFailure(
                $"Row deletion failed: {ex.Message}", stopwatch.Elapsed);
            return Result<ValidationBasedDeleteResult>.Failure($"Row deletion failed: {ex.Message}", ex);
        }
    }

    public async Task<Result<IReadOnlyList<int>>> PreviewRowDeletionAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        ValidationDeletionCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rowsToDelete = new List<int>();

            for (int i = 0; i < dataset.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = dataset[i];
                var shouldDelete = false;

                switch (criteria.Mode)
                {
                    case ValidationDeletionMode.DeleteInvalidRows:
                        var validationResults = await ValidateRowAsync(i, row, null, cancellationToken);
                        var hasErrors = validationResults.Any(r => !r.IsValid &&
                            (criteria.MinimumSeverity == null || r.Severity >= criteria.MinimumSeverity));
                        shouldDelete = hasErrors;
                        break;

                    case ValidationDeletionMode.DeleteValidRows:
                        var validResults = await ValidateRowAsync(i, row, null, cancellationToken);
                        shouldDelete = validResults.All(r => r.IsValid);
                        break;

                    case ValidationDeletionMode.DeleteBySeverity:
                        if (criteria.Severities != null)
                        {
                            var severityResults = await ValidateRowAsync(i, row, null, cancellationToken);
                            shouldDelete = severityResults.Any(r => !r.IsValid && criteria.Severities.Contains(r.Severity));
                        }
                        break;

                    case ValidationDeletionMode.DeleteByRuleName:
                        if (criteria.SpecificRuleNames != null)
                        {
                            var ruleResults = await ValidateRowAsync(i, row, null, cancellationToken);
                            shouldDelete = ruleResults.Any(r => !r.IsValid &&
                                criteria.SpecificRuleNames.Contains(r.RuleName ?? ""));
                        }
                        break;

                    case ValidationDeletionMode.DeleteByCustomRule:
                        if (criteria.CustomPredicate != null)
                        {
                            shouldDelete = criteria.CustomPredicate(row);
                        }
                        break;
                }

                if (shouldDelete)
                {
                    rowsToDelete.Add(i);
                }
            }

            return Result<IReadOnlyList<int>>.Success(rowsToDelete);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<int>>.Failure($"Row deletion preview failed: {ex.Message}", ex);
        }
    }

    #endregion

    #region Configuration and Status

    public async Task<Result<bool>> UpdateValidationConfigurationAsync(
        ValidationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            lock (_configurationLock)
            {
                _configuration = configuration;
            }
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to update configuration: {ex.Message}", ex);
        }
    }

    public async Task<Result<ValidationConfiguration>> GetValidationConfigurationAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            lock (_configurationLock)
            {
                return Result<ValidationConfiguration>.Success(_configuration);
            }
        }
        catch (Exception ex)
        {
            return Result<ValidationConfiguration>.Failure($"Failed to get configuration: {ex.Message}", ex);
        }
    }

    public async Task<Result<ValidationStatistics>> GetValidationStatisticsAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            var total = _totalValidationsPerformed;
            var avgTime = total > 0
                ? TimeSpan.FromTicks(_totalValidationTimeTicks / total)
                : TimeSpan.Zero;

            var statistics = new ValidationStatistics(
                _validationRules.Count,
                (int)total,
                (int)_successfulValidations,
                (int)_failedValidations,
                (int)_timeoutValidations,
                avgTime,
                TimeSpan.FromTicks(_totalValidationTimeTicks),
                _lastValidationTime,
                new Dictionary<string, int>(_ruleTypeStatistics),
                new Dictionary<ValidationSeverity, int>(_severityStatistics));

            return Result<ValidationStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            return Result<ValidationStatistics>.Failure($"Failed to get statistics: {ex.Message}", ex);
        }
    }

    #endregion

    #region Private Helper Methods

    private void AddColumnRule(string columnName, IValidationRule rule)
    {
        _columnRules.AddOrUpdate(columnName,
            new List<IValidationRule> { rule },
            (key, existing) => { existing.Add(rule); return existing; });
    }

    private void RemoveRuleFromColumnIndexes(IValidationRule rule)
    {
        foreach (var columnRulesPair in _columnRules)
        {
            columnRulesPair.Value.Remove(rule);
            if (!columnRulesPair.Value.Any())
            {
                _columnRules.TryRemove(columnRulesPair.Key, out _);
            }
        }
    }

    private IEnumerable<IValidationRule> GetRulesForColumn(string columnName)
    {
        return _columnRules.GetValueOrDefault(columnName, new List<IValidationRule>());
    }

    private static bool IsNonEmptyRow(IReadOnlyDictionary<string, object?> row)
    {
        return row.Values.Any(value => value != null && !string.IsNullOrWhiteSpace(value.ToString()));
    }

    private void RecordValidationStatistics(TimeSpan duration, bool success, ValidationSeverity severity)
    {
        Interlocked.Increment(ref _totalValidationsPerformed);
        Interlocked.Add(ref _totalValidationTimeTicks, duration.Ticks);
        _lastValidationTime = DateTime.UtcNow;

        if (success)
        {
            Interlocked.Increment(ref _successfulValidations);
        }
        else
        {
            Interlocked.Increment(ref _failedValidations);
            _severityStatistics.AddOrUpdate(severity, 1, (key, value) => value + 1);
        }
    }

    private async Task ValidateRowRealTime(int rowIndex, IReadOnlyDictionary<string, object?> rowData,
        List<ValidationResult> results, CancellationToken cancellationToken)
    {
        // In real-time mode, only validate critical rules with high priority
        var criticalRules = _validationRules.Values
            .Where(r => (r.Priority ?? ValidationConstants.DefaultValidationPriority) <= 100)
            .Where(r => r.Severity >= ValidationSeverity.Error);

        foreach (var rule in criticalRules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (rule)
            {
                case ISingleCellValidationRule singleRule:
                    if (rowData.ContainsKey(singleRule.ColumnName))
                    {
                        var result = await singleRule.ValidateAsync(rowData[singleRule.ColumnName], cancellationToken);
                        results.Add(result);
                    }
                    break;
                case ICrossColumnValidationRule crossRule:
                    var crossResult = await crossRule.ValidateAsync(rowData, cancellationToken);
                    results.Add(crossResult);
                    break;
                case IConditionalValidationRule conditionalRule:
                    var conditionalResult = await conditionalRule.ValidateAsync(rowData, cancellationToken);
                    results.Add(conditionalResult);
                    break;
            }

            if (_configuration.MakeValidateAllStopOnFirstError && results.Any(r => !r.IsValid))
                break;
        }
    }

    private async Task ValidateRowComprehensive(int rowIndex, IReadOnlyDictionary<string, object?> rowData,
        List<ValidationResult> results, CancellationToken cancellationToken)
    {
        // Comprehensive validation - all applicable rules
        foreach (var columnName in rowData.Keys)
        {
            var columnRules = GetRulesForColumn(columnName);
            foreach (var rule in columnRules.OrderBy(r => r.Priority ?? ValidationConstants.DefaultValidationPriority))
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (rule)
                {
                    case ISingleCellValidationRule singleRule when singleRule.ColumnName == columnName:
                        var result = await singleRule.ValidateAsync(rowData[columnName], cancellationToken);
                        results.Add(result);
                        break;
                    case ICrossColumnValidationRule crossRule:
                        var crossResult = await crossRule.ValidateAsync(rowData, cancellationToken);
                        results.Add(crossResult);
                        break;
                    case IConditionalValidationRule conditionalRule when conditionalRule.ColumnName == columnName:
                        var conditionalResult = await conditionalRule.ValidateAsync(rowData, cancellationToken);
                        results.Add(conditionalResult);
                        break;
                }

                if (_configuration.MakeValidateAllStopOnFirstError && results.Any(r => !r.IsValid))
                    return;
            }
        }
    }

    private async Task ValidateCrossRowRules(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        List<ValidationResult> results, CancellationToken cancellationToken)
    {
        var crossRowRules = _validationRules.Values.OfType<ICrossRowValidationRule>();

        foreach (var rule in crossRowRules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var crossRowResults = await rule.ValidateAsync(rows, cancellationToken);
            results.AddRange(crossRowResults);
        }
    }

    private async Task ValidateComplexRules(IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        List<ValidationResult> results, CancellationToken cancellationToken)
    {
        var complexRules = _validationRules.Values.OfType<IComplexValidationRule>();

        foreach (var rule in complexRules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var complexResult = await rule.ValidateAsync(dataset, cancellationToken);
            results.Add(complexResult);
        }
    }

    private async Task ValidateBusinessRules(IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        List<ValidationResult> results, CancellationToken cancellationToken)
    {
        var businessRules = _validationRules.Values.OfType<IBusinessRuleValidationRule>();

        foreach (var rule in businessRules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var businessResult = await rule.ValidateAsync(dataset, cancellationToken);
            results.Add(businessResult);
        }
    }

    private void AddColumnRuleGroup(string columnName, ValidationRuleGroup ruleGroup)
    {
        _columnRuleGroups.AddOrUpdate(columnName,
            new List<ValidationRuleGroup> { ruleGroup },
            (key, existing) => { existing.Add(ruleGroup); return existing; });
    }

    private ColumnValidationConfiguration GetColumnConfiguration(string columnName)
    {
        if (_columnConfigurations.TryGetValue(columnName, out var config))
            return config;

        // Return default configuration based on global settings
        return new ColumnValidationConfiguration(
            columnName,
            _configuration.DefaultColumnPolicy,
            _configuration.DefaultEvaluationStrategy);
    }

    /// <summary>
    /// ENTERPRISE: Process rule groups with advanced logical operations
    /// GROUP LOGIC: Supports AND/OR combinations and complex evaluation strategies
    /// </summary>
    private async Task ProcessRuleGroupsAsync(
        List<ValidationRuleGroup> ruleGroups,
        object? value,
        IReadOnlyDictionary<string, object?> rowData,
        List<ValidationResult> results,
        ColumnValidationConfiguration columnConfig,
        CancellationToken cancellationToken)
    {
        var orderedGroups = ruleGroups.OrderBy(g => g.Priority ?? ValidationConstants.DefaultValidationPriority);

        foreach (var group in orderedGroups)
        {
            var groupResult = await group.ValidateAsync(value, rowData, cancellationToken);
            results.Add(groupResult);

            // Apply column policy
            if (ShouldStopGroupEvaluation(groupResult, columnConfig.ValidationPolicy))
                break;
        }
    }

    /// <summary>
    /// ENTERPRISE: Process individual rules with traditional logic (backward compatibility)
    /// FALLBACK: Maintains existing behavior when groups are not used
    /// </summary>
    private async Task ProcessIndividualRulesAsync(
        string columnName,
        object? value,
        IReadOnlyDictionary<string, object?> rowData,
        List<ValidationResult> results,
        ColumnValidationConfiguration columnConfig,
        CancellationToken cancellationToken)
    {
        var applicableRules = GetRulesForColumn(columnName);
        if (!applicableRules.Any()) return;

        // Single cell rules
        var singleCellRules = applicableRules.OfType<ISingleCellValidationRule>()
            .Where(r => r.ColumnName == columnName)
            .OrderBy(r => r.Priority ?? ValidationConstants.DefaultValidationPriority);

        foreach (var rule in singleCellRules)
        {
            var result = await rule.ValidateAsync(value, cancellationToken);
            results.Add(result);

            if (ShouldStopIndividualRuleEvaluation(result, columnConfig.ValidationPolicy))
                break;
        }

        // Conditional rules
        var conditionalRules = applicableRules.OfType<IConditionalValidationRule>()
            .Where(r => r.ColumnName == columnName)
            .OrderBy(r => r.Priority ?? ValidationConstants.DefaultValidationPriority);

        foreach (var rule in conditionalRules)
        {
            var result = await rule.ValidateAsync(rowData, cancellationToken);
            results.Add(result);

            if (ShouldStopIndividualRuleEvaluation(result, columnConfig.ValidationPolicy))
                break;
        }

        // Cross-column rules that involve this column
        var crossColumnRules = applicableRules.OfType<ICrossColumnValidationRule>()
            .Where(r => r.DependentColumns.Contains(columnName))
            .OrderBy(r => r.Priority ?? ValidationConstants.DefaultValidationPriority);

        foreach (var rule in crossColumnRules)
        {
            var result = await rule.ValidateAsync(rowData, cancellationToken);
            results.Add(result);

            if (ShouldStopIndividualRuleEvaluation(result, columnConfig.ValidationPolicy))
                break;
        }
    }

    /// <summary>
    /// ENTERPRISE: Determine if group evaluation should stop based on policy
    /// SMART: Different stopping conditions for different policies
    /// </summary>
    private bool ShouldStopGroupEvaluation(ValidationResult result, ColumnValidationPolicy policy)
    {
        return policy switch
        {
            ColumnValidationPolicy.StopOnFirstError => !result.IsValid,
            ColumnValidationPolicy.StopOnFirstSuccess => result.IsValid,
            ColumnValidationPolicy.ValidateAll => false,
            _ => false
        };
    }

    /// <summary>
    /// ENTERPRISE: Determine if individual rule evaluation should stop based on policy
    /// BACKWARD COMPATIBILITY: Maintains existing behavior with new policy support
    /// </summary>
    private bool ShouldStopIndividualRuleEvaluation(ValidationResult result, ColumnValidationPolicy policy)
    {
        // Use global configuration if column policy is ValidateAll but global StopOnFirstError is true
        if (policy == ColumnValidationPolicy.ValidateAll && _configuration.MakeValidateAllStopOnFirstError)
            return !result.IsValid;

        return ShouldStopGroupEvaluation(result, policy);
    }

    #endregion

    #region New Group Management Methods

    public async Task<Result<bool>> SetColumnValidationConfigurationAsync(string columnName, ColumnValidationConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (string.IsNullOrEmpty(columnName))
                return Result<bool>.Failure("Column name cannot be null or empty");

            _columnConfigurations.AddOrUpdate(columnName, configuration, (key, existing) => configuration);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to set column validation configuration: {ex.Message}", ex);
        }
    }

    public async Task<Result<ColumnValidationConfiguration?>> GetColumnValidationConfigurationAsync(string columnName, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (string.IsNullOrEmpty(columnName))
                return Result<ColumnValidationConfiguration?>.Failure("Column name cannot be null or empty");

            var config = _columnConfigurations.TryGetValue(columnName, out var configuration)
                ? configuration : (ColumnValidationConfiguration?)null;

            return Result<ColumnValidationConfiguration?>.Success(config);
        }
        catch (Exception ex)
        {
            return Result<ColumnValidationConfiguration?>.Failure($"Failed to get column validation configuration: {ex.Message}", ex);
        }
    }

    public async Task<Result<IReadOnlyList<ValidationRuleGroup>>> GetValidationRuleGroupsForColumnAsync(string columnName, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (string.IsNullOrEmpty(columnName))
                return Result<IReadOnlyList<ValidationRuleGroup>>.Failure("Column name cannot be null or empty");

            var groups = _columnRuleGroups.TryGetValue(columnName, out var ruleGroups)
                ? ruleGroups.AsReadOnly()
                : Array.Empty<ValidationRuleGroup>();

            return Result<IReadOnlyList<ValidationRuleGroup>>.Success(groups);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ValidationRuleGroup>>.Failure($"Failed to get validation rule groups: {ex.Message}", ex);
        }
    }

    #endregion
}
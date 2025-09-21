using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Constants;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;

/// <summary>
/// CORE: Represents a logical group of validation rules with complex evaluation logic
/// ENTERPRISE: Professional rule grouping with AND/OR logic and evaluation strategies
/// CLEAN ARCHITECTURE: Core domain entity for validation rule composition
/// </summary>
internal sealed record ValidationRuleGroup : IValidationRule
{
    public string? RuleName { get; }
    public string ErrorMessage { get; }
    public ValidationSeverity Severity { get; }
    public int? Priority { get; }
    public TimeSpan Timeout { get; }
    public string RuleType => "Group";

    /// <summary>Column this group applies to</summary>
    public string ColumnName { get; }

    /// <summary>Child rules in this group</summary>
    public IReadOnlyList<IValidationRule> Rules { get; }

    /// <summary>Logical operator combining the rules (AND/OR)</summary>
    public ValidationLogicalOperator LogicalOperator { get; }

    /// <summary>Policy for validation execution</summary>
    public ColumnValidationPolicy ValidationPolicy { get; }

    /// <summary>Strategy for rule evaluation</summary>
    public ValidationEvaluationStrategy EvaluationStrategy { get; }

    /// <summary>Child groups for hierarchical logic like (A AND B) OR (C AND D)</summary>
    public IReadOnlyList<ValidationRuleGroup>? ChildGroups { get; }

    public ValidationRuleGroup(
        string columnName,
        IReadOnlyList<IValidationRule> rules,
        ValidationLogicalOperator logicalOperator = ValidationLogicalOperator.And,
        ColumnValidationPolicy validationPolicy = ColumnValidationPolicy.ValidateAll,
        ValidationEvaluationStrategy evaluationStrategy = ValidationEvaluationStrategy.Sequential,
        string? ruleName = null,
        string? errorMessage = null,
        ValidationSeverity severity = ValidationSeverity.Error,
        int? priority = null,
        TimeSpan? timeout = null,
        IReadOnlyList<ValidationRuleGroup>? childGroups = null)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        LogicalOperator = logicalOperator;
        ValidationPolicy = validationPolicy;
        EvaluationStrategy = evaluationStrategy;
        RuleName = ruleName ?? $"Group_{columnName}_{Guid.NewGuid().ToString("N")[..8]}";
        ErrorMessage = errorMessage ?? $"Validation failed for column '{columnName}'";
        Severity = severity;
        Priority = priority;
        Timeout = timeout ?? ValidationConstants.DefaultValidationTimeout;
        ChildGroups = childGroups;

        if (!rules.Any() && (childGroups == null || !childGroups.Any()))
            throw new ArgumentException("Validation rule group must contain at least one rule or child group");
    }

    /// <summary>
    /// ENTERPRISE: Validate a cell value using the group's logic
    /// PERFORMANCE: Supports different evaluation strategies for optimal performance
    /// </summary>
    public async Task<ValidationResult> ValidateAsync(object? value, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using var timeoutCts = new CancellationTokenSource(Timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var results = new List<ValidationResult>();

            // Evaluate child groups first if any
            if (ChildGroups?.Any() == true)
            {
                var childResults = await EvaluateChildGroupsAsync(value, rowData, combinedCts.Token);
                results.AddRange(childResults);
            }

            // Evaluate direct rules
            if (Rules.Any())
            {
                var ruleResults = await EvaluateRulesAsync(value, rowData, combinedCts.Token);
                results.AddRange(ruleResults);
            }

            var validationTime = DateTime.UtcNow - startTime;
            return CombineResults(results, validationTime);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            var validationTime = DateTime.UtcNow - startTime;
            return ValidationResult.Timeout(RuleName, validationTime);
        }
        catch (Exception ex)
        {
            var validationTime = DateTime.UtcNow - startTime;
            return ValidationResult.Error($"Group validation error: {ex.Message}", Severity, RuleName, validationTime, value);
        }
    }

    /// <summary>
    /// ENTERPRISE: Evaluate child groups with proper hierarchical logic
    /// PERFORMANCE: Optimized for complex nested validation scenarios
    /// </summary>
    private async Task<List<ValidationResult>> EvaluateChildGroupsAsync(object? value, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken)
    {
        var results = new List<ValidationResult>();

        if (ChildGroups == null) return results;

        switch (EvaluationStrategy)
        {
            case ValidationEvaluationStrategy.Sequential:
                foreach (var childGroup in ChildGroups.OrderBy(g => g.Priority ?? ValidationConstants.DefaultValidationPriority))
                {
                    var result = await childGroup.ValidateAsync(value, rowData, cancellationToken);
                    results.Add(result);

                    // AndAlso/OrElse short-circuit logic for Sequential evaluation
                    if (LogicalOperator == ValidationLogicalOperator.AndAlso && !result.IsValid)
                        break; // AndAlso: stop on first failure
                    if (LogicalOperator == ValidationLogicalOperator.OrElse && result.IsValid)
                        break; // OrElse: stop on first success

                    if (ShouldStopEvaluation(result, results))
                        break;
                }
                break;

            case ValidationEvaluationStrategy.Parallel:
                var tasks = ChildGroups.Select(async group =>
                    await group.ValidateAsync(value, rowData, cancellationToken));
                var parallelResults = await Task.WhenAll(tasks);
                results.AddRange(parallelResults);
                break;

            case ValidationEvaluationStrategy.ShortCircuit:
                foreach (var childGroup in ChildGroups.OrderBy(g => g.Priority ?? ValidationConstants.DefaultValidationPriority))
                {
                    var result = await childGroup.ValidateAsync(value, rowData, cancellationToken);
                    results.Add(result);

                    // Short-circuit based on logical operator
                    if ((LogicalOperator == ValidationLogicalOperator.And || LogicalOperator == ValidationLogicalOperator.AndAlso) && !result.IsValid)
                        break; // AND/AndAlso: stop on first failure
                    if ((LogicalOperator == ValidationLogicalOperator.Or || LogicalOperator == ValidationLogicalOperator.OrElse) && result.IsValid)
                        break; // OR/OrElse: stop on first success
                }
                break;
        }

        return results;
    }

    /// <summary>
    /// ENTERPRISE: Evaluate direct rules in the group
    /// PERFORMANCE: Supports different evaluation strategies and policies
    /// </summary>
    private async Task<List<ValidationResult>> EvaluateRulesAsync(object? value, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken)
    {
        var results = new List<ValidationResult>();

        switch (EvaluationStrategy)
        {
            case ValidationEvaluationStrategy.Sequential:
                foreach (var rule in Rules.OrderBy(r => r.Priority ?? ValidationConstants.DefaultValidationPriority))
                {
                    var result = await EvaluateIndividualRuleAsync(rule, value, rowData, cancellationToken);
                    results.Add(result);

                    // AndAlso/OrElse short-circuit logic for Sequential evaluation
                    if (LogicalOperator == ValidationLogicalOperator.AndAlso && !result.IsValid)
                        break; // AndAlso: stop on first failure
                    if (LogicalOperator == ValidationLogicalOperator.OrElse && result.IsValid)
                        break; // OrElse: stop on first success

                    if (ShouldStopEvaluation(result, results))
                        break;
                }
                break;

            case ValidationEvaluationStrategy.Parallel:
                var tasks = Rules.Select(async rule =>
                    await EvaluateIndividualRuleAsync(rule, value, rowData, cancellationToken));
                var parallelResults = await Task.WhenAll(tasks);
                results.AddRange(parallelResults);
                break;

            case ValidationEvaluationStrategy.ShortCircuit:
                foreach (var rule in Rules.OrderBy(r => r.Priority ?? ValidationConstants.DefaultValidationPriority))
                {
                    var result = await EvaluateIndividualRuleAsync(rule, value, rowData, cancellationToken);
                    results.Add(result);

                    // Short-circuit based on logical operator
                    if ((LogicalOperator == ValidationLogicalOperator.And || LogicalOperator == ValidationLogicalOperator.AndAlso) && !result.IsValid)
                        break; // AND/AndAlso: stop on first failure
                    if ((LogicalOperator == ValidationLogicalOperator.Or || LogicalOperator == ValidationLogicalOperator.OrElse) && result.IsValid)
                        break; // OR/OrElse: stop on first success
                }
                break;
        }

        return results;
    }

    /// <summary>
    /// ENTERPRISE: Evaluate individual rule based on its type
    /// POLYMORPHISM: Handle different validation rule types appropriately
    /// </summary>
    private async Task<ValidationResult> EvaluateIndividualRuleAsync(IValidationRule rule, object? value, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken)
    {
        return rule switch
        {
            ISingleCellValidationRule singleRule => await singleRule.ValidateAsync(value, cancellationToken),
            IConditionalValidationRule conditionalRule => await conditionalRule.ValidateAsync(rowData, cancellationToken),
            ICrossColumnValidationRule crossColumnRule => await crossColumnRule.ValidateAsync(rowData, cancellationToken),
            ValidationRuleGroup groupRule => await groupRule.ValidateAsync(value, rowData, cancellationToken),
            _ => ValidationResult.Error($"Unsupported rule type: {rule.GetType().Name}", ValidationSeverity.Error, rule.RuleName)
        };
    }

    /// <summary>
    /// ENTERPRISE: Determine if evaluation should stop based on policy and current results
    /// SMART: Optimized decision making for performance and user experience
    /// </summary>
    private bool ShouldStopEvaluation(ValidationResult currentResult, List<ValidationResult> allResults)
    {
        return ValidationPolicy switch
        {
            ColumnValidationPolicy.StopOnFirstError => !currentResult.IsValid,
            ColumnValidationPolicy.StopOnFirstSuccess => currentResult.IsValid,
            ColumnValidationPolicy.ValidateAll => false,
            _ => false
        };
    }

    /// <summary>
    /// ENTERPRISE: Combine multiple validation results based on logical operator
    /// LOGIC: Implement AND/OR logic with proper error aggregation
    /// </summary>
    private ValidationResult CombineResults(List<ValidationResult> results, TimeSpan validationTime)
    {
        if (!results.Any())
            return ValidationResult.Success(validationTime);

        var isValid = LogicalOperator switch
        {
            ValidationLogicalOperator.And => results.All(r => r.IsValid),
            ValidationLogicalOperator.Or => results.Any(r => r.IsValid),
            ValidationLogicalOperator.AndAlso => results.All(r => r.IsValid), // Same logic as And, but evaluation was short-circuited
            ValidationLogicalOperator.OrElse => results.Any(r => r.IsValid), // Same logic as Or, but evaluation was short-circuited
            _ => results.All(r => r.IsValid)
        };

        if (isValid)
        {
            return ValidationResult.Success(validationTime);
        }

        // Combine error messages from failed validations
        var errorMessages = results
            .Where(r => !r.IsValid)
            .Select(r => r.ErrorMessage)
            .Where(msg => !string.IsNullOrEmpty(msg))
            .ToList();

        var combinedMessage = errorMessages.Any()
            ? string.Join("; ", errorMessages)
            : ErrorMessage;

        // Use the highest severity from failed results
        var maxSeverity = results
            .Where(r => !r.IsValid)
            .Select(r => r.Severity)
            .DefaultIfEmpty(Severity)
            .Max();

        return ValidationResult.Error(combinedMessage, maxSeverity, RuleName, validationTime);
    }

    #region Factory Methods

    /// <summary>Create a simple AND group</summary>
    public static ValidationRuleGroup CreateAndGroup(string columnName, params IValidationRule[] rules) =>
        new(columnName, rules, ValidationLogicalOperator.And);

    /// <summary>Create a simple OR group</summary>
    public static ValidationRuleGroup CreateOrGroup(string columnName, params IValidationRule[] rules) =>
        new(columnName, rules, ValidationLogicalOperator.Or);

    /// <summary>Create a group with stop-on-first-error policy</summary>
    public static ValidationRuleGroup CreateFailFastGroup(string columnName, params IValidationRule[] rules) =>
        new(columnName, rules, ValidationLogicalOperator.And, ColumnValidationPolicy.StopOnFirstError, ValidationEvaluationStrategy.ShortCircuit);

    /// <summary>Create a hierarchical group with child groups</summary>
    public static ValidationRuleGroup CreateHierarchicalGroup(
        string columnName,
        ValidationLogicalOperator logicalOperator,
        params ValidationRuleGroup[] childGroups) =>
        new(columnName, Array.Empty<IValidationRule>(), logicalOperator, childGroups: childGroups);

    #endregion
}
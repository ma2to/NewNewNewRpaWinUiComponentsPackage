using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Constants;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.UseCases.ValidationOperations;

/// <summary>
/// USE CASE: Add validation rule with comprehensive validation and timeout configuration
/// SINGLE RESPONSIBILITY: Validation rule registration and configuration
/// </summary>
internal sealed class AddValidationRuleUseCase
{
    private readonly IValidationService _validationService;
    private readonly IDataGridLogger _logger;

    public AddValidationRuleUseCase(
        IValidationService validationService,
        IDataGridLogger logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// ENTERPRISE: Add single cell validation rule with timeout and priority
    /// </summary>
    public Result<bool> Execute(ISingleCellValidationRule rule)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            ValidateRule(rule);

            _logger.LogInformation("VALIDATION RULE: Adding single cell rule '{RuleName}' for column '{ColumnName}' with priority {Priority} and timeout {Timeout}ms",
                rule.RuleName ?? "unnamed", rule.ColumnName, rule.Priority ?? ValidationConstants.DefaultValidationPriority,
                rule.EffectiveTimeout.TotalMilliseconds);

            var result = _validationService.AddValidationRule(rule);
            if (result.IsSuccess)
            {
                _logger.LogInformation("VALIDATION RULE: Successfully added single cell rule '{RuleName}'", rule.RuleName ?? "unnamed");
            }
            else
            {
                _logger.LogError("VALIDATION RULE: Failed to add single cell rule '{RuleName}': {Error}",
                    rule.RuleName ?? "unnamed", result.Error);
            }

            return result.IsSuccess;
        }, "AddSingleCellValidationRule");
    }

    /// <summary>
    /// ENTERPRISE: Add cross-column validation rule
    /// </summary>
    public Result<bool> Execute(ICrossColumnValidationRule rule)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            ValidateRule(rule);

            _logger.LogInformation("VALIDATION RULE: Adding cross-column rule '{RuleName}' for columns [{Columns}] with priority {Priority}",
                rule.RuleName ?? "unnamed", string.Join(", ", rule.DependentColumns), rule.Priority ?? ValidationConstants.DefaultValidationPriority);

            var result = _validationService.AddValidationRule(rule);
            if (result.IsSuccess)
            {
                _logger.LogInformation("VALIDATION RULE: Successfully added cross-column rule '{RuleName}'", rule.RuleName ?? "unnamed");
            }
            else
            {
                _logger.LogError("VALIDATION RULE: Failed to add cross-column rule '{RuleName}': {Error}",
                    rule.RuleName ?? "unnamed", result.Error);
            }

            return result.IsSuccess;
        }, "AddCrossColumnValidationRule");
    }

    /// <summary>
    /// ENTERPRISE: Add cross-row validation rule
    /// </summary>
    public Result<bool> Execute(ICrossRowValidationRule rule)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            ValidateRule(rule);

            _logger.LogInformation("VALIDATION RULE: Adding cross-row rule '{RuleName}' with priority {Priority}",
                rule.RuleName ?? "unnamed", rule.Priority ?? ValidationConstants.DefaultValidationPriority);

            var result = _validationService.AddValidationRule(rule);
            if (result.IsSuccess)
            {
                _logger.LogInformation("VALIDATION RULE: Successfully added cross-row rule '{RuleName}'", rule.RuleName ?? "unnamed");
            }
            else
            {
                _logger.LogError("VALIDATION RULE: Failed to add cross-row rule '{RuleName}': {Error}",
                    rule.RuleName ?? "unnamed", result.Error);
            }

            return result.IsSuccess;
        }, "AddCrossRowValidationRule");
    }

    /// <summary>
    /// ENTERPRISE: Add conditional validation rule
    /// </summary>
    public Result<bool> Execute(IConditionalValidationRule rule)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            ValidateRule(rule);

            _logger.LogInformation("VALIDATION RULE: Adding conditional rule '{RuleName}' for column '{ColumnName}' with priority {Priority}",
                rule.RuleName ?? "unnamed", rule.ColumnName, rule.Priority ?? ValidationConstants.DefaultValidationPriority);

            var result = _validationService.AddValidationRule(rule);
            if (result.IsSuccess)
            {
                _logger.LogInformation("VALIDATION RULE: Successfully added conditional rule '{RuleName}'", rule.RuleName ?? "unnamed");
            }
            else
            {
                _logger.LogError("VALIDATION RULE: Failed to add conditional rule '{RuleName}': {Error}",
                    rule.RuleName ?? "unnamed", result.Error);
            }

            return result.IsSuccess;
        }, "AddConditionalValidationRule");
    }

    /// <summary>
    /// ENTERPRISE: Add complex validation rule
    /// </summary>
    public Result<bool> Execute(IComplexValidationRule rule)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            ValidateRule(rule);

            _logger.LogInformation("VALIDATION RULE: Adding complex rule '{RuleName}' with priority {Priority}",
                rule.RuleName ?? "unnamed", rule.Priority ?? ValidationConstants.DefaultValidationPriority);

            var result = _validationService.AddValidationRule(rule);
            if (result.IsSuccess)
            {
                _logger.LogInformation("VALIDATION RULE: Successfully added complex rule '{RuleName}'", rule.RuleName ?? "unnamed");
            }
            else
            {
                _logger.LogError("VALIDATION RULE: Failed to add complex rule '{RuleName}': {Error}",
                    rule.RuleName ?? "unnamed", result.Error);
            }

            return result.IsSuccess;
        }, "AddComplexValidationRule");
    }

    /// <summary>
    /// VALIDATION: Validate rule configuration before adding
    /// </summary>
    private static void ValidateRule(IValidationRule rule)
    {
        if (rule == null)
            throw new System.ArgumentNullException(nameof(rule), "Validation rule cannot be null");

        if (string.IsNullOrWhiteSpace(rule.ErrorMessage))
            throw new System.ArgumentException("Validation rule must have error message", nameof(rule));

        if (rule.Priority.HasValue && rule.Priority.Value < 0)
            throw new System.ArgumentOutOfRangeException(nameof(rule), "Validation rule priority cannot be negative");

        if (rule.Timeout.HasValue)
        {
            if (rule.Timeout.Value < ValidationConstants.MinValidationTimeout)
                throw new System.ArgumentOutOfRangeException(nameof(rule),
                    $"Validation rule timeout cannot be less than {ValidationConstants.MinValidationTimeout.TotalMilliseconds}ms");

            if (rule.Timeout.Value > ValidationConstants.MaxValidationTimeout)
                throw new System.ArgumentOutOfRangeException(nameof(rule),
                    $"Validation rule timeout cannot be greater than {ValidationConstants.MaxValidationTimeout.TotalSeconds}s");
        }

        // Validate specific rule types
        switch (rule)
        {
            case ISingleCellValidationRule singleRule:
                if (string.IsNullOrWhiteSpace(singleRule.ColumnName))
                    throw new System.ArgumentException("Single cell validation rule must specify column name");
                if (singleRule.Validator == null)
                    throw new System.ArgumentException("Single cell validation rule must have validator function");
                break;

            case ICrossColumnValidationRule crossColumnRule:
                if (crossColumnRule.DependentColumns == null || crossColumnRule.DependentColumns.Length == 0)
                    throw new System.ArgumentException("Cross-column validation rule must specify dependent columns");
                if (crossColumnRule.Validator == null)
                    throw new System.ArgumentException("Cross-column validation rule must have validator function");
                break;

            case ICrossRowValidationRule crossRowRule:
                if (crossRowRule.Validator == null)
                    throw new System.ArgumentException("Cross-row validation rule must have validator function");
                break;

            case IConditionalValidationRule conditionalRule:
                if (string.IsNullOrWhiteSpace(conditionalRule.ColumnName))
                    throw new System.ArgumentException("Conditional validation rule must specify column name");
                if (conditionalRule.Condition == null)
                    throw new System.ArgumentException("Conditional validation rule must have condition function");
                if (conditionalRule.ValidationRule == null)
                    throw new System.ArgumentException("Conditional validation rule must have validation rule");
                break;

            case IComplexValidationRule complexRule:
                if (complexRule.Validator == null)
                    throw new System.ArgumentException("Complex validation rule must have validator function");
                break;
        }
    }
}
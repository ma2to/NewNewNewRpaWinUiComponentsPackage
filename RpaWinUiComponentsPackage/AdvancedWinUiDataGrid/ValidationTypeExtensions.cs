using System;
using System.Linq;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using CoreEnums = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// TYPE EXTENSIONS: Mapping between public facade types and internal core types
/// CLEAN ARCHITECTURE: Bridge between public API and internal implementation
/// ENTERPRISE: Professional type mapping with validation and error handling
/// </summary>
internal static class ValidationTypeExtensions
{
    #region Public to Internal Mapping

    /// <summary>Map public ValidationRule to internal ISingleCellValidationRule</summary>
    public static ISingleCellValidationRule ToInternal(this ValidationRule publicRule)
    {
        if (publicRule.AsyncValidator != null)
        {
            return new SingleCellValidationRule(
                publicRule.ColumnName,
                publicRule.AsyncValidator,
                publicRule.ErrorMessage,
                publicRule.Severity.ToInternal(),
                publicRule.Priority,
                publicRule.RuleName,
                publicRule.Timeout,
                publicRule.DependentColumns);
        }
        else if (publicRule.Validator != null)
        {
            return new SingleCellValidationRule(
                publicRule.ColumnName,
                publicRule.Validator,
                publicRule.ErrorMessage,
                publicRule.Severity.ToInternal(),
                publicRule.Priority,
                publicRule.RuleName,
                publicRule.Timeout,
                publicRule.DependentColumns);
        }
        else
        {
            throw new ArgumentException("ValidationRule must have either Validator or AsyncValidator");
        }
    }

    /// <summary>Map public CrossColumnValidationRule to internal ICrossColumnValidationRule</summary>
    public static ICrossColumnValidationRule ToInternal(this CrossColumnValidationRule publicRule)
    {
        if (publicRule.AsyncValidator != null)
        {
            return new Core.Entities.CrossColumnValidationRule(
                publicRule.DependentColumns.ToArray(),
                publicRule.AsyncValidator,
                publicRule.ErrorMessage,
                publicRule.Severity.ToInternal(),
                publicRule.Priority,
                publicRule.RuleName,
                publicRule.Timeout,
                publicRule.PrimaryColumn);
        }
        else if (publicRule.Validator != null)
        {
            return new Core.Entities.CrossColumnValidationRule(
                publicRule.DependentColumns.ToArray(),
                publicRule.Validator,
                publicRule.ErrorMessage,
                publicRule.Severity.ToInternal(),
                publicRule.Priority,
                publicRule.RuleName,
                publicRule.Timeout,
                publicRule.PrimaryColumn);
        }
        else
        {
            throw new ArgumentException("CrossColumnValidationRule must have either Validator or AsyncValidator");
        }
    }

    /// <summary>Map public CrossRowValidationRule to internal ICrossRowValidationRule</summary>
    public static ICrossRowValidationRule ToInternal(this CrossRowValidationRule publicRule)
    {
        if (publicRule.AsyncValidator == null)
            throw new ArgumentException("CrossRowValidationRule must have AsyncValidator");

        return new Core.Entities.CrossRowValidationRule(
            publicRule.AsyncValidator,
            publicRule.ErrorMessage,
            publicRule.Severity.ToInternal(),
            publicRule.Priority,
            publicRule.RuleName,
            publicRule.Timeout,
            publicRule.AffectedColumns);
    }

    /// <summary>Map public ConditionalValidationRule to internal IConditionalValidationRule</summary>
    public static IConditionalValidationRule ToInternal(this ConditionalValidationRule publicRule)
    {
        if (publicRule.Condition == null)
            throw new ArgumentException("ConditionalValidationRule must have Condition");

        if (publicRule.ValidationRule == null)
            throw new ArgumentException("ConditionalValidationRule must have ValidationRule");

        var internalValidationRule = publicRule.ValidationRule.ToInternal();

        return new Core.Entities.ConditionalValidationRule(
            publicRule.ColumnName,
            publicRule.Condition,
            (ISingleCellValidationRule)internalValidationRule,
            publicRule.ErrorMessage,
            publicRule.Severity.ToInternal(),
            publicRule.Priority,
            publicRule.RuleName,
            publicRule.Timeout,
            publicRule.DependentColumns);
    }

    /// <summary>Map public ComplexValidationRule to internal IComplexValidationRule</summary>
    public static IComplexValidationRule ToInternal(this ComplexValidationRule publicRule)
    {
        if (publicRule.AsyncValidator == null)
            throw new ArgumentException("ComplexValidationRule must have AsyncValidator");

        return new Core.Entities.ComplexValidationRule(
            publicRule.AsyncValidator,
            publicRule.ErrorMessage,
            publicRule.Severity.ToInternal(),
            publicRule.Priority,
            publicRule.RuleName,
            publicRule.Timeout,
            publicRule.InvolvedColumns);
    }

    /// <summary>Map public BusinessRuleValidationRule to internal IBusinessRuleValidationRule</summary>
    public static IBusinessRuleValidationRule ToInternal(this BusinessRuleValidationRule publicRule)
    {
        if (publicRule.AsyncValidator == null)
            throw new ArgumentException("BusinessRuleValidationRule must have AsyncValidator");

        return new Core.Entities.BusinessRuleValidationRule(
            publicRule.BusinessRuleName,
            publicRule.RuleScope,
            publicRule.AsyncValidator,
            publicRule.ErrorMessage,
            publicRule.Severity.ToInternal(),
            publicRule.Priority,
            publicRule.RuleName,
            publicRule.Timeout,
            publicRule.AffectedColumns);
    }

    /// <summary>Map public ValidationConfiguration to internal ValidationConfiguration</summary>
    public static CoreTypes.ValidationConfiguration ToInternal(this ValidationConfiguration publicConfig)
    {
        return new CoreTypes.ValidationConfiguration(
            publicConfig.EnableValidation,
            publicConfig.DefaultTrigger.ToInternal(),
            publicConfig.DefaultTimeout,
            publicConfig.EnableRealTimeValidation,
            publicConfig.EnableBulkValidation,
            publicConfig.MaxConcurrentValidations,
            publicConfig.MakeValidateAllStopOnFirstError,
            publicConfig.ValidateOnlyVisibleRows);
    }

    /// <summary>Map public ValidationDeletionCriteria to internal ValidationDeletionCriteria</summary>
    public static CoreTypes.ValidationDeletionCriteria ToInternal(this ValidationDeletionCriteria publicCriteria)
    {
        return new CoreTypes.ValidationDeletionCriteria(
            publicCriteria.Mode.ToInternal(),
            publicCriteria.Severities?.Select(s => s.ToInternal()).ToList(),
            publicCriteria.SpecificRuleNames,
            publicCriteria.CustomPredicate,
            publicCriteria.MinimumSeverity?.ToInternal());
    }

    /// <summary>Map public ValidationDeletionOptions to internal ValidationDeletionOptions</summary>
    public static CoreTypes.ValidationDeletionOptions ToInternal(this ValidationDeletionOptions publicOptions)
    {
        return new CoreTypes.ValidationDeletionOptions(
            publicOptions.RequireConfirmation,
            publicOptions.Progress,
            publicOptions.PreviewMode,
            publicOptions.BatchSize,
            publicOptions.MaxOperationTime);
    }

    /// <summary>Map public ValidationSeverity to internal ValidationSeverity</summary>
    public static CoreEnums.ValidationSeverity ToInternal(this ValidationSeverity publicSeverity)
    {
        return publicSeverity switch
        {
            ValidationSeverity.Info => CoreEnums.ValidationSeverity.Info,
            ValidationSeverity.Warning => CoreEnums.ValidationSeverity.Warning,
            ValidationSeverity.Error => CoreEnums.ValidationSeverity.Error,
            ValidationSeverity.Critical => CoreEnums.ValidationSeverity.Critical,
            _ => CoreEnums.ValidationSeverity.Error
        };
    }

    /// <summary>Map public ValidationTrigger to internal ValidationTrigger</summary>
    public static CoreEnums.ValidationTrigger ToInternal(this ValidationTrigger publicTrigger)
    {
        return publicTrigger switch
        {
            ValidationTrigger.Manual => CoreEnums.ValidationTrigger.Manual,
            ValidationTrigger.OnCellChanged => CoreEnums.ValidationTrigger.OnCellChanged,
            ValidationTrigger.OnTextChanged => CoreEnums.ValidationTrigger.OnTextChanged,
            ValidationTrigger.OnCellExit => CoreEnums.ValidationTrigger.OnCellExit,
            ValidationTrigger.OnRowComplete => CoreEnums.ValidationTrigger.OnRowComplete,
            ValidationTrigger.Bulk => CoreEnums.ValidationTrigger.Bulk,
            _ => CoreEnums.ValidationTrigger.OnCellChanged
        };
    }

    /// <summary>Map public ValidationDeletionMode to internal ValidationDeletionMode</summary>
    public static CoreEnums.ValidationDeletionMode ToInternal(this ValidationDeletionMode publicMode)
    {
        return publicMode switch
        {
            ValidationDeletionMode.DeleteInvalidRows => CoreEnums.ValidationDeletionMode.DeleteInvalidRows,
            ValidationDeletionMode.DeleteValidRows => CoreEnums.ValidationDeletionMode.DeleteValidRows,
            ValidationDeletionMode.DeleteByCustomRule => CoreEnums.ValidationDeletionMode.DeleteByCustomRule,
            ValidationDeletionMode.DeleteBySeverity => CoreEnums.ValidationDeletionMode.DeleteBySeverity,
            ValidationDeletionMode.DeleteByRuleName => CoreEnums.ValidationDeletionMode.DeleteByRuleName,
            _ => CoreEnums.ValidationDeletionMode.DeleteInvalidRows
        };
    }

    /// <summary>Map public ValidationLogicalOperator to internal ValidationLogicalOperator</summary>
    public static CoreEnums.ValidationLogicalOperator ToInternal(this ValidationLogicalOperator publicOperator)
    {
        return publicOperator switch
        {
            ValidationLogicalOperator.And => CoreEnums.ValidationLogicalOperator.And,
            ValidationLogicalOperator.Or => CoreEnums.ValidationLogicalOperator.Or,
            ValidationLogicalOperator.AndAlso => CoreEnums.ValidationLogicalOperator.AndAlso,
            ValidationLogicalOperator.OrElse => CoreEnums.ValidationLogicalOperator.OrElse,
            _ => CoreEnums.ValidationLogicalOperator.And
        };
    }

    /// <summary>Map public ColumnValidationPolicy to internal ColumnValidationPolicy</summary>
    public static CoreEnums.ColumnValidationPolicy ToInternal(this ColumnValidationPolicy publicPolicy)
    {
        return publicPolicy switch
        {
            ColumnValidationPolicy.StopOnFirstError => CoreEnums.ColumnValidationPolicy.StopOnFirstError,
            ColumnValidationPolicy.ValidateAll => CoreEnums.ColumnValidationPolicy.ValidateAll,
            ColumnValidationPolicy.StopOnFirstSuccess => CoreEnums.ColumnValidationPolicy.StopOnFirstSuccess,
            _ => CoreEnums.ColumnValidationPolicy.ValidateAll
        };
    }

    /// <summary>Map public ValidationEvaluationStrategy to internal ValidationEvaluationStrategy</summary>
    public static CoreEnums.ValidationEvaluationStrategy ToInternal(this ValidationEvaluationStrategy publicStrategy)
    {
        return publicStrategy switch
        {
            ValidationEvaluationStrategy.Sequential => CoreEnums.ValidationEvaluationStrategy.Sequential,
            ValidationEvaluationStrategy.Parallel => CoreEnums.ValidationEvaluationStrategy.Parallel,
            ValidationEvaluationStrategy.ShortCircuit => CoreEnums.ValidationEvaluationStrategy.ShortCircuit,
            _ => CoreEnums.ValidationEvaluationStrategy.Sequential
        };
    }

    /// <summary>Map public ValidationRuleGroup to internal ValidationRuleGroup</summary>
    public static Core.Entities.ValidationRuleGroup ToInternal(this ValidationRuleGroup publicGroup)
    {
        var internalRules = publicGroup.Rules.Select(r => r.ToInternal()).ToList();
        var internalChildGroups = publicGroup.ChildGroups?.Select(g => g.ToInternal()).ToList();

        return new Core.Entities.ValidationRuleGroup(
            publicGroup.ColumnName,
            internalRules,
            publicGroup.LogicalOperator.ToInternal(),
            publicGroup.ValidationPolicy.ToInternal(),
            publicGroup.EvaluationStrategy.ToInternal(),
            publicGroup.RuleName,
            publicGroup.ErrorMessage,
            publicGroup.Severity.ToInternal(),
            publicGroup.Priority,
            publicGroup.Timeout != TimeSpan.Zero ? publicGroup.Timeout : null,
            internalChildGroups);
    }

    /// <summary>Map public ColumnValidationConfiguration to internal ColumnValidationConfiguration</summary>
    public static CoreTypes.ColumnValidationConfiguration ToInternal(this ColumnValidationConfiguration publicConfig)
    {
        return new CoreTypes.ColumnValidationConfiguration(
            publicConfig.ColumnName,
            publicConfig.ValidationPolicy.ToInternal(),
            publicConfig.EvaluationStrategy.ToInternal(),
            publicConfig.DefaultLogicalOperator.ToInternal(),
            publicConfig.ColumnTimeout,
            publicConfig.AllowRuleGroups);
    }

    #endregion

    #region Internal to Public Mapping

    /// <summary>Map internal ValidationResult to public ValidationResult</summary>
    public static ValidationResult ToPublic(this CoreTypes.ValidationResult internalResult)
    {
        return new ValidationResult
        {
            IsValid = internalResult.IsValid,
            Message = internalResult.ErrorMessage ?? "",
            Severity = internalResult.Severity.ToPublic(),
            RuleName = internalResult.RuleName,
            ValidationTime = internalResult.ValidationTime,
            Value = internalResult.ValidatedValue,
            IsTimeout = internalResult.IsTimeout
        };
    }

    /// <summary>Map internal ValidationBasedDeleteResult to public ValidationBasedDeleteResult</summary>
    public static ValidationBasedDeleteResult ToPublic(this CoreTypes.ValidationBasedDeleteResult internalResult)
    {
        var errors = internalResult.ValidationErrors.Select(e => e.ErrorMessage).ToList();

        return new ValidationBasedDeleteResult
        {
            TotalRowsEvaluated = internalResult.TotalRowsEvaluated,
            RowsDeleted = internalResult.RowsDeleted,
            RemainingRows = internalResult.RemainingRows,
            ValidationErrors = errors,
            OperationDuration = internalResult.OperationDuration,
            Success = internalResult.Success,
            ErrorMessage = internalResult.ErrorMessage,
            DeletedRowIndices = internalResult.DeletedRowIndices
        };
    }

    /// <summary>Map internal ValidationConfiguration to public ValidationConfiguration</summary>
    public static ValidationConfiguration ToPublic(this CoreTypes.ValidationConfiguration internalConfig)
    {
        return new ValidationConfiguration
        {
            EnableValidation = internalConfig.EnableValidation,
            DefaultTrigger = internalConfig.DefaultTrigger.ToPublic(),
            DefaultTimeout = internalConfig.DefaultTimeout,
            EnableRealTimeValidation = internalConfig.EnableRealTimeValidation,
            EnableBulkValidation = internalConfig.EnableBulkValidation,
            MaxConcurrentValidations = internalConfig.MaxConcurrentValidations,
            MakeValidateAllStopOnFirstError = internalConfig.MakeValidateAllStopOnFirstError,
            ValidateOnlyVisibleRows = internalConfig.ValidateOnlyVisibleRows
        };
    }

    /// <summary>Map internal ValidationSeverity to public ValidationSeverity</summary>
    public static ValidationSeverity ToPublic(this CoreEnums.ValidationSeverity internalSeverity)
    {
        return internalSeverity switch
        {
            CoreEnums.ValidationSeverity.Info => ValidationSeverity.Info,
            CoreEnums.ValidationSeverity.Warning => ValidationSeverity.Warning,
            CoreEnums.ValidationSeverity.Error => ValidationSeverity.Error,
            CoreEnums.ValidationSeverity.Critical => ValidationSeverity.Critical,
            _ => ValidationSeverity.Error
        };
    }

    /// <summary>Map internal ValidationTrigger to public ValidationTrigger</summary>
    public static ValidationTrigger ToPublic(this CoreEnums.ValidationTrigger internalTrigger)
    {
        return internalTrigger switch
        {
            CoreEnums.ValidationTrigger.Manual => ValidationTrigger.Manual,
            CoreEnums.ValidationTrigger.OnCellChanged => ValidationTrigger.OnCellChanged,
            CoreEnums.ValidationTrigger.OnTextChanged => ValidationTrigger.OnTextChanged,
            CoreEnums.ValidationTrigger.OnCellExit => ValidationTrigger.OnCellExit,
            CoreEnums.ValidationTrigger.OnRowComplete => ValidationTrigger.OnRowComplete,
            CoreEnums.ValidationTrigger.Bulk => ValidationTrigger.Bulk,
            _ => ValidationTrigger.OnCellChanged
        };
    }

    /// <summary>Map internal ValidationDeletionMode to public ValidationDeletionMode</summary>
    public static ValidationDeletionMode ToPublic(this CoreEnums.ValidationDeletionMode internalMode)
    {
        return internalMode switch
        {
            CoreEnums.ValidationDeletionMode.DeleteInvalidRows => ValidationDeletionMode.DeleteInvalidRows,
            CoreEnums.ValidationDeletionMode.DeleteValidRows => ValidationDeletionMode.DeleteValidRows,
            CoreEnums.ValidationDeletionMode.DeleteByCustomRule => ValidationDeletionMode.DeleteByCustomRule,
            CoreEnums.ValidationDeletionMode.DeleteBySeverity => ValidationDeletionMode.DeleteBySeverity,
            CoreEnums.ValidationDeletionMode.DeleteByRuleName => ValidationDeletionMode.DeleteByRuleName,
            _ => ValidationDeletionMode.DeleteInvalidRows
        };
    }

    /// <summary>Map internal ValidationLogicalOperator to public ValidationLogicalOperator</summary>
    public static ValidationLogicalOperator ToPublic(this CoreEnums.ValidationLogicalOperator internalOperator)
    {
        return internalOperator switch
        {
            CoreEnums.ValidationLogicalOperator.And => ValidationLogicalOperator.And,
            CoreEnums.ValidationLogicalOperator.Or => ValidationLogicalOperator.Or,
            CoreEnums.ValidationLogicalOperator.AndAlso => ValidationLogicalOperator.AndAlso,
            CoreEnums.ValidationLogicalOperator.OrElse => ValidationLogicalOperator.OrElse,
            _ => ValidationLogicalOperator.And
        };
    }

    /// <summary>Map internal ColumnValidationPolicy to public ColumnValidationPolicy</summary>
    public static ColumnValidationPolicy ToPublic(this CoreEnums.ColumnValidationPolicy internalPolicy)
    {
        return internalPolicy switch
        {
            CoreEnums.ColumnValidationPolicy.StopOnFirstError => ColumnValidationPolicy.StopOnFirstError,
            CoreEnums.ColumnValidationPolicy.ValidateAll => ColumnValidationPolicy.ValidateAll,
            CoreEnums.ColumnValidationPolicy.StopOnFirstSuccess => ColumnValidationPolicy.StopOnFirstSuccess,
            _ => ColumnValidationPolicy.ValidateAll
        };
    }

    /// <summary>Map internal ValidationEvaluationStrategy to public ValidationEvaluationStrategy</summary>
    public static ValidationEvaluationStrategy ToPublic(this CoreEnums.ValidationEvaluationStrategy internalStrategy)
    {
        return internalStrategy switch
        {
            CoreEnums.ValidationEvaluationStrategy.Sequential => ValidationEvaluationStrategy.Sequential,
            CoreEnums.ValidationEvaluationStrategy.Parallel => ValidationEvaluationStrategy.Parallel,
            CoreEnums.ValidationEvaluationStrategy.ShortCircuit => ValidationEvaluationStrategy.ShortCircuit,
            _ => ValidationEvaluationStrategy.Sequential
        };
    }

    /// <summary>Map internal ColumnValidationConfiguration to public ColumnValidationConfiguration</summary>
    public static ColumnValidationConfiguration ToPublic(this CoreTypes.ColumnValidationConfiguration internalConfig)
    {
        return new ColumnValidationConfiguration
        {
            ColumnName = internalConfig.ColumnName,
            ValidationPolicy = internalConfig.ValidationPolicy.ToPublic(),
            EvaluationStrategy = internalConfig.EvaluationStrategy.ToPublic(),
            DefaultLogicalOperator = internalConfig.DefaultLogicalOperator.ToPublic(),
            ColumnTimeout = internalConfig.ColumnTimeout,
            AllowRuleGroups = internalConfig.AllowRuleGroups
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>Create a convenience async validator from sync validator</summary>
    public static Func<object?, Task<bool>> ToAsync(this Func<object?, bool> syncValidator)
    {
        return value => Task.FromResult(syncValidator(value));
    }

    /// <summary>Create a convenience async cross-column validator from simple validator</summary>
    public static Func<System.Collections.Generic.IReadOnlyDictionary<string, object?>, Task<ValidationResult>>
        ToAsyncCrossColumn(this Func<System.Collections.Generic.IReadOnlyDictionary<string, object?>, (bool isValid, string? errorMessage)> validator,
        CoreEnums.ValidationSeverity severity, string? ruleName)
    {
        return rowData =>
        {
            var (isValid, errorMessage) = validator(rowData);
            var result = isValid
                ? CoreTypes.ValidationResult.Success()
                : CoreTypes.ValidationResult.Error(errorMessage ?? "Validation failed", severity, ruleName);
            return Task.FromResult(result);
        };
    }

    #endregion
}
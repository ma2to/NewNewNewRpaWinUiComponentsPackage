using System;
using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;

/// <summary>
/// DOMAIN: Cross-row validation rule implementation
/// ENTERPRISE: Validates data across multiple rows for uniqueness, totals, etc.
/// </summary>
internal sealed record CrossRowValidationRule(
    Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, IReadOnlyList<ValidationResult>> ValidatorFunc,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null) : ICrossRowValidationRule
{
    public string RuleType => "CrossRow";
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(2);

    public Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, IReadOnlyList<ValidationResult>> Validator => ValidatorFunc;
}

/// <summary>
/// DOMAIN: Complex validation rule implementation
/// ENTERPRISE: Validates complex business rules across entire dataset
/// </summary>
internal sealed record ComplexValidationRule(
    Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, ValidationResult> ValidatorFunc,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null) : IComplexValidationRule
{
    public string RuleType => "Complex";
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(2);

    public Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, ValidationResult> Validator => ValidatorFunc;
}

/// <summary>
/// DOMAIN: Conditional validation rule implementation
/// ENTERPRISE: Validates column only if condition is met
/// </summary>
internal sealed record ConditionalValidationRule(
    string ColumnName,
    Func<IReadOnlyDictionary<string, object?>, bool> Condition,
    ISingleCellValidationRule ValidationRule,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null) : IConditionalValidationRule
{
    public string RuleType => "Conditional";
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(2);
}

/// <summary>
/// DOMAIN: Single cell validation rule implementation
/// ENTERPRISE: Basic validation for individual cell values
/// </summary>
internal sealed record SingleCellValidationRule(
    string ColumnName,
    Func<object?, bool> Validator,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null) : ISingleCellValidationRule
{
    public string RuleType => "SingleCell";
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(2);
}

/// <summary>
/// DOMAIN: Cross-column validation rule implementation
/// ENTERPRISE: Validates data across multiple columns in same row
/// </summary>
internal sealed record CrossColumnValidationRule(
    string[] DependentColumns,
    Func<IReadOnlyDictionary<string, object?>, (bool isValid, string? errorMessage)> ValidatorFunc,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null) : ICrossColumnValidationRule
{
    public string RuleType => "CrossColumn";
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(2);

    public Func<IReadOnlyDictionary<string, object?>, ValidationResult> Validator =>
        rowData =>
        {
            var (isValid, errorMessage) = ValidatorFunc(rowData);
            return isValid
                ? ValidationResult.Success()
                : ValidationResult.Error(errorMessage ?? ErrorMessage, Severity, RuleName);
        };
}
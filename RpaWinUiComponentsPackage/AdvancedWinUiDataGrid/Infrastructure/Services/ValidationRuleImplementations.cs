using System;
using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Services;

/// <summary>
/// INTERNAL: Single cell validation rule implementation
/// HIDDEN: Internal implementation to prevent namespace pollution
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
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(2);
}

/// <summary>
/// INTERNAL: Cross-column validation rule implementation
/// HIDDEN: Internal implementation to prevent namespace pollution
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

/// <summary>
/// INTERNAL: Cross-row validation rule implementation
/// HIDDEN: Internal implementation to prevent namespace pollution
/// </summary>
internal sealed record CrossRowValidationRule(
    Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, IReadOnlyList<ValidationResult>> ValidatorFunc,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null) : ICrossRowValidationRule
{
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(2);

    public Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, IReadOnlyList<ValidationResult>> Validator => ValidatorFunc;
}

/// <summary>
/// INTERNAL: Conditional validation rule implementation
/// HIDDEN: Internal implementation to prevent namespace pollution
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
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(2);
}

/// <summary>
/// INTERNAL: Complex validation rule implementation
/// HIDDEN: Internal implementation to prevent namespace pollution
/// </summary>
internal sealed record ComplexValidationRule(
    Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, ValidationResult> ValidatorFunc,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null) : IComplexValidationRule
{
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(2);

    public Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, ValidationResult> Validator => ValidatorFunc;
}
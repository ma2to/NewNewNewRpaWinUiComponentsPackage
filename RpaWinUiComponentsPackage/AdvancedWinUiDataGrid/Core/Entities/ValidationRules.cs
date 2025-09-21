using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Constants;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;

/// <summary>
/// CORE: Single cell validation rule implementation with timeout support
/// ENTERPRISE: Basic validation for individual cell values with professional timeout handling
/// </summary>
internal sealed record SingleCellValidationRule(
    string ColumnName,
    Func<object?, Task<bool>> AsyncValidator,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null,
    IReadOnlyList<string>? DependentColumns = null) : ISingleCellValidationRule
{
    public string RuleType => "SingleCell";
    public TimeSpan Timeout { get; } = Timeout ?? ValidationConstants.DefaultValidationTimeout;

    public async Task<ValidationResult> ValidateAsync(object? value, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using var timeoutCts = new CancellationTokenSource(Timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var isValid = await AsyncValidator(value);
            var validationTime = DateTime.UtcNow - startTime;

            return isValid
                ? ValidationResult.Success(validationTime, value)
                : ValidationResult.Error(ErrorMessage, Severity, RuleName, validationTime, value);
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
            return ValidationResult.Error($"Validation error: {ex.Message}", ValidationSeverity.Error, RuleName, validationTime, value);
        }
    }

    // Convenience constructor for synchronous validators
    public SingleCellValidationRule(
        string columnName,
        Func<object?, bool> validator,
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        int? priority = null,
        string? ruleName = null,
        TimeSpan? timeout = null,
        IReadOnlyList<string>? dependentColumns = null)
        : this(columnName, value => Task.FromResult(validator(value)), errorMessage, severity, priority, ruleName, timeout, dependentColumns)
    {
    }
}

/// <summary>
/// CORE: Cross-column validation rule implementation with timeout support
/// ENTERPRISE: Validates data across multiple columns in same row with professional timeout handling
/// </summary>
internal sealed record CrossColumnValidationRule(
    IReadOnlyList<string> DependentColumns,
    Func<IReadOnlyDictionary<string, object?>, Task<ValidationResult>> AsyncValidator,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null,
    string? PrimaryColumn = null) : ICrossColumnValidationRule
{
    public string RuleType => "CrossColumn";
    public TimeSpan Timeout { get; } = Timeout ?? ValidationConstants.DefaultValidationTimeout;

    public async Task<ValidationResult> ValidateAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using var timeoutCts = new CancellationTokenSource(Timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var result = await AsyncValidator(rowData);
            var validationTime = DateTime.UtcNow - startTime;

            return result with { ValidationTime = validationTime };
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
            return ValidationResult.Error($"Cross-column validation error: {ex.Message}", ValidationSeverity.Error, RuleName, validationTime);
        }
    }

    // Convenience constructor for simple validation functions
    public CrossColumnValidationRule(
        IReadOnlyList<string> dependentColumns,
        Func<IReadOnlyDictionary<string, object?>, (bool isValid, string? errorMessage)> validator,
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        int? priority = null,
        string? ruleName = null,
        TimeSpan? timeout = null,
        string? primaryColumn = null)
        : this(dependentColumns,
               rowData =>
               {
                   var (isValid, error) = validator(rowData);
                   return Task.FromResult(isValid
                       ? ValidationResult.Success()
                       : ValidationResult.Error(error ?? errorMessage, severity, ruleName));
               },
               errorMessage, severity, priority, ruleName, timeout, primaryColumn)
    {
    }
}

/// <summary>
/// CORE: Cross-row validation rule implementation with timeout support
/// ENTERPRISE: Validates data across multiple rows for uniqueness, totals, etc. with professional timeout handling
/// </summary>
internal sealed record CrossRowValidationRule(
    Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, Task<IReadOnlyList<ValidationResult>>> AsyncValidator,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null,
    IReadOnlyList<string>? AffectedColumns = null) : ICrossRowValidationRule
{
    public string RuleType => "CrossRow";
    public TimeSpan Timeout { get; } = Timeout ?? ValidationConstants.DefaultValidationTimeout;

    public async Task<IReadOnlyList<ValidationResult>> ValidateAsync(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using var timeoutCts = new CancellationTokenSource(Timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var results = await AsyncValidator(rows);
            var validationTime = DateTime.UtcNow - startTime;

            // Update validation times
            return results.Select(r => r with { ValidationTime = validationTime }).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            var validationTime = DateTime.UtcNow - startTime;
            return rows.Select((_, index) => ValidationResult.Timeout(RuleName, validationTime, index)).ToList();
        }
        catch (Exception ex)
        {
            var validationTime = DateTime.UtcNow - startTime;
            return rows.Select((_, index) =>
                ValidationResult.Error($"Cross-row validation error: {ex.Message}", ValidationSeverity.Error, RuleName, validationTime))
                .ToList();
        }
    }
}

/// <summary>
/// CORE: Complex validation rule implementation with timeout support
/// ENTERPRISE: Validates complex business rules across entire dataset with professional timeout handling
/// </summary>
internal sealed record ComplexValidationRule(
    Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, Task<ValidationResult>> AsyncValidator,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null,
    IReadOnlyList<string>? InvolvedColumns = null) : IComplexValidationRule
{
    public string RuleType => "Complex";
    public TimeSpan Timeout { get; } = Timeout ?? ValidationConstants.DefaultValidationTimeout;

    public async Task<ValidationResult> ValidateAsync(IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using var timeoutCts = new CancellationTokenSource(Timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var result = await AsyncValidator(dataset);
            var validationTime = DateTime.UtcNow - startTime;

            return result with { ValidationTime = validationTime };
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
            return ValidationResult.Error($"Complex validation error: {ex.Message}", ValidationSeverity.Error, RuleName, validationTime);
        }
    }
}

/// <summary>
/// CORE: Conditional validation rule implementation with timeout support
/// ENTERPRISE: Validates column only if condition is met with professional timeout handling
/// </summary>
internal sealed record ConditionalValidationRule(
    string ColumnName,
    Func<IReadOnlyDictionary<string, object?>, bool> Condition,
    ISingleCellValidationRule ValidationRule,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null,
    IReadOnlyList<string>? DependentColumns = null) : IConditionalValidationRule
{
    public string RuleType => "Conditional";
    public TimeSpan Timeout { get; } = Timeout ?? ValidationConstants.DefaultValidationTimeout;

    public async Task<ValidationResult> ValidateAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using var timeoutCts = new CancellationTokenSource(Timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Check condition first
            if (!Condition(rowData))
            {
                var validationTime = DateTime.UtcNow - startTime;
                return ValidationResult.Success(validationTime, rowData.GetValueOrDefault(ColumnName));
            }

            // Apply validation rule if condition is met
            var cellValue = rowData.GetValueOrDefault(ColumnName);
            var result = await ValidationRule.ValidateAsync(cellValue, combinedCts.Token);

            var totalValidationTime = DateTime.UtcNow - startTime;
            return result with { ValidationTime = totalValidationTime };
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
            return ValidationResult.Error($"Conditional validation error: {ex.Message}", ValidationSeverity.Error, RuleName, validationTime);
        }
    }
}

/// <summary>
/// CORE: Business rule validation implementation with timeout support
/// ENTERPRISE: Validates complex business rules with custom logic and professional timeout handling
/// </summary>
internal sealed record BusinessRuleValidationRule(
    string BusinessRuleName,
    string RuleScope,
    Func<object, Task<ValidationResult>> AsyncValidator,
    string ErrorMessage,
    ValidationSeverity Severity = ValidationSeverity.Error,
    int? Priority = null,
    string? RuleName = null,
    TimeSpan? Timeout = null,
    IReadOnlyList<string>? AffectedColumns = null) : IBusinessRuleValidationRule
{
    public string RuleType => "BusinessRule";
    public TimeSpan Timeout { get; } = Timeout ?? ValidationConstants.DefaultValidationTimeout;

    public async Task<ValidationResult> ValidateAsync(object context, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using var timeoutCts = new CancellationTokenSource(Timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var result = await AsyncValidator(context);
            var validationTime = DateTime.UtcNow - startTime;

            return result with { ValidationTime = validationTime };
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
            return ValidationResult.Error($"Business rule validation error: {ex.Message}", ValidationSeverity.Error, RuleName, validationTime);
        }
    }
}
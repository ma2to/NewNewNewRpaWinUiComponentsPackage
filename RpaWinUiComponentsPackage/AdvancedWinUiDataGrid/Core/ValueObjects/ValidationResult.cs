using System;
using System.Collections.Generic;
using System.Linq;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// CORE: Represents the result of a validation operation with timeout support
/// IMMUTABLE: Value object ensuring consistency across validation operations
/// ENTERPRISE: Professional validation result with comprehensive error information
/// </summary>
internal readonly record struct ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    public ValidationSeverity Severity { get; }
    public string? RuleName { get; }
    public int? RowIndex { get; }
    public string? ColumnName { get; }
    public TimeSpan ValidationTime { get; }
    public bool IsTimeout { get; }
    public object? ValidatedValue { get; }

    private ValidationResult(
        bool isValid,
        string? errorMessage,
        ValidationSeverity severity,
        string? ruleName,
        int? rowIndex,
        string? columnName,
        TimeSpan validationTime,
        bool isTimeout,
        object? validatedValue)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        Severity = severity;
        RuleName = ruleName;
        RowIndex = rowIndex;
        ColumnName = columnName;
        ValidationTime = validationTime;
        IsTimeout = isTimeout;
        ValidatedValue = validatedValue;
    }

    /// <summary>Create successful validation result</summary>
    public static ValidationResult Success(TimeSpan? validationTime = null, object? value = null)
        => new(true, null, ValidationSeverity.Info, null, null, null, validationTime ?? TimeSpan.Zero, false, value);

    /// <summary>Create failed validation result with error details</summary>
    public static ValidationResult Error(
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        string? ruleName = null,
        TimeSpan? validationTime = null,
        object? value = null)
        => new(false, errorMessage, severity, ruleName, null, null, validationTime ?? TimeSpan.Zero, false, value);

    /// <summary>Create failed validation result for specific cell</summary>
    public static ValidationResult ErrorForCell(
        int rowIndex,
        string columnName,
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        string? ruleName = null,
        TimeSpan? validationTime = null,
        object? value = null)
        => new(false, errorMessage, severity, ruleName, rowIndex, columnName, validationTime ?? TimeSpan.Zero, false, value);

    /// <summary>Create failed validation result for specific row</summary>
    public static ValidationResult ErrorForRow(
        int rowIndex,
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        string? ruleName = null,
        TimeSpan? validationTime = null)
        => new(false, errorMessage, severity, ruleName, rowIndex, null, validationTime ?? TimeSpan.Zero, false, null);

    /// <summary>Create timeout validation result</summary>
    public static ValidationResult Timeout(
        string? ruleName = null,
        TimeSpan? validationTime = null,
        int? rowIndex = null,
        string? columnName = null)
        => new(false, "Timeout", ValidationSeverity.Error, ruleName, rowIndex, columnName,
               validationTime ?? TimeSpan.Zero, true, null);

    /// <summary>Combine multiple validation results</summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var failures = results.Where(r => !r.IsValid).ToList();
        if (!failures.Any())
            return Success(TimeSpan.FromTicks(results.Sum(r => r.ValidationTime.Ticks)));

        var highestSeverity = failures.Max(f => f.Severity);
        var firstError = failures.First(f => f.Severity == highestSeverity);
        var totalTime = TimeSpan.FromTicks(results.Sum(r => r.ValidationTime.Ticks));

        return new ValidationResult(
            false,
            firstError.ErrorMessage,
            highestSeverity,
            firstError.RuleName,
            firstError.RowIndex,
            firstError.ColumnName,
            totalTime,
            failures.Any(f => f.IsTimeout),
            firstError.ValidatedValue);
    }

    /// <summary>Combine multiple validation results into collection</summary>
    public static IReadOnlyList<ValidationResult> CombineAll(params ValidationResult[] results)
    {
        return results.Where(r => !r.IsValid).ToList();
    }

    public override string ToString()
    {
        if (IsValid)
            return "Valid";

        var location = (RowIndex, ColumnName) switch
        {
            (int row, string col) => $" at [{row}, {col}]",
            (int row, null) => $" at row {row}",
            (null, string col) => $" at column {col}",
            _ => ""
        };

        var rule = !string.IsNullOrEmpty(RuleName) ? $" (Rule: {RuleName})" : "";
        var timeout = IsTimeout ? " [TIMEOUT]" : "";
        var time = ValidationTime > TimeSpan.Zero ? $" ({ValidationTime.TotalMilliseconds:F1}ms)" : "";

        return $"{Severity}: {ErrorMessage}{location}{rule}{timeout}{time}";
    }
}

/// <summary>
/// CORE: Error information for validation operations
/// ENTERPRISE: Detailed error tracking for validation failures
/// </summary>
internal readonly record struct ValidationError
{
    public int RowIndex { get; }
    public string? ColumnName { get; }
    public string ErrorMessage { get; }
    public ValidationSeverity Severity { get; }
    public string? RuleName { get; }
    public bool IsTimeout { get; }
    public object? Value { get; }

    public ValidationError(
        int rowIndex,
        string? columnName,
        string errorMessage,
        ValidationSeverity severity,
        string? ruleName,
        bool isTimeout,
        object? value)
    {
        RowIndex = rowIndex;
        ColumnName = columnName;
        ErrorMessage = errorMessage;
        Severity = severity;
        RuleName = ruleName;
        IsTimeout = isTimeout;
        Value = value;
    }

    public static ValidationError FromValidationResult(ValidationResult result, int rowIndex, string? columnName = null)
    {
        return new ValidationError(
            result.RowIndex ?? rowIndex,
            result.ColumnName ?? columnName,
            result.ErrorMessage ?? "Unknown validation error",
            result.Severity,
            result.RuleName,
            result.IsTimeout,
            result.ValidatedValue);
    }
}
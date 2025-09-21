using System.Collections.Generic;
using System.Linq;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// PUBLIC API: Represents the result of a validation operation
/// IMMUTABLE: Value object ensuring consistency across validation operations
/// </summary>
internal readonly record struct ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    public ValidationSeverity Severity { get; }
    public string? RuleName { get; }
    public int? RowIndex { get; }
    public string? ColumnName { get; }

    private ValidationResult(bool isValid, string? errorMessage, ValidationSeverity severity, string? ruleName, int? rowIndex, string? columnName)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        Severity = severity;
        RuleName = ruleName;
        RowIndex = rowIndex;
        ColumnName = columnName;
    }

    /// <summary>Create successful validation result</summary>
    public static ValidationResult Success() => new(true, null, ValidationSeverity.Info, null, null, null);

    /// <summary>Create failed validation result with error details</summary>
    public static ValidationResult Error(string errorMessage, ValidationSeverity severity = ValidationSeverity.Error, string? ruleName = null)
        => new(false, errorMessage, severity, ruleName, null, null);

    /// <summary>Create failed validation result for specific cell</summary>
    public static ValidationResult ErrorForCell(int rowIndex, string columnName, string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error, string? ruleName = null)
        => new(false, errorMessage, severity, ruleName, rowIndex, columnName);

    /// <summary>Create failed validation result for specific row</summary>
    public static ValidationResult ErrorForRow(int rowIndex, string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error, string? ruleName = null)
        => new(false, errorMessage, severity, ruleName, rowIndex, null);

    /// <summary>Combine multiple validation results</summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var failures = results.Where(r => !r.IsValid).ToList();
        if (!failures.Any())
            return Success();

        var highestSeverity = failures.Max(f => f.Severity);
        var firstError = failures.First(f => f.Severity == highestSeverity);
        return new ValidationResult(false, firstError.ErrorMessage, highestSeverity, firstError.RuleName, firstError.RowIndex, firstError.ColumnName);
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
        return $"{Severity}: {ErrorMessage}{location}{rule}";
    }
}
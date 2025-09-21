using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;

/// <summary>
/// CORE: Base interface for all validation rules with timeout support
/// ENTERPRISE: Foundation for comprehensive validation system
/// </summary>
internal interface IValidationRule
{
    /// <summary>Unique name for the validation rule</summary>
    string? RuleName { get; }

    /// <summary>Error message when validation fails</summary>
    string ErrorMessage { get; }

    /// <summary>Severity level of validation failure</summary>
    ValidationSeverity Severity { get; }

    /// <summary>Priority for rule execution (lower = higher priority)</summary>
    int? Priority { get; }

    /// <summary>Timeout for rule execution (default 2 seconds)</summary>
    TimeSpan Timeout { get; }

    /// <summary>Rule type identifier for categorization</summary>
    string RuleType { get; }
}

/// <summary>
/// CORE: Single cell validation rule for individual cell values
/// ENTERPRISE: Basic validation building block with timeout support
/// </summary>
internal interface ISingleCellValidationRule : IValidationRule
{
    /// <summary>Column name to validate</summary>
    string ColumnName { get; }

    /// <summary>Validation function for cell value with timeout support</summary>
    Task<ValidationResult> ValidateAsync(object? value, CancellationToken cancellationToken = default);

    /// <summary>Dependent columns that trigger revalidation of this rule</summary>
    IReadOnlyList<string>? DependentColumns { get; }
}

/// <summary>
/// CORE: Cross-column validation rule for same row validation
/// ENTERPRISE: Validates data across multiple columns in same row with timeout
/// </summary>
internal interface ICrossColumnValidationRule : IValidationRule
{
    /// <summary>Dependent columns that trigger this rule</summary>
    IReadOnlyList<string> DependentColumns { get; }

    /// <summary>Primary column for error reporting</summary>
    string? PrimaryColumn { get; }

    /// <summary>Validation function for row data with timeout support</summary>
    Task<ValidationResult> ValidateAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);
}

/// <summary>
/// CORE: Cross-row validation rule for multiple row validation
/// ENTERPRISE: Supports uniqueness, totals, and cross-row business rules with timeout
/// </summary>
internal interface ICrossRowValidationRule : IValidationRule
{
    /// <summary>Affected columns for this rule</summary>
    IReadOnlyList<string>? AffectedColumns { get; }

    /// <summary>Validation function for multiple rows with timeout support</summary>
    Task<IReadOnlyList<ValidationResult>> ValidateAsync(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default);
}

/// <summary>
/// CORE: Complex validation rule for cross-row and cross-column validation
/// ENTERPRISE: Supports complex business rules across entire dataset with timeout
/// </summary>
internal interface IComplexValidationRule : IValidationRule
{
    /// <summary>Involved columns for this rule</summary>
    IReadOnlyList<string>? InvolvedColumns { get; }

    /// <summary>Validation function for entire dataset with timeout support</summary>
    Task<ValidationResult> ValidateAsync(IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset, CancellationToken cancellationToken = default);
}

/// <summary>
/// CORE: Conditional validation rule - validates only if condition is met
/// ENTERPRISE: Supports conditional business logic validation with timeout
/// </summary>
internal interface IConditionalValidationRule : IValidationRule
{
    /// <summary>Column name to validate</summary>
    string ColumnName { get; }

    /// <summary>Condition that must be true to trigger validation</summary>
    Func<IReadOnlyDictionary<string, object?>, bool> Condition { get; }

    /// <summary>Validation rule to apply if condition is met</summary>
    ISingleCellValidationRule ValidationRule { get; }

    /// <summary>Dependent columns for condition evaluation</summary>
    IReadOnlyList<string>? DependentColumns { get; }

    /// <summary>Validation function with condition check and timeout support</summary>
    Task<ValidationResult> ValidateAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default);
}

/// <summary>
/// CORE: Business rule validation for complex domain logic
/// ENTERPRISE: Validates complex business rules with custom logic and timeout
/// </summary>
internal interface IBusinessRuleValidationRule : IValidationRule
{
    /// <summary>Business rule name or identifier</summary>
    string BusinessRuleName { get; }

    /// <summary>Affected columns for this business rule</summary>
    IReadOnlyList<string>? AffectedColumns { get; }

    /// <summary>Business rule scope (cell, row, dataset)</summary>
    string RuleScope { get; }

    /// <summary>Validation function for business rule with timeout support</summary>
    Task<ValidationResult> ValidateAsync(object context, CancellationToken cancellationToken = default);
}
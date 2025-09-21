using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

/// <summary>
/// CORE: Logical operators for combining validation rules
/// ENTERPRISE: Professional logical operations for complex validation scenarios
/// </summary>
internal enum ValidationLogicalOperator
{
    /// <summary>All rules must pass (AND logic)</summary>
    And = 0,

    /// <summary>At least one rule must pass (OR logic)</summary>
    Or = 1,

    /// <summary>All rules must pass with short-circuit evaluation (stops on first FALSE)</summary>
    AndAlso = 2,

    /// <summary>At least one rule must pass with short-circuit evaluation (stops on first TRUE)</summary>
    OrElse = 3
}

/// <summary>
/// CORE: Policy for validation execution on column level
/// ENTERPRISE: Control validation behavior for groups of rules
/// </summary>
internal enum ColumnValidationPolicy
{
    /// <summary>Stop validation after first failed rule (fail-fast)</summary>
    StopOnFirstError = 0,

    /// <summary>Execute all rules regardless of failures (collect all errors)</summary>
    ValidateAll = 1,

    /// <summary>Stop validation after first successful rule (success-fast, useful with OR logic)</summary>
    StopOnFirstSuccess = 2
}

/// <summary>
/// CORE: Evaluation strategy for validation rule groups
/// ENTERPRISE: Advanced evaluation strategies for complex validation scenarios
/// </summary>
internal enum ValidationEvaluationStrategy
{
    /// <summary>Evaluate rules sequentially in priority order</summary>
    Sequential = 0,

    /// <summary>Evaluate rules in parallel where possible</summary>
    Parallel = 1,

    /// <summary>Short-circuit evaluation (stop on first definitive result)</summary>
    ShortCircuit = 2
}
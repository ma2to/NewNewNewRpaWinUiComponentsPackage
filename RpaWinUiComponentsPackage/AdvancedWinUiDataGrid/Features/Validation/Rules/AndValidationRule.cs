using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Rules;

/// <summary>
/// Composite validation rule that combines two rules with AND logic.
/// Both rules must pass for validation to succeed.
/// SHORT-CIRCUIT: If left rule fails, right rule is NOT evaluated (optimization).
/// </summary>
public sealed class AndValidationRule : IValidationRule
{
    private readonly IValidationRule _left;
    private readonly IValidationRule _right;
    private readonly string _ruleId;
    private readonly string _ruleName;
    private readonly TimeSpan _validationTimeout;

    /// <summary>
    /// Creates a new AND composite validation rule
    /// </summary>
    /// <param name="left">Left rule to evaluate</param>
    /// <param name="right">Right rule to evaluate</param>
    /// <param name="ruleId">Optional custom rule ID (auto-generated if null)</param>
    /// <param name="ruleName">Optional custom rule name (auto-generated if null)</param>
    /// <param name="validationTimeout">Optional timeout (uses maximum of child rule timeouts if not specified)</param>
    /// <exception cref="ArgumentNullException">Thrown when left or right is null</exception>
    public AndValidationRule(
        IValidationRule left,
        IValidationRule right,
        string? ruleId = null,
        string? ruleName = null,
        TimeSpan? validationTimeout = null)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
        _ruleId = ruleId ?? $"AND_{left.RuleId}_{right.RuleId}";
        _ruleName = ruleName ?? $"{left.RuleName} AND {right.RuleName}";

        // Use maximum timeout of child rules, or provided timeout
        _validationTimeout = validationTimeout ?? TimeSpan.FromMilliseconds(
            Math.Max(left.ValidationTimeout.TotalMilliseconds, right.ValidationTimeout.TotalMilliseconds));
    }

    /// <inheritdoc />
    public string RuleId => _ruleId;

    /// <inheritdoc />
    public string RuleName => _ruleName;

    /// <inheritdoc />
    public IReadOnlyList<string> DependentColumns =>
        _left.DependentColumns
            .Union(_right.DependentColumns)
            .Distinct()
            .ToList();

    /// <inheritdoc />
    public bool IsEnabled => _left.IsEnabled && _right.IsEnabled;

    /// <inheritdoc />
    public TimeSpan ValidationTimeout => _validationTimeout;

    /// <inheritdoc />
    /// <remarks>
    /// SHORT-CIRCUIT LOGIC:
    /// 1. Evaluates left rule first
    /// 2. If left fails → returns immediately (right NOT evaluated)
    /// 3. If left passes → evaluates right rule
    /// 4. Returns right result (success or failure)
    /// </remarks>
    public ValidationResult Validate(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context)
    {
        // SHORT-CIRCUIT AND: If left fails, return immediately
        var leftResult = _left.Validate(row, context);
        if (!leftResult.IsValid)
        {
            return leftResult; // Left failed → AND fails
        }

        // Left passed, evaluate right
        var rightResult = _right.Validate(row, context);
        if (!rightResult.IsValid)
        {
            return rightResult; // Right failed → AND fails
        }

        // Both passed → AND succeeds
        return ValidationResult.Success();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Async version with same short-circuit logic as synchronous Validate
    /// </remarks>
    public async Task<ValidationResult> ValidateAsync(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        // SHORT-CIRCUIT AND: If left fails, return immediately
        var leftResult = await _left.ValidateAsync(row, context, cancellationToken);
        if (!leftResult.IsValid)
        {
            return leftResult; // Left failed → AND fails
        }

        // Left passed, evaluate right
        var rightResult = await _right.ValidateAsync(row, context, cancellationToken);
        if (!rightResult.IsValid)
        {
            return rightResult; // Right failed → AND fails
        }

        // Both passed → AND succeeds
        return ValidationResult.Success();
    }
}

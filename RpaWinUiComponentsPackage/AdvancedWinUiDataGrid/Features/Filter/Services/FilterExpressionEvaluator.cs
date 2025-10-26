using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Services;

/// <summary>
/// Evaluates filter expressions against in-memory row data
/// Implements visitor pattern for expression tree traversal
/// Supports type-aware filtering (numeric, datetime dd.MM.yyyy, text)
/// </summary>
internal sealed class FilterExpressionEvaluator : IFilterExpressionVisitor<bool>
{
    private readonly ILogger<FilterExpressionEvaluator>? _logger;
    private readonly FilterTypeDetector _typeDetector;
    private IReadOnlyDictionary<string, object?> _currentRow = null!;

    public FilterExpressionEvaluator(ILogger<FilterExpressionEvaluator>? logger = null)
    {
        _logger = logger;
        _typeDetector = new FilterTypeDetector(null); // Different logger type
    }

    /// <summary>
    /// Evaluates filter expression against a row
    /// Returns true if row matches the filter criteria
    /// </summary>
    public bool Evaluate(FilterExpression expression, IReadOnlyDictionary<string, object?> row)
    {
        _currentRow = row;
        return expression.Accept(this);
    }

    /// <summary>
    /// Visit simple condition node - evaluates single filter criterion
    /// </summary>
    public bool VisitCondition(FilterCondition condition)
    {
        // Get cell value from current row
        if (!_currentRow.TryGetValue(condition.ColumnName, out var cellValue))
        {
            cellValue = null; // Column doesn't exist - treat as null
        }

        // Evaluate operator
        return EvaluateOperator(cellValue, condition.Operator, condition.Value);
    }

    /// <summary>
    /// Visit binary expression node - combines sub-expression results with logical operator
    /// </summary>
    public bool VisitBinaryExpression(FilterBinaryExpression expression)
    {
        return expression.Type switch
        {
            FilterExpressionType.And => expression.Left.Accept(this) & expression.Right.Accept(this),
            FilterExpressionType.Or => expression.Left.Accept(this) | expression.Right.Accept(this),
            FilterExpressionType.AndAlso => expression.Left.Accept(this) && expression.Right.Accept(this),
            FilterExpressionType.OrElse => expression.Left.Accept(this) || expression.Right.Accept(this),
            _ => throw new NotSupportedException($"Filter expression type {expression.Type} not supported")
        };
    }

    /// <summary>
    /// Evaluates single operator against cell value and filter value
    /// Type-aware comparison (numeric, datetime dd.MM.yyyy, text)
    /// </summary>
    private bool EvaluateOperator(object? cellValue, FilterOperator op, object? filterValue)
    {
        return op switch
        {
            FilterOperator.Equals => _typeDetector.ValuesAreEqual(cellValue, filterValue),
            FilterOperator.NotEquals => !_typeDetector.ValuesAreEqual(cellValue, filterValue),
            FilterOperator.Contains => StringContains(cellValue, filterValue),
            FilterOperator.NotContains => !StringContains(cellValue, filterValue),
            FilterOperator.StartsWith => StringStartsWith(cellValue, filterValue),
            FilterOperator.EndsWith => StringEndsWith(cellValue, filterValue),
            FilterOperator.GreaterThan => _typeDetector.CompareValues(cellValue, filterValue) > 0,
            FilterOperator.GreaterThanOrEqual => _typeDetector.CompareValues(cellValue, filterValue) >= 0,
            FilterOperator.LessThan => _typeDetector.CompareValues(cellValue, filterValue) < 0,
            FilterOperator.LessThanOrEqual => _typeDetector.CompareValues(cellValue, filterValue) <= 0,
            FilterOperator.IsNull => cellValue == null,
            FilterOperator.IsNotNull => cellValue != null,
            FilterOperator.IsEmpty => _typeDetector.IsValueEmpty(cellValue),
            FilterOperator.IsNotEmpty => !_typeDetector.IsValueEmpty(cellValue),
            FilterOperator.In => ValueInList(cellValue, filterValue),
            FilterOperator.Regex => ValueMatchesRegex(cellValue, filterValue),
            _ => throw new NotSupportedException($"Filter operator {op} not supported")
        };
    }

    #region String Operations

    private bool StringContains(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";
        return cellStr.Contains(filterStr, StringComparison.OrdinalIgnoreCase);
    }

    private bool StringStartsWith(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";
        return cellStr.StartsWith(filterStr, StringComparison.OrdinalIgnoreCase);
    }

    private bool StringEndsWith(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";
        return cellStr.EndsWith(filterStr, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region IN and REGEXP Operations

    /// <summary>
    /// Checks if cellValue is in the list of filterValues (IN operator)
    /// Supports List, IEnumerable, array, or single comma-separated string
    /// Example: Status IN ('Active', 'Pending', 'Completed')
    /// </summary>
    private bool ValueInList(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        // Convert cellValue to string for comparison
        var cellStr = cellValue.ToString() ?? "";

        // Handle different filterValue types
        if (filterValue is System.Collections.IEnumerable enumerable && !(filterValue is string))
        {
            // filterValue is a collection (List<string>, string[], etc.)
            foreach (var item in enumerable)
            {
                var itemStr = item?.ToString() ?? "";
                if (string.Equals(cellStr, itemStr, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        else if (filterValue is string filterStr)
        {
            // filterValue is a single string (might be comma-separated)
            // Split by comma and check each value
            var values = filterStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var value in values)
            {
                if (string.Equals(cellStr, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // Fallback: direct equality check
        return string.Equals(cellStr, filterValue.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if cellValue matches the regex pattern (REGEXP operator)
    /// Example: Email REGEXP '^test.*@example\.com$'
    /// </summary>
    private bool ValueMatchesRegex(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var patternStr = filterValue.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(patternStr))
            return false;

        try
        {
            var regex = new System.Text.RegularExpressions.Regex(
                patternStr,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

            return regex.IsMatch(cellStr);
        }
        catch (System.Text.RegularExpressions.RegexParseException ex)
        {
            _logger?.LogWarning(ex, "Invalid regex pattern: {Pattern}", patternStr);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error evaluating regex pattern: {Pattern}", patternStr);
            return false;
        }
    }

    #endregion
}

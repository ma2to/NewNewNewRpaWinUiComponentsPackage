using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Models;

/// <summary>
/// Defines the type of filter expression node
/// Supports complex logical expressions: ((A and B) or (C and (D or E)))
/// </summary>
internal enum FilterExpressionType
{
    /// <summary>Simple condition (Column > Value)</summary>
    Condition,

    /// <summary>Logical AND - all conditions must be true</summary>
    And,

    /// <summary>Logical OR - at least one condition must be true</summary>
    Or,

    /// <summary>Logical AND with short-circuit evaluation</summary>
    AndAlso,

    /// <summary>Logical OR with short-circuit evaluation</summary>
    OrElse
}

/// <summary>
/// Base class for filter expression tree nodes
/// Implements Composite pattern for complex filter expressions
/// </summary>
internal abstract class FilterExpression
{
    /// <summary>Type of this expression node</summary>
    public abstract FilterExpressionType Type { get; }

    /// <summary>
    /// Accepts visitor for tree traversal
    /// Used by evaluator and SQL generator
    /// </summary>
    public abstract T Accept<T>(IFilterExpressionVisitor<T> visitor);
}

/// <summary>
/// Leaf node: simple filter condition (e.g., Age > 18, City = 'Bratislava')
/// </summary>
internal sealed class FilterCondition : FilterExpression
{
    public override FilterExpressionType Type => FilterExpressionType.Condition;

    /// <summary>Column name to filter on</summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>Filter operator (Equals, GreaterThan, Contains, etc.)</summary>
    public FilterOperator Operator { get; init; }

    /// <summary>Value to compare against</summary>
    public object? Value { get; init; }

    /// <summary>
    /// Optional: .NET type of the column for type-aware filtering
    /// Used to determine parsing strategy (number, datetime dd.MM.yyyy, text)
    /// </summary>
    public Type? ColumnType { get; init; }

    public override T Accept<T>(IFilterExpressionVisitor<T> visitor)
    {
        return visitor.VisitCondition(this);
    }

    public override string ToString()
    {
        return $"{ColumnName} {Operator} {Value}";
    }
}

/// <summary>
/// Branch node: binary logical operation (AND, OR, ANDALSO, ORELSE)
/// Left and Right are sub-expressions (can be conditions or other binary expressions)
/// </summary>
internal sealed class FilterBinaryExpression : FilterExpression
{
    private readonly FilterExpressionType _type;

    public override FilterExpressionType Type => _type;

    public FilterBinaryExpression()
    {
        _type = FilterExpressionType.And;
    }

    public FilterBinaryExpression(FilterExpressionType type, FilterExpression left, FilterExpression right)
    {
        _type = type;
        Left = left;
        Right = right;
    }

    /// <summary>Left sub-expression</summary>
    public FilterExpression Left { get; init; } = null!;

    /// <summary>Right sub-expression</summary>
    public FilterExpression Right { get; init; } = null!;

    public override T Accept<T>(IFilterExpressionVisitor<T> visitor)
    {
        return visitor.VisitBinaryExpression(this);
    }

    public override string ToString()
    {
        var operatorStr = Type switch
        {
            FilterExpressionType.And => "AND",
            FilterExpressionType.Or => "OR",
            FilterExpressionType.AndAlso => "ANDALSO",
            FilterExpressionType.OrElse => "ORELSE",
            _ => "UNKNOWN"
        };

        return $"({Left} {operatorStr} {Right})";
    }
}

/// <summary>
/// Visitor pattern interface for filter expression traversal
/// Implemented by evaluator (in-memory) and SQL generator (HybridRowStore)
/// </summary>
internal interface IFilterExpressionVisitor<T>
{
    /// <summary>Visit a simple condition node</summary>
    T VisitCondition(FilterCondition condition);

    /// <summary>Visit a binary expression node (AND/OR/ANDALSO/ORELSE)</summary>
    T VisitBinaryExpression(FilterBinaryExpression expression);
}

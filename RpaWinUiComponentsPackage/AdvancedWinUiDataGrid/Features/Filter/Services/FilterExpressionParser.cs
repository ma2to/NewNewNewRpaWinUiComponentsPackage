using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Services;

/// <summary>
/// Parses filter expression strings into expression tree
/// Supports complex logical expressions: ((Age > 18 AND City = 'Bratislava') OR (Status = 'Active' AND (Premium = true OR VIP = true)))
/// Grammar:
///   Expression     ::= OrElseExpr
///   OrElseExpr     ::= AndAlsoExpr ( 'ORELSE' AndAlsoExpr )*
///   AndAlsoExpr    ::= OrExpr ( 'ANDALSO' OrExpr )*
///   OrExpr         ::= AndExpr ( 'OR' AndExpr )*
///   AndExpr        ::= Primary ( 'AND' Primary )*
///   Primary        ::= '(' Expression ')' | Condition
///   Condition      ::= ColumnName Operator Value
///   Operator       ::= '=' | '!=' | '>' | '<' | '>=' | '<='
/// </summary>
internal sealed class FilterExpressionParser
{
    private readonly ILogger<FilterExpressionParser>? _logger;
    private readonly FilterTypeDetector _typeDetector;

    private string[] _tokens = Array.Empty<string>();
    private int _currentTokenIndex;

    public FilterExpressionParser(ILogger<FilterExpressionParser>? logger = null)
    {
        _logger = logger;
        _typeDetector = new FilterTypeDetector(null); // Different logger type
    }

    /// <summary>
    /// Parses filter expression string into expression tree
    /// Example: "((Age > 18 AND City = 'Bratislava') OR (Status = 'Active'))"
    /// </summary>
    public FilterExpression Parse(string expressionString)
    {
        if (string.IsNullOrWhiteSpace(expressionString))
        {
            throw new ArgumentException("Filter expression cannot be empty", nameof(expressionString));
        }

        _logger?.LogInformation("Parsing filter expression: {Expression}", expressionString);

        // Tokenize input
        _tokens = Tokenize(expressionString);
        _currentTokenIndex = 0;

        // Parse expression tree
        var expression = ParseOrElseExpression();

        // Ensure all tokens consumed
        if (_currentTokenIndex < _tokens.Length)
        {
            throw new FormatException($"Unexpected token at position {_currentTokenIndex}: {_tokens[_currentTokenIndex]}");
        }

        _logger?.LogInformation("Successfully parsed filter expression: {Expression}", expression);
        return expression;
    }

    #region Tokenization

    private string[] Tokenize(string input)
    {
        var tokens = new List<string>();
        var currentToken = "";
        var inQuotes = false;

        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];

            // Handle quoted strings
            if (ch == '\'')
            {
                inQuotes = !inQuotes;
                currentToken += ch;
                continue;
            }

            if (inQuotes)
            {
                currentToken += ch;
                continue;
            }

            // Handle delimiters
            if (char.IsWhiteSpace(ch) || ch == '(' || ch == ')')
            {
                if (!string.IsNullOrEmpty(currentToken))
                {
                    tokens.Add(currentToken);
                    currentToken = "";
                }

                if (ch == '(' || ch == ')')
                {
                    tokens.Add(ch.ToString());
                }

                continue;
            }

            // Handle operators (>=, <=, !=)
            if (ch == '>' || ch == '<' || ch == '!' || ch == '=')
            {
                if (!string.IsNullOrEmpty(currentToken))
                {
                    tokens.Add(currentToken);
                    currentToken = "";
                }

                // Check for two-character operators
                if (i + 1 < input.Length && input[i + 1] == '=')
                {
                    tokens.Add($"{ch}=");
                    i++; // Skip next char
                }
                else
                {
                    tokens.Add(ch.ToString());
                }

                continue;
            }

            currentToken += ch;
        }

        if (!string.IsNullOrEmpty(currentToken))
        {
            tokens.Add(currentToken);
        }

        _logger?.LogDebug("Tokenized expression into {Count} tokens: {Tokens}", tokens.Count, string.Join(" | ", tokens));
        return tokens.ToArray();
    }

    #endregion

    #region Recursive Descent Parser

    /// <summary>
    /// OrElseExpr ::= AndAlsoExpr ( 'ORELSE' AndAlsoExpr )*
    /// </summary>
    private FilterExpression ParseOrElseExpression()
    {
        var left = ParseAndAlsoExpression();

        while (CurrentTokenIs("ORELSE"))
        {
            Consume("ORELSE");
            var right = ParseAndAlsoExpression();
            left = new FilterBinaryExpression(FilterExpressionType.OrElse, left, right);
        }

        return left;
    }

    /// <summary>
    /// AndAlsoExpr ::= OrExpr ( 'ANDALSO' OrExpr )*
    /// </summary>
    private FilterExpression ParseAndAlsoExpression()
    {
        var left = ParseOrExpression();

        while (CurrentTokenIs("ANDALSO"))
        {
            Consume("ANDALSO");
            var right = ParseOrExpression();
            left = new FilterBinaryExpression(FilterExpressionType.AndAlso, left, right);
        }

        return left;
    }

    /// <summary>
    /// OrExpr ::= AndExpr ( 'OR' AndExpr )*
    /// </summary>
    private FilterExpression ParseOrExpression()
    {
        var left = ParseAndExpression();

        while (CurrentTokenIs("OR"))
        {
            Consume("OR");
            var right = ParseAndExpression();
            left = new FilterBinaryExpression(FilterExpressionType.Or, left, right);
        }

        return left;
    }

    /// <summary>
    /// AndExpr ::= Primary ( 'AND' Primary )*
    /// </summary>
    private FilterExpression ParseAndExpression()
    {
        var left = ParsePrimary();

        while (CurrentTokenIs("AND"))
        {
            Consume("AND");
            var right = ParsePrimary();
            left = new FilterBinaryExpression(FilterExpressionType.And, left, right);
        }

        return left;
    }

    /// <summary>
    /// Primary ::= '(' Expression ')' | Condition
    /// </summary>
    private FilterExpression ParsePrimary()
    {
        // Handle parenthesized expressions
        if (CurrentTokenIs("("))
        {
            Consume("(");
            var expression = ParseOrElseExpression();
            Consume(")");
            return expression;
        }

        // Parse condition: ColumnName Operator Value
        return ParseCondition();
    }

    /// <summary>
    /// Condition ::= ColumnName Operator Value
    /// </summary>
    private FilterCondition ParseCondition()
    {
        // 1. Column name
        var columnName = ConsumeToken();

        // 2. Operator
        var operatorToken = ConsumeToken();
        var filterOperator = ParseOperator(operatorToken);

        // 3. Value
        var valueToken = ConsumeToken();
        var value = ParseValue(valueToken);

        return new FilterCondition
        {
            ColumnName = columnName,
            Operator = filterOperator,
            Value = value,
            ColumnType = _typeDetector.DetectType(value)
        };
    }

    private FilterOperator ParseOperator(string operatorToken)
    {
        return operatorToken.ToUpperInvariant() switch
        {
            "=" => FilterOperator.Equals,
            "!=" => FilterOperator.NotEquals,
            ">" => FilterOperator.GreaterThan,
            "<" => FilterOperator.LessThan,
            ">=" => FilterOperator.GreaterThanOrEqual,
            "<=" => FilterOperator.LessThanOrEqual,
            "CONTAINS" => FilterOperator.Contains,
            "STARTSWITH" => FilterOperator.StartsWith,
            "ENDSWITH" => FilterOperator.EndsWith,
            "IN" => FilterOperator.In,
            "REGEX" => FilterOperator.Regex,
            _ => throw new FormatException($"Unknown operator: {operatorToken}")
        };
    }

    private object? ParseValue(string valueToken)
    {
        // Remove quotes from string values
        if (valueToken.StartsWith("'") && valueToken.EndsWith("'"))
        {
            return valueToken[1..^1]; // Remove surrounding quotes
        }

        // Try parse as number
        if (_typeDetector.TryGetNumericValue(valueToken, out var numericValue))
        {
            return numericValue;
        }

        // Try parse as datetime (dd.MM.yyyy)
        if (_typeDetector.TryGetDateTimeValue(valueToken, out var dateTimeValue))
        {
            return dateTimeValue;
        }

        // Try parse as boolean
        if (bool.TryParse(valueToken, out var boolValue))
        {
            return boolValue;
        }

        // Default to string
        return valueToken;
    }

    #endregion

    #region Token Navigation

    private string CurrentToken()
    {
        if (_currentTokenIndex >= _tokens.Length)
        {
            throw new FormatException($"Unexpected end of expression at position {_currentTokenIndex}");
        }

        return _tokens[_currentTokenIndex];
    }

    private bool CurrentTokenIs(string expected)
    {
        if (_currentTokenIndex >= _tokens.Length)
            return false;

        return string.Equals(_tokens[_currentTokenIndex], expected, StringComparison.OrdinalIgnoreCase);
    }

    private string ConsumeToken()
    {
        var token = CurrentToken();
        _currentTokenIndex++;
        return token;
    }

    private void Consume(string expected)
    {
        if (!CurrentTokenIs(expected))
        {
            throw new FormatException($"Expected '{expected}' but found '{CurrentToken()}' at position {_currentTokenIndex}");
        }

        _currentTokenIndex++;
    }

    #endregion
}

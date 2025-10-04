using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// Color mode enumeration
/// </summary>
internal enum ColorMode
{
    Cell,
    Row,
    Column,
    Conditional
}

/// <summary>
/// Color target enumeration
/// </summary>
internal enum ColorTarget
{
    Background,
    Foreground,
    Border
}

/// <summary>
/// Conditional formatting rule type
/// </summary>
internal enum ConditionalFormattingRule
{
    Equals,
    NotEquals,
    Contains,
    GreaterThan,
    LessThan,
    Between,
    IsEmpty,
    IsNotEmpty
}

/// <summary>
/// Color configuration for cells/rows/columns
/// </summary>
internal sealed record ColorConfiguration
{
    public string? BackgroundColor { get; init; }
    public string? ForegroundColor { get; init; }
    public string? BorderColor { get; init; }
    public ColorMode Mode { get; init; } = ColorMode.Cell;
    public int? RowIndex { get; init; }
    public int? ColumnIndex { get; init; }
    public string? ColumnName { get; init; }

    public static ColorConfiguration CreateCellColor(int rowIndex, int columnIndex, string backgroundColor, string? foregroundColor = null) =>
        new()
        {
            Mode = ColorMode.Cell,
            RowIndex = rowIndex,
            ColumnIndex = columnIndex,
            BackgroundColor = backgroundColor,
            ForegroundColor = foregroundColor
        };

    public static ColorConfiguration CreateRowColor(int rowIndex, string backgroundColor, string? foregroundColor = null) =>
        new()
        {
            Mode = ColorMode.Row,
            RowIndex = rowIndex,
            BackgroundColor = backgroundColor,
            ForegroundColor = foregroundColor
        };

    public static ColorConfiguration CreateColumnColor(string columnName, string backgroundColor, string? foregroundColor = null) =>
        new()
        {
            Mode = ColorMode.Column,
            ColumnName = columnName,
            BackgroundColor = backgroundColor,
            ForegroundColor = foregroundColor
        };
}

/// <summary>
/// Conditional format rule with condition
/// </summary>
internal sealed record ConditionalFormatRule
{
    public required string ColumnName { get; init; }
    public required ConditionalFormattingRule Rule { get; init; }
    public object? Value { get; init; }
    public object? SecondValue { get; init; }
    public required ColorConfiguration ColorConfig { get; init; }

    public static ConditionalFormatRule Create(string columnName, ConditionalFormattingRule rule, object? value, ColorConfiguration colorConfig) =>
        new()
        {
            ColumnName = columnName,
            Rule = rule,
            Value = value,
            ColorConfig = colorConfig
        };
}

/// <summary>
/// Result of color operation
/// </summary>
internal sealed record ColorResult
{
    public bool Success { get; init; }
    public int AffectedCells { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }

    public static ColorResult CreateSuccess(int affectedCells, TimeSpan duration) =>
        new()
        {
            Success = true,
            AffectedCells = affectedCells,
            Duration = duration
        };

    public static ColorResult CreateFailure(string errorMessage) =>
        new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
}

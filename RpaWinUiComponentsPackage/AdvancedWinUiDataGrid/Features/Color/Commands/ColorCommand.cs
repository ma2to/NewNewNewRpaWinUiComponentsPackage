using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Commands;

/// <summary>
/// Command for applying color to cells/rows/columns
/// </summary>
internal sealed record ApplyColorCommand
{
    internal required ColorConfiguration ColorConfig { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static ApplyColorCommand Create(ColorConfiguration colorConfig) =>
        new() { ColorConfig = colorConfig };
}

/// <summary>
/// Command for applying conditional formatting
/// </summary>
internal sealed record ApplyConditionalFormattingCommand
{
    internal required IReadOnlyList<ConditionalFormatRule> Rules { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static ApplyConditionalFormattingCommand Create(IReadOnlyList<ConditionalFormatRule> rules) =>
        new() { Rules = rules };
}

/// <summary>
/// Command for clearing color
/// </summary>
internal sealed record ClearColorCommand
{
    internal ColorMode Mode { get; init; } = ColorMode.Cell;
    internal int? RowIndex { get; init; }
    internal int? ColumnIndex { get; init; }
    internal string? ColumnName { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static ClearColorCommand Create(ColorMode mode) =>
        new() { Mode = mode };

    internal static ClearColorCommand ForCell(int rowIndex, int columnIndex) =>
        new() { Mode = ColorMode.Cell, RowIndex = rowIndex, ColumnIndex = columnIndex };

    internal static ClearColorCommand ForRow(int rowIndex) =>
        new() { Mode = ColorMode.Row, RowIndex = rowIndex };

    internal static ClearColorCommand ForColumn(string columnName) =>
        new() { Mode = ColorMode.Column, ColumnName = columnName };
}

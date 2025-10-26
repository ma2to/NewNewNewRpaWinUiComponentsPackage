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

    /// <summary>
    /// WARNING: Unstable - changes on sort/filter/delete. Use RowId instead.
    /// </summary>
    internal int? RowIndex { get; init; }

    /// <summary>
    /// WARNING: Unstable - changes on column reorder/hide. Use ColumnName instead.
    /// </summary>
    internal int? ColumnIndex { get; init; }

    /// <summary>
    /// Stable row identifier (from __rowId field)
    /// When both RowId and RowIndex are set, RowId takes precedence.
    /// </summary>
    internal string? RowId { get; init; }

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

    // NEW: Stable rowId-based factory methods
    internal static ClearColorCommand ForCellByRowId(string rowId, string columnName) =>
        new() { Mode = ColorMode.Cell, RowId = rowId, ColumnName = columnName };

    internal static ClearColorCommand ForRowByRowId(string rowId) =>
        new() { Mode = ColorMode.Row, RowId = rowId };
}

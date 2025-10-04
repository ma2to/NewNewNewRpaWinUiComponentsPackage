using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowColumnCell.Commands;

/// <summary>
/// Command for batch cell updates
/// </summary>
internal sealed record BatchUpdateCellsCommand
{
    internal required IReadOnlyList<BatchCellOperation> Operations { get; init; }
    internal bool ValidateBeforeCommit { get; init; } = true;
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static BatchUpdateCellsCommand Create(IReadOnlyList<BatchCellOperation> operations) =>
        new() { Operations = operations };
}

/// <summary>
/// Command for batch row insertion
/// </summary>
internal sealed record BatchInsertRowsCommand
{
    internal required IReadOnlyList<BatchRowOperation> Operations { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static BatchInsertRowsCommand Create(IReadOnlyList<BatchRowOperation> operations) =>
        new() { Operations = operations };
}

/// <summary>
/// Command for batch row deletion
/// </summary>
internal sealed record BatchDeleteRowsCommand
{
    internal required IReadOnlyList<int> RowIndices { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static BatchDeleteRowsCommand Create(IReadOnlyList<int> rowIndices) =>
        new() { RowIndices = rowIndices };
}

/// <summary>
/// Command for batch column updates
/// </summary>
internal sealed record BatchUpdateColumnsCommand
{
    internal required IReadOnlyList<BatchColumnOperation> Operations { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static BatchUpdateColumnsCommand Create(IReadOnlyList<BatchColumnOperation> operations) =>
        new() { Operations = operations };
}

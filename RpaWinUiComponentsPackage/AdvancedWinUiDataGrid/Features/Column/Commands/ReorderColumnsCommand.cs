namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Commands;

/// <summary>
/// Command for reordering columns
/// </summary>
internal record ReorderColumnsCommand(
    IReadOnlyList<string> NewColumnOrder
);
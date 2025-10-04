namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Commands;

/// <summary>
/// Command for removing a column
/// </summary>
internal record RemoveColumnCommand(
    string ColumnName
);
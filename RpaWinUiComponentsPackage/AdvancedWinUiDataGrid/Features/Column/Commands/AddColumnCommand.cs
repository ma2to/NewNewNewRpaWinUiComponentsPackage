using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Commands;

/// <summary>
/// Command for adding a new column
/// </summary>
internal record AddColumnCommand(
    ColumnDefinition ColumnDefinition,
    object? DefaultValue = null
);
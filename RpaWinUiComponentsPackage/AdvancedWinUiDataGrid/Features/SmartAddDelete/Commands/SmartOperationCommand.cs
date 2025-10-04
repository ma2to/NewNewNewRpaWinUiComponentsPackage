using System;
using System.Collections.Generic;
using System.Threading;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Commands;

/// <summary>
/// COMMAND PATTERN: Smart add rows internal command
/// CONSISTENT: Similar structure to SortCommand and SearchCommand
/// </summary>
internal sealed record SmartAddRowsInternalCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> DataToAdd { get; init; }
    internal required RowManagementConfiguration Configuration { get; init; }
    internal bool PreserveRowNumbers { get; init; } = true;
    internal IProgress<RowManagementProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static SmartAddRowsInternalCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> dataToAdd,
        RowManagementConfiguration configuration) =>
        new() { DataToAdd = dataToAdd, Configuration = configuration };
}

/// <summary>
/// COMMAND PATTERN: Smart delete rows internal command
/// ENTERPRISE: Context-aware delete with minimum row enforcement
/// </summary>
internal sealed record SmartDeleteRowsInternalCommand
{
    internal required IReadOnlyList<int> RowIndexesToDelete { get; init; }
    internal required RowManagementConfiguration Configuration { get; init; }
    internal int CurrentRowCount { get; init; }
    internal bool ForcePhysicalDelete { get; init; } = false;
    internal IProgress<RowManagementProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static SmartDeleteRowsInternalCommand Create(
        IReadOnlyList<int> rowIndexesToDelete,
        RowManagementConfiguration configuration,
        int currentRowCount) =>
        new()
        {
            RowIndexesToDelete = rowIndexesToDelete,
            Configuration = configuration,
            CurrentRowCount = currentRowCount
        };
}

/// <summary>
/// COMMAND PATTERN: Auto-expand empty row internal command
/// SMART: Automatic empty row maintenance
/// </summary>
internal sealed record AutoExpandEmptyRowInternalCommand
{
    internal required RowManagementConfiguration Configuration { get; init; }
    internal int CurrentRowCount { get; init; }
    internal bool TriggerExpansion { get; init; } = true;
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static AutoExpandEmptyRowInternalCommand Create(
        RowManagementConfiguration configuration,
        int currentRowCount) =>
        new()
        {
            Configuration = configuration,
            CurrentRowCount = currentRowCount
        };
}

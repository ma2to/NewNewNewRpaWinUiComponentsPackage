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

    /// <summary>
    /// PERFORMANCE: Skip automatic validation after delete for rapid operations.
    /// When true, validation is skipped (user can manually trigger validation after bulk operations).
    /// When false (default), validation runs with debounce to avoid blocking UI.
    /// </summary>
    internal bool SkipAutomaticValidation { get; init; } = false;

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
/// COMMAND PATTERN: Smart delete rows by ID internal command
/// ROBUST: Uses stable row IDs instead of indices to avoid index shifting bugs
/// </summary>
internal sealed record SmartDeleteRowsByIdInternalCommand
{
    internal required IReadOnlyList<string> RowIdsToDelete { get; init; }
    internal required RowManagementConfiguration Configuration { get; init; }
    internal int CurrentRowCount { get; init; }
    internal bool ForcePhysicalDelete { get; init; } = false;
    internal IProgress<RowManagementProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    /// <summary>
    /// PERFORMANCE: Skip automatic validation after delete for rapid operations.
    /// When true, validation is skipped (user can manually trigger validation after bulk operations).
    /// When false (default), validation runs with debounce to avoid blocking UI.
    /// </summary>
    internal bool SkipAutomaticValidation { get; init; } = false;

    internal static SmartDeleteRowsByIdInternalCommand Create(
        IReadOnlyList<string> rowIdsToDelete,
        RowManagementConfiguration configuration,
        int currentRowCount) =>
        new()
        {
            RowIdsToDelete = rowIdsToDelete,
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

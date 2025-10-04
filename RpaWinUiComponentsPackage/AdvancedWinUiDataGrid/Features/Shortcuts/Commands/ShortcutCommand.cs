using System;
using System.Collections.Generic;
using System.Threading;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts.Commands;

/// <summary>
/// Simplified shortcut execution command
/// </summary>
internal sealed record ShortcutCommand
{
    internal required string ShortcutName { get; init; }
    internal IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
    internal IProgress<ShortcutProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static ShortcutCommand Create(string name) =>
        new() { ShortcutName = name };
}

/// <summary>
/// COMMAND PATTERN: Register single shortcut command
/// CONSISTENT: Rovnaká štruktúra ako SearchCommand a SortCommand
/// </summary>
internal sealed record RegisterShortcutInternalCommand
{
    internal required ShortcutDefinition ShortcutDefinition { get; init; }
    internal bool OverrideExisting { get; init; } = false;
    internal bool ValidateConflicts { get; init; } = true;
    internal IProgress<ShortcutProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static RegisterShortcutInternalCommand Create(ShortcutDefinition definition) =>
        new() { ShortcutDefinition = definition };
}

/// <summary>
/// COMMAND PATTERN: Execute shortcut command
/// ENTERPRISE: Context-aware shortcut execution
/// </summary>
internal sealed record ExecuteShortcutInternalCommand
{
    internal required KeyCombination KeyCombination { get; init; }
    internal required ShortcutExecutionContext ExecutionContext { get; init; }
    internal bool StrictContextMatch { get; init; } = true;
    internal TimeSpan? ExecutionTimeout { get; init; }
    internal IProgress<ShortcutProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static ExecuteShortcutInternalCommand Create(
        KeyCombination keyCombination,
        ShortcutExecutionContext context) =>
        new() { KeyCombination = keyCombination, ExecutionContext = context };
}

/// <summary>
/// COMMAND PATTERN: Bulk shortcut registration command
/// PERFORMANCE: Efficient registration of multiple shortcuts
/// </summary>
internal sealed record RegisterShortcutsInternalCommand
{
    internal required IReadOnlyList<ShortcutDefinition> ShortcutDefinitions { get; init; }
    internal bool ClearExisting { get; init; } = false;
    internal bool ValidateConflicts { get; init; } = true;
    internal ShortcutExecutionMode DefaultExecutionMode { get; init; } = ShortcutExecutionMode.Immediate;
    internal IProgress<ShortcutProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static RegisterShortcutsInternalCommand Create(IReadOnlyList<ShortcutDefinition> definitions) =>
        new() { ShortcutDefinitions = definitions };
}

/// <summary>
/// COMMAND PATTERN: Unregister shortcut command
/// MANAGEMENT: Dynamic shortcut removal
/// </summary>
internal sealed record UnregisterShortcutCommand
{
    internal required KeyCombination KeyCombination { get; init; }
    internal ShortcutContext? Context { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static UnregisterShortcutCommand Create(KeyCombination keyCombination) =>
        new() { KeyCombination = keyCombination };
}

/// <summary>
/// COMMAND PATTERN: Get registered shortcuts query command
/// INTROSPECTION: Retrieve shortcuts by context
/// </summary>
internal sealed record GetShortcutsCommand
{
    internal ShortcutContext? Context { get; init; }
    internal bool IncludeDisabled { get; init; } = false;
    internal ShortcutCategory? Category { get; init; }

    internal static GetShortcutsCommand All => new();

    internal static GetShortcutsCommand ByContext(ShortcutContext context) =>
        new() { Context = context };
}

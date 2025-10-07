using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts.Commands;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts.Interfaces;

/// <summary>
/// Internal service interface for keyboard shortcuts management
/// </summary>
internal interface IShortcutService
{
    /// <summary>
    /// Execute a predefined shortcut by name
    /// </summary>
    Task<ShortcutResult> ExecuteShortcutAsync(ShortcutCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a shortcut by key combination
    /// </summary>
    Task<ShortcutResult> ExecuteShortcutByKeyAsync(ExecuteShortcutCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register a new shortcut definition
    /// </summary>
    Task<ShortcutRegistrationResult> RegisterShortcutAsync(RegisterShortcutCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register multiple shortcuts at once
    /// </summary>
    Task<ShortcutRegistrationResult> RegisterShortcutsAsync(RegisterShortcutsCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregister a shortcut by key combination
    /// </summary>
    Task<bool> UnregisterShortcutAsync(KeyCombination keyCombination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all registered shortcuts
    /// </summary>
    IReadOnlyList<ShortcutDefinition> GetRegisteredShortcuts(ShortcutContext? context = null);

    /// <summary>
    /// Validate shortcut conflicts
    /// </summary>
    Task<IReadOnlyList<string>> ValidateShortcutConflictsAsync(IReadOnlyList<ShortcutDefinition> shortcuts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all registered shortcuts with full details
    /// </summary>
    IReadOnlyList<ShortcutDefinition> GetAllShortcuts();
}

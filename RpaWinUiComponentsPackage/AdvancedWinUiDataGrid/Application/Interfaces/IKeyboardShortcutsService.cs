using System;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Keyboard shortcuts functionality
/// CLEAN ARCHITECTURE: Application layer interface for keyboard shortcut operations
/// </summary>
internal interface IKeyboardShortcutsService
{
    // Shortcut registration operations
    Task<KeyboardShortcutResult> RegisterShortcutAsync(
        KeyboardShortcut shortcut,
        Func<Task> action,
        CancellationToken cancellationToken = default);

    // Shortcut execution operations
    Task<KeyboardShortcutResult> ExecuteShortcutAsync(
        string keysCombination,
        CancellationToken cancellationToken = default);

    // Configuration operations
    Task<KeyboardShortcutResult> ApplyConfigurationAsync(
        KeyboardShortcutConfiguration configuration,
        CancellationToken cancellationToken = default);

    KeyboardShortcutConfiguration GetCurrentConfiguration();

    // Utility operations
    bool IsShortcutRegistered(string keysCombination);
    bool IsValidKeyCombination(string keysCombination);
}


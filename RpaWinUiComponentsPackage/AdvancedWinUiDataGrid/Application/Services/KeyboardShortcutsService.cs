using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// INTERNAL SERVICE: Keyboard shortcuts operations implementation
/// CLEAN ARCHITECTURE: Application layer service for keyboard shortcut operations
/// </summary>
internal sealed class KeyboardShortcutsService : IKeyboardShortcutsService
{
    private readonly ConcurrentDictionary<string, Func<Task>> _registeredShortcuts = new();
    private KeyboardShortcutConfiguration _currentConfiguration = KeyboardShortcutConfiguration.CreateDefault();

    public async Task<KeyboardShortcutResult> RegisterShortcutAsync(
        KeyboardShortcut shortcut,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!IsValidKeyCombination(shortcut.KeyCombination))
                {
                    return KeyboardShortcutResult.Failure($"Invalid key combination: {shortcut.KeyCombination}");
                }

                _registeredShortcuts.AddOrUpdate(shortcut.KeyCombination, action, (key, oldAction) => action);

                stopwatch.Stop();
                return KeyboardShortcutResult.CreateSuccess($"Registered shortcut: {shortcut.KeyCombination}", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return KeyboardShortcutResult.Failure($"Failed to register shortcut: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<KeyboardShortcutResult> ExecuteShortcutAsync(
        string keysCombination,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!_registeredShortcuts.TryGetValue(keysCombination, out var action))
            {
                stopwatch.Stop();
                return KeyboardShortcutResult.Failure($"Shortcut not found: {keysCombination}");
            }

            await action();

            stopwatch.Stop();
            return KeyboardShortcutResult.CreateSuccess($"Executed shortcut: {keysCombination}", stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return KeyboardShortcutResult.Failure($"Failed to execute shortcut: {ex.Message}");
        }
    }

    public async Task<KeyboardShortcutResult> ApplyConfigurationAsync(
        KeyboardShortcutConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _currentConfiguration = configuration;

                // Clear existing shortcuts and register new ones based on configuration
                _registeredShortcuts.Clear();

                // Register default shortcuts based on configuration
                if (configuration.EnableNavigationShortcuts)
                {
                    RegisterDefaultNavigationShortcuts();
                }

                if (configuration.EnableEditingShortcuts)
                {
                    RegisterDefaultEditingShortcuts();
                }

                if (configuration.EnableSelectionShortcuts)
                {
                    RegisterDefaultSelectionShortcuts();
                }

                if (configuration.EnableDataOperationShortcuts)
                {
                    RegisterDefaultDataOperationShortcuts();
                }

                stopwatch.Stop();
                return KeyboardShortcutResult.CreateSuccess("Configuration applied successfully", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return KeyboardShortcutResult.Failure($"Failed to apply configuration: {ex.Message}");
            }
        }, cancellationToken);
    }

    public KeyboardShortcutConfiguration GetCurrentConfiguration()
    {
        return _currentConfiguration;
    }

    public bool IsShortcutRegistered(string keysCombination)
    {
        return _registeredShortcuts.ContainsKey(keysCombination);
    }

    public bool IsValidKeyCombination(string keysCombination)
    {
        if (string.IsNullOrWhiteSpace(keysCombination))
            return false;

        // Basic validation - in a real implementation, would parse key combinations properly
        var validKeys = new[] { "Ctrl", "Shift", "Alt", "Enter", "Escape", "Delete", "F2", "Up", "Down", "Left", "Right", "PageUp", "PageDown", "A", "C", "V" };
        var parts = keysCombination.Split('+');

        foreach (var part in parts)
        {
            if (!Array.Exists(validKeys, key => string.Equals(key, part.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    private void RegisterDefaultNavigationShortcuts()
    {
        RegisterNoOpShortcut("Up");
        RegisterNoOpShortcut("Down");
        RegisterNoOpShortcut("Left");
        RegisterNoOpShortcut("Right");
        RegisterNoOpShortcut("PageUp");
        RegisterNoOpShortcut("PageDown");
    }

    private void RegisterDefaultEditingShortcuts()
    {
        RegisterNoOpShortcut("F2");
        RegisterNoOpShortcut("Enter");
        RegisterNoOpShortcut("Escape");
    }

    private void RegisterDefaultSelectionShortcuts()
    {
        RegisterNoOpShortcut("Ctrl+A");
        RegisterNoOpShortcut("Shift+Up");
        RegisterNoOpShortcut("Shift+Down");
    }

    private void RegisterDefaultDataOperationShortcuts()
    {
        RegisterNoOpShortcut("Ctrl+C");
        RegisterNoOpShortcut("Ctrl+V");
        RegisterNoOpShortcut("Delete");
    }

    private void RegisterNoOpShortcut(string keysCombination)
    {
        _registeredShortcuts.TryAdd(keysCombination, () => Task.CompletedTask);
    }
}
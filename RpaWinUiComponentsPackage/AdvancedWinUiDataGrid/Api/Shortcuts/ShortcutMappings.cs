using System.Windows.Input;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts.Commands;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Shortcuts;

/// <summary>
/// Extension methods for mapping shortcut-related types between public and internal representations
/// </summary>
internal static class ShortcutMappings
{
    /// <summary>
    /// Convert public PublicShortcutDefinition (from Api.Models) to internal RegisterShortcutCommand
    /// </summary>
    public static RegisterShortcutCommand ToInternal(this Api.Models.PublicShortcutDefinition publicShortcut)
    {
        var keyCombination = ParseKeyCombination(publicShortcut.ShortcutKey);

        var shortcutDefinition = new ShortcutDefinition
        {
            Name = publicShortcut.Name,
            KeyCombination = keyCombination,
            IsEnabled = publicShortcut.IsEnabled,
            Description = publicShortcut.Name // Use Name as Description
        };

        return RegisterShortcutCommand.Create(shortcutDefinition);
    }

    /// <summary>
    /// Convert public PublicShortcutDefinition (from Commands) to internal RegisterShortcutCommand
    /// </summary>
    public static RegisterShortcutCommand ToInternal(this PublicShortcutDefinition publicShortcut)
    {
        var keyCombination = ParseKeyCombination(publicShortcut.KeyCombination);

        var shortcutDefinition = new ShortcutDefinition
        {
            Name = publicShortcut.Name,
            KeyCombination = keyCombination,
            IsEnabled = publicShortcut.IsEnabled,
            Description = publicShortcut.Description
        };

        return RegisterShortcutCommand.Create(shortcutDefinition);
    }

    /// <summary>
    /// Convert internal ShortcutDefinition to public PublicShortcutDefinition
    /// </summary>
    public static PublicShortcutDefinition ToPublic(this ShortcutDefinition internalShortcut)
    {
        return new PublicShortcutDefinition
        {
            KeyCombination = internalShortcut.KeyCombination.DisplayName,
            Name = internalShortcut.Name,
            Description = internalShortcut.Description,
            IsEnabled = internalShortcut.IsEnabled
        };
    }

    /// <summary>
    /// Convert internal ShortcutRegistrationResult to public Result
    /// </summary>
    public static PublicResult ToPublic(this ShortcutRegistrationResult result)
    {
        if (result.Success)
        {
            return PublicResult.Success();
        }

        var errorMessage = result.ConflictMessages.Any()
            ? string.Join(", ", result.ConflictMessages)
            : "Shortcut registration failed";

        return PublicResult.Failure(errorMessage);
    }

    /// <summary>
    /// Parse string key combination to internal KeyCombination
    /// Supports formats like: "Ctrl+C", "Alt+Delete", "Shift+F1", "Ctrl+Shift+A"
    /// </summary>
    private static KeyCombination ParseKeyCombination(string shortcutKey)
    {
        if (string.IsNullOrWhiteSpace(shortcutKey))
        {
            return new KeyCombination();
        }

        var parts = shortcutKey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return new KeyCombination();
        }

        var modifiers = ModifierKeys.None;
        Key primaryKey = Key.None;

        foreach (var part in parts)
        {
            var upperPart = part.ToUpperInvariant();

            switch (upperPart)
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= ModifierKeys.Control;
                    break;

                case "SHIFT":
                    modifiers |= ModifierKeys.Shift;
                    break;

                case "ALT":
                    modifiers |= ModifierKeys.Alt;
                    break;

                case "WIN":
                case "WINDOWS":
                    modifiers |= ModifierKeys.Windows;
                    break;

                default:
                    // Try to parse as a Key enum value
                    if (Enum.TryParse<Key>(part, true, out var key))
                    {
                        primaryKey = key;
                    }
                    break;
            }
        }

        return KeyCombination.Create(primaryKey, modifiers);
    }
}

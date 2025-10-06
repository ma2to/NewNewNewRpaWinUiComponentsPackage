namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

/// <summary>
/// Public shortcut definition
/// </summary>
public sealed class PublicShortcutDefinition
{
    /// <summary>
    /// Shortcut key combination (e.g., "Ctrl+C", "Alt+Delete")
    /// </summary>
    public string ShortcutKey { get; init; } = string.Empty;

    /// <summary>
    /// Shortcut name/description
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Action to execute when shortcut is triggered
    /// </summary>
    public Action? Action { get; init; }

    /// <summary>
    /// Whether shortcut is enabled
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}

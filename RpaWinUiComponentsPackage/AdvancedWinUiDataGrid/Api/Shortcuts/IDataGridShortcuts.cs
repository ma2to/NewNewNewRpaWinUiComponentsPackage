using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Shortcuts;

/// <summary>
/// Public interface for DataGrid keyboard shortcuts.
/// Provides keyboard shortcut management and execution.
/// </summary>
public interface IDataGridShortcuts
{
    /// <summary>
    /// Registers a custom keyboard shortcut.
    /// </summary>
    /// <param name="shortcut">Shortcut definition</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> RegisterShortcutAsync(Api.Models.PublicShortcutDefinition shortcut, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a keyboard shortcut.
    /// </summary>
    /// <param name="shortcutKey">Shortcut key combination</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> UnregisterShortcutAsync(string shortcutKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered shortcuts.
    /// </summary>
    /// <returns>Collection of shortcut definitions</returns>
    IReadOnlyList<PublicShortcutDefinition> GetAllShortcuts();

    /// <summary>
    /// Enables or disables shortcuts.
    /// </summary>
    /// <param name="enabled">True to enable shortcuts</param>
    /// <returns>Result of the operation</returns>
    PublicResult SetShortcutsEnabled(bool enabled);

    /// <summary>
    /// Checks if shortcuts are enabled.
    /// </summary>
    /// <returns>True if shortcuts are enabled</returns>
    bool AreShortcutsEnabled();

    /// <summary>
    /// Resets all shortcuts to defaults.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ResetToDefaultShortcutsAsync(CancellationToken cancellationToken = default);
}

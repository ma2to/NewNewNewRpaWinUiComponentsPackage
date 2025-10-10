using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Shortcuts;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Shortcuts;

/// <summary>
/// Internal implementation of DataGrid keyboard shortcuts.
/// Delegates to internal shortcuts service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridShortcuts : IDataGridShortcuts
{
    private readonly ILogger<DataGridShortcuts>? _logger;
    private readonly IShortcutService _shortcutService;

    public DataGridShortcuts(
        IShortcutService shortcutService,
        ILogger<DataGridShortcuts>? logger = null)
    {
        _shortcutService = shortcutService ?? throw new ArgumentNullException(nameof(shortcutService));
        _logger = logger;
    }

    public async Task<PublicResult> RegisterShortcutAsync(PublicShortcutDefinition shortcut, CancellationToken cancellationToken = default)
    {
        try
        {
            if (shortcut == null)
                return PublicResult.Failure("Shortcut definition cannot be null");

            _logger?.LogInformation("Registering shortcut '{ShortcutKey}' via Shortcuts module", shortcut.ShortcutKey);

            var internalShortcut = shortcut.ToInternal();
            var internalResult = await _shortcutService.RegisterShortcutAsync(internalShortcut, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RegisterShortcut failed in Shortcuts module");
            throw;
        }
    }

    public async Task<PublicResult> UnregisterShortcutAsync(string shortcutKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Unregistering shortcut '{ShortcutKey}' via Shortcuts module", shortcutKey);

            // TODO: Need to convert shortcutKey to KeyCombination
            // For now, return not implemented
            await Task.CompletedTask;
            return PublicResult.Failure("UnregisterShortcut not yet implemented - key conversion needed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UnregisterShortcut failed in Shortcuts module");
            throw;
        }
    }

    public IReadOnlyList<PublicShortcutInfo> GetAllShortcuts()
    {
        try
        {
            var internalShortcuts = _shortcutService.GetAllShortcuts();
            return internalShortcuts.Select(s => s.ToPublic()).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetAllShortcuts failed in Shortcuts module");
            throw;
        }
    }

    public PublicResult SetShortcutsEnabled(bool enabled)
    {
        try
        {
            _logger?.LogInformation("Setting shortcuts enabled to {Enabled} via Shortcuts module", enabled);

            // TODO: Implement SetShortcutsEnabled in IShortcutService
            return PublicResult.Failure("SetShortcutsEnabled not yet implemented");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetShortcutsEnabled failed in Shortcuts module");
            throw;
        }
    }

    public bool AreShortcutsEnabled()
    {
        try
        {
            // TODO: Implement AreShortcutsEnabled in IShortcutService
            return true; // Default to enabled
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AreShortcutsEnabled check failed in Shortcuts module");
            throw;
        }
    }

    public async Task<PublicResult> ResetToDefaultShortcutsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Resetting to default shortcuts via Shortcuts module");

            // TODO: Implement ResetToDefaultShortcutsAsync in IShortcutService
            await Task.CompletedTask;
            return PublicResult.Failure("ResetToDefaultShortcuts not yet implemented");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ResetToDefaultShortcuts failed in Shortcuts module");
            throw;
        }
    }
}

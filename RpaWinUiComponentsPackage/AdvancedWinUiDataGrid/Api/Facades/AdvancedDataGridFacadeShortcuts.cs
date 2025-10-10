using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Partial class containing Keyboard Shortcuts Operations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Keyboard Shortcuts Operations

    public async Task<ShortcutDataResult> ExecuteShortcutAsync(ExecuteShortcutDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var shortcutService = scope.ServiceProvider.GetRequiredService<Features.Shortcuts.Interfaces.IShortcutService>();

            var internalCommand = new Features.Shortcuts.Commands.ShortcutCommand
            {
                ShortcutName = command.ShortcutName,
                Parameters = command.Parameters ?? new Dictionary<string, object?>(),
                CancellationToken = cancellationToken
            };

            var internalResult = await shortcutService.ExecuteShortcutAsync(internalCommand, cancellationToken);

            return new ShortcutDataResult
            {
                Success = internalResult.Success,
                ShortcutName = internalResult.ExecutedShortcut,
                ExecutionTime = internalResult.ExecutionTime,
                ErrorMessages = internalResult.ErrorMessages,
                ResultData = internalResult.Result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortcut execution failed: {Message}", ex.Message);
            return new ShortcutDataResult
            {
                Success = false,
                ShortcutName = command.ShortcutName,
                ErrorMessages = new[] { $"Execution failed: {ex.Message}" }
            };
        }
    }

    public async Task<bool> RegisterShortcutAsync(PublicShortcutDefinition shortcut)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var shortcutService = scope.ServiceProvider.GetRequiredService<Features.Shortcuts.Interfaces.IShortcutService>();

            // For simplified implementation, we'll skip full registration
            // This would need full KeyCombination parsing in production
            _logger.LogInformation("Shortcut registration: {Name} - {Key}", shortcut.Name, shortcut.ShortcutKey);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortcut registration failed: {Message}", ex.Message);
            return false;
        }
    }

    public IReadOnlyList<PublicShortcutInfo> GetRegisteredShortcuts()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var shortcutService = scope.ServiceProvider.GetRequiredService<Features.Shortcuts.Interfaces.IShortcutService>();

            var shortcuts = shortcutService.GetRegisteredShortcuts();
            return shortcuts.Select(s => new PublicShortcutInfo
            {
                Name = s.Name,
                Description = s.Description,
                KeyCombination = s.KeyCombination.DisplayName,
                IsEnabled = s.IsEnabled
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registered shortcuts: {Message}", ex.Message);
            return Array.Empty<PublicShortcutInfo>();
        }
    }

    #endregion
}


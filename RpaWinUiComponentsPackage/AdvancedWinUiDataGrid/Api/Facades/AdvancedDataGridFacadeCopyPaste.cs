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
/// Partial class containing Copy/Paste Operations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Copy/Paste Operations

    /// <summary>
    /// Sets clipboard content for copy/paste operations
    /// </summary>
    public void SetClipboard(object payload)
    {
        ThrowIfDisposed();

        var copyPasteService = _serviceProvider.GetRequiredService<ICopyPasteService>();
        copyPasteService.SetClipboard(payload);
        _logger.LogDebug("Clipboard content updated");
    }

    /// <summary>
    /// Gets clipboard content for copy/paste operations
    /// </summary>
    public object? GetClipboard()
    {
        ThrowIfDisposed();

        var copyPasteService = _serviceProvider.GetRequiredService<ICopyPasteService>();
        return copyPasteService.GetClipboard();
    }

    /// <summary>
    /// Copies selected data to clipboard
    /// </summary>
    public async Task<CopyPasteResult> CopyAsync(CopyDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.CopyPaste, nameof(CopyAsync));

        _logger.LogInformation("Starting copy operation [CorrelationId: {CorrelationId}]", command.CorrelationId);

        try
        {
            var copyPasteService = _serviceProvider.GetRequiredService<ICopyPasteService>();
            return await copyPasteService.CopyToClipboardAsync(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy operation failed [CorrelationId: {CorrelationId}]", command.CorrelationId);
            throw;
        }
    }

    /// <summary>
    /// Pastes data from clipboard
    /// </summary>
    public async Task<CopyPasteResult> PasteAsync(PasteDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.CopyPaste, nameof(PasteAsync));

        _logger.LogInformation("Starting paste operation [CorrelationId: {CorrelationId}]", command.CorrelationId);

        try
        {
            var copyPasteService = _serviceProvider.GetRequiredService<ICopyPasteService>();
            return await copyPasteService.PasteFromClipboardAsync(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paste operation failed [CorrelationId: {CorrelationId}]", command.CorrelationId);
            throw;
        }
    }

    #endregion
}


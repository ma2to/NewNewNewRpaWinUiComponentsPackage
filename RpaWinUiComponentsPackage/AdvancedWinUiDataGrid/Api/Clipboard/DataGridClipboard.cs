using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Clipboard;

/// <summary>
/// Internal implementation of DataGrid clipboard operations.
/// Delegates to internal clipboard service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridClipboard : IDataGridClipboard
{
    private readonly ILogger<DataGridClipboard>? _logger;
    private readonly ICopyPasteService _copyPasteService;

    public DataGridClipboard(
        ICopyPasteService copyPasteService,
        ILogger<DataGridClipboard>? logger = null)
    {
        _copyPasteService = copyPasteService ?? throw new ArgumentNullException(nameof(copyPasteService));
        _logger = logger;
    }

    public async Task<PublicResult> CopyAsync(bool includeHeaders = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Copying to clipboard via Clipboard module (includeHeaders: {IncludeHeaders})", includeHeaders);

            var internalResult = await _copyPasteService.CopyAsync(includeHeaders, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Copy failed in Clipboard module");
            throw;
        }
    }

    public async Task<PublicResult> CutAsync(bool includeHeaders = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Cutting to clipboard via Clipboard module (includeHeaders: {IncludeHeaders})", includeHeaders);

            var internalResult = await _copyPasteService.CutAsync(includeHeaders, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Cut failed in Clipboard module");
            throw;
        }
    }

    public async Task<PublicResult<int>> PasteAsync(int startRowIndex, string startColumnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Pasting from clipboard at [{RowIndex}, {ColumnName}] via Clipboard module", startRowIndex, startColumnName);

            var internalResult = await _copyPasteService.PasteAsync(startRowIndex, startColumnName, cancellationToken);
            return new PublicResult<int>
            {
                IsSuccess = internalResult.IsSuccess,
                ErrorMessage = internalResult.ErrorMessage,
                Value = internalResult.Value
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Paste failed in Clipboard module");
            throw;
        }
    }

    public bool CanPaste()
    {
        try
        {
            return _copyPasteService.CanPaste();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CanPaste check failed in Clipboard module");
            throw;
        }
    }

    public async Task<string> GetClipboardTextAsync()
    {
        try
        {
            return await _copyPasteService.GetClipboardTextAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetClipboardText failed in Clipboard module");
            throw;
        }
    }

    public async Task<PublicResult> SetClipboardTextAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Setting clipboard text via Clipboard module");

            var internalResult = await _copyPasteService.SetClipboardTextAsync(text, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetClipboardText failed in Clipboard module");
            throw;
        }
    }
}

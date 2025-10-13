using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;
using System.Data;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.IO;

/// <summary>
/// Internal implementation of DataGrid Import/Export operations.
/// Delegates to internal Import/Export services.
/// </summary>
internal sealed class DataGridIO : IDataGridIO
{
    private readonly ILogger<DataGridIO>? _logger;
    private readonly IImportService _importService;
    private readonly IExportService _exportService;
    private readonly IRowStore _rowStore;
    private readonly UiNotificationService? _uiNotificationService;
    private readonly AdvancedDataGridOptions _options;

    public DataGridIO(
        IImportService importService,
        IExportService exportService,
        IRowStore rowStore,
        AdvancedDataGridOptions options,
        UiNotificationService? uiNotificationService = null,
        ILogger<DataGridIO>? logger = null)
    {
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _uiNotificationService = uiNotificationService;
        _logger = logger;
    }

    public async Task<ImportResult> ImportAsync(ImportDataCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Importing data via IO module");
            var internalResult = await _importService.ImportAsync(command.ToInternal(), cancellationToken);
            var result = internalResult.ToPublic();

            // Trigger automatic UI refresh in Interactive mode
            if (result.IsSuccess)
            {
                await TriggerUIRefreshIfNeededAsync("Import", result.ImportedRows);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Import failed in IO module");
            throw;
        }
    }

    public async Task<ExportResult> ExportAsync(ExportDataCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Exporting data via IO module");
            var internalResult = await _exportService.ExportAsync(command.ToInternal(), cancellationToken);
            return internalResult.ToPublic(internalResult.ExportedData);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Export failed in IO module");
            throw;
        }
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetCurrentData()
    {
        try
        {
            _logger?.LogInformation("Getting current data via IO module");
            return _rowStore.GetAllRows();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetCurrentData failed in IO module");
            throw;
        }
    }

    public async Task<DataTable> GetCurrentDataAsDataTableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Getting current data as DataTable via IO module");
            var exportCommand = new ExportDataCommand { IncludeHeaders = true };
            var internalResult = await _exportService.ExportAsync(exportCommand.ToInternal(), cancellationToken);
            return (internalResult.ExportedData as DataTable) ?? new DataTable();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetCurrentDataAsDataTable failed in IO module");
            throw;
        }
    }

    /// <summary>
    /// Triggers automatic UI refresh ONLY in Interactive mode
    /// </summary>
    private async Task TriggerUIRefreshIfNeededAsync(string operationType, int affectedRows)
    {
        // Automatický refresh LEN v Interactive mode
        if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
        {
            await _uiNotificationService.NotifyDataRefreshAsync(affectedRows, operationType);
        }
        // V Readonly/Headless mode → skip (automatický refresh je zakázaný)
    }
}

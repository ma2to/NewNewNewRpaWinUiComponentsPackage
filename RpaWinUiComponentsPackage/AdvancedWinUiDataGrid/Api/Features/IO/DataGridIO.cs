using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
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

    public DataGridIO(
        IImportService importService,
        IExportService exportService,
        IRowStore rowStore,
        ILogger<DataGridIO>? logger = null)
    {
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _logger = logger;
    }

    public async Task<ImportResult> ImportAsync(ImportDataCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Importing data via IO module");
            var internalResult = await _importService.ImportAsync(command.ToInternal(), cancellationToken);
            return internalResult.ToPublic();
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
}

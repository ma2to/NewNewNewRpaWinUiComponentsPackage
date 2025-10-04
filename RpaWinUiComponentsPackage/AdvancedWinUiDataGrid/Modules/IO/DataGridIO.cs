using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Internal;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using System.Data;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.IO;

/// <summary>
/// Implementation of DataGrid Import/Export operations
/// </summary>
internal sealed class DataGridIO : IDataGridIO
{
    private readonly ILogger<DataGridIO> _logger;
    private readonly IImportService _importService;
    private readonly IExportService _exportService;

    public DataGridIO(
        ILogger<DataGridIO> logger,
        IImportService importService,
        IExportService exportService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
    }

    public async Task<ImportResult> ImportAsync(ImportDataCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Importing data via DataGridIO module");

            // Convert public command to internal command
            var internalCommand = command.ToInternal();
            var internalResult = await _importService.ImportAsync(internalCommand, cancellationToken);

            // Convert internal result to public result
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed in DataGridIO module");
            throw;
        }
    }

    public async Task<ExportResult> ExportAsync(ExportDataCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting data via DataGridIO module");

            // Convert public command to internal command
            var internalCommand = command.ToInternal();
            var internalResult = await _exportService.ExportAsync(internalCommand, cancellationToken);

            // Convert internal result to public result (pass ExportedData as second parameter)
            return internalResult.ToPublic(internalResult.ExportedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed in DataGridIO module");
            throw;
        }
    }

    public async Task<DataTable> GetCurrentDataAsDataTableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting current data as DataTable via DataGridIO module");

            // Create an export command to get all data
            var exportCommand = new ExportDataCommand
            {
                IncludeHeaders = true,
                CorrelationId = Guid.NewGuid().ToString()
            };

            var internalCommand = exportCommand.ToInternal();
            var result = await _exportService.ExportAsync(internalCommand, CancellationToken.None);

            return (result.ExportedData as DataTable) ?? new DataTable();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentDataAsDataTable failed in DataGridIO module");
            throw;
        }
    }
}

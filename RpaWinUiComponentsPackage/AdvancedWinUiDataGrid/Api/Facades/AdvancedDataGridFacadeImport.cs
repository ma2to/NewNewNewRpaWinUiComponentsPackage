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
/// Partial class containing Import/Export Operations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Import/Export Operations

    /// <summary>
    /// Imports data using command pattern with LINQ optimization and validation pipeline
    /// </summary>
    public async Task<ImportResult> ImportAsync(ImportDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Import, nameof(ImportAsync));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Starting import operation - create operation scope for automatic tracking
        using var logScope = _operationLogger.LogOperationStart("ImportAsync", new
        {
            OperationId = operationId,
            CorrelationId = command.CorrelationId
        });

        _logger.LogInformation("Starting import operation {OperationId} [CorrelationId: {CorrelationId}]",
            operationId, command.CorrelationId);

        try
        {
            // Create operation scope for scoped services
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

            // Map public command to internal command
            var internalCommand = command.ToInternal();

            // Execute internal import
            var internalResult = await importService.ImportAsync(internalCommand, cancellationToken);

            // Map internal result to public PublicResult
            var result = internalResult.ToPublic();

            // Automatický UI refresh v Interactive mode
            await TriggerUIRefreshIfNeededAsync("Import", result.ImportedRows);

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Import operation {OperationId} completed in {Duration}ms [CorrelationId: {CorrelationId}]",
                operationId, stopwatch.ElapsedMilliseconds, command.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import operation {OperationId} failed [CorrelationId: {CorrelationId}]",
                operationId, command.CorrelationId);
            logScope.MarkFailure(ex);
            throw;
        }
    }

    /// <summary>
    /// Exports data using command pattern with complex filtering
    /// </summary>
    public async Task<ExportResult> ExportAsync(ExportDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Export, nameof(ExportAsync));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Starting export operation - create operation scope for automatic tracking
        using var logScope = _operationLogger.LogOperationStart("ExportAsync", new
        {
            OperationId = operationId,
            CorrelationId = command.CorrelationId
        });

        _logger.LogInformation("Starting export operation {OperationId} [CorrelationId: {CorrelationId}]",
            operationId, command.CorrelationId);

        try
        {
            // Create operation scope for scoped services
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();

            // Map public command to internal command
            var internalCommand = command.ToInternal();

            // Execute internal export
            var internalResult = await exportService.ExportAsync(internalCommand, cancellationToken);

            // Map internal result to public PublicResult (with exported data)
            var result = internalResult.ToPublic(internalResult.ExportedData);

            // Automatický UI refresh v Interactive mode
            await TriggerUIRefreshIfNeededAsync("Export", result.ExportedRows);

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Export operation {OperationId} completed in {Duration}ms [CorrelationId: {CorrelationId}]",
                operationId, stopwatch.ElapsedMilliseconds, command.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export operation {OperationId} failed [CorrelationId: {CorrelationId}]",
                operationId, command.CorrelationId);
            logScope.MarkFailure(ex);
            throw;
        }
    }

    #endregion
}

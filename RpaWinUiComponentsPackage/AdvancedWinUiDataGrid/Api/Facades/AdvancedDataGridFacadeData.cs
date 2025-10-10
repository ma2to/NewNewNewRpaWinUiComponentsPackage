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
/// Partial class containing Data Access APIs
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Data Access APIs

    /// <summary>
    /// Gets current grid data as read-only dictionary collection
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetCurrentData()
    {
        ThrowIfDisposed();

        try
        {
            var rowStore = _serviceProvider.GetRequiredService<IRowStore>();
            var data = rowStore.GetAllRowsAsync().GetAwaiter().GetResult();
            return data.ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current data");
            return new List<IReadOnlyDictionary<string, object?>>().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets current grid data as DataTable
    /// </summary>
    public async Task<DataTable> GetCurrentDataAsDataTableAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();
            var rowStore = scope.ServiceProvider.GetRequiredService<IRowStore>();

            var currentData = await rowStore.GetAllRowsAsync(cancellationToken);
            var exportCommand = ExportDataCommand.ToDataTable(correlationId: Guid.NewGuid().ToString());
            var internalCommand = exportCommand.ToInternal();

            return await exportService.ExportToDataTableAsync(currentData, internalCommand, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current data as DataTable");
            return new DataTable();
        }
    }

    #endregion

    #region Row Management APIs

    /// <summary>
    /// Adds a single row to the grid
    /// </summary>
    public async Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.RowColumnOperations, nameof(AddRowAsync));

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var rowsService = scope.ServiceProvider.GetRequiredService<Rows.IDataGridRows>();

            var result = await rowsService.AddRowAsync(rowData, CancellationToken.None);

            if (result.IsSuccess)
            {
                // Trigger UI refresh in Interactive mode
                await TriggerUIRefreshIfNeededAsync("AddRow", 1);

                _logger.LogDebug("Row added successfully at index {RowIndex}", result.Data);
                return result.Data;
            }
            else
            {
                _logger.LogWarning("AddRowAsync failed: {Message}", result.Message);
                return -1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddRowAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Adds multiple rows to the grid in a single batch operation (high performance)
    /// </summary>
    public async Task<int> AddRowsBatchAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.RowColumnOperations, nameof(AddRowsBatchAsync));

        var rowsList = rows.ToList();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Starting batch add of {RowCount} rows", rowsList.Count);

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var rowsService = scope.ServiceProvider.GetRequiredService<Rows.IDataGridRows>();

            var result = await rowsService.AddRowsAsync(rowsList, cancellationToken);

            if (result.IsSuccess)
            {
                // Single UI refresh for entire batch in Interactive mode
                await TriggerUIRefreshIfNeededAsync("AddRowsBatch", result.Data);

                _logger.LogInformation("Batch add completed: {RowCount} rows in {Duration}ms",
                    result.Data, sw.ElapsedMilliseconds);

                return result.Data;
            }
            else
            {
                _logger.LogWarning("AddRowsBatchAsync failed: {Message}", result.Message);
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddRowsBatchAsync failed for {RowCount} rows", rowsList.Count);
            throw;
        }
    }

    /// <summary>
    /// Removes a row at the specified index
    /// </summary>
    public async Task<bool> RemoveRowAsync(int rowIndex)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.RowColumnOperations, nameof(RemoveRowAsync));

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var rowsService = scope.ServiceProvider.GetRequiredService<Rows.IDataGridRows>();

            var result = await rowsService.RemoveRowAsync(rowIndex, CancellationToken.None);

            if (result.IsSuccess)
            {
                await TriggerUIRefreshIfNeededAsync("RemoveRow", 1);
                _logger.LogDebug("Row {RowIndex} removed successfully", rowIndex);
                return true;
            }
            else
            {
                _logger.LogWarning("RemoveRowAsync failed for row {RowIndex}: {Message}", rowIndex, result.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveRowAsync failed for row {RowIndex}", rowIndex);
            throw;
        }
    }

    /// <summary>
    /// Updates a row at the specified index
    /// </summary>
    public async Task<bool> UpdateRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.RowColumnOperations, nameof(UpdateRowAsync));

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var rowsService = scope.ServiceProvider.GetRequiredService<Rows.IDataGridRows>();

            var result = await rowsService.UpdateRowAsync(rowIndex, rowData, CancellationToken.None);

            if (result.IsSuccess)
            {
                await TriggerUIRefreshIfNeededAsync("UpdateRow", 1);
                _logger.LogDebug("Row {RowIndex} updated successfully", rowIndex);
                return true;
            }
            else
            {
                _logger.LogWarning("UpdateRowAsync failed for row {RowIndex}: {Message}", rowIndex, result.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateRowAsync failed for row {RowIndex}", rowIndex);
            throw;
        }
    }

    /// <summary>
    /// Gets row data by index
    /// </summary>
    public IReadOnlyDictionary<string, object?>? GetRow(int rowIndex)
    {
        ThrowIfDisposed();

        try
        {
            var rowStore = _serviceProvider.GetRequiredService<IRowStore>();
            var allRows = rowStore.GetAllRowsAsync().GetAwaiter().GetResult();
            var rowsList = allRows.ToList();

            if (rowIndex >= 0 && rowIndex < rowsList.Count)
            {
                return rowsList[rowIndex];
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get row {RowIndex}", rowIndex);
            return null;
        }
    }

    /// <summary>
    /// Gets the total number of rows
    /// </summary>
    public int GetRowCount()
    {
        ThrowIfDisposed();

        try
        {
            var rowStore = _serviceProvider.GetRequiredService<IRowStore>();
            return rowStore.GetRowCount();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get row count");
            return 0;
        }
    }

    /// <summary>
    /// Gets the number of visible rows (after filtering)
    /// </summary>
    public int GetVisibleRowCount()
    {
        ThrowIfDisposed();

        try
        {
            var rowStore = _serviceProvider.GetRequiredService<IRowStore>();
            // Use filtered row count for visible rows
            var count = rowStore.GetFilteredRowCountAsync().GetAwaiter().GetResult();
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get visible row count");
            return 0;
        }
    }

    /// <summary>
    /// Clears all rows from the grid
    /// </summary>
    public async Task ClearAllRowsAsync()
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.RowColumnOperations, nameof(ClearAllRowsAsync));

        try
        {
            var rowStore = _serviceProvider.GetRequiredService<IRowStore>();
            await rowStore.ClearAllRowsAsync();
            await TriggerUIRefreshIfNeededAsync("ClearAllRows", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all rows");
            throw;
        }
    }

    #endregion
}


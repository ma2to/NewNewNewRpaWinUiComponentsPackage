using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowColumnCell.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Batch;

/// <summary>
/// Internal implementation of DataGrid batch operations.
/// Delegates to internal RowColumnCell service.
/// </summary>
internal sealed class DataGridBatch : IDataGridBatch
{
    private readonly ILogger<DataGridBatch>? _logger;
    private readonly IRowColumnCellService _rowColumnCellService;

    public DataGridBatch(
        IRowColumnCellService rowColumnCellService,
        ILogger<DataGridBatch>? logger = null)
    {
        _rowColumnCellService = rowColumnCellService ?? throw new ArgumentNullException(nameof(rowColumnCellService));
        _logger = logger;
    }

    public PublicResult BeginBatchUpdate()
    {
        try
        {
            _logger?.LogInformation("Beginning batch update via Batch module");
            _rowColumnCellService.BeginBatchUpdate();
            return new PublicResult { IsSuccess = true, Message = "Batch update started" };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BeginBatchUpdate failed in Batch module");
            throw;
        }
    }

    public PublicResult EndBatchUpdate()
    {
        try
        {
            _logger?.LogInformation("Ending batch update via Batch module");
            _rowColumnCellService.EndBatchUpdate();
            return new PublicResult { IsSuccess = true, Message = "Batch update ended" };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "EndBatchUpdate failed in Batch module");
            throw;
        }
    }

    public async Task<PublicResult<int>> BatchUpdateCellsAsync(IEnumerable<PublicCellUpdate> cellUpdates, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Batch updating cells via Batch module");
            var count = await _rowColumnCellService.BatchUpdateCellsAsync(cellUpdates, cancellationToken);
            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = $"Updated {count} cells",
                Data = count
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BatchUpdateCells failed in Batch module");
            throw;
        }
    }

    public async Task<PublicResult<int>> BatchUpdateColumnAsync(IEnumerable<int> rowIndices, string columnName, object? newValue, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Batch updating column '{ColumnName}' via Batch module", columnName);
            var count = await _rowColumnCellService.BatchUpdateColumnAsync(rowIndices, columnName, newValue, cancellationToken);
            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = $"Updated {count} cells in column '{columnName}'",
                Data = count
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BatchUpdateColumn failed in Batch module");
            throw;
        }
    }

    public async Task<PublicResult<int>> BatchDeleteRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Batch deleting rows via Batch module");
            var count = await _rowColumnCellService.BatchDeleteRowsAsync(rowIndices, cancellationToken);
            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = $"Deleted {count} rows",
                Data = count
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BatchDeleteRows failed in Batch module");
            throw;
        }
    }

    public async Task<PublicResult<int>> BatchTransformAsync(IEnumerable<int> rowIndices, string columnName, Func<object?, object?> transformFunc, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Batch transforming column '{ColumnName}' via Batch module", columnName);
            var count = await _rowColumnCellService.BatchTransformAsync(rowIndices, columnName, transformFunc, cancellationToken);
            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = $"Transformed {count} cells",
                Data = count
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BatchTransform failed in Batch module");
            throw;
        }
    }

    public bool IsInBatchUpdate()
    {
        try
        {
            return _rowColumnCellService.IsInBatchUpdate();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsInBatchUpdate check failed in Batch module");
            throw;
        }
    }
}

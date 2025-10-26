using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowColumnCell.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Batch;

/// <summary>
/// Internal implementation of DataGrid batch operations.
/// Delegates to internal RowColumnCell service.
/// </summary>
internal sealed class DataGridBatch : IDataGridBatch
{
    private readonly ILogger<DataGridBatch>? _logger;
    private readonly IRowColumnCellService _rowColumnCellService;
    private readonly IRowStore _rowStore;

    public DataGridBatch(
        IRowColumnCellService rowColumnCellService,
        IRowStore rowStore,
        ILogger<DataGridBatch>? logger = null)
    {
        _rowColumnCellService = rowColumnCellService ?? throw new ArgumentNullException(nameof(rowColumnCellService));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
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

    /// <summary>
    /// Updates a column value for multiple rows by stable row IDs.
    /// Converts rowIds to current rowIndices and delegates to service.
    /// </summary>
    public async Task<PublicResult<int>> BatchUpdateColumnAsync(IEnumerable<string> rowIds, string columnName, object? newValue, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Batch updating column '{ColumnName}' by rowIds via Batch module", columnName);

            // Convert rowIds to current rowIndices
            var rowIdsList = rowIds.ToList();
            var rowIndices = new List<int>();

            foreach (var rowId in rowIdsList)
            {
                var rowIndex = _rowStore.GetRowIndexById(rowId);
                if (rowIndex.HasValue)
                {
                    rowIndices.Add(rowIndex.Value);
                }
                else
                {
                    _logger?.LogWarning("Row {RowId} not found, skipping update", rowId);
                }
            }

            if (rowIndices.Count == 0)
            {
                return new PublicResult<int>
                {
                    IsSuccess = false,
                    Message = "None of the specified rows were found",
                    Data = 0
                };
            }

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
            _logger?.LogError(ex, "BatchUpdateColumn (by rowIds) failed in Batch module");
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

    /// <summary>
    /// Deletes multiple rows in a single operation by stable row IDs.
    /// Converts rowIds to current rowIndices and delegates to service.
    /// </summary>
    public async Task<PublicResult<int>> BatchDeleteRowsAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Batch deleting rows by rowIds via Batch module");

            // Convert rowIds to current rowIndices
            var rowIdsList = rowIds.ToList();
            var rowIndices = new List<int>();

            foreach (var rowId in rowIdsList)
            {
                var rowIndex = _rowStore.GetRowIndexById(rowId);
                if (rowIndex.HasValue)
                {
                    rowIndices.Add(rowIndex.Value);
                }
                else
                {
                    _logger?.LogWarning("Row {RowId} not found, skipping deletion", rowId);
                }
            }

            if (rowIndices.Count == 0)
            {
                return new PublicResult<int>
                {
                    IsSuccess = false,
                    Message = "None of the specified rows were found",
                    Data = 0
                };
            }

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
            _logger?.LogError(ex, "BatchDeleteRows (by rowIds) failed in Batch module");
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

    /// <summary>
    /// Applies a transformation function to multiple cells by stable row IDs.
    /// Converts rowIds to current rowIndices and delegates to service.
    /// </summary>
    public async Task<PublicResult<int>> BatchTransformAsync(IEnumerable<string> rowIds, string columnName, Func<object?, object?> transformFunc, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Batch transforming column '{ColumnName}' by rowIds via Batch module", columnName);

            // Convert rowIds to current rowIndices
            var rowIdsList = rowIds.ToList();
            var rowIndices = new List<int>();

            foreach (var rowId in rowIdsList)
            {
                var rowIndex = _rowStore.GetRowIndexById(rowId);
                if (rowIndex.HasValue)
                {
                    rowIndices.Add(rowIndex.Value);
                }
                else
                {
                    _logger?.LogWarning("Row {RowId} not found, skipping transformation", rowId);
                }
            }

            if (rowIndices.Count == 0)
            {
                return new PublicResult<int>
                {
                    IsSuccess = false,
                    Message = "None of the specified rows were found",
                    Data = 0
                };
            }

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
            _logger?.LogError(ex, "BatchTransform (by rowIds) failed in Batch module");
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

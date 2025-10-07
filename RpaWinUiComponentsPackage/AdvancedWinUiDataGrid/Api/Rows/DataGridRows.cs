using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Rows;

/// <summary>
/// Internal implementation of DataGrid row operations.
/// Delegates to internal Row Store.
/// </summary>
internal sealed class DataGridRows : IDataGridRows
{
    private readonly ILogger<DataGridRows>? _logger;
    private readonly IRowStore _rowStore;

    public DataGridRows(
        IRowStore rowStore,
        ILogger<DataGridRows>? logger = null)
    {
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _logger = logger;
    }

    public async Task<PublicResult<int>> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Adding row via Rows module");
            var rowIndex = await _rowStore.AddRowAsync(rowData, cancellationToken);
            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = "Row added successfully",
                Data = rowIndex
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AddRow failed in Rows module");
            throw;
        }
    }

    public async Task<PublicResult<int>> AddRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Adding multiple rows via Rows module");
            var count = await _rowStore.AddRowsAsync(rowsData, cancellationToken);
            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = $"Added {count} rows successfully",
                Data = count
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AddRows failed in Rows module");
            throw;
        }
    }

    public async Task<PublicResult> InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Inserting row at index {RowIndex} via Rows module", rowIndex);
            await _rowStore.InsertRowAsync(rowIndex, rowData, cancellationToken);
            return new PublicResult
            {
                IsSuccess = true,
                Message = "Row inserted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "InsertRow failed in Rows module");
            throw;
        }
    }

    public async Task<PublicResult> UpdateRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Updating row {RowIndex} via Rows module", rowIndex);
            await _rowStore.UpdateRowAsync(rowIndex, rowData, cancellationToken);
            return new PublicResult
            {
                IsSuccess = true,
                Message = "Row updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UpdateRow failed in Rows module");
            throw;
        }
    }

    public async Task<PublicResult> RemoveRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Removing row {RowIndex} via Rows module", rowIndex);
            await _rowStore.RemoveRowAsync(rowIndex, cancellationToken);
            return new PublicResult
            {
                IsSuccess = true,
                Message = "Row removed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RemoveRow failed in Rows module");
            throw;
        }
    }

    public async Task<PublicResult<int>> RemoveRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Removing multiple rows via Rows module");
            var count = await _rowStore.RemoveRowsAsync(rowIndices, cancellationToken);
            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = $"Removed {count} rows successfully",
                Data = count
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RemoveRows failed in Rows module");
            throw;
        }
    }

    public async Task<PublicResult> ClearAllRowsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Clearing all rows via Rows module");
            await _rowStore.ClearAllRowsAsync(cancellationToken);
            return new PublicResult
            {
                IsSuccess = true,
                Message = "All rows cleared successfully"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearAllRows failed in Rows module");
            throw;
        }
    }

    public IReadOnlyDictionary<string, object?>? GetRow(int rowIndex)
    {
        try
        {
            return _rowStore.GetRow(rowIndex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetRow failed in Rows module for row {RowIndex}", rowIndex);
            throw;
        }
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetAllRows()
    {
        try
        {
            return _rowStore.GetAllRows();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetAllRows failed in Rows module");
            throw;
        }
    }

    public int GetRowCount()
    {
        try
        {
            return _rowStore.GetRowCount();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetRowCount failed in Rows module");
            throw;
        }
    }

    public bool RowExists(int rowIndex)
    {
        try
        {
            return _rowStore.RowExists(rowIndex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RowExists check failed in Rows module for row {RowIndex}", rowIndex);
            throw;
        }
    }

    public async Task<PublicResult<int>> DuplicateRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Duplicating row {RowIndex} via Rows module", rowIndex);
            var rowData = _rowStore.GetRow(rowIndex);
            if (rowData == null)
            {
                return new PublicResult<int>
                {
                    IsSuccess = false,
                    Message = $"Row {rowIndex} not found",
                    Data = -1
                };
            }

            var newRowIndex = await _rowStore.AddRowAsync(rowData, cancellationToken);
            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = "Row duplicated successfully",
                Data = newRowIndex
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DuplicateRow failed in Rows module");
            throw;
        }
    }
}

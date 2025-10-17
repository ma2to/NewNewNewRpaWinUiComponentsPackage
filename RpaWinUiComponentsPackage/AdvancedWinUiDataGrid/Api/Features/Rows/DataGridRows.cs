using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Rows;

/// <summary>
/// Internal implementation of DataGrid row operations.
/// Delegates to internal Row Store.
/// </summary>
internal sealed class DataGridRows : IDataGridRows
{
    private readonly ILogger<DataGridRows>? _logger;
    private readonly IRowStore _rowStore;
    private readonly UiNotificationService? _uiNotificationService;
    private readonly AdvancedDataGridOptions _options;

    public DataGridRows(
        IRowStore rowStore,
        AdvancedDataGridOptions options,
        UiNotificationService? uiNotificationService = null,
        ILogger<DataGridRows>? logger = null)
    {
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _uiNotificationService = uiNotificationService;
        _logger = logger;
    }

    public async Task<PublicResult<int>> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Adding row via Rows module");
            var rowIndex = await _rowStore.AddRowAsync(rowData, cancellationToken);

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("AddRow", 1);

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

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("AddRows", count);

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

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("InsertRow", 1);

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

    public async Task<PublicResult> UpdateRowAsync(string rowId, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Updating row {RowId} via Rows module", rowId);

            var success = await _rowStore.UpdateRowByIdAsync(rowId, rowData, cancellationToken);

            if (!success)
            {
                return new PublicResult
                {
                    IsSuccess = false,
                    Message = $"Row {rowId} not found or update failed"
                };
            }

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("UpdateRow", 1);

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

    public async Task<PublicResult> RemoveRowAsync(string rowId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Removing row {RowId} via Rows module", rowId);

            var success = await _rowStore.RemoveRowByIdAsync(rowId, cancellationToken);

            if (!success)
            {
                return new PublicResult
                {
                    IsSuccess = false,
                    Message = $"Row {rowId} not found or removal failed"
                };
            }

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("RemoveRow", 1);

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

    public async Task<PublicResult<int>> RemoveRowsAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowIdsList = rowIds.ToList();
            _logger?.LogInformation("Removing {Count} rows via Rows module", rowIdsList.Count);

            await _rowStore.RemoveRowsAsync(rowIdsList, cancellationToken);

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("RemoveRows", rowIdsList.Count);

            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = $"Removed {rowIdsList.Count} rows successfully",
                Data = rowIdsList.Count
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
            var currentCount = _rowStore.GetRowCount();
            await _rowStore.ClearAllRowsAsync(cancellationToken);

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("ClearAllRows", currentCount);

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

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("DuplicateRow", 1);

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

    /// <summary>
    /// Gets the unique row ID for a row at the specified index.
    /// USE CASE: User clicks on row in UI, UI event provides RowIndex, need to convert to stable RowID.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index</param>
    /// <returns>The unique row ID (from __rowId field) or null if not found</returns>
    public string? GetRowIdByIndex(int rowIndex)
    {
        try
        {
            var rowData = _rowStore.GetRow(rowIndex);
            if (rowData != null && rowData.TryGetValue("__rowId", out var rowIdValue))
            {
                return rowIdValue?.ToString();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetRowIdByIndex failed for row {RowIndex}", rowIndex);
            return null;
        }
    }

    /// <summary>
    /// Gets the current row index for a row with the specified ID.
    /// USE CASE: Have RowID from database, want to scroll/highlight row in UI.
    /// </summary>
    /// <param name="rowId">Unique row identifier</param>
    /// <returns>Current zero-based row index or null if not found</returns>
    public int? GetRowIndexById(string rowId)
    {
        try
        {
            // TODO: Implement efficient lookup when IRowStore supports RowID operations
            // For now, linear search through all rows
            var allRows = _rowStore.GetAllRows();
            for (int i = 0; i < allRows.Count; i++)
            {
                if (allRows[i].TryGetValue("__rowId", out var rowIdValue))
                {
                    if (rowIdValue?.ToString() == rowId)
                    {
                        return i;
                    }
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetRowIndexById failed for RowID {RowId}", rowId);
            return null;
        }
    }

    /// <summary>
    /// Gets the row ID of the currently selected row (if single selection).
    /// USE CASE: Shortcut to avoid manual conversion in single-select scenarios.
    /// </summary>
    /// <returns>Row ID or null if no row selected</returns>
    public string? GetSelectedRowId()
    {
        try
        {
            // TODO: Implement when selection tracking is available
            _logger?.LogWarning("GetSelectedRowId() not yet implemented - requires selection tracking");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetSelectedRowId failed");
            return null;
        }
    }

    /// <summary>
    /// Gets the row IDs of all currently selected rows.
    /// USE CASE: Shortcut for multi-select delete/update operations.
    /// </summary>
    /// <returns>Array of row IDs (empty array if no selection)</returns>
    public string[] GetSelectedRowIds()
    {
        try
        {
            // TODO: Implement when selection tracking is available
            _logger?.LogWarning("GetSelectedRowIds() not yet implemented - requires selection tracking");
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetSelectedRowIds failed");
            return Array.Empty<string>();
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

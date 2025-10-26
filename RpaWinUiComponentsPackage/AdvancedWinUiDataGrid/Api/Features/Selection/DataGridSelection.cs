using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Selection;

/// <summary>
/// Internal implementation of DataGrid selection operations.
/// Delegates to internal selection service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridSelection : IDataGridSelection
{
    private readonly ILogger<DataGridSelection>? _logger;
    private readonly ISelectionService _selectionService;
    private readonly IRowStore _rowStore;

    public DataGridSelection(
        ISelectionService selectionService,
        IRowStore rowStore,
        ILogger<DataGridSelection>? logger = null)
    {
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _logger = logger;
    }

    public async Task<PublicResult> SelectRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Selecting row {RowIndex} via Selection module", rowIndex);

            var internalResult = await _selectionService.SelectRowAsync(rowIndex, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SelectRow failed in Selection module");
            throw;
        }
    }

    /// <summary>
    /// Selects a specific row by stable row ID.
    /// Converts rowId to current rowIndex and delegates to selection service.
    /// </summary>
    public async Task<PublicResult> SelectRowAsync(string rowId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Selecting row {RowId} via Selection module", rowId);

            // Convert rowId to current rowIndex
            var rowIndex = _rowStore.GetRowIndexById(rowId);
            if (rowIndex == null)
            {
                return new PublicResult
                {
                    IsSuccess = false,
                    Message = $"Row {rowId} not found"
                };
            }

            var internalResult = await _selectionService.SelectRowAsync(rowIndex.Value, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SelectRow failed in Selection module for rowId {RowId}", rowId);
            throw;
        }
    }

    public async Task<PublicResult> SelectRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Selecting multiple rows via Selection module");

            var internalResult = await _selectionService.SelectRowsAsync(rowIndices, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SelectRows failed in Selection module");
            throw;
        }
    }

    /// <summary>
    /// Selects multiple rows by stable row IDs.
    /// Converts rowIds to current rowIndices and delegates to selection service.
    /// </summary>
    public async Task<PublicResult> SelectRowsAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Selecting multiple rows by rowIds via Selection module");

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
                    _logger?.LogWarning("Row {RowId} not found, skipping selection", rowId);
                }
            }

            if (rowIndices.Count == 0)
            {
                return new PublicResult
                {
                    IsSuccess = false,
                    Message = "None of the specified rows were found"
                };
            }

            var internalResult = await _selectionService.SelectRowsAsync(rowIndices, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SelectRows failed in Selection module for rowIds");
            throw;
        }
    }

    public async Task<PublicResult> SelectRowRangeAsync(int startRowIndex, int endRowIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Selecting row range [{Start}-{End}] via Selection module", startRowIndex, endRowIndex);

            var internalResult = await _selectionService.SelectRowRangeAsync(startRowIndex, endRowIndex, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SelectRowRange failed in Selection module");
            throw;
        }
    }

    public async Task<PublicResult> SelectAllRowsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Selecting all rows via Selection module");

            var internalResult = await _selectionService.SelectAllRowsAsync(cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SelectAllRows failed in Selection module");
            throw;
        }
    }

    public async Task<PublicResult> ClearSelectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Clearing selection via Selection module");

            var internalResult = await _selectionService.ClearSelectionPublicAsync(cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearSelection failed in Selection module");
            throw;
        }
    }

    public IReadOnlyList<int> GetSelectedRowIndices()
    {
        try
        {
            return _selectionService.GetSelectedRowIndices();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetSelectedRowIndices failed in Selection module");
            throw;
        }
    }

    public int GetSelectedRowCount()
    {
        try
        {
            return _selectionService.GetSelectedRowCount();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetSelectedRowCount failed in Selection module");
            throw;
        }
    }

    public bool IsRowSelected(int rowIndex)
    {
        try
        {
            return _selectionService.IsRowSelected(rowIndex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsRowSelected check failed in Selection module for row {RowIndex}", rowIndex);
            throw;
        }
    }

    /// <summary>
    /// Checks if a row is selected by stable row ID.
    /// Converts rowId to current rowIndex and delegates to selection service.
    /// </summary>
    public bool IsRowSelected(string rowId)
    {
        try
        {
            // Convert rowId to current rowIndex
            var rowIndex = _rowStore.GetRowIndexById(rowId);
            if (rowIndex == null)
            {
                _logger?.LogWarning("Row {RowId} not found when checking selection", rowId);
                return false;
            }

            return _selectionService.IsRowSelected(rowIndex.Value);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsRowSelected check failed in Selection module for rowId {RowId}", rowId);
            throw;
        }
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetSelectedRowsData()
    {
        try
        {
            return _selectionService.GetSelectedRowsData();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetSelectedRowsData failed in Selection module");
            throw;
        }
    }
}

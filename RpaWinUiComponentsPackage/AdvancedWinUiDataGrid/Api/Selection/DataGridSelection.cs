using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Selection;

/// <summary>
/// Internal implementation of DataGrid selection operations.
/// Delegates to internal selection service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridSelection : IDataGridSelection
{
    private readonly ILogger<DataGridSelection>? _logger;
    private readonly ISelectionService _selectionService;

    public DataGridSelection(
        ISelectionService selectionService,
        ILogger<DataGridSelection>? logger = null)
    {
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
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

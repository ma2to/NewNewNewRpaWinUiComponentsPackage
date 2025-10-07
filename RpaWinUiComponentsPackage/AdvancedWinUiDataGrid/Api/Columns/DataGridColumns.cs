using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Columns;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.ColumnResize.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Columns;

/// <summary>
/// Internal implementation of DataGrid column operations.
/// Delegates to internal column service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridColumns : IDataGridColumns
{
    private readonly ILogger<DataGridColumns>? _logger;
    private readonly IColumnService _columnService;
    private readonly IColumnResizeService _columnResizeService;

    public DataGridColumns(
        IColumnService columnService,
        IColumnResizeService columnResizeService,
        ILogger<DataGridColumns>? logger = null)
    {
        _columnService = columnService ?? throw new ArgumentNullException(nameof(columnService));
        _columnResizeService = columnResizeService ?? throw new ArgumentNullException(nameof(columnResizeService));
        _logger = logger;
    }

    public async Task<PublicResult> AddColumnAsync(PublicColumnDefinition columnDefinition, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Adding column '{ColumnName}' via Columns module", columnDefinition?.Name);

            var internalDefinition = columnDefinition.ToInternal();
            var internalResult = await _columnService.AddColumnAsync(internalDefinition, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AddColumn failed in Columns module");
            throw;
        }
    }

    public async Task<PublicResult> RemoveColumnAsync(string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Removing column '{ColumnName}' via Columns module", columnName);

            var internalResult = await _columnService.RemoveColumnAsync(columnName, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RemoveColumn failed in Columns module");
            throw;
        }
    }

    public async Task<PublicResult> ShowColumnAsync(string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Showing column '{ColumnName}' via Columns module", columnName);

            var internalResult = await _columnService.ShowColumnAsync(columnName, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ShowColumn failed in Columns module");
            throw;
        }
    }

    public async Task<PublicResult> HideColumnAsync(string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Hiding column '{ColumnName}' via Columns module", columnName);

            var internalResult = await _columnService.HideColumnAsync(columnName, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "HideColumn failed in Columns module");
            throw;
        }
    }

    public async Task<PublicResult> ReorderColumnAsync(string columnName, int newIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Reordering column '{ColumnName}' to index {NewIndex} via Columns module", columnName, newIndex);

            // Find the current index of the column
            var column = _columnService.GetColumn(columnName);
            if (column == null)
            {
                return PublicResult.Failure($"Column '{columnName}' not found");
            }

            var allColumns = _columnService.GetAllColumns();
            var fromIndex = allColumns.ToList().FindIndex(c => c.Name == columnName);

            if (fromIndex < 0)
            {
                return PublicResult.Failure($"Column '{columnName}' not found in column list");
            }

            var internalResult = await _columnService.ReorderColumnAsync(fromIndex, newIndex, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ReorderColumn failed in Columns module");
            throw;
        }
    }

    public async Task<PublicResult> ResizeColumnAsync(string columnName, double newWidth, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Resizing column '{ColumnName}' to width {NewWidth} via Columns module", columnName, newWidth);

            await _columnResizeService.ResizeColumnAsync(columnName, newWidth, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ResizeColumn failed in Columns module");
            throw;
        }
    }

    public async Task<PublicResult> AutoFitColumnAsync(string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Auto-fitting column '{ColumnName}' via Columns module", columnName);

            await _columnResizeService.AutoFitColumnAsync(columnName, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AutoFitColumn failed in Columns module");
            throw;
        }
    }

    public async Task<PublicResult> AutoFitAllColumnsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Auto-fitting all columns via Columns module");

            await _columnResizeService.AutoFitAllColumnsAsync(cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AutoFitAllColumns failed in Columns module");
            throw;
        }
    }

    public IReadOnlyList<PublicColumnDefinition> GetAllColumns()
    {
        try
        {
            var internalColumns = _columnService.GetAllColumns();
            return internalColumns.Select(c => c.ToPublic()).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetAllColumns failed in Columns module");
            throw;
        }
    }

    public IReadOnlyList<PublicColumnDefinition> GetVisibleColumns()
    {
        try
        {
            var internalColumns = _columnService.GetVisibleColumns();
            return internalColumns.Select(c => c.ToPublic()).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetVisibleColumns failed in Columns module");
            throw;
        }
    }

    public PublicColumnDefinition? GetColumn(string columnName)
    {
        try
        {
            var internalColumn = _columnService.GetColumn(columnName);
            return internalColumn?.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetColumn failed in Columns module for column '{ColumnName}'", columnName);
            throw;
        }
    }

    public bool ColumnExists(string columnName)
    {
        try
        {
            return _columnService.ColumnExists(columnName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ColumnExists check failed in Columns module for column '{ColumnName}'", columnName);
            throw;
        }
    }

    public int GetColumnCount()
    {
        try
        {
            return _columnService.GetColumnCount();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetColumnCount failed in Columns module");
            throw;
        }
    }

    public int GetVisibleColumnCount()
    {
        try
        {
            return _columnService.GetVisibleColumnCount();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetVisibleColumnCount failed in Columns module");
            throw;
        }
    }
}

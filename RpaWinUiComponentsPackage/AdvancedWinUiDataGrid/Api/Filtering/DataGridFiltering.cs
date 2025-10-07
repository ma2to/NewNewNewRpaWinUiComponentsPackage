using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Filtering;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Filtering;

/// <summary>
/// Internal implementation of DataGrid filtering operations.
/// Delegates to internal filtering service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridFiltering : IDataGridFiltering
{
    private readonly ILogger<DataGridFiltering>? _logger;
    private readonly IFilterService _filterService;

    public DataGridFiltering(
        IFilterService filterService,
        ILogger<DataGridFiltering>? logger = null)
    {
        _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
        _logger = logger;
    }

    public async Task<PublicResult> ApplyColumnFilterAsync(string columnName, PublicFilterOperator filterOperator, object? filterValue, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Applying filter to column '{ColumnName}' with operator {Operator} via Filtering module", columnName, filterOperator);

            var internalOperator = filterOperator.ToInternal();
            var internalResult = await _filterService.ApplyColumnFilterAsync(columnName, internalOperator, filterValue, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ApplyColumnFilter failed in Filtering module");
            throw;
        }
    }

    public async Task<PublicResult> ApplyMultipleFiltersAsync(IEnumerable<PublicFilterDescriptor> filters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Applying multiple filters via Filtering module");

            var internalFilters = filters.Select(f => f.ToInternal()).ToList();
            var internalResult = await _filterService.ApplyMultipleFiltersAsync(internalFilters, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ApplyMultipleFilters failed in Filtering module");
            throw;
        }
    }

    public async Task<PublicResult> RemoveColumnFilterAsync(string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Removing filter from column '{ColumnName}' via Filtering module", columnName);

            var internalResult = await _filterService.RemoveColumnFilterAsync(columnName, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RemoveColumnFilter failed in Filtering module");
            throw;
        }
    }

    public async Task<PublicResult> ClearAllFiltersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Clearing all filters via Filtering module");

            var internalResult = await _filterService.ClearAllFiltersAsync(cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearAllFilters failed in Filtering module");
            throw;
        }
    }

    public IReadOnlyList<PublicFilterDescriptor> GetCurrentFilters()
    {
        try
        {
            var internalFilters = _filterService.GetCurrentFilters();
            return internalFilters.Select(f => f.ToPublic()).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetCurrentFilters failed in Filtering module");
            throw;
        }
    }

    public bool IsColumnFiltered(string columnName)
    {
        try
        {
            return _filterService.IsColumnFiltered(columnName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsColumnFiltered check failed in Filtering module for column '{ColumnName}'", columnName);
            throw;
        }
    }

    public int GetFilterCount()
    {
        try
        {
            return _filterService.GetFilterCount();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetFilterCount failed in Filtering module");
            throw;
        }
    }

    public int GetFilteredRowCount()
    {
        try
        {
            // Synchronous wrapper around async method
            return (int)_filterService.GetFilteredRowCountAsync(default).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetFilteredRowCount failed in Filtering module");
            throw;
        }
    }

    public async Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _filterService.GetFilteredRowCountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetFilteredRowCountAsync failed in Filtering module");
            throw;
        }
    }
}

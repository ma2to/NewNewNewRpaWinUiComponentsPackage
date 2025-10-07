using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Sorting;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Sorting;

/// <summary>
/// Internal implementation of DataGrid sorting operations.
/// Delegates to internal sorting service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridSorting : IDataGridSorting
{
    private readonly ILogger<DataGridSorting>? _logger;
    private readonly ISortService _sortService;

    public DataGridSorting(
        ISortService sortService,
        ILogger<DataGridSorting>? logger = null)
    {
        _sortService = sortService ?? throw new ArgumentNullException(nameof(sortService));
        _logger = logger;
    }

    public async Task<PublicResult> SortByColumnAsync(string columnName, PublicSortDirection direction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Sorting by column '{ColumnName}' with direction {Direction} via Sorting module", columnName, direction);

            var internalDirection = direction.ToInternal();
            var internalResult = await _sortService.SortByColumnAsync(columnName, internalDirection, cancellationToken);
            return internalResult ? PublicResult.Success() : PublicResult.Failure("Sort operation failed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SortByColumn failed in Sorting module");
            throw;
        }
    }

    public async Task<PublicResult> SortByMultipleColumnsAsync(IEnumerable<PublicSortDescriptor> sortDescriptors, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Sorting by multiple columns via Sorting module");

            var internalDescriptors = sortDescriptors.Select(d => d.ToInternal()).ToList();
            var internalResult = await _sortService.SortByMultipleColumnsAsync(internalDescriptors, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SortByMultipleColumns failed in Sorting module");
            throw;
        }
    }

    public async Task<PublicResult> ClearSortingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Clearing all sorting via Sorting module");

            var internalResult = await _sortService.ClearSortingAsync(cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearSorting failed in Sorting module");
            throw;
        }
    }

    public IReadOnlyList<PublicSortDescriptor> GetCurrentSortDescriptors()
    {
        try
        {
            var internalDescriptors = _sortService.GetCurrentSortDescriptors();
            return internalDescriptors.Select(d => d.ToPublic()).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetCurrentSortDescriptors failed in Sorting module");
            throw;
        }
    }

    public async Task<PublicResult<PublicSortDirection>> ToggleSortDirectionAsync(string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Toggling sort direction for column '{ColumnName}' via Sorting module", columnName);

            var internalResult = await _sortService.ToggleSortDirectionAsync(columnName, cancellationToken);

            // Convert Api.Models.PublicSortDirection to PublicSortDirection (different types with same values)
            var apiDirection = internalResult.Value.ToPublic();
            var publicDirection = (PublicSortDirection)(int)apiDirection;

            return new PublicResult<PublicSortDirection>
            {
                IsSuccess = internalResult.IsSuccess,
                ErrorMessage = internalResult.ErrorMessage,
                Value = publicDirection
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ToggleSortDirection failed in Sorting module");
            throw;
        }
    }

    public bool IsColumnSorted(string columnName)
    {
        try
        {
            return _sortService.IsColumnSorted(columnName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsColumnSorted check failed in Sorting module for column '{ColumnName}'", columnName);
            throw;
        }
    }

    public Api.Models.PublicSortDirection GetColumnSortDirection(string columnName)
    {
        try
        {
            var internalDirection = _sortService.GetColumnSortDirection(columnName);
            return internalDirection.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetColumnSortDirection failed in Sorting module for column '{ColumnName}'", columnName);
            throw;
        }
    }
}

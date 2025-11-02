using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
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
    private readonly UIAdapters.WinUI.UiNotificationService? _uiNotificationService;
    private readonly Infrastructure.Persistence.Interfaces.IRowStore? _rowStore;

    public DataGridSorting(
        ISortService sortService,
        ILogger<DataGridSorting>? logger = null,
        UIAdapters.WinUI.UiNotificationService? uiNotificationService = null,
        Infrastructure.Persistence.Interfaces.IRowStore? rowStore = null)
    {
        _sortService = sortService ?? throw new ArgumentNullException(nameof(sortService));
        _logger = logger;
        _uiNotificationService = uiNotificationService;
        _rowStore = rowStore;
    }

    public async Task<PublicResult> SortByColumnAsync(string columnName, PublicSortDirection direction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Sorting by column '{ColumnName}' with direction {Direction} via Sorting module", columnName, direction);

            var internalDirection = direction.ToInternal();
            var internalResult = await _sortService.SortByColumnAsync(columnName, internalDirection, cancellationToken);

            if (!internalResult)
            {
                return PublicResult.Failure("Sort operation failed");
            }

            // ✅ PROFESSIONAL FIX: Trigger UI refresh after successful sort
            // This ensures InternalUIUpdateHandler detects changes and updates DataGridViewModel
            if (_uiNotificationService != null && _rowStore != null)
            {
                // Get current row count to pass to notification
                var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
                var rowCount = allRows.Count();

                _logger?.LogInformation("Triggering UI refresh after sort: {RowCount} rows", rowCount);
                await _uiNotificationService.NotifyDataRefreshAsync(rowCount, "Sort");
            }
            else
            {
                _logger?.LogDebug("UiNotificationService or RowStore not available - skipping UI refresh notification");
            }

            return PublicResult.Success();
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

            // ✅ PROFESSIONAL FIX: Check if sort succeeded before triggering UI refresh
            if (!internalResult.IsSuccess)
            {
                return internalResult.ToPublic();
            }

            // ✅ PROFESSIONAL FIX: Trigger UI refresh after successful multi-column sort
            // This ensures InternalUIUpdateHandler detects changes and updates DataGridViewModel
            if (_uiNotificationService != null && _rowStore != null)
            {
                // Get current row count to pass to notification
                var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
                var rowCount = allRows.Count();

                _logger?.LogInformation("Triggering UI refresh after multi-column sort: {RowCount} rows", rowCount);
                await _uiNotificationService.NotifyDataRefreshAsync(rowCount, "MultiSort");
            }
            else
            {
                _logger?.LogDebug("UiNotificationService or RowStore not available - skipping UI refresh notification");
            }

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

            if (!internalResult.IsSuccess)
            {
                return internalResult.ToPublic();
            }

            // ✅ PROFESSIONAL FIX: Trigger UI refresh after successful clear sort
            // This ensures InternalUIUpdateHandler detects changes and updates DataGridViewModel
            if (_uiNotificationService != null && _rowStore != null)
            {
                // Get current row count to pass to notification
                var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
                var rowCount = allRows.Count();

                _logger?.LogInformation("Triggering UI refresh after clear sort: {RowCount} rows", rowCount);
                await _uiNotificationService.NotifyDataRefreshAsync(rowCount, "ClearSort");
            }
            else
            {
                _logger?.LogDebug("UiNotificationService or RowStore not available - skipping UI refresh notification");
            }

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

            // Convert PublicSortDirection to PublicSortDirection (different types with same values)
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

    public PublicSortDirection GetColumnSortDirection(string columnName)
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

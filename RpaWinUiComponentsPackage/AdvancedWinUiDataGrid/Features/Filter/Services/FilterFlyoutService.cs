using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Services;

/// <summary>
/// Service for managing filter flyout operations
/// Supports two filter modes:
/// 1. Checkbox mode: SQL WHERE IN (value1, value2, ...)
/// 2. Regex mode: SQL WHERE REGEXP 'pattern'
/// </summary>
internal sealed class FilterFlyoutService
{
    private readonly ILogger<FilterFlyoutService>? _logger;
    private readonly IRowStore _rowStore;
    private readonly UIAdapters.WinUI.UiNotificationService? _uiNotificationService;

    public FilterFlyoutService(
        ILogger<FilterFlyoutService>? logger,
        IRowStore rowStore,
        UIAdapters.WinUI.UiNotificationService? uiNotificationService)
    {
        _logger = logger;
        _rowStore = rowStore;
        _uiNotificationService = uiNotificationService;
    }

    /// <summary>
    /// Load unique values for a column (for checkbox filter mode)
    /// SQL: SELECT DISTINCT json_extract(data, '$.columnName') AS value
    ///      FROM grid_rows WHERE __isDeleted = 0
    ///      ORDER BY value
    /// </summary>
    /// <param name="columnName">Column to get unique values from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unique values (distinct, sorted)</returns>
    public async Task<List<string>> LoadUniqueValuesAsync(
        string columnName,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Loading unique values for column: {ColumnName}", columnName);

        try
        {
            // Get all rows (active store will optimize this - either InMemory or SQLite)
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);

            // Extract unique values from the specified column
            var uniqueValues = allRows
                .Select(row => row.ContainsKey(columnName) ? row[columnName]?.ToString() ?? string.Empty : string.Empty)
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct()
                .OrderBy(value => value)
                .ToList();

            _logger?.LogInformation(
                "Loaded {Count} unique values for column {ColumnName}",
                uniqueValues.Count,
                columnName);

            return uniqueValues;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load unique values for column {ColumnName}", columnName);
            return new List<string>();
        }
    }

    /// <summary>
    /// Apply checkbox filter (SQL WHERE IN)
    /// Filters rows to only show those with selected values
    /// </summary>
    /// <param name="columnName">Column to filter</param>
    /// <param name="selectedValues">Values to include (checked items)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ApplyCheckboxFilterAsync(
        string columnName,
        List<string> selectedValues,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation(
            "Applying checkbox filter: Column={ColumnName}, Values={Values}",
            columnName,
            string.Join(", ", selectedValues.Take(5)) + (selectedValues.Count > 5 ? "..." : ""));

        try
        {
            // Create filter criteria for checkbox mode (WHERE IN)
            // NOTE: For FilterOperator.In, we pass the list as the Value property
            // InMemoryRowStore.ValueInList() handles List<string> as the filterValue
            var filterCriteria = new FilterCriteria
            {
                ColumnName = columnName,
                Operator = FilterOperator.In,
                Value = selectedValues  // Pass List<string> as object
            };

            // Apply filter via row store
            _rowStore.SetFilterCriteria(new List<object> { filterCriteria });

            _logger?.LogInformation(
                "Checkbox filter applied successfully: {Count} values selected",
                selectedValues.Count);

            // ✅ PROFESSIONAL FIX: Trigger UI refresh to update DataGridViewModel
            if (_uiNotificationService != null)
            {
                var filteredCount = (int)await _rowStore.GetRowCountAsync(onlyFiltered: true, cancellationToken);
                _logger?.LogInformation("Triggering UI refresh after checkbox filter: {FilteredCount} rows", filteredCount);
                await _uiNotificationService.NotifyDataRefreshAsync(filteredCount, "CheckboxFilter");
            }
            else
            {
                _logger?.LogWarning("UiNotificationService not available - UI will not auto-refresh after filter");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to apply checkbox filter for column {ColumnName}", columnName);
            throw;
        }
    }

    /// <summary>
    /// Apply regex filter (SQL WHERE REGEXP)
    /// Filters rows using regex pattern matching
    /// </summary>
    /// <param name="columnName">Column to filter</param>
    /// <param name="regexPattern">Regex pattern (e.g., "^test.*|.*data$")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ApplyRegexFilterAsync(
        string columnName,
        string regexPattern,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation(
            "Applying regex filter: Column={ColumnName}, Pattern={Pattern}",
            columnName,
            regexPattern);

        try
        {
            // Create filter criteria for regex mode (WHERE REGEXP)
            var filterCriteria = new FilterCriteria
            {
                ColumnName = columnName,
                Operator = FilterOperator.Regex,  // NEW OPERATOR
                Value = regexPattern
            };

            // Apply filter via row store
            _rowStore.SetFilterCriteria(new List<object> { filterCriteria });

            _logger?.LogInformation("Regex filter applied successfully: Pattern={Pattern}", regexPattern);

            // ✅ PROFESSIONAL FIX: Trigger UI refresh to update DataGridViewModel
            if (_uiNotificationService != null)
            {
                var filteredCount = (int)await _rowStore.GetRowCountAsync(onlyFiltered: true, cancellationToken);
                _logger?.LogInformation("Triggering UI refresh after regex filter: {FilteredCount} rows", filteredCount);
                await _uiNotificationService.NotifyDataRefreshAsync(filteredCount, "RegexFilter");
            }
            else
            {
                _logger?.LogWarning("UiNotificationService not available - UI will not auto-refresh after filter");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to apply regex filter for column {ColumnName}", columnName);
            throw;
        }
    }

    /// <summary>
    /// Clear filter for a specific column
    /// Removes all filtering, shows all rows
    /// </summary>
    /// <param name="columnName">Column to clear filter from (currently clears ALL filters)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ClearFilterAsync(
        string columnName,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Clearing filter for column: {ColumnName}", columnName);

        try
        {
            // Clear all filter criteria
            _rowStore.ClearFilterCriteria();

            _logger?.LogInformation("Filter cleared successfully for column {ColumnName}", columnName);

            // ✅ PROFESSIONAL FIX: Trigger UI refresh to show all rows
            if (_uiNotificationService != null)
            {
                var totalCount = (int)await _rowStore.GetRowCountAsync(onlyFiltered: false, cancellationToken);
                _logger?.LogInformation("Triggering UI refresh after clear filter: {TotalCount} rows", totalCount);
                await _uiNotificationService.NotifyDataRefreshAsync(totalCount, "ClearFilter");
            }
            else
            {
                _logger?.LogWarning("UiNotificationService not available - UI will not auto-refresh after clear");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear filter for column {ColumnName}", columnName);
            throw;
        }
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Clear all filters (for all columns)
    /// Removes all filtering, shows all rows
    /// This is a wrapper around ClearFilterAsync() for consistency with FilterService API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ClearAllFiltersAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Clearing all filters");

        try
        {
            // Clear all filter criteria (clears all columns)
            _rowStore.ClearFilterCriteria();

            _logger?.LogInformation("All filters cleared successfully");

            // ✅ PROFESSIONAL FIX: Trigger UI refresh to show all rows
            if (_uiNotificationService != null)
            {
                var totalCount = (int)await _rowStore.GetRowCountAsync(onlyFiltered: false, cancellationToken);
                _logger?.LogInformation("Triggering UI refresh after clear all filters: {TotalCount} rows", totalCount);
                await _uiNotificationService.NotifyDataRefreshAsync(totalCount, "ClearAllFilters");
            }
            else
            {
                _logger?.LogWarning("UiNotificationService not available - UI will not auto-refresh after clear");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to clear all filters");
            throw;
        }
    }
}

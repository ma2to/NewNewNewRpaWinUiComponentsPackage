using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;

/// <summary>
/// Service interface for filtering operations
/// Implements Scoped lifetime per DI_DECISIONS.md - per-operation state isolation
/// </summary>
internal interface IFilterService
{
    /// <summary>
    /// Apply filter to data
    /// </summary>
    Task<int> ApplyFilterAsync(string columnName, FilterOperator @operator, object? value);

    /// <summary>
    /// Clear all filters
    /// </summary>
    Task<int> ClearFiltersAsync();

    /// <summary>
    /// Get current filter state
    /// </summary>
    IReadOnlyList<FilterCriteria> GetActiveFilters();

    /// <summary>
    /// Check if filters are currently applied
    /// </summary>
    bool HasActiveFilters();

    /// <summary>
    /// Get filtered data based on active filters
    /// </summary>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetFilteredDataAsync();
}

/// <summary>
/// Filter criteria definition
/// </summary>
internal class FilterCriteria
{
    public string ColumnName { get; init; } = string.Empty;
    public FilterOperator Operator { get; init; }
    public object? Value { get; init; }
}
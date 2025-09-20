using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Search and filter functionality
/// CLEAN ARCHITECTURE: Application layer interface for search/filter operations
/// </summary>
internal interface ISearchFilterService
{
    // Advanced search operations
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default);

    // Quick search operations
    IReadOnlyList<SearchResult> QuickSearch(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        bool caseSensitive = false);

    // Filter operations
    FilterResult ApplyFilter(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        FilterDefinition filter);

    FilterResult ApplyFilters(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<FilterDefinition> filters,
        FilterLogicOperator logicOperator = FilterLogicOperator.And);

    Task<FilterResult> ApplyAdvancedFilterAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<AdvancedFilter> filters,
        CancellationToken cancellationToken = default);

    // Utility operations
    bool ValidateFilter(FilterDefinition filter);
    IReadOnlyList<string> GetFilterableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data);
}


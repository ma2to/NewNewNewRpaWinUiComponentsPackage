using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// PUBLIC FACADE: Main entry point for AdvancedWinUiDataGrid functionality
/// CLEAN ARCHITECTURE: Facade pattern hiding internal implementation details
/// ENTERPRISE: Professional API for data grid operations
/// </summary>
public sealed class AdvancedDataGridFacade
{
    private readonly ISearchFilterService _searchFilterService;
    private readonly ISortService _sortService;
    private readonly IImportExportService _importExportService;
    private readonly IKeyboardShortcutsService _keyboardShortcutsService;
    private readonly IPerformanceService _performanceService;
    private readonly IAutoRowHeightService _autoRowHeightService;

    /// <summary>
    /// CONSTRUCTOR: Default parameterless constructor for easy usage
    /// Creates internal services for standalone use
    /// </summary>
    public AdvancedDataGridFacade()
    {
        // Create internal service instances for standalone usage
        _searchFilterService = new SearchFilterService();
        _sortService = new SortService();
        _importExportService = new ImportExportService();
        _keyboardShortcutsService = new KeyboardShortcutsService();
        _performanceService = new PerformanceService();
        _autoRowHeightService = new AutoRowHeightService();
    }

    /// <summary>
    /// CONSTRUCTOR: Dependency injection constructor for advanced scenarios
    /// INTERNAL: Used by DI container when services are registered
    /// </summary>
    internal AdvancedDataGridFacade(
        ISearchFilterService searchFilterService,
        ISortService sortService,
        IImportExportService importExportService,
        IKeyboardShortcutsService keyboardShortcutsService,
        IPerformanceService performanceService,
        IAutoRowHeightService autoRowHeightService)
    {
        _searchFilterService = searchFilterService ?? throw new ArgumentNullException(nameof(searchFilterService));
        _sortService = sortService ?? throw new ArgumentNullException(nameof(sortService));
        _importExportService = importExportService ?? throw new ArgumentNullException(nameof(importExportService));
        _keyboardShortcutsService = keyboardShortcutsService ?? throw new ArgumentNullException(nameof(keyboardShortcutsService));
        _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
        _autoRowHeightService = autoRowHeightService ?? throw new ArgumentNullException(nameof(autoRowHeightService));
    }

    #region Search and Filter Operations

    /// <summary>
    /// PUBLIC API: Apply advanced filter to data with complex criteria
    /// </summary>
    public async Task<FilterResult> ApplyAdvancedFilterAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<AdvancedFilter> filters,
        CancellationToken cancellationToken = default)
    {
        var internalFilters = filters.ToInternalList(f => f.ToInternal());
        var result = await _searchFilterService.ApplyAdvancedFilterAsync(data, internalFilters, cancellationToken);
        return result.ToPublic();
    }

    /// <summary>
    /// PUBLIC API: Apply simple filter to data
    /// </summary>
    public FilterResult ApplyFilter(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        FilterDefinition filter)
    {
        var result = _searchFilterService.ApplyFilter(data, filter.ToInternal());
        return result.ToPublic();
    }

    /// <summary>
    /// PUBLIC API: Apply multiple filters to data
    /// </summary>
    public FilterResult ApplyFilters(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<FilterDefinition> filters,
        FilterLogicOperator logicOperator = FilterLogicOperator.And)
    {
        var internalFilters = filters.ToInternalList(f => f.ToInternal());
        var internalLogicOperator = (Core.ValueObjects.FilterLogicOperator)(int)logicOperator;
        var result = _searchFilterService.ApplyFilters(data, internalFilters, internalLogicOperator);
        return result.ToPublic();
    }

    /// <summary>
    /// PUBLIC API: Search data with advanced criteria
    /// </summary>
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchFilterService.SearchAsync(data, searchCriteria.ToInternal(), cancellationToken);
        return results.ToPublicList(r => r.ToPublic());
    }

    /// <summary>
    /// PUBLIC API: Simple text search across all columns
    /// </summary>
    public IReadOnlyList<SearchResult> QuickSearch(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string searchText,
        bool caseSensitive = false)
    {
        var results = _searchFilterService.QuickSearch(data, searchText, caseSensitive);
        return results.ToPublicList(r => r.ToPublic());
    }

    #endregion

    #region Sort Operations

    /// <summary>
    /// PUBLIC API: Sort data by single column
    /// </summary>
    public async Task<SortResult> SortAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        SortDirection direction = SortDirection.Ascending,
        CancellationToken cancellationToken = default)
    {
        var internalDirection = (Core.ValueObjects.SortDirection)(int)direction;
        var result = await _sortService.SortAsync(data, columnName, internalDirection, cancellationToken);
        return result.ToPublic();
    }

    /// <summary>
    /// PUBLIC API: Sort data by multiple columns
    /// </summary>
    public async Task<SortResult> MultiSortAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<SortColumnConfiguration> sortConfigurations,
        CancellationToken cancellationToken = default)
    {
        var internalConfigs = sortConfigurations.ToInternalList(c => c.ToInternal());
        var result = await _sortService.MultiSortAsync(data, internalConfigs, cancellationToken);
        return result.ToPublic();
    }

    /// <summary>
    /// PUBLIC API: Synchronous sort by column
    /// </summary>
    public SortResult Sort(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        SortDirection direction = SortDirection.Ascending)
    {
        var internalDirection = (Core.ValueObjects.SortDirection)(int)direction;
        var result = _sortService.Sort(data, columnName, internalDirection);
        return result.ToPublic();
    }

    #endregion

    #region Import Operations

    /// <summary>
    /// PUBLIC API: Import data from DataTable
    /// </summary>
    public async Task<ImportResult> ImportFromDataTableAsync(
        DataTable dataTable,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var internalOptions = options?.ToInternal();
        var result = await _importExportService.ImportFromDataTableAsync(dataTable, internalOptions, cancellationToken);
        return result.ToPublic();
    }

    /// <summary>
    /// PUBLIC API: Import data from Dictionary collection
    /// </summary>
    public async Task<ImportResult> ImportFromDictionaryAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> sourceData,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var internalOptions = options?.ToInternal();
        var result = await _importExportService.ImportFromDictionaryAsync(sourceData, internalOptions, cancellationToken);
        return result.ToPublic();
    }

    #endregion

    #region Export Operations

    /// <summary>
    /// PUBLIC API: Export data to DataTable
    /// </summary>
    public async Task<DataTable> ExportToDataTableAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var internalOptions = options?.ToInternal();
        return await _importExportService.ExportToDataTableAsync(data, internalOptions, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Export data to Dictionary collection
    /// </summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExportToDictionaryAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var internalOptions = options?.ToInternal();
        return await _importExportService.ExportToDictionaryAsync(data, internalOptions, cancellationToken);
    }

    #endregion

    #region Copy/Paste Operations

    /// <summary>
    /// PUBLIC API: Copy selected data to clipboard
    /// </summary>
    public async Task<CopyPasteResult> CopyToClipboardAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> selectedData,
        bool includeHeaders = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _importExportService.CopyToClipboardAsync(selectedData, includeHeaders, cancellationToken);
        return result.ToPublic();
    }

    /// <summary>
    /// PUBLIC API: Paste data from clipboard
    /// </summary>
    public async Task<CopyPasteResult> PasteFromClipboardAsync(
        int targetRowIndex = 0,
        int targetColumnIndex = 0,
        ImportMode mode = ImportMode.Replace,
        CancellationToken cancellationToken = default)
    {
        var internalMode = (Core.ValueObjects.ImportMode)(int)mode;
        var result = await _importExportService.PasteFromClipboardAsync(targetRowIndex, targetColumnIndex, internalMode, cancellationToken);
        return result.ToPublic();
    }

    #endregion

    #region Keyboard Shortcuts

    /// <summary>
    /// PUBLIC API: Register keyboard shortcut for data grid operations
    /// </summary>
    public Task<KeyboardShortcutResult> RegisterShortcutAsync(
        KeyboardShortcut shortcut,
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        return _keyboardShortcutsService.RegisterShortcutAsync(shortcut, action, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Execute keyboard shortcut by key combination
    /// </summary>
    public Task<KeyboardShortcutResult> ExecuteShortcutAsync(
        string keysCombination,
        CancellationToken cancellationToken = default)
    {
        return _keyboardShortcutsService.ExecuteShortcutAsync(keysCombination, cancellationToken);
    }

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// PUBLIC API: Get performance metrics for operations
    /// </summary>
    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(
        TimeSpan? timeWindow = null,
        CancellationToken cancellationToken = default)
    {
        return await _performanceService.GetPerformanceMetricsAsync(timeWindow, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Reset performance counters
    /// </summary>
    public Task ResetPerformanceCountersAsync(CancellationToken cancellationToken = default)
    {
        return _performanceService.ResetPerformanceCountersAsync(cancellationToken);
    }

    #endregion

    #region Auto Row Height

    /// <summary>
    /// PUBLIC API: Enable automatic row height adjustment
    /// </summary>
    public Task<AutoRowHeightResult> EnableAutoRowHeightAsync(
        AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return _autoRowHeightService.EnableAutoRowHeightAsync(configuration, cancellationToken);
    }

    /// <summary>
    /// PUBLIC API: Calculate optimal row heights
    /// </summary>
    public Task<AutoRowHeightResult> CalculateOptimalRowHeightsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        RowHeightCalculationOptions options,
        CancellationToken cancellationToken = default)
    {
        return _autoRowHeightService.CalculateOptimalRowHeightsAsync(data, options, cancellationToken);
    }

    #endregion
}
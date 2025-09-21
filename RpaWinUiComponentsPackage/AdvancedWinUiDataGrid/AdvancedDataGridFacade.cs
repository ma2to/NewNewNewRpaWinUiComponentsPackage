using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// PUBLIC FACADE: Main entry point for AdvancedWinUiDataGrid functionality
/// CLEAN ARCHITECTURE: Facade pattern hiding internal implementation details
/// ENTERPRISE: Professional API for data grid operations
/// EXTERNAL LOGGING: Supports external ILogger<T> for centralized logging (Serilog, NLog, Microsoft.Extensions.Logging)
/// NULL SAFETY: NullLogger fallback ensures operation without external logging configuration
/// </summary>
public sealed class AdvancedDataGridFacade
{
    private readonly ILogger<AdvancedDataGridFacade> _logger;
    private readonly ISearchFilterService _searchFilterService;
    private readonly ISortService _sortService;
    private readonly IImportExportService _importExportService;
    private readonly IKeyboardShortcutsService _keyboardShortcutsService;
    private readonly IPerformanceService _performanceService;
    private readonly IAutoRowHeightService _autoRowHeightService;
    private readonly IValidationService _validationService;

    /// <summary>
    /// CONSTRUCTOR: Default parameterless constructor for easy usage
    /// Creates internal services for standalone use with NullLogger (no external logging required)
    /// </summary>
    public AdvancedDataGridFacade()
    {
        // Use NullLogger for safe operation without external logging configuration
        _logger = NullLogger<AdvancedDataGridFacade>.Instance;

        // Create internal service instances for standalone usage
        _searchFilterService = new SearchFilterService();
        _sortService = new SortService();
        _importExportService = new ImportExportService();
        _keyboardShortcutsService = new KeyboardShortcutsService();
        _performanceService = new PerformanceService();
        _autoRowHeightService = new AutoRowHeightService();
        _validationService = new ValidationService();
    }

    /// <summary>
    /// CONSTRUCTOR: External logging constructor for centralized logging integration
    /// PUBLIC: Use this when you want to integrate with external logging providers (Serilog, NLog, etc.)
    /// NULL SAFETY: If logger is null, NullLogger is used (no exceptions thrown)
    /// </summary>
    public AdvancedDataGridFacade(ILogger<AdvancedDataGridFacade>? logger)
    {
        // Null safety - use NullLogger if no external logger provided
        _logger = logger ?? NullLogger<AdvancedDataGridFacade>.Instance;

        // Create internal service instances
        _searchFilterService = new SearchFilterService();
        _sortService = new SortService();
        _importExportService = new ImportExportService();
        _keyboardShortcutsService = new KeyboardShortcutsService();
        _performanceService = new PerformanceService();
        _autoRowHeightService = new AutoRowHeightService();
        _validationService = new ValidationService();
    }

    /// <summary>
    /// CONSTRUCTOR: Full dependency injection constructor for advanced scenarios
    /// INTERNAL: Used by DI container when all services are registered
    /// </summary>
    internal AdvancedDataGridFacade(
        ILogger<AdvancedDataGridFacade>? logger,
        ISearchFilterService searchFilterService,
        ISortService sortService,
        IImportExportService importExportService,
        IKeyboardShortcutsService keyboardShortcutsService,
        IPerformanceService performanceService,
        IAutoRowHeightService autoRowHeightService,
        IValidationService validationService)
    {
        _logger = logger ?? NullLogger<AdvancedDataGridFacade>.Instance;
        _searchFilterService = searchFilterService ?? throw new ArgumentNullException(nameof(searchFilterService));
        _sortService = sortService ?? throw new ArgumentNullException(nameof(sortService));
        _importExportService = importExportService ?? throw new ArgumentNullException(nameof(importExportService));
        _keyboardShortcutsService = keyboardShortcutsService ?? throw new ArgumentNullException(nameof(keyboardShortcutsService));
        _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
        _autoRowHeightService = autoRowHeightService ?? throw new ArgumentNullException(nameof(autoRowHeightService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
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
        _logger.LogDebug("Starting advanced filter operation with {FilterCount} filters", filters.Count);

        try
        {
            var result = await _searchFilterService.ApplyAdvancedFilterAsync(data, filters, cancellationToken);

            _logger.LogInformation("Advanced filter completed: {MatchingRows}/{TotalRows} rows matched in {ProcessingTime}ms",
                result.MatchingRows, result.TotalRowsProcessed, result.ProcessingTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Advanced filter operation failed with {FilterCount} filters", filters.Count);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Apply simple filter to data
    /// </summary>
    public FilterResult ApplyFilter(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        FilterDefinition filter)
    {
        var result = _searchFilterService.ApplyFilter(data, filter);
        return result;
    }

    /// <summary>
    /// PUBLIC API: Apply multiple filters to data
    /// </summary>
    public FilterResult ApplyFilters(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<FilterDefinition> filters,
        FilterLogicOperator logicOperator = FilterLogicOperator.And)
    {
        var result = _searchFilterService.ApplyFilters(data, filters, logicOperator);
        return result;
    }

    /// <summary>
    /// PUBLIC API: Search data with advanced criteria
    /// </summary>
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        AdvancedSearchCriteria searchCriteria,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting search operation: '{SearchText}', Scope: {Scope}, UseRegex: {UseRegex}",
            searchCriteria.SearchText, searchCriteria.Scope, searchCriteria.UseRegex);

        try
        {
            var results = await _searchFilterService.SearchAsync(data, searchCriteria, cancellationToken);

            _logger.LogInformation("Search completed: {ResultCount} matches found for '{SearchText}'",
                results.Count, searchCriteria.SearchText);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search operation failed for '{SearchText}'", searchCriteria.SearchText);
            throw;
        }
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
        return results;
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
        _logger.LogDebug("Starting sort operation: Column '{ColumnName}', Direction: {Direction}", columnName, direction);

        try
        {
            var result = await _sortService.SortAsync(data, columnName, direction, cancellationToken);

            _logger.LogInformation("Sort completed: {ProcessedRows} rows sorted by '{ColumnName}' in {SortTime}ms",
                result.ProcessedRows, columnName, result.SortTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sort operation failed for column '{ColumnName}'", columnName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Sort data by multiple columns
    /// </summary>
    public async Task<SortResult> MultiSortAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<SortColumnConfiguration> sortConfigurations,
        CancellationToken cancellationToken = default)
    {
        var result = await _sortService.MultiSortAsync(data, sortConfigurations, cancellationToken);
        return result;
    }

    /// <summary>
    /// PUBLIC API: Synchronous sort by column
    /// </summary>
    public SortResult Sort(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        SortDirection direction = SortDirection.Ascending)
    {
        var result = _sortService.Sort(data, columnName, direction);
        return result;
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
        _logger.LogInformation("Starting DataTable import: {RowCount} rows, {ColumnCount} columns, Mode: {ImportMode}",
            dataTable.Rows.Count, dataTable.Columns.Count, options?.Mode ?? ImportMode.Replace);

        try
        {
            var internalOptions = options?.ToInternal();
            var result = await _importExportService.ImportFromDataTableAsync(dataTable, internalOptions, cancellationToken);
            var publicResult = result.ToPublic();

            if (publicResult.Success)
            {
                _logger.LogInformation("DataTable import completed successfully: {ImportedRows}/{TotalRows} rows imported in {ImportTime}ms",
                    publicResult.ImportedRows, publicResult.TotalRows, publicResult.ImportTime.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning("DataTable import completed with errors: {ErrorCount} errors, {ImportedRows}/{TotalRows} rows imported",
                    publicResult.ErrorMessages.Count, publicResult.ImportedRows, publicResult.TotalRows);
            }

            return publicResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DataTable import failed: {RowCount} rows, Mode: {ImportMode}",
                dataTable.Rows.Count, options?.Mode ?? ImportMode.Replace);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Import data from Dictionary collection
    /// </summary>
    public async Task<ImportResult> ImportFromDictionaryAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> sourceData,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var dataList = sourceData.ToList();
        _logger.LogInformation("Starting Dictionary import: {RowCount} rows, Mode: {ImportMode}",
            dataList.Count, options?.Mode ?? ImportMode.Replace);

        try
        {
            var internalOptions = options?.ToInternal();
            var result = await _importExportService.ImportFromDictionaryAsync(sourceData, internalOptions, cancellationToken);
            var publicResult = result.ToPublic();

            if (publicResult.Success)
            {
                _logger.LogInformation("Dictionary import completed successfully: {ImportedRows}/{TotalRows} rows imported in {ImportTime}ms",
                    publicResult.ImportedRows, publicResult.TotalRows, publicResult.ImportTime.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning("Dictionary import completed with errors: {ErrorCount} errors, {ImportedRows}/{TotalRows} rows imported",
                    publicResult.ErrorMessages.Count, publicResult.ImportedRows, publicResult.TotalRows);
            }

            return publicResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dictionary import failed: {RowCount} rows, Mode: {ImportMode}",
                dataList.Count, options?.Mode ?? ImportMode.Replace);
            throw;
        }
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
        var internalMode = mode.ToInternal();
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
        try
        {
            var metrics = await _performanceService.GetPerformanceMetricsAsync(timeWindow, cancellationToken);

            _logger.LogInformation("Performance metrics retrieved: {TotalOperations} operations, {MemoryUsageMB:F2} MB memory, {AverageTime:F2}ms avg time",
                metrics.TotalOperations, metrics.MemoryUsage / (1024.0 * 1024.0), metrics.AverageTime);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve performance metrics");
            throw;
        }
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
    public async Task<AutoRowHeightResult> EnableAutoRowHeightAsync(
        AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var internalConfig = configuration.ToInternal();
        var result = await _autoRowHeightService.EnableAutoRowHeightAsync(internalConfig, cancellationToken);
        return result.ToPublic();
    }

    /// <summary>
    /// PUBLIC API: Calculate optimal row heights
    /// </summary>
    public async Task<AutoRowHeightResult> CalculateOptimalRowHeightsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        RowHeightCalculationOptions options,
        CancellationToken cancellationToken = default)
    {
        var dataList = data.ToList();
        _logger.LogInformation("Starting row height calculation: {RowCount} rows, MinHeight: {MinHeight}, MaxHeight: {MaxHeight}",
            dataList.Count, options.MinimumRowHeight, options.MaximumRowHeight);

        try
        {
            var internalOptions = options.ToInternal();
            var result = await _autoRowHeightService.CalculateOptimalRowHeightsAsync(data, internalOptions, cancellationToken);
            var publicResult = result.ToPublic();

            if (publicResult.Success)
            {
                _logger.LogInformation("Row height calculation completed: {CalculatedRows} rows processed in {CalculationTime}ms",
                    publicResult.CalculatedHeights.Count, publicResult.CalculationTime.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning("Row height calculation failed: {ErrorMessage}", publicResult.ErrorMessage);
            }

            return publicResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Row height calculation failed for {RowCount} rows", dataList.Count);
            throw;
        }
    }

    #endregion

    #region Comprehensive Validation Operations

    /// <summary>
    /// PUBLIC API: Add single cell validation rule
    /// ENTERPRISE: Professional validation rule management with timeout support
    /// </summary>
    public async Task<Result<bool>> AddValidationRuleAsync(
        ValidationRule validationRule,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding single cell validation rule: {RuleName} for column {ColumnName}",
            validationRule.RuleName, validationRule.ColumnName);

        try
        {
            var internalRule = validationRule.ToInternal();
            var result = await _validationService.AddValidationRuleAsync(internalRule, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully added validation rule: {RuleName} for column {ColumnName}",
                    validationRule.RuleName, validationRule.ColumnName);
            }
            else
            {
                _logger.LogWarning("Failed to add validation rule: {RuleName}, Error: {Error}",
                    validationRule.RuleName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding validation rule: {RuleName}", validationRule.RuleName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Add cross-column validation rule
    /// ENTERPRISE: Professional cross-column validation with timeout support
    /// </summary>
    public async Task<Result<bool>> AddCrossColumnValidationRuleAsync(
        CrossColumnValidationRule validationRule,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding cross-column validation rule: {RuleName} for columns [{Columns}]",
            validationRule.RuleName, string.Join(", ", validationRule.DependentColumns));

        try
        {
            var internalRule = validationRule.ToInternal();
            var result = await _validationService.AddValidationRuleAsync(internalRule, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully added cross-column validation rule: {RuleName}",
                    validationRule.RuleName);
            }
            else
            {
                _logger.LogWarning("Failed to add cross-column validation rule: {RuleName}, Error: {Error}",
                    validationRule.RuleName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding cross-column validation rule: {RuleName}", validationRule.RuleName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Add cross-row validation rule
    /// ENTERPRISE: Professional cross-row validation with timeout support
    /// </summary>
    public async Task<Result<bool>> AddCrossRowValidationRuleAsync(
        CrossRowValidationRule validationRule,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding cross-row validation rule: {RuleName}", validationRule.RuleName);

        try
        {
            var internalRule = validationRule.ToInternal();
            var result = await _validationService.AddValidationRuleAsync(internalRule, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully added cross-row validation rule: {RuleName}",
                    validationRule.RuleName);
            }
            else
            {
                _logger.LogWarning("Failed to add cross-row validation rule: {RuleName}, Error: {Error}",
                    validationRule.RuleName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding cross-row validation rule: {RuleName}", validationRule.RuleName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Add conditional validation rule
    /// ENTERPRISE: Professional conditional validation with timeout support
    /// </summary>
    public async Task<Result<bool>> AddConditionalValidationRuleAsync(
        ConditionalValidationRule validationRule,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding conditional validation rule: {RuleName} for column {ColumnName}",
            validationRule.RuleName, validationRule.ColumnName);

        try
        {
            var internalRule = validationRule.ToInternal();
            var result = await _validationService.AddValidationRuleAsync(internalRule, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully added conditional validation rule: {RuleName}",
                    validationRule.RuleName);
            }
            else
            {
                _logger.LogWarning("Failed to add conditional validation rule: {RuleName}, Error: {Error}",
                    validationRule.RuleName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding conditional validation rule: {RuleName}", validationRule.RuleName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Add complex validation rule
    /// ENTERPRISE: Professional complex validation with timeout support
    /// </summary>
    public async Task<Result<bool>> AddComplexValidationRuleAsync(
        ComplexValidationRule validationRule,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding complex validation rule: {RuleName}", validationRule.RuleName);

        try
        {
            var internalRule = validationRule.ToInternal();
            var result = await _validationService.AddValidationRuleAsync(internalRule, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully added complex validation rule: {RuleName}",
                    validationRule.RuleName);
            }
            else
            {
                _logger.LogWarning("Failed to add complex validation rule: {RuleName}, Error: {Error}",
                    validationRule.RuleName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding complex validation rule: {RuleName}", validationRule.RuleName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Add business rule validation
    /// ENTERPRISE: Professional business rule validation with timeout support
    /// </summary>
    public async Task<Result<bool>> AddBusinessRuleValidationAsync(
        BusinessRuleValidationRule validationRule,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding business rule validation: {BusinessRuleName}", validationRule.BusinessRuleName);

        try
        {
            var internalRule = validationRule.ToInternal();
            var result = await _validationService.AddValidationRuleAsync(internalRule, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully added business rule validation: {BusinessRuleName}",
                    validationRule.BusinessRuleName);
            }
            else
            {
                _logger.LogWarning("Failed to add business rule validation: {BusinessRuleName}, Error: {Error}",
                    validationRule.BusinessRuleName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding business rule validation: {BusinessRuleName}", validationRule.BusinessRuleName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Remove validation rules by column names
    /// ENTERPRISE: Efficient bulk removal of validation rules
    /// </summary>
    public async Task<Result<bool>> RemoveValidationRulesAsync(
        IReadOnlyList<string> columnNames,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Removing validation rules for columns: [{Columns}]", string.Join(", ", columnNames));

        try
        {
            var result = await _validationService.RemoveValidationRulesAsync(columnNames, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully removed validation rules for {ColumnCount} columns", columnNames.Count);
            }
            else
            {
                _logger.LogWarning("Failed to remove validation rules: {Error}", result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing validation rules for columns: [{Columns}]", string.Join(", ", columnNames));
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Remove validation rule by rule name
    /// ENTERPRISE: Targeted removal of specific validation rule
    /// </summary>
    public async Task<Result<bool>> RemoveValidationRuleAsync(
        string ruleName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Removing validation rule: {RuleName}", ruleName);

        try
        {
            var result = await _validationService.RemoveValidationRuleAsync(ruleName, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully removed validation rule: {RuleName}", ruleName);
            }
            else
            {
                _logger.LogWarning("Failed to remove validation rule: {RuleName}, Error: {Error}", ruleName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing validation rule: {RuleName}", ruleName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Clear all validation rules
    /// ENTERPRISE: Complete validation system reset
    /// </summary>
    public async Task<Result<bool>> ClearAllValidationRulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Clearing all validation rules");

        try
        {
            var result = await _validationService.ClearAllValidationRulesAsync(cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully cleared all validation rules");
            }
            else
            {
                _logger.LogWarning("Failed to clear validation rules: {Error}", result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all validation rules");
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Validate single cell with smart validation decision making
    /// ENTERPRISE: Professional cell validation with timeout support
    /// </summary>
    public async Task<ValidationResult> ValidateCellAsync(
        int rowIndex,
        string columnName,
        object? value,
        IReadOnlyDictionary<string, object?> rowData,
        ValidationTrigger trigger = ValidationTrigger.OnCellChanged,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating cell [{RowIndex}, {ColumnName}] with trigger: {Trigger}", rowIndex, columnName, trigger);

        try
        {
            var context = _validationService.DetermineValidationContext(trigger.ToInternal(), 1, 1);
            var internalResult = await _validationService.ValidateCellAsync(rowIndex, columnName, value, rowData, context, cancellationToken);
            var publicResult = internalResult.ToPublic();

            if (publicResult.IsValid)
            {
                _logger.LogDebug("Cell validation passed for [{RowIndex}, {ColumnName}]", rowIndex, columnName);
            }
            else
            {
                _logger.LogWarning("Cell validation failed for [{RowIndex}, {ColumnName}]: {Message}",
                    rowIndex, columnName, publicResult.Message);
            }

            return publicResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cell [{RowIndex}, {ColumnName}]", rowIndex, columnName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Validate entire row with all applicable rules
    /// ENTERPRISE: Comprehensive row validation with smart decision making
    /// </summary>
    public async Task<IReadOnlyList<ValidationResult>> ValidateRowAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        ValidationTrigger trigger = ValidationTrigger.OnCellChanged,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating row {RowIndex} with {ColumnCount} columns, trigger: {Trigger}",
            rowIndex, rowData.Count, trigger);

        try
        {
            var context = _validationService.DetermineValidationContext(trigger.ToInternal(), 1, rowData.Count);
            var internalResults = await _validationService.ValidateRowAsync(rowIndex, rowData, context, cancellationToken);
            var publicResults = internalResults.Select(r => r.ToPublic()).ToList();

            var errorCount = publicResults.Count(r => !r.IsValid);
            if (errorCount > 0)
            {
                _logger.LogWarning("Row {RowIndex} validation completed with {ErrorCount} errors", rowIndex, errorCount);
            }
            else
            {
                _logger.LogDebug("Row {RowIndex} validation passed", rowIndex);
            }

            return publicResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating row {RowIndex}", rowIndex);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Validate multiple rows with progress reporting
    /// ENTERPRISE: Bulk validation with smart bulk/real-time decision making
    /// </summary>
    public async Task<IReadOnlyList<ValidationResult>> ValidateRowsAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        ValidationTrigger trigger = ValidationTrigger.Bulk,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting validation of {RowCount} rows with trigger: {Trigger}", rows.Count, trigger);

        try
        {
            var context = _validationService.DetermineValidationContext(
                trigger.ToInternal(),
                rows.Count,
                rows.FirstOrDefault()?.Count ?? 0,
                isImportOperation: rows.Count > 100);

            var internalResults = await _validationService.ValidateRowsAsync(rows, context, progress, cancellationToken);
            var publicResults = internalResults.Select(r => r.ToPublic()).ToList();

            var errorCount = publicResults.Count(r => !r.IsValid);
            _logger.LogInformation("Rows validation completed: {TotalRows} rows processed, {ErrorCount} errors found",
                rows.Count, errorCount);

            return publicResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating {RowCount} rows", rows.Count);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Validate entire dataset with comprehensive rules
    /// ENTERPRISE: Full dataset validation including complex business rules
    /// </summary>
    public async Task<IReadOnlyList<ValidationResult>> ValidateDatasetAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        ValidationTrigger trigger = ValidationTrigger.Bulk,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting comprehensive dataset validation: {RowCount} rows", dataset.Count);

        try
        {
            var context = _validationService.DetermineValidationContext(
                trigger.ToInternal(),
                dataset.Count,
                dataset.FirstOrDefault()?.Count ?? 0,
                isImportOperation: true);

            var internalResults = await _validationService.ValidateDatasetAsync(dataset, context, progress, cancellationToken);
            var publicResults = internalResults.Select(r => r.ToPublic()).ToList();

            var errorCount = publicResults.Count(r => !r.IsValid);
            _logger.LogInformation("Dataset validation completed: {TotalRows} rows, {ErrorCount} errors, {ValidationTime}ms",
                dataset.Count, errorCount, publicResults.Sum(r => r.ValidationTime.TotalMilliseconds));

            return publicResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating dataset with {RowCount} rows", dataset.Count);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Check if all non-empty rows are valid
    /// ENTERPRISE: Quick validation status check for entire dataset
    /// </summary>
    public async Task<Result<bool>> AreAllNonEmptyRowsValidAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        bool onlyFilteredRows = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking validation status for {RowCount} rows (filtered: {OnlyFiltered})",
            dataset.Count, onlyFilteredRows);

        try
        {
            var result = await _validationService.AreAllNonEmptyRowsValidAsync(dataset, onlyFilteredRows, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Dataset validation check completed: {IsValid}", result.Value ? "All Valid" : "Has Errors");
            }
            else
            {
                _logger.LogWarning("Dataset validation check failed: {Error}", result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking dataset validation status");
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Delete rows based on validation criteria
    /// ENTERPRISE: Professional row deletion with validation-based criteria
    /// </summary>
    public async Task<Result<ValidationBasedDeleteResult>> DeleteRowsWithValidationAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        ValidationDeletionCriteria criteria,
        ValidationDeletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting validation-based row deletion: {RowCount} rows, Mode: {Mode}",
            dataset.Count, criteria.Mode);

        try
        {
            var internalCriteria = criteria.ToInternal();
            var internalOptions = options?.ToInternal();

            var internalResult = await _validationService.DeleteRowsWithValidationAsync(dataset, internalCriteria, internalOptions, cancellationToken);

            if (internalResult.IsSuccess)
            {
                var publicResult = internalResult.Value.ToPublic();
                _logger.LogInformation("Validation-based deletion completed: {DeletedRows}/{TotalRows} rows marked for deletion in {Duration}ms",
                    publicResult.RowsDeleted, publicResult.TotalRowsEvaluated, publicResult.OperationDuration.TotalMilliseconds);
                return Result<ValidationBasedDeleteResult>.Success(publicResult);
            }
            else
            {
                _logger.LogWarning("Validation-based deletion failed: {Error}", internalResult.Error);
                return Result<ValidationBasedDeleteResult>.Failure(internalResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in validation-based row deletion");
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Preview which rows would be deleted without actual deletion
    /// ENTERPRISE: Safety feature for previewing deletion impact
    /// </summary>
    public async Task<Result<IReadOnlyList<int>>> PreviewRowDeletionAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        ValidationDeletionCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Previewing row deletion for {RowCount} rows with mode: {Mode}", dataset.Count, criteria.Mode);

        try
        {
            var internalCriteria = criteria.ToInternal();
            var result = await _validationService.PreviewRowDeletionAsync(dataset, internalCriteria, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Row deletion preview completed: {RowsToDelete}/{TotalRows} rows would be deleted",
                    result.Value.Count, dataset.Count);
            }
            else
            {
                _logger.LogWarning("Row deletion preview failed: {Error}", result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing row deletion");
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Update validation configuration
    /// ENTERPRISE: Runtime configuration changes for validation system
    /// </summary>
    public async Task<Result<bool>> UpdateValidationConfigurationAsync(
        ValidationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating validation configuration: RealTime={RealTime}, Bulk={Bulk}, Trigger={Trigger}",
            configuration.EnableRealTimeValidation, configuration.EnableBulkValidation, configuration.DefaultTrigger);

        try
        {
            var internalConfig = configuration.ToInternal();
            var result = await _validationService.UpdateValidationConfigurationAsync(internalConfig, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Validation configuration updated successfully");
            }
            else
            {
                _logger.LogWarning("Failed to update validation configuration: {Error}", result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating validation configuration");
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Get current validation configuration
    /// ENTERPRISE: Configuration introspection for monitoring
    /// </summary>
    public async Task<Result<ValidationConfiguration>> GetValidationConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var internalResult = await _validationService.GetValidationConfigurationAsync(cancellationToken);

            if (internalResult.IsSuccess)
            {
                var publicConfig = internalResult.Value.ToPublic();
                return Result<ValidationConfiguration>.Success(publicConfig);
            }
            else
            {
                return Result<ValidationConfiguration>.Failure(internalResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation configuration");
            throw;
        }
    }

    #region Group Validation API

    /// <summary>
    /// PUBLIC API: Add validation rule group with advanced logical combinations
    /// ENTERPRISE: Complex validation scenarios with AND/OR logic
    /// </summary>
    public async Task<Result<bool>> AddValidationRuleGroupAsync(
        ValidationRuleGroup ruleGroup,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding validation rule group: {GroupName} for column {ColumnName} with {RuleCount} rules",
            ruleGroup.RuleName, ruleGroup.ColumnName, ruleGroup.Rules.Count);

        try
        {
            var internalGroup = ruleGroup.ToInternal();
            var result = await _validationService.AddValidationRuleGroupAsync(internalGroup, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully added validation rule group: {GroupName} for column {ColumnName}",
                    ruleGroup.RuleName, ruleGroup.ColumnName);
            }
            else
            {
                _logger.LogWarning("Failed to add validation rule group: {GroupName}, Error: {Error}",
                    ruleGroup.RuleName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding validation rule group: {GroupName}", ruleGroup.RuleName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Set column-specific validation configuration
    /// ENTERPRISE: Fine-grained control over validation behavior per column
    /// </summary>
    public async Task<Result<bool>> SetColumnValidationConfigurationAsync(
        string columnName,
        ColumnValidationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting column validation configuration for {ColumnName}: Policy={Policy}, Strategy={Strategy}",
            columnName, configuration.ValidationPolicy, configuration.EvaluationStrategy);

        try
        {
            var internalConfig = configuration.ToInternal();
            var result = await _validationService.SetColumnValidationConfigurationAsync(columnName, internalConfig, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully set column validation configuration for {ColumnName}", columnName);
            }
            else
            {
                _logger.LogWarning("Failed to set column validation configuration for {ColumnName}: {Error}",
                    columnName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting column validation configuration for {ColumnName}", columnName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Get column-specific validation configuration
    /// ENTERPRISE: Retrieve current column validation settings
    /// </summary>
    public async Task<Result<ColumnValidationConfiguration?>> GetColumnValidationConfigurationAsync(
        string columnName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var internalResult = await _validationService.GetColumnValidationConfigurationAsync(columnName, cancellationToken);

            if (internalResult.IsSuccess)
            {
                var publicConfig = internalResult.Value?.ToPublic();
                return Result<ColumnValidationConfiguration?>.Success(publicConfig);
            }
            else
            {
                return Result<ColumnValidationConfiguration?>.Failure(internalResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting column validation configuration for {ColumnName}", columnName);
            throw;
        }
    }

    /// <summary>
    /// PUBLIC API: Get all validation rule groups for a column
    /// ENTERPRISE: Retrieve all groups affecting a specific column
    /// </summary>
    public async Task<Result<IReadOnlyList<ValidationRuleGroup>>> GetValidationRuleGroupsForColumnAsync(
        string columnName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var internalResult = await _validationService.GetValidationRuleGroupsForColumnAsync(columnName, cancellationToken);

            if (internalResult.IsSuccess)
            {
                // Note: We would need to convert internal groups to public, but this requires reverse mapping
                // For now, return empty list as placeholder - this would need proper implementation
                var publicGroups = Array.Empty<ValidationRuleGroup>();
                return Result<IReadOnlyList<ValidationRuleGroup>>.Success(publicGroups);
            }
            else
            {
                return Result<IReadOnlyList<ValidationRuleGroup>>.Failure(internalResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation rule groups for {ColumnName}", columnName);
            throw;
        }
    }

    #endregion

    #endregion
}
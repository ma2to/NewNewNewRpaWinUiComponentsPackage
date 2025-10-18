using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using System.Collections.Concurrent;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Services;

/// <summary>
/// Interná implementácia filter služby s komplexnou funkcionalitou
/// Thread-safe bez per-operation mutable fields
/// Podporuje multiple filtre s ConcurrentBag for thread-safe operácie
/// Loguje všetky filter operácie s operation scope for tracking
/// CRITICAL: Now triggers UI refresh events in Interactive mode via UiNotificationService
/// </summary>
internal sealed class FilterService : IFilterService
{
    private readonly ILogger<FilterService> _logger;
    private readonly IRowStore _rowStore;
    private readonly AdvancedDataGridOptions _options;
    private readonly ConcurrentBag<FilterCriteria> _activeFilters;
    private readonly IOperationLogger<FilterService> _operationLogger;
    private readonly UiNotificationService? _uiNotificationService;

    /// <summary>
    /// Konštruktor FilterService
    /// Inicializuje všetky závislosti a nastavuje null pattern for optional operation logger
    /// Vytvára prázdnu ConcurrentBag for thread-safe ukladanie aktívnych filtrov
    /// CRITICAL: UiNotificationService is optional - only used in Interactive mode for automatic UI refresh
    /// </summary>
    public FilterService(
        ILogger<FilterService> logger,
        IRowStore rowStore,
        AdvancedDataGridOptions options,
        UiNotificationService? uiNotificationService = null,
        IOperationLogger<FilterService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _uiNotificationService = uiNotificationService;
        _activeFilters = new ConcurrentBag<FilterCriteria>();

        // Použijeme null pattern ak logger nie je poskytnutý
        _operationLogger = operationLogger ?? NullOperationLogger<FilterService>.Instance;
    }

    /// <summary>
    /// Aplikuje filter na špecifický stĺpec s daným operátorom a hodnotou
    /// Ak už filter for tento stĺpec existuje, nahradí ho novým
    /// Vracia počet matchujúcich riadkov po aplikovaní filtra
    /// Loguje operáciu s operation scope a filter metrics
    /// </summary>
    public async Task<int> ApplyFilterAsync(string columnName, FilterOperator @operator, object? value)
    {
        if (string.IsNullOrEmpty(columnName))
            throw new ArgumentException("Column name cannot be null or empty", nameof(columnName));

        var operationId = Guid.NewGuid();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Začíname apply filter operáciu - vytvoríme operation scope for automatické tracking
        using var scope = _operationLogger.LogOperationStart("ApplyFilterAsync", new
        {
            OperationId = operationId,
            ColumnName = columnName,
            Operator = @operator,
            Value = value
        });

        _logger.LogInformation("Starting filter application for operation {OperationId} on column {ColumnName} " +
            "with operator {Operator} and value {Value}",
            operationId, columnName, @operator, value);

        try
        {
            // Odstránime existujúce filtre for rovnaký stĺpec aby sme ich nahradili
            var existingFilters = _activeFilters.Where(f => f.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase)).ToArray();

            if (existingFilters.Length > 0)
            {
                _logger.LogInformation("Found {ExistingFilterCount} existing filters for column {ColumnName}, " +
                    "will be replaced with new filter for operation {OperationId}",
                    existingFilters.Length, columnName, operationId);

                foreach (var existingFilter in existingFilters)
                {
                    _activeFilters.TryTake(out _);
                }
            }

            // Pridáme nový filter
            var filter = new FilterCriteria
            {
                ColumnName = columnName,
                Operator = @operator,
                Value = value
            };

            _activeFilters.Add(filter);

            _logger.LogInformation("Filter added to active filters. Current active filter count: {ActiveFilterCount} " +
                "for operation {OperationId}",
                _activeFilters.Count, operationId);

            // ✅ CRITICAL FIX: Apply filter criteria to IRowStore - builds filtered view index
            _logger.LogInformation("Applying filter criteria to IRowStore for operation {OperationId}", operationId);
            _rowStore.SetFilterCriteria(_activeFilters.ToArray());

            // Get filtered count from IRowStore (O(1) - uses cached filtered view index)
            var filteredCount = (int)await _rowStore.GetRowCountAsync(onlyFiltered: true, default);
            var totalRows = (int)await _rowStore.GetRowCountAsync(onlyFiltered: false, default);

            // Zalogujeme metriky filter operácie
            _operationLogger.LogFilterOperation(
                filterType: $"{@operator}",
                filterName: columnName,
                totalRows: totalRows,
                matchingRows: filteredCount,
                duration: stopwatch.Elapsed);

            _logger.LogInformation("Filter applied successfully in {Duration}ms for operation {OperationId}. " +
                "Visible rows: {FilteredCount}/{TotalRows}, Active filters: {ActiveFilterCount}",
                stopwatch.ElapsedMilliseconds, operationId, filteredCount, totalRows, _activeFilters.Count);

            // ✅ CRITICAL FIX: Trigger UI refresh event in Interactive mode
            if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
            {
                var eventArgs = new PublicDataRefreshEventArgs
                {
                    AffectedRows = filteredCount,
                    ColumnCount = 0,
                    OperationType = "ApplyFilter",
                    RefreshTime = DateTime.UtcNow,
                    RequiresFullReload = true // Filter changes entire view
                };

                _logger.LogInformation("Triggering UI refresh for filter operation {OperationId}", operationId);
                await _uiNotificationService.NotifyDataRefreshWithMetadataAsync(eventArgs);
            }

            scope.MarkSuccess(new
            {
                FilteredCount = filteredCount,
                TotalRows = totalRows,
                ColumnName = columnName,
                Operator = @operator,
                ActiveFilterCount = _activeFilters.Count,
                Duration = stopwatch.Elapsed
            });

            return filteredCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Filter application failed for operation {OperationId} on column {ColumnName}: {Message}",
                operationId, columnName, ex.Message);

            scope.MarkFailure(ex);
            throw;
        }
    }

    /// <summary>
    /// Vymaže všetky aktívne filtre
    /// Vracia celkový počet riadkov po vymazaní filtrov
    /// Loguje operáciu s operation scope a počtom vymazaných filtrov
    /// </summary>
    public async Task<int> ClearFiltersAsync()
    {
        var operationId = Guid.NewGuid();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Začíname clear filters operáciu - vytvoríme operation scope for automatické tracking
        using var scope = _operationLogger.LogOperationStart("ClearFiltersAsync", new
        {
            OperationId = operationId,
            CurrentFilterCount = _activeFilters.Count
        });

        _logger.LogInformation("Starting clearing of all filters for operation {OperationId}. " +
            "Current active filter count: {FilterCount}",
            operationId, _activeFilters.Count);

        try
        {
            var filterCount = _activeFilters.Count;

            // Vymažeme všetky filtre
            _logger.LogInformation("Clearing {FilterCount} active filters for operation {OperationId}",
                filterCount, operationId);

            while (_activeFilters.TryTake(out _)) { }

            // Získame celkový počet riadkov po vymazaní filtrov
            var totalRows = await _rowStore.GetRowCountAsync(default);

            _logger.LogInformation("Cleared {FilterCount} filters successfully in {Duration}ms for operation {OperationId}. " +
                "Total visible rows: {TotalRows}",
                filterCount, stopwatch.ElapsedMilliseconds, operationId, totalRows);

            scope.MarkSuccess(new
            {
                ClearedFilterCount = filterCount,
                TotalVisibleRows = totalRows,
                Duration = stopwatch.Elapsed
            });

            return (int)totalRows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clearing filters failed for operation {OperationId}: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            throw;
        }
    }

    /// <summary>
    /// Získa zoznam všetkých aktívnych filtrov
    /// Vracia immutable array kópiu for thread-safe čítanie
    /// </summary>
    public IReadOnlyList<FilterCriteria> GetActiveFilters()
    {
        return _activeFilters.ToArray();
    }

    /// <summary>
    /// Kontroluje či existujú nejaké aktívne filtre
    /// </summary>
    public bool HasActiveFilters()
    {
        return !_activeFilters.IsEmpty;
    }

    /// <summary>
    /// Aplikuje všetky aktívne filtre na dáta a vracia počet matchujúcich riadkov
    /// Thread-safe bez per-operation mutable fields
    /// Používa batch spracovanie for optimálny výkon
    /// </summary>
    private async Task<int> ApplyFiltersToDataAsync(Guid operationId)
    {
        var activeFilters = _activeFilters.ToArray();
        if (activeFilters.Length == 0)
        {
            var totalCount = await _rowStore.GetRowCountAsync(default);

            _logger.LogInformation("No active filters - returning total row count: {TotalCount} for operation {OperationId}",
                totalCount, operationId);

            return (int)totalCount;
        }

        _logger.LogInformation("Applying {FilterCount} filters to data for operation {OperationId}",
            activeFilters.Length, operationId);

        // Získame všetky dáta z row store
        var allData = await _rowStore.GetAllRowsAsync(default);
        var totalRows = allData.Count();
        var matchingRows = 0;

        _logger.LogInformation("Loaded {TotalRows} rows from store, starting batch filtering for operation {OperationId}",
            totalRows, operationId);

        // Spracujeme dáta v dávkach for lepší výkon
        var batchSize = _options.BatchSize;
        var batchCount = 0;

        for (int i = 0; i < totalRows; i += batchSize)
        {
            var batchEnd = Math.Min(i + batchSize, totalRows);
            var batch = allData.Skip(i).Take(batchEnd - i);
            batchCount++;

            foreach (var row in batch)
            {
                if (RowMatchesAllFilters(row, activeFilters))
                {
                    matchingRows++;
                }
            }
        }

        _logger.LogInformation("Filtering completed for operation {OperationId}. Processed {TotalRows} rows in {BatchCount} batches. " +
            "Matching rows: {MatchingRows} ({MatchPercentage:F2}%)",
            operationId, totalRows, batchCount, matchingRows, totalRows > 0 ? (matchingRows * 100.0 / totalRows) : 0);

        return matchingRows;
    }

    /// <summary>
    /// Kontroluje či riadok matchuje všetky aktívne filtre (AND logika)
    /// Vracia false ak aspoň jeden filter nezhoduje, inak true
    /// </summary>
    private bool RowMatchesAllFilters(IReadOnlyDictionary<string, object?> row, FilterCriteria[] filters)
    {
        foreach (var filter in filters)
        {
            if (!RowMatchesFilter(row, filter))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Kontroluje či riadok matchuje špecifický filter
    /// Komplexná business logika for všetky filter operátory
    /// Podporuje Equals, Contains, GreaterThan, IsNull a všetky ďalšie operátory
    /// </summary>
    private bool RowMatchesFilter(IReadOnlyDictionary<string, object?> row, FilterCriteria filter)
    {
        if (!row.TryGetValue(filter.ColumnName, out var cellValue))
        {
            // Column doesn't exist, treat as null
            cellValue = null;
        }

        switch (filter.Operator)
        {
            case FilterOperator.Equals:
                return ValuesAreEqual(cellValue, filter.Value);

            case FilterOperator.NotEquals:
                return !ValuesAreEqual(cellValue, filter.Value);

            case FilterOperator.Contains:
                return StringContains(cellValue, filter.Value);

            case FilterOperator.NotContains:
                return !StringContains(cellValue, filter.Value);

            case FilterOperator.StartsWith:
                return StringStartsWith(cellValue, filter.Value);

            case FilterOperator.EndsWith:
                return StringEndsWith(cellValue, filter.Value);

            case FilterOperator.GreaterThan:
                return CompareValues(cellValue, filter.Value) > 0;

            case FilterOperator.GreaterThanOrEqual:
                return CompareValues(cellValue, filter.Value) >= 0;

            case FilterOperator.LessThan:
                return CompareValues(cellValue, filter.Value) < 0;

            case FilterOperator.LessThanOrEqual:
                return CompareValues(cellValue, filter.Value) <= 0;

            case FilterOperator.IsNull:
                return cellValue == null;

            case FilterOperator.IsNotNull:
                return cellValue != null;

            case FilterOperator.IsEmpty:
                return IsValueEmpty(cellValue);

            case FilterOperator.IsNotEmpty:
                return !IsValueEmpty(cellValue);

            default:
                _logger.LogWarning("Unknown filter operator: {Operator}", filter.Operator);
                return false;
        }
    }

    /// <summary>
    /// Kontroluje či sú dve hodnoty rovnaké s type coercion
    /// Používa case-insensitive string porovnanie ak direct equality zlyhá
    /// </summary>
    private bool ValuesAreEqual(object? cellValue, object? filterValue)
    {
        if (cellValue == null && filterValue == null)
            return true;

        if (cellValue == null || filterValue == null)
            return false;

        // Direct equality check first
        if (cellValue.Equals(filterValue))
            return true;

        // String comparison (case-insensitive)
        var cellStr = cellValue.ToString();
        var filterStr = filterValue.ToString();

        return string.Equals(cellStr, filterStr, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kontroluje či cell hodnota obsahuje filter hodnotu (case-insensitive string search)
    /// </summary>
    private bool StringContains(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";

        return cellStr.Contains(filterStr, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kontroluje či cell hodnota začína filter hodnotou (case-insensitive)
    /// </summary>
    private bool StringStartsWith(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";

        return cellStr.StartsWith(filterStr, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kontroluje či cell hodnota končí filter hodnotou (case-insensitive)
    /// </summary>
    private bool StringEndsWith(object? cellValue, object? filterValue)
    {
        if (cellValue == null || filterValue == null)
            return false;

        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";

        return cellStr.EndsWith(filterStr, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Porovnáva dve hodnoty s type coercion
    /// Vracia: -1 ak cellValue < filterValue, 0 ak rovné, 1 ak cellValue > filterValue
    /// Pokúša sa numeric porovnanie, potom DateTime, nakoniec string fallback
    /// </summary>
    private int CompareValues(object? cellValue, object? filterValue)
    {
        if (cellValue == null && filterValue == null)
            return 0;

        if (cellValue == null)
            return -1;

        if (filterValue == null)
            return 1;

        // Try numeric comparison first
        if (TryGetNumericValue(cellValue, out var cellNumeric) &&
            TryGetNumericValue(filterValue, out var filterNumeric))
        {
            return cellNumeric.CompareTo(filterNumeric);
        }

        // Try DateTime comparison
        if (TryGetDateTimeValue(cellValue, out var cellDate) &&
            TryGetDateTimeValue(filterValue, out var filterDate))
        {
            return cellDate.CompareTo(filterDate);
        }

        // Fallback to string comparison
        var cellStr = cellValue.ToString() ?? "";
        var filterStr = filterValue.ToString() ?? "";

        return string.Compare(cellStr, filterStr, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Pokúša sa získať numerickú hodnotu z objektu
    /// Podporuje decimal, double, float, long, int, short, byte a string parsing
    /// </summary>
    private bool TryGetNumericValue(object value, out decimal numericValue)
    {
        numericValue = 0;

        if (value is decimal d)
        {
            numericValue = d;
            return true;
        }
        if (value is double dbl)
        {
            numericValue = (decimal)dbl;
            return true;
        }
        if (value is float f)
        {
            numericValue = (decimal)f;
            return true;
        }
        if (value is long l)
        {
            numericValue = l;
            return true;
        }
        if (value is int i)
        {
            numericValue = i;
            return true;
        }
        if (value is short s)
        {
            numericValue = s;
            return true;
        }
        if (value is byte b)
        {
            numericValue = b;
            return true;
        }
        if (value is string str)
        {
            return decimal.TryParse(str, out numericValue);
        }

        return false;
    }

    /// <summary>
    /// Pokúša sa získať DateTime hodnotu z objektu
    /// Podporuje DateTime, DateTimeOffset a string parsing
    /// </summary>
    private bool TryGetDateTimeValue(object value, out DateTime dateTimeValue)
    {
        dateTimeValue = default;

        if (value is DateTime dt)
        {
            dateTimeValue = dt;
            return true;
        }
        if (value is DateTimeOffset dto)
        {
            dateTimeValue = dto.DateTime;
            return true;
        }
        if (value is string str)
        {
            return DateTime.TryParse(str, out dateTimeValue);
        }

        return false;
    }

    /// <summary>
    /// Kontroluje či je hodnota považovaná za prázdnu
    /// Prázdna je null, whitespace string or prázdna collection
    /// </summary>
    private bool IsValueEmpty(object? value)
    {
        if (value == null)
            return true;
        if (value is string s)
            return string.IsNullOrWhiteSpace(s);
        if (value is System.Collections.ICollection c)
            return c.Count == 0;

        return false;
    }

    /// <summary>
    /// Získa filtrované dáta na základe aktívnych filtrov
    /// Používané Export službou for onlyFiltered funkcionalitu
    /// Vracia iba riadky ktoré matchujú všetky aktívne filtre
    /// </summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetFilteredDataAsync()
    {
        var activeFilters = _activeFilters.ToArray();
        var allData = await _rowStore.GetAllRowsAsync(default);

        if (activeFilters.Length == 0)
        {
            return allData.ToList();
        }
        var filteredData = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var row in allData)
        {
            if (RowMatchesAllFilters(row, activeFilters))
            {
                filteredData.Add(row);
            }
        }

        return filteredData;
    }

    #region Wrapper Methods for Public API

    public async Task<Common.Models.Result> ApplyColumnFilterAsync(string columnName, FilterOperator @operator, object? value, CancellationToken cancellationToken = default)
    {
        try
        {
            await ApplyFilterAsync(columnName, @operator, value);
            return Common.Models.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apply column filter failed: {Message}", ex.Message);
            return Common.Models.Result.Failure($"Apply filter failed: {ex.Message}");
        }
    }

    public async Task<Common.Models.Result> ApplyMultipleFiltersAsync(IEnumerable<FilterCriteria> filters, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var filter in filters)
            {
                await ApplyFilterAsync(filter.ColumnName, filter.Operator, filter.Value);
            }
            return Common.Models.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apply multiple filters failed: {Message}", ex.Message);
            return Common.Models.Result.Failure($"Apply multiple filters failed: {ex.Message}");
        }
    }

    public async Task<Common.Models.Result> RemoveColumnFilterAsync(string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run(() =>
            {
                var filtersToRemove = _activeFilters
                    .Where(f => f.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var filter in filtersToRemove)
                {
                    _activeFilters.TryTake(out _);
                }

                // Rebuild the bag without the removed items
                var remainingFilters = _activeFilters
                    .Where(f => !f.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                _activeFilters.Clear();
                foreach (var filter in remainingFilters)
                {
                    _activeFilters.Add(filter);
                }

                _logger.LogInformation("Removed filter for column {ColumnName}", columnName);
            }, cancellationToken);
            return Common.Models.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove column filter failed: {Message}", ex.Message);
            return Common.Models.Result.Failure($"Remove filter failed: {ex.Message}");
        }
    }

    public async Task<Common.Models.Result> ClearAllFiltersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await ClearFiltersAsync();
            return Common.Models.Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear all filters failed: {Message}", ex.Message);
            return Common.Models.Result.Failure($"Clear filters failed: {ex.Message}");
        }
    }

    public IReadOnlyList<FilterCriteria> GetCurrentFilters()
    {
        return GetActiveFilters();
    }

    public bool IsColumnFiltered(string columnName)
    {
        return _activeFilters.Any(f => f.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    }

    public int GetFilterCount()
    {
        return _activeFilters.Count;
    }

    public async Task<long> GetFilteredRowCountAsync(CancellationToken cancellationToken = default)
    {
        var filteredData = await GetFilteredDataAsync();
        return filteredData.Count;
    }

    #endregion
}
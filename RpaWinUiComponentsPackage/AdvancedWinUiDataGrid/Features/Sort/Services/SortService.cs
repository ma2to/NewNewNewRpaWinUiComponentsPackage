using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Utilities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Services;

/// <summary>
/// Interná implementácia sort služby s LINQ optimalizáciami
/// Thread-safe s podporou parallel processing
/// </summary>
internal sealed class SortService : ISortService
{
    private const int ParallelProcessingThreshold = 1000;
    private readonly ILogger<SortService> _logger;
    private readonly IOperationLogger<SortService> _operationLogger;
    private readonly Infrastructure.Persistence.Interfaces.IRowStore? _rowStore;
    private List<(string ColumnName, CoreTypes.SortDirection Direction)> _currentSort = new();

    public SortService(
        ILogger<SortService> logger,
        Infrastructure.Persistence.Interfaces.IRowStore? rowStore = null,
        IOperationLogger<SortService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore;
        _operationLogger = operationLogger ?? NullOperationLogger<SortService>.Instance;
    }

    /// <summary>
    /// Vykoná jednokolónkové triedenie s LINQ optimization
    /// </summary>
    public async Task<SortResult> SortAsync(SortCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("SortAsync", new
        {
            OperationId = operationId,
            ColumnName = command.ColumnName,
            Direction = command.Direction,
            PerformanceMode = command.PerformanceMode
        });

        _logger.LogInformation("Starting sort operation {OperationId}: column={ColumnName}, direction={Direction}",
            operationId, command.ColumnName, command.Direction);

        try
        {
            // Validácia sortability
            var isSortable = SortAlgorithms.IsColumnSortable(command.Data, command.ColumnName);
            if (!isSortable)
            {
                var error = $"Column '{command.ColumnName}' is not sortable";
                _logger.LogWarning("Sort validation failed for operation {OperationId}: {Error}",
                    operationId, error);
                scope.MarkFailure(new InvalidOperationException(error));
                return SortResult.CreateFailure(new[] { error }, stopwatch.Elapsed);
            }

            // Konverzia na list pre performance
            var dataList = command.Data.ToList();
            _logger.LogInformation("Processing {RowCount} rows for sort operation {OperationId}",
                dataList.Count, operationId);

            // Type detection pre optimalizáciu
            var detectedType = SortAlgorithms.DetectSortDataType(dataList, command.ColumnName);
            _logger.LogInformation("Detected sort type: {DetectedType} for column {ColumnName}",
                detectedType?.Name ?? "Unknown", command.ColumnName);

            // Výber sort stratégie
            var useParallel = command.EnableParallelProcessing && dataList.Count > ParallelProcessingThreshold;

            // LINQ sort execution
            IEnumerable<IReadOnlyDictionary<string, object?>> sortedData;

            if (useParallel)
            {
                _logger.LogInformation("Using parallel sort for {RowCount} rows", dataList.Count);
                sortedData = command.Direction == CoreTypes.SortDirection.Ascending
                    ? dataList.AsParallel()
                        .WithCancellation(cancellationToken)
                        .OrderBy(row => SortAlgorithms.GetSortValue(row, command.ColumnName))
                    : dataList.AsParallel()
                        .WithCancellation(cancellationToken)
                        .OrderByDescending(row => SortAlgorithms.GetSortValue(row, command.ColumnName));
            }
            else
            {
                _logger.LogInformation("Using sequential sort for {RowCount} rows", dataList.Count);
                sortedData = command.Direction == CoreTypes.SortDirection.Ascending
                    ? dataList.OrderBy(row => SortAlgorithms.GetSortValue(row, command.ColumnName))
                    : dataList.OrderByDescending(row => SortAlgorithms.GetSortValue(row, command.ColumnName));
            }

            var resultList = sortedData.ToList();
            stopwatch.Stop();

            _logger.LogInformation("Sort operation {OperationId} completed in {Duration}ms: sorted {RowCount} rows",
                operationId, stopwatch.ElapsedMilliseconds, resultList.Count);

            scope.MarkSuccess(new
            {
                SortedRows = resultList.Count,
                Duration = stopwatch.Elapsed,
                UsedParallel = useParallel
            });

            return SortResult.CreateSuccess(
                resultList,
                new[] { CoreTypes.SortColumnConfiguration.Create(command.ColumnName, command.Direction) },
                stopwatch.Elapsed,
                command.PerformanceMode,
                usedParallel: useParallel);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Sort operation {OperationId} was cancelled", operationId);
            scope.MarkFailure(new OperationCanceledException("Sort cancelled"));
            return SortResult.CreateFailure(new[] { "Sort operation was cancelled" }, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sort operation {OperationId} failed: {Message}",
                operationId, ex.Message);
            scope.MarkFailure(ex);
            return SortResult.CreateFailure(new[] { $"Sort failed: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Vykoná multi-column triedenie s ThenBy chains
    /// </summary>
    public async Task<SortResult> MultiSortAsync(MultiSortCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("MultiSortAsync", new
        {
            OperationId = operationId,
            ColumnCount = command.SortColumns.Count,
            PerformanceMode = command.PerformanceMode
        });

        _logger.LogInformation("Starting multi-column sort operation {OperationId}: {ColumnCount} columns",
            operationId, command.SortColumns.Count);

        try
        {
            var dataList = command.Data.ToList();
            var enabledSorts = command.SortColumns
                .Where(s => s.IsEnabled && s.Direction != CoreTypes.SortDirection.None)
                .OrderBy(s => s.Priority)
                .ToList();

            if (!enabledSorts.Any())
            {
                _logger.LogWarning("No enabled sort columns for operation {OperationId}", operationId);
                return SortResult.CreateSuccess(dataList, Array.Empty<CoreTypes.SortColumnConfiguration>(), stopwatch.Elapsed);
            }

            _logger.LogInformation("Processing {RowCount} rows with {SortCount} sort criteria for operation {OperationId}",
                dataList.Count, enabledSorts.Count, operationId);

            // Build LINQ OrderBy -> ThenBy chain
            IOrderedEnumerable<IReadOnlyDictionary<string, object?>> orderedData = null!;
            var useParallel = command.EnableParallelProcessing && dataList.Count > ParallelProcessingThreshold;

            foreach (var (sortConfig, index) in enabledSorts.Select((s, i) => (s, i)))
            {
                if (index == 0)
                {
                    // Primary sort
                    if (useParallel)
                    {
                        var parallelResult = sortConfig.Direction == CoreTypes.SortDirection.Ascending
                            ? dataList.AsParallel()
                                .WithCancellation(cancellationToken)
                                .OrderBy(row => SortAlgorithms.GetSortValue(row, sortConfig.ColumnName))
                            : dataList.AsParallel()
                                .WithCancellation(cancellationToken)
                                .OrderByDescending(row => SortAlgorithms.GetSortValue(row, sortConfig.ColumnName));

                        // Convert to sequential for ThenBy chains
                        orderedData = parallelResult.AsSequential().OrderBy(r => SortAlgorithms.GetSortValue(r, sortConfig.ColumnName));
                    }
                    else
                    {
                        orderedData = sortConfig.Direction == CoreTypes.SortDirection.Ascending
                            ? dataList.OrderBy(row => SortAlgorithms.GetSortValue(row, sortConfig.ColumnName))
                            : dataList.OrderByDescending(row => SortAlgorithms.GetSortValue(row, sortConfig.ColumnName));
                    }
                }
                else
                {
                    // Secondary sorts (ThenBy chain)
                    orderedData = sortConfig.Direction == CoreTypes.SortDirection.Ascending
                        ? orderedData.ThenBy(row => SortAlgorithms.GetSortValue(row, sortConfig.ColumnName))
                        : orderedData.ThenByDescending(row => SortAlgorithms.GetSortValue(row, sortConfig.ColumnName));
                }
            }

            var resultList = orderedData?.ToList() ?? dataList;
            stopwatch.Stop();

            _logger.LogInformation("Multi-column sort operation {OperationId} completed in {Duration}ms: sorted {RowCount} rows with {ColumnCount} columns",
                operationId, stopwatch.ElapsedMilliseconds, resultList.Count, enabledSorts.Count);

            scope.MarkSuccess(new
            {
                SortedRows = resultList.Count,
                Duration = stopwatch.Elapsed,
                ColumnCount = enabledSorts.Count,
                UsedParallel = useParallel
            });

            return SortResult.CreateSuccess(
                resultList,
                enabledSorts,
                stopwatch.Elapsed,
                command.PerformanceMode,
                usedParallel: useParallel,
                usedStable: command.Stability == CoreTypes.SortStability.Stable);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Multi-column sort operation {OperationId} was cancelled", operationId);
            scope.MarkFailure(new OperationCanceledException("Multi-sort cancelled"));
            return SortResult.CreateFailure(new[] { "Multi-sort operation was cancelled" }, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Multi-column sort operation {OperationId} failed: {Message}",
                operationId, ex.Message);
            scope.MarkFailure(ex);
            return SortResult.CreateFailure(new[] { $"Multi-sort failed: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Získa zoznam sortovateľných stĺpcov
    /// </summary>
    public IReadOnlyList<string> GetSortableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data)
    {
        try
        {
            var firstRow = data.FirstOrDefault();
            if (firstRow == null)
                return Array.Empty<string>();

            var sortableColumns = firstRow.Keys
                .Where(columnName => SortAlgorithms.IsColumnSortable(data, columnName))
                .ToList();

            _logger.LogInformation("Discovered {ColumnCount} sortable columns", sortableColumns.Count);
            return sortableColumns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sortable columns: {Message}", ex.Message);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Odporúči optimálny performance mode na základe charakteristík dát
    /// </summary>
    public CoreTypes.SortPerformanceMode GetRecommendedPerformanceMode(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<CoreTypes.SortColumnConfiguration> sortColumns)
    {
        try
        {
            var dataList = data as IReadOnlyList<IReadOnlyDictionary<string, object?>> ?? data.ToList();
            var rowCount = dataList.Count;
            var columnCount = sortColumns.Count;

            _logger.LogInformation("Recommending performance mode for {RowCount} rows and {ColumnCount} columns",
                rowCount, columnCount);

            // Auto selection based on data size
            var recommendedMode = rowCount switch
            {
                < 100 => CoreTypes.SortPerformanceMode.Sequential,
                < ParallelProcessingThreshold => CoreTypes.SortPerformanceMode.Auto,
                _ => CoreTypes.SortPerformanceMode.Parallel
            };

            _logger.LogInformation("Recommended performance mode: {Mode}", recommendedMode);
            return recommendedMode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recommend performance mode: {Message}", ex.Message);
            return CoreTypes.SortPerformanceMode.Auto;
        }
    }

    // LEGACY API IMPLEMENTATION (backward compatibility)

    /// <summary>
    /// Legacy sort by column - používa row store
    /// </summary>
    public async Task<bool> SortByColumnAsync(string columnName, CoreTypes.SortDirection direction, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_rowStore == null)
            {
                _logger.LogWarning("RowStore not available for legacy sort");
                return false;
            }

            _logger.LogInformation("Legacy sort: column={ColumnName}, direction={Direction}", columnName, direction);

            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
            var sortedRows = direction == CoreTypes.SortDirection.Ascending
                ? allRows.OrderBy(r => SortAlgorithms.GetSortValue(r, columnName)).ToList()
                : allRows.OrderByDescending(r => SortAlgorithms.GetSortValue(r, columnName)).ToList();

            await _rowStore.ReplaceAllRowsAsync(sortedRows, cancellationToken);
            _currentSort = new() { (columnName, direction) };

            _logger.LogInformation("Legacy sort completed: {RowCount} rows", sortedRows.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Legacy sort failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Vyčistí aktuálne sort nastavenia
    /// </summary>
    public async Task<bool> ClearSortAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing sort settings");
        _currentSort.Clear();
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Získa aktuálne sort nastavenia
    /// </summary>
    public IReadOnlyList<(string ColumnName, CoreTypes.SortDirection Direction)> GetCurrentSort() => _currentSort.AsReadOnly();

    /// <summary>
    /// Indikátor či sú dáta sortované
    /// </summary>
    public bool IsSorted() => _currentSort.Any();

    /// <summary>
    /// Vykoná advanced sort s business rules a custom sort keys
    /// </summary>
    public async Task<SortResult> AdvancedSortAsync(AdvancedSortCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("AdvancedSortAsync", new
        {
            OperationId = operationId,
            ConfigurationName = command.SortConfiguration.ConfigurationName,
            ColumnCount = command.SortConfiguration.SortColumns.Count
        });

        _logger.LogInformation("Starting advanced sort operation {OperationId}: config={ConfigName}, columns={ColumnCount}",
            operationId, command.SortConfiguration.ConfigurationName, command.SortConfiguration.SortColumns.Count);

        try
        {
            var dataList = command.Data.ToList();
            var config = command.SortConfiguration;

            // Validácia konfigurácie
            if (!config.SortColumns.Any())
            {
                _logger.LogWarning("No sort columns in advanced configuration for operation {OperationId}", operationId);
                return SortResult.CreateSuccess(dataList, Array.Empty<CoreTypes.SortColumnConfiguration>(), stopwatch.Elapsed);
            }

            // Ak je custom sort key, použijeme ho
            if (config.CustomSortKey != null && command.Context != null)
            {
                _logger.LogInformation("Using custom sort key for operation {OperationId}", operationId);

                var context = command.Context with { CancellationToken = cancellationToken };
                var keyedData = dataList.Select(row => new
                {
                    Row = row,
                    SortKey = config.CustomSortKey(row, context)
                }).ToList();

                var sortedKeyed = keyedData.OrderBy(x => x.SortKey).ToList();
                var resultList = sortedKeyed.Select(x => x.Row).ToList();

                stopwatch.Stop();
                _logger.LogInformation("Advanced sort with custom key completed in {Duration}ms for {RowCount} rows",
                    stopwatch.ElapsedMilliseconds, resultList.Count);

                scope.MarkSuccess(new { Duration = stopwatch.Elapsed, RowCount = resultList.Count });
                return SortResult.CreateSuccess(resultList, config.SortColumns.ToList(), stopwatch.Elapsed,
                    config.PerformanceMode, config.EnableParallelProcessing, config.Stability == CoreTypes.SortStability.Stable);
            }

            // Použijeme MultiSortAsync pre štandardný advanced sort
            var multiSortCommand = MultiSortCommand.Create(dataList, config.SortColumns);
            multiSortCommand = multiSortCommand with
            {
                EnableParallelProcessing = config.EnableParallelProcessing,
                PerformanceMode = config.PerformanceMode,
                Stability = config.Stability,
                CancellationToken = cancellationToken
            };

            var result = await MultiSortAsync(multiSortCommand, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("Advanced sort operation {OperationId} completed in {Duration}ms",
                operationId, stopwatch.ElapsedMilliseconds);

            scope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.Success });
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Advanced sort operation {OperationId} was cancelled", operationId);
            scope.MarkFailure(new OperationCanceledException("Advanced sort cancelled"));
            return SortResult.CreateFailure(new[] { "Advanced sort operation was cancelled" }, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Advanced sort operation {OperationId} failed: {Message}",
                operationId, ex.Message);
            scope.MarkFailure(ex);
            return SortResult.CreateFailure(new[] { $"Advanced sort failed: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Quick sort pre okamžité výsledky (synchronous)
    /// </summary>
    public SortResult QuickSort(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName, CoreTypes.SortDirection direction = CoreTypes.SortDirection.Ascending)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var dataList = data.ToList();
            _logger.LogInformation("QuickSort: column={ColumnName}, direction={Direction}, rows={RowCount}",
                columnName, direction, dataList.Count);

            // Validácia sortability
            var isSortable = SortAlgorithms.IsColumnSortable(dataList, columnName);
            if (!isSortable)
            {
                var error = $"Column '{columnName}' is not sortable";
                _logger.LogWarning("QuickSort validation failed: {Error}", error);
                return SortResult.CreateFailure(new[] { error }, stopwatch.Elapsed);
            }

            // Jednoduché synchronné triedenie
            var sortedData = direction == CoreTypes.SortDirection.Ascending
                ? dataList.OrderBy(row => SortAlgorithms.GetSortValue(row, columnName)).ToList()
                : dataList.OrderByDescending(row => SortAlgorithms.GetSortValue(row, columnName)).ToList();

            stopwatch.Stop();
            _logger.LogInformation("QuickSort completed in {Duration}ms for {RowCount} rows",
                stopwatch.ElapsedMilliseconds, sortedData.Count);

            return SortResult.CreateSuccess(
                sortedData,
                new[] { CoreTypes.SortColumnConfiguration.Create(columnName, direction) },
                stopwatch.Elapsed,
                CoreTypes.SortPerformanceMode.Sequential,
                usedParallel: false,
                usedStable: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickSort failed: {Message}", ex.Message);
            return SortResult.CreateFailure(new[] { $"QuickSort failed: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Validuje sort konfiguráciu
    /// </summary>
    public async Task<Common.Models.Result> ValidateSortConfigurationAsync(CoreTypes.AdvancedSortConfiguration sortConfiguration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating sort configuration: {ConfigName}", sortConfiguration.ConfigurationName);

            // Kontrola sort columns
            if (sortConfiguration.SortColumns == null || !sortConfiguration.SortColumns.Any())
            {
                return Common.Models.Result.Failure("Sort configuration must contain at least one sort column");
            }

            // Kontrola max sort columns
            if (sortConfiguration.SortColumns.Count > sortConfiguration.MaxSortColumns)
            {
                return Common.Models.Result.Failure($"Sort configuration contains {sortConfiguration.SortColumns.Count} columns, but maximum is {sortConfiguration.MaxSortColumns}");
            }

            // Kontrola duplicitných priorít
            var duplicatePriorities = sortConfiguration.SortColumns
                .GroupBy(s => s.Priority)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatePriorities.Any())
            {
                return Common.Models.Result.Failure($"Duplicate sort priorities found: {string.Join(", ", duplicatePriorities)}");
            }

            // Kontrola column names
            var emptyColumnNames = sortConfiguration.SortColumns
                .Where(s => string.IsNullOrWhiteSpace(s.ColumnName))
                .ToList();

            if (emptyColumnNames.Any())
            {
                return Common.Models.Result.Failure("Some sort columns have empty column names");
            }

            _logger.LogInformation("Sort configuration validation passed for {ConfigName}", sortConfiguration.ConfigurationName);
            return await Task.FromResult(Common.Models.Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sort configuration validation failed: {Message}", ex.Message);
            return Common.Models.Result.Failure($"Validation error: {ex.Message}");
        }
    }
}

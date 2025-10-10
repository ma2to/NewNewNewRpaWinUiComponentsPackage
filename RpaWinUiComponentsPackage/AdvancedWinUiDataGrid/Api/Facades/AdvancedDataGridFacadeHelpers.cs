using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Partial class containing Private Helper Methods + Helper Classes + Placeholder Implementations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Private Helper Methods

    /// <summary>
    /// Centralized UI refresh logic - triggers automatic UI refresh ONLY in Interactive mode
    /// </summary>
    /// <param name="operationType">Type of operation that triggered refresh</param>
    /// <param name="affectedRows">Number of affected rows</param>
    private async Task TriggerUIRefreshIfNeededAsync(string operationType, int affectedRows)
    {
        // Automatický refresh LEN v Interactive mode
        if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
        {
            await _uiNotificationService.NotifyDataRefreshAsync(affectedRows, operationType);
        }
        // V Headless mode → skip (automatický refresh je zakázaný)
    }

    /// <summary>
    /// Helper class for notification subscriptions
    /// </summary>
    private sealed class NotificationSubscription : IDisposable
    {
        private readonly Action _unsubscribeAction;
        private bool _disposed;

        public NotificationSubscription(Action unsubscribeAction)
        {
            _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _unsubscribeAction();
            _disposed = true;
        }
    }

    private static long EstimateDataTableSize(DataTable dataTable)
    {
        // Rough estimation of DataTable size in bytes
        return dataTable.Rows.Count * dataTable.Columns.Count * 50L;
    }

    private static long EstimateDictionarySize(IReadOnlyList<IReadOnlyDictionary<string, object?>> dictionaries)
    {
        // Rough estimation of dictionary collection size in bytes
        var avgColumns = dictionaries.FirstOrDefault()?.Count ?? 0;
        return dictionaries.Count * avgColumns * 30L;
    }

    #endregion

    #region Placeholder Implementations

    // These methods would need actual implementations based on specific requirements
    // For now, providing basic placeholder implementations

    public async Task<int> ApplyFilterAsync(string columnName, PublicFilterOperator @operator, object? value)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Filter, nameof(ApplyFilterAsync));

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var filterService = scope.ServiceProvider.GetRequiredService<IFilterService>();
            var internalOperator = @operator.ToInternal();
            return await filterService.ApplyFilterAsync(columnName, internalOperator, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply filter to column {ColumnName}", columnName);
            return 0;
        }
    }

    public async Task<int> ClearFiltersAsync()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var filterService = scope.ServiceProvider.GetRequiredService<IFilterService>();
            return await filterService.ClearFiltersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear filters");
            return 0;
        }
    }

    /// <summary>
    /// Clears the current filter (singular - alias for ClearFiltersAsync)
    /// </summary>
    public async Task ClearFilterAsync()
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Filter, nameof(ClearFilterAsync));

        try
        {
            var filterService = _serviceProvider.GetRequiredService<IFilterService>();
            await filterService.ClearFiltersAsync();
            await TriggerUIRefreshIfNeededAsync("ClearFilter", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear filter");
            throw;
        }
    }

    /// <summary>
    /// Sorts data by single column using command pattern
    /// </summary>
    public async Task<SortDataResult> SortAsync(SortDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Sort, nameof(SortAsync));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = command.CorrelationId ?? Guid.NewGuid();

        using var logScope = _operationLogger.LogOperationStart("SortAsync", new
        {
            OperationId = operationId,
            ColumnName = command.ColumnName,
            Direction = command.Direction
        });

        _logger.LogInformation("Starting sort operation {OperationId}: column={ColumnName}, direction={Direction}",
            operationId, command.ColumnName, command.Direction);

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            // Map public command to internal
            var internalCommand = command.ToInternal();

            // Execute sort
            var internalResult = await sortService.SortAsync(internalCommand, cancellationToken);

            // Map result to public
            var result = internalResult.ToPublic();

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Sort operation {OperationId} completed in {Duration}ms",
                operationId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sort operation {OperationId} failed", operationId);
            logScope.MarkFailure(ex);
            return new SortDataResult(false, Array.Empty<IReadOnlyDictionary<string, object?>>(), 0, stopwatch.Elapsed, false, new[] { $"Sort failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Sorts data by multiple columns using command pattern
    /// </summary>
    public async Task<SortDataResult> MultiSortAsync(MultiSortDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Sort, nameof(MultiSortAsync));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = command.CorrelationId ?? Guid.NewGuid();

        using var logScope = _operationLogger.LogOperationStart("MultiSortAsync", new
        {
            OperationId = operationId,
            ColumnCount = command.SortColumns.Count
        });

        _logger.LogInformation("Starting multi-sort operation {OperationId}: {ColumnCount} columns",
            operationId, command.SortColumns.Count);

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            // Map public command to internal
            var internalCommand = command.ToInternal();

            // Execute multi-sort
            var internalResult = await sortService.MultiSortAsync(internalCommand, cancellationToken);

            // Map result to public
            var result = internalResult.ToPublic();

            logScope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.IsSuccess });
            _logger.LogInformation("Multi-sort operation {OperationId} completed in {Duration}ms",
                operationId, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Multi-sort operation {OperationId} failed", operationId);
            logScope.MarkFailure(ex);
            return new SortDataResult(false, Array.Empty<IReadOnlyDictionary<string, object?>>(), 0, stopwatch.Elapsed, false, new[] { $"Multi-sort failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Quick synchronous sort for immediate results
    /// </summary>
    public SortDataResult QuickSort(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName, PublicSortDirection direction = PublicSortDirection.Ascending)
    {
        ThrowIfDisposed();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            // Map public direction to internal
            var internalDirection = direction.ToInternal();

            // Execute quick sort
            var internalResult = sortService.QuickSort(data, columnName, internalDirection);

            // Map result to public
            var result = internalResult.ToPublic();

            _logger.LogInformation("QuickSort completed in {Duration}ms for column {ColumnName}",
                stopwatch.ElapsedMilliseconds, columnName);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuickSort failed for column {ColumnName}", columnName);
            return new SortDataResult(false, Array.Empty<IReadOnlyDictionary<string, object?>>(), 0, stopwatch.Elapsed, false, new[] { $"QuickSort failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Gets list of sortable columns
    /// </summary>
    public IReadOnlyList<string> GetSortableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            return sortService.GetSortableColumns(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sortable columns");
            return Array.Empty<string>();
        }
    }

    public async Task<PublicResult> SortByColumnAsync(string columnName, PublicSortDirection direction)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            // Map public direction to internal
            var internalDirection = direction.ToInternal();
            var success = await sortService.SortByColumnAsync(columnName, (Core.ValueObjects.SortDirection)internalDirection, CancellationToken.None);
            return success ? PublicResult.Success() : PublicResult.Failure("Sort failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Legacy sort by column failed");
            return PublicResult.Failure($"Sort failed: {ex.Message}");
        }
    }

    public async Task<PublicResult> ClearSortingAsync()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var sortService = scope.ServiceProvider.GetRequiredService<Features.Sort.Interfaces.ISortService>();

            var success = await sortService.ClearSortAsync();
            return success ? PublicResult.Success() : PublicResult.Failure("Clear sort failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear sorting failed");
            return PublicResult.Failure($"Clear sort failed: {ex.Message}");
        }
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Helper class for accumulating rule statistics during validation
    /// </summary>
    private class RuleStatsAccumulator
    {
        public string RuleName { get; set; } = string.Empty;
        public int ExecutionCount { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public int ErrorsFound { get; set; }
    }

    #endregion
}

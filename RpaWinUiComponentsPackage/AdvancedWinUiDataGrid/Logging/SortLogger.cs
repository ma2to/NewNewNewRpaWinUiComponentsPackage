using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Logging;

/// <summary>
/// Internal specialized logger for sort operations
/// Provides detailed logging and performance tracking for sorting scenarios
/// </summary>
internal sealed class SortLogger
{
    private readonly ILogger<SortLogger> _logger;

    public SortLogger(ILogger<SortLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log sort operation start
    /// </summary>
    public void LogSortStart(Guid operationId, string columnName, string direction, int totalRows, string sortMode)
    {
        _logger.LogInformation("Sort operation started [{OperationId}]: Column={ColumnName}, Direction={Direction}, TotalRows={TotalRows}, Mode={SortMode}",
            operationId, columnName, direction, totalRows, sortMode);
    }

    /// <summary>
    /// Log sort completion
    /// </summary>
    public void LogSortCompletion(Guid operationId, bool success, int sortedRows, TimeSpan duration)
    {
        if (success)
        {
            _logger.LogInformation("Sort operation completed successfully [{OperationId}]: SortedRows={SortedRows}, Duration={Duration}ms",
                operationId, sortedRows, duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogError("Sort operation failed [{OperationId}]: Duration={Duration}ms",
                operationId, duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Log sort performance metrics
    /// </summary>
    public void LogPerformanceMetrics(Guid operationId, double rowsPerSecond, int comparisonCount, long memoryUsed)
    {
        _logger.LogInformation("Sort performance [{OperationId}]: Rate={RowsPerSecond:F2} rows/sec, Comparisons={ComparisonCount}, Memory={MemoryUsed:N0} bytes",
            operationId, rowsPerSecond, comparisonCount, memoryUsed);
    }

    /// <summary>
    /// Log critical sort error
    /// </summary>
    public void LogCriticalError(Guid operationId, Exception exception, string context)
    {
        _logger.LogCritical(exception, "Critical sort error [{OperationId}]: Context={Context}",
            operationId, context);
    }
}

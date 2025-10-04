using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Logging;

/// <summary>
/// Internal specialized logger for filter operations
/// Provides detailed logging and performance tracking for filtering scenarios
/// </summary>
internal sealed class FilterLogger
{
    private readonly ILogger<FilterLogger> _logger;

    public FilterLogger(ILogger<FilterLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log filter operation start
    /// </summary>
    public void LogFilterStart(Guid operationId, string filterExpression, int totalRows, int filterCount)
    {
        _logger.LogInformation("Filter operation started [{OperationId}]: Expression={FilterExpression}, TotalRows={TotalRows}, FilterCount={FilterCount}",
            operationId, filterExpression, totalRows, filterCount);
    }

    /// <summary>
    /// Log filter results
    /// </summary>
    public void LogFilterResults(Guid operationId, int filteredRows, int totalRows, TimeSpan duration)
    {
        var filterPercentage = totalRows > 0 ? (double)filteredRows / totalRows * 100 : 0;
        _logger.LogInformation("Filter completed [{OperationId}]: Filtered={FilteredRows}/{TotalRows} ({FilterPercentage:F1}%), Duration={Duration}ms",
            operationId, filteredRows, totalRows, filterPercentage, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Log filter performance metrics
    /// </summary>
    public void LogPerformanceMetrics(Guid operationId, double rowsPerSecond, long memoryUsed)
    {
        _logger.LogInformation("Filter performance [{OperationId}]: Rate={RowsPerSecond:F2} rows/sec, Memory={MemoryUsed:N0} bytes",
            operationId, rowsPerSecond, memoryUsed);
    }

    /// <summary>
    /// Log critical filter error
    /// </summary>
    public void LogCriticalError(Guid operationId, Exception exception, string context)
    {
        _logger.LogCritical(exception, "Critical filter error [{OperationId}]: Context={Context}",
            operationId, context);
    }
}

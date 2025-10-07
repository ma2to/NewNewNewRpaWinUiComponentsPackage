using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Performance mode for sort operations
/// </summary>
public enum PublicSortPerformanceMode
{
    /// <summary>Automatic selection based on data size</summary>
    Auto,
    /// <summary>Single-threaded sorting</summary>
    Sequential,
    /// <summary>Multi-threaded parallel sorting</summary>
    Parallel,
    /// <summary>Advanced optimizations with caching</summary>
    Optimized
}

/// <summary>
/// Progress information for sort operations
/// </summary>
/// <param name="ProcessedRows">Number of processed rows</param>
/// <param name="TotalRows">Total number of rows to sort</param>
/// <param name="ElapsedTime">Time elapsed since operation start</param>
/// <param name="CurrentOperation">Description of current operation</param>
/// <param name="CurrentColumn">Column currently being sorted</param>
public record SortProgress(
    int ProcessedRows,
    int TotalRows,
    TimeSpan ElapsedTime,
    string CurrentOperation = "",
    string? CurrentColumn = null
)
{
    /// <summary>Calculated completion percentage (0-100)</summary>
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;

    /// <summary>Estimated time remaining based on current progress</summary>
    public TimeSpan? EstimatedTimeRemaining => ProcessedRows > 0 && TotalRows > ProcessedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ProcessedRows) / ProcessedRows)
        : null;

    public SortProgress() : this(0, 0, TimeSpan.Zero, "", null) { }
}

/// <summary>
/// Command for single-column sort operation
/// </summary>
/// <param name="Data">Data to sort</param>
/// <param name="ColumnName">Column name to sort by</param>
/// <param name="Direction">Sort direction (Ascending/Descending)</param>
/// <param name="CaseSensitive">Case-sensitive string comparison</param>
/// <param name="PerformanceMode">Performance optimization mode</param>
/// <param name="Timeout">Operation timeout</param>
/// <param name="Progress">Progress reporting callback</param>
/// <param name="CorrelationId">Operation correlation ID for logging</param>
public record SortDataCommand(
    IEnumerable<IReadOnlyDictionary<string, object?>> Data,
    string ColumnName,
    PublicSortDirection Direction = PublicSortDirection.Ascending,
    bool CaseSensitive = false,
    PublicSortPerformanceMode PerformanceMode = PublicSortPerformanceMode.Auto,
    TimeSpan? Timeout = null,
    IProgress<SortProgress>? Progress = null,
    Guid? CorrelationId = null
)
{
    public SortDataCommand() : this(Array.Empty<IReadOnlyDictionary<string, object?>>(), "", PublicSortDirection.Ascending) { }
}

/// <summary>
/// Configuration for single sort column
/// </summary>
/// <param name="ColumnName">Column name to sort</param>
/// <param name="Direction">Sort direction</param>
/// <param name="Priority">Sort priority (0 = primary, 1+ = secondary)</param>
/// <param name="CaseSensitive">Case-sensitive string comparison</param>
public record SortColumnConfig(
    string ColumnName,
    PublicSortDirection Direction,
    int Priority = 0,
    bool CaseSensitive = false
);

/// <summary>
/// Command for multi-column sort operation
/// </summary>
/// <param name="Data">Data to sort</param>
/// <param name="SortColumns">List of columns to sort by</param>
/// <param name="PerformanceMode">Performance optimization mode</param>
/// <param name="Timeout">Operation timeout</param>
/// <param name="Progress">Progress reporting callback</param>
/// <param name="CorrelationId">Operation correlation ID for logging</param>
public record MultiSortDataCommand(
    IEnumerable<IReadOnlyDictionary<string, object?>> Data,
    IReadOnlyList<SortColumnConfig> SortColumns,
    PublicSortPerformanceMode PerformanceMode = PublicSortPerformanceMode.Auto,
    TimeSpan? Timeout = null,
    IProgress<SortProgress>? Progress = null,
    Guid? CorrelationId = null
)
{
    public MultiSortDataCommand() : this(Array.Empty<IReadOnlyDictionary<string, object?>>(), Array.Empty<SortColumnConfig>()) { }
}

/// <summary>
/// Result of sort operation with statistics
/// </summary>
/// <param name="IsSuccess">Whether operation succeeded</param>
/// <param name="SortedData">Sorted data</param>
/// <param name="ProcessedRows">Number of rows processed</param>
/// <param name="Duration">Total operation duration</param>
/// <param name="UsedParallelProcessing">Whether parallel processing was used</param>
/// <param name="ErrorMessages">Error messages if operation failed</param>
public record SortDataResult(
    bool IsSuccess,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> SortedData,
    int ProcessedRows,
    TimeSpan Duration,
    bool UsedParallelProcessing = false,
    IReadOnlyList<string>? ErrorMessages = null
)
{
    public SortDataResult() : this(false, Array.Empty<IReadOnlyDictionary<string, object?>>(), 0, TimeSpan.Zero, false, null) { }
}

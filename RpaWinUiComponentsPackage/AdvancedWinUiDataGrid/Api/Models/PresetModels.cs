namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public sort configuration with preset support
/// </summary>
public sealed class PublicSortConfiguration
{
    /// <summary>
    /// Name of this sort configuration
    /// </summary>
    public string ConfigurationName { get; init; } = string.Empty;

    /// <summary>
    /// Sort columns with priority
    /// </summary>
    public IReadOnlyList<PublicSortColumn> SortColumns { get; init; } = Array.Empty<PublicSortColumn>();

    /// <summary>
    /// Performance mode (Auto, Optimized, Maximum)
    /// </summary>
    public string PerformanceMode { get; init; } = "Auto";

    /// <summary>
    /// Enable parallel processing for large datasets
    /// </summary>
    public bool EnableParallelProcessing { get; init; }

    /// <summary>
    /// Maximum number of sort columns
    /// </summary>
    public int MaxSortColumns { get; init; } = 3;

    /// <summary>
    /// Batch size for sort operations
    /// </summary>
    public int BatchSize { get; init; } = 1000;
}

/// <summary>
/// Public sort column configuration
/// </summary>
public sealed class PublicSortColumn
{
    /// <summary>
    /// Column name to sort by
    /// </summary>
    public string ColumnName { get; init; } = string.Empty;

    /// <summary>
    /// Sort direction (Ascending, Descending)
    /// </summary>
    public string Direction { get; init; } = "Ascending";

    /// <summary>
    /// Sort priority (0 = highest priority)
    /// </summary>
    public int Priority { get; init; }
}

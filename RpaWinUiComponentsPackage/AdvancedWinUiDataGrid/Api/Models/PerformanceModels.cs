namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

/// <summary>
/// Public performance metrics
/// </summary>
public sealed class PublicPerformanceMetrics
{
    /// <summary>
    /// Average operation duration
    /// </summary>
    public TimeSpan AverageOperationDuration { get; init; }

    /// <summary>
    /// Total operations executed
    /// </summary>
    public long TotalOperations { get; init; }

    /// <summary>
    /// Operations per second
    /// </summary>
    public double OperationsPerSecond { get; init; }

    /// <summary>
    /// Peak memory usage in bytes
    /// </summary>
    public long PeakMemoryUsageBytes { get; init; }

    /// <summary>
    /// Current memory usage in bytes
    /// </summary>
    public long CurrentMemoryUsageBytes { get; init; }

    /// <summary>
    /// Render time statistics
    /// </summary>
    public PublicRenderStats RenderStats { get; init; } = new();
}

/// <summary>
/// Public render statistics
/// </summary>
public sealed class PublicRenderStats
{
    /// <summary>
    /// Average render time
    /// </summary>
    public TimeSpan AverageRenderTime { get; init; }

    /// <summary>
    /// Total renders
    /// </summary>
    public long TotalRenders { get; init; }

    /// <summary>
    /// Frames per second
    /// </summary>
    public double FramesPerSecond { get; init; }
}

/// <summary>
/// Public memory usage information
/// </summary>
public sealed class PublicMemoryUsage
{
    /// <summary>
    /// Total allocated memory in bytes
    /// </summary>
    public long AllocatedBytes { get; init; }

    /// <summary>
    /// Grid data memory usage in bytes
    /// </summary>
    public long DataMemoryBytes { get; init; }

    /// <summary>
    /// UI memory usage in bytes
    /// </summary>
    public long UiMemoryBytes { get; init; }

    /// <summary>
    /// Cache memory usage in bytes
    /// </summary>
    public long CacheMemoryBytes { get; init; }
}

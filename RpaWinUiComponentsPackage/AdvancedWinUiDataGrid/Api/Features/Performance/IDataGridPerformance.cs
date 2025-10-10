
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Performance;

/// <summary>
/// Public interface for DataGrid performance operations.
/// Provides performance monitoring, optimization, and diagnostics.
/// </summary>
public interface IDataGridPerformance
{
    /// <summary>
    /// Gets current performance metrics.
    /// </summary>
    /// <returns>Performance metrics</returns>
    PublicPerformanceMetrics GetPerformanceMetrics();

    /// <summary>
    /// Resets performance metrics.
    /// </summary>
    /// <returns>Result of the operation</returns>
    PublicResult ResetPerformanceMetrics();

    /// <summary>
    /// Enables virtualization for better performance with large datasets.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> EnableVirtualizationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables virtualization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> DisableVirtualizationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes memory usage by clearing internal caches.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> OptimizeMemoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets memory usage statistics.
    /// </summary>
    /// <returns>Memory usage information</returns>
    PublicMemoryUsage GetMemoryUsage();

    /// <summary>
    /// Checks if virtualization is enabled.
    /// </summary>
    /// <returns>True if virtualization is enabled</returns>
    bool IsVirtualizationEnabled();

    /// <summary>
    /// Sets rendering throttle delay (ms) to reduce UI updates.
    /// </summary>
    /// <param name="delayMs">Delay in milliseconds</param>
    /// <returns>Result of the operation</returns>
    PublicResult SetRenderingThrottle(int delayMs);

    /// <summary>
    /// Gets current rendering throttle delay.
    /// </summary>
    /// <returns>Throttle delay in milliseconds</returns>
    int GetRenderingThrottle();
}

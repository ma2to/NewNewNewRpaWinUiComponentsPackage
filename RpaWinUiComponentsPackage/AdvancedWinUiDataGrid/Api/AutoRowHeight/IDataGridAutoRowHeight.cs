using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.AutoRowHeight;

/// <summary>
/// Public interface for DataGrid auto row height operations.
/// Provides automatic row height adjustment based on content.
/// </summary>
public interface IDataGridAutoRowHeight
{
    /// <summary>
    /// Enables auto row height for the grid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> EnableAutoRowHeightAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables auto row height for the grid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> DisableAutoRowHeightAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjusts height for a specific row.
    /// </summary>
    /// <param name="rowIndex">Row index to adjust</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with calculated height</returns>
    Task<PublicResult<double>> AdjustRowHeightAsync(int rowIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjusts height for all rows.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> AdjustAllRowHeightsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets minimum row height.
    /// </summary>
    /// <param name="minHeight">Minimum height in pixels</param>
    /// <returns>Result of the operation</returns>
    PublicResult SetMinRowHeight(double minHeight);

    /// <summary>
    /// Sets maximum row height.
    /// </summary>
    /// <param name="maxHeight">Maximum height in pixels</param>
    /// <returns>Result of the operation</returns>
    PublicResult SetMaxRowHeight(double maxHeight);

    /// <summary>
    /// Checks if auto row height is enabled.
    /// </summary>
    /// <returns>True if auto row height is enabled</returns>
    bool IsAutoRowHeightEnabled();

    /// <summary>
    /// Gets current minimum row height.
    /// </summary>
    /// <returns>Minimum row height</returns>
    double GetMinRowHeight();

    /// <summary>
    /// Gets current maximum row height.
    /// </summary>
    /// <returns>Maximum row height</returns>
    double GetMaxRowHeight();
}

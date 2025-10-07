using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Theming;

/// <summary>
/// Public interface for DataGrid theming operations.
/// Provides theme management and color customization.
/// </summary>
public interface IDataGridTheming
{
    /// <summary>
    /// Applies a theme to the grid.
    /// </summary>
    /// <param name="theme">Theme to apply</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ApplyThemeAsync(PublicGridTheme theme, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current theme.
    /// </summary>
    /// <returns>Current grid theme</returns>
    PublicGridTheme GetCurrentTheme();

    /// <summary>
    /// Resets theme to default.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ResetToDefaultThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets cell background color.
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="columnName">Column name</param>
    /// <param name="color">Color value (hex format)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SetCellBackgroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets cell foreground color.
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="columnName">Column name</param>
    /// <param name="color">Color value (hex format)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SetCellForegroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets row background color.
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="color">Color value (hex format)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SetRowBackgroundColorAsync(int rowIndex, string color, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears custom colors from a cell.
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="columnName">Column name</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearCellColorsAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all custom colors from the grid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearAllColorsAsync(CancellationToken cancellationToken = default);
}

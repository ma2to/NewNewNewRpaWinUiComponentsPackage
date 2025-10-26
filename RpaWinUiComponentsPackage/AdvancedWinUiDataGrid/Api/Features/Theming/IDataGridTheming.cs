
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
    /// Sets cell background color by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// BREAKING CHANGE v3.0: Replaces rowIndex-based SetCellBackgroundColorAsync.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="columnName">Column name</param>
    /// <param name="color">Color value (hex format)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SetCellBackgroundColorAsync(string rowId, string columnName, string color, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets cell foreground color by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// BREAKING CHANGE v3.0: Replaces rowIndex-based SetCellForegroundColorAsync.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="columnName">Column name</param>
    /// <param name="color">Color value (hex format)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SetCellForegroundColorAsync(string rowId, string columnName, string color, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets row background color by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// BREAKING CHANGE v3.0: Replaces rowIndex-based SetRowBackgroundColorAsync.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="color">Color value (hex format)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SetRowBackgroundColorAsync(string rowId, string color, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears custom colors from a cell by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// BREAKING CHANGE v3.0: Replaces rowIndex-based ClearCellColorsAsync.
    /// </summary>
    /// <param name="rowId">Stable row identifier (from __rowId field)</param>
    /// <param name="columnName">Column name</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearCellColorsAsync(string rowId, string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all custom colors from the grid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearAllColorsAsync(CancellationToken cancellationToken = default);
}

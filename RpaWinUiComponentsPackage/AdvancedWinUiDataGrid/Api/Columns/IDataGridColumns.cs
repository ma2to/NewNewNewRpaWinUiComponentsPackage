using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Columns;

/// <summary>
/// Public interface for DataGrid column operations.
/// Provides comprehensive column management including visibility, ordering, resizing, and configuration.
/// </summary>
public interface IDataGridColumns
{
    /// <summary>
    /// Adds a new column to the grid.
    /// </summary>
    /// <param name="columnDefinition">Column definition</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> AddColumnAsync(PublicColumnDefinition columnDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a column from the grid.
    /// </summary>
    /// <param name="columnName">Column name to remove</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> RemoveColumnAsync(string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a hidden column.
    /// </summary>
    /// <param name="columnName">Column name to show</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ShowColumnAsync(string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides a visible column.
    /// </summary>
    /// <param name="columnName">Column name to hide</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> HideColumnAsync(string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders a column to a new position.
    /// </summary>
    /// <param name="columnName">Column name to move</param>
    /// <param name="newIndex">New column index</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ReorderColumnAsync(string columnName, int newIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resizes a column to specified width.
    /// </summary>
    /// <param name="columnName">Column name to resize</param>
    /// <param name="newWidth">New column width</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ResizeColumnAsync(string columnName, double newWidth, CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-fits a column width to content.
    /// </summary>
    /// <param name="columnName">Column name to auto-fit</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> AutoFitColumnAsync(string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-fits all columns to their content.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> AutoFitAllColumnsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all column definitions.
    /// </summary>
    /// <returns>Collection of column definitions</returns>
    IReadOnlyList<PublicColumnDefinition> GetAllColumns();

    /// <summary>
    /// Gets visible column definitions.
    /// </summary>
    /// <returns>Collection of visible column definitions</returns>
    IReadOnlyList<PublicColumnDefinition> GetVisibleColumns();

    /// <summary>
    /// Gets a specific column definition.
    /// </summary>
    /// <param name="columnName">Column name</param>
    /// <returns>Column definition or null if not found</returns>
    PublicColumnDefinition? GetColumn(string columnName);

    /// <summary>
    /// Checks if a column exists.
    /// </summary>
    /// <param name="columnName">Column name to check</param>
    /// <returns>True if column exists</returns>
    bool ColumnExists(string columnName);

    /// <summary>
    /// Gets column count.
    /// </summary>
    /// <returns>Total number of columns</returns>
    int GetColumnCount();

    /// <summary>
    /// Gets visible column count.
    /// </summary>
    /// <returns>Number of visible columns</returns>
    int GetVisibleColumnCount();
}


namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Sorting;

/// <summary>
/// Public interface for DataGrid sorting operations.
/// Provides comprehensive sorting functionality including single, multi-column, and custom sorting.
/// </summary>
public interface IDataGridSorting
{
    /// <summary>
    /// Sorts data by single column with specified direction.
    /// </summary>
    /// <param name="columnName">Column name to sort by</param>
    /// <param name="direction">Sort direction (ascending/descending)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SortByColumnAsync(string columnName, PublicSortDirection direction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sorts data by multiple columns with specified directions.
    /// </summary>
    /// <param name="sortDescriptors">Collection of sort descriptors</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SortByMultipleColumnsAsync(IEnumerable<PublicSortDescriptor> sortDescriptors, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all sorting from the grid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearSortingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current sort descriptors applied to the grid.
    /// </summary>
    /// <returns>Collection of active sort descriptors</returns>
    IReadOnlyList<PublicSortDescriptor> GetCurrentSortDescriptors();

    /// <summary>
    /// Toggles sort direction for a column (None -> Ascending -> Descending -> None).
    /// </summary>
    /// <param name="columnName">Column name to toggle</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with new sort direction</returns>
    Task<PublicResult<PublicSortDirection>> ToggleSortDirectionAsync(string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a column is currently sorted.
    /// </summary>
    /// <param name="columnName">Column name to check</param>
    /// <returns>True if column is sorted</returns>
    bool IsColumnSorted(string columnName);

    /// <summary>
    /// Gets sort direction for a specific column.
    /// </summary>
    /// <param name="columnName">Column name</param>
    /// <returns>Sort direction for the column</returns>
    Api.Models.PublicSortDirection GetColumnSortDirection(string columnName);
}

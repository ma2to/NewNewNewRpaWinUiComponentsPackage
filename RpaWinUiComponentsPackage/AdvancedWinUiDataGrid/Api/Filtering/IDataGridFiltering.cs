using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Filtering;

/// <summary>
/// Public interface for DataGrid filtering operations.
/// Provides comprehensive filtering functionality including column filters, custom predicates, and filter management.
/// </summary>
public interface IDataGridFiltering
{
    /// <summary>
    /// Applies a filter to a specific column.
    /// </summary>
    /// <param name="columnName">Column name to filter</param>
    /// <param name="filterOperator">Filter operator (Equals, Contains, etc.)</param>
    /// <param name="filterValue">Value to filter by</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ApplyColumnFilterAsync(string columnName, PublicFilterOperator filterOperator, object? filterValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies multiple filters to the grid.
    /// </summary>
    /// <param name="filters">Collection of filter descriptors</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ApplyMultipleFiltersAsync(IEnumerable<PublicFilterDescriptor> filters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes filter from a specific column.
    /// </summary>
    /// <param name="columnName">Column name to remove filter from</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> RemoveColumnFilterAsync(string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all filters from the grid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ClearAllFiltersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current filter descriptors applied to the grid.
    /// </summary>
    /// <returns>Collection of active filter descriptors</returns>
    IReadOnlyList<PublicFilterDescriptor> GetCurrentFilters();

    /// <summary>
    /// Checks if a column has an active filter.
    /// </summary>
    /// <param name="columnName">Column name to check</param>
    /// <returns>True if column has a filter</returns>
    bool IsColumnFiltered(string columnName);

    /// <summary>
    /// Gets filter count.
    /// </summary>
    /// <returns>Number of active filters</returns>
    int GetFilterCount();

    /// <summary>
    /// Gets filtered row count.
    /// </summary>
    /// <returns>Number of rows matching current filters</returns>
    int GetFilteredRowCount();
}

using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;

/// <summary>
/// Service interface for column management operations
/// Implements Scoped lifetime per DI_DECISIONS.md - per-operation state isolation
/// </summary>
internal interface IColumnService
{
    /// <summary>
    /// Gets all column definitions
    /// </summary>
    /// <returns>Read-only collection of column definitions</returns>
    IReadOnlyList<ColumnDefinition> GetColumnDefinitions();

    /// <summary>
    /// Adds a new column definition
    /// </summary>
    /// <param name="columnDefinition">Column definition to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> AddColumnAsync(
        ColumnDefinition columnDefinition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a column by name
    /// </summary>
    /// <param name="columnName">Name of column to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> RemoveColumnAsync(
        string columnName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing column definition
    /// </summary>
    /// <param name="columnDefinition">Updated column definition</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> UpdateColumnAsync(
        ColumnDefinition columnDefinition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a column definition by name
    /// </summary>
    /// <param name="columnName">Name of column to get</param>
    /// <returns>Column definition or null if not found</returns>
    ColumnDefinition? GetColumnByName(string columnName);

    /// <summary>
    /// Starts column resize operation - NO per-operation mutable fields
    /// </summary>
    /// <param name="columnIndex">Index of column to resize</param>
    /// <param name="clientX">Initial client X coordinate</param>
    void StartColumnResize(int columnIndex, double clientX);

    /// <summary>
    /// Updates column resize operation - NO per-operation mutable fields
    /// </summary>
    /// <param name="columnIndex">Index of column being resized</param>
    /// <param name="clientX">Current client X coordinate</param>
    void UpdateColumnResize(int columnIndex, double clientX);

    /// <summary>
    /// Ends column resize operation - NO per-operation mutable fields
    /// </summary>
    /// <param name="columnIndex">Index of column being resized</param>
    /// <param name="clientX">Final client X coordinate</param>
    void EndColumnResize(int columnIndex, double clientX);

    /// <summary>
    /// Resizes a column to specific width
    /// </summary>
    /// <param name="columnIndex">Index of column to resize</param>
    /// <param name="newWidth">New width for the column</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> ResizeColumnAsync(
        int columnIndex,
        double newWidth,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of columns
    /// </summary>
    /// <returns>Total column count</returns>
    int GetColumnCount();

    /// <summary>
    /// Gets visible columns (excluding hidden special columns)
    /// </summary>
    /// <returns>Visible column definitions</returns>
    IReadOnlyList<ColumnDefinition> GetVisibleColumns();

    /// <summary>
    /// Reorders columns
    /// </summary>
    /// <param name="fromIndex">Source column index</param>
    /// <param name="toIndex">Target column index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> ReorderColumnAsync(
        int fromIndex,
        int toIndex,
        CancellationToken cancellationToken = default);

    // Additional methods called by Facade for internal operations

    /// <summary>
    /// Starts column resize operation (internal)
    /// </summary>
    /// <param name="columnIndex">Index of column to resize</param>
    /// <param name="clientX">Initial client X coordinate</param>
    void StartResizeInternal(int columnIndex, double clientX);

    /// <summary>
    /// Updates column resize operation (internal)
    /// </summary>
    /// <param name="clientX">Current client X coordinate</param>
    void UpdateResizeInternal(double clientX);

    /// <summary>
    /// Ends column resize operation (internal)
    /// </summary>
    void EndResizeInternal();

    /// <summary>
    /// Adds a new column definition (simple)
    /// </summary>
    /// <param name="columnDefinition">Column definition to add</param>
    /// <returns>True if successful</returns>
    bool AddColumn(ColumnDefinition columnDefinition);

    /// <summary>
    /// Removes a column by name (simple)
    /// </summary>
    /// <param name="columnName">Name of column to remove</param>
    /// <returns>True if successful</returns>
    bool RemoveColumn(string columnName);

    /// <summary>
    /// Updates an existing column definition (simple)
    /// </summary>
    /// <param name="columnDefinition">Updated column definition</param>
    /// <returns>True if successful</returns>
    bool UpdateColumn(ColumnDefinition columnDefinition);

    // Wrapper methods for public API
    Task<Result> ShowColumnAsync(string columnName, CancellationToken cancellationToken = default);
    Task<Result> HideColumnAsync(string columnName, CancellationToken cancellationToken = default);
    ColumnDefinition? GetColumn(string columnName);
    IReadOnlyList<ColumnDefinition> GetAllColumns();
    bool ColumnExists(string columnName);
    int GetVisibleColumnCount();
}
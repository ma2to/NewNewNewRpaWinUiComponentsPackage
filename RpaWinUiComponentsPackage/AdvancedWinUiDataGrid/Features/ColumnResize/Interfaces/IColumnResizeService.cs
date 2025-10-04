namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.ColumnResize.Interfaces;

/// <summary>
/// Internal service for column resize operations via drag & drop
/// Thread-safe implementation with debounce support
/// </summary>
internal interface IColumnResizeService
{
    /// <summary>
    /// Resizes a column to a specific width
    /// Enforces min/max width constraints
    /// </summary>
    /// <param name="columnIndex">Index of the column to resize</param>
    /// <param name="newWidth">New width in pixels</param>
    /// <returns>Actual width applied after constraint enforcement</returns>
    double ResizeColumn(int columnIndex, double newWidth);

    /// <summary>
    /// Starts a column resize operation (mouse down on column border)
    /// </summary>
    /// <param name="columnIndex">Index of the column being resized</param>
    /// <param name="clientX">Initial mouse X position</param>
    void StartColumnResize(int columnIndex, double clientX);

    /// <summary>
    /// Updates column width during drag (mouse move)
    /// Debounced for performance
    /// </summary>
    /// <param name="clientX">Current mouse X position</param>
    void UpdateColumnResize(double clientX);

    /// <summary>
    /// Ends the column resize operation (mouse up)
    /// </summary>
    void EndColumnResize();

    /// <summary>
    /// Gets the current width of a column
    /// </summary>
    /// <param name="columnIndex">Index of the column</param>
    /// <returns>Current width in pixels</returns>
    double GetColumnWidth(int columnIndex);

    /// <summary>
    /// Checks if a resize operation is currently active
    /// </summary>
    /// <returns>True if resizing, false otherwise</returns>
    bool IsResizing();
}

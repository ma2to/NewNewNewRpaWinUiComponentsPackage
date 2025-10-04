using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;

/// <summary>
/// Service interface for selection management operations
/// Implements Scoped lifetime per DI_DECISIONS.md - per-operation state isolation
/// </summary>
internal interface ISelectionService
{
    /// <summary>
    /// Selects a specific cell - NO per-operation mutable fields
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="col">Column index</param>
    void SelectCell(int row, int col);

    /// <summary>
    /// Starts drag selection operation - NO per-operation mutable fields
    /// </summary>
    /// <param name="row">Starting row index</param>
    /// <param name="col">Starting column index</param>
    void StartDragSelect(int row, int col);

    /// <summary>
    /// Updates drag selection to new position - NO per-operation mutable fields
    /// </summary>
    /// <param name="row">Current row index</param>
    /// <param name="col">Current column index</param>
    void DragSelectTo(int row, int col);

    /// <summary>
    /// Ends drag selection operation - NO per-operation mutable fields
    /// </summary>
    /// <param name="row">Final row index</param>
    /// <param name="col">Final column index</param>
    void EndDragSelect(int row, int col);

    /// <summary>
    /// Toggles cell selection state - NO per-operation mutable fields
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="col">Column index</param>
    void ToggleCellSelection(int row, int col);

    /// <summary>
    /// Extends selection to specified cell - NO per-operation mutable fields
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="col">Column index</param>
    void ExtendSelectionTo(int row, int col);

    /// <summary>
    /// Gets currently selected cells
    /// </summary>
    /// <returns>Collection of selected cell coordinates</returns>
    IReadOnlyList<(int Row, int Column)> GetSelectedCells();

    /// <summary>
    /// Gets currently selected rows
    /// </summary>
    /// <returns>Collection of selected row indices</returns>
    IReadOnlyList<int> GetSelectedRows();

    /// <summary>
    /// Gets currently selected columns
    /// </summary>
    /// <returns>Collection of selected column indices</returns>
    IReadOnlyList<int> GetSelectedColumns();

    /// <summary>
    /// Selects entire row
    /// </summary>
    /// <param name="rowIndex">Row index to select</param>
    void SelectRow(int rowIndex);

    /// <summary>
    /// Selects entire column
    /// </summary>
    /// <param name="columnIndex">Column index to select</param>
    void SelectColumn(int columnIndex);

    /// <summary>
    /// Selects range of cells
    /// </summary>
    /// <param name="startRow">Start row index</param>
    /// <param name="startCol">Start column index</param>
    /// <param name="endRow">End row index</param>
    /// <param name="endCol">End column index</param>
    void SelectRange(int startRow, int startCol, int endRow, int endCol);

    /// <summary>
    /// Clears all selections
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// Selects all cells
    /// </summary>
    void SelectAll();

    /// <summary>
    /// Gets selection bounds
    /// </summary>
    /// <returns>Selection bounds as (startRow, startCol, endRow, endCol) or null if no selection</returns>
    (int StartRow, int StartCol, int EndRow, int EndCol)? GetSelectionBounds();

    /// <summary>
    /// Checks if a cell is selected
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="col">Column index</param>
    /// <returns>True if cell is selected</returns>
    bool IsCellSelected(int row, int col);

    /// <summary>
    /// Checks if a row is selected
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <returns>True if row is selected</returns>
    bool IsRowSelected(int rowIndex);

    /// <summary>
    /// Checks if a column is selected
    /// </summary>
    /// <param name="columnIndex">Column index</param>
    /// <returns>True if column is selected</returns>
    bool IsColumnSelected(int columnIndex);

    /// <summary>
    /// Gets selected data as dictionary collection
    /// </summary>
    /// <returns>Selected data</returns>
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GetSelectedData();

    /// <summary>
    /// Creates an immutable snapshot of current selection for thread-safe access
    /// </summary>
    /// <returns>Immutable selection snapshot</returns>
    SelectionSnapshot CreateSelectionSnapshot();

    // Additional methods called by Facade for internal operations

    /// <summary>
    /// Starts drag selection operation (internal)
    /// </summary>
    /// <param name="row">Starting row index</param>
    /// <param name="col">Starting column index</param>
    void StartDragSelectInternal(int row, int col);

    /// <summary>
    /// Updates drag selection to new position (internal)
    /// </summary>
    /// <param name="row">Current row index</param>
    /// <param name="col">Current column index</param>
    void UpdateDragSelectInternal(int row, int col);

    /// <summary>
    /// Ends drag selection operation (internal)
    /// </summary>
    void EndDragSelectInternal();

    /// <summary>
    /// Selects a specific cell (internal)
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="col">Column index</param>
    void SelectCellInternal(int row, int col);

    /// <summary>
    /// Toggles cell selection state (internal)
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="col">Column index</param>
    void ToggleSelectionInternal(int row, int col);

    /// <summary>
    /// Extends selection to specified cell (internal)
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="col">Column index</param>
    void ExtendSelectionInternal(int row, int col);
}

/// <summary>
/// Immutable snapshot of selection state for thread-safe access
/// </summary>
/// <param name="SelectedCells">Selected cell coordinates</param>
/// <param name="SelectedRows">Selected row indices</param>
/// <param name="SelectedColumns">Selected column indices</param>
/// <param name="SelectionBounds">Selection bounds</param>
/// <param name="Timestamp">When snapshot was created</param>
internal record SelectionSnapshot(
    IReadOnlyList<(int Row, int Column)> SelectedCells,
    IReadOnlyList<int> SelectedRows,
    IReadOnlyList<int> SelectedColumns,
    (int StartRow, int StartCol, int EndRow, int EndCol)? SelectionBounds,
    DateTime Timestamp
)
{
    /// <summary>
    /// Gets the number of selected cells
    /// </summary>
    public int SelectedCellCount => SelectedCells.Count;

    /// <summary>
    /// Gets whether any selection exists
    /// </summary>
    public bool HasSelection => SelectedCells.Count > 0 || SelectedRows.Count > 0 || SelectedColumns.Count > 0;

    /// <summary>
    /// Creates an empty selection snapshot
    /// </summary>
    public static SelectionSnapshot Empty => new(
        Array.Empty<(int, int)>(),
        Array.Empty<int>(),
        Array.Empty<int>(),
        null,
        DateTime.UtcNow
    );
}
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Represents a cell address in the grid
/// </summary>
internal record CellAddress(int Row, string Column);

/// <summary>
/// Represents a selection range
/// </summary>
internal record SelectionRange(
    int StartRow,
    int EndRow,
    string StartColumn,
    string EndColumn
);

/// <summary>
/// Command for selecting cells
/// </summary>
internal record SelectCellsCommand(
    IReadOnlyList<CellAddress> CellAddresses,
    SelectionMode SelectionMode = SelectionMode.Replace
);

/// <summary>
/// Command for selecting rows
/// </summary>
internal record SelectRowsCommand(
    IReadOnlyList<int> RowIndices,
    SelectionMode SelectionMode = SelectionMode.Replace
);

/// <summary>
/// Command for selecting columns
/// </summary>
internal record SelectColumnsCommand(
    IReadOnlyList<string> ColumnNames,
    SelectionMode SelectionMode = SelectionMode.Replace
);

/// <summary>
/// Selection mode enumeration
/// </summary>
internal enum SelectionMode
{
    Replace,
    Add,
    Remove,
    Toggle
}

/// <summary>
/// Result of selection operation
/// </summary>
internal class SelectionResult
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the selected cell addresses
    /// </summary>
    public IReadOnlyList<CellAddress> SelectedCells { get; init; } = Array.Empty<CellAddress>();

    /// <summary>
    /// Gets the operation duration
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the error message if failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates successful result
    /// </summary>
    public static SelectionResult Success(IReadOnlyList<CellAddress> selectedCells, TimeSpan duration)
    {
        return new SelectionResult
        {
            IsSuccess = true,
            SelectedCells = selectedCells,
            Duration = duration
        };
    }

    /// <summary>
    /// Creates failed result
    /// </summary>
    public static SelectionResult Failed(string errorMessage, TimeSpan duration)
    {
        return new SelectionResult
        {
            IsSuccess = false,
            Duration = duration,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates cancelled result
    /// </summary>
    public static SelectionResult Cancelled(TimeSpan duration)
    {
        return new SelectionResult
        {
            IsSuccess = false,
            Duration = duration,
            ErrorMessage = "Operation was cancelled"
        };
    }
}
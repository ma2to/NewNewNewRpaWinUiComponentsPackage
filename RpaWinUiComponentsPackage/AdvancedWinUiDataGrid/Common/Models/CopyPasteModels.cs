using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Command for copying data
/// </summary>
internal record CopyDataCommand(
    IReadOnlyList<CellAddress> SelectedItems,
    CopyMode CopyMode = CopyMode.SelectedCells,
    bool IncludeFormatting = false,
    bool CopyFormulas = false
);

/// <summary>
/// Command for pasting data
/// </summary>
internal record PasteDataCommand(
    int TargetRow,
    string TargetColumn,
    PasteMode PasteMode = PasteMode.Overwrite,
    bool PasteFormatting = false,
    bool PasteFormulas = false,
    bool SkipEmptyCells = false
);

/// <summary>
/// Copy mode enumeration
/// </summary>
internal enum CopyMode
{
    SelectedCells,
    SelectedRows,
    SelectedColumns
}

/// <summary>
/// Paste mode enumeration
/// </summary>
internal enum PasteMode
{
    Insert,
    Overwrite,
    Append
}

/// <summary>
/// Result of copy/paste operation
/// </summary>
internal class CopyPasteResult
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the number of items processed
    /// </summary>
    public int ProcessedItems { get; init; }

    /// <summary>
    /// Gets the operation duration
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets whether validation passed after the operation
    /// </summary>
    public bool ValidationPassed { get; init; } = true;

    /// <summary>
    /// Gets the error message if failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates successful result
    /// </summary>
    public static CopyPasteResult Success(int processedItems, TimeSpan duration, bool validationPassed = true)
    {
        return new CopyPasteResult
        {
            IsSuccess = true,
            ProcessedItems = processedItems,
            Duration = duration,
            ValidationPassed = validationPassed
        };
    }

    /// <summary>
    /// Creates failed result
    /// </summary>
    public static CopyPasteResult Failed(IReadOnlyList<string> errors, TimeSpan duration)
    {
        return new CopyPasteResult
        {
            IsSuccess = false,
            Duration = duration,
            ErrorMessage = string.Join("; ", errors)
        };
    }

    /// <summary>
    /// Creates cancelled result
    /// </summary>
    public static CopyPasteResult Cancelled(TimeSpan duration)
    {
        return new CopyPasteResult
        {
            IsSuccess = false,
            Duration = duration,
            ErrorMessage = "Operation was cancelled"
        };
    }
}
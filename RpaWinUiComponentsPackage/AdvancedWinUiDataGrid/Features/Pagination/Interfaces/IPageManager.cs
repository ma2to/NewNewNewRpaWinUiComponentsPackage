using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Pagination.Interfaces;

/// <summary>
/// Manages pagination state and calculations for DataGrid.
/// CRITICAL: Enables virtual row management where physical row count stays constant
/// and only data shifts between pages.
/// </summary>
public interface IPageManager
{
    /// <summary>
    /// Current page index (0-based)
    /// </summary>
    int CurrentPage { get; }

    /// <summary>
    /// Number of rows per page (default 20)
    /// </summary>
    int PageSize { get; set; }

    /// <summary>
    /// Total number of data rows (not physical viewport rows)
    /// </summary>
    long TotalDataRows { get; }

    /// <summary>
    /// Total number of pages based on TotalDataRows / PageSize
    /// </summary>
    int TotalPages { get; }

    /// <summary>
    /// Can navigate to previous page
    /// </summary>
    bool CanGoPrevious { get; }

    /// <summary>
    /// Can navigate to next page
    /// </summary>
    bool CanGoNext { get; }

    /// <summary>
    /// Event fired when page changes
    /// </summary>
    event EventHandler<PageChangedEventArgs>? PageChanged;

    /// <summary>
    /// Event fired when page size changes
    /// </summary>
    event EventHandler<int>? PageSizeChanged;

    /// <summary>
    /// Set total data rows (recalculates total pages)
    /// </summary>
    void SetTotalDataRows(long totalRows);

    /// <summary>
    /// Navigate to specific page (0-based)
    /// </summary>
    bool GoToPage(int pageIndex);

    /// <summary>
    /// Navigate to next page
    /// </summary>
    bool NextPage();

    /// <summary>
    /// Navigate to previous page
    /// </summary>
    bool PreviousPage();

    /// <summary>
    /// Navigate to first page
    /// </summary>
    bool FirstPage();

    /// <summary>
    /// Navigate to last page
    /// </summary>
    bool LastPage();

    /// <summary>
    /// Get the data row range for current page (start index, count)
    /// </summary>
    (long startIndex, int count) GetCurrentPageRange();

    /// <summary>
    /// Get the data row range for specific page
    /// </summary>
    (long startIndex, int count) GetPageRange(int pageIndex);

    /// <summary>
    /// Get display text for current page (e.g., "Page 1 of 5")
    /// </summary>
    string GetPageDisplayText();

    /// <summary>
    /// Reset pagination to first page
    /// </summary>
    void Reset();
}

/// <summary>
/// Event args for page change event
/// </summary>
public class PageChangedEventArgs : EventArgs
{
    public int OldPage { get; init; }
    public int NewPage { get; init; }
    public long StartIndex { get; init; }
    public int Count { get; init; }
}

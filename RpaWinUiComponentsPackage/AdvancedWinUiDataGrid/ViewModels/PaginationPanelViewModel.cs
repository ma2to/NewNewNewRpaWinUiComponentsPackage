using System.Collections.ObjectModel;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// ViewModel for pagination panel.
/// Manages pagination state including current page, page size, total count, and navigation.
/// </summary>
public sealed class PaginationPanelViewModel : ViewModelBase
{
    private int _currentPage = 1;
    private int _pageSize = 1000;
    private long _totalRowCount = 0;
    private bool _isPaginationEnabled = true;

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(PageInfo));
                UpdatePageNumbers();
            }
        }
    }

    /// <summary>
    /// Number of rows per page
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value) && value > 0)
            {
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(PageInfo));
                UpdatePageNumbers();
            }
        }
    }

    /// <summary>
    /// Total number of rows in dataset (after filtering)
    /// </summary>
    public long TotalRowCount
    {
        get => _totalRowCount;
        set
        {
            if (SetProperty(ref _totalRowCount, value))
            {
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(PageInfo));
                UpdatePageNumbers();
            }
        }
    }

    /// <summary>
    /// Whether pagination is enabled
    /// </summary>
    public bool IsPaginationEnabled
    {
        get => _isPaginationEnabled;
        set => SetProperty(ref _isPaginationEnabled, value);
    }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRowCount / PageSize) : 0;

    /// <summary>
    /// Can navigate to next page
    /// </summary>
    public bool CanGoToNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Can navigate to previous page
    /// </summary>
    public bool CanGoToPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Page information text (e.g., "Page 1 of 10 (1,000 rows)")
    /// </summary>
    public string PageInfo
    {
        get
        {
            var startRow = (CurrentPage - 1) * PageSize + 1;
            var endRow = Math.Min(CurrentPage * PageSize, TotalRowCount);
            return $"Page {CurrentPage} of {TotalPages} ({startRow:N0}-{endRow:N0} of {TotalRowCount:N0} rows)";
        }
    }

    /// <summary>
    /// Collection of page numbers to display (smart rendering: 1 2 3 ... current ... last)
    /// </summary>
    public ObservableCollection<PaginationPageItem> PageNumbers { get; } = new();

    /// <summary>
    /// Event fired when page changes
    /// </summary>
    public event EventHandler<int>? PageChanged;

    /// <summary>
    /// Navigate to next page
    /// </summary>
    public void GoToNextPage()
    {
        if (CanGoToNextPage)
        {
            GoToPage(CurrentPage + 1);
        }
    }

    /// <summary>
    /// Navigate to previous page
    /// </summary>
    public void GoToPreviousPage()
    {
        if (CanGoToPreviousPage)
        {
            GoToPage(CurrentPage - 1);
        }
    }

    /// <summary>
    /// Navigate to specific page
    /// </summary>
    public void GoToPage(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > TotalPages)
            return;

        if (CurrentPage != pageNumber)
        {
            CurrentPage = pageNumber;
            PageChanged?.Invoke(this, pageNumber);
        }
    }

    /// <summary>
    /// Update page numbers collection with smart rendering
    /// Shows: 1 2 3 ... current-1 current current+1 ... last-2 last-1 last
    /// </summary>
    private void UpdatePageNumbers()
    {
        PageNumbers.Clear();

        if (TotalPages <= 0)
            return;

        // If total pages <= 10, show all
        if (TotalPages <= 10)
        {
            for (int i = 1; i <= TotalPages; i++)
            {
                PageNumbers.Add(new PaginationPageItem
                {
                    PageNumber = i,
                    IsCurrent = i == CurrentPage,
                    IsEllipsis = false
                });
            }
            return;
        }

        // Smart rendering for many pages
        // Always show: 1, 2, 3
        for (int i = 1; i <= Math.Min(3, TotalPages); i++)
        {
            PageNumbers.Add(new PaginationPageItem
            {
                PageNumber = i,
                IsCurrent = i == CurrentPage,
                IsEllipsis = false
            });
        }

        // Add ellipsis if needed
        if (CurrentPage > 5)
        {
            PageNumbers.Add(new PaginationPageItem { IsEllipsis = true });
        }

        // Show current page and neighbors (if not already shown)
        int rangeStart = Math.Max(4, CurrentPage - 1);
        int rangeEnd = Math.Min(TotalPages - 3, CurrentPage + 1);

        for (int i = rangeStart; i <= rangeEnd; i++)
        {
            PageNumbers.Add(new PaginationPageItem
            {
                PageNumber = i,
                IsCurrent = i == CurrentPage,
                IsEllipsis = false
            });
        }

        // Add ellipsis if needed
        if (CurrentPage < TotalPages - 4)
        {
            PageNumbers.Add(new PaginationPageItem { IsEllipsis = true });
        }

        // Always show last 3 pages
        for (int i = Math.Max(TotalPages - 2, 4); i <= TotalPages; i++)
        {
            if (!PageNumbers.Any(p => p.PageNumber == i))
            {
                PageNumbers.Add(new PaginationPageItem
                {
                    PageNumber = i,
                    IsCurrent = i == CurrentPage,
                    IsEllipsis = false
                });
            }
        }
    }
}

/// <summary>
/// Represents a single item in the pagination display (page number or ellipsis)
/// </summary>
public sealed class PaginationPageItem
{
    /// <summary>
    /// Page number (0 for ellipsis)
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Whether this is the current page
    /// </summary>
    public bool IsCurrent { get; init; }

    /// <summary>
    /// Whether this is an ellipsis (...)
    /// </summary>
    public bool IsEllipsis { get; init; }

    /// <summary>
    /// Display text for this item
    /// </summary>
    public string DisplayText => IsEllipsis ? "..." : PageNumber.ToString();
}

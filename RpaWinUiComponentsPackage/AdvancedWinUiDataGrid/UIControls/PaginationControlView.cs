using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Pagination.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Pagination navigation control for DataGrid.
/// Displays: << < [1] [2] [3] [4] [5] > >>
///
/// FEATURES:
/// - << First Page (Page 0)
/// - < Previous Page
/// - Page numbers (max 5 visible, intelligently centered)
/// - > Next Page
/// - >> Last Page
///
/// PAGE NUMBERS LOGIC (max 5 visible):
/// - Total 3 pages → Show: 1 2 3
/// - Total 10 pages, current 1 → Show: 1 2 3 4 5
/// - Total 10 pages, current 4 → Show: 2 3 4 5 6
/// - Total 10 pages, current 8 → Show: 6 7 8 9 10
/// </summary>
public sealed class PaginationControlView : StackPanel, IDisposable
{
    private readonly IPageManager _pageManager;
    private readonly ILogger<PaginationControlView>? _logger;

    private Button? _firstPageButton;
    private Button? _previousPageButton;
    private StackPanel? _pageNumbersPanel;
    private Button? _nextPageButton;
    private Button? _lastPageButton;
    private TextBlock? _pageInfoText;

    private bool _disposed = false;

    /// <summary>
    /// Creates pagination control for PageManager.
    /// Automatically subscribes to PageChanged events and updates UI.
    /// </summary>
    public PaginationControlView(IPageManager pageManager, ILogger<PaginationControlView>? logger = null)
    {
        _pageManager = pageManager ?? throw new ArgumentNullException(nameof(pageManager));
        _logger = logger;

        Orientation = Orientation.Horizontal;
        Spacing = 8;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Margin = new Thickness(0, 10, 0, 10);

        BuildPaginationUI();

        // Subscribe to PageManager events
        _pageManager.PageChanged += OnPageChanged;

        // ✅ SENIOR FIX: Subscribe to PageSizeChanged event
        // CRITICAL: This event fires when TotalDataRows changes (not just PageSize!)
        // Without this: Pagination stays hidden after first data load (TotalPages changes from 0→7)
        _pageManager.PageSizeChanged += OnPageSizeOrTotalDataChanged;

        // Initial update
        UpdatePaginationUI();

        _logger?.LogInformation("PaginationControlView created (PageSize={PageSize}, TotalPages={TotalPages})",
            _pageManager.PageSize, _pageManager.TotalPages);
    }

    /// <summary>
    /// Builds pagination UI structure.
    /// Layout: << < [page numbers panel] > >> [page info]
    /// </summary>
    private void BuildPaginationUI()
    {
        // << First Page button
        _firstPageButton = new Button
        {
            Content = "<<",
            MinWidth = 45,
            Height = 32,
            Padding = new Thickness(8, 4, 8, 4)
        };
        ToolTipService.SetToolTip(_firstPageButton, "First Page");
        _firstPageButton.Click += (s, e) => NavigateToFirstPage();
        Children.Add(_firstPageButton);

        // < Previous Page button
        _previousPageButton = new Button
        {
            Content = "<",
            MinWidth = 45,
            Height = 32,
            Padding = new Thickness(8, 4, 8, 4)
        };
        ToolTipService.SetToolTip(_previousPageButton, "Previous Page");
        _previousPageButton.Click += (s, e) => NavigateToPreviousPage();
        Children.Add(_previousPageButton);

        // Page numbers panel (max 5 visible numbers)
        _pageNumbersPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };
        Children.Add(_pageNumbersPanel);

        // > Next Page button
        _nextPageButton = new Button
        {
            Content = ">",
            MinWidth = 45,
            Height = 32,
            Padding = new Thickness(8, 4, 8, 4)
        };
        ToolTipService.SetToolTip(_nextPageButton, "Next Page");
        _nextPageButton.Click += (s, e) => NavigateToNextPage();
        Children.Add(_nextPageButton);

        // >> Last Page button
        _lastPageButton = new Button
        {
            Content = ">>",
            MinWidth = 45,
            Height = 32,
            Padding = new Thickness(8, 4, 8, 4)
        };
        ToolTipService.SetToolTip(_lastPageButton, "Last Page");
        _lastPageButton.Click += (s, e) => NavigateToLastPage();
        Children.Add(_lastPageButton);

        // Page info text (e.g., "Page 1 of 3")
        _pageInfoText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0),
            FontSize = 14
        };
        Children.Add(_pageInfoText);

        _logger?.LogDebug("Pagination UI structure built successfully");
    }

    /// <summary>
    /// Updates pagination UI based on current PageManager state.
    /// Called automatically when page changes or data updates.
    /// </summary>
    private void UpdatePaginationUI()
    {
        if (_disposed)
            return;

        var currentPage = _pageManager.CurrentPage;
        var totalPages = _pageManager.TotalPages;
        var totalDataRows = _pageManager.TotalDataRows;

        _logger?.LogTrace("Updating pagination UI: CurrentPage={Current}, TotalPages={Total}, TotalDataRows={Rows}",
            currentPage, totalPages, totalDataRows);

        // ✅ SENIOR FIX: Hide pagination if no data OR only 1 page (no navigation needed)
        // When TotalPages <= 1, pagination controls are meaningless (can't navigate anywhere)
        if (totalPages <= 1 || totalDataRows == 0)
        {
            Visibility = Visibility.Collapsed;
            _logger?.LogDebug("Pagination hidden (TotalPages={Pages}, TotalDataRows={Rows})",
                totalPages, totalDataRows);
            return;
        }

        Visibility = Visibility.Visible;

        // Update navigation button states
        UpdateNavigationButtons(currentPage, totalPages);

        // Rebuild page number buttons
        RebuildPageNumbers(currentPage, totalPages);

        // Update page info text
        UpdatePageInfoText(currentPage, totalPages, totalDataRows);
    }

    /// <summary>
    /// Updates navigation button enabled states.
    /// </summary>
    private void UpdateNavigationButtons(int currentPage, int totalPages)
    {
        if (_firstPageButton != null)
            _firstPageButton.IsEnabled = _pageManager.CanGoPrevious;

        if (_previousPageButton != null)
            _previousPageButton.IsEnabled = _pageManager.CanGoPrevious;

        if (_nextPageButton != null)
            _nextPageButton.IsEnabled = _pageManager.CanGoNext;

        if (_lastPageButton != null)
            _lastPageButton.IsEnabled = _pageManager.CanGoNext;

        _logger?.LogTrace("Navigation buttons updated: CanGoPrevious={Prev}, CanGoNext={Next}",
            _pageManager.CanGoPrevious, _pageManager.CanGoNext);
    }

    /// <summary>
    /// Rebuilds page number buttons with intelligent range calculation.
    /// Shows max 5 page numbers, centered around current page.
    ///
    /// ALGORITHM:
    /// - Total <= 5: Show all pages
    /// - Total > 5: Show 5 pages centered around current (2 before, current, 2 after)
    /// - Adjust range when near start/end to always show 5 pages
    /// </summary>
    private void RebuildPageNumbers(int currentPage, int totalPages)
    {
        if (_pageNumbersPanel == null)
            return;

        _pageNumbersPanel.Children.Clear();

        var (startPage, endPage) = GetVisiblePageRange(currentPage, totalPages);

        _logger?.LogTrace("Rebuilding page numbers: Current={Current}, Total={Total}, VisibleRange={Start}-{End}",
            currentPage + 1, totalPages, startPage + 1, endPage + 1);

        for (int page = startPage; page <= endPage; page++)
        {
            var pageButton = CreatePageNumberButton(page, page == currentPage);
            _pageNumbersPanel.Children.Add(pageButton);
        }
    }

    /// <summary>
    /// Creates a page number button.
    /// Current page is highlighted and disabled.
    /// </summary>
    private Button CreatePageNumberButton(int pageIndex, bool isCurrent)
    {
        var button = new Button
        {
            Content = (pageIndex + 1).ToString(), // Display 1-based page numbers
            MinWidth = 40,
            Height = 32,
            Padding = new Thickness(8, 4, 8, 4),
            Tag = pageIndex
        };

        if (isCurrent)
        {
            // Highlight current page
            button.IsEnabled = false;
            button.Opacity = 1.0;
            button.Background = new SolidColorBrush(Microsoft.UI.Colors.LightBlue);
            button.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
        }
        else
        {
            button.Click += (s, e) => NavigateToPage(pageIndex);
        }

        return button;
    }

    /// <summary>
    /// Updates page info text (e.g., "Page 1 of 3 (250 rows)").
    /// </summary>
    private void UpdatePageInfoText(int currentPage, int totalPages, long totalDataRows)
    {
        if (_pageInfoText == null)
            return;

        var (startIndex, count) = _pageManager.GetCurrentPageRange();
        var endIndex = startIndex + count - 1;

        _pageInfoText.Text = $"Page {currentPage + 1} of {totalPages} (rows {startIndex + 1}-{endIndex + 1} of {totalDataRows})";
    }

    /// <summary>
    /// Calculates the visible page number range for pagination UI.
    /// Shows max 5 page numbers, centered around current page.
    ///
    /// EXAMPLES:
    /// - Total 3, Current 0 → Range 0-2 (show: 1 2 3)
    /// - Total 10, Current 0 → Range 0-4 (show: 1 2 3 4 5)
    /// - Total 10, Current 3 → Range 1-5 (show: 2 3 4 5 6)
    /// - Total 10, Current 7 → Range 5-9 (show: 6 7 8 9 10)
    /// </summary>
    private static (int startPage, int endPage) GetVisiblePageRange(int currentPage, int totalPages)
    {
        const int MAX_VISIBLE_PAGES = 5;

        if (totalPages <= MAX_VISIBLE_PAGES)
        {
            // Show all pages if total <= 5
            return (0, totalPages - 1);
        }

        // Center current page in visible range (show 2 before, current, 2 after)
        int startPage = Math.Max(0, currentPage - 2);
        int endPage = Math.Min(totalPages - 1, startPage + MAX_VISIBLE_PAGES - 1);

        // Adjust start if we're near the end
        if (endPage - startPage < MAX_VISIBLE_PAGES - 1)
        {
            startPage = Math.Max(0, endPage - MAX_VISIBLE_PAGES + 1);
        }

        return (startPage, endPage);
    }

    #region Navigation Methods

    private void NavigateToFirstPage()
    {
        _logger?.LogDebug("User clicked: Navigate to first page");
        _pageManager.GoToPage(0);
    }

    private void NavigateToPreviousPage()
    {
        _logger?.LogDebug("User clicked: Navigate to previous page");
        _pageManager.PreviousPage();
    }

    private void NavigateToPage(int pageIndex)
    {
        _logger?.LogDebug("User clicked: Navigate to page {Page}", pageIndex + 1);
        _pageManager.GoToPage(pageIndex);
    }

    private void NavigateToNextPage()
    {
        _logger?.LogDebug("User clicked: Navigate to next page");
        _pageManager.NextPage();
    }

    private void NavigateToLastPage()
    {
        _logger?.LogDebug("User clicked: Navigate to last page");
        _pageManager.GoToPage(_pageManager.TotalPages - 1);
    }

    #endregion

    /// <summary>
    /// Handles PageManager PageChanged event.
    /// Automatically updates UI when page changes.
    /// </summary>
    private void OnPageChanged(object? sender, PageChangedEventArgs e)
    {
        _logger?.LogInformation("Page changed: {OldPage} → {NewPage} (TotalPages={Total})",
            e.OldPage + 1, e.NewPage + 1, _pageManager.TotalPages);

        UpdatePaginationUI();
    }

    /// <summary>
    /// ✅ SENIOR FIX: Handles PageSizeChanged event.
    /// CRITICAL: PageManager fires this event when TotalDataRows changes (not just PageSize!)
    /// This ensures pagination UI becomes visible when data is first loaded.
    /// Example: TotalDataRows 0→100, TotalPages 0→7 → pagination should appear
    /// </summary>
    private void OnPageSizeOrTotalDataChanged(object? sender, int newPageSize)
    {
        _logger?.LogInformation("PageSize or TotalDataRows changed: PageSize={PageSize}, TotalPages={TotalPages}, TotalDataRows={TotalDataRows}",
            newPageSize, _pageManager.TotalPages, _pageManager.TotalDataRows);

        UpdatePaginationUI();
    }

    /// <summary>
    /// Disposes pagination control and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _pageManager.PageChanged -= OnPageChanged;
        _pageManager.PageSizeChanged -= OnPageSizeOrTotalDataChanged;  // ✅ SENIOR FIX: Proper cleanup
        _disposed = true;

        _logger?.LogDebug("PaginationControlView disposed");
    }
}

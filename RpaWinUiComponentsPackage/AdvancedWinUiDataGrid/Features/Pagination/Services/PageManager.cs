using System;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Pagination.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Pagination.Services;

/// <summary>
/// Implementation of IPageManager for DataGrid pagination.
/// ARCHITECTURE: Separates "data count" from "viewport row count" for virtual row management.
/// </summary>
public sealed class PageManager : IPageManager
{
    private readonly ILogger<PageManager> _logger;
    private int _currentPage = 0;
    private int _pageSize = 20;
    private long _totalDataRows = 0;

    public PageManager(ILogger<PageManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("PageManager created with PageSize={PageSize}", _pageSize);
    }

    #region Properties

    public int CurrentPage => _currentPage;

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "PageSize must be greater than 0");

            if (_pageSize != value)
            {
                var oldPageSize = _pageSize;
                _pageSize = value;

                _logger.LogInformation("PageSize changed from {OldSize} to {NewSize}", oldPageSize, _pageSize);

                // Adjust current page to keep similar data visible
                var oldStartIndex = oldPageSize * _currentPage;
                _currentPage = (int)(oldStartIndex / _pageSize);

                // Ensure current page is within bounds
                if (_currentPage >= TotalPages)
                    _currentPage = Math.Max(0, TotalPages - 1);

                PageSizeChanged?.Invoke(this, _pageSize);
                RaisePageChanged(_currentPage, _currentPage); // Notify with same page but different range
            }
        }
    }

    public long TotalDataRows => _totalDataRows;

    public int TotalPages
    {
        get
        {
            if (_totalDataRows == 0)
                return 0;

            return (int)Math.Ceiling((double)_totalDataRows / _pageSize);
        }
    }

    public bool CanGoPrevious => _currentPage > 0;

    public bool CanGoNext => _currentPage < TotalPages - 1;

    #endregion

    #region Events

    public event EventHandler<PageChangedEventArgs>? PageChanged;
    public event EventHandler<int>? PageSizeChanged;

    #endregion

    #region Public Methods

    public void SetTotalDataRows(long totalRows)
    {
        if (totalRows < 0)
            throw new ArgumentOutOfRangeException(nameof(totalRows), "TotalDataRows cannot be negative");

        var oldTotal = _totalDataRows;
        _totalDataRows = totalRows;

        _logger.LogDebug("TotalDataRows changed from {OldTotal} to {NewTotal}, TotalPages={TotalPages}",
            oldTotal, _totalDataRows, TotalPages);

        // If current page is now out of bounds, move to last page
        if (_currentPage >= TotalPages && TotalPages > 0)
        {
            var oldPage = _currentPage;
            _currentPage = TotalPages - 1;
            _logger.LogDebug("Current page {OldPage} out of bounds, moved to last page {NewPage}",
                oldPage, _currentPage);
            RaisePageChanged(oldPage, _currentPage);
        }
        else if (_currentPage > 0 && TotalPages == 0)
        {
            // All data removed, reset to page 0
            var oldPage = _currentPage;
            _currentPage = 0;
            _logger.LogDebug("All data removed, reset to page 0 from page {OldPage}", oldPage);
            RaisePageChanged(oldPage, _currentPage);
        }
    }

    public bool GoToPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= TotalPages)
        {
            _logger.LogWarning("Invalid page index {PageIndex}, valid range is 0 to {MaxPage}",
                pageIndex, TotalPages - 1);
            return false;
        }

        if (_currentPage == pageIndex)
        {
            _logger.LogTrace("Already on page {PageIndex}", pageIndex);
            return true;
        }

        var oldPage = _currentPage;
        _currentPage = pageIndex;

        _logger.LogInformation("Navigated from page {OldPage} to page {NewPage}", oldPage, _currentPage);

        RaisePageChanged(oldPage, _currentPage);
        return true;
    }

    public bool NextPage()
    {
        if (!CanGoNext)
        {
            _logger.LogTrace("Cannot go to next page, already at last page {CurrentPage}", _currentPage);
            return false;
        }

        return GoToPage(_currentPage + 1);
    }

    public bool PreviousPage()
    {
        if (!CanGoPrevious)
        {
            _logger.LogTrace("Cannot go to previous page, already at first page");
            return false;
        }

        return GoToPage(_currentPage - 1);
    }

    public bool FirstPage()
    {
        return GoToPage(0);
    }

    public bool LastPage()
    {
        if (TotalPages == 0)
            return false;

        return GoToPage(TotalPages - 1);
    }

    public (long startIndex, int count) GetCurrentPageRange()
    {
        return GetPageRange(_currentPage);
    }

    public (long startIndex, int count) GetPageRange(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= TotalPages)
        {
            _logger.LogWarning("Invalid page index {PageIndex} in GetPageRange", pageIndex);
            return (0, 0);
        }

        long startIndex = (long)pageIndex * _pageSize;
        long remainingRows = _totalDataRows - startIndex;
        int count = (int)Math.Min(_pageSize, remainingRows);

        return (startIndex, count);
    }

    public string GetPageDisplayText()
    {
        if (TotalPages == 0)
            return "No data";

        return $"Page {_currentPage + 1} of {TotalPages}";
    }

    public void Reset()
    {
        _logger.LogInformation("Resetting pagination to first page");

        var oldPage = _currentPage;
        _currentPage = 0;

        if (oldPage != 0)
        {
            RaisePageChanged(oldPage, _currentPage);
        }
    }

    #endregion

    #region Private Methods

    private void RaisePageChanged(int oldPage, int newPage)
    {
        var (startIndex, count) = GetCurrentPageRange();

        var args = new PageChangedEventArgs
        {
            OldPage = oldPage,
            NewPage = newPage,
            StartIndex = startIndex,
            Count = count
        };

        _logger.LogTrace("Raising PageChanged event: OldPage={OldPage}, NewPage={NewPage}, StartIndex={StartIndex}, Count={Count}",
            oldPage, newPage, startIndex, count);

        PageChanged?.Invoke(this, args);
    }

    #endregion
}

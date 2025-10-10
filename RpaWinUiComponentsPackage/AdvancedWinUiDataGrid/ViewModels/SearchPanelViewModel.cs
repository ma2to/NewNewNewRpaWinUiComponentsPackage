using Microsoft.UI.Xaml;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// ViewModel for the search panel
/// </summary>
public sealed class SearchPanelViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private bool _isVisible = true;
    private bool _caseSensitive;
    private bool _showSearchInFilteredOnlyButton;
    private bool _searchInFilteredOnly;

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public Visibility Visibility => IsVisible ? Visibility.Visible : Visibility.Collapsed;

    public bool CaseSensitive
    {
        get => _caseSensitive;
        set => SetProperty(ref _caseSensitive, value);
    }

    public bool ShowSearchInFilteredOnlyButton
    {
        get => _showSearchInFilteredOnlyButton;
        set => SetProperty(ref _showSearchInFilteredOnlyButton, value);
    }

    public Visibility SearchInFilteredOnlyButtonVisibility => ShowSearchInFilteredOnlyButton ? Visibility.Visible : Visibility.Collapsed;

    public bool SearchInFilteredOnly
    {
        get => _searchInFilteredOnly;
        set => SetProperty(ref _searchInFilteredOnly, value);
    }

    public void ClearSearch()
    {
        SearchText = string.Empty;
    }
}

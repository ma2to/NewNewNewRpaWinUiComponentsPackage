using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// ViewModel for the filter row
/// Contains filter TextBoxes for each column
/// </summary>
public sealed class FilterRowViewModel : ViewModelBase
{
    private bool _isVisible;

    public ObservableCollection<ColumnFilterViewModel> ColumnFilters { get; } = new();

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public Visibility Visibility => IsVisible ? Visibility.Visible : Visibility.Collapsed;

    public void ClearAllFilters()
    {
        foreach (var filter in ColumnFilters)
        {
            filter.FilterText = string.Empty;
        }
    }
}

/// <summary>
/// ViewModel for a single column filter TextBox
/// </summary>
public sealed class ColumnFilterViewModel : ViewModelBase
{
    private string _columnName = string.Empty;
    private string _filterText = string.Empty;
    private double _width = 100;

    public string ColumnName
    {
        get => _columnName;
        set => SetProperty(ref _columnName, value);
    }

    public string FilterText
    {
        get => _filterText;
        set => SetProperty(ref _filterText, value);
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }
}

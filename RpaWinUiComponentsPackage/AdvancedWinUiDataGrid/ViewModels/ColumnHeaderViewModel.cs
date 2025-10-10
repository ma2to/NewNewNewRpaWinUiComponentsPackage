namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// ViewModel for a column header
/// Supports resizing, sorting, and selection
/// </summary>
public sealed class ColumnHeaderViewModel : ViewModelBase
{
    private string _columnName = string.Empty;
    private string _displayName = string.Empty;
    private double _width = 100;
    private bool _isResizing;
    private bool _isSelected;
    private string _sortDirection = "None"; // None, Ascending, Descending

    public string ColumnName
    {
        get => _columnName;
        set => SetProperty(ref _columnName, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public bool IsResizing
    {
        get => _isResizing;
        set => SetProperty(ref _isResizing, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string SortDirection
    {
        get => _sortDirection;
        set => SetProperty(ref _sortDirection, value);
    }
}

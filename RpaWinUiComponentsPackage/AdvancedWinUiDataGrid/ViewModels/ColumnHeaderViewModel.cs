using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// ViewModel for a column header
/// Supports resizing, sorting, selection, and special column types
/// </summary>
public sealed class ColumnHeaderViewModel : ViewModelBase
{
    private string _columnName = string.Empty;
    private string _displayName = string.Empty;
    private double _width = 100;
    private bool _isResizing;
    private bool _isSelected;
    private string _sortDirection = "None"; // None, Ascending, Descending
    private SpecialColumnType _specialType = SpecialColumnType.None;
    private bool _isResizable = true;
    private int _displayOrder = 0;

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

    /// <summary>
    /// Gets or sets the type of special column (None for normal data columns)
    /// </summary>
    public SpecialColumnType SpecialType
    {
        get => _specialType;
        set => SetProperty(ref _specialType, value);
    }

    /// <summary>
    /// Gets whether this is a special column (not a normal data column)
    /// </summary>
    public bool IsSpecialColumn => SpecialType != SpecialColumnType.None;

    /// <summary>
    /// Gets or sets whether this column can be resized by the user
    /// Special columns like RowNumber, Checkbox, DeleteRow are typically not resizable
    /// </summary>
    public bool IsResizable
    {
        get => _isResizable;
        set => SetProperty(ref _isResizable, value);
    }

    /// <summary>
    /// Gets or sets the display order of this column in the grid
    /// Lower values appear first (leftmost)
    /// </summary>
    public int DisplayOrder
    {
        get => _displayOrder;
        set => SetProperty(ref _displayOrder, value);
    }
}

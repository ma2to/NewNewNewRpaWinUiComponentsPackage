using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Models;

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

    private bool _useAutoWidth;

    /// <summary>
    /// Gets or sets whether this column should use auto-width (Star sizing)
    /// Used primarily for ValidationAlerts column which auto-expands by default
    /// After manual resize, this is set to false to use fixed width instead
    /// </summary>
    public bool UseAutoWidth
    {
        get => _useAutoWidth;
        set => SetProperty(ref _useAutoWidth, value);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // FILTER FLYOUT PROPERTIES (Excel-like filtering with checkbox + regex support)
    // ═══════════════════════════════════════════════════════════════════════════════

    private MenuFlyout? _filterFlyout;
    private ObservableCollection<FilterValueItem> _uniqueValues = new();
    private bool _hasActiveFilter;
    private string _filterSearchText = string.Empty;
    private string _filterRegexText = string.Empty;
    private FilterMode _filterMode = FilterMode.Checkbox;

    /// <summary>
    /// The filter flyout attached to this column header
    /// Opens when user clicks the filter icon in the header
    /// </summary>
    public MenuFlyout? FilterFlyout
    {
        get => _filterFlyout;
        set => SetProperty(ref _filterFlyout, value);
    }

    /// <summary>
    /// Collection of unique values for checkbox filter mode
    /// Populated from SQL: SELECT DISTINCT column_value FROM grid_rows
    /// </summary>
    public ObservableCollection<FilterValueItem> UniqueValues
    {
        get => _uniqueValues;
        set => SetProperty(ref _uniqueValues, value);
    }

    /// <summary>
    /// Indicates whether this column has an active filter applied
    /// Used to show filter badge/indicator in column header
    /// </summary>
    public bool HasActiveFilter
    {
        get => _hasActiveFilter;
        set => SetProperty(ref _hasActiveFilter, value);
    }

    /// <summary>
    /// Search text for filtering the unique values list in checkbox mode
    /// Does NOT filter data - only filters the checkbox list itself
    /// Example: User types "test" → shows only unique values containing "test"
    /// </summary>
    public string FilterSearchText
    {
        get => _filterSearchText;
        set => SetProperty(ref _filterSearchText, value);
    }

    /// <summary>
    /// Regex pattern for regex filter mode
    /// Applied to data via SQL: WHERE column_value REGEXP 'pattern'
    /// Example: "^test.*|.*data$" matches rows starting with "test" or ending with "data"
    /// </summary>
    public string FilterRegexText
    {
        get => _filterRegexText;
        set => SetProperty(ref _filterRegexText, value);
    }

    /// <summary>
    /// Current filter mode: Checkbox (SQL WHERE IN) or Regex (SQL WHERE REGEXP)
    /// Default: Checkbox (most common use case)
    /// </summary>
    public FilterMode FilterMode
    {
        get => _filterMode;
        set => SetProperty(ref _filterMode, value);
    }

    /// <summary>
    /// Helper property: Is filter mode Checkbox?
    /// Used for conditional visibility in XAML (checkbox list, "Select All" button)
    /// </summary>
    public bool IsCheckboxMode => FilterMode == FilterMode.Checkbox;

    /// <summary>
    /// Helper property: Is filter mode Regex?
    /// Used for conditional visibility in XAML (regex input field)
    /// </summary>
    public bool IsRegexMode => FilterMode == FilterMode.Regex;

    // ═══════════════════════════════════════════════════════════════════════════════
    // 3-STATE CHECKBOX HEADER PROPERTIES (for SpecialColumnType.Checkbox)
    // ═══════════════════════════════════════════════════════════════════════════════

    private bool? _isCheckboxHeaderChecked;

    /// <summary>
    /// 3-state checkbox header state (for SpecialColumnType.Checkbox columns)
    /// - true: All rows are checked (Full checkmark ✓)
    /// - false: No rows are checked (Empty checkbox ☐)
    /// - null: Some rows are checked, some are not (Indeterminate ■)
    /// </summary>
    public bool? IsCheckboxHeaderChecked
    {
        get => _isCheckboxHeaderChecked;
        set => SetProperty(ref _isCheckboxHeaderChecked, value);
    }
}

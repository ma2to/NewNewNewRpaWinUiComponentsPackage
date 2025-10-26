using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Models;

/// <summary>
/// Represents a single unique value in the filter flyout checkbox list
/// Used for Checkbox filter mode (SQL WHERE IN (...))
/// </summary>
public sealed class FilterValueItem : ViewModelBase
{
    private string _value = string.Empty;
    private bool _isSelected;
    private int _count;

    /// <summary>
    /// The unique value (display text)
    /// Example: "Active", "Pending", "Completed"
    /// </summary>
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    /// <summary>
    /// Whether this value is selected (checked) in the filter list
    /// When true, this value is included in SQL WHERE IN (...)
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Number of rows with this value (optional, for display)
    /// Example: "Active (142)" shows count next to value
    /// </summary>
    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

    /// <summary>
    /// Display text with optional count
    /// Example: "Active (142)" if ShowCount=true, otherwise just "Active"
    /// </summary>
    public string DisplayText => Count > 0 ? $"{Value} ({Count})" : Value;
}

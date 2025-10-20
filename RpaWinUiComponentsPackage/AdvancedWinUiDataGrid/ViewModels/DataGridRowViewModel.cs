using System.Collections.ObjectModel;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// ViewModel for a single row in the data grid
/// Contains collection of cell ViewModels
/// </summary>
public sealed class DataGridRowViewModel : ViewModelBase
{
    private bool _isSelected;
    private bool _hasValidationErrors;

    public int RowIndex { get; set; }

    /// <summary>
    /// Gets or sets the unique row ID (from __rowId field in data).
    /// This ID is stable across row operations (delete, sort, filter).
    /// </summary>
    public string? RowId { get; set; }

    public ObservableCollection<CellViewModel> Cells { get; } = new();

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool HasValidationErrors
    {
        get => _hasValidationErrors;
        set => SetProperty(ref _hasValidationErrors, value);
    }

    /// <summary>
    /// Select or deselect all cells in this row
    /// </summary>
    public void SetRowSelection(bool isSelected)
    {
        IsSelected = isSelected;
        foreach (var cell in Cells)
        {
            cell.IsSelected = isSelected;
        }
    }
}

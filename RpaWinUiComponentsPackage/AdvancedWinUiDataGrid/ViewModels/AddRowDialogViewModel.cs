using System.Collections.ObjectModel;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// ViewModel for Add Row modal dialog.
/// Manages collection of field ViewModels and overall validation state.
/// </summary>
internal sealed class AddRowDialogViewModel : ViewModelBase
{
    /// <summary>
    /// Collection of field ViewModels (one per column)
    /// </summary>
    public ObservableCollection<AddRowFieldViewModel> ColumnFields { get; }

    /// <summary>
    /// True if all fields are valid (no validation errors)
    /// </summary>
    public bool IsAllValid => ColumnFields.All(f => !f.HasError);

    public AddRowDialogViewModel(
        IEnumerable<string> columnNames,
        IReadOnlyDictionary<string, object?>? defaultValues = null)
    {
        ColumnFields = new ObservableCollection<AddRowFieldViewModel>();

        foreach (var columnName in columnNames)
        {
            var defaultValue = defaultValues?.TryGetValue(columnName, out var val) == true
                ? val?.ToString() ?? ""
                : "";

            var field = new AddRowFieldViewModel(columnName, defaultValue);
            field.PropertyChanged += Field_PropertyChanged;

            ColumnFields.Add(field);
        }
    }

    private void Field_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AddRowFieldViewModel.HasError))
        {
            OnPropertyChanged(nameof(IsAllValid));
        }
    }

    /// <summary>
    /// Get row data from all fields
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetRowData()
    {
        var rowData = new Dictionary<string, object?>();

        foreach (var field in ColumnFields)
        {
            rowData[field.ColumnName] = field.Value;
        }

        return rowData;
    }
}

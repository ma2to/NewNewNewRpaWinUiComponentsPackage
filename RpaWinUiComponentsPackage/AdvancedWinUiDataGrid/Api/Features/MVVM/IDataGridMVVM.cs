using System.ComponentModel;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.MVVM;

/// <summary>
/// Public interface for DataGrid MVVM support.
/// Provides ViewModel binding, property change notifications, and command support.
/// </summary>
public interface IDataGridMVVM
{
    /// <summary>
    /// Binds grid to a ViewModel collection.
    /// </summary>
    /// <param name="viewModelCollection">ViewModel collection to bind</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> BindToViewModelAsync(IEnumerable<INotifyPropertyChanged> viewModelCollection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unbinds current ViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> UnbindViewModelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current bound ViewModel collection.
    /// </summary>
    /// <returns>Bound ViewModel collection or null</returns>
    IEnumerable<INotifyPropertyChanged>? GetBoundViewModel();

    /// <summary>
    /// Refreshes grid from ViewModel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> RefreshFromViewModelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates ViewModel from grid data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> UpdateViewModelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables two-way binding between grid and ViewModel.
    /// </summary>
    /// <param name="enabled">True to enable two-way binding</param>
    /// <returns>Result of the operation</returns>
    PublicResult SetTwoWayBindingEnabled(bool enabled);

    /// <summary>
    /// Checks if two-way binding is enabled.
    /// </summary>
    /// <returns>True if two-way binding is enabled</returns>
    bool IsTwoWayBindingEnabled();

    /// <summary>
    /// Checks if grid is bound to a ViewModel.
    /// </summary>
    /// <returns>True if bound to ViewModel</returns>
    bool IsBoundToViewModel();

    // MVVM Transformations

    /// <summary>
    /// Adapts raw row data to UI-friendly view model for MVVM binding.
    /// </summary>
    /// <param name="rowData">Raw row data</param>
    /// <param name="rowIndex">Row index</param>
    /// <returns>Public row view model</returns>
    PublicRowViewModel AdaptToRowViewModel(IReadOnlyDictionary<string, object?> rowData, int rowIndex);

    /// <summary>
    /// Adapts multiple rows to view models for MVVM binding.
    /// </summary>
    /// <param name="rows">Collection of row data</param>
    /// <param name="startIndex">Starting index for rows</param>
    /// <returns>Collection of public row view models</returns>
    IReadOnlyList<PublicRowViewModel> AdaptToRowViewModels(IEnumerable<IReadOnlyDictionary<string, object?>> rows, int startIndex = 0);

    /// <summary>
    /// Adapts column definition to UI-friendly view model for MVVM binding.
    /// </summary>
    /// <param name="columnDefinition">Column definition</param>
    /// <returns>Public column view model</returns>
    PublicColumnViewModel AdaptToColumnViewModel(PublicColumnDefinition columnDefinition);

    /// <summary>
    /// Adapts validation errors to UI-friendly view models.
    /// This is primarily a convenience method for transforming collections.
    /// </summary>
    /// <param name="errors">Collection of validation errors</param>
    /// <returns>Read-only collection of validation error view models</returns>
    IReadOnlyList<PublicValidationErrorViewModel> AdaptValidationErrors(IReadOnlyList<PublicValidationErrorViewModel> errors);

    // Business Presets

    /// <summary>
    /// Creates employee hierarchy sort preset (Department → Position → Salary).
    /// </summary>
    /// <param name="departmentColumn">Department column name</param>
    /// <param name="positionColumn">Position column name</param>
    /// <param name="salaryColumn">Salary column name</param>
    /// <returns>Sort configuration preset</returns>
    PublicSortConfiguration CreateEmployeeHierarchySortPreset(string departmentColumn = "Department", string positionColumn = "Position", string salaryColumn = "Salary");

    /// <summary>
    /// Creates customer priority sort preset (Tier → Value → JoinDate).
    /// </summary>
    /// <param name="tierColumn">Tier column name</param>
    /// <param name="valueColumn">Value column name</param>
    /// <param name="joinDateColumn">Join date column name</param>
    /// <returns>Sort configuration preset</returns>
    PublicSortConfiguration CreateCustomerPrioritySortPreset(string tierColumn = "CustomerTier", string valueColumn = "TotalValue", string joinDateColumn = "JoinDate");

    /// <summary>
    /// Gets responsive row height preset.
    /// </summary>
    /// <returns>Auto row height configuration preset</returns>
    PublicAutoRowHeightConfiguration GetResponsiveHeightPreset();

    /// <summary>
    /// Gets compact row height preset.
    /// </summary>
    /// <returns>Auto row height configuration preset</returns>
    PublicAutoRowHeightConfiguration GetCompactHeightPreset();

    /// <summary>
    /// Gets performance row height preset.
    /// </summary>
    /// <returns>Auto row height configuration preset</returns>
    PublicAutoRowHeightConfiguration GetPerformanceHeightPreset();
}

using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
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
}

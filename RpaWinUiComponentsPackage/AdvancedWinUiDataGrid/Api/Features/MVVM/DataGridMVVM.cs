using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;
using System.ComponentModel;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.MVVM;

/// <summary>
/// Internal implementation of DataGrid MVVM support.
/// Delegates to internal MVVM service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridMVVM : IDataGridMVVM
{
    private readonly ILogger<DataGridMVVM>? _logger;
    private readonly GridViewModelAdapter _gridViewModelAdapter;

    public DataGridMVVM(
        GridViewModelAdapter gridViewModelAdapter,
        ILogger<DataGridMVVM>? logger = null)
    {
        _gridViewModelAdapter = gridViewModelAdapter ?? throw new ArgumentNullException(nameof(gridViewModelAdapter));
        _logger = logger;
    }

    public async Task<PublicResult> BindToViewModelAsync(IEnumerable<INotifyPropertyChanged> viewModelCollection, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Binding to ViewModel via MVVM module");

            await _gridViewModelAdapter.BindToViewModelAsync(viewModelCollection, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BindToViewModel failed in MVVM module");
            throw;
        }
    }

    public async Task<PublicResult> UnbindViewModelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Unbinding ViewModel via MVVM module");

            await _gridViewModelAdapter.UnbindViewModelAsync(cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UnbindViewModel failed in MVVM module");
            throw;
        }
    }

    public IEnumerable<INotifyPropertyChanged>? GetBoundViewModel()
    {
        try
        {
            var viewModel = _gridViewModelAdapter.GetBoundViewModel();
            return viewModel as IEnumerable<INotifyPropertyChanged>;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetBoundViewModel failed in MVVM module");
            throw;
        }
    }

    public async Task<PublicResult> RefreshFromViewModelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Refreshing from ViewModel via MVVM module");

            await _gridViewModelAdapter.RefreshFromViewModelAsync(cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RefreshFromViewModel failed in MVVM module");
            throw;
        }
    }

    public async Task<PublicResult> UpdateViewModelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Updating ViewModel via MVVM module");

            await _gridViewModelAdapter.UpdateViewModelAsync(cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UpdateViewModel failed in MVVM module");
            throw;
        }
    }

    public PublicResult SetTwoWayBindingEnabled(bool enabled)
    {
        try
        {
            _logger?.LogInformation("Setting two-way binding enabled to {Enabled} via MVVM module", enabled);

            _gridViewModelAdapter.SetTwoWayBindingEnabled(enabled);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetTwoWayBindingEnabled failed in MVVM module");
            throw;
        }
    }

    public bool IsTwoWayBindingEnabled()
    {
        try
        {
            return _gridViewModelAdapter.IsTwoWayBindingEnabled();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsTwoWayBindingEnabled check failed in MVVM module");
            throw;
        }
    }

    public bool IsBoundToViewModel()
    {
        try
        {
            return _gridViewModelAdapter.IsBoundToViewModel();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsBoundToViewModel check failed in MVVM module");
            throw;
        }
    }
}

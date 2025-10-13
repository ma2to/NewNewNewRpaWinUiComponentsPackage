using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;
using System.ComponentModel;
using System.Data;

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

    // MVVM Transformations

    public PublicRowViewModel AdaptToRowViewModel(IReadOnlyDictionary<string, object?> rowData, int rowIndex)
    {
        if (rowData == null)
            throw new ArgumentNullException(nameof(rowData));

        try
        {
            // Use internal adapter to create internal view model
            var internalViewModel = _gridViewModelAdapter.AdaptToRowViewModel(rowData, rowIndex);

            // Transform to public view model
            return new PublicRowViewModel
            {
                Index = internalViewModel.Index,
                IsSelected = internalViewModel.IsSelected,
                IsValid = internalViewModel.IsValid,
                ValidationErrors = internalViewModel.ValidationErrors != null
                    ? internalViewModel.ValidationErrors.ToList().AsReadOnly()
                    : Array.Empty<string>(),
                ValidationErrorDetails = Array.Empty<PublicValidationErrorViewModel>(),
                CellValues = internalViewModel.CellValues != null
                    ? new Dictionary<string, object?>(internalViewModel.CellValues)
                    : new Dictionary<string, object?>()
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AdaptToRowViewModel failed in MVVM module");
            throw;
        }
    }

    public IReadOnlyList<PublicRowViewModel> AdaptToRowViewModels(IEnumerable<IReadOnlyDictionary<string, object?>> rows, int startIndex = 0)
    {
        if (rows == null)
            throw new ArgumentNullException(nameof(rows));

        try
        {
            var viewModels = new List<PublicRowViewModel>();
            var currentIndex = startIndex;

            foreach (var row in rows)
            {
                var viewModel = AdaptToRowViewModel(row, currentIndex);
                viewModels.Add(viewModel);
                currentIndex++;
            }

            return viewModels.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AdaptToRowViewModels failed in MVVM module");
            throw;
        }
    }

    public PublicColumnViewModel AdaptToColumnViewModel(PublicColumnDefinition columnDefinition)
    {
        if (columnDefinition == null)
            throw new ArgumentNullException(nameof(columnDefinition));

        try
        {
            // Convert public column definition to internal
            var internalColumnDef = columnDefinition.ToInternal();

            // Use internal adapter to create internal view model
            var internalViewModel = _gridViewModelAdapter.AdaptToColumnViewModel(internalColumnDef);

            // Transform to public view model
            return new PublicColumnViewModel
            {
                Name = internalViewModel.Name,
                DisplayName = internalViewModel.DisplayName,
                IsVisible = internalViewModel.IsVisible,
                Width = internalViewModel.Width,
                IsReadOnly = internalViewModel.IsReadOnly,
                DataType = internalViewModel.DataType,
                SortDirection = internalViewModel.SortDirection
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AdaptToColumnViewModel failed in MVVM module");
            throw;
        }
    }

    public IReadOnlyList<PublicValidationErrorViewModel> AdaptValidationErrors(IReadOnlyList<PublicValidationErrorViewModel> errors)
    {
        if (errors == null)
            throw new ArgumentNullException(nameof(errors));

        try
        {
            // Return as readonly list (already in correct format)
            return errors.ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AdaptValidationErrors failed in MVVM module");
            throw;
        }
    }

    // Business Presets

    public PublicSortConfiguration CreateEmployeeHierarchySortPreset(string departmentColumn = "Department", string positionColumn = "Position", string salaryColumn = "Salary")
    {
        try
        {
            _logger?.LogDebug("Creating employee hierarchy sort preset with columns: {Department}, {Position}, {Salary}",
                departmentColumn, positionColumn, salaryColumn);

            var internalConfig = Core.ValueObjects.AdvancedSortConfiguration.CreateEmployeeHierarchy(
                departmentColumn, positionColumn, salaryColumn);

            return MapToPublicSortConfiguration(internalConfig);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CreateEmployeeHierarchySortPreset failed in MVVM module");
            throw;
        }
    }

    public PublicSortConfiguration CreateCustomerPrioritySortPreset(string tierColumn = "CustomerTier", string valueColumn = "TotalValue", string joinDateColumn = "JoinDate")
    {
        try
        {
            _logger?.LogDebug("Creating customer priority sort preset with columns: {Tier}, {Value}, {JoinDate}",
                tierColumn, valueColumn, joinDateColumn);

            var internalConfig = Core.ValueObjects.AdvancedSortConfiguration.CreateCustomerPriority(
                tierColumn, valueColumn, joinDateColumn);

            return MapToPublicSortConfiguration(internalConfig);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CreateCustomerPrioritySortPreset failed in MVVM module");
            throw;
        }
    }

    public PublicAutoRowHeightConfiguration GetResponsiveHeightPreset()
    {
        try
        {
            _logger?.LogDebug("Getting responsive height preset via MVVM module");

            var internalConfig = Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration.Responsive;
            return MapToPublicAutoRowHeightConfiguration(internalConfig);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetResponsiveHeightPreset failed in MVVM module");
            throw;
        }
    }

    public PublicAutoRowHeightConfiguration GetCompactHeightPreset()
    {
        try
        {
            _logger?.LogDebug("Getting compact height preset via MVVM module");

            var internalConfig = Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration.Compact;
            return MapToPublicAutoRowHeightConfiguration(internalConfig);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetCompactHeightPreset failed in MVVM module");
            throw;
        }
    }

    public PublicAutoRowHeightConfiguration GetPerformanceHeightPreset()
    {
        try
        {
            _logger?.LogDebug("Getting performance height preset via MVVM module");

            var internalConfig = Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration.Performance;
            return MapToPublicAutoRowHeightConfiguration(internalConfig);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetPerformanceHeightPreset failed in MVVM module");
            throw;
        }
    }

    // Private mapping helpers

    private static PublicSortConfiguration MapToPublicSortConfiguration(Core.ValueObjects.AdvancedSortConfiguration internalConfig)
    {
        return new PublicSortConfiguration
        {
            ConfigurationName = internalConfig.ConfigurationName,
            SortColumns = internalConfig.SortColumns
                .Select(col => new PublicSortColumn
                {
                    ColumnName = col.ColumnName,
                    Direction = col.Direction.ToString(),
                    Priority = col.Priority
                })
                .ToList()
                .AsReadOnly(),
            PerformanceMode = internalConfig.PerformanceMode.ToString(),
            EnableParallelProcessing = internalConfig.EnableParallelProcessing,
            MaxSortColumns = internalConfig.MaxSortColumns,
            BatchSize = 1000 // Default batch size
        };
    }

    private static PublicAutoRowHeightConfiguration MapToPublicAutoRowHeightConfiguration(Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration internalConfig)
    {
        return new PublicAutoRowHeightConfiguration
        {
            IsEnabled = internalConfig.IsEnabled,
            MinimumRowHeight = internalConfig.MinimumRowHeight,
            MaximumRowHeight = internalConfig.MaximumRowHeight,
            DefaultFontFamily = internalConfig.DefaultFontFamily,
            DefaultFontSize = internalConfig.DefaultFontSize,
            EnableTextWrapping = internalConfig.EnableTextWrapping,
            UseCache = internalConfig.UseCache,
            CacheMaxSize = internalConfig.CacheMaxSize
        };
    }
}

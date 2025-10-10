using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Partial class containing MVVM Transformations and Business Presets
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region MVVM Transformations

    /// <summary>
    /// Adapts raw row data to UI-friendly view model for MVVM binding
    /// </summary>
    public PublicRowViewModel AdaptToRowViewModel(IReadOnlyDictionary<string, object?> rowData, int rowIndex)
    {
        ThrowIfDisposed();

        if (_gridViewModelAdapter == null)
        {
            throw new InvalidOperationException(
                "MVVM transformations are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (rowData == null)
            throw new ArgumentNullException(nameof(rowData));

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
            ValidationErrorDetails = Array.Empty<PublicValidationErrorViewModel>(), // Not populated by internal adapter
            CellValues = internalViewModel.CellValues != null
                ? new Dictionary<string, object?>(internalViewModel.CellValues)
                : new Dictionary<string, object?>()
        };
    }

    /// <summary>
    /// Adapts multiple rows to view models for MVVM binding
    /// </summary>
    public IReadOnlyList<PublicRowViewModel> AdaptToRowViewModels(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex = 0)
    {
        ThrowIfDisposed();

        if (_gridViewModelAdapter == null)
        {
            throw new InvalidOperationException(
                "MVVM transformations are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (rows == null)
            throw new ArgumentNullException(nameof(rows));

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

    /// <summary>
    /// Adapts column definition to UI-friendly view model for MVVM binding
    /// </summary>
    public PublicColumnViewModel AdaptToColumnViewModel(PublicColumnDefinition columnDefinition)
    {
        ThrowIfDisposed();

        if (_gridViewModelAdapter == null)
        {
            throw new InvalidOperationException(
                "MVVM transformations are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (columnDefinition == null)
            throw new ArgumentNullException(nameof(columnDefinition));

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

    /// <summary>
    /// Adapts validation errors to UI-friendly view models
    /// This is primarily a convenience method for transforming collections
    /// </summary>
    public IReadOnlyList<PublicValidationErrorViewModel> AdaptValidationErrors(
        IReadOnlyList<PublicValidationErrorViewModel> errors)
    {
        ThrowIfDisposed();

        if (_gridViewModelAdapter == null)
        {
            throw new InvalidOperationException(
                "MVVM transformations are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (errors == null)
            throw new ArgumentNullException(nameof(errors));

        // Return as readonly list (already in correct format)
        return errors.ToList().AsReadOnly();
    }

    #endregion
    #region Business Presets

    /// <summary>
    /// Creates employee hierarchy sort preset (Department → Position → Salary)
    /// </summary>
    public PublicSortConfiguration CreateEmployeeHierarchySortPreset(
        string departmentColumn = "Department",
        string positionColumn = "Position",
        string salaryColumn = "Salary")
    {
        ThrowIfDisposed();
        _logger.LogDebug("Creating employee hierarchy sort preset with columns: {Department}, {Position}, {Salary}",
            departmentColumn, positionColumn, salaryColumn);

        var internalConfig = Core.ValueObjects.AdvancedSortConfiguration.CreateEmployeeHierarchy(
            departmentColumn, positionColumn, salaryColumn);

        return MapToPublicSortConfiguration(internalConfig);
    }

    /// <summary>
    /// Creates customer priority sort preset (Tier → Value → JoinDate)
    /// </summary>
    public PublicSortConfiguration CreateCustomerPrioritySortPreset(
        string tierColumn = "CustomerTier",
        string valueColumn = "TotalValue",
        string joinDateColumn = "JoinDate")
    {
        ThrowIfDisposed();
        _logger.LogDebug("Creating customer priority sort preset with columns: {Tier}, {Value}, {JoinDate}",
            tierColumn, valueColumn, joinDateColumn);

        var internalConfig = Core.ValueObjects.AdvancedSortConfiguration.CreateCustomerPriority(
            tierColumn, valueColumn, joinDateColumn);

        return MapToPublicSortConfiguration(internalConfig);
    }

    /// <summary>
    /// Gets responsive row height preset
    /// </summary>
    public PublicAutoRowHeightConfiguration GetResponsiveHeightPreset()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Getting responsive height preset");

        var internalConfig = Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration.Responsive;
        return MapToPublicAutoRowHeightConfiguration(internalConfig);
    }

    /// <summary>
    /// Gets compact row height preset
    /// </summary>
    public PublicAutoRowHeightConfiguration GetCompactHeightPreset()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Getting compact height preset");

        var internalConfig = Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration.Compact;
        return MapToPublicAutoRowHeightConfiguration(internalConfig);
    }

    /// <summary>
    /// Gets performance row height preset
    /// </summary>
    public PublicAutoRowHeightConfiguration GetPerformanceHeightPreset()
    {
        ThrowIfDisposed();
        _logger.LogDebug("Getting performance height preset");

        var internalConfig = Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration.Performance;
        return MapToPublicAutoRowHeightConfiguration(internalConfig);
    }

    /// <summary>
    /// Maps internal sort configuration to public type
    /// </summary>
    private static PublicSortConfiguration MapToPublicSortConfiguration(
        Core.ValueObjects.AdvancedSortConfiguration internalConfig)
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

    /// <summary>
    /// Maps internal auto row height configuration to public type
    /// </summary>
    private static PublicAutoRowHeightConfiguration MapToPublicAutoRowHeightConfiguration(
        Features.AutoRowHeight.Interfaces.AutoRowHeightConfiguration internalConfig)
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

    #endregion
}


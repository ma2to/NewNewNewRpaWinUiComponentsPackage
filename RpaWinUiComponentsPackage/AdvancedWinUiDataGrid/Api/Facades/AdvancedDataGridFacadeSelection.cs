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
/// Partial class containing Selection Operations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Selection Operations

    /// <summary>
    /// Starts column resize operation
    /// </summary>
    public double ResizeColumn(int columnIndex, double newWidth)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.ColumnResize, nameof(ResizeColumn));

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            return resizeService.ResizeColumn(columnIndex, newWidth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize column {ColumnIndex}: {Message}", columnIndex, ex.Message);
            throw;
        }
    }

    public void StartColumnResize(int columnIndex, double clientX)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.ColumnResize, nameof(StartColumnResize));

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            resizeService.StartColumnResize(columnIndex, clientX);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start column resize for column {ColumnIndex}: {Message}", columnIndex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Updates column resize operation
    /// </summary>
    public void UpdateColumnResize(double clientX)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.ColumnResize, nameof(UpdateColumnResize));

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            resizeService.UpdateColumnResize(clientX);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update column resize at clientX {ClientX}: {Message}", clientX, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Ends column resize operation
    /// </summary>
    public void EndColumnResize()
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.ColumnResize, nameof(EndColumnResize));

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            resizeService.EndColumnResize();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end column resize: {Message}", ex.Message);
            throw;
        }
    }

    public double GetColumnWidth(int columnIndex)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.ColumnResize, nameof(GetColumnWidth));

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            return resizeService.GetColumnWidth(columnIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get column width for column {ColumnIndex}: {Message}", columnIndex, ex.Message);
            throw;
        }
    }

    public bool IsResizing()
    {
        ThrowIfDisposed();

        try
        {
            var resizeService = _serviceProvider.GetRequiredService<Features.ColumnResize.Interfaces.IColumnResizeService>();
            return resizeService.IsResizing();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check resize status: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Starts drag selection operation
    /// </summary>
    public void StartDragSelect(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.StartDragSelectInternal(row, col);
    }

    /// <summary>
    /// Updates drag selection to new position
    /// </summary>
    public void DragSelectTo(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.UpdateDragSelectInternal(row, col);
    }

    /// <summary>
    /// Ends drag selection operation
    /// </summary>
    public void EndDragSelect(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.EndDragSelectInternal();
    }

    /// <summary>
    /// Selects a specific cell
    /// </summary>
    public void SelectCell(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.SelectCellInternal(row, col);
    }

    /// <summary>
    /// Toggles cell selection state
    /// </summary>
    public void ToggleCellSelection(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.ToggleSelectionInternal(row, col);
    }

    /// <summary>
    /// Extends selection to specified cell
    /// </summary>
    public void ExtendSelectionTo(int row, int col)
    {
        ThrowIfDisposed();

        using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
        var selectionService = scope.ServiceProvider.GetRequiredService<ISelectionService>();
        selectionService.ExtendSelectionInternal(row, col);
    }

    /// <summary>
    /// Selects a row by index
    /// </summary>
    public async Task SelectRowAsync(int rowIndex)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Selection, nameof(SelectRowAsync));

        try
        {
            var selectionService = _serviceProvider.GetRequiredService<ISelectionService>();
            selectionService.SelectCell(rowIndex, 0);
            await TriggerUIRefreshIfNeededAsync("SelectRow", 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select row {RowIndex}", rowIndex);
            throw;
        }
    }

    /// <summary>
    /// Clears all selections
    /// </summary>
    public async Task ClearSelectionAsync()
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Selection, nameof(ClearSelectionAsync));

        try
        {
            var selectionService = _serviceProvider.GetRequiredService<ISelectionService>();
            selectionService.ClearSelection();
            await TriggerUIRefreshIfNeededAsync("ClearSelection", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear selection");
            throw;
        }
    }

    #endregion
}


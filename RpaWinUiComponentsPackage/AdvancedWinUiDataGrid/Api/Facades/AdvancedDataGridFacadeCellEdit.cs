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
/// Partial class containing Cell Edit Operations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Cell Edit Operations

    /// <summary>
    /// Begins an edit session for a specific cell
    /// </summary>
    public async Task<CellEditResult> BeginEditAsync(BeginEditDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Beginning edit for row {RowIndex}, column {ColumnName}", command.RowIndex, command.ColumnName);

            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var cellEditService = scope.ServiceProvider.GetRequiredService<Features.CellEdit.Interfaces.ICellEditService>();

            var result = await cellEditService.BeginEditAsync(command.RowIndex, command.ColumnName, cancellationToken);

            if (result.IsSuccess)
            {
                return CellEditResult.Success(result.SessionId, result.ValidationAlerts);
            }
            else
            {
                return CellEditResult.Failure(result.ErrorMessage ?? "Failed to begin edit");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin edit for row {RowIndex}, column {ColumnName}", command.RowIndex, command.ColumnName);
            return CellEditResult.Failure($"Edit operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates a cell value with real-time validation
    /// </summary>
    public async Task<CellEditResult> UpdateCellAsync(UpdateCellDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Updating cell for row {RowIndex}, column {ColumnName}", command.RowIndex, command.ColumnName);

            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var cellEditService = scope.ServiceProvider.GetRequiredService<Features.CellEdit.Interfaces.ICellEditService>();

            var result = await cellEditService.UpdateCellAsync(command.RowIndex, command.ColumnName, command.NewValue, cancellationToken);

            if (!result.IsSuccess)
            {
                return CellEditResult.Failure(result.ErrorMessage ?? "Failed to update cell");
            }

            // Check validation result
            if (result.ValidationResult != null && !result.ValidationResult.IsValid)
            {
                return CellEditResult.ValidationError(
                    result.ValidationResult.ErrorMessage ?? "Validation failed",
                    result.ValidationResult.Severity,
                    result.ValidationAlerts);
            }

            return CellEditResult.Success(result.SessionId, result.ValidationAlerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cell for row {RowIndex}, column {ColumnName}", command.RowIndex, command.ColumnName);
            return CellEditResult.Failure($"Update operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Commits the current edit session
    /// </summary>
    public async Task<CellEditResult> CommitEditAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Committing edit session");

            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var cellEditService = scope.ServiceProvider.GetRequiredService<Features.CellEdit.Interfaces.ICellEditService>();

            var result = await cellEditService.CommitEditAsync(cancellationToken);

            if (result.IsSuccess)
            {
                return CellEditResult.Success(result.SessionId);
            }
            else
            {
                return CellEditResult.Failure(result.ErrorMessage ?? "Failed to commit edit");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit edit session");
            return CellEditResult.Failure($"Commit operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels the current edit session
    /// </summary>
    public async Task<CellEditResult> CancelEditAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Canceling edit session");

            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var cellEditService = scope.ServiceProvider.GetRequiredService<Features.CellEdit.Interfaces.ICellEditService>();

            var result = await cellEditService.CancelEditAsync(cancellationToken);

            if (result.IsSuccess)
            {
                return CellEditResult.Success(result.SessionId);
            }
            else
            {
                return CellEditResult.Failure(result.ErrorMessage ?? "Failed to cancel edit");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel edit session");
            return CellEditResult.Failure($"Cancel operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets validation alerts for a specific row
    /// </summary>
    public string GetValidationAlerts(int rowIndex)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();

            return validationService.GetValidationAlertsForRow(rowIndex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get validation alerts for row {RowIndex}", rowIndex);
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks if a row has validation errors
    /// </summary>
    public bool HasValidationErrors(int rowIndex)
    {
        ThrowIfDisposed();

        try
        {
            var alerts = GetValidationAlerts(rowIndex);
            return !string.IsNullOrEmpty(alerts) && alerts.Contains("Error:", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check validation errors for row {RowIndex}", rowIndex);
            return false;
        }
    }

    /// <summary>
    /// Updates a cell value with simplified signature (interface requirement)
    /// </summary>
    public async Task UpdateCellAsync(int rowIndex, string columnName, object? value)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.RowColumnOperations, nameof(UpdateCellAsync));

        try
        {
            var command = new UpdateCellDataCommand
            {
                RowIndex = rowIndex,
                ColumnName = columnName,
                NewValue = value
            };
            await UpdateCellAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cell at row {RowIndex}, column {ColumnName}", rowIndex, columnName);
            throw;
        }
    }

    #endregion
}


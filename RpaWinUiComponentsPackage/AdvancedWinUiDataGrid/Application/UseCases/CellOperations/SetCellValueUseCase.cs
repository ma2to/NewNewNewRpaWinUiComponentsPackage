using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.UseCases.CellOperations;

/// <summary>
/// USE CASE: Set cell value with validation and UI mode awareness
/// SINGLE RESPONSIBILITY: Cell value modification logic
/// </summary>
internal sealed class SetCellValueUseCase
{
    private readonly IDataGridRepository _repository;
    private readonly IValidationService _validationService;
    private readonly IDataGridLogger _logger;

    public SetCellValueUseCase(
        IDataGridRepository repository,
        IValidationService validationService,
        IDataGridLogger logger)
    {
        _repository = repository;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// ENTERPRISE: Set cell value with comprehensive validation and logging
    /// </summary>
    public async Task<Result> ExecuteAsync(
        int rowIndex,
        string columnName,
        object? newValue,
        DataGridOperationMode operationMode = DataGridOperationMode.UI,
        bool validateImmediately = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("CELL OPERATION START: Setting value for cell [{RowIndex}, {ColumnName}] to '{NewValue}' in {Mode} mode",
                rowIndex, columnName, newValue?.ToString() ?? "null", operationMode);

            // 1. Validate cell coordinates
            var cellResult = _repository.GetCell(rowIndex, columnName);
            if (cellResult.IsFailure)
            {
                _logger.LogWarning("CELL OPERATION: Cell [{RowIndex}, {ColumnName}] not found: {Error}",
                    rowIndex, columnName, cellResult.Error);
                return Result.Failure($"Cell not found: {cellResult.Error}");
            }

            var cell = cellResult.Value;
            if (cell == null)
            {
                _logger.LogError("CELL OPERATION: Cell [{RowIndex}, {ColumnName}] is null despite successful retrieval",
                    rowIndex, columnName);
                return Result.Failure($"Cell [{rowIndex}, {columnName}] is null");
            }

            // 2. Check if cell is read-only
            if (cell.IsReadOnly)
            {
                _logger.LogWarning("CELL OPERATION: Attempted to modify read-only cell [{RowIndex}, {ColumnName}]",
                    rowIndex, columnName);
                return Result.Failure($"Cell [{rowIndex}, {columnName}] is read-only");
            }

            var oldValue = cell.Value;

            // 3. Update cell value
            var updateResult = _repository.UpdateCell(rowIndex, columnName, newValue);
            if (updateResult.IsFailure)
            {
                _logger.LogError("CELL OPERATION: Failed to update cell [{RowIndex}, {ColumnName}]: {Error}",
                    rowIndex, columnName, updateResult.Error);
                return updateResult;
            }

            _logger.LogInformation("CELL OPERATION: Successfully updated cell [{RowIndex}, {ColumnName}] from '{OldValue}' to '{NewValue}'",
                rowIndex, columnName, oldValue?.ToString() ?? "null", newValue?.ToString() ?? "null");

            // 4. Perform real-time validation if requested and in UI mode
            if (validateImmediately && operationMode == DataGridOperationMode.UI)
            {
                await PerformCellValidationAsync(rowIndex, columnName, newValue, cancellationToken);
            }

            _logger.LogInformation("CELL OPERATION SUCCESS: SetCellValue completed");
            return Result.Success();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "CELL OPERATION FAILED: SetCellValue failed - {ErrorMessage}", ex.Message);
            return Result.Failure($"Operation SetCellValue failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// VALIDATION: Perform real-time cell validation
    /// </summary>
    private async Task PerformCellValidationAsync(
        int rowIndex,
        string columnName,
        object? value,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("VALIDATION: Starting real-time validation for cell [{RowIndex}, {ColumnName}]",
                rowIndex, columnName);

            // Get row data for cross-column validation
            var rowResult = _repository.GetRow(rowIndex);
            if (rowResult.IsFailure || rowResult.Value == null)
            {
                _logger.LogWarning("VALIDATION: Could not retrieve row {RowIndex} for validation: {Error}",
                    rowIndex, rowResult.Error);
                return;
            }

            var rowData = rowResult.Value.GetRowData();

            // Validate the cell
            var validationResult = await _validationService.ValidateCellAsync(
                columnName, value, rowData, cancellationToken);

            if (validationResult.IsFailure)
            {
                _logger.LogWarning("VALIDATION: Cell validation failed for [{RowIndex}, {ColumnName}]: {Error}",
                    rowIndex, columnName, validationResult.Error);
                return;
            }

            var result = validationResult.Value;
            if (!result.IsValid)
            {
                _logger.LogWarning("VALIDATION: Cell [{RowIndex}, {ColumnName}] validation error: {ErrorMessage} (Severity: {Severity})",
                    rowIndex, columnName, result.ErrorMessage, result.Severity);

                // Update cell with validation result
                var cell = _repository.GetCell(rowIndex, columnName).Value;
                cell?.AddValidationResult(result);
            }
            else
            {
                _logger.LogInformation("VALIDATION: Cell [{RowIndex}, {ColumnName}] validation passed",
                    rowIndex, columnName);

                // Clear validation errors for this cell
                var cell = _repository.GetCell(rowIndex, columnName).Value;
                cell?.ClearValidationResults();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("VALIDATION: Cell validation cancelled for [{RowIndex}, {ColumnName}]",
                rowIndex, columnName);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "VALIDATION: Unexpected error during cell validation for [{RowIndex}, {ColumnName}]",
                rowIndex, columnName);
        }
    }
}
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.SpecialColumns.Interfaces;

/// <summary>
/// Service interface for managing special columns (row number, validation alerts, etc.)
/// Implements Scoped lifetime per DI_DECISIONS.md - per-operation state isolation
/// </summary>
internal interface ISpecialColumnService
{
    /// <summary>
    /// Gets the validation alerts column name
    /// </summary>
    string ValidationAlertsColumnName { get; }

    /// <summary>
    /// Gets the row number column name
    /// </summary>
    string RowNumberColumnName { get; }

    /// <summary>
    /// Updates validation alerts for a specific row
    /// </summary>
    /// <param name="rowIndex">Row index to update</param>
    /// <param name="alertMessage">Alert message to set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> UpdateValidationAlertsAsync(int rowIndex, string alertMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets validation alerts for a specific row
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <returns>Validation alerts message or null</returns>
    string? GetValidationAlerts(int rowIndex);

    /// <summary>
    /// Clears validation alerts for a specific row
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> ClearValidationAlertsAsync(int rowIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all validation alerts
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> ClearAllValidationAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a column is a special column
    /// </summary>
    /// <param name="columnName">Column name to check</param>
    /// <returns>True if column is special</returns>
    bool IsSpecialColumn(string columnName);

    /// <summary>
    /// Gets the special column type for a column
    /// </summary>
    /// <param name="columnName">Column name</param>
    /// <returns>Special column type</returns>
    SpecialColumnType GetSpecialColumnType(string columnName);

    /// <summary>
    /// Initializes special columns (adds them if they don't exist)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> InitializeSpecialColumnsAsync(CancellationToken cancellationToken = default);
}

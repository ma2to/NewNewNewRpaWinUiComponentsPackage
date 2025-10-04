using System.Data;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;

/// <summary>
/// Service interface for data import operations with comprehensive import functionality
/// Implements Scoped lifetime per DI_DECISIONS.md - per-operation state isolation
/// </summary>
internal interface IImportService
{
    /// <summary>
    /// Imports data using command pattern with validation pipeline
    /// CRITICAL: Import ONLY supports DataTable and Dictionary - NO JSON/Excel/CSV
    /// </summary>
    /// <param name="command">Import command with data and configuration</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Import result with metrics and status</returns>
    Task<InternalImportResult> ImportAsync(
        InternalImportDataCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates import data before actual import operation
    /// </summary>
    /// <param name="command">Import command to validate</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Validation result with errors and warnings</returns>
    Task<InternalImportValidationResult> ValidateImportDataAsync(
        InternalImportDataCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supported import modes for current data configuration
    /// </summary>
    /// <param name="dataType">Type of data to import</param>
    /// <param name="targetSchema">Target schema for validation</param>
    /// <returns>List of supported import modes</returns>
    IReadOnlyList<ImportMode> GetSupportedImportModes(Type dataType, object? targetSchema = null);

    /// <summary>
    /// Estimates import duration and resource requirements
    /// </summary>
    /// <param name="command">Import command to analyze</param>
    /// <returns>Estimation result with duration and resource requirements</returns>
    Task<(TimeSpan EstimatedDuration, long EstimatedMemoryUsage)> EstimateImportRequirementsAsync(
        InternalImportDataCommand command);
}
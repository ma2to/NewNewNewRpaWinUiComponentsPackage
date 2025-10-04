using System.Data;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;

/// <summary>
/// Service interface for data export operations with comprehensive export functionality
/// Implements Scoped lifetime per DI_DECISIONS.md - per-operation state isolation
/// </summary>
internal interface IExportService
{
    /// <summary>
    /// Export to DataTable format with comprehensive filtering
    /// </summary>
    Task<System.Data.DataTable> ExportToDataTableAsync(IEnumerable<IReadOnlyDictionary<string, object?>> data, InternalExportDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export to Dictionary format with comprehensive filtering
    /// </summary>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExportToDictionaryAsync(IEnumerable<IReadOnlyDictionary<string, object?>> data, InternalExportDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports data using command pattern with comprehensive filtering
    /// CRITICAL: Export ONLY supports DataTable and Dictionary - NO JSON/Excel/CSV
    /// CRITICAL: Supports onlyChecked and onlyFiltered arguments that can be combined
    /// </summary>
    /// <param name="command">Export command with configuration</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Export result with metrics and status</returns>
    Task<InternalExportResult> ExportAsync(
        InternalExportDataCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates export configuration before actual export operation
    /// </summary>
    /// <param name="command">Export command to validate</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Validation result with errors and warnings</returns>
    Task<InternalExportValidationResult> ValidateExportConfigurationAsync(
        InternalExportDataCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supported export formats for current data configuration
    /// </summary>
    /// <returns>List of supported export formats</returns>
    IReadOnlyList<ExportFormat> GetSupportedFormats();

    /// <summary>
    /// Estimates export requirements and resource usage
    /// </summary>
    /// <param name="command">Export command to analyze</param>
    /// <returns>Estimation result with duration and resource requirements</returns>
    Task<(TimeSpan EstimatedDuration, long EstimatedMemoryUsage)> EstimateExportRequirementsAsync(
        InternalExportDataCommand command);
}
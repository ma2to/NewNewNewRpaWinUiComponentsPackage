using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using System.Data;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.IO;

/// <summary>
/// Public interface for DataGrid Import/Export operations.
/// Handles data import from various sources (DataTable, Dictionary) and export to different formats.
/// </summary>
/// <remarks>
/// This interface provides high-level I/O operations for the DataGrid component,
/// abstracting away the complexity of internal import/export services.
/// </remarks>
public interface IDataGridIO
{
    /// <summary>
    /// Imports data from DataTable or Dictionary into the grid with validation pipeline.
    /// Supports LINQ optimization and comprehensive error handling.
    /// </summary>
    /// <param name="command">Import command containing data source and configuration</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Import result with metrics (rows imported, validation errors, duration)</returns>
    /// <example>
    /// <code>
    /// var command = new ImportDataCommand
    /// {
    ///     Data = myDataTable,
    ///     ValidateOnImport = true
    /// };
    /// var result = await io.ImportAsync(command);
    /// Console.WriteLine($"Imported {result.ImportedRowCount} rows");
    /// </code>
    /// </example>
    Task<ImportResult> ImportAsync(ImportDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports data using command pattern with comprehensive filtering.
    /// Supports export to DataTable or Dictionary format.
    /// </summary>
    /// <param name="command">Export command with configuration (include headers, filtering options)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Export result with exported data and metrics</returns>
    /// <example>
    /// <code>
    /// var command = new ExportDataCommand
    /// {
    ///     IncludeHeaders = true
    /// };
    /// var result = await io.ExportAsync(command);
    /// var dataTable = result.ExportedData as DataTable;
    /// </code>
    /// </example>
    Task<ExportResult> ExportAsync(ExportDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current grid data as read-only dictionary collection.
    /// Useful for programmatic access to grid data without export overhead.
    /// </summary>
    /// <returns>Current data in the grid as dictionary collection</returns>
    /// <example>
    /// <code>
    /// var data = io.GetCurrentData();
    /// foreach (var row in data)
    /// {
    ///     Console.WriteLine($"Name: {row["Name"]}");
    /// }
    /// </code>
    /// </example>
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GetCurrentData();

    /// <summary>
    /// Gets current grid data as DataTable.
    /// Convenience method for legacy code or DataTable-based workflows.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Current data as DataTable</returns>
    /// <example>
    /// <code>
    /// var dataTable = await io.GetCurrentDataAsDataTableAsync();
    /// // Use DataTable with legacy APIs
    /// </code>
    /// </example>
    Task<DataTable> GetCurrentDataAsDataTableAsync(CancellationToken cancellationToken = default);
}

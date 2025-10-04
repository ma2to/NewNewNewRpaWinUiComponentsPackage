using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using System.Data;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.IO;

/// <summary>
/// Public interface for DataGrid Import/Export operations
/// Handles data import from various sources and export to different formats
/// </summary>
public interface IDataGridIO
{
    /// <summary>
    /// Imports data using the specified import command
    /// </summary>
    Task<ImportResult> ImportAsync(ImportDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports data using the specified export command
    /// </summary>
    Task<ExportResult> ExportAsync(ExportDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current grid data as a DataTable
    /// </summary>
    Task<DataTable> GetCurrentDataAsDataTableAsync(CancellationToken cancellationToken = default);
}

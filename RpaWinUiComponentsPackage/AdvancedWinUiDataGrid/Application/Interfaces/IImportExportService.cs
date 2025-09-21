using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Import and export functionality for DataTable and Dictionary
/// CLEAN ARCHITECTURE: Application layer interface for data import/export operations
/// </summary>
internal interface IImportExportService
{
    // Import operations - DataTable and Dictionary only as requested
    Task<CoreTypes.ImportResult> ImportFromDataTableAsync(
        DataTable dataTable,
        CoreTypes.ImportOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<CoreTypes.ImportResult> ImportFromDictionaryAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> sourceData,
        CoreTypes.ImportOptions? options = null,
        CancellationToken cancellationToken = default);

    // Export operations - DataTable and Dictionary only as requested
    Task<DataTable> ExportToDataTableAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.ExportOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExportToDictionaryAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.ExportOptions? options = null,
        CancellationToken cancellationToken = default);

    // Copy/Paste operations
    Task<CoreTypes.CopyPasteResult> CopyToClipboardAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> selectedData,
        bool includeHeaders = true,
        CancellationToken cancellationToken = default);

    Task<CoreTypes.CopyPasteResult> PasteFromClipboardAsync(
        int targetRowIndex = 0,
        int targetColumnIndex = 0,
        CoreTypes.ImportMode mode = CoreTypes.ImportMode.Replace,
        CancellationToken cancellationToken = default);

    // Utility operations
    bool CanImport(object source);
    bool ValidateImportData(object source, CoreTypes.ImportOptions? options = null);
}


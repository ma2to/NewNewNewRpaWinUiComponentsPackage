using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using InternalImportDataCommand = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models.InternalImportDataCommand;
using InternalImportResult = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models.InternalImportResult;
using InternalExportDataCommand = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models.InternalExportDataCommand;
using InternalExportResult = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models.InternalExportResult;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;

/// <summary>
/// INTERNAL: Maps between public API models and internal service models
/// </summary>
internal static class ModelMapper
{
    /// <summary>
    /// Map public ImportDataCommand to internal ImportDataCommand
    /// </summary>
    public static InternalImportDataCommand ToInternal(this ImportDataCommand publicCommand)
    {
        if (publicCommand.DataTableData != null)
        {
            return InternalImportDataCommand.FromDataTable(
                publicCommand.DataTableData,
                publicCommand.Mode.ToInternal(),
                publicCommand.CorrelationId);
        }
        else if (publicCommand.DictionaryData != null)
        {
            return InternalImportDataCommand.FromDictionaries(
                publicCommand.DictionaryData,
                publicCommand.Mode.ToInternal(),
                publicCommand.CorrelationId);
        }
        else
        {
            throw new ArgumentException("Either DataTableData or DictionaryData must be provided");
        }
    }

    /// <summary>
    /// Map internal ImportResult to public ImportResult
    /// </summary>
    public static ImportResult ToPublic(this InternalImportResult internalResult)
    {
        return new ImportResult
        {
            IsSuccess = internalResult.IsSuccess,
            ImportedRows = internalResult.ImportedRows,
            TotalRows = internalResult.TotalRows,
            ImportTime = internalResult.ImportTime,
            ValidationPassed = internalResult.ValidationPassed,
            ErrorMessages = internalResult.ErrorMessages,
            CorrelationId = internalResult.CorrelationId
        };
    }

    /// <summary>
    /// Map public ExportDataCommand to internal ExportDataCommand
    /// </summary>
    public static InternalExportDataCommand ToInternal(this ExportDataCommand publicCommand)
    {
        return new InternalExportDataCommand
        {
            Format = publicCommand.Format.ToInternal(),
            IncludeValidationAlerts = publicCommand.IncludeValidationAlerts,
            ExportOnlyChecked = publicCommand.ExportOnlyChecked,
            ExportOnlyFiltered = publicCommand.ExportOnlyFiltered,
            RemoveAfterExport = publicCommand.RemoveAfterExport,
            IncludeHeaders = publicCommand.IncludeHeaders,
            ColumnNames = publicCommand.ColumnNames,
            CorrelationId = publicCommand.CorrelationId ?? Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// Map internal ExportResult to public ExportResult
    /// </summary>
    public static ExportResult ToPublic(this InternalExportResult internalResult, object? exportedData)
    {
        return new ExportResult
        {
            IsSuccess = internalResult.IsSuccess,
            ExportedData = exportedData,
            ExportedRows = internalResult.ExportedRows,
            TotalRows = internalResult.TotalRows,
            ExportTime = internalResult.ExportTime,
            ValidationPassed = internalResult.ValidationPassed,
            ErrorMessages = internalResult.ErrorMessages,
            CorrelationId = internalResult.CorrelationId
        };
    }
}

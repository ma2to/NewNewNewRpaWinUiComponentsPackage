using System.Data;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// PUBLIC API: Command for importing data into the grid
/// Supports DataTable and Dictionary formats only
/// </summary>
public class ImportDataCommand
{
    /// <summary>
    /// DataTable data source (mutually exclusive with DictionaryData)
    /// </summary>
    public DataTable? DataTableData { get; init; }

    /// <summary>
    /// Dictionary collection data source (mutually exclusive with DataTableData)
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>>? DictionaryData { get; init; }

    /// <summary>
    /// Import mode (Replace, Append, Merge)
    /// </summary>
    public PublicImportMode Mode { get; init; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Whether to validate after import
    /// </summary>
    public bool ValidateAfterImport { get; init; } = true;

    /// <summary>
    /// Create import command from DataTable
    /// </summary>
    public static ImportDataCommand FromDataTable(DataTable dataTable, PublicImportMode mode = PublicImportMode.Replace, string? correlationId = null)
        => new ImportDataCommand
        {
            DataTableData = dataTable,
            Mode = mode,
            CorrelationId = correlationId
        };

    /// <summary>
    /// Create import command from Dictionary collection
    /// </summary>
    public static ImportDataCommand FromDictionaries(IReadOnlyList<IReadOnlyDictionary<string, object?>> data, PublicImportMode mode = PublicImportMode.Replace, string? correlationId = null)
        => new ImportDataCommand
        {
            DictionaryData = data,
            Mode = mode,
            CorrelationId = correlationId
        };
}

/// <summary>
/// PUBLIC API: Result of import operation
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the number of rows imported
    /// </summary>
    public int ImportedRows { get; init; }

    /// <summary>
    /// Gets the total number of rows in source
    /// </summary>
    public int TotalRows { get; init; }

    /// <summary>
    /// Gets the import operation duration
    /// </summary>
    public TimeSpan ImportTime { get; init; }

    /// <summary>
    /// Gets whether validation passed after import
    /// </summary>
    public bool ValidationPassed { get; init; }

    /// <summary>
    /// Gets the error messages if failed
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the correlation ID
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the import mode used
    /// </summary>
    public PublicImportMode PublicImportMode { get; init; }

    /// <summary>
    /// Creates successful result
    /// </summary>
    public static ImportResult CreateSuccess(int importedRows, int totalRows, TimeSpan importTime, PublicImportMode mode, string? correlationId = null, bool validationPassed = true)
    {
        return new ImportResult
        {
            IsSuccess = true,
            ImportedRows = importedRows,
            TotalRows = totalRows,
            ImportTime = importTime,
            ValidationPassed = validationPassed,
            PublicImportMode = mode,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates failed result
    /// </summary>
    public static ImportResult Failure(IReadOnlyList<string> errors, TimeSpan importTime, PublicImportMode mode, string? correlationId = null)
    {
        return new ImportResult
        {
            IsSuccess = false,
            ImportTime = importTime,
            ErrorMessages = errors,
            PublicImportMode = mode,
            CorrelationId = correlationId
        };
    }
}

/// <summary>
/// PUBLIC API: Command for exporting data from the grid
/// Supports DataTable and Dictionary formats only
/// </summary>
public class ExportDataCommand
{
    /// <summary>
    /// Export format (DataTable or Dictionary)
    /// </summary>
    public PublicExportFormat Format { get; init; } = PublicExportFormat.Dictionary;

    /// <summary>
    /// Include validation alerts in export
    /// </summary>
    public bool IncludeValidationAlerts { get; init; } = false;

    /// <summary>
    /// Export only checked/selected rows
    /// </summary>
    public bool ExportOnlyChecked { get; init; } = false;

    /// <summary>
    /// Export only currently filtered rows
    /// </summary>
    public bool ExportOnlyFiltered { get; init; } = false;

    /// <summary>
    /// Remove exported rows from source after export
    /// </summary>
    public bool RemoveAfterExport { get; init; } = false;

    /// <summary>
    /// Include column headers in export
    /// </summary>
    public bool IncludeHeaders { get; init; } = true;

    /// <summary>
    /// Optional column selection (null = all non-special columns)
    /// </summary>
    public IReadOnlyList<string>? ColumnNames { get; init; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Create export command for DataTable format
    /// </summary>
    public static ExportDataCommand ToDataTable(
        bool includeValidationAlerts = false,
        bool exportOnlyChecked = false,
        bool exportOnlyFiltered = false,
        bool removeAfterExport = false,
        IReadOnlyList<string>? columnNames = null,
        string? correlationId = null)
        => new ExportDataCommand
        {
            Format = PublicExportFormat.DataTable,
            IncludeValidationAlerts = includeValidationAlerts,
            ExportOnlyChecked = exportOnlyChecked,
            ExportOnlyFiltered = exportOnlyFiltered,
            RemoveAfterExport = removeAfterExport,
            ColumnNames = columnNames,
            CorrelationId = correlationId
        };

    /// <summary>
    /// Create export command for Dictionary format
    /// </summary>
    public static ExportDataCommand ToDictionary(
        bool includeValidationAlerts = false,
        bool exportOnlyChecked = false,
        bool exportOnlyFiltered = false,
        bool removeAfterExport = false,
        IReadOnlyList<string>? columnNames = null,
        string? correlationId = null)
        => new ExportDataCommand
        {
            Format = PublicExportFormat.Dictionary,
            IncludeValidationAlerts = includeValidationAlerts,
            ExportOnlyChecked = exportOnlyChecked,
            ExportOnlyFiltered = exportOnlyFiltered,
            RemoveAfterExport = removeAfterExport,
            ColumnNames = columnNames,
            CorrelationId = correlationId
        };
}

/// <summary>
/// PUBLIC API: Result of export operation
/// </summary>
public class ExportResult
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the exported data (DataTable or List of Dictionaries depending on format)
    /// </summary>
    public object? ExportedData { get; init; }

    /// <summary>
    /// Gets the number of rows exported
    /// </summary>
    public int ExportedRows { get; init; }

    /// <summary>
    /// Gets the total number of rows available
    /// </summary>
    public int TotalRows { get; init; }

    /// <summary>
    /// Gets the export operation duration
    /// </summary>
    public TimeSpan ExportTime { get; init; }

    /// <summary>
    /// Gets whether validation passed before export
    /// </summary>
    public bool ValidationPassed { get; init; } = true;

    /// <summary>
    /// Gets the error messages if failed
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the correlation ID
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the export format used
    /// </summary>
    public PublicExportFormat Format { get; init; }

    /// <summary>
    /// Gets the estimated data size in bytes
    /// </summary>
    public long DataSize { get; init; }

    /// <summary>
    /// Creates successful result with all details
    /// </summary>
    public static ExportResult CreateSuccess(int exportedRows, int totalRows, TimeSpan exportTime, PublicExportFormat format, long dataSize, string correlationId)
    {
        return new ExportResult
        {
            IsSuccess = true,
            ExportedRows = exportedRows,
            TotalRows = totalRows,
            ExportTime = exportTime,
            Format = format,
            DataSize = dataSize,
            CorrelationId = correlationId,
            ValidationPassed = true
        };
    }

    /// <summary>
    /// Creates failed result with error message
    /// </summary>
    public static ExportResult Failure(string errorMessage, TimeSpan exportTime, PublicExportFormat format, string? correlationId = null)
    {
        return new ExportResult
        {
            IsSuccess = false,
            ExportTime = exportTime,
            Format = format,
            CorrelationId = correlationId,
            ErrorMessages = new[] { errorMessage }
        };
    }

}

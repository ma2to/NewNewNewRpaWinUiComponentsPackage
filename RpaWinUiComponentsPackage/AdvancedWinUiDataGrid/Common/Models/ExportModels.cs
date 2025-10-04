using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Progress information for export operations
/// INTERNAL: Used by internal services only
/// </summary>
internal class InternalExportProgress
{
    /// <summary>
    /// Gets the number of rows processed
    /// </summary>
    public int ProcessedRows { get; init; }

    /// <summary>
    /// Gets the total number of rows to export
    /// </summary>
    public int TotalRows { get; init; }

    /// <summary>
    /// Gets the progress percentage (0-100)
    /// </summary>
    public double ProgressPercent => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;

    /// <summary>
    /// Gets the current status message
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Gets the current export format
    /// </summary>
    public ExportFormat CurrentFormat { get; init; }

    /// <summary>
    /// Gets whether the operation is complete
    /// </summary>
    public bool IsComplete => ProcessedRows >= TotalRows;
}

/// <summary>
/// Command for exporting data - ONLY supports DataTable and Dictionary
/// Supports combining ExportOnlyChecked and ExportOnlyFiltered
/// INTERNAL: Used by internal services only
/// </summary>
internal class InternalExportDataCommand
{
    /// <summary>
    /// Export format (DataTable or Dictionary)
    /// </summary>
    public ExportFormat Format { get; init; } = ExportFormat.Dictionary;

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
    /// Operation timeout
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Progress reporting callback
    /// </summary>
    public IProgress<InternalExportProgress>? ExportProgress { get; init; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Create export command for DataTable format
    /// </summary>
    public static InternalExportDataCommand ToDataTable(
        bool includeValidationAlerts = false,
        bool exportOnlyChecked = false,
        bool exportOnlyFiltered = false,
        bool removeAfterExport = false,
        bool includeHeaders = true,
        IReadOnlyList<string>? columnNames = null,
        IProgress<InternalExportProgress>? progress = null,
        string? correlationId = null)
        => new InternalExportDataCommand
        {
            Format = ExportFormat.DataTable,
            IncludeValidationAlerts = includeValidationAlerts,
            ExportOnlyChecked = exportOnlyChecked,
            ExportOnlyFiltered = exportOnlyFiltered,
            RemoveAfterExport = removeAfterExport,
            IncludeHeaders = includeHeaders,
            ColumnNames = columnNames,
            ExportProgress = progress,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString()
        };

    /// <summary>
    /// Create export command for Dictionary format
    /// </summary>
    public static InternalExportDataCommand ToDictionary(
        bool includeValidationAlerts = false,
        bool exportOnlyChecked = false,
        bool exportOnlyFiltered = false,
        bool removeAfterExport = false,
        bool includeHeaders = true,
        IReadOnlyList<string>? columnNames = null,
        IProgress<InternalExportProgress>? progress = null,
        string? correlationId = null)
        => new InternalExportDataCommand
        {
            Format = ExportFormat.Dictionary,
            IncludeValidationAlerts = includeValidationAlerts,
            ExportOnlyChecked = exportOnlyChecked,
            ExportOnlyFiltered = exportOnlyFiltered,
            RemoveAfterExport = removeAfterExport,
            IncludeHeaders = includeHeaders,
            ColumnNames = columnNames,
            ExportProgress = progress,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString()
        };
}

/// <summary>
/// Result of export operation
/// INTERNAL: Used by internal services only
/// </summary>
internal class InternalExportResult
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the exported data
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
    /// Gets the export format used
    /// </summary>
    public ExportFormat Format { get; init; }

    /// <summary>
    /// Gets the estimated data size in bytes
    /// </summary>
    public long DataSize { get; init; }

    /// <summary>
    /// Gets the correlation ID
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets whether validation passed
    /// </summary>
    public bool ValidationPassed { get; init; } = true;

    /// <summary>
    /// Gets the error messages if failed
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates successful result
    /// </summary>
    public static InternalExportResult Success(object exportedData, int exportedRows, TimeSpan exportTime, bool validationPassed = true)
    {
        return new InternalExportResult
        {
            IsSuccess = true,
            ExportedData = exportedData,
            ExportedRows = exportedRows,
            ExportTime = exportTime,
            ValidationPassed = validationPassed
        };
    }

    /// <summary>
    /// Creates successful result with all details
    /// </summary>
    public static InternalExportResult CreateSuccess(int exportedRows, int totalRows, TimeSpan exportTime, ExportFormat format, long dataSize, string correlationId)
    {
        return new InternalExportResult
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
    public static InternalExportResult Failure(string errorMessage, TimeSpan exportTime, ExportFormat format, string? correlationId = null)
    {
        return new InternalExportResult
        {
            IsSuccess = false,
            ExportTime = exportTime,
            Format = format,
            CorrelationId = correlationId,
            ErrorMessages = new[] { errorMessage }
        };
    }

    /// <summary>
    /// Creates cancelled result
    /// </summary>
    public static InternalExportResult Cancelled(TimeSpan exportTime)
    {
        return new InternalExportResult
        {
            IsSuccess = false,
            ExportTime = exportTime,
            ErrorMessages = new[] { "Operation was cancelled" }
        };
    }
}

/// <summary>
/// Export validation result
/// INTERNAL: Used by internal services only
/// </summary>
internal class InternalExportValidationResult
{
    /// <summary>
    /// Gets whether the export configuration is valid
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets validation error messages
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates valid result
    /// </summary>
    public static InternalExportValidationResult Valid()
    {
        return new InternalExportValidationResult
        {
            IsValid = true
        };
    }

    /// <summary>
    /// Creates invalid result
    /// </summary>
    public static InternalExportValidationResult Invalid(IReadOnlyList<string> errors)
    {
        return new InternalExportValidationResult
        {
            IsValid = false,
            ValidationErrors = errors
        };
    }
}
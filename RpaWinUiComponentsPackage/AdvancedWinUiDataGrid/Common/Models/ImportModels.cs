using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using System.Data;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Command for importing data - ONLY supports DataTable and Dictionary
/// INTERNAL: Used by internal services only
/// </summary>
internal class InternalImportDataCommand
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
    public ImportMode Mode { get; init; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Whether to validate after import
    /// </summary>
    public bool ValidateAfterImport { get; init; } = true;

    /// <summary>
    /// Progress reporting callback
    /// </summary>
    public IProgress<InternalImportProgress>? Progress { get; init; }

    /// <summary>
    /// Create import command from DataTable
    /// </summary>
    public static InternalImportDataCommand FromDataTable(DataTable dataTable, ImportMode mode, string? correlationId = null)
        => new InternalImportDataCommand
        {
            DataTableData = dataTable,
            Mode = mode,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString()
        };

    /// <summary>
    /// Create import command from Dictionary collection
    /// </summary>
    public static InternalImportDataCommand FromDictionaries(IReadOnlyList<IReadOnlyDictionary<string, object?>> data, ImportMode mode, string? correlationId = null)
        => new InternalImportDataCommand
        {
            DictionaryData = data,
            Mode = mode,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString()
        };
}

/// <summary>
/// Progress information for import operations
/// INTERNAL: Used by internal services only
/// </summary>
internal class InternalImportProgress
{
    /// <summary>
    /// Gets the number of rows processed
    /// </summary>
    public int ProcessedRows { get; init; }

    /// <summary>
    /// Gets the total number of rows to process
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
    /// Gets whether the operation is complete
    /// </summary>
    public bool IsComplete => ProcessedRows >= TotalRows;
}

/// <summary>
/// Result of import operation
/// INTERNAL: Used by internal services only
/// </summary>
internal class InternalImportResult
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
    /// Gets the import mode used
    /// </summary>
    public ImportMode ImportMode { get; init; }

    /// <summary>
    /// Creates successful result
    /// </summary>
    public static InternalImportResult CreateSuccess(int importedRows, int totalRows, TimeSpan importTime, ImportMode mode, string? correlationId = null, bool validationPassed = true)
    {
        return new InternalImportResult
        {
            IsSuccess = true,
            ImportedRows = importedRows,
            TotalRows = totalRows,
            ImportTime = importTime,
            ValidationPassed = validationPassed,
            ImportMode = mode,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates failed result
    /// </summary>
    public static InternalImportResult Failure(IReadOnlyList<string> errors, TimeSpan importTime, ImportMode mode, string? correlationId = null)
    {
        return new InternalImportResult
        {
            IsSuccess = false,
            ImportTime = importTime,
            ErrorMessages = errors,
            ImportMode = mode,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates cancelled result
    /// </summary>
    public static InternalImportResult Cancelled(TimeSpan importTime, string? correlationId = null)
    {
        return new InternalImportResult
        {
            IsSuccess = false,
            ImportTime = importTime,
            ErrorMessages = new[] { "Operation was cancelled" },
            CorrelationId = correlationId
        };
    }
}

/// <summary>
/// Validation result for import data
/// INTERNAL: Used by internal services only
/// </summary>
internal class InternalImportValidationResult
{
    /// <summary>
    /// Gets whether the import data is valid
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets validation error messages
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets validation warning messages
    /// </summary>
    public IReadOnlyList<string> ValidationWarnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the number of rows validated
    /// </summary>
    public int ValidatedRows { get; init; }

    /// <summary>
    /// Constructor for InternalImportValidationResult
    /// </summary>
    public InternalImportValidationResult(bool isValid, IReadOnlyList<string> errors, IReadOnlyList<string> warnings, int validatedRows = 0)
    {
        IsValid = isValid;
        ValidationErrors = errors;
        ValidationWarnings = warnings;
        ValidatedRows = validatedRows;
    }

    /// <summary>
    /// Creates valid result
    /// </summary>
    public static InternalImportValidationResult Valid(int validatedRows = 0)
    {
        return new InternalImportValidationResult(true, Array.Empty<string>(), Array.Empty<string>(), validatedRows);
    }

    /// <summary>
    /// Creates invalid result
    /// </summary>
    public static InternalImportValidationResult Invalid(IReadOnlyList<string> errors, int validatedRows = 0)
    {
        return new InternalImportValidationResult(false, errors, Array.Empty<string>(), validatedRows);
    }
}
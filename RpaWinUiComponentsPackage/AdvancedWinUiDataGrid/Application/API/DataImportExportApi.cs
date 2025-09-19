using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.API;

/// <summary>
/// PUBLIC API: Import mode options
/// </summary>
public enum ImportMode
{
    Replace,
    Append,
    Insert,
    Merge
}

/// <summary>
/// PUBLIC API: Import options for Dictionary and DataTable
/// </summary>
public sealed record ImportOptions
{
    public ImportMode Mode { get; init; } = ImportMode.Replace;
    public int StartRowIndex { get; init; } = 0;
    public bool ValidateBeforeImport { get; init; } = true;
    public bool CreateMissingColumns { get; init; } = true;
    public Dictionary<string, string>? ColumnMapping { get; init; }
    public IProgress<ImportProgress>? Progress { get; init; }

    public static ImportOptions Default => new();
}

/// <summary>
/// PUBLIC API: Import progress information
/// </summary>
public sealed record ImportProgress
{
    public int ProcessedRows { get; init; }
    public int TotalRows { get; init; }
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    public TimeSpan ElapsedTime { get; init; }
    public string CurrentOperation { get; init; } = string.Empty;

    public static ImportProgress Create(int processed, int total, TimeSpan elapsed, string operation = "") =>
        new()
        {
            ProcessedRows = processed,
            TotalRows = total,
            ElapsedTime = elapsed,
            CurrentOperation = operation
        };
}

/// <summary>
/// PUBLIC API: Import result with statistics
/// </summary>
public sealed record ImportResult
{
    public bool Success { get; init; }
    public int ImportedRows { get; init; }
    public int SkippedRows { get; init; }
    public int TotalRows { get; init; }
    public TimeSpan ImportTime { get; init; }
    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> WarningMessages { get; init; } = Array.Empty<string>();

    public static ImportResult CreateSuccess(int importedRows, int totalRows, TimeSpan importTime) =>
        new()
        {
            Success = true,
            ImportedRows = importedRows,
            TotalRows = totalRows,
            ImportTime = importTime
        };

    public static ImportResult Failure(IReadOnlyList<string> errors, TimeSpan importTime) =>
        new()
        {
            Success = false,
            ErrorMessages = errors,
            ImportTime = importTime
        };
}

/// <summary>
/// PUBLIC API: Copy/Paste operation result
/// </summary>
public sealed record CopyPasteResult
{
    public bool Success { get; init; }
    public int ProcessedRows { get; init; }
    public string? ClipboardData { get; init; }
    public string? ErrorMessage { get; init; }

    public static CopyPasteResult CreateSuccess(int processedRows, string? clipboardData = null) =>
        new() { Success = true, ProcessedRows = processedRows, ClipboardData = clipboardData };

    public static CopyPasteResult Failure(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// INTERNAL: Import data command structure
/// ENTERPRISE: Enhanced command with all required arguments for professional usage
/// </summary>
internal sealed record ImportDataCommand
{
    internal List<Dictionary<string, object?>>? DictionaryData { get; init; }
    internal DataTable? DataTableData { get; init; }
    internal Dictionary<int, bool>? CheckboxStates { get; init; }
    internal int StartRow { get; init; } = 1;
    internal ImportMode Mode { get; init; } = ImportMode.Replace;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<ValidationProgress>? ValidationProgress { get; init; }

    // Backward compatibility aliases
    internal List<Dictionary<string, object?>>? Data => DictionaryData;
    internal bool ValidateBeforeImport { get; init; } = true;

    internal static ImportDataCommand FromDictionary(
        List<Dictionary<string, object?>> data,
        Dictionary<int, bool>? checkboxStates = null,
        int startRow = 1,
        ImportMode mode = ImportMode.Replace,
        TimeSpan? timeout = null,
        IProgress<ValidationProgress>? validationProgress = null)
    {
        return new ImportDataCommand
        {
            DictionaryData = data,
            CheckboxStates = checkboxStates,
            StartRow = startRow,
            Mode = mode,
            Timeout = timeout,
            ValidationProgress = validationProgress
        };
    }

    internal static ImportDataCommand FromDataTable(
        DataTable dataTable,
        Dictionary<int, bool>? checkboxStates = null,
        int startRow = 1,
        ImportMode mode = ImportMode.Replace,
        TimeSpan? timeout = null,
        IProgress<ValidationProgress>? validationProgress = null)
    {
        return new ImportDataCommand
        {
            DataTableData = dataTable,
            CheckboxStates = checkboxStates,
            StartRow = startRow,
            Mode = mode,
            Timeout = timeout,
            ValidationProgress = validationProgress
        };
    }
}

/// <summary>
/// COMPATIBILITY: Command for importing data from DataTable
/// ENTERPRISE: Backward compatibility with enhanced functionality
/// </summary>
internal sealed record ImportFromDataTableCommand
{
    internal DataTable? DataTableData { get; init; }
    internal Dictionary<int, bool>? CheckboxStates { get; init; }
    internal int StartRow { get; init; } = 1;
    internal ImportMode Mode { get; init; } = ImportMode.Replace;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<ValidationProgress>? ValidationProgress { get; init; }

    // Backward compatibility aliases
    internal DataTable? DataTable => DataTableData;
    internal IProgress<ValidationProgress>? Progress => ValidationProgress;

    internal static ImportFromDataTableCommand FromDataTable(
        DataTable dataTable,
        Dictionary<int, bool>? checkboxStates = null,
        int startRow = 1,
        ImportMode mode = ImportMode.Replace,
        TimeSpan? timeout = null,
        IProgress<ValidationProgress>? validationProgress = null)
    {
        return new ImportFromDataTableCommand
        {
            DataTableData = dataTable,
            CheckboxStates = checkboxStates,
            StartRow = startRow,
            Mode = mode,
            Timeout = timeout,
            ValidationProgress = validationProgress
        };
    }
}

/// <summary>
/// INTERNAL: Export data command structure
/// ENTERPRISE: Enhanced command with ValidAlerts support and advanced filtering
/// </summary>
internal sealed record ExportDataCommand
{
    internal bool IncludeValidAlerts { get; init; } = false;
    internal bool ExportOnlyChecked { get; init; } = false;
    internal bool ExportOnlyFiltered { get; init; } = false;
    internal bool RemoveAfter { get; init; } = false;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<ExportProgress>? ExportProgress { get; init; }
    internal ExportFormat Format { get; init; } = ExportFormat.Dictionary;

    // Backward compatibility alias
    internal bool IncludeValidationAlerts => IncludeValidAlerts;

    internal static ExportDataCommand ToDictionary(
        bool includeValidAlerts = false,
        bool exportOnlyChecked = false,
        bool exportOnlyFiltered = false,
        bool removeAfter = false,
        TimeSpan? timeout = null,
        IProgress<ExportProgress>? exportProgress = null)
    {
        return new ExportDataCommand
        {
            IncludeValidAlerts = includeValidAlerts,
            ExportOnlyChecked = exportOnlyChecked,
            ExportOnlyFiltered = exportOnlyFiltered,
            RemoveAfter = removeAfter,
            Timeout = timeout,
            ExportProgress = exportProgress,
            Format = ExportFormat.Dictionary
        };
    }

    internal static ExportDataCommand ToDataTable(
        bool includeValidAlerts = false,
        bool exportOnlyChecked = false,
        bool exportOnlyFiltered = false,
        bool removeAfter = false,
        TimeSpan? timeout = null,
        IProgress<ExportProgress>? exportProgress = null)
    {
        return new ExportDataCommand
        {
            IncludeValidAlerts = includeValidAlerts,
            ExportOnlyChecked = exportOnlyChecked,
            ExportOnlyFiltered = exportOnlyFiltered,
            RemoveAfter = removeAfter,
            Timeout = timeout,
            ExportProgress = exportProgress,
            Format = ExportFormat.DataTable
        };
    }
}

/// <summary>
/// COMPATIBILITY: Command for exporting data to DataTable
/// ENTERPRISE: Backward compatibility with enhanced functionality
/// </summary>
internal sealed record ExportToDataTableCommand
{
    internal bool IncludeValidAlerts { get; init; } = false;
    internal bool ExportOnlyChecked { get; init; } = false;
    internal bool ExportOnlyFiltered { get; init; } = false;
    internal bool RemoveAfter { get; init; } = false;
    internal TimeSpan? Timeout { get; init; }
    internal IProgress<ExportProgress>? ExportProgress { get; init; }
    internal ExportFormat Format { get; init; } = ExportFormat.DataTable;

    // Backward compatibility aliases
    internal bool IncludeValidationAlerts => IncludeValidAlerts;
    internal IProgress<ExportProgress>? Progress => ExportProgress;

    internal static ExportToDataTableCommand ToDataTable(
        bool includeValidAlerts = false,
        bool exportOnlyChecked = false,
        bool exportOnlyFiltered = false,
        bool removeAfter = false,
        TimeSpan? timeout = null,
        IProgress<ExportProgress>? exportProgress = null)
    {
        return new ExportToDataTableCommand
        {
            IncludeValidAlerts = includeValidAlerts,
            ExportOnlyChecked = exportOnlyChecked,
            ExportOnlyFiltered = exportOnlyFiltered,
            RemoveAfter = removeAfter,
            Timeout = timeout,
            ExportProgress = exportProgress,
            Format = ExportFormat.DataTable
        };
    }
}

/// <summary>
/// PUBLIC API: Export format options
/// ENTERPRISE: Format options available for external usage
/// </summary>
public enum ExportFormat
{
    Dictionary,
    DataTable
}

/// <summary>
/// PUBLIC API: Export options
/// </summary>
public sealed record ExportOptions
{
    public bool IncludeValidationErrors { get; init; } = false;
    public bool IncludeRowNumbers { get; init; } = false;
    public bool IncludeCheckboxStates { get; init; } = false;
    public string DateTimeFormat { get; init; } = "yyyy-MM-dd HH:mm:ss";
    public string NumberFormat { get; init; } = "F2";
    public string NullValuePlaceholder { get; init; } = "";
    public Dictionary<string, string>? ColumnMapping { get; init; }

    public static ExportOptions Default => new();
}

/// <summary>
/// PUBLIC API: Validation progress information
/// </summary>
public sealed record ValidationProgress
{
    public int ValidatedRows { get; init; }
    public int TotalRows { get; init; }
    public double CompletionPercentage => TotalRows > 0 ? (double)ValidatedRows / TotalRows * 100 : 0;
    public TimeSpan ElapsedTime { get; init; }
    public string CurrentRule { get; init; } = string.Empty;
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public string? CurrentRowData { get; init; }

    public static ValidationProgress Create(
        int validated,
        int total,
        TimeSpan elapsed,
        string currentRule = "",
        int errorCount = 0,
        int warningCount = 0,
        string? currentRowData = null) =>
        new()
        {
            ValidatedRows = validated,
            TotalRows = total,
            ElapsedTime = elapsed,
            CurrentRule = currentRule,
            ErrorCount = errorCount,
            WarningCount = warningCount,
            CurrentRowData = currentRowData
        };
}

/// <summary>
/// PUBLIC API: Export progress information
/// </summary>
public sealed record ExportProgress
{
    public int ProcessedRows { get; init; }
    public int TotalRows { get; init; }
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    public TimeSpan ElapsedTime { get; init; }
    public string CurrentOperation { get; init; } = string.Empty;
    public long ProcessedBytes { get; init; }
    public long EstimatedTotalBytes { get; init; }

    public static ExportProgress Create(
        int processed,
        int total,
        TimeSpan elapsed,
        string operation = "",
        long processedBytes = 0,
        long estimatedBytes = 0) =>
        new()
        {
            ProcessedRows = processed,
            TotalRows = total,
            ElapsedTime = elapsed,
            CurrentOperation = operation,
            ProcessedBytes = processedBytes,
            EstimatedTotalBytes = estimatedBytes
        };
}

/// <summary>
/// PUBLIC API: Export result with statistics
/// </summary>
public sealed record ExportResult
{
    public bool Success { get; init; }
    public int ExportedRows { get; init; }
    public int TotalRows { get; init; }
    public TimeSpan ExportTime { get; init; }
    public long DataSize { get; init; }
    public ExportFormat Format { get; init; }
    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> WarningMessages { get; init; } = Array.Empty<string>();

    public static ExportResult CreateSuccess(
        int exportedRows,
        int totalRows,
        TimeSpan exportTime,
        ExportFormat format,
        long dataSize = 0) =>
        new()
        {
            Success = true,
            ExportedRows = exportedRows,
            TotalRows = totalRows,
            ExportTime = exportTime,
            Format = format,
            DataSize = dataSize
        };

    public static ExportResult Failure(IReadOnlyList<string> errors, TimeSpan exportTime, ExportFormat format) =>
        new()
        {
            Success = false,
            ErrorMessages = errors,
            ExportTime = exportTime,
            Format = format
        };
}
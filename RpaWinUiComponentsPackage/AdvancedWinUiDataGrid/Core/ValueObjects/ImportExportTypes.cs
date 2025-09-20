using System;
using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// CORE ENUM: Import mode enumeration
/// </summary>
internal enum ImportMode
{
    Replace,
    Append,
    Insert,
    Merge
}

/// <summary>
/// CORE VALUE OBJECT: Import options configuration
/// </summary>
internal sealed record ImportOptions
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
/// CORE VALUE OBJECT: Export options configuration
/// </summary>
internal sealed record ExportOptions
{
    public bool IncludeHeaders { get; init; } = true;
    public IReadOnlyList<string>? ColumnsToExport { get; init; }
    public string? DateTimeFormat { get; init; }
    public IProgress<ExportProgress>? Progress { get; init; }

    public static ExportOptions Default => new();
}

/// <summary>
/// CORE VALUE OBJECT: Import progress information
/// </summary>
internal sealed record ImportProgress
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
/// CORE VALUE OBJECT: Export progress information
/// </summary>
internal sealed record ExportProgress
{
    public int ProcessedRows { get; init; }
    public int TotalRows { get; init; }
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    public TimeSpan ElapsedTime { get; init; }
    public string CurrentOperation { get; init; } = string.Empty;

    public static ExportProgress Create(int processed, int total, TimeSpan elapsed, string operation = "") =>
        new()
        {
            ProcessedRows = processed,
            TotalRows = total,
            ElapsedTime = elapsed,
            CurrentOperation = operation
        };
}

/// <summary>
/// CORE VALUE OBJECT: Import operation result
/// </summary>
internal sealed record ImportResult
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
/// CORE VALUE OBJECT: Copy/Paste operation result
/// </summary>
internal sealed record CopyPasteResult
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
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// PUBLIC API TYPES: Public DTO and enums with mapping to internal types
/// Internal types remain internal, accessed via facade mapping pattern
/// </summary>

// Public API enums - part of public API
public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    IsNull,
    IsNotNull,
    IsEmpty,
    IsNotEmpty,
    Regex
}

public enum FilterLogicOperator
{
    And,
    Or
}

public enum SearchScope
{
    AllData,
    VisibleData,
    SelectedData
}

public sealed record FilterDefinition
{
    public string? ColumnName { get; init; }
    public FilterOperator Operator { get; init; }
    public object? Value { get; init; }
    public object? SecondValue { get; init; }
    public FilterLogicOperator LogicOperator { get; init; } = FilterLogicOperator.And;
    public string? FilterName { get; init; }

    public static FilterDefinition Equals(string columnName, object value) =>
        new() { ColumnName = columnName, Operator = FilterOperator.Equals, Value = value };

    public static FilterDefinition Contains(string columnName, string value) =>
        new() { ColumnName = columnName, Operator = FilterOperator.Contains, Value = value };

    public static FilterDefinition GreaterThan(string columnName, object value) =>
        new() { ColumnName = columnName, Operator = FilterOperator.GreaterThan, Value = value };

    public static FilterDefinition Regex(string columnName, string pattern) =>
        new() { ColumnName = columnName, Operator = FilterOperator.Regex, Value = pattern };
}

public sealed record AdvancedFilter
{
    public string? ColumnName { get; init; }
    public FilterOperator Operator { get; init; }
    public object? Value { get; init; }
    public object? SecondValue { get; init; }
    public FilterLogicOperator LogicOperator { get; init; } = FilterLogicOperator.And;
    public bool GroupStart { get; init; }
    public bool GroupEnd { get; init; }
    public string? FilterName { get; init; }

    public static AdvancedFilter Equals(string columnName, object value, FilterLogicOperator logicOperator = FilterLogicOperator.And, bool groupStart = false, bool groupEnd = false) =>
        new()
        {
            ColumnName = columnName,
            Operator = FilterOperator.Equals,
            Value = value,
            LogicOperator = logicOperator,
            GroupStart = groupStart,
            GroupEnd = groupEnd
        };

    public static AdvancedFilter Contains(string columnName, string value, FilterLogicOperator logicOperator = FilterLogicOperator.And, bool groupStart = false, bool groupEnd = false) =>
        new()
        {
            ColumnName = columnName,
            Operator = FilterOperator.Contains,
            Value = value,
            LogicOperator = logicOperator,
            GroupStart = groupStart,
            GroupEnd = groupEnd
        };
}

public sealed record AdvancedSearchCriteria
{
    public string SearchText { get; init; } = string.Empty;
    public string[]? TargetColumns { get; init; }
    public bool UseRegex { get; init; }
    public bool CaseSensitive { get; init; }
    public SearchScope Scope { get; init; } = SearchScope.AllData;
    public int? MaxMatches { get; init; }
    public TimeSpan? Timeout { get; init; } = TimeSpan.FromSeconds(5);
}

public sealed record SearchResult
{
    public int RowIndex { get; init; }
    public string ColumnName { get; init; } = string.Empty;
    public object? Value { get; init; }
    public string? MatchedText { get; init; }
    public int MatchStartIndex { get; init; }
    public int MatchLength { get; init; }

    public static SearchResult Create(int rowIndex, string columnName, object? value, string? matchedText = null) =>
        new()
        {
            RowIndex = rowIndex,
            ColumnName = columnName,
            Value = value,
            MatchedText = matchedText
        };
}

public sealed record FilterResult
{
    public int TotalRowsProcessed { get; init; }
    public int MatchingRows { get; init; }
    public int FilteredOutRows { get; init; }
    public TimeSpan ProcessingTime { get; init; }
    public IReadOnlyList<int> MatchingRowIndices { get; init; } = Array.Empty<int>();

    public static FilterResult Create(int total, int matching, TimeSpan processingTime, IReadOnlyList<int>? matchingIndices = null) =>
        new()
        {
            TotalRowsProcessed = total,
            MatchingRows = matching,
            FilteredOutRows = total - matching,
            ProcessingTime = processingTime,
            MatchingRowIndices = matchingIndices ?? Array.Empty<int>()
        };
}

// Sort Types
public enum SortDirection
{
    None = 0,
    Ascending = 1,
    Descending = 2
}

public sealed record SortColumnConfiguration
{
    public string ColumnName { get; init; } = string.Empty;
    public SortDirection Direction { get; init; } = SortDirection.None;
    public int Priority { get; init; } = 0;
    public bool IsPrimary { get; init; } = false;

    public static SortColumnConfiguration Create(string columnName, SortDirection direction, int priority = 0) =>
        new()
        {
            ColumnName = columnName,
            Direction = direction,
            Priority = priority,
            IsPrimary = priority == 0
        };
}

public sealed record SortResult
{
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> SortedData { get; init; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
    public IReadOnlyList<SortColumnConfiguration> AppliedSorts { get; init; } = Array.Empty<SortColumnConfiguration>();
    public TimeSpan SortTime { get; init; }
    public int ProcessedRows { get; init; }

    public static SortResult Create(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> sortedData,
        IReadOnlyList<SortColumnConfiguration> appliedSorts,
        TimeSpan sortTime) =>
        new()
        {
            SortedData = sortedData,
            AppliedSorts = appliedSorts,
            SortTime = sortTime,
            ProcessedRows = sortedData.Count
        };

    public static SortResult Empty => new();
}

// Import/Export Types
public enum ImportMode
{
    Replace,
    Append,
    Insert,
    Merge
}

public sealed record ImportOptions
{
    public ImportMode Mode { get; init; } = ImportMode.Replace;
    public int StartRowIndex { get; init; } = 0;
    public bool ValidateBeforeImport { get; init; } = true;
    public bool CreateMissingColumns { get; init; } = true;
    public Dictionary<string, string>? ColumnMapping { get; init; }
    public IProgress<double>? Progress { get; init; }

    public static ImportOptions Default => new();
}

public sealed record ExportOptions
{
    public bool IncludeHeaders { get; init; } = true;
    public IReadOnlyList<string>? ColumnsToExport { get; init; }
    public string? DateTimeFormat { get; init; }
    public IProgress<double>? Progress { get; init; }

    public static ExportOptions Default => new();
}

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

// Validation Types
public enum ValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

public readonly record struct ValidationResult
{
    public bool IsValid { get; init; }
    public ValidationSeverity Severity { get; init; }
    public string Message { get; init; }
    public string? RuleName { get; init; }
    public TimeSpan ValidationTime { get; init; }
    public object? Value { get; init; }
    public bool IsTimeout { get; init; }

    public static ValidationResult Valid() => new() { IsValid = true, Message = "Valid" };
    public static ValidationResult Success() => Valid();
    public static ValidationResult Error(string message, ValidationSeverity severity = ValidationSeverity.Error, string? ruleName = null) =>
        new() { IsValid = false, Message = message, Severity = severity, RuleName = ruleName };
    public static ValidationResult Invalid(string message, ValidationSeverity severity = ValidationSeverity.Error, string? ruleName = null) =>
        new() { IsValid = false, Message = message, Severity = severity, RuleName = ruleName };
    public static ValidationResult Timeout(string? ruleName = null) =>
        new() { IsValid = false, Message = "Timeout", Severity = ValidationSeverity.Error, RuleName = ruleName, IsTimeout = true };
}

// Keyboard Shortcuts Types
public sealed record KeyboardShortcut
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string KeyCombination { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;

    public static KeyboardShortcut Create(string name, string keyCombo, string description) =>
        new() { Name = name, KeyCombination = keyCombo, Description = description };
}

public sealed record KeyboardShortcutResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public string? ShortcutName { get; init; }

    public static KeyboardShortcutResult CreateSuccess(string shortcutName, TimeSpan executionTime) =>
        new() { Success = true, ShortcutName = shortcutName, ExecutionTime = executionTime };

    public static KeyboardShortcutResult Failure(string errorMessage, string? shortcutName = null) =>
        new() { Success = false, ErrorMessage = errorMessage, ShortcutName = shortcutName };
}

// Performance Types
public sealed record PerformanceMetrics
{
    public long TotalOperations { get; init; }
    public TimeSpan TotalTime { get; init; }
    public double AverageTime { get; init; }
    public long MemoryUsage { get; init; }
    public DateTime StartTime { get; init; }
    public Dictionary<string, object> AdditionalMetrics { get; init; } = new();

    public static PerformanceMetrics Create(long operations, TimeSpan totalTime, long memoryUsage) =>
        new()
        {
            TotalOperations = operations,
            TotalTime = totalTime,
            AverageTime = operations > 0 ? totalTime.TotalMilliseconds / operations : 0,
            MemoryUsage = memoryUsage,
            StartTime = DateTime.UtcNow.Subtract(totalTime)
        };
}

// AutoRowHeight Types
public sealed record AutoRowHeightConfiguration
{
    public bool IsEnabled { get; init; } = true;
    public double MinRowHeight { get; init; } = 20;
    public double MaxRowHeight { get; init; } = 200;
    public double DefaultRowHeight { get; init; } = 25;
    public bool EnableTextWrapping { get; init; } = true;
    public double Padding { get; init; } = 4;

    // Additional properties needed by services
    public bool EnableMeasurementCache { get; init; } = true;
    public double FontSize { get; init; } = 12;
    public string FontFamily { get; init; } = "Segoe UI";
    public double CellPadding { get; init; } = 4;
    public double LineHeight { get; init; } = 1.2;
    public bool EnableTextTrimming { get; init; } = false;
    public bool TextWrapping { get; init; } = true;
    public double MinimumRowHeight { get; init; } = 20;
    public double MaximumRowHeight { get; init; } = 200;

    public static AutoRowHeightConfiguration Default => new();
}

public sealed record RowHeightCalculationOptions
{
    public double MaxWidth { get; init; } = 200;
    public bool IncludeHeaders { get; init; } = true;
    public bool UseCache { get; init; } = true;
    public TimeSpan CacheTimeout { get; init; } = TimeSpan.FromMinutes(5);

    // Additional properties needed by services
    public IProgress<double>? Progress { get; init; }
    public double MinimumRowHeight { get; init; } = 20;
    public double MaximumRowHeight { get; init; } = 200;
    public string[]? SpecificColumns { get; init; }

    public static RowHeightCalculationOptions Default => new();
}

public sealed record AutoRowHeightResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<int, double> CalculatedHeights { get; init; } = new();
    public TimeSpan CalculationTime { get; init; }

    public static AutoRowHeightResult CreateSuccess(Dictionary<int, double> heights, TimeSpan calculationTime) =>
        new() { Success = true, CalculatedHeights = heights, CalculationTime = calculationTime };

    public static AutoRowHeightResult Failure(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
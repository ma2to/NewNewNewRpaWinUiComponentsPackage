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

// Comprehensive Validation Types for Public API
public enum ValidationTrigger
{
    Manual = 0,
    OnCellChanged = 1,
    OnTextChanged = 2,
    OnCellExit = 3,
    OnRowComplete = 4,
    Bulk = 5
}

public enum ValidationDeletionMode
{
    DeleteInvalidRows = 0,
    DeleteValidRows = 1,
    DeleteByCustomRule = 2,
    DeleteBySeverity = 3,
    DeleteByRuleName = 4
}

// Group Validation Enums
public enum ValidationLogicalOperator
{
    And = 0,
    Or = 1,
    AndAlso = 2,
    OrElse = 3
}

public enum ColumnValidationPolicy
{
    StopOnFirstError = 0,
    ValidateAll = 1,
    StopOnFirstSuccess = 2
}

public enum ValidationEvaluationStrategy
{
    Sequential = 0,
    Parallel = 1,
    ShortCircuit = 2
}

// 1️⃣ Single Cell Validation Rule
public sealed record ValidationRule
{
    public string ColumnName { get; init; } = string.Empty;
    public Func<object?, Task<bool>>? AsyncValidator { get; init; }
    public Func<object?, bool>? Validator { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public int? Priority { get; init; }
    public string? RuleName { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
    public IReadOnlyList<string>? DependentColumns { get; init; }

    public static ValidationRule Create(string columnName, Func<object?, bool> validator, string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error, int? priority = null, string? ruleName = null,
        TimeSpan? timeout = null, IReadOnlyList<string>? dependentColumns = null) =>
        new()
        {
            ColumnName = columnName,
            Validator = validator,
            ErrorMessage = errorMessage,
            Severity = severity,
            Priority = priority,
            RuleName = ruleName,
            Timeout = timeout ?? TimeSpan.FromSeconds(2),
            DependentColumns = dependentColumns
        };

    public static ValidationRule CreateAsync(string columnName, Func<object?, Task<bool>> asyncValidator, string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error, int? priority = null, string? ruleName = null,
        TimeSpan? timeout = null, IReadOnlyList<string>? dependentColumns = null) =>
        new()
        {
            ColumnName = columnName,
            AsyncValidator = asyncValidator,
            ErrorMessage = errorMessage,
            Severity = severity,
            Priority = priority,
            RuleName = ruleName,
            Timeout = timeout ?? TimeSpan.FromSeconds(2),
            DependentColumns = dependentColumns
        };
}

// 2️⃣ Cross-Column Validation Rule
public sealed record CrossColumnValidationRule
{
    public IReadOnlyList<string> DependentColumns { get; init; } = Array.Empty<string>();
    public Func<IReadOnlyDictionary<string, object?>, Task<ValidationResult>>? AsyncValidator { get; init; }
    public Func<IReadOnlyDictionary<string, object?>, (bool isValid, string? errorMessage)>? Validator { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public int? Priority { get; init; }
    public string? RuleName { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
    public string? PrimaryColumn { get; init; }

    public static CrossColumnValidationRule Create(
        IReadOnlyList<string> dependentColumns,
        Func<IReadOnlyDictionary<string, object?>, (bool isValid, string? errorMessage)> validator,
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        int? priority = null,
        string? ruleName = null,
        TimeSpan? timeout = null,
        string? primaryColumn = null) =>
        new()
        {
            DependentColumns = dependentColumns,
            Validator = validator,
            ErrorMessage = errorMessage,
            Severity = severity,
            Priority = priority,
            RuleName = ruleName,
            Timeout = timeout ?? TimeSpan.FromSeconds(2),
            PrimaryColumn = primaryColumn
        };
}

// 3️⃣ Cross-Row Validation Rule
public sealed record CrossRowValidationRule
{
    public Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, Task<IReadOnlyList<ValidationResult>>>? AsyncValidator { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public int? Priority { get; init; }
    public string? RuleName { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
    public IReadOnlyList<string>? AffectedColumns { get; init; }

    public static CrossRowValidationRule Create(
        Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, Task<IReadOnlyList<ValidationResult>>> asyncValidator,
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        int? priority = null,
        string? ruleName = null,
        TimeSpan? timeout = null,
        IReadOnlyList<string>? affectedColumns = null) =>
        new()
        {
            AsyncValidator = asyncValidator,
            ErrorMessage = errorMessage,
            Severity = severity,
            Priority = priority,
            RuleName = ruleName,
            Timeout = timeout ?? TimeSpan.FromSeconds(2),
            AffectedColumns = affectedColumns
        };
}

// 4️⃣ Conditional Validation Rule
public sealed record ConditionalValidationRule
{
    public string ColumnName { get; init; } = string.Empty;
    public Func<IReadOnlyDictionary<string, object?>, bool>? Condition { get; init; }
    public ValidationRule? ValidationRule { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public int? Priority { get; init; }
    public string? RuleName { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
    public IReadOnlyList<string>? DependentColumns { get; init; }

    public static ConditionalValidationRule Create(
        string columnName,
        Func<IReadOnlyDictionary<string, object?>, bool> condition,
        ValidationRule validationRule,
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        int? priority = null,
        string? ruleName = null,
        TimeSpan? timeout = null,
        IReadOnlyList<string>? dependentColumns = null) =>
        new()
        {
            ColumnName = columnName,
            Condition = condition,
            ValidationRule = validationRule,
            ErrorMessage = errorMessage,
            Severity = severity,
            Priority = priority,
            RuleName = ruleName,
            Timeout = timeout ?? TimeSpan.FromSeconds(2),
            DependentColumns = dependentColumns
        };
}

// 5️⃣ Complex Validation Rule
public sealed record ComplexValidationRule
{
    public Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, Task<ValidationResult>>? AsyncValidator { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public int? Priority { get; init; }
    public string? RuleName { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
    public IReadOnlyList<string>? InvolvedColumns { get; init; }

    public static ComplexValidationRule Create(
        Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, Task<ValidationResult>> asyncValidator,
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        int? priority = null,
        string? ruleName = null,
        TimeSpan? timeout = null,
        IReadOnlyList<string>? involvedColumns = null) =>
        new()
        {
            AsyncValidator = asyncValidator,
            ErrorMessage = errorMessage,
            Severity = severity,
            Priority = priority,
            RuleName = ruleName,
            Timeout = timeout ?? TimeSpan.FromSeconds(2),
            InvolvedColumns = involvedColumns
        };
}

// 6️⃣ Business Rule Validation
public sealed record BusinessRuleValidationRule
{
    public string BusinessRuleName { get; init; } = string.Empty;
    public string RuleScope { get; init; } = string.Empty;
    public Func<object, Task<ValidationResult>>? AsyncValidator { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public int? Priority { get; init; }
    public string? RuleName { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
    public IReadOnlyList<string>? AffectedColumns { get; init; }

    public static BusinessRuleValidationRule Create(
        string businessRuleName,
        string ruleScope,
        Func<object, Task<ValidationResult>> asyncValidator,
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        int? priority = null,
        string? ruleName = null,
        TimeSpan? timeout = null,
        IReadOnlyList<string>? affectedColumns = null) =>
        new()
        {
            BusinessRuleName = businessRuleName,
            RuleScope = ruleScope,
            AsyncValidator = asyncValidator,
            ErrorMessage = errorMessage,
            Severity = severity,
            Priority = priority,
            RuleName = ruleName,
            Timeout = timeout ?? TimeSpan.FromSeconds(2),
            AffectedColumns = affectedColumns
        };
}

// 7️⃣ Validation Rule Group - Advanced logical combinations
public sealed record ValidationRuleGroup
{
    public string ColumnName { get; init; } = string.Empty;
    public IReadOnlyList<ValidationRule> Rules { get; init; } = Array.Empty<ValidationRule>();
    public ValidationLogicalOperator LogicalOperator { get; init; } = ValidationLogicalOperator.And;
    public ColumnValidationPolicy ValidationPolicy { get; init; } = ColumnValidationPolicy.ValidateAll;
    public ValidationEvaluationStrategy EvaluationStrategy { get; init; } = ValidationEvaluationStrategy.Sequential;
    public string? RuleName { get; init; }
    public string? ErrorMessage { get; init; }
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    public int? Priority { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
    public IReadOnlyList<ValidationRuleGroup>? ChildGroups { get; init; }

    public static ValidationRuleGroup CreateAndGroup(string columnName, params ValidationRule[] rules) =>
        new()
        {
            ColumnName = columnName,
            Rules = rules,
            LogicalOperator = ValidationLogicalOperator.And,
            ValidationPolicy = ColumnValidationPolicy.ValidateAll,
            EvaluationStrategy = ValidationEvaluationStrategy.Sequential
        };

    public static ValidationRuleGroup CreateOrGroup(string columnName, params ValidationRule[] rules) =>
        new()
        {
            ColumnName = columnName,
            Rules = rules,
            LogicalOperator = ValidationLogicalOperator.Or,
            ValidationPolicy = ColumnValidationPolicy.StopOnFirstSuccess,
            EvaluationStrategy = ValidationEvaluationStrategy.ShortCircuit
        };

    public static ValidationRuleGroup CreateFailFastGroup(string columnName, params ValidationRule[] rules) =>
        new()
        {
            ColumnName = columnName,
            Rules = rules,
            LogicalOperator = ValidationLogicalOperator.And,
            ValidationPolicy = ColumnValidationPolicy.StopOnFirstError,
            EvaluationStrategy = ValidationEvaluationStrategy.ShortCircuit
        };

    public static ValidationRuleGroup CreateAndAlsoGroup(string columnName, params ValidationRule[] rules) =>
        new()
        {
            ColumnName = columnName,
            Rules = rules,
            LogicalOperator = ValidationLogicalOperator.AndAlso,
            ValidationPolicy = ColumnValidationPolicy.ValidateAll,
            EvaluationStrategy = ValidationEvaluationStrategy.Sequential
        };

    public static ValidationRuleGroup CreateOrElseGroup(string columnName, params ValidationRule[] rules) =>
        new()
        {
            ColumnName = columnName,
            Rules = rules,
            LogicalOperator = ValidationLogicalOperator.OrElse,
            ValidationPolicy = ColumnValidationPolicy.ValidateAll,
            EvaluationStrategy = ValidationEvaluationStrategy.Sequential
        };

    public static ValidationRuleGroup CreateHierarchicalGroup(
        string columnName,
        ValidationLogicalOperator logicalOperator,
        string? groupName = null,
        params ValidationRuleGroup[] childGroups) =>
        new()
        {
            ColumnName = columnName,
            Rules = Array.Empty<ValidationRule>(),
            LogicalOperator = logicalOperator,
            ValidationPolicy = ColumnValidationPolicy.ValidateAll,
            EvaluationStrategy = ValidationEvaluationStrategy.Sequential,
            RuleName = groupName,
            ChildGroups = childGroups
        };
}

// Column Validation Configuration
public sealed record ColumnValidationConfiguration
{
    public string ColumnName { get; init; } = string.Empty;
    public ColumnValidationPolicy ValidationPolicy { get; init; } = ColumnValidationPolicy.ValidateAll;
    public ValidationEvaluationStrategy EvaluationStrategy { get; init; } = ValidationEvaluationStrategy.Sequential;
    public ValidationLogicalOperator DefaultLogicalOperator { get; init; } = ValidationLogicalOperator.And;
    public TimeSpan? ColumnTimeout { get; init; }
    public bool AllowRuleGroups { get; init; } = true;

    public static ColumnValidationConfiguration FailFast(string columnName) =>
        new()
        {
            ColumnName = columnName,
            ValidationPolicy = ColumnValidationPolicy.StopOnFirstError,
            EvaluationStrategy = ValidationEvaluationStrategy.ShortCircuit
        };

    public static ColumnValidationConfiguration SuccessFast(string columnName) =>
        new()
        {
            ColumnName = columnName,
            ValidationPolicy = ColumnValidationPolicy.StopOnFirstSuccess,
            EvaluationStrategy = ValidationEvaluationStrategy.ShortCircuit,
            DefaultLogicalOperator = ValidationLogicalOperator.Or
        };

    public static ColumnValidationConfiguration Parallel(string columnName) =>
        new()
        {
            ColumnName = columnName,
            ValidationPolicy = ColumnValidationPolicy.ValidateAll,
            EvaluationStrategy = ValidationEvaluationStrategy.Parallel
        };
}

// Enhanced Validation Configuration
public sealed record ValidationConfiguration
{
    public bool EnableValidation { get; init; } = true;
    public ValidationTrigger DefaultTrigger { get; init; } = ValidationTrigger.OnCellChanged;
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(2);
    public bool EnableRealTimeValidation { get; init; } = true;
    public bool EnableBulkValidation { get; init; } = true;
    public int MaxConcurrentValidations { get; init; } = 10;
    public bool MakeValidateAllStopOnFirstError { get; init; } = false;
    public bool ValidateOnlyVisibleRows { get; init; } = false;

    // New group validation properties
    public ColumnValidationPolicy DefaultColumnPolicy { get; init; } = ColumnValidationPolicy.ValidateAll;
    public ValidationEvaluationStrategy DefaultEvaluationStrategy { get; init; } = ValidationEvaluationStrategy.Sequential;
    public bool EnableGroupValidation { get; init; } = true;
    public IReadOnlyDictionary<string, ColumnValidationConfiguration>? ColumnSpecificConfigurations { get; init; }

    public static ValidationConfiguration Default => new();
    public static ValidationConfiguration RealTime => new()
    {
        EnableRealTimeValidation = true,
        DefaultTrigger = ValidationTrigger.OnTextChanged,
        EnableBulkValidation = false,
        DefaultEvaluationStrategy = ValidationEvaluationStrategy.ShortCircuit
    };
    public static ValidationConfiguration Bulk => new()
    {
        EnableRealTimeValidation = false,
        DefaultTrigger = ValidationTrigger.Bulk,
        EnableBulkValidation = true,
        DefaultEvaluationStrategy = ValidationEvaluationStrategy.Parallel
    };
    public static ValidationConfiguration HighPerformance => new()
    {
        EnableRealTimeValidation = false,
        DefaultTrigger = ValidationTrigger.OnCellExit,
        MaxConcurrentValidations = 20,
        ValidateOnlyVisibleRows = true,
        DefaultColumnPolicy = ColumnValidationPolicy.StopOnFirstError,
        DefaultEvaluationStrategy = ValidationEvaluationStrategy.ShortCircuit
    };
}

// Validation Deletion Types
public sealed record ValidationDeletionCriteria
{
    public ValidationDeletionMode Mode { get; init; }
    public IReadOnlyList<ValidationSeverity>? Severities { get; init; }
    public IReadOnlyList<string>? SpecificRuleNames { get; init; }
    public Func<IReadOnlyDictionary<string, object?>, bool>? CustomPredicate { get; init; }
    public ValidationSeverity? MinimumSeverity { get; init; }

    public static ValidationDeletionCriteria DeleteInvalidRows(ValidationSeverity? minimumSeverity = null) =>
        new() { Mode = ValidationDeletionMode.DeleteInvalidRows, MinimumSeverity = minimumSeverity };

    public static ValidationDeletionCriteria DeleteValidRows() =>
        new() { Mode = ValidationDeletionMode.DeleteValidRows };

    public static ValidationDeletionCriteria DeleteBySeverity(params ValidationSeverity[] severities) =>
        new() { Mode = ValidationDeletionMode.DeleteBySeverity, Severities = severities };

    public static ValidationDeletionCriteria DeleteByRuleName(params string[] ruleNames) =>
        new() { Mode = ValidationDeletionMode.DeleteByRuleName, SpecificRuleNames = ruleNames };

    public static ValidationDeletionCriteria DeleteByCustomRule(Func<IReadOnlyDictionary<string, object?>, bool> predicate) =>
        new() { Mode = ValidationDeletionMode.DeleteByCustomRule, CustomPredicate = predicate };
}

public sealed record ValidationDeletionOptions
{
    public bool RequireConfirmation { get; init; } = true;
    public IProgress<double>? Progress { get; init; }
    public bool PreviewMode { get; init; } = false;
    public int BatchSize { get; init; } = 1000;
    public TimeSpan MaxOperationTime { get; init; } = TimeSpan.FromMinutes(5);

    public static ValidationDeletionOptions Default => new();
    public static ValidationDeletionOptions NoConfirmation => new() { RequireConfirmation = false };
    public static ValidationDeletionOptions PreviewOnly => new() { PreviewMode = true };
}

public sealed record ValidationBasedDeleteResult
{
    public int TotalRowsEvaluated { get; init; }
    public int RowsDeleted { get; init; }
    public int RemainingRows { get; init; }
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();
    public TimeSpan OperationDuration { get; init; }
    public bool Success { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<int> DeletedRowIndices { get; init; } = Array.Empty<int>();

    public static ValidationBasedDeleteResult CreateSuccess(int totalEvaluated, int deleted, int remaining, TimeSpan duration, IReadOnlyList<int>? deletedIndices = null) =>
        new()
        {
            TotalRowsEvaluated = totalEvaluated,
            RowsDeleted = deleted,
            RemainingRows = remaining,
            OperationDuration = duration,
            Success = true,
            DeletedRowIndices = deletedIndices ?? Array.Empty<int>()
        };

    public static ValidationBasedDeleteResult CreateFailure(string errorMessage, TimeSpan duration) =>
        new()
        {
            Success = false,
            ErrorMessage = errorMessage,
            OperationDuration = duration
        };
}
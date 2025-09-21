using System;
using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// CORE: Configuration for validation system
/// ENTERPRISE: Professional validation configuration with comprehensive settings
/// </summary>
internal readonly record struct ValidationConfiguration
{
    public bool EnableValidation { get; }
    public ValidationTrigger DefaultTrigger { get; }
    public TimeSpan DefaultTimeout { get; }
    public bool EnableRealTimeValidation { get; }
    public bool EnableBulkValidation { get; }
    public int MaxConcurrentValidations { get; }
    public bool MakeValidateAllStopOnFirstError { get; }
    public bool ValidateOnlyVisibleRows { get; }

    // New properties for group validation support
    public ColumnValidationPolicy DefaultColumnPolicy { get; }
    public ValidationEvaluationStrategy DefaultEvaluationStrategy { get; }
    public bool EnableGroupValidation { get; }
    public IReadOnlyDictionary<string, ColumnValidationConfiguration>? ColumnSpecificConfigurations { get; }

    public ValidationConfiguration(
        bool enableValidation = true,
        ValidationTrigger defaultTrigger = ValidationTrigger.OnCellChanged,
        TimeSpan? defaultTimeout = null,
        bool enableRealTimeValidation = true,
        bool enableBulkValidation = true,
        int maxConcurrentValidations = 10,
        bool makeValidateAllStopOnFirstError = false,
        bool validateOnlyVisibleRows = false,
        ColumnValidationPolicy defaultColumnPolicy = ColumnValidationPolicy.ValidateAll,
        ValidationEvaluationStrategy defaultEvaluationStrategy = ValidationEvaluationStrategy.Sequential,
        bool enableGroupValidation = true,
        IReadOnlyDictionary<string, ColumnValidationConfiguration>? columnSpecificConfigurations = null)
    {
        EnableValidation = enableValidation;
        DefaultTrigger = defaultTrigger;
        DefaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(2);
        EnableRealTimeValidation = enableRealTimeValidation;
        EnableBulkValidation = enableBulkValidation;
        MaxConcurrentValidations = maxConcurrentValidations;
        MakeValidateAllStopOnFirstError = makeValidateAllStopOnFirstError;
        ValidateOnlyVisibleRows = validateOnlyVisibleRows;
        DefaultColumnPolicy = defaultColumnPolicy;
        DefaultEvaluationStrategy = defaultEvaluationStrategy;
        EnableGroupValidation = enableGroupValidation;
        ColumnSpecificConfigurations = columnSpecificConfigurations;
    }

    public static ValidationConfiguration Default => new();

    public static ValidationConfiguration RealTime => new(
        enableRealTimeValidation: true,
        defaultTrigger: ValidationTrigger.OnTextChanged,
        enableBulkValidation: false);

    public static ValidationConfiguration Bulk => new(
        enableRealTimeValidation: false,
        defaultTrigger: ValidationTrigger.Bulk,
        enableBulkValidation: true);

    public static ValidationConfiguration HighPerformance => new(
        enableRealTimeValidation: false,
        defaultTrigger: ValidationTrigger.OnCellExit,
        maxConcurrentValidations: 20,
        validateOnlyVisibleRows: true);
}

/// <summary>
/// CORE: Criteria for validation-based row deletion
/// ENTERPRISE: Professional row deletion configuration based on validation state
/// </summary>
internal readonly record struct ValidationDeletionCriteria
{
    public ValidationDeletionMode Mode { get; }
    public IReadOnlyList<ValidationSeverity>? Severities { get; }
    public IReadOnlyList<string>? SpecificRuleNames { get; }
    public Func<IReadOnlyDictionary<string, object?>, bool>? CustomPredicate { get; }
    public ValidationSeverity? MinimumSeverity { get; }

    public ValidationDeletionCriteria(
        ValidationDeletionMode mode,
        IReadOnlyList<ValidationSeverity>? severities = null,
        IReadOnlyList<string>? specificRuleNames = null,
        Func<IReadOnlyDictionary<string, object?>, bool>? customPredicate = null,
        ValidationSeverity? minimumSeverity = null)
    {
        Mode = mode;
        Severities = severities;
        SpecificRuleNames = specificRuleNames;
        CustomPredicate = customPredicate;
        MinimumSeverity = minimumSeverity;
    }

    public static ValidationDeletionCriteria DeleteInvalidRows(ValidationSeverity? minimumSeverity = null) =>
        new(ValidationDeletionMode.DeleteInvalidRows, minimumSeverity: minimumSeverity);

    public static ValidationDeletionCriteria DeleteValidRows() =>
        new(ValidationDeletionMode.DeleteValidRows);

    public static ValidationDeletionCriteria DeleteBySeverity(params ValidationSeverity[] severities) =>
        new(ValidationDeletionMode.DeleteBySeverity, severities);

    public static ValidationDeletionCriteria DeleteByRuleName(params string[] ruleNames) =>
        new(ValidationDeletionMode.DeleteByRuleName, specificRuleNames: ruleNames);

    public static ValidationDeletionCriteria DeleteByCustomRule(Func<IReadOnlyDictionary<string, object?>, bool> predicate) =>
        new(ValidationDeletionMode.DeleteByCustomRule, customPredicate: predicate);
}

/// <summary>
/// CORE: Options for validation-based row deletion
/// ENTERPRISE: Professional deletion options with progress reporting
/// </summary>
internal readonly record struct ValidationDeletionOptions
{
    public bool RequireConfirmation { get; }
    public IProgress<ValidationDeletionProgress>? Progress { get; }
    public bool PreviewMode { get; }
    public int BatchSize { get; }
    public TimeSpan MaxOperationTime { get; }

    public ValidationDeletionOptions(
        bool requireConfirmation = true,
        IProgress<ValidationDeletionProgress>? progress = null,
        bool previewMode = false,
        int batchSize = 1000,
        TimeSpan? maxOperationTime = null)
    {
        RequireConfirmation = requireConfirmation;
        Progress = progress;
        PreviewMode = previewMode;
        BatchSize = batchSize;
        MaxOperationTime = maxOperationTime ?? TimeSpan.FromMinutes(5);
    }

    public static ValidationDeletionOptions Default => new();
    public static ValidationDeletionOptions NoConfirmation => new(requireConfirmation: false);
    public static ValidationDeletionOptions PreviewOnly => new(previewMode: true);
}

/// <summary>
/// CORE: Progress information for validation-based deletion operations
/// ENTERPRISE: Professional progress tracking for long-running operations
/// </summary>
internal readonly record struct ValidationDeletionProgress
{
    public int TotalRows { get; }
    public int ProcessedRows { get; }
    public int RowsMarkedForDeletion { get; }
    public int ActuallyDeletedRows { get; }
    public TimeSpan ElapsedTime { get; }
    public string? CurrentOperation { get; }
    public double PercentComplete => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100.0 : 0.0;

    public ValidationDeletionProgress(
        int totalRows,
        int processedRows,
        int rowsMarkedForDeletion,
        int actuallyDeletedRows,
        TimeSpan elapsedTime,
        string? currentOperation = null)
    {
        TotalRows = totalRows;
        ProcessedRows = processedRows;
        RowsMarkedForDeletion = rowsMarkedForDeletion;
        ActuallyDeletedRows = actuallyDeletedRows;
        ElapsedTime = elapsedTime;
        CurrentOperation = currentOperation;
    }
}

/// <summary>
/// CORE: Result of validation-based row deletion operation
/// ENTERPRISE: Comprehensive result information for deletion operations
/// </summary>
internal readonly record struct ValidationBasedDeleteResult
{
    public int TotalRowsEvaluated { get; }
    public int RowsDeleted { get; }
    public int RemainingRows { get; }
    public IReadOnlyList<ValidationError> ValidationErrors { get; }
    public TimeSpan OperationDuration { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<int> DeletedRowIndices { get; }

    public ValidationBasedDeleteResult(
        int totalRowsEvaluated,
        int rowsDeleted,
        int remainingRows,
        IReadOnlyList<ValidationError> validationErrors,
        TimeSpan operationDuration,
        bool success = true,
        string? errorMessage = null,
        IReadOnlyList<int>? deletedRowIndices = null)
    {
        TotalRowsEvaluated = totalRowsEvaluated;
        RowsDeleted = rowsDeleted;
        RemainingRows = remainingRows;
        ValidationErrors = validationErrors;
        OperationDuration = operationDuration;
        Success = success;
        ErrorMessage = errorMessage;
        DeletedRowIndices = deletedRowIndices ?? Array.Empty<int>();
    }

    public static ValidationBasedDeleteResult CreateSuccess(
        int totalEvaluated,
        int deleted,
        int remaining,
        TimeSpan duration,
        IReadOnlyList<int>? deletedIndices = null) =>
        new(totalEvaluated, deleted, remaining, Array.Empty<ValidationError>(), duration,
            true, null, deletedIndices);

    public static ValidationBasedDeleteResult CreateFailure(
        string errorMessage,
        TimeSpan duration,
        IReadOnlyList<ValidationError>? errors = null) =>
        new(0, 0, 0, errors ?? Array.Empty<ValidationError>(), duration, false, errorMessage);
}

/// <summary>
/// CORE: Context for smart validation decision making
/// ENTERPRISE: Intelligence for determining when to use real-time vs bulk validation
/// </summary>
internal readonly record struct ValidationContext
{
    public ValidationTrigger Trigger { get; }
    public int AffectedRowCount { get; }
    public int AffectedColumnCount { get; }
    public bool IsImportOperation { get; }
    public bool IsPasteOperation { get; }
    public bool IsUserTyping { get; }
    public TimeSpan? AvailableTime { get; }
    public int ValidationRuleCount { get; }

    public ValidationContext(
        ValidationTrigger trigger,
        int affectedRowCount = 1,
        int affectedColumnCount = 1,
        bool isImportOperation = false,
        bool isPasteOperation = false,
        bool isUserTyping = false,
        TimeSpan? availableTime = null,
        int validationRuleCount = 0)
    {
        Trigger = trigger;
        AffectedRowCount = affectedRowCount;
        AffectedColumnCount = affectedColumnCount;
        IsImportOperation = isImportOperation;
        IsPasteOperation = isPasteOperation;
        IsUserTyping = isUserTyping;
        AvailableTime = availableTime;
        ValidationRuleCount = validationRuleCount;
    }

    /// <summary>
    /// SMART DECISION: Determine if bulk validation should be used
    /// PERFORMANCE: Optimize validation strategy based on context
    /// </summary>
    public bool ShouldUseBulkValidation =>
        IsImportOperation ||
        IsPasteOperation ||
        AffectedRowCount > 10 ||
        (AffectedRowCount * ValidationRuleCount) > 50 ||
        Trigger == ValidationTrigger.Bulk;

    /// <summary>
    /// SMART DECISION: Determine if real-time validation should be used
    /// UX: Provide immediate feedback when appropriate
    /// </summary>
    public bool ShouldUseRealTimeValidation =>
        !ShouldUseBulkValidation &&
        (IsUserTyping || Trigger == ValidationTrigger.OnTextChanged) &&
        AffectedRowCount <= 5 &&
        ValidationRuleCount <= 10;
}

/// <summary>
/// CORE: Column-specific validation configuration
/// ENTERPRISE: Fine-grained control over validation behavior per column
/// </summary>
internal readonly record struct ColumnValidationConfiguration
{
    public string ColumnName { get; }
    public ColumnValidationPolicy ValidationPolicy { get; }
    public ValidationEvaluationStrategy EvaluationStrategy { get; }
    public ValidationLogicalOperator DefaultLogicalOperator { get; }
    public TimeSpan? ColumnTimeout { get; }
    public bool AllowRuleGroups { get; }

    public ColumnValidationConfiguration(
        string columnName,
        ColumnValidationPolicy validationPolicy = ColumnValidationPolicy.ValidateAll,
        ValidationEvaluationStrategy evaluationStrategy = ValidationEvaluationStrategy.Sequential,
        ValidationLogicalOperator defaultLogicalOperator = ValidationLogicalOperator.And,
        TimeSpan? columnTimeout = null,
        bool allowRuleGroups = true)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        ValidationPolicy = validationPolicy;
        EvaluationStrategy = evaluationStrategy;
        DefaultLogicalOperator = defaultLogicalOperator;
        ColumnTimeout = columnTimeout;
        AllowRuleGroups = allowRuleGroups;
    }

    public static ColumnValidationConfiguration FailFast(string columnName) =>
        new(columnName, ColumnValidationPolicy.StopOnFirstError, ValidationEvaluationStrategy.ShortCircuit);

    public static ColumnValidationConfiguration SuccessFast(string columnName) =>
        new(columnName, ColumnValidationPolicy.StopOnFirstSuccess, ValidationEvaluationStrategy.ShortCircuit, ValidationLogicalOperator.Or);

    public static ColumnValidationConfiguration Parallel(string columnName) =>
        new(columnName, ColumnValidationPolicy.ValidateAll, ValidationEvaluationStrategy.Parallel);
}
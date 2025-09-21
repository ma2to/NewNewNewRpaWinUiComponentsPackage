using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// APPLICATION: Validation service interface for comprehensive validation operations
/// CLEAN ARCHITECTURE: Application layer contract for validation business logic
/// ENTERPRISE: Professional validation service with timeout support and smart decision making
/// </summary>
internal interface IValidationService
{
    #region Rule Management

    /// <summary>
    /// ENTERPRISE: Add validation rule to the system
    /// GENERIC: Supports all validation rule types through generic interface
    /// </summary>
    Task<Result<bool>> AddValidationRuleAsync<T>(T rule, CancellationToken cancellationToken = default)
        where T : IValidationRule;

    /// <summary>
    /// ENTERPRISE: Add validation rule group to the system
    /// GROUP: Supports complex logical combinations of validation rules
    /// </summary>
    Task<Result<bool>> AddValidationRuleGroupAsync(ValidationRuleGroup ruleGroup, CancellationToken cancellationToken = default);

    /// <summary>
    /// ENTERPRISE: Remove validation rules by column names
    /// BULK: Efficient removal of multiple rules
    /// </summary>
    Task<Result<bool>> RemoveValidationRulesAsync(IReadOnlyList<string> columnNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// ENTERPRISE: Remove validation rule by rule name
    /// TARGETED: Remove specific named rule
    /// </summary>
    Task<Result<bool>> RemoveValidationRuleAsync(string ruleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// ENTERPRISE: Clear all validation rules
    /// RESET: Complete validation system reset
    /// </summary>
    Task<Result<bool>> ClearAllValidationRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// QUERY: Get all validation rules currently registered
    /// MONITORING: System introspection for debugging and monitoring
    /// </summary>
    Task<Result<IReadOnlyList<IValidationRule>>> GetAllValidationRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONFIGURATION: Set column-specific validation configuration
    /// GRANULAR: Fine-grained control over validation behavior per column
    /// </summary>
    Task<Result<bool>> SetColumnValidationConfigurationAsync(string columnName, ColumnValidationConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// QUERY: Get column-specific validation configuration
    /// MONITORING: Retrieve current column validation settings
    /// </summary>
    Task<Result<ColumnValidationConfiguration?>> GetColumnValidationConfigurationAsync(string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// QUERY: Get all validation rule groups for a column
    /// MONITORING: Retrieve all groups affecting a specific column
    /// </summary>
    Task<Result<IReadOnlyList<ValidationRuleGroup>>> GetValidationRuleGroupsForColumnAsync(string columnName, CancellationToken cancellationToken = default);

    #endregion

    #region Validation Operations

    /// <summary>
    /// ENTERPRISE: Validate single cell with timeout support
    /// SMART: Automatic decision between real-time and deferred validation
    /// </summary>
    Task<ValidationResult> ValidateCellAsync(
        int rowIndex,
        string columnName,
        object? value,
        IReadOnlyDictionary<string, object?> rowData,
        ValidationContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ENTERPRISE: Validate entire row with all applicable rules
    /// COMPREHENSIVE: Cross-column and conditional validation support
    /// </summary>
    Task<IReadOnlyList<ValidationResult>> ValidateRowAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        ValidationContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ENTERPRISE: Validate multiple rows with cross-row rules
    /// BULK: Efficient batch validation with progress reporting
    /// </summary>
    Task<IReadOnlyList<ValidationResult>> ValidateRowsAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        ValidationContext? context = null,
        IProgress<double>? progress = null,
        bool validateOnlyVisibleRows = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ENTERPRISE: Validate entire dataset with all rule types
    /// COMPREHENSIVE: Full dataset validation including complex business rules
    /// </summary>
    Task<IReadOnlyList<ValidationResult>> ValidateDatasetAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        ValidationContext? context = null,
        IProgress<double>? progress = null,
        bool validateOnlyVisibleRows = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ENTERPRISE: Check if all non-empty rows are valid
    /// OVERVIEW: Quick validation status check for entire dataset
    /// </summary>
    Task<Result<bool>> AreAllNonEmptyRowsValidAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        bool onlyFilteredRows = false,
        bool validateOnlyVisibleRows = false,
        CancellationToken cancellationToken = default);

    #endregion

    #region Smart Validation Decision Making

    /// <summary>
    /// SMART: Determine optimal validation strategy based on context
    /// PERFORMANCE: Intelligent decision making for validation approach
    /// </summary>
    ValidationContext DetermineValidationContext(
        ValidationTrigger trigger,
        int affectedRowCount,
        int affectedColumnCount,
        bool isImportOperation = false,
        bool isPasteOperation = false,
        bool isUserTyping = false);

    /// <summary>
    /// SMART: Check if real-time validation should be used
    /// UX: Optimize user experience with appropriate validation timing
    /// </summary>
    bool ShouldUseRealTimeValidation(ValidationContext context);

    /// <summary>
    /// SMART: Check if bulk validation should be used
    /// PERFORMANCE: Optimize performance for large operations
    /// </summary>
    bool ShouldUseBulkValidation(ValidationContext context);

    #endregion

    #region Row Deletion Based on Validation

    /// <summary>
    /// ENTERPRISE: Delete rows based on validation criteria
    /// PROFESSIONAL: Batch operation with progress reporting and safety checks
    /// </summary>
    Task<Result<ValidationBasedDeleteResult>> DeleteRowsWithValidationAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        ValidationDeletionCriteria criteria,
        ValidationDeletionOptions? options = null,
        bool validateOnlyVisibleRows = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// PREVIEW: Preview which rows would be deleted without actual deletion
    /// SAFETY: Allow users to preview deletion impact before confirmation
    /// </summary>
    Task<Result<IReadOnlyList<int>>> PreviewRowDeletionAsync(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> dataset,
        ValidationDeletionCriteria criteria,
        bool validateOnlyVisibleRows = false,
        CancellationToken cancellationToken = default);

    #endregion

    #region Configuration and Status

    /// <summary>
    /// CONFIGURATION: Update validation system configuration
    /// RUNTIME: Dynamic configuration changes
    /// </summary>
    Task<Result<bool>> UpdateValidationConfigurationAsync(
        ValidationConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// QUERY: Get current validation configuration
    /// MONITORING: System configuration introspection
    /// </summary>
    Task<Result<ValidationConfiguration>> GetValidationConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// STATISTICS: Get validation performance metrics
    /// MONITORING: Performance and usage statistics
    /// </summary>
    Task<Result<ValidationStatistics>> GetValidationStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// CORE: Validation statistics for monitoring and performance analysis
/// ENTERPRISE: Professional metrics collection for validation operations
/// </summary>
internal readonly record struct ValidationStatistics
{
    public int TotalRulesRegistered { get; }
    public int TotalValidationsPerformed { get; }
    public int SuccessfulValidations { get; }
    public int FailedValidations { get; }
    public int TimeoutValidations { get; }
    public TimeSpan AverageValidationTime { get; }
    public TimeSpan TotalValidationTime { get; }
    public DateTime LastValidationTime { get; }
    public Dictionary<string, int> RuleTypeStatistics { get; }
    public Dictionary<ValidationSeverity, int> SeverityStatistics { get; }

    public ValidationStatistics(
        int totalRulesRegistered,
        int totalValidationsPerformed,
        int successfulValidations,
        int failedValidations,
        int timeoutValidations,
        TimeSpan averageValidationTime,
        TimeSpan totalValidationTime,
        DateTime lastValidationTime,
        Dictionary<string, int> ruleTypeStatistics,
        Dictionary<ValidationSeverity, int> severityStatistics)
    {
        TotalRulesRegistered = totalRulesRegistered;
        TotalValidationsPerformed = totalValidationsPerformed;
        SuccessfulValidations = successfulValidations;
        FailedValidations = failedValidations;
        TimeoutValidations = timeoutValidations;
        AverageValidationTime = averageValidationTime;
        TotalValidationTime = totalValidationTime;
        LastValidationTime = lastValidationTime;
        RuleTypeStatistics = ruleTypeStatistics;
        SeverityStatistics = severityStatistics;
    }

    public static ValidationStatistics Empty => new(
        0, 0, 0, 0, 0, TimeSpan.Zero, TimeSpan.Zero, DateTime.MinValue,
        new Dictionary<string, int>(), new Dictionary<ValidationSeverity, int>());
}
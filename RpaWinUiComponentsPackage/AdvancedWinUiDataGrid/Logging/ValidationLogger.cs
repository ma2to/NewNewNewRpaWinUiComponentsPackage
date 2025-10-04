using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Logging;

/// <summary>
/// Internal specialized logger for validation operations
/// Provides detailed logging and performance tracking for data validation scenarios
/// </summary>
internal sealed class ValidationLogger
{
    private readonly ILogger<ValidationLogger> _logger;

    public ValidationLogger(ILogger<ValidationLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log validation operation start
    /// </summary>
    /// <param name="operationId">Unique operation identifier</param>
    /// <param name="rowCount">Number of rows to validate</param>
    /// <param name="ruleCount">Number of validation rules</param>
    /// <param name="onlyFiltered">Whether validating only filtered data</param>
    /// <param name="validateBefore">Whether this is a pre-operation validation</param>
    public void LogValidationStart(Guid operationId, int rowCount, int ruleCount, bool onlyFiltered, bool validateBefore)
    {
        _logger.LogInformation("Validation operation started [{OperationId}]: Rows={RowCount}, Rules={RuleCount}, OnlyFiltered={OnlyFiltered}, ValidateBefore={ValidateBefore}",
            operationId, rowCount, ruleCount, onlyFiltered, validateBefore);
    }

    /// <summary>
    /// Log validation progress
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="validatedRows">Number of rows validated</param>
    /// <param name="totalRows">Total number of rows</param>
    /// <param name="currentRule">Current validation rule being applied</param>
    /// <param name="elapsedTime">Time elapsed since start</param>
    public void LogValidationProgress(Guid operationId, int validatedRows, int totalRows, string currentRule, TimeSpan elapsedTime)
    {
        var progressPercentage = totalRows > 0 ? (double)validatedRows / totalRows * 100 : 0;

        _logger.LogDebug("Validation progress [{OperationId}]: {ValidatedRows}/{TotalRows} ({ProgressPercentage:F1}%) - Rule: {CurrentRule}, Elapsed: {ElapsedTime}ms",
            operationId, validatedRows, totalRows, progressPercentage, currentRule, elapsedTime.TotalMilliseconds);
    }

    /// <summary>
    /// Log validation rule execution
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="ruleName">Name of the validation rule</param>
    /// <param name="ruleType">Type of validation rule</param>
    /// <param name="affectedRows">Number of rows affected by this rule</param>
    /// <param name="ruleExecutionTime">Time spent executing this rule</param>
    public void LogRuleExecution(Guid operationId, string ruleName, string ruleType, int affectedRows, TimeSpan ruleExecutionTime)
    {
        _logger.LogDebug("Validation rule executed [{OperationId}]: Rule={RuleName}, Type={RuleType}, AffectedRows={AffectedRows}, Duration={Duration}ms",
            operationId, ruleName, ruleType, affectedRows, ruleExecutionTime.TotalMilliseconds);
    }

    /// <summary>
    /// Log validation error found
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="validationError">The validation error that was found</param>
    public void LogValidationError(Guid operationId, ValidationError validationError)
    {
        var severityLevel = validationError.Severity switch
        {
            ValidationSeverity.Error => LogLevel.Warning,
            ValidationSeverity.Warning => LogLevel.Information,
            ValidationSeverity.Info => LogLevel.Debug,
            _ => LogLevel.Debug
        };

        _logger.Log(severityLevel, "Validation error found [{OperationId}]: Row={RowIndex}, Column={ColumnName}, Severity={Severity}, Message={Message}, Code={ErrorCode}",
            operationId, validationError.RowIndex, validationError.ColumnName, validationError.Severity, validationError.Message, validationError.ErrorCode);
    }

    /// <summary>
    /// Log batch validation results
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="batchNumber">Current batch number</param>
    /// <param name="batchSize">Size of validated batch</param>
    /// <param name="validRows">Number of valid rows in batch</param>
    /// <param name="errorCount">Number of errors in batch</param>
    /// <param name="batchTime">Time to validate batch</param>
    public void LogBatchValidation(Guid operationId, int batchNumber, int batchSize, int validRows, int errorCount, TimeSpan batchTime)
    {
        _logger.LogTrace("Validation batch completed [{OperationId}]: Batch={BatchNumber}, Size={BatchSize}, Valid={ValidRows}, Errors={ErrorCount}, Duration={Duration}ms",
            operationId, batchNumber, batchSize, validRows, errorCount, batchTime.TotalMilliseconds);
    }

    /// <summary>
    /// Log validation state cache operations
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="operation">Cache operation (hit, miss, write, clear)</param>
    /// <param name="scopeDescription">Description of validation scope</param>
    /// <param name="cacheSize">Size of cache (if applicable)</param>
    public void LogValidationCache(Guid operationId, string operation, string scopeDescription, int? cacheSize = null)
    {
        if (cacheSize.HasValue)
        {
            _logger.LogTrace("Validation cache [{OperationId}]: Operation={Operation}, Scope={ScopeDescription}, CacheSize={CacheSize}",
                operationId, operation, scopeDescription, cacheSize.Value);
        }
        else
        {
            _logger.LogTrace("Validation cache [{OperationId}]: Operation={Operation}, Scope={ScopeDescription}",
                operationId, operation, scopeDescription);
        }
    }

    /// <summary>
    /// Log validation completion
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="success">Whether validation completed successfully</param>
    /// <param name="totalRows">Total number of rows validated</param>
    /// <param name="validRows">Number of valid rows</param>
    /// <param name="totalErrors">Total number of validation errors</param>
    /// <param name="errorsByType">Error counts by validation severity</param>
    /// <param name="totalTime">Total validation time</param>
    /// <param name="errorMessage">Error message if failed</param>
    public void LogValidationCompletion(Guid operationId, bool success, int totalRows, int validRows, int totalErrors,
        Dictionary<ValidationSeverity, int> errorsByType, TimeSpan totalTime, string? errorMessage = null)
    {
        if (success)
        {
            _logger.LogInformation("Validation operation completed successfully [{OperationId}]: TotalRows={TotalRows}, ValidRows={ValidRows}, TotalErrors={TotalErrors}, Duration={Duration}ms, ErrorBreakdown={ErrorBreakdown}",
                operationId, totalRows, validRows, totalErrors, totalTime.TotalMilliseconds, FormatErrorBreakdown(errorsByType));
        }
        else
        {
            _logger.LogError("Validation operation failed [{OperationId}]: TotalRows={TotalRows}, ValidRows={ValidRows}, TotalErrors={TotalErrors}, Duration={Duration}ms, Error={ErrorMessage}",
                operationId, totalRows, validRows, totalErrors, totalTime.TotalMilliseconds, errorMessage ?? "Unknown error");
        }
    }

    /// <summary>
    /// Log validation performance metrics
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="rowsPerSecond">Validation rate in rows per second</param>
    /// <param name="rulesPerSecond">Rule execution rate</param>
    /// <param name="memoryUsed">Memory used during validation (bytes)</param>
    /// <param name="peakMemory">Peak memory usage (bytes)</param>
    /// <param name="cacheHitRate">Cache hit rate percentage</param>
    public void LogPerformanceMetrics(Guid operationId, double rowsPerSecond, double rulesPerSecond, long memoryUsed, long peakMemory, double cacheHitRate)
    {
        _logger.LogInformation("Validation performance [{OperationId}]: Rate={RowsPerSecond:F2} rows/sec, RuleRate={RulesPerSecond:F2} rules/sec, CacheHitRate={CacheHitRate:F1}%, Memory={MemoryUsed:N0} bytes, Peak={PeakMemory:N0} bytes",
            operationId, rowsPerSecond, rulesPerSecond, cacheHitRate, memoryUsed, peakMemory);
    }

    /// <summary>
    /// Log validation rule statistics
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="ruleStatistics">Statistics for each validation rule</param>
    public void LogRuleStatistics(Guid operationId, Dictionary<string, RuleStatistics> ruleStatistics)
    {
        foreach (var kvp in ruleStatistics)
        {
            _logger.LogDebug("Validation rule statistics [{OperationId}]: Rule={RuleName}, Executions={Executions}, AverageTime={AverageTime:F2}ms, ErrorsFound={ErrorsFound}",
                operationId, kvp.Key, kvp.Value.ExecutionCount, kvp.Value.AverageExecutionTime, kvp.Value.ErrorsFound);
        }
    }

    /// <summary>
    /// Log critical validation error
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="context">Additional context information</param>
    public void LogCriticalError(Guid operationId, Exception exception, string context)
    {
        _logger.LogCritical(exception, "Critical validation error [{OperationId}]: Context={Context}",
            operationId, context);
    }

    /// <summary>
    /// Log validation state refresh to UI
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="errorCount">Number of errors being displayed</param>
    /// <param name="refreshMode">Mode of refresh (immediate, batched, etc.)</param>
    public void LogUIRefresh(Guid operationId, int errorCount, string refreshMode)
    {
        _logger.LogDebug("Validation UI refresh [{OperationId}]: ErrorCount={ErrorCount}, RefreshMode={RefreshMode}",
            operationId, errorCount, refreshMode);
    }

    private string FormatErrorBreakdown(Dictionary<ValidationSeverity, int> errorsByType)
    {
        if (errorsByType.Count == 0)
            return "None";

        var breakdown = errorsByType.Select(kvp => $"{kvp.Key}:{kvp.Value}");
        return string.Join(", ", breakdown);
    }
}

/// <summary>
/// Statistics for validation rule execution
/// </summary>
internal sealed class RuleStatistics
{
    public int ExecutionCount { get; set; }
    public double AverageExecutionTime { get; set; }
    public int ErrorsFound { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
}
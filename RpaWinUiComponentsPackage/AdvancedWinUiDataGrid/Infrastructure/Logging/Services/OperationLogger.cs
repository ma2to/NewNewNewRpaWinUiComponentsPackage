using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Services;

/// <summary>
/// Universal operation logger implementation with comprehensive tracking
/// Provides detailed logging of all operations in the system
/// </summary>
internal sealed class OperationLogger<T> : IOperationLogger<T>
{
    private readonly ILogger<T> _logger;

    public OperationLogger(ILogger<T> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IOperationScope LogOperationStart(string operationName, object? context = null)
    {
        // Create new operation scope for tracking
        return new OperationScope(_logger, operationName, context);
    }

    public Task LogOperationStartAsync(string operationName, object? context = null)
    {
        // Asynchronous version - just log start
        _logger.LogInformation(
            "Async operation '{OperationName}' starting. Context: {@Context}",
            operationName, context);

        return Task.CompletedTask;
    }

    public void LogOperationSuccess(string operationName, object? result = null, TimeSpan? duration = null)
    {
        // Log successful operation completion
        if (duration.HasValue)
        {
            _logger.LogInformation(
                "Operation '{OperationName}' completed successfully in {Duration}ms. Result: {@Result}",
                operationName, duration.Value.TotalMilliseconds, result);
        }
        else
        {
            _logger.LogInformation(
                "Operation '{OperationName}' completed successfully. Result: {@Result}",
                operationName, result);
        }
    }

    public void LogOperationFailure(string operationName, Exception exception, object? context = null)
    {
        // Log operation failure with exception
        _logger.LogError(exception,
            "Operation '{OperationName}' failed. Context: {@Context}",
            operationName, context);
    }

    public void LogOperationWarning(string operationName, string warning, object? context = null)
    {
        // Log warning for operation
        _logger.LogWarning(
            "Operation '{OperationName}' warning: {Warning}. Context: {@Context}",
            operationName, warning, context);
    }

    public IOperationScope LogCommandOperationStart<TCommand>(TCommand command, object? parameters = null)
    {
        // Start command operation with command type
        var commandType = typeof(TCommand).Name;
        return new OperationScope(_logger, $"Command_{commandType}", new { Command = command, Parameters = parameters });
    }

    public void LogCommandSuccess<TCommand>(string commandType, TCommand command, TimeSpan duration)
    {
        // Log successful command execution
        _logger.LogInformation(
            "Command '{CommandType}' executed successfully in {Duration}ms. Command: {@Command}",
            commandType, duration.TotalMilliseconds, command);
    }

    public void LogCommandFailure<TCommand>(string commandType, TCommand command, Exception exception, TimeSpan duration)
    {
        // Log command failure
        _logger.LogError(exception,
            "Command '{CommandType}' failed after {Duration}ms. Command: {@Command}",
            commandType, duration.TotalMilliseconds, command);
    }

    public void LogFilterOperation(string filterType, string filterName, int totalRows, int matchingRows, TimeSpan duration)
    {
        // Log filter operation with metrics
        _logger.LogInformation(
            "Filter operation '{FilterType}' ({FilterName}) completed: {MatchingRows}/{TotalRows} rows matched in {Duration}ms",
            filterType, filterName, matchingRows, totalRows, duration.TotalMilliseconds);
    }

    public void LogAdvancedFilterOperation(string businessRule, int totalFilters, int totalRows, int matchingRows, TimeSpan duration)
    {
        // Log advanced filter operation
        _logger.LogInformation(
            "Advanced filter with business rule '{BusinessRule}' completed: {TotalFilters} filters applied, {MatchingRows}/{TotalRows} rows matched in {Duration}ms",
            businessRule, totalFilters, matchingRows, totalRows, duration.TotalMilliseconds);
    }

    public void LogImportOperation(string importType, int totalRows, int importedRows, TimeSpan duration)
    {
        // Log import operation
        _logger.LogInformation(
            "Import operation '{ImportType}' completed: {ImportedRows}/{TotalRows} rows imported in {Duration}ms",
            importType, importedRows, totalRows, duration.TotalMilliseconds);
    }

    public void LogExportOperation(string exportType, int totalRows, int exportedRows, TimeSpan duration)
    {
        // Log export operation
        _logger.LogInformation(
            "Export operation '{ExportType}' completed: {ExportedRows}/{TotalRows} rows exported in {Duration}ms",
            exportType, exportedRows, totalRows, duration.TotalMilliseconds);
    }

    public void LogValidationOperation(string validationType, int totalRows, int validRows, int ruleCount, TimeSpan duration)
    {
        // Log validation with metrics
        _logger.LogInformation(
            "Validation '{ValidationType}' completed: {ValidRows}/{TotalRows} rows valid, {RuleCount} rules applied in {Duration}ms",
            validationType, validRows, totalRows, ruleCount, duration.TotalMilliseconds);
    }

    public void LogPerformanceMetrics(string operationType, object metrics)
    {
        // Log performance metrics
        _logger.LogInformation(
            "Performance metrics for '{OperationType}': {@Metrics}",
            operationType, metrics);
    }

    public void LogLINQOptimization(string operationType, bool usedParallel, bool usedShortCircuit, TimeSpan duration)
    {
        // Log LINQ optimization
        _logger.LogInformation(
            "LINQ optimization for '{OperationType}': Parallel={UsedParallel}, ShortCircuit={UsedShortCircuit}, Duration={Duration}ms",
            operationType, usedParallel, usedShortCircuit, duration.TotalMilliseconds);
    }
}

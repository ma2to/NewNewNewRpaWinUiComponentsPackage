using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Services;

/// <summary>
/// Universal operation logger implementation with comprehensive tracking
/// Poskytuje detailné logovanie všetkých operácií v systéme
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
        // Vytvoríme nový operation scope pre tracking
        return new OperationScope(_logger, operationName, context);
    }

    public Task LogOperationStartAsync(string operationName, object? context = null)
    {
        // Asynchrónna verzia - len zalogujeme start
        _logger.LogInformation(
            "Async operation '{OperationName}' starting. Context: {@Context}",
            operationName, context);

        return Task.CompletedTask;
    }

    public void LogOperationSuccess(string operationName, object? result = null, TimeSpan? duration = null)
    {
        // Zalogujeme úspešné dokončenie operácie
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
        // Zalogujeme zlyhanie operácie s exception
        _logger.LogError(exception,
            "Operation '{OperationName}' failed. Context: {@Context}",
            operationName, context);
    }

    public void LogOperationWarning(string operationName, string warning, object? context = null)
    {
        // Zalogujeme warning pre operáciu
        _logger.LogWarning(
            "Operation '{OperationName}' warning: {Warning}. Context: {@Context}",
            operationName, warning, context);
    }

    public IOperationScope LogCommandOperationStart<TCommand>(TCommand command, object? parameters = null)
    {
        // Začneme command operáciu s typom commandu
        var commandType = typeof(TCommand).Name;
        return new OperationScope(_logger, $"Command_{commandType}", new { Command = command, Parameters = parameters });
    }

    public void LogCommandSuccess<TCommand>(string commandType, TCommand command, TimeSpan duration)
    {
        // Zalogujeme úspešné vykonanie commandu
        _logger.LogInformation(
            "Command '{CommandType}' executed successfully in {Duration}ms. Command: {@Command}",
            commandType, duration.TotalMilliseconds, command);
    }

    public void LogCommandFailure<TCommand>(string commandType, TCommand command, Exception exception, TimeSpan duration)
    {
        // Zalogujeme zlyhanie commandu
        _logger.LogError(exception,
            "Command '{CommandType}' failed after {Duration}ms. Command: {@Command}",
            commandType, duration.TotalMilliseconds, command);
    }

    public void LogFilterOperation(string filterType, string filterName, int totalRows, int matchingRows, TimeSpan duration)
    {
        // Zalogujeme filter operáciu s metrikami
        _logger.LogInformation(
            "Filter operation '{FilterType}' ({FilterName}) completed: {MatchingRows}/{TotalRows} rows matched in {Duration}ms",
            filterType, filterName, matchingRows, totalRows, duration.TotalMilliseconds);
    }

    public void LogAdvancedFilterOperation(string businessRule, int totalFilters, int totalRows, int matchingRows, TimeSpan duration)
    {
        // Zalogujeme pokročilú filter operáciu
        _logger.LogInformation(
            "Advanced filter with business rule '{BusinessRule}' completed: {TotalFilters} filters applied, {MatchingRows}/{TotalRows} rows matched in {Duration}ms",
            businessRule, totalFilters, matchingRows, totalRows, duration.TotalMilliseconds);
    }

    public void LogImportOperation(string importType, int totalRows, int importedRows, TimeSpan duration)
    {
        // Zalogujeme import operáciu
        _logger.LogInformation(
            "Import operation '{ImportType}' completed: {ImportedRows}/{TotalRows} rows imported in {Duration}ms",
            importType, importedRows, totalRows, duration.TotalMilliseconds);
    }

    public void LogExportOperation(string exportType, int totalRows, int exportedRows, TimeSpan duration)
    {
        // Zalogujeme export operáciu
        _logger.LogInformation(
            "Export operation '{ExportType}' completed: {ExportedRows}/{TotalRows} rows exported in {Duration}ms",
            exportType, exportedRows, totalRows, duration.TotalMilliseconds);
    }

    public void LogValidationOperation(string validationType, int totalRows, int validRows, int ruleCount, TimeSpan duration)
    {
        // Zalogujeme validáciu s metrikami
        _logger.LogInformation(
            "Validation '{ValidationType}' completed: {ValidRows}/{TotalRows} rows valid, {RuleCount} rules applied in {Duration}ms",
            validationType, validRows, totalRows, ruleCount, duration.TotalMilliseconds);
    }

    public void LogPerformanceMetrics(string operationType, object metrics)
    {
        // Zalogujeme performance metriky
        _logger.LogInformation(
            "Performance metrics for '{OperationType}': {@Metrics}",
            operationType, metrics);
    }

    public void LogLINQOptimization(string operationType, bool usedParallel, bool usedShortCircuit, TimeSpan duration)
    {
        // Zalogujeme LINQ optimalizáciu
        _logger.LogInformation(
            "LINQ optimization for '{OperationType}': Parallel={UsedParallel}, ShortCircuit={UsedShortCircuit}, Duration={Duration}ms",
            operationType, usedParallel, usedShortCircuit, duration.TotalMilliseconds);
    }
}

using System;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

/// <summary>
/// Universal operation logger for comprehensive operation tracking
/// Used for logging all operations in the system with automatic time measurement
/// </summary>
/// <typeparam name="T">Service type for logger context</typeparam>
internal interface IOperationLogger<T>
{
    /// <summary>
    /// Starts operation logging and returns scope for automatic tracking
    /// </summary>
    IOperationScope LogOperationStart(string operationName, object? context = null);

    /// <summary>
    /// Asynchronously starts operation logging
    /// </summary>
    Task LogOperationStartAsync(string operationName, object? context = null);

    /// <summary>
    /// Logs successful operation completion
    /// </summary>
    void LogOperationSuccess(string operationName, object? result = null, TimeSpan? duration = null);

    /// <summary>
    /// Logs operation failure with exception
    /// </summary>
    void LogOperationFailure(string operationName, Exception exception, object? context = null);

    /// <summary>
    /// Logs warning for operation
    /// </summary>
    void LogOperationWarning(string operationName, string warning, object? context = null);

    /// <summary>
    /// Starts logging command operation
    /// </summary>
    IOperationScope LogCommandOperationStart<TCommand>(TCommand command, object? parameters = null);

    /// <summary>
    /// Logs successful command execution
    /// </summary>
    void LogCommandSuccess<TCommand>(string commandType, TCommand command, TimeSpan duration);

    /// <summary>
    /// Logs command failure
    /// </summary>
    void LogCommandFailure<TCommand>(string commandType, TCommand command, Exception exception, TimeSpan duration);

    /// <summary>
    /// Logs filter operation with metrics
    /// </summary>
    void LogFilterOperation(string filterType, string filterName, int totalRows, int matchingRows, TimeSpan duration);

    /// <summary>
    /// Logs advanced filter operation with business rule
    /// </summary>
    void LogAdvancedFilterOperation(string businessRule, int totalFilters, int totalRows, int matchingRows, TimeSpan duration);

    /// <summary>
    /// Logs import operation
    /// </summary>
    void LogImportOperation(string importType, int totalRows, int importedRows, TimeSpan duration);

    /// <summary>
    /// Logs export operation
    /// </summary>
    void LogExportOperation(string exportType, int totalRows, int exportedRows, TimeSpan duration);

    /// <summary>
    /// Logs validation with metrics
    /// </summary>
    void LogValidationOperation(string validationType, int totalRows, int validRows, int ruleCount, TimeSpan duration);

    /// <summary>
    /// Logs performance metrics
    /// </summary>
    void LogPerformanceMetrics(string operationType, object metrics);

    /// <summary>
    /// Logs LINQ optimization
    /// </summary>
    void LogLINQOptimization(string operationType, bool usedParallel, bool usedShortCircuit, TimeSpan duration);
}

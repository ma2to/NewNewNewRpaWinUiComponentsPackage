using System;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

/// <summary>
/// Universal operation logger for comprehensive operation tracking
/// Používa sa pre logovanie všetkých operácií v systéme s automatickým meraním času
/// </summary>
/// <typeparam name="T">Service type for logger context</typeparam>
internal interface IOperationLogger<T>
{
    /// <summary>
    /// Spustí logovanie operácie a vráti scope pre automatické tracking
    /// </summary>
    IOperationScope LogOperationStart(string operationName, object? context = null);

    /// <summary>
    /// Asynchrónne spustí logovanie operácie
    /// </summary>
    Task LogOperationStartAsync(string operationName, object? context = null);

    /// <summary>
    /// Zaloguje úspešné dokončenie operácie
    /// </summary>
    void LogOperationSuccess(string operationName, object? result = null, TimeSpan? duration = null);

    /// <summary>
    /// Zaloguje zlyhanie operácie s exception
    /// </summary>
    void LogOperationFailure(string operationName, Exception exception, object? context = null);

    /// <summary>
    /// Zaloguje warning pre operáciu
    /// </summary>
    void LogOperationWarning(string operationName, string warning, object? context = null);

    /// <summary>
    /// Spustí logovanie command operácie
    /// </summary>
    IOperationScope LogCommandOperationStart<TCommand>(TCommand command, object? parameters = null);

    /// <summary>
    /// Zaloguje úspešné vykonanie command
    /// </summary>
    void LogCommandSuccess<TCommand>(string commandType, TCommand command, TimeSpan duration);

    /// <summary>
    /// Zaloguje zlyhanie command
    /// </summary>
    void LogCommandFailure<TCommand>(string commandType, TCommand command, Exception exception, TimeSpan duration);

    /// <summary>
    /// Zaloguje filter operáciu s metrikami
    /// </summary>
    void LogFilterOperation(string filterType, string filterName, int totalRows, int matchingRows, TimeSpan duration);

    /// <summary>
    /// Zaloguje pokročilú filter operáciu s business rule
    /// </summary>
    void LogAdvancedFilterOperation(string businessRule, int totalFilters, int totalRows, int matchingRows, TimeSpan duration);

    /// <summary>
    /// Zaloguje import operáciu
    /// </summary>
    void LogImportOperation(string importType, int totalRows, int importedRows, TimeSpan duration);

    /// <summary>
    /// Zaloguje export operáciu
    /// </summary>
    void LogExportOperation(string exportType, int totalRows, int exportedRows, TimeSpan duration);

    /// <summary>
    /// Zaloguje validáciu s metrikami
    /// </summary>
    void LogValidationOperation(string validationType, int totalRows, int validRows, int ruleCount, TimeSpan duration);

    /// <summary>
    /// Zaloguje performance metriky
    /// </summary>
    void LogPerformanceMetrics(string operationType, object metrics);

    /// <summary>
    /// Zaloguje LINQ optimalizáciu
    /// </summary>
    void LogLINQOptimization(string operationType, bool usedParallel, bool usedShortCircuit, TimeSpan duration);
}

using System;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

/// <summary>
/// Null implementation of IOperationLogger - používa sa keď logging nie je dostupný
/// Poskytuje zero-overhead implementation s full interface compliance
/// </summary>
internal sealed class NullOperationLogger<T> : IOperationLogger<T>
{
    /// <summary>
    /// Singleton instance for minimálne alokácie
    /// </summary>
    public static readonly NullOperationLogger<T> Instance = new();

    // Súkromný konštruktor for singleton pattern
    private NullOperationLogger() { }

    // Všetky metódy vracajú null scope or nerobia nič
    public IOperationScope LogOperationStart(string operationName, object? context = null)
        => NullOperationScope.Instance;

    public Task LogOperationStartAsync(string operationName, object? context = null)
        => Task.CompletedTask;

    public void LogOperationSuccess(string operationName, object? result = null, TimeSpan? duration = null) { }

    public void LogOperationFailure(string operationName, Exception exception, object? context = null) { }

    public void LogOperationWarning(string operationName, string warning, object? context = null) { }

    public IOperationScope LogCommandOperationStart<TCommand>(TCommand command, object? parameters = null)
        => NullOperationScope.Instance;

    public void LogCommandSuccess<TCommand>(string commandType, TCommand command, TimeSpan duration) { }

    public void LogCommandFailure<TCommand>(string commandType, TCommand command, Exception exception, TimeSpan duration) { }

    public void LogFilterOperation(string filterType, string filterName, int totalRows, int matchingRows, TimeSpan duration) { }

    public void LogAdvancedFilterOperation(string businessRule, int totalFilters, int totalRows, int matchingRows, TimeSpan duration) { }

    public void LogImportOperation(string importType, int totalRows, int importedRows, TimeSpan duration) { }

    public void LogExportOperation(string exportType, int totalRows, int exportedRows, TimeSpan duration) { }

    public void LogValidationOperation(string validationType, int totalRows, int validRows, int ruleCount, TimeSpan duration) { }

    public void LogPerformanceMetrics(string operationType, object metrics) { }

    public void LogLINQOptimization(string operationType, bool usedParallel, bool usedShortCircuit, TimeSpan duration) { }
}

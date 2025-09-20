using System;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;

/// <summary>
/// CORE: Interface for comprehensive data grid logging
/// ENTERPRISE: Supports both structured and performance logging
/// </summary>
internal interface IDataGridLogger
{
    /// <summary>Log informational message with context</summary>
    void LogInformation(string message, params object[] args);

    /// <summary>Log warning message with context</summary>
    void LogWarning(string message, params object[] args);

    /// <summary>Log warning with exception</summary>
    void LogWarning(Exception exception, string message, params object[] args);

    /// <summary>Log error message with exception context</summary>
    void LogError(string message, params object[] args);

    /// <summary>Log error with exception</summary>
    void LogError(Exception exception, string message, params object[] args);

    /// <summary>Execute operation with automatic logging and Result wrapping</summary>
    Result<T> ExecuteWithLogging<T>(Func<T> operation, string? operationName = null);

    /// <summary>Execute operation with automatic logging</summary>
    Result ExecuteWithLogging(Action operation, string? operationName = null);

    /// <summary>Create scoped logger with additional context</summary>
    IDataGridLogger CreateScope(string scopeName);

    /// <summary>Log operation timing and performance metrics</summary>
    void LogPerformance(string operation, TimeSpan duration, object? context = null);
}
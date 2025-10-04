using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Exceptions;

/// <summary>
/// ENTERPRISE: Centralized exception handling with logging
/// THREAD SAFE: Thread-safe exception processing
/// </summary>
internal static class ExceptionHandler
{
    /// <summary>
    /// Handle exception with logging and optional fallback value
    /// </summary>
    public static T? HandleException<T>(
        Exception exception,
        ILogger logger,
        string context,
        T? fallbackValue = default)
    {
        LogException(exception, logger, context);
        return fallbackValue;
    }

    /// <summary>
    /// Handle exception asynchronously
    /// </summary>
    public static async Task<T?> HandleExceptionAsync<T>(
        Exception exception,
        ILogger logger,
        string context,
        T? fallbackValue = default)
    {
        await Task.Run(() => LogException(exception, logger, context));
        return fallbackValue;
    }

    /// <summary>
    /// Handle exception with custom recovery action
    /// </summary>
    public static async Task<T?> HandleWithRecoveryAsync<T>(
        Exception exception,
        ILogger logger,
        string context,
        Func<Exception, Task<T?>> recoveryAction)
    {
        LogException(exception, logger, context);

        try
        {
            return await recoveryAction(exception);
        }
        catch (Exception recoveryEx)
        {
            logger.LogError(recoveryEx, "Recovery action failed for context: {Context}", context);
            return default;
        }
    }

    /// <summary>
    /// Log exception with appropriate severity
    /// </summary>
    private static void LogException(Exception exception, ILogger logger, string context)
    {
        var logLevel = DetermineLogLevel(exception);

        switch (logLevel)
        {
            case LogLevel.Critical:
                logger.LogCritical(exception, "CRITICAL exception in context: {Context}", context);
                break;
            case LogLevel.Error:
                logger.LogError(exception, "Error in context: {Context}", context);
                break;
            case LogLevel.Warning:
                logger.LogWarning(exception, "Warning in context: {Context}", context);
                break;
            default:
                logger.LogInformation(exception, "Exception in context: {Context}", context);
                break;
        }
    }

    /// <summary>
    /// Determine appropriate log level based on exception type
    /// </summary>
    private static LogLevel DetermineLogLevel(Exception exception)
    {
        return exception switch
        {
            OutOfMemoryException => LogLevel.Critical,
            StackOverflowException => LogLevel.Critical,
            GridConfigurationException => LogLevel.Error,
            GridOperationException => LogLevel.Error,
            GridDataException => LogLevel.Warning,
            GridValidationException => LogLevel.Warning,
            _ => LogLevel.Error
        };
    }

    /// <summary>
    /// Create detailed exception message
    /// </summary>
    public static string CreateDetailedMessage(Exception exception, string context)
    {
        return $"Context: {context} | Type: {exception.GetType().Name} | Message: {exception.Message}";
    }
}

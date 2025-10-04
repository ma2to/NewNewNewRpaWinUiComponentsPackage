namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Exceptions;

/// <summary>
/// ENTERPRISE: Extension methods for exception handling
/// </summary>
internal static class ExceptionExtensions
{
    /// <summary>
    /// Get innermost exception
    /// </summary>
    public static Exception GetInnermostException(this Exception exception)
    {
        while (exception.InnerException != null)
        {
            exception = exception.InnerException;
        }
        return exception;
    }

    /// <summary>
    /// Get all exception messages in the chain
    /// </summary>
    public static IEnumerable<string> GetAllMessages(this Exception exception)
    {
        var messages = new List<string>();
        var current = exception;

        while (current != null)
        {
            messages.Add(current.Message);
            current = current.InnerException;
        }

        return messages;
    }

    /// <summary>
    /// Get detailed exception information
    /// </summary>
    public static string GetDetailedInfo(this Exception exception)
    {
        return $@"Exception Type: {exception.GetType().FullName}
Message: {exception.Message}
Stack Trace: {exception.StackTrace}
Inner Exception: {exception.InnerException?.Message ?? "None"}";
    }

    /// <summary>
    /// Check if exception is retriable
    /// </summary>
    public static bool IsRetriable(this Exception exception)
    {
        return exception switch
        {
            GridValidationException => false,
            GridConfigurationException => false,
            GridDataException => false,
            OperationCanceledException => false,
            TimeoutException => true,
            _ => true
        };
    }

    /// <summary>
    /// Get safe error message (without sensitive data)
    /// </summary>
    public static string GetSafeMessage(this Exception exception)
    {
        return exception switch
        {
            GridValidationException gve => $"Validation failed: {gve.ValidationErrors.Count} error(s)",
            GridOperationException goe => $"Operation '{goe.OperationName}' failed",
            GridConfigurationException gce => $"Configuration error: {gce.ConfigurationKey}",
            GridDataException gde => $"Data error at row {gde.RowIndex}, column {gde.ColumnName}",
            _ => "An error occurred"
        };
    }
}

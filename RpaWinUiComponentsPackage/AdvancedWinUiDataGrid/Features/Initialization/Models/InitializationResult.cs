namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

/// <summary>
/// Result of component initialization operation
/// Immutable result object with success/failure state
/// </summary>
internal sealed record InitializationResult
{
    /// <summary>Indicates whether initialization completed successfully</summary>
    internal bool IsSuccess { get; init; }

    /// <summary>Descriptive message about the initialization outcome</summary>
    internal string Message { get; init; } = string.Empty;

    /// <summary>Error message if initialization failed, null if successful</summary>
    internal string? ErrorMessage { get; init; }

    /// <summary>Total time taken for the initialization process</summary>
    internal TimeSpan? Duration { get; init; }

    /// <summary>Exception that caused the initialization failure, if any</summary>
    internal Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful initialization result
    /// </summary>
    internal static InitializationResult Success(string message, TimeSpan? duration = null) =>
        new()
        {
            IsSuccess = true,
            Message = message,
            Duration = duration
        };

    /// <summary>
    /// Creates a failed initialization result
    /// </summary>
    internal static InitializationResult Failure(string errorMessage, Exception? exception = null) =>
        new()
        {
            IsSuccess = false,
            Message = "Initialization failed",
            ErrorMessage = errorMessage,
            Exception = exception
        };

    /// <summary>
    /// Creates a result for when component is already initialized
    /// </summary>
    internal static InitializationResult AlreadyInitialized() =>
        new()
        {
            IsSuccess = true,
            Message = "Component is already initialized"
        };
}

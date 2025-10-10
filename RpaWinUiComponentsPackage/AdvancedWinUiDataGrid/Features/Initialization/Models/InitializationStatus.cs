namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

/// <summary>
/// Represents the current initialization status of the component
/// Thread-safe immutable record for runtime inspection
/// </summary>
internal sealed record InitializationStatus
{
    /// <summary>Indicates whether the component is fully initialized</summary>
    internal bool IsInitialized { get; init; }

    /// <summary>Indicates if the component is running in headless mode (no UI dependencies)</summary>
    internal bool IsHeadlessMode { get; init; }

    /// <summary>Current initialization phase being executed</summary>
    internal InitializationPhase CurrentPhase { get; init; }

    /// <summary>Timestamp when initialization was started</summary>
    internal DateTime? InitializationStartTime { get; init; }

    /// <summary>Timestamp when initialization completed successfully</summary>
    internal DateTime? InitializationCompletedTime { get; init; }

    /// <summary>Total duration of the initialization process</summary>
    internal TimeSpan? InitializationDuration { get; init; }

    /// <summary>Last error message encountered during initialization, if any</summary>
    internal string? LastError { get; init; }
}

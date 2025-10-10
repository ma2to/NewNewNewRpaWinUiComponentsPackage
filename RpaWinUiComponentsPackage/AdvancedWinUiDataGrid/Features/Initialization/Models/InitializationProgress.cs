namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

/// <summary>
/// Represents initialization progress information for the component
/// Thread-safe immutable record for progress reporting
/// </summary>
internal sealed record InitializationProgress
{
    /// <summary>Number of initialization steps that have been completed</summary>
    internal int CompletedSteps { get; init; }

    /// <summary>Total number of initialization steps to complete</summary>
    internal int TotalSteps { get; init; }

    /// <summary>Completion percentage calculated as ratio of completed to total steps (0-100)</summary>
    internal double CompletionPercentage => TotalSteps > 0
        ? (double)CompletedSteps / TotalSteps * 100
        : 0;

    /// <summary>Time elapsed since initialization started</summary>
    internal TimeSpan ElapsedTime { get; init; }

    /// <summary>Name of the operation currently being executed</summary>
    internal string CurrentOperation { get; init; } = string.Empty;

    /// <summary>Current initialization phase in the startup sequence</summary>
    internal InitializationPhase CurrentPhase { get; init; } = InitializationPhase.None;

    /// <summary>Indicates if initialization is running in headless mode without UI</summary>
    internal bool IsHeadlessMode { get; init; }

    /// <summary>Estimated time remaining based on current progress rate, null if cannot be estimated</summary>
    internal TimeSpan? EstimatedTimeRemaining => CompletedSteps > 0 && TotalSteps > CompletedSteps
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalSteps - CompletedSteps) / CompletedSteps)
        : null;
}

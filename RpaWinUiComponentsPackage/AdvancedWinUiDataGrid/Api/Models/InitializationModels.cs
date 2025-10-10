namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// PUBLIC API: Component initialization result
/// </summary>
public sealed record PublicInitializationResult
{
    /// <summary>Indicator of successful initialization</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Initialization result message</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Error message (if initialization failed)</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Initialization duration</summary>
    public TimeSpan? Duration { get; init; }
}

/// <summary>
/// PUBLIC API: Component initialization progress
/// </summary>
public sealed record PublicInitializationProgress
{
    /// <summary>Number of completed steps</summary>
    public int CompletedSteps { get; init; }

    /// <summary>Total number of steps</summary>
    public int TotalSteps { get; init; }

    /// <summary>Completion percentage (0-100)</summary>
    public double CompletionPercentage { get; init; }

    /// <summary>Elapsed time since initialization start</summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>Current operation name</summary>
    public string CurrentOperation { get; init; } = string.Empty;

    /// <summary>Headless mode indicator</summary>
    public bool IsHeadlessMode { get; init; }

    /// <summary>Estimated remaining time</summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }
}

/// <summary>
/// PUBLIC API: Current component initialization status
/// </summary>
public sealed record PublicInitializationStatus
{
    /// <summary>Indicator whether the component is initialized</summary>
    public bool IsInitialized { get; init; }

    /// <summary>Headless mode indicator</summary>
    public bool IsHeadlessMode { get; init; }

    /// <summary>Time when initialization was started</summary>
    public DateTime? InitializationStartTime { get; init; }

    /// <summary>Time when initialization was completed</summary>
    public DateTime? InitializationCompletedTime { get; init; }

    /// <summary>Initialization duration</summary>
    public TimeSpan? InitializationDuration { get; init; }

    /// <summary>Last error message (if exists)</summary>
    public string? LastError { get; init; }
}

/// <summary>
/// PUBLIC API: Minimal configuration for initialization (for public API)
/// </summary>
public sealed record PublicInitializationConfiguration
{
    /// <summary>Enable smart operations</summary>
    public bool EnableSmartOperations { get; init; } = true;

    /// <summary>Enable advanced validation</summary>
    public bool EnableAdvancedValidation { get; init; } = true;

    /// <summary>Enable performance optimizations</summary>
    public bool EnablePerformanceOptimizations { get; init; } = true;

    /// <summary>Initialization timeout</summary>
    public TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Default configuration
    /// </summary>
    public static PublicInitializationConfiguration Default => new();

    /// <summary>
    /// Configuration optimized for high performance
    /// </summary>
    public static PublicInitializationConfiguration HighPerformance => new()
    {
        EnablePerformanceOptimizations = true
    };

    /// <summary>
    /// Configuration optimized for server mode (headless)
    /// </summary>
    public static PublicInitializationConfiguration ServerMode => new()
    {
        EnableSmartOperations = false,
        EnablePerformanceOptimizations = true
    };
}

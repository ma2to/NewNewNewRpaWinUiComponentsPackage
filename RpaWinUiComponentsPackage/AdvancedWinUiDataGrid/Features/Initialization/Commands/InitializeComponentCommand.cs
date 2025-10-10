using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;

/// <summary>
/// Command for initializing the Advanced Data Grid component
/// Immutable record with factory methods for different scenarios
/// Supports both UI and Headless modes
/// </summary>
internal sealed record InitializeComponentCommand
{
    /// <summary>Configuration settings for the initialization process</summary>
    internal InitializationConfiguration Configuration { get; init; } = new();

    /// <summary>Indicates if initialization should run in headless mode without UI dependencies</summary>
    internal bool IsHeadlessMode { get; init; } = false;

    /// <summary>Whether to validate configuration before starting initialization</summary>
    internal bool ValidateConfiguration { get; init; } = true;

    /// <summary>Maximum time allowed for initialization to complete</summary>
    internal TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Progress reporter for tracking initialization progress</summary>
    internal IProgress<InitializationProgress>? ProgressReporter { get; init; }

    /// <summary>Cancellation token for aborting the initialization operation</summary>
    internal CancellationToken CancellationToken { get; init; } = default;

    /// <summary>
    /// Creates initialization command for UI mode
    /// Initializes component with full UI dependencies
    /// </summary>
    internal static InitializeComponentCommand ForUI(
        InitializationConfiguration? config = null,
        IProgress<InitializationProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        new()
        {
            Configuration = config ?? new(),
            IsHeadlessMode = false,
            ProgressReporter = progress,
            CancellationToken = cancellationToken
        };

    /// <summary>
    /// Creates initialization command for Headless mode
    /// Optimized for server/background scenarios without UI
    /// </summary>
    internal static InitializeComponentCommand ForHeadless(
        InitializationConfiguration? config = null,
        IProgress<InitializationProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        new()
        {
            Configuration = config ?? new(),
            IsHeadlessMode = true,
            ProgressReporter = progress,
            CancellationToken = cancellationToken
        };
}

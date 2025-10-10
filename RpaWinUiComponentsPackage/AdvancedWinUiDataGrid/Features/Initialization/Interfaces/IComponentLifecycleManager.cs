using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;

/// <summary>
/// Interface for component lifecycle management
/// Orchestrates startup and shutdown sequences
/// Thread-safe with progress reporting support
/// </summary>
internal interface IComponentLifecycleManager
{
    /// <summary>
    /// Initializes components according to the specified command
    /// Launches complete startup sequence with progress tracking
    /// </summary>
    Task<InitializationResult> InitializeAsync(
        InitializeComponentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Graceful shutdown with cleanup of all resources
    /// Flushing pending operations and unregister global handlers
    /// </summary>
    Task<bool> ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current initialization status
    /// </summary>
    InitializationStatus GetStatus();

    /// <summary>
    /// Indicator whether the component is initialized
    /// </summary>
    bool IsInitialized { get; }
}

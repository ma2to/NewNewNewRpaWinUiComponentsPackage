using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;

/// <summary>
/// Interface for initialization pattern (UI vs Headless mode).
/// Implements strategy pattern for different initialization modes.
/// </summary>
internal interface IInitializationPattern
{
    /// <summary>
    /// Initializes mode-specific services based on the operation mode.
    /// </summary>
    Task<InitializationResult> InitializeServicesAsync(
        InitializeComponentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies mode-specific performance optimizations.
    /// </summary>
    Task<InitializationResult> ApplyOptimizationsAsync(
        InitializeComponentCommand command,
        CancellationToken cancellationToken = default);
}

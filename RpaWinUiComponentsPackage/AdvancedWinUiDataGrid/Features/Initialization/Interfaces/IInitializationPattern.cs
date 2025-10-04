using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;

/// <summary>
/// Interface pre initialization pattern (UI vs Headless)
/// Strategy pattern pre rôzne režimy inicializácie
/// </summary>
internal interface IInitializationPattern
{
    /// <summary>
    /// Inicializuje služby špecifické pre daný mode
    /// </summary>
    Task<InitializationResult> InitializeServicesAsync(
        InitializeComponentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aplikuje optimalizácie špecifické pre daný mode
    /// </summary>
    Task<InitializationResult> ApplyOptimizationsAsync(
        InitializeComponentCommand command,
        CancellationToken cancellationToken = default);
}

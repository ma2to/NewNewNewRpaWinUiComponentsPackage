using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;

/// <summary>
/// Interface pre správu životného cyklu komponentu
/// Orchestruje startup aj shutdown sekvencie
/// Thread-safe s podporou progress reporting
/// </summary>
internal interface IComponentLifecycleManager
{
    /// <summary>
    /// Inicializuje komponenty podľa zadaného command
    /// Spustí kompletnú startup sekvenciu s progress tracking
    /// </summary>
    Task<InitializationResult> InitializeAsync(
        InitializeComponentCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Graceful shutdown s cleanup všetkých resource
    /// Flushing pending operations a unregister global handlers
    /// </summary>
    Task<bool> ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Získa aktuálny status inicializácie
    /// </summary>
    InitializationStatus GetStatus();

    /// <summary>
    /// Indikátor či je komponenta inicializovaná
    /// </summary>
    bool IsInitialized { get; }
}

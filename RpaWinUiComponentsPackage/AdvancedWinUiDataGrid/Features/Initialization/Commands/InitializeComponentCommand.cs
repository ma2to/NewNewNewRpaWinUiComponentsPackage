using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;

/// <summary>
/// Command pre inicializáciu Advanced Data Grid komponentu
/// Immutable record s factory methods pre rôzne scenáre
/// Podporuje UI aj Headless režim
/// </summary>
internal sealed record InitializeComponentCommand
{
    /// <summary>Konfigurácia inicializácie</summary>
    internal InitializationConfiguration Configuration { get; init; } = new();

    /// <summary>Indikátor headless režimu (bez UI dependencies)</summary>
    internal bool IsHeadlessMode { get; init; } = false;

    /// <summary>Či sa má validovať konfigurácia pred inicializáciou</summary>
    internal bool ValidateConfiguration { get; init; } = true;

    /// <summary>Timeout pre inicializáciu</summary>
    internal TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Progress reporter pre sledovanie pokroku</summary>
    internal IProgress<InitializationProgress>? ProgressReporter { get; init; }

    /// <summary>Cancellation token pre zrušenie operácie</summary>
    internal CancellationToken CancellationToken { get; init; } = default;

    /// <summary>
    /// Factory metóda pre UI režim
    /// Inicializuje komponent s UI dependencies
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
    /// Factory metóda pre Headless režim
    /// Optimalizované pre server/background scenáre bez UI
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

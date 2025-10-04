namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

/// <summary>
/// Reprezentuje aktuálny status inicializácie komponentu
/// Thread-safe immutable record pre runtime inspection
/// </summary>
internal sealed record InitializationStatus
{
    /// <summary>Indikátor či je komponenta inicializovaná</summary>
    internal bool IsInitialized { get; init; }

    /// <summary>Indikátor headless režimu</summary>
    internal bool IsHeadlessMode { get; init; }

    /// <summary>Aktuálna fáza inicializácie</summary>
    internal InitializationPhase CurrentPhase { get; init; }

    /// <summary>Čas kedy bola inicializácia spustená</summary>
    internal DateTime? InitializationStartTime { get; init; }

    /// <summary>Čas kedy bola inicializácia dokončená</summary>
    internal DateTime? InitializationCompletedTime { get; init; }

    /// <summary>Trvanie inicializácie</summary>
    internal TimeSpan? InitializationDuration { get; init; }

    /// <summary>Posledná chybová správa (ak existuje)</summary>
    internal string? LastError { get; init; }
}

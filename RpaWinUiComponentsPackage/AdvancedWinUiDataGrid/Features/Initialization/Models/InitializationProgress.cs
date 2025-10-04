namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

/// <summary>
/// Reprezentuje pokrok inicializácie komponentu
/// Thread-safe immutable record pre progress reporting
/// </summary>
internal sealed record InitializationProgress
{
    /// <summary>Počet dokončených krokov</summary>
    internal int CompletedSteps { get; init; }

    /// <summary>Celkový počet krokov</summary>
    internal int TotalSteps { get; init; }

    /// <summary>Percentuálne dokončenie (0-100)</summary>
    internal double CompletionPercentage => TotalSteps > 0
        ? (double)CompletedSteps / TotalSteps * 100
        : 0;

    /// <summary>Uplynulý čas od začiatku inicializácie</summary>
    internal TimeSpan ElapsedTime { get; init; }

    /// <summary>Názov aktuálnej operácie</summary>
    internal string CurrentOperation { get; init; } = string.Empty;

    /// <summary>Aktuálna fáza inicializácie</summary>
    internal InitializationPhase CurrentPhase { get; init; } = InitializationPhase.None;

    /// <summary>Indikátor headless režimu</summary>
    internal bool IsHeadlessMode { get; init; }

    /// <summary>Odhadovaný zostávajúci čas (null ak nie je možné odhadnúť)</summary>
    internal TimeSpan? EstimatedTimeRemaining => CompletedSteps > 0 && TotalSteps > CompletedSteps
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalSteps - CompletedSteps) / CompletedSteps)
        : null;
}

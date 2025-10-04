namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

/// <summary>
/// PUBLIC API: Výsledok inicializácie komponentu
/// </summary>
public sealed record PublicInitializationResult
{
    /// <summary>Indikátor úspešnej inicializácie</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Správa o výsledku inicializácie</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Chybová správa (ak inicializácia zlyhala)</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Trvanie inicializácie</summary>
    public TimeSpan? Duration { get; init; }
}

/// <summary>
/// PUBLIC API: Pokrok inicializácie komponentu
/// </summary>
public sealed record PublicInitializationProgress
{
    /// <summary>Počet dokončených krokov</summary>
    public int CompletedSteps { get; init; }

    /// <summary>Celkový počet krokov</summary>
    public int TotalSteps { get; init; }

    /// <summary>Percentuálne dokončenie (0-100)</summary>
    public double CompletionPercentage { get; init; }

    /// <summary>Uplynulý čas od začiatku inicializácie</summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>Názov aktuálnej operácie</summary>
    public string CurrentOperation { get; init; } = string.Empty;

    /// <summary>Indikátor headless režimu</summary>
    public bool IsHeadlessMode { get; init; }

    /// <summary>Odhadovaný zostávajúci čas</summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }
}

/// <summary>
/// PUBLIC API: Aktuálny status inicializácie komponentu
/// </summary>
public sealed record PublicInitializationStatus
{
    /// <summary>Indikátor či je komponenta inicializovaná</summary>
    public bool IsInitialized { get; init; }

    /// <summary>Indikátor headless režimu</summary>
    public bool IsHeadlessMode { get; init; }

    /// <summary>Čas kedy bola inicializácia spustená</summary>
    public DateTime? InitializationStartTime { get; init; }

    /// <summary>Čas kedy bola inicializácia dokončená</summary>
    public DateTime? InitializationCompletedTime { get; init; }

    /// <summary>Trvanie inicializácie</summary>
    public TimeSpan? InitializationDuration { get; init; }

    /// <summary>Posledná chybová správa (ak existuje)</summary>
    public string? LastError { get; init; }
}

/// <summary>
/// PUBLIC API: Minimálna konfigurácia pre inicializáciu (pre public API)
/// </summary>
public sealed record PublicInitializationConfiguration
{
    /// <summary>Povoliť smart operácie</summary>
    public bool EnableSmartOperations { get; init; } = true;

    /// <summary>Povoliť pokročilú validáciu</summary>
    public bool EnableAdvancedValidation { get; init; } = true;

    /// <summary>Povoliť performance optimalizácie</summary>
    public bool EnablePerformanceOptimizations { get; init; } = true;

    /// <summary>Timeout pre inicializáciu</summary>
    public TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Predvolená konfigurácia
    /// </summary>
    public static PublicInitializationConfiguration Default => new();

    /// <summary>
    /// Konfigurácia optimalizovaná pre vysoký výkon
    /// </summary>
    public static PublicInitializationConfiguration HighPerformance => new()
    {
        EnablePerformanceOptimizations = true
    };

    /// <summary>
    /// Konfigurácia optimalizovaná pre server režim (headless)
    /// </summary>
    public static PublicInitializationConfiguration ServerMode => new()
    {
        EnableSmartOperations = false,
        EnablePerformanceOptimizations = true
    };
}

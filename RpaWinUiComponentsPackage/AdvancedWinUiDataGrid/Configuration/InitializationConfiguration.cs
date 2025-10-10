using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

/// <summary>
/// Hlavná konfigurácia for inicializáciu Advanced Data Grid komponentu
/// Hierarchická konfiguračná štruktúra for všetky aspekty inicializácie
/// Immutable record s factory methods for bežné scenáre
/// </summary>
internal sealed record InitializationConfiguration
{
    // SEKCIE KONFIGURÁCIE

    /// <summary>Konfigurácia performance optimalizácií</summary>
    internal PerformanceConfiguration? PerformanceConfig { get; init; }

    /// <summary>Konfigurácia validation systému</summary>
    internal ValidationConfiguration? ValidationConfig { get; init; }

    /// <summary>Konfigurácia grid správania</summary>
    internal GridBehaviorConfiguration? GridBehaviorConfig { get; init; }

    /// <summary>Vlastné nastavenia</summary>
    internal Dictionary<string, object?>? CustomSettings { get; init; }

    /// <summary>Konfigurácia farieb a témy (implementujeme neskôr)</summary>
    internal object? ColorTheme { get; init; }

    /// <summary>Konfigurácia auto-výšky riadkov</summary>
    internal AutoRowHeightConfiguration? AutoRowHeightConfig { get; init; }

    /// <summary>Konfigurácia klávesových skratiek</summary>
    internal KeyboardShortcutConfiguration? KeyboardConfig { get; init; }

    // INITIALIZATION-SPECIFIC SETTINGS

    /// <summary>Povoliť smart operácie (Add Above/Below, Smart Delete)</summary>
    internal bool EnableSmartOperations { get; init; } = true;

    /// <summary>Povoliť pokročilú validáciu</summary>
    internal bool EnableAdvancedValidation { get; init; } = true;

    /// <summary>Povoliť performance optimalizácie</summary>
    internal bool EnablePerformanceOptimizations { get; init; } = true;

    /// <summary>Timeout for inicializáciu</summary>
    internal TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Minimálny log level</summary>
    internal LogLevel MinimumLogLevel { get; init; } = LogLevel.Information;

    // FACTORY METHODS PRE BEŽNÉ SCENÁRE

    /// <summary>Predvolená konfigurácia</summary>
    internal static InitializationConfiguration Default => new();

    /// <summary>
    /// Konfigurácia optimalizovaná for vysoký výkon
    /// Vhodné for veľké datasety a production scenáre
    /// </summary>
    internal static InitializationConfiguration HighPerformance => new()
    {
        PerformanceConfig = new PerformanceConfiguration
        {
            EnableVirtualization = true,
            EnableLazyLoading = true,
            EnableMemoryOptimization = true,
            EnableAsyncOperations = true,
            VirtualizationThreshold = 500, // Nižší threshold for lepší výkon
            MaxConcurrentOperations = Environment.ProcessorCount * 2
        },
        EnablePerformanceOptimizations = true
    };

    /// <summary>
    /// Konfigurácia optimalizovaná for server režim (headless)
    /// Vypína UI-heavy operácie a zvyšuje concurrency
    /// </summary>
    internal static InitializationConfiguration ServerMode => new()
    {
        EnableSmartOperations = false, // Vypnúť UI-heavy operácie
        PerformanceConfig = new PerformanceConfiguration
        {
            EnableVirtualization = false, // Nie je potrebné v headless mode
            EnableAsyncOperations = true,
            MaxConcurrentOperations = Environment.ProcessorCount * 4 // Vyššia concurrency
        }
    };
}

/// <summary>
/// Konfigurácia performance optimalizácií
/// </summary>
internal sealed record PerformanceConfiguration
{
    internal bool EnableVirtualization { get; init; } = true;
    internal bool EnableLazyLoading { get; init; } = true;
    internal bool EnableMemoryOptimization { get; init; } = false;
    internal bool EnableAsyncOperations { get; init; } = true;
    internal int VirtualizationThreshold { get; init; } = 1000;
    internal int MaxConcurrentOperations { get; init; } = Environment.ProcessorCount;
}

/// <summary>
/// Konfigurácia validation systému
/// </summary>
internal sealed record ValidationConfiguration
{
    internal bool EnableValidation { get; init; } = true;
    internal bool EnableAsyncValidation { get; init; } = true;
    internal int ValidationBatchSize { get; init; } = 100;
    internal TimeSpan ValidationTimeout { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Konfigurácia auto-výšky riadkov
/// </summary>
internal sealed record AutoRowHeightConfiguration
{
    internal bool EnableAutoRowHeight { get; init; } = false;
    internal int MinimumRowHeight { get; init; } = 20;
    internal int MaximumRowHeight { get; init; } = 500;
}

/// <summary>
/// Konfigurácia klávesových skratiek
/// </summary>
internal sealed record KeyboardShortcutConfiguration
{
    internal bool EnableKeyboardShortcuts { get; init; } = true;
    internal Dictionary<string, string>? CustomShortcuts { get; init; }
}

/// <summary>
/// Konfigurácia grid správania
/// </summary>
internal sealed record GridBehaviorConfiguration
{
    internal bool EnableSmartOperations { get; init; } = true;
    internal bool EnableAutomaticValidation { get; init; } = true;
    internal int MinimumRowCount { get; init; } = 0;
}

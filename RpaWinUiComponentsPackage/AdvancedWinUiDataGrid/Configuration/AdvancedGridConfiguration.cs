using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

/// <summary>
/// ENTERPRISE: Extended configuration options for advanced features
/// </summary>
internal sealed record AdvancedGridConfiguration
{
    // UI Configuration
    public UIMode UIMode { get; init; } = UIMode.Interactive;
    public ThemeMode ThemeMode { get; init; } = ThemeMode.Light;
    public RenderingMode RenderingMode { get; init; } = RenderingMode.Standard;

    // Security Configuration
    public SecurityLevel SecurityLevel { get; init; } = SecurityLevel.Standard;
    public bool EnableInputValidation { get; init; } = true;
    public bool EnableAccessControl { get; init; } = false;

    // Logging Configuration
    public LoggingConfiguration LoggingConfiguration { get; init; } = LoggingConfiguration.Default;

    // Performance Configuration
    public bool EnableCaching { get; init; } = true;
    public TimeSpan CacheExpiration { get; init; } = TimeSpan.FromMinutes(30);
    public int MaxConcurrentOperations { get; init; } = Environment.ProcessorCount;

    // Feature Toggles
    public bool EnableExperimentalFeatures { get; init; } = false;
    public bool EnableDiagnostics { get; init; } = false;

    public static AdvancedGridConfiguration Default => new();

    public static AdvancedGridConfiguration HighPerformance => new()
    {
        RenderingMode = RenderingMode.HighPerformance,
        EnableCaching = true,
        MaxConcurrentOperations = Environment.ProcessorCount * 2
    };

    public static AdvancedGridConfiguration Secure => new()
    {
        SecurityLevel = SecurityLevel.Enhanced,
        EnableInputValidation = true,
        EnableAccessControl = true
    };
}

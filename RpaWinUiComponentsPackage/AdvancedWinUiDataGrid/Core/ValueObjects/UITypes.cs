namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

/// <summary>
/// DDD: UI operation mode value object
/// </summary>
internal enum UIMode
{
    Interactive,
    ReadOnly,
    Disabled
}

/// <summary>
/// DDD: Rendering mode for performance optimization
/// </summary>
internal enum RenderingMode
{
    Standard,
    HighPerformance,
    LowLatency
}

/// <summary>
/// DDD: Theme mode for UI customization
/// </summary>
internal enum ThemeMode
{
    Light,
    Dark,
    HighContrast,
    Custom
}

/// <summary>
/// DDD: UI refresh scope
/// </summary>
internal enum UIRefreshScope
{
    Full,
    Partial,
    Incremental
}

/// <summary>
/// DDD: UI theme configuration value object
/// </summary>
internal sealed record UIThemeConfiguration
{
    public ThemeMode Mode { get; init; } = ThemeMode.Light;
    public string? CustomThemeName { get; init; }
    public IDictionary<string, string>? ColorOverrides { get; init; }

    public static UIThemeConfiguration Light => new() { Mode = ThemeMode.Light };
    public static UIThemeConfiguration Dark => new() { Mode = ThemeMode.Dark };
    public static UIThemeConfiguration HighContrast => new() { Mode = ThemeMode.HighContrast };
}

/// <summary>
/// DDD: UI refresh request value object
/// </summary>
internal sealed record UIRefreshRequest
{
    public UIRefreshScope Scope { get; init; } = UIRefreshScope.Full;
    public bool ForceUpdate { get; init; } = false;
    public TimeSpan? DelayBeforeRefresh { get; init; }
    public IReadOnlyList<string>? TargetElements { get; init; }
}

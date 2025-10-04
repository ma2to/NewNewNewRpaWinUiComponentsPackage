using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging;

/// <summary>
/// CONFIGURATION: Logging configuration options
/// </summary>
internal sealed record LoggingConfiguration
{
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;
    public bool EnableStructuredLogging { get; init; } = true;
    public bool EnablePerformanceLogging { get; init; } = true;
    public bool EnableCorrelationIds { get; init; } = true;
    public bool EnableDetailedExceptions { get; init; } = false;
    public TimeSpan PerformanceThreshold { get; init; } = TimeSpan.FromSeconds(1);

    public static LoggingConfiguration Default => new();

    public static LoggingConfiguration Development => new()
    {
        MinimumLevel = LogLevel.Debug,
        EnableDetailedExceptions = true,
        PerformanceThreshold = TimeSpan.FromMilliseconds(500)
    };

    public static LoggingConfiguration Production => new()
    {
        MinimumLevel = LogLevel.Information,
        EnableDetailedExceptions = false,
        PerformanceThreshold = TimeSpan.FromSeconds(2)
    };
}

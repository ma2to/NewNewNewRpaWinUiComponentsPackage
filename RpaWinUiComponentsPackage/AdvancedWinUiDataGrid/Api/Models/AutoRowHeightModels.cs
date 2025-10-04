namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Configuration for automatic row height calculations (Public DTO)
/// </summary>
public record PublicAutoRowHeightConfiguration(
    bool IsEnabled = true,
    double MinimumRowHeight = 20.0,
    double MaximumRowHeight = 200.0,
    string DefaultFontFamily = "Segoe UI",
    double DefaultFontSize = 12.0,
    bool EnableTextWrapping = true,
    bool UseCache = true,
    int CacheMaxSize = 1000,
    double? CalculationTimeoutSeconds = null
);

/// <summary>
/// Result of auto row height operation (Public DTO)
/// </summary>
public record PublicAutoRowHeightResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    double? DurationMs = null,
    int? AffectedRows = null
);

/// <summary>
/// Result of row height calculation (Public DTO)
/// </summary>
public record PublicRowHeightCalculationResult(
    int RowIndex,
    double CalculatedHeight,
    bool IsSuccess,
    string? ErrorMessage = null,
    double? CalculationTimeMs = null
);

/// <summary>
/// Text measurement result (Public DTO)
/// </summary>
public record PublicTextMeasurementResult(
    double Width,
    double Height,
    string MeasuredText,
    string FontFamily,
    double FontSize,
    bool TextWrapped
);

/// <summary>
/// Options for row height calculation (Public DTO)
/// </summary>
public record PublicRowHeightCalculationOptions(
    double? MinHeight = null,
    double? MaxHeight = null,
    string? FontFamily = null,
    double? FontSize = null,
    bool? EnableWrapping = null
);

/// <summary>
/// Progress information for batch calculations (Public DTO)
/// </summary>
public record PublicBatchCalculationProgress(
    int ProcessedRows,
    int TotalRows,
    double ElapsedTimeMs,
    string CurrentOperation = ""
)
{
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
}

/// <summary>
/// Statistics for auto row height operations (Public DTO)
/// </summary>
public record PublicAutoRowHeightStatistics(
    int TotalCalculations,
    int CachedCalculations,
    int FailedCalculations,
    double TotalCalculationTimeMs,
    double AverageCalculationTimeMs,
    double CacheHitRate,
    int CurrentCacheSize
);

/// <summary>
/// Cache statistics (Public DTO)
/// </summary>
public record PublicCacheStatistics(
    int TotalEntries,
    int MaxSize,
    double HitRate,
    double MissRate,
    long MemoryUsageBytes,
    double OldestEntryAgeMs,
    int RecentEvictions
);

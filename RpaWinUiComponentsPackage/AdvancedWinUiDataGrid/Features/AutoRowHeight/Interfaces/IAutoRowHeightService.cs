using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;

/// <summary>
/// Service interface for automatic row height management with enterprise features
/// Implements Scoped lifetime per DI_DECISIONS.md - per-operation state isolation
/// PERFORMANCE: Optimized text measurement with intelligent caching
/// VIRTUALIZATION: Support for large datasets with minimal memory footprint
/// </summary>
internal interface IAutoRowHeightService
{
    #region Configuration Management

    /// <summary>
    /// Enable/configure automatic row height calculation
    /// CONFIGURATION: Apply measurement settings with validation
    /// </summary>
    Task<AutoRowHeightResult> EnableAutoRowHeightAsync(
        AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply new configuration to existing setup
    /// DYNAMIC: Runtime configuration updates with cache invalidation
    /// </summary>
    Task<AutoRowHeightResult> ApplyConfigurationAsync(
        AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate measurement cache
    /// PERFORMANCE: Force recalculation for updated content
    /// </summary>
    Task<bool> InvalidateHeightCacheAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Height Calculations

    /// <summary>
    /// Calculate optimal row heights for all rows
    /// BATCH: Efficient bulk height calculations with progress reporting
    /// </summary>
    Task<IReadOnlyList<RowHeightCalculationResult>> CalculateOptimalRowHeightsAsync(
        IProgress<BatchCalculationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate height for specific row
    /// PRECISION: Accurate height calculation with text wrapping support
    /// </summary>
    Task<RowHeightCalculationResult> CalculateRowHeightAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        RowHeightCalculationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Measure text dimensions for height calculation
    /// TEXT: Advanced text measurement with font and wrapping support
    /// </summary>
    Task<TextMeasurementResult> MeasureTextAsync(
        string text,
        string fontFamily,
        double fontSize,
        double maxWidth,
        bool textWrapping = true,
        CancellationToken cancellationToken = default);

    #endregion

    #region Statistics & Monitoring

    /// <summary>
    /// Get current auto height statistics
    /// PERFORMANCE: Measurement performance and cache metrics
    /// </summary>
    AutoRowHeightStatistics GetStatistics();

    /// <summary>
    /// Get measurement cache information
    /// CACHE: Cache hit rates and memory usage analysis
    /// </summary>
    CacheStatistics GetCacheStatistics();

    #endregion

    // Simple wrapper methods for /Api public interface
    Task<Common.Models.Result> EnableAutoRowHeightAsync(CancellationToken cancellationToken = default);
    Task<Common.Models.Result> DisableAutoRowHeightAsync(CancellationToken cancellationToken = default);
    Task<Common.Models.Result<double>> AdjustRowHeightAsync(int rowIndex, CancellationToken cancellationToken = default);
    Task<Common.Models.Result> AdjustAllRowHeightsAsync(CancellationToken cancellationToken = default);
    Common.Models.Result SetMinRowHeight(double height);
    Common.Models.Result SetMaxRowHeight(double height);
    bool IsAutoRowHeightEnabled();
    double GetMinRowHeight();
    double GetMaxRowHeight();
}

/// <summary>
/// Configuration for automatic row height calculations
/// </summary>
internal record AutoRowHeightConfiguration(
    bool IsEnabled = true,
    double MinimumRowHeight = 20.0,
    double MaximumRowHeight = 200.0,
    string DefaultFontFamily = "Segoe UI",
    double DefaultFontSize = 12.0,
    bool EnableTextWrapping = true,
    bool UseCache = true,
    int CacheMaxSize = 1000,
    TimeSpan? CalculationTimeout = null
)
{
    public static AutoRowHeightConfiguration Default { get; } = new();

    public static AutoRowHeightConfiguration Responsive { get; } = new(
        MinimumRowHeight: 24.0,
        MaximumRowHeight: 150.0,
        DefaultFontSize: 13.0
    );

    public static AutoRowHeightConfiguration Compact { get; } = new(
        MinimumRowHeight: 18.0,
        MaximumRowHeight: 100.0,
        DefaultFontSize: 11.0
    );

    public static AutoRowHeightConfiguration Performance { get; } = new(
        EnableTextWrapping: false,
        CacheMaxSize: 2000,
        CalculationTimeout: TimeSpan.FromSeconds(1)
    );
}

/// <summary>
/// Result of auto row height operation
/// </summary>
internal record AutoRowHeightResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    TimeSpan? Duration = null,
    int? AffectedRows = null
)
{
    public static AutoRowHeightResult Success(TimeSpan duration, int affectedRows) =>
        new(true, null, duration, affectedRows);

    public static AutoRowHeightResult CreateFailure(IReadOnlyList<string> errors, TimeSpan duration) =>
        new(false, string.Join(", ", errors), duration);

    public static AutoRowHeightResult Failure(string error, TimeSpan? duration = null) =>
        new(false, error, duration);
}

/// <summary>
/// Result of row height calculation
/// </summary>
internal record RowHeightCalculationResult(
    int RowIndex,
    double CalculatedHeight,
    bool IsSuccess,
    string? ErrorMessage = null,
    TimeSpan? CalculationTime = null
)
{
    public static RowHeightCalculationResult Success(int rowIndex, double height, TimeSpan calculationTime) =>
        new(rowIndex, height, true, null, calculationTime);

    public static RowHeightCalculationResult Failure(int rowIndex, string error) =>
        new(rowIndex, 0, false, error);
}

/// <summary>
/// Text measurement result with dimensions
/// </summary>
internal record TextMeasurementResult(
    double Width,
    double Height,
    string MeasuredText,
    string FontFamily,
    double FontSize,
    bool TextWrapped
);

/// <summary>
/// Options for row height calculation
/// </summary>
internal record RowHeightCalculationOptions(
    double? MinHeight = null,
    double? MaxHeight = null,
    string? FontFamily = null,
    double? FontSize = null,
    bool? EnableWrapping = null
);

/// <summary>
/// Progress information for batch calculations
/// </summary>
internal record BatchCalculationProgress(
    int ProcessedRows,
    int TotalRows,
    TimeSpan ElapsedTime,
    string CurrentOperation = ""
)
{
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;

    public TimeSpan? EstimatedTimeRemaining => ProcessedRows > 0 && TotalRows > ProcessedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ProcessedRows) / ProcessedRows)
        : null;
};

/// <summary>
/// Statistics for auto row height operations
/// </summary>
internal record AutoRowHeightStatistics(
    int TotalCalculations,
    int CachedCalculations,
    int FailedCalculations,
    TimeSpan TotalCalculationTime,
    TimeSpan AverageCalculationTime,
    double CacheHitRate,
    int CurrentCacheSize
);

/// <summary>
/// Cache statistics for monitoring
/// </summary>
internal record CacheStatistics(
    int TotalEntries,
    int MaxSize,
    double HitRate,
    double MissRate,
    long MemoryUsageBytes,
    TimeSpan OldestEntry,
    int RecentEvictions
);
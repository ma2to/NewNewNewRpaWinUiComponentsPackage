using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Services;

/// <summary>
/// Internal implementation of automatic row height management service
/// PERFORMANCE: Optimized text measurement with intelligent caching
/// VIRTUALIZATION: Support for large datasets with minimal memory footprint
/// Thread-safe with no per-operation mutable fields
/// </summary>
internal sealed class AutoRowHeightService : IAutoRowHeightService
{
    private readonly ILogger<AutoRowHeightService> _logger;
    private readonly IRowStore _rowStore;
    private readonly AdvancedDataGridOptions _options;

    // Thread-safe shared state - not per-operation fields
    private readonly ConcurrentDictionary<string, TextMeasurementResult> _measurementCache = new();
    private readonly SemaphoreSlim _calculationSemaphore;

    private volatile AutoRowHeightConfiguration _currentConfiguration = AutoRowHeightConfiguration.Default;
    private volatile bool _isEnabled = false;

    // Statistics tracking - thread-safe
    private volatile int _totalCalculations = 0;
    private volatile int _cachedCalculations = 0;
    private volatile int _failedCalculations = 0;
    private long _totalCalculationTicks = 0; // Use Interlocked.Add for long values

    private readonly IOperationLogger<AutoRowHeightService> _operationLogger;

    public AutoRowHeightService(
        ILogger<AutoRowHeightService> logger,
        IRowStore rowStore,
        AdvancedDataGridOptions options,
        IOperationLogger<AutoRowHeightService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Použijeme null pattern ak logger nie je poskytnutý
        _operationLogger = operationLogger ?? NullOperationLogger<AutoRowHeightService>.Instance;

        _calculationSemaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
    }

    #region Configuration Management

    public async Task<AutoRowHeightResult> EnableAutoRowHeightAsync(
        AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname enable auto row height operáciu - vytvoríme operation scope pre automatické tracking
        using var scope = _operationLogger.LogOperationStart("EnableAutoRowHeightAsync", new
        {
            OperationId = operationId,
            MinimumRowHeight = configuration.MinimumRowHeight,
            MaximumRowHeight = configuration.MaximumRowHeight,
            IsEnabled = configuration.IsEnabled
        });

        _logger.LogInformation("Starting enable auto row height for operation {OperationId} with min={MinHeight}, max={MaxHeight}",
            operationId, configuration.MinimumRowHeight, configuration.MaximumRowHeight);

        try
        {
            // Validujeme konfiguráciu - per-operation state kept locally
            _logger.LogInformation("Validating auto row height configuration for operation {OperationId}", operationId);

            var validationResult = ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Auto row height configuration validation failed for operation {OperationId}: {Errors}",
                    operationId, string.Join(", ", validationResult.ErrorMessages));

                scope.MarkFailure(new InvalidOperationException($"Configuration validation failed: {string.Join(", ", validationResult.ErrorMessages)}"));
                return AutoRowHeightResult.CreateFailure(validationResult.ErrorMessages, stopwatch.Elapsed);
            }

            // Aplikujeme konfiguráciu atomicky
            _currentConfiguration = configuration;
            _isEnabled = configuration.IsEnabled;

            _logger.LogInformation("Configuration applied. IsEnabled={IsEnabled} for operation {OperationId}",
                configuration.IsEnabled, operationId);

            // Vymažeme cache ak sa konfigurácia významne zmenila
            if (ShouldClearCache(configuration))
            {
                _logger.LogInformation("Configuration changed significantly, clearing cache for operation {OperationId}", operationId);
                await InvalidateHeightCacheAsync(cancellationToken);
            }

            _logger.LogInformation("Auto row height enabled successfully in {Duration}ms for operation {OperationId}",
                stopwatch.ElapsedMilliseconds, operationId);

            scope.MarkSuccess(new
            {
                IsEnabled = configuration.IsEnabled,
                MinHeight = configuration.MinimumRowHeight,
                MaxHeight = configuration.MaximumRowHeight,
                Duration = stopwatch.Elapsed
            });

            return AutoRowHeightResult.Success(stopwatch.Elapsed, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto row height enablement failed for operation {OperationId}: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            return AutoRowHeightResult.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    public async Task<AutoRowHeightResult> ApplyConfigurationAsync(
        AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return await EnableAutoRowHeightAsync(configuration, cancellationToken);
    }

    public async Task<bool> InvalidateHeightCacheAsync(CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();

        _logger.LogInformation("Starting invalidation of auto row height cache for operation {OperationId}. " +
            "Current cache size: {CacheSize}",
            operationId, _measurementCache.Count);

        try
        {
            var cacheSize = _measurementCache.Count;
            _measurementCache.Clear();

            _logger.LogInformation("Auto row height cache invalidated for operation {OperationId}. " +
                "Cleared {CacheSize} entries",
                operationId, cacheSize);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate auto row height cache for operation {OperationId}: {Message}",
                operationId, ex.Message);
            return false;
        }
    }

    #endregion

    #region Height Calculations

    public async Task<IReadOnlyList<RowHeightCalculationResult>> CalculateOptimalRowHeightsAsync(
        IProgress<BatchCalculationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            _logger.LogWarning("Auto row height is not enabled - skipping calculation");
            return Array.Empty<RowHeightCalculationResult>();
        }

        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname batch row height calculation - vytvoríme operation scope pre automatické tracking
        using var scope = _operationLogger.LogOperationStart("CalculateOptimalRowHeightsAsync", new
        {
            OperationId = operationId
        });

        _logger.LogInformation("Starting batch row height calculation for operation {OperationId}", operationId);

        try
        {
            // Získame všetky riadky - per-operation data access
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
            var allRowsList = allRows.ToList();
            var totalRows = allRowsList.Count;
            var results = new List<RowHeightCalculationResult>(totalRows);

            _logger.LogInformation("Loaded {TotalRows} rows for height calculation for operation {OperationId}",
                totalRows, operationId);

            // Spracujeme v dávkach aby sme sa vyhli memory pressure
            var batchSize = _options.BatchSize;
            var processedRows = 0;

            for (int batchStart = 0; batchStart < totalRows; batchStart += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchEnd = Math.Min(batchStart + batchSize, totalRows);
                var batchRows = allRowsList.Skip(batchStart).Take(batchEnd - batchStart).ToList();

                _logger.LogInformation("Processing batch {BatchStart}-{BatchEnd} for operation {OperationId}",
                    batchStart, batchEnd - 1, operationId);

                // Spracujeme dávku s paralelným výpočtom
                var batchResults = await CalculateBatchRowHeightsAsync(batchRows, batchStart, cancellationToken);
                results.AddRange(batchResults);

                processedRows += batchRows.Count;

                // Reportujeme progress
                progress?.Report(new BatchCalculationProgress(
                    ProcessedRows: processedRows,
                    TotalRows: totalRows,
                    ElapsedTime: stopwatch.Elapsed,
                    CurrentOperation: $"Processing batch {batchStart}-{batchEnd - 1}"
                ));

                // Malé delay pre cooperative processing
                if (batchEnd < totalRows)
                    await Task.Delay(1, cancellationToken);
            }

            var stats = GetStatistics();

            _logger.LogInformation("Batch row height calculation {OperationId} completed in {Duration}ms. " +
                "Processed {RowCount} rows. Cache hit rate: {CacheHitRate:F2}%",
                operationId, stopwatch.ElapsedMilliseconds, processedRows, stats.CacheHitRate);

            scope.MarkSuccess(new
            {
                ProcessedRows = processedRows,
                CacheHitRate = stats.CacheHitRate,
                Duration = stopwatch.Elapsed
            });

            return results;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("Batch row height calculation {OperationId} was cancelled", operationId);

            scope.MarkFailure(ex);
            return Array.Empty<RowHeightCalculationResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch row height calculation {OperationId} failed: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            return Array.Empty<RowHeightCalculationResult>();
        }
    }

    public async Task<RowHeightCalculationResult> CalculateRowHeightAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        RowHeightCalculationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            return RowHeightCalculationResult.Failure(rowIndex, "Auto row height is not enabled");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _calculationSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Use per-operation configuration
                var config = _currentConfiguration;
                var effectiveOptions = MergeOptions(options, config);

                // Calculate height based on row content - per-operation state
                var calculatedHeight = await CalculateHeightForRowDataAsync(
                    rowData, effectiveOptions, cancellationToken);

                // Apply constraints
                var constrainedHeight = Math.Max(effectiveOptions.MinHeight ?? config.MinimumRowHeight,
                    Math.Min(calculatedHeight, effectiveOptions.MaxHeight ?? config.MaximumRowHeight));

                // Update statistics atomically
                Interlocked.Increment(ref _totalCalculations);
                Interlocked.Add(ref _totalCalculationTicks, stopwatch.ElapsedTicks);

                return RowHeightCalculationResult.Success(rowIndex, constrainedHeight, stopwatch.Elapsed);
            }
            finally
            {
                _calculationSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failedCalculations);
            _logger.LogError(ex, "Row height calculation failed for row {RowIndex}", rowIndex);
            return RowHeightCalculationResult.Failure(rowIndex, ex.Message);
        }
    }

    public async Task<TextMeasurementResult> MeasureTextAsync(
        string text,
        string fontFamily,
        double fontSize,
        double maxWidth,
        bool textWrapping = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new TextMeasurementResult(0, _currentConfiguration.MinimumRowHeight,
                text ?? string.Empty, fontFamily, fontSize, false);
        }

        // Create cache key - per-operation data
        var cacheKey = $"{text}|{fontFamily}|{fontSize}|{maxWidth}|{textWrapping}";

        // Check cache first
        if (_currentConfiguration.UseCache && _measurementCache.TryGetValue(cacheKey, out var cachedResult))
        {
            Interlocked.Increment(ref _cachedCalculations);
            return cachedResult;
        }

        try
        {
            // Perform text measurement - this would typically use WinUI TextBlock or similar
            var measuredHeight = await MeasureTextInternalAsync(text, fontFamily, fontSize, maxWidth, textWrapping, cancellationToken);

            var result = new TextMeasurementResult(
                Width: Math.Min(maxWidth, CalculateTextWidth(text, fontFamily, fontSize)),
                Height: measuredHeight,
                MeasuredText: text,
                FontFamily: fontFamily,
                FontSize: fontSize,
                TextWrapped: textWrapping && measuredHeight > fontSize * 1.2
            );

            // Cache result if enabled
            if (_currentConfiguration.UseCache && _measurementCache.Count < _currentConfiguration.CacheMaxSize)
            {
                _measurementCache.TryAdd(cacheKey, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text measurement failed for text: {TextLength} chars", text.Length);

            // Return fallback measurement
            return new TextMeasurementResult(
                Width: maxWidth,
                Height: fontSize * 1.5, // Fallback height
                MeasuredText: text,
                FontFamily: fontFamily,
                FontSize: fontSize,
                TextWrapped: false
            );
        }
    }

    #endregion

    #region Statistics & Monitoring

    public AutoRowHeightStatistics GetStatistics()
    {
        var totalCalcs = _totalCalculations;
        var cachedCalcs = _cachedCalculations;
        var failedCalcs = _failedCalculations;
        var totalTicks = _totalCalculationTicks;

        return new AutoRowHeightStatistics(
            TotalCalculations: totalCalcs,
            CachedCalculations: cachedCalcs,
            FailedCalculations: failedCalcs,
            TotalCalculationTime: new TimeSpan(totalTicks),
            AverageCalculationTime: totalCalcs > 0 ? new TimeSpan(totalTicks / totalCalcs) : TimeSpan.Zero,
            CacheHitRate: totalCalcs > 0 ? (double)cachedCalcs / totalCalcs * 100 : 0,
            CurrentCacheSize: _measurementCache.Count
        );
    }

    public CacheStatistics GetCacheStatistics()
    {
        var cacheCount = _measurementCache.Count;
        var totalCalcs = _totalCalculations;
        var cachedCalcs = _cachedCalculations;

        return new CacheStatistics(
            TotalEntries: cacheCount,
            MaxSize: _currentConfiguration.CacheMaxSize,
            HitRate: totalCalcs > 0 ? (double)cachedCalcs / totalCalcs * 100 : 0,
            MissRate: totalCalcs > 0 ? (double)(totalCalcs - cachedCalcs) / totalCalcs * 100 : 0,
            MemoryUsageBytes: EstimateCacheMemoryUsage(),
            OldestEntry: TimeSpan.Zero, // Could be tracked if needed
            RecentEvictions: 0 // Could be tracked if needed
        );
    }

    #endregion

    #region Private Helper Methods

    private (bool IsValid, IReadOnlyList<string> ErrorMessages) ValidateConfiguration(AutoRowHeightConfiguration configuration)
    {
        var errors = new List<string>();

        if (configuration.MinimumRowHeight <= 0)
            errors.Add("Minimum row height must be positive");

        if (configuration.MaximumRowHeight <= configuration.MinimumRowHeight)
            errors.Add("Maximum row height must be greater than minimum row height");

        if (configuration.DefaultFontSize <= 0)
            errors.Add("Default font size must be positive");

        if (configuration.CacheMaxSize < 0)
            errors.Add("Cache max size must be non-negative");

        return (errors.Count == 0, errors);
    }

    private bool ShouldClearCache(AutoRowHeightConfiguration newConfiguration)
    {
        return newConfiguration.DefaultFontFamily != _currentConfiguration.DefaultFontFamily ||
               Math.Abs(newConfiguration.DefaultFontSize - _currentConfiguration.DefaultFontSize) > 0.1 ||
               newConfiguration.EnableTextWrapping != _currentConfiguration.EnableTextWrapping;
    }

    private async Task<List<RowHeightCalculationResult>> CalculateBatchRowHeightsAsync(
        List<IReadOnlyDictionary<string, object?>> batchRows,
        int batchStartIndex,
        CancellationToken cancellationToken)
    {
        var results = new List<RowHeightCalculationResult>(batchRows.Count);

        // Process rows in parallel within the batch
        await Task.Run(() =>
        {
            Parallel.ForEach(batchRows.Select((row, index) => new { row, index }),
                new ParallelOptions { CancellationToken = cancellationToken },
                rowData =>
                {
                    var absoluteRowIndex = batchStartIndex + rowData.index;
                    var result = CalculateRowHeightAsync(absoluteRowIndex, rowData.row, null, cancellationToken).Result;

                    lock (results)
                    {
                        results.Add(result);
                    }
                });
        }, cancellationToken);

        return results.OrderBy(r => r.RowIndex).ToList();
    }

    private RowHeightCalculationOptions MergeOptions(RowHeightCalculationOptions? options, AutoRowHeightConfiguration config)
    {
        return new RowHeightCalculationOptions(
            MinHeight: options?.MinHeight ?? config.MinimumRowHeight,
            MaxHeight: options?.MaxHeight ?? config.MaximumRowHeight,
            FontFamily: options?.FontFamily ?? config.DefaultFontFamily,
            FontSize: options?.FontSize ?? config.DefaultFontSize,
            EnableWrapping: options?.EnableWrapping ?? config.EnableTextWrapping
        );
    }

    private async Task<double> CalculateHeightForRowDataAsync(
        IReadOnlyDictionary<string, object?> rowData,
        RowHeightCalculationOptions options,
        CancellationToken cancellationToken)
    {
        double maxHeight = options.MinHeight ?? _currentConfiguration.MinimumRowHeight;

        // Calculate height for each column's content
        foreach (var kvp in rowData)
        {
            var text = kvp.Value?.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                // Assume standard column width for measurement
                var columnWidth = 150.0; // This would typically come from actual column widths

                var measurement = await MeasureTextAsync(
                    text,
                    options.FontFamily ?? _currentConfiguration.DefaultFontFamily,
                    options.FontSize ?? _currentConfiguration.DefaultFontSize,
                    columnWidth,
                    options.EnableWrapping ?? _currentConfiguration.EnableTextWrapping,
                    cancellationToken);

                maxHeight = Math.Max(maxHeight, measurement.Height);
            }
        }

        return maxHeight;
    }

    private async Task<double> MeasureTextInternalAsync(
        string text,
        string fontFamily,
        double fontSize,
        double maxWidth,
        bool textWrapping,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Placeholder for actual async text measurement

        // Simple text height estimation - in real implementation this would use WinUI TextBlock
        var lines = textWrapping ? Math.Ceiling(CalculateTextWidth(text, fontFamily, fontSize) / maxWidth) : 1;
        return Math.Max(fontSize * 1.2 * lines, _currentConfiguration.MinimumRowHeight);
    }

    private double CalculateTextWidth(string text, string fontFamily, double fontSize)
    {
        // Simple width estimation - in real implementation this would use actual text measurement
        return text.Length * fontSize * 0.6; // Rough character width estimation
    }

    private long EstimateCacheMemoryUsage()
    {
        // Rough estimation of cache memory usage
        return _measurementCache.Count * 200; // Assume ~200 bytes per cache entry
    }

    #endregion

    #region Simple Wrapper Methods for Public API

    // Tieto metódy slúžia ako jednoduché wrappery pre DataGridAutoRowHeight v /Api
    // Mapujú jednoduché volania na command-based internal API

    async Task<Common.Models.Result> IAutoRowHeightService.EnableAutoRowHeightAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await this.EnableAutoRowHeightAsync(AutoRowHeightConfiguration.Default, cancellationToken);
            return result.IsSuccess
                ? Common.Models.Result.Success()
                : Common.Models.Result.Failure(result.ErrorMessage ?? "Enable failed");
        }
        catch (Exception ex)
        {
            return Common.Models.Result.Failure($"Enable auto row height failed: {ex.Message}");
        }
    }

    public async Task<Common.Models.Result> DisableAutoRowHeightAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        _isEnabled = false;
        _currentConfiguration = _currentConfiguration with { IsEnabled = false };
        _logger.LogInformation("Auto row height disabled");
        return Common.Models.Result.Success();
    }

    public async Task<Common.Models.Result<double>> AdjustRowHeightAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowData = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
            if (rowData == null)
                return Common.Models.Result<double>.Failure("Row not found");

            var result = await CalculateRowHeightAsync(rowIndex, rowData, null, cancellationToken);
            return result.IsSuccess
                ? Common.Models.Result<double>.Success(result.CalculatedHeight)
                : Common.Models.Result<double>.Failure(result.ErrorMessage ?? "Calculation failed");
        }
        catch (Exception ex)
        {
            return Common.Models.Result<double>.Failure($"Adjust row height failed: {ex.Message}");
        }
    }

    public async Task<Common.Models.Result> AdjustAllRowHeightsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await CalculateOptimalRowHeightsAsync(null, cancellationToken);
            var failedCount = results.Count(r => !r.IsSuccess);
            return failedCount == 0
                ? Common.Models.Result.Success()
                : Common.Models.Result.Failure($"{failedCount} rows failed calculation");
        }
        catch (Exception ex)
        {
            return Common.Models.Result.Failure($"Adjust all row heights failed: {ex.Message}");
        }
    }

    public Common.Models.Result SetMinRowHeight(double height)
    {
        _currentConfiguration = _currentConfiguration with { MinimumRowHeight = height };
        _logger.LogInformation("Minimum row height set to {Height}", height);
        return Common.Models.Result.Success();
    }

    public Common.Models.Result SetMaxRowHeight(double height)
    {
        _currentConfiguration = _currentConfiguration with { MaximumRowHeight = height };
        _logger.LogInformation("Maximum row height set to {Height}", height);
        return Common.Models.Result.Success();
    }

    public bool IsAutoRowHeightEnabled()
    {
        return _isEnabled;
    }

    public double GetMinRowHeight()
    {
        return _currentConfiguration.MinimumRowHeight;
    }

    public double GetMaxRowHeight()
    {
        return _currentConfiguration.MaximumRowHeight;
    }

    #endregion
}
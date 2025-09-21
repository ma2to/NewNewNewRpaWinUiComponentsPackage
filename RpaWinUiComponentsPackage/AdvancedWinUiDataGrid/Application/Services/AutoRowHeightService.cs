using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// INTERNAL SERVICE: Auto row height operations implementation
/// CLEAN ARCHITECTURE: Application layer service for auto row height operations
/// </summary>
internal sealed class AutoRowHeightService : IAutoRowHeightService
{
    private readonly ConcurrentDictionary<string, CoreTypes.TextMeasurementResult> _measurementCache = new();
    private CoreTypes.AutoRowHeightConfiguration _currentConfiguration = CoreTypes.AutoRowHeightConfiguration.Default;
    private bool _isEnabled = false;

    public async Task<CoreTypes.AutoRowHeightResult> EnableAutoRowHeightAsync(
        CoreTypes.AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _currentConfiguration = configuration;
                _isEnabled = configuration.IsEnabled;

                if (!configuration.EnableMeasurementCache)
                {
                    _measurementCache.Clear();
                }

                stopwatch.Stop();
                return CoreTypes.AutoRowHeightResult.CreateSuccess(
                    Array.Empty<CoreTypes.RowHeightCalculationResult>(),
                    stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return CoreTypes.AutoRowHeightResult.Failure($"Failed to enable auto row height: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<CoreTypes.AutoRowHeightResult> CalculateOptimalRowHeightsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.RowHeightCalculationOptions options,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var dataList = data.ToList();
                var results = new List<CoreTypes.RowHeightCalculationResult>();

                for (int i = 0; i < dataList.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var row = dataList[i];
                    var rowResult = CalculateRowHeightInternal(row, i, options);
                    results.Add(rowResult);

                    // Report progress if available
                    if (options.Progress != null)
                    {
                        var progress = CoreTypes.BatchCalculationProgress.Create(i + 1, dataList.Count, stopwatch.Elapsed);
                        options.Progress.Report(progress);
                    }
                }

                stopwatch.Stop();
                return CoreTypes.AutoRowHeightResult.CreateSuccess(results, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return CoreTypes.AutoRowHeightResult.Failure($"Failed to calculate row heights: {ex.Message}");
            }
        }, cancellationToken);
    }

    public async Task<CoreTypes.RowHeightCalculationResult> CalculateRowHeightAsync(
        IReadOnlyDictionary<string, object?> rowData,
        int rowIndex,
        CoreTypes.AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var options = new CoreTypes.RowHeightCalculationOptions
            {
                MaximumRowHeight = configuration.MaximumRowHeight,
                MinimumRowHeight = configuration.MinimumRowHeight,
                UseCache = configuration.EnableMeasurementCache
            };

            return CalculateRowHeightInternal(rowData, rowIndex, options);
        }, cancellationToken);
    }

    public async Task<CoreTypes.TextMeasurementResult> MeasureTextAsync(
        string text,
        double maxWidth,
        CoreTypes.AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var cacheKey = $"{text}|{maxWidth}|{configuration.FontSize}|{configuration.FontFamily}";

            if (configuration.EnableMeasurementCache && _measurementCache.TryGetValue(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            var result = MeasureTextInternal(text, maxWidth, configuration);

            if (configuration.EnableMeasurementCache)
            {
                _measurementCache.TryAdd(cacheKey, result);
            }

            return result;
        }, cancellationToken);
    }

    public async Task ApplyConfigurationAsync(
        CoreTypes.AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _currentConfiguration = configuration;
            _isEnabled = configuration.IsEnabled;

            if (!configuration.EnableMeasurementCache)
            {
                _measurementCache.Clear();
            }
        }, cancellationToken);
    }

    public CoreTypes.AutoRowHeightConfiguration GetCurrentConfiguration()
    {
        return _currentConfiguration;
    }

    public bool IsAutoRowHeightEnabled()
    {
        return _isEnabled;
    }

    public async Task InvalidateHeightCacheAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() => _measurementCache.Clear(), cancellationToken);
    }

    private CoreTypes.RowHeightCalculationResult CalculateRowHeightInternal(
        IReadOnlyDictionary<string, object?> rowData,
        int rowIndex,
        CoreTypes.RowHeightCalculationOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var columnMeasurements = new Dictionary<string, CoreTypes.TextMeasurementResult>();

        double maxHeight = options.MinimumRowHeight;

        var columnsToMeasure = options.SpecificColumns?.ToList() ?? rowData.Keys.ToList();

        foreach (var columnName in columnsToMeasure)
        {
            if (!rowData.TryGetValue(columnName, out var value) || value == null)
                continue;

            var text = value.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(text))
                continue;

            // Assume a default column width for measurement
            var columnWidth = 200.0; // Would get from actual column configuration
            var measurement = MeasureTextInternal(text, columnWidth, _currentConfiguration);

            columnMeasurements[columnName] = measurement;

            // Update max height considering padding
            var cellHeight = measurement.Height + _currentConfiguration.CellPadding.Top + _currentConfiguration.CellPadding.Bottom;
            maxHeight = Math.Max(maxHeight, cellHeight);
        }

        // Apply height constraints
        maxHeight = Math.Max(options.MinimumRowHeight, Math.Min(maxHeight, options.MaximumRowHeight));

        stopwatch.Stop();

        return CoreTypes.RowHeightCalculationResult.Create(
            rowIndex,
            maxHeight,
            columnMeasurements,
            stopwatch.Elapsed,
            fromCache: false);
    }

    private CoreTypes.TextMeasurementResult MeasureTextInternal(
        string text,
        double maxWidth,
        CoreTypes.AutoRowHeightConfiguration configuration)
    {
        if (string.IsNullOrEmpty(text))
        {
            return CoreTypes.TextMeasurementResult.Create(0, configuration.MinimumRowHeight, 1);
        }

        // Simple text measurement simulation
        // In a real implementation, would use actual text rendering measurement
        var charactersPerLine = Math.Max(1, (int)(maxWidth / (configuration.FontSize * 0.6))); // Rough estimation
        var lines = Math.Max(1, (int)Math.Ceiling((double)text.Length / charactersPerLine));

        if (configuration.TextWrapping == Microsoft.UI.Xaml.TextWrapping.NoWrap)
        {
            lines = 1;
        }

        var lineHeight = configuration.FontSize * configuration.LineHeight;
        var totalHeight = lines * lineHeight;

        var isTruncated = false;
        var truncatedText = text;

        // Apply maximum height constraint
        if (totalHeight > configuration.MaximumRowHeight)
        {
            var maxLines = Math.Max(1, (int)(configuration.MaximumRowHeight / lineHeight));
            lines = maxLines;
            totalHeight = maxLines * lineHeight;

            if (configuration.EnableTextTrimming)
            {
                isTruncated = true;
                var maxChars = maxLines * charactersPerLine - 3; // Leave space for ellipsis
                if (text.Length > maxChars)
                {
                    truncatedText = text.Substring(0, Math.Max(0, maxChars)) + "...";
                }
            }
        }

        return CoreTypes.TextMeasurementResult.Create(
            Math.Min(maxWidth, text.Length * (configuration.FontSize * 0.6)),
            totalHeight,
            lines,
            isTruncated,
            isTruncated ? truncatedText : null);
    }
}
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// CORE VALUE OBJECT: Auto row height configuration
/// </summary>
internal sealed record AutoRowHeightConfiguration
{
    /// <summary>Enable auto row height for multiline text</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Minimum row height in pixels</summary>
    public double MinimumRowHeight { get; init; } = 32;

    /// <summary>Maximum row height in pixels (0 = unlimited)</summary>
    public double MaximumRowHeight { get; init; } = 200;

    /// <summary>Padding inside cells for text layout</summary>
    public Thickness CellPadding { get; init; } = new(8, 4, 8, 4);

    /// <summary>Text wrapping mode for multiline text</summary>
    public TextWrapping TextWrapping { get; init; } = TextWrapping.Wrap;

    /// <summary>Enable text trimming when content exceeds max height</summary>
    public bool EnableTextTrimming { get; init; } = true;

    /// <summary>Text trimming mode</summary>
    public TextTrimming TextTrimming { get; init; } = TextTrimming.WordEllipsis;

    /// <summary>Font size for text measurement</summary>
    public double FontSize { get; init; } = 14;

    /// <summary>Font family for text measurement</summary>
    public string FontFamily { get; init; } = "Segoe UI";

    /// <summary>Line height multiplier (1.0 = normal, 1.2 = 120% spacing)</summary>
    public double LineHeight { get; init; } = 1.2;

    /// <summary>Debounce interval for height recalculation during typing</summary>
    public TimeSpan RecalculationDebounce { get; init; } = TimeSpan.FromMilliseconds(300);

    /// <summary>Enable performance optimization for large datasets</summary>
    public bool UseVirtualizedMeasurement { get; init; } = true;

    /// <summary>Cache measurement results for better performance</summary>
    public bool EnableMeasurementCache { get; init; } = true;

    /// <summary>Create default configuration</summary>
    public static AutoRowHeightConfiguration Default => new();

    /// <summary>Create compact configuration with smaller heights</summary>
    public static AutoRowHeightConfiguration Compact => new()
    {
        MinimumRowHeight = 24,
        MaximumRowHeight = 120,
        CellPadding = new(6, 2, 6, 2),
        FontSize = 12,
        LineHeight = 1.1
    };

    /// <summary>Create spacious configuration with larger heights</summary>
    public static AutoRowHeightConfiguration Spacious => new()
    {
        MinimumRowHeight = 40,
        MaximumRowHeight = 300,
        CellPadding = new(12, 8, 12, 8),
        FontSize = 16,
        LineHeight = 1.4
    };
}

/// <summary>
/// CORE VALUE OBJECT: Text measurement result
/// </summary>
internal sealed record TextMeasurementResult
{
    public double Width { get; init; }
    public double Height { get; init; }
    public int LineCount { get; init; }
    public bool IsTruncated { get; init; }
    public string? TruncatedText { get; init; }

    public static TextMeasurementResult Create(double width, double height, int lineCount, bool isTruncated = false, string? truncatedText = null) =>
        new()
        {
            Width = width,
            Height = height,
            LineCount = lineCount,
            IsTruncated = isTruncated,
            TruncatedText = truncatedText
        };
}

/// <summary>
/// CORE VALUE OBJECT: Row height calculation result
/// </summary>
internal sealed record RowHeightCalculationResult
{
    public int RowIndex { get; init; }
    public double CalculatedHeight { get; init; }
    public double ActualHeight { get; init; }
    public IReadOnlyDictionary<string, TextMeasurementResult> ColumnMeasurements { get; init; } = new Dictionary<string, TextMeasurementResult>();
    public TimeSpan CalculationTime { get; init; }
    public bool FromCache { get; init; }

    public static RowHeightCalculationResult Create(int rowIndex, double height, IReadOnlyDictionary<string, TextMeasurementResult>? columnMeasurements = null, TimeSpan calculationTime = default, bool fromCache = false) =>
        new()
        {
            RowIndex = rowIndex,
            CalculatedHeight = height,
            ActualHeight = height,
            ColumnMeasurements = columnMeasurements ?? new Dictionary<string, TextMeasurementResult>(),
            CalculationTime = calculationTime,
            FromCache = fromCache
        };
}

/// <summary>
/// CORE VALUE OBJECT: Progress reporting for batch row height calculations
/// </summary>
internal sealed record BatchCalculationProgress
{
    public int ProcessedRows { get; init; }
    public int TotalRows { get; init; }
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    public TimeSpan ElapsedTime { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    public static BatchCalculationProgress Create(int processed, int total, TimeSpan elapsed) =>
        new()
        {
            ProcessedRows = processed,
            TotalRows = total,
            ElapsedTime = elapsed
        };
}

/// <summary>
/// CORE VALUE OBJECT: Row height calculation options
/// </summary>
internal sealed record RowHeightCalculationOptions
{
    public double MaximumRowHeight { get; init; } = 200;
    public double MinimumRowHeight { get; init; } = 32;
    public bool UseCache { get; init; } = true;
    public bool MeasureAllColumns { get; init; } = true;
    public IReadOnlyList<string>? SpecificColumns { get; init; }
    public IProgress<BatchCalculationProgress>? Progress { get; init; }

    public static RowHeightCalculationOptions Default => new();
}

/// <summary>
/// CORE VALUE OBJECT: Auto row height result
/// </summary>
internal sealed record AutoRowHeightResult
{
    public bool Success { get; init; }
    public IReadOnlyList<RowHeightCalculationResult> CalculatedHeights { get; init; } = Array.Empty<RowHeightCalculationResult>();
    public TimeSpan TotalCalculationTime { get; init; }
    public int ProcessedRows { get; init; }
    public string? ErrorMessage { get; init; }
    public BatchCalculationProgress? Progress { get; init; }

    public static AutoRowHeightResult CreateSuccess(
        IReadOnlyList<RowHeightCalculationResult> heights,
        TimeSpan calculationTime) =>
        new()
        {
            Success = true,
            CalculatedHeights = heights,
            TotalCalculationTime = calculationTime,
            ProcessedRows = heights.Count
        };

    public static AutoRowHeightResult Failure(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
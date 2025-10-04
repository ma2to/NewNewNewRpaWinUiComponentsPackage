# KOMPLETNÁ ŠPECIFIKÁCIA: AUTOMATIC ROW HEIGHT MANAGEMENT SYSTÉM PRE ADVANCEDWINUIDATAGRID

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY & ŠTRUKTÚRA

### Clean Architecture + Algorithm Pattern (Jednotná s ostatnými časťami)
- **Facade Layer**: `IAdvancedDataGridFacade` (public API) - **ŽIADNE** priame AutoRowHeight metódy
- **Configuration Layer**: `AdvancedDataGridOptions.AutoRowHeightMode` (public configuration ONLY)
- **Application Layer**: Height calculation services, measurement management (**INTERNAL ONLY**)
- **Core Layer**: Text measurement algorithms, pure functions (**INTERNAL ONLY**)
- **Infrastructure Layer**: Performance monitoring, caching systems (**INTERNAL ONLY**)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### ⚠️ KRITICKÉ: PUBLIC API POLICY
- **AutoRowHeightService je INTERNAL** - používateľ nemá priamy prístup
- **Žiadne public metódy** pre AutoRowHeight v Facade
- **Konfigurácia ĽIBLEN cez Options** - `AutoRowHeightMode` enum (Disabled, Enabled, Auto)
- **Facade automaticky volá AutoRowHeightService** interným spôsobom podľa konfigurácie
- Používateľ **NEKONTROLUJE** row height manuálne, len nastaví režim v Options

### SOLID Principles
- **Single Responsibility**: Každý measurement service má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové typy measurement strategies bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky measurement services implementujú spoločné interface
- **Interface Segregation**: Špecializované interfaces pre rôzne typy measurement operations
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained (Jednotné s ostatnými časťami)
- **Clean Architecture**: Services v Application layer, pure algorithms v Core layer
- **Hybrid DI**: Service factory methods s dependency injection support
- **Functional/OOP**: Pure measurement functions + encapsulated service behavior
- **SOLID**: Single responsibility pre každý measurement operation type
- **LINQ Optimization**: Lazy evaluation, parallel processing pre batch calculations
- **Performance**: Measurement caching, virtualized calculations, smart algorithms
- **Thread Safety**: Immutable configurations, atomic cache operations
- **Internal DI Registration**: Všetky auto height časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť `.oldbackup_timestamp` súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s algorithm pattern a pure function design
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## 📋 CORE SERVICE ARCHITECTURE & INTERFACES

### 1. **IAutoRowHeightService.cs** - Application Interface

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// APPLICATION INTERFACE: Automatic row height management service
/// ENTERPRISE: Professional text measurement with performance optimization
/// VIRTUALIZATION: Support for virtualized measurement in large datasets
/// </summary>
internal interface IAutoRowHeightService
{
    #region Configuration Management

    /// <summary>
    /// CORE: Enable/configure automatic row height calculation
    /// CONFIGURATION: Apply measurement settings with validation
    /// </summary>
    Task<AutoRowHeightResult> EnableAutoRowHeightAsync(
        AutoRowHeightConfiguration configuration,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// UTILITY: Apply new configuration to existing setup
    /// DYNAMIC: Runtime configuration updates with cache invalidation
    /// </summary>
    Task<AutoRowHeightResult> ApplyConfigurationAsync(
        AutoRowHeightConfiguration configuration,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// MAINTENANCE: Invalidate measurement cache
    /// PERFORMANCE: Force recalculation for updated content
    /// </summary>
    Task<bool> InvalidateHeightCacheAsync(cancellationToken cancellationToken = default);

    #endregion

    #region Height Calculations

    /// <summary>
    /// PERFORMANCE: Calculate optimal row heights for all rows
    /// BATCH: Efficient bulk height calculations with progress reporting
    /// </summary>
    Task<IReadOnlyList<RowHeightCalculationResult>> CalculateOptimalRowHeightsAsync(
        IProgress<BatchCalculationProgress>? progress = null,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// SINGLE: Calculate height for specific row
    /// PRECISION: Accurate height calculation with text wrapping support
    /// </summary>
    Task<RowHeightCalculationResult> CalculateRowHeightAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        RowHeightCalculationOptions? options = null,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// CORE: Measure text dimensions for height calculation
    /// TEXT: Advanced text measurement with font and wrapping support
    /// </summary>
    Task<TextMeasurementResult> MeasureTextAsync(
        string text,
        string fontFamily,
        double fontSize,
        double maxWidth,
        bool textWrapping = true,
        cancellationToken cancellationToken = default);

    #endregion

    #region Statistics & Monitoring

    /// <summary>
    /// MONITORING: Get current auto height statistics
    /// PERFORMANCE: Measurement performance and cache metrics
    /// </summary>
    AutoRowHeightStatistics GetStatistics();

    /// <summary>
    /// DIAGNOSTICS: Get measurement cache information
    /// CACHE: Cache hit rates and memory usage analysis
    /// </summary>
    CacheStatistics GetCacheStatistics();

    #endregion
}
```

### 2. **AutoRowHeightService.cs** - Application Service Implementation

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Utilities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// APPLICATION SERVICE: Automatic row height management with enterprise features
/// PERFORMANCE: Optimized text measurement with intelligent caching
/// VIRTUALIZATION: Support for large datasets with minimal memory footprint
/// </summary>
internal sealed class AutoRowHeightService : IAutoRowHeightService
{
    private readonly ILogger<AutoRowHeightService> _logger;
    private readonly IOperationLogger<AutoRowHeightService> _operationLogger;
    private readonly ICommandLogger<AutoRowHeightService> _commandLogger;

    private readonly ConcurrentDictionary<string, TextMeasurementResult> _measurementCache = new();
    private readonly SemaphoreSlim _calculationSemaphore = new(Environment.ProcessorCount, Environment.ProcessorCount);

    private AutoRowHeightConfiguration _currentConfiguration = AutoRowHeightConfiguration.Default;
    private volatile bool _isEnabled = false;

    public AutoRowHeightService(
        ILogger<AutoRowHeightService> logger,
        IOperationLogger<AutoRowHeightService> operationLogger,
        ICommandLogger<AutoRowHeightService> commandLogger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationLogger = operationLogger ?? throw new ArgumentNullException(nameof(operationLogger));
        _commandLogger = commandLogger ?? throw new ArgumentNullException(nameof(commandLogger));
    }

    #region Configuration Management

    public async Task<AutoRowHeightResult> EnableAutoRowHeightAsync(
        AutoRowHeightConfiguration configuration,
        cancellationToken cancellationToken = default)
    {
        using var scope = _operationLogger.LogOperationStart("EnableAutoRowHeight",
            new { configuration.IsEnabled, configuration.MinimumRowHeight, configuration.MaximumRowHeight });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // VALIDATION: Comprehensive configuration validation
            var validationResult = ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                return AutoRowHeightResult.CreateFailure(validationResult.ErrorMessages, stopwatch.Elapsed);
            }

            _currentConfiguration = configuration;
            _isEnabled = configuration.IsEnabled;

            // PERFORMANCE: Clear cache when configuration changes
            if (configuration.EnableMeasurementCache)
            {
                _measurementCache.Clear();
                _logger.LogInformation("Measurement cache cleared due to configuration change");
            }

            stopwatch.Stop();

            var result = AutoRowHeightResult.CreateSuccess(
                isEnabled: _isEnabled,
                configuration: _currentConfiguration,
                operationTime: stopwatch.Elapsed);

            _logger.LogInformation("Auto row height enabled: isEnabled={IsEnabled}, minHeight={MinHeight}, maxHeight={MaxHeight}",
                _isEnabled, configuration.MinimumRowHeight, configuration.MaximumRowHeight);

            scope.MarkSuccess(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable auto row height");
            scope.MarkFailure(ex);

            return AutoRowHeightResult.CreateFailure(new[] { ex.Message }, stopwatch.Elapsed);
        }
    }

    #endregion

    #region Height Calculations

    public async Task<IReadOnlyList<RowHeightCalculationResult>> CalculateOptimalRowHeightsAsync(
        IProgress<BatchCalculationProgress>? progress = null,
        cancellationToken cancellationToken = default)
    {
        using var scope = _operationLogger.LogOperationStart("CalculateOptimalRowHeights", new { });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<RowHeightCalculationResult>();

        try
        {
            if (!_isEnabled)
            {
                _logger.LogWarning("Auto row height calculation attempted but feature is disabled");
                return Array.Empty<RowHeightCalculationResult>();
            }

            // PERFORMANCE: Get rows for measurement (this would be injected data source)
            var rowsToMeasure = await GetRowsForMeasurementAsync(cancellationToken);
            var totalRows = rowsToMeasure.Count;

            _logger.LogInformation("Starting batch row height calculation for {RowCount} rows", totalRows);

            // PERFORMANCE: Parallel processing with semaphore control
            var calculationTasks = rowsToMeasure.Select(async (rowData, index) =>
            {
                await _calculationSemaphore.WaitAsync(cancellationToken);
                try
                {
                    var result = await CalculateRowHeightAsync(index, rowData, null, cancellationToken);

                    // PROGRESS REPORTING
                    progress?.Report(new BatchCalculationProgress
                    {
                        ProcessedRows = index + 1,
                        TotalRows = totalRows,
                        CurrentRowIndex = index,
                        ElapsedTime = stopwatch.Elapsed
                    });

                    return result;
                }
                finally
                {
                    _calculationSemaphore.Release();
                }
            });

            results.AddRange(await Task.WhenAll(calculationTasks));
            stopwatch.Stop();

            _logger.LogInformation("Batch row height calculation completed: processedRows={ProcessedRows}, time={Duration}ms",
                results.Count, stopwatch.ElapsedMilliseconds);

            scope.MarkSuccess(results);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate optimal row heights");
            scope.MarkFailure(ex);
            return Array.Empty<RowHeightCalculationResult>();
        }
    }

    public async Task<RowHeightCalculationResult> CalculateRowHeightAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        RowHeightCalculationOptions? options = null,
        cancellationToken cancellationToken = default)
    {
        var calculationStopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var effectiveOptions = options ?? RowHeightCalculationOptions.Default;
            var maxHeightForRow = 0.0;

            // PERFORMANCE: Process each cell in parallel for multi-column height calculation
            var cellHeightTasks = rowData.Select(async kvp =>
            {
                var columnName = kvp.Key;
                var cellValue = kvp.Value?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(cellValue))
                    return _currentConfiguration.MinimumRowHeight;

                // CACHING: Check measurement cache first
                var cacheKey = GenerateCacheKey(cellValue, _currentConfiguration);
                if (_currentConfiguration.EnableMeasurementCache && _measurementCache.TryGetValue(cacheKey, out var cachedResult))
                {
                    return cachedResult.Height;
                }

                // CORE ALGORITHM: Use pure function for text measurement
                var measurementConfig = new TextMeasurementConfiguration
                {
                    FontFamily = _currentConfiguration.FontFamily,
                    FontSize = _currentConfiguration.FontSize,
                    LineHeight = _currentConfiguration.LineHeight,
                    TextWrapping = _currentConfiguration.TextWrapping,
                    EnableTextTrimming = _currentConfiguration.EnableTextTrimming,
                    TextTrimming = _currentConfiguration.TextTrimming
                };

                var measurement = TextMeasurementAlgorithms.MeasureText(
                    cellValue,
                    effectiveOptions.AvailableWidth,
                    measurementConfig);

                // CACHING: Store result for future use
                if (_currentConfiguration.EnableMeasurementCache)
                {
                    _measurementCache.TryAdd(cacheKey, measurement);
                }

                return measurement.Height + _currentConfiguration.CellPadding.Top + _currentConfiguration.CellPadding.Bottom;
            });

            var cellHeights = await Task.WhenAll(cellHeightTasks);
            maxHeightForRow = cellHeights.Max();

            // CONSTRAINTS: Apply min/max height limits
            var finalHeight = Math.Max(_currentConfiguration.MinimumRowHeight,
                Math.Min(_currentConfiguration.MaximumRowHeight, maxHeightForRow));

            calculationStopwatch.Stop();

            return new RowHeightCalculationResult
            {
                RowIndex = rowIndex,
                CalculatedHeight = finalHeight,
                MeasuredCells = cellHeights.Length,
                CalculationTime = calculationStopwatch.Elapsed,
                WasFromCache = false,
                Configuration = _currentConfiguration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate row height for rowIndex={RowIndex}", rowIndex);
            calculationStopwatch.Stop();

            return new RowHeightCalculationResult
            {
                RowIndex = rowIndex,
                CalculatedHeight = _currentConfiguration.MinimumRowHeight,
                MeasuredCells = 0,
                CalculationTime = calculationStopwatch.Elapsed,
                WasFromCache = false,
                Configuration = _currentConfiguration,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<TextMeasurementResult> MeasureTextAsync(
        string text,
        string fontFamily,
        double fontSize,
        double maxWidth,
        bool textWrapping = true,
        cancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        // PURE FUNCTION: Delegate to core algorithm
        var config = new TextMeasurementConfiguration
        {
            FontFamily = fontFamily,
            FontSize = fontSize,
            TextWrapping = textWrapping
        };

        return TextMeasurementAlgorithms.MeasureText(text, maxWidth, config);
    }

    #endregion

    #region Private Helper Methods

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetRowsForMeasurementAsync(
        cancellationToken cancellationToken)
    {
        // Implementation would get actual row data from data source
        await Task.CompletedTask;
        return Array.Empty<IReadOnlyDictionary<string, object?>>();
    }

    private string GenerateCacheKey(string text, AutoRowHeightConfiguration config)
    {
        return $"{text.GetHashCode()}_{config.FontFamily}_{config.FontSize}_{config.TextWrapping}";
    }

    private ValidationResult ValidateConfiguration(AutoRowHeightConfiguration configuration)
    {
        var errors = new List<string>();

        if (configuration.MinimumRowHeight <= 0)
            errors.Add("Minimum row height must be greater than 0");

        if (configuration.MaximumRowHeight <= configuration.MinimumRowHeight)
            errors.Add("Maximum row height must be greater than minimum row height");

        if (configuration.FontSize <= 0)
            errors.Add("Font size must be greater than 0");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            ErrorMessages = errors
        };
    }

    #endregion
}
```

## 🎯 CORE ALGORITHMS & VALUE OBJECTS

### 1. **TextMeasurementAlgorithms.cs** - Pure Function Algorithms

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Utilities;

/// <summary>
/// PURE FUNCTIONS: Text measurement algorithms for row height calculation
/// IMMUTABLE: Thread-safe algorithms with no side effects
/// PERFORMANCE: Optimized calculations with intelligent text processing
/// </summary>
internal static class TextMeasurementAlgorithms
{
    #region Core Measurement Functions

    /// <summary>
    /// CORE: Measure text dimensions with wrapping support
    /// PURE FUNCTION: No side effects, deterministic results
    /// </summary>
    public static TextMeasurementResult MeasureText(
        string text,
        double maxWidth,
        TextMeasurementConfiguration config)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new TextMeasurementResult
            {
                Width = 0,
                Height = config.LineHeight,
                LineCount = 1,
                IsTruncated = false,
                TruncatedText = string.Empty
            };
        }

        // PERFORMANCE: Calculate character metrics once
        var avgCharWidth = CalculateAverageCharacterWidth(config.FontFamily, config.FontSize);
        var lineHeight = CalculateLineHeight(config.FontSize, config.LineHeight);

        if (!config.TextWrapping || maxWidth <= 0)
        {
            // Single line measurement
            var width = text.Length * avgCharWidth;
            var truncated = ApplyTextTrimming(text, maxWidth, avgCharWidth, config);

            return new TextMeasurementResult
            {
                Width = Math.Min(width, maxWidth),
                Height = lineHeight,
                LineCount = 1,
                IsTruncated = truncated.IsTruncated,
                TruncatedText = truncated.TruncatedText
            };
        }

        // Multi-line measurement with word wrapping
        var lineCount = CalculateLineCount(text, maxWidth, avgCharWidth, config);
        var totalHeight = lineCount * lineHeight;

        return new TextMeasurementResult
        {
            Width = maxWidth,
            Height = totalHeight,
            LineCount = lineCount,
            IsTruncated = false,
            TruncatedText = text
        };
    }

    /// <summary>
    /// PERFORMANCE: Calculate optimal row height with padding
    /// CONSTRAINTS: Apply min/max height constraints
    /// </summary>
    public static double CalculateOptimalRowHeight(
        TextMeasurementResult measurement,
        AutoRowHeightConfiguration config)
    {
        var heightWithPadding = measurement.Height +
                               config.CellPadding.Top +
                               config.CellPadding.Bottom;

        return Math.Max(config.MinimumRowHeight,
            Math.Min(config.MaximumRowHeight, heightWithPadding));
    }

    #endregion

    #region Supporting Algorithms

    /// <summary>
    /// PERFORMANCE: Calculate line count with word wrapping
    /// INTELLIGENT: Word boundary analysis for accurate wrapping
    /// </summary>
    public static int CalculateLineCount(
        string text,
        double maxWidth,
        double averageCharWidth,
        TextMeasurementConfiguration config)
    {
        if (maxWidth <= 0 || !config.TextWrapping)
            return 1;

        var charactersPerLine = (int)(maxWidth / averageCharWidth);
        if (charactersPerLine <= 0)
            return 1;

        // INTELLIGENT: Word boundary analysis
        var wordBoundaries = AnalyzeWordBoundaries(text);
        var lines = 1;
        var currentLineLength = 0;

        foreach (var word in wordBoundaries.Words)
        {
            if (currentLineLength + word.Length > charactersPerLine && currentLineLength > 0)
            {
                lines++;
                currentLineLength = word.Length;
            }
            else
            {
                currentLineLength += word.Length;
            }
        }

        return lines;
    }

    /// <summary>
    /// PERFORMANCE: Calculate characters that fit per line
    /// PRECISE: Account for font metrics and spacing
    /// </summary>
    public static int CalculateCharactersPerLine(double availableWidth, double averageCharWidth)
    {
        if (availableWidth <= 0 || averageCharWidth <= 0)
            return 0;

        return (int)(availableWidth / averageCharWidth);
    }

    /// <summary>
    /// FONT METRICS: Calculate average character width for font
    /// APPROXIMATION: Fast calculation using typical character distribution
    /// </summary>
    public static double CalculateAverageCharacterWidth(string fontFamily, double fontSize)
    {
        // APPROXIMATION: Typical character width as percentage of font size
        // This would ideally use actual font metrics from the rendering system
        var baseWidth = fontSize * 0.6; // Approximate ratio for most fonts

        // FONT-SPECIFIC: Adjust for known font families
        var fontMultiplier = fontFamily.ToLowerInvariant() switch
        {
            "consolas" or "courier new" or "monaco" => 1.0, // Monospace fonts
            "arial" or "helvetica" => 0.9,
            "times new roman" or "georgia" => 0.85,
            _ => 0.9 // Default multiplier
        };

        return baseWidth * fontMultiplier;
    }

    /// <summary>
    /// LINE METRICS: Calculate line height from font size and line height ratio
    /// TYPOGRAPHY: Proper line spacing calculation
    /// </summary>
    public static double CalculateLineHeight(double fontSize, double lineHeightRatio)
    {
        return fontSize * Math.Max(1.0, lineHeightRatio);
    }

    /// <summary>
    /// TEXT PROCESSING: Apply text trimming for overflow handling
    /// ELLIPSIS: Smart truncation with ellipsis placement
    /// </summary>
    public static (bool IsTruncated, string TruncatedText) ApplyTextTrimming(
        string text,
        double maxWidth,
        double averageCharWidth,
        TextMeasurementConfiguration config)
    {
        if (!config.EnableTextTrimming || maxWidth <= 0)
            return (false, text);

        var maxCharacters = CalculateCharactersPerLine(maxWidth, averageCharWidth);
        if (text.Length <= maxCharacters)
            return (false, text);

        // SMART TRIMMING: Apply different trimming strategies
        var truncatedText = config.TextTrimming switch
        {
            TextTrimming.CharacterEllipsis => text.Substring(0, Math.Max(0, maxCharacters - 3)) + "...",
            TextTrimming.WordEllipsis => TrimAtWordBoundary(text, maxCharacters - 3) + "...",
            _ => text.Substring(0, maxCharacters)
        };

        return (true, truncatedText);
    }

    /// <summary>
    /// WORD PROCESSING: Analyze word boundaries for intelligent wrapping
    /// LINGUISTICS: Handle various word separators and punctuation
    /// </summary>
    public static WordBoundaryInfo AnalyzeWordBoundaries(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new WordBoundaryInfo { Words = Array.Empty<string>() };

        // TOKENIZATION: Split on word boundaries while preserving separators
        var words = text
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToArray();

        return new WordBoundaryInfo
        {
            Words = words,
            AverageWordLength = words.Length > 0 ? words.Average(w => w.Length) : 0,
            LongestWord = words.Length > 0 ? words.Max(w => w.Length) : 0
        };
    }

    #endregion

    #region Private Helper Methods

    private static string TrimAtWordBoundary(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        var lastSpace = text.LastIndexOf(' ', Math.Min(maxLength, text.Length - 1));
        return lastSpace > 0 ? text.Substring(0, lastSpace) : text.Substring(0, maxLength);
    }

    #endregion
}

/// <summary>
/// ENUM: Text trimming strategies for overflow handling
/// </summary>
internal enum TextTrimming
{
    None,
    CharacterEllipsis,
    WordEllipsis
}
```

### 2. **AutoRowHeightTypes.cs** - Configuration & Result Types

```csharp
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// CORE CONFIGURATION: Auto row height configuration with comprehensive options
/// IMMUTABLE: Thread-safe configuration with factory methods
/// </summary>
internal sealed record AutoRowHeightConfiguration
{
    // Core Settings
    public bool IsEnabled { get; init; } = true;
    public double MinimumRowHeight { get; init; } = 25.0;
    public double MaximumRowHeight { get; init; } = 200.0;
    public Thickness CellPadding { get; init; } = new(8, 4, 8, 4);

    // Text Settings
    public bool TextWrapping { get; init; } = true;
    public bool EnableTextTrimming { get; init; } = false;
    public TextTrimming TextTrimming { get; init; } = TextTrimming.CharacterEllipsis;
    public double FontSize { get; init; } = 14.0;
    public string FontFamily { get; init; } = "Segoe UI";
    public double LineHeight { get; init; } = 1.2;

    // Performance Settings
    public TimeSpan RecalculationDebounce { get; init; } = TimeSpan.FromMilliseconds(300);
    public bool UseVirtualizedMeasurement { get; init; } = true;
    public bool EnableMeasurementCache { get; init; } = true;

    // Factory Methods - Presets
    public static AutoRowHeightConfiguration Default => new();

    public static AutoRowHeightConfiguration Compact => new()
    {
        MinimumRowHeight = 20.0,
        MaximumRowHeight = 100.0,
        CellPadding = new(4, 2, 4, 2),
        FontSize = 12.0,
        LineHeight = 1.1
    };

    public static AutoRowHeightConfiguration Spacious => new()
    {
        MinimumRowHeight = 35.0,
        MaximumRowHeight = 300.0,
        CellPadding = new(12, 8, 12, 8),
        FontSize = 16.0,
        LineHeight = 1.4
    };

    // Factory Methods - Custom
    public static AutoRowHeightConfiguration Create(
        bool isEnabled = true,
        double minHeight = 25.0,
        double maxHeight = 200.0) => new()
        {
            IsEnabled = isEnabled,
            MinimumRowHeight = minHeight,
            MaximumRowHeight = maxHeight
        };

    public static AutoRowHeightConfiguration WithFont(
        string fontFamily,
        double fontSize,
        double lineHeight = 1.2) => new()
        {
            FontFamily = fontFamily,
            FontSize = fontSize,
            LineHeight = lineHeight
        };
}

/// <summary>
/// RESULT TYPE: Text measurement result with comprehensive metrics
/// </summary>
internal sealed record TextMeasurementResult
{
    public double Width { get; init; }
    public double Height { get; init; }
    public int LineCount { get; init; }
    public bool IsTruncated { get; init; }
    public string TruncatedText { get; init; } = string.Empty;
}

/// <summary>
/// RESULT TYPE: Row height calculation result with timing and context
/// </summary>
internal sealed record RowHeightCalculationResult
{
    public int RowIndex { get; init; }
    public double CalculatedHeight { get; init; }
    public int MeasuredCells { get; init; }
    public TimeSpan CalculationTime { get; init; }
    public bool WasFromCache { get; init; }
    public AutoRowHeightConfiguration Configuration { get; init; } = AutoRowHeightConfiguration.Default;
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// PROGRESS TYPE: Batch calculation progress reporting
/// </summary>
internal sealed record BatchCalculationProgress
{
    public int ProcessedRows { get; init; }
    public int TotalRows { get; init; }
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    public int CurrentRowIndex { get; init; }
    public TimeSpan ElapsedTime { get; init; }

    public TimeSpan? EstimatedTimeRemaining => ProcessedRows > 0 && TotalRows > ProcessedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ProcessedRows) / ProcessedRows)
        : null;
}

/// <summary>
/// OPTIONS TYPE: Row height calculation options
/// </summary>
internal sealed record RowHeightCalculationOptions
{
    public double AvailableWidth { get; init; } = 200.0;
    public bool ForceRecalculation { get; init; } = false;
    public bool UseCache { get; init; } = true;

    public static RowHeightCalculationOptions Default => new();

    public static RowHeightCalculationOptions WithWidth(double availableWidth) =>
        new() { AvailableWidth = availableWidth };
}

/// <summary>
/// RESULT TYPE: Auto row height operation result
/// </summary>
internal sealed record AutoRowHeightResult
{
    public bool Success { get; init; }
    public bool IsEnabled { get; init; }
    public AutoRowHeightConfiguration? Configuration { get; init; }
    public TimeSpan OperationTime { get; init; }
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();

    public static AutoRowHeightResult CreateSuccess(
        bool isEnabled,
        AutoRowHeightConfiguration configuration,
        TimeSpan operationTime) => new()
        {
            Success = true,
            IsEnabled = isEnabled,
            Configuration = configuration,
            OperationTime = operationTime
        };

    public static AutoRowHeightResult CreateFailure(
        IReadOnlyList<string> messages,
        TimeSpan operationTime) => new()
        {
            Success = false,
            Messages = messages,
            OperationTime = operationTime
        };
}

/// <summary>
/// CONFIGURATION TYPE: Immutable text measurement configuration
/// PURE: Configuration for pure function algorithms
/// </summary>
internal sealed record TextMeasurementConfiguration
{
    public string FontFamily { get; init; } = "Segoe UI";
    public double FontSize { get; init; } = 14.0;
    public double LineHeight { get; init; } = 1.2;
    public bool TextWrapping { get; init; } = true;
    public bool EnableTextTrimming { get; init; } = false;
    public TextTrimming TextTrimming { get; init; } = TextTrimming.CharacterEllipsis;
}

/// <summary>
/// ANALYSIS TYPE: Word boundary analysis results
/// </summary>
internal sealed record WordBoundaryInfo
{
    public IReadOnlyList<string> Words { get; init; } = Array.Empty<string>();
    public double AverageWordLength { get; init; }
    public int LongestWord { get; init; }
}
```

## 🎯 PUBLIC API - CONFIGURATION ONLY

### ⚠️ ŽIADNE FACADE METÓDY PRE AUTO ROW HEIGHT

AutoRowHeight **NEMÁ ŽIADNE PUBLIC API METÓDY** v `IAdvancedDataGridFacade`.

Všetko je **INTERNAL** a riadené automaticky cez konfiguráciu.

### Public Configuration v AdvancedDataGridOptions

```csharp
/// <summary>
/// PUBLIC: Auto row height mode configuration
/// Controls automatic row height calculation behavior
/// </summary>
public enum AutoRowHeightMode
{
    /// <summary>
    /// Auto row height is disabled - use fixed row heights
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Auto row height is enabled for all rows
    /// </summary>
    Enabled = 1,

    /// <summary>
    /// Auto mode - enable only when needed (performance optimization)
    /// </summary>
    Auto = 2
}

/// <summary>
/// PUBLIC: AdvancedDataGridOptions - Auto Row Height Configuration
/// </summary>
public class AdvancedDataGridOptions
{
    /// <summary>
    /// Auto row height mode (Disabled, Enabled, Auto)
    /// DEFAULT: Disabled
    /// </summary>
    public AutoRowHeightMode AutoRowHeightMode { get; set; } = AutoRowHeightMode.Disabled;

    /// <summary>
    /// Minimum row height in pixels
    /// DEFAULT: 25.0
    /// </summary>
    public double MinimumRowHeight { get; set; } = 25.0;

    /// <summary>
    /// Maximum row height in pixels
    /// DEFAULT: 200.0
    /// </summary>
    public double MaximumRowHeight { get; set; } = 200.0;

    // ... ostatné options properties
}
```

### Použitie - Príklad

```csharp
// Používateľ nastaví ĽIBLEN konfiguráciu
var options = new AdvancedDataGridOptions
{
    AutoRowHeightMode = AutoRowHeightMode.Enabled,
    MinimumRowHeight = 30.0,
    MaximumRowHeight = 150.0
};

var facade = AdvancedDataGridFacadeFactory.CreateStandalone(options);

// AutoRowHeightService je automaticky volané INTERNE
// Používateľ NEKONTROLUJE row heights manuálne
```

### Internal Facade Integration

```csharp
// V AdvancedDataGridFacade (INTERNAL implementation)
internal class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    private readonly IAutoRowHeightService _autoRowHeightService; // INTERNAL
    private readonly AdvancedDataGridOptions _options;

    // INTERNAL: Automatické volanie pri importoch/zmenách
    private async Task AutoCalculateRowHeightsIfNeededAsync()
    {
        if (_options.AutoRowHeightMode == AutoRowHeightMode.Disabled)
            return;

        // INTERNAL: Volanie internal service
        await _autoRowHeightService.CalculateOptimalRowHeightsAsync();
    }
}
```

## ⚡ PERFORMANCE OPTIMIZATIONS & CACHING

### Measurement Performance Optimizations

```csharp
/// <summary>
/// PERFORMANCE: Advanced measurement caching system
/// ENTERPRISE: High-performance text measurement with intelligent cache management
/// </summary>
internal sealed class MeasurementCacheManager
{
    private readonly ConcurrentDictionary<string, TextMeasurementResult> _cache = new();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

    // PERFORMANCE: LRU cache implementation
    private readonly Dictionary<string, DateTime> _accessTimes = new();
    private readonly object _accessLock = new();

    public MeasurementCacheManager()
    {
        // MAINTENANCE: Periodic cache cleanup
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cacheExpiry, _cacheExpiry);
    }

    // PERFORMANCE: Smart cache key generation
    public string GenerateCacheKey(string text, TextMeasurementConfiguration config)
    {
        return $"{text.GetHashCode()}_{config.FontFamily}_{config.FontSize}_{config.TextWrapping}_{config.LineHeight}";
    }

    // ATOMIC: Thread-safe cache operations
    public bool TryGetCachedMeasurement(string cacheKey, out TextMeasurementResult? result)
    {
        lock (_accessLock)
        {
            if (_cache.TryGetValue(cacheKey, out result))
            {
                _accessTimes[cacheKey] = DateTime.UtcNow;
                return true;
            }
        }

        result = null;
        return false;
    }

    // PERFORMANCE: Efficient cache storage
    public void CacheMeasurement(string cacheKey, TextMeasurementResult result)
    {
        lock (_accessLock)
        {
            _cache.TryAdd(cacheKey, result);
            _accessTimes[cacheKey] = DateTime.UtcNow;
        }
    }

    // MAINTENANCE: Cleanup expired cache entries
    private void CleanupExpiredEntries(object? state)
    {
        lock (_accessLock)
        {
            var expiredKeys = _accessTimes
                .Where(kvp => DateTime.UtcNow - kvp.Value > _cacheExpiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
                _accessTimes.Remove(key);
            }
        }
    }
}

/// <summary>
/// VIRTUALIZATION: Virtualized measurement for large datasets
/// MEMORY: Minimal memory footprint for large grids
/// </summary>
internal sealed class VirtualizedMeasurementManager
{
    private const int MeasurementBatchSize = 100;

    // PERFORMANCE: Measure only visible rows
    public async Task<IReadOnlyList<RowHeightCalculationResult>> MeasureVisibleRowsAsync(
        int firstVisibleRow,
        int lastVisibleRow,
        IEnumerable<IReadOnlyDictionary<string, object?>> rowData,
        AutoRowHeightConfiguration config,
        cancellationToken cancellationToken = default)
    {
        var visibleRows = rowData
            .Skip(firstVisibleRow)
            .Take(lastVisibleRow - firstVisibleRow + 1)
            .Select((row, index) => new { Index = firstVisibleRow + index, Data = row });

        // BATCH PROCESSING: Process in smaller batches to avoid memory pressure
        var results = new List<RowHeightCalculationResult>();

        await foreach (var batch in visibleRows.Chunk(MeasurementBatchSize).ToAsyncEnumerable())
        {
            var batchTasks = batch.Select(async row =>
            {
                // Individual row measurement logic here
                await Task.CompletedTask;
                return new RowHeightCalculationResult
                {
                    RowIndex = row.Index,
                    CalculatedHeight = config.MinimumRowHeight // Simplified for example
                };
            });

            var batchResults = await Task.WhenAll(batchTasks);
            results.AddRange(batchResults);
        }

        return results;
    }
}
```

## **🔍 LOGGING ŠPECIFIKÁCIA PRE AUTO ROW HEIGHT**

### **Internal DI Registration & Service Distribution**
Všetky auto height logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`**:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<IAutoHeightLogger<AutoRowHeightService>, AutoHeightLogger<AutoRowHeightService>>();
services.AddSingleton<IOperationLogger<AutoRowHeightService>, OperationLogger<AutoRowHeightService>>();
services.AddSingleton<ICommandLogger<AutoRowHeightService>, CommandLogger<AutoRowHeightService>>();
```

### **Auto Height Operations Logging**

```csharp
// Configuration logging
_autoHeightLogger.LogConfigurationChange(oldConfig, newConfig, configurationTime);
_logger.LogInformation("Auto height configuration updated: minHeight={MinHeight}, maxHeight={MaxHeight}, time={Duration}ms",
    newConfig.MinimumRowHeight, newConfig.MaximumRowHeight, configurationTime.TotalMilliseconds);

// Measurement logging
_autoHeightLogger.LogMeasurementOperation(rowIndex, cellCount, calculationTime);
_logger.LogInformation("Row height calculated: rowIndex={RowIndex}, cellCount={CellCount}, height={Height}, time={Duration}ms",
    rowIndex, cellCount, calculatedHeight, calculationTime.TotalMilliseconds);

// Cache performance logging
_logger.LogInformation("Measurement cache performance: hitRate={HitRate:P2}, cacheSize={CacheSize}, cleanupCount={CleanupCount}",
    cacheHitRate, cacheSize, cleanupCount);

// Batch operation logging
_logger.LogInformation("Batch height calculation: processedRows={ProcessedRows}, totalTime={TotalTime}ms, avgTimePerRow={AvgTime}ms",
    processedRows, totalTime.TotalMilliseconds, totalTime.TotalMilliseconds / processedRows);
```

### **Performance Metrics Logging**

```csharp
// Text measurement performance
_logger.LogInformation("Text measurement performance: measurementsPerSecond={Rate}, cacheHitRate={HitRate:P2}",
    measurementsPerSecond, cacheHitRate);

// Virtualization metrics
_logger.LogInformation("Virtualized measurement: visibleRows={VisibleRows}, measuredRows={MeasuredRows}, efficiency={Efficiency:P2}",
    visibleRowCount, measuredRowCount, (double)measuredRowCount / visibleRowCount);

// Memory usage logging
if (memoryUsage > memoryThreshold)
{
    _logger.LogWarning("High memory usage in auto height: currentUsage={CurrentUsage}MB, threshold={Threshold}MB",
        memoryUsage / 1024 / 1024, memoryThreshold / 1024 / 1024);
}
```

### **Logging Levels Usage:**
- **Information**: Successful measurements, configuration changes, cache statistics, batch operations
- **Warning**: High memory usage, performance issues, cache evictions, measurement fallbacks
- **Error**: Measurement failures, configuration validation errors, cache failures
- **Critical**: System-level measurement failures, memory exhaustion, algorithm failures

Táto špecifikácia poskytuje kompletný, enterprise-ready automatic row height management systém s pokročilými text measurement algoritmi, performance optimalizáciami, intelligent caching a jednotnou architektúrou s ostatnými časťami komponentu.
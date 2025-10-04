# KOMPLETNÁ ŠPECIFIKÁCIA ENTERPRISE UI INFRASTRUCTURE

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY

### Clean Architecture + Command Pattern
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: UI services, interaction handlers (internal)
- **Core Layer**: UI domain entities, rendering algorithms (internal)
- **Infrastructure Layer**: WinUI integration, performance optimization (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý UI service má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové UI components bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky UI handlers implementujú spoločné interface
- **Interface Segregation**: Špecializované interfaces pre rôzne typy UI operations
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable UI commands + encapsulated behavior
- **SOLID**: Single responsibility pre každý UI component type
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable commands, atomic operation updates
- **Internal DI Registration**: Všetky UI časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť .oldbackup_timestamp súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a LINQ optimizations
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované a bezpečné a stabilné riešenie

## 🎨 CORE UI INFRASTRUCTURE COMPONENTS

### 1. **AutoRowHeightService** - Advanced Height Calculation

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// ENTERPRISE: Advanced row height calculation with performance optimization
/// ASYNC: Non-blocking height calculation for responsive UI
/// CACHING: Thread-safe measurement cache for performance
/// ALGORITHMS: Sophisticated text measurement and layout algorithms
/// </summary>
internal sealed class AutoRowHeightService
{
    private readonly ConcurrentDictionary<string, double> _measurementCache = new();
    private readonly SemaphoreSlim _calculationSemaphore = new(Environment.ProcessorCount);
    private volatile bool _cachingEnabled = true;

    /// <summary>
    /// ENTERPRISE: Calculate optimal row height with advanced text measurement
    /// PERFORMANCE: Cached results with intelligent cache key generation
    /// ASYNC: Non-blocking calculation with cancellation support
    /// </summary>
    public async Task<double> CalculateRowHeightAsync(
        IReadOnlyDictionary<string, object?> rowData,
        IReadOnlyList<ColumnDefinition> columns,
        AutoRowHeightConfiguration configuration,
        cancellationToken cancellationToken = default)
    {
        // Generate cache key based on content and configuration
        var cacheKey = GenerateCacheKey(rowData, columns, configuration);

        if (_cachingEnabled && _measurementCache.TryGetValue(cacheKey, out var cachedHeight))
        {
            return cachedHeight;
        }

        await _calculationSemaphore.WaitAsync(cancellationToken);
        try
        {
            using var scope = PerformanceCounters.CreateOperationScope("AutoRowHeight_Calculate");

            var maxHeight = await CalculateMaxCellHeightAsync(rowData, columns, configuration, cancellationToken);

            // Apply height constraints
            var constrainedHeight = Math.Max(configuration.MinimumRowHeight,
                                   Math.Min(configuration.MaximumRowHeight, maxHeight));

            // Cache the result
            if (_cachingEnabled)
            {
                _measurementCache.TryAdd(cacheKey, constrainedHeight);
            }

            return constrainedHeight;
        }
        finally
        {
            _calculationSemaphore.Release();
        }
    }

    /// <summary>
    /// PERFORMANCE: Batch height calculation for large datasets
    /// LINQ OPTIMIZED: Parallel processing with controlled concurrency
    /// </summary>
    public async Task<IReadOnlyDictionary<int, double>> CalculateOptimalRowHeightsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        IReadOnlyList<ColumnDefinition> columns,
        AutoRowHeightConfiguration configuration,
        IProgress<RowHeightCalculationProgress>? progressReporter = null,
        cancellationToken cancellationToken = default)
    {
        var rowList = rows.ToList();
        var results = new ConcurrentDictionary<int, double>();
        var processedCount = 0;

        using var scope = PerformanceCounters.CreateOperationScope("AutoRowHeight_BatchCalculate");

        var tasks = rowList.Select(async (row, index) =>
        {
            var height = await CalculateRowHeightAsync(row, columns, configuration, cancellationToken);
            results.TryAdd(index, height);

            var current = Interlocked.Increment(ref processedCount);
            progressReporter?.Report(new RowHeightCalculationProgress
            {
                ProcessedRows = current,
                TotalRows = rowList.Count,
                CurrentRowIndex = index,
                CalculatedHeight = height
            });
        });

        await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// ALGORITHMS: Advanced text measurement with word-aware wrapping
    /// PERFORMANCE: Font metrics caching and optimized calculations
    /// </summary>
    private async Task<double> CalculateMaxCellHeightAsync(
        IReadOnlyDictionary<string, object?> rowData,
        IReadOnlyList<ColumnDefinition> columns,
        AutoRowHeightConfiguration configuration,
        cancellationToken cancellationToken)
    {
        var maxHeight = configuration.MinimumRowHeight;

        var heightTasks = columns
            .Where(col => col.IsVisible && !col.IsReadOnly)
            .Select(async column =>
            {
                if (!rowData.TryGetValue(column.Name, out var cellValue) || cellValue == null)
                    return configuration.MinimumRowHeight;

                var cellText = cellValue.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(cellText))
                    return configuration.MinimumRowHeight;

                // Use TextMeasurementAlgorithms for precise calculation
                var textHeight = await Task.Run(() =>
                    TextMeasurementAlgorithms.CalculateTextHeight(
                        cellText,
                        column.Width ?? configuration.DefaultColumnWidth,
                        configuration.FontSize,
                        configuration.FontFamily,
                        configuration.TextWrapping,
                        configuration.LineHeight), cancellationToken);

                // Add cell padding
                return textHeight + configuration.CellPadding.Top + configuration.CellPadding.Bottom;
            });

        var heights = await Task.WhenAll(heightTasks);
        return Math.Max(maxHeight, heights.Max());
    }

    /// <summary>
    /// MEMORY MANAGEMENT: Clear cache during memory pressure
    /// </summary>
    public void ClearCache()
    {
        _measurementCache.Clear();
        PerformanceCounters.IncrementCounter("AutoRowHeight_CacheCleared");
    }

    /// <summary>
    /// CONFIGURATION: Enable/disable caching based on performance needs
    /// </summary>
    public void SetCachingEnabled(bool enabled)
    {
        _cachingEnabled = enabled;
        if (!enabled)
        {
            ClearCache();
        }
    }

    private string GenerateCacheKey(
        IReadOnlyDictionary<string, object?> rowData,
        IReadOnlyList<ColumnDefinition> columns,
        AutoRowHeightConfiguration configuration)
    {
        // Create deterministic cache key
        var contentHash = string.Join("|",
            rowData.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        var configHash = $"{configuration.FontSize}:{configuration.FontFamily}:{configuration.LineHeight}";
        var columnsHash = string.Join(",", columns.Select(c => $"{c.Name}:{c.Width}"));

        return $"{contentHash.GetHashCode()}:{configHash.GetHashCode()}:{columnsHash.GetHashCode()}";
    }
}

/// <summary>
/// DDD: Auto row height configuration value object
/// </summary>
public sealed record AutoRowHeightConfiguration
{
    public double MinimumRowHeight { get; init; } = 25.0;
    public double MaximumRowHeight { get; init; } = 200.0;
    public double DefaultColumnWidth { get; init; } = 150.0;
    public double FontSize { get; init; } = 14.0;
    public string FontFamily { get; init; } = "Segoe UI";
    public double LineHeight { get; init; } = 1.2;
    public TextWrappingMode TextWrapping { get; init; } = TextWrappingMode.Wrap;
    public CellPadding CellPadding { get; init; } = new(4, 4, 8, 8);

    public static AutoRowHeightConfiguration Default => new();
    public static AutoRowHeightConfiguration Compact => new()
    {
        MinimumRowHeight = 20.0,
        MaximumRowHeight = 100.0,
        FontSize = 12.0,
        CellPadding = new(2, 2, 4, 4)
    };
}

/// <summary>
/// DDD: Cell padding specification
/// </summary>
public sealed record CellPadding(double Top, double Right, double Bottom, double Left);

/// <summary>
/// DDD: Row height calculation progress
/// </summary>
public sealed record RowHeightCalculationProgress
{
    public int ProcessedRows { get; init; }
    public int TotalRows { get; init; }
    public int CurrentRowIndex { get; init; }
    public double CalculatedHeight { get; init; }
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
}

public enum TextWrappingMode
{
    None,
    Wrap,
    WrapWithOverflow
}
```

### 2. **TextMeasurementAlgorithms** - Advanced Text Rendering

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Algorithms;

/// <summary>
/// ENTERPRISE: Advanced text measurement algorithms with font-aware calculations
/// FUNCTIONAL: Pure functions without side effects for maximum testability
/// PERFORMANCE: Optimized for high-throughput text processing
/// ALGORITHMS: Sophisticated text layout and word boundary analysis
/// </summary>
internal static class TextMeasurementAlgorithms
{
    #region Font Metrics Constants

    // Font-specific character width factors for accurate estimation
    private static readonly IReadOnlyDictionary<string, double> FontWidthFactors = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
    {
        ["Segoe UI"] = 0.55,
        ["Arial"] = 0.56,
        ["Calibri"] = 0.52,
        ["Times New Roman"] = 0.50,
        ["Courier New"] = 0.60, // Monospace
        ["Consolas"] = 0.60,    // Monospace
        ["Georgia"] = 0.54,
        ["Verdana"] = 0.58,
        ["Tahoma"] = 0.53,
        ["Comic Sans MS"] = 0.57
    };

    private const double DefaultWidthFactor = 0.55;
    private const double MonospaceWidthFactor = 0.60;

    #endregion

    /// <summary>
    /// ALGORITHMS: Calculate text height with word-aware wrapping
    /// PERFORMANCE: Optimized character-per-line calculation
    /// </summary>
    public static double CalculateTextHeight(
        string text,
        double availableWidth,
        double fontSize,
        string fontFamily,
        TextWrappingMode textWrapping = TextWrappingMode.Wrap,
        double lineHeight = 1.2)
    {
        if (string.IsNullOrEmpty(text) || availableWidth <= 0 || fontSize <= 0)
            return fontSize * lineHeight;

        var charactersPerLine = CalculateCharactersPerLine(availableWidth, fontSize, fontFamily);
        var lineCount = CalculateLineCount(text, charactersPerLine, textWrapping);

        return Math.Max(1, lineCount) * fontSize * lineHeight;
    }

    /// <summary>
    /// PERFORMANCE: Calculate average character width based on font metrics
    /// FONT AWARENESS: Different calculations for different font families
    /// </summary>
    public static double CalculateAverageCharacterWidth(double fontSize, string fontFamily)
    {
        var widthFactor = GetFontWidthFactor(fontFamily);
        return fontSize * widthFactor;
    }

    /// <summary>
    /// ALGORITHMS: Calculate characters per line with font considerations
    /// </summary>
    private static int CalculateCharactersPerLine(double availableWidth, double fontSize, string fontFamily)
    {
        var avgCharWidth = CalculateAverageCharacterWidth(fontSize, fontFamily);
        return Math.Max(1, (int)(availableWidth / avgCharWidth));
    }

    /// <summary>
    /// ALGORITHMS: Calculate line count with intelligent word wrapping
    /// WORD BOUNDARY: Respects word boundaries for better text flow
    /// </summary>
    private static int CalculateLineCount(string text, int charactersPerLine, TextWrappingMode textWrapping)
    {
        if (textWrapping == TextWrappingMode.None)
        {
            return text.Contains('\n') ? text.Count(c => c == '\n') + 1 : 1;
        }

        var lines = text.Split('\n', StringSplitOptions.None);
        var totalLines = 0;

        foreach (var line in lines)
        {
            if (line.Length <= charactersPerLine)
            {
                totalLines++;
                continue;
            }

            // Word-aware wrapping
            totalLines += CalculateWrappedLineCount(line, charactersPerLine, textWrapping);
        }

        return Math.Max(1, totalLines);
    }

    /// <summary>
    /// ALGORITHMS: Calculate wrapped lines with word boundary respect
    /// WORD PROCESSING: Intelligent text segmentation for optimal wrapping
    /// </summary>
    private static int CalculateWrappedLineCount(string text, int charactersPerLine, TextWrappingMode textWrapping)
    {
        if (string.IsNullOrEmpty(text))
            return 1;

        var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.None);
        var lineCount = 0;
        var currentLineLength = 0;

        foreach (var word in words)
        {
            var wordLength = word.Length;
            var spaceNeeded = currentLineLength == 0 ? wordLength : wordLength + 1; // +1 for space

            if (currentLineLength + spaceNeeded <= charactersPerLine)
            {
                currentLineLength += spaceNeeded;
            }
            else
            {
                // Start new line
                lineCount++;

                if (wordLength > charactersPerLine)
                {
                    // Word is too long, force break
                    var extraLines = (wordLength - 1) / charactersPerLine;
                    lineCount += extraLines;
                    currentLineLength = wordLength % charactersPerLine;
                }
                else
                {
                    currentLineLength = wordLength;
                }
            }
        }

        return lineCount + (currentLineLength > 0 ? 1 : 0);
    }

    /// <summary>
    /// ALGORITHMS: Analyze word boundaries for intelligent text flow
    /// </summary>
    public static IReadOnlyList<TextSegment> AnalyzeWordBoundaries(string text, int charactersPerLine)
    {
        var segments = new List<TextSegment>();
        if (string.IsNullOrEmpty(text))
            return segments;

        var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.None);
        var currentLineLength = 0;
        var currentSegment = new List<string>();

        foreach (var word in words)
        {
            var spaceNeeded = currentLineLength == 0 ? word.Length : word.Length + 1;

            if (currentLineLength + spaceNeeded <= charactersPerLine)
            {
                currentSegment.Add(word);
                currentLineLength += spaceNeeded;
            }
            else
            {
                // Finish current segment
                if (currentSegment.Any())
                {
                    segments.Add(new TextSegment(string.Join(" ", currentSegment), currentLineLength));
                }

                // Start new segment
                currentSegment = new List<string> { word };
                currentLineLength = word.Length;
            }
        }

        // Add final segment
        if (currentSegment.Any())
        {
            segments.Add(new TextSegment(string.Join(" ", currentSegment), currentLineLength));
        }

        return segments;
    }

    /// <summary>
    /// ALGORITHMS: Calculate text with ellipsis truncation
    /// UI OPTIMIZATION: Handles text truncation for height-constrained scenarios
    /// </summary>
    public static string TruncateTextWithEllipsis(
        string text,
        int maxLines,
        int charactersPerLine,
        string ellipsis = "...")
    {
        if (string.IsNullOrEmpty(text) || maxLines <= 0)
            return string.Empty;

        var segments = AnalyzeWordBoundaries(text, charactersPerLine);
        if (segments.Count <= maxLines)
            return text;

        var truncatedSegments = segments.Take(maxLines - 1).ToList();
        var lastSegmentText = segments[maxLines - 1].Text;

        // Ensure last line has room for ellipsis
        var ellipsisSpace = ellipsis.Length;
        if (lastSegmentText.Length + ellipsisSpace > charactersPerLine)
        {
            var availableSpace = charactersPerLine - ellipsisSpace;
            lastSegmentText = lastSegmentText.Substring(0, Math.Max(0, availableSpace)).TrimEnd();
        }

        truncatedSegments.Add(new TextSegment(lastSegmentText + ellipsis, lastSegmentText.Length + ellipsisSpace));

        return string.Join("\n", truncatedSegments.Select(s => s.Text));
    }

    /// <summary>
    /// PERFORMANCE: Get font-specific width factor with fallback
    /// </summary>
    private static double GetFontWidthFactor(string fontFamily)
    {
        if (string.IsNullOrEmpty(fontFamily))
            return DefaultWidthFactor;

        if (FontWidthFactors.TryGetValue(fontFamily, out var factor))
            return factor;

        // Check for monospace fonts
        var lowerFont = fontFamily.ToLowerInvariant();
        if (lowerFont.Contains("mono") || lowerFont.Contains("consol") || lowerFont.Contains("courier"))
            return MonospaceWidthFactor;

        return DefaultWidthFactor;
    }
}

/// <summary>
/// DDD: Text segment value object for word boundary analysis
/// </summary>
public sealed record TextSegment(string Text, int Length)
{
    public bool IsEmpty => string.IsNullOrEmpty(Text);
    public bool ExceedsLength(int maxLength) => Length > maxLength;
}
```

### 3. **CellEditingService** - Advanced Editing Workflows

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// ENTERPRISE: Advanced cell editing with transaction-like behavior
/// ASYNC: Non-blocking editing operations with cancellation support
/// VALIDATION: Real-time validation during editing process
/// WORKFLOW: Complex editing workflows with rollback capabilities
/// </summary>
internal sealed class CellEditingService
{
    private readonly ConcurrentDictionary<string, EditingSession> _activeSessions = new();
    private readonly SemaphoreSlim _editingSemaphore = new(Environment.ProcessorCount);
    private volatile EditingConfiguration _configuration = EditingConfiguration.Default;

    /// <summary>
    /// ENTERPRISE: Start cell editing session with validation
    /// TRANSACTION: Creates isolated editing context with rollback capability
    /// </summary>
    public async Task<Result<EditingSession>> StartEditingAsync(
        CellEditingCommand command,
        cancellationToken cancellationToken = default)
    {
        await _editingSemaphore.WaitAsync(cancellationToken);
        try
        {
            using var scope = PerformanceCounters.CreateOperationScope("CellEditing_Start");

            // Validate editing permissions
            var validationResult = await ValidateEditingPermissionsAsync(command, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                return Result<EditingSession>.Failure(validationResult.ErrorMessage);
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var editingSession = new EditingSession
            {
                SessionId = sessionId,
                RowIndex = command.RowIndex,
                ColumnName = command.ColumnName,
                OriginalValue = command.CurrentValue,
                CurrentValue = command.CurrentValue,
                EditingMode = command.EditingMode,
                StartedAt = DateTime.UtcNow,
                Configuration = _configuration
            };

            _activeSessions.TryAdd(sessionId, editingSession);

            // Log editing start
            UIInteractionLogger.LogEditingStarted(sessionId, command.RowIndex,
                command.ColumnName, command.CurrentValue);

            return Result<EditingSession>.Success(editingSession);
        }
        finally
        {
            _editingSemaphore.Release();
        }
    }

    /// <summary>
    /// VALIDATION: Real-time value validation during editing
    /// PERFORMANCE: Incremental validation with debouncing
    /// </summary>
    public async Task<Result<ValidationResult>> ValidateEditingValueAsync(
        string sessionId,
        object? newValue,
        cancellationToken cancellationToken = default)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            return Result<ValidationResult>.Failure("Editing session not found");
        }

        using var scope = PerformanceCounters.CreateOperationScope("CellEditing_Validate");

        try
        {
            // Update session with new value
            var updatedSession = session with
            {
                CurrentValue = newValue,
                LastModified = DateTime.UtcNow,
                ValidationAttempts = session.ValidationAttempts + 1
            };
            _activeSessions.TryUpdate(sessionId, updatedSession, session);

            // Perform validation based on column definition and business rules
            var validationResult = await ExecuteEditingValidationAsync(updatedSession, cancellationToken);

            // Log validation result
            UIInteractionLogger.LogEditingValidation(sessionId, newValue,
                validationResult.IsValid, validationResult.Errors);

            return Result<ValidationResult>.Success(validationResult);
        }
        catch (Exception ex)
        {
            return Result<ValidationResult>.Failure($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// TRANSACTION: Commit editing changes with final validation
    /// WORKFLOW: Complete editing workflow with success/failure handling
    /// </summary>
    public async Task<Result<CellEditingResult>> CommitEditingAsync(
        string sessionId,
        cancellationToken cancellationToken = default)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            return Result<CellEditingResult>.Failure("Editing session not found");
        }

        await _editingSemaphore.WaitAsync(cancellationToken);
        try
        {
            using var scope = PerformanceCounters.CreateOperationScope("CellEditing_Commit");

            // Final validation before commit
            var finalValidation = await ExecuteEditingValidationAsync(session, cancellationToken);
            if (!finalValidation.IsValid)
            {
                return Result<CellEditingResult>.Failure(
                    $"Commit failed - validation errors: {string.Join(", ", finalValidation.Errors)}");
            }

            // Create editing result
            var editingResult = new CellEditingResult
            {
                SessionId = sessionId,
                RowIndex = session.RowIndex,
                ColumnName = session.ColumnName,
                OriginalValue = session.OriginalValue,
                FinalValue = session.CurrentValue,
                EditingDuration = DateTime.UtcNow - session.StartedAt,
                ValidationAttempts = session.ValidationAttempts,
                WasModified = !Equals(session.OriginalValue, session.CurrentValue)
            };

            // Remove session
            _activeSessions.TryRemove(sessionId, out _);

            // Log successful commit
            UIInteractionLogger.LogEditingCommitted(sessionId, session.RowIndex,
                session.ColumnName, session.OriginalValue, session.CurrentValue,
                editingResult.EditingDuration);

            return Result<CellEditingResult>.Success(editingResult);
        }
        finally
        {
            _editingSemaphore.Release();
        }
    }

    /// <summary>
    /// TRANSACTION: Cancel editing and revert to original value
    /// ROLLBACK: Complete rollback with session cleanup
    /// </summary>
    public async Task<Result<bool>> CancelEditingAsync(
        string sessionId,
        cancellationToken cancellationToken = default)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            return Result<bool>.Failure("Editing session not found");
        }

        using var scope = PerformanceCounters.CreateOperationScope("CellEditing_Cancel");

        // Remove session
        _activeSessions.TryRemove(sessionId, out _);

        // Log cancellation
        UIInteractionLogger.LogEditingCancelled(sessionId, session.RowIndex,
            session.ColumnName, DateTime.UtcNow - session.StartedAt);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// BULK OPERATIONS: Batch editing for multiple cells
    /// PERFORMANCE: Optimized batch processing with parallel validation
    /// </summary>
    public async Task<Result<IReadOnlyList<CellEditingResult>>> CommitBatchEditingAsync(
        IEnumerable<string> sessionIds,
        cancellationToken cancellationToken = default)
    {
        var sessionList = sessionIds.ToList();
        var results = new ConcurrentBag<CellEditingResult>();
        var errors = new ConcurrentBag<string>();

        using var scope = PerformanceCounters.CreateOperationScope("CellEditing_BatchCommit");

        var tasks = sessionList.Select(async sessionId =>
        {
            var result = await CommitEditingAsync(sessionId, cancellationToken);
            if (result.IsSuccess)
            {
                results.Add(result.Value);
            }
            else
            {
                errors.Add($"Session {sessionId}: {result.ErrorMessage}");
            }
        });

        await Task.WhenAll(tasks);

        if (errors.Any())
        {
            return Result<IReadOnlyList<CellEditingResult>>.Failure(
                $"Batch commit had errors: {string.Join("; ", errors)}");
        }

        return Result<IReadOnlyList<CellEditingResult>>.Success(results.ToList());
    }

    /// <summary>
    /// CONFIGURATION: Update editing configuration at runtime
    /// </summary>
    public void UpdateConfiguration(EditingConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// MONITORING: Get current editing statistics
    /// </summary>
    public EditingStatistics GetEditingStatistics()
    {
        return new EditingStatistics
        {
            ActiveSessions = _activeSessions.Count,
            TotalValidationAttempts = _activeSessions.Values.Sum(s => s.ValidationAttempts),
            LongestSessionDuration = _activeSessions.Values
                .Select(s => DateTime.UtcNow - s.StartedAt)
                .DefaultIfEmpty()
                .Max(),
            SessionsByMode = _activeSessions.Values
                .GroupBy(s => s.EditingMode)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private async Task<Result<bool>> ValidateEditingPermissionsAsync(
        CellEditingCommand command,
        cancellationToken cancellationToken)
    {
        // Implement permission validation based on column definition and user context
        if (command.ColumnDefinition.IsReadOnly)
        {
            return Result<bool>.Failure("Column is read-only");
        }

        // Additional business rule validation can be added here
        return Result<bool>.Success(true);
    }

    private async Task<ValidationResult> ExecuteEditingValidationAsync(
        EditingSession session,
        cancellationToken cancellationToken)
    {
        // Implement comprehensive validation logic
        var errors = new List<string>();

        // Type validation
        if (session.CurrentValue != null && session.ColumnDefinition != null)
        {
            // Add type-specific validation logic here
        }

        // Business rule validation
        // Add business rule validation based on session configuration

        return errors.Any()
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}

/// <summary>
/// DDD: Editing session value object with transaction semantics
/// </summary>
public sealed record EditingSession
{
    public required string SessionId { get; init; }
    public required int RowIndex { get; init; }
    public required string ColumnName { get; init; }
    public object? OriginalValue { get; init; }
    public object? CurrentValue { get; init; }
    public EditingMode EditingMode { get; init; } = EditingMode.Standard;
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastModified { get; init; } = DateTime.UtcNow;
    public int ValidationAttempts { get; init; } = 0;
    public EditingConfiguration Configuration { get; init; } = EditingConfiguration.Default;
    public ColumnDefinition? ColumnDefinition { get; init; }

    public TimeSpan SessionDuration => DateTime.UtcNow - StartedAt;
    public bool HasBeenModified => !Equals(OriginalValue, CurrentValue);
}

/// <summary>
/// DDD: Cell editing result value object
/// </summary>
public sealed record CellEditingResult
{
    public required string SessionId { get; init; }
    public required int RowIndex { get; init; }
    public required string ColumnName { get; init; }
    public object? OriginalValue { get; init; }
    public object? FinalValue { get; init; }
    public TimeSpan EditingDuration { get; init; }
    public int ValidationAttempts { get; init; }
    public bool WasModified { get; init; }
}

/// <summary>
/// DDD: Editing configuration value object
/// </summary>
public sealed record EditingConfiguration
{
    public bool EnableRealTimeValidation { get; init; } = true;
    public TimeSpan ValidationDebounceTime { get; init; } = TimeSpan.FromMilliseconds(300);
    public int MaxValidationAttempts { get; init; } = 10;
    public bool AutoCommitOnFocusLost { get; init; } = true;
    public bool EnableUndoRedo { get; init; } = true;
    public EditingMode DefaultEditingMode { get; init; } = EditingMode.Standard;

    public static EditingConfiguration Default => new();
    public static EditingConfiguration HighPerformance => new()
    {
        EnableRealTimeValidation = false,
        ValidationDebounceTime = TimeSpan.FromMilliseconds(500),
        AutoCommitOnFocusLost = false
    };
}

public enum EditingMode
{
    Standard,
    InPlace,
    Modal,
    Batch
}
```

## 📋 COMMAND PATTERN PRE UI OPERATIONS

### CellEditingCommand
```csharp
public sealed record CellEditingCommand
{
    public required int RowIndex { get; init; }
    public required string ColumnName { get; init; }
    public required ColumnDefinition ColumnDefinition { get; init; }
    public object? CurrentValue { get; init; }
    public EditingMode EditingMode { get; init; } = EditingMode.Standard;
    public bool EnableValidation { get; init; } = true;
    public bool AutoCommit { get; init; } = false;
    public TimeSpan? EditingTimeout { get; init; }
    public IProgress<EditingProgress>? ProgressReporter { get; init; }
    public cancellationToken cancellationToken { get; init; } = default;

    // FLEXIBLE factory methods s DI support
    public static CellEditingCommand Create(int rowIndex, string columnName, ColumnDefinition columnDef) =>
        new() { RowIndex = rowIndex, ColumnName = columnName, ColumnDefinition = columnDef };

    public static CellEditingCommand WithValue(int rowIndex, string columnName, ColumnDefinition columnDef, object? value) =>
        new() { RowIndex = rowIndex, ColumnName = columnName, ColumnDefinition = columnDef, CurrentValue = value };

    // DI factory method
    public static CellEditingCommand CreateWithDI(int rowIndex, string columnName, ColumnDefinition columnDef, IServiceProvider services) =>
        new() { RowIndex = rowIndex, ColumnName = columnName, ColumnDefinition = columnDef };
}
```

### UIInteractionCommand
```csharp
public sealed record UIInteractionCommand
{
    public required UIInteractionType InteractionType { get; init; }
    public required string ElementId { get; init; }
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
    public bool EnableLogging { get; init; } = true;
    public bool EnableValidation { get; init; } = true;
    public TimeSpan? InteractionTimeout { get; init; }
    public UIInteractionContext? Context { get; init; }

    // FLEXIBLE factory methods
    public static UIInteractionCommand Create(UIInteractionType type, string elementId) =>
        new() { InteractionType = type, ElementId = elementId };

    public static UIInteractionCommand WithParameters(
        UIInteractionType type,
        string elementId,
        IReadOnlyDictionary<string, object?> parameters) =>
        new() { InteractionType = type, ElementId = elementId, Parameters = parameters };

    // LINQ optimized factory pre bulk interactions
    public static IEnumerable<UIInteractionCommand> CreateBulk(
        IEnumerable<(UIInteractionType type, string elementId)> interactions) =>
        interactions.Select(interaction => Create(interaction.type, interaction.elementId));
}

public enum UIInteractionType
{
    Click,
    DoubleClick,
    KeyPress,
    TextInput,
    Selection,
    Scroll,
    Resize,
    DragDrop,
    ContextMenu,
    Focus,
    Blur
}
```

## 🎯 FAÇADE API METÓDY

### UI Operations API
```csharp
#region UI Operations with Command Pattern

/// <summary>
/// PUBLIC API: Start cell editing using command pattern
/// ENTERPRISE: Professional cell editing with transaction support
/// CONSISTENT: Rovnaká štruktúra ako ImportAsync a ValidateAsync
/// </summary>
Task<Result<EditingSession>> StartCellEditingAsync(
    CellEditingCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Process UI interaction
/// ENTERPRISE: Comprehensive interaction handling with logging
/// LINQ OPTIMIZED: Parallel processing for bulk interactions
/// </summary>
Task<Result<UIInteractionResult>> ProcessUIInteractionAsync(
    UIInteractionCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Calculate optimal row heights
/// PERFORMANCE: Batch calculation with progress reporting
/// </summary>
Task<Result<IReadOnlyDictionary<int, double>>> CalculateRowHeightsAsync(
    IEnumerable<IReadOnlyDictionary<string, object?>> rows,
    IReadOnlyList<ColumnDefinition> columns,
    AutoRowHeightConfiguration? configuration = null,
    IProgress<RowHeightCalculationProgress>? progressReporter = null,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Get current UI state
/// PERFORMANCE: Immediate state retrieval without processing overhead
/// </summary>
UIStateSnapshot GetCurrentUIState();

#endregion

#region UI Configuration and Theming

/// <summary>
/// PUBLIC API: Apply color theme to UI
/// ENTERPRISE: Professional theming with validation
/// </summary>
Task<Result<bool>> ApplyColorThemeAsync(ColorConfiguration theme);

/// <summary>
/// PUBLIC API: Configure UI behavior settings
/// DYNAMIC: Runtime configuration of UI behavior
/// </summary>
Task<Result<bool>> ConfigureUIBehaviorAsync(UIBehaviorConfiguration configuration);

/// <summary>
/// PUBLIC API: Get UI performance metrics
/// MONITORING: Comprehensive UI performance statistics
/// </summary>
UIPerformanceMetrics GetUIPerformanceMetrics();

#endregion

#region Keyboard and Interaction Handling

/// <summary>
/// PUBLIC API: Process keyboard shortcut
/// ENTERPRISE: Context-aware shortcut processing with conflict resolution
/// </summary>
Task<Result<ShortcutExecutionResult>> ProcessKeyboardShortcutAsync(
    KeyboardShortcut shortcut,
    UIContext context,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Configure keyboard shortcuts
/// CUSTOMIZATION: User-configurable shortcut mappings
/// </summary>
Task<Result<bool>> ConfigureKeyboardShortcutsAsync(
    IReadOnlyList<KeyboardShortcutMapping> shortcuts);

/// <summary>
/// PUBLIC API: Get interaction statistics
/// ANALYTICS: User interaction analytics and usage patterns
/// </summary>
UIInteractionStatistics GetInteractionStatistics();

#endregion
```

## 🎨 WINUI INTEGRATION PATTERNS

### WinUI Color System Integration
```csharp
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.UI;

/// <summary>
/// WINUI INTEGRATION: Native WinUI color system integration
/// THEMING: Comprehensive theme support with system integration
/// PERFORMANCE: Cached color conversions for optimal performance
/// </summary>
internal static class WinUIColorConverter
{
    private static readonly ConcurrentDictionary<string, SolidColorBrush> _brushCache = new();

    /// <summary>
    /// INTEGRATION: Convert internal color configuration to WinUI brushes
    /// CACHING: Performance optimization with brush caching
    /// </summary>
    public static SolidColorBrush ConvertToBrush(Color color)
    {
        var colorKey = $"{color.A}_{color.R}_{color.G}_{color.B}";
        return _brushCache.GetOrAdd(colorKey, _ => new SolidColorBrush(color));
    }

    /// <summary>
    /// THEMING: Apply color configuration to WinUI theme resources
    /// SYSTEM INTEGRATION: Integrates with WinUI theme system
    /// </summary>
    public static void ApplyColorConfigurationToTheme(
        ColorConfiguration colorConfig,
        ResourceDictionary themeResources)
    {
        // Grid appearance
        themeResources["GridBackgroundBrush"] = ConvertToBrush(colorConfig.GridBackgroundColor);
        themeResources["GridBorderBrush"] = ConvertToBrush(colorConfig.GridBorderColor);
        themeResources["GridLinesBrush"] = ConvertToBrush(colorConfig.GridLinesColor);

        // Header styling
        themeResources["HeaderBackgroundBrush"] = ConvertToBrush(colorConfig.HeaderBackgroundColor);
        themeResources["HeaderForegroundBrush"] = ConvertToBrush(colorConfig.HeaderForegroundColor);
        themeResources["HeaderBorderBrush"] = ConvertToBrush(colorConfig.HeaderBorderColor);

        // Cell states
        themeResources["CellBackgroundBrush"] = ConvertToBrush(colorConfig.CellBackgroundColor);
        themeResources["CellForegroundBrush"] = ConvertToBrush(colorConfig.CellForegroundColor);
        themeResources["SelectedCellBackgroundBrush"] = ConvertToBrush(colorConfig.SelectedCellBackgroundColor);
        themeResources["EditingCellBackgroundBrush"] = ConvertToBrush(colorConfig.EditingCellBackgroundColor);

        // Validation states
        themeResources["ErrorCellBackgroundBrush"] = ConvertToBrush(colorConfig.ErrorCellBackgroundColor);
        themeResources["ErrorTextBrush"] = ConvertToBrush(colorConfig.ErrorTextColor);
        themeResources["WarningTextBrush"] = ConvertToBrush(colorConfig.WarningTextColor);
        themeResources["InfoTextBrush"] = ConvertToBrush(colorConfig.InfoTextColor);
    }

    /// <summary>
    /// ACCESSIBILITY: High contrast theme support
    /// </summary>
    public static ColorConfiguration GetHighContrastTheme()
    {
        return new ColorConfiguration
        {
            GridBackgroundColor = Colors.White,
            GridLinesColor = Colors.Black,
            CellBackgroundColor = Colors.White,
            CellForegroundColor = Colors.Black,
            SelectedCellBackgroundColor = Colors.Navy,
            SelectedCellForegroundColor = Colors.White,
            ErrorCellBackgroundColor = Colors.Red,
            ErrorTextColor = Colors.White
        };
    }

    /// <summary>
    /// MEMORY MANAGEMENT: Clear brush cache during memory pressure
    /// </summary>
    public static void ClearBrushCache()
    {
        _brushCache.Clear();
    }
}
```

## 🎯 KĽÚČOVÉ VYLEPŠENIA PODĽA ARCHITEKTÚRY

### 1. **Advanced UI Services**
- **AutoRowHeightService**: Sophisticated text measurement s font awareness
- **CellEditingService**: Transaction-based editing s validation workflows
- **TextMeasurementAlgorithms**: Word-boundary aware text layout algorithms
- **Keyboard Shortcuts**: Context-aware shortcut processing s conflict resolution

### 2. **WinUI Native Integration**
- **Color System**: Native WinUI color and theming integration
- **Resource Management**: Proper WinUI resource dictionary integration
- **Accessibility**: High contrast and accessibility support
- **Performance**: Cached brush conversion for optimal rendering

### 3. **Enterprise UI Features**
- **Transaction Semantics**: Full editing transaction support s rollback
- **Comprehensive Logging**: All UI interactions logged for analytics
- **Performance Monitoring**: Real-time UI performance metrics
- **Configuration Management**: Runtime UI behavior configuration

### 4. **Advanced Text Processing**
- **Font Awareness**: Font-specific character width calculations
- **Word Boundary Analysis**: Intelligent text wrapping algorithms
- **Multi-line Support**: Complex text layout with line height control
- **Truncation**: Smart text truncation s ellipsis support

### 5. **UI State Management**
- **Session Management**: Isolated editing sessions s cleanup
- **State Synchronization**: Thread-safe state management
- **Event Correlation**: Comprehensive interaction tracking
- **Memory Optimization**: Automatic cache cleanup during pressure

### 6. **Professional User Experience**
- **Context-Aware Operations**: Different behavior based on UI context
- **Progress Reporting**: Real-time progress for long operations
- **Error Handling**: Graceful error handling s user feedback
- **Customization**: Extensive configuration options

---

## **🔍 LOGGING ŠPECIFIKÁCIA PRE UI OPERÁCIE**

### **Internal DI Registration & Service Distribution**
Všetky UI logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a distribuované cez internal DI do UI services:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<IUIInteractionLogger<AutoRowHeightService>, UIInteractionLogger<AutoRowHeightService>>();
services.AddSingleton<IUIInteractionLogger<CellEditingService>, UIInteractionLogger<CellEditingService>>();
services.AddSingleton<IOperationLogger<UIService>, OperationLogger<UIService>>();
services.AddSingleton<ICommandLogger<UIService>, CommandLogger<UIService>>();

// V UI service constructors
public AutoRowHeightService(
    ILogger<AutoRowHeightService> logger,
    IUIInteractionLogger<AutoRowHeightService> uiLogger,
    IOperationLogger<AutoRowHeightService> operationLogger)
```

### **UI Operations Logging**
```csharp
// Cell editing operations
_uiLogger.LogEditingStarted(sessionId, rowIndex, columnName, originalValue);
_logger.LogInformation("Cell editing started: session={SessionId}, row={RowIndex}, column={ColumnName}, duration={Duration}ms",
    sessionId, rowIndex, columnName, editingDuration.TotalMilliseconds);

// Row height calculations
_uiLogger.LogRowHeightCalculation(rowIndex, calculatedHeight, measurementTime);
_logger.LogInformation("Row height calculated: row={RowIndex}, height={Height}px, time={Time}ms",
    rowIndex, calculatedHeight, measurementTime.TotalMilliseconds);

// UI interactions
_uiLogger.LogUIInteraction(interactionType, elementId, wasSuccessful, interactionDuration);
_logger.LogInformation("UI interaction: type={InteractionType}, element={ElementId}, success={Success}, time={Time}ms",
    interactionType, elementId, wasSuccessful, interactionDuration.TotalMilliseconds);
```

### **Theme and Configuration Logging**
```csharp
// Theme application
_logger.LogInformation("Color theme applied: theme={ThemeName}, colors={ColorCount}, resources_updated={ResourceCount}",
    theme.ThemeName, colorCount, resourcesUpdated);

// Configuration updates
_logger.LogInformation("UI configuration updated: setting={SettingName}, old_value={OldValue}, new_value={NewValue}",
    settingName, oldValue, newValue);

// Performance monitoring
_logger.LogInformation("UI performance: avg_response_time={ResponseTime}ms, ui_operations={OperationCount}, memory_usage={MemoryUsage}MB",
    avgResponseTime.TotalMilliseconds, uiOperationCount, memoryUsageInMB);
```

### **Keyboard and Interaction Logging**
```csharp
// Keyboard shortcuts
_logger.LogInformation("Keyboard shortcut executed: shortcut={Shortcut}, context={Context}, success={Success}, time={Time}ms",
    shortcut.ToString(), context.ToString(), wasSuccessful, executionTime.TotalMilliseconds);

// Text measurement operations
_logger.LogInformation("Text measurement: characters={CharCount}, lines={LineCount}, width={Width}px, height={Height}px, time={Time}ms",
    characterCount, lineCount, measuredWidth, measuredHeight, measurementTime.TotalMilliseconds);

// UI state changes
_logger.LogInformation("UI state changed: component={Component}, old_state={OldState}, new_state={NewState}, trigger={Trigger}",
    componentName, oldState, newState, changeTrigger);
```

### **Logging Levels Usage:**
- **Information**: Successful UI operations, theme applications, configuration updates, interaction completions
- **Warning**: Performance degradation, editing validation warnings, theme compatibility issues
- **Error**: UI operation failures, editing commit errors, theme application failures, rendering errors
- **Critical**: UI system failures, unrecoverable editing states, critical rendering failures

Táto špecifikácia poskytuje kompletný, enterprise-ready UI systém s pokročilými editovania capabilities, WinUI integráciou a jednotnou architektúrou s ostatnými časťami komponentu.


## Column resize (drag & drop)

Behavior:
- User can resize a column by moving the mouse to the right border of a column header (cursor changes to horizontal resize).
- Press and hold left mouse button on the column border, then drag left or right.
- The entire column (header and data cells) resizes together.
- Minimum and maximum widths are enforced by configuration (`MinimumColumnWidth`, `MaximumColumnWidth`).

Public API (entry point):
```csharp
// Example on the public entry point (AdvancedDataGridFacade)
void ResizeColumn(int columnIndex, double newWidth);
void StartColumnResize(int columnIndex, double clientX); // called by UI when mouse down
void UpdateColumnResize(double clientX); // called on mouse move
void EndColumnResize(); // called on mouse up
```

Internal implementation note:
- Entry point methods map to internal services (e.g. `IColumnService.ResizeColumnInternal(...)`) via DI.
- Implementation should debounce width updates for performance and only reflow visible rows when necessary.



## Cell selection & multi-select behaviors

Behavior:
- Single click on a cell selects that cell.
- Click and drag selects a contiguous rectangular range of cells (including header and data coordinates).
- Ctrl + Click toggles selection of individual cells without losing the existing selection (multi-select arbitrary cells).
- Shift + Click extends selection from the last focused cell to the clicked cell (range).
- Copying the selection should preserve the relative layout / shape: when pasted into Excel or similar, the cells retain their relative positions (row/column offsets).

Public API (entry point):
```csharp
// Selection operations exposed on the public entry point
void SelectCell(int rowIndex, int columnIndex);
void StartDragSelect(int startRow, int startColumn);
void UpdateDragSelect(int currentRow, int currentColumn);
void EndDragSelect();
void ToggleCellSelection(int rowIndex, int columnIndex); // Ctrl+Click behavior
void ExtendSelectionTo(int rowIndex, int columnIndex); // Shift+Click behavior
CopyResult CopySelection(); // returns a structured payload suitable for clipboard, preserving shape
```

Clipboard copy semantics:
- `CopyResult` should include a 2D array of cell payloads and metadata about empty cells so that pasting into Excel keeps the same offsets.
- For example, copying cells {(0,0), (0,2)} produces a 1-row x 3-column area with a blank in the middle so Excel preserves spacing.

Internal implementation note:
- Internal services manage the low-level buffer; the public entry point maps calls to those internals.
- The `CopySelection` implementation should serialize as tab-separated lines preserving empty cells:
  - Each row of the selection becomes one line.
  - Cells separated by `	`.
  - Empty cells represented by empty fields between tabs.
```



## Special columns (UI & Export behavior)

This component supports four special columns that are part of the grid UI model. They are considered special because they are not part of the main data model's exported columns by default (except `validAlerts` which may be included explicitly). Their behavior:

1. `validAlerts` (always enabled)
   - Shows custom validation messages produced by validation rules in `ValidationDocumentation.md`.
   - Format: concatenated messages per cell/row, e.g. `ColumnA: missing value; ColumnB: invalid format; ...`
   - This column **is always present** in the grid model but is **only included in exports** when `IncludeValidAlerts = true` on `ExportDataCommand`.
   - Layout behavior: `validAlerts` fills remaining horizontal space between last non-special column and end of grid (or between last non-special column and `deleteRowColumn` if that is enabled). Only a minimum width is enforced; it is flexible.

2. `rowNumber` (optional)
   - Displays the row number for each row (1-based or configured offset).
   - Controlled by initialization configuration (UI & Headless): default `false`. When enabled it is **always** the first visible column in the grid (index 0). Even when disabled, rows retain row numbers internally (used for stable identity), but the column is hidden.

3. `checkboxColumn` (optional)
   - Displays a checkbox per row for user selection; header checkbox toggles all rows.
   - Controlled by initialization configuration (default `false`).
   - If `ExportOnlyChecked = true`, only rows with this checkbox set to `true` are exported. If the checkbox column is disabled, `ExportOnlyChecked` is ignored.

4. `deleteRowColumn` (optional)
   - Displays a delete icon for row-level deletion; clicking deletes the row.
   - Controlled by initialization configuration (default `false`).
   - Layout: when enabled it is always the **last** column (far right) with fixed width sufficient for the icon. If enabled, `validAlerts` appears immediately to the left of `deleteRowColumn`; otherwise `validAlerts` is the last column.

Ordering summary when special columns are enabled:
- `rowNumber` — if enabled, always first.
- `checkboxColumn` — if enabled, appears after `rowNumber` if rowNumber is enabled; otherwise it becomes first.
- Non-special data columns follow.
- `validAlerts` — always present; flexible width; placed after non-special columns and before `deleteRowColumn` (if enabled) or at the end.
- `deleteRowColumn` — if enabled, always last (fixed width).

Export rules:
- `rowNumber`, `checkboxColumn`, `deleteRowColumn` are **not** exported as data columns (they are UI-only) unless you explicitly include `validAlerts` via `IncludeValidAlerts`.
- `validAlerts` can be exported as a special column when `IncludeValidAlerts = true`.



## Column name deduplication (initialization behavior)

When the component is initialized with user-provided column names, the implementation MUST ensure column names are unique in the visible model. If duplicate names are provided, the grid will automatically disambiguate them by appending a suffix `_{n}` to subsequent duplicates (n starting at 1).

Example:
- Input names: `["meno","priezvisko","meno","cislo"]`
- Internal visible column names become: `["meno","priezvisko","meno_1","cislo"]`

Special columns (`validAlerts`, `rowNumber`, `checkboxColumn`, `deleteRowColumn`) are exempt from this renaming rule — their names are fixed and cannot be overridden.



### Facade: UI refresh for validation results

The public entry point `AdvancedDataGridFacade` exposes a method to request pushing validation results to the UI on-demand. This is used in headless mode or when the consumer wants to control UI refresh timing after bulk validation:

```csharp
// Request the component to push the latest validation results to UI (no-op in headless until caller requests)
void RefreshValidationResultsToUI();
```

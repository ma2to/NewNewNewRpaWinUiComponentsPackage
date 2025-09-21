using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Services;

/// <summary>
/// CORE: Smart operations service for automated delete and expand functionality
/// ENTERPRISE: Professional automation with pattern recognition and smart logic
/// CLEAN ARCHITECTURE: Core domain service for intelligent grid operations
/// INTERNAL: Not exposed to public API - hardcoded component behavior
/// </summary>
internal sealed class SmartOperationsService
{
    private readonly ILogger<SmartOperationsService>? _logger;
    private readonly Dictionary<string, SmartPattern> _learnedPatterns;
    private readonly object _lockObject = new();

    public SmartOperationsService(ILogger<SmartOperationsService>? logger = null)
    {
        _logger = logger;
        _learnedPatterns = new Dictionary<string, SmartPattern>();
    }

    /// <summary>
    /// SMART DELETE: Analyze data patterns and suggest/perform intelligent row deletion
    /// PATTERN RECOGNITION: Learn from user behavior and data relationships
    /// </summary>
    public async Task<SmartDeleteResult> AnalyzeSmartDeleteAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        GridBehaviorConfiguration behavior,
        CancellationToken cancellationToken = default)
    {
        if (!behavior.EnableSmartDelete || data.Count == 0)
            return SmartDeleteResult.NoAction();

        try
        {
            _logger?.LogDebug("Starting smart delete analysis for {RowCount} rows", data.Count);

            var deleteSuggestions = new List<SmartDeleteSuggestion>();

            // 1. Detect duplicate rows
            var duplicates = await DetectDuplicateRowsAsync(data, columns, cancellationToken);
            if (duplicates.Any())
            {
                deleteSuggestions.Add(new SmartDeleteSuggestion(
                    "Duplicate Rows",
                    $"Found {duplicates.Count} duplicate rows that can be safely removed",
                    duplicates,
                    SmartDeleteReason.Duplicates,
                    0.95f));
            }

            // 2. Detect empty or incomplete rows
            var emptyRows = await DetectEmptyRowsAsync(data, columns, cancellationToken);
            if (emptyRows.Any())
            {
                deleteSuggestions.Add(new SmartDeleteSuggestion(
                    "Empty Rows",
                    $"Found {emptyRows.Count} rows with mostly empty data",
                    emptyRows,
                    SmartDeleteReason.EmptyData,
                    0.85f));
            }

            // 3. Detect outlier rows based on patterns
            var outliers = await DetectOutlierRowsAsync(data, columns, cancellationToken);
            if (outliers.Any())
            {
                deleteSuggestions.Add(new SmartDeleteSuggestion(
                    "Data Outliers",
                    $"Found {outliers.Count} rows that appear to be data entry errors or outliers",
                    outliers,
                    SmartDeleteReason.DataOutliers,
                    0.70f));
            }

            // 4. Detect rows violating learned patterns
            var patternViolations = await DetectPatternViolationsAsync(data, columns, cancellationToken);
            if (patternViolations.Any())
            {
                deleteSuggestions.Add(new SmartDeleteSuggestion(
                    "Pattern Violations",
                    $"Found {patternViolations.Count} rows that don't match learned data patterns",
                    patternViolations,
                    SmartDeleteReason.PatternViolation,
                    0.60f));
            }

            _logger?.LogInformation("Smart delete analysis completed. Found {SuggestionCount} suggestions", deleteSuggestions.Count);

            return SmartDeleteResult.WithSuggestions(deleteSuggestions);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during smart delete analysis");
            return SmartDeleteResult.Error($"Smart delete analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// SMART EXPAND: Analyze data patterns and suggest intelligent row expansion/completion
    /// AUTO-FILL: Predict missing data based on patterns and relationships
    /// </summary>
    public async Task<SmartExpandResult> AnalyzeSmartExpandAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        GridBehaviorConfiguration behavior,
        CancellationToken cancellationToken = default)
    {
        if (!behavior.EnableSmartExpand || data.Count == 0)
            return SmartExpandResult.NoAction();

        try
        {
            _logger?.LogDebug("Starting smart expand analysis for {RowCount} rows", data.Count);

            var expandSuggestions = new List<SmartExpandSuggestion>();

            // 1. Predict missing values based on column patterns
            var missingValuePredictions = await PredictMissingValuesAsync(data, columns, cancellationToken);
            if (missingValuePredictions.Any())
            {
                expandSuggestions.Add(new SmartExpandSuggestion(
                    "Missing Value Predictions",
                    $"Can predict {missingValuePredictions.Count} missing values based on data patterns",
                    missingValuePredictions,
                    SmartExpandReason.MissingValues,
                    0.80f));
            }

            // 2. Suggest new rows based on sequences or patterns
            var sequenceCompletions = await SuggestSequenceCompletionsAsync(data, columns, cancellationToken);
            if (sequenceCompletions.Any())
            {
                expandSuggestions.Add(new SmartExpandSuggestion(
                    "Sequence Completions",
                    $"Can add {sequenceCompletions.Count} rows to complete data sequences",
                    sequenceCompletions,
                    SmartExpandReason.SequenceCompletion,
                    0.75f));
            }

            // 3. Suggest derived columns or calculated fields
            var derivedFields = await SuggestDerivedFieldsAsync(data, columns, cancellationToken);
            if (derivedFields.Any())
            {
                expandSuggestions.Add(new SmartExpandSuggestion(
                    "Derived Fields",
                    $"Can add {derivedFields.Count} calculated columns based on existing data",
                    derivedFields,
                    SmartExpandReason.DerivedFields,
                    0.70f));
            }

            // 4. Suggest data enrichment from learned patterns
            var enrichmentSuggestions = await SuggestDataEnrichmentAsync(data, columns, cancellationToken);
            if (enrichmentSuggestions.Any())
            {
                expandSuggestions.Add(new SmartExpandSuggestion(
                    "Data Enrichment",
                    $"Can enrich {enrichmentSuggestions.Count} rows with additional information",
                    enrichmentSuggestions,
                    SmartExpandReason.DataEnrichment,
                    0.65f));
            }

            _logger?.LogInformation("Smart expand analysis completed. Found {SuggestionCount} suggestions", expandSuggestions.Count);

            return SmartExpandResult.WithSuggestions(expandSuggestions);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during smart expand analysis");
            return SmartExpandResult.Error($"Smart expand analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// LEARNING: Update learned patterns based on user actions and data changes
    /// ADAPTIVE: Improve suggestions over time based on usage patterns
    /// </summary>
    public async Task LearnFromUserActionsAsync(
        SmartOperationAction action,
        IReadOnlyList<Dictionary<string, object?>> affectedData,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                var patternKey = GeneratePatternKey(columns);

                if (!_learnedPatterns.TryGetValue(patternKey, out var pattern))
                {
                    pattern = new SmartPattern(patternKey, DateTime.UtcNow);
                    _learnedPatterns[patternKey] = pattern;
                }

                pattern.RecordAction(action, affectedData);
                _logger?.LogDebug("Learned from user action: {ActionType} affecting {RowCount} rows", action.Type, affectedData.Count);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to learn from user actions");
        }
    }

    #region Private Helper Methods

    private async Task<List<int>> DetectDuplicateRowsAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        // LINQ OPTIMIZATION: Replace nested loops with functional approach
        var keyColumns = columns.Where(c => !c.Name.Contains("Id", StringComparison.OrdinalIgnoreCase)).ToList();

        var duplicateIndexes = data
            .SelectMany((row, index) => data
                .Skip(index + 1)
                .Select((otherRow, otherIndex) => new { index, otherIndex = index + 1 + otherIndex, row, otherRow }))
            .Where(pair => !cancellationToken.IsCancellationRequested && AreRowsEqual(pair.row, pair.otherRow, keyColumns))
            .Select(pair => pair.otherIndex)
            .Distinct()
            .ToList();

        return duplicateIndexes;
    }

    private async Task<List<int>> DetectEmptyRowsAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        // LINQ OPTIMIZATION: Replace manual loop with functional pipeline
        var emptyRowIndexes = data
            .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
            .Select((row, index) => new
            {
                Index = index,
                EmptyRatio = 1.0 - (double)columns.Count(column =>
                    row.TryGetValue(column.Name, out var value) &&
                    value != null &&
                    !string.IsNullOrWhiteSpace(value.ToString())) / columns.Count
            })
            .Where(item => item.EmptyRatio >= 0.7) // 70% or more columns are empty
            .Select(item => item.Index)
            .ToList();

        return emptyRowIndexes;
    }

    private async Task<List<int>> DetectOutlierRowsAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken)
    {
        var outlierIndexes = new List<int>();

        // Detect statistical outliers in numeric columns
        var numericColumns = columns.Where(c =>
            c.DataType == typeof(int) ||
            c.DataType == typeof(decimal) ||
            c.DataType == typeof(double) ||
            c.DataType == typeof(float)).ToList();

        foreach (var column in numericColumns)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // LINQ OPTIMIZATION: Functional approach with statistical calculations
            var values = data
                .Select((row, index) => new { Value = row.GetValueOrDefault(column.Name), Index = index })
                .Where(x => x.Value != null && decimal.TryParse(x.Value.ToString(), out _))
                .Select(x => new { DecimalValue = decimal.Parse(x.Value!.ToString()!), x.Index })
                .ToList();

            if (values.Count < 3) continue;

            var mean = values.Average(v => v.DecimalValue);
            var variance = values.Average(v => (double)((v.DecimalValue - mean) * (v.DecimalValue - mean)));
            var stdDev = Math.Sqrt(variance);

            // LINQ OPTIMIZATION: Replace foreach with functional approach
            var columnOutliers = values
                .Where(value => Math.Abs((double)(value.DecimalValue - mean)) / stdDev > 3.0)
                .Select(value => value.Index)
                .Where(index => !outlierIndexes.Contains(index));

            outlierIndexes.AddRange(columnOutliers);
        }

        await Task.CompletedTask;
        return outlierIndexes.Distinct().ToList();
    }

    private async Task<List<int>> DetectPatternViolationsAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken)
    {
        var violationIndexes = new List<int>();

        lock (_lockObject)
        {
            var patternKey = GeneratePatternKey(columns);
            if (_learnedPatterns.TryGetValue(patternKey, out var pattern))
            {
                violationIndexes.AddRange(pattern.DetectViolations(data));
            }
        }

        await Task.CompletedTask;
        return violationIndexes;
    }

    private async Task<List<SmartValuePrediction>> PredictMissingValuesAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken)
    {
        var predictions = new List<SmartValuePrediction>();

        // LINQ OPTIMIZATION: Replace nested loops with functional pipeline
        var columnPredictions = columns
            .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
            .SelectMany(column => data
                .Select((row, index) => new { row, index, column })
                .Where(item => !item.row.TryGetValue(item.column.Name, out var value) ||
                              value == null ||
                              string.IsNullOrWhiteSpace(value.ToString()))
                .Select(item => new { item.index, item.column, item.row })
                .Select(item => new { item.index, item.column, predictedValue = PredictValueFromPattern(item.row, item.column, data, columns) })
                .Where(item => item.predictedValue != null)
                .Select(item => new SmartValuePrediction(item.index, item.column.Name, item.predictedValue!, 0.75f)));

        predictions.AddRange(columnPredictions);

        await Task.CompletedTask;
        return predictions;
    }

    private async Task<List<SmartRowSuggestion>> SuggestSequenceCompletionsAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken)
    {
        var suggestions = new List<SmartRowSuggestion>();

        // LINQ OPTIMIZATION: Replace manual loops with functional approach
        var numericColumns = columns.Where(c =>
            c.DataType == typeof(int) ||
            c.DataType == typeof(decimal)).ToList();

        var sequenceSuggestions = numericColumns
            .TakeWhile(_ => !cancellationToken.IsCancellationRequested)
            .Select(column => new { column, sequence = DetectNumericSequence(data, column) })
            .Where(item => item.sequence != null)
            .SelectMany(item => GenerateSequenceCompletions(item.sequence!, 3)
                .Select(nextValue => new SmartRowSuggestion(
                    CreateRowFromSequence(data.LastOrDefault(), item.column, nextValue), 0.70f)));

        suggestions.AddRange(sequenceSuggestions);

        await Task.CompletedTask;
        return suggestions;
    }

    private async Task<List<SmartColumnSuggestion>> SuggestDerivedFieldsAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken)
    {
        var suggestions = new List<SmartColumnSuggestion>();

        // Suggest calculated fields based on existing numeric columns
        var numericColumns = columns.Where(c =>
            c.DataType == typeof(int) ||
            c.DataType == typeof(decimal) ||
            c.DataType == typeof(double)).ToList();

        if (numericColumns.Count >= 2)
        {
            // Suggest sum, difference, ratio columns
            var firstCol = numericColumns[0];
            var secondCol = numericColumns[1];

            suggestions.Add(new SmartColumnSuggestion(
                $"{firstCol.Name}_Plus_{secondCol.Name}",
                typeof(decimal),
                $"Sum of {firstCol.DisplayName} and {secondCol.DisplayName}",
                0.65f));

            suggestions.Add(new SmartColumnSuggestion(
                $"{firstCol.Name}_Ratio_{secondCol.Name}",
                typeof(decimal),
                $"Ratio of {firstCol.DisplayName} to {secondCol.DisplayName}",
                0.60f));
        }

        await Task.CompletedTask;
        return suggestions;
    }

    private async Task<List<SmartEnrichmentSuggestion>> SuggestDataEnrichmentAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken)
    {
        var suggestions = new List<SmartEnrichmentSuggestion>();

        // Suggest enrichment based on learned patterns
        lock (_lockObject)
        {
            var patternKey = GeneratePatternKey(columns);
            if (_learnedPatterns.TryGetValue(patternKey, out var pattern))
            {
                suggestions.AddRange(pattern.SuggestEnrichments(data));
            }
        }

        await Task.CompletedTask;
        return suggestions;
    }

    private bool AreRowsEqual(Dictionary<string, object?> row1, Dictionary<string, object?> row2, IList<ColumnDefinition> keyColumns)
    {
        foreach (var column in keyColumns)
        {
            var value1 = row1.GetValueOrDefault(column.Name)?.ToString() ?? "";
            var value2 = row2.GetValueOrDefault(column.Name)?.ToString() ?? "";

            if (!string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private object? PredictValueFromPattern(Dictionary<string, object?> row, ColumnDefinition column, IReadOnlyList<Dictionary<string, object?>> allData, IReadOnlyList<ColumnDefinition> allColumns)
    {
        // Simple pattern-based prediction logic
        // In a real implementation, this would use machine learning or more sophisticated algorithms

        if (column.DataType == typeof(string))
        {
            // LINQ OPTIMIZATION: Functional approach for finding most common value
            return allData
                .Select(r => r.GetValueOrDefault(column.Name)?.ToString())
                .Where(v => !string.IsNullOrEmpty(v))
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
        }

        return null;
    }

    private NumericSequence? DetectNumericSequence(IReadOnlyList<Dictionary<string, object?>> data, ColumnDefinition column)
    {
        // LINQ OPTIMIZATION: Functional approach for sequence detection
        var values = data
            .Select(row => row.GetValueOrDefault(column.Name))
            .Where(v => v != null && decimal.TryParse(v.ToString(), out _))
            .Select(v => decimal.Parse(v!.ToString()!))
            .ToList();

        if (values.Count < 3) return null;

        var differences = values
            .Skip(1)
            .Select((value, index) => value - values[index])
            .ToList();

        var avgDifference = differences.Average();
        var isSequence = differences.All(d => Math.Abs(d - avgDifference) < 0.01m);

        return isSequence ? new NumericSequence(values.Last(), avgDifference) : null;
    }

    private List<decimal> GenerateSequenceCompletions(NumericSequence sequence, int count)
    {
        var completions = new List<decimal>();
        var current = sequence.LastValue;

        for (int i = 0; i < count; i++)
        {
            current += sequence.Step;
            completions.Add(current);
        }

        return completions;
    }

    private Dictionary<string, object?> CreateRowFromSequence(Dictionary<string, object?>? templateRow, ColumnDefinition column, decimal value)
    {
        var newRow = templateRow?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object?>();
        newRow[column.Name] = value;
        return newRow;
    }

    private string GeneratePatternKey(IReadOnlyList<ColumnDefinition> columns)
    {
        return string.Join("_", columns.Select(c => $"{c.Name}:{c.DataType.Name}").OrderBy(x => x));
    }

    #endregion
}

#region Supporting Types

internal record NumericSequence(decimal LastValue, decimal Step);

internal class SmartPattern
{
    public string Key { get; }
    public DateTime CreatedAt { get; }
    public List<SmartOperationAction> Actions { get; }

    public SmartPattern(string key, DateTime createdAt)
    {
        Key = key;
        CreatedAt = createdAt;
        Actions = new List<SmartOperationAction>();
    }

    public void RecordAction(SmartOperationAction action, IReadOnlyList<Dictionary<string, object?>> affectedData)
    {
        Actions.Add(action);
        // In real implementation, analyze the data patterns and store insights
    }

    public List<int> DetectViolations(IReadOnlyList<Dictionary<string, object?>> data)
    {
        // Placeholder for pattern violation detection based on learned actions
        return new List<int>();
    }

    public List<SmartEnrichmentSuggestion> SuggestEnrichments(IReadOnlyList<Dictionary<string, object?>> data)
    {
        // Placeholder for enrichment suggestions based on learned patterns
        return new List<SmartEnrichmentSuggestion>();
    }
}

#endregion
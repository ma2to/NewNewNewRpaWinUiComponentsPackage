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
        var duplicateIndexes = new List<int>();
        var keyColumns = columns.Where(c => !c.Name.Contains("Id", StringComparison.OrdinalIgnoreCase)).ToList();

        for (int i = 0; i < data.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (int j = i + 1; j < data.Count; j++)
            {
                if (AreRowsEqual(data[i], data[j], keyColumns))
                {
                    if (!duplicateIndexes.Contains(j))
                        duplicateIndexes.Add(j);
                }
            }
        }

        await Task.CompletedTask;
        return duplicateIndexes;
    }

    private async Task<List<int>> DetectEmptyRowsAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken)
    {
        var emptyRowIndexes = new List<int>();

        for (int i = 0; i < data.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = data[i];
            var nonEmptyColumns = 0;

            foreach (var column in columns)
            {
                if (row.TryGetValue(column.Name, out var value) &&
                    value != null &&
                    !string.IsNullOrWhiteSpace(value.ToString()))
                {
                    nonEmptyColumns++;
                }
            }

            var emptyRatio = 1.0 - (double)nonEmptyColumns / columns.Count;
            if (emptyRatio >= 0.7) // 70% or more columns are empty
            {
                emptyRowIndexes.Add(i);
            }
        }

        await Task.CompletedTask;
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

            var values = data
                .Select((row, index) => new { Value = row.GetValueOrDefault(column.Name), Index = index })
                .Where(x => x.Value != null && decimal.TryParse(x.Value.ToString(), out _))
                .Select(x => new { DecimalValue = decimal.Parse(x.Value!.ToString()!), x.Index })
                .ToList();

            if (values.Count < 3) continue;

            var mean = values.Average(v => v.DecimalValue);
            var variance = values.Average(v => (double)((v.DecimalValue - mean) * (v.DecimalValue - mean)));
            var stdDev = Math.Sqrt(variance);

            foreach (var value in values)
            {
                var zScore = Math.Abs((double)(value.DecimalValue - mean)) / stdDev;
                if (zScore > 3.0) // Z-score threshold for outliers
                {
                    if (!outlierIndexes.Contains(value.Index))
                        outlierIndexes.Add(value.Index);
                }
            }
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

        foreach (var column in columns)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (int i = 0; i < data.Count; i++)
            {
                var row = data[i];
                if (!row.TryGetValue(column.Name, out var value) ||
                    value == null ||
                    string.IsNullOrWhiteSpace(value.ToString()))
                {
                    var predictedValue = PredictValueFromPattern(row, column, data, columns);
                    if (predictedValue != null)
                    {
                        predictions.Add(new SmartValuePrediction(i, column.Name, predictedValue, 0.75f));
                    }
                }
            }
        }

        await Task.CompletedTask;
        return predictions;
    }

    private async Task<List<SmartRowSuggestion>> SuggestSequenceCompletionsAsync(
        IReadOnlyList<Dictionary<string, object?>> data,
        IReadOnlyList<ColumnDefinition> columns,
        CancellationToken cancellationToken)
    {
        var suggestions = new List<SmartRowSuggestion>();

        // Detect numeric sequences and suggest completions
        var numericColumns = columns.Where(c =>
            c.DataType == typeof(int) ||
            c.DataType == typeof(decimal)).ToList();

        foreach (var column in numericColumns)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sequence = DetectNumericSequence(data, column);
            if (sequence != null)
            {
                var nextValues = GenerateSequenceCompletions(sequence, 3);
                foreach (var nextValue in nextValues)
                {
                    var newRow = CreateRowFromSequence(data.LastOrDefault(), column, nextValue);
                    suggestions.Add(new SmartRowSuggestion(newRow, 0.70f));
                }
            }
        }

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
            // Find most common value for this column
            var commonValues = allData
                .Select(r => r.GetValueOrDefault(column.Name)?.ToString())
                .Where(v => !string.IsNullOrEmpty(v))
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return commonValues?.Key;
        }

        return null;
    }

    private NumericSequence? DetectNumericSequence(IReadOnlyList<Dictionary<string, object?>> data, ColumnDefinition column)
    {
        var values = data
            .Select(row => row.GetValueOrDefault(column.Name))
            .Where(v => v != null && decimal.TryParse(v.ToString(), out _))
            .Select(v => decimal.Parse(v!.ToString()!))
            .ToList();

        if (values.Count < 3) return null;

        var differences = new List<decimal>();
        for (int i = 1; i < values.Count; i++)
        {
            differences.Add(values[i] - values[i - 1]);
        }

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
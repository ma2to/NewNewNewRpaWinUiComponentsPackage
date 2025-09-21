using System;
using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

#region Smart Delete Results

/// <summary>
/// CORE: Result of smart delete analysis
/// ENTERPRISE: Professional result with suggestions and confidence scores
/// </summary>
internal sealed record SmartDeleteResult
{
    public bool HasSuggestions => Suggestions.Count > 0;
    public bool IsError => !string.IsNullOrEmpty(ErrorMessage);
    public IReadOnlyList<SmartDeleteSuggestion> Suggestions { get; }
    public string? ErrorMessage { get; }
    public DateTime AnalyzedAt { get; }

    private SmartDeleteResult(IReadOnlyList<SmartDeleteSuggestion> suggestions, string? errorMessage = null)
    {
        Suggestions = suggestions;
        ErrorMessage = errorMessage;
        AnalyzedAt = DateTime.UtcNow;
    }

    public static SmartDeleteResult WithSuggestions(IReadOnlyList<SmartDeleteSuggestion> suggestions) =>
        new(suggestions);

    public static SmartDeleteResult NoAction() =>
        new(Array.Empty<SmartDeleteSuggestion>());

    public static SmartDeleteResult Error(string errorMessage) =>
        new(Array.Empty<SmartDeleteSuggestion>(), errorMessage);
}

/// <summary>
/// CORE: Individual smart delete suggestion
/// ENTERPRISE: Professional suggestion with reasoning and confidence
/// </summary>
internal sealed record SmartDeleteSuggestion
{
    public string Title { get; }
    public string Description { get; }
    public IReadOnlyList<int> RowIndexes { get; }
    public SmartDeleteReason Reason { get; }
    public float Confidence { get; }
    public bool IsAutoApplicable => Confidence >= 0.90f;

    public SmartDeleteSuggestion(
        string title,
        string description,
        IReadOnlyList<int> rowIndexes,
        SmartDeleteReason reason,
        float confidence)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        RowIndexes = rowIndexes ?? throw new ArgumentNullException(nameof(rowIndexes));
        Reason = reason;
        Confidence = Math.Clamp(confidence, 0f, 1f);
    }
}

/// <summary>
/// CORE: Reasons for smart delete suggestions
/// ENTERPRISE: Professional categorization of deletion logic
/// </summary>
internal enum SmartDeleteReason
{
    Duplicates,
    EmptyData,
    DataOutliers,
    PatternViolation,
    UserPattern,
    ValidationFailure
}

#endregion

#region Smart Expand Results

/// <summary>
/// CORE: Result of smart expand analysis
/// ENTERPRISE: Professional result with expansion suggestions
/// </summary>
internal sealed record SmartExpandResult
{
    public bool HasSuggestions => Suggestions.Count > 0;
    public bool IsError => !string.IsNullOrEmpty(ErrorMessage);
    public IReadOnlyList<SmartExpandSuggestion> Suggestions { get; }
    public string? ErrorMessage { get; }
    public DateTime AnalyzedAt { get; }

    private SmartExpandResult(IReadOnlyList<SmartExpandSuggestion> suggestions, string? errorMessage = null)
    {
        Suggestions = suggestions;
        ErrorMessage = errorMessage;
        AnalyzedAt = DateTime.UtcNow;
    }

    public static SmartExpandResult WithSuggestions(IReadOnlyList<SmartExpandSuggestion> suggestions) =>
        new(suggestions);

    public static SmartExpandResult NoAction() =>
        new(Array.Empty<SmartExpandSuggestion>());

    public static SmartExpandResult Error(string errorMessage) =>
        new(Array.Empty<SmartExpandSuggestion>(), errorMessage);
}

/// <summary>
/// CORE: Individual smart expand suggestion
/// ENTERPRISE: Professional expansion suggestion with multiple types
/// </summary>
internal sealed record SmartExpandSuggestion
{
    public string Title { get; }
    public string Description { get; }
    public object SuggestionData { get; }
    public SmartExpandReason Reason { get; }
    public float Confidence { get; }
    public bool IsAutoApplicable => Confidence >= 0.85f;

    public SmartExpandSuggestion(
        string title,
        string description,
        object suggestionData,
        SmartExpandReason reason,
        float confidence)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        SuggestionData = suggestionData ?? throw new ArgumentNullException(nameof(suggestionData));
        Reason = reason;
        Confidence = Math.Clamp(confidence, 0f, 1f);
    }
}

/// <summary>
/// CORE: Reasons for smart expand suggestions
/// ENTERPRISE: Professional categorization of expansion logic
/// </summary>
internal enum SmartExpandReason
{
    MissingValues,
    SequenceCompletion,
    DerivedFields,
    DataEnrichment,
    PatternCompletion,
    RelatedData
}

#endregion

#region Smart Operation Support Types

/// <summary>
/// CORE: Smart value prediction for missing data
/// ENTERPRISE: Professional prediction with confidence scoring
/// </summary>
internal sealed record SmartValuePrediction
{
    public int RowIndex { get; }
    public string ColumnName { get; }
    public object PredictedValue { get; }
    public float Confidence { get; }

    public SmartValuePrediction(int rowIndex, string columnName, object predictedValue, float confidence)
    {
        RowIndex = rowIndex;
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        PredictedValue = predictedValue ?? throw new ArgumentNullException(nameof(predictedValue));
        Confidence = Math.Clamp(confidence, 0f, 1f);
    }
}

/// <summary>
/// CORE: Smart row suggestion for sequence completion
/// ENTERPRISE: Professional row suggestion with pattern-based generation
/// </summary>
internal sealed record SmartRowSuggestion
{
    public Dictionary<string, object?> SuggestedRow { get; }
    public float Confidence { get; }

    public SmartRowSuggestion(Dictionary<string, object?> suggestedRow, float confidence)
    {
        SuggestedRow = suggestedRow ?? throw new ArgumentNullException(nameof(suggestedRow));
        Confidence = Math.Clamp(confidence, 0f, 1f);
    }
}

/// <summary>
/// CORE: Smart column suggestion for derived fields
/// ENTERPRISE: Professional column suggestion with calculated field logic
/// </summary>
internal sealed record SmartColumnSuggestion
{
    public string ColumnName { get; }
    public Type DataType { get; }
    public string Description { get; }
    public float Confidence { get; }

    public SmartColumnSuggestion(string columnName, Type dataType, string description, float confidence)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Confidence = Math.Clamp(confidence, 0f, 1f);
    }
}

/// <summary>
/// CORE: Smart enrichment suggestion for additional data
/// ENTERPRISE: Professional enrichment with external data integration
/// </summary>
internal sealed record SmartEnrichmentSuggestion
{
    public int RowIndex { get; }
    public Dictionary<string, object?> EnrichmentData { get; }
    public string Source { get; }
    public float Confidence { get; }

    public SmartEnrichmentSuggestion(int rowIndex, Dictionary<string, object?> enrichmentData, string source, float confidence)
    {
        RowIndex = rowIndex;
        EnrichmentData = enrichmentData ?? throw new ArgumentNullException(nameof(enrichmentData));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Confidence = Math.Clamp(confidence, 0f, 1f);
    }
}

/// <summary>
/// CORE: Smart operation action for learning patterns
/// ENTERPRISE: Professional action tracking for pattern recognition
/// </summary>
internal sealed record SmartOperationAction
{
    public SmartOperationType Type { get; }
    public DateTime Timestamp { get; }
    public Dictionary<string, object> Metadata { get; }

    public SmartOperationAction(SmartOperationType type, Dictionary<string, object>? metadata = null)
    {
        Type = type;
        Timestamp = DateTime.UtcNow;
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}

/// <summary>
/// CORE: Types of smart operations for learning
/// ENTERPRISE: Professional categorization of user actions
/// </summary>
internal enum SmartOperationType
{
    DeleteAccepted,
    DeleteRejected,
    ExpandAccepted,
    ExpandRejected,
    ValueCorrected,
    PatternLearned
}

#endregion
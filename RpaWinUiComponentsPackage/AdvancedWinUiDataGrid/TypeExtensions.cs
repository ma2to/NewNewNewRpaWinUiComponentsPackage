using System;
using System.Collections.Generic;
using System.Linq;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// INTERNAL CONVERSIONS: Extension methods to convert between public facade types and internal Core types
/// This allows facade to work with internal services while exposing public API
/// </summary>
internal static class TypeExtensions
{
    // Filter Extensions
    public static CoreTypes.FilterDefinition ToInternal(this FilterDefinition filter) => new()
    {
        ColumnName = filter.ColumnName,
        Operator = (CoreTypes.FilterOperator)(int)filter.Operator,
        Value = filter.Value,
        SecondValue = filter.SecondValue,
        LogicOperator = (CoreTypes.FilterLogicOperator)(int)filter.LogicOperator,
        FilterName = filter.FilterName
    };

    public static FilterDefinition ToPublic(this CoreTypes.FilterDefinition filter) => new()
    {
        ColumnName = filter.ColumnName,
        Operator = (FilterOperator)(int)filter.Operator,
        Value = filter.Value,
        SecondValue = filter.SecondValue,
        LogicOperator = (FilterLogicOperator)(int)filter.LogicOperator,
        FilterName = filter.FilterName
    };

    public static CoreTypes.AdvancedFilter ToInternal(this AdvancedFilter filter) => new()
    {
        ColumnName = filter.ColumnName,
        Operator = (CoreTypes.FilterOperator)(int)filter.Operator,
        Value = filter.Value,
        SecondValue = filter.SecondValue,
        LogicOperator = (CoreTypes.FilterLogicOperator)(int)filter.LogicOperator,
        GroupStart = filter.GroupStart,
        GroupEnd = filter.GroupEnd,
        FilterName = filter.FilterName
    };

    public static CoreTypes.AdvancedSearchCriteria ToInternal(this AdvancedSearchCriteria criteria) => new()
    {
        SearchText = criteria.SearchText,
        TargetColumns = criteria.TargetColumns,
        UseRegex = criteria.UseRegex,
        CaseSensitive = criteria.CaseSensitive,
        Scope = (CoreTypes.SearchScope)(int)criteria.Scope,
        MaxMatches = criteria.MaxMatches,
        Timeout = criteria.Timeout
    };

    public static SearchResult ToPublic(this CoreTypes.SearchResult result) => new()
    {
        RowIndex = result.RowIndex,
        ColumnName = result.ColumnName,
        Value = result.Value,
        MatchedText = result.MatchedText,
        MatchStartIndex = result.MatchStartIndex,
        MatchLength = result.MatchLength
    };

    public static FilterResult ToPublic(this CoreTypes.FilterResult result) => new()
    {
        TotalRowsProcessed = result.TotalRowsProcessed,
        MatchingRows = result.MatchingRows,
        FilteredOutRows = result.FilteredOutRows,
        ProcessingTime = result.ProcessingTime,
        MatchingRowIndices = result.MatchingRowIndices
    };

    // Sort Extensions
    public static CoreTypes.SortColumnConfiguration ToInternal(this SortColumnConfiguration config) => new()
    {
        ColumnName = config.ColumnName,
        Direction = (CoreTypes.SortDirection)(int)config.Direction,
        Priority = config.Priority,
        IsPrimary = config.IsPrimary
    };

    public static SortColumnConfiguration ToPublic(this CoreTypes.SortColumnConfiguration config) => new()
    {
        ColumnName = config.ColumnName,
        Direction = (SortDirection)(int)config.Direction,
        Priority = config.Priority,
        IsPrimary = config.IsPrimary
    };

    public static SortResult ToPublic(this CoreTypes.SortResult result) => new()
    {
        SortedData = result.SortedData,
        AppliedSorts = result.AppliedSorts.Select(s => s.ToPublic()).ToArray(),
        SortTime = result.SortTime,
        ProcessedRows = result.ProcessedRows
    };

    // Import/Export Extensions
    public static CoreTypes.ImportOptions ToInternal(this ImportOptions options) => new()
    {
        Mode = (CoreTypes.ImportMode)(int)options.Mode,
        StartRowIndex = options.StartRowIndex,
        ValidateBeforeImport = options.ValidateBeforeImport,
        CreateMissingColumns = options.CreateMissingColumns,
        ColumnMapping = options.ColumnMapping,
        Progress = options.Progress != null ? new ImportProgressAdapter(options.Progress) : null
    };

    public static CoreTypes.ExportOptions ToInternal(this ExportOptions options) => new()
    {
        IncludeHeaders = options.IncludeHeaders,
        ColumnsToExport = options.ColumnsToExport,
        DateTimeFormat = options.DateTimeFormat,
        Progress = options.Progress != null ? new ExportProgressAdapter(options.Progress) : null
    };

    public static ImportResult ToPublic(this CoreTypes.ImportResult result) => new()
    {
        Success = result.Success,
        ImportedRows = result.ImportedRows,
        SkippedRows = result.SkippedRows,
        TotalRows = result.TotalRows,
        ImportTime = result.ImportTime,
        ErrorMessages = result.ErrorMessages,
        WarningMessages = result.WarningMessages
    };

    public static CopyPasteResult ToPublic(this CoreTypes.CopyPasteResult result) => new()
    {
        Success = result.Success,
        ProcessedRows = result.ProcessedRows,
        ClipboardData = result.ClipboardData,
        ErrorMessage = result.ErrorMessage
    };

    // Validation Extensions
    public static ValidationResult ToPublic(this CoreTypes.ValidationResult result) => new()
    {
        IsValid = result.IsValid,
        Severity = (ValidationSeverity)(int)result.Severity,
        Message = result.ErrorMessage ?? "",
        RuleName = result.RuleName,
        ValidationTime = TimeSpan.Zero, // Default placeholder
        Value = null, // Default placeholder
        IsTimeout = false // Default placeholder
    };

    // Collection conversions
    public static IReadOnlyList<T> ToPublicList<T, TInternal>(this IReadOnlyList<TInternal> internalList, Func<TInternal, T> converter)
    {
        return internalList.Select(converter).ToArray();
    }

    public static IReadOnlyList<TInternal> ToInternalList<T, TInternal>(this IReadOnlyList<T> publicList, Func<T, TInternal> converter)
    {
        return publicList.Select(converter).ToArray();
    }

    // Additional missing extensions for list conversions
    public static IReadOnlyList<AdvancedFilter> ToPublicAdvancedFilterList(this IReadOnlyList<CoreTypes.AdvancedFilter> internalList)
    {
        return internalList.Select(ToPublic).ToArray();
    }

    public static IReadOnlyList<CoreTypes.AdvancedFilter> ToInternalAdvancedFilterList(this IReadOnlyList<AdvancedFilter> publicList)
    {
        return publicList.Select(ToInternal).ToArray();
    }

    public static IReadOnlyList<FilterDefinition> ToPublicFilterList(this IReadOnlyList<CoreTypes.FilterDefinition> internalList)
    {
        return internalList.Select(ToPublic).ToArray();
    }

    public static IReadOnlyList<CoreTypes.FilterDefinition> ToInternalFilterList(this IReadOnlyList<FilterDefinition> publicList)
    {
        return publicList.Select(ToInternal).ToArray();
    }

    public static IReadOnlyList<SearchResult> ToPublicSearchResultList(this IReadOnlyList<CoreTypes.SearchResult> internalList)
    {
        return internalList.Select(ToPublic).ToArray();
    }

    public static IReadOnlyList<SortColumnConfiguration> ToPublicSortList(this IReadOnlyList<CoreTypes.SortColumnConfiguration> internalList)
    {
        return internalList.Select(ToPublic).ToArray();
    }

    public static IReadOnlyList<CoreTypes.SortColumnConfiguration> ToInternalSortList(this IReadOnlyList<SortColumnConfiguration> publicList)
    {
        return publicList.Select(ToInternal).ToArray();
    }

    // Missing ToPublic conversion for AdvancedFilter
    public static AdvancedFilter ToPublic(this CoreTypes.AdvancedFilter filter) => new()
    {
        ColumnName = filter.ColumnName,
        Operator = (FilterOperator)(int)filter.Operator,
        Value = filter.Value,
        SecondValue = filter.SecondValue,
        LogicOperator = (FilterLogicOperator)(int)filter.LogicOperator,
        GroupStart = filter.GroupStart,
        GroupEnd = filter.GroupEnd,
        FilterName = filter.FilterName
    };

    // Direction conversions
    public static CoreTypes.SortDirection ToInternal(this SortDirection direction) => (CoreTypes.SortDirection)(int)direction;
    public static SortDirection ToPublic(this CoreTypes.SortDirection direction) => (SortDirection)(int)direction;

    // Mode conversions
    public static CoreTypes.ImportMode ToInternal(this ImportMode mode) => (CoreTypes.ImportMode)(int)mode;
    public static ImportMode ToPublic(this CoreTypes.ImportMode mode) => (ImportMode)(int)mode;

    // Logic operator conversions
    public static CoreTypes.FilterLogicOperator ToInternal(this FilterLogicOperator op) => (CoreTypes.FilterLogicOperator)(int)op;
    public static FilterLogicOperator ToPublic(this CoreTypes.FilterLogicOperator op) => (FilterLogicOperator)(int)op;

    // AutoRowHeight conversion helpers for architectural debt resolution
    public static Dictionary<int, double> ToHeightDictionary(this CoreTypes.RowHeightCalculationResult[] results)
    {
        return results.ToDictionary(r => r.RowIndex, r => r.CalculatedHeight);
    }

    public static Dictionary<int, double> ToHeightDictionary(this IReadOnlyList<CoreTypes.RowHeightCalculationResult> results)
    {
        return results.ToDictionary(r => r.RowIndex, r => r.CalculatedHeight);
    }

    public static Dictionary<int, double> ToHeightDictionary(this List<CoreTypes.RowHeightCalculationResult> results)
    {
        return results.ToDictionary(r => r.RowIndex, r => r.CalculatedHeight);
    }

    // AutoRowHeight Extensions
    public static CoreTypes.AutoRowHeightConfiguration ToInternal(this AutoRowHeightConfiguration config) => new()
    {
        IsEnabled = config.IsEnabled,
        MinimumRowHeight = config.MinimumRowHeight,
        MaximumRowHeight = config.MaximumRowHeight,
        CellPadding = new Microsoft.UI.Xaml.Thickness(config.CellPadding),
        TextWrapping = config.TextWrapping ? Microsoft.UI.Xaml.TextWrapping.Wrap : Microsoft.UI.Xaml.TextWrapping.NoWrap,
        EnableTextTrimming = config.EnableTextTrimming,
        FontSize = config.FontSize,
        FontFamily = config.FontFamily,
        LineHeight = config.LineHeight,
        EnableMeasurementCache = config.EnableMeasurementCache
    };

    public static CoreTypes.RowHeightCalculationOptions ToInternal(this RowHeightCalculationOptions options) => new()
    {
        MaximumRowHeight = options.MaximumRowHeight,
        MinimumRowHeight = options.MinimumRowHeight,
        UseCache = options.UseCache,
        SpecificColumns = options.SpecificColumns,
        Progress = options.Progress != null ? new CoreProgressAdapter(options.Progress) : null
    };

    public static AutoRowHeightResult ToPublic(this CoreTypes.AutoRowHeightResult result) => new()
    {
        Success = result.Success,
        CalculatedHeights = result.CalculatedHeights.ToHeightDictionary(),
        CalculationTime = result.TotalCalculationTime,
        ErrorMessage = result.ErrorMessage
    };

    // Progress adapter to convert IProgress<double> to IProgress<BatchCalculationProgress>
    private sealed class CoreProgressAdapter : IProgress<CoreTypes.BatchCalculationProgress>
    {
        private readonly IProgress<double> _progress;

        public CoreProgressAdapter(IProgress<double> progress)
        {
            _progress = progress;
        }

        public void Report(CoreTypes.BatchCalculationProgress value)
        {
            _progress.Report(value.CompletionPercentage / 100.0);
        }
    }

    // Progress adapter for Import operations
    private sealed class ImportProgressAdapter : IProgress<CoreTypes.ImportProgress>
    {
        private readonly IProgress<double> _progress;

        public ImportProgressAdapter(IProgress<double> progress)
        {
            _progress = progress;
        }

        public void Report(CoreTypes.ImportProgress value)
        {
            _progress.Report(value.CompletionPercentage / 100.0);
        }
    }

    // Progress adapter for Export operations
    private sealed class ExportProgressAdapter : IProgress<CoreTypes.ExportProgress>
    {
        private readonly IProgress<double> _progress;

        public ExportProgressAdapter(IProgress<double> progress)
        {
            _progress = progress;
        }

        public void Report(CoreTypes.ExportProgress value)
        {
            _progress.Report(value.CompletionPercentage / 100.0);
        }
    }
}
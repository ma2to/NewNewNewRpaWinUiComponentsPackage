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
        ColumnMapping = options.ColumnMapping
    };

    public static CoreTypes.ExportOptions ToInternal(this ExportOptions options) => new()
    {
        IncludeHeaders = options.IncludeHeaders,
        ColumnsToExport = options.ColumnsToExport,
        DateTimeFormat = options.DateTimeFormat
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
        Message = result.Message,
        RuleName = result.RuleName,
        ValidationTime = result.ValidationTime,
        Value = result.Value,
        IsTimeout = result.IsTimeout
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
}
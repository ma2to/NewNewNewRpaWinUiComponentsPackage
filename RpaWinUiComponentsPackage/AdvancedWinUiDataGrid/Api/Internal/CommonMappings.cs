using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Internal;

/// <summary>
/// Mapping extensions for common types (Result, enums, etc.)
/// </summary>
internal static class CommonMappings
{
    #region Result Mappings

    public static PublicResult ToPublic(this Result result)
    {
        return result.IsSuccess
            ? PublicResult.Success()
            : PublicResult.Failure(result.ErrorMessage ?? "Operation failed");
    }

    public static PublicResult<T> ToPublic<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? PublicResult<T>.Success(result.Value)
            : PublicResult<T>.Failure(result.ErrorMessage ?? "Operation failed");
    }

    #endregion

    #region Enum Mappings

    public static PublicSpecialColumnType ToPublic(this SpecialColumnType type)
    {
        return type switch
        {
            SpecialColumnType.None => PublicSpecialColumnType.None,
            SpecialColumnType.RowNumber => PublicSpecialColumnType.RowNumber,
            SpecialColumnType.Checkbox => PublicSpecialColumnType.Checkbox,
            SpecialColumnType.ValidationAlerts => PublicSpecialColumnType.ValidationAlerts,
            _ => PublicSpecialColumnType.None
        };
    }

    public static SpecialColumnType ToInternal(this PublicSpecialColumnType type)
    {
        return type switch
        {
            PublicSpecialColumnType.None => SpecialColumnType.None,
            PublicSpecialColumnType.RowNumber => SpecialColumnType.RowNumber,
            PublicSpecialColumnType.Checkbox => SpecialColumnType.Checkbox,
            PublicSpecialColumnType.ValidationAlerts => SpecialColumnType.ValidationAlerts,
            _ => SpecialColumnType.None
        };
    }

    public static PublicFilterOperator ToPublic(this FilterOperator op)
    {
        return op switch
        {
            FilterOperator.Equals => PublicFilterOperator.Equals,
            FilterOperator.NotEquals => PublicFilterOperator.NotEquals,
            FilterOperator.Contains => PublicFilterOperator.Contains,
            FilterOperator.NotContains => PublicFilterOperator.NotContains,
            FilterOperator.StartsWith => PublicFilterOperator.StartsWith,
            FilterOperator.EndsWith => PublicFilterOperator.EndsWith,
            FilterOperator.GreaterThan => PublicFilterOperator.GreaterThan,
            FilterOperator.LessThan => PublicFilterOperator.LessThan,
            FilterOperator.GreaterThanOrEqual => PublicFilterOperator.GreaterThanOrEqual,
            FilterOperator.LessThanOrEqual => PublicFilterOperator.LessThanOrEqual,
            FilterOperator.IsNull => PublicFilterOperator.IsNull,
            FilterOperator.IsNotNull => PublicFilterOperator.IsNotNull,
            _ => PublicFilterOperator.Equals
        };
    }

    public static FilterOperator ToInternal(this PublicFilterOperator op)
    {
        return op switch
        {
            PublicFilterOperator.Equals => FilterOperator.Equals,
            PublicFilterOperator.NotEquals => FilterOperator.NotEquals,
            PublicFilterOperator.Contains => FilterOperator.Contains,
            PublicFilterOperator.NotContains => FilterOperator.NotContains,
            PublicFilterOperator.StartsWith => FilterOperator.StartsWith,
            PublicFilterOperator.EndsWith => FilterOperator.EndsWith,
            PublicFilterOperator.GreaterThan => FilterOperator.GreaterThan,
            PublicFilterOperator.LessThan => FilterOperator.LessThan,
            PublicFilterOperator.GreaterThanOrEqual => FilterOperator.GreaterThanOrEqual,
            PublicFilterOperator.LessThanOrEqual => FilterOperator.LessThanOrEqual,
            PublicFilterOperator.IsNull => FilterOperator.IsNull,
            PublicFilterOperator.IsNotNull => FilterOperator.IsNotNull,
            PublicFilterOperator.InRange => FilterOperator.GreaterThanOrEqual, // Map to range check
            _ => FilterOperator.Equals
        };
    }

    public static PublicValidationSeverity ToPublic(this ValidationSeverity severity)
    {
        return severity switch
        {
            ValidationSeverity.Info => PublicValidationSeverity.Info,
            ValidationSeverity.Warning => PublicValidationSeverity.Warning,
            ValidationSeverity.Error => PublicValidationSeverity.Error,
            ValidationSeverity.Critical => PublicValidationSeverity.Critical,
            _ => PublicValidationSeverity.Error
        };
    }

    public static ValidationSeverity ToInternal(this PublicValidationSeverity severity)
    {
        return severity switch
        {
            PublicValidationSeverity.Info => ValidationSeverity.Info,
            PublicValidationSeverity.Warning => ValidationSeverity.Warning,
            PublicValidationSeverity.Error => ValidationSeverity.Error,
            PublicValidationSeverity.Critical => ValidationSeverity.Critical,
            _ => ValidationSeverity.Error
        };
    }

    public static PublicDataGridOperationMode ToPublic(this DataGridOperationMode mode)
    {
        return mode switch
        {
            DataGridOperationMode.UI => PublicDataGridOperationMode.Interactive,
            DataGridOperationMode.Headless => PublicDataGridOperationMode.Headless,
            _ => PublicDataGridOperationMode.Interactive
        };
    }

    public static DataGridOperationMode ToInternal(this PublicDataGridOperationMode mode)
    {
        return mode switch
        {
            PublicDataGridOperationMode.Interactive => DataGridOperationMode.UI,
            PublicDataGridOperationMode.Readonly => DataGridOperationMode.UI, // Map to UI
            PublicDataGridOperationMode.Headless => DataGridOperationMode.Headless,
            _ => DataGridOperationMode.UI
        };
    }

    public static PublicValidationStrategy ToPublic(this ValidationStrategy strategy)
    {
        return strategy switch
        {
            ValidationStrategy.Automatic => PublicValidationStrategy.OnInput,
            ValidationStrategy.Manual => PublicValidationStrategy.Manual,
            ValidationStrategy.RealTime => PublicValidationStrategy.OnInput,
            ValidationStrategy.Batch => PublicValidationStrategy.OnSubmit,
            _ => PublicValidationStrategy.OnInput
        };
    }

    public static ValidationStrategy ToInternal(this PublicValidationStrategy strategy)
    {
        return strategy switch
        {
            PublicValidationStrategy.OnInput => ValidationStrategy.RealTime,
            PublicValidationStrategy.OnBlur => ValidationStrategy.RealTime,
            PublicValidationStrategy.OnSubmit => ValidationStrategy.Batch,
            PublicValidationStrategy.Manual => ValidationStrategy.Manual,
            _ => ValidationStrategy.Automatic
        };
    }

    public static PublicAutoRowHeightMode ToPublic(this AutoRowHeightMode mode)
    {
        return mode switch
        {
            AutoRowHeightMode.Disabled => PublicAutoRowHeightMode.Disabled,
            AutoRowHeightMode.Enabled => PublicAutoRowHeightMode.Enabled,
            AutoRowHeightMode.Auto => PublicAutoRowHeightMode.Smart,
            _ => PublicAutoRowHeightMode.Disabled
        };
    }

    public static AutoRowHeightMode ToInternal(this PublicAutoRowHeightMode mode)
    {
        return mode switch
        {
            PublicAutoRowHeightMode.Disabled => AutoRowHeightMode.Disabled,
            PublicAutoRowHeightMode.Enabled => AutoRowHeightMode.Enabled,
            PublicAutoRowHeightMode.Smart => AutoRowHeightMode.Auto,
            _ => AutoRowHeightMode.Disabled
        };
    }

    public static PublicImportMode ToPublic(this ImportMode mode)
    {
        return mode switch
        {
            ImportMode.Append => PublicImportMode.Append,
            ImportMode.Replace => PublicImportMode.Replace,
            ImportMode.Merge => PublicImportMode.Merge,
            _ => PublicImportMode.Append
        };
    }

    public static ImportMode ToInternal(this PublicImportMode mode)
    {
        return mode switch
        {
            PublicImportMode.Append => ImportMode.Append,
            PublicImportMode.Replace => ImportMode.Replace,
            PublicImportMode.Merge => ImportMode.Merge,
            _ => ImportMode.Append
        };
    }

    public static PublicExportFormat ToPublic(this ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Dictionary => PublicExportFormat.Dictionary,
            ExportFormat.DataTable => PublicExportFormat.DataTable,
            _ => PublicExportFormat.Dictionary
        };
    }

    public static ExportFormat ToInternal(this PublicExportFormat format)
    {
        return format switch
        {
            PublicExportFormat.Dictionary => ExportFormat.Dictionary,
            PublicExportFormat.DataTable => ExportFormat.DataTable,
            _ => ExportFormat.Dictionary
        };
    }

    public static PublicClipboardFormat ToPublic(this ClipboardFormat format)
    {
        return PublicClipboardFormat.Excel; // Always Excel for clipboard compatibility
    }

    public static ClipboardFormat ToInternal(this PublicClipboardFormat format)
    {
        return ClipboardFormat.TabSeparated; // Tab-separated for Excel compatibility
    }

    #endregion

    #region AutoRowHeight Mappings

    public static AutoRowHeightConfiguration ToInternal(this PublicAutoRowHeightConfiguration config)
    {
        return new AutoRowHeightConfiguration(
            IsEnabled: config.IsEnabled,
            MinimumRowHeight: config.MinimumRowHeight,
            MaximumRowHeight: config.MaximumRowHeight,
            DefaultFontFamily: config.DefaultFontFamily,
            DefaultFontSize: config.DefaultFontSize,
            EnableTextWrapping: config.EnableTextWrapping,
            UseCache: config.UseCache,
            CacheMaxSize: config.CacheMaxSize,
            CalculationTimeout: config.CalculationTimeoutSeconds.HasValue
                ? TimeSpan.FromSeconds(config.CalculationTimeoutSeconds.Value)
                : null
        );
    }

    public static PublicAutoRowHeightResult ToPublic(this AutoRowHeightResult result)
    {
        return new PublicAutoRowHeightResult(
            IsSuccess: result.IsSuccess,
            ErrorMessage: result.ErrorMessage,
            DurationMs: result.Duration?.TotalMilliseconds,
            AffectedRows: result.AffectedRows
        );
    }

    public static PublicRowHeightCalculationResult ToPublic(this RowHeightCalculationResult result)
    {
        return new PublicRowHeightCalculationResult(
            RowIndex: result.RowIndex,
            CalculatedHeight: result.CalculatedHeight,
            IsSuccess: result.IsSuccess,
            ErrorMessage: result.ErrorMessage,
            CalculationTimeMs: result.CalculationTime?.TotalMilliseconds
        );
    }

    public static List<PublicRowHeightCalculationResult> ToPublicList(this IEnumerable<RowHeightCalculationResult> results)
    {
        return results.Select(r => r.ToPublic()).ToList();
    }

    public static PublicTextMeasurementResult ToPublic(this TextMeasurementResult result)
    {
        return new PublicTextMeasurementResult(
            Width: result.Width,
            Height: result.Height,
            MeasuredText: result.MeasuredText,
            FontFamily: result.FontFamily,
            FontSize: result.FontSize,
            TextWrapped: result.TextWrapped
        );
    }

    public static RowHeightCalculationOptions? ToInternal(this PublicRowHeightCalculationOptions? options)
    {
        if (options == null) return null;

        return new RowHeightCalculationOptions(
            MinHeight: options.MinHeight,
            MaxHeight: options.MaxHeight,
            FontFamily: options.FontFamily,
            FontSize: options.FontSize,
            EnableWrapping: options.EnableWrapping
        );
    }

    public static PublicAutoRowHeightStatistics ToPublic(this AutoRowHeightStatistics stats)
    {
        return new PublicAutoRowHeightStatistics(
            TotalCalculations: stats.TotalCalculations,
            CachedCalculations: stats.CachedCalculations,
            FailedCalculations: stats.FailedCalculations,
            TotalCalculationTimeMs: stats.TotalCalculationTime.TotalMilliseconds,
            AverageCalculationTimeMs: stats.AverageCalculationTime.TotalMilliseconds,
            CacheHitRate: stats.CacheHitRate,
            CurrentCacheSize: stats.CurrentCacheSize
        );
    }

    public static PublicCacheStatistics ToPublic(this CacheStatistics stats)
    {
        return new PublicCacheStatistics(
            TotalEntries: stats.TotalEntries,
            MaxSize: stats.MaxSize,
            HitRate: stats.HitRate,
            MissRate: stats.MissRate,
            MemoryUsageBytes: stats.MemoryUsageBytes,
            OldestEntryAgeMs: stats.OldestEntry.TotalMilliseconds,
            RecentEvictions: stats.RecentEvictions
        );
    }

    #endregion
}

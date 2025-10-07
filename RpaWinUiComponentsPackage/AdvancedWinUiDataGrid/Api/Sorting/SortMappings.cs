using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Sorting;

/// <summary>
/// Mapping extensions pre konverziu medzi public a internal sort types
/// </summary>
internal static class SortMappings
{
    #region Public → Internal

    /// <summary>
    /// Konvertuje public sort direction (Api.Models) na internal Core.ValueObjects.SortDirection
    /// </summary>
    internal static Core.ValueObjects.SortDirection ToInternal(this Api.Models.PublicSortDirection direction) => direction switch
    {
        Api.Models.PublicSortDirection.None => Core.ValueObjects.SortDirection.None,
        Api.Models.PublicSortDirection.Ascending => Core.ValueObjects.SortDirection.Ascending,
        Api.Models.PublicSortDirection.Descending => Core.ValueObjects.SortDirection.Descending,
        _ => Core.ValueObjects.SortDirection.Ascending
    };

    /// <summary>
    /// Konvertuje public performance mode na internal
    /// </summary>
    internal static SortPerformanceMode ToInternal(this PublicSortPerformanceMode mode) => mode switch
    {
        PublicSortPerformanceMode.Auto => SortPerformanceMode.Auto,
        PublicSortPerformanceMode.Sequential => SortPerformanceMode.Sequential,
        PublicSortPerformanceMode.Parallel => SortPerformanceMode.Parallel,
        PublicSortPerformanceMode.Optimized => SortPerformanceMode.Optimized,
        _ => SortPerformanceMode.Auto
    };

    /// <summary>
    /// Konvertuje public sort column config na internal
    /// </summary>
    internal static SortColumnConfiguration ToInternal(this SortColumnConfig config)
    {
        var direction = SortMappings.ToInternal(config.Direction);
        return SortColumnConfiguration.Create(
            config.ColumnName,
            direction,
            config.Priority
        ) with
        {
            CaseSensitive = config.CaseSensitive
        };
    }

    /// <summary>
    /// Konvertuje public sort command na internal
    /// </summary>
    internal static SortCommand ToInternal(this SortDataCommand command)
    {
        var direction = SortMappings.ToInternal(command.Direction);
        return new()
        {
            Data = command.Data,
            ColumnName = command.ColumnName,
            Direction = direction,
            CaseSensitive = command.CaseSensitive,
            PerformanceMode = command.PerformanceMode.ToInternal(),
            Timeout = command.Timeout
        };
    }

    /// <summary>
    /// Konvertuje public multi-sort command na internal
    /// </summary>
    internal static MultiSortCommand ToInternal(this MultiSortDataCommand command) =>
        new()
        {
            Data = command.Data,
            SortColumns = command.SortColumns.Select(c => c.ToInternal()).ToList(),
            PerformanceMode = command.PerformanceMode.ToInternal(),
            Timeout = command.Timeout
        };

    #endregion

    #region Internal → Public

    /// <summary>
    /// Konvertuje internal Core.ValueObjects.SortDirection na public (Api.Models namespace)
    /// </summary>
    internal static Api.Models.PublicSortDirection ToPublic(this Core.ValueObjects.SortDirection direction) => direction switch
    {
        Core.ValueObjects.SortDirection.None => Api.Models.PublicSortDirection.None,
        Core.ValueObjects.SortDirection.Ascending => Api.Models.PublicSortDirection.Ascending,
        Core.ValueObjects.SortDirection.Descending => Api.Models.PublicSortDirection.Descending,
        _ => Api.Models.PublicSortDirection.Ascending
    };

    /// <summary>
    /// Konvertuje internal performance mode na public
    /// </summary>
    internal static PublicSortPerformanceMode ToPublic(this SortPerformanceMode mode) => mode switch
    {
        SortPerformanceMode.Auto => PublicSortPerformanceMode.Auto,
        SortPerformanceMode.Sequential => PublicSortPerformanceMode.Sequential,
        SortPerformanceMode.Parallel => PublicSortPerformanceMode.Parallel,
        SortPerformanceMode.Optimized => PublicSortPerformanceMode.Optimized,
        _ => PublicSortPerformanceMode.Auto
    };

    /// <summary>
    /// Konvertuje internal sort progress na public
    /// </summary>
    internal static SortProgress ToPublic(this Core.ValueObjects.SortProgress progress) =>
        new(
            progress.ProcessedRows,
            progress.TotalRows,
            progress.ElapsedTime,
            progress.CurrentOperation,
            progress.CurrentColumn
        );

    /// <summary>
    /// Konvertuje internal sort result na public
    /// </summary>
    internal static SortDataResult ToPublic(this SortResult result) =>
        new(
            IsSuccess: result.Success,
            SortedData: result.SortedData,
            ProcessedRows: result.ProcessedRows,
            Duration: result.SortTime,
            UsedParallelProcessing: result.UsedParallelProcessing,
            ErrorMessages: result.ErrorMessages
        );

    /// <summary>
    /// Konvertuje internal SortColumnConfiguration na public PublicSortDescriptor
    /// </summary>
    internal static Api.Models.PublicSortDescriptor ToPublic(this SortColumnConfiguration config) =>
        new()
        {
            ColumnName = config.ColumnName,
            Direction = config.Direction.ToPublic(),
            Priority = config.Priority
        };

    /// <summary>
    /// Konvertuje public PublicSortDescriptor na internal SortColumnConfiguration
    /// </summary>
    internal static SortColumnConfiguration ToInternal(this Api.Models.PublicSortDescriptor descriptor) =>
        SortColumnConfiguration.Create(
            descriptor.ColumnName,
            descriptor.Direction.ToInternal(),
            descriptor.Priority
        );

    #endregion
}

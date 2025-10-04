using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Internal;

/// <summary>
/// Mapping extensions pre konverziu medzi public a internal sort types
/// </summary>
internal static class SortMappings
{
    #region Public → Internal

    /// <summary>
    /// Konvertuje public sort direction na internal
    /// </summary>
    internal static SortDirection ToInternal(this PublicSortDirection direction) => direction switch
    {
        PublicSortDirection.None => SortDirection.None,
        PublicSortDirection.Ascending => SortDirection.Ascending,
        PublicSortDirection.Descending => SortDirection.Descending,
        _ => SortDirection.Ascending
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
    internal static SortColumnConfiguration ToInternal(this SortColumnConfig config) =>
        SortColumnConfiguration.Create(
            config.ColumnName,
            config.Direction.ToInternal(),
            config.Priority
        ) with
        {
            CaseSensitive = config.CaseSensitive
        };

    /// <summary>
    /// Konvertuje public sort command na internal
    /// </summary>
    internal static SortCommand ToInternal(this SortDataCommand command) =>
        new()
        {
            Data = command.Data,
            ColumnName = command.ColumnName,
            Direction = command.Direction.ToInternal(),
            CaseSensitive = command.CaseSensitive,
            PerformanceMode = command.PerformanceMode.ToInternal(),
            Timeout = command.Timeout
        };

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
    /// Konvertuje internal sort direction na public
    /// </summary>
    internal static PublicSortDirection ToPublic(this SortDirection direction) => direction switch
    {
        SortDirection.None => PublicSortDirection.None,
        SortDirection.Ascending => PublicSortDirection.Ascending,
        SortDirection.Descending => PublicSortDirection.Descending,
        _ => PublicSortDirection.Ascending
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

    #endregion
}

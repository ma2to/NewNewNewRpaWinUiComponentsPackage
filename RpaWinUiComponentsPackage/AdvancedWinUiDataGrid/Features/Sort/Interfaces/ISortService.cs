using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Interfaces;

/// <summary>
/// Interface for sort service with comprehensive sorting capabilities
/// Combines legacy API and new command pattern API
/// </summary>
internal interface ISortService
{
    // NOVÉ COMMAND PATTERN API

    /// <summary>
    /// Performs single-column sorting (command pattern)
    /// </summary>
    Task<SortResult> SortAsync(SortCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs multi-column sorting (command pattern)
    /// </summary>
    Task<SortResult> MultiSortAsync(MultiSortCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs advanced sort with business rules (command pattern)
    /// </summary>
    Task<SortResult> AdvancedSortAsync(AdvancedSortCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quick sort for immediate results (synchronous)
    /// </summary>
    SortResult QuickSort(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName, CoreTypes.SortDirection direction = CoreTypes.SortDirection.Ascending);

    // LEGACY API (backward compatibility)

    /// <summary>
    /// Performs sort by column (legacy compatibility)
    /// </summary>
    Task<bool> SortByColumnAsync(string columnName, CoreTypes.SortDirection direction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears current sort settings
    /// </summary>
    Task<bool> ClearSortAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current sort settings
    /// </summary>
    IReadOnlyList<(string ColumnName, CoreTypes.SortDirection Direction)> GetCurrentSort();

    /// <summary>
    /// Indicator whether data is sorted
    /// </summary>
    bool IsSorted();

    // UTILITY API

    /// <summary>
    /// Gets sortable columns from data
    /// </summary>
    IReadOnlyList<string> GetSortableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data);

    /// <summary>
    /// Recommends optimal performance mode
    /// </summary>
    CoreTypes.SortPerformanceMode GetRecommendedPerformanceMode(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<CoreTypes.SortColumnConfiguration> sortColumns);

    /// <summary>
    /// Validates sort configuration
    /// </summary>
    Task<Common.Models.Result> ValidateSortConfigurationAsync(CoreTypes.AdvancedSortConfiguration sortConfiguration, CancellationToken cancellationToken = default);

    // PUBLIC API COMPATIBILITY METHODS

    /// <summary>
    /// Sort by multiple columns (public API compatibility)
    /// </summary>
    Task<Common.Models.Result> SortByMultipleColumnsAsync(IReadOnlyList<CoreTypes.SortColumnConfiguration> sortDescriptors, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear sorting (public API compatibility)
    /// </summary>
    Task<Common.Models.Result> ClearSortingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current sort descriptors (public API compatibility)
    /// </summary>
    IReadOnlyList<CoreTypes.SortColumnConfiguration> GetCurrentSortDescriptors();

    /// <summary>
    /// Toggle sort direction for column (public API compatibility)
    /// </summary>
    Task<Common.Models.Result<CoreTypes.SortDirection>> ToggleSortDirectionAsync(string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if column is sorted (public API compatibility)
    /// </summary>
    bool IsColumnSorted(string columnName);

    /// <summary>
    /// Get column sort direction (public API compatibility)
    /// </summary>
    CoreTypes.SortDirection GetColumnSortDirection(string columnName);
}

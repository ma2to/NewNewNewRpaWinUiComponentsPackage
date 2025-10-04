using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Sort.Interfaces;

/// <summary>
/// Interface pre sort službu s comprehensive sorting capabilities
/// Kombinuje legacy API aj nové command pattern API
/// </summary>
internal interface ISortService
{
    // NOVÉ COMMAND PATTERN API

    /// <summary>
    /// Vykoná jednokolónkové triedenie (command pattern)
    /// </summary>
    Task<SortResult> SortAsync(SortCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vykoná multi-column triedenie (command pattern)
    /// </summary>
    Task<SortResult> MultiSortAsync(MultiSortCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vykoná advanced sort s business rules (command pattern)
    /// </summary>
    Task<SortResult> AdvancedSortAsync(AdvancedSortCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quick sort pre okamžité výsledky (synchronous)
    /// </summary>
    SortResult QuickSort(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName, CoreTypes.SortDirection direction = CoreTypes.SortDirection.Ascending);

    // LEGACY API (backward compatibility)

    /// <summary>
    /// Vykoná sort podľa stĺpca (legacy compatibility)
    /// </summary>
    Task<bool> SortByColumnAsync(string columnName, CoreTypes.SortDirection direction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vyčistí aktuálne sort nastavenia
    /// </summary>
    Task<bool> ClearSortAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Získa aktuálne sort nastavenia
    /// </summary>
    IReadOnlyList<(string ColumnName, CoreTypes.SortDirection Direction)> GetCurrentSort();

    /// <summary>
    /// Indikátor či sú dáta sortované
    /// </summary>
    bool IsSorted();

    // UTILITY API

    /// <summary>
    /// Získa sortovateľné stĺpce z dát
    /// </summary>
    IReadOnlyList<string> GetSortableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data);

    /// <summary>
    /// Odporúči optimálny performance mode
    /// </summary>
    CoreTypes.SortPerformanceMode GetRecommendedPerformanceMode(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<CoreTypes.SortColumnConfiguration> sortColumns);

    /// <summary>
    /// Validuje sort konfiguráciu
    /// </summary>
    Task<Common.Models.Result> ValidateSortConfigurationAsync(CoreTypes.AdvancedSortConfiguration sortConfiguration, CancellationToken cancellationToken = default);
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Sort functionality
/// CLEAN ARCHITECTURE: Application layer interface for sort operations
/// </summary>
internal interface ISortService
{
    // Single column sort
    Task<SortResult> SortAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        SortDirection direction = SortDirection.Ascending,
        CancellationToken cancellationToken = default);

    SortResult Sort(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        string columnName,
        SortDirection direction = SortDirection.Ascending);

    // Multi-column sort
    Task<SortResult> MultiSortAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<SortColumnConfiguration> sortConfigurations,
        CancellationToken cancellationToken = default);

    SortResult MultiSort(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        IReadOnlyList<SortColumnConfiguration> sortConfigurations);

    // Custom sort operations
    Task<SortResult> SortWithConfigurationAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        SortConfiguration configuration,
        CancellationToken cancellationToken = default);

    SortResult SortWithConfiguration(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        SortConfiguration configuration);

    // Utility operations
    bool CanSort(string columnName, IEnumerable<IReadOnlyDictionary<string, object?>> data);
    IReadOnlyList<string> GetSortableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data);
}


using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Commands;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Interfaces;

/// <summary>
/// Internal service interface for smart row add/delete operations
/// </summary>
internal interface ISmartOperationService
{
    /// <summary>
    /// Smart add rows with minimum rows management
    /// </summary>
    Task<RowManagementResult> SmartAddRowsAsync(SmartAddRowsInternalCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Smart delete rows with context-aware logic (uses row indices)
    /// </summary>
    Task<RowManagementResult> SmartDeleteRowsAsync(SmartDeleteRowsInternalCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Smart delete rows by stable row IDs (recommended to avoid index shifting bugs)
    /// </summary>
    Task<RowManagementResult> SmartDeleteRowsByIdAsync(SmartDeleteRowsByIdInternalCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-expand empty row maintenance
    /// </summary>
    Task<RowManagementResult> AutoExpandEmptyRowAsync(AutoExpandEmptyRowInternalCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate row management configuration
    /// </summary>
    Task<Result> ValidateRowManagementConfigurationAsync(RowManagementConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current row management statistics
    /// </summary>
    RowManagementStatistics GetRowManagementStatistics();

    /// <summary>
    /// Auto-fill cells with pattern detection and smart fill logic
    /// </summary>
    Task<RowManagementResult> AutoFillAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> currentData,
        int startRowIndex,
        int endRowIndex,
        string columnName,
        CancellationToken cancellationToken = default);
}

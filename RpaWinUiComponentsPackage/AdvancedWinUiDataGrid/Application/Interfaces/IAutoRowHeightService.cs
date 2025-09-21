using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using CoreTypes = RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Auto row height functionality
/// CLEAN ARCHITECTURE: Application layer interface for auto row height operations
/// </summary>
internal interface IAutoRowHeightService
{
    // Auto row height operations
    Task<CoreTypes.AutoRowHeightResult> EnableAutoRowHeightAsync(
        CoreTypes.AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<CoreTypes.AutoRowHeightResult> CalculateOptimalRowHeightsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        CoreTypes.RowHeightCalculationOptions options,
        CancellationToken cancellationToken = default);

    // Single row operations
    Task<CoreTypes.RowHeightCalculationResult> CalculateRowHeightAsync(
        IReadOnlyDictionary<string, object?> rowData,
        int rowIndex,
        CoreTypes.AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    // Text measurement operations
    Task<CoreTypes.TextMeasurementResult> MeasureTextAsync(
        string text,
        double maxWidth,
        CoreTypes.AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    // Configuration operations
    Task ApplyConfigurationAsync(
        CoreTypes.AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    CoreTypes.AutoRowHeightConfiguration GetCurrentConfiguration();

    // Utility operations
    bool IsAutoRowHeightEnabled();
    Task InvalidateHeightCacheAsync(CancellationToken cancellationToken = default);
}


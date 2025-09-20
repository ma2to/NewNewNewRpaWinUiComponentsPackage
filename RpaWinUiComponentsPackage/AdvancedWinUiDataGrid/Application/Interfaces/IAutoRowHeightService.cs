using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// INTERNAL INTERFACE: Auto row height functionality
/// CLEAN ARCHITECTURE: Application layer interface for auto row height operations
/// </summary>
internal interface IAutoRowHeightService
{
    // Auto row height operations
    Task<AutoRowHeightResult> EnableAutoRowHeightAsync(
        AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<AutoRowHeightResult> CalculateOptimalRowHeightsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        RowHeightCalculationOptions options,
        CancellationToken cancellationToken = default);

    // Single row operations
    Task<RowHeightCalculationResult> CalculateRowHeightAsync(
        IReadOnlyDictionary<string, object?> rowData,
        int rowIndex,
        AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    // Text measurement operations
    Task<TextMeasurementResult> MeasureTextAsync(
        string text,
        double maxWidth,
        AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    // Configuration operations
    Task ApplyConfigurationAsync(
        AutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    AutoRowHeightConfiguration GetCurrentConfiguration();

    // Utility operations
    bool IsAutoRowHeightEnabled();
    Task InvalidateHeightCacheAsync(CancellationToken cancellationToken = default);
}


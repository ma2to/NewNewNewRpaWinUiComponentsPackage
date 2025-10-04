using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Interfaces;

/// <summary>
/// Interface for color management service
/// </summary>
internal interface IColorService
{
    /// <summary>
    /// Apply color to cells/rows/columns
    /// </summary>
    Task<ColorResult> ApplyColorAsync(ApplyColorCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply conditional formatting rules
    /// </summary>
    Task<ColorResult> ApplyConditionalFormattingAsync(ApplyConditionalFormattingCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear color from cells/rows/columns
    /// </summary>
    Task<ColorResult> ClearColorAsync(ClearColorCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all colored cells
    /// </summary>
    IReadOnlyDictionary<string, ColorConfiguration> GetColoredCells();

    /// <summary>
    /// Validate color configuration
    /// </summary>
    Task<Result> ValidateColorConfigurationAsync(ColorConfiguration config, CancellationToken cancellationToken = default);
}

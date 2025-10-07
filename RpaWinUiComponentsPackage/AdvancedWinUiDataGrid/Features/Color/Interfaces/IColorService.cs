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

    /// <summary>
    /// Set color for specific element state property
    /// </summary>
    Task SetElementStatePropertyColorAsync(string element, string state, string property, string color, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get color for specific element state property
    /// </summary>
    Task<string> GetElementStatePropertyColorAsync(string element, string state, string property, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear color for specific element state property
    /// </summary>
    Task ClearElementStatePropertyColorAsync(string element, string state, string property, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get default color for specific element state property
    /// </summary>
    Task<string> GetDefaultColorAsync(string element, string state, string property, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable zebra rows
    /// </summary>
    Task EnableZebraRowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disable zebra rows
    /// </summary>
    Task DisableZebraRowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if zebra rows is enabled
    /// </summary>
    bool IsZebraRowsEnabled();

    /// <summary>
    /// Set zebra row colors
    /// </summary>
    Task SetZebraRowColorsAsync(string evenRowColor, string oddRowColor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get zebra row colors
    /// </summary>
    (string evenRowColor, string oddRowColor) GetZebraRowColors();

    /// <summary>
    /// Reset zebra row colors to default
    /// </summary>
    Task ResetZebraRowColorsToDefaultAsync(CancellationToken cancellationToken = default);

    // Old API compatibility methods
    Task SetCellBackgroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default);
    Task SetCellForegroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default);
    Task SetRowBackgroundColorAsync(int rowIndex, string color, CancellationToken cancellationToken = default);
    Task ClearCellColorsAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default);
    Task ClearAllColorsAsync(CancellationToken cancellationToken = default);
}

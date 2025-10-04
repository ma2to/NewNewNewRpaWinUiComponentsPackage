using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Interfaces;

/// <summary>
/// INTERNAL: UI management service interface
/// THREAD SAFE: All operations are thread-safe
/// ASYNC: Non-blocking UI operations
/// </summary>
internal interface IUIService
{
    /// <summary>
    /// Update UI theme
    /// </summary>
    Task<Result<UIOperationResult>> UpdateThemeAsync(UpdateThemeCommand command);

    /// <summary>
    /// Refresh UI elements
    /// </summary>
    Task<Result<UIOperationResult>> RefreshUIAsync(RefreshUICommand command);

    /// <summary>
    /// Set UI mode
    /// </summary>
    Task<Result<UIOperationResult>> SetUIModeAsync(SetUIModeCommand command);

    /// <summary>
    /// Get current UI state
    /// </summary>
    UIState GetCurrentState();
}

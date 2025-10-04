using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Models;

/// <summary>
/// DDD: UI operation result
/// </summary>
public sealed record UIOperationResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public TimeSpan OperationDuration { get; init; }
    public int AffectedElements { get; init; }

    public static UIOperationResult Successful(TimeSpan duration, int affectedElements = 0) =>
        new() { Success = true, OperationDuration = duration, AffectedElements = affectedElements };

    public static UIOperationResult Failed(string message) =>
        new() { Success = false, Message = message };
}

/// <summary>
/// DDD: UI state snapshot
/// </summary>
internal sealed record UIState
{
    public UIMode CurrentMode { get; init; } = UIMode.Interactive;
    public ThemeMode CurrentTheme { get; init; } = ThemeMode.Light;
    public RenderingMode RenderingMode { get; init; } = RenderingMode.Standard;
    public DateTime LastUpdateTime { get; init; } = DateTime.UtcNow;
    public int TotalElements { get; init; }
    public int VisibleElements { get; init; }
}

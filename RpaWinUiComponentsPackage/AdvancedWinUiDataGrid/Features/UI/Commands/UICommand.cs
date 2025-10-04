using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Commands;

/// <summary>
/// COMMAND PATTERN: Update UI theme command
/// </summary>
internal sealed record UpdateThemeCommand
{
    public required UIThemeConfiguration ThemeConfiguration { get; init; }
    public bool ApplyImmediately { get; init; } = true;
    public CancellationToken CancellationToken { get; init; } = default;

    public static UpdateThemeCommand Create(UIThemeConfiguration theme) =>
        new() { ThemeConfiguration = theme };

    public static UpdateThemeCommand Light() =>
        new() { ThemeConfiguration = UIThemeConfiguration.Light };

    public static UpdateThemeCommand Dark() =>
        new() { ThemeConfiguration = UIThemeConfiguration.Dark };
}

/// <summary>
/// COMMAND PATTERN: Refresh UI command
/// </summary>
internal sealed record RefreshUICommand
{
    public required UIRefreshRequest RefreshRequest { get; init; }
    public bool ForceRedraw { get; init; } = false;
    public CancellationToken CancellationToken { get; init; } = default;

    public static RefreshUICommand Full() =>
        new() { RefreshRequest = new UIRefreshRequest { Scope = UIRefreshScope.Full } };

    public static RefreshUICommand Partial(IReadOnlyList<string> targetElements) =>
        new() { RefreshRequest = new UIRefreshRequest { Scope = UIRefreshScope.Partial, TargetElements = targetElements } };
}

/// <summary>
/// COMMAND PATTERN: Set UI mode command
/// </summary>
internal sealed record SetUIModeCommand
{
    public required UIMode Mode { get; init; }
    public CancellationToken CancellationToken { get; init; } = default;

    public static SetUIModeCommand Interactive() =>
        new() { Mode = UIMode.Interactive };

    public static SetUIModeCommand ReadOnly() =>
        new() { Mode = UIMode.ReadOnly };

    public static SetUIModeCommand Disabled() =>
        new() { Mode = UIMode.Disabled };
}

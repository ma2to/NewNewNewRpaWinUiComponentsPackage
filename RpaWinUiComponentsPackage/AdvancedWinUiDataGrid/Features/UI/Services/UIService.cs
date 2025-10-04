using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using System.Diagnostics;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.UI.Services;

/// <summary>
/// INTERNAL: UI management service implementation
/// THREAD SAFE: Thread-safe UI operations
/// </summary>
internal sealed class UIService : IUIService
{
    private readonly ILogger<UIService> _logger;
    private readonly IOperationLogger<UIService> _operationLogger;

    private UIMode _currentMode = UIMode.Interactive;
    private ThemeMode _currentTheme = ThemeMode.Light;
    private RenderingMode _renderingMode = RenderingMode.Standard;
    private readonly object _stateLock = new();

    public UIService(
        ILogger<UIService> logger,
        IOperationLogger<UIService> operationLogger)
    {
        _logger = logger;
        _operationLogger = operationLogger;
    }

    public async Task<Result<UIOperationResult>> UpdateThemeAsync(UpdateThemeCommand command)
    {
        using var scope = _operationLogger.LogOperationStart(nameof(UpdateThemeAsync),
            new { theme = command.ThemeConfiguration.Mode });

        try
        {
            var sw = Stopwatch.StartNew();

            lock (_stateLock)
            {
                _currentTheme = command.ThemeConfiguration.Mode;
            }

            _logger.LogInformation("UI theme updated to {Theme}", command.ThemeConfiguration.Mode);

            sw.Stop();
            var result = UIOperationResult.Successful(sw.Elapsed, affectedElements: 1);
            scope.MarkSuccess(result);
            return Result<UIOperationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update UI theme");
            scope.MarkFailure(ex);
            return Result<UIOperationResult>.Failure($"Theme update failed: {ex.Message}");
        }
    }

    public async Task<Result<UIOperationResult>> RefreshUIAsync(RefreshUICommand command)
    {
        using var scope = _operationLogger.LogOperationStart(nameof(RefreshUIAsync),
            new { scope = command.RefreshRequest.Scope });

        try
        {
            var sw = Stopwatch.StartNew();

            // Simulate UI refresh
            await Task.Delay(10, command.CancellationToken);

            _logger.LogInformation("UI refreshed with scope {Scope}", command.RefreshRequest.Scope);

            sw.Stop();
            var result = UIOperationResult.Successful(sw.Elapsed);
            scope.MarkSuccess(result);
            return Result<UIOperationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh UI");
            scope.MarkFailure(ex);
            return Result<UIOperationResult>.Failure($"UI refresh failed: {ex.Message}");
        }
    }

    public async Task<Result<UIOperationResult>> SetUIModeAsync(SetUIModeCommand command)
    {
        using var scope = _operationLogger.LogOperationStart(nameof(SetUIModeAsync),
            new { mode = command.Mode });

        try
        {
            var sw = Stopwatch.StartNew();

            lock (_stateLock)
            {
                _currentMode = command.Mode;
            }

            _logger.LogInformation("UI mode set to {Mode}", command.Mode);

            sw.Stop();
            var result = UIOperationResult.Successful(sw.Elapsed);
            scope.MarkSuccess(result);
            return await Task.FromResult(Result<UIOperationResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set UI mode");
            scope.MarkFailure(ex);
            return Result<UIOperationResult>.Failure($"UI mode change failed: {ex.Message}");
        }
    }

    public UIState GetCurrentState()
    {
        lock (_stateLock)
        {
            return new UIState
            {
                CurrentMode = _currentMode,
                CurrentTheme = _currentTheme,
                RenderingMode = _renderingMode,
                LastUpdateTime = DateTime.UtcNow
            };
        }
    }
}

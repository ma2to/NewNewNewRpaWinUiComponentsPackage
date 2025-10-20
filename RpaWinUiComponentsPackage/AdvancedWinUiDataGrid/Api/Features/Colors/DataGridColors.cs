using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// PUBLIC API: Facade for direct color management.
/// Provides granular control over individual UI element colors without using themes.
/// </summary>
internal sealed class DataGridColors : IDataGridColors
{
    private readonly ILogger<DataGridColors>? _logger;
    private readonly ColorManagementService _colorService;

    public DataGridColors(
        ColorManagementService colorService,
        ILogger<DataGridColors>? logger = null)
    {
        _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
        _logger = logger;
    }

    public async Task SetElementColorAsync(
        UIElementType elementType,
        UIElementState state,
        ColorProperty property,
        string hexColor,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("SetElementColor: {Element}:{State}.{Property} = {Color}",
                elementType, state, property, hexColor);

            await _colorService.SetElementColorAsync(elementType, state, property, hexColor, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetElementColor failed for {Element}:{State}.{Property}",
                elementType, state, property);
            throw;
        }
    }

    public async Task SetElementColorsAsync(
        UIElementType elementType,
        UIElementState state,
        ColorSet colorSet,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("SetElementColors: {Element}:{State}", elementType, state);

            await _colorService.SetElementColorsAsync(elementType, state, colorSet, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetElementColors failed for {Element}:{State}", elementType, state);
            throw;
        }
    }

    public async Task SetMultipleElementColorsAsync(
        IReadOnlyDictionary<(UIElementType ElementType, UIElementState State, ColorProperty Property), string> colorUpdates,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("SetMultipleElementColors: {Count} updates", colorUpdates.Count);

            await _colorService.SetMultipleElementColorsAsync(colorUpdates, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetMultipleElementColors failed");
            throw;
        }
    }

    public async Task<string?> GetElementColorAsync(
        UIElementType elementType,
        UIElementState state,
        ColorProperty property,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _colorService.GetElementColorAsync(elementType, state, property, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetElementColor failed for {Element}:{State}.{Property}",
                elementType, state, property);
            throw;
        }
    }

    public async Task<ColorSet?> GetElementColorsAsync(
        UIElementType elementType,
        UIElementState state,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _colorService.GetElementColorsAsync(elementType, state, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetElementColors failed for {Element}:{State}", elementType, state);
            throw;
        }
    }

    public async Task ResetElementToDefaultAsync(
        UIElementType elementType,
        UIElementState state,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("ResetElementToDefault: {Element}:{State}", elementType, state);

            await _colorService.ResetElementToDefaultAsync(elementType, state, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ResetElementToDefault failed for {Element}:{State}", elementType, state);
            throw;
        }
    }

    public async Task ResetAllToDefaultAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("ResetAllToDefault");

            await _colorService.ResetAllToDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ResetAllToDefault failed");
            throw;
        }
    }

    public async Task<ComprehensiveColorTheme> GetCurrentThemeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _colorService.GetCurrentThemeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetCurrentTheme failed");
            throw;
        }
    }
}

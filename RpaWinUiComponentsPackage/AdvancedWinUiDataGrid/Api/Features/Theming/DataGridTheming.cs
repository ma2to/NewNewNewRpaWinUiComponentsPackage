using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Theming;

/// <summary>
/// Internal implementation of DataGrid theming operations.
/// Delegates to internal theming service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridTheming : IDataGridTheming
{
    private readonly ILogger<DataGridTheming>? _logger;
    private readonly IColorService _colorService;
    private readonly ThemeService _themeService;

    public DataGridTheming(
        IColorService colorService,
        ThemeService themeService,
        ILogger<DataGridTheming>? logger = null)
    {
        _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _logger = logger;
    }

    public async Task<PublicResult> ApplyThemeAsync(PublicGridTheme theme, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Applying theme '{ThemeName}' via Theming module", theme?.ThemeName);

            var internalTheme = theme.ToInternal();
            await _themeService.ApplyThemeAsync(internalTheme.ThemeName, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ApplyTheme failed in Theming module");
            throw;
        }
    }

    public PublicGridTheme GetCurrentTheme()
    {
        try
        {
            var internalTheme = _themeService.GetCurrentTheme();
            return internalTheme.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetCurrentTheme failed in Theming module");
            throw;
        }
    }

    public async Task<PublicResult> ResetToDefaultThemeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Resetting to default theme via Theming module");

            await _themeService.ResetToDefaultThemeAsync(cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ResetToDefaultTheme failed in Theming module");
            throw;
        }
    }

    public async Task<PublicResult> SetCellBackgroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Setting cell background color for [{RowIndex}, {ColumnName}] via Theming module", rowIndex, columnName);

            await _colorService.SetCellBackgroundColorAsync(rowIndex, columnName, color, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetCellBackgroundColor failed in Theming module");
            throw;
        }
    }

    public async Task<PublicResult> SetCellForegroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Setting cell foreground color for [{RowIndex}, {ColumnName}] via Theming module", rowIndex, columnName);

            await _colorService.SetCellForegroundColorAsync(rowIndex, columnName, color, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetCellForegroundColor failed in Theming module");
            throw;
        }
    }

    public async Task<PublicResult> SetRowBackgroundColorAsync(int rowIndex, string color, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Setting row background color for row {RowIndex} via Theming module", rowIndex);

            await _colorService.SetRowBackgroundColorAsync(rowIndex, color, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetRowBackgroundColor failed in Theming module");
            throw;
        }
    }

    public async Task<PublicResult> ClearCellColorsAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Clearing cell colors for [{RowIndex}, {ColumnName}] via Theming module", rowIndex, columnName);

            await _colorService.ClearCellColorsAsync(rowIndex, columnName, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearCellColors failed in Theming module");
            throw;
        }
    }

    public async Task<PublicResult> ClearAllColorsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Clearing all custom colors via Theming module");

            await _colorService.ClearAllColorsAsync(cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearAllColors failed in Theming module");
            throw;
        }
    }
}

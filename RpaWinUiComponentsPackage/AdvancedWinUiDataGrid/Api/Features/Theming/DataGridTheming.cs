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

    /// <summary>
    /// Sets cell background color by stable row ID.
    /// BREAKING CHANGE v3.0: Uses rowId-based key for stable color identification.
    /// </summary>
    public async Task<PublicResult> SetCellBackgroundColorAsync(string rowId, string columnName, string color, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Setting cell background color for rowId {RowId}, column {ColumnName}", rowId, columnName);

            // Use ColorService with rowId-based key: Cell_{rowId}_{columnName}
            var key = $"Cell_{rowId}_{columnName}";
            await _colorService.SetElementStatePropertyColorAsync(key, "Normal", "BackgroundColor", color, cancellationToken);

            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetCellBackgroundColor failed for rowId {RowId}", rowId);
            throw;
        }
    }

    /// <summary>
    /// Sets cell foreground color by stable row ID.
    /// BREAKING CHANGE v3.0: Uses rowId-based key for stable color identification.
    /// </summary>
    public async Task<PublicResult> SetCellForegroundColorAsync(string rowId, string columnName, string color, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Setting cell foreground color for rowId {RowId}, column {ColumnName}", rowId, columnName);

            // Use ColorService with rowId-based key: Cell_{rowId}_{columnName}
            var key = $"Cell_{rowId}_{columnName}";
            await _colorService.SetElementStatePropertyColorAsync(key, "Normal", "TextColor", color, cancellationToken);

            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetCellForegroundColor failed for rowId {RowId}", rowId);
            throw;
        }
    }

    /// <summary>
    /// Sets row background color by stable row ID.
    /// BREAKING CHANGE v3.0: Uses rowId-based key for stable color identification.
    /// </summary>
    public async Task<PublicResult> SetRowBackgroundColorAsync(string rowId, string color, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Setting row background color for rowId {RowId}", rowId);

            // Use ColorService with rowId-based key: Row_{rowId}
            var key = $"Row_{rowId}";
            await _colorService.SetElementStatePropertyColorAsync(key, "Normal", "BackgroundColor", color, cancellationToken);

            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetRowBackgroundColor failed for rowId {RowId}", rowId);
            throw;
        }
    }

    /// <summary>
    /// Clears custom colors from a cell by stable row ID.
    /// BREAKING CHANGE v3.0: Uses rowId-based key for stable color identification.
    /// </summary>
    public async Task<PublicResult> ClearCellColorsAsync(string rowId, string columnName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Clearing cell colors for rowId {RowId}, column {ColumnName}", rowId, columnName);

            // Clear by setting null/transparent colors for the cell
            var key = $"Cell_{rowId}_{columnName}";
            await _colorService.SetElementStatePropertyColorAsync(key, "Normal", "BackgroundColor", null, cancellationToken);
            await _colorService.SetElementStatePropertyColorAsync(key, "Normal", "TextColor", null, cancellationToken);

            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearCellColors failed for rowId {RowId}", rowId);
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

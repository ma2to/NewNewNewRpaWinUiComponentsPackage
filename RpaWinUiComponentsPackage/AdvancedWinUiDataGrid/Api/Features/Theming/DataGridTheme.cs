using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// PUBLIC API: Facade for comprehensive theme management.
/// Provides theme creation, import/export, and application.
/// </summary>
internal sealed class DataGridTheme : IDataGridTheme
{
    private readonly ILogger<DataGridTheme>? _logger;
    private readonly ThemeManagementService _themeService;

    public DataGridTheme(
        ThemeManagementService themeService,
        ILogger<DataGridTheme>? logger = null)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _logger = logger;
    }

    public async Task ApplyThemeAsync(
        ComprehensiveColorTheme theme,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("ApplyTheme: {ThemeName}", theme.Name);

            await _themeService.ApplyThemeAsync(theme, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ApplyTheme failed for theme {ThemeName}", theme?.Name);
            throw;
        }
    }

    public async Task<ComprehensiveColorTheme> GetCurrentThemeAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _themeService.GetCurrentThemeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetCurrentTheme failed");
            throw;
        }
    }

    public async Task<ComprehensiveColorTheme> CreateCustomThemeFromColorsAsync(
        string themeName,
        ThemeCategory category,
        IReadOnlyDictionary<string, string> colors,
        string? author = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("CreateCustomThemeFromColors: {ThemeName} ({Count} colors)",
                themeName, colors.Count);

            return await _themeService.CreateCustomThemeFromColorsAsync(
                themeName, category, colors, author, description, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CreateCustomThemeFromColors failed for {ThemeName}", themeName);
            throw;
        }
    }

    public async Task<string> ExportThemeAsync(
        ComprehensiveColorTheme theme,
        ThemeExportFormat format = ThemeExportFormat.JSON,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("ExportTheme: {ThemeName} to {Format}", theme.Name, format);

            return format switch
            {
                ThemeExportFormat.JSON => await _themeService.ExportThemeToJsonAsync(theme, cancellationToken),
                ThemeExportFormat.XML => await _themeService.ExportThemeToXmlAsync(theme, cancellationToken),
                _ => await _themeService.ExportThemeToJsonAsync(theme, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExportTheme failed for {ThemeName}", theme?.Name);
            throw;
        }
    }

    public async Task<string> ExportCurrentThemeAsync(
        ThemeExportFormat format = ThemeExportFormat.JSON,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("ExportCurrentTheme to {Format}", format);

            var currentTheme = await _themeService.GetCurrentThemeAsync(cancellationToken);
            return await ExportThemeAsync(currentTheme, format, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExportCurrentTheme failed");
            throw;
        }
    }

    public async Task<ComprehensiveColorTheme> ImportThemeAsync(
        string themeData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("ImportTheme from string ({Length} bytes)", themeData.Length);

            // Auto-detect format (JSON is default)
            // Simple detection: JSON starts with {, XML starts with <
            var trimmed = themeData.TrimStart();
            if (trimmed.StartsWith("<"))
            {
                return await _themeService.ImportThemeFromXmlAsync(themeData, cancellationToken);
            }
            else
            {
                return await _themeService.ImportThemeFromJsonAsync(themeData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ImportTheme failed");
            throw;
        }
    }

    public async Task SaveThemeAsync(
        ComprehensiveColorTheme theme,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("SaveTheme: {ThemeName} to {FilePath}", theme.Name, filePath);

            await _themeService.SaveThemeAsync(theme, filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SaveTheme failed for {ThemeName} to {FilePath}",
                theme?.Name, filePath);
            throw;
        }
    }

    public async Task<ComprehensiveColorTheme> LoadThemeAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("LoadTheme from {FilePath}", filePath);

            return await _themeService.LoadThemeAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "LoadTheme failed from {FilePath}", filePath);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableThemesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _themeService.GetAvailableThemesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetAvailableThemes failed");
            throw;
        }
    }

    public async Task<ComprehensiveColorTheme?> GetBuiltInThemeAsync(
        string themeName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("GetBuiltInTheme: {ThemeName}", themeName);

            return await _themeService.GetBuiltInThemeAsync(themeName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetBuiltInTheme failed for {ThemeName}", themeName);
            throw;
        }
    }
}

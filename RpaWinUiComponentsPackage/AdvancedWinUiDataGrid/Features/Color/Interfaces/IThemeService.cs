
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Interfaces;

/// <summary>
/// Internal interface for theme management service
/// </summary>
internal interface IThemeService
{
    /// <summary>
    /// Create a new theme
    /// </summary>
    Task CreateThemeAsync(string themeName, IEnumerable<PublicElementStatePropertyColor> colorDefinitions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply a theme by name
    /// </summary>
    Task ApplyThemeAsync(string themeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save theme to file
    /// </summary>
    Task SaveThemeAsync(string themeName, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load theme from file
    /// </summary>
    Task LoadThemeAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available theme names
    /// </summary>
    IReadOnlyList<string> GetAvailableThemes();

    /// <summary>
    /// Get current theme name
    /// </summary>
    string GetCurrentTheme();

    /// <summary>
    /// Reset to default theme
    /// </summary>
    Task ResetToDefaultThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a theme
    /// </summary>
    Task DeleteThemeAsync(string themeName, CancellationToken cancellationToken = default);
}

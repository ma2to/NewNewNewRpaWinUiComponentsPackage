using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Format for theme export
/// </summary>
public enum ThemeExportFormat
{
    /// <summary>JSON format (recommended for portability)</summary>
    JSON,

    /// <summary>XML format</summary>
    XML
}

/// <summary>
/// PUBLIC API: Interface for comprehensive theme management.
/// Supports theme creation, import/export, and application.
/// This is part of BOD 5: Advanced Theme System and BOD 7: External Application Integration.
/// </summary>
public interface IDataGridTheme
{
    /// <summary>
    /// Applies a complete theme to the grid, replacing all color settings.
    /// This is the primary method for theme switching (Light, Dark, Custom, etc.).
    /// </summary>
    /// <param name="theme">The theme to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task completing when theme is applied</returns>
    Task ApplyThemeAsync(
        ComprehensiveColorTheme theme,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active theme (includes all color customizations).
    /// Useful for exporting current state or creating derived themes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current comprehensive theme</returns>
    Task<ComprehensiveColorTheme> GetCurrentThemeAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a custom theme from a dictionary of color mappings.
    /// Simplified theme creation for external applications.
    /// Color keys follow pattern: "{ElementType}{State}{Property}" (e.g., "HeaderNormalBackground").
    /// </summary>
    /// <param name="themeName">Name for the custom theme</param>
    /// <param name="category">Theme category (typically Custom)</param>
    /// <param name="colors">Dictionary of color key to hex value mappings</param>
    /// <param name="author">Optional author name</param>
    /// <param name="description">Optional theme description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New custom theme</returns>
    Task<ComprehensiveColorTheme> CreateCustomThemeFromColorsAsync(
        string themeName,
        ThemeCategory category,
        IReadOnlyDictionary<string, string> colors,
        string? author = null,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a theme to JSON or XML format.
    /// JSON is recommended for portability and sharing across applications.
    /// </summary>
    /// <param name="theme">The theme to export</param>
    /// <param name="format">Export format (JSON or XML)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Serialized theme string</returns>
    Task<string> ExportThemeAsync(
        ComprehensiveColorTheme theme,
        ThemeExportFormat format = ThemeExportFormat.JSON,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the currently active theme to JSON or XML format.
    /// Convenience method that combines GetCurrentThemeAsync + ExportThemeAsync.
    /// </summary>
    /// <param name="format">Export format (JSON or XML)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Serialized theme string</returns>
    Task<string> ExportCurrentThemeAsync(
        ThemeExportFormat format = ThemeExportFormat.JSON,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a theme from JSON or XML string.
    /// Format is auto-detected based on content.
    /// </summary>
    /// <param name="themeData">Serialized theme string (JSON or XML)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized theme</returns>
    Task<ComprehensiveColorTheme> ImportThemeAsync(
        string themeData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a theme to a file.
    /// File extension determines format (.json, .xml).
    /// </summary>
    /// <param name="theme">The theme to save</param>
    /// <param name="filePath">File path to save to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task completing when file is saved</returns>
    Task SaveThemeAsync(
        ComprehensiveColorTheme theme,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a theme from a file.
    /// Format is auto-detected from file extension.
    /// </summary>
    /// <param name="filePath">File path to load from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Loaded theme</returns>
    Task<ComprehensiveColorTheme> LoadThemeAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of available built-in themes.
    /// Includes DefaultLight, DefaultDark, DefaultHighContrast.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of built-in theme names</returns>
    Task<IReadOnlyList<string>> GetAvailableThemesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a built-in theme by name.
    /// </summary>
    /// <param name="themeName">Theme name (e.g., "DefaultLight", "DefaultDark", "DefaultHighContrast")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Built-in theme or null if not found</returns>
    Task<ComprehensiveColorTheme?> GetBuiltInThemeAsync(
        string themeName,
        CancellationToken cancellationToken = default);
}

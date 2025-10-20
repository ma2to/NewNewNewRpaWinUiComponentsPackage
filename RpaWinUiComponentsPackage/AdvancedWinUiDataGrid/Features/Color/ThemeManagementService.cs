using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color;

/// <summary>
/// Internal service for comprehensive theme management.
/// Handles theme import/export, creation, and storage.
/// Thread-safe for concurrent operations.
/// </summary>
internal sealed class ThemeManagementService
{
    private readonly ILogger<ThemeManagementService> _logger;
    private readonly ColorManagementService _colorService;
    private readonly Dictionary<string, ComprehensiveColorTheme> _builtInThemes;

    public ThemeManagementService(
        ILogger<ThemeManagementService> logger,
        ColorManagementService colorService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));

        // Register built-in themes
        _builtInThemes = new Dictionary<string, ComprehensiveColorTheme>
        {
            ["DefaultLight"] = ComprehensiveColorTheme.DefaultLight,
            ["DefaultDark"] = ComprehensiveColorTheme.DefaultDark,
            ["DefaultHighContrast"] = ComprehensiveColorTheme.DefaultHighContrast
        };

        _logger.LogInformation("ThemeManagementService initialized with {Count} built-in themes", _builtInThemes.Count);
    }

    /// <summary>
    /// Applies a comprehensive theme to the grid.
    /// </summary>
    public async Task ApplyThemeAsync(
        ComprehensiveColorTheme theme,
        CancellationToken cancellationToken = default)
    {
        if (theme == null) throw new ArgumentNullException(nameof(theme));

        _logger.LogInformation("Applying theme: {ThemeName} ({Category})", theme.Name, theme.Category);

        await _colorService.SetThemeAsync(theme, cancellationToken);

        _logger.LogInformation("Theme applied successfully");
    }

    /// <summary>
    /// Gets the current comprehensive theme.
    /// </summary>
    public async Task<ComprehensiveColorTheme> GetCurrentThemeAsync(
        CancellationToken cancellationToken = default)
    {
        return await _colorService.GetCurrentThemeAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a custom theme from simplified color dictionary.
    /// Color keys follow pattern: "{ElementType}{State}{Property}" (e.g., "HeaderNormalBackground").
    /// </summary>
    public async Task<ComprehensiveColorTheme> CreateCustomThemeFromColorsAsync(
        string themeName,
        ThemeCategory category,
        IReadOnlyDictionary<string, string> colors,
        string? author = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        _logger.LogInformation("Creating custom theme '{ThemeName}' from {Count} color definitions", themeName, colors.Count);

        // Start with default theme based on category
        var baseTheme = category switch
        {
            ThemeCategory.Light => ComprehensiveColorTheme.DefaultLight,
            ThemeCategory.Dark => ComprehensiveColorTheme.DefaultDark,
            ThemeCategory.HighContrast => ComprehensiveColorTheme.DefaultHighContrast,
            _ => ComprehensiveColorTheme.DefaultLight
        };

        var customTheme = baseTheme with
        {
            Name = themeName,
            Category = category,
            Author = author,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        // Apply custom colors from dictionary
        // Parse color keys and apply to theme
        // Example: "HeaderNormalBackground" -> UIElementType.Header, UIElementState.Normal, ColorProperty.Background
        foreach (var kvp in colors)
        {
            if (TryParseColorKey(kvp.Key, out var elementType, out var state, out var property))
            {
                customTheme = ApplyColorToTheme(customTheme, elementType, state, property, kvp.Value);
            }
            else
            {
                _logger.LogWarning("Skipping invalid color key: {Key}", kvp.Key);
            }
        }

        _logger.LogInformation("Custom theme '{ThemeName}' created successfully", themeName);

        return customTheme;
    }

    /// <summary>
    /// Exports theme to JSON format.
    /// </summary>
    public async Task<string> ExportThemeToJsonAsync(
        ComprehensiveColorTheme theme,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        _logger.LogDebug("Exporting theme '{ThemeName}' to JSON", theme.Name);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(theme, options);

        _logger.LogInformation("Theme '{ThemeName}' exported to JSON ({Length} bytes)", theme.Name, json.Length);

        return json;
    }

    /// <summary>
    /// Imports theme from JSON string.
    /// </summary>
    public async Task<ComprehensiveColorTheme> ImportThemeFromJsonAsync(
        string json,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON data cannot be empty", nameof(json));

        _logger.LogDebug("Importing theme from JSON ({Length} bytes)", json.Length);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var theme = JsonSerializer.Deserialize<ComprehensiveColorTheme>(json, options);

        if (theme == null)
            throw new InvalidOperationException("Failed to deserialize theme from JSON");

        _logger.LogInformation("Theme '{ThemeName}' imported from JSON", theme.Name);

        return theme;
    }

    /// <summary>
    /// Exports theme to XML format.
    /// </summary>
    public async Task<string> ExportThemeToXmlAsync(
        ComprehensiveColorTheme theme,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        _logger.LogDebug("Exporting theme '{ThemeName}' to XML", theme.Name);

        var xmlSerializer = new XmlSerializer(typeof(ComprehensiveColorTheme));

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
            Async = false
        });

        xmlSerializer.Serialize(xmlWriter, theme);
        var xml = stringWriter.ToString();

        _logger.LogInformation("Theme '{ThemeName}' exported to XML ({Length} bytes)", theme.Name, xml.Length);

        return xml;
    }

    /// <summary>
    /// Imports theme from XML string.
    /// </summary>
    public async Task<ComprehensiveColorTheme> ImportThemeFromXmlAsync(
        string xml,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML data cannot be empty", nameof(xml));

        _logger.LogDebug("Importing theme from XML ({Length} bytes)", xml.Length);

        var xmlSerializer = new XmlSerializer(typeof(ComprehensiveColorTheme));

        using var stringReader = new StringReader(xml);
        var theme = xmlSerializer.Deserialize(stringReader) as ComprehensiveColorTheme;

        if (theme == null)
            throw new InvalidOperationException("Failed to deserialize theme from XML");

        _logger.LogInformation("Theme '{ThemeName}' imported from XML", theme.Name);

        return theme;
    }

    /// <summary>
    /// Saves theme to file.
    /// </summary>
    public async Task SaveThemeAsync(
        ComprehensiveColorTheme theme,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));

        _logger.LogInformation("Saving theme '{ThemeName}' to {FilePath}", theme.Name, filePath);

        // Determine format from extension
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var format = extension switch
        {
            ".json" => ThemeExportFormat.JSON,
            ".xml" => ThemeExportFormat.XML,
            _ => ThemeExportFormat.JSON // Default to JSON
        };

        var themeData = format switch
        {
            ThemeExportFormat.JSON => await ExportThemeToJsonAsync(theme, cancellationToken),
            ThemeExportFormat.XML => await ExportThemeToXmlAsync(theme, cancellationToken),
            _ => await ExportThemeToJsonAsync(theme, cancellationToken)
        };

        await File.WriteAllTextAsync(filePath, themeData, cancellationToken);

        _logger.LogInformation("Theme '{ThemeName}' saved to {FilePath}", theme.Name, filePath);
    }

    /// <summary>
    /// Loads theme from file.
    /// </summary>
    public async Task<ComprehensiveColorTheme> LoadThemeAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));
        if (!File.Exists(filePath)) throw new FileNotFoundException($"Theme file not found: {filePath}");

        _logger.LogInformation("Loading theme from {FilePath}", filePath);

        var themeData = await File.ReadAllTextAsync(filePath, cancellationToken);

        // Auto-detect format
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var theme = extension switch
        {
            ".json" => await ImportThemeFromJsonAsync(themeData, cancellationToken),
            ".xml" => await ImportThemeFromXmlAsync(themeData, cancellationToken),
            _ => await ImportThemeFromJsonAsync(themeData, cancellationToken) // Default to JSON
        };

        _logger.LogInformation("Theme '{ThemeName}' loaded from {FilePath}", theme.Name, filePath);

        return theme;
    }

    /// <summary>
    /// Gets list of available built-in themes.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAvailableThemesAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _builtInThemes.Keys.ToList();
    }

    /// <summary>
    /// Gets a built-in theme by name.
    /// </summary>
    public async Task<ComprehensiveColorTheme?> GetBuiltInThemeAsync(
        string themeName,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return _builtInThemes.TryGetValue(themeName, out var theme) ? theme : null;
    }

    /// <summary>
    /// Helper: Tries to parse color key string to element type, state, and property.
    /// Format: "{ElementType}{State}{Property}" (e.g., "HeaderNormalBackground")
    /// </summary>
    private bool TryParseColorKey(
        string key,
        out UIElementType elementType,
        out UIElementState state,
        out ColorProperty property)
    {
        elementType = default;
        state = default;
        property = default;

        // Simple parsing logic - can be enhanced for robustness
        // Expected format: HeaderNormalBackground, CellHoverForeground, ButtonPressedBorder, etc.

        // Extract property (last part: Background, Foreground, or Border)
        if (key.EndsWith("Background"))
        {
            property = ColorProperty.Background;
            key = key[..^10]; // Remove "Background"
        }
        else if (key.EndsWith("Foreground"))
        {
            property = ColorProperty.Foreground;
            key = key[..^10]; // Remove "Foreground"
        }
        else if (key.EndsWith("Border"))
        {
            property = ColorProperty.Border;
            key = key[..^6]; // Remove "Border"
        }
        else
        {
            return false;
        }

        // Extract element type (first part)
        if (key.StartsWith("Header"))
        {
            elementType = UIElementType.Header;
            key = key[6..]; // Remove "Header"
        }
        else if (key.StartsWith("Cell"))
        {
            elementType = UIElementType.Cell;
            key = key[4..]; // Remove "Cell"
        }
        else if (key.StartsWith("Button"))
        {
            elementType = UIElementType.Button;
            key = key[6..]; // Remove "Button"
        }
        else if (key.StartsWith("Row"))
        {
            elementType = UIElementType.Row;
            key = key[3..]; // Remove "Row"
        }
        else
        {
            return false;
        }

        // Remaining part is state
        return Enum.TryParse<UIElementState>(key, ignoreCase: true, out state);
    }

    /// <summary>
    /// Helper: Applies color to theme (immutable pattern).
    /// </summary>
    private ComprehensiveColorTheme ApplyColorToTheme(
        ComprehensiveColorTheme theme,
        UIElementType elementType,
        UIElementState state,
        ColorProperty property,
        string hexColor)
    {
        // Get current ColorSet
        var colorSet = GetColorSet(theme, elementType, state) ?? ColorSet.Default;

        // Update property
        var newColorSet = property switch
        {
            ColorProperty.Background => colorSet with { Background = hexColor },
            ColorProperty.Foreground => colorSet with { Foreground = hexColor },
            ColorProperty.Border => colorSet with { Border = hexColor },
            _ => colorSet
        };

        // Set back to theme
        return SetColorSet(theme, elementType, state, newColorSet);
    }

    /// <summary>
    /// Helper: Gets ColorSet from theme.
    /// </summary>
    private static ColorSet? GetColorSet(
        ComprehensiveColorTheme theme,
        UIElementType elementType,
        UIElementState state)
    {
        return (elementType, state) switch
        {
            (UIElementType.Header, UIElementState.Normal) => theme.HeaderColors.Normal,
            (UIElementType.Header, UIElementState.Hover) => theme.HeaderColors.Hover,
            (UIElementType.Header, UIElementState.Pressed) => theme.HeaderColors.Pressed,
            (UIElementType.Header, UIElementState.Selected) => theme.HeaderColors.Selected,
            (UIElementType.Header, UIElementState.Disabled) => theme.HeaderColors.Disabled,

            (UIElementType.Cell, UIElementState.Normal) => theme.CellColors.Normal,
            (UIElementType.Cell, UIElementState.Hover) => theme.CellColors.Hover,
            (UIElementType.Cell, UIElementState.Focused) => theme.CellColors.Focused,
            (UIElementType.Cell, UIElementState.Editing) => theme.CellColors.Editing,
            (UIElementType.Cell, UIElementState.Error) => theme.CellColors.Error,
            (UIElementType.Cell, UIElementState.Warning) => theme.CellColors.Warning,
            (UIElementType.Cell, UIElementState.Success) => theme.CellColors.Success,
            (UIElementType.Cell, UIElementState.ReadOnly) => theme.CellColors.ReadOnly,
            (UIElementType.Cell, UIElementState.Disabled) => theme.CellColors.Disabled,
            (UIElementType.Cell, UIElementState.Selected) => theme.CellColors.Selected,

            (UIElementType.Button, UIElementState.Normal) => theme.ButtonColors.Normal,
            (UIElementType.Button, UIElementState.Hover) => theme.ButtonColors.Hover,
            (UIElementType.Button, UIElementState.Pressed) => theme.ButtonColors.Pressed,
            (UIElementType.Button, UIElementState.Disabled) => theme.ButtonColors.Disabled,

            (UIElementType.Row, UIElementState.Normal) => theme.RowColors.Normal,
            (UIElementType.Row, UIElementState.Hover) => theme.RowColors.Hover,
            (UIElementType.Row, UIElementState.Selected) => theme.RowColors.Selected,

            _ => null
        };
    }

    /// <summary>
    /// Helper: Sets ColorSet in theme.
    /// </summary>
    private static ComprehensiveColorTheme SetColorSet(
        ComprehensiveColorTheme theme,
        UIElementType elementType,
        UIElementState state,
        ColorSet colorSet)
    {
        return (elementType, state) switch
        {
            (UIElementType.Header, UIElementState.Normal) => theme with { HeaderColors = theme.HeaderColors with { Normal = colorSet } },
            (UIElementType.Header, UIElementState.Hover) => theme with { HeaderColors = theme.HeaderColors with { Hover = colorSet } },
            (UIElementType.Header, UIElementState.Pressed) => theme with { HeaderColors = theme.HeaderColors with { Pressed = colorSet } },
            (UIElementType.Header, UIElementState.Selected) => theme with { HeaderColors = theme.HeaderColors with { Selected = colorSet } },
            (UIElementType.Header, UIElementState.Disabled) => theme with { HeaderColors = theme.HeaderColors with { Disabled = colorSet } },

            (UIElementType.Cell, UIElementState.Normal) => theme with { CellColors = theme.CellColors with { Normal = colorSet } },
            (UIElementType.Cell, UIElementState.Hover) => theme with { CellColors = theme.CellColors with { Hover = colorSet } },
            (UIElementType.Cell, UIElementState.Focused) => theme with { CellColors = theme.CellColors with { Focused = colorSet } },
            (UIElementType.Cell, UIElementState.Editing) => theme with { CellColors = theme.CellColors with { Editing = colorSet } },
            (UIElementType.Cell, UIElementState.Error) => theme with { CellColors = theme.CellColors with { Error = colorSet } },
            (UIElementType.Cell, UIElementState.Warning) => theme with { CellColors = theme.CellColors with { Warning = colorSet } },
            (UIElementType.Cell, UIElementState.Success) => theme with { CellColors = theme.CellColors with { Success = colorSet } },
            (UIElementType.Cell, UIElementState.ReadOnly) => theme with { CellColors = theme.CellColors with { ReadOnly = colorSet } },
            (UIElementType.Cell, UIElementState.Disabled) => theme with { CellColors = theme.CellColors with { Disabled = colorSet } },
            (UIElementType.Cell, UIElementState.Selected) => theme with { CellColors = theme.CellColors with { Selected = colorSet } },

            (UIElementType.Button, UIElementState.Normal) => theme with { ButtonColors = theme.ButtonColors with { Normal = colorSet } },
            (UIElementType.Button, UIElementState.Hover) => theme with { ButtonColors = theme.ButtonColors with { Hover = colorSet } },
            (UIElementType.Button, UIElementState.Pressed) => theme with { ButtonColors = theme.ButtonColors with { Pressed = colorSet } },
            (UIElementType.Button, UIElementState.Disabled) => theme with { ButtonColors = theme.ButtonColors with { Disabled = colorSet } },

            (UIElementType.Row, UIElementState.Normal) => theme with { RowColors = theme.RowColors with { Normal = colorSet } },
            (UIElementType.Row, UIElementState.Hover) => theme with { RowColors = theme.RowColors with { Hover = colorSet } },
            (UIElementType.Row, UIElementState.Selected) => theme with { RowColors = theme.RowColors with { Selected = colorSet } },

            _ => theme
        };
    }
}

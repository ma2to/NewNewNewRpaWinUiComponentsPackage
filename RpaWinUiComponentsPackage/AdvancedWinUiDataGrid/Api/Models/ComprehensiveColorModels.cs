using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Represents a set of colors for an element (Background, Foreground, Border)
/// Immutable record type for thread-safe color management
/// </summary>
public sealed record ColorSet
{
    /// <summary>Background color (hex format: #RRGGBB or #AARRGGBB)</summary>
    public string? Background { get; init; }

    /// <summary>Foreground/text color (hex format: #RRGGBB or #AARRGGBB)</summary>
    public string? Foreground { get; init; }

    /// <summary>Border color (hex format: #RRGGBB or #AARRGGBB)</summary>
    public string? Border { get; init; }

    /// <summary>
    /// Creates a default transparent color set
    /// </summary>
    public static ColorSet Default => new()
    {
        Background = "#00000000", // Transparent
        Foreground = "#000000",   // Black
        Border = "#E0E0E0"        // Light Gray
    };
}

/// <summary>
/// Defines all color states for header elements
/// </summary>
public sealed record HeaderElementColors
{
    /// <summary>Normal state colors</summary>
    public ColorSet Normal { get; init; } = new()
    {
        Background = "#F3F3F3",
        Foreground = "#000000",
        Border = "#D0D0D0"
    };

    /// <summary>Hover state colors</summary>
    public ColorSet Hover { get; init; } = new()
    {
        Background = "#E5E5E5",
        Foreground = "#000000",
        Border = "#C0C0C0"
    };

    /// <summary>Pressed state colors</summary>
    public ColorSet Pressed { get; init; } = new()
    {
        Background = "#D0D0D0",
        Foreground = "#000000",
        Border = "#B0B0B0"
    };

    /// <summary>Selected state colors (for sorted columns)</summary>
    public ColorSet Selected { get; init; } = new()
    {
        Background = "#0078D4",
        Foreground = "#FFFFFF",
        Border = "#005A9E"
    };

    /// <summary>Disabled state colors</summary>
    public ColorSet Disabled { get; init; } = new()
    {
        Background = "#F0F0F0",
        Foreground = "#A0A0A0",
        Border = "#E0E0E0"
    };
}

/// <summary>
/// Defines all color states for cell elements
/// </summary>
public sealed record CellElementColors
{
    /// <summary>Normal state colors</summary>
    public ColorSet Normal { get; init; } = new()
    {
        Background = "#FFFFFF",
        Foreground = "#000000",
        Border = "#E0E0E0"
    };

    /// <summary>Hover state colors</summary>
    public ColorSet Hover { get; init; } = new()
    {
        Background = "#F5F5F5",
        Foreground = "#000000",
        Border = "#D0D0D0"
    };

    /// <summary>Focused state colors</summary>
    public ColorSet Focused { get; init; } = new()
    {
        Background = "#E3F2FD",
        Foreground = "#000000",
        Border = "#0078D4"
    };

    /// <summary>Editing state colors</summary>
    public ColorSet Editing { get; init; } = new()
    {
        Background = "#FFF4CE",
        Foreground = "#000000",
        Border = "#FFA500"
    };

    /// <summary>Error state colors (validation failed)</summary>
    public ColorSet Error { get; init; } = new()
    {
        Background = "#FFE6E6",
        Foreground = "#000000",
        Border = "#FF0000"
    };

    /// <summary>Warning state colors</summary>
    public ColorSet Warning { get; init; } = new()
    {
        Background = "#FFF9E6",
        Foreground = "#000000",
        Border = "#FFA500"
    };

    /// <summary>Success state colors (validation passed)</summary>
    public ColorSet Success { get; init; } = new()
    {
        Background = "#E8F5E9",
        Foreground = "#000000",
        Border = "#4CAF50"
    };

    /// <summary>ReadOnly state colors</summary>
    public ColorSet ReadOnly { get; init; } = new()
    {
        Background = "#F5F5F5",
        Foreground = "#606060",
        Border = "#D0D0D0"
    };

    /// <summary>Disabled state colors</summary>
    public ColorSet Disabled { get; init; } = new()
    {
        Background = "#F0F0F0",
        Foreground = "#A0A0A0",
        Border = "#E0E0E0"
    };

    /// <summary>Selected state colors</summary>
    public ColorSet Selected { get; init; } = new()
    {
        Background = "#CCE8FF",
        Foreground = "#000000",
        Border = "#0078D4"
    };
}

/// <summary>
/// Defines all color states for button elements
/// </summary>
public sealed record ButtonElementColors
{
    /// <summary>Normal state colors</summary>
    public ColorSet Normal { get; init; } = new()
    {
        Background = "#F0F0F0",
        Foreground = "#000000",
        Border = "#C0C0C0"
    };

    /// <summary>Hover state colors</summary>
    public ColorSet Hover { get; init; } = new()
    {
        Background = "#E0E0E0",
        Foreground = "#000000",
        Border = "#B0B0B0"
    };

    /// <summary>Pressed state colors</summary>
    public ColorSet Pressed { get; init; } = new()
    {
        Background = "#D0D0D0",
        Foreground = "#000000",
        Border = "#A0A0A0"
    };

    /// <summary>Disabled state colors</summary>
    public ColorSet Disabled { get; init; } = new()
    {
        Background = "#F5F5F5",
        Foreground = "#C0C0C0",
        Border = "#E0E0E0"
    };
}

/// <summary>
/// Defines all color states for row elements
/// </summary>
public sealed record RowElementColors
{
    /// <summary>Normal state colors (used for both even/odd rows based on configuration)</summary>
    public ColorSet Normal { get; init; } = new()
    {
        Background = "#FFFFFF",
        Foreground = "#000000",
        Border = "#E0E0E0"
    };

    /// <summary>Alternate row colors (for striped rows)</summary>
    public ColorSet Alternate { get; init; } = new()
    {
        Background = "#F9F9F9",
        Foreground = "#000000",
        Border = "#E0E0E0"
    };

    /// <summary>Hover state colors</summary>
    public ColorSet Hover { get; init; } = new()
    {
        Background = "#F0F0F0",
        Foreground = "#000000",
        Border = "#D0D0D0"
    };

    /// <summary>Selected state colors</summary>
    public ColorSet Selected { get; init; } = new()
    {
        Background = "#0078D4",
        Foreground = "#FFFFFF",
        Border = "#005A9E"
    };

    /// <summary>Selected but inactive (grid lost focus)</summary>
    public ColorSet SelectedInactive { get; init; } = new()
    {
        Background = "#CCCCCC",
        Foreground = "#000000",
        Border = "#B0B0B0"
    };
}

/// <summary>
/// Defines colors for grid-level elements
/// </summary>
public sealed record GridElementColors
{
    /// <summary>Normal state colors</summary>
    public ColorSet Normal { get; init; } = new()
    {
        Background = "#FFFFFF",
        Foreground = "#000000",
        Border = "#CCCCCC"
    };
}

/// <summary>
/// Defines colors for scrollbar elements
/// </summary>
public sealed record ScrollBarElementColors
{
    /// <summary>Normal state colors</summary>
    public ColorSet Normal { get; init; } = new()
    {
        Background = "#F0F0F0",
        Foreground = "#808080",
        Border = "#D0D0D0"
    };

    /// <summary>Hover state colors</summary>
    public ColorSet Hover { get; init; } = new()
    {
        Background = "#E0E0E0",
        Foreground = "#606060",
        Border = "#C0C0C0"
    };

    /// <summary>Pressed state colors</summary>
    public ColorSet Pressed { get; init; } = new()
    {
        Background = "#D0D0D0",
        Foreground = "#505050",
        Border = "#B0B0B0"
    };

    /// <summary>Disabled state colors</summary>
    public ColorSet Disabled { get; init; } = new()
    {
        Background = "#F5F5F5",
        Foreground = "#C0C0C0",
        Border = "#E0E0E0"
    };
}

/// <summary>
/// Defines colors for filter row elements
/// </summary>
public sealed record FilterRowElementColors
{
    /// <summary>Normal state colors</summary>
    public ColorSet Normal { get; init; } = new()
    {
        Background = "#FAFAFA",
        Foreground = "#000000",
        Border = "#D0D0D0"
    };

    /// <summary>Active filter state colors</summary>
    public ColorSet Active { get; init; } = new()
    {
        Background = "#E3F2FD",
        Foreground = "#0078D4",
        Border = "#0078D4"
    };

    /// <summary>Hover state colors</summary>
    public ColorSet Hover { get; init; } = new()
    {
        Background = "#F5F5F5",
        Foreground = "#000000",
        Border = "#C0C0C0"
    };

    /// <summary>Disabled state colors</summary>
    public ColorSet Disabled { get; init; } = new()
    {
        Background = "#F0F0F0",
        Foreground = "#A0A0A0",
        Border = "#E0E0E0"
    };
}

/// <summary>
/// Defines colors for search panel elements
/// </summary>
public sealed record SearchPanelElementColors
{
    /// <summary>Normal state colors</summary>
    public ColorSet Normal { get; init; } = new()
    {
        Background = "#FFFFFF",
        Foreground = "#000000",
        Border = "#D0D0D0"
    };

    /// <summary>Active search state colors</summary>
    public ColorSet Active { get; init; } = new()
    {
        Background = "#FFFACD",
        Foreground = "#000000",
        Border = "#FFD700"
    };

    /// <summary>Match found state colors</summary>
    public ColorSet MatchFound { get; init; } = new()
    {
        Background = "#90EE90",
        Foreground = "#000000",
        Border = "#32CD32"
    };

    /// <summary>No match state colors</summary>
    public ColorSet NoMatch { get; init; } = new()
    {
        Background = "#FFB6C1",
        Foreground = "#000000",
        Border = "#FF69B4"
    };
}

/// <summary>
/// Defines colors for pagination panel elements
/// </summary>
public sealed record PaginationElementColors
{
    /// <summary>Normal state colors</summary>
    public ColorSet Normal { get; init; } = new()
    {
        Background = "#F5F5F5",
        Foreground = "#000000",
        Border = "#D0D0D0"
    };

    /// <summary>Hover state colors</summary>
    public ColorSet Hover { get; init; } = new()
    {
        Background = "#E0E0E0",
        Foreground = "#000000",
        Border = "#C0C0C0"
    };

    /// <summary>Current page state colors</summary>
    public ColorSet CurrentPage { get; init; } = new()
    {
        Background = "#0078D4",
        Foreground = "#FFFFFF",
        Border = "#005A9E"
    };

    /// <summary>Disabled state colors</summary>
    public ColorSet Disabled { get; init; } = new()
    {
        Background = "#F0F0F0",
        Foreground = "#C0C0C0",
        Border = "#E0E0E0"
    };
}

/// <summary>
/// Defines colors for special column elements (row number, checkbox, delete, validation alerts)
/// </summary>
public sealed record SpecialColumnElementColors
{
    /// <summary>Normal state colors</summary>
    public ColorSet Normal { get; init; } = new()
    {
        Background = "#F9F9F9",
        Foreground = "#606060",
        Border = "#D0D0D0"
    };

    /// <summary>Hover state colors</summary>
    public ColorSet Hover { get; init; } = new()
    {
        Background = "#F0F0F0",
        Foreground = "#404040",
        Border = "#C0C0C0"
    };

    /// <summary>Selected state colors</summary>
    public ColorSet Selected { get; init; } = new()
    {
        Background = "#CCE8FF",
        Foreground = "#000000",
        Border = "#0078D4"
    };

    /// <summary>Disabled state colors</summary>
    public ColorSet Disabled { get; init; } = new()
    {
        Background = "#F5F5F5",
        Foreground = "#C0C0C0",
        Border = "#E0E0E0"
    };

    /// <summary>Alert/Warning state colors (for validation alerts column)</summary>
    public ColorSet Alert { get; init; } = new()
    {
        Background = "#FFF3E0",
        Foreground = "#F57C00",
        Border = "#FF9800"
    };
}

/// <summary>
/// Comprehensive color theme with granular control over all UI elements and states
/// </summary>
public sealed record ComprehensiveColorTheme
{
    /// <summary>Theme name (e.g., "Corporate Blue")</summary>
    public string Name { get; init; } = "Default";

    /// <summary>Theme category (Light, Dark, HighContrast, Custom)</summary>
    public ThemeCategory Category { get; init; } = ThemeCategory.Light;

    /// <summary>Theme author</summary>
    public string? Author { get; init; }

    /// <summary>Theme description</summary>
    public string? Description { get; init; }

    /// <summary>Theme version</summary>
    public string? Version { get; init; } = "1.0.0";

    /// <summary>Creation timestamp</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Grid-level colors</summary>
    public GridElementColors GridColors { get; init; } = new();

    /// <summary>Header colors (all states)</summary>
    public HeaderElementColors HeaderColors { get; init; } = new();

    /// <summary>Cell colors (all states)</summary>
    public CellElementColors CellColors { get; init; } = new();

    /// <summary>Button colors (all states)</summary>
    public ButtonElementColors ButtonColors { get; init; } = new();

    /// <summary>Row colors (all states)</summary>
    public RowElementColors RowColors { get; init; } = new();

    /// <summary>ScrollBar colors (all states)</summary>
    public ScrollBarElementColors ScrollBarColors { get; init; } = new();

    /// <summary>Filter row colors (all states)</summary>
    public FilterRowElementColors FilterRowColors { get; init; } = new();

    /// <summary>Search panel colors (all states)</summary>
    public SearchPanelElementColors SearchPanelColors { get; init; } = new();

    /// <summary>Pagination colors (all states)</summary>
    public PaginationElementColors PaginationColors { get; init; } = new();

    /// <summary>Special column colors (row number, checkbox, delete, validation alerts)</summary>
    public SpecialColumnElementColors SpecialColumnColors { get; init; } = new();

    /// <summary>
    /// Default light theme
    /// </summary>
    public static ComprehensiveColorTheme DefaultLight => new()
    {
        Name = "Default Light",
        Category = ThemeCategory.Light,
        Description = "Default light theme with clean white background"
    };

    /// <summary>
    /// Default dark theme
    /// </summary>
    public static ComprehensiveColorTheme DefaultDark => new()
    {
        Name = "Default Dark",
        Category = ThemeCategory.Dark,
        Description = "Default dark theme with dark gray background",
        GridColors = new()
        {
            Normal = new()
            {
                Background = "#1E1E1E",
                Foreground = "#FFFFFF",
                Border = "#3F3F3F"
            }
        },
        HeaderColors = new()
        {
            Normal = new() { Background = "#2D2D2D", Foreground = "#FFFFFF", Border = "#3F3F3F" },
            Hover = new() { Background = "#3F3F3F", Foreground = "#FFFFFF", Border = "#4F4F4F" },
            Pressed = new() { Background = "#4F4F4F", Foreground = "#FFFFFF", Border = "#5F5F5F" }
        },
        CellColors = new()
        {
            Normal = new() { Background = "#252526", Foreground = "#CCCCCC", Border = "#3F3F3F" },
            Hover = new() { Background = "#2D2D2D", Foreground = "#FFFFFF", Border = "#4F4F4F" },
            Focused = new() { Background = "#1E3A5F", Foreground = "#FFFFFF", Border = "#007ACC" },
            Editing = new() { Background = "#3E3E00", Foreground = "#FFFFFF", Border = "#FFA500" }
        },
        RowColors = new()
        {
            Normal = new() { Background = "#252526", Foreground = "#CCCCCC", Border = "#3F3F3F" },
            Alternate = new() { Background = "#2D2D2D", Foreground = "#CCCCCC", Border = "#3F3F3F" },
            Selected = new() { Background = "#007ACC", Foreground = "#FFFFFF", Border = "#005A9E" }
        }
    };

    /// <summary>
    /// Default high contrast theme (accessibility)
    /// </summary>
    public static ComprehensiveColorTheme DefaultHighContrast => new()
    {
        Name = "High Contrast",
        Category = ThemeCategory.HighContrast,
        Description = "High contrast theme for accessibility",
        GridColors = new()
        {
            Normal = new()
            {
                Background = "#000000",
                Foreground = "#FFFFFF",
                Border = "#FFFFFF"
            }
        },
        HeaderColors = new()
        {
            Normal = new() { Background = "#000000", Foreground = "#FFFF00", Border = "#FFFF00" },
            Hover = new() { Background = "#FFFF00", Foreground = "#000000", Border = "#FFFFFF" },
            Pressed = new() { Background = "#00FFFF", Foreground = "#000000", Border = "#FFFFFF" }
        },
        CellColors = new()
        {
            Normal = new() { Background = "#000000", Foreground = "#FFFFFF", Border = "#808080" },
            Hover = new() { Background = "#00FFFF", Foreground = "#000000", Border = "#FFFFFF" },
            Focused = new() { Background = "#0000FF", Foreground = "#FFFF00", Border = "#FFFF00" },
            Error = new() { Background = "#FF0000", Foreground = "#FFFFFF", Border = "#FFFFFF" }
        }
    };
}

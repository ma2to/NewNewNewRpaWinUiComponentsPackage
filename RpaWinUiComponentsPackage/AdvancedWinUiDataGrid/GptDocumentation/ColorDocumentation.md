# KOMPLETNÁ ŠPECIFIKÁCIA COLOR & THEME SYSTÉMU

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY

### Clean Architecture + Command Pattern
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Theme handlers, color services (internal)
- **Core Layer**: Color domain entities, theme rules (internal)
- **Infrastructure Layer**: Color rendering, theme persistence (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý color element má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové UI elementy bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky color configurations implementujú `IColorConfiguration`
- **Interface Segregation**: Špecializované interfaces pre rôzne typy UI elementov
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable color configurations + encapsulated behavior
- **SOLID**: Single responsibility pre každý UI element color
- **LINQ Optimization**: Lazy evaluation, parallel processing, streaming where beneficial
- **Performance**: Object pooling, atomic operations, minimal allocations
- **Thread Safety**: Immutable color commands, atomic UI updates
- **Internal DI Registration**: Všetky color časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť .oldbackup_timestamp súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a LINQ optimizations
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované a bezpečné a stabilné riešenie

## 🎨 UI ELEMENTS A ICH FARBY STAVY

### 1. **Grid Container Elements**
```csharp
// GRID BACKGROUND & STRUCTURE
var gridColors = new GridElementColors
{
    BackgroundColor = Colors.White,           // Default grid background
    BorderColor = Colors.LightGray,           // Grid outer border
    LineColor = Colors.LightGray,             // Grid internal lines
    ScrollBarColor = Colors.Gray,             // Scrollbar elements
    ResizeHandleColor = Colors.DarkGray,      // Column resize handles

    // GRID STATES
    FocusedBorderColor = Colors.Blue,         // When grid has focus
    DisabledBackgroundColor = Colors.LightGray, // When grid is disabled
    LoadingOverlayColor = Colors.WhiteSmoke   // Loading state overlay
};

// 🔄 AUTOMATICKÉ REVALIDOVANIE:
// Pri zmene grid state sa automaticky prekreslí s novými farbami
// Toto platí pre VŠETKY UI elementy, nie len ako príklad
```

### 2. **Header Elements**
```csharp
var headerColors = new HeaderElementColors
{
    // NORMAL STATE
    BackgroundColor = Colors.LightGray,       // Default header background
    ForegroundColor = Colors.Black,           // Header text color
    BorderColor = Colors.Gray,                // Header borders
    SeparatorColor = Colors.DarkGray,         // Column separators

    // HOVER STATE
    HoverBackgroundColor = Colors.Silver,     // On mouse hover
    HoverForegroundColor = Colors.DarkBlue,   // Hover text color
    HoverBorderColor = Colors.Blue,           // Hover border highlight

    // PRESSED/ACTIVE STATE
    PressedBackgroundColor = Colors.DarkGray, // When clicked/pressed
    PressedForegroundColor = Colors.White,    // Pressed text color

    // SORT INDICATOR STATES
    SortAscendingColor = Colors.Green,        // Ascending sort arrow
    SortDescendingColor = Colors.Red,         // Descending sort arrow
    SortNeutralColor = Colors.Gray,           // No sort indicator

    // RESIZE & SELECTION STATES
    ResizeIndicatorColor = Colors.Blue,       // Column resize indicator
    SelectedColumnColor = Colors.LightBlue    // Selected column highlight
};

// 🔄 AUTOMATICKÉ REVALIDOVANIE pre VŠETKY header elementy
// Pri zmene header state (hover, click, sort) sa automaticky aplikujú správne farby
```

### 3. **Cell Elements s Všetkými Stavmi**
```csharp
var cellColors = new CellElementColors
{
    // NORMAL CELL STATES
    BackgroundColor = Colors.White,           // Default cell background
    ForegroundColor = Colors.Black,           // Default cell text
    BorderColor = Colors.LightGray,           // Cell border

    // SELECTION STATES
    SelectedBackgroundColor = Colors.LightBlue, // Single cell selected
    SelectedForegroundColor = Colors.Black,    // Selected cell text
    SelectedBorderColor = Colors.Blue,         // Selected cell border
    MultiSelectedBackgroundColor = Colors.LightCyan, // Multiple cells selected

    // FOCUS STATES
    FocusedBorderColor = Colors.Blue,          // Cell with keyboard focus
    FocusedBackgroundColor = Colors.AliceBlue, // Focused cell background
    FocusRectangleColor = Colors.DarkBlue,     // Focus rectangle

    // EDITING STATES
    EditingBackgroundColor = Colors.LightYellow, // Cell being edited
    EditingForegroundColor = Colors.Black,     // Editing text color
    EditingBorderColor = Colors.Orange,        // Editing border
    EditingCursorColor = Colors.Black,         // Text cursor in edit mode

    // VALIDATION STATES
    ErrorBackgroundColor = Colors.LightPink,   // Validation error
    ErrorForegroundColor = Colors.DarkRed,     // Error text color
    ErrorBorderColor = Colors.Red,             // Error border
    WarningBackgroundColor = Colors.LightYellow, // Validation warning
    WarningForegroundColor = Colors.DarkOrange, // Warning text color
    WarningBorderColor = Colors.Orange,        // Warning border
    InfoBackgroundColor = Colors.LightCyan,    // Validation info
    InfoForegroundColor = Colors.DarkBlue,     // Info text color

    // HOVER & INTERACTION STATES
    HoverBackgroundColor = Colors.WhiteSmoke,  // Mouse hover
    HoverBorderColor = Colors.Gray,            // Hover border
    ReadOnlyBackgroundColor = Colors.LightGray, // Read-only cells
    DisabledBackgroundColor = Colors.Gainsboro, // Disabled cells
    DisabledForegroundColor = Colors.Gray,     // Disabled text

    // DATA TYPE SPECIFIC COLORS
    NumericCellColor = Colors.AliceBlue,       // Numeric data cells
    DateCellColor = Colors.LavenderBlush,      // Date cells
    BooleanCellColor = Colors.Honeydew,        // Boolean cells
    TextCellColor = Colors.White,              // Text cells

    // SPECIAL CELL STATES
    CalculatedCellColor = Colors.LightCyan,    // Calculated/formula cells
    ModifiedCellColor = Colors.LightGreen,     // Recently modified
    NewRowCellColor = Colors.LightYellow       // New row cells
};
```

### 4. **Button & Action Elements**
```csharp
var buttonColors = new ButtonElementColors
{
    // DELETE BUTTON STATES
    DeleteButtonBackground = Colors.LightCoral,
    DeleteButtonForeground = Colors.DarkRed,
    DeleteButtonHoverBackground = Colors.Coral,
    DeleteButtonPressedBackground = Colors.Red,
    DeleteButtonDisabledBackground = Colors.LightGray,

    // ADD ROW BUTTON STATES
    AddButtonBackground = Colors.LightGreen,
    AddButtonForeground = Colors.DarkGreen,
    AddButtonHoverBackground = Colors.LimeGreen,
    AddButtonPressedBackground = Colors.Green,

    // FILTER BUTTON STATES
    FilterButtonBackground = Colors.LightBlue,
    FilterButtonForeground = Colors.DarkBlue,
    FilterActiveBackground = Colors.Blue,
    FilterActiveForeground = Colors.White,

    // SORT BUTTON STATES
    SortButtonBackground = Colors.LightGray,
    SortButtonForeground = Colors.Black,
    SortActiveBackground = Colors.Gray,
    SortActiveForeground = Colors.White
};
```

### 5. **Checkbox & Special Control Elements**
```csharp
var controlColors = new ControlElementColors
{
    // CHECKBOX STATES
    CheckBoxBorderColor = Colors.Gray,
    CheckBoxBackgroundColor = Colors.White,
    CheckBoxCheckedBackgroundColor = Colors.Blue,
    CheckBoxCheckedForegroundColor = Colors.White,
    CheckBoxHoverBorderColor = Colors.DarkGray,
    CheckBoxDisabledBackgroundColor = Colors.LightGray,
    CheckBoxIndeterminateBackgroundColor = Colors.Orange,

    // DROPDOWN STATES
    DropdownBackgroundColor = Colors.White,
    DropdownBorderColor = Colors.Gray,
    DropdownHoverBackgroundColor = Colors.LightGray,
    DropdownOpenBackgroundColor = Colors.AliceBlue,
    DropdownDisabledBackgroundColor = Colors.Gainsboro,

    // SLIDER STATES (pre ranges a filtering)
    SliderTrackColor = Colors.LightGray,
    SliderThumbColor = Colors.Blue,
    SliderThumbHoverColor = Colors.DarkBlue,
    SliderActiveTrackColor = Colors.Blue
};
```

### 6. **Row Number & Special Columns**
```csharp
var specialColumnColors = new SpecialColumnColors
{
    // ROW NUMBER COLUMN
    RowNumberBackgroundColor = Colors.LightGray,
    RowNumberForegroundColor = Colors.Black,
    RowNumberBorderColor = Colors.Gray,
    RowNumberSelectedColor = Colors.Blue,
    RowNumberHoverColor = Colors.Silver,

    // VALIDATION ALERTS COLUMN
    ValidAlertsBackgroundColor = Colors.White,
    ValidAlertsErrorIconColor = Colors.Red,
    ValidAlertsWarningIconColor = Colors.Orange,
    ValidAlertsInfoIconColor = Colors.Blue,
    ValidAlertsSuccessIconColor = Colors.Green,

    // ACTION COLUMNS
    ActionColumnBackgroundColor = Colors.WhiteSmoke,
    ActionColumnHoverColor = Colors.LightGray
};
```

## 📋 COMMAND PATTERN PRE COLOR MANAGEMENT

### ApplyColorThemeCommand
```csharp
public sealed record ApplyColorThemeCommand<T> where T : IColorConfiguration
{
    public required T ColorConfiguration { get; init; }
    public ColorApplicationScope Scope { get; init; } = ColorApplicationScope.Global;
    public bool PreserveUserCustomizations { get; init; } = true;
    public TimeSpan? AnimationDuration { get; init; }

    // Factory methods pre FLEXIBLE creation s DI support
    public static ApplyColorThemeCommand<T> Create(T colorConfig) => new() { ColorConfiguration = colorConfig };

    public static ApplyColorThemeCommand<T> WithScope(T colorConfig, ColorApplicationScope scope) =>
        new() { ColorConfiguration = colorConfig, Scope = scope };

    // DI factory method
    public static ApplyColorThemeCommand<T> CreateWithDI(T colorConfig, IServiceProvider services) =>
        new() { ColorConfiguration = colorConfig };
}
```

### CreateCustomThemeCommand
```csharp
public sealed record CreateCustomThemeCommand
{
    public required string ThemeName { get; init; }
    public required Dictionary<UIElementType, Dictionary<UIElementState, Color>> ColorMappings { get; init; }
    public string? Description { get; init; }
    public ThemeCategory Category { get; init; } = ThemeCategory.Custom;

    // FLEXIBLE factory methods s DI support
    public static CreateCustomThemeCommand Create(string themeName, Dictionary<UIElementType, Dictionary<UIElementState, Color>> colorMappings) =>
        new() { ThemeName = themeName, ColorMappings = colorMappings };

    public static CreateCustomThemeCommand WithDescription(string themeName, Dictionary<UIElementType, Dictionary<UIElementState, Color>> colorMappings, string description) =>
        new() { ThemeName = themeName, ColorMappings = colorMappings, Description = description };
}
```

### UpdateElementColorCommand
```csharp
public sealed record UpdateElementColorCommand
{
    public required UIElementType ElementType { get; init; }
    public required UIElementState ElementState { get; init; }
    public required Color NewColor { get; init; }
    public ColorApplicationScope Scope { get; init; } = ColorApplicationScope.Global;
    public bool UpdateRelatedElements { get; init; } = false;

    // FLEXIBLE factory methods s LINQ optimization
    public static UpdateElementColorCommand Create(UIElementType elementType, UIElementState state, Color color) =>
        new() { ElementType = elementType, ElementState = state, NewColor = color };

    public static UpdateElementColorCommand WithScope(UIElementType elementType, UIElementState state, Color color, ColorApplicationScope scope) =>
        new() { ElementType = elementType, ElementState = state, NewColor = color, Scope = scope };

    // LINQ optimized factory pre bulk updates
    public static IEnumerable<UpdateElementColorCommand> CreateBulk(
        IEnumerable<(UIElementType type, UIElementState state, Color color)> colorUpdates) =>
        colorUpdates.Select(update => Create(update.type, update.state, update.color));
}
```

## 🎯 FAÇADE API METÓDY

### Universal Color Theme API
```csharp
// FLEXIBLE generic approach - nie hardcoded factory methods
Task<Result<bool>> ApplyColorThemeAsync<T>(T colorTheme) where T : IColorConfiguration;

// Príklady použitia:
await facade.ApplyColorThemeAsync(new LightColorTheme
{
    GridBackgroundColor = Colors.White,
    HeaderBackgroundColor = Colors.LightGray
});

await facade.ApplyColorThemeAsync(new DarkColorTheme
{
    GridBackgroundColor = Colors.DarkGray,
    HeaderBackgroundColor = Colors.Gray
});

await facade.ApplyColorThemeAsync(new CustomColorTheme
{
    GridBackgroundColor = Colors.Navy,
    CellSelectedBackgroundColor = Colors.Gold
});
```

### Individual Color Management
```csharp
// Update specific UI element color
Task<Result<bool>> UpdateElementColorAsync(UIElementType elementType, UIElementState state, Color newColor);
Task<Result<bool>> UpdateElementColorsAsync(IReadOnlyDictionary<UIElementType, IReadOnlyDictionary<UIElementState, Color>> colorMappings);

// Get current colors
Task<Result<Color>> GetElementColorAsync(UIElementType elementType, UIElementState state);
Task<Result<IReadOnlyDictionary<UIElementState, Color>>> GetElementColorsAsync(UIElementType elementType);

// Reset to defaults
Task<Result<bool>> ResetElementColorsAsync(UIElementType elementType);
Task<Result<bool>> ResetAllColorsAsync();
```

### Custom Theme Creation
```csharp
/// <summary>
/// PUBLIC API: Create custom theme from current color state
/// ENTERPRISE: Professional theme creation with validation and persistence
/// EXTERNAL APP: Applications can create and manage their own themes
/// </summary>
Task<Result<CustomColorTheme>> CreateCustomThemeAsync(
    string themeName,
    string? description = null,
    ThemeCategory category = ThemeCategory.Custom);

/// <summary>
/// PUBLIC API: Create theme from color specifications
/// ENTERPRISE: Direct theme creation from external color definitions
/// EXTERNAL APP: Full control over theme creation process
/// </summary>
Task<Result<CustomColorTheme>> CreateCustomThemeFromColorsAsync(
    string themeName,
    Dictionary<UIElementType, Dictionary<UIElementState, Color>> colorMappings,
    string? description = null);

/// <summary>
/// PUBLIC API: Export theme for external storage/sharing
/// ENTERPRISE: Theme portability for enterprise deployments
/// </summary>
Task<Result<string>> ExportThemeAsync(string themeName, ThemeExportFormat format = ThemeExportFormat.Json);

/// <summary>
/// PUBLIC API: Import theme from external source
/// ENTERPRISE: Theme sharing and deployment capabilities
/// </summary>
Task<Result<bool>> ImportThemeAsync(string themeData, ThemeExportFormat format = ThemeExportFormat.Json);
```

### Theme Management API
```csharp
Task<Result<IReadOnlyList<string>>> GetAvailableThemesAsync();
Task<Result<CustomColorTheme>> GetThemeAsync(string themeName);
Task<Result<bool>> DeleteThemeAsync(string themeName);
Task<Result<string>> GetCurrentThemeNameAsync();
```

## 🌈 PREDEFINOVANÉ TÉMY S KOMPLETNÝM POKRYTÍM

### Light Theme (Default)
```csharp
public static ComprehensiveColorTheme LightTheme => new()
{
    ThemeName = "Light",
    Description = "Professional light theme for daytime usage",

    // Grid Elements
    GridColors = new GridElementColors
    {
        BackgroundColor = Colors.White,
        BorderColor = Colors.LightGray,
        LineColor = Colors.LightGray,
        FocusedBorderColor = Colors.Blue
    },

    // Header Elements
    HeaderColors = new HeaderElementColors
    {
        BackgroundColor = Colors.LightGray,
        ForegroundColor = Colors.Black,
        HoverBackgroundColor = Colors.Silver,
        SortAscendingColor = Colors.Green
    },

    // Cell Elements - kompletné pokrytie všetkých stavov
    CellColors = new CellElementColors
    {
        BackgroundColor = Colors.White,
        ForegroundColor = Colors.Black,
        SelectedBackgroundColor = Colors.LightBlue,
        EditingBackgroundColor = Colors.LightYellow,
        ErrorBackgroundColor = Colors.LightPink,
        WarningBackgroundColor = Colors.LightYellow,
        HoverBackgroundColor = Colors.WhiteSmoke
        // ... všetky ostatné stavy
    },

    // Button & Control Elements
    ButtonColors = new ButtonElementColors { /* ... */ },
    ControlColors = new ControlElementColors { /* ... */ },
    SpecialColumnColors = new SpecialColumnColors { /* ... */ }
};
```

### Dark Theme
```csharp
public static ComprehensiveColorTheme DarkTheme => new()
{
    ThemeName = "Dark",
    Description = "Modern dark theme for low-light environments",

    GridColors = new GridElementColors
    {
        BackgroundColor = Colors.DarkGray,
        BorderColor = Colors.Gray,
        LineColor = Colors.Gray,
        FocusedBorderColor = Colors.CornflowerBlue
    },

    HeaderColors = new HeaderElementColors
    {
        BackgroundColor = Colors.Gray,
        ForegroundColor = Colors.White,
        HoverBackgroundColor = Colors.DimGray,
        SortAscendingColor = Colors.LightGreen
    },

    CellColors = new CellElementColors
    {
        BackgroundColor = Colors.DimGray,
        ForegroundColor = Colors.White,
        SelectedBackgroundColor = Colors.DarkBlue,
        EditingBackgroundColor = Colors.DarkGoldenrod,
        ErrorBackgroundColor = Colors.DarkRed,
        WarningBackgroundColor = Colors.DarkOrange,
        HoverBackgroundColor = Colors.SlateGray
        // ... všetky ostatné stavy prispôsobené pre dark theme
    }
    // ... kompletné dark theme specifications
};
```

### High Contrast Theme
```csharp
public static ComprehensiveColorTheme HighContrastTheme => new()
{
    ThemeName = "HighContrast",
    Description = "High contrast theme for accessibility",

    GridColors = new GridElementColors
    {
        BackgroundColor = Colors.Black,
        BorderColor = Colors.White,
        LineColor = Colors.White,
        FocusedBorderColor = Colors.Yellow
    },

    CellColors = new CellElementColors
    {
        BackgroundColor = Colors.Black,
        ForegroundColor = Colors.White,
        SelectedBackgroundColor = Colors.Blue,
        ErrorBackgroundColor = Colors.Red,
        WarningBackgroundColor = Colors.Yellow,
        // Vysoký kontrast pre všetky stavy
    }
    // ... high contrast specifications
};
```

## ⚡ AUTOMATICKÉ THEME DETECTION & APPLICATION

```csharp
// 🔄 AUTOMATICKÉ THEME APPLICATION platí pre VŠETKY UI elementy:

// 1. GridElementColors - pri zmene grid state
// 2. HeaderElementColors - pri zmene header interaction
// 3. CellElementColors - pri zmene cell state (select, edit, validate)
// 4. ButtonElementColors - pri zmene button interaction
// 5. ControlElementColors - pri zmene control state
// 6. SpecialColumnColors - pri zmene special column state

// Implementácia automatickej aplikácie s LINQ optimization:
internal sealed class AutomaticThemeApplicationService
{
    private readonly ConcurrentDictionary<UIElementType, ConcurrentDictionary<UIElementState, Color>> _currentTheme = new();
    private readonly ObjectPool<ColorContext> _contextPool;

    public void RegisterTheme(ComprehensiveColorTheme theme)
    {
        // LINQ OPTIMIZATION: Parallel processing pre theme registration
        var colorMappings = theme.GetAllColorMappings()
            .AsParallel()
            .ToLookup(mapping => mapping.ElementType, mapping => new { mapping.State, mapping.Color })
            .ToDictionary(
                group => group.Key,
                group => new ConcurrentDictionary<UIElementState, Color>(
                    group.ToDictionary(item => item.State, item => item.Color)));

        foreach (var (elementType, stateColors) in colorMappings)
        {
            _currentTheme.AddOrUpdate(elementType, stateColors, (key, existing) => stateColors);
        }
    }

    // LINQ optimized + thread safe color application
    public async Task OnElementStateChanged(UIElementType elementType, UIElementState oldState, UIElementState newState,
        IUIElementContext elementContext)
    {
        if (_currentTheme.TryGetValue(elementType, out var stateColors) &&
            stateColors.TryGetValue(newState, out var newColor))
        {
            await elementContext.ApplyColorAsync(newColor);
        }
    }

    // Special handling pre complex state transitions
    public async Task OnMultipleElementStateChanged(IEnumerable<(UIElementType type, UIElementState state, IUIElementContext context)> changes)
    {
        // Parallel LINQ processing s object pooling
        var colorApplicationTasks = changes.AsParallel()
            .Where(change => _currentTheme.TryGetValue(change.type, out var stateColors) &&
                           stateColors.TryGetValue(change.state, out _))
            .Select(async change =>
            {
                var stateColors = _currentTheme[change.type];
                var color = stateColors[change.state];
                await change.context.ApplyColorAsync(color);
            })
            .ToArray();

        await Task.WhenAll(colorApplicationTasks);
    }
}
```

## 🧠 SMART THEME MANAGEMENT

```csharp
public enum ColorApplicationScope
{
    Global,          // Apply to entire grid
    Column,          // Apply to specific column
    Row,             // Apply to specific row
    Cell,            // Apply to specific cell
    Selection        // Apply to current selection
}

public enum UIElementType
{
    GridContainer, HeaderCell, DataCell, ButtonControl,
    CheckboxControl, DropdownControl, SliderControl,
    RowNumberColumn, ValidationColumn, ActionColumn,
    ScrollBar, ResizeHandle, FocusRectangle
}

public enum UIElementState
{
    Normal, Hover, Pressed, Selected, Focused, Editing,
    Error, Warning, Info, Success, Disabled, ReadOnly,
    Checked, Unchecked, Indeterminate, Active, Inactive,
    Ascending, Descending, Filtered, Loading
}

public enum ThemeCategory
{
    System,          // Built-in system themes
    Custom,          // User-created themes
    Enterprise,      // Enterprise/corporate themes
    Accessibility,   // High contrast/accessibility themes
    Seasonal         // Temporary/seasonal themes
}

// Smart decision making algoritmus s LINQ optimization:
public async Task<ComprehensiveColorTheme> GetRecommendedThemeAsync(
    SystemThemePreference systemPreference,
    AccessibilityRequirements accessibilityNeeds,
    UserPreferences userPrefs)
{
    // Prahy pre rozhodovanie s performance optimization:
    var recommendedTheme = systemPreference switch
    {
        SystemThemePreference.Light => LightTheme,
        SystemThemePreference.Dark => DarkTheme,
        SystemThemePreference.HighContrast => HighContrastTheme,
        SystemThemePreference.Auto => await DetectOptimalThemeAsync(),
        _ => LightTheme
    };

    // Apply accessibility modifications ak potrebné
    if (accessibilityNeeds.RequireHighContrast)
    {
        recommendedTheme = await ApplyHighContrastModificationsAsync(recommendedTheme);
    }

    return recommendedTheme;
}
```

## 🔧 EXTERNAL APPLICATION INTEGRATION

### Application-Level Theme Creation
```csharp
// V aplikácii (MIMO komponent):
var customTheme = new Dictionary<UIElementType, Dictionary<UIElementState, Color>>
{
    [UIElementType.GridContainer] = new()
    {
        [UIElementState.Normal] = Colors.White,
        [UIElementState.Focused] = Colors.AliceBlue
    },
    [UIElementType.HeaderCell] = new()
    {
        [UIElementState.Normal] = Colors.Navy,
        [UIElementState.Hover] = Colors.DarkBlue,
        [UIElementState.Pressed] = Colors.Blue
    },
    [UIElementType.DataCell] = new()
    {
        [UIElementState.Normal] = Colors.White,
        [UIElementState.Selected] = Colors.Gold,
        [UIElementState.Editing] = Colors.LightYellow,
        [UIElementState.Error] = Colors.LightPink,
        [UIElementState.Warning] = Colors.Orange,
        [UIElementState.Hover] = Colors.WhiteSmoke
    }
    // ... kompletná definícia všetkých elementov a stavov
};

// Apply custom theme
await dataGrid.CreateCustomThemeFromColorsAsync("CorporateTheme", customTheme, "Corporate branding theme");
await dataGrid.ApplyColorThemeAsync("CorporateTheme");
```

### Direct Color Modification
```csharp
// Priama zmena konkrétnych farieb:
await dataGrid.UpdateElementColorAsync(UIElementType.DataCell, UIElementState.Selected, Colors.Gold);
await dataGrid.UpdateElementColorAsync(UIElementType.HeaderCell, UIElementState.Normal, Colors.Navy);
await dataGrid.UpdateElementColorAsync(UIElementType.DataCell, UIElementState.Error, Colors.Crimson);

// Bulk color updates s LINQ optimization:
var colorUpdates = new Dictionary<UIElementType, Dictionary<UIElementState, Color>>
{
    [UIElementType.DataCell] = new()
    {
        [UIElementState.Selected] = Colors.Gold,
        [UIElementState.Error] = Colors.Crimson,
        [UIElementState.Warning] = Colors.Orange
    }
};

await dataGrid.UpdateElementColorsAsync(colorUpdates);
```

### Theme Export/Import for Application Management
```csharp
// Export current theme pre external storage
var themeJson = await dataGrid.ExportThemeAsync("MyCurrentTheme", ThemeExportFormat.Json);
await SaveThemeToFileAsync("themes/corporate-theme.json", themeJson);

// Import theme from application storage
var themeData = await LoadThemeFromFileAsync("themes/saved-theme.json");
await dataGrid.ImportThemeAsync(themeData, ThemeExportFormat.Json);
```

## 🎯 PERFORMANCE & OPTIMIZATION

### LINQ Optimizations
- **Lazy evaluation** pre theme applications
- **Parallel processing** pre bulk color updates
- **Streaming** pre real-time color changes
- **Object pooling** pre ColorContext
- **Minimal allocations** s immutable color commands
- **Hash-based color lookup** pre performance pri veľkých témach

### Thread Safety
- **Immutable color commands** a value objects
- **Atomic color updates**
- **ConcurrentDictionary** pre theme mappings
- **Thread-safe collections** pre color states
- **Concurrent theme application** s parallel LINQ

### DI Integration
- **Command factory methods** s dependency injection support
- **Service provider integration** pre external theme services
- **Interface contracts preservation** pri refactoringu

## 🎯 KĽÚČOVÉ VYLEPŠENIA PODĽA KOREKCIÍ

1. **🔄 Automatické color application** - platí pre **VŠETKY** UI elementy a stavy
2. **📋 Kompletné pokrytie** - každý UI element a každý jeho stav má definovanú farbu
3. **🔧 Flexibilné theme creation** - nie hardcoded, ale flexible object creation
4. **📊 External application integration** - complete control from outside the component
5. **⚡ Performance optimization** - LINQ, parallel processing, object pooling, thread safety
6. **🏗️ Clean Architecture** - Commands v Core, processing v Application, hybrid DI support
7. **🔄 Complete replacement** - .oldbackup_timestamp files, žiadna backward compatibility
8. **🎨 Universal theme system** - support for any UI element type and state
9. **🌈 Advanced theme management** - creation, export, import, automatic detection

---

## **🔍 LOGGING ŠPECIFIKÁCIA PRE COLOR OPERÁCIE**

### **Internal DI Registration & Service Distribution**
Všetky color logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a distribuované cez internal DI do `ColorService`:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<IColorLogger<ColorService>, ColorLogger<ColorService>>();
services.AddSingleton<IOperationLogger<ColorService>, OperationLogger<ColorService>>();
services.AddSingleton<ICommandLogger<ColorService>, CommandLogger<ColorService>>();

// V ColorService constructor
public ColorService(
    ILogger<ColorService> logger,
    IColorLogger<ColorService> colorLogger,
    IOperationLogger<ColorService> operationLogger,
    ICommandLogger<ColorService> commandLogger)
```

### **Color Application Logging Integration**
Color systém implementuje comprehensive logging pre všetky typy UI elementov s automatickou color application a smart theme detection.

### **Theme Application Logging**
```csharp
// Universal theme application logging
await _colorLogger.LogThemeApplication(themeName, theme.GetType().Name,
    elementCount: theme.GetElementCount(), duration: applicationTime);

_logger.LogInformation("Theme '{ThemeName}' applied: elements={ElementCount}, duration={Duration}ms",
    themeName, theme.GetElementCount(), applicationTime.TotalMilliseconds);

// Individual color change logging
_logger.LogInformation("Element color updated: type={ElementType}, state={ElementState}, color={Color}",
    elementType, elementState, newColor);

// Bulk color update logging
_logger.LogInformation("Bulk color update completed: {UpdateCount} colors updated in {Duration}ms",
    colorUpdates.Count, updateTime.TotalMilliseconds);
```

### **Custom Theme Creation Logging**
```csharp
// Theme creation logging
_logger.LogInformation("Custom theme created: name='{ThemeName}', elements={ElementCount}, category={Category}",
    customTheme.ThemeName, customTheme.GetElementCount(), customTheme.Category);

// Theme export/import logging
_logger.LogInformation("Theme exported: name='{ThemeName}', format={Format}, size={SizeBytes}",
    themeName, exportFormat, exportData.Length);

_logger.LogInformation("Theme imported successfully: name='{ThemeName}', elements={ElementCount}",
    importedTheme.ThemeName, importedTheme.GetElementCount());
```

### **Automatic Color Application Logging**
```csharp
// Element state change logging
_logger.LogInformation("Element state changed: type={ElementType}, oldState={OldState}, newState={NewState}, colorApplied={ColorApplied}",
    elementType, oldState, newState, wasColorApplied);

// Bulk state change logging
_logger.LogInformation("Multiple element states changed: {ChangeCount} elements updated, {ColorApplications} colors applied in {Duration}ms",
    stateChanges.Count, colorApplicationCount, processingTime.TotalMilliseconds);
```

### **Theme Detection & Recommendation Logging**
```csharp
// Smart theme recommendation logging
_logger.LogInformation("Theme recommendation: system={SystemPreference}, accessibility={AccessibilityNeeds}, recommended={RecommendedTheme}",
    systemPreference, accessibilityRequirements.RequireHighContrast, recommendedTheme.ThemeName);

// Theme switching logging
_logger.LogInformation("Theme switched: from='{OldTheme}' to='{NewTheme}', reason={SwitchReason}",
    oldTheme?.ThemeName ?? "None", newTheme.ThemeName, switchReason);
```

### **Logging Levels Usage:**
- **Information**: Theme applications, color updates, successful operations, state changes
- **Warning**: Theme conflicts, performance degradation, color validation issues
- **Error**: Theme application failures, color service errors, configuration errors
- **Critical**: Color system failures, UI rendering issues, theme corruption
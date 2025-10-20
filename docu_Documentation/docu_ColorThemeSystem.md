# COLOR & THEME SYSTEM - Špecifikácia

## 📋 METADATA

**Dátum vytvorenia:** 20.10.2025
**Verzia:** 1.0
**Jazyk kódu:** English
**Jazyk dokumentácie:** Slovenčina
**Komponent:** AdvancedDataGrid - Color & Theme Module
**Odhadovaný čas vývoja:** 28-36 dní (cca 1.5-2 mesiace pri full-time práci)

---

## 🎯 PREHĽAD

Tento dokument špecifikuje komplexný Color & Theme systém pre AdvancedDataGrid komponent. Systém poskytuje granulárnu kontrolu nad farbami každého UI elementu v každom stave, podporuje témy, export/import a priamu zmenu farieb bez nutnosti vytvárať témy.

---

## 📐 BODY 4-7: COLOR & THEME SYSTEM

### **BOD 4: Comprehensive UI Element Color System**
**Odhadovaný čas vývoja: 12-15 dní**

#### **Čo to je:**
Granulárna kontrola nad **každým stavom každého UI elementu**. Namiesto jednoduchého nastavenia farby (napr. "header background = modrá"), máš kontrolu nad všetkými stavmi.

#### **Príklady použitia:**

```csharp
// Namiesto jednoduchého:
headerBackground = Colors.Blue

// Máš granulárnu kontrolu:
HeaderElementColors {
    Normal: { Background = Blue, Foreground = White, Border = Gray },
    Hover: { Background = LightBlue, Foreground = White, Border = DarkGray },
    Pressed: { Background = DarkBlue, Foreground = White, Border = Black },
    Selected: { Background = Orange, Foreground = White, Border = OrangeRed },
    Disabled: { Background = LightGray, Foreground = DarkGray, Border = Gray }
}

CellElementColors {
    Normal: { Background = White, Foreground = Black, Border = LightGray },
    Editing: { Background = Yellow, Border = Orange },
    Error: { Background = LightRed, Border = Red },
    Warning: { Background = LightYellow, Border = Orange },
    ReadOnly: { Background = LightGray, Foreground = DarkGray }
}
```

#### **Default farby:**

Každý element má pre-definované default farby, ktoré fungujú hneď po inicializácii:

```csharp
// Grid má built-in default farby
HeaderElementColors.Default = {
    Normal: { Background = #F3F3F3, Foreground = #000000, Border = #D0D0D0 },
    Hover: { Background = #E5E5E5, Foreground = #000000, Border = #C0C0C0 },
    Pressed: { Background = #D0D0D0, Foreground = #000000, Border = #B0B0B0 },
    Selected: { Background = #0078D4, Foreground = #FFFFFF, Border = #005A9E },
    Disabled: { Background = #F0F0F0, Foreground = #A0A0A0, Border = #E0E0E0 }
}

CellElementColors.Default = {
    Normal: { Background = #FFFFFF, Foreground = #000000, Border = #E0E0E0 },
    Editing: { Background = #FFF4CE, Border = #FFA500 },
    Error: { Background = #FFE6E6, Border = #FF0000 },
    Warning: { Background = #FFF9E6, Border = #FFA500 },
    ReadOnly: { Background = #F5F5F5, Foreground = #606060, Border = #D0D0D0 },
    Selected: { Background = #CCE8FF, Foreground = #000000, Border = #0078D4 }
}
```

#### **API pre priamu zmenu farieb (bez tém):**

```csharp
// 1. Zmena farby pre konkrétny element a stav
await facade.Colors.SetElementColorAsync(
    UIElementType.Header,
    UIElementState.Normal,
    ColorProperty.Background,
    Colors.Purple
);

// 2. Zmena viacerých farieb naraz
await facade.Colors.SetElementColorsAsync(UIElementType.Header, UIElementState.Normal, new {
    Background = Colors.Purple,
    Foreground = Colors.White,
    Border = Colors.DarkPurple
});

// 3. Získanie aktuálnej farby
Color currentColor = await facade.Colors.GetElementColorAsync(
    UIElementType.Header,
    UIElementState.Hover,
    ColorProperty.Background
);

// 4. Reset na default farbu
await facade.Colors.ResetElementToDefaultAsync(UIElementType.Header, UIElementState.Normal);

// 5. Reset všetkých elementov na default
await facade.Colors.ResetAllToDefaultAsync();
```

#### **Výhody:**
- ✅ Profesionálny UX (vizuálny feedback pre každý stav)
- ✅ Accessibility (farby pre disabled/readonly stavy)
- ✅ Branding (konzistentné farby podľa firemných štandardov)
- ✅ Default farby fungujú out-of-the-box
- ✅ Priama zmena farieb bez nutnosti vytvárať tému

---

### **BOD 5: Advanced Theme System**
**Odhadovaný čas vývoja: 8-10 dní**

#### **A) Enumy pre kategorizáciu:**

```csharp
enum UIElementType {
    Header,           // Hlavičky stĺpcov
    Cell,             // Bunky s dátami
    Button,           // Tlačidlá (napr. delete button)
    ScrollBar,        // Scrollbary
    FilterRow,        // Filter riadok
    SearchPanel,      // Search panel
    PaginationPanel,  // Pagination controls
    ValidationAlert,  // Validation alert ikony
    SpecialColumn     // Special columns (checkbox, row number)
}

enum UIElementState {
    Normal,      // Základný stav
    Hover,       // Myš nad elementom
    Pressed,     // Element je stlačený
    Selected,    // Element je vybraný
    Focused,     // Element má focus (keyboard navigation)
    Editing,     // Bunka sa edituje
    Disabled,    // Element je disabled
    ReadOnly,    // Element je read-only
    Error,       // Validačná chyba
    Warning,     // Validačné upozornenie
    Success      // Validácia OK
}

enum ColorProperty {
    Background,   // Farba pozadia
    Foreground,   // Farba textu
    Border        // Farba okraja
}
```

#### **B) ThemeCategory:**

```csharp
enum ThemeCategory {
    Light,          // Svetlá téma (biele pozadie)
    Dark,           // Tmavá téma (čierne pozadie)
    HighContrast,   // Vysoký kontrast (accessibility)
    Custom          // Vlastná téma
}
```

#### **C) ComprehensiveColorTheme:**

```csharp
class ComprehensiveColorTheme {
    string Name;                           // "Corporate Blue"
    ThemeCategory Category;                // Light/Dark/HighContrast/Custom

    GridElementColors GridColors;          // Farby pre celý grid
    HeaderElementColors HeaderColors;      // Farby pre headers
    CellElementColors CellColors;          // Farby pre bunky
    ButtonElementColors ButtonColors;      // Farby pre buttons
    ScrollBarColors ScrollBarColors;       // Farby pre scrollbary
    FilterRowColors FilterRowColors;       // Farby pre filter row
    SearchPanelColors SearchPanelColors;   // Farby pre search panel
    PaginationColors PaginationColors;     // Farby pre pagination
    SpecialColumnColors SpecialColors;     // Farby pre special columns

    // Metadata
    string Author;                         // "Jan Novák"
    DateTime CreatedAt;                    // Kedy bola vytvorená
    string Description;                    // "Firemná modrá téma pre corporate apps"
    Version Version;                       // "1.2.0"
}
```

#### **Default témy:**

```csharp
// Grid má vstavané default témy
ComprehensiveColorTheme.DefaultLight = { ... };   // Default svetlá téma
ComprehensiveColorTheme.DefaultDark = { ... };    // Default tmavá téma
ComprehensiveColorTheme.DefaultHighContrast = { ... };  // Default high contrast
```

#### **D) Theme Export/Import (JSON):**

**JSON formát témy:**

```json
{
  "name": "Corporate Blue Theme",
  "category": "Light",
  "author": "IT Department",
  "version": "1.0.0",
  "description": "Official company branding theme",
  "createdAt": "2025-01-15T10:30:00Z",

  "headerColors": {
    "normal": {
      "background": "#0078D4",
      "foreground": "#FFFFFF",
      "border": "#005A9E"
    },
    "hover": {
      "background": "#106EBE",
      "foreground": "#FFFFFF",
      "border": "#004578"
    },
    "pressed": {
      "background": "#005A9E",
      "foreground": "#FFFFFF",
      "border": "#00447A"
    }
  },

  "cellColors": {
    "normal": { "background": "#FFFFFF", "foreground": "#000000" },
    "editing": { "background": "#FFF4CE", "border": "#FFA500" },
    "error": { "background": "#FFE6E6", "border": "#FF0000" },
    "selected": { "background": "#CCE8FF", "foreground": "#000000" }
  },

  "buttonColors": { },
  "scrollBarColors": { }
}
```

**API pre export/import:**

```csharp
// Export aktuálnej témy
string json = await facade.Theme.ExportCurrentThemeAsync(ThemeExportFormat.JSON);
File.WriteAllText("current_theme.json", json);

// Import témy z JSON súboru
string json = File.ReadAllText("corporate_theme.json");
ComprehensiveColorTheme theme = await facade.Theme.ImportThemeAsync(json);

// Aplikuj importovanú tému
await facade.Theme.ApplyThemeAsync(theme);

// Získaj aktuálnu tému
ComprehensiveColorTheme currentTheme = await facade.Theme.GetCurrentThemeAsync();
```

#### **Výhody:**
- ✅ Easy branding: Load theme from JSON file (firemná identita)
- ✅ Theme sharing: Export/import medzi aplikáciami, team sharing
- ✅ User customization: GUI theme editor môže generovať JSON
- ✅ Default témy fungujú out-of-the-box

---

### **BOD 6: Priama zmena farieb + Tvorba vlastných tém**
**Odhadovaný čas vývoja: 5-7 dní**

#### **API pre priamu zmenu farieb (BEZ použitia tém):**

```csharp
// 1. Zmena jednotlivej farby
await facade.Colors.SetElementColorAsync(
    UIElementType.Header,
    UIElementState.Hover,
    ColorProperty.Background,
    Colors.Purple
);

// 2. Zmena viacerých vlastností naraz
await facade.Colors.SetElementColorsAsync(UIElementType.Cell, UIElementState.Error, new {
    Background = Colors.LightPink,
    Foreground = Colors.DarkRed,
    Border = Colors.Red
});

// 3. Bulk update pre viacero elementov
await facade.Colors.SetMultipleElementColorsAsync(new Dictionary<(UIElementType, UIElementState, ColorProperty), Color> {
    [(UIElementType.Header, UIElementState.Normal, ColorProperty.Background)] = Colors.Blue,
    [(UIElementType.Header, UIElementState.Hover, ColorProperty.Background)] = Colors.LightBlue,
    [(UIElementType.Cell, UIElementState.Error, ColorProperty.Border)] = Colors.Red
});

// 4. Získanie aktuálnej farby
Color currentBg = await facade.Colors.GetElementColorAsync(
    UIElementType.Header,
    UIElementState.Normal,
    ColorProperty.Background
);

// 5. Reset na default
await facade.Colors.ResetElementToDefaultAsync(UIElementType.Header, UIElementState.Hover);
await facade.Colors.ResetAllToDefaultAsync();  // Reset všetkého
```

#### **API pre tvorbu vlastných tém:**

```csharp
// 1. Vytvor vlastnú tému z dictionary
var customTheme = await facade.Theme.CreateCustomThemeFromColorsAsync(
    themeName: "My Custom Theme",
    themeCategory: ThemeCategory.Custom,
    colors: new Dictionary<string, Color> {
        ["HeaderBackground"] = Colors.Purple,
        ["HeaderForeground"] = Colors.White,
        ["HeaderBorder"] = Colors.DarkPurple,
        ["CellEditingBackground"] = Colors.LightYellow,
        ["CellEditingBorder"] = Colors.Orange,
        ["CellErrorBackground"] = Colors.LightPink,
        ["CellErrorBorder"] = Colors.Red,
        ["ButtonHoverBackground"] = Colors.LightBlue
    }
);

// 2. Vytvor tému programmatically
var theme = new ComprehensiveColorTheme {
    Name = "Corporate Blue",
    Category = ThemeCategory.Light,
    Author = "IT Department",
    Description = "Official company theme",

    HeaderColors = new HeaderElementColors {
        Normal = new ColorSet { Background = Colors.Blue, Foreground = Colors.White },
        Hover = new ColorSet { Background = Colors.LightBlue, Foreground = Colors.White }
    },

    CellColors = new CellElementColors {
        Normal = new ColorSet { Background = Colors.White, Foreground = Colors.Black },
        Editing = new ColorSet { Background = Colors.LightYellow, Border = Colors.Orange },
        Error = new ColorSet { Background = Colors.LightPink, Border = Colors.Red }
    }
    // atď.
};

// 3. Aplikuj tému
await facade.Theme.ApplyThemeAsync(customTheme);

// 4. Modifikuj existujúcu tému
var currentTheme = await facade.Theme.GetCurrentThemeAsync();
currentTheme.HeaderColors.Hover.Background = Colors.DarkBlue;
await facade.Theme.ApplyThemeAsync(currentTheme);

// 5. Uloženie vlastnej témy
await facade.Theme.SaveThemeAsync(customTheme, "my_theme.json");

// 6. Načítanie uloženej témy
var loadedTheme = await facade.Theme.LoadThemeAsync("my_theme.json");
await facade.Theme.ApplyThemeAsync(loadedTheme);
```

#### **Kombinácia priamych zmien a tém:**

```csharp
// 1. Začni s témou
await facade.Theme.ApplyThemeAsync(ComprehensiveColorTheme.DefaultLight);

// 2. Uprav jednotlivé farby priamo (override témy)
await facade.Colors.SetElementColorAsync(
    UIElementType.Header,
    UIElementState.Normal,
    ColorProperty.Background,
    Colors.Purple  // Override default light theme header color
);

// 3. Ak aplikuješ novú tému, priame zmeny sa prepíšu
await facade.Theme.ApplyThemeAsync(ComprehensiveColorTheme.DefaultDark);
// Purple header sa prepíše na dark theme header color
```

#### **Výhody:**
- ✅ Flexibilita: Môžeš použiť témy ALEBO priame farby ALEBO obe
- ✅ Default farby: Funguje hned po inicializácii
- ✅ Easy customization: Priame API pre jednoduché zmeny
- ✅ Theme portability: Export/import pre sharing
- ✅ Programmatic control: Plná kontrola z demo app

---

### **BOD 7: External Application Theme Integration**
**Odhadovaný čas vývoja: 3-4 dni**

#### **Čo to je:**
API pre externe aplikácie (tvoja demo app) na vytváranie/modifikáciu tém.

#### **API:**

```csharp
// Z tvojej demo aplikácie môžeš:

// 1. Vytvor vlastnú tému z dictionary
var customTheme = await facade.Theme.CreateCustomThemeFromColorsAsync(
    "My Theme",
    ThemeCategory.Custom,
    new Dictionary<string, Color> {
        ["HeaderBackground"] = Colors.Purple,
        ["HeaderForeground"] = Colors.White,
        ["CellEditingBackground"] = Colors.LightYellow,
        ["CellEditingBorder"] = Colors.Orange,
        ["ErrorCellBackground"] = Colors.LightPink,
        ["ErrorCellBorder"] = Colors.Red,
        ["ButtonHoverBackground"] = Colors.LightBlue
    }
);

// 2. Aplikuj tému
await facade.Theme.ApplyThemeAsync(customTheme);

// 3. Export do JSON súboru
var json = await facade.Theme.ExportThemeAsync(customTheme, ThemeExportFormat.JSON);
File.WriteAllText("my_custom_theme.json", json);

// 4. Import z JSON súboru
var importedTheme = await facade.Theme.ImportThemeAsync(File.ReadAllText("corporate_theme.json"));
await facade.Theme.ApplyThemeAsync(importedTheme);

// 5. Priama zmena farieb (bez tém)
await facade.Colors.SetElementColorAsync(UIElementType.Header, UIElementState.Hover, ColorProperty.Background, Colors.DarkBlue);

// 6. Bulk update viacerých farieb
await facade.Colors.SetMultipleElementColorsAsync(new Dictionary<(UIElementType, UIElementState, ColorProperty), Color> {
    [(UIElementType.Header, UIElementState.Normal, ColorProperty.Background)] = Colors.Blue,
    [(UIElementType.Header, UIElementState.Hover, ColorProperty.Background)] = Colors.LightBlue,
    [(UIElementType.Cell, UIElementState.Error, ColorProperty.Border)] = Colors.Red
});

// 7. Kombinuj tému + priame zmeny
await facade.Theme.ApplyThemeAsync(ComprehensiveColorTheme.DefaultLight);
await facade.Colors.SetElementColorAsync(UIElementType.Header, UIElementState.Normal, ColorProperty.Background, Colors.Purple);
```

#### **Výhody:**
- ✅ Theme sharing medzi aplikáciami - Export/import JSON
- ✅ Corporate branding - Centrálny JSON file s témou (IT department ho distribuuje)
- ✅ User customization - GUI theme editor môže generovať JSON
- ✅ Portability - JSON je univerzálny formát
- ✅ Plná kontrola z demo app - Programmatic theme creation
- ✅ Priama zmena farieb - Bez nutnosti vytvárať celú tému

---

## 📊 CELKOVÝ ODHADOVANÝ ČAS VÝVOJA

**Body 4-7 (Color/Theme System):** **28-36 dní**
- Bod 4: 12-15 dní
- Bod 5: 8-10 dní
- Bod 6: 5-7 dní
- Bod 7: 3-4 dní

---

## ✅ ZÁVER

**Body 4-7 (Color/Theme):**
- ✅ Profesionálny UX, branding, accessibility
- ✅ Default farby fungujú out-of-the-box
- ✅ Priame API pre zmenu farieb BEZ tém
- ✅ Témy pre konzistentné branding
- ✅ Užitočné ak chceš polished produkt

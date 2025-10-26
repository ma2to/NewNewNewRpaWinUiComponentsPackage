# 📊 ANALÝZA: UI Vylepšenia z WinUI.TableView (KOMPLETNÁ FINÁLNA VERZIA)

## 📋 **PREREQUISITE: AdaptiveStorage implementácia**

⚠️ **KRITICKÉ:** Pred začatím UI vylepšení je potrebné dokončiť **Adaptive Storage Strategy** podľa dokumentu:
- 📄 `docu_Documentation/20-10-2025/docu_AdaptiveStorageStrategy.md`
- ⏱️ **Čas:** 6-8 dní (Fáza 1-4 + Testing)
- 🎯 **Účel:** Automatické prepínanie InMemory ↔ Hybrid SQLite na základe row count
- 💡 **Výhoda:** Keď bude Adaptive Storage hotový, UI vylepšenia budú fungovať optimálne pri 100K+ rows

---

## 🔍 **ANALÝZA AKTUÁLNEHO STAVU**

### ✅ **ČO UŽ MÁTE IMPLEMENTOVANÉ:**

| Feature | Status | Súbory | Poznámka |
|---------|--------|--------|----------|
| **Column Resize (základné)** | ⚠️ **FUNGUJE, ALE...** | `ResizeGripControl.cs`<br/>`ColumnResizeService.cs` | Máte resize grip, ale možno neseka/nie je plynulý ako WinUI.TableView |
| **Checkbox Column (základné)** | ⚠️ **BASIC ONLY** | `EnableCheckboxColumn` option<br/>`SpecialColumnType.Checkbox` | Máte checkbox column, ale NIE 3-state header a NIE ich UI styling |
| **Insert Row Above/Below** | ✅ **HOTOVÉ** | `docu_InsertRowFunctionality.md`<br/>`HybridRowStore.InsertRowAfterAsync()`<br/>`InsertRowBeforeAsync()` | Máte Insert Row funkcionalitu (UserInserted metadata) |
| **Pagination** | ✅ **HOTOVÉ** | `PaginationPanelViewModel.cs` | Kompletný pagination s smart page numbers (1 2 3 ... current ... last) |
| **Search Panel** | ⚠️ **INÁ LOGIKA** | `SearchPanelViewModel.cs` | Máte search panel, ale **funguje ako next/back**, nie ako filter |
| **Filter Row** | ⚠️ **TEXT INPUT** | `FilterRowViewModel.cs` | Máte text input per column, ale **NIE Excel-like flyout** |
| **Sorting Support** | ⚠️ **PARTIAL** | `ColumnHeaderViewModel.SortDirection` | Máte sort direction property, ale **nevidím click handler** |
| **ThemeManager** | ✅ **HOTOVÉ** | `ThemeManager.cs` | Komplexný color system s runtime theme changes |
| **Cell Styling** | ✅ **HOTOVÉ** | `CellViewModel.cs` | Background, border, validation colors |

---

### ❌ **ČO CHÝBA (potrebné pridať z WinUI.TableView):**

| Feature | Priorita | Zložitosť | Čo to znamená |
|---------|----------|-----------|---------------|
| **Excel-like Filter Flyout** | 🔴 P1 | **VYSOKÁ** | Flyout s unique values (checkboxes) **+ text input pre regex filtering** |
| **Checkbox Column (KOMPLETNÉ)** | 🔴 P1 | **STREDNÁ** | **Header: 3-state** + **Cell checkboxy: UI styling z WinUI.TableView** |
| **Drag & Drop Column Resize (KOMPLETNÉ)** | 🔴 P1 | **STREDNÁ** | **Okopírovať z WinUI.TableView:** zmena cursoru, plynulý realtime resize, zachovať ValidationAlerts "*" logic |
| **Context Menu (s Insert Row)** | 🟡 P2 | **STREDNÁ** | Right-click menu na cell/row (Copy, Delete, **Insert Row Above/Below**) |
| **Search as Filter** | 🟠 P1 | **STREDNÁ** | Search nevyužíva next/back, ale **vyfiltruje riadky** podľa hľadania |
| **Sort on Header Click (3-state)** | 🟡 P2 | **NÍZKA** | Kliknutie: None → Asc → Desc → None (▲/▼ indicator) |
| **Zebra Rows (konfigurovateľné)** | 🟢 P3 | **NÍZKA** | Alternate row background (default farby, ale meniť z options) |
| **UI Modularizácia** | 🟡 P2 | **STREDNÁ** | Viac menších reusable XAML súborov namiesto jedného veľkého |

---

## 🎯 **NÁVRH RIEŠENIA (Feature by Feature)**

### **1. Excel-like Filter Flyout (s regex search)** 🔴 P1

#### **Čo to je:**
- Kliknutie na filter icon v headeri → otvorí flyout
- Flyout obsahuje:
  - **Text input pre regex filtering** (novinka!)
  - Search box (filter unique values v zozname)
  - Zoznam unique values (checkboxes)
  - "Select All" / "Clear" buttons
  - "OK" / "Cancel" buttons
- Dva režimy:
  1. **Checkbox režim:** SQL WHERE IN ('value1', 'value2', ...)
  2. **Regex režim:** SQL WHERE json_extract(data, '$.column') REGEXP 'pattern'

#### **Implementácia:**

**A) Nový ViewModel:**
```csharp
// ColumnHeaderViewModel.cs - PRIDAŤ PROPERTIES
public MenuFlyout? FilterFlyout { get; set; }
public ObservableCollection<FilterValueItem> UniqueValues { get; set; } = new();
public bool HasActiveFilter { get; set; }  // Indikátor (filter icon badge)
public string FilterSearchText { get; set; }  // Search box v flyoute (filtruje unique values)
public string FilterRegexText { get; set; }   // NOVÉ: Regex pattern input
public FilterMode FilterMode { get; set; }    // NOVÉ: Checkbox / Regex

// NOVÝ ENUM
public enum FilterMode
{
    Checkbox,  // SQL WHERE IN (...)
    Regex      // SQL WHERE REGEXP ...
}
```

**B) FilterValueItem (nový model):**
```csharp
public class FilterValueItem
{
    public string Value { get; set; }
    public bool IsSelected { get; set; }
    public int Count { get; set; }  // Počet výskytov (optional)
}
```

**C) FilterFlyoutService (nový service):**
```csharp
// Features/Filter/Services/FilterFlyoutService.cs
public class FilterFlyoutService
{
    // Load unique values z SQLite
    public async Task<List<string>> LoadUniqueValuesAsync(string columnName)
    {
        // SQL: SELECT DISTINCT json_extract(data, '$.columnName') AS value
        //      FROM grid_rows WHERE __isDeleted = 0
        //      ORDER BY value
    }

    // Apply checkbox filter
    public async Task ApplyCheckboxFilterAsync(string columnName, List<string> selectedValues)
    {
        await _rowStore.SetFilterCriteria(new FilterCriteria
        {
            ColumnName = columnName,
            Operator = FilterOperator.In,
            Values = selectedValues
        });
    }

    // Apply regex filter
    public async Task ApplyRegexFilterAsync(string columnName, string regexPattern)
    {
        await _rowStore.SetFilterCriteria(new FilterCriteria
        {
            ColumnName = columnName,
            Operator = FilterOperator.Regex,  // NOVÝ OPERATOR
            Value = regexPattern
        });
    }

    public async Task ClearFilterAsync(string columnName)
    {
        await _rowStore.ClearFilterCriteria();
    }
}
```

**D) HybridRowStore - PRIDAŤ REGEX SUPPORT:**
```csharp
// BuildSqlFilterCondition() - PRIDAŤ CASE
private string BuildSqlFilterCondition(FilterCriteria filter)
{
    var columnPath = $"$.{filter.ColumnName}";

    return filter.Operator switch
    {
        // ... existing cases ...

        FilterOperator.In =>
            $"json_extract(data, '{columnPath}') IN ({string.Join(", ", filter.Values.Select(v => FormatSqlValue(v)))})",

        // NOVÉ: Regex support
        FilterOperator.Regex =>
            $"json_extract(data, '{columnPath}') REGEXP '{EscapeRegex(filter.Value)}'",

        _ => throw new NotSupportedException($"Filter operator {filter.Operator} not supported")
    };
}

private string EscapeRegex(string pattern)
{
    // Escape SQL injection attempts
    return pattern.Replace("'", "''");
}
```

**E) XAML Flyout Template (MODULAR - REUSABLE):**

**NOVÝ SÚBOR:** `UIControls/FilterFlyout/FilterFlyoutTemplate.xaml`
```xaml
<!-- REUSABLE COMPONENT: Filter Flyout Template -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <DataTemplate x:Key="FilterFlyoutContentTemplate">
        <StackPanel Width="250" Padding="8">
            <!-- REŽIM SELECTOR (Checkbox / Regex) -->
            <ComboBox Header="Filter Mode:" SelectedIndex="{Binding FilterMode, Mode=TwoWay}">
                <ComboBoxItem Content="Values (Checkbox)" />
                <ComboBoxItem Content="Regex Pattern" />
            </ComboBox>

            <!-- REGEX INPUT -->
            <TextBox Header="Regex Pattern:"
                     Text="{Binding FilterRegexText, Mode=TwoWay}"
                     Visibility="{Binding IsRegexMode, Mode=OneWay}"
                     PlaceholderText="e.g. ^test.*|.*data$"
                     Margin="0,8,0,0" />

            <!-- UNIQUE VALUES SEARCH -->
            <TextBox Header="Search Values:"
                     Text="{Binding FilterSearchText, Mode=TwoWay}"
                     Visibility="{Binding IsCheckboxMode, Mode=OneWay}"
                     PlaceholderText="Filter values..."
                     Margin="0,8,0,0" />

            <!-- UNIQUE VALUES LIST -->
            <ContentControl Content="{Binding}"
                           ContentTemplate="{StaticResource FilterValueListTemplate}"
                           Visibility="{Binding IsCheckboxMode, Mode=OneWay}"
                           Margin="0,8,0,0" />

            <!-- BUTTONS -->
            <ContentControl Content="{Binding}"
                           ContentTemplate="{StaticResource FilterButtonsTemplate}"
                           Margin="0,8,0,0" />
        </StackPanel>
    </DataTemplate>

    <!-- FILTER VALUE LIST (SEPARATE REUSABLE COMPONENT) -->
    <DataTemplate x:Key="FilterValueListTemplate">
        <ScrollViewer MaxHeight="300">
            <ItemsRepeater ItemsSource="{Binding UniqueValues}">
                <ItemsRepeater.ItemTemplate>
                    <DataTemplate>
                        <CheckBox Content="{Binding Value}"
                                  IsChecked="{Binding IsSelected, Mode=TwoWay}"
                                  Margin="0,4,0,0" />
                    </DataTemplate>
                </ItemsRepeater.ItemTemplate>
            </ItemsRepeater>
        </ScrollViewer>
    </DataTemplate>

    <!-- FILTER BUTTONS (SEPARATE REUSABLE COMPONENT) -->
    <DataTemplate x:Key="FilterButtonsTemplate">
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8">
            <Button Content="Select All"
                    Command="{Binding SelectAllCommand}"
                    Visibility="{Binding IsCheckboxMode, Mode=OneWay}" />
            <Button Content="Clear" Command="{Binding ClearCommand}" />
            <Button Content="Cancel" Command="{Binding CancelCommand}" />
            <Button Content="OK"
                    Command="{Binding ApplyCommand}"
                    Style="{StaticResource AccentButtonStyle}" />
        </StackPanel>
    </DataTemplate>

</ResourceDictionary>
```

**POUŽITIE V ColumnHeaderControl.xaml:**
```xaml
<MenuFlyout x:Name="FilterFlyout">
    <MenuFlyout.MenuFlyoutPresenterStyle>
        <Style TargetType="MenuFlyoutPresenter">
            <Setter Property="Padding" Value="0" />
        </Style>
    </MenuFlyout.MenuFlyoutPresenterStyle>

    <ContentControl Content="{x:Bind ViewModel}"
                   ContentTemplate="{StaticResource FilterFlyoutContentTemplate}" />
</MenuFlyout>
```

#### **Výhody:**
✅ SQL-optimized (indexes)
✅ Regex support (powerful filtering)
✅ Funguje pri 10M+ rows
✅ Excel UX (intuitívne) + Developer UX (regex)
✅ **MODULAR XAML:** 3 reusable templates (Content, ValueList, Buttons)

#### **Čas:** **9-11 hodín**
- 2h: FilterFlyoutService + LoadUniqueValuesAsync
- 2h: Regex support v HybridRowStore (FilterOperator.Regex, BuildSqlFilterCondition)
- 4h: XAML modularizácia (3 separate templates: Content, ValueList, Buttons)
- 2h: Prepojenie na ColumnHeaderViewModel + event handlers
- 1h: Testing + debugging

---

### **2. Checkbox Column - KOMPLETNÉ (Header 3-state + Cell UI z WinUI.TableView)** 🔴 P1

#### **Čo to je (z WinUI.TableView):**

**A) Header checkbox má 3 stavy:**
1. **Empty (Unchecked):** Žiadny riadok nie je checked
2. **Full Checkmark (Checked):** Všetky riadky sú checked
3. **Indeterminate (■):** Niektoré riadky sú checked, iné nie

**B) Cell checkbox UI (okopírovať z WinUI.TableView):**
- **Styling:** Border color, background, checkmark glyph, hover/pressed states
- **Size:** Min width/height z ich template
- **Animations:** Smooth check/uncheck transitions

#### **Implementácia:**

**A) ColumnHeaderViewModel - PRIDAŤ PRE CHECKBOX HEADER:**
```csharp
// Pre SpecialColumnType.Checkbox headers
public bool? IsCheckboxHeaderChecked { get; set; }  // true = all, false = none, null = indeterminate
```

**B) CheckboxHeaderService (nový helper):**
```csharp
// Features/SpecialColumns/Services/CheckboxHeaderService.cs
public class CheckboxHeaderService
{
    public bool? GetHeaderCheckState(IEnumerable<DataGridRowViewModel> rows)
    {
        var checkedCount = rows.Count(r => r.IsRowSelected);
        var totalCount = rows.Count();

        if (checkedCount == 0) return false;           // Empty (Unchecked)
        if (checkedCount == totalCount) return true;   // Full (Checked)
        return null;                                    // Indeterminate (■)
    }

    public void OnHeaderCheckboxClicked(ColumnHeaderViewModel header, IEnumerable<DataGridRowViewModel> rows)
    {
        var currentState = GetHeaderCheckState(rows);

        // Cycle: Empty → Checked → Empty (skip Indeterminate on click)
        var newState = currentState != true;

        foreach (var row in rows)
        {
            row.IsRowSelected = newState;
        }

        header.IsCheckboxHeaderChecked = newState;
    }
}
```

**C) MODULAR XAML - HEADER CHECKBOX:**

**NOVÝ SÚBOR:** `UIControls/Checkbox/CheckboxHeaderTemplate.xaml`
```xaml
<!-- REUSABLE COMPONENT: 3-State Checkbox Header -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <DataTemplate x:Key="CheckboxHeaderTemplate">
        <CheckBox IsChecked="{Binding IsCheckboxHeaderChecked, Mode=TwoWay}"
                  IsThreeState="True"
                  Command="{Binding HeaderCheckboxClickedCommand}">
            <CheckBox.Resources>
                <!-- CUSTOM STYLE PRE INDETERMINATE STATE -->
                <Style TargetType="CheckBox" BasedOn="{StaticResource DefaultCheckBoxStyle}">
                    <Setter Property="Template">
                        <Setter.Value>
                            <ControlTemplate TargetType="CheckBox">
                                <Grid>
                                    <!-- Indeterminate Glyph (■) -->
                                    <FontIcon x:Name="IndeterminateGlyph"
                                              Glyph="&#xE739;"
                                              FontSize="12"
                                              Visibility="Collapsed" />
                                    <!-- Normal checkbox UI -->
                                    <!-- ... rest of template ... -->

                                    <VisualStateManager.VisualStateGroups>
                                        <VisualStateGroup x:Name="CheckStates">
                                            <VisualState x:Name="Indeterminate">
                                                <VisualState.Setters>
                                                    <Setter Target="IndeterminateGlyph.Visibility" Value="Visible" />
                                                </VisualState.Setters>
                                            </VisualState>
                                        </VisualStateGroup>
                                    </VisualStateManager.VisualStateGroups>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </CheckBox.Resources>
        </CheckBox>
    </DataTemplate>

</ResourceDictionary>
```

**D) MODULAR XAML - CELL CHECKBOX (OKOPÍROVAŤ Z WinUI.TableView):**

**NOVÝ SÚBOR:** `UIControls/Checkbox/CheckboxCellTemplate.xaml`
```xaml
<!-- REUSABLE COMPONENT: Cell Checkbox (styled from WinUI.TableView) -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <DataTemplate x:Key="CheckboxCellTemplate">
        <CheckBox IsChecked="{Binding IsRowSelected, Mode=TwoWay}"
                  MinWidth="{Binding Options.CheckboxMinWidth}"
                  MinHeight="{Binding Options.CheckboxMinHeight}"
                  HorizontalAlignment="Center"
                  VerticalAlignment="Center">
            <CheckBox.Resources>
                <!-- OKOPÍROVAŤ KOMPLETNÝ STYLE Z WinUI.TableView -->
                <!-- Border styling (colors, thickness) -->
                <!-- Checkmark glyph -->
                <!-- Hover/Pressed states -->
                <!-- Animations (smooth transitions) -->
                <Style TargetType="CheckBox" BasedOn="{StaticResource DefaultCheckBoxStyle}">
                    <Setter Property="BorderBrush" Value="{Binding Options.CheckboxBorderColor}" />
                    <Setter Property="BorderThickness" Value="{Binding Options.CheckboxBorderThickness}" />
                    <Setter Property="Background" Value="{Binding Options.CheckboxBackgroundColor}" />
                    <!-- ... EXTRAHUJTE Z WinUI.TableView/src/Controls/TableViewCell.xaml ... -->
                </Style>
            </CheckBox.Resources>
        </CheckBox>
    </DataTemplate>

</ResourceDictionary>
```

**E) POUŽITIE V CellControl.xaml:**
```xaml
<!-- CellControl.xaml - pre SpecialColumnType.Checkbox cells -->
<ContentControl Content="{x:Bind ViewModel}"
               ContentTemplate="{StaticResource CheckboxCellTemplate}"
               Visibility="{x:Bind IsCheckboxCell, Mode=OneWay}" />
```

**F) Auto-update header checkbox (keď user checkne row):**
```csharp
// DataGridViewModel.cs - PRIDAŤ EVENT HANDLER
private void OnRowSelectionChanged(DataGridRowViewModel row)
{
    // Update header checkbox state
    var checkboxHeader = ColumnHeaders.FirstOrDefault(h => h.SpecialType == SpecialColumnType.Checkbox);
    if (checkboxHeader != null)
    {
        checkboxHeader.IsCheckboxHeaderChecked = _checkboxHeaderService.GetHeaderCheckState(Rows);
    }
}
```

#### **Výhody:**
✅ Intuitívny UX (Excel pattern)
✅ Vizuálna indikácia partial selection
✅ Professional styling (z WinUI.TableView)
✅ Smooth animations
✅ **MODULAR XAML:** 2 separate templates (Header, Cell) - reusable

#### **Čas:** **6-8 hodín**
- 2h: CheckboxHeaderService + GetHeaderCheckState logic
- 2h: Header checkbox XAML template s custom Indeterminate glyph (■) - MODULAR
- 2h: **Cell checkbox XAML - okopírovať kompletný style z WinUI.TableView - MODULAR**
- 1h: Auto-update header na row changes
- 1h: Testing

---

### **3. Drag & Drop Column Resize - KOMPLETNÉ (z WinUI.TableView)** 🔴 P1

#### **Čo to je:**
- **Okopírovať z WinUI.TableView:**
  - Zmena cursoru (SizeWestEast) - plynulá, bez sekerov
  - Realtime resize (live update šírky počas drag, nie len po release)
  - Smooth animations

- **ZACHOVAŤ VAŠU LOGIKU:**
  - ValidationAlerts stĺpec má šírku `*` (auto-expand)
  - Po drag&drop resize ValidationAlerts → fixed width (napr. 200px)
  - Ak máte entry point API `SetColumnWidth()` → po volaní zostáva fixed width

#### **Implementácia:**

**A) EXTRAHUJTE Z WinUI.TableView:**
```
// Súbory na analýzu:
- WinUI.TableView/src/Controls/TableViewColumnHeader.cs
- WinUI.TableView/src/Primitives/TableViewColumnHeaderBase.cs
- XAML template pre resize grip
```

**B) Porovnajte s vaším `ResizeGripControl.cs`:**
```csharp
// VÁŠ KÓD (RpaWinUiComponentsPackage):
// ResizeGripControl.cs - riadky 1-100

// ČO ANALYZOVAŤ V ICH KÓDE:
// 1. Cursor management (static vs. per-instance)
// 2. PointerPressed/PointerMoved/PointerReleased handlers
// 3. Realtime width update (během drag, nie len po release)
// 4. Throttling/debouncing (ak majú)
// 5. Visual feedback (grip highlight, column preview)
```

**C) NOVÝ ResizeGripControl (hybrid z WinUI.TableView + váš kód):**
```csharp
// UIControls/ResizeGripControl.cs
internal sealed class ResizeGripControl : Control
{
    // OKOPÍROVAŤ Z WinUI.TableView:
    // - Cursor management (smooth, no flicker)
    // - Realtime resize logic (update width during drag)
    // - Visual states (Normal, PointerOver, Pressed)

    // ZACHOVAŤ Z VÁŠHO KÓDU:
    // - Static cursor fields (optimization)
    // - IsHitTestVisible properties
    // - Width = 4px (standard grip width)

    // NOVÉ: Realtime resize support
    private bool _isDragging;
    private double _dragStartX;
    private double _columnStartWidth;

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = true;
        _dragStartX = e.GetCurrentPoint(this).Position.X;
        _columnStartWidth = _columnHeader.Width;

        this.CapturePointer(e.Pointer);  // KRITICKÉ: Capture pointer pre smooth drag
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;

        var currentX = e.GetCurrentPoint(this).Position.X;
        var delta = currentX - _dragStartX;
        var newWidth = _columnStartWidth + delta;

        // REALTIME UPDATE (počas drag)
        _columnResizeService.ResizeColumn(_columnIndex, newWidth);
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            this.ReleasePointerCapture(e.Pointer);
        }
    }
}
```

**D) ZACHOVAŤ LOGIKU PRE ValidationAlerts:**
```csharp
// ColumnResizeService.cs - PRIDAŤ LOGIC
public double ResizeColumn(int columnIndex, double newWidth)
{
    lock (_resizeLock)
    {
        var column = _columnService.GetColumnDefinitions()[columnIndex];

        // ZACHOVAŤ: ValidationAlerts special handling
        if (column.SpecialType == SpecialColumnType.ValidationAlerts)
        {
            // PRED drag: Width = "*" (auto-expand)
            // PO drag: Width = fixed (napr. 200px)
            _logger.LogInformation(
                "ValidationAlerts column resized from auto (*) to fixed width: {Width}px",
                newWidth);
        }

        // Update column width
        var updatedColumn = column.Clone();
        updatedColumn.Width = constrainedWidth;
        _columnService.UpdateColumn(updatedColumn);

        return constrainedWidth;
    }
}
```

**E) Entry Point API (ak existuje):**
```csharp
// IDataGridColumns.cs
public interface IDataGridColumns
{
    // Ak už máte túto metódu, ZACHOVAŤ:
    Task SetColumnWidthAsync(string columnName, double width);

    // Po volaní tejto metódy → column má fixed width (nie "*")
}
```

#### **Výhody:**
✅ Plynulý resize (z WinUI.TableView)
✅ Realtime visual feedback
✅ Zachovaná logika pre ValidationAlerts
✅ Entry point API compatibility

#### **Čas:** **6-8 hodín**
- 2h: Analýza WinUI.TableView resize implementácie
- 2h: Refactor ResizeGripControl (realtime resize + pointer capture)
- 1h: Zachovať ValidationAlerts "*" → fixed width logic
- 1h: Testing (smooth drag, no flicker, ValidationAlerts behavior)
- 1h: Entry point API testing

---

### **4. Context Menu (s Insert Row Above/Below)** 🟡 P2

#### **Čo to je:**
- Right-click na cell → menu (Copy, Copy Row, Delete Row, **Insert Row Above/Below**)
- Right-click na row → menu (Copy Row, Delete Row, **Insert Row Above/Below**)
- **Použiť existujúcu Insert Row funkcionalitu:**
  - `HybridRowStore.InsertRowAfterAsync()` (riadky 1644-1688)
  - `HybridRowStore.InsertRowBeforeAsync()` (riadky 1694-1738)
  - Dokumentácia: `docu_InsertRowFunctionality.md`

#### **Implementácia:**

**A) CellViewModel - PRIDAŤ:**
```csharp
public MenuFlyout? ContextMenu { get; set; }
```

**B) DataGridRowViewModel - PRIDAŤ:**
```csharp
public MenuFlyout? RowContextMenu { get; set; }
```

**C) MODULAR XAML - CONTEXT MENU TEMPLATES:**

**NOVÝ SÚBOR:** `UIControls/ContextMenu/CellContextMenuTemplate.xaml`
```xaml
<!-- REUSABLE COMPONENT: Cell Context Menu -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <MenuFlyout x:Key="CellContextMenuTemplate">
        <!-- Copy Actions -->
        <MenuFlyoutItem Text="Copy"
                       Icon="{StaticResource CopyIcon}"
                       Command="{Binding CopyCellCommand}" />
        <MenuFlyoutItem Text="Copy Row"
                       Icon="{StaticResource CopyRowIcon}"
                       Command="{Binding CopyRowCommand}" />

        <MenuFlyoutSeparator />

        <!-- Insert Row Actions -->
        <MenuFlyoutItem Text="Insert Row Above"
                       Icon="{StaticResource InsertAboveIcon}"
                       Command="{Binding InsertRowAboveCommand}" />
        <MenuFlyoutItem Text="Insert Row Below"
                       Icon="{StaticResource InsertBelowIcon}"
                       Command="{Binding InsertRowBelowCommand}" />

        <MenuFlyoutSeparator />

        <!-- Delete Action -->
        <MenuFlyoutItem Text="Delete Row"
                       Icon="{StaticResource DeleteIcon}"
                       Command="{Binding DeleteRowCommand}" />
    </MenuFlyout>

</ResourceDictionary>
```

**NOVÝ SÚBOR:** `UIControls/ContextMenu/RowContextMenuTemplate.xaml`
```xaml
<!-- REUSABLE COMPONENT: Row Context Menu -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <MenuFlyout x:Key="RowContextMenuTemplate">
        <!-- Copy Action -->
        <MenuFlyoutItem Text="Copy Row"
                       Icon="{StaticResource CopyRowIcon}"
                       Command="{Binding CopyRowCommand}" />

        <MenuFlyoutSeparator />

        <!-- Insert Row Actions -->
        <MenuFlyoutItem Text="Insert Row Above"
                       Icon="{StaticResource InsertAboveIcon}"
                       Command="{Binding InsertRowAboveCommand}" />
        <MenuFlyoutItem Text="Insert Row Below"
                       Icon="{StaticResource InsertBelowIcon}"
                       Command="{Binding InsertRowBelowCommand}" />

        <MenuFlyoutSeparator />

        <!-- Delete Action -->
        <MenuFlyoutItem Text="Delete Row"
                       Icon="{StaticResource DeleteIcon}"
                       Command="{Binding DeleteRowCommand}" />
    </MenuFlyout>

</ResourceDictionary>
```

**D) Commands Implementation:**
```csharp
// CellViewModel.cs - PRIDAŤ COMMANDS
public ICommand InsertRowAboveCommand => new RelayCommand(async () =>
{
    var emptyRow = CreateEmptyRow();
    await _rowStore.InsertRowBeforeAsync(RowIndex, emptyRow);
});

public ICommand InsertRowBelowCommand => new RelayCommand(async () =>
{
    var emptyRow = CreateEmptyRow();
    await _rowStore.InsertRowAfterAsync(RowIndex, emptyRow);
});

// Helper: Create empty row with all columns set to null
private IReadOnlyDictionary<string, object?> CreateEmptyRow()
{
    return _columnNames.ToDictionary(col => col, col => (object?)null);
}
```

**E) Event handlers v DataGridCellsView:**
```csharp
// OnCellRightClick
private void OnCellRightClick(CellViewModel cell, PointerRoutedEventArgs e)
{
    if (cell.ContextMenu == null)
    {
        cell.ContextMenu = Resources["CellContextMenuTemplate"] as MenuFlyout;
    }

    var element = e.OriginalSource as FrameworkElement;
    cell.ContextMenu.ShowAt(element);
}
```

#### **Výhody:**
✅ Insert Row Above/Below (používa existujúcu funkcionalitu)
✅ Copy/Delete actions
✅ **MODULAR XAML:** 2 separate templates (Cell, Row) - reusable
✅ Prepojené na HybridRowStore (InsertRowBeforeAsync, InsertRowAfterAsync)

#### **Čas:** **4-5 hodín**
- 1h: MODULAR XAML templates (CellContextMenu, RowContextMenu)
- 1h: Commands implementation (InsertRowAbove, InsertRowBelow)
- 1h: Event handlers (RightTapped)
- 1h: Clipboard integration (Copy)
- 1h: Testing

---

### **5. Sort on Header Click (3-state: None → Asc → Desc → None)** 🟡 P2

#### **Čo to je:**
- **1. kliknutie:** None → Ascending (▲)
- **2. kliknutie:** Ascending → Descending (▼)
- **3. kliknutie:** Descending → None (žiadny indikátor)
- **4. kliknutie:** None → Ascending (opakuje sa)

#### **Implementácia:**

**A) ColumnHeaderViewModel - PROPERTY UŽ EXISTUJE:**
```csharp
public string SortDirection { get; set; }  // "None", "Ascending", "Descending"
```

**B) HeadersRowView - PRIDAŤ CLICK HANDLER:**
```csharp
private async void OnHeaderClicked(ColumnHeaderViewModel header)
{
    // 3-STATE CYCLE: None → Asc → Desc → None
    header.SortDirection = header.SortDirection switch
    {
        "None" => "Ascending",
        "Ascending" => "Descending",
        "Descending" => "None",
        _ => "Ascending"
    };

    // Clear sort indicators on other columns
    foreach (var otherHeader in _viewModel.ColumnHeaders.Where(h => h != header))
    {
        otherHeader.SortDirection = "None";
    }

    // Apply sort
    await _sortService.SortByColumnAsync(header.ColumnName, header.SortDirection);
}
```

**C) SortService - prepojenie na SQL:**
```csharp
// Features/Sort/Services/SortService.cs
public async Task SortByColumnAsync(string columnName, string direction)
{
    if (direction == "None")
    {
        _rowStore.ClearSortCriteria();
    }
    else
    {
        var sortDir = direction == "Ascending" ? SortDirection.Ascending : SortDirection.Descending;
        _rowStore.SetSortCriteria(columnName, sortDir);
    }

    // Refresh UI
    await _dataRefreshService.RefreshAsync();
}
```

**D) MODULAR XAML - SORT INDICATOR:**

**NOVÝ SÚBOR:** `UIControls/Sort/SortIndicatorTemplate.xaml`
```xaml
<!-- REUSABLE COMPONENT: Sort Indicator -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <DataTemplate x:Key="SortIndicatorTemplate">
        <TextBlock FontFamily="Segoe MDL2 Assets"
                   FontSize="12"
                   Margin="4,0,0,0"
                   VerticalAlignment="Center">
            <TextBlock.Text>
                <Binding Path="SortDirection" Converter="{StaticResource SortDirectionToGlyphConverter}" />
            </TextBlock.Text>
        </TextBlock>
    </DataTemplate>

</ResourceDictionary>
```

**E) Converter:**
```csharp
public class SortDirectionToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() switch
        {
            "Ascending" => "\uE0A0",   // ▲
            "Descending" => "\uE0A1",  // ▼
            _ => string.Empty          // None
        };
    }
}
```

**F) POUŽITIE V ColumnHeaderControl.xaml:**
```xaml
<ContentControl Content="{x:Bind ViewModel}"
               ContentTemplate="{StaticResource SortIndicatorTemplate}" />
```

#### **Výhody:**
✅ 3-state cycle (None → Asc → Desc → None)
✅ Visual indicator (▲/▼)
✅ **MODULAR XAML:** Separate template - reusable

#### **Čas:** **2-3 hodiny**
- 1h: 3-state cycle logic v OnHeaderClicked
- 1h: MODULAR sort indicator XAML template + converter
- 1h: Testing

---

### **6. Zebra Rows (konfigurovateľné farby a text)** 🟢 P3

#### **Čo to je:**
- **Default:**
  - Párne riadky: biela (#FFFFFF) background, čierny (#000000) text
  - Nepárne riadky: svetlo sivá (#F5F5F5) background, čierny (#000000) text
- **Konfigurovateľné z entry pointu** (AdvancedDataGridOptions)
  - **Background farby** pre párne/nepárne riadky
  - **Text farby** pre párne/nepárne riadky
- **Voliteľné** (enable/disable)

#### **Implementácia:**

**A) AdvancedDataGridOptions - PRIDAŤ:**
```csharp
public class AdvancedDataGridOptions
{
    // ... existing properties ...

    /// <summary>
    /// Enable zebra rows (alternate row background and foreground colors)
    /// Default: false
    /// </summary>
    public bool EnableZebraRows { get; set; } = false;

    /// <summary>
    /// Zebra row background color for even rows (0, 2, 4, ...)
    /// Default: #FFFFFF (white)
    /// </summary>
    public string ZebraRowEvenBackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Zebra row background color for odd rows (1, 3, 5, ...)
    /// Default: #F5F5F5 (light gray)
    /// </summary>
    public string ZebraRowOddBackgroundColor { get; set; } = "#F5F5F5";

    /// <summary>
    /// Zebra row foreground (text) color for even rows (0, 2, 4, ...)
    /// Default: #000000 (black)
    /// </summary>
    public string ZebraRowEvenForegroundColor { get; set; } = "#000000";

    /// <summary>
    /// Zebra row foreground (text) color for odd rows (1, 3, 5, ...)
    /// Default: #000000 (black)
    /// </summary>
    public string ZebraRowOddForegroundColor { get; set; } = "#000000";
}
```

**B) ThemeManager - PRIDAŤ:**
```csharp
public class ThemeManager : ViewModelBase
{
    // ... existing properties ...

    // Zebra Row Background Colors
    public SolidColorBrush ZebraRowEvenBackground =>
        Options?.EnableZebraRows == true
            ? ParseColor(Options.ZebraRowEvenBackgroundColor)
            : CellDefaultBackground;

    public SolidColorBrush ZebraRowOddBackground =>
        Options?.EnableZebraRows == true
            ? ParseColor(Options.ZebraRowOddBackgroundColor)
            : CellDefaultBackground;

    // Zebra Row Foreground Colors
    public SolidColorBrush ZebraRowEvenForeground =>
        Options?.EnableZebraRows == true
            ? ParseColor(Options.ZebraRowEvenForegroundColor)
            : CellDefaultForeground;

    public SolidColorBrush ZebraRowOddForeground =>
        Options?.EnableZebraRows == true
            ? ParseColor(Options.ZebraRowOddForegroundColor)
            : CellDefaultForeground;
}
```

**C) CellViewModel - APPLY ZEBRA COLOR (Background + Foreground):**
```csharp
// UpdateVisualState() metóda
private void UpdateVisualState()
{
    // Priority: Validation > Selected > SearchFound > Zebra > Default

    if (_isValidationError)
    {
        _backgroundBrush = _themeManager?.CellValidationErrorBackground ?? /* fallback */;
        _foregroundBrush = _themeManager?.CellValidationErrorForeground ?? /* fallback */;
        // ... validation styling ...
    }
    else if (_isSelected || _isRowSelected)
    {
        _backgroundBrush = _themeManager?.CellSelectedBackground ?? /* fallback */;
        _foregroundBrush = _themeManager?.CellSelectedForeground ?? /* fallback */;
        // ... selection styling ...
    }
    else if (_isSearchFound)
    {
        _backgroundBrush = _themeManager?.CellSearchMatchBackground ?? /* fallback */;
        _foregroundBrush = _themeManager?.CellSearchMatchForeground ?? /* fallback */;
        // ... search highlight styling ...
    }
    else if (_themeManager?.Options?.EnableZebraRows == true)
    {
        // NOVÉ: Zebra rows (background + foreground)
        bool isEvenRow = (RowIndex % 2 == 0);
        _backgroundBrush = isEvenRow
            ? _themeManager.ZebraRowEvenBackground
            : _themeManager.ZebraRowOddBackground;
        _foregroundBrush = isEvenRow
            ? _themeManager.ZebraRowEvenForeground
            : _themeManager.ZebraRowOddForeground;
    }
    else
    {
        // Default
        _backgroundBrush = _themeManager?.CellDefaultBackground ?? /* fallback */;
        _foregroundBrush = _themeManager?.CellDefaultForeground ?? /* fallback */;
    }

    // ... rest of styling ...
}
```

**D) Usage example (entry point):**
```csharp
// Example 1: Default zebra rows (white/light gray background, black text)
var options1 = new AdvancedDataGridOptions
{
    EnableZebraRows = true
    // Uses defaults:
    // - Even rows: #FFFFFF background, #000000 text
    // - Odd rows: #F5F5F5 background, #000000 text
};

// Example 2: Custom background colors, default text
var options2 = new AdvancedDataGridOptions
{
    EnableZebraRows = true,
    ZebraRowEvenBackgroundColor = "#FFFFFF",   // White
    ZebraRowOddBackgroundColor = "#E8F4F8"     // Light blue
    // Text color inherits default (#000000 black)
};

// Example 3: Custom background AND text colors
var options3 = new AdvancedDataGridOptions
{
    EnableZebraRows = true,
    ZebraRowEvenBackgroundColor = "#FFFFFF",   // White background
    ZebraRowEvenForegroundColor = "#000000",   // Black text
    ZebraRowOddBackgroundColor = "#2C3E50",    // Dark blue background
    ZebraRowOddForegroundColor = "#ECF0F1"     // Light gray text
};
```

#### **Výhody:**
✅ Konfigurovateľné farby (background + foreground)
✅ Default hodnoty (white / light gray background, black text)
✅ Optional (enable/disable)
✅ Rešpektuje priority (validation > selected > zebra)
✅ Flexibilita: môžete nastaviť len background alebo aj background + foreground

#### **Čas:** **1.5-2 hodiny**
- 30min: Pridať options (EnableZebraRows, 4 color properties)
- 30min: ThemeManager properties (background + foreground)
- 30min: Logika v CellViewModel.UpdateVisualState()
- 30min: Testing

---

### **7. Search as Filter (nie next/back)** 🟠 P1

#### **Čo zmeniť:**

**PRED (aktuálne - DEPRECATED):**
```
User zadá "test" → naviguje na PRVÝ match (Next/Back buttons)
```

**PO (nová logika):**
```
User zadá "test" → VYFILTRUJE všetky riadky obsahujúce "test"
Výsledok: Zobrazí iba matching rows (ako Excel filter)
```

#### **Implementácia:**

**A) SearchPanelViewModel - PRIDAŤ:**
```csharp
public class SearchPanelViewModel : ViewModelBase
{
    // ... existing properties ...

    // NOVÉ: Column selector pre search
    public ObservableCollection<string> SelectedColumns { get; set; } = new();
    public bool SearchInAllColumns { get; set; } = true;

    // DEPRECATED: Next/Back funkcionalita (zachovať pre backward compatibility)
    [Obsolete("Use SearchAsFilter instead. Next/Back navigation will be removed in future version.")]
    public int CurrentMatchIndex { get; set; }
}
```

**B) SearchService - NOVÁ METÓDA:**
```csharp
// Features/Search/Services/SearchService.cs

// NOVÁ METÓDA: Search as filter
public async Task<List<string>> SearchAndFilterAsync(
    string searchText,
    string[]? targetColumns = null,
    bool caseSensitive = false)
{
    if (string.IsNullOrWhiteSpace(searchText))
    {
        // Clear search filter
        await _filterService.ClearSearchFilterAsync();
        return new List<string>();
    }

    // Volá HybridRowStore.SearchAsync() → FTS5 search → vracia rowIds
    var matchedRowIds = await _rowStore.SearchAsync(searchText, targetColumns, caseSensitive);

    _logger.LogInformation("Search found {Count} matching rows for '{SearchText}'",
        matchedRowIds.Count, searchText);

    // Aplikuje filter (zobrazí iba matching rows)
    await _filterService.SetSearchFilterAsync(matchedRowIds);

    return matchedRowIds;
}

// DEPRECATED: Zachované pre backward compatibility
[Obsolete("Use SearchAndFilterAsync instead. This method will be removed in future version.")]
public async Task<SearchResult> FindNextAsync(string searchText)
{
    // ... existing next/back logic ...
}

[Obsolete("Use SearchAndFilterAsync instead. This method will be removed in future version.")]
public async Task<SearchResult> FindPreviousAsync(string searchText)
{
    // ... existing next/back logic ...
}
```

**C) FilterService - PRIDAŤ SEARCH FILTER SUPPORT:**
```csharp
// Features/Filter/Services/FilterService.cs

private List<string>? _searchFilterRowIds;  // RowIds from search

public async Task SetSearchFilterAsync(List<string> matchedRowIds)
{
    _searchFilterRowIds = matchedRowIds;
    await ApplyCombinedFiltersAsync();  // Combine with column filters
}

public async Task ClearSearchFilterAsync()
{
    _searchFilterRowIds = null;
    await ApplyCombinedFiltersAsync();
}

private async Task ApplyCombinedFiltersAsync()
{
    // Combine search filter + column filters
    // SQL: WHERE __rowId IN (...searchRowIds...) AND (column filters...)
}
```

**D) HybridRowStore.SearchAsync():**
✅ **UŽ EXISTUJE** (riadky 649-740)
- Používa FTS5 full-text search
- Podporuje targetColumns
- Vracia List<string> matchedRowIds

**E) MODULAR XAML - SEARCH PANEL:**

**NOVÝ SÚBOR:** `UIControls/Search/SearchPanelTemplate.xaml`
```xaml
<!-- REUSABLE COMPONENT: Search Panel -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <DataTemplate x:Key="SearchPanelTemplate">
        <StackPanel Orientation="Horizontal" Spacing="8">
            <!-- Search TextBox -->
            <TextBox Header="Search:"
                     Text="{Binding SearchText, Mode=TwoWay}"
                     PlaceholderText="Enter search term..."
                     Width="200" />

            <!-- Column selector -->
            <ComboBox Header="Search in:" Width="150">
                <ComboBoxItem Content="All Columns" IsSelected="True" />
                <ComboBoxItem Content="Selected Columns..." />
            </ComboBox>

            <!-- Action Buttons -->
            <ContentControl Content="{Binding}"
                           ContentTemplate="{StaticResource SearchButtonsTemplate}"
                           VerticalAlignment="Bottom" />

            <!-- DEPRECATED: Next/Back buttons -->
            <ContentControl Content="{Binding}"
                           ContentTemplate="{StaticResource DeprecatedNavigationButtonsTemplate}"
                           Visibility="Collapsed"
                           VerticalAlignment="Bottom" />
        </StackPanel>
    </DataTemplate>

    <!-- SEARCH BUTTONS (SEPARATE REUSABLE COMPONENT) -->
    <DataTemplate x:Key="SearchButtonsTemplate">
        <StackPanel Orientation="Horizontal" Spacing="4">
            <Button Content="Search"
                    Command="{Binding SearchCommand}"
                    Style="{StaticResource AccentButtonStyle}" />
            <Button Content="Clear"
                    Command="{Binding ClearSearchCommand}" />
        </StackPanel>
    </DataTemplate>

    <!-- DEPRECATED NAVIGATION BUTTONS (SEPARATE - HIDDEN BY DEFAULT) -->
    <DataTemplate x:Key="DeprecatedNavigationButtonsTemplate">
        <StackPanel Orientation="Horizontal" Spacing="4"
                    ToolTipService.ToolTip="Deprecated: Use Search button instead">
            <Button Content="◄ Back"
                    Command="{Binding FindPreviousCommand}"
                    IsEnabled="False" />
            <Button Content="Next ►"
                    Command="{Binding FindNextCommand}"
                    IsEnabled="False" />
        </StackPanel>
    </DataTemplate>

</ResourceDictionary>
```

#### **Výhody:**
✅ Excel-like search UX (filter výsledky)
✅ FTS5 optimalizované (< 200ms pri 10M rows)
✅ Backward compatible (deprecated Next/Back zachované)
✅ **MODULAR XAML:** 3 separate templates (Panel, Buttons, DeprecatedButtons)

#### **Čas:** **5-6 hodín**
- 2h: Nová SearchAndFilterAsync() metóda + FilterService integration
- 2h: MODULAR XAML templates (SearchPanel, Buttons, DeprecatedButtons)
- 1h: Deprecation warnings ([Obsolete] attribute)
- 1h: Testing

---

### **8. Import Multi-Mode Support (UI rozšírenie)** 🟡 P2

#### **Čo to je:**
- **Import má viac módov:**
  - Append (pridať na koniec)
  - Replace (nahradiť všetky dáta)
  - Merge (zlúčiť podľa kľúča)
  - Insert At Position
- **UI musí vedieť s týmto pracovať:**
  - Dropdown selector pre import mode
  - Mode-specific options (napr. key column pre Merge)

#### **Implementácia:**

**A) Import Mode Enum (ak už neexistuje):**
```csharp
// Api/Models/ImportModels.cs
public enum ImportMode
{
    Append,        // Add rows to end
    Replace,       // Clear all, then import
    Merge,         // Merge by key column
    InsertAtPosition  // Insert at specific index
}
```

**B) MODULAR XAML - IMPORT MODE SELECTOR:**

**NOVÝ SÚBOR:** `UIControls/Import/ImportModeSelector.xaml`
```xaml
<!-- REUSABLE COMPONENT: Import Mode Selector -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <DataTemplate x:Key="ImportModeSelectorTemplate">
        <StackPanel Spacing="8">
            <!-- Mode Selector -->
            <ComboBox Header="Import Mode:"
                      SelectedIndex="{Binding SelectedImportMode, Mode=TwoWay}"
                      Width="200">
                <ComboBoxItem Content="Append (Add to End)" />
                <ComboBoxItem Content="Replace (Clear All)" />
                <ComboBoxItem Content="Merge (By Key)" />
                <ComboBoxItem Content="Insert At Position" />
            </ComboBox>

            <!-- Mode-Specific Options -->
            <ContentControl Content="{Binding}"
                           ContentTemplate="{StaticResource ImportModeOptionsTemplate}" />
        </StackPanel>
    </DataTemplate>

    <!-- MODE-SPECIFIC OPTIONS (SEPARATE REUSABLE COMPONENT) -->
    <DataTemplate x:Key="ImportModeOptionsTemplate">
        <StackPanel Spacing="4">
            <!-- Merge Mode: Key Column Selector -->
            <ComboBox Header="Key Column:"
                      ItemsSource="{Binding Columns}"
                      SelectedItem="{Binding MergeKeyColumn, Mode=TwoWay}"
                      Visibility="{Binding IsMergeMode, Mode=OneWay}"
                      Width="200" />

            <!-- Insert At Position Mode: Position Input -->
            <NumberBox Header="Insert Position:"
                       Value="{Binding InsertPosition, Mode=TwoWay}"
                       Minimum="0"
                       Visibility="{Binding IsInsertAtPositionMode, Mode=OneWay}"
                       Width="200" />
        </StackPanel>
    </DataTemplate>

</ResourceDictionary>
```

**C) Import Dialog (použitie):**
```xaml
<!-- ImportDialog.xaml -->
<StackPanel Spacing="16">
    <!-- File Picker -->
    <Button Content="Select File..." Command="{Binding SelectFileCommand}" />

    <!-- Import Mode Selector (MODULAR COMPONENT) -->
    <ContentControl Content="{Binding ImportViewModel}"
                   ContentTemplate="{StaticResource ImportModeSelectorTemplate}" />

    <!-- Import Button -->
    <Button Content="Import"
            Command="{Binding ImportCommand}"
            Style="{StaticResource AccentButtonStyle}" />
</StackPanel>
```

**D) ImportService - PODPORUJE VŠETKY MÓDY:**
```csharp
// Features/IO/Services/ImportService.cs
public async Task ImportAsync(
    string filePath,
    ImportMode mode,
    ImportOptions options,  // Contains: MergeKeyColumn, InsertPosition, etc.
    CancellationToken cancellationToken = default)
{
    switch (mode)
    {
        case ImportMode.Append:
            await ImportAppendAsync(filePath, cancellationToken);
            break;

        case ImportMode.Replace:
            await ImportReplaceAsync(filePath, cancellationToken);
            break;

        case ImportMode.Merge:
            await ImportMergeAsync(filePath, options.MergeKeyColumn, cancellationToken);
            break;

        case ImportMode.InsertAtPosition:
            await ImportInsertAsync(filePath, options.InsertPosition, cancellationToken);
            break;
    }
}
```

#### **Výhody:**
✅ Podporuje všetky import módy
✅ UI dynamicky zobrazuje mode-specific options
✅ **MODULAR XAML:** 2 separate templates (Selector, Options) - reusable

#### **Čas:** **3-4 hodiny**
- 1h: MODULAR XAML templates (ImportModeSelector, ImportModeOptions)
- 1h: ViewModel binding (SelectedImportMode, visibility converters)
- 1h: ImportService integration (ak treba rozšíriť)
- 1h: Testing

---

## 🎨 **UI MODULARIZÁCIA - SÚHRN**

### **Princíp:**
✅ **Separation of Concerns:** UI oddelené od kódu
✅ **Reusability:** Malé XAML komponenty znovupoužiteľné
✅ **Kompozícia:** Skladanie malých komponentov do väčších celkov
✅ **Professional Approach:** Ako v enterprise projektoch (nie zbytočné rozdrobenie)

### **Štruktúra XAML súborov:**

```
UIControls/
├── FilterFlyout/
│   ├── FilterFlyoutTemplate.xaml        (Main flyout layout)
│   ├── FilterValueListTemplate.xaml     (Checkbox list)
│   └── FilterButtonsTemplate.xaml       (OK/Cancel/Clear buttons)
│
├── Checkbox/
│   ├── CheckboxHeaderTemplate.xaml      (3-state header checkbox)
│   └── CheckboxCellTemplate.xaml        (Cell checkbox with styling)
│
├── ContextMenu/
│   ├── CellContextMenuTemplate.xaml     (Cell right-click menu)
│   └── RowContextMenuTemplate.xaml      (Row right-click menu)
│
├── Sort/
│   └── SortIndicatorTemplate.xaml       (▲/▼ indicator)
│
├── Search/
│   ├── SearchPanelTemplate.xaml         (Main search panel)
│   ├── SearchButtonsTemplate.xaml       (Search/Clear buttons)
│   └── DeprecatedNavigationTemplate.xaml (Next/Back - deprecated)
│
└── Import/
    ├── ImportModeSelectorTemplate.xaml  (Import mode dropdown)
    └── ImportModeOptionsTemplate.xaml   (Mode-specific options)
```

### **Výhody modularizácie:**
✅ **Reusable:** Templates sa dajú použiť v iných častiach UI
✅ **Maintainable:** Zmena v template sa propaguje všade
✅ **Testable:** Malé komponenty sa dajú ľahšie testovať
✅ **Readable:** Menšie súbory sú prehľadnejšie
✅ **Professional:** Štandard v enterprise WinUI/WPF aplikáciách

### **Príklad kompozície:**
```xaml
<!-- AdvancedDataGridControl.xaml - KOMPOZÍCIA -->
<Grid>
    <!-- Search Panel (MODULAR) -->
    <ContentControl ContentTemplate="{StaticResource SearchPanelTemplate}" />

    <!-- Headers Row -->
    <ItemsRepeater>
        <!-- Column Header (MODULAR) -->
        <DataTemplate>
            <StackPanel>
                <!-- Header Text -->
                <TextBlock Text="{Binding DisplayName}" />

                <!-- Sort Indicator (MODULAR) -->
                <ContentControl ContentTemplate="{StaticResource SortIndicatorTemplate}" />

                <!-- Filter Flyout (MODULAR) -->
                <MenuFlyout>
                    <ContentControl ContentTemplate="{StaticResource FilterFlyoutTemplate}" />
                </MenuFlyout>
            </StackPanel>
        </DataTemplate>
    </ItemsRepeater>

    <!-- Data Cells -->
    <!-- ... -->
</Grid>
```

---

## ⏱️ **ČASOVÝ ODHAD (CELKOM - FINÁLNY)**

| Fáza | Features | Čas |
|------|----------|-----|
| **Fáza 1: Filtrovanie** | Excel-like filter flyout (checkbox + regex) + MODULAR XAML | **9-11h** |
| **Fáza 2: Checkbox Column KOMPLETNÉ** | 3-state header + Cell UI z WinUI.TableView + MODULAR XAML | **6-8h** |
| **Fáza 3: Column Resize KOMPLETNÉ** | Drag & Drop z WinUI.TableView + zachovať ValidationAlerts logic | **6-8h** |
| **Fáza 4: Context Menu s Insert Row** | Cell/Row context menu + Insert Row Above/Below + MODULAR XAML | **4-5h** |
| **Fáza 5: Hľadanie** | Search as filter (nie next/back) + Deprecation + MODULAR XAML | **5-6h** |
| **Fáza 6: Sortovanie** | Sort on header click (3-state) + MODULAR XAML | **2-3h** |
| **Fáza 7: Zebra Rows** | Alternate row colors (konfigurovateľné) | **1-2h** |
| **Fáza 8: Import Multi-Mode** | Import mode selector UI + MODULAR XAML | **3-4h** |
| **Testing & Integration** | Comprehensive testing + Deprecation cleanup + XAML modularizácia review | **7-9h** |

### **CELKOM:** **43-56 hodín** (5.5 - 7 pracovných dní)

---

## 🗑️ **DEPRECATION STRATEGY**

### **Princíp:**
1. ✅ **Označiť staré metódy ako [Obsolete]**
2. ✅ **Zachovať funkčnosť (backward compatibility)**
3. ✅ **Pridať warning message**
4. ✅ **Po testovaní (2-4 týždne) → vymazať**

### **Príklad:**

```csharp
// SearchService.cs

[Obsolete("Use SearchAndFilterAsync instead. This method will be removed in v2.0.", false)]
public async Task<SearchResult> FindNextAsync(string searchText)
{
    _logger.LogWarning("FindNextAsync is deprecated. Please migrate to SearchAndFilterAsync.");
    // ... existing implementation (zachované) ...
}

// Po 2-4 týždňoch testovania:
[Obsolete("Use SearchAndFilterAsync instead. This method will be removed in v2.0.", true)]  // Error namiesto Warning
public async Task<SearchResult> FindNextAsync(string searchText) { ... }

// Po ďalších 2-4 týždňoch:
// Vymazať úplne
```

### **Čo označiť ako deprecated:**

| Metóda/Property | Dôvod | Náhrada |
|-----------------|-------|---------|
| `SearchService.FindNextAsync()` | Search as filter namiesto next/back | `SearchAndFilterAsync()` |
| `SearchService.FindPreviousAsync()` | Search as filter namiesto next/back | `SearchAndFilterAsync()` |
| `SearchPanelViewModel.CurrentMatchIndex` | Nepotrebný po zmene na filter | `SearchResultCount` |
| UI buttons "Next" / "Back" v SearchPanel | Nahradené "Search" buttonom | `OnSearchClicked()` |

---

## ✅ **ODPORÚČANIE**

### **🟢 ÁNO, DÁ SA TO UROBIŤ**

**Prečo:**
1. ✅ **Máte solídny základ:**
   - Column resize (základné) ✅
   - Checkbox column (basic) ✅
   - **Insert Row Above/Below funkcionalita** ✅ (docu_InsertRowFunctionality.md)
   - Pagination ✅
   - ThemeManager ✅
   - SQL backend (filter/sort/search) ✅

2. ✅ **WinUI.TableView UI prvky sa dajú extrahovať:**
   - Excel-like filter flyout (XAML template)
   - 3-state checkbox (Indeterminate glyph)
   - **Cell checkbox UI styling**
   - **Column resize implementation (plynulý, realtime)**
   - Context menu patterns
   - Sort indicators (▲/▼)

3. ✅ **Váš SQL backend je LEPŠÍ:**
   - HybridRowStore už má `SearchAsync()` s FTS5
   - `SetFilterCriteria()` s SQL WHERE
   - `SetSortCriteria()` s SQL ORDER BY
   - Regex support (pridáme FilterOperator.Regex)
   - **Insert Row funkcionalita** (InsertRowBeforeAsync, InsertRowAfterAsync)

4. ✅ **Čistý approach:**
   - Nemusíte integrovať celú knižnicu
   - Kopírujete len UI patterns
   - Zachováte vašu architektúru
   - Deprecation strategy (bezpečné odstránenie starého kódu)
   - **UI modularizácia** (professional approach)

5. ✅ **Konfigurovateľné farby:**
   - Default hodnoty (white / light gray)
   - Entry point customization (AdvancedDataGridOptions)
   - ThemeManager integration

6. ✅ **Zachováte special logic:**
   - ValidationAlerts "*" → fixed width po resize
   - Entry point API compatibility
   - **Insert Row Above/Below** (existujúca funkcionalita)

7. ✅ **Import Multi-Mode:**
   - UI vie pracovať s rôznymi import módmi
   - Mode-specific options (Merge key column, Insert position)

---

## 📋 **IMPLEMENTAČNÝ PLÁN**

### **1. PREREQUISITE (6-8 dní):**
📄 Dokončiť **Adaptive Storage Strategy** podľa `docu_AdaptiveStorageStrategy.md`

### **2. UI VYLEPŠENIA (5.5-7 dní):**

#### **Prioritný poriadok:**

**Week 1:**
- ✅ **Fáza 1:** Excel-like filter flyout (checkbox + regex) + MODULAR XAML (9-11h)
- ✅ **Fáza 2:** 3-state checkbox header + Cell UI styling + MODULAR XAML (6-8h)

**Week 2:**
- ✅ **Fáza 3:** Column resize z WinUI.TableView (6-8h)
- ✅ **Fáza 4:** Context menu s Insert Row + MODULAR XAML (4-5h)
- ✅ **Fáza 5:** Search as filter + Deprecation + MODULAR XAML (5-6h)

**Week 3:**
- ✅ **Fáza 6:** Sort on header click (3-state) + MODULAR XAML (2-3h)
- ✅ **Fáza 7:** Zebra rows (konfigurovateľné) (1-2h)
- ✅ **Fáza 8:** Import multi-mode UI + MODULAR XAML (3-4h)
- ✅ **Testing & Integration** (7-9h)

---

## 🚨 **RIZIKÁ & MITIGATION**

| Riziko | Pravdepodobnosť | Mitigation |
|--------|-----------------|------------|
| **WinUI.TableView flyout XAML je príliš zviazaný s ich architektúrou** | STREDNÁ | Prepíšete flyout od nuly (cca +2h), používate ich len ako UX referencia |
| **SQLite REGEXP nie je built-in** | VYSOKÁ | Použite LIKE patterns alebo registrujte custom REGEXP function cez `SqliteConnection.CreateFunction()` |
| **FTS5 search je pomalé pri 10M rows** | NÍZKA | FTS5 je optimalizované (< 200ms pri 10M rows podľa dokumentácie) |
| **Zebra rows môžu spomaliť rendering** | NÍZKA | Color sa aplikuje len pri create/recycle ViewModelu (ViewportManager cache) |
| **Deprecated kód sa omylom zmaže** | STREDNÁ | 3-fázový deprecation (Warning → Error → Delete) s 2-4 týždňovými intervalmi |
| **Column resize je laggy po kopírovaní** | STREDNÁ | Použiť pointer capture + throttling (ak treba) |
| **Príliš veľa XAML súborov (over-modularization)** | STREDNÁ | Vytvárať moduly len kde to dáva zmysel (filter flyout, checkbox, search) |

---

## 📊 **ZÁVER**

### **✅ ÁNO, ODPORÚČAM TÚTO CESTU**

**Prečo:**
1. **Rýchlejšie** než integrácia celej WinUI.TableView (43-56h vs. týždne synchronizačnej logiky)
2. **Zachováte architektúru** (Hybrid SQLite, ViewportManager, headless mode)
3. **Kontrola nad kódom** (viete presne čo robí každý riadok)
4. **Získate best-of-both:**
   - Váš SQL backend (performance + regex support)
   - Ich UI patterns (UX + styling + smooth resize)
   - **Vaša Insert Row funkcionalita** (zachovaná)
5. **Bezpečná migrácia:**
   - Deprecation strategy
   - Backward compatibility
   - Postupné odstránenie starého kódu
6. **Kompletné riešenie:**
   - Header 3-state checkbox
   - **Cell checkbox UI styling**
   - **Plynulý column resize**
   - ValidationAlerts special logic
   - **Context menu s Insert Row Above/Below**
   - **Import multi-mode support**
7. **Professional UI:**
   - **Modular XAML komponenty** (reusable)
   - **Separation of concerns** (UI oddelené od kódu)
   - **Kompozícia** (skladanie malých komponentov)

### **Workflow:**
```
1. Adaptive Storage (6-8 dní) → PREREQUISITE
2. Excel Filter Flyout + Regex + MODULAR XAML (1.5 dňa) → NAJDÔLEŽITEJŠIE
3. Checkbox KOMPLETNÉ (header + cells UI) + MODULAR XAML (1 deň)
4. Column Resize KOMPLETNÉ (z WinUI.TableView) (1 deň)
5. Context Menu s Insert Row + MODULAR XAML (0.5 dňa)
6. Search as Filter + Deprecation + MODULAR XAML (1 deň)
7. Sort 3-state + Zebra + Import Multi-Mode + MODULAR XAML (1 deň)
8. Testing (1 deň)
```

**CELKOM:** **13-15 dní** (Adaptive Storage + UI vylepšenia + XAML modularizácia)

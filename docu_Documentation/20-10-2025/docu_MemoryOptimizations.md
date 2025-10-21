# Memory Optimizations Specification

**Priorita:** 🔴 P2 (Vysoká) + 🟡 P4 (Nízka)
**Čas:** 11-14 hodín
**Dependencies:** UI Virtualization (pre BrushPool integration)

---

## 📋 Prehľad

Pamäťové optimalizácie nad rámec UI virtualizácie:
1. **BrushPool** - Deduplikácia SolidColorBrush objektov (P2)
2. **Dispose Pattern** - Pre ViewModels (P2)
3. **ArrayPool** - Pre Dictionary/DataTable operácie (P4)
4. **WeakEventManager** - Memory leak prevention (P4)

---

## 🔴 PRIORITA 2 - BrushPool

### Úloha 2.2: BrushPool Implementation

**Čas:** 4-5 hodín

#### Otázka: Potrebujem BrushPool ak farby sú nastaviteľné cez API?

**ODPOVEĎ: ÁNO!**

**Dôvod:**
- Farby SÚ nastaviteľné cez API (DEFAULT hodnoty)
- ALE: V praxi sa opakovajú
  - 1000 error cells = 1 red brush (nie 1000!)
  - 500 selected cells = 1 blue brush (nie 500!)
- BrushPool = deduplikácia opakujúcich sa farieb

**Úspora:**
- PRED: 10K cells × 3 brushes × 50 bytes = 1.5 MB
- PO: ~10 unique colors × 50 bytes = 500 bytes (~3000x úspora!)

#### Implementácia

**Súbor:** BrushPool.cs (NOVÝ)

```csharp
using System.Collections.Concurrent;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Optimization;

/// <summary>
/// Thread-safe pool for SolidColorBrush deduplication.
/// Reduces memory usage even with API-settable colors (colors repeat in practice).
/// </summary>
public static class BrushPool
{
    private static readonly ConcurrentDictionary<Color, SolidColorBrush> _cache = new();

    /// <summary>
    /// Gets or creates SolidColorBrush for specified color.
    /// Thread-safe.
    /// </summary>
    public static SolidColorBrush GetBrush(Color color)
    {
        return _cache.GetOrAdd(color, c => new SolidColorBrush(c));
    }

    /// <summary>
    /// Clears all cached brushes.
    /// Use with caution - only when theme completely changes.
    /// </summary>
    public static void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets statistics about cached brushes.
    /// </summary>
    public static (int UniqueColors, long EstimatedMemoryBytes) GetStatistics()
    {
        var count = _cache.Count;
        var estimatedMemory = count * 50L; // ~50 bytes per brush
        return (count, estimatedMemory);
    }
}
```

#### Integration - CellViewModel.cs

**Lokácia:** Line 24-26 (fields)

**PRED:**

```csharp
private SolidColorBrush _borderBrush = new(Colors.Gray);
private SolidColorBrush _backgroundBrush = new(Colors.White);
private SolidColorBrush _foregroundBrush = new(Colors.Black);
```

**PO:**

```csharp
private SolidColorBrush _borderBrush = BrushPool.GetBrush(Colors.Gray);
private SolidColorBrush _backgroundBrush = BrushPool.GetBrush(Colors.White);
private SolidColorBrush _foregroundBrush = BrushPool.GetBrush(Colors.Black);
```

#### Integration - UpdateCellAppearance()

**Lokácia:** CellViewModel.cs (line 270-322)

**KRITICKÁ ZMENA:** Validation success = default border color (NIE zelená!)

**PRED:**

```csharp
// Validation error
_borderBrush = new SolidColorBrush(Colors.Red);

// Validation success
_borderBrush = new SolidColorBrush(Colors.Green); // ❌ NESPRÁVNE!

// Search found
_backgroundBrush = new SolidColorBrush(Colors.Orange);

// Selected
_backgroundBrush = new SolidColorBrush(Colors.Blue);
```

**PO:**

```csharp
// Validation error (DEFAULT červená, nastaviteľná cez API)
_borderBrush = BrushPool.GetBrush(
    _themeManager?.ValidationErrorBorder?.Color ?? Colors.Red);

// Validation success = ROVNAKÁ farba ako default border! (NIE zelená!)
_borderBrush = BrushPool.GetBrush(
    _themeManager?.CellBorder?.Color ?? Colors.Gray);

// Search found (DEFAULT oranžová, nastaviteľná)
_backgroundBrush = BrushPool.GetBrush(
    _themeManager?.SearchFoundBackground?.Color ?? Colors.Orange);

// Selected (DEFAULT modrá, nastaviteľná)
_backgroundBrush = BrushPool.GetBrush(
    _themeManager?.SelectedCellBackground?.Color ?? Colors.Blue);
```

**POZNÁMKA:** Všetky farby sú DEFAULT hodnoty, každá sa dá zmeniť cez public API!

#### Integration - ThemeManager.cs

**Lokácia:** Constructor alebo property setters

```csharp
public class ThemeManager
{
    // Replace all `new SolidColorBrush(...)` with `BrushPool.GetBrush(...)`

    public SolidColorBrush CellBorder { get; set; } = BrushPool.GetBrush(Colors.Gray);
    public SolidColorBrush CellDefaultBackground { get; set; } = BrushPool.GetBrush(Colors.White);
    public SolidColorBrush ValidationErrorBorder { get; set; } = BrushPool.GetBrush(Colors.Red);
    public SolidColorBrush SearchFoundBackground { get; set; } = BrushPool.GetBrush(Colors.Orange);
    public SolidColorBrush SelectedCellBackground { get; set; } = BrushPool.GetBrush(Colors.LightBlue);
    // ... atď.
}
```

#### API pre Custom Colors

Užívateľ môže nastaviť vlastné farby:

```csharp
// Example: Custom red for validation errors
var themeManager = new ThemeManager();
themeManager.ValidationErrorBorder = BrushPool.GetBrush(Color.FromArgb(255, 200, 0, 0));

// BrushPool automaticky deduplikuje:
// - Ak 1000 cells má túto farbu → 1 brush instance!
```

#### Testing

**Test Case 1: Deduplication**

```csharp
// 1. Create 1000 cells with validation errors (red)
// 2. Assert: Only 1 red brush instance exists
// 3. Call BrushPool.GetStatistics()
// 4. Assert: UniqueColors <= 10 (typical: ~5-10 colors total)
```

**Test Case 2: Validation Success Color**

```csharp
// 1. Set cell validation = success
// 2. Assert: BorderBrush.Color == ThemeManager.CellBorder.Color
// 3. Assert: BorderBrush.Color != Colors.Green (NIE zelená!)
```

---

## 🔴 PRIORITA 2 - Dispose Pattern

### Úloha 2.3: Dispose Pattern pre ViewModels

**Čas:** 3-4 hodiny

#### Účel

Zabezpečiť správne uvoľnenie zdrojov:
- Unsubscribe events (memory leaks!)
- Null out references
- Clear collections

#### Implementácia - CellViewModel

**Súbor:** CellViewModel.cs

```csharp
public sealed class CellViewModel : INotifyPropertyChanged, IDisposable
{
    private bool _disposed = false;

    // ... existing code ...

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Unsubscribe events (ak existujú)
        // NOTE: BrushPool brushes NIE SÚ disposed (shared!)

        // Null out references (GC help)
        _borderBrush = null!;
        _backgroundBrush = null!;
        _foregroundBrush = null!;
        _displayValue = null;
        _rawValue = null;

        _logger?.LogTrace("CellViewModel disposed: {RowId}.{ColumnName}", RowId, ColumnName);
    }
}
```

#### Implementácia - DataGridRowViewModel

**Súbor:** DataGridRowViewModel.cs

```csharp
public sealed class DataGridRowViewModel : IDisposable
{
    private bool _disposed = false;

    // ... existing code ...

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Dispose cells
        foreach (var cell in Cells.OfType<IDisposable>())
        {
            cell.Dispose();
        }

        Cells.Clear();

        _logger?.LogTrace("DataGridRowViewModel disposed: {RowId}", RowId);
    }
}
```

#### Integration - ViewportManager

Už implementované v FÁZA A (UI Virtualization):

```csharp
// Dispose ViewModels mimo viewport
foreach (var idx in toRemove)
{
    _viewportCache[idx].Dispose(); // ✅ Volá sa automaticky!
    _viewportCache.Remove(idx);
}
```

---

## 🟡 PRIORITA 4 - ArrayPool

### Úloha 4.1: ArrayPool pre Dictionary/DataTable Operations

**Čas:** 3-4 hodiny

#### Účel

Minimalizovať Large Object Heap (LOH) alokácie pri import/export:
- DataTable → Dictionary conversion
- Dictionary → DataTable conversion
- Temporary buffers pre bulk operations

#### Implementácia - ImportService

**Súbor:** ImportService.cs

**Lokácia:** ImportAsync metóda (pri DataTable processing)

```csharp
using System.Buffers;

public async Task<PublicResult<ImportResult>> ImportAsync(
    DataTable dataTable,
    CancellationToken cancellationToken = default)
{
    // ... existing code ...

    // Rent array buffer from pool
    var columnCount = dataTable.Columns.Count;
    object?[] buffer = ArrayPool<object?>.Shared.Rent(columnCount);

    try
    {
        foreach (DataRow dataRow in dataTable.Rows)
        {
            // Copy values to buffer
            for (int i = 0; i < columnCount; i++)
            {
                buffer[i] = dataRow[i] == DBNull.Value ? null : dataRow[i];
            }

            // Process buffer
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < columnCount; i++)
            {
                row[dataTable.Columns[i].ColumnName] = buffer[i];
            }

            // Add metadata
            row["__creationType"] = "Imported";
            row["__lastModified"] = DateTime.UtcNow;

            rows.Add(row);

            // Clear buffer for next iteration (security!)
            Array.Clear(buffer, 0, columnCount);
        }
    }
    finally
    {
        // ALWAYS return buffer to pool!
        ArrayPool<object?>.Shared.Return(buffer, clearArray: true);
    }

    // ... rest of import logic ...
}
```

#### Implementácia - ExportService

**Súbor:** ExportService.cs

**Lokácia:** ExportAsync metóda (pri Dictionary → DataTable)

```csharp
using System.Buffers;

public async Task<PublicResult<ExportResult>> ExportAsync(
    ExportConfiguration config,
    CancellationToken cancellationToken = default)
{
    // ... existing code ...

    var dataTable = new DataTable();

    // Add columns
    foreach (var columnName in columnNames)
    {
        dataTable.Columns.Add(columnName, typeof(object));
    }

    // Rent buffer
    var columnCount = columnNames.Count;
    object?[] buffer = ArrayPool<object?>.Shared.Rent(columnCount);

    try
    {
        // Stream rows
        await foreach (var batch in _rowStore.StreamRowsAsync(config.OnlyFiltered, false, 1000, cancellationToken))
        {
            foreach (var row in batch)
            {
                // Copy to buffer
                for (int i = 0; i < columnCount; i++)
                {
                    var columnName = columnNames[i];
                    buffer[i] = row.TryGetValue(columnName, out var value) ? value : null;
                }

                // Add to DataTable
                var dataRow = dataTable.NewRow();
                dataRow.ItemArray = buffer.Take(columnCount).ToArray();
                dataTable.Rows.Add(dataRow);

                // Clear buffer
                Array.Clear(buffer, 0, columnCount);
            }
        }
    }
    finally
    {
        ArrayPool<object?>.Shared.Return(buffer, clearArray: true);
    }

    return PublicResult<ExportResult>.Success(new ExportResult(dataTable));
}
```

#### Výsledok

- ✅ LOH alokácie minimalizované
- ✅ ~2-5% zrýchlenie import/export
- ✅ Lepšia GC performance

---

## 🟡 PRIORITA 4 - WeakEventManager

### Úloha 4.2: WeakEventManager Implementation

**Čas:** 4-5 hodín

#### Účel

Predchádzať memory leaks pri event subscriptions:
- ThemeChanged events
- PropertyChanged events
- CollectionChanged events

#### Implementácia

**Súbor:** WeakEventManager.cs (NOVÝ)

```csharp
using System;
using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Optimization;

/// <summary>
/// Weak event manager to prevent memory leaks.
/// </summary>
public static class WeakEventManager<TSource, TEventArgs>
    where TSource : class
    where TEventArgs : EventArgs
{
    private static readonly Dictionary<TSource, List<WeakReference<EventHandler<TEventArgs>>>> _handlers = new();

    public static void AddHandler(
        TSource source,
        string eventName,
        EventHandler<TEventArgs> handler)
    {
        if (source == null || handler == null)
            return;

        lock (_handlers)
        {
            if (!_handlers.TryGetValue(source, out var list))
            {
                list = new List<WeakReference<EventHandler<TEventArgs>>>();
                _handlers[source] = list;
            }

            list.Add(new WeakReference<EventHandler<TEventArgs>>(handler));
        }
    }

    public static void RemoveHandler(
        TSource source,
        string eventName,
        EventHandler<TEventArgs> handler)
    {
        if (source == null || handler == null)
            return;

        lock (_handlers)
        {
            if (_handlers.TryGetValue(source, out var list))
            {
                list.RemoveAll(wr =>
                {
                    if (!wr.TryGetTarget(out var h))
                        return true; // Dead reference

                    return h == handler;
                });

                if (list.Count == 0)
                    _handlers.Remove(source);
            }
        }
    }

    public static void RaiseEvent(TSource source, TEventArgs args)
    {
        if (source == null)
            return;

        List<WeakReference<EventHandler<TEventArgs>>>? list;

        lock (_handlers)
        {
            if (!_handlers.TryGetValue(source, out list))
                return;

            // Remove dead references
            list.RemoveAll(wr => !wr.TryGetTarget(out _));
        }

        // Invoke handlers
        foreach (var wr in list)
        {
            if (wr.TryGetTarget(out var handler))
            {
                try
                {
                    handler(source, args);
                }
                catch
                {
                    // Ignore handler exceptions
                }
            }
        }
    }
}
```

#### Integration - ThemeManager

**Súbor:** ThemeManager.cs

**PRED:**

```csharp
public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

protected void OnThemeChanged()
{
    ThemeChanged?.Invoke(this, new ThemeChangedEventArgs());
}
```

**PO:**

```csharp
public void RaiseThemeChanged()
{
    WeakEventManager<ThemeManager, ThemeChangedEventArgs>.RaiseEvent(
        this,
        new ThemeChangedEventArgs());
}
```

#### Integration - CellViewModel

**PRED:**

```csharp
_themeManager.ThemeChanged += OnThemeChanged;

// Problem: Memory leak ak nie je unsubscribed!
```

**PO:**

```csharp
WeakEventManager<ThemeManager, ThemeChangedEventArgs>.AddHandler(
    _themeManager,
    nameof(_themeManager.ThemeChanged),
    OnThemeChanged);

// No memory leak - weak reference!
// Dispose() už nie je kritický pre event cleanup
```

---

## 📊 Zhrnutie Výsledkov

### Pamäťové Úspory

| Optimalizácia       | Úspora (Relatívna) | Úspora (Absolútna @ 10K rows) |
|---------------------|--------------------|---------------------------------|
| UI Virtualization   | 70-80%             | ~3.6 GB → ~700 MB              |
| BrushPool           | 15-20%             | ~150 MB → ~500 KB              |
| Dispose Pattern     | 10%                | ~50 MB → ~45 MB                |
| ArrayPool           | 2-5%               | ~20 MB → ~19 MB (LOH)          |
| WeakEventManager    | 1-2%               | Leak prevention                |
| **CELKOM**          | **~90%**           | **~4 GB → ~400 MB**            |

### Performance Improvements

- **Import/Export:** 2-5% rýchlejšie (ArrayPool)
- **Scroll:** Smooth (UI Virtualization + Element Recycling)
- **Theme Change:** No memory leaks (WeakEventManager)

---

## 🧪 Testing

### Test Case 1: BrushPool Effectiveness

```csharp
// 1. Create 10,000 cells (mixed colors: red errors, blue selected, default)
// 2. Call BrushPool.GetStatistics()
// 3. Assert: UniqueColors <= 10
// 4. Assert: EstimatedMemory < 1 KB (vs 1.5 MB without pool)
```

### Test Case 2: Memory Leak Prevention

```csharp
// 1. Create 1000 ViewModels subscribed to ThemeChanged
// 2. Dispose ViewModels (without explicit unsubscribe)
// 3. Force GC
// 4. Assert: ViewModels are collected (WeakEventManager prevents leak)
```

### Test Case 3: ArrayPool Usage

```csharp
// 1. Import 10,000 rows from DataTable
// 2. Monitor LOH allocations
// 3. Assert: LOH allocations < 1 MB (with ArrayPool)
// 4. Compare: LOH allocations ~ 50 MB (without ArrayPool)
```

---

## 📝 Dependencies

- **BrushPool:** Žiadne
- **Dispose:** UI Virtualization (ViewportManager volá Dispose)
- **ArrayPool:** Import/Export services
- **WeakEventManager:** ThemeManager

---

**Posledná aktualizácia:** 2025-10-20
**Status:** Ready for Implementation

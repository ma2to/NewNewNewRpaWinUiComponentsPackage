# KOMPLETNÁ ŠPECIFIKÁCIA: POKROČILÝ KEYBOARD SHORTCUTS SYSTÉM PRE ADVANCEDWINUIDATAGRID

## 🏗️ ARCHITEKTONICKÉ PRINCÍPY & ŠTRUKTÚRA

### Clean Architecture + Command Pattern (Jednotná s ostatnými časťami)
- **Facade Layer**: `IAdvancedDataGridFacade` (public API)
- **Application Layer**: Command handlers, shortcut services (internal)
- **Core Layer**: Shortcut definitions, key combinations, value objects (internal)
- **Infrastructure Layer**: Event handling, key capture, UI integration (internal)
- **Hybrid Internal DI + Functional/OOP**: Kombinuje dependency injection s funkcionálnym programovaním

### SOLID Principles
- **Single Responsibility**: Každý shortcut command má jednu zodpovednosť
- **Open/Closed**: Rozšíriteľné pre nové shortcuts bez zmeny existujúceho kódu
- **Liskov Substitution**: Všetky shortcut commands implementujú spoločné interface
- **Interface Segregation**: Špecializované interfaces pre rôzne typy shortcuts
- **Dependency Inversion**: Facade závislí od abstrakcií, nie konkrétnych implementácií

### Architectural Principles Maintained (Jednotné s ostatnými časťami)
- **Clean Architecture**: Commands v Core layer, processing v Application layer
- **Hybrid DI**: Command factory methods s dependency injection support
- **Functional/OOP**: Immutable commands + encapsulated behavior
- **SOLID**: Single responsibility pre každý shortcut type
- **Event-Driven**: Asynchronous shortcut processing s event aggregation
- **Performance**: Key combination caching, fast lookup tables
- **Thread Safety**: Immutable shortcut definitions, atomic operations
- **Internal DI Registration**: Všetky shortcut časti budú registrované v InternalServiceRegistration.cs

## 🔄 BACKUP STRATEGY & IMPLEMENTATION APPROACH

### 1. Backup Strategy
- Vytvoriť `.oldbackup_timestamp` súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### 2. Implementation Replacement
- Kompletný refaktoring s command pattern a event-driven architecture
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## 📋 CORE VALUE OBJECTS & COMMAND PATTERN

### 1. **ShortcutTypes.cs** - Core Layer

```csharp
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Threading;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

#region Enums

/// <summary>
/// ENTERPRISE: Shortcut context enumeration for different operational modes
/// </summary>
internal enum ShortcutContext
{
    None = 0,
    Normal = 1,          // Normal grid navigation
    EditMode = 2,        // Cell editing mode
    Selection = 3,       // Multi-cell selection
    HeaderMode = 4,      // Column header operations
    RowMode = 5         // Row-level operations
}

/// <summary>
/// ENTERPRISE: Shortcut execution modes for different behaviors
/// </summary>
internal enum ShortcutExecutionMode
{
    Immediate,          // Execute immediately
    Buffered,           // Buffer and execute on next tick
    Conditional,        // Execute only if conditions met
    Queued             // Add to execution queue
}

/// <summary>
/// ENTERPRISE: Shortcut category for logical grouping
/// </summary>
internal enum ShortcutCategory
{
    Navigation,         // Arrow keys, Tab, Home, End
    Editing,           // Enter, Esc, F2
    Selection,         // Ctrl+A, Shift+Click
    DataManipulation,  // Ctrl+C, Ctrl+V, Delete
    View,             // Ctrl+Home, Ctrl+End
    Custom            // User-defined shortcuts
}

#endregion

#region Progress & Context Types

/// <summary>
/// ENTERPRISE: Shortcut execution progress reporting
/// CONSISTENT: Rovnaká štruktúra ako SortProgress a ExportProgress
/// </summary>
internal sealed record ShortcutProgress
{
    internal int ProcessedShortcuts { get; init; }
    internal int TotalShortcuts { get; init; }
    internal double CompletionPercentage => TotalShortcuts > 0 ? (double)ProcessedShortcuts / TotalShortcuts * 100 : 0;
    internal TimeSpan ElapsedTime { get; init; }
    internal string CurrentShortcut { get; init; } = string.Empty;
    internal ShortcutContext CurrentContext { get; init; } = ShortcutContext.None;
    internal string CurrentOperation { get; init; } = string.Empty;

    /// <summary>Estimated time remaining based on current progress</summary>
    internal TimeSpan? EstimatedTimeRemaining => ProcessedShortcuts > 0 && TotalShortcuts > ProcessedShortcuts
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalShortcuts - ProcessedShortcuts) / ProcessedShortcuts)
        : null;

    public ShortcutProgress() : this(0, 0, TimeSpan.Zero, "", ShortcutContext.None, "") { }

    public ShortcutProgress(int processedShortcuts, int totalShortcuts, TimeSpan elapsedTime,
        string currentShortcut, ShortcutContext currentContext, string currentOperation)
    {
        ProcessedShortcuts = processedShortcuts;
        TotalShortcuts = totalShortcuts;
        ElapsedTime = elapsedTime;
        CurrentShortcut = currentShortcut;
        CurrentContext = currentContext;
        CurrentOperation = currentOperation;
    }
}

/// <summary>
/// ENTERPRISE: Shortcut execution context with DI support
/// HYBRID DI: Poskytuje services pre custom shortcut functions
/// </summary>
internal sealed record ShortcutExecutionContext
{
    internal IServiceProvider? ServiceProvider { get; init; }
    internal IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
    internal cancellationToken cancellationToken { get; init; } = default;
    internal DateTime ExecutionTime { get; init; } = DateTime.UtcNow;
    internal ShortcutContext Context { get; init; } = ShortcutContext.Normal;
    internal object? SourceElement { get; init; }
    internal Point? CursorPosition { get; init; }
    internal bool PreventDefault { get; init; } = true;
}

#endregion

#region Core Shortcut Definitions

/// <summary>
/// ENTERPRISE: Key combination definition with modifier support
/// FUNCTIONAL: Immutable key combination with flexible composition
/// </summary>
internal sealed record KeyCombination
{
    internal Key PrimaryKey { get; init; } = Key.None;
    internal ModifierKeys Modifiers { get; init; } = ModifierKeys.None;
    internal string DisplayName { get; init; } = string.Empty;
    internal string Description { get; init; } = string.Empty;
    internal bool IsValid => PrimaryKey != Key.None;

    // Factory methods pre common combinations
    internal static KeyCombination Create(Key key, ModifierKeys modifiers = ModifierKeys.None) =>
        new()
        {
            PrimaryKey = key,
            Modifiers = modifiers,
            DisplayName = GenerateDisplayName(key, modifiers)
        };

    internal static KeyCombination Ctrl(Key key) =>
        Create(key, ModifierKeys.Control);

    internal static KeyCombination Shift(Key key) =>
        Create(key, ModifierKeys.Shift);

    internal static KeyCombination Alt(Key key) =>
        Create(key, ModifierKeys.Alt);

    internal static KeyCombination CtrlShift(Key key) =>
        Create(key, ModifierKeys.Control | ModifierKeys.Shift);

    private static string GenerateDisplayName(Key key, ModifierKeys modifiers)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");

        parts.Add(key.ToString());

        return string.Join("+", parts);
    }
}

/// <summary>
/// ENTERPRISE: Shortcut definition with comprehensive configuration
/// COMMAND PATTERN: Encapsulates shortcut behavior and execution logic
/// </summary>
internal sealed record ShortcutDefinition
{
    internal string Name { get; init; } = string.Empty;
    internal KeyCombination KeyCombination { get; init; } = new();
    internal ShortcutContext Context { get; init; } = ShortcutContext.Normal;
    internal ShortcutCategory Category { get; init; } = ShortcutCategory.Navigation;
    internal ShortcutExecutionMode ExecutionMode { get; init; } = ShortcutExecutionMode.Immediate;
    internal bool IsEnabled { get; init; } = true;
    internal int Priority { get; init; } = 0;
    internal string Description { get; init; } = string.Empty;
    internal Func<ShortcutExecutionContext, Task<ShortcutResult>>? Handler { get; init; }
    internal Func<ShortcutExecutionContext, bool>? CanExecute { get; init; }

    // FLEXIBLE factory methods
    internal static ShortcutDefinition Create(
        string name,
        KeyCombination keyCombination,
        Func<ShortcutExecutionContext, Task<ShortcutResult>> handler,
        ShortcutContext context = ShortcutContext.Normal) =>
        new()
        {
            Name = name,
            KeyCombination = keyCombination,
            Handler = handler,
            Context = context
        };

    internal static ShortcutDefinition Navigation(
        string name,
        Key key,
        Func<ShortcutExecutionContext, Task<ShortcutResult>> handler) =>
        new()
        {
            Name = name,
            KeyCombination = KeyCombination.Create(key),
            Handler = handler,
            Context = ShortcutContext.Normal,
            Category = ShortcutCategory.Navigation
        };

    internal static ShortcutDefinition EditMode(
        string name,
        Key key,
        Func<ShortcutExecutionContext, Task<ShortcutResult>> handler) =>
        new()
        {
            Name = name,
            KeyCombination = KeyCombination.Create(key),
            Handler = handler,
            Context = ShortcutContext.EditMode,
            Category = ShortcutCategory.Editing
        };
}

#endregion

#region Command Objects

/// <summary>
/// COMMAND PATTERN: Register shortcut command
/// CONSISTENT: Rovnaká štruktúra ako SortCommand a ImportCommand
/// </summary>
internal sealed record RegisterShortcutCommand
{
    internal required ShortcutDefinition ShortcutDefinition { get; init; }
    internal bool OverrideExisting { get; init; } = false;
    internal bool ValidateConflicts { get; init; } = true;
    internal IProgress<ShortcutProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    internal static RegisterShortcutCommand Create(ShortcutDefinition definition) =>
        new() { ShortcutDefinition = definition };
}

/// <summary>
/// COMMAND PATTERN: Execute shortcut command
/// ENTERPRISE: Comprehensive shortcut execution with context
/// </summary>
internal sealed record ExecuteShortcutCommand
{
    internal required KeyCombination KeyCombination { get; init; }
    internal required ShortcutExecutionContext ExecutionContext { get; init; }
    internal bool StrictContextMatch { get; init; } = true;
    internal TimeSpan? ExecutionTimeout { get; init; }
    internal IProgress<ShortcutProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    internal static ExecuteShortcutCommand Create(
        KeyCombination keyCombination,
        ShortcutExecutionContext context) =>
        new() { KeyCombination = keyCombination, ExecutionContext = context };
}

/// <summary>
/// COMMAND PATTERN: Bulk shortcut registration command
/// PERFORMANCE: Efficient registration of multiple shortcuts
/// </summary>
internal sealed record RegisterShortcutsCommand
{
    internal required IReadOnlyList<ShortcutDefinition> ShortcutDefinitions { get; init; }
    internal bool ClearExisting { get; init; } = false;
    internal bool ValidateConflicts { get; init; } = true;
    internal ShortcutExecutionMode DefaultExecutionMode { get; init; } = ShortcutExecutionMode.Immediate;
    internal IProgress<ShortcutProgress>? ProgressReporter { get; init; }
    internal cancellationToken cancellationToken { get; init; } = default;

    internal static RegisterShortcutsCommand Create(IReadOnlyList<ShortcutDefinition> definitions) =>
        new() { ShortcutDefinitions = definitions };
}

#endregion

#region Result Objects

/// <summary>
/// ENTERPRISE: Shortcut execution result with comprehensive feedback
/// CONSISTENT: Rovnaká štruktúra ako SortResult a ImportResult
/// </summary>
internal sealed record ShortcutResult
{
    internal bool Success { get; init; }
    internal string ExecutedShortcut { get; init; } = string.Empty;
    internal ShortcutContext ExecutionContext { get; init; } = ShortcutContext.None;
    internal TimeSpan ExecutionTime { get; init; }
    internal IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();
    internal object? Result { get; init; }
    internal bool PreventFurtherProcessing { get; init; } = false;

    internal static ShortcutResult CreateSuccess(
        string shortcutName,
        ShortcutContext context,
        TimeSpan executionTime,
        object? result = null) =>
        new()
        {
            Success = true,
            ExecutedShortcut = shortcutName,
            ExecutionContext = context,
            ExecutionTime = executionTime,
            Result = result
        };

    internal static ShortcutResult CreateFailure(
        string shortcutName,
        IReadOnlyList<string> errors,
        TimeSpan executionTime) =>
        new()
        {
            Success = false,
            ExecutedShortcut = shortcutName,
            ErrorMessages = errors,
            ExecutionTime = executionTime
        };

    internal static ShortcutResult Empty => new();
}

/// <summary>
/// ENTERPRISE: Shortcut registration statistics
/// MONITORING: Registration and conflict tracking
/// </summary>
internal sealed record ShortcutRegistrationResult
{
    internal bool Success { get; init; }
    internal int RegisteredCount { get; init; }
    internal int SkippedCount { get; init; }
    internal int ConflictCount { get; init; }
    internal TimeSpan RegistrationTime { get; init; }
    internal IReadOnlyList<string> ConflictMessages { get; init; } = Array.Empty<string>();
    internal IReadOnlyList<ShortcutDefinition> RegisteredShortcuts { get; init; } = Array.Empty<ShortcutDefinition>();
}

#endregion
```

## 🎯 FACADE API METÓDY

### Základné Shortcut API (Consistent s existujúcimi metódami)

```csharp
#region Keyboard Shortcuts Operations with Command Pattern

/// <summary>
/// PUBLIC API: Register single shortcut using command pattern
/// ENTERPRISE: Professional shortcut registration with conflict detection
/// </summary>
Task<ShortcutRegistrationResult> RegisterShortcutAsync(
    RegisterShortcutCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Register multiple shortcuts with bulk processing
/// PERFORMANCE: Efficient bulk registration with validation
/// </summary>
Task<ShortcutRegistrationResult> RegisterShortcutsAsync(
    RegisterShortcutsCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Execute shortcut command
/// ENTERPRISE: Context-aware shortcut execution
/// </summary>
Task<ShortcutResult> ExecuteShortcutAsync(
    ExecuteShortcutCommand command,
    cancellationToken cancellationToken = default);

/// <summary>
/// PUBLIC API: Unregister shortcut by key combination
/// MANAGEMENT: Dynamic shortcut removal
/// </summary>
Task<bool> UnregisterShortcutAsync(KeyCombination keyCombination);

/// <summary>
/// PUBLIC API: Get all registered shortcuts
/// INTROSPECTION: Retrieve all active shortcuts
/// </summary>
IReadOnlyList<ShortcutDefinition> GetRegisteredShortcuts(ShortcutContext? context = null);

/// <summary>
/// PUBLIC API: Check for shortcut conflicts
/// VALIDATION: Conflict detection and resolution
/// </summary>
Task<IReadOnlyList<string>> ValidateShortcutConflictsAsync(
    IReadOnlyList<ShortcutDefinition> shortcuts);

#endregion
```

## 🔧 PREDEFINED SHORTCUTS SPECIFICATIONS

### Edit Mode Shortcuts

```csharp
// ESC - Zruší zmeny v bunke a ukončí edit mód
ShortcutDefinition.EditMode("CancelEdit", Key.Escape,
    async (context) => await CancelCellEditAsync(context));

// Enter - Potvrdí zmeny a zostane na bunke
ShortcutDefinition.EditMode("ConfirmEdit", Key.Enter,
    async (context) => await ConfirmCellEditAsync(context));

// Shift+Enter - Vloží nový riadok do bunky (multiline editing)
ShortcutDefinition.EditMode("InsertNewLine", Key.Enter,
    async (context) => await InsertNewLineAsync(context))
    { KeyCombination = KeyCombination.Shift(Key.Enter) };

// Tab (in edit) - Vloží tab znak do bunky
ShortcutDefinition.EditMode("InsertTab", Key.Tab,
    async (context) => await InsertTabAsync(context));
```

### Navigation Shortcuts

```csharp
// Arrow Keys - Navigácia medzi bunkami s auto-commit zmien
ShortcutDefinition.Navigation("MoveUp", Key.Up,
    async (context) => await NavigateToAdjacentCellAsync(context, Direction.Up));

ShortcutDefinition.Navigation("MoveDown", Key.Down,
    async (context) => await NavigateToAdjacentCellAsync(context, Direction.Down));

ShortcutDefinition.Navigation("MoveLeft", Key.Left,
    async (context) => await NavigateToAdjacentCellAsync(context, Direction.Left));

ShortcutDefinition.Navigation("MoveRight", Key.Right,
    async (context) => await NavigateToAdjacentCellAsync(context, Direction.Right));

// Tab - Ďalšia bunka (doprava → koniec riadku → prvá v novom riadku)
ShortcutDefinition.Navigation("MoveNext", Key.Tab,
    async (context) => await MoveToNextCellAsync(context));

// Shift+Tab - Predchádzajúca bunka (doľava → začiatok riadku → posledná v predošlom)
ShortcutDefinition.Navigation("MovePrevious", Key.Tab,
    async (context) => await MoveToPreviousCellAsync(context))
    { KeyCombination = KeyCombination.Shift(Key.Tab) };
```

### Selection Shortcuts

```csharp
// Ctrl+A - Označí všetky bunky (okrem DeleteRows a CheckBox columns ak sú zapnuté)
ShortcutDefinition.Create("SelectAll", KeyCombination.Ctrl(Key.A),
    async (context) => await SelectAllCellsAsync(context), ShortcutContext.Selection);

// Ctrl+Home - Prvá bunka v tabuľke
ShortcutDefinition.Create("GoToFirst", KeyCombination.Ctrl(Key.Home),
    async (context) => await GoToFirstCellAsync(context));

// Ctrl+End - Posledná bunka s dátami
ShortcutDefinition.Create("GoToLast", KeyCombination.Ctrl(Key.End),
    async (context) => await GoToLastCellAsync(context));

// Ctrl+Click - Toggle selection (pridať/odobrať z výberu)
ShortcutDefinition.Create("ToggleSelection", KeyCombination.Ctrl(Key.LeftClick),
    async (context) => await ToggleCellSelectionAsync(context), ShortcutContext.Selection);
```

### Data Manipulation Shortcuts

```csharp
// Ctrl+C - Copy selected cells/rows
ShortcutDefinition.Create("Copy", KeyCombination.Ctrl(Key.C),
    async (context) => await CopySelectedDataAsync(context), ShortcutContext.Selection);

// Ctrl+V - Paste data to selected location
ShortcutDefinition.Create("Paste", KeyCombination.Ctrl(Key.V),
    async (context) => await PasteDataAsync(context));

// Ctrl+X - Cut selected cells/rows
ShortcutDefinition.Create("Cut", KeyCombination.Ctrl(Key.X),
    async (context) => await CutSelectedDataAsync(context), ShortcutContext.Selection);

// Delete - Delete content of selected cells
ShortcutDefinition.Create("DeleteContent", Key.Delete,
    async (context) => await DeleteSelectedContentAsync(context));

// F2 - Enter edit mode for current cell
ShortcutDefinition.Create("EnterEditMode", Key.F2,
    async (context) => await EnterCellEditModeAsync(context));
```

### Advanced Navigation Shortcuts

```csharp
// Page Up - Move one page up
ShortcutDefinition.Navigation("PageUp", Key.PageUp,
    async (context) => await NavigatePageAsync(context, Direction.Up));

// Page Down - Move one page down
ShortcutDefinition.Navigation("PageDown", Key.PageDown,
    async (context) => await NavigatePageAsync(context, Direction.Down));

// Home - Move to first column of current row
ShortcutDefinition.Navigation("HomeRow", Key.Home,
    async (context) => await MoveToRowStartAsync(context));

// End - Move to last column of current row
ShortcutDefinition.Navigation("EndRow", Key.End,
    async (context) => await MoveToRowEndAsync(context));

// Ctrl+Left - Move to first column
ShortcutDefinition.Navigation("FirstColumn", KeyCombination.Ctrl(Key.Left),
    async (context) => await MoveToFirstColumnAsync(context));

// Ctrl+Right - Move to last column
ShortcutDefinition.Navigation("LastColumn", KeyCombination.Ctrl(Key.Right),
    async (context) => await MoveToLastColumnAsync(context));
```

## ⚡ PERFORMANCE & EVENT OPTIMIZATIONS

### Smart Key Processing s Event Aggregation

```csharp
internal sealed class AdvancedShortcutService
{
    private readonly Dictionary<KeyCombination, ShortcutDefinition> _shortcuts = new();
    private readonly KeyCombinationCache _keyCache = new();
    private const int KeyCacheThreshold = 100;

    public async Task<ShortcutResult> ProcessKeyAsync(KeyEventArgs e, ShortcutExecutionContext context)
    {
        var keyCombination = KeyCombination.Create(e.Key, Keyboard.Modifiers);

        // PERFORMANCE: Fast lookup with caching
        if (_keyCache.TryGetShortcut(keyCombination, context.Context, out var shortcut))
        {
            return await ExecuteShortcutAsync(shortcut, context);
        }

        return ShortcutResult.Empty;
    }

    // PERFORMANCE: Context-aware shortcut resolution
    private ShortcutDefinition? ResolveShortcut(KeyCombination combination, ShortcutContext context)
    {
        // Priority-based resolution: EditMode > Selection > Normal
        var contextPriority = context switch
        {
            ShortcutContext.EditMode => 3,
            ShortcutContext.Selection => 2,
            ShortcutContext.Normal => 1,
            _ => 0
        };

        return _shortcuts.Values
            .Where(s => s.KeyCombination.Equals(combination) &&
                       (s.Context == context || s.Context == ShortcutContext.None))
            .OrderByDescending(s => GetContextPriority(s.Context))
            .ThenByDescending(s => s.Priority)
            .FirstOrDefault();
    }

    // PERFORMANCE: Batch shortcut registration
    public async Task<ShortcutRegistrationResult> RegisterShortcutsAsync(
        IReadOnlyList<ShortcutDefinition> shortcuts)
    {
        var stopwatch = Stopwatch.StartNew();
        var registered = new List<ShortcutDefinition>();
        var conflicts = new List<string>();

        foreach (var shortcut in shortcuts)
        {
            if (ValidateShortcut(shortcut, out var conflict))
            {
                _shortcuts[shortcut.KeyCombination] = shortcut;
                registered.Add(shortcut);
            }
            else
            {
                conflicts.Add(conflict);
            }
        }

        // PERFORMANCE: Rebuild cache after batch registration
        await RebuildKeyCache();

        stopwatch.Stop();

        return new ShortcutRegistrationResult
        {
            Success = conflicts.Count == 0,
            RegisteredCount = registered.Count,
            ConflictCount = conflicts.Count,
            RegistrationTime = stopwatch.Elapsed,
            ConflictMessages = conflicts,
            RegisteredShortcuts = registered
        };
    }

    // PERFORMANCE: Smart cache invalidation
    private async Task RebuildKeyCache()
    {
        if (_shortcuts.Count > KeyCacheThreshold)
        {
            await Task.Run(() => _keyCache.BuildOptimizedLookup(_shortcuts.Values));
        }
        else
        {
            _keyCache.BuildSimpleLookup(_shortcuts.Values);
        }
    }
}

/// <summary>
/// PERFORMANCE: Optimized key combination cache with context awareness
/// ENTERPRISE: Fast lookup for frequent shortcut operations
/// </summary>
internal sealed class KeyCombinationCache
{
    private readonly Dictionary<string, ShortcutDefinition> _contextualCache = new();
    private readonly Dictionary<KeyCombination, List<ShortcutDefinition>> _conflictResolution = new();

    public bool TryGetShortcut(KeyCombination combination, ShortcutContext context,
        out ShortcutDefinition? shortcut)
    {
        var cacheKey = $"{combination.DisplayName}:{context}";

        if (_contextualCache.TryGetValue(cacheKey, out shortcut))
        {
            return shortcut?.IsEnabled == true;
        }

        // Fallback to conflict resolution
        if (_conflictResolution.TryGetValue(combination, out var candidates))
        {
            shortcut = candidates
                .Where(s => s.Context == context || s.Context == ShortcutContext.None)
                .OrderByDescending(s => GetContextPriority(s.Context))
                .FirstOrDefault();

            if (shortcut != null)
            {
                _contextualCache[cacheKey] = shortcut;
                return shortcut.IsEnabled;
            }
        }

        shortcut = null;
        return false;
    }

    public void BuildOptimizedLookup(IEnumerable<ShortcutDefinition> shortcuts)
    {
        _contextualCache.Clear();
        _conflictResolution.Clear();

        // PERFORMANCE: Parallel processing for large shortcut sets
        var groupedShortcuts = shortcuts
            .Where(s => s.IsEnabled)
            .GroupBy(s => s.KeyCombination)
            .AsParallel()
            .ToList();

        foreach (var group in groupedShortcuts)
        {
            var combination = group.Key;
            var definitions = group.OrderByDescending(s => s.Priority).ToList();

            _conflictResolution[combination] = definitions;

            // Pre-populate cache for common contexts
            foreach (var context in Enum.GetValues<ShortcutContext>())
            {
                var contextShortcut = definitions
                    .FirstOrDefault(s => s.Context == context || s.Context == ShortcutContext.None);

                if (contextShortcut != null)
                {
                    var cacheKey = $"{combination.DisplayName}:{context}";
                    _contextualCache[cacheKey] = contextShortcut;
                }
            }
        }
    }

    private static int GetContextPriority(ShortcutContext context) => context switch
    {
        ShortcutContext.EditMode => 4,
        ShortcutContext.Selection => 3,
        ShortcutContext.HeaderMode => 2,
        ShortcutContext.Normal => 1,
        _ => 0
    };
}
```

## 🎯 KĽÚČOVÉ VYLEPŠENIA & ROZŠÍRENIA

### 1. **Context-Aware Execution**
- **EditMode**: Shortcuts specific pre cell editing (ESC, Enter, Shift+Enter)
- **Selection**: Multi-selection operations (Ctrl+A, Ctrl+Click)
- **Normal**: Standard navigation a data operations
- **HeaderMode**: Column-specific operations
- **RowMode**: Row-level operations

### 2. **Advanced Conflict Resolution**
- **Priority-Based**: Higher priority shortcuts override lower ones
- **Context-Specific**: Same key combination can have different behaviors in different contexts
- **Dynamic Registration**: Runtime shortcut registration s conflict validation
- **Fallback Chains**: Context → General → None hierarchy

### 3. **Performance Optimizations**
- **Key Caching**: Optimized lookup tables pre frequent operations
- **Event Aggregation**: Batch processing pre multiple key events
- **Context Switching**: Fast context transitions s minimal overhead
- **Lazy Loading**: On-demand shortcut resolution

### 4. **Enterprise Features**
- **Audit Logging**: Comprehensive shortcut execution tracking
- **Conflict Detection**: Automated conflict identification and resolution
- **Progress Reporting**: Real-time feedback pre bulk operations
- **Error Handling**: Graceful degradation s detailed error messages

### 5. **Extensibility**
- **Custom Handlers**: User-defined shortcut behaviors
- **Plugin Support**: Modular shortcut extensions
- **Configuration**: External shortcut configuration files
- **Theming**: Context-sensitive shortcut behaviors

## **🔍 LOGGING ŠPECIFIKÁCIA PRE SHORTCUT OPERÁCIE**

### **Internal DI Registration & Service Distribution**
Všetky shortcut logging services sú registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a injektované do `KeyboardShortcutsService` cez internal DI systém:

```csharp
// V InternalServiceRegistration.cs
services.AddSingleton<IShortcutLogger<KeyboardShortcutsService>, ShortcutLogger<KeyboardShortcutsService>>();
services.AddSingleton<IOperationLogger<KeyboardShortcutsService>, OperationLogger<KeyboardShortcutsService>>();
services.AddSingleton<ICommandLogger<KeyboardShortcutsService>, CommandLogger<KeyboardShortcutsService>>();

// V KeyboardShortcutsService constructor
public KeyboardShortcutsService(
    ILogger<KeyboardShortcutsService> logger,
    IShortcutLogger<KeyboardShortcutsService> shortcutLogger,
    IOperationLogger<KeyboardShortcutsService> operationLogger,
    ICommandLogger<KeyboardShortcutsService> commandLogger)
```

### **Shortcut Registration and Execution Logging**

```csharp
// Shortcut registration logging
_shortcutLogger.LogShortcutRegistration(shortcut.Name, shortcut.KeyCombination.DisplayName,
    shortcut.Context.ToString(), registrationSuccess, registrationTime);

_logger.LogInformation("Shortcut registered: '{ShortcutName}' ({KeyCombination}) for context {Context}, success={Success}",
    shortcut.Name, shortcut.KeyCombination.DisplayName, shortcut.Context, registrationSuccess);

// Shortcut execution logging
_shortcutLogger.LogShortcutExecution(shortcut.Name, shortcut.KeyCombination.DisplayName,
    executionContext.Context.ToString(), result.Success, result.ExecutionTime);

_logger.LogInformation("Shortcut executed: '{ShortcutName}' ({KeyCombination}) in context {Context}, duration={Duration}ms",
    shortcut.Name, shortcut.KeyCombination.DisplayName, executionContext.Context, result.ExecutionTime.TotalMilliseconds);
```

### **Context Switching and Conflict Resolution Logging**

```csharp
// Context switching logging
_shortcutLogger.LogContextSwitch(oldContext.ToString(), newContext.ToString(),
    activeShortcuts.Count, contextSwitchTime);

_logger.LogInformation("Shortcut context switched: {OldContext} → {NewContext}, activeShortcuts={Count}, switchTime={Time}ms",
    oldContext, newContext, activeShortcuts.Count, contextSwitchTime.TotalMilliseconds);

// Conflict resolution logging
_shortcutLogger.LogShortcutConflict(existingShortcut.Name, newShortcut.Name,
    keyCombination.DisplayName, conflictResolution);

_logger.LogWarning("Shortcut conflict: '{ExistingShortcut}' vs '{NewShortcut}' for {KeyCombination}, resolution={Resolution}",
    existingShortcut.Name, newShortcut.Name, keyCombination.DisplayName, conflictResolution);
```

### **Performance and Cache Logging**

```csharp
// Cache performance logging
_logger.LogInformation("Shortcut cache rebuilt: {ShortcutCount} shortcuts, buildTime={BuildTime}ms, cacheHitRate={HitRate:P2}",
    shortcutCount, buildTime.TotalMilliseconds, cacheHitRate);

// Key processing performance
if (processingTime > PerformanceThresholds.ShortcutWarningThreshold)
{
    _logger.LogWarning("Slow shortcut processing: {KeyCombination} took {ProcessingTime}ms in context {Context}",
        keyCombination.DisplayName, processingTime.TotalMilliseconds, context);
}
```

### **Bulk Operations and Progress Logging**

```csharp
// Bulk registration logging
_logger.LogInformation("Bulk shortcut registration: {TotalCount} shortcuts, registered={RegisteredCount}, conflicts={ConflictCount}",
    shortcuts.Count, result.RegisteredCount, result.ConflictCount);

// Progress reporting for large operations
progressReporter?.Report(new ShortcutProgress
{
    ProcessedShortcuts = processedCount,
    TotalShortcuts = totalCount,
    CurrentShortcut = currentShortcut.Name,
    CurrentContext = currentShortcut.Context,
    CurrentOperation = "Registering shortcuts..."
});
```

### **Logging Levels Usage:**
- **Information**: Successful registrations, executions, context switches, performance metrics
- **Warning**: Conflicts, performance issues, context mismatches, disabled shortcuts
- **Error**: Registration failures, execution errors, invalid key combinations
- **Critical**: System-level shortcut failures, context corruption, cache rebuild failures

Táto špecifikácia poskytuje kompletný, enterprise-ready keyboard shortcuts systém s pokročilou event-driven architektúrou, performance optimalizáciami a jednotnou architektúrou s ostatnými časťami komponentu.
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
/// HYBRID DI: Poskytuje services for custom shortcut functions
/// </summary>
internal sealed record ShortcutExecutionContext
{
    internal IServiceProvider? ServiceProvider { get; init; }
    internal IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
    internal CancellationToken CancellationToken { get; init; } = default;
    internal DateTime ExecutionTime { get; init; } = DateTime.UtcNow;
    internal ShortcutContext Context { get; init; } = ShortcutContext.Normal;
    internal object? SourceElement { get; init; }
    internal (double X, double Y)? CursorPosition { get; init; }
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

    // Factory methods for common combinations
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

    public override int GetHashCode()
    {
        return HashCode.Combine(PrimaryKey, Modifiers);
    }

    public bool Equals(KeyCombination? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return PrimaryKey == other.PrimaryKey && Modifiers == other.Modifiers;
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
    internal CancellationToken CancellationToken { get; init; } = default;

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
    internal CancellationToken CancellationToken { get; init; } = default;

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
    internal CancellationToken CancellationToken { get; init; } = default;

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

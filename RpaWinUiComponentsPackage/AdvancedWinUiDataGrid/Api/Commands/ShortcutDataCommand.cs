using System;
using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public command for executing shortcuts
/// </summary>
public sealed record ExecuteShortcutDataCommand
{
    /// <summary>
    /// Name of shortcut to execute
    /// </summary>
    public required string ShortcutName { get; init; }

    /// <summary>
    /// Optional parameters for shortcut execution
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }
}

/// <summary>
/// Public result from shortcut execution
/// </summary>
public sealed record ShortcutDataResult
{
    /// <summary>
    /// Indicates if shortcut execution was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Name of executed shortcut
    /// </summary>
    public string ShortcutName { get; init; } = string.Empty;

    /// <summary>
    /// Execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Error messages if any
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Result data from execution
    /// </summary>
    public object? ResultData { get; init; }
}

/// <summary>
/// Public shortcut definition
/// </summary>
public sealed record PublicShortcutDefinition
{
    /// <summary>
    /// Shortcut name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Shortcut description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Key combination display name (e.g., "Ctrl+C")
    /// </summary>
    public string KeyCombination { get; init; } = string.Empty;

    /// <summary>
    /// Whether shortcut is enabled
    /// </summary>
    public bool IsEnabled { get; init; } = true;
}

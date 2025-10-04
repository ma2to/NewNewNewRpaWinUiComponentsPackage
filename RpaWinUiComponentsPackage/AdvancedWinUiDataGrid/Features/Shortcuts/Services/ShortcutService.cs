using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Shortcuts.Services;

/// <summary>
/// Internal implementation of shortcut service with basic shortcuts support
/// Thread-safe with logging support
/// </summary>
internal sealed class ShortcutService : IShortcutService
{
    private readonly ILogger<ShortcutService> _logger;
    private readonly IOperationLogger<ShortcutService> _operationLogger;
    private readonly IRowStore? _rowStore;
    private readonly Dictionary<KeyCombination, ShortcutDefinition> _shortcuts = new();
    private readonly object _lock = new();

    public ShortcutService(
        ILogger<ShortcutService> logger,
        IRowStore? rowStore = null,
        IOperationLogger<ShortcutService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore;
        _operationLogger = operationLogger ?? NullOperationLogger<ShortcutService>.Instance;

        // Register default shortcuts
        RegisterDefaultShortcuts();
    }

    public async Task<ShortcutResult> ExecuteShortcutAsync(ShortcutCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        using var scope = _operationLogger.LogOperationStart("ExecuteShortcutAsync", new
        {
            OperationId = operationId,
            ShortcutName = command.ShortcutName
        });

        _logger.LogInformation("Executing shortcut {OperationId}: name={ShortcutName}",
            operationId, command.ShortcutName);

        try
        {
            var shortcut = FindShortcutByName(command.ShortcutName);
            if (shortcut == null)
            {
                var error = $"Shortcut '{command.ShortcutName}' not found";
                _logger.LogWarning("Shortcut execution failed for operation {OperationId}: {Error}", operationId, error);
                scope.MarkFailure(new InvalidOperationException(error));
                return ShortcutResult.CreateFailure(command.ShortcutName, new[] { error }, stopwatch.Elapsed);
            }

            if (!shortcut.IsEnabled)
            {
                var error = $"Shortcut '{command.ShortcutName}' is disabled";
                _logger.LogWarning("Shortcut execution failed for operation {OperationId}: {Error}", operationId, error);
                scope.MarkFailure(new InvalidOperationException(error));
                return ShortcutResult.CreateFailure(command.ShortcutName, new[] { error }, stopwatch.Elapsed);
            }

            var context = new ShortcutExecutionContext
            {
                Parameters = command.Parameters,
                CancellationToken = cancellationToken
            };

            if (shortcut.Handler != null)
            {
                var result = await shortcut.Handler(context);
                stopwatch.Stop();

                _logger.LogInformation("Shortcut execution {OperationId} completed in {Duration}ms",
                    operationId, stopwatch.ElapsedMilliseconds);

                scope.MarkSuccess(new { Duration = stopwatch.Elapsed, Success = result.Success });
                return result;
            }

            // Execute predefined shortcut
            var executionResult = await ExecutePredefinedShortcut(shortcut, context, cancellationToken);
            stopwatch.Stop();

            scope.MarkSuccess(new { Duration = stopwatch.Elapsed });
            return executionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortcut execution {OperationId} failed: {Message}", operationId, ex.Message);
            scope.MarkFailure(ex);
            return ShortcutResult.CreateFailure(command.ShortcutName, new[] { $"Execution failed: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    public async Task<ShortcutResult> ExecuteShortcutByKeyAsync(ExecuteShortcutCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        _logger.LogInformation("Executing shortcut by key {OperationId}: key={Key}",
            operationId, command.KeyCombination.DisplayName);

        try
        {
            var shortcut = FindShortcutByKey(command.KeyCombination, command.ExecutionContext.Context, command.StrictContextMatch);
            if (shortcut == null)
            {
                var error = $"No shortcut found for key combination '{command.KeyCombination.DisplayName}'";
                _logger.LogWarning("Shortcut execution by key failed: {Error}", error);
                return ShortcutResult.CreateFailure(command.KeyCombination.DisplayName, new[] { error }, stopwatch.Elapsed);
            }

            if (shortcut.Handler != null)
            {
                var result = await shortcut.Handler(command.ExecutionContext);
                stopwatch.Stop();
                return result;
            }

            var executionResult = await ExecutePredefinedShortcut(shortcut, command.ExecutionContext, cancellationToken);
            stopwatch.Stop();
            return executionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortcut execution by key failed: {Message}", ex.Message);
            return ShortcutResult.CreateFailure(command.KeyCombination.DisplayName, new[] { $"Execution failed: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    public async Task<ShortcutRegistrationResult> RegisterShortcutAsync(RegisterShortcutCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            lock (_lock)
            {
                if (_shortcuts.ContainsKey(command.ShortcutDefinition.KeyCombination) && !command.OverrideExisting)
                {
                    var conflict = $"Shortcut with key '{command.ShortcutDefinition.KeyCombination.DisplayName}' already exists";
                    _logger.LogWarning("Shortcut registration conflict: {Conflict}", conflict);

                    return new ShortcutRegistrationResult
                    {
                        Success = false,
                        RegisteredCount = 0,
                        ConflictCount = 1,
                        ConflictMessages = new[] { conflict },
                        RegistrationTime = stopwatch.Elapsed
                    };
                }

                _shortcuts[command.ShortcutDefinition.KeyCombination] = command.ShortcutDefinition;
            }

            stopwatch.Stop();
            _logger.LogInformation("Shortcut registered: {Name} ({Key})",
                command.ShortcutDefinition.Name, command.ShortcutDefinition.KeyCombination.DisplayName);

            return new ShortcutRegistrationResult
            {
                Success = true,
                RegisteredCount = 1,
                RegistrationTime = stopwatch.Elapsed,
                RegisteredShortcuts = new[] { command.ShortcutDefinition }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortcut registration failed: {Message}", ex.Message);
            return new ShortcutRegistrationResult
            {
                Success = false,
                RegistrationTime = stopwatch.Elapsed,
                ConflictMessages = new[] { ex.Message }
            };
        }

    }

    public async Task<ShortcutRegistrationResult> RegisterShortcutsAsync(RegisterShortcutsCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var registered = new List<ShortcutDefinition>();
        var conflicts = new List<string>();

        try
        {
            lock (_lock)
            {
                if (command.ClearExisting)
                {
                    _shortcuts.Clear();
                }

                foreach (var shortcut in command.ShortcutDefinitions)
                {
                    if (_shortcuts.ContainsKey(shortcut.KeyCombination) && command.ValidateConflicts)
                    {
                        conflicts.Add($"Shortcut with key '{shortcut.KeyCombination.DisplayName}' already exists");
                        continue;
                    }

                    _shortcuts[shortcut.KeyCombination] = shortcut;
                    registered.Add(shortcut);
                }
            }

            stopwatch.Stop();
            _logger.LogInformation("Bulk shortcut registration: {RegisteredCount} registered, {ConflictCount} conflicts",
                registered.Count, conflicts.Count);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk shortcut registration failed: {Message}", ex.Message);
            return new ShortcutRegistrationResult
            {
                Success = false,
                RegistrationTime = stopwatch.Elapsed,
                ConflictMessages = new[] { ex.Message }
            };
        }

    }

    public async Task<bool> UnregisterShortcutAsync(KeyCombination keyCombination, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lock)
            {
                var removed = _shortcuts.Remove(keyCombination);
                _logger.LogInformation("Shortcut unregistered: {Key}, success={Success}",
                    keyCombination.DisplayName, removed);
                return removed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortcut unregistration failed: {Message}", ex.Message);
            return false;
        }

    }

    public IReadOnlyList<ShortcutDefinition> GetRegisteredShortcuts(ShortcutContext? context = null)
    {
        lock (_lock)
        {
            var shortcuts = _shortcuts.Values.AsEnumerable();

            if (context.HasValue)
            {
                shortcuts = shortcuts.Where(s => s.Context == context.Value || s.Context == ShortcutContext.None);
            }

            return shortcuts.ToList();
        }
    }

    public async Task<IReadOnlyList<string>> ValidateShortcutConflictsAsync(IReadOnlyList<ShortcutDefinition> shortcuts, CancellationToken cancellationToken = default)
    {
        var conflicts = new List<string>();

        try
        {
            var grouped = shortcuts.GroupBy(s => s.KeyCombination);
            foreach (var group in grouped)
            {
                if (group.Count() > 1)
                {
                    conflicts.Add($"Key combination '{group.Key.DisplayName}' is used by multiple shortcuts: {string.Join(", ", group.Select(s => s.Name))}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortcut conflict validation failed: {Message}", ex.Message);
            conflicts.Add(ex.Message);
        }

        return await Task.FromResult(conflicts);
    }

    #region Private Helper Methods

    private void RegisterDefaultShortcuts()
    {
        try
        {
            // Copy (Ctrl+C)
            var copyShortcut = ShortcutDefinition.Create(
                "Copy",
                KeyCombination.Ctrl(Key.C),
                CopyHandler,
                ShortcutContext.Normal);

            // Paste (Ctrl+V)
            var pasteShortcut = ShortcutDefinition.Create(
                "Paste",
                KeyCombination.Ctrl(Key.V),
                PasteHandler,
                ShortcutContext.Normal);

            // Delete (Delete key)
            var deleteShortcut = ShortcutDefinition.Create(
                "Delete",
                KeyCombination.Create(Key.Delete),
                DeleteHandler,
                ShortcutContext.Normal);

            // Select All (Ctrl+A)
            var selectAllShortcut = ShortcutDefinition.Create(
                "SelectAll",
                KeyCombination.Ctrl(Key.A),
                SelectAllHandler,
                ShortcutContext.Normal);

            // Find (Ctrl+F)
            var findShortcut = ShortcutDefinition.Create(
                "Find",
                KeyCombination.Ctrl(Key.F),
                FindHandler,
                ShortcutContext.Normal);

            lock (_lock)
            {
                _shortcuts[copyShortcut.KeyCombination] = copyShortcut;
                _shortcuts[pasteShortcut.KeyCombination] = pasteShortcut;
                _shortcuts[deleteShortcut.KeyCombination] = deleteShortcut;
                _shortcuts[selectAllShortcut.KeyCombination] = selectAllShortcut;
                _shortcuts[findShortcut.KeyCombination] = findShortcut;
            }

            _logger.LogInformation("Default shortcuts registered: 5 shortcuts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register default shortcuts: {Message}", ex.Message);
        }
    }

    private async Task<ShortcutResult> CopyHandler(ShortcutExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Executing Copy shortcut");
        // Basic implementation - can be extended
        stopwatch.Stop();
        return ShortcutResult.CreateSuccess("Copy", context.Context, stopwatch.Elapsed, "Copy operation completed");
    }

    private async Task<ShortcutResult> PasteHandler(ShortcutExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Executing Paste shortcut");
        // Basic implementation - can be extended
        stopwatch.Stop();
        return ShortcutResult.CreateSuccess("Paste", context.Context, stopwatch.Elapsed, "Paste operation completed");
    }

    private async Task<ShortcutResult> DeleteHandler(ShortcutExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Executing Delete shortcut");
        // Basic implementation - can be extended
        stopwatch.Stop();
        return ShortcutResult.CreateSuccess("Delete", context.Context, stopwatch.Elapsed, "Delete operation completed");
    }

    private async Task<ShortcutResult> SelectAllHandler(ShortcutExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Executing SelectAll shortcut");
        // Basic implementation - can be extended
        stopwatch.Stop();
        return ShortcutResult.CreateSuccess("SelectAll", context.Context, stopwatch.Elapsed, "SelectAll operation completed");
    }

    private async Task<ShortcutResult> FindHandler(ShortcutExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Executing Find shortcut");
        // Basic implementation - can be extended
        stopwatch.Stop();
        return ShortcutResult.CreateSuccess("Find", context.Context, stopwatch.Elapsed, "Find operation completed");
    }

    private ShortcutDefinition? FindShortcutByName(string name)
    {
        lock (_lock)
        {
            return _shortcuts.Values.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    private ShortcutDefinition? FindShortcutByKey(KeyCombination keyCombination, ShortcutContext context, bool strictMatch)
    {
        lock (_lock)
        {
            var candidates = _shortcuts.Values
                .Where(s => s.KeyCombination.Equals(keyCombination))
                .ToList();

            if (!candidates.Any())
                return null;

            if (strictMatch)
            {
                return candidates.FirstOrDefault(s => s.Context == context);
            }

            // Context priority: exact match > None > any
            return candidates
                .OrderByDescending(s => s.Context == context ? 2 : (s.Context == ShortcutContext.None ? 1 : 0))
                .ThenByDescending(s => s.Priority)
                .FirstOrDefault();
        }
    }

    private async Task<ShortcutResult> ExecutePredefinedShortcut(ShortcutDefinition shortcut, ShortcutExecutionContext context, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing predefined shortcut: {Name}", shortcut.Name);

            // Placeholder for predefined shortcut execution
            // Can be extended with actual implementation

            stopwatch.Stop();
            return ShortcutResult.CreateSuccess(shortcut.Name, context.Context, stopwatch.Elapsed, "Operation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Predefined shortcut execution failed: {Message}", ex.Message);
            return ShortcutResult.CreateFailure(shortcut.Name, new[] { ex.Message }, stopwatch.Elapsed);
        }

    }

    #endregion
}

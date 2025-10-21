using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Optimization;

/// <summary>
/// Weak event manager to prevent memory leaks from event subscriptions.
/// Uses WeakReference to allow subscribers to be garbage collected even if they forget to unsubscribe.
/// Thread-safe implementation.
/// </summary>
/// <typeparam name="TSource">Event source type</typeparam>
/// <typeparam name="TEventArgs">Event arguments type</typeparam>
public static class WeakEventManager<TSource, TEventArgs>
    where TSource : class
    where TEventArgs : EventArgs
{
    private static readonly Dictionary<TSource, List<WeakReference<EventHandler<TEventArgs>>>> _handlers = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Adds event handler using weak reference.
    /// Handler can be garbage collected even without explicit removal.
    /// </summary>
    /// <param name="source">Event source</param>
    /// <param name="eventName">Event name (for debugging/logging)</param>
    /// <param name="handler">Event handler to add</param>
    public static void AddHandler(
        TSource source,
        string eventName,
        EventHandler<TEventArgs> handler)
    {
        if (source == null || handler == null)
            return;

        lock (_lock)
        {
            if (!_handlers.TryGetValue(source, out var list))
            {
                list = new List<WeakReference<EventHandler<TEventArgs>>>();
                _handlers[source] = list;
            }

            // Check if handler already exists (prevent duplicates)
            var existingHandler = list.FirstOrDefault(wr =>
            {
                if (wr.TryGetTarget(out var h))
                    return h == handler;
                return false;
            });

            if (existingHandler == null)
            {
                list.Add(new WeakReference<EventHandler<TEventArgs>>(handler));
            }
        }
    }

    /// <summary>
    /// Removes event handler.
    /// </summary>
    /// <param name="source">Event source</param>
    /// <param name="eventName">Event name (for debugging/logging)</param>
    /// <param name="handler">Event handler to remove</param>
    public static void RemoveHandler(
        TSource source,
        string eventName,
        EventHandler<TEventArgs> handler)
    {
        if (source == null || handler == null)
            return;

        lock (_lock)
        {
            if (_handlers.TryGetValue(source, out var list))
            {
                list.RemoveAll(wr =>
                {
                    if (!wr.TryGetTarget(out var h))
                        return true; // Dead reference - remove

                    return h == handler;
                });

                // Cleanup: remove source entry if no handlers left
                if (list.Count == 0)
                    _handlers.Remove(source);
            }
        }
    }

    /// <summary>
    /// Raises event to all registered handlers.
    /// Automatically removes dead weak references during invocation.
    /// </summary>
    /// <param name="source">Event source</param>
    /// <param name="args">Event arguments</param>
    public static void RaiseEvent(TSource source, TEventArgs args)
    {
        if (source == null)
            return;

        List<WeakReference<EventHandler<TEventArgs>>>? handlersCopy;

        lock (_lock)
        {
            if (!_handlers.TryGetValue(source, out var list))
                return;

            // Remove dead references before invoking
            list.RemoveAll(wr => !wr.TryGetTarget(out _));

            if (list.Count == 0)
            {
                _handlers.Remove(source);
                return;
            }

            // Create copy to avoid lock during invocation
            handlersCopy = new List<WeakReference<EventHandler<TEventArgs>>>(list);
        }

        // Invoke handlers outside lock (prevent deadlocks)
        foreach (var wr in handlersCopy)
        {
            if (wr.TryGetTarget(out var handler))
            {
                try
                {
                    handler(source, args);
                }
                catch
                {
                    // Ignore handler exceptions (prevent one bad handler from breaking all)
                }
            }
        }
    }

    /// <summary>
    /// Gets number of active handlers for a source.
    /// Useful for debugging/diagnostics.
    /// </summary>
    public static int GetHandlerCount(TSource source)
    {
        if (source == null)
            return 0;

        lock (_lock)
        {
            if (_handlers.TryGetValue(source, out var list))
            {
                // Count only alive handlers
                return list.Count(wr => wr.TryGetTarget(out _));
            }
        }

        return 0;
    }

    /// <summary>
    /// Clears all handlers for all sources.
    /// Use with caution - only for testing or cleanup.
    /// </summary>
    public static void ClearAll()
    {
        lock (_lock)
        {
            _handlers.Clear();
        }
    }
}

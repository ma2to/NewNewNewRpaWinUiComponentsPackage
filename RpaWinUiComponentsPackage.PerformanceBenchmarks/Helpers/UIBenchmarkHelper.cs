using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;
using System.Runtime.InteropServices;

namespace RpaWinUiComponentsPackage.PerformanceBenchmarks.Helpers;

/// <summary>
/// Helper class for creating facades with UI dispatcher support
/// Enables benchmarking UI-dependent operations
/// </summary>
internal static class UIBenchmarkHelper
{
    private static DispatcherQueueController? _dispatcherController;
    private static DispatcherQueue? _dispatcher;
    private static readonly object _lock = new();

    /// <summary>
    /// Creates a facade WITH UI dispatcher support
    /// Automatically initializes dispatcher if not available
    /// </summary>
    /// <param name="batchSize">Batch size for operations</param>
    /// <param name="features">Grid features to enable</param>
    /// <returns>Configured facade with UI support</returns>
    public static IAdvancedDataGridFacade CreateWithUI(int batchSize, params GridFeature[] features)
    {
        // Ensure we have a UI dispatcher
        var dispatcher = EnsureDispatcher();

        var loggerFactory = NullLoggerFactory.Instance;

        var options = new AdvancedDataGridOptions
        {
            BatchSize = batchSize,
            EnableParallelProcessing = true,
            EnableRealTimeValidation = false,
            LoggerFactory = loggerFactory,
            DispatcherQueue = dispatcher,
            OperationMode = PublicDataGridOperationMode.Interactive
        };

        // Enable specific features
        options.EnabledFeatures.Clear();
        foreach (var feature in features)
        {
            options.EnabledFeatures.Add(feature);
        }

        return AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory, dispatcher);
    }

    /// <summary>
    /// Creates a facade in readonly mode (Headless+UI)
    /// Has UI dispatcher present but no automatic UI refresh
    /// Provides middle ground between Headless and Interactive modes
    /// </summary>
    /// <param name="batchSize">Batch size for operations</param>
    /// <param name="features">Grid features to enable</param>
    /// <returns>Configured facade in readonly mode</returns>
    public static IAdvancedDataGridFacade CreateReadonly(int batchSize, params GridFeature[] features)
    {
        // UI dispatcher is present
        var dispatcher = EnsureDispatcher();
        var loggerFactory = NullLoggerFactory.Instance;

        var options = new AdvancedDataGridOptions
        {
            BatchSize = batchSize,
            EnableParallelProcessing = true,
            EnableRealTimeValidation = false,
            LoggerFactory = loggerFactory,
            DispatcherQueue = dispatcher,
            OperationMode = PublicDataGridOperationMode.Readonly // Key difference!
        };

        // Enable specific features
        options.EnabledFeatures.Clear();
        foreach (var feature in features)
        {
            options.EnabledFeatures.Add(feature);
        }

        return AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory, dispatcher);
    }

    /// <summary>
    /// Creates a facade without UI dispatcher (headless mode)
    /// </summary>
    /// <param name="batchSize">Batch size for operations</param>
    /// <param name="features">Grid features to enable</param>
    /// <returns>Configured facade in headless mode</returns>
    public static IAdvancedDataGridFacade CreateHeadless(int batchSize, params GridFeature[] features)
    {
        var loggerFactory = NullLoggerFactory.Instance;

        var options = new AdvancedDataGridOptions
        {
            BatchSize = batchSize,
            EnableParallelProcessing = true,
            EnableRealTimeValidation = false,
            LoggerFactory = loggerFactory,
            OperationMode = PublicDataGridOperationMode.Headless
        };

        // Enable specific features
        options.EnabledFeatures.Clear();
        foreach (var feature in features)
        {
            options.EnabledFeatures.Add(feature);
        }

        return AdvancedDataGridFacadeFactory.CreateStandalone(options, loggerFactory, null);
    }

    /// <summary>
    /// Ensures UI dispatcher is available, creates if necessary
    /// Thread-safe initialization
    /// </summary>
    /// <returns>DispatcherQueue instance</returns>
    public static DispatcherQueue EnsureDispatcher()
    {
        if (_dispatcher != null)
            return _dispatcher;

        lock (_lock)
        {
            if (_dispatcher != null)
                return _dispatcher;

            try
            {
                // Try to get dispatcher for current thread
                _dispatcher = DispatcherQueue.GetForCurrentThread();

                if (_dispatcher == null)
                {
                    // Create new dispatcher queue controller
                    _dispatcherController = DispatcherQueueController.CreateOnDedicatedThread();
                    _dispatcher = _dispatcherController.DispatcherQueue;
                }

                return _dispatcher;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to create UI dispatcher: {ex.Message}");
                Console.WriteLine("[WARNING] UI benchmarks will be skipped");
                throw new InvalidOperationException("UI dispatcher not available. Run benchmarks on a UI thread or skip UI tests.", ex);
            }
        }
    }

    /// <summary>
    /// Checks if UI dispatcher is available
    /// </summary>
    /// <returns>True if dispatcher is available, false otherwise</returns>
    public static bool IsUIAvailable()
    {
        try
        {
            var dispatcher = DispatcherQueue.GetForCurrentThread();
            return dispatcher != null || _dispatcher != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Executes an action on UI thread if available, otherwise throws
    /// </summary>
    /// <param name="action">Action to execute</param>
    public static void RunOnUIThread(Action action)
    {
        var dispatcher = EnsureDispatcher();

        bool completed = false;
        Exception? exception = null;

        dispatcher.TryEnqueue(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                completed = true;
            }
        });

        // Wait for completion (with timeout)
        var timeout = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (!completed && DateTime.UtcNow - startTime < timeout)
        {
            Thread.Sleep(10);
        }

        if (!completed)
        {
            throw new TimeoutException("UI operation timed out");
        }

        if (exception != null)
        {
            throw exception;
        }
    }

    /// <summary>
    /// Executes an async function on UI thread if available
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="func">Function to execute</param>
    /// <returns>Result of the function</returns>
    public static async Task<T> RunOnUIThreadAsync<T>(Func<Task<T>> func)
    {
        var dispatcher = EnsureDispatcher();

        var tcs = new TaskCompletionSource<T>();

        dispatcher.TryEnqueue(async () =>
        {
            try
            {
                var result = await func();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }

    /// <summary>
    /// Cleans up dispatcher resources
    /// Call this when benchmarks are complete
    /// </summary>
    public static void Cleanup()
    {
        lock (_lock)
        {
            _dispatcherController?.ShutdownQueueAsync();
            _dispatcherController = null;
            _dispatcher = null;
        }
    }

    /// <summary>
    /// Creates a column definition for benchmarks
    /// </summary>
    public static PublicColumnDefinition CreateColumn(string name, Type dataType)
    {
        return new PublicColumnDefinition
        {
            Name = name,
            Header = name,
            DataType = dataType,
            Width = 100,
            MinWidth = 50,
            IsVisible = true,
            IsSortable = true,
            IsFilterable = true
        };
    }
}

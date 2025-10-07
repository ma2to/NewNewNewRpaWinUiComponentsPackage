using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

/// <summary>
/// Internal service that pushes UI updates via DispatcherQueue
/// Handles thread-safe UI notifications and updates from background operations
/// </summary>
internal sealed class UiNotificationService
{
    private readonly ILogger<UiNotificationService> _logger;
    private readonly DispatcherQueue? _dispatcherQueue;
    private volatile bool _isDisposed;

    public UiNotificationService(
        ILogger<UiNotificationService> logger,
        DispatcherQueue? dispatcherQueue = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcherQueue = dispatcherQueue;

        _logger.LogDebug("UiNotificationService initialized with dispatcher: {HasDispatcher}", _dispatcherQueue != null);
    }

    /// <summary>
    /// Execute UI update on the UI thread
    /// </summary>
    /// <param name="uiUpdate">UI update action to execute</param>
    /// <param name="priority">Priority of the UI update</param>
    /// <returns>True if update was scheduled successfully</returns>
    public bool ExecuteOnUIThread(Action uiUpdate, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot execute UI update - service is disposed");
            return false;
        }

        if (uiUpdate == null)
            throw new ArgumentNullException(nameof(uiUpdate));

        try
        {
            if (_dispatcherQueue == null)
            {
                _logger.LogTrace("No dispatcher available - executing update synchronously");
                uiUpdate();
                return true;
            }

            var success = _dispatcherQueue.TryEnqueue(priority, () =>
            {
                try
                {
                    _logger.LogTrace("Executing UI update on dispatcher thread");
                    uiUpdate();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute UI update on dispatcher thread");
                }
            });

            if (!success)
            {
                _logger.LogWarning("Failed to enqueue UI update to dispatcher");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule UI update");
            return false;
        }
    }

    /// <summary>
    /// Execute async UI update on the UI thread
    /// </summary>
    /// <param name="uiUpdateAsync">Async UI update to execute</param>
    /// <param name="priority">Priority of the UI update</param>
    /// <returns>Task representing the async operation</returns>
    public Task<bool> ExecuteOnUIThreadAsync(Func<Task> uiUpdateAsync, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot execute async UI update - service is disposed");
            return Task.FromResult(false);
        }

        if (uiUpdateAsync == null)
            throw new ArgumentNullException(nameof(uiUpdateAsync));

        var tcs = new TaskCompletionSource<bool>();

        try
        {
            if (_dispatcherQueue == null)
            {
                _logger.LogTrace("No dispatcher available - executing async update synchronously");
                Task.Run(async () =>
                {
                    try
                    {
                        await uiUpdateAsync();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to execute async UI update synchronously");
                        tcs.SetResult(false);
                    }
                });
                return tcs.Task;
            }

            var success = _dispatcherQueue.TryEnqueue(priority, async () =>
            {
                try
                {
                    _logger.LogTrace("Executing async UI update on dispatcher thread");
                    await uiUpdateAsync();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute async UI update on dispatcher thread");
                    tcs.SetResult(false);
                }
            });

            if (!success)
            {
                _logger.LogWarning("Failed to enqueue async UI update to dispatcher");
                tcs.SetResult(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule async UI update");
            tcs.SetResult(false);
        }

        return tcs.Task;
    }

    /// <summary>
    /// Notify UI about validation results refresh
    /// </summary>
    /// <param name="validationCount">Number of validation errors</param>
    /// <param name="hasErrors">Whether there are validation errors</param>
    public void NotifyValidationResultsRefresh(int validationCount, bool hasErrors)
    {
        ExecuteOnUIThread(() =>
        {
            _logger.LogInformation("UI notification: Validation results refreshed - Count: {ValidationCount}, HasErrors: {HasErrors}",
                validationCount, hasErrors);

            // This would typically trigger UI binding updates
            // In a real implementation, this might raise events or update observable collections
            OnValidationResultsRefreshed?.Invoke(validationCount, hasErrors);
        });
    }

    /// <summary>
    /// Notify UI about data refresh
    /// </summary>
    /// <param name="rowCount">Number of rows</param>
    /// <param name="columnCount">Number of columns</param>
    public void NotifyDataRefresh(int rowCount, int columnCount)
    {
        ExecuteOnUIThread(() =>
        {
            _logger.LogInformation("UI notification: Data refreshed - Rows: {RowCount}, Columns: {ColumnCount}",
                rowCount, columnCount);

            OnDataRefreshed?.Invoke(rowCount, columnCount);
        });
    }

    /// <summary>
    /// Notify UI about data refresh (async version with operation type)
    /// Used for unified UI/Headless architecture
    /// </summary>
    /// <param name="affectedRows">Number of affected rows</param>
    /// <param name="operationType">Type of operation that triggered refresh</param>
    public Task NotifyDataRefreshAsync(int affectedRows, string operationType)
    {
        return ExecuteOnUIThreadAsync(async () =>
        {
            _logger.LogInformation("UI notification: Data refreshed - AffectedRows: {AffectedRows}, Operation: {OperationType}",
                affectedRows, operationType);

            // Trigger data refreshed event
            OnDataRefreshed?.Invoke(affectedRows, 0);

            await Task.CompletedTask;
        });
    }

    /// <summary>
    /// Notify UI about operation progress
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="progressPercentage">Progress percentage (0-100)</param>
    /// <param name="message">Optional progress message</param>
    public void NotifyOperationProgress(string operationName, double progressPercentage, string? message = null)
    {
        ExecuteOnUIThread(() =>
        {
            _logger.LogDebug("UI notification: Operation progress - {OperationName}: {ProgressPercentage}% {Message}",
                operationName, progressPercentage, message ?? "");

            OnOperationProgress?.Invoke(operationName, progressPercentage, message);
        }, DispatcherQueuePriority.Low); // Use low priority for progress updates
    }

    /// <summary>
    /// Event raised when validation results are refreshed
    /// </summary>
    public event Action<int, bool>? OnValidationResultsRefreshed;

    /// <summary>
    /// Event raised when data is refreshed
    /// </summary>
    public event Action<int, int>? OnDataRefreshed;

    /// <summary>
    /// Event raised when operation progress is updated
    /// </summary>
    public event Action<string, double, string?>? OnOperationProgress;

    /// <summary>
    /// Event raised when data changes
    /// </summary>
    public event EventHandler<DataChangedEventArgs>? DataChanged;

    /// <summary>
    /// Event raised when validation state changes
    /// </summary>
    public event EventHandler<ValidationChangedEventArgs>? ValidationChanged;

    /// <summary>
    /// Event raised when a cell is edited
    /// </summary>
    public event EventHandler<CellEditedEventArgs>? CellEdited;

    /// <summary>
    /// Event raised when selection changes
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Event raised for custom notifications
    /// </summary>
    public event EventHandler<CustomNotificationEventArgs>? CustomNotification;

    /// <summary>
    /// Raise custom notification event
    /// </summary>
    public void RaiseCustomNotification(string message, object? data = null)
    {
        ExecuteOnUIThread(() =>
        {
            _logger.LogInformation("Custom notification: {Message}", message);
            CustomNotification?.Invoke(this, new CustomNotificationEventArgs { Message = message, Data = data });
        });
    }

    /// <summary>
    /// Dispose the service and clean up resources
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Clear event handlers
        OnValidationResultsRefreshed = null;
        OnDataRefreshed = null;
        OnOperationProgress = null;
        DataChanged = null;
        ValidationChanged = null;
        CellEdited = null;
        SelectionChanged = null;
        CustomNotification = null;

        _logger.LogDebug("UiNotificationService disposed");
    }
}

/// <summary>
/// Event args for data changed events
/// </summary>
internal class DataChangedEventArgs : EventArgs
{
    public string OperationType { get; init; } = string.Empty;
    public int AffectedRowCount { get; init; }
}

/// <summary>
/// Event args for validation changed events
/// </summary>
internal class ValidationChangedEventArgs : EventArgs
{
    public int ErrorCount { get; init; }
    public bool HasErrors { get; init; }
}

/// <summary>
/// Event args for cell edited events
/// </summary>
internal class CellEditedEventArgs : EventArgs
{
    public int RowIndex { get; init; }
    public string ColumnName { get; init; } = string.Empty;
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}

/// <summary>
/// Event args for selection changed events
/// </summary>
internal class SelectionChangedEventArgs : EventArgs
{
    public IReadOnlyList<int> SelectedRowIndices { get; init; } = Array.Empty<int>();
}

/// <summary>
/// Event args for custom notifications
/// </summary>
internal class CustomNotificationEventArgs : EventArgs
{
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }
}
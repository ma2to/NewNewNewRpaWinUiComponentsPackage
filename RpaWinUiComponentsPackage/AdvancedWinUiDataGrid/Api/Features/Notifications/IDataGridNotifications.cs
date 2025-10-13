
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Notifications;

/// <summary>
/// Public interface for DataGrid notifications and events.
/// Provides event subscription and notification management.
/// </summary>
public interface IDataGridNotifications
{
    /// <summary>
    /// Event raised when data changes.
    /// </summary>
    event EventHandler<PublicDataRefreshEventArgs>? DataChanged;

    /// <summary>
    /// Event raised when validation results change.
    /// </summary>
    event EventHandler<PublicValidationRefreshEventArgs>? ValidationChanged;

    /// <summary>
    /// Event raised when operation progress updates.
    /// </summary>
    event EventHandler<PublicOperationProgressEventArgs>? OperationProgress;

    /// <summary>
    /// Event raised when a cell is edited.
    /// </summary>
    event EventHandler<PublicCellEditEventArgs>? CellEdited;

    /// <summary>
    /// Event raised when selection changes.
    /// </summary>
    event EventHandler<PublicSelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Subscribes to data change notifications.
    /// </summary>
    /// <param name="handler">Event handler</param>
    void SubscribeToDataChanged(EventHandler<PublicDataRefreshEventArgs> handler);

    /// <summary>
    /// Unsubscribes from data change notifications.
    /// </summary>
    /// <param name="handler">Event handler</param>
    void UnsubscribeFromDataChanged(EventHandler<PublicDataRefreshEventArgs> handler);

    /// <summary>
    /// Subscribes to validation change notifications.
    /// </summary>
    /// <param name="handler">Event handler</param>
    void SubscribeToValidationChanged(EventHandler<PublicValidationRefreshEventArgs> handler);

    /// <summary>
    /// Unsubscribes from validation change notifications.
    /// </summary>
    /// <param name="handler">Event handler</param>
    void UnsubscribeFromValidationChanged(EventHandler<PublicValidationRefreshEventArgs> handler);

    /// <summary>
    /// Raises a custom notification.
    /// </summary>
    /// <param name="eventName">Event name</param>
    /// <param name="eventArgs">Event arguments</param>
    void RaiseCustomNotification(string eventName, object? eventArgs);

    /// <summary>
    /// Clears all event subscriptions.
    /// </summary>
    void ClearAllSubscriptions();

    /// <summary>
    /// Subscribes to validation refresh notifications (IDisposable pattern).
    /// Returns a disposable that unsubscribes when disposed.
    /// </summary>
    /// <param name="handler">Action handler for validation refresh events</param>
    /// <returns>Disposable subscription</returns>
    IDisposable SubscribeToValidationRefresh(Action<PublicValidationRefreshEventArgs> handler);

    /// <summary>
    /// Subscribes to data refresh notifications (IDisposable pattern).
    /// Returns a disposable that unsubscribes when disposed.
    /// </summary>
    /// <param name="handler">Action handler for data refresh events</param>
    /// <returns>Disposable subscription</returns>
    IDisposable SubscribeToDataRefresh(Action<PublicDataRefreshEventArgs> handler);

    /// <summary>
    /// Subscribes to operation progress notifications (IDisposable pattern).
    /// Returns a disposable that unsubscribes when disposed.
    /// </summary>
    /// <param name="handler">Action handler for operation progress events</param>
    /// <returns>Disposable subscription</returns>
    IDisposable SubscribeToOperationProgress(Action<PublicOperationProgressEventArgs> handler);

    /// <summary>
    /// Manually refreshes UI after operations.
    /// Available in both Interactive and Headless modes (if DispatcherQueue is provided).
    /// - Interactive mode: Automatic UI refresh after operations + manual via this method
    /// - Headless mode: NO automatic refresh, ONLY manual via this method
    /// </summary>
    /// <param name="operationType">Type of operation that triggered the refresh</param>
    /// <param name="affectedRows">Number of affected rows</param>
    Task RefreshUIAsync(string operationType = "ManualRefresh", int affectedRows = 0);
}

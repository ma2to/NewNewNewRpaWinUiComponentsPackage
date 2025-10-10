
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
}

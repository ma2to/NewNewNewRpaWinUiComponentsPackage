using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Notifications;

/// <summary>
/// Internal implementation of DataGrid notifications and events.
/// Delegates to internal notification service and provides event management.
/// </summary>
internal sealed class DataGridNotifications : IDataGridNotifications
{
    private readonly ILogger<DataGridNotifications>? _logger;
    private readonly UiNotificationService _uiNotificationService;

    public event EventHandler<PublicDataRefreshEventArgs>? DataChanged;
    public event EventHandler<PublicValidationRefreshEventArgs>? ValidationChanged;
    public event EventHandler<PublicOperationProgressEventArgs>? OperationProgress;
    public event EventHandler<PublicCellEditEventArgs>? CellEdited;
    public event EventHandler<PublicSelectionChangedEventArgs>? SelectionChanged;

    public DataGridNotifications(
        UiNotificationService uiNotificationService,
        ILogger<DataGridNotifications>? logger = null)
    {
        _uiNotificationService = uiNotificationService ?? throw new ArgumentNullException(nameof(uiNotificationService));
        _logger = logger;

        // Wire up internal events to public events
        _uiNotificationService.DataChanged += OnInternalDataChanged;
        _uiNotificationService.ValidationChanged += OnInternalValidationChanged;
        _uiNotificationService.OnOperationProgress += OnInternalOperationProgressAction;
        _uiNotificationService.CellEdited += OnInternalCellEdited;
        _uiNotificationService.SelectionChanged += OnInternalSelectionChanged;
    }

    public void SubscribeToDataChanged(EventHandler<PublicDataRefreshEventArgs> handler)
    {
        try
        {
            _logger?.LogInformation("Subscribing to DataChanged notifications via Notifications module");
            DataChanged += handler;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SubscribeToDataChanged failed in Notifications module");
            throw;
        }
    }

    public void UnsubscribeFromDataChanged(EventHandler<PublicDataRefreshEventArgs> handler)
    {
        try
        {
            _logger?.LogInformation("Unsubscribing from DataChanged notifications via Notifications module");
            DataChanged -= handler;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UnsubscribeFromDataChanged failed in Notifications module");
            throw;
        }
    }

    public void SubscribeToValidationChanged(EventHandler<PublicValidationRefreshEventArgs> handler)
    {
        try
        {
            _logger?.LogInformation("Subscribing to ValidationChanged notifications via Notifications module");
            ValidationChanged += handler;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SubscribeToValidationChanged failed in Notifications module");
            throw;
        }
    }

    public void UnsubscribeFromValidationChanged(EventHandler<PublicValidationRefreshEventArgs> handler)
    {
        try
        {
            _logger?.LogInformation("Unsubscribing from ValidationChanged notifications via Notifications module");
            ValidationChanged -= handler;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UnsubscribeFromValidationChanged failed in Notifications module");
            throw;
        }
    }

    public void RaiseCustomNotification(string eventName, object? eventArgs)
    {
        try
        {
            _logger?.LogInformation("Raising custom notification '{EventName}' via Notifications module", eventName);
            _uiNotificationService.RaiseCustomNotification(eventName, eventArgs);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RaiseCustomNotification failed in Notifications module");
            throw;
        }
    }

    public void ClearAllSubscriptions()
    {
        try
        {
            _logger?.LogInformation("Clearing all subscriptions via Notifications module");

            DataChanged = null;
            ValidationChanged = null;
            OperationProgress = null;
            CellEdited = null;
            SelectionChanged = null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearAllSubscriptions failed in Notifications module");
            throw;
        }
    }

    private void OnInternalDataChanged(object? sender, object e)
    {
        // Convert internal event args to public and raise
        if (e is PublicDataRefreshEventArgs publicArgs)
        {
            DataChanged?.Invoke(this, publicArgs);
        }
    }

    private void OnInternalValidationChanged(object? sender, object e)
    {
        if (e is PublicValidationRefreshEventArgs publicArgs)
        {
            ValidationChanged?.Invoke(this, publicArgs);
        }
    }

    private void OnInternalOperationProgressAction(string operationName, double progressPercentage, string? message)
    {
        var publicArgs = new PublicOperationProgressEventArgs
        {
            OperationName = operationName,
            ProgressPercentage = progressPercentage,
            Message = message
        };
        OperationProgress?.Invoke(this, publicArgs);
    }

    private void OnInternalCellEdited(object? sender, object e)
    {
        if (e is PublicCellEditEventArgs publicArgs)
        {
            CellEdited?.Invoke(this, publicArgs);
        }
    }

    private void OnInternalSelectionChanged(object? sender, object e)
    {
        if (e is PublicSelectionChangedEventArgs publicArgs)
        {
            SelectionChanged?.Invoke(this, publicArgs);
        }
    }
}

using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Partial class containing UI Notification Subscriptions
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region UI Notification Subscriptions

    /// <summary>
    /// Subscribes to validation refresh notifications
    /// </summary>
    public IDisposable SubscribeToValidationRefresh(Action<PublicValidationRefreshEventArgs> handler)
    {
        ThrowIfDisposed();

        if (_uiNotificationService == null)
        {
            throw new InvalidOperationException(
                "UI notification subscriptions are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _logger.LogDebug("Subscribing to validation refresh notifications");

        // Subscribe to internal event and wrap it
        Action<int, bool> internalHandler = (errorCount, hasErrors) =>
        {
            var eventArgs = new PublicValidationRefreshEventArgs
            {
                TotalErrors = errorCount,
                ErrorCount = hasErrors ? errorCount : 0,
                WarningCount = 0, // Not tracked separately in current implementation
                HasErrors = hasErrors,
                RefreshTime = DateTime.UtcNow
            };

            handler(eventArgs);
        };

        _uiNotificationService.OnValidationResultsRefreshed += internalHandler;

        // Return disposable that unsubscribes
        return new NotificationSubscription(() =>
        {
            _uiNotificationService.OnValidationResultsRefreshed -= internalHandler;
            _logger.LogDebug("Unsubscribed from validation refresh notifications");
        });
    }

    /// <summary>
    /// Subscribes to data refresh notifications
    /// </summary>
    public IDisposable SubscribeToDataRefresh(Action<PublicDataRefreshEventArgs> handler)
    {
        ThrowIfDisposed();

        if (_uiNotificationService == null)
        {
            throw new InvalidOperationException(
                "UI notification subscriptions are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _logger.LogDebug("Subscribing to data refresh notifications");

        // Subscribe to internal event and wrap it
        Action<int, int> internalHandler = (rowCount, columnCount) =>
        {
            var eventArgs = new PublicDataRefreshEventArgs
            {
                AffectedRows = rowCount,
                ColumnCount = columnCount,
                OperationType = "DataRefresh",
                RefreshTime = DateTime.UtcNow
            };

            handler(eventArgs);
        };

        _uiNotificationService.OnDataRefreshed += internalHandler;

        // Return disposable that unsubscribes
        return new NotificationSubscription(() =>
        {
            _uiNotificationService.OnDataRefreshed -= internalHandler;
            _logger.LogDebug("Unsubscribed from data refresh notifications");
        });
    }

    /// <summary>
    /// Subscribes to operation progress notifications
    /// </summary>
    public IDisposable SubscribeToOperationProgress(Action<PublicOperationProgressEventArgs> handler)
    {
        ThrowIfDisposed();

        if (_uiNotificationService == null)
        {
            throw new InvalidOperationException(
                "UI notification subscriptions are not available because DispatcherQueue was not provided in AdvancedDataGridOptions.");
        }

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _logger.LogDebug("Subscribing to operation progress notifications");

        // Subscribe to internal event and wrap it
        Action<string, double, string?> internalHandler = (operationName, progressPercentage, message) =>
        {
            var eventArgs = new PublicOperationProgressEventArgs
            {
                OperationName = operationName,
                ProcessedItems = 0, // Not tracked separately
                TotalItems = 0, // Not tracked separately
                ProgressPercentage = progressPercentage,
                Message = message,
                ElapsedTime = TimeSpan.Zero // Not tracked separately
            };

            handler(eventArgs);
        };

        _uiNotificationService.OnOperationProgress += internalHandler;

        // Return disposable that unsubscribes
        return new NotificationSubscription(() =>
        {
            _uiNotificationService.OnOperationProgress -= internalHandler;
            _logger.LogDebug("Unsubscribed from operation progress notifications");
        });
    }

    #endregion
}


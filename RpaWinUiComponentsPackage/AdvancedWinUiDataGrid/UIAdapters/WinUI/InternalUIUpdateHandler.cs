using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using System.Linq;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

/// <summary>
/// Internal handler for automatic UI updates in Interactive mode.
/// Subscribes to UiNotificationService.OnDataRefreshed and applies granular updates to DataGridViewModel.
/// This eliminates the need for full GetAllRows() + LoadData() rebuild for 10M+ row performance.
///
/// CRITICAL: This handler is ONLY active in Interactive mode. In Headless mode, no subscriptions are made.
/// </summary>
internal sealed class InternalUIUpdateHandler : IDisposable
{
    private readonly ILogger<InternalUIUpdateHandler> _logger;
    private readonly UiNotificationService _uiNotificationService;
    private readonly DataGridViewModel? _viewModel;
    private readonly IRowStore _rowStore;
    private readonly AdvancedDataGridOptions _options;
    private readonly DispatcherQueue? _dispatcherQueue;
    private bool _isDisposed;

    /// <summary>
    /// Creates internal UI update handler.
    /// Automatically subscribes to UI refresh events ONLY in Interactive mode.
    /// </summary>
    public InternalUIUpdateHandler(
        UiNotificationService uiNotificationService,
        IRowStore rowStore,
        AdvancedDataGridOptions options,
        DispatcherQueue? dispatcherQueue = null,
        DataGridViewModel? viewModel = null,
        ILogger<InternalUIUpdateHandler>? logger = null)
    {
        _uiNotificationService = uiNotificationService ?? throw new ArgumentNullException(nameof(uiNotificationService));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _dispatcherQueue = dispatcherQueue;
        _viewModel = viewModel;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // ✅ Subscribe ONLY in Interactive mode
        if (_options.OperationMode == PublicDataGridOperationMode.Interactive)
        {
            _uiNotificationService.OnDataRefreshed += HandleDataRefreshWithMetadata;
            _logger.LogInformation("InternalUIUpdateHandler activated for Interactive mode (granular updates enabled)");
        }
        else
        {
            _logger.LogInformation("InternalUIUpdateHandler initialized but inactive (mode={Mode})", _options.OperationMode);
        }
    }

    /// <summary>
    /// Handles data refresh events with granular metadata.
    /// Applies incremental UI updates instead of full rebuild for 10M+ row performance.
    /// </summary>
    private void HandleDataRefreshWithMetadata(PublicDataRefreshEventArgs eventArgs)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot handle data refresh - handler is disposed");
            return;
        }

        if (_viewModel == null)
        {
            _logger.LogDebug("No ViewModel bound - skipping granular UI update");
            return;
        }

        _logger.LogInformation("Handling data refresh internally: Operation={Op}, PhysicalDeletes={Del}, ContentClears={Clr}, Updates={Upd}",
            eventArgs.OperationType,
            eventArgs.PhysicallyDeletedIndices.Count,
            eventArgs.ContentClearedIndices.Count,
            eventArgs.UpdatedRowData.Count);

        try
        {
            // Execute on UI thread if DispatcherQueue is available
            if (_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    ApplyGranularUpdates(eventArgs);
                });
            }
            else
            {
                // No dispatcher - execute synchronously (for testing or non-UI scenarios)
                ApplyGranularUpdates(eventArgs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Internal UI update failed for operation {Op}", eventArgs.OperationType);
        }
    }

    /// <summary>
    /// Applies granular updates to the ViewModel based on event metadata.
    /// This is the core optimization that eliminates full rebuild for 10M+ rows.
    /// FALLBACK: If no granular metadata is available, performs full reload from IRowStore.
    /// </summary>
    private void ApplyGranularUpdates(PublicDataRefreshEventArgs eventArgs)
    {
        if (_viewModel == null)
            return;

        try
        {
            bool hasGranularMetadata = eventArgs.PhysicallyDeletedIndices.Any() ||
                                       eventArgs.ContentClearedIndices.Any() ||
                                       eventArgs.UpdatedRowData.Any();

            // SCENARIO A: Physical delete → Remove rows from ViewModel
            // This fires NotifyCollectionChangedAction.Remove instead of Reset
            if (eventArgs.PhysicallyDeletedIndices.Any())
            {
                _logger.LogDebug("Applying {Count} physical row deletions", eventArgs.PhysicallyDeletedIndices.Count);

                // CRITICAL: Track if any invalid indices detected (indicates UI/Backend desync)
                var hadInvalidIndex = false;

                // Sort descending to avoid index shifting issues during removal
                foreach (var deletedIndex in eventArgs.PhysicallyDeletedIndices.OrderByDescending(i => i))
                {
                    if (deletedIndex >= 0 && deletedIndex < _viewModel.Rows.Count)
                    {
                        _viewModel.Rows.RemoveAt(deletedIndex); // ✅ Granular RemoveAt()
                        _logger.LogTrace("Removed row at index {Index}", deletedIndex);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid delete index {Index} (ViewModel has {Count} rows)",
                            deletedIndex, _viewModel.Rows.Count);
                        hadInvalidIndex = true;
                    }
                }

                // CRITICAL FIX: If invalid index detected, UI and Backend are out of sync → full reload
                if (hadInvalidIndex)
                {
                    _logger.LogWarning("Invalid indices detected - UI/Backend desynchronized - performing full reload to resync");
                    PerformFullReload();
                    return; // Exit early, full reload handles everything
                }
            }

            // SCENARIO B: Content cleared → Update cell values to null
            if (eventArgs.ContentClearedIndices.Any())
            {
                _logger.LogDebug("Applying {Count} content clears", eventArgs.ContentClearedIndices.Count);

                foreach (var clearedIndex in eventArgs.ContentClearedIndices)
                {
                    if (clearedIndex >= 0 && clearedIndex < _viewModel.Rows.Count)
                    {
                        var rowViewModel = _viewModel.Rows[clearedIndex];
                        foreach (var cell in rowViewModel.Cells.Where(c => !c.IsSpecialColumn))
                        {
                            cell.Value = null; // Clear content
                        }
                        _logger.LogTrace("Cleared content at row {Index}", clearedIndex);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid clear index {Index} (ViewModel has {Count} rows)",
                            clearedIndex, _viewModel.Rows.Count);
                    }
                }
            }

            // SCENARIO C: Shifted/updated rows → Update cell values
            if (eventArgs.UpdatedRowData.Any())
            {
                _logger.LogDebug("Applying {Count} row updates (shifted rows)", eventArgs.UpdatedRowData.Count);

                foreach (var kvp in eventArgs.UpdatedRowData)
                {
                    int rowIndex = kvp.Key;
                    var newRowData = kvp.Value;

                    if (rowIndex >= 0 && rowIndex < _viewModel.Rows.Count)
                    {
                        var rowViewModel = _viewModel.Rows[rowIndex];
                        foreach (var cell in rowViewModel.Cells.Where(c => !c.IsSpecialColumn))
                        {
                            if (newRowData.TryGetValue(cell.ColumnName, out var newValue))
                            {
                                cell.Value = newValue; // Update shifted value
                            }
                        }
                        _logger.LogTrace("Updated shifted row at index {Index}", rowIndex);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid update index {Index} (ViewModel has {Count} rows)",
                            rowIndex, _viewModel.Rows.Count);
                    }
                }
            }

            // FALLBACK: No granular metadata → Full reload from IRowStore
            // This happens after Import, AddRow, or other operations that don't provide granular updates
            if (!hasGranularMetadata)
            {
                _logger.LogInformation("No granular metadata available for operation {Op} - performing full reload from IRowStore",
                    eventArgs.OperationType);

                PerformFullReload();
            }
            else
            {
                _logger.LogInformation("Granular UI updates completed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply granular UI updates");
            // Don't rethrow - we want to be resilient to UI update failures
        }
    }

    /// <summary>
    /// Performs full reload of ViewModel from IRowStore.
    /// Used as fallback when granular metadata is not available (e.g., after Import, AddRow).
    /// </summary>
    private void PerformFullReload()
    {
        if (_viewModel == null)
            return;

        try
        {
            _logger.LogDebug("Performing full reload from IRowStore...");

            // Get all rows from backend
            var allRows = _rowStore.GetAllRows();

            if (allRows == null || allRows.Count == 0)
            {
                _logger.LogDebug("No data in IRowStore - clearing ViewModel");
                _viewModel.InitializeColumns(new List<string>(), _options);
                _viewModel.LoadRows(new List<Dictionary<string, object?>>());
                return;
            }

            // Extract column headers from first row
            var headers = allRows.First().Keys.ToList();

            _logger.LogDebug("Reloading {RowCount} rows with {ColumnCount} columns", allRows.Count, headers.Count);

            // Load data into ViewModel (InitializeColumns first, then LoadRows)
            _viewModel.InitializeColumns(headers, _options);
            _viewModel.LoadRows(allRows);

            _logger.LogInformation("Full reload completed - loaded {RowCount} rows", allRows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform full reload from IRowStore");
        }
    }

    /// <summary>
    /// Disposes the handler and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_options.OperationMode == PublicDataGridOperationMode.Interactive)
        {
            _uiNotificationService.OnDataRefreshed -= HandleDataRefreshWithMetadata;
            _logger.LogInformation("InternalUIUpdateHandler deactivated (unsubscribed from events)");
        }

        _isDisposed = true;
    }
}

using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

/// <summary>
/// Internal handler for automatic UI-triggered operations in Interactive mode.
/// Subscribes to UI events (DeleteRowRequested, CellEditCompleted) and automatically calls facade operations.
/// This eliminates the need for application code to handle these events in Interactive mode.
///
/// CRITICAL: This handler is ONLY active in Interactive mode. In Headless/Readonly mode, no subscriptions are made.
/// </summary>
internal sealed class InternalUIOperationHandler : IDisposable
{
    private readonly ILogger<InternalUIOperationHandler> _logger;
    private readonly AdvancedDataGridControl? _uiControl;
    private readonly IAdvancedDataGridFacade _facade;
    private readonly AdvancedDataGridOptions _options;
    private bool _isDisposed;

    /// <summary>
    /// Creates internal UI operation handler.
    /// Automatically subscribes to UI events ONLY in Interactive mode.
    /// </summary>
    public InternalUIOperationHandler(
        IAdvancedDataGridFacade facade,
        AdvancedDataGridOptions options,
        AdvancedDataGridControl? uiControl = null,
        ILogger<InternalUIOperationHandler>? logger = null)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _uiControl = uiControl;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // ✅ Subscribe ONLY in Interactive mode
        if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiControl != null)
        {
            _uiControl.DeleteRowRequested += OnDeleteRowRequested;
            _uiControl.CellEditCompleted += OnCellEditCompleted;
            _logger.LogInformation("InternalUIOperationHandler activated for Interactive mode (auto-delete and auto-expand enabled)");
        }
        else
        {
            _logger.LogInformation("InternalUIOperationHandler initialized but inactive (mode={Mode}, hasUIControl={HasControl})",
                _options.OperationMode, _uiControl != null);
        }
    }

    /// <summary>
    /// Handles delete row requests from UI control.
    /// Automatically calls facade.SmartOperations.SmartDeleteRowByIdAsync with default config.
    /// </summary>
    private async void OnDeleteRowRequested(object? sender, DeleteRowRequestedEventArgs args)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot handle delete request - handler is disposed");
            return;
        }

        try
        {
            _logger.LogInformation("Auto-handling delete request for row {RowIndex}, rowId {RowId}", args.RowIndex, args.RowId);

            // Use default smart operations config (always keep last empty)
            var config = PublicSmartOperationsConfig.Create(
                enableSmartDelete: true,
                enableAutoExpand: true,
                alwaysKeepLastEmpty: true
            );

            // CRITICAL: Use rowId-based delete to avoid index shifting bugs
            PublicSmartOperationResult result;
            if (!string.IsNullOrEmpty(args.RowId))
            {
                result = await _facade.SmartOperations.SmartDeleteRowByIdAsync(args.RowId, config);
            }
            else
            {
                _logger.LogWarning("RowId is null, falling back to index-based delete");
                result = await _facade.SmartOperations.SmartDeleteRowAsync(args.RowIndex, config);
            }

            if (result.IsSuccess)
            {
                _logger.LogInformation("Auto-delete successful: {RowCount} rows, {PhysicalDeletes} physical, {ContentClears} cleared",
                    result.FinalRowCount, result.Statistics.RowsPhysicallyDeleted, result.Statistics.RowsContentCleared);
            }
            else
            {
                _logger.LogError("Auto-delete failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during auto-delete handling");
        }
    }

    /// <summary>
    /// Handles cell edit completion from UI control.
    /// CRITICAL: Syncs cell value to backend storage FIRST, then triggers auto-expand if needed.
    /// This ensures ViewModel and IRowStore are always synchronized, preventing data loss during full reloads.
    /// </summary>
    private async void OnCellEditCompleted(object? sender, CellViewModel cell)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot handle cell edit - handler is disposed");
            return;
        }

        if (_uiControl == null)
        {
            return;
        }

        try
        {
            var totalRows = _uiControl.ViewModel.Rows.Count;
            if (totalRows == 0)
            {
                return; // No rows, nothing to do
            }

            // STEP 1: ALWAYS sync cell value to backend storage (IRowStore)
            // This is CRITICAL - without this, ViewModel changes are lost during full reload
            if (!cell.IsSpecialColumn)
            {
                _logger.LogInformation("Cell edit completed: row {RowIndex}, column {ColumnName}, value '{Value}' - syncing to backend",
                    cell.RowIndex, cell.ColumnName, cell.Value);

                try
                {
                    // Use Editing API to update cell value in backend storage
                    var updateResult = await _facade.Editing.UpdateCellAsync(
                        cell.RowIndex,
                        cell.ColumnName,
                        cell.Value,
                        CancellationToken.None
                    );

                    if (updateResult.IsSuccess)
                    {
                        _logger.LogDebug("Cell value synced to backend successfully: row {RowIndex}, column {ColumnName}",
                            cell.RowIndex, cell.ColumnName);
                    }
                    else
                    {
                        _logger.LogError("Failed to sync cell value to backend: row {RowIndex}, column {ColumnName}, error: {Error}",
                            cell.RowIndex, cell.ColumnName, updateResult.ErrorMessage);
                        // Continue anyway - auto-expand still needs to run
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception syncing cell value to backend: row {RowIndex}, column {ColumnName}",
                        cell.RowIndex, cell.ColumnName);
                    // Continue anyway - auto-expand still needs to run
                }
            }

            // STEP 2: Check if we need auto-expand (only if editing last row)
            var lastRowIndex = totalRows - 1;

            if (cell.RowIndex != lastRowIndex)
            {
                // Not the last row, no auto-expand needed
                return;
            }

            _logger.LogDebug("Cell edited in last row (row {RowIndex}, column {ColumnName})", cell.RowIndex, cell.ColumnName);

            // Check if the last row is still empty (excluding __rowId and special columns)
            var lastRow = _uiControl.ViewModel.Rows[lastRowIndex];
            var hasData = lastRow.Cells
                .Where(c => !c.IsSpecialColumn) // Ignore special columns
                .Any(c => c.Value != null && !string.IsNullOrWhiteSpace(c.Value.ToString()));

            if (!hasData)
            {
                _logger.LogDebug("Last row is still empty after edit - no auto-expand needed");
                return;
            }

            // Last row has data → Trigger auto-expand to add new empty row
            _logger.LogInformation("Last row now has data - triggering auto-expand...");

            var config = PublicSmartOperationsConfig.Create(
                enableSmartDelete: true,
                enableAutoExpand: true,
                alwaysKeepLastEmpty: true
            );

            var result = await _facade.SmartOperations.AutoExpandEmptyRowAsync(config);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Auto-expand successful: {EmptyRowsCreated} empty rows created, final count {FinalRowCount}",
                    result.Statistics.EmptyRowsCreated, result.FinalRowCount);
            }
            else
            {
                _logger.LogError("Auto-expand failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during cell edit completion handling");
        }
    }

    /// <summary>
    /// Disposes the handler and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiControl != null)
        {
            _uiControl.DeleteRowRequested -= OnDeleteRowRequested;
            _uiControl.CellEditCompleted -= OnCellEditCompleted;
            _logger.LogInformation("InternalUIOperationHandler deactivated (unsubscribed from events)");
        }

        _isDisposed = true;
    }
}

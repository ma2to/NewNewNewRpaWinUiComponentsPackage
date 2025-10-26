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
            _uiControl.InsertRowRequested += OnInsertRowRequested;
            _uiControl.CellEditCompleted += OnCellEditCompleted;
            _logger.LogInformation("InternalUIOperationHandler activated for Interactive mode (auto-delete, auto-insert, and auto-expand enabled)");
        }
        else
        {
            _logger.LogInformation("InternalUIOperationHandler initialized but inactive (mode={Mode}, hasUIControl={HasControl})",
                _options.OperationMode, _uiControl != null);
        }
    }

    /// <summary>
    /// Handles delete row requests from UI control.
    /// NEW ARCHITECTURE: Uses facade.Rows.RemoveRowsAsync for data-shifting deletion.
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

            // NEW ARCHITECTURE: Direct row deletion with automatic data shifting
            // CRITICAL: Use rowId-based delete to avoid index shifting bugs
            if (!string.IsNullOrEmpty(args.RowId))
            {
                var result = await _facade.Rows.RemoveRowsAsync(new[] { args.RowId });

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Auto-delete successful: {RowsDeleted} rows deleted", result.Data);
                }
                else
                {
                    _logger.LogError("Auto-delete failed: {Error}", result.ErrorMessage);
                }
            }
            else
            {
                _logger.LogWarning("RowId is null - cannot perform delete (rowId required in new architecture)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during auto-delete handling");
        }
    }

    /// <summary>
    /// Handles insert row requests from UI control.
    /// NEW ARCHITECTURE: Uses facade.Rows.InsertRowAsync to insert empty row at position.
    /// </summary>
    private async void OnInsertRowRequested(object? sender, InsertRowRequestedEventArgs args)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot handle insert request - handler is disposed");
            return;
        }

        try
        {
            _logger.LogInformation("Auto-handling insert row request for row {RowIndex}, rowId {RowId}", args.RowIndex, args.RowId);
            _logger.LogDebug("Insert row request triggered from UI button click");

            // MIGRATED: Use stable rowId instead of volatile rowIndex
            // Insert empty row after specified row by stable rowId
            if (!string.IsNullOrEmpty(args.RowId))
            {
                var result = await _facade.Rows.InsertRowAfterIdAsync(args.RowId, null); // null = empty row

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Auto-insert successful: inserted after rowId {RowId}", args.RowId);
                    _logger.LogDebug("Auto-insert operation completed successfully");
                }
                else
                {
                    _logger.LogError("Auto-insert failed: {Error}", result.ErrorMessage);
                }
            }
            else
            {
                _logger.LogWarning("Insert row request has no RowId, cannot insert");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during auto-insert handling: row {RowIndex}, rowId {RowId}",
                args.RowIndex, args.RowId);
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
                _logger.LogInformation("Cell edit completed: row {RowIndex}, rowId {RowId}, column {ColumnName}, value '{Value}' - syncing to backend",
                    cell.RowIndex, cell.RowId, cell.ColumnName, cell.Value);

                try
                {
                    // CRITICAL FIX: Use rowId to find current row index (protects against row shifting during concurrent delete operations)
                    // Between cell edit start and completion, rows may have been deleted/shifted → rowIndex may be stale
                    int currentRowIndex = cell.RowIndex;

                    if (!string.IsNullOrEmpty(cell.RowId))
                    {
                        // Find current row index by rowId in ViewModel
                        var rowViewModel = _uiControl.ViewModel.Rows.FirstOrDefault(r => r.RowId == cell.RowId);
                        if (rowViewModel != null)
                        {
                            currentRowIndex = _uiControl.ViewModel.Rows.IndexOf(rowViewModel);
                            if (currentRowIndex != cell.RowIndex)
                            {
                                _logger.LogInformation("Row index shifted during edit: original={Original}, current={Current}, rowId={RowId}",
                                    cell.RowIndex, currentRowIndex, cell.RowId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Row with rowId {RowId} not found in ViewModel - row may have been deleted during edit", cell.RowId);
                            return; // Row was deleted, skip update
                        }
                    }

                    // BREAKING CHANGE v3.0: Use rowId instead of rowIndex (stable across sort/filter/delete)
                    // Use Editing API to update cell value in backend storage with stable rowId
                    var updateResult = await _facade.Editing.UpdateCellAsync(
                        cell.RowId,
                        cell.ColumnName,
                        cell.Value,
                        CancellationToken.None
                    );

                    if (updateResult.IsSuccess)
                    {
                        _logger.LogDebug("Cell value synced to backend successfully: row {RowIndex}, column {ColumnName}",
                            currentRowIndex, cell.ColumnName);
                    }
                    else
                    {
                        _logger.LogError("Failed to sync cell value to backend: row {RowIndex}, column {ColumnName}, error: {Error}",
                            currentRowIndex, cell.ColumnName, updateResult.ErrorMessage);
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

            // NEW ARCHITECTURE: Add empty row at end using Rows.AddRowAsync
            var result = await _facade.Rows.AddRowAsync(null); // null = empty row

            if (result.IsSuccess)
            {
                _logger.LogInformation("Auto-expand successful: added empty row at end");
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
            _uiControl.InsertRowRequested -= OnInsertRowRequested;
            _uiControl.CellEditCompleted -= OnCellEditCompleted;
            _logger.LogInformation("InternalUIOperationHandler deactivated (unsubscribed from events)");
        }

        _isDisposed = true;
    }
}

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
    /// PROFESSIONAL SOLUTION: Uses virtual delete to shift data up without changing row count.
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
            _logger.LogInformation("AUTO-DELETE (VIRTUAL): Delete request for row {RowIndex}, rowId {RowId}", args.RowIndex, args.RowId);

            if (!string.IsNullOrEmpty(args.RowId))
            {
                // ✅ PROFESSIONAL SOLUTION: Use virtual delete instead of physical delete
                // EFEKT: Dáta riadku sa úplne zmažú (posunú všetky nasledujúce riadky nahor), posledný riadok ostane prázdny
                var result = await _facade.Rows.VirtualDeleteRowAsync(args.RowId);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("AUTO-DELETE (VIRTUAL): Row data completely deleted (shifted up): {RowId}", args.RowId);
                }
                else
                {
                    _logger.LogError("AUTO-DELETE (VIRTUAL): Delete failed: {Error}", result.ErrorMessage);
                }
            }
            else
            {
                _logger.LogWarning("RowId is null - cannot perform virtual delete");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during virtual delete handling");
        }
    }

    /// <summary>
    /// Handles insert row requests from UI control (InsertRow special column button).
    /// PROFESSIONAL SOLUTION: Uses virtual insert to shift data down without changing row count.
    /// CRITICAL: Only active in Interactive mode - automatically inserts empty row below clicked row.
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
            _logger.LogInformation("AUTO-INSERT (VIRTUAL): User clicked InsertRow button at RowIndex={RowIndex}, RowId={RowId}",
                args.RowIndex, args.RowId ?? "(NULL)");

            // ✅ ENHANCED FIX: Better null handling with detailed logging
            if (string.IsNullOrEmpty(args.RowId))
            {
                _logger.LogError("AUTO-INSERT: CRITICAL BUG - RowId is NULL! Cannot insert row. " +
                    "This indicates CellViewModel.RowId is not set correctly. RowIndex={RowIndex}", args.RowIndex);
                return;
            }

            // ✅ PROFESSIONAL SOLUTION: Use virtual insert instead of physical insert
            var result = await _facade.Rows.VirtualInsertEmptyRowAfterAsync(args.RowId, CancellationToken.None);

            if (result.IsSuccess)
            {
                _logger.LogInformation("AUTO-INSERT (VIRTUAL): Empty row inserted virtually after rowId {RowId}", args.RowId);
                // UI refresh handled automatically by InternalUIUpdateHandler
            }
            else
            {
                _logger.LogError("AUTO-INSERT (VIRTUAL): Insert failed - {ErrorMessage}. RowIndex={RowIndex}, RowId={RowId}",
                    result.ErrorMessage, args.RowIndex, args.RowId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AUTO-INSERT (VIRTUAL): Exception during virtual insert operation. RowIndex={RowIndex}, RowId={RowId}",
                args.RowIndex, args.RowId ?? "(NULL)");
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

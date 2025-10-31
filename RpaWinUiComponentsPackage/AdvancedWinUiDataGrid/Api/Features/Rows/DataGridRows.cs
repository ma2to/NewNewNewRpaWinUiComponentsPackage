using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Rows;

/// <summary>
/// Internal implementation of DataGrid row operations.
/// Delegates to internal Row Store.
/// </summary>
internal sealed class DataGridRows : IDataGridRows
{
    private readonly ILogger<DataGridRows>? _logger;
    private readonly IRowStore _rowStore;
    private readonly UiNotificationService? _uiNotificationService;
    private readonly AdvancedDataGridOptions _options;
    private readonly Features.Validation.Interfaces.IValidationService? _validationService;

    public DataGridRows(
        IRowStore rowStore,
        AdvancedDataGridOptions options,
        UiNotificationService? uiNotificationService = null,
        Features.Validation.Interfaces.IValidationService? validationService = null,
        ILogger<DataGridRows>? logger = null)
    {
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _uiNotificationService = uiNotificationService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<PublicResult<int>> AddRowAsync(IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        try
        {
            // CRITICAL: Validate that __rowId is not used as a column name (reserved for internal use)
            ValidateNoReservedColumnNames(rowData);

            _logger?.LogInformation("Adding row via Rows module");
            var rowIndex = await _rowStore.AddRowAsync(rowData, cancellationToken);

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("AddRow", 1);

            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = "Row added successfully",
                Data = rowIndex
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AddRow failed in Rows module");
            throw;
        }
    }

    public async Task<PublicResult<int>> AddRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rowsData, CancellationToken cancellationToken = default)
    {
        try
        {
            // CRITICAL: Validate that __rowId is not used as a column name (reserved for internal use)
            var rowsList = rowsData.ToList();
            var firstRow = rowsList.FirstOrDefault();
            if (firstRow != null)
            {
                ValidateNoReservedColumnNames(firstRow);
            }

            _logger?.LogInformation("Adding multiple rows via Rows module");
            var count = await _rowStore.AddRowsAsync(rowsList, cancellationToken);

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("AddRows", count);

            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = $"Added {count} rows successfully",
                Data = count
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AddRows failed in Rows module");
            throw;
        }
    }

    // public async Task<PublicResult> InsertRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    // {
    //     try
    //     {
    //         // CRITICAL: Validate that __rowId is not used as a column name (reserved for internal use)
    //         ValidateNoReservedColumnNames(rowData);
    //
    //         _logger?.LogInformation("Inserting row at index {RowIndex} via Rows module", rowIndex);
    //         await _rowStore.InsertRowAsync(rowIndex, rowData, cancellationToken);
    //
    //         // Trigger automatic UI refresh in Interactive mode
    //         await TriggerUIRefreshIfNeededAsync("InsertRow", 1);
    //
    //         return new PublicResult
    //         {
    //             IsSuccess = true,
    //             Message = "Row inserted successfully"
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger?.LogError(ex, "InsertRow failed in Rows module");
    //         throw;
    //     }
    // }

    /// <summary>
    /// Inserts a row before a specific row by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    public async Task<PublicResult> InsertRowBeforeIdAsync(string referenceRowId, IReadOnlyDictionary<string, object?>? rowData, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate row data if provided
            if (rowData != null)
            {
                ValidateNoReservedColumnNames(rowData);
            }

            _logger?.LogInformation("Inserting row before rowId {ReferenceRowId} via Rows module", referenceRowId);

            // Find the current index of the reference row
            var referenceIndex = _rowStore.GetRowIndexById(referenceRowId);
            if (referenceIndex == null)
            {
                return new PublicResult
                {
                    IsSuccess = false,
                    Message = $"Reference row {referenceRowId} not found"
                };
            }

            // Insert at the reference index (before the reference row)
            await _rowStore.InsertRowAsync(referenceIndex.Value, rowData ?? new Dictionary<string, object?>(), cancellationToken);

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("InsertRowBeforeId", 1);

            return new PublicResult
            {
                IsSuccess = true,
                Message = "Row inserted before reference row successfully"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "InsertRowBeforeId failed in Rows module for referenceRowId {ReferenceRowId}", referenceRowId);
            throw;
        }
    }

    /// <summary>
    /// Inserts a row after a specific row by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    public async Task<PublicResult> InsertRowAfterIdAsync(string referenceRowId, IReadOnlyDictionary<string, object?>? rowData, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate row data if provided
            if (rowData != null)
            {
                ValidateNoReservedColumnNames(rowData);
            }

            _logger?.LogInformation("Inserting row after rowId {ReferenceRowId} via Rows module", referenceRowId);

            // Find the current index of the reference row
            var referenceIndex = _rowStore.GetRowIndexById(referenceRowId);
            if (referenceIndex == null)
            {
                return new PublicResult
                {
                    IsSuccess = false,
                    Message = $"Reference row {referenceRowId} not found"
                };
            }

            // Insert after the reference index
            await _rowStore.InsertRowAsync(referenceIndex.Value + 1, rowData ?? new Dictionary<string, object?>(), cancellationToken);

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("InsertRowAfterId", 1);

            return new PublicResult
            {
                IsSuccess = true,
                Message = "Row inserted after reference row successfully"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "InsertRowAfterId failed in Rows module for referenceRowId {ReferenceRowId}", referenceRowId);
            throw;
        }
    }

    /// <summary>
    /// Inserts an empty row after a specific row by stable row ID.
    /// Convenience method for Interactive mode where empty rows are inserted via UI button.
    /// This method creates an empty row with all column values set to null.
    /// </summary>
    public async Task<PublicResult> InsertEmptyRowAfterAsync(string referenceRowId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("PUBLIC API: InsertEmptyRowAfterAsync called for rowId {RowId}", referenceRowId);

            if (string.IsNullOrEmpty(referenceRowId))
            {
                _logger?.LogWarning("PUBLIC API: InsertEmptyRowAfterAsync called with null/empty referenceRowId");
                return PublicResult.Failure("Reference row ID cannot be null or empty");
            }

            // Create empty row data with all columns set to null
            // Get column names from first existing row (if available)
            // ✅ DEADLOCK FIX: Use GetRowAsync instead of GetRow
            var firstRow = await _rowStore.GetRowAsync(0, cancellationToken);
            var emptyRowData = new Dictionary<string, object?>();

            if (firstRow != null)
            {
                // ✅ FIX: Use columns from first row as template, but EXCLUDE reserved column names
                // CRITICAL: __rowId is reserved for internal use and auto-generated by IRowStore
                foreach (var colName in firstRow.Keys)
                {
                    if (colName == "__rowId")
                    {
                        // Skip reserved internal column - will be auto-generated by row store
                        continue;
                    }
                    emptyRowData[colName] = null;
                }

                _logger?.LogDebug("PUBLIC API: Created empty row data with {ColumnCount} columns (excluded __rowId)", emptyRowData.Count);
            }
            else
            {
                // No rows exist - cannot determine columns, return error
                _logger?.LogWarning("PUBLIC API: InsertEmptyRowAfterAsync called but no rows exist to determine column structure");
                return PublicResult.Failure("Cannot insert empty row - no existing rows to determine column structure");
            }

            // Use existing InsertRowAfterIdAsync with empty row data
            var result = await InsertRowAfterIdAsync(referenceRowId, emptyRowData, cancellationToken);

            if (result.IsSuccess)
            {
                _logger?.LogInformation("PUBLIC API: Empty row inserted successfully after rowId {RowId}", referenceRowId);
            }
            else
            {
                _logger?.LogWarning("PUBLIC API: InsertEmptyRowAfterAsync failed: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "PUBLIC API: Exception in InsertEmptyRowAfterAsync for rowId {RowId}", referenceRowId);
            return PublicResult.Failure($"Failed to insert empty row: {ex.Message}");
        }
    }

    public async Task<PublicResult> UpdateRowAsync(string rowId, IReadOnlyDictionary<string, object?> rowData, CancellationToken cancellationToken = default)
    {
        try
        {
            // CRITICAL: Validate that __rowId is not used as a column name (reserved for internal use)
            ValidateNoReservedColumnNames(rowData);

            _logger?.LogInformation("Updating row {RowId} via Rows module", rowId);

            var success = await _rowStore.UpdateRowByIdAsync(rowId, rowData, cancellationToken);

            if (!success)
            {
                return new PublicResult
                {
                    IsSuccess = false,
                    Message = $"Row {rowId} not found or update failed"
                };
            }

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("UpdateRow", 1);

            return new PublicResult
            {
                IsSuccess = true,
                Message = "Row updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UpdateRow failed in Rows module");
            throw;
        }
    }

    public async Task<PublicResult> RemoveRowAsync(string rowId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Removing row {RowId} via Rows module", rowId);

            var success = await _rowStore.RemoveRowByIdAsync(rowId, cancellationToken);

            if (!success)
            {
                return new PublicResult
                {
                    IsSuccess = false,
                    Message = $"Row {rowId} not found or removal failed"
                };
            }

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("RemoveRow", 1);

            return new PublicResult
            {
                IsSuccess = true,
                Message = "Row removed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RemoveRow failed in Rows module");
            throw;
        }
    }

    public async Task<PublicResult<int>> RemoveRowsAsync(IEnumerable<string> rowIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowIdsList = rowIds.ToList();
            _logger?.LogInformation("Removing {Count} rows via Rows module", rowIdsList.Count);

            await _rowStore.RemoveRowsAsync(rowIdsList, cancellationToken);

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("RemoveRows", rowIdsList.Count);

            return new PublicResult<int>
            {
                IsSuccess = true,
                Message = $"Removed {rowIdsList.Count} rows successfully",
                Data = rowIdsList.Count
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RemoveRows failed in Rows module");
            throw;
        }
    }

    public async Task<PublicResult> ClearAllRowsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Clearing all rows via Rows module");
            // ✅ DEADLOCK FIX: Use async method instead of sync
            var currentCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);
            await _rowStore.ClearAllRowsAsync(cancellationToken);

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("ClearAllRows", currentCount);

            return new PublicResult
            {
                IsSuccess = true,
                Message = "All rows cleared successfully"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ClearAllRows failed in Rows module");
            throw;
        }
    }

    // public IReadOnlyDictionary<string, object?>? GetRow(int rowIndex)
    // {
    //     try
    //     {
    //         return _rowStore.GetRow(rowIndex);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger?.LogError(ex, "GetRow failed in Rows module for row {RowIndex}", rowIndex);
    //         throw;
    //     }
    // }

    /// <summary>
    /// Gets row data by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? GetRow(string rowId)
    {
        try
        {
            return _rowStore.GetRowById(rowId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetRow failed in Rows module for rowId {RowId}", rowId);
            throw;
        }
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetAllRows()
    {
        try
        {
            return _rowStore.GetAllRows();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetAllRows failed in Rows module");
            throw;
        }
    }

    public int GetRowCount()
    {
        try
        {
            return _rowStore.GetRowCount();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetRowCount failed in Rows module");
            throw;
        }
    }

    // public bool RowExists(int rowIndex)
    // {
    //     try
    //     {
    //         return _rowStore.RowExists(rowIndex);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger?.LogError(ex, "RowExists check failed in Rows module for row {RowIndex}", rowIndex);
    //         throw;
    //     }
    // }

    /// <summary>
    /// Checks if a row exists by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    public bool RowExists(string rowId)
    {
        try
        {
            return _rowStore.RowExistsById(rowId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RowExists check failed in Rows module for rowId {RowId}", rowId);
            throw;
        }
    }

    // public async Task<PublicResult<int>> DuplicateRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    // {
    //     try
    //     {
    //         _logger?.LogInformation("Duplicating row {RowIndex} via Rows module", rowIndex);
    //         var rowData = _rowStore.GetRow(rowIndex);
    //         if (rowData == null)
    //         {
    //             return new PublicResult<int>
    //             {
    //                 IsSuccess = false,
    //                 Message = $"Row {rowIndex} not found",
    //                 Data = -1
    //             };
    //         }
    //
    //         var newRowIndex = await _rowStore.AddRowAsync(rowData, cancellationToken);
    //
    //         // Trigger automatic UI refresh in Interactive mode
    //         await TriggerUIRefreshIfNeededAsync("DuplicateRow", 1);
    //
    //         return new PublicResult<int>
    //         {
    //             IsSuccess = true,
    //             Message = "Row duplicated successfully",
    //             Data = newRowIndex
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger?.LogError(ex, "DuplicateRow failed in Rows module");
    //         throw;
    //     }
    // }

    /// <summary>
    /// Duplicates a row by stable row ID.
    /// STABLE: Uses rowId which persists across sort/filter/delete operations.
    /// </summary>
    public async Task<PublicResult<string>> DuplicateRowAsync(string rowId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Duplicating row {RowId} via Rows module", rowId);
            var rowData = _rowStore.GetRowById(rowId);
            if (rowData == null)
            {
                return new PublicResult<string>
                {
                    IsSuccess = false,
                    Message = $"Row {rowId} not found",
                    Data = string.Empty
                };
            }

            // Add the duplicated row
            var newRowIndex = await _rowStore.AddRowAsync(rowData, cancellationToken);

            // Get the rowId of the newly created row
            var newRowId = _rowStore.GetRowIdByIndex(newRowIndex);

            // Trigger automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("DuplicateRow", 1);

            return new PublicResult<string>
            {
                IsSuccess = true,
                Message = "Row duplicated successfully",
                Data = newRowId ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DuplicateRow failed in Rows module for rowId {RowId}", rowId);
            throw;
        }
    }

    /// <summary>
    /// Gets the unique row ID for a row at the specified index.
    /// USE CASE: User clicks on row in UI, UI event provides RowIndex, need to convert to stable RowID.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index</param>
    /// <returns>The unique row ID (from __rowId field) or null if not found</returns>
    public string? GetRowIdByIndex(int rowIndex)
    {
        try
        {
            var rowData = _rowStore.GetRow(rowIndex);
            if (rowData != null && rowData.TryGetValue("__rowId", out var rowIdValue))
            {
                return rowIdValue?.ToString();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetRowIdByIndex failed for row {RowIndex}", rowIndex);
            return null;
        }
    }

    /// <summary>
    /// Gets the current row index for a row with the specified ID.
    /// USE CASE: Have RowID from database, want to scroll/highlight row in UI.
    /// </summary>
    /// <param name="rowId">Unique row identifier</param>
    /// <returns>Current zero-based row index or null if not found</returns>
    public int? GetRowIndexById(string rowId)
    {
        try
        {
            // TODO: Implement efficient lookup when IRowStore supports RowID operations
            // For now, linear search through all rows
            var allRows = _rowStore.GetAllRows();
            for (int i = 0; i < allRows.Count; i++)
            {
                if (allRows[i].TryGetValue("__rowId", out var rowIdValue))
                {
                    if (rowIdValue?.ToString() == rowId)
                    {
                        return i;
                    }
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetRowIndexById failed for RowID {RowId}", rowId);
            return null;
        }
    }

    /// <summary>
    /// Gets the row ID of the currently selected row (if single selection).
    /// USE CASE: Shortcut to avoid manual conversion in single-select scenarios.
    /// </summary>
    /// <returns>Row ID or null if no row selected</returns>
    public string? GetSelectedRowId()
    {
        try
        {
            // TODO: Implement when selection tracking is available
            _logger?.LogWarning("GetSelectedRowId() not yet implemented - requires selection tracking");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetSelectedRowId failed");
            return null;
        }
    }

    /// <summary>
    /// Gets the row IDs of all currently selected rows.
    /// USE CASE: Shortcut for multi-select delete/update operations.
    /// </summary>
    /// <returns>Array of row IDs (empty array if no selection)</returns>
    public string[] GetSelectedRowIds()
    {
        try
        {
            // TODO: Implement when selection tracking is available
            _logger?.LogWarning("GetSelectedRowIds() not yet implemented - requires selection tracking");
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetSelectedRowIds failed");
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Validate row data without adding to grid.
    /// Uses ValidationService to run all configured validation rules.
    /// </summary>
    public async Task<PublicValidationResult> ValidateRowDataAsync(
        IReadOnlyDictionary<string, object?> rowData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Validating row data with {ColumnCount} columns", rowData.Count);

            // If no validation service configured, treat as valid
            if (_validationService == null)
            {
                _logger?.LogDebug("No validation service configured - treating row as valid");
                return PublicValidationResult.Success();
            }

            // Create validation context (use public ValidationContext)
            var context = new ValidationContext
            {
                RowIndex = -1, // Not in grid yet
                OperationId = Guid.NewGuid().ToString()
            };

            // Validate row using ValidationService
            var validationResult = await _validationService.ValidateRowAsync(rowData, context, cancellationToken);

            if (validationResult.IsValid)
            {
                _logger?.LogDebug("Row validation successful");
                return PublicValidationResult.Success();
            }

            // Convert validation result to public format
            var publicErrors = new List<PublicCellValidationError>();

            // ValidationResult has a single error message
            if (!string.IsNullOrEmpty(validationResult.ErrorMessage))
            {
                publicErrors.Add(new PublicCellValidationError
                {
                    ColumnName = validationResult.AffectedColumn ?? "Unknown",
                    ErrorMessage = validationResult.ErrorMessage,
                    Severity = validationResult.Severity // Already PublicValidationSeverity
                });
            }

            _logger?.LogDebug("Row validation failed with {ErrorCount} errors", publicErrors.Count);

            return PublicValidationResult.Failure(publicErrors);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Validation failed with exception");
            return PublicValidationResult.Failure(new[]
            {
                new PublicCellValidationError
                {
                    ColumnName = "Unknown",
                    ErrorMessage = $"Validation error: {ex.Message}",
                    Severity = PublicValidationSeverity.Error
                }
            });
        }
    }

    /// <summary>
    /// ✅ PROFESSIONAL SOLUTION: Physically inserts empty row at position (FIXED UI POOL).
    /// ARCHITECTURE:
    /// - Uses IRowStore.InsertRowAtIndexAsync() to PHYSICALLY add row (RowCount++)
    /// - PageManager.TotalDataRows updated → TotalPages recalculated
    /// - UI: FIXED pool of PageSize ViewModels (always 15, never changes)
    ///   - UpdateViewModelsInPlace() updates existing ViewModels IN-PLACE
    ///   - Empty rows (pool padding) become VISIBLE when data count increases
    ///   - NO new UI objects created (pool size constant)
    /// PARAMETERS:
    /// - referenceRowId: RowId of reference row (STABLE identifier, not RowIndex!)
    /// EXAMPLE: PageSize=15, Page 7 has 10 visible rows
    ///   - UI pool: 10 visible + 5 invisible (total 15)
    ///   - User clicks INSERT after row 9 (RowId="R99")
    ///   - InsertRowAtIndexAsync(98) → RowCount 100→101
    ///   - UpdateViewModelsInPlace(): 11 visible + 4 invisible (total 15)
    ///   - Result: Row 10 became VISIBLE (was invisible), NO new UI objects!
    /// </summary>
    public async Task<PublicResult> VirtualInsertEmptyRowAfterAsync(string referenceRowId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("PHYSICAL INSERT (FIXED UI POOL): Starting for rowId {RowId}", referenceRowId);

            // 1. Find reference row index BY RowId (STABLE identifier)
            var referenceIndex = _rowStore.GetRowIndexById(referenceRowId);
            if (referenceIndex == null)
            {
                return PublicResult.Failure($"Reference row {referenceRowId} not found");
            }

            var insertAtIndex = referenceIndex.Value + 1;

            // ✅ DEADLOCK FIX: Use async methods instead of sync (avoid .GetAwaiter().GetResult())
            var oldRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);

            // 2. Get template for empty row (all columns = null)
            // ✅ DEADLOCK FIX: Use GetRowAsync instead of GetRow
            var firstRow = await _rowStore.GetRowAsync(0, cancellationToken);
            if (firstRow == null)
            {
                return PublicResult.Failure("Cannot determine column structure - no rows exist");
            }

            var emptyRowData = new Dictionary<string, object?>();
            foreach (var colName in firstRow.Keys)
            {
                if (colName == "__rowId" || colName == "__createdAt")
                    continue;  // System columns added by InsertRowAtIndexAsync
                emptyRowData[colName] = null;
            }

            // ✅ STEP 3: PHYSICALLY INSERT row at position (RowCount++)
            // IRowStore will:
            // - Generate new RowId (STABLE identifier)
            // - Calculate ULID timestamp for correct sort order
            // - Add row to dictionary
            // - Return RowId (for logging/debugging)
            var newRowId = await _rowStore.InsertRowAtIndexAsync(insertAtIndex, emptyRowData, cancellationToken);

            // ✅ DEADLOCK FIX: Use async method instead of sync
            var newRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);

            _logger?.LogInformation("PHYSICAL INSERT: Added empty row at index {Index} (RowId={RowId}), RowCount: {OldCount}→{NewCount}",
                insertAtIndex, newRowId, oldRowCount, newRowCount);

            // ✅ STEP 4: Trigger UI refresh (calls UpdateViewModelsInPlace)
            // UI BEHAVIOR (FIXED POOL):
            // - GetRowsRangeAsync() returns new data count (e.g., 10→11 rows)
            // - UpdateViewModelsInPlace() updates EXISTING ViewModels (pool size=15, UNCHANGED)
            // - IF page had invisible rows (e.g., 10 visible + 5 invisible):
            //   → One invisible row becomes VISIBLE (11 visible + 4 invisible)
            //   → RowId updated from null → newRowId
            //   → IsVisible updated from false → true
            //   → NO new UI objects created!
            // - IF page was full (15 visible):
            //   → Last row shifts to next page
            //   → Still 15 visible (data updated IN-PLACE)
            //   → NO new UI objects created!
            // NOTE: PageManager.TotalDataRows will be auto-updated in UiNotificationService.NotifyDataRefreshAsync
            await TriggerUIRefreshIfNeededAsync("VirtualInsert", 1);

            _logger?.LogInformation("PHYSICAL INSERT (FIXED UI POOL): Completed - dataset grew by 1 row");

            return new PublicResult
            {
                IsSuccess = true,
                Message = $"Empty row inserted at position {insertAtIndex}, dataset now has {newRowCount} rows (RowId={newRowId})"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "PHYSICAL INSERT failed for rowId {RowId}", referenceRowId);
            throw;
        }
    }

    /// <summary>
    /// ✅ PROFESSIONAL SOLUTION: Virtuálne vloží prázdny riadok PRED zadaný riadok (FIXED UI POOL).
    /// ARCHITECTURE:
    /// - Shifts all rows with __rowNumber >= target down by 1 (__rowNumber++)
    /// - Creates new empty row at target's original __rowNumber position
    /// - PHYSICALLY increases RowCount by 1 (real insert operation)
    /// - UI: FIXED pool of PageSize ViewModels (always 15, never changes)
    ///   - UpdateViewModelsInPlace() updates existing ViewModels IN-PLACE
    ///   - Invisible rows become VISIBLE when data count increases
    ///   - NO new UI objects created (pool size constant)
    /// PARAMETERS:
    /// - referenceRowId: RowId BEFORE which to insert (this row will be shifted down)
    /// EXAMPLE: PageSize=15, Page 1 has 15 visible rows
    ///   - User right-clicks row 5 (RowId="R5") → "Insert 2 rows above"
    ///   - VirtualInsertEmptyRowBeforeAsync("R5") called 2 times
    ///   - First call: Row 5-15 shift to 6-16, new empty row inserted at position 5
    ///   - Second call: Row 5-16 shift to 6-17, new empty row inserted at position 5
    ///   - Result: 2 empty rows at positions 5-6, original row 5 is now row 7
    /// </summary>
    public async Task<PublicResult> VirtualInsertEmptyRowBeforeAsync(string referenceRowId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("PHYSICAL INSERT BEFORE (FIXED UI POOL): Starting for rowId {RowId}", referenceRowId);

            // 1. Find reference row index BY RowId (STABLE identifier)
            var referenceIndex = _rowStore.GetRowIndexById(referenceRowId);
            if (referenceIndex == null)
            {
                return PublicResult.Failure($"Reference row {referenceRowId} not found");
            }

            var insertAtIndex = referenceIndex.Value; // Insert BEFORE reference = insert AT reference index

            // ✅ DEADLOCK FIX: Use async methods instead of sync
            var oldRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);

            // 2. Get template for empty row (all columns = null)
            var firstRow = await _rowStore.GetRowAsync(0, cancellationToken);
            if (firstRow == null)
            {
                return PublicResult.Failure("Cannot determine column structure - no rows exist");
            }

            var emptyRowData = new Dictionary<string, object?>();
            foreach (var colName in firstRow.Keys)
            {
                if (colName == "__rowId" || colName == "__createdAt")
                    continue;  // System columns added by InsertRowAtIndexAsync
                emptyRowData[colName] = null;
            }

            // ✅ STEP 3: PHYSICALLY INSERT row at position BEFORE reference (RowCount++)
            // IRowStore.InsertRowAtIndexAsync will:
            // - Shift all rows at insertAtIndex and after DOWN by 1
            // - Generate new RowId (STABLE identifier)
            // - Calculate ULID timestamp for correct sort order
            // - Add row to dictionary
            // - Return RowId (for logging/debugging)
            var newRowId = await _rowStore.InsertRowAtIndexAsync(insertAtIndex, emptyRowData, cancellationToken);

            // ✅ DEADLOCK FIX: Use async method instead of sync
            var newRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);

            _logger?.LogInformation("PHYSICAL INSERT BEFORE: Added empty row at index {Index} (RowId={RowId}), reference row {RefRowId} shifted down, RowCount: {OldCount}→{NewCount}",
                insertAtIndex, newRowId, referenceRowId, oldRowCount, newRowCount);

            // ✅ STEP 4: Trigger UI refresh (calls UpdateViewModelsInPlace)
            // UI BEHAVIOR (FIXED POOL):
            // - GetRowsRangeAsync() returns new data count (e.g., 10→11 rows)
            // - UpdateViewModelsInPlace() updates EXISTING ViewModels (pool size=15, UNCHANGED)
            // - IF page had invisible rows (e.g., 10 visible + 5 invisible):
            //   → One invisible row becomes VISIBLE (11 visible + 4 invisible)
            //   → NO new UI objects created!
            // - IF page was full (15 visible):
            //   → Last row shifts to next page
            //   → Still 15 visible (data updated IN-PLACE)
            // NOTE: PageManager.TotalDataRows will be auto-updated in UiNotificationService.NotifyDataRefreshAsync
            await TriggerUIRefreshIfNeededAsync("VirtualInsertBefore", 1);

            _logger?.LogInformation("PHYSICAL INSERT BEFORE (FIXED UI POOL): Completed - dataset grew by 1 row");

            return new PublicResult
            {
                IsSuccess = true,
                Message = $"Empty row inserted BEFORE position {insertAtIndex}, dataset now has {newRowCount} rows (RowId={newRowId})"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "PHYSICAL INSERT BEFORE failed for rowId {RowId}", referenceRowId);
            throw;
        }
    }

    /// <summary>
    /// ✅ PROFESSIONAL SOLUTION: Physically deletes row (FIXED UI POOL).
    /// ARCHITECTURE:
    /// - Uses IRowStore.DeleteRowByIdAsync() to PHYSICALLY remove row (RowCount--)
    /// - PageManager.TotalDataRows updated → TotalPages recalculated
    /// - UI: FIXED pool of PageSize ViewModels (always 15, never changes)
    ///   - UpdateViewModelsInPlace() updates existing ViewModels IN-PLACE
    ///   - Visible rows become INVISIBLE when data count decreases
    ///   - NO UI objects removed (pool size constant)
    /// PARAMETERS:
    /// - rowId: RowId to delete (STABLE identifier, not RowIndex!)
    /// EXAMPLE: PageSize=15, Page 7 has 10 visible rows
    ///   - UI pool: 10 visible + 5 invisible (total 15)
    ///   - User clicks DELETE on row 5 (RowId="R95")
    ///   - DeleteRowByIdAsync("R95") → RowCount 100→99
    ///   - UpdateViewModelsInPlace(): 9 visible + 6 invisible (total 15)
    ///   - Result: Row 9 became INVISIBLE (was visible), NO UI objects removed!
    /// </summary>
    public async Task<PublicResult> VirtualDeleteRowAsync(string rowId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("PHYSICAL DELETE (FIXED UI POOL): Starting for rowId {RowId}", rowId);

            // 1. Find row index BY RowId (for logging only - DeleteRowByIdAsync uses RowId directly)
            var deleteIndex = _rowStore.GetRowIndexById(rowId);
            if (deleteIndex == null)
            {
                return PublicResult.Failure($"Row {rowId} not found");
            }

            // ✅ DEADLOCK FIX: Use async method instead of sync
            var oldRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);

            // ✅ STEP 2: PHYSICALLY DELETE row BY RowId (RowCount--)
            // CRITICAL: Uses RowId (STABLE), not RowIndex (unstable)!
            // IRowStore will:
            // - Remove row from dictionary
            // - Rows after deleted row automatically SHIFT UP via sort order
            // - Clear caches
            await _rowStore.DeleteRowByIdAsync(rowId, cancellationToken);

            // ✅ DEADLOCK FIX: Use async method instead of sync
            var newRowCount = (int)await _rowStore.GetRowCountAsync(cancellationToken);

            _logger?.LogInformation("PHYSICAL DELETE: Deleted row at index {Index} (RowId={RowId}), RowCount: {OldCount}→{NewCount}",
                deleteIndex.Value, rowId, oldRowCount, newRowCount);

            // ✅ STEP 3: Trigger UI refresh (calls UpdateViewModelsInPlace)
            // UI BEHAVIOR (FIXED POOL):
            // - GetRowsRangeAsync() returns new data count (e.g., 10→9 rows)
            // - UpdateViewModelsInPlace() updates EXISTING ViewModels (pool size=15, UNCHANGED)
            // - IF page has visible rows (e.g., 10 visible + 5 invisible):
            //   → One visible row becomes INVISIBLE (9 visible + 6 invisible)
            //   → RowId updated from oldRowId → null
            //   → IsVisible updated from true → false
            //   → NO UI objects removed!
            // - IF page refills from next page (rows shift UP):
            //   → Still 15 visible (data updated IN-PLACE)
            //   → NO UI objects removed!
            // NOTE: PageManager.TotalDataRows will be auto-updated in UiNotificationService.NotifyDataRefreshAsync
            await TriggerUIRefreshIfNeededAsync("VirtualDelete", 1);

            _logger?.LogInformation("PHYSICAL DELETE (FIXED UI POOL): Completed - dataset shrunk by 1 row");

            return new PublicResult
            {
                IsSuccess = true,
                Message = $"Row deleted at position {deleteIndex.Value} (RowId={rowId}), dataset now has {newRowCount} rows"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "PHYSICAL DELETE failed for rowId {RowId}", rowId);
            throw;
        }
    }

    /// <summary>
    /// Triggers automatic UI refresh ONLY in Interactive mode
    /// </summary>
    private async Task TriggerUIRefreshIfNeededAsync(string operationType, int affectedRows)
    {
        // Automatický refresh LEN v Interactive mode
        if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
        {
            await _uiNotificationService.NotifyDataRefreshAsync(affectedRows, operationType);
        }
        // V Readonly/Headless mode → skip (automatický refresh je zakázaný)
    }

    /// <summary>
    /// Validates that row data does not contain reserved column names.
    /// CRITICAL: __rowId is reserved for internal use and cannot be used as a user column.
    /// DEFENSIVE: Handles null rowData gracefully (null is valid - represents empty row).
    /// </summary>
    private void ValidateNoReservedColumnNames(IReadOnlyDictionary<string, object?>? rowData)
    {
        // ✅ DEFENSIVE PROGRAMMING: Check for null first
        // Null row data is valid (e.g., AddRowAsync(null) creates empty row)
        if (rowData == null)
        {
            return;
        }

        // ✅ Now safe to call ContainsKey
        if (rowData.ContainsKey("__rowId"))
        {
            throw new ArgumentException(
                "Column name '__rowId' is reserved for internal use and cannot be used as a data column. " +
                "Please use a different column name (e.g., 'rowId', 'RowId', 'row_id').",
                nameof(rowData));
        }
    }
}

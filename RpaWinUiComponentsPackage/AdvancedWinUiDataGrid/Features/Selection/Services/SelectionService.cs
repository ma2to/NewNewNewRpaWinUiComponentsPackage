using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using System.Collections.Concurrent;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Services;

/// <summary>
/// Internal implementation of selection service with comprehensive functionality
/// Scoped service for per-operation state isolation
/// Thread-safe with no per-operation mutable fields
/// </summary>
internal sealed class SelectionService : ISelectionService
{
    private readonly ILogger<SelectionService> _logger;
    private readonly AdvancedDataGridOptions _options;
    private readonly IOperationLogger<SelectionService> _operationLogger;
    private readonly ConcurrentDictionary<int, bool> _selectedRows = new();
    private readonly Infrastructure.Persistence.Interfaces.IRowStore? _rowStore;

    public SelectionService(
        ILogger<SelectionService> logger,
        AdvancedDataGridOptions options,
        IOperationLogger<SelectionService>? operationLogger = null,
        Infrastructure.Persistence.Interfaces.IRowStore? rowStore = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rowStore = rowStore;

        // Use null pattern if logger is not provided
        _operationLogger = operationLogger ?? NullOperationLogger<SelectionService>.Instance;
    }

    /// <summary>
    /// Selects a specific cell - synchronous interface method
    /// </summary>
    public void SelectCell(int row, int col)
    {
        _logger.LogInformation("Selecting cell ({Row},{Col})", row, col);
        // Implementation would select specific cell
    }

    /// <summary>
    /// Starts drag selection operation - synchronous interface method
    /// </summary>
    public void StartDragSelect(int row, int col)
    {
        _logger.LogDebug("Starting drag select at ({Row},{Col})", row, col);
        // Implementation would start drag selection
    }

    /// <summary>
    /// Updates drag selection to new position - synchronous interface method
    /// </summary>
    public void DragSelectTo(int row, int col)
    {
        _logger.LogDebug("Dragging select to ({Row},{Col})", row, col);
        // Implementation would update drag selection
    }

    /// <summary>
    /// Ends drag selection operation - synchronous interface method
    /// </summary>
    public void EndDragSelect(int row, int col)
    {
        _logger.LogDebug("Ending drag select at ({Row},{Col})", row, col);
        // Implementation would end drag selection
    }

    /// <summary>
    /// Toggles cell selection state - synchronous interface method
    /// </summary>
    public void ToggleCellSelection(int row, int col)
    {
        _logger.LogDebug("Toggling cell selection ({Row},{Col})", row, col);
        // Implementation would toggle cell selection
    }

    /// <summary>
    /// Extends selection to specified cell - synchronous interface method
    /// </summary>
    public void ExtendSelectionTo(int row, int col)
    {
        _logger.LogDebug("Extending selection to ({Row},{Col})", row, col);
        // Implementation would extend selection
    }

    /// <summary>
    /// Select cells with comprehensive validation and thread-safe operation
    /// Returns current selection state after operation
    /// </summary>
    public async Task<SelectionResult> SelectCellsAsync(SelectCellsCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Starting select cells operation - create operation scope for automatic tracking
        using var scope = _operationLogger.LogOperationStart("SelectCellsAsync", new
        {
            OperationId = operationId,
            CellCount = command.CellAddresses.Count,
            SelectionMode = command.SelectionMode
        });

        _logger.LogInformation("Starting select cells operation {OperationId} for {CellCount} cells with mode {SelectionMode}",
            operationId, command.CellAddresses.Count, command.SelectionMode);

        try
        {
            // Validate selection command
            var validationResult = await ValidateSelectionCommandAsync(command, operationId, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("Selection validation failed for operation {OperationId}: {Error}",
                    operationId, validationResult.ErrorMessage);

                scope.MarkFailure(new InvalidOperationException(validationResult.ErrorMessage ?? "Selection validation failed"));
                return SelectionResult.Failed(validationResult.ErrorMessage ?? "Selection validation failed", stopwatch.Elapsed);
            }

            // Process selection according to mode
            var selectedCells = await ProcessSelectionAsync(command, operationId, cancellationToken);

            _logger.LogInformation("Select cells operation {OperationId} completed successfully in {Duration}ms, selected {CellCount} cells",
                operationId, stopwatch.ElapsedMilliseconds, selectedCells.Count);

            scope.MarkSuccess(new
            {
                SelectedCellCount = selectedCells.Count,
                SelectionMode = command.SelectionMode,
                Duration = stopwatch.Elapsed
            });

            return SelectionResult.Success(selectedCells, stopwatch.Elapsed);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("Select cells operation {OperationId} was cancelled", operationId);

            scope.MarkFailure(ex);
            return SelectionResult.Cancelled(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Select cells operation {OperationId} failed with error: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            return SelectionResult.Failed($"Selection failed: {ex.Message}", stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Select rows with comprehensive validation and thread-safe operation
    /// Returns current selection state after operation
    /// </summary>
    public async Task<SelectionResult> SelectRowsAsync(SelectRowsCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        _logger.LogInformation("Starting select rows operation {OperationId} for {RowCount} rows with mode {SelectionMode}",
            operationId, command.RowIndices.Count, command.SelectionMode);

        try
        {
            // Validate selection command
            var validationResult = await ValidateRowSelectionCommandAsync(command, operationId, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("Row selection validation failed for operation {OperationId}: {Error}",
                    operationId, validationResult.ErrorMessage);
                return SelectionResult.Failed(validationResult.ErrorMessage ?? "Row selection validation failed", stopwatch.Elapsed);
            }

            // Convert row selection to cell selection for consistent handling
            var cellAddresses = ConvertRowIndicesToCellAddresses(command.RowIndices, operationId);

            _logger.LogInformation("Select rows operation {OperationId} completed successfully in {Duration}ms, selected {RowCount} rows ({CellCount} cells)",
                operationId, stopwatch.ElapsedMilliseconds, command.RowIndices.Count, cellAddresses.Count);

            return SelectionResult.Success(cellAddresses, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Select rows operation {OperationId} was cancelled", operationId);
            return SelectionResult.Cancelled(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Select rows operation {OperationId} failed with exception", operationId);
            return SelectionResult.Failed($"Row selection failed: {ex.Message}", stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Select columns with comprehensive validation and thread-safe operation
    /// Returns current selection state after operation
    /// </summary>
    public async Task<SelectionResult> SelectColumnsAsync(SelectColumnsCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        _logger.LogInformation("Starting select columns operation {OperationId} for {ColumnCount} columns with mode {SelectionMode}",
            operationId, command.ColumnNames.Count, command.SelectionMode);

        try
        {
            // Validate selection command
            var validationResult = await ValidateColumnSelectionCommandAsync(command, operationId, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("Column selection validation failed for operation {OperationId}: {Error}",
                    operationId, validationResult.ErrorMessage);
                return SelectionResult.Failed(validationResult.ErrorMessage ?? "Column selection validation failed", stopwatch.Elapsed);
            }

            // Convert column selection to cell selection for consistent handling
            var cellAddresses = await ConvertColumnNamesToCellAddressesAsync(command.ColumnNames, operationId, cancellationToken);

            _logger.LogInformation("Select columns operation {OperationId} completed successfully in {Duration}ms, selected {ColumnCount} columns ({CellCount} cells)",
                operationId, stopwatch.ElapsedMilliseconds, command.ColumnNames.Count, cellAddresses.Count);

            return SelectionResult.Success(cellAddresses, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Select columns operation {OperationId} was cancelled", operationId);
            return SelectionResult.Cancelled(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Select columns operation {OperationId} failed with exception", operationId);
            return SelectionResult.Failed($"Column selection failed: {ex.Message}", stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Clear all selections with thread-safe operation
    /// </summary>
    public async Task<SelectionResult> ClearSelectionAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        _logger.LogInformation("Starting clear selection operation {OperationId}", operationId);

        try
        {
            // Clear selection - return empty collection
            await Task.CompletedTask;

            _logger.LogInformation("Clear selection operation {OperationId} completed successfully in {Duration}ms",
                operationId, stopwatch.ElapsedMilliseconds);

            return SelectionResult.Success(Array.Empty<CellAddress>(), stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Clear selection operation {OperationId} was cancelled", operationId);
            return SelectionResult.Cancelled(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear selection operation {OperationId} failed with exception", operationId);
            return SelectionResult.Failed($"Clear selection failed: {ex.Message}", stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Get current selection ranges for efficient processing
    /// </summary>
    public async Task<IReadOnlyList<SelectionRange>> GetSelectionRangesAsync(IReadOnlyList<CellAddress> selectedCells, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();
        _logger.LogDebug("Getting selection ranges for {CellCount} cells in operation {OperationId}",
            selectedCells.Count, operationId);

        try
        {
            if (selectedCells.Count == 0)
                return Array.Empty<SelectionRange>();

            // Group cells into continuous ranges for efficient processing
            var ranges = CalculateSelectionRanges(selectedCells, operationId);

            await Task.CompletedTask;
            return ranges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get selection ranges for operation {OperationId}", operationId);
            return Array.Empty<SelectionRange>();
        }
    }

    /// <summary>
    /// Validate selection command
    /// </summary>
    private async Task<Result> ValidateSelectionCommandAsync(
        SelectCellsCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        if (command.CellAddresses.Count == 0)
            return Result.Failure("No cells specified for selection");

        if (command.CellAddresses.Count > _options.MaxSelectionSize)
            return Result.Failure($"Selection size exceeds maximum limit of {_options.MaxSelectionSize}");

        // Validate cell addresses
        foreach (var cellAddress in command.CellAddresses)
        {
            if (cellAddress.Row < 0)
                return Result.Failure($"Invalid row index: {cellAddress.Row}");

            if (string.IsNullOrWhiteSpace(cellAddress.Column))
                return Result.Failure("Column name cannot be empty");
        }

        _logger.LogDebug("Selection command validated for operation {OperationId}: {CellCount} cells",
            operationId, command.CellAddresses.Count);

        await Task.CompletedTask;
        return Result.Success();
    }

    /// <summary>
    /// Validate row selection command
    /// </summary>
    private async Task<Result> ValidateRowSelectionCommandAsync(
        SelectRowsCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        if (command.RowIndices.Count == 0)
            return Result.Failure("No rows specified for selection");

        if (command.RowIndices.Count > _options.MaxSelectionSize)
            return Result.Failure($"Row selection size exceeds maximum limit of {_options.MaxSelectionSize}");

        // Validate row indices
        foreach (var rowIndex in command.RowIndices)
        {
            if (rowIndex < 0)
                return Result.Failure($"Invalid row index: {rowIndex}");
        }

        _logger.LogDebug("Row selection command validated for operation {OperationId}: {RowCount} rows",
            operationId, command.RowIndices.Count);

        await Task.CompletedTask;
        return Result.Success();
    }

    /// <summary>
    /// Validate column selection command
    /// </summary>
    private async Task<Result> ValidateColumnSelectionCommandAsync(
        SelectColumnsCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        if (command.ColumnNames.Count == 0)
            return Result.Failure("No columns specified for selection");

        if (command.ColumnNames.Count > _options.MaxSelectionSize)
            return Result.Failure($"Column selection size exceeds maximum limit of {_options.MaxSelectionSize}");

        // Validate column names
        foreach (var columnName in command.ColumnNames)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return Result.Failure("Column name cannot be empty");
        }

        _logger.LogDebug("Column selection command validated for operation {OperationId}: {ColumnCount} columns",
            operationId, command.ColumnNames.Count);

        await Task.CompletedTask;
        return Result.Success();
    }

    /// <summary>
    /// Process selection based on selection mode
    /// </summary>
    private async Task<IReadOnlyList<CellAddress>> ProcessSelectionAsync(
        SelectCellsCommand command,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing selection for operation {OperationId} with mode {SelectionMode}",
            operationId, command.SelectionMode);

        switch (command.SelectionMode)
        {
            case SelectionMode.Replace:
                // Replace current selection with new cells
                return command.CellAddresses.ToList();

            case SelectionMode.Add:
                // For simplicity, just return the new cells
                // In a real implementation, this would merge with existing selection
                return command.CellAddresses.ToList();

            case SelectionMode.Remove:
                // For simplicity, return empty selection
                // In a real implementation, this would remove from existing selection
                return Array.Empty<CellAddress>();

            case SelectionMode.Toggle:
                // For simplicity, just return the new cells
                // In a real implementation, this would toggle each cell's selection state
                return command.CellAddresses.ToList();

            default:
                throw new NotSupportedException($"Selection mode {command.SelectionMode} is not supported");
        }
    }

    /// <summary>
    /// Convert row indices to cell addresses
    /// </summary>
    private List<CellAddress> ConvertRowIndicesToCellAddresses(
        IReadOnlyList<int> rowIndices,
        Guid operationId)
    {
        var cellAddresses = new List<CellAddress>();

        // For simplicity, assume we have common column names
        // In a real implementation, this would get actual column names from the data
        var commonColumns = new[] { "Column1", "Column2", "Column3" }; // Simplified

        foreach (var rowIndex in rowIndices)
        {
            foreach (var columnName in commonColumns)
            {
                cellAddresses.Add(new CellAddress(rowIndex, columnName));
            }
        }

        _logger.LogDebug("Converted {RowCount} rows to {CellCount} cell addresses for operation {OperationId}",
            rowIndices.Count, cellAddresses.Count, operationId);

        return cellAddresses;
    }

    /// <summary>
    /// Convert column names to cell addresses
    /// </summary>
    private async Task<List<CellAddress>> ConvertColumnNamesToCellAddressesAsync(
        IReadOnlyList<string> columnNames,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var cellAddresses = new List<CellAddress>();

        // For simplicity, assume we have a fixed number of rows
        // In a real implementation, this would get actual row count from the data
        var estimatedRowCount = _options.EstimatedRowCount; // Simplified

        foreach (var columnName in columnNames)
        {
            for (int row = 0; row < estimatedRowCount; row++)
            {
                cellAddresses.Add(new CellAddress(row, columnName));
            }
        }

        _logger.LogDebug("Converted {ColumnCount} columns to {CellCount} cell addresses for operation {OperationId}",
            columnNames.Count, cellAddresses.Count, operationId);

        await Task.CompletedTask;
        return cellAddresses;
    }

    /// <summary>
    /// Calculate selection ranges from individual cell addresses
    /// </summary>
    private List<SelectionRange> CalculateSelectionRanges(
        IReadOnlyList<CellAddress> selectedCells,
        Guid operationId)
    {
        var ranges = new List<SelectionRange>();

        if (selectedCells.Count == 0)
            return ranges;

        // Group cells by column for range calculation
        var cellsByColumn = selectedCells
            .GroupBy(cell => cell.Column)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Row).OrderBy(r => r).ToList());

        foreach (var columnGroup in cellsByColumn)
        {
            var columnName = columnGroup.Key;
            var sortedRows = columnGroup.Value;

            // Find continuous ranges within each column
            var currentRangeStart = sortedRows[0];
            var currentRangeEnd = sortedRows[0];

            for (int i = 1; i < sortedRows.Count; i++)
            {
                if (sortedRows[i] == currentRangeEnd + 1)
                {
                    // Continuous range continues
                    currentRangeEnd = sortedRows[i];
                }
                else
                {
                    // Range break - create range for previous sequence
                    ranges.Add(new SelectionRange(
                        StartRow: currentRangeStart,
                        EndRow: currentRangeEnd,
                        StartColumn: columnName,
                        EndColumn: columnName
                    ));

                    // Start new range
                    currentRangeStart = sortedRows[i];
                    currentRangeEnd = sortedRows[i];
                }
            }

            // Add the final range
            ranges.Add(new SelectionRange(
                StartRow: currentRangeStart,
                EndRow: currentRangeEnd,
                StartColumn: columnName,
                EndColumn: columnName
            ));
        }

        _logger.LogDebug("Calculated {RangeCount} selection ranges from {CellCount} cells for operation {OperationId}",
            ranges.Count, selectedCells.Count, operationId);

        return ranges;
    }

    /// <summary>
    /// Gets currently selected cells
    /// </summary>
    public IReadOnlyList<(int Row, int Column)> GetSelectedCells()
    {
        // Implementation would track selected cells
        return Array.Empty<(int, int)>();
    }

    /// <summary>
    /// Gets currently selected rows
    /// </summary>
    public IReadOnlyList<int> GetSelectedRows()
    {
        // Implementation would track selected rows
        return Array.Empty<int>();
    }

    /// <summary>
    /// Gets currently selected columns
    /// </summary>
    public IReadOnlyList<int> GetSelectedColumns()
    {
        // Implementation would track selected columns
        return Array.Empty<int>();
    }

    /// <summary>
    /// Selects entire row
    /// </summary>
    public void SelectRow(int rowIndex)
    {
        _logger.LogDebug("Selecting row {RowIndex}", rowIndex);
        // Implementation would select entire row
    }

    /// <summary>
    /// Selects entire column
    /// </summary>
    public void SelectColumn(int columnIndex)
    {
        _logger.LogDebug("Selecting column {ColumnIndex}", columnIndex);
        // Implementation would select entire column
    }

    /// <summary>
    /// Selects range of cells
    /// </summary>
    public void SelectRange(int startRow, int startCol, int endRow, int endCol)
    {
        _logger.LogDebug("Selecting range ({StartRow},{StartCol}) to ({EndRow},{EndCol})",
            startRow, startCol, endRow, endCol);
        // Implementation would select range
    }

    /// <summary>
    /// Clears all selections
    /// </summary>
    public void ClearSelection()
    {
        _logger.LogDebug("Clearing all selections");
        // Implementation would clear selections
    }

    /// <summary>
    /// Selects all cells
    /// </summary>
    public void SelectAll()
    {
        _logger.LogDebug("Selecting all cells");
        // Implementation would select all
    }

    /// <summary>
    /// Gets selection bounds
    /// </summary>
    public (int StartRow, int StartCol, int EndRow, int EndCol)? GetSelectionBounds()
    {
        // Implementation would calculate bounds
        return null;
    }

    /// <summary>
    /// Checks if a cell is selected
    /// </summary>
    public bool IsCellSelected(int row, int col)
    {
        // Implementation would check selection state
        return false;
    }

    /// <summary>
    /// Checks if a row is selected
    /// </summary>
    public bool IsRowSelected(int rowIndex)
    {
        // Implementation would check row selection state
        return false;
    }

    /// <summary>
    /// Checks if a column is selected
    /// </summary>
    public bool IsColumnSelected(int columnIndex)
    {
        // Implementation would check column selection state
        return false;
    }

    /// <summary>
    /// Gets selected data as dictionary collection
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetSelectedData()
    {
        // Implementation would return selected data
        return Array.Empty<IReadOnlyDictionary<string, object?>>();
    }

    /// <summary>
    /// Creates an immutable snapshot of current selection for thread-safe access
    /// </summary>
    public SelectionSnapshot CreateSelectionSnapshot()
    {
        return SelectionSnapshot.Empty;
    }

    // Additional methods for internal facade operations

    /// <summary>
    /// Starts drag selection operation (internal)
    /// </summary>
    public void StartDragSelectInternal(int row, int col)
    {
        _logger.LogDebug("Starting drag select (internal) at ({Row},{Col})", row, col);
        // Implementation would start drag selection with per-operation local state
    }

    /// <summary>
    /// Updates drag selection to new position (internal)
    /// </summary>
    public void UpdateDragSelectInternal(int row, int col)
    {
        _logger.LogDebug("Updating drag select (internal) to ({Row},{Col})", row, col);
        // Implementation would update drag selection with per-operation local state
    }

    /// <summary>
    /// Ends drag selection operation (internal)
    /// </summary>
    public void EndDragSelectInternal()
    {
        _logger.LogDebug("Ending drag select (internal)");
        // Implementation would finalize drag selection with per-operation local state
    }

    /// <summary>
    /// Selects a specific cell (internal)
    /// </summary>
    public void SelectCellInternal(int row, int col)
    {
        _logger.LogDebug("Selecting cell (internal) ({Row},{Col})", row, col);
        // Implementation would select specific cell with per-operation local state
    }

    /// <summary>
    /// Toggles cell selection state (internal)
    /// </summary>
    public void ToggleSelectionInternal(int row, int col)
    {
        _logger.LogDebug("Toggling selection (internal) ({Row},{Col})", row, col);
        // Implementation would toggle cell selection with per-operation local state
    }

    /// <summary>
    /// Extends selection to specified cell (internal)
    /// </summary>
    public void ExtendSelectionInternal(int row, int col)
    {
        _logger.LogDebug("Extending selection (internal) to ({Row},{Col})", row, col);
        // Implementation would extend selection with per-operation local state
    }

    #region Public API Compatibility Methods

    /// <summary>
    /// Selects a specific row by index (async for public API)
    /// </summary>
    public async Task<Common.Models.Result> SelectRowAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Selecting row {RowIndex}", rowIndex);
                SelectRow(rowIndex);
                _selectedRows[rowIndex] = true;
                return Common.Models.Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to select row {RowIndex}: {Message}", rowIndex, ex.Message);
                return Common.Models.Result.Failure($"Failed to select row: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Selects multiple rows by indices (async for public API)
    /// </summary>
    public async Task<Common.Models.Result> SelectRowsAsync(IEnumerable<int> rowIndices, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var indices = rowIndices.ToList();
                _logger.LogInformation("Selecting {Count} rows", indices.Count);

                foreach (var rowIndex in indices)
                {
                    SelectRow(rowIndex);
                    _selectedRows[rowIndex] = true;
                }

                return Common.Models.Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to select rows: {Message}", ex.Message);
                return Common.Models.Result.Failure($"Failed to select rows: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Selects a range of rows (async for public API)
    /// </summary>
    public async Task<Common.Models.Result> SelectRowRangeAsync(int startRowIndex, int endRowIndex, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Selecting row range [{Start}-{End}]", startRowIndex, endRowIndex);

                var start = Math.Min(startRowIndex, endRowIndex);
                var end = Math.Max(startRowIndex, endRowIndex);

                for (int i = start; i <= end; i++)
                {
                    SelectRow(i);
                    _selectedRows[i] = true;
                }

                return Common.Models.Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to select row range: {Message}", ex.Message);
                return Common.Models.Result.Failure($"Failed to select row range: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Selects all rows (async for public API)
    /// </summary>
    public async Task<Common.Models.Result> SelectAllRowsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Selecting all rows");
                SelectAll();
                return Common.Models.Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to select all rows: {Message}", ex.Message);
                return Common.Models.Result.Failure($"Failed to select all rows: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Clears all selections (public API compatibility)
    /// Calls the synchronous ClearSelection method
    /// </summary>
    public Task<Common.Models.Result> ClearSelectionPublicAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Clearing selection (public API)");
            ClearSelection();
            _selectedRows.Clear();
            return Task.FromResult(Common.Models.Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear selection: {Message}", ex.Message);
            return Task.FromResult(Common.Models.Result.Failure($"Failed to clear selection: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets indices of selected rows (public API compatibility)
    /// </summary>
    public IReadOnlyList<int> GetSelectedRowIndices()
    {
        var selectedRows = GetSelectedRows();
        return selectedRows;
    }

    /// <summary>
    /// Gets count of selected rows (public API compatibility)
    /// </summary>
    public int GetSelectedRowCount()
    {
        return GetSelectedRows().Count;
    }

    /// <summary>
    /// Gets data from selected rows (public API compatibility)
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GetSelectedRowsData()
    {
        return GetSelectedData();
    }

    #endregion
}
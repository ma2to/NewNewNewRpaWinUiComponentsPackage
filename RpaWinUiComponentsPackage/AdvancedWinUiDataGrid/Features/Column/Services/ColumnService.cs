using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Results;
using System.Collections.Concurrent;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Services;

/// <summary>
/// Internal implementation of column service with comprehensive functionality
/// Scoped service for per-operation state isolation
/// Thread-safe with no per-operation mutable fields
/// </summary>
internal sealed class ColumnService : IColumnService
{
    private readonly ILogger<ColumnService> _logger;
    private readonly Infrastructure.Persistence.Interfaces.IRowStore _rowStore;
    private readonly AdvancedDataGridOptions _options;
    private readonly IOperationLogger<ColumnService> _operationLogger;

    public ColumnService(
        ILogger<ColumnService> logger,
        Infrastructure.Persistence.Interfaces.IRowStore rowStore,
        AdvancedDataGridOptions options,
        IOperationLogger<ColumnService>? operationLogger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // Použijeme null pattern ak logger nie je poskytnutý
        _operationLogger = operationLogger ?? NullOperationLogger<ColumnService>.Instance;
    }

    /// <summary>
    /// Add new column with comprehensive configuration
    /// Thread-safe operation with local state only
    /// </summary>
    public async Task<Results.ColumnOperationResult> AddColumnAsync(Commands.AddColumnCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname add column operáciu - vytvoríme operation scope pre automatické tracking
        using var scope = _operationLogger.LogOperationStart("AddColumnAsync", new
        {
            OperationId = operationId,
            ColumnName = command.ColumnDefinition.Name,
            DataType = command.ColumnDefinition.DataType,
            DefaultValue = command.DefaultValue
        });

        _logger.LogInformation("Starting add column operation {OperationId} for column {ColumnName} with type {DataType}",
            operationId, command.ColumnDefinition.Name, command.ColumnDefinition.DataType);

        try
        {
            // Validujeme column definition
            _logger.LogInformation("Validating column definition for operation {OperationId}", operationId);

            var validationResult = await ValidateColumnDefinitionAsync(command.ColumnDefinition, operationId, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("Column validation failed for operation {OperationId}: {Error}",
                    operationId, validationResult.ErrorMessage);

                scope.MarkFailure(new InvalidOperationException(validationResult.ErrorMessage ?? "Column validation failed"));
                return Results.ColumnOperationResult.Failed(validationResult.ErrorMessage ?? "Column validation failed", stopwatch.Elapsed);
            }

            _logger.LogInformation("Column validation successful, adding column to rows for operation {OperationId}", operationId);

            // Pridáme stĺpec do všetkých existujúcich riadkov
            var addResult = await AddColumnToRowsAsync(command.ColumnDefinition, command.DefaultValue, operationId, cancellationToken);
            if (!addResult.IsSuccess)
            {
                _logger.LogError("Failed to add column to rows for operation {OperationId}: {Error}",
                    operationId, addResult.ErrorMessage);

                scope.MarkFailure(new InvalidOperationException(addResult.ErrorMessage ?? "Failed to add column"));
                return Results.ColumnOperationResult.Failed(addResult.ErrorMessage ?? "Failed to add column", stopwatch.Elapsed);
            }

            _logger.LogInformation("Add column operation {OperationId} completed successfully in {Duration}ms for column {ColumnName}",
                operationId, stopwatch.ElapsedMilliseconds, command.ColumnDefinition.Name);

            scope.MarkSuccess(new
            {
                ColumnName = command.ColumnDefinition.Name,
                DataType = command.ColumnDefinition.DataType,
                Duration = stopwatch.Elapsed
            });

            return Results.ColumnOperationResult.Success(command.ColumnDefinition.Name, stopwatch.Elapsed);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("Add column operation {OperationId} was cancelled", operationId);

            scope.MarkFailure(ex);
            return Results.ColumnOperationResult.Cancelled(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add column operation {OperationId} failed with error: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            return Results.ColumnOperationResult.Failed($"Add column failed: {ex.Message}", stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Remove column with comprehensive cleanup
    /// Thread-safe operation with local state only
    /// </summary>
    public async Task<Results.ColumnOperationResult> RemoveColumnAsync(Commands.RemoveColumnCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname remove column operáciu - vytvoríme operation scope pre automatické tracking
        using var scope = _operationLogger.LogOperationStart("RemoveColumnAsync", new
        {
            OperationId = operationId,
            ColumnName = command.ColumnName
        });

        _logger.LogInformation("Starting remove column operation {OperationId} for column {ColumnName}",
            operationId, command.ColumnName);

        try
        {
            // Validujeme či stĺpec existuje
            _logger.LogInformation("Validating column existence {ColumnName} for operation {OperationId}",
                command.ColumnName, operationId);

            var existsResult = await ValidateColumnExistsAsync(command.ColumnName, operationId, cancellationToken);
            if (!existsResult.IsSuccess)
            {
                _logger.LogWarning("Column validation failed for operation {OperationId}: {Error}",
                    operationId, existsResult.ErrorMessage);

                scope.MarkFailure(new InvalidOperationException(existsResult.ErrorMessage ?? "Column validation failed"));
                return Results.ColumnOperationResult.Failed(existsResult.ErrorMessage ?? "Column validation failed", stopwatch.Elapsed);
            }

            _logger.LogInformation("Column exists, removing column from all rows for operation {OperationId}", operationId);

            // Odstránime stĺpec zo všetkých riadkov
            var removeResult = await RemoveColumnFromRowsAsync(command.ColumnName, operationId, cancellationToken);
            if (!removeResult.IsSuccess)
            {
                _logger.LogError("Failed to remove column from rows for operation {OperationId}: {Error}",
                    operationId, removeResult.ErrorMessage);

                scope.MarkFailure(new InvalidOperationException(removeResult.ErrorMessage ?? "Failed to remove column"));
                return Results.ColumnOperationResult.Failed(removeResult.ErrorMessage ?? "Failed to remove column", stopwatch.Elapsed);
            }

            _logger.LogInformation("Remove column operation {OperationId} completed successfully in {Duration}ms for column {ColumnName}",
                operationId, stopwatch.ElapsedMilliseconds, command.ColumnName);

            scope.MarkSuccess(new
            {
                ColumnName = command.ColumnName,
                Duration = stopwatch.Elapsed
            });

            return Results.ColumnOperationResult.Success(command.ColumnName, stopwatch.Elapsed);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("Remove column operation {OperationId} was cancelled", operationId);

            scope.MarkFailure(ex);
            return Results.ColumnOperationResult.Cancelled(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove column operation {OperationId} failed with error: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            return Results.ColumnOperationResult.Failed($"Remove column failed: {ex.Message}", stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Reorder columns with comprehensive validation
    /// Thread-safe operation with local state only
    /// </summary>
    public async Task<Results.ColumnOperationResult> ReorderColumnsAsync(Commands.ReorderColumnsCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        _logger.LogInformation("Starting reorder columns operation {OperationId} with {ColumnCount} columns",
            operationId, command.NewColumnOrder.Count);

        try
        {
            // Validate column order
            var validationResult = await ValidateColumnOrderAsync(command.NewColumnOrder, operationId, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("Column order validation failed for operation {OperationId}: {Error}",
                    operationId, validationResult.ErrorMessage);
                return Results.ColumnOperationResult.Failed(validationResult.ErrorMessage ?? "Column order validation failed", stopwatch.Elapsed);
            }

            // Apply new column order to rows
            var reorderResult = await ReorderColumnInRowsAsync(command.NewColumnOrder, operationId, cancellationToken);
            if (!reorderResult.IsSuccess)
            {
                _logger.LogError("Failed to reorder columns in rows for operation {OperationId}: {Error}",
                    operationId, reorderResult.ErrorMessage);
                return Results.ColumnOperationResult.Failed(reorderResult.ErrorMessage ?? "Failed to reorder columns", stopwatch.Elapsed);
            }

            _logger.LogInformation("Reorder columns operation {OperationId} completed successfully in {Duration}ms",
                operationId, stopwatch.ElapsedMilliseconds);

            return Results.ColumnOperationResult.Success($"Reordered {command.NewColumnOrder.Count} columns", stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Reorder columns operation {OperationId} was cancelled", operationId);
            return Results.ColumnOperationResult.Cancelled(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reorder columns operation {OperationId} failed with exception", operationId);
            return Results.ColumnOperationResult.Failed($"Reorder columns failed: {ex.Message}", stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Get current column definitions
    /// Thread-safe read operation
    /// </summary>
    public async Task<IReadOnlyList<ColumnDefinition>> GetColumnDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid();
        _logger.LogDebug("Getting column definitions for operation {OperationId}", operationId);

        try
        {
            // Get sample row to determine current columns
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
            if (allRows.Count() == 0)
            {
                _logger.LogDebug("No rows found, returning empty column definitions for operation {OperationId}", operationId);
                return Array.Empty<ColumnDefinition>();
            }

            var columnDefinitions = new List<ColumnDefinition>();
            var sampleRow = allRows.First();

            foreach (var kvp in sampleRow)
            {
                var columnDef = new ColumnDefinition
                {
                    Name = kvp.Key,
                    DataType = kvp.Value?.GetType() ?? typeof(object),
                    IsVisible = true,
                    Width = 100, // Default width
                    SpecialType = (Common.SpecialColumnType)DetermineSpecialColumnType(kvp.Key, kvp.Value)
                };

                columnDefinitions.Add(columnDef);
            }

            _logger.LogDebug("Retrieved {ColumnCount} column definitions for operation {OperationId}",
                columnDefinitions.Count, operationId);

            return columnDefinitions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get column definitions for operation {OperationId}", operationId);
            return Array.Empty<ColumnDefinition>();
        }
    }

    /// <summary>
    /// Validate column definition
    /// </summary>
    private async Task<Result> ValidateColumnDefinitionAsync(
        ColumnDefinition columnDefinition,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(columnDefinition.Name))
            return Result.Failure("Column name cannot be empty");

        // Check if column already exists
        var existingColumns = await GetColumnDefinitionsAsync(cancellationToken);
        if (existingColumns.Any(c => c.Name.Equals(columnDefinition.Name, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure($"Column '{columnDefinition.Name}' already exists");

        _logger.LogDebug("Column definition validated for operation {OperationId}: {ColumnName}",
            operationId, columnDefinition.Name);

        return Result.Success();
    }

    /// <summary>
    /// Validate column exists
    /// </summary>
    private async Task<Result> ValidateColumnExistsAsync(
        string columnName,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return Result.Failure("Column name cannot be empty");

        var existingColumns = await GetColumnDefinitionsAsync(cancellationToken);
        if (!existingColumns.Any(c => c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure($"Column '{columnName}' does not exist");

        _logger.LogDebug("Column existence validated for operation {OperationId}: {ColumnName}",
            operationId, columnName);

        return Result.Success();
    }

    /// <summary>
    /// Validate column order
    /// </summary>
    private async Task<Result> ValidateColumnOrderAsync(
        IReadOnlyList<string> newColumnOrder,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        if (newColumnOrder.Count == 0)
            return Result.Failure("Column order cannot be empty");

        var existingColumns = await GetColumnDefinitionsAsync(cancellationToken);
        var existingNames = existingColumns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check for missing columns
        var missingColumns = existingNames.Except(newColumnOrder, StringComparer.OrdinalIgnoreCase).ToList();
        if (missingColumns.Count > 0)
            return Result.Failure($"Missing columns in new order: {string.Join(", ", missingColumns)}");

        // Check for extra columns
        var extraColumns = newColumnOrder.Except(existingNames, StringComparer.OrdinalIgnoreCase).ToList();
        if (extraColumns.Count > 0)
            return Result.Failure($"Unknown columns in new order: {string.Join(", ", extraColumns)}");

        _logger.LogDebug("Column order validated for operation {OperationId}: {ColumnCount} columns",
            operationId, newColumnOrder.Count);

        return Result.Success();
    }

    /// <summary>
    /// Add column to all existing rows
    /// </summary>
    private async Task<Result> AddColumnToRowsAsync(
        ColumnDefinition columnDefinition,
        object? defaultValue,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
            var updatedRows = new List<IReadOnlyDictionary<string, object?>>();

            foreach (var row in allRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var updatedRow = new Dictionary<string, object?>(row)
                {
                    [columnDefinition.Name] = defaultValue
                };

                updatedRows.Add(updatedRow);
            }

            // Replace all rows with updated versions
            await _rowStore.ReplaceAllRowsAsync(updatedRows, cancellationToken);

            _logger.LogDebug("Added column {ColumnName} to {RowCount} rows for operation {OperationId}",
                columnDefinition.Name, allRows.Count(), operationId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add column to rows for operation {OperationId}", operationId);
            return Result.Failure($"Failed to add column to rows: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove column from all rows
    /// </summary>
    private async Task<Result> RemoveColumnFromRowsAsync(
        string columnName,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
            var updatedRows = new List<IReadOnlyDictionary<string, object?>>();

            foreach (var row in allRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var updatedRow = new Dictionary<string, object?>(row);
                updatedRow.Remove(columnName);

                updatedRows.Add(updatedRow);
            }

            // Replace all rows with updated versions
            await _rowStore.ReplaceAllRowsAsync(updatedRows, cancellationToken);

            _logger.LogDebug("Removed column {ColumnName} from {RowCount} rows for operation {OperationId}",
                columnName, allRows.Count(), operationId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove column from rows for operation {OperationId}", operationId);
            return Result.Failure($"Failed to remove column from rows: {ex.Message}");
        }
    }

    /// <summary>
    /// Reorder columns in all rows
    /// </summary>
    private async Task<Result> ReorderColumnInRowsAsync(
        IReadOnlyList<string> newColumnOrder,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);
            var updatedRows = new List<IReadOnlyDictionary<string, object?>>();

            foreach (var row in allRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var reorderedRow = new Dictionary<string, object?>();

                // Add columns in new order
                foreach (var columnName in newColumnOrder)
                {
                    if (row.TryGetValue(columnName, out var value))
                    {
                        reorderedRow[columnName] = value;
                    }
                }

                updatedRows.Add(reorderedRow);
            }

            // Replace all rows with reordered versions
            await _rowStore.ReplaceAllRowsAsync(updatedRows, cancellationToken);

            _logger.LogDebug("Reordered columns in {RowCount} rows for operation {OperationId}",
                allRows.Count(), operationId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder columns in rows for operation {OperationId}", operationId);
            return Result.Failure($"Failed to reorder columns in rows: {ex.Message}");
        }
    }

    /// <summary>
    /// Determine special column type based on name and value
    /// </summary>
    private static Common.SpecialColumnType DetermineSpecialColumnType(string columnName, object? value)
    {
        if (string.IsNullOrEmpty(columnName))
            return Common.SpecialColumnType.Normal;

        var lowerName = columnName.ToLowerInvariant();

        if (lowerName.Contains("checkbox") || lowerName.Contains("check") ||
            lowerName.Contains("selected") || lowerName == "isselected" ||
            lowerName == "ischecked" || (value is bool))
        {
            return Common.SpecialColumnType.Checkbox;
        }

        if (lowerName.Contains("rownum") || lowerName.Contains("rownumber") ||
            lowerName == "row" || lowerName == "#")
        {
            return Common.SpecialColumnType.RowNumber;
        }

        return Common.SpecialColumnType.Normal;
    }

    /// <summary>
    /// Gets all column definitions (synchronous interface method)
    /// </summary>
    public IReadOnlyList<ColumnDefinition> GetColumnDefinitions()
    {
        try
        {
            return GetColumnDefinitionsAsync().Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get column definitions synchronously");
            return Array.Empty<ColumnDefinition>();
        }
    }

    /// <summary>
    /// Adds a new column definition (simplified interface method)
    /// </summary>
    public async Task<Result> AddColumnAsync(ColumnDefinition columnDefinition, CancellationToken cancellationToken = default)
    {
        var command = new Commands.AddColumnCommand(columnDefinition, null);
        var result = await AddColumnAsync(command, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage ?? "Add column failed");
    }

    /// <summary>
    /// Removes a column by name (simplified interface method)
    /// </summary>
    public async Task<Result> RemoveColumnAsync(string columnName, CancellationToken cancellationToken = default)
    {
        var command = new Commands.RemoveColumnCommand(columnName);
        var result = await RemoveColumnAsync(command, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage ?? "Remove column failed");
    }

    /// <summary>
    /// Updates an existing column definition
    /// </summary>
    public async Task<Result> UpdateColumnAsync(ColumnDefinition columnDefinition, CancellationToken cancellationToken = default)
    {
        // For now, implement as remove and add
        var removeResult = await RemoveColumnAsync(columnDefinition.Name, cancellationToken);
        if (!removeResult.IsSuccess)
            return removeResult;

        return await AddColumnAsync(columnDefinition, cancellationToken);
    }

    /// <summary>
    /// Gets a column definition by name
    /// </summary>
    public ColumnDefinition? GetColumnByName(string columnName)
    {
        try
        {
            var columns = GetColumnDefinitions();
            return columns.FirstOrDefault(c => c.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get column by name: {ColumnName}", columnName);
            return null;
        }
    }

    /// <summary>
    /// Starts column resize operation
    /// </summary>
    public void StartColumnResize(int columnIndex, double clientX)
    {
        _logger.LogDebug("Starting column resize for column {ColumnIndex} at X: {ClientX}", columnIndex, clientX);
        // Implementation would start resize operation
    }

    /// <summary>
    /// Updates column resize operation
    /// </summary>
    public void UpdateColumnResize(int columnIndex, double clientX)
    {
        _logger.LogDebug("Updating column resize for column {ColumnIndex} at X: {ClientX}", columnIndex, clientX);
        // Implementation would update resize operation
    }

    /// <summary>
    /// Ends column resize operation
    /// </summary>
    public void EndColumnResize(int columnIndex, double clientX)
    {
        _logger.LogDebug("Ending column resize for column {ColumnIndex} at X: {ClientX}", columnIndex, clientX);
        // Implementation would finalize resize operation
    }

    /// <summary>
    /// Resizes a column to specific width
    /// </summary>
    public async Task<Result> ResizeColumnAsync(int columnIndex, double newWidth, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Resizing column {ColumnIndex} to width {NewWidth}", columnIndex, newWidth);
            // Implementation would update column width
            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize column {ColumnIndex}", columnIndex);
            return Result.Failure($"Failed to resize column: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the total number of columns
    /// </summary>
    public int GetColumnCount()
    {
        try
        {
            var columns = GetColumnDefinitions();
            return columns.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get column count");
            return 0;
        }
    }

    /// <summary>
    /// Gets visible columns (excluding hidden special columns)
    /// </summary>
    public IReadOnlyList<ColumnDefinition> GetVisibleColumns()
    {
        try
        {
            var columns = GetColumnDefinitions();
            return columns.Where(c => c.IsVisible).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get visible columns");
            return Array.Empty<ColumnDefinition>();
        }
    }

    /// <summary>
    /// Reorders columns
    /// </summary>
    public async Task<Result> ReorderColumnAsync(int fromIndex, int toIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            var columns = GetColumnDefinitions();
            if (fromIndex < 0 || fromIndex >= columns.Count || toIndex < 0 || toIndex >= columns.Count)
                return Result.Failure("Invalid column indices for reorder operation");

            var columnNames = columns.Select(c => c.Name).ToList();
            var columnToMove = columnNames[fromIndex];
            columnNames.RemoveAt(fromIndex);
            columnNames.Insert(toIndex, columnToMove);

            var command = new Commands.ReorderColumnsCommand(columnNames);
            var result = await ReorderColumnsAsync(command, cancellationToken);
            return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage ?? "Reorder column failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder column from {FromIndex} to {ToIndex}", fromIndex, toIndex);
            return Result.Failure($"Failed to reorder column: {ex.Message}");
        }
    }

    // Additional methods for internal facade operations

    /// <summary>
    /// Starts column resize operation (internal)
    /// </summary>
    public void StartResizeInternal(int columnIndex, double clientX)
    {
        _logger.LogDebug("Starting column resize for column {ColumnIndex} at X: {ClientX}", columnIndex, clientX);
        // Implementation would start resize operation with per-operation local state
    }

    /// <summary>
    /// Updates column resize operation (internal)
    /// </summary>
    public void UpdateResizeInternal(double clientX)
    {
        _logger.LogDebug("Updating column resize at X: {ClientX}", clientX);
        // Implementation would update resize operation with per-operation local state
    }

    /// <summary>
    /// Ends column resize operation (internal)
    /// </summary>
    public void EndResizeInternal()
    {
        _logger.LogDebug("Ending column resize operation");
        // Implementation would finalize resize operation with per-operation local state
    }

    /// <summary>
    /// Adds a new column definition (simple synchronous version)
    /// </summary>
    public bool AddColumn(ColumnDefinition columnDefinition)
    {
        try
        {
            var result = AddColumnAsync(columnDefinition).Result;
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add column synchronously: {ColumnName}", columnDefinition?.Name);
            return false;
        }
    }

    /// <summary>
    /// Removes a column by name (simple synchronous version)
    /// </summary>
    public bool RemoveColumn(string columnName)
    {
        try
        {
            var result = RemoveColumnAsync(columnName).Result;
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove column synchronously: {ColumnName}", columnName);
            return false;
        }
    }

    /// <summary>
    /// Updates an existing column definition (simple synchronous version)
    /// </summary>
    public bool UpdateColumn(ColumnDefinition columnDefinition)
    {
        try
        {
            var result = UpdateColumnAsync(columnDefinition).Result;
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update column synchronously: {ColumnName}", columnDefinition?.Name);
            return false;
        }
    }
}
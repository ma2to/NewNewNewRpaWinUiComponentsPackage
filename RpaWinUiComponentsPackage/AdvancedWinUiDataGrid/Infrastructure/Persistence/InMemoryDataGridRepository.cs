using System;
using System.Collections.Generic;
using System.Linq;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence;

/// <summary>
/// INFRASTRUCTURE: In-memory repository implementation for data grid storage
/// SINGLE RESPONSIBILITY: Data persistence and retrieval operations
/// PERFORMANCE: Optimized for frequent reads with change tracking
/// </summary>
internal sealed class InMemoryDataGridRepository : IDataGridRepository, IDisposable
{
    private readonly IDataGridLogger _logger;
    private readonly List<DataRow> _rows = new();
    private readonly List<DataColumn> _columns = new();
    private readonly object _lock = new();
    private bool _disposed;

    public InMemoryDataGridRepository(IDataGridLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("REPOSITORY: Initialized in-memory data grid repository");
    }

    #region Row Operations

    public Result<IReadOnlyList<DataRow>> GetAllRows()
    {
        return _logger.ExecuteWithLogging(() =>
        {
            lock (_lock)
            {
                var rows = (IReadOnlyList<DataRow>)_rows.ToList().AsReadOnly();
                _logger.LogInformation("REPOSITORY: Retrieved {RowCount} rows", rows.Count);
                return rows;
            }
        }, "GetAllRows");
    }

    public Result<DataRow?> GetRow(int rowIndex)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            lock (_lock)
            {
                if (rowIndex < 0 || rowIndex >= _rows.Count)
                {
                    _logger.LogWarning("REPOSITORY: Row index {RowIndex} out of range (0-{MaxIndex})",
                        rowIndex, _rows.Count - 1);
                    return null;
                }

                var row = _rows[rowIndex];
                _logger.LogInformation("REPOSITORY: Retrieved row {RowIndex} with {CellCount} cells",
                    rowIndex, row.Cells.Count);
                return row;
            }
        }, "GetRow");
    }

    public Result AddRow(DataRow row)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            lock (_lock)
            {
                _rows.Add(row);

                // Subscribe to row state changes
                row.StateChanged += OnRowStateChanged;

                _logger.LogInformation("REPOSITORY: Added row {RowIndex} with {CellCount} cells",
                    row.RowIndex, row.Cells.Count);
            }
            return; // Fix for void lambda
        }, "AddRow");
    }

    public Result UpdateRow(DataRow row)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            lock (_lock)
            {
                var existingRowIndex = _rows.FindIndex(r => r.RowIndex == row.RowIndex);
                if (existingRowIndex == -1)
                {
                    throw new InvalidOperationException($"Row with index {row.RowIndex} not found");
                }

                // Unsubscribe from old row
                _rows[existingRowIndex].StateChanged -= OnRowStateChanged;

                // Replace with new row
                _rows[existingRowIndex] = row;

                // Subscribe to new row
                row.StateChanged += OnRowStateChanged;

                _logger.LogInformation("REPOSITORY: Updated row {RowIndex}", row.RowIndex);
            }
        }, "UpdateRow");
    }

    public Result RemoveRow(int rowIndex)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            lock (_lock)
            {
                var row = _rows.FirstOrDefault(r => r.RowIndex == rowIndex);
                if (row == null)
                {
                    throw new InvalidOperationException($"Row with index {rowIndex} not found");
                }

                // Unsubscribe from events
                row.StateChanged -= OnRowStateChanged;

                _rows.RemoveAll(r => r.RowIndex == rowIndex);

                // Update row indices for subsequent rows
                for (int i = 0; i < _rows.Count; i++)
                {
                    if (_rows[i].RowIndex > rowIndex)
                    {
                        // This would require a method to update row index
                        // For now, we'll log the inconsistency
                        _logger.LogWarning("REPOSITORY: Row index inconsistency after removal of row {RemovedIndex}",
                            rowIndex);
                    }
                }

                _logger.LogInformation("REPOSITORY: Removed row {RowIndex}", rowIndex);
            }
        }, "RemoveRow");
    }

    #endregion

    #region Column Operations

    public Result<IReadOnlyList<DataColumn>> GetAllColumns()
    {
        return _logger.ExecuteWithLogging(() =>
        {
            lock (_lock)
            {
                var columns = (IReadOnlyList<DataColumn>)_columns.ToList().AsReadOnly();
                _logger.LogInformation("REPOSITORY: Retrieved {ColumnCount} columns", columns.Count);
                return columns;
            }
        }, "GetAllColumns");
    }

    public Result<DataColumn?> GetColumn(string columnName)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentException("Column name cannot be empty", nameof(columnName));

            lock (_lock)
            {
                var column = _columns.FirstOrDefault(c => c.Name == columnName);
                if (column != null)
                {
                    _logger.LogInformation("REPOSITORY: Retrieved column '{ColumnName}' (Width: {Width}px, Type: {Type})",
                        columnName, column.Width, column.SpecialType);
                }
                else
                {
                    _logger.LogWarning("REPOSITORY: Column '{ColumnName}' not found", columnName);
                }
                return column;
            }
        }, "GetColumn");
    }

    public Result AddColumn(DataColumn column)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            lock (_lock)
            {
                // Check for name conflicts
                if (_columns.Any(c => c.Name == column.Name))
                {
                    throw new InvalidOperationException($"Column with name '{column.Name}' already exists");
                }

                _columns.Add(column);

                // Subscribe to column events
                column.NameChanged += OnColumnNameChanged;
                column.WidthChanged += OnColumnWidthChanged;

                // Set display order
                column.DisplayOrder = _columns.Count - 1;

                _logger.LogInformation("REPOSITORY: Added column '{ColumnName}' (Width: {Width}px, Type: {Type})",
                    column.Name, column.Width, column.SpecialType);
            }
        }, "AddColumn");
    }

    public Result UpdateColumn(DataColumn column)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            lock (_lock)
            {
                var existingColumnIndex = _columns.FindIndex(c => c.OriginalName == column.OriginalName);
                if (existingColumnIndex == -1)
                {
                    throw new InvalidOperationException($"Column with original name '{column.OriginalName}' not found");
                }

                // Unsubscribe from old column
                var oldColumn = _columns[existingColumnIndex];
                oldColumn.NameChanged -= OnColumnNameChanged;
                oldColumn.WidthChanged -= OnColumnWidthChanged;

                // Replace with new column
                _columns[existingColumnIndex] = column;

                // Subscribe to new column
                column.NameChanged += OnColumnNameChanged;
                column.WidthChanged += OnColumnWidthChanged;

                _logger.LogInformation("REPOSITORY: Updated column '{ColumnName}'", column.Name);
            }
        }, "UpdateColumn");
    }

    public Result RemoveColumn(string columnName)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentException("Column name cannot be empty", nameof(columnName));

            lock (_lock)
            {
                var column = _columns.FirstOrDefault(c => c.Name == columnName);
                if (column == null)
                {
                    throw new InvalidOperationException($"Column '{columnName}' not found");
                }

                // Unsubscribe from events
                column.NameChanged -= OnColumnNameChanged;
                column.WidthChanged -= OnColumnWidthChanged;

                _columns.Remove(column);

                // Remove cells from all rows
                foreach (var row in _rows)
                {
                    row.RemoveCell(columnName);
                }

                _logger.LogInformation("REPOSITORY: Removed column '{ColumnName}' and associated cells", columnName);
            }
        }, "RemoveColumn");
    }

    #endregion

    #region Cell Operations

    public Result<Cell?> GetCell(int rowIndex, string columnName)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentException("Column name cannot be empty", nameof(columnName));

            lock (_lock)
            {
                var row = _rows.FirstOrDefault(r => r.RowIndex == rowIndex);
                if (row == null)
                {
                    _logger.LogWarning("REPOSITORY: Row {RowIndex} not found for cell retrieval", rowIndex);
                    return null;
                }

                var cell = row.GetCell(columnName);
                if (cell != null)
                {
                    _logger.LogInformation("REPOSITORY: Retrieved cell [{RowIndex}, {ColumnName}] with value '{Value}'",
                        rowIndex, columnName, cell.Value?.ToString() ?? "null");
                }
                else
                {
                    _logger.LogWarning("REPOSITORY: Cell [{RowIndex}, {ColumnName}] not found", rowIndex, columnName);
                }
                return cell;
            }
        }, "GetCell");
    }

    public Result UpdateCell(int rowIndex, string columnName, object? value)
    {
        return _logger.ExecuteWithLogging(() =>
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentException("Column name cannot be empty", nameof(columnName));

            lock (_lock)
            {
                var row = _rows.FirstOrDefault(r => r.RowIndex == rowIndex);
                if (row == null)
                {
                    throw new InvalidOperationException($"Row {rowIndex} not found");
                }

                var oldValue = row.GetCellValue(columnName);
                row.SetCellValue(columnName, value);

                _logger.LogInformation("REPOSITORY: Updated cell [{RowIndex}, {ColumnName}] from '{OldValue}' to '{NewValue}'",
                    rowIndex, columnName, oldValue?.ToString() ?? "null", value?.ToString() ?? "null");
            }
        }, "UpdateCell");
    }

    #endregion

    #region Utility Operations

    public Result<int> GetRowCount()
    {
        return _logger.ExecuteWithLogging(() =>
        {
            lock (_lock)
            {
                return _rows.Count;
            }
        }, "GetRowCount");
    }

    public Result<int> GetColumnCount()
    {
        return _logger.ExecuteWithLogging(() =>
        {
            lock (_lock)
            {
                return _columns.Count;
            }
        }, "GetColumnCount");
    }

    public Result ClearAllData()
    {
        return _logger.ExecuteWithLogging(() =>
        {
            lock (_lock)
            {
                // Unsubscribe from all events
                foreach (var row in _rows)
                {
                    row.StateChanged -= OnRowStateChanged;
                }

                foreach (var column in _columns)
                {
                    column.NameChanged -= OnColumnNameChanged;
                    column.WidthChanged -= OnColumnWidthChanged;
                }

                var rowCount = _rows.Count;
                var columnCount = _columns.Count;

                _rows.Clear();
                _columns.Clear();

                _logger.LogInformation("REPOSITORY: Cleared all data - {RowCount} rows and {ColumnCount} columns removed",
                    rowCount, columnCount);
            }
        }, "ClearAllData");
    }

    public Result CommitChanges()
    {
        return _logger.ExecuteWithLogging(() =>
        {
            lock (_lock)
            {
                int changedRows = 0;
                foreach (var row in _rows.Where(r => r.HasUnsavedChanges))
                {
                    row.CommitChanges();
                    changedRows++;
                }

                _logger.LogInformation("REPOSITORY: Committed changes for {ChangedRows} rows", changedRows);
            }
        }, "CommitChanges");
    }

    public Result RevertChanges()
    {
        return _logger.ExecuteWithLogging(() =>
        {
            lock (_lock)
            {
                int changedRows = 0;
                foreach (var row in _rows.Where(r => r.HasUnsavedChanges))
                {
                    row.RevertChanges();
                    changedRows++;
                }

                _logger.LogInformation("REPOSITORY: Reverted changes for {ChangedRows} rows", changedRows);
            }
        }, "RevertChanges");
    }

    #endregion

    #region Event Handlers

    private void OnRowStateChanged(object? sender, RowStateChangedEventArgs e)
    {
        if (sender is DataRow row)
        {
            _logger.LogInformation("REPOSITORY: Row {RowIndex} state changed: {ChangeType}",
                row.RowIndex, e.ChangeType);
        }
    }

    private void OnColumnNameChanged(object? sender, ColumnNameChangedEventArgs e)
    {
        _logger.LogInformation("REPOSITORY: Column renamed from '{OldName}' to '{NewName}'",
            e.OldName, e.NewName);
    }

    private void OnColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
    {
        if (sender is DataColumn column)
        {
            _logger.LogInformation("REPOSITORY: Column '{ColumnName}' width changed from {OldWidth}px to {NewWidth}px",
                column.Name, e.OldWidth, e.NewWidth);
        }
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            // Unsubscribe from all events
            foreach (var row in _rows)
            {
                row.StateChanged -= OnRowStateChanged;
            }

            foreach (var column in _columns)
            {
                column.NameChanged -= OnColumnNameChanged;
                column.WidthChanged -= OnColumnWidthChanged;
            }

            _rows.Clear();
            _columns.Clear();

            _logger.LogInformation("REPOSITORY: Disposed");
            _disposed = true;
        }
    }
}
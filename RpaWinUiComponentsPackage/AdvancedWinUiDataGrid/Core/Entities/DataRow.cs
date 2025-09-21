using System;
using System.Collections.Generic;
using System.Linq;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;

/// <summary>
/// DOMAIN ENTITY: Represents a single row in the data grid
/// SINGLE RESPONSIBILITY: Row state management and cell coordination
/// </summary>
internal sealed class DataRow
{
    private readonly Dictionary<string, Cell> _cells = new();

    public int RowIndex { get; }

    /// <summary>
    /// CORE PROPERTY: Row number for stable identification and ordering
    /// This is automatically managed and should never be exported/imported
    /// </summary>
    public int RowNumber { get; set; }

    public bool IsEmpty => _cells.Values.All(c => c.IsEmpty);
    public bool HasUnsavedChanges => _cells.Values.Any(c => c.HasUnsavedChanges);
    public bool HasValidationErrors => _cells.Values.Any(c => c.HasValidationErrors);
    public IReadOnlyDictionary<string, Cell> Cells => _cells.AsReadOnly();

    public event EventHandler<RowStateChangedEventArgs>? StateChanged;
    public event EventHandler<CellValueChangedEventArgs>? CellValueChanged;

    public DataRow(int rowIndex, int rowNumber = 0)
    {
        if (rowIndex < 0) throw new ArgumentOutOfRangeException(nameof(rowIndex), "Row index cannot be negative");
        if (rowNumber < 0) throw new ArgumentOutOfRangeException(nameof(rowNumber), "Row number cannot be negative");
        RowIndex = rowIndex;
        RowNumber = rowNumber;
    }

    /// <summary>
    /// ENTERPRISE: Add or update cell in this row
    /// </summary>
    public void SetCell(string columnName, Cell cell)
    {
        if (string.IsNullOrEmpty(columnName)) throw new ArgumentException("Column name cannot be empty", nameof(columnName));
        if (cell == null) throw new ArgumentNullException(nameof(cell));

        var wasEmpty = IsEmpty;
        var hadChanges = HasUnsavedChanges;
        var hadErrors = HasValidationErrors;

        if (_cells.ContainsKey(columnName))
        {
            _cells[columnName].ValueChanged -= OnCellValueChanged;
        }

        _cells[columnName] = cell;
        cell.ValueChanged += OnCellValueChanged;

        NotifyStateChangeIfNeeded(wasEmpty, hadChanges, hadErrors);
    }

    /// <summary>
    /// ENTERPRISE: Get cell by column name
    /// </summary>
    public Cell? GetCell(string columnName)
    {
        return _cells.TryGetValue(columnName, out var cell) ? cell : null;
    }

    /// <summary>
    /// ENTERPRISE: Get cell value by column name
    /// </summary>
    public object? GetCellValue(string columnName)
    {
        return GetCell(columnName)?.Value;
    }

    /// <summary>
    /// ENTERPRISE: Set cell value by column name
    /// </summary>
    public void SetCellValue(string columnName, object? value)
    {
        var cell = GetCell(columnName);
        if (cell != null)
        {
            cell.Value = value;
        }
        else
        {
            var cellAddress = new CellAddress(RowIndex, GetColumnIndex(columnName));
            var newCell = new Cell(cellAddress, columnName, value);
            SetCell(columnName, newCell);
        }
    }

    /// <summary>
    /// ENTERPRISE: Remove cell from row
    /// </summary>
    public bool RemoveCell(string columnName)
    {
        if (!_cells.TryGetValue(columnName, out var cell)) return false;

        var wasEmpty = IsEmpty;
        var hadChanges = HasUnsavedChanges;
        var hadErrors = HasValidationErrors;

        cell.ValueChanged -= OnCellValueChanged;
        _cells.Remove(columnName);

        NotifyStateChangeIfNeeded(wasEmpty, hadChanges, hadErrors);
        return true;
    }

    /// <summary>
    /// ENTERPRISE: Commit all changes in this row
    /// </summary>
    public void CommitChanges()
    {
        var hadChanges = HasUnsavedChanges;

        foreach (var cell in _cells.Values)
        {
            cell.CommitChanges();
        }

        if (hadChanges)
        {
            StateChanged?.Invoke(this, new RowStateChangedEventArgs(RowStateChangeType.ChangesCommitted));
        }
    }

    /// <summary>
    /// ENTERPRISE: Revert all changes in this row
    /// </summary>
    public void RevertChanges()
    {
        var hadChanges = HasUnsavedChanges;

        foreach (var cell in _cells.Values)
        {
            cell.RevertChanges();
        }

        if (hadChanges)
        {
            StateChanged?.Invoke(this, new RowStateChangedEventArgs(RowStateChangeType.ChangesReverted));
        }
    }

    /// <summary>
    /// VALIDATION: Clear all validation results in this row
    /// </summary>
    public void ClearValidationResults()
    {
        var hadErrors = HasValidationErrors;

        foreach (var cell in _cells.Values)
        {
            cell.ClearValidationResults();
        }

        if (hadErrors)
        {
            StateChanged?.Invoke(this, new RowStateChangedEventArgs(RowStateChangeType.ValidationCleared));
        }
    }

    /// <summary>
    /// ENTERPRISE: Get row data as dictionary for validation and export
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetRowData()
    {
        return _cells.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
    }

    /// <summary>
    /// ENTERPRISE: Get highest validation severity in this row
    /// VALIDATION: Aggregates validation results for row-level status indication
    /// </summary>
    public ValidationSeverity GetHighestSeverity()
    {
        if (!HasValidationErrors)
            return ValidationSeverity.Info;

        var maxSeverity = ValidationSeverity.Info;
        foreach (var cell in _cells.Values)
        {
            if (cell.HasValidationErrors)
            {
                foreach (var validationResult in cell.ValidationResults)
                {
                    if (validationResult.Severity > maxSeverity)
                        maxSeverity = validationResult.Severity;
                }
            }
        }

        return maxSeverity;
    }

    private void OnCellValueChanged(object? sender, CellValueChangedEventArgs e)
    {
        // ENTERPRISE: Forward cell value changes to row level for aggregate operations
        CellValueChanged?.Invoke(this, e);

        var wasEmpty = _cells.Values.Where(c => c != sender).All(c => c.IsEmpty) && (e.OldValue == null || string.IsNullOrWhiteSpace(e.OldValue?.ToString()));
        var isEmpty = IsEmpty;

        if (wasEmpty != isEmpty)
        {
            StateChanged?.Invoke(this, new RowStateChangedEventArgs(
                isEmpty ? RowStateChangeType.BecameEmpty : RowStateChangeType.BecameNotEmpty));
        }
    }

    private void NotifyStateChangeIfNeeded(bool wasEmpty, bool hadChanges, bool hadErrors)
    {
        if (wasEmpty != IsEmpty)
        {
            StateChanged?.Invoke(this, new RowStateChangedEventArgs(
                IsEmpty ? RowStateChangeType.BecameEmpty : RowStateChangeType.BecameNotEmpty));
        }

        if (hadChanges != HasUnsavedChanges)
        {
            StateChanged?.Invoke(this, new RowStateChangedEventArgs(
                HasUnsavedChanges ? RowStateChangeType.ChangesDetected : RowStateChangeType.ChangesCommitted));
        }

        if (hadErrors != HasValidationErrors)
        {
            StateChanged?.Invoke(this, new RowStateChangedEventArgs(
                HasValidationErrors ? RowStateChangeType.ValidationErrors : RowStateChangeType.ValidationCleared));
        }
    }

    private int GetColumnIndex(string columnName)
    {
        // This would be provided by the grid context
        // For now, use the order in which columns were added
        return _cells.Keys.ToList().IndexOf(columnName);
    }

    public override string ToString()
    {
        var status = HasValidationErrors ? " (Invalid)" :
                    HasUnsavedChanges ? " (Modified)" :
                    IsEmpty ? " (Empty)" : "";
        return $"Row[{RowIndex}] RowNum[{RowNumber}]: {_cells.Count} cells{status}";
    }
}

/// <summary>
/// EVENT ARGS: Row state change notification
/// </summary>
internal sealed class RowStateChangedEventArgs : EventArgs
{
    public RowStateChangeType ChangeType { get; }

    public RowStateChangedEventArgs(RowStateChangeType changeType)
    {
        ChangeType = changeType;
    }
}

/// <summary>
/// ENUM: Types of row state changes
/// </summary>
internal enum RowStateChangeType
{
    BecameEmpty,
    BecameNotEmpty,
    ChangesDetected,
    ChangesCommitted,
    ChangesReverted,
    ValidationErrors,
    ValidationCleared
}
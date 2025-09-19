using System;
using System.Collections.Generic;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;

/// <summary>
/// DOMAIN ENTITY: Represents a single cell in the data grid
/// SINGLE RESPONSIBILITY: Cell state management and validation tracking
/// </summary>
internal sealed class Cell
{
    private object? _value;
    private object? _originalValue;
    private readonly List<ValidationResult> _validationResults = new();

    public CellAddress Address { get; }
    public string ColumnName { get; }
    public bool IsReadOnly { get; set; }

    public object? Value
    {
        get => _value;
        set
        {
            if (_value?.Equals(value) == true) return;

            var oldValue = _value;
            _value = value;
            HasUnsavedChanges = !_originalValue?.Equals(value) == true;

            ValueChanged?.Invoke(this, new CellValueChangedEventArgs(oldValue, value, ColumnName));
        }
    }

    public object? OriginalValue => _originalValue;
    public bool HasUnsavedChanges { get; private set; }
    public bool IsEmpty => Value == null || string.IsNullOrWhiteSpace(Value.ToString());
    public bool HasValidationErrors => _validationResults.Exists(r => !r.IsValid);
    public IReadOnlyList<ValidationResult> ValidationResults => _validationResults.AsReadOnly();

    public event EventHandler<CellValueChangedEventArgs>? ValueChanged;

    public Cell(CellAddress address, string columnName, object? initialValue = null)
    {
        Address = address;
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        _value = initialValue;
        _originalValue = initialValue;
    }

    /// <summary>
    /// ENTERPRISE: Commit changes making current value the original
    /// </summary>
    public void CommitChanges()
    {
        _originalValue = _value;
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// ENTERPRISE: Revert to original value discarding changes
    /// </summary>
    public void RevertChanges()
    {
        if (!HasUnsavedChanges) return;

        var oldValue = _value;
        _value = _originalValue;
        HasUnsavedChanges = false;

        ValueChanged?.Invoke(this, new CellValueChangedEventArgs(oldValue, _originalValue, ColumnName));
    }

    /// <summary>
    /// VALIDATION: Set validation results for this cell
    /// </summary>
    public void SetValidationResults(IEnumerable<ValidationResult> results)
    {
        _validationResults.Clear();
        _validationResults.AddRange(results);
    }

    /// <summary>
    /// VALIDATION: Add single validation result
    /// </summary>
    public void AddValidationResult(ValidationResult result)
    {
        _validationResults.Add(result);
    }

    /// <summary>
    /// VALIDATION: Clear all validation results
    /// </summary>
    public void ClearValidationResults()
    {
        _validationResults.Clear();
    }

    /// <summary>
    /// VALIDATION: Get highest severity validation error
    /// </summary>
    public ValidationSeverity GetHighestSeverity()
    {
        if (!HasValidationErrors) return ValidationSeverity.Info;

        var maxSeverity = ValidationSeverity.Info;
        foreach (var result in _validationResults)
        {
            if (!result.IsValid && result.Severity > maxSeverity)
                maxSeverity = result.Severity;
        }
        return maxSeverity;
    }

    public override string ToString()
    {
        var status = HasValidationErrors ? " (Invalid)" :
                    HasUnsavedChanges ? " (Modified)" : "";
        return $"Cell[{Address.ToExcelAddress()}]: {Value}{status}";
    }
}

/// <summary>
/// EVENT ARGS: Cell value change notification
/// ENTERPRISE: Comprehensive change tracking for audit and validation purposes
/// </summary>
internal sealed class CellValueChangedEventArgs : EventArgs
{
    public object? OldValue { get; }
    public object? NewValue { get; }

    /// <summary>
    /// ENTERPRISE: Column name for identifying the source of change in row-level events
    /// </summary>
    public string ColumnName { get; }

    public CellValueChangedEventArgs(object? oldValue, object? newValue, string columnName)
    {
        OldValue = oldValue;
        NewValue = newValue;
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
    }
}
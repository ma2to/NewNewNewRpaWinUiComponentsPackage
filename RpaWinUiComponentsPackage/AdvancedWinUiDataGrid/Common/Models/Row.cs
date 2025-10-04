using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Represents a data row in the grid with thread-safe operations
/// ENTERPRISE: Enhanced with row number management, timestamps, and state tracking
/// </summary>
internal class Row
{
    private readonly ConcurrentDictionary<string, object?> _cells = new();
    private readonly object _lockObject = new();
    private bool _isSelected;
    private bool _isVisible = true;
    private bool _isChecked;
    private string? _validationMessage;
    private int _rowNumber;

    /// <summary>
    /// Gets or sets the unique identifier for this row
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the row index in the grid (0-based internal index)
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the row number (1-based display number for user)
    /// ENTERPRISE: Used for row numbering feature, automatically managed
    /// </summary>
    public int RowNumber
    {
        get => _rowNumber;
        set => _rowNumber = value;
    }

    /// <summary>
    /// Gets the creation timestamp of this row
    /// SMART: Used as fallback for ordering when RowNumbers are corrupted
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last modified timestamp
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether this row is checked (for checkbox column feature)
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => _isChecked = value;
    }

    /// <summary>
    /// Gets or sets whether this row is selected
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => _isSelected = value;
    }

    /// <summary>
    /// Gets or sets whether this row is visible (after filtering)
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }

    /// <summary>
    /// Gets or sets the validation message for this row
    /// </summary>
    public string? ValidationMessage
    {
        get => _validationMessage;
        set => _validationMessage = value;
    }

    /// <summary>
    /// Gets whether this row has validation errors
    /// </summary>
    public bool HasValidationErrors => !string.IsNullOrEmpty(_validationMessage);

    /// <summary>
    /// Gets whether this row is empty (all cells are null or empty)
    /// </summary>
    public bool IsEmpty => _cells.Values.All(v => v == null || string.IsNullOrWhiteSpace(v?.ToString()));

    /// <summary>
    /// Gets the cell value for the specified column
    /// </summary>
    /// <param name="columnName">Name of the column</param>
    /// <returns>Cell value or null if not found</returns>
    public object? GetValue(string columnName)
    {
        return _cells.TryGetValue(columnName, out var value) ? value : null;
    }

    /// <summary>
    /// Sets the cell value for the specified column
    /// </summary>
    /// <param name="columnName">Name of the column</param>
    /// <param name="value">Value to set</param>
    public void SetValue(string columnName, object? value)
    {
        _cells.AddOrUpdate(columnName, value, (_, _) => value);
    }

    /// <summary>
    /// Gets all cell values as a read-only dictionary
    /// </summary>
    /// <returns>Read-only dictionary of cell values</returns>
    public IReadOnlyDictionary<string, object?> GetAllValues()
    {
        return new ReadOnlyDictionary<string, object?>(_cells.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    /// <summary>
    /// Clears all cell values
    /// </summary>
    public void Clear()
    {
        _cells.Clear();
        _validationMessage = null;
    }

    /// <summary>
    /// Creates a deep copy of this row
    /// </summary>
    /// <returns>New row instance with copied data</returns>
    public Row Clone()
    {
        var clonedRow = new Row
        {
            Index = this.Index,
            RowNumber = this.RowNumber,
            IsSelected = this.IsSelected,
            IsVisible = this.IsVisible,
            IsChecked = this.IsChecked,
            ValidationMessage = this.ValidationMessage,
            LastModified = this.LastModified
        };

        foreach (var kvp in _cells)
        {
            clonedRow.SetValue(kvp.Key, kvp.Value);
        }

        return clonedRow;
    }

    /// <summary>
    /// Gets the column names that have values in this row
    /// </summary>
    /// <returns>Collection of column names</returns>
    public IEnumerable<string> GetColumnNames()
    {
        return _cells.Keys;
    }

    /// <summary>
    /// Checks if the row contains a value for the specified column
    /// </summary>
    /// <param name="columnName">Name of the column</param>
    /// <returns>True if column has a value, false otherwise</returns>
    public bool HasColumn(string columnName)
    {
        return _cells.ContainsKey(columnName);
    }

    /// <summary>
    /// Removes the value for the specified column
    /// </summary>
    /// <param name="columnName">Name of the column to remove</param>
    /// <returns>True if removed successfully, false if column didn't exist</returns>
    public bool RemoveColumn(string columnName)
    {
        return _cells.TryRemove(columnName, out _);
    }
}
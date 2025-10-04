namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Represents an individual cell in the data grid
/// </summary>
internal class Cell
{
    /// <summary>
    /// Gets or sets the raw value of the cell
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the formatted display value of the cell
    /// </summary>
    public string? DisplayValue { get; set; }

    /// <summary>
    /// Gets or sets the column name this cell belongs to
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the row index this cell belongs to
    /// </summary>
    public int RowIndex { get; set; }

    /// <summary>
    /// Gets or sets whether this cell is selected
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets or sets whether this cell is in edit mode
    /// </summary>
    public bool IsEditing { get; set; }

    /// <summary>
    /// Gets or sets whether this cell is read-only
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the validation error message for this cell
    /// </summary>
    public string? ValidationError { get; set; }

    /// <summary>
    /// Gets whether this cell has a validation error
    /// </summary>
    public bool HasValidationError => !string.IsNullOrEmpty(ValidationError);

    /// <summary>
    /// Gets whether this cell is empty
    /// </summary>
    public bool IsEmpty => Value == null || string.IsNullOrWhiteSpace(Value.ToString());

    /// <summary>
    /// Gets the string representation of the cell value
    /// </summary>
    /// <returns>String representation of the value</returns>
    public string GetStringValue()
    {
        return Value?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Gets the typed value of the cell
    /// </summary>
    /// <typeparam name="T">Type to convert to</typeparam>
    /// <returns>Typed value or default if conversion fails</returns>
    public T? GetTypedValue<T>()
    {
        if (Value == null) return default;

        try
        {
            return (T)Convert.ChangeType(Value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Sets the value with automatic display value formatting
    /// </summary>
    /// <param name="value">Value to set</param>
    /// <param name="formatProvider">Optional format provider</param>
    public void SetValue(object? value, IFormatProvider? formatProvider = null)
    {
        Value = value;

        if (value != null)
        {
            DisplayValue = formatProvider != null && value is IFormattable formattable
                ? formattable.ToString(null, formatProvider)
                : value.ToString();
        }
        else
        {
            DisplayValue = string.Empty;
        }
    }

    /// <summary>
    /// Clears the cell value and validation error
    /// </summary>
    public void Clear()
    {
        Value = null;
        DisplayValue = string.Empty;
        ValidationError = null;
        IsEditing = false;
    }

    /// <summary>
    /// Creates a copy of this cell
    /// </summary>
    /// <returns>New cell instance with copied properties</returns>
    public Cell Clone()
    {
        return new Cell
        {
            Value = this.Value,
            DisplayValue = this.DisplayValue,
            ColumnName = this.ColumnName,
            RowIndex = this.RowIndex,
            IsSelected = this.IsSelected,
            IsEditing = this.IsEditing,
            IsReadOnly = this.IsReadOnly,
            ValidationError = this.ValidationError
        };
    }
}
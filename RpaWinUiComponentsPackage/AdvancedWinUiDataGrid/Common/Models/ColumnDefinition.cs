namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

/// <summary>
/// Defines the configuration and properties of a grid column
/// </summary>
internal class ColumnDefinition
{
    /// <summary>
    /// Gets or sets the unique name of the column
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display header text for the column
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name (alias for Header for backward compatibility)
    /// </summary>
    public string DisplayName
    {
        get => Header;
        set => Header = value;
    }

    /// <summary>
    /// Gets or sets the data type of the column
    /// </summary>
    public Type DataType { get; set; } = typeof(string);

    /// <summary>
    /// Gets or sets the sort direction for this column
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.None;

    /// <summary>
    /// Gets or sets the width of the column
    /// </summary>
    public double Width { get; set; } = 100.0;

    /// <summary>
    /// Gets or sets the minimum width of the column
    /// </summary>
    public double MinWidth { get; set; } = 50.0;

    /// <summary>
    /// Gets or sets the maximum width of the column
    /// </summary>
    public double MaxWidth { get; set; } = double.MaxValue;

    /// <summary>
    /// Gets or sets whether the column is visible
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the column is read-only
    /// </summary>
    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the column can be sorted
    /// </summary>
    public bool IsSortable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the column can be filtered
    /// </summary>
    public bool IsFilterable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the column can be resized
    /// </summary>
    public bool IsResizable { get; set; } = true;

    /// <summary>
    /// Gets or sets the display order of the column
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Gets or sets the format string for displaying values
    /// </summary>
    public string? FormatString { get; set; }

    /// <summary>
    /// Gets or sets the default value for new cells in this column
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets whether this column is a special column type
    /// </summary>
    public SpecialColumnType SpecialType { get; set; } = SpecialColumnType.None;

    /// <summary>
    /// Gets or sets custom properties for the column
    /// </summary>
    public Dictionary<string, object?> CustomProperties { get; set; } = new();

    /// <summary>
    /// Gets whether this column is a special system column
    /// </summary>
    public bool IsSpecialColumn => SpecialType != SpecialColumnType.None;

    /// <summary>
    /// Creates a copy of this column definition
    /// </summary>
    /// <returns>New ColumnDefinition instance with copied properties</returns>
    public ColumnDefinition Clone()
    {
        return new ColumnDefinition
        {
            Name = this.Name,
            Header = this.Header,
            DataType = this.DataType,
            Width = this.Width,
            MinWidth = this.MinWidth,
            MaxWidth = this.MaxWidth,
            IsVisible = this.IsVisible,
            IsReadOnly = this.IsReadOnly,
            IsSortable = this.IsSortable,
            IsFilterable = this.IsFilterable,
            IsResizable = this.IsResizable,
            DisplayOrder = this.DisplayOrder,
            FormatString = this.FormatString,
            DefaultValue = this.DefaultValue,
            SpecialType = this.SpecialType,
            CustomProperties = new Dictionary<string, object?>(this.CustomProperties)
        };
    }

    /// <summary>
    /// Gets a custom property value
    /// </summary>
    /// <typeparam name="T">Type of the property</typeparam>
    /// <param name="key">Property key</param>
    /// <returns>Property value or default if not found</returns>
    public T? GetCustomProperty<T>(string key)
    {
        if (CustomProperties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Sets a custom property value
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    public void SetCustomProperty(string key, object? value)
    {
        CustomProperties[key] = value;
    }

    /// <summary>
    /// Validates the column definition
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(Header) &&
               Width >= MinWidth &&
               Width <= MaxWidth &&
               MinWidth >= 0 &&
               MaxWidth > MinWidth;
    }
}
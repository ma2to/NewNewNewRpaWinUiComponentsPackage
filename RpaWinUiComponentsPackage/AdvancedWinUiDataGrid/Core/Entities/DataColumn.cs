using System;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;

/// <summary>
/// DOMAIN ENTITY: Represents a column definition in the data grid
/// SINGLE RESPONSIBILITY: Column metadata and configuration management
/// </summary>
internal sealed class DataColumn
{
    private string _name;
    private double _width;
    private string _displayName;
    private object? _defaultValue;

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Column name cannot be empty", nameof(value));

            var oldName = _name;
            _name = value;
            NameChanged?.Invoke(this, new ColumnNameChangedEventArgs(oldName, value));
        }
    }

    public string OriginalName { get; }

    /// <summary>
    /// ENTERPRISE: Display name for UI presentation
    /// LOCALIZATION: Can be localized while keeping Name as identifier
    /// </summary>
    public string DisplayName
    {
        get => string.IsNullOrWhiteSpace(_displayName) ? _name : _displayName;
        set => _displayName = value ?? string.Empty;
    }

    /// <summary>
    /// ENTERPRISE: Default value for new cells in this column
    /// VALIDATION: Provides consistent initialization for new data
    /// </summary>
    public object? DefaultValue
    {
        get => _defaultValue;
        set => _defaultValue = value;
    }

    public double Width
    {
        get => _width;
        set
        {
            if (value < MinWidth) value = MinWidth;
            if (MaxWidth.HasValue && value > MaxWidth.Value) value = MaxWidth.Value;

            if (Math.Abs(_width - value) < 0.01) return;

            var oldWidth = _width;
            _width = value;
            WidthChanged?.Invoke(this, new ColumnWidthChangedEventArgs(oldWidth, value));
        }
    }

    public double MinWidth { get; set; } = 50;
    public double? MaxWidth { get; set; }
    public Type DataType { get; set; } = typeof(string);
    public SpecialColumnType SpecialType { get; set; } = SpecialColumnType.None;
    public int DisplayOrder { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsResizable { get; set; } = true;
    public bool IsSortable { get; set; } = true;

    public bool IsSpecialColumn => SpecialType != SpecialColumnType.None;

    public event EventHandler<ColumnNameChangedEventArgs>? NameChanged;
    public event EventHandler<ColumnWidthChangedEventArgs>? WidthChanged;

    public DataColumn(string name, double initialWidth = 100)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Column name cannot be empty", nameof(name));

        _name = name;
        OriginalName = name;
        _displayName = name; // Initialize display name to same as name
        _width = Math.Max(initialWidth, MinWidth);
    }

    /// <summary>
    /// ENTERPRISE: Create column with automatic name collision resolution
    /// </summary>
    public static DataColumn CreateWithUniqueNameResolution(string desiredName, Func<string, bool> nameExistsChecker, double initialWidth = 100)
    {
        var uniqueName = GenerateUniqueName(desiredName, nameExistsChecker);
        return new DataColumn(uniqueName, initialWidth);
    }

    /// <summary>
    /// ENTERPRISE: Generate unique column name with _1, _2, etc. suffix
    /// </summary>
    private static string GenerateUniqueName(string baseName, Func<string, bool> nameExists)
    {
        if (!nameExists(baseName))
            return baseName;

        var counter = 1;
        string candidateName;

        do
        {
            candidateName = $"{baseName}_{counter}";
            counter++;
        } while (nameExists(candidateName));

        return candidateName;
    }

    /// <summary>
    /// ENTERPRISE: Reset width to auto-fit content
    /// </summary>
    public void AutoFitWidth(double calculatedWidth)
    {
        Width = Math.Max(calculatedWidth, MinWidth);
    }

    /// <summary>
    /// CONFIGURATION: Configure as special column type
    /// </summary>
    public void ConfigureAsSpecialColumn(SpecialColumnType specialType, double? specialWidth = null)
    {
        SpecialType = specialType;

        if (specialWidth.HasValue)
        {
            Width = specialWidth.Value;
        }
        else
        {
            // Set default widths for special columns
            Width = specialType switch
            {
                SpecialColumnType.CheckBox => 50,
                SpecialColumnType.DeleteRow => 80,
                SpecialColumnType.ValidAlerts => 200,
                SpecialColumnType.RowNumber => 60,
                _ => Width
            };
        }

        // Special columns have specific behavior
        switch (specialType)
        {
            case SpecialColumnType.CheckBox:
                DataType = typeof(bool);
                break;
            case SpecialColumnType.DeleteRow:
                IsReadOnly = true;
                IsSortable = false;
                break;
            case SpecialColumnType.ValidAlerts:
                IsReadOnly = true;
                IsSortable = false;
                DataType = typeof(string);
                break;
            case SpecialColumnType.RowNumber:
                IsReadOnly = true;
                IsSortable = false;
                IsResizable = false;
                DataType = typeof(int);
                break;
        }
    }

    /// <summary>
    /// ENTERPRISE: Rename column with validation and event notification
    /// LOGGING: Operation is logged for audit purposes
    /// </summary>
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Column name cannot be empty", nameof(newName));

        if (newName == _name)
            return; // No change needed

        var oldName = _name;
        _name = newName;
        NameChanged?.Invoke(this, new ColumnNameChangedEventArgs(oldName, newName));
    }

    /// <summary>
    /// ENTERPRISE: Update display name for UI presentation
    /// LOCALIZATION: Allows localized display names while preserving logical name
    /// </summary>
    public void UpdateDisplayName(string newDisplayName)
    {
        if (newDisplayName == null)
            throw new ArgumentNullException(nameof(newDisplayName));

        _displayName = newDisplayName;
    }

    /// <summary>
    /// ENTERPRISE: Set default value for new cells in this column
    /// VALIDATION: Ensures type compatibility and consistent data initialization
    /// </summary>
    public void SetDefaultValue(object? defaultValue)
    {
        // ENTERPRISE: Type validation for data integrity
        if (defaultValue != null && !DataType.IsAssignableFrom(defaultValue.GetType()))
        {
            // Try conversion for common types
            try
            {
                defaultValue = Convert.ChangeType(defaultValue, DataType);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException($"Default value type {defaultValue.GetType().Name} is not compatible with column data type {DataType.Name}", nameof(defaultValue));
            }
        }

        _defaultValue = defaultValue;
    }

    public override string ToString()
    {
        var special = IsSpecialColumn ? $" ({SpecialType})" : "";
        return $"Column[{Name}]: {Width}px{special}";
    }
}

/// <summary>
/// EVENT ARGS: Column name change notification
/// </summary>
internal sealed class ColumnNameChangedEventArgs : EventArgs
{
    public string OldName { get; }
    public string NewName { get; }

    public ColumnNameChangedEventArgs(string oldName, string newName)
    {
        OldName = oldName;
        NewName = newName;
    }
}

/// <summary>
/// EVENT ARGS: Column width change notification
/// </summary>
internal sealed class ColumnWidthChangedEventArgs : EventArgs
{
    public double OldWidth { get; }
    public double NewWidth { get; }

    public ColumnWidthChangedEventArgs(double oldWidth, double newWidth)
    {
        OldWidth = oldWidth;
        NewWidth = newWidth;
    }
}
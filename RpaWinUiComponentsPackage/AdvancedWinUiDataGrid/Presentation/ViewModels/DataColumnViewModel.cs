using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Windows.UI;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Presentation.ViewModels;

/// <summary>
/// PRESENTATION: ViewModel for data column with configuration and styling support
/// MVVM PATTERN: Wraps DataColumn entity for UI binding
/// RESIZING: Interactive column width management with drag and drop support
/// </summary>
internal sealed class DataColumnViewModel : INotifyPropertyChanged, IDisposable
{
    #region Private Fields

    private readonly DataColumn _dataColumn;
    private readonly IDataGridLogger _logger;
    private ColorConfiguration _colorConfiguration;
    private bool _disposed;

    #endregion

    #region Properties

    /// <summary>Column name (current, may be renamed)</summary>
    public string Name => _dataColumn.Name;

    /// <summary>Original column name (immutable)</summary>
    public string OriginalName => _dataColumn.OriginalName;

    /// <summary>Column display name for UI</summary>
    public string DisplayName => _dataColumn.DisplayName;

    /// <summary>Column width in pixels</summary>
    public double Width
    {
        get => _dataColumn.Width;
        set
        {
            if (Math.Abs(_dataColumn.Width - value) > 0.1)
            {
                var oldWidth = _dataColumn.Width;
                _dataColumn.Width = value;
                OnPropertyChanged();
                _logger.LogInformation("VIEWMODEL: Column '{ColumnName}' width changed from {OldWidth}px to {NewWidth}px",
                    Name, oldWidth, value);
            }
        }
    }

    /// <summary>Minimum allowed column width</summary>
    public double MinWidth => _dataColumn.MinWidth;

    /// <summary>Maximum allowed column width</summary>
    public double MaxWidth => _dataColumn.MaxWidth ?? double.MaxValue;

    /// <summary>Default value for new cells in this column</summary>
    public object? DefaultValue => _dataColumn.DefaultValue;

    /// <summary>Column data type</summary>
    public Type DataType => _dataColumn.DataType;

    /// <summary>Special column type</summary>
    public ColumnSpecialType SpecialType => (ColumnSpecialType)_dataColumn.SpecialType;

    /// <summary>Display order in the grid</summary>
    public int DisplayOrder
    {
        get => _dataColumn.DisplayOrder;
        set
        {
            if (_dataColumn.DisplayOrder != value)
            {
                var oldOrder = _dataColumn.DisplayOrder;
                _dataColumn.DisplayOrder = value;
                OnPropertyChanged();
                _logger.LogInformation("VIEWMODEL: Column '{ColumnName}' display order changed from {OldOrder} to {NewOrder}",
                    Name, oldOrder, value);
            }
        }
    }

    /// <summary>Indicates if column is visible</summary>
    public bool IsVisible
    {
        get => _dataColumn.IsVisible;
        set
        {
            if (_dataColumn.IsVisible != value)
            {
                _dataColumn.IsVisible = value;
                OnPropertyChanged();
                _logger.LogInformation("VIEWMODEL: Column '{ColumnName}' visibility changed to {IsVisible}",
                    Name, value);
            }
        }
    }

    /// <summary>Indicates if column is sortable</summary>
    public bool IsSortable => _dataColumn.IsSortable;

    /// <summary>Indicates if column is resizable</summary>
    public bool IsResizable => _dataColumn.IsResizable;

    /// <summary>Indicates if column allows editing</summary>
    public bool IsReadOnly => _dataColumn.IsReadOnly;

    /// <summary>Current color configuration</summary>
    public ColorConfiguration ColorConfiguration
    {
        get => _colorConfiguration;
        set => SetProperty(ref _colorConfiguration, value);
    }

    #endregion

    #region UI Styling Properties

    /// <summary>Header background color</summary>
    public Color HeaderBackgroundColor => ColorConfiguration.HeaderBackgroundColor;

    /// <summary>Header foreground color</summary>
    public Color HeaderForegroundColor => ColorConfiguration.HeaderForegroundColor;

    /// <summary>Header border color</summary>
    public Color HeaderBorderColor => ColorConfiguration.HeaderBorderColor;

    /// <summary>Special column styling based on type</summary>
    public Color SpecialColumnColor
    {
        get
        {
            return SpecialType switch
            {
                ColumnSpecialType.DeleteRow => ColorConfiguration.DeleteButtonBackgroundColor,
                ColumnSpecialType.CheckBox => ColorConfiguration.CheckBoxBorderColor,
                ColumnSpecialType.ValidAlerts => ColorConfiguration.ValidationErrorTextColor,
                ColumnSpecialType.RowNumber => ColorConfiguration.HeaderBackgroundColor,
                _ => ColorConfiguration.GridBackgroundColor
            };
        }
    }

    /// <summary>Column header text alignment</summary>
    public string TextAlignment
    {
        get
        {
            return SpecialType switch
            {
                ColumnSpecialType.RowNumber => "Center",
                ColumnSpecialType.DeleteRow => "Center",
                ColumnSpecialType.CheckBox => "Center",
                ColumnSpecialType.ValidAlerts => "Center",
                _ when DataType == typeof(int) || DataType == typeof(decimal) || DataType == typeof(double) => "Right",
                _ => "Left"
            };
        }
    }

    #endregion

    #region Resizing Support

    /// <summary>Indicates if column is currently being resized</summary>
    private bool _isResizing;
    public bool IsResizing
    {
        get => _isResizing;
        set
        {
            if (SetProperty(ref _isResizing, value))
            {
                _logger.LogInformation("VIEWMODEL: Column '{ColumnName}' resizing state: {IsResizing}",
                    Name, value);
            }
        }
    }

    /// <summary>Start resize operation</summary>
    public void StartResize()
    {
        if (IsResizable)
        {
            IsResizing = true;
            _logger.LogInformation("VIEWMODEL: Started resizing column '{ColumnName}' (current width: {Width}px)",
                Name, Width);
        }
    }

    /// <summary>Update width during resize operation</summary>
    public void UpdateResizeWidth(double newWidth)
    {
        if (IsResizing && IsResizable)
        {
            // Apply constraints
            var constrainedWidth = Math.Max(MinWidth, Math.Min(MaxWidth, newWidth));
            Width = constrainedWidth;
        }
    }

    /// <summary>End resize operation</summary>
    public void EndResize()
    {
        if (IsResizing)
        {
            IsResizing = false;
            _logger.LogInformation("VIEWMODEL: Finished resizing column '{ColumnName}' (final width: {Width}px)",
                Name, Width);
        }
    }

    #endregion

    #region Constructor

    public DataColumnViewModel(DataColumn dataColumn, IDataGridLogger logger)
    {
        _dataColumn = dataColumn ?? throw new ArgumentNullException(nameof(dataColumn));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _colorConfiguration = ColorConfiguration.CreateDefault();

        // Subscribe to column events
        _dataColumn.NameChanged += OnColumnNameChanged;
        _dataColumn.WidthChanged += OnColumnWidthChanged;

        _logger.LogInformation("VIEWMODEL: DataColumnViewModel created for column '{ColumnName}' (Type: {SpecialType}, Width: {Width}px)",
            Name, SpecialType, Width);
    }

    #endregion

    #region Special Column Behavior

    /// <summary>Indicates if this is a standard data column</summary>
    public bool IsDataColumn => SpecialType == ColumnSpecialType.Standard;

    /// <summary>Indicates if this is a checkbox column</summary>
    public bool IsCheckBoxColumn => SpecialType == ColumnSpecialType.CheckBox;

    /// <summary>Indicates if this is a delete row column</summary>
    public bool IsDeleteRowColumn => SpecialType == ColumnSpecialType.DeleteRow;

    /// <summary>Indicates if this is a row number column</summary>
    public bool IsRowNumberColumn => SpecialType == ColumnSpecialType.RowNumber;

    /// <summary>Indicates if this is a validation alerts column</summary>
    public bool IsValidationAlertsColumn => SpecialType == ColumnSpecialType.ValidAlerts;

    /// <summary>Get column header display text</summary>
    public string HeaderText
    {
        get
        {
            return SpecialType switch
            {
                ColumnSpecialType.CheckBox => "☑",
                ColumnSpecialType.DeleteRow => "🗑",
                ColumnSpecialType.RowNumber => "#",
                ColumnSpecialType.ValidAlerts => "⚠",
                _ => DisplayName
            };
        }
    }

    /// <summary>Get column header tooltip</summary>
    public string HeaderTooltip
    {
        get
        {
            return SpecialType switch
            {
                ColumnSpecialType.CheckBox => "Select/Deselect rows",
                ColumnSpecialType.DeleteRow => "Delete row",
                ColumnSpecialType.RowNumber => "Row number",
                ColumnSpecialType.ValidAlerts => "Validation alerts",
                _ => $"{DisplayName} ({DataType.Name})"
            };
        }
    }

    #endregion

    #region Data Operations

    /// <summary>Rename column</summary>
    public void Rename(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName) && newName != Name)
        {
            var oldName = Name;
            _dataColumn.Rename(newName);
            _logger.LogInformation("VIEWMODEL: Column renamed from '{OldName}' to '{NewName}'", oldName, newName);
        }
    }

    /// <summary>Update display name</summary>
    public void UpdateDisplayName(string newDisplayName)
    {
        if (!string.IsNullOrWhiteSpace(newDisplayName) && newDisplayName != DisplayName)
        {
            var oldDisplayName = DisplayName;
            _dataColumn.UpdateDisplayName(newDisplayName);
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(HeaderText));
            _logger.LogInformation("VIEWMODEL: Column '{ColumnName}' display name changed from '{OldDisplayName}' to '{NewDisplayName}'",
                Name, oldDisplayName, newDisplayName);
        }
    }

    /// <summary>Set default value for new cells</summary>
    public void SetDefaultValue(object? defaultValue)
    {
        var oldDefault = DefaultValue;
        _dataColumn.SetDefaultValue(defaultValue);
        OnPropertyChanged(nameof(DefaultValue));
        _logger.LogInformation("VIEWMODEL: Column '{ColumnName}' default value changed from '{OldDefault}' to '{NewDefault}'",
            Name, oldDefault?.ToString() ?? "null", defaultValue?.ToString() ?? "null");
    }

    #endregion

    #region Event Handlers

    private void OnColumnNameChanged(object? sender, ColumnNameChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(HeaderText));
        _logger.LogInformation("VIEWMODEL: Column name changed event received: '{OldName}' -> '{NewName}'",
            e.OldName, e.NewName);
    }

    private void OnColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Width));
        _logger.LogInformation("VIEWMODEL: Column width changed event received: {OldWidth}px -> {NewWidth}px",
            e.OldWidth, e.NewWidth);
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _logger.LogInformation("VIEWMODEL: Disposing DataColumnViewModel for column '{ColumnName}'", Name);

            // Unsubscribe from events
            _dataColumn.NameChanged -= OnColumnNameChanged;
            _dataColumn.WidthChanged -= OnColumnWidthChanged;

            _logger.LogInformation("VIEWMODEL: DataColumnViewModel for column '{ColumnName}' disposed successfully", Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "VIEWMODEL: Error during DataColumnViewModel disposal for column '{ColumnName}'", Name);
        }
        finally
        {
            _disposed = true;
        }
    }

    #endregion
}
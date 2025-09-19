using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Windows.UI;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Presentation.ViewModels;

/// <summary>
/// PRESENTATION: ViewModel for individual cell with validation and styling support
/// MVVM PATTERN: Wraps Cell entity for UI binding with color and validation feedback
/// VALIDATION: Real-time validation result display with severity-based styling
/// </summary>
internal sealed class CellViewModel : INotifyPropertyChanged, IDisposable
{
    #region Private Fields

    private readonly Cell _cell;
    private readonly IDataGridLogger _logger;
    private ColorConfiguration _colorConfiguration;
    private bool _disposed;

    #endregion

    #region Properties

    /// <summary>Cell address (row and column position)</summary>
    public CellAddress Address => _cell.Address;

    /// <summary>Column name this cell belongs to</summary>
    public string ColumnName => _cell.ColumnName;

    /// <summary>Current cell value</summary>
    public object? Value
    {
        get => _cell.Value;
        set
        {
            if (!Equals(_cell.Value, value))
            {
                var oldValue = _cell.Value;
                _cell.Value = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayValue));
                OnPropertyChanged(nameof(HasUnsavedChanges));

                _logger.LogInformation("VIEWMODEL: Cell [{Row}, {Column}] value changed from '{OldValue}' to '{NewValue}'",
                    Address.Row, ColumnName, oldValue?.ToString() ?? "null", value?.ToString() ?? "null");
            }
        }
    }

    /// <summary>Formatted display value for UI</summary>
    public string DisplayValue
    {
        get
        {
            if (_cell.Value == null) return string.Empty;
            if (_cell.Value is DateTime dateTime) return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            if (_cell.Value is decimal decimalValue) return decimalValue.ToString("F2");
            if (_cell.Value is double doubleValue) return doubleValue.ToString("F2");
            if (_cell.Value is bool boolValue) return boolValue ? "✓" : "✗";
            return _cell.Value.ToString() ?? string.Empty;
        }
    }

    /// <summary>Original value before any modifications</summary>
    public object? OriginalValue => _cell.OriginalValue;

    /// <summary>Indicates if cell has unsaved changes</summary>
    public bool HasUnsavedChanges => _cell.HasUnsavedChanges;

    /// <summary>Indicates if cell is empty</summary>
    public bool IsEmpty => _cell.IsEmpty;

    /// <summary>Indicates if cell is read-only</summary>
    public bool IsReadOnly => _cell.IsReadOnly;

    /// <summary>Indicates if cell has validation errors</summary>
    public bool HasValidationErrors => _cell.HasValidationErrors;

    /// <summary>Validation results for this cell</summary>
    public IReadOnlyList<ValidationResult> ValidationResults => _cell.ValidationResults;

    /// <summary>Highest validation severity for this cell</summary>
    public ValidationSeverity HighestSeverity => _cell.GetHighestSeverity();

    /// <summary>Validation error messages combined</summary>
    public string ValidationErrorMessages
    {
        get
        {
            var errorMessages = ValidationResults
                .Where(r => !r.IsValid)
                .Select(r => r.ErrorMessage)
                .Where(msg => !string.IsNullOrEmpty(msg));

            return string.Join(Environment.NewLine, errorMessages);
        }
    }

    /// <summary>Current color configuration</summary>
    public ColorConfiguration ColorConfiguration
    {
        get => _colorConfiguration;
        set => SetProperty(ref _colorConfiguration, value);
    }

    #endregion

    #region UI Styling Properties

    /// <summary>Background color based on cell state and validation</summary>
    public Color BackgroundColor
    {
        get
        {
            if (HasValidationErrors)
            {
                return HighestSeverity switch
                {
                    ValidationSeverity.Error => ColorConfiguration.CellErrorBackgroundColor,
                    ValidationSeverity.Warning => Colors.LightYellow,
                    _ => ColorConfiguration.GridBackgroundColor
                };
            }

            if (HasUnsavedChanges)
                return ColorConfiguration.CellEditingBackgroundColor;

            if (IsSelected)
                return ColorConfiguration.CellSelectedBackgroundColor;

            return ColorConfiguration.GridBackgroundColor;
        }
    }

    /// <summary>Border color based on cell state and validation</summary>
    public Color BorderColor
    {
        get
        {
            if (HasValidationErrors)
            {
                return HighestSeverity switch
                {
                    ValidationSeverity.Error => ColorConfiguration.CellErrorBorderColor,
                    ValidationSeverity.Warning => Colors.Orange,
                    _ => ColorConfiguration.GridBorderColor
                };
            }

            if (IsSelected)
                return ColorConfiguration.CellSelectedBorderColor;

            return ColorConfiguration.GridBorderColor;
        }
    }

    /// <summary>Text color based on validation severity</summary>
    public Color ForegroundColor
    {
        get
        {
            if (HasValidationErrors)
            {
                return HighestSeverity switch
                {
                    ValidationSeverity.Error => ColorConfiguration.ValidationErrorTextColor,
                    ValidationSeverity.Warning => ColorConfiguration.ValidationWarningTextColor,
                    ValidationSeverity.Info => ColorConfiguration.ValidationInfoTextColor,
                    _ => Colors.Black
                };
            }

            return Colors.Black;
        }
    }

    /// <summary>Indicates if this cell is currently selected</summary>
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(BorderColor));
            }
        }
    }

    /// <summary>Indicates if this cell is currently being edited</summary>
    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                _logger.LogInformation("VIEWMODEL: Cell [{Row}, {Column}] editing state: {IsEditing}",
                    Address.Row, ColumnName, value);
            }
        }
    }

    #endregion

    #region Constructor

    public CellViewModel(Cell cell, IDataGridLogger logger)
    {
        _cell = cell ?? throw new ArgumentNullException(nameof(cell));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _colorConfiguration = ColorConfiguration.CreateDefault();

        // Subscribe to cell events
        _cell.ValueChanged += OnCellValueChanged;

        _logger.LogInformation("VIEWMODEL: CellViewModel created for [{Row}, {Column}]",
            Address.Row, ColumnName);
    }

    #endregion

    #region Data Operations

    /// <summary>Commit changes for this cell</summary>
    public void CommitChanges()
    {
        _cell.CommitChanges();
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(BackgroundColor));

        _logger.LogInformation("VIEWMODEL: Changes committed for cell [{Row}, {Column}]",
            Address.Row, ColumnName);
    }

    /// <summary>Revert changes for this cell</summary>
    public void RevertChanges()
    {
        _cell.RevertChanges();
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(BackgroundColor));

        _logger.LogInformation("VIEWMODEL: Changes reverted for cell [{Row}, {Column}]",
            Address.Row, ColumnName);
    }

    /// <summary>Clear all validation results</summary>
    public void ClearValidationResults()
    {
        _cell.ClearValidationResults();
        OnPropertyChanged(nameof(HasValidationErrors));
        OnPropertyChanged(nameof(ValidationResults));
        OnPropertyChanged(nameof(ValidationErrorMessages));
        OnPropertyChanged(nameof(HighestSeverity));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(BorderColor));
        OnPropertyChanged(nameof(ForegroundColor));

        _logger.LogInformation("VIEWMODEL: Validation results cleared for cell [{Row}, {Column}]",
            Address.Row, ColumnName);
    }

    /// <summary>Add validation result to this cell</summary>
    public void AddValidationResult(ValidationResult validationResult)
    {
        _cell.AddValidationResult(validationResult);
        OnPropertyChanged(nameof(HasValidationErrors));
        OnPropertyChanged(nameof(ValidationResults));
        OnPropertyChanged(nameof(ValidationErrorMessages));
        OnPropertyChanged(nameof(HighestSeverity));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(BorderColor));
        OnPropertyChanged(nameof(ForegroundColor));

        _logger.LogInformation("VIEWMODEL: Validation result added to cell [{Row}, {Column}]: {Severity} - {Message}",
            Address.Row, ColumnName, validationResult.Severity, validationResult.ErrorMessage);
    }

    /// <summary>Refresh ViewModel from underlying cell model</summary>
    public void RefreshFromModel()
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasValidationErrors));
        OnPropertyChanged(nameof(ValidationResults));
        OnPropertyChanged(nameof(ValidationErrorMessages));
        OnPropertyChanged(nameof(HighestSeverity));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(BorderColor));
        OnPropertyChanged(nameof(ForegroundColor));

        _logger.LogInformation("VIEWMODEL: Cell [{Row}, {Column}] refreshed from model",
            Address.Row, ColumnName);
    }

    #endregion

    #region Special Cell Type Support

    /// <summary>Indicates if this is a checkbox cell</summary>
    public bool IsCheckBoxCell => ColumnName == "CheckBox" || _cell.Value is bool;

    /// <summary>Indicates if this is a delete button cell</summary>
    public bool IsDeleteButtonCell => ColumnName == "DeleteRow";

    /// <summary>Indicates if this is a row number cell</summary>
    public bool IsRowNumberCell => ColumnName == "RowNumber";

    /// <summary>Indicates if this is a validation alerts cell</summary>
    public bool IsValidationAlertsCell => ColumnName == "ValidAlerts";

    /// <summary>Get checkbox value (if applicable)</summary>
    public bool CheckBoxValue
    {
        get => _cell.Value is bool boolValue && boolValue;
        set
        {
            if (IsCheckBoxCell)
            {
                Value = value;
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnCellValueChanged(object? sender, CellValueChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(BackgroundColor));

        _logger.LogInformation("VIEWMODEL: Cell [{Row}, {Column}] value changed event received",
            Address.Row, ColumnName);
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
            _logger.LogInformation("VIEWMODEL: Disposing CellViewModel for [{Row}, {Column}]",
                Address.Row, ColumnName);

            // Unsubscribe from events
            _cell.ValueChanged -= OnCellValueChanged;

            _logger.LogInformation("VIEWMODEL: CellViewModel for [{Row}, {Column}] disposed successfully",
                Address.Row, ColumnName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "VIEWMODEL: Error during CellViewModel disposal for [{Row}, {Column}]",
                Address.Row, ColumnName);
        }
        finally
        {
            _disposed = true;
        }
    }

    #endregion
}
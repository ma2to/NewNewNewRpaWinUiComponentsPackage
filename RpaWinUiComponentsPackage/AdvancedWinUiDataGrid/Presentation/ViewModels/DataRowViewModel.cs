using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Presentation.ViewModels;

/// <summary>
/// PRESENTATION: ViewModel for individual data row with cell management
/// MVVM PATTERN: Wraps DataRow entity for UI binding
/// CHANGE TRACKING: Monitors cell-level changes for UI feedback
/// </summary>
internal sealed class DataRowViewModel : INotifyPropertyChanged, IDisposable
{
    #region Private Fields

    private readonly DataRow _dataRow;
    private readonly IDataGridLogger _logger;
    private ColorConfiguration _colorConfiguration;
    private bool _disposed;

    #endregion

    #region Properties

    /// <summary>Row index in the grid</summary>
    public int RowIndex => _dataRow.RowIndex;

    /// <summary>Indicates if row has unsaved changes</summary>
    public bool HasUnsavedChanges => _dataRow.HasUnsavedChanges;

    /// <summary>Indicates if row has validation errors</summary>
    public bool HasValidationErrors => _dataRow.HasValidationErrors;

    /// <summary>Highest validation severity for this row</summary>
    public ValidationSeverity HighestSeverity => _dataRow.GetHighestSeverity();

    /// <summary>Collection of cell ViewModels</summary>
    public ObservableCollection<CellViewModel> Cells { get; } = new();

    /// <summary>Current color configuration</summary>
    public ColorConfiguration ColorConfiguration
    {
        get => _colorConfiguration;
        set
        {
            if (SetProperty(ref _colorConfiguration, value))
            {
                // Apply color configuration to all cells
                foreach (var cell in Cells)
                {
                    cell.ColorConfiguration = value;
                }
            }
        }
    }

    /// <summary>Indicates if this row is selected</summary>
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    #endregion

    #region Constructor

    public DataRowViewModel(DataRow dataRow, IDataGridLogger logger)
    {
        _dataRow = dataRow ?? throw new ArgumentNullException(nameof(dataRow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _colorConfiguration = ColorConfiguration.CreateDefault();

        // Subscribe to data row events
        _dataRow.StateChanged += OnDataRowStateChanged;
        _dataRow.CellValueChanged += OnCellValueChanged;

        // Create cell ViewModels
        InitializeCells();

        _logger.LogInformation("VIEWMODEL: DataRowViewModel created for row {RowIndex} with {CellCount} cells",
            RowIndex, Cells.Count);
    }

    #endregion

    #region Cell Management

    private void InitializeCells()
    {
        Cells.Clear();

        foreach (var cell in _dataRow.Cells.Values)
        {
            var cellViewModel = new CellViewModel(cell, _logger)
            {
                ColorConfiguration = ColorConfiguration
            };
            Cells.Add(cellViewModel);
        }

        _logger.LogInformation("VIEWMODEL: Initialized {CellCount} cell ViewModels for row {RowIndex}",
            Cells.Count, RowIndex);
    }

    /// <summary>Get cell ViewModel by column name</summary>
    public CellViewModel? GetCell(string columnName)
    {
        return Cells.FirstOrDefault(c => c.ColumnName == columnName);
    }

    /// <summary>Get cell value by column name</summary>
    public object? GetCellValue(string columnName)
    {
        return GetCell(columnName)?.Value;
    }

    /// <summary>Set cell value by column name</summary>
    public void SetCellValue(string columnName, object? value)
    {
        var cellViewModel = GetCell(columnName);
        if (cellViewModel != null)
        {
            cellViewModel.Value = value;
            _logger.LogInformation("VIEWMODEL: Cell value set for [{RowIndex}, {ColumnName}] to '{Value}'",
                RowIndex, columnName, value?.ToString() ?? "null");
        }
        else
        {
            _logger.LogWarning("VIEWMODEL: Cell not found for column '{ColumnName}' in row {RowIndex}",
                columnName, RowIndex);
        }
    }

    /// <summary>Add new cell to the row</summary>
    public void AddCell(string columnName, object? initialValue = null)
    {
        try
        {
            // Add cell to data model
            _dataRow.SetCellValue(columnName, initialValue);

            // Create ViewModel for the new cell
            var cell = _dataRow.GetCell(columnName);
            if (cell != null)
            {
                var cellViewModel = new CellViewModel(cell, _logger)
                {
                    ColorConfiguration = ColorConfiguration
                };
                Cells.Add(cellViewModel);

                _logger.LogInformation("VIEWMODEL: Cell added for column '{ColumnName}' in row {RowIndex}",
                    columnName, RowIndex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VIEWMODEL: Error adding cell for column '{ColumnName}' in row {RowIndex}",
                columnName, RowIndex);
        }
    }

    /// <summary>Remove cell from the row</summary>
    public void RemoveCell(string columnName)
    {
        try
        {
            var cellViewModel = GetCell(columnName);
            if (cellViewModel != null)
            {
                Cells.Remove(cellViewModel);
                cellViewModel.Dispose();
            }

            _dataRow.RemoveCell(columnName);

            _logger.LogInformation("VIEWMODEL: Cell removed for column '{ColumnName}' from row {RowIndex}",
                columnName, RowIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VIEWMODEL: Error removing cell for column '{ColumnName}' from row {RowIndex}",
                columnName, RowIndex);
        }
    }

    #endregion

    #region Data Operations

    /// <summary>Commit all changes in this row</summary>
    public void CommitChanges()
    {
        _dataRow.CommitChanges();

        // Update cell ViewModels
        foreach (var cell in Cells)
        {
            cell.RefreshFromModel();
        }

        OnPropertyChanged(nameof(HasUnsavedChanges));
        _logger.LogInformation("VIEWMODEL: Changes committed for row {RowIndex}", RowIndex);
    }

    /// <summary>Revert all changes in this row</summary>
    public void RevertChanges()
    {
        _dataRow.RevertChanges();

        // Update cell ViewModels
        foreach (var cell in Cells)
        {
            cell.RefreshFromModel();
        }

        OnPropertyChanged(nameof(HasUnsavedChanges));
        _logger.LogInformation("VIEWMODEL: Changes reverted for row {RowIndex}", RowIndex);
    }

    /// <summary>Refresh ViewModel from underlying data model</summary>
    public void RefreshFromModel()
    {
        // Refresh all cell ViewModels
        foreach (var cell in Cells)
        {
            cell.RefreshFromModel();
        }

        // Notify property changes
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(HasValidationErrors));
        OnPropertyChanged(nameof(HighestSeverity));

        _logger.LogInformation("VIEWMODEL: Row {RowIndex} refreshed from model", RowIndex);
    }

    #endregion

    #region Event Handlers

    private void OnDataRowStateChanged(object? sender, RowStateChangedEventArgs e)
    {
        // Update UI properties based on state changes
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(HasValidationErrors));
        OnPropertyChanged(nameof(HighestSeverity));

        _logger.LogInformation("VIEWMODEL: Row {RowIndex} state changed: {ChangeType}",
            RowIndex, e.ChangeType);
    }

    private void OnCellValueChanged(object? sender, CellValueChangedEventArgs e)
    {
        // Find the corresponding cell ViewModel and update it
        var cellViewModel = GetCell(e.ColumnName);
        if (cellViewModel != null)
        {
            cellViewModel.RefreshFromModel();
        }

        _logger.LogInformation("VIEWMODEL: Cell value changed in row {RowIndex}, column '{ColumnName}'",
            RowIndex, e.ColumnName);
    }

    #endregion

    #region Special Column Support

    /// <summary>Indicates if this row should show delete button</summary>
    public bool ShowDeleteButton => true; // Can be made configurable

    /// <summary>Indicates if row has checkbox column</summary>
    public bool HasCheckBox => Cells.Any(c => c.ColumnName == "CheckBox");

    /// <summary>Get checkbox value if exists</summary>
    public bool? CheckBoxValue
    {
        get
        {
            var checkBoxCell = GetCell("CheckBox");
            if (checkBoxCell?.Value is bool boolValue)
                return boolValue;
            return null;
        }
        set
        {
            var checkBoxCell = GetCell("CheckBox");
            if (checkBoxCell != null)
            {
                checkBoxCell.Value = value ?? false;
            }
        }
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
            _logger.LogInformation("VIEWMODEL: Disposing DataRowViewModel for row {RowIndex}", RowIndex);

            // Unsubscribe from events
            _dataRow.StateChanged -= OnDataRowStateChanged;
            _dataRow.CellValueChanged -= OnCellValueChanged;

            // Dispose cell ViewModels
            foreach (var cell in Cells)
            {
                cell.Dispose();
            }
            Cells.Clear();

            _logger.LogInformation("VIEWMODEL: DataRowViewModel for row {RowIndex} disposed successfully", RowIndex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "VIEWMODEL: Error during DataRowViewModel disposal for row {RowIndex}", RowIndex);
        }
        finally
        {
            _disposed = true;
        }
    }

    #endregion
}
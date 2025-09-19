using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Presentation.ViewModels;

/// <summary>
/// PRESENTATION: Main ViewModel for AdvancedDataGrid UI components
/// MVVM PATTERN: Coordinates between UI and Application layer
/// PERFORMANCE: Optimized for large datasets with virtualization support
/// </summary>
internal sealed class DataGridViewModel : INotifyPropertyChanged, IDisposable
{
    #region Private Fields

    private readonly IDataGridLogger _logger;
    private readonly IDataGridRepository _repository;
    private readonly IValidationService _validationService;
    private DataGridOperationMode _operationMode;
    private ColorConfiguration _colorConfiguration;
    private bool _disposed;

    #endregion

    #region Observable Collections

    /// <summary>Observable collection of data rows for UI binding</summary>
    public ObservableCollection<DataRowViewModel> Rows { get; } = new();

    /// <summary>Observable collection of column configurations for UI binding</summary>
    public ObservableCollection<DataColumnViewModel> Columns { get; } = new();

    #endregion

    #region Properties

    private bool _isLoading;
    /// <summary>Indicates if data operations are in progress</summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                _logger.LogInformation("UI: Loading state changed to {IsLoading}", value);
            }
        }
    }

    private string _statusMessage = "Ready";
    /// <summary>Status message for user feedback</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private int _selectedRowIndex = -1;
    /// <summary>Currently selected row index</summary>
    public int SelectedRowIndex
    {
        get => _selectedRowIndex;
        set
        {
            if (SetProperty(ref _selectedRowIndex, value))
            {
                OnSelectedRowChanged();
            }
        }
    }

    private string _selectedColumnName = string.Empty;
    /// <summary>Currently selected column name</summary>
    public string SelectedColumnName
    {
        get => _selectedColumnName;
        set
        {
            if (SetProperty(ref _selectedColumnName, value))
            {
                OnSelectedColumnChanged();
            }
        }
    }

    /// <summary>Current operation mode</summary>
    public DataGridOperationMode OperationMode
    {
        get => _operationMode;
        set
        {
            if (SetProperty(ref _operationMode, value))
            {
                _logger.LogInformation("UI: Operation mode changed to {Mode}", value);
                OnOperationModeChanged();
            }
        }
    }

    /// <summary>Current color configuration</summary>
    public ColorConfiguration ColorConfiguration
    {
        get => _colorConfiguration;
        set
        {
            if (SetProperty(ref _colorConfiguration, value))
            {
                _logger.LogInformation("UI: Color configuration updated");
                OnColorConfigurationChanged();
            }
        }
    }

    #endregion

    #region Commands

    /// <summary>Command to add new row</summary>
    public ICommand AddRowCommand { get; }

    /// <summary>Command to delete selected row</summary>
    public ICommand DeleteRowCommand { get; }

    /// <summary>Command to refresh UI manually (Headless mode)</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Command to commit all changes</summary>
    public ICommand CommitChangesCommand { get; }

    /// <summary>Command to revert all changes</summary>
    public ICommand RevertChangesCommand { get; }

    #endregion

    #region Constructor

    public DataGridViewModel(
        IDataGridRepository repository,
        IValidationService validationService,
        IDataGridLogger logger,
        DataGridOperationMode operationMode = DataGridOperationMode.UI)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationMode = operationMode;
        _colorConfiguration = ColorConfiguration.CreateDefault();

        // Initialize commands
        AddRowCommand = new RelayCommand(async () => await AddRowAsync(), () => !IsLoading);
        DeleteRowCommand = new RelayCommand(async () => await DeleteSelectedRowAsync(),
            () => !IsLoading && SelectedRowIndex >= 0);
        RefreshCommand = new RelayCommand(async () => await RefreshDataAsync(), () => !IsLoading);
        CommitChangesCommand = new RelayCommand(async () => await CommitChangesAsync(), () => !IsLoading);
        RevertChangesCommand = new RelayCommand(async () => await RevertChangesAsync(), () => !IsLoading);

        _logger.LogInformation("VIEWMODEL: DataGridViewModel initialized in {Mode} mode", operationMode);

        // Load initial data
        _ = Task.Run(LoadDataAsync);
    }

    #endregion

    #region Data Operations

    /// <summary>Load data from repository and populate ViewModels</summary>
    public async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading data...";

            _logger.LogInformation("VIEWMODEL: Starting data load operation");

            // Load columns first
            var columnsResult = _repository.GetAllColumns();
            if (columnsResult.IsSuccess)
            {
                Columns.Clear();
                foreach (var column in columnsResult.Value)
                {
                    Columns.Add(new DataColumnViewModel(column, _logger));
                }
            }

            // Load rows
            var rowsResult = _repository.GetAllRows();
            if (rowsResult.IsSuccess)
            {
                Rows.Clear();
                foreach (var row in rowsResult.Value)
                {
                    Rows.Add(new DataRowViewModel(row, _logger));
                }
            }

            StatusMessage = $"Loaded {Rows.Count} rows, {Columns.Count} columns";
            _logger.LogInformation("VIEWMODEL: Data loaded successfully - {RowCount} rows, {ColumnCount} columns",
                Rows.Count, Columns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VIEWMODEL: Error loading data");
            StatusMessage = "Error loading data";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Add new row to the grid</summary>
    public async Task AddRowAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Adding new row...";

            var newRow = new DataRow(Rows.Count);

            // Add default cells for all columns
            foreach (var column in Columns)
            {
                newRow.SetCellValue(column.Name, column.DefaultValue);
            }

            var result = _repository.AddRow(newRow);
            if (result.IsSuccess)
            {
                var rowViewModel = new DataRowViewModel(newRow, _logger);
                Rows.Add(rowViewModel);

                StatusMessage = $"Row {newRow.RowIndex} added successfully";
                _logger.LogInformation("VIEWMODEL: New row added at index {RowIndex}", newRow.RowIndex);
            }
            else
            {
                StatusMessage = $"Failed to add row: {result.Error}";
                _logger.LogWarning("VIEWMODEL: Failed to add row: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VIEWMODEL: Error adding new row");
            StatusMessage = "Error adding row";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Delete currently selected row</summary>
    public async Task DeleteSelectedRowAsync()
    {
        if (SelectedRowIndex < 0 || SelectedRowIndex >= Rows.Count)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Deleting row {SelectedRowIndex}...";

            var result = _repository.RemoveRow(SelectedRowIndex);
            if (result.IsSuccess)
            {
                Rows.RemoveAt(SelectedRowIndex);
                SelectedRowIndex = -1;

                StatusMessage = "Row deleted successfully";
                _logger.LogInformation("VIEWMODEL: Row deleted at index {RowIndex}", SelectedRowIndex);
            }
            else
            {
                StatusMessage = $"Failed to delete row: {result.Error}";
                _logger.LogWarning("VIEWMODEL: Failed to delete row: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VIEWMODEL: Error deleting row {RowIndex}", SelectedRowIndex);
            StatusMessage = "Error deleting row";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Refresh data from repository (manual refresh for Headless mode)</summary>
    public async Task RefreshDataAsync()
    {
        await LoadDataAsync();
    }

    /// <summary>Commit all pending changes</summary>
    public async Task CommitChangesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Committing changes...";

            var result = _repository.CommitChanges();
            if (result.IsSuccess)
            {
                // Update ViewModels to reflect committed state
                foreach (var row in Rows)
                {
                    row.RefreshFromModel();
                }

                StatusMessage = "Changes committed successfully";
                _logger.LogInformation("VIEWMODEL: All changes committed successfully");
            }
            else
            {
                StatusMessage = $"Failed to commit changes: {result.Error}";
                _logger.LogWarning("VIEWMODEL: Failed to commit changes: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VIEWMODEL: Error committing changes");
            StatusMessage = "Error committing changes";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Revert all pending changes</summary>
    public async Task RevertChangesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Reverting changes...";

            var result = _repository.RevertChanges();
            if (result.IsSuccess)
            {
                // Update ViewModels to reflect reverted state
                foreach (var row in Rows)
                {
                    row.RefreshFromModel();
                }

                StatusMessage = "Changes reverted successfully";
                _logger.LogInformation("VIEWMODEL: All changes reverted successfully");
            }
            else
            {
                StatusMessage = $"Failed to revert changes: {result.Error}";
                _logger.LogWarning("VIEWMODEL: Failed to revert changes: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VIEWMODEL: Error reverting changes");
            StatusMessage = "Error reverting changes";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Event Handlers

    private void OnSelectedRowChanged()
    {
        _logger.LogInformation("VIEWMODEL: Selected row changed to {RowIndex}", SelectedRowIndex);
        // Update command can execute states
        ((RelayCommand)DeleteRowCommand).RaiseCanExecuteChanged();
    }

    private void OnSelectedColumnChanged()
    {
        _logger.LogInformation("VIEWMODEL: Selected column changed to {ColumnName}", SelectedColumnName);
    }

    private void OnOperationModeChanged()
    {
        // Adjust UI behavior based on operation mode
        if (OperationMode == DataGridOperationMode.Headless)
        {
            _logger.LogInformation("VIEWMODEL: Switched to Headless mode - manual updates required");
        }
        else
        {
            _logger.LogInformation("VIEWMODEL: Switched to UI mode - automatic updates enabled");
        }
    }

    private void OnColorConfigurationChanged()
    {
        // Notify all child ViewModels of color changes
        foreach (var row in Rows)
        {
            row.ColorConfiguration = ColorConfiguration;
        }

        foreach (var column in Columns)
        {
            column.ColorConfiguration = ColorConfiguration;
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
            _logger.LogInformation("VIEWMODEL: Disposing DataGridViewModel");

            // Dispose child ViewModels
            foreach (var row in Rows)
            {
                row.Dispose();
            }
            Rows.Clear();

            foreach (var column in Columns)
            {
                column.Dispose();
            }
            Columns.Clear();

            _logger.LogInformation("VIEWMODEL: DataGridViewModel disposed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "VIEWMODEL: Error during DataGridViewModel disposal");
        }
        finally
        {
            _disposed = true;
        }
    }

    #endregion
}

#region Helper Command Implementation

/// <summary>Simple RelayCommand implementation for MVVM</summary>
internal sealed class RelayCommand : ICommand
{
    private readonly Func<Task> _executeAsync;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute ?? (() => true);
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute();

    public async void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            await _executeAsync();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

#endregion
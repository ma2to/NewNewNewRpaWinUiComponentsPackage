using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;

/// <summary>
/// Data grid cells view that displays rows and cells in a scrollable area.
/// This is the main content area of the grid where data is displayed.
/// Handles cell selection (single, multi-select with Ctrl, range selection with drag).
/// Uses Grid layout with ColumnDefinitions synchronized with headers and filters for proper column alignment.
/// MEMORY LEAK FIX: Implements proper event handler cleanup to prevent duplicate subscriptions.
/// </summary>
public sealed class DataGridCellsView : UserControl, IDisposable
{
    private readonly DataGridViewModel _viewModel;

    private readonly ScrollViewer _scrollViewer;
    private readonly StackPanel _rowsPanel;
    private bool _isMouseDown; // Tracks whether mouse is pressed for drag selection
    private bool _disposed;

    /// <summary>
    /// Holds cleanup actions for a row control to properly unsubscribe event handlers
    /// </summary>
    private sealed class RowControlData
    {
        public DataGridRowViewModel Row { get; }
        public NotifyCollectionChangedEventHandler? CellsChangedHandler { get; set; }
        public List<Action> CleanupActions { get; } = new();

        public RowControlData(DataGridRowViewModel row)
        {
            Row = row;
        }

        public void Cleanup()
        {
            // Unsubscribe from row.Cells collection changes
            if (CellsChangedHandler != null && Row.Cells != null)
            {
                Row.Cells.CollectionChanged -= CellsChangedHandler;
                CellsChangedHandler = null;
            }

            // Execute all cleanup actions (unsubscribe from cell control events)
            foreach (var action in CleanupActions)
            {
                action?.Invoke();
            }
            CleanupActions.Clear();
        }
    }

    /// <summary>
    /// Event fired when user requests to delete a row via delete button.
    /// Contains both rowIndex (for display) and rowId (for stable identification).
    /// </summary>
    public event EventHandler<DeleteRowRequestedEventArgs>? DeleteRowRequested;

    /// <summary>
    /// Event fired when user changes row selection via checkbox.
    /// </summary>
    public event EventHandler<(int rowIndex, bool isSelected)>? RowSelectionChanged;

    /// <summary>
    /// Event fired when user completes editing a cell (presses Enter).
    /// Used to trigger auto-expand when last row is edited.
    /// </summary>
    public event EventHandler<CellViewModel>? CellEditCompleted;

    /// <summary>
    /// Creates a new data grid cells view bound to the specified view model.
    /// Automatically subscribes to row collection changes and column definition changes.
    /// </summary>
    /// <param name="viewModel">The view model that manages the grid's data and state</param>
    /// <exception cref="ArgumentNullException">Thrown when viewModel is null</exception>
    public DataGridCellsView(DataGridViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Create ScrollViewer for scrollable area
        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        // Create vertical StackPanel for rows
        _rowsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Padding = new Thickness(8)
        };

        // Initialize with existing rows
        foreach (var row in _viewModel.Rows)
        {
            _rowsPanel.Children.Add(CreateRowControl(row));
        }

        // Listen for collection changes
        _viewModel.Rows.CollectionChanged += OnRowsCollectionChanged;

        // Listen for column definition changes to rebuild rows
        _viewModel.ColumnDefinitionsChanged += OnColumnDefinitionsChanged;

        // Handle pointer events for range selection
        _scrollViewer.PointerPressed += OnPointerPressed;
        _scrollViewer.PointerReleased += OnPointerReleased;

        // Set panel as ScrollViewer content
        _scrollViewer.Content = _rowsPanel;

        // Set ScrollViewer as UserControl content
        Content = _scrollViewer;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isMouseDown = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isMouseDown)
        {
            _isMouseDown = false;
            _viewModel.EndRangeSelection();
        }
    }

    private void OnColumnDefinitionsChanged(object? sender, EventArgs e)
    {
        // Rebuild all rows when column definitions change
        RebuildAllRows();
    }

    private void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (DataGridRowViewModel row in e.NewItems)
            {
                _rowsPanel.Children.Add(CreateRowControl(row));
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (DataGridRowViewModel row in e.OldItems)
            {
                var controlToRemove = _rowsPanel.Children
                    .OfType<Grid>()
                    .FirstOrDefault(g => g.DataContext == row);
                if (controlToRemove != null)
                {
                    // MEMORY LEAK FIX: Cleanup event handlers before removing
                    CleanupRowControl(controlToRemove);
                    _rowsPanel.Children.Remove(controlToRemove);
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // CRITICAL FIX: Reset means "everything changed, rebuild all rows"
            // This is fired by BulkObservableCollection.AddRange() after bulk import
            RebuildAllRows();
        }
    }

    private void RebuildAllRows()
    {
        // MEMORY LEAK FIX: Cleanup all event handlers before clearing
        CleanupAllRows();

        _rowsPanel.Children.Clear();
        foreach (var row in _viewModel.Rows)
        {
            _rowsPanel.Children.Add(CreateRowControl(row));
        }
    }

    /// <summary>
    /// Cleans up all event handlers for all rows to prevent memory leaks
    /// </summary>
    private void CleanupAllRows()
    {
        foreach (var child in _rowsPanel.Children.OfType<Grid>())
        {
            if (child.Tag is RowControlData rowData)
            {
                rowData.Cleanup();
            }
        }
    }

    /// <summary>
    /// Cleans up event handlers for a specific row control
    /// </summary>
    private void CleanupRowControl(Grid rowGrid)
    {
        if (rowGrid?.Tag is RowControlData rowData)
        {
            rowData.Cleanup();
            rowGrid.Tag = null;
        }
    }

    private Grid CreateRowControl(DataGridRowViewModel row)
    {
        // Create Grid for this row with columns matching DataGridViewModel
        var rowGrid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 2),
            DataContext = row
        };

        // MEMORY LEAK FIX: Create cleanup tracking for this row
        var rowData = new RowControlData(row);
        rowGrid.Tag = rowData;

        // Add column definitions from ViewModel
        var definitions = _viewModel.CreateColumnDefinitions();
        foreach (var def in definitions)
        {
            rowGrid.ColumnDefinitions.Add(def);
        }

        // Create cell controls for each cell in the row
        for (int i = 0; i < row.Cells.Count; i++)
        {
            var cell = row.Cells[i];

            // Use SpecialColumnCellControl for special columns, CellControl for normal data columns
            if (cell.IsSpecialColumn)
            {
                var specialControl = new SpecialColumnCellControl(cell);

                // Subscribe to special column events and track for cleanup
                Action<int, bool> selectionHandler = (rowIndex, isSelected) =>
                {
                    HandleRowSelectionChanged(rowIndex, isSelected);
                };
                specialControl.OnRowSelectionChanged += selectionHandler;

                EventHandler<DeleteRowRequestedEventArgs> deleteHandler = (sender, args) =>
                {
                    HandleDeleteRowRequested(args);
                };
                specialControl.OnDeleteRowRequested += deleteHandler;

                // Store cleanup actions
                rowData.CleanupActions.Add(() =>
                {
                    specialControl.OnRowSelectionChanged -= selectionHandler;
                    specialControl.OnDeleteRowRequested -= deleteHandler;
                });

                Grid.SetColumn(specialControl, cell.ColumnIndex);
                rowGrid.Children.Add(specialControl);
            }
            else
            {
                // Normal data cell with editing support
                var normalCellControl = new CellControl(cell);

                // Wire up selection and edit events
                normalCellControl.CellSelected += OnCellSelected;
                normalCellControl.CellPointerEntered += OnCellPointerEntered;
                normalCellControl.CellEditCompleted += OnCellEditCompleted;

                // Store cleanup actions
                rowData.CleanupActions.Add(() =>
                {
                    normalCellControl.CellSelected -= OnCellSelected;
                    normalCellControl.CellPointerEntered -= OnCellPointerEntered;
                    normalCellControl.CellEditCompleted -= OnCellEditCompleted;
                });

                Grid.SetColumn(normalCellControl, cell.ColumnIndex);
                rowGrid.Children.Add(normalCellControl);
            }
        }

        // Listen for changes to the Cells collection and track handler for cleanup
        NotifyCollectionChangedEventHandler cellsHandler = (s, e) => OnCellsCollectionChanged(rowGrid, row.Cells, e);
        row.Cells.CollectionChanged += cellsHandler;
        rowData.CellsChangedHandler = cellsHandler;

        return rowGrid;
    }

    /// <summary>
    /// Handles row selection change from checkbox special column
    /// </summary>
    private void HandleRowSelectionChanged(int rowIndex, bool isSelected)
    {
        // Fire event to notify parent control (AdvancedDataGridControl)
        RowSelectionChanged?.Invoke(this, (rowIndex, isSelected));
    }

    /// <summary>
    /// Handles delete row request from delete button special column
    /// </summary>
    private void HandleDeleteRowRequested(DeleteRowRequestedEventArgs args)
    {
        // Fire event to notify parent control (AdvancedDataGridControl)
        DeleteRowRequested?.Invoke(this, args);
    }

    private void OnCellsCollectionChanged(Grid rowGrid, ObservableCollection<CellViewModel> cells, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            // Get the RowControlData to track new event handlers
            var rowData = rowGrid.Tag as RowControlData;

            foreach (CellViewModel cell in e.NewItems)
            {
                // Use SpecialColumnCellControl for special columns, CellControl for normal data columns
                if (cell.IsSpecialColumn)
                {
                    var specialControl = new SpecialColumnCellControl(cell);

                    // Subscribe to special column events and track for cleanup
                    Action<int, bool> selectionHandler = (rowIndex, isSelected) =>
                    {
                        HandleRowSelectionChanged(rowIndex, isSelected);
                    };
                    specialControl.OnRowSelectionChanged += selectionHandler;

                    EventHandler<DeleteRowRequestedEventArgs> deleteHandler = (sender, args) =>
                    {
                        HandleDeleteRowRequested(args);
                    };
                    specialControl.OnDeleteRowRequested += deleteHandler;

                    // Store cleanup actions
                    rowData?.CleanupActions.Add(() =>
                    {
                        specialControl.OnRowSelectionChanged -= selectionHandler;
                        specialControl.OnDeleteRowRequested -= deleteHandler;
                    });

                    Grid.SetColumn(specialControl, cell.ColumnIndex);
                    rowGrid.Children.Add(specialControl);
                }
                else
                {
                    var normalCellControl = new CellControl(cell);

                    // Wire up selection and edit events
                    normalCellControl.CellSelected += OnCellSelected;
                    normalCellControl.CellPointerEntered += OnCellPointerEntered;
                    normalCellControl.CellEditCompleted += OnCellEditCompleted;

                    // Store cleanup actions
                    rowData?.CleanupActions.Add(() =>
                    {
                        normalCellControl.CellSelected -= OnCellSelected;
                        normalCellControl.CellPointerEntered -= OnCellPointerEntered;
                        normalCellControl.CellEditCompleted -= OnCellEditCompleted;
                    });

                    Grid.SetColumn(normalCellControl, cell.ColumnIndex);
                    rowGrid.Children.Add(normalCellControl);
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (CellViewModel cell in e.OldItems)
            {
                var controlToRemove = rowGrid.Children
                    .OfType<CellControl>()
                    .FirstOrDefault(cc => cc.ViewModel == cell);
                if (controlToRemove != null)
                {
                    // Note: Individual cell cleanup is handled by parent row cleanup
                    rowGrid.Children.Remove(controlToRemove);
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // Note: Cleanup is handled by parent row cleanup when row is removed
            rowGrid.Children.Clear();
        }
    }

    private void OnCellSelected(object? sender, CellSelectionEventArgs e)
    {
        if (_isMouseDown && !e.IsCtrlPressed)
        {
            // Start range selection
            _viewModel.StartRangeSelection(e.Cell);
        }
        else
        {
            // Single or Ctrl+click selection
            _viewModel.SelectCell(e.Cell, e.IsCtrlPressed);
        }
    }

    private void OnCellPointerEntered(object? sender, CellViewModel cell)
    {
        // Handle pointer entered - for range selection
        if (_isMouseDown)
        {
            _viewModel.UpdateRangeSelection(cell);
        }
    }

    private void OnCellEditCompleted(object? sender, CellViewModel cell)
    {
        // Forward cell edit completion to parent control
        // This allows the application layer to trigger auto-expand when last row is edited
        CellEditCompleted?.Invoke(this, cell);
    }

    /// <summary>
    /// Disposes the DataGridCellsView and cleans up all event handlers
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Cleanup all row event handlers
        CleanupAllRows();

        // Unsubscribe from ViewModel events
        if (_viewModel != null)
        {
            _viewModel.Rows.CollectionChanged -= OnRowsCollectionChanged;
            _viewModel.ColumnDefinitionsChanged -= OnColumnDefinitionsChanged;
        }

        // Unsubscribe from ScrollViewer events
        if (_scrollViewer != null)
        {
            _scrollViewer.PointerPressed -= OnPointerPressed;
            _scrollViewer.PointerReleased -= OnPointerReleased;
        }
    }
}

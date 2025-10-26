using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Viewport;

/// <summary>
/// ElementFactory for ItemsRepeater with element recycling.
/// Creates Grid row elements from DataGridRowViewModel and recycles them for memory efficiency.
/// Maintains a recycling pool (max 50 elements) to reduce GC pressure during scrolling.
///
/// PERFORMANCE TARGET:
/// - Element recycling reduces creation overhead by ~80%
/// - Max 50 recycled elements in pool (~5-10 MB overhead)
/// - Proper cleanup prevents memory leaks
/// </summary>
internal sealed class DataGridElementFactory : IElementFactory
{
    private readonly ViewportManager _viewportManager;
    private readonly DataGridViewModel _viewModel;
    private readonly ILogger<DataGridElementFactory> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    // Recycling pool for Grid elements
    private readonly Queue<Grid> _recycledElements = new();
    private const int MAX_RECYCLE_POOL = 50;

    // Event handlers for row operations
    public event Action<int, bool>? OnRowSelectionChanged;
    public event EventHandler<DeleteRowRequestedEventArgs>? OnDeleteRowRequested;
    public event EventHandler<InsertRowRequestedEventArgs>? OnInsertRowRequested;
    public event EventHandler<CellSelectionEventArgs>? OnCellSelected;
    public event EventHandler<CellViewModel>? OnCellEditStarted;
    public event EventHandler<CellViewModel>? OnCellEditCompleted;
    public event EventHandler<CellViewModel>? OnCellPointerEntered;
    public event EventHandler<CellValueChangedEventArgs>? OnCellValueChanged;

    public DataGridElementFactory(
        ViewportManager viewportManager,
        DataGridViewModel viewModel,
        ILogger<DataGridElementFactory> logger,
        ILoggerFactory? loggerFactory = null)
    {
        _viewportManager = viewportManager ?? throw new ArgumentNullException(nameof(viewportManager));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory;

        _logger.LogInformation("DataGridElementFactory created (MAX_RECYCLE_POOL: {MaxPool})", MAX_RECYCLE_POOL);
    }

    /// <summary>
    /// Gets or creates element for specified data item.
    /// Uses recycled Grid if available, otherwise creates new one.
    /// </summary>
    public UIElement GetElement(ElementFactoryGetArgs args)
    {
        try
        {
            // args.Data is the index (int) from ItemsSource
            if (args.Data is not int index)
            {
                _logger.LogWarning("GetElement called with non-int data: {Data}", args.Data);
                return CreatePlaceholderElement();
            }

            _logger.LogTrace("GetElement called for index {Index}", index);

            // Get ViewModel from ViewportManager
            var rowViewModel = _viewportManager.GetRowViewModel(index);
            if (rowViewModel == null)
            {
                _logger.LogWarning("No ViewModel found for index {Index}, returning empty placeholder", index);
                return CreatePlaceholderElement();
            }

            // Try to recycle existing Grid
            Grid rowGrid;
            if (_recycledElements.Count > 0)
            {
                rowGrid = _recycledElements.Dequeue();
                _logger.LogTrace("Recycled Grid element (pool size: {PoolSize})", _recycledElements.Count);
            }
            else
            {
                rowGrid = new Grid
                {
                    Margin = new Thickness(0, 0, 0, 2) // Row spacing
                };
                _logger.LogTrace("Created new Grid element");
            }

            // Configure Grid for this row
            ConfigureRowGrid(rowGrid, rowViewModel);

            return rowGrid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetElement failed: {Message}", ex.Message);
            return CreatePlaceholderElement();
        }
    }

    /// <summary>
    /// Recycles element when it leaves the viewport.
    /// Cleans up event handlers, clears DataContext, and adds to pool if not full.
    /// </summary>
    public void RecycleElement(ElementFactoryRecycleArgs args)
    {
        try
        {
            if (args.Element is not Grid rowGrid)
            {
                _logger.LogWarning("RecycleElement called with non-Grid element: {Type}", args.Element?.GetType().Name ?? "null");
                return;
            }

            _logger.LogTrace("RecycleElement called");

            // STEP 1: Clean up RowControlData (event handlers, etc.)
            if (rowGrid.Tag is RowControlData controlData)
            {
                controlData.Cleanup();
                rowGrid.Tag = null;
            }

            // STEP 2: Clear DataContext
            rowGrid.DataContext = null;

            // STEP 3: Clear all children
            rowGrid.Children.Clear();
            rowGrid.ColumnDefinitions.Clear();

            // STEP 4: Add to recycle pool if not full
            if (_recycledElements.Count < MAX_RECYCLE_POOL)
            {
                _recycledElements.Enqueue(rowGrid);
                _logger.LogTrace("Added Grid to recycle pool (pool size: {PoolSize})", _recycledElements.Count);
            }
            else
            {
                _logger.LogTrace("Recycle pool full, discarding Grid");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecycleElement failed: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Configures Grid element for specified row ViewModel.
    /// Creates column definitions, cell controls, and subscribes to events.
    /// </summary>
    private void ConfigureRowGrid(Grid rowGrid, DataGridRowViewModel rowViewModel)
    {
        // Set DataContext
        rowGrid.DataContext = rowViewModel;

        // Create RowControlData for cleanup tracking
        var controlData = new RowControlData(rowViewModel);
        rowGrid.Tag = controlData;

        // SENIOR FIX: Add column definitions based on ColumnHeaders
        // ValidationAlerts column uses Star width (fills remaining space) with MinWidth constraint
        // All other columns use Pixel width (fixed)
        foreach (var header in _viewModel.ColumnHeaders)
        {
            ColumnDefinition colDef;

            if (header.SpecialType == Common.SpecialColumnType.ValidationAlerts)
            {
                // ValidationAlerts: Star width (fills remaining space after other columns)
                // Min width: header.Width (e.g. 150px) - cannot shrink below this
                // Real width: calculated as remaining space after subtracting other columns
                // NOTE: If user resizes via drag & drop, header.Width updates and GridUnitType switches to Pixel
                colDef = new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star),  // Fills remaining space
                    MinWidth = header.Width  // Cannot go below this (e.g. 150px)
                };

                _logger.LogTrace("ValidationAlerts column: Star width with MinWidth={MinWidth}px", header.Width);
            }
            else
            {
                // All other columns: Fixed Pixel width
                colDef = new ColumnDefinition
                {
                    Width = new GridLength(header.Width, GridUnitType.Pixel)
                };
            }

            rowGrid.ColumnDefinitions.Add(colDef);
        }

        // Create cell controls
        int columnIndex = 0;
        foreach (var cellViewModel in rowViewModel.Cells)
        {
            FrameworkElement cellControl;

            // Create appropriate control based on cell type
            if (cellViewModel.IsSpecialColumn)
            {
                cellControl = CreateSpecialColumnCell(cellViewModel, controlData);
            }
            else
            {
                cellControl = CreateNormalCell(cellViewModel, controlData);
            }

            // Set column position
            Grid.SetColumn(cellControl, columnIndex);
            rowGrid.Children.Add(cellControl);

            columnIndex++;
        }

        _logger.LogTrace("Configured Grid for row {RowIndex} with {CellCount} cells",
            rowViewModel.RowIndex, rowViewModel.Cells.Count);
    }

    /// <summary>
    /// Creates SpecialColumnCellControl and subscribes to its events.
    /// </summary>
    private FrameworkElement CreateSpecialColumnCell(CellViewModel cellViewModel, RowControlData controlData)
    {
        var specialControl = new SpecialColumnCellControl(cellViewModel, _loggerFactory?.CreateLogger<SpecialColumnCellControl>());

        // Create event handlers
        Action<int, bool> rowSelectionHandler = (rowIndex, isSelected) =>
        {
            OnRowSelectionChanged?.Invoke(rowIndex, isSelected);
        };

        EventHandler<DeleteRowRequestedEventArgs> deleteRowHandler = (sender, args) =>
        {
            OnDeleteRowRequested?.Invoke(sender, args);
        };

        EventHandler<InsertRowRequestedEventArgs> insertRowHandler = (sender, args) =>
        {
            OnInsertRowRequested?.Invoke(sender, args);
        };

        // Subscribe to events
        specialControl.OnRowSelectionChanged += rowSelectionHandler;
        specialControl.OnDeleteRowRequested += deleteRowHandler;
        specialControl.OnInsertRowRequested += insertRowHandler;

        // Track cleanup actions (unsubscribe handlers)
        controlData.AddCleanupAction(() =>
        {
            specialControl.OnRowSelectionChanged -= rowSelectionHandler;
            specialControl.OnDeleteRowRequested -= deleteRowHandler;
            specialControl.OnInsertRowRequested -= insertRowHandler;
        });

        return specialControl;
    }

    /// <summary>
    /// Creates CellControl and subscribes to its events.
    /// </summary>
    private FrameworkElement CreateNormalCell(CellViewModel cellViewModel, RowControlData controlData)
    {
        var cellControl = new CellControl(cellViewModel, _loggerFactory?.CreateLogger<CellControl>());

        // Create event handlers
        EventHandler<CellSelectionEventArgs> cellSelectedHandler = (sender, args) =>
        {
            OnCellSelected?.Invoke(sender, args);
        };

        EventHandler<CellViewModel> cellEditStartedHandler = (sender, vm) =>
        {
            OnCellEditStarted?.Invoke(sender, vm);
        };

        EventHandler<CellViewModel> cellEditCompletedHandler = (sender, vm) =>
        {
            OnCellEditCompleted?.Invoke(sender, vm);
        };

        EventHandler<CellViewModel> cellPointerEnteredHandler = (sender, vm) =>
        {
            OnCellPointerEntered?.Invoke(sender, vm);
        };

        EventHandler<CellValueChangedEventArgs> cellValueChangedHandler = (sender, args) =>
        {
            OnCellValueChanged?.Invoke(sender, args);
        };

        // Subscribe to events
        cellControl.CellSelected += cellSelectedHandler;
        cellControl.CellEditStarted += cellEditStartedHandler;
        cellControl.CellEditCompleted += cellEditCompletedHandler;
        cellControl.CellPointerEntered += cellPointerEnteredHandler;
        cellControl.CellValueChanged += cellValueChangedHandler;

        // Track cleanup actions (unsubscribe handlers)
        controlData.AddCleanupAction(() =>
        {
            cellControl.CellSelected -= cellSelectedHandler;
            cellControl.CellEditStarted -= cellEditStartedHandler;
            cellControl.CellEditCompleted -= cellEditCompletedHandler;
            cellControl.CellPointerEntered -= cellPointerEnteredHandler;
            cellControl.CellValueChanged -= cellValueChangedHandler;
        });

        return cellControl;
    }

    /// <summary>
    /// Creates placeholder element for loading/error states.
    /// </summary>
    private UIElement CreatePlaceholderElement()
    {
        return new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
            Height = 30,
            Child = new TextBlock
            {
                Text = "Loading...",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    /// <summary>
    /// Clears the recycling pool (call on dispose or major changes).
    /// </summary>
    public void ClearRecyclePool()
    {
        _logger.LogInformation("Clearing recycle pool ({Count} elements)", _recycledElements.Count);
        _recycledElements.Clear();
    }
}

/// <summary>
/// Helper class for tracking row control cleanup actions.
/// Prevents memory leaks by ensuring event handlers are properly unsubscribed.
/// </summary>
internal sealed class RowControlData
{
    private readonly DataGridRowViewModel _rowViewModel;
    private readonly List<Action> _cleanupActions = new();

    public RowControlData(DataGridRowViewModel rowViewModel)
    {
        _rowViewModel = rowViewModel ?? throw new ArgumentNullException(nameof(rowViewModel));
    }

    /// <summary>
    /// Adds a cleanup action to be executed when the row control is recycled.
    /// </summary>
    public void AddCleanupAction(Action cleanupAction)
    {
        if (cleanupAction != null)
        {
            _cleanupActions.Add(cleanupAction);
        }
    }

    /// <summary>
    /// Executes all cleanup actions and clears the list.
    /// Call this when recycling the row control.
    /// </summary>
    public void Cleanup()
    {
        foreach (var action in _cleanupActions)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                // Ignore cleanup errors to prevent cascading failures
            }
        }

        _cleanupActions.Clear();
    }

    public DataGridRowViewModel RowViewModel => _rowViewModel;
}

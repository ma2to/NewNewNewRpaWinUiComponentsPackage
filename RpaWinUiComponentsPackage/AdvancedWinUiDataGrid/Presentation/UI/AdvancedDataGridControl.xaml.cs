using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Enums;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Presentation.ViewModels;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Presentation.UI;

/// <summary>
/// PRESENTATION: Main UI control for AdvancedDataGrid with WinUI 3 implementation
/// PERFORMANCE: Uses ItemsRepeater for virtualization and smooth scrolling
/// INTERACTION: Supports column resizing, cell editing, and keyboard navigation
/// </summary>
internal sealed partial class AdvancedDataGridControl : UserControl, IDisposable
{
    #region Dependencies and Fields

    private readonly IDataGridLogger _logger;
    private DataGridViewModel? _viewModel;
    private bool _disposed;

    #endregion

    #region Properties

    /// <summary>Data context as strongly-typed ViewModel</summary>
    public DataGridViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel != value)
            {
                _viewModel?.Dispose();
                _viewModel = value;
                DataContext = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHeadlessMode));
                OnPropertyChanged(nameof(RowCountText));
            }
        }
    }

    /// <summary>Indicates if control is in headless mode</summary>
    public bool IsHeadlessMode => ViewModel?.OperationMode == DataGridOperationMode.Headless;

    /// <summary>Row count display text for status bar</summary>
    public string RowCountText
    {
        get
        {
            if (ViewModel == null) return "No data";
            var rowCount = ViewModel.Rows.Count;
            var columnCount = ViewModel.Columns.Count;
            return $"{rowCount} rows, {columnCount} columns";
        }
    }

    #endregion

    #region Constructor

    public AdvancedDataGridControl(IDataGridLogger? logger = null)
    {
        _logger = logger ?? new DataGridLogger(null, "UI");

        this.InitializeComponent();

        _logger.LogInformation("UI: AdvancedDataGridControl initialized");

        // Subscribe to ViewModel property changes for UI updates
        this.Loaded += OnControlLoaded;
        this.Unloaded += OnControlUnloaded;
    }

    #endregion

    #region Event Handlers - Control Lifecycle

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("UI: AdvancedDataGridControl loaded");

        // Subscribe to ViewModel collection changes
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnControlUnloaded(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("UI: AdvancedDataGridControl unloaded");

        // Unsubscribe from ViewModel events
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update UI-specific properties when ViewModel changes
        if (e.PropertyName == nameof(DataGridViewModel.Rows) ||
            e.PropertyName == nameof(DataGridViewModel.Columns))
        {
            OnPropertyChanged(nameof(RowCountText));
        }
        else if (e.PropertyName == nameof(DataGridViewModel.OperationMode))
        {
            OnPropertyChanged(nameof(IsHeadlessMode));
        }

        _logger.LogInformation("UI: ViewModel property changed: {PropertyName}", e.PropertyName ?? "Unknown");
    }

    #endregion

    #region Event Handlers - Cell Interaction

    private void OnCellGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is CellViewModel cellViewModel)
        {
            cellViewModel.IsSelected = true;
            cellViewModel.IsEditing = true;

            // Update ViewModel selection
            if (ViewModel != null)
            {
                ViewModel.SelectedRowIndex = cellViewModel.Address.RowIndex;
                ViewModel.SelectedColumnName = cellViewModel.ColumnName;
            }

            _logger.LogInformation("UI: Cell [{Row}, {Column}] got focus",
                cellViewModel.Address.RowIndex, cellViewModel.ColumnName);
        }
    }

    private void OnCellLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is CellViewModel cellViewModel)
        {
            cellViewModel.IsSelected = false;
            cellViewModel.IsEditing = false;

            _logger.LogInformation("UI: Cell [{Row}, {Column}] lost focus",
                cellViewModel.Address.RowIndex, cellViewModel.ColumnName);
        }
    }

    private void OnCellTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is CellViewModel cellViewModel)
        {
            // Update cell value through ViewModel
            cellViewModel.Value = textBox.Text;

            _logger.LogInformation("UI: Cell [{Row}, {Column}] text changed to '{Text}'",
                cellViewModel.Address.RowIndex, cellViewModel.ColumnName, textBox.Text);
        }
    }

    #endregion

    #region Event Handlers - Column Resizing

    private void OnColumnResizeStarted(object sender, DragStartedEventArgs e)
    {
        if (sender is Thumb thumb &&
            thumb.DataContext is DataColumnViewModel columnViewModel)
        {
            columnViewModel.StartResize();
            _logger.LogInformation("UI: Started resizing column '{ColumnName}'", columnViewModel.Name);
        }
    }

    private void OnColumnResizeDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is Thumb thumb &&
            thumb.DataContext is DataColumnViewModel columnViewModel)
        {
            var newWidth = columnViewModel.Width + e.HorizontalChange;
            columnViewModel.UpdateResizeWidth(newWidth);

            _logger.LogInformation("UI: Resizing column '{ColumnName}' to {Width}px",
                columnViewModel.Name, newWidth);
        }
    }

    private void OnColumnResizeCompleted(object sender, DragCompletedEventArgs e)
    {
        if (sender is Thumb thumb &&
            thumb.DataContext is DataColumnViewModel columnViewModel)
        {
            columnViewModel.EndResize();
            _logger.LogInformation("UI: Finished resizing column '{ColumnName}' to {Width}px",
                columnViewModel.Name, columnViewModel.Width);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>Initialize control with ViewModel</summary>
    public void Initialize(DataGridViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger.LogInformation("UI: AdvancedDataGridControl initialized with ViewModel");
    }

    /// <summary>Focus specific cell</summary>
    public void FocusCell(int rowIndex, string columnName)
    {
        try
        {
            // Find the specific cell and focus it
            if (ViewModel != null &&
                rowIndex >= 0 && rowIndex < ViewModel.Rows.Count)
            {
                var rowViewModel = ViewModel.Rows[rowIndex];
                var cellViewModel = rowViewModel.GetCell(columnName);

                if (cellViewModel != null)
                {
                    cellViewModel.IsSelected = true;
                    ViewModel.SelectedRowIndex = rowIndex;
                    ViewModel.SelectedColumnName = columnName;

                    _logger.LogInformation("UI: Focused cell [{Row}, {Column}]", rowIndex, columnName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UI: Error focusing cell [{Row}, {Column}]", rowIndex, columnName);
        }
    }


    /// <summary>Apply color configuration to UI elements</summary>
    public void ApplyColorConfiguration(ColorConfiguration colorConfiguration)
    {
        try
        {
            if (ViewModel != null)
            {
                ViewModel.ColorConfiguration = colorConfiguration;
                _logger.LogInformation("UI: Applied color configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UI: Error applying color configuration");
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Scroll Support - Mouse Wheel & Auto Sizing

    /// <summary>
    /// SCROLL SUPPORT: Handle mouse wheel scrolling for vertical navigation
    /// ENTERPRISE: Smooth scrolling experience for large datasets
    /// PERFORMANCE: Efficient scrolling with acceleration support
    /// </summary>
    private void OnScrollViewerPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            if (sender is ScrollViewer scrollViewer && e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                // Get mouse wheel delta from the pointer event
                var delta = e.GetCurrentPoint(scrollViewer).Properties.MouseWheelDelta;
                if (delta != 0)
                {
                    // Calculate scroll delta - negative values = scroll down, positive = scroll up
                    double scrollDelta = delta * 0.5; // Multiply for smoother scrolling

                    // Get current vertical offset and calculate new position
                    double currentOffset = scrollViewer.VerticalOffset;
                    double newOffset = Math.Max(0, currentOffset - scrollDelta);

                    // Apply the scroll with smooth animation
                    scrollViewer.ChangeView(null, newOffset, null, false);

                    _logger.LogInformation("SCROLL: Mouse wheel scroll - Delta: {Delta}, NewOffset: {NewOffset}",
                        delta, newOffset);

                    // Mark event as handled to prevent parent controls from handling it
                    e.Handled = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SCROLL: Error handling mouse wheel scroll");
        }
    }

    /// <summary>
    /// SCROLL SUPPORT: Programmatic scrolling to specific row
    /// ENTERPRISE: Navigation support for large datasets
    /// </summary>
    public void ScrollToRow(int rowIndex)
    {
        try
        {
            if (MainScrollViewer != null && ViewModel != null &&
                rowIndex >= 0 && rowIndex < ViewModel.Rows.Count)
            {
                // Calculate estimated row height (this could be made more sophisticated)
                double estimatedRowHeight = 32.0; // Default row height in pixels
                double targetOffset = rowIndex * estimatedRowHeight;

                // Scroll to the calculated position with smooth animation
                MainScrollViewer.ChangeView(null, targetOffset, null, true);

                _logger.LogInformation("SCROLL: Scrolled to row {RowIndex} at offset {Offset}",
                    rowIndex, targetOffset);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SCROLL: Error scrolling to row {RowIndex}", rowIndex);
        }
    }

    /// <summary>
    /// SCROLL SUPPORT: Ensure the table adapts to container size changes
    /// RESPONSIVE: Auto-scrollbars when content overflows container
    /// </summary>
    private void OnDataGridContainerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            if (MainScrollViewer != null && DataGridContainer != null)
            {
                // Get container and content sizes
                double containerWidth = e.NewSize.Width;
                double containerHeight = e.NewSize.Height;
                double contentWidth = DataGridContainer.ActualWidth;
                double contentHeight = DataGridContainer.ActualHeight;

                // Log size information for debugging
                _logger.LogInformation("SCROLL: Container size changed - Container: {ContainerW}x{ContainerH}, Content: {ContentW}x{ContentH}",
                    containerWidth, containerHeight, contentWidth, contentHeight);

                // The ScrollViewer will automatically show/hide scrollbars based on Auto visibility settings
                // No additional logic needed - WinUI handles this automatically
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SCROLL: Error handling container size change");
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _logger.LogInformation("UI: Disposing AdvancedDataGridControl");

            // Unsubscribe from events
            this.Loaded -= OnControlLoaded;
            this.Unloaded -= OnControlUnloaded;

            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.Dispose();
                ViewModel = null;
            }

            _logger.LogInformation("UI: AdvancedDataGridControl disposed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UI: Error during AdvancedDataGridControl disposal");
        }
        finally
        {
            // CLEANUP: Dispose logger if it implements IDisposable (DataGridLogger does)
            if (_logger is IDisposable disposableLogger)
            {
                disposableLogger.Dispose();
            }
            _disposed = true;
        }
    }

    #endregion
}
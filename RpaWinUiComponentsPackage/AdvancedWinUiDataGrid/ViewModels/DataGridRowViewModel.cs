using System.Collections.ObjectModel;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// ViewModel for a single row in the data grid
/// Contains collection of cell ViewModels
/// Implements IDisposable for proper cleanup of cell ViewModels (memory leak prevention)
/// </summary>
public sealed class DataGridRowViewModel : ViewModelBase, IDisposable
{
    private bool _isSelected;
    private bool _hasValidationErrors;
    private bool _isVisible = true;
    private bool _disposed;

    // ✅ SENIOR FIX: Weak reference to parent DataGridViewModel to prevent circular reference memory leak
    // WeakReference allows GC to collect parent if grid is disposed but row ViewModels still referenced
    private WeakReference<DataGridViewModel>? _parentViewModel;

    public int RowIndex { get; set; }

    /// <summary>
    /// Gets or sets the unique row ID (from __rowId field in data).
    /// This ID is stable across row operations (delete, sort, filter).
    /// </summary>
    public string? RowId { get; set; }

    /// <summary>
    /// Indicates whether this row is visible in UI (FIXED UI POOL).
    /// FALSE for empty rows in UI pool padding (when page has fewer data rows than PageSize).
    /// Used by DataGridElementFactory to render collapsed placeholder.
    /// ARCHITECTURE:
    /// - FIXED UI POOL: Always PageSize ViewModels (e.g., 15)
    /// - Page with 10 data rows: 10 visible + 5 invisible (total 15)
    /// - Invisible rows: RowId=null, IsVisible=false, Height=0, Collapsed
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public ObservableCollection<CellViewModel> Cells { get; } = new();

    /// <summary>
    /// ✅ SENIOR FIX: IsSelected with batch update optimization.
    /// Suppresses PropertyChanged during bulk operations (SelectAll/DeselectAll) for performance.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            // Check if parent is in batch update mode (SelectAll/DeselectAll optimization)
            if (_parentViewModel != null &&
                _parentViewModel.TryGetTarget(out var parent) &&
                parent.IsBatchUpdating)
            {
                // ✅ BATCH MODE: Update value WITHOUT triggering PropertyChanged
                // This prevents 100+ individual UI updates during SelectAll/DeselectAll
                _isSelected = value;
            }
            else
            {
                // ✅ NORMAL MODE: Update with PropertyChanged notification
                SetProperty(ref _isSelected, value);
            }
        }
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Gets PageManager from parent DataGridViewModel for global row numbering.
    /// Used by CellViewModel.DisplayRowNumber to calculate global position (Page 1: 1-15, Page 2: 16-30).
    /// Returns null if parent not available.
    /// </summary>
    public Features.Pagination.Interfaces.IPageManager? PageManager
    {
        get
        {
            if (_parentViewModel != null && _parentViewModel.TryGetTarget(out var parent))
            {
                return parent.PageManager;
            }
            return null;
        }
    }

    /// <summary>
    /// ✅ SENIOR FIX: Sets parent DataGridViewModel reference for batch update optimization.
    /// Uses WeakReference to prevent circular reference memory leak.
    /// INTERNAL: Called by DataGridViewModel.LoadRows when creating row ViewModels.
    /// </summary>
    internal void SetParentViewModel(DataGridViewModel parentViewModel)
    {
        _parentViewModel = new WeakReference<DataGridViewModel>(parentViewModel);
    }

    /// <summary>
    /// ✅ SENIOR FIX: Public helper to trigger PropertyChanged externally.
    /// Used for batch update UI refresh (SelectAll/DeselectAll) when batch mode suppressed events.
    /// INTERNAL: Only accessible within ViewModels namespace for controlled usage.
    /// </summary>
    internal void RaisePropertyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
    }

    public bool HasValidationErrors
    {
        get => _hasValidationErrors;
        set => SetProperty(ref _hasValidationErrors, value);
    }

    /// <summary>
    /// Select or deselect all cells in this row
    /// </summary>
    public void SetRowSelection(bool isSelected)
    {
        IsSelected = isSelected;
        foreach (var cell in Cells)
        {
            cell.IsSelected = isSelected;
        }
    }

    /// <summary>
    /// Disposes the row ViewModel and all child cell ViewModels.
    /// CRITICAL for memory management in UI virtualization - prevents memory leaks when rows leave viewport.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Dispose all cells if they implement IDisposable
        foreach (var cell in Cells.OfType<IDisposable>())
        {
            cell.Dispose();
        }

        // Clear collection to release references
        Cells.Clear();
    }
}

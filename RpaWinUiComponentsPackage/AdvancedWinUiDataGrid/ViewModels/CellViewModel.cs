using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

/// <summary>
/// View model for a single cell in the data grid.
/// Manages cell state including value, selection, validation, and visual appearance.
/// Automatically updates visual styling based on current state (validation error, selected, search match, etc.).
/// Supports special column types (RowNumber, Checkbox, ValidationAlerts, DeleteRow).
/// Implements IDisposable for proper cleanup (memory leak prevention).
/// </summary>
public sealed class CellViewModel : ViewModelBase, IDisposable
{
    private readonly ThemeManager? _themeManager;
    private readonly ILogger<CellViewModel>? _logger;
    private object? _value;
    private bool _isSelected;
    private bool _isSearchFound;
    private bool _isValidationError;
    private bool _isValidationSuccess;
    private bool _isEditing;
    private bool _focusRequested;
    private string _validationMessage = string.Empty;
    private SolidColorBrush _borderBrush = Features.Optimization.BrushPool.GetBrush(Colors.Gray);
    private SolidColorBrush _backgroundBrush = Features.Optimization.BrushPool.GetBrush(Colors.White);
    private SolidColorBrush _foregroundBrush = Features.Optimization.BrushPool.GetBrush(Colors.Black);
    private double _borderThickness = 1.0;
    private SpecialColumnType _specialType = SpecialColumnType.None;
    private bool _isReadOnly = false;
    private bool _isRowSelected = false;
    private string? _validationAlertMessage = null;
    private bool _disposed;

    // ✅ CRITICAL FIX: Reference to parent DataGridRowViewModel for checkbox synchronization
    private DataGridRowViewModel? _parentRow;

    /// <summary>
    /// Creates a new cell view model with optional theme support.
    /// When a theme manager is provided, the cell will automatically use theme colors.
    /// Without a theme manager, default colors (gray border, white background, black text) are used.
    /// </summary>
    /// <param name="themeManager">Optional theme manager for consistent coloring across the grid</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public CellViewModel(ThemeManager? themeManager = null, ILogger<CellViewModel>? logger = null)
    {
        _themeManager = themeManager;
        _logger = logger;
        if (_themeManager != null)
        {
            // Use theme colors as defaults
            _borderBrush = _themeManager.CellBorder;
            _backgroundBrush = _themeManager.CellDefaultBackground;
            _foregroundBrush = _themeManager.CellDefaultForeground;
        }
    }

    /// <summary>
    /// Gets or sets the row index of this cell in the grid (zero-based).
    /// </summary>
    public int RowIndex { get; set; }

    /// <summary>
    /// Gets or sets the unique row ID (from __rowId field in data).
    /// This ID is stable across row operations (delete, sort, filter).
    /// </summary>
    public string? RowId { get; set; }

    /// <summary>
    /// Gets or sets the column index of this cell in the grid (zero-based).
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Gets or sets the column name that this cell belongs to.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value displayed in this cell.
    /// Can be any type (string, number, date, etc.) and will be converted to string for display.
    /// </summary>
    public object? Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    /// <summary>
    /// Gets or sets whether this cell is currently selected.
    /// Selected cells are highlighted with a blue border and background.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                UpdateCellAppearance();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this cell matches the current search criteria.
    /// Search matches are highlighted with an orange/warning color.
    /// </summary>
    public bool IsSearchFound
    {
        get => _isSearchFound;
        set
        {
            if (SetProperty(ref _isSearchFound, value))
            {
                UpdateCellAppearance();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this cell has a validation error.
    /// Validation errors are shown with a red border and background.
    /// This has higher priority than other states in the visual hierarchy.
    /// </summary>
    public bool IsValidationError
    {
        get => _isValidationError;
        set
        {
            if (SetProperty(ref _isValidationError, value))
            {
                _logger?.LogTrace("CellViewModel[{Row},{Col}] '{ColumnName}': IsValidationError changed to {Value}",
                    RowIndex, ColumnIndex, ColumnName, value);
                UpdateCellAppearance();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this cell passed validation successfully.
    /// Successful validation is shown with a green indicator.
    /// </summary>
    public bool IsValidationSuccess
    {
        get => _isValidationSuccess;
        set
        {
            if (SetProperty(ref _isValidationSuccess, value))
            {
                UpdateCellAppearance();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this cell is currently being edited.
    /// When true, the cell shows an editable text box instead of read-only text.
    /// SENIOR FIX: Preserves IsSelected state when exiting edit mode (keeps selection on edited cell).
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                // SENIOR FIX: Preserve IsSelected state when exiting edit mode
                // User wants cell to remain selected after confirming/canceling edit
                if (!value && _isSelected)  // Exiting edit mode && was selected
                {
                    // Force update appearance to show selection border
                    UpdateCellAppearance();
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets whether focus should be programmatically applied to this cell's control.
    /// Used for keyboard navigation (arrow keys) to move focus between cells.
    /// ARCHITECTURE: CellControl subscribes to PropertyChanged and applies focus when this is set to true.
    /// CRITICAL: Automatically resets to false after focus is applied to prevent infinite loops.
    /// </summary>
    public bool FocusRequested
    {
        get => _focusRequested;
        set => SetProperty(ref _focusRequested, value);
    }

    /// <summary>
    /// Gets or sets the validation error message for this cell.
    /// This message is typically shown in a tooltip when the user hovers over an invalid cell.
    /// </summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>
    /// Gets or sets the border brush for this cell.
    /// This is automatically updated based on cell state (validation, selection, etc.).
    /// DEFENSIVE: Automatically uses BrushPool for deduplication even on direct assignment.
    /// </summary>
    public SolidColorBrush BorderBrush
    {
        get => _borderBrush;
        set
        {
            // Defensive: ensure BrushPool usage even if set directly (API safety)
            var pooledBrush = Features.Optimization.BrushPool.GetBrush(value.Color);
            SetProperty(ref _borderBrush, pooledBrush);
        }
    }

    /// <summary>
    /// Gets or sets the background brush for this cell.
    /// This is automatically updated based on cell state (validation, selection, etc.).
    /// DEFENSIVE: Automatically uses BrushPool for deduplication even on direct assignment.
    /// </summary>
    public SolidColorBrush BackgroundBrush
    {
        get => _backgroundBrush;
        set
        {
            // Defensive: ensure BrushPool usage even if set directly (API safety)
            var pooledBrush = Features.Optimization.BrushPool.GetBrush(value.Color);
            SetProperty(ref _backgroundBrush, pooledBrush);
        }
    }

    /// <summary>
    /// Gets or sets the foreground (text) brush for this cell.
    /// This is automatically updated based on cell state (validation, selection, etc.).
    /// DEFENSIVE: Automatically uses BrushPool for deduplication even on direct assignment.
    /// </summary>
    public SolidColorBrush ForegroundBrush
    {
        get => _foregroundBrush;
        set
        {
            // Defensive: ensure BrushPool usage even if set directly (API safety)
            var pooledBrush = Features.Optimization.BrushPool.GetBrush(value.Color);
            SetProperty(ref _foregroundBrush, pooledBrush);
        }
    }

    /// <summary>
    /// Gets or sets the border thickness for this cell in pixels.
    /// Normal cells have thickness 1.0, highlighted cells (selected, validation error, etc.) have thickness 2.0.
    /// </summary>
    public double BorderThickness
    {
        get => _borderThickness;
        set => SetProperty(ref _borderThickness, value);
    }

    /// <summary>
    /// Gets or sets the type of special column (None for normal data columns)
    /// </summary>
    public SpecialColumnType SpecialType
    {
        get => _specialType;
        set => SetProperty(ref _specialType, value);
    }

    /// <summary>
    /// Gets whether this is a special column cell (not a normal data cell)
    /// </summary>
    public bool IsSpecialColumn => SpecialType != SpecialColumnType.None;

    /// <summary>
    /// Gets or sets whether this cell is read-only (cannot be edited)
    /// Special column cells are typically read-only
    /// </summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Gets GLOBAL row number for RowNumber special column (1-based)
    /// BEFORE: Displayed page-relative number (1-15 on every page)
    /// AFTER: Displays global position in dataset (Page 1: 1-15, Page 2: 16-30, etc.)
    /// ARCHITECTURE:
    /// - RowIndex = page-relative index (0-14)
    /// - PageManager.CurrentPage = current page number (0-based)
    /// - PageManager.PageSize = rows per page (e.g., 15)
    /// - Global position = (CurrentPage × PageSize) + RowIndex + 1
    /// EXAMPLE: Page 2 (CurrentPage=1), RowIndex=5, PageSize=15
    ///   → Global position = (1 × 15) + 5 + 1 = 21
    /// FILTERING/SEARCH: Recalculates automatically when PageManager updates
    /// </summary>
    public int DisplayRowNumber
    {
        get
        {
            // Get PageManager from parent row (if available)
            var pageManager = _parentRow?.PageManager;
            if (pageManager != null)
            {
                // Calculate global position: (page × pageSize) + row + 1
                var globalPosition = (pageManager.CurrentPage * pageManager.PageSize) + RowIndex + 1;
                return globalPosition;
            }

            // Fallback: page-relative number (if PageManager not available)
            return RowIndex + 1;
        }
    }

    /// <summary>
    /// Gets or sets whether the row is selected (for Checkbox special column)
    /// ✅ CRITICAL FIX: Synchronizes with parent DataGridRowViewModel.IsSelected
    /// ✅ INFINITE LOOP FIX: Guard condition prevents bidirectional sync loop
    /// </summary>
    public bool IsRowSelected
    {
        get => _isRowSelected;
        set
        {
            if (SetProperty(ref _isRowSelected, value))
            {
                // ✅ GUARD: Only sync if parent exists AND value differs (prevents infinite loop)
                // Without this guard: cell.IsRowSelected=true → parent.IsSelected=true
                // → parent.PropertyChanged → cell.IsRowSelected=true → LOOP
                if (_parentRow != null && _parentRow.IsSelected != value)
                {
                    _parentRow.IsSelected = value;
                }
            }
        }
    }

    /// <summary>
    /// ✅ CRITICAL FIX: Sets the parent DataGridRowViewModel for BIDIRECTIONAL checkbox synchronization.
    /// Must be called when creating cells for a row.
    /// BIDIRECTIONAL: cell.IsRowSelected ↔ parent.IsSelected (both directions synced)
    /// </summary>
    public void SetParentRow(DataGridRowViewModel parentRow)
    {
        _parentRow = parentRow;

        // ✅ BIDIRECTIONAL SYNC: Parent → Cell (when parent.IsSelected changes, update cell)
        // This ensures header checkbox clicks propagate to cell checkboxes
        _parentRow.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DataGridRowViewModel.IsSelected))
            {
                // Sync cell checkbox visual with row selection
                if (_isRowSelected != _parentRow.IsSelected)
                {
                    _isRowSelected = _parentRow.IsSelected;
                    OnPropertyChanged(nameof(IsRowSelected));
                }
            }
        };
    }

    /// <summary>
    /// Gets or sets the validation alert message (for ValidationAlerts special column)
    /// </summary>
    public string? ValidationAlertMessage
    {
        get => _validationAlertMessage;
        set => SetProperty(ref _validationAlertMessage, value);
    }

    /// <summary>
    /// Gets whether this cell has a validation alert message
    /// </summary>
    public bool HasValidationAlert => !string.IsNullOrEmpty(ValidationAlertMessage);

    /// <summary>
    /// Gets the theme manager for accessing theme colors
    /// </summary>
    public ThemeManager? Theme => _themeManager;

    /// <summary>
    /// Update cell visual appearance based on current state
    /// Priority: ValidationError+Selected (combined) > ValidationError > ValidationSuccess > SearchFound > Selected > Default
    /// Uses ThemeManager colors if available, otherwise falls back to hardcoded colors
    /// SPECIAL CASE: When both ValidationError and Selected are true, use selection background with validation error border/text
    /// </summary>
    private void UpdateCellAppearance()
    {
        _logger?.LogTrace("CellViewModel[{Row},{Col}] UpdateCellAppearance: IsValidationError={IsValidationError}, IsSelected={IsSelected}, IsSearchFound={IsSearchFound}",
            RowIndex, ColumnIndex, IsValidationError, IsSelected, IsSearchFound);

        // SPECIAL CASE: Validation error + Selection combined
        // User requirement: Show selection background (blue) with validation error border and text (red)
        // This allows distinguishing both states simultaneously
        if (IsValidationError && IsSelected)
        {
            var errorBorder = _themeManager?.ValidationErrorBorder ?? Features.Optimization.BrushPool.GetBrush(Colors.Red);
            BorderBrush = errorBorder;
            BackgroundBrush = _themeManager?.MultiSelectionBackground ?? Features.Optimization.BrushPool.GetBrush(Color.FromArgb(30, 0, 120, 215)); // Selection blue
            ForegroundBrush = _themeManager?.ValidationErrorForeground ?? Features.Optimization.BrushPool.GetBrush(Colors.Red);
            BorderThickness = 2.0;

            _logger?.LogTrace("Applied ValidationError+Selected style: BorderColor={BorderColor}, BorderThickness={BorderThickness}",
                errorBorder.Color, BorderThickness);
            return;
        }

        if (IsValidationError)
        {
            var errorBorder = _themeManager?.ValidationErrorBorder ?? Features.Optimization.BrushPool.GetBrush(Colors.Red);
            var errorBackground = _themeManager?.ValidationErrorBackground ?? Features.Optimization.BrushPool.GetBrush(Color.FromArgb(20, 255, 0, 0));
            BorderBrush = errorBorder;
            BackgroundBrush = errorBackground;
            ForegroundBrush = _themeManager?.ValidationErrorForeground ?? Features.Optimization.BrushPool.GetBrush(Colors.Black);
            BorderThickness = 2.0;

            _logger?.LogInformation("APPLIED VALIDATION ERROR STYLE: RowId={RowId}, ColumnName={ColumnName}, " +
                                  "BorderColor={BorderColor}, BackgroundColor={BackgroundColor}, BorderThickness={BorderThickness}, " +
                                  "ValidationMessage={ValidationMessage}",
                RowId, ColumnName, errorBorder.Color, errorBackground.Color, BorderThickness, ValidationMessage);
        }
        else if (IsValidationSuccess)
        {
            // CRITICAL: Validation success = DEFAULT border color (NOT green!)
            // Success only shows as default cell appearance (validation passed = no visual change)
            BorderBrush = _themeManager?.CellBorder ?? Features.Optimization.BrushPool.GetBrush(Colors.Gray);
            BackgroundBrush = _themeManager?.CellDefaultBackground ?? Features.Optimization.BrushPool.GetBrush(Colors.White);
            ForegroundBrush = _themeManager?.CellDefaultForeground ?? Features.Optimization.BrushPool.GetBrush(Colors.Black);
            BorderThickness = 1.0;
        }
        else if (IsSearchFound)
        {
            // Search uses Warning colors from theme
            BorderBrush = _themeManager?.ValidationWarningBorder ?? Features.Optimization.BrushPool.GetBrush(Colors.Orange);
            BackgroundBrush = _themeManager?.ValidationWarningBackground ?? Features.Optimization.BrushPool.GetBrush(Color.FromArgb(40, 255, 165, 0));
            ForegroundBrush = _themeManager?.ValidationWarningForeground ?? Features.Optimization.BrushPool.GetBrush(Colors.Black);
            BorderThickness = 2.0;
        }
        else if (IsSelected)
        {
            BorderBrush = _themeManager?.SelectionBorder ?? Features.Optimization.BrushPool.GetBrush(Colors.Blue);
            BackgroundBrush = _themeManager?.MultiSelectionBackground ?? Features.Optimization.BrushPool.GetBrush(Color.FromArgb(30, 0, 120, 215));
            ForegroundBrush = _themeManager?.MultiSelectionForeground ?? Features.Optimization.BrushPool.GetBrush(Colors.Black);
            BorderThickness = 2.0;
        }
        else
        {
            // Default state - apply Zebra Row colors if enabled
            BorderBrush = _themeManager?.CellBorder ?? Features.Optimization.BrushPool.GetBrush(Colors.Gray);

            // FÁZA 6: Zebra Rows support (configurable alternate row colors)
            // Priority: Validation > Selected > SearchFound > Zebra > Default
            if (_themeManager?.Options?.EnableZebraRows == true)
            {
                // Apply zebra row colors based on row index (even/odd)
                bool isEvenRow = (RowIndex % 2 == 0);
                BackgroundBrush = isEvenRow
                    ? _themeManager.ZebraRowEvenBackground
                    : _themeManager.ZebraRowOddBackground;
                ForegroundBrush = isEvenRow
                    ? _themeManager.ZebraRowEvenForeground
                    : _themeManager.ZebraRowOddForeground;
            }
            else
            {
                // Default colors (no zebra rows)
                BackgroundBrush = _themeManager?.CellDefaultBackground ?? Features.Optimization.BrushPool.GetBrush(Colors.White);
                ForegroundBrush = _themeManager?.CellDefaultForeground ?? Features.Optimization.BrushPool.GetBrush(Colors.Black);
            }

            BorderThickness = 1.0;
        }
    }

    /// <summary>
    /// Disposes the cell ViewModel and releases resources.
    /// NOTE: BrushPool brushes are NOT disposed here (they are shared instances).
    /// CRITICAL for memory management in UI virtualization.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // NOTE: BrushPool brushes NIE SÚ disposed (shared!)
        // We only null out references to help GC

        // Null out references for GC
        _borderBrush = null!;
        _backgroundBrush = null!;
        _foregroundBrush = null!;
        _value = null;
        _validationMessage = string.Empty;
        _validationAlertMessage = null;
    }
}

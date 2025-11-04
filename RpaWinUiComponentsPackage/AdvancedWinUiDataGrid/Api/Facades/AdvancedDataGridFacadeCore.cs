using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public implementation of IAdvancedDataGridFacade
/// Orchestrates all component operations via internal services
/// CORE: Constructor, disposal, and common helper methods
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AdvancedDataGridOptions _options;
    private readonly ILogger<AdvancedDataGridFacade> _logger;
    private readonly IOperationLogger<AdvancedDataGridFacade> _operationLogger;
    private readonly DispatcherQueue? _dispatcher;
    private readonly UIAdapters.WinUI.UiNotificationService? _uiNotificationService;
    private readonly UIAdapters.WinUI.GridViewModelAdapter? _gridViewModelAdapter;
    private readonly UIAdapters.WinUI.InternalUIUpdateHandler? _internalUIUpdateHandler;
    private readonly UIAdapters.WinUI.InternalUIOperationHandler? _internalUIOperationHandler;
    private readonly UIAdapters.WinUI.InternalUISortHandler? _internalUISortHandler;
    private readonly Features.Color.ThemeService _themeService;
    private readonly Features.Schema.ColumnSchemaService _columnSchemaService;
    private bool _disposed;

    // Feature module dependencies
    private readonly Columns.IDataGridColumns _columns;
    private readonly Editing.IDataGridEditing _editing;
    private readonly Filtering.IDataGridFiltering _filtering;
    private readonly Selection.IDataGridSelection _selection;
    private readonly Sorting.IDataGridSorting _sorting;
    private readonly Configuration.IDataGridConfiguration _configuration;
    private readonly Rows.IDataGridRows _rows;
    private readonly Batch.IDataGridBatch _batch;
    private readonly IO.IDataGridIO _io;
    private readonly Clipboard.IDataGridClipboard _clipboard;
    private readonly Search.IDataGridSearch _search;
    private readonly Validation.IDataGridValidation _validation;
    private readonly Performance.IDataGridPerformance _performance;
    private readonly Theming.IDataGridTheming _theming;
    private readonly Notifications.IDataGridNotifications _notifications;
    private readonly AutoRowHeight.IDataGridAutoRowHeight _autoRowHeight;
    private readonly Shortcuts.IDataGridShortcuts _shortcuts;
    private readonly MVVM.IDataGridMVVM _mvvm;
    // REMOVED: SmartOperations - replaced by RowManagement feature
    // private readonly SmartOperations.IDataGridSmartOperations _smartOperations;
    private readonly Environments.IEnvironmentConfiguration _environment;
    private readonly IDataGridColors _colors;
    private readonly IDataGridTheme _theme;

    #region Feature Module Properties

    /// <summary>
    /// Column management operations
    /// </summary>
    public Columns.IDataGridColumns Columns => _columns;

    /// <summary>
    /// Cell editing operations
    /// </summary>
    public Editing.IDataGridEditing Editing => _editing;

    /// <summary>
    /// Filtering operations
    /// </summary>
    public Filtering.IDataGridFiltering Filtering => _filtering;

    /// <summary>
    /// Selection operations
    /// </summary>
    public Selection.IDataGridSelection Selection => _selection;

    /// <summary>
    /// Sorting operations
    /// </summary>
    public Sorting.IDataGridSorting Sorting => _sorting;

    /// <summary>
    /// Configuration management
    /// </summary>
    public Configuration.IDataGridConfiguration Configuration => _configuration;

    /// <summary>
    /// Row management operations
    /// </summary>
    public Rows.IDataGridRows Rows => _rows;

    /// <summary>
    /// Batch operations
    /// </summary>
    public Batch.IDataGridBatch Batch => _batch;

    /// <summary>
    /// Import/Export operations
    /// </summary>
    public IO.IDataGridIO IO => _io;

    /// <summary>
    /// Clipboard operations
    /// </summary>
    public Clipboard.IDataGridClipboard Clipboard => _clipboard;

    /// <summary>
    /// Search operations
    /// </summary>
    public Search.IDataGridSearch Search => _search;

    /// <summary>
    /// Validation operations
    /// </summary>
    public Validation.IDataGridValidation Validation => _validation;

    /// <summary>
    /// Performance monitoring
    /// </summary>
    public Performance.IDataGridPerformance Performance => _performance;

    /// <summary>
    /// Theme and color management
    /// </summary>
    public Theming.IDataGridTheming Theming => _theming;

    /// <summary>
    /// UI notifications and subscriptions
    /// </summary>
    public Notifications.IDataGridNotifications Notifications => _notifications;

    /// <summary>
    /// Auto row height management
    /// </summary>
    public AutoRowHeight.IDataGridAutoRowHeight AutoRowHeight => _autoRowHeight;

    /// <summary>
    /// Keyboard shortcuts
    /// </summary>
    public Shortcuts.IDataGridShortcuts Shortcuts => _shortcuts;

    /// <summary>
    /// MVVM binding support
    /// </summary>
    public MVVM.IDataGridMVVM MVVM => _mvvm;

    // REMOVED: SmartOperations - replaced by RowManagement feature
    // Use Rows.RemoveRowsAsync(rowIds) for deletion with automatic data shifting
    // public SmartOperations.IDataGridSmartOperations SmartOperations => _smartOperations;

    /// <summary>
    /// Environment configuration management (application-level settings)
    /// </summary>
    public Environments.IEnvironmentConfiguration Environment => _environment;

    /// <summary>
    /// Direct color management (BOD 4+6: Granular color control without themes)
    /// </summary>
    public IDataGridColors Colors => _colors;

    /// <summary>
    /// Comprehensive theme management (BOD 5+7: Theme creation, import/export, application)
    /// </summary>
    public IDataGridTheme Theme => _theme;

    #endregion

    /// <summary>
    /// AdvancedDataGridFacade constructor
    /// Initializes dependencies and obtains operation logger via DI
    /// </summary>
    public AdvancedDataGridFacade(
        IServiceProvider serviceProvider,
        AdvancedDataGridOptions options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = serviceProvider.GetRequiredService<ILogger<AdvancedDataGridFacade>>();
        _dispatcher = serviceProvider.GetService<DispatcherQueue>();

        // Obtain operation logger via DI, or use null pattern
        var operationLogger = serviceProvider.GetService<IOperationLogger<AdvancedDataGridFacade>>();
        _operationLogger = operationLogger ?? NullOperationLogger<AdvancedDataGridFacade>.Instance;

        // Obtain UI notification service (available if DispatcherQueue provided)
        _uiNotificationService = serviceProvider.GetService<UIAdapters.WinUI.UiNotificationService>();

        // Obtain GridViewModelAdapter (available if DispatcherQueue provided)
        _gridViewModelAdapter = serviceProvider.GetService<UIAdapters.WinUI.GridViewModelAdapter>();

        // CRITICAL: Obtain InternalUIUpdateHandler to enable automatic UI updates in Interactive mode
        // This MUST be resolved to activate the handler and subscribe to OnDataRefreshed events
        _internalUIUpdateHandler = serviceProvider.GetService<UIAdapters.WinUI.InternalUIUpdateHandler>();

        // CRITICAL: Create InternalUIOperationHandler to enable automatic UI operation handling in Interactive mode
        // This handler automatically processes delete and auto-expand operations without application code involvement
        // NOTE: Cannot be registered in DI because it needs reference to facade (this)
        if (options.OperationMode == PublicDataGridOperationMode.Interactive)
        {
            var uiControl = serviceProvider.GetService<UIControls.AdvancedDataGridControl>();
            var uiOperationLogger = serviceProvider.GetService<ILogger<UIAdapters.WinUI.InternalUIOperationHandler>>();
            _internalUIOperationHandler = new UIAdapters.WinUI.InternalUIOperationHandler(
                this, // Pass facade reference
                options,
                uiControl,
                uiOperationLogger
            );
        }

        // PROFESSIONAL: Create InternalUISortHandler to enable automatic sort operations
        // This handler automatically processes sort requests from header clicks without application code involvement
        // ACTIVE in all operation modes (Interactive, Headless, Readonly) - sorting is always allowed
        // NOTE: Needs reference to facade (this) and ViewModel, so created here instead of DI
        {
            var viewModel = serviceProvider.GetService<ViewModels.DataGridViewModel>();
            if (viewModel != null)
            {
                var uiSortLogger = serviceProvider.GetService<ILogger<UIAdapters.WinUI.InternalUISortHandler>>();
                _internalUISortHandler = new UIAdapters.WinUI.InternalUISortHandler(
                    this, // Pass facade reference
                    viewModel,
                    uiSortLogger
                );
                _logger.LogInformation("InternalUISortHandler initialized for automatic sort handling");
            }
            else
            {
                _logger.LogWarning("DataGridViewModel not available - InternalUISortHandler not created");
            }
        }

        // PROFESSIONAL: Inject FilterFlyoutService into DataGridViewModel
        // This enables HeadersRowView to trigger filter operations (checkbox + regex modes)
        // ACTIVE in all operation modes (Interactive, Headless, Readonly) - filtering is always allowed
        {
            var viewModel = serviceProvider.GetService<ViewModels.DataGridViewModel>();
            if (viewModel != null)
            {
                var rowStore = serviceProvider.GetRequiredService<Infrastructure.Persistence.Interfaces.IRowStore>();
                var filterLogger = serviceProvider.GetService<ILogger<Features.Filter.Services.FilterFlyoutService>>();
                var uiNotificationService = serviceProvider.GetService<UIAdapters.WinUI.UiNotificationService>();

                // ✅ PROFESSIONAL FIX: Include UiNotificationService for UI refresh after filter
                var filterFlyoutService = new Features.Filter.Services.FilterFlyoutService(
                    filterLogger,
                    rowStore,
                    uiNotificationService);

                // ✅ Inject FilterFlyoutService into ViewModel (accessible from HeadersRowView)
                viewModel.FilterFlyoutService = filterFlyoutService;
                _logger.LogInformation("FilterFlyoutService created and injected into DataGridViewModel (with UI refresh support)");

                // ✅ PROFESSIONAL FIX: Inject Facade reference into ViewModel (accessible from DataGridCellsView)
                // REASON: Enables realtime preview validation during edit mode (keystroke validation)
                // USE CASE: User types in cell → DataGridCellsView calls facade.CellEdit.PreviewValidateCellAsync()
                viewModel.Facade = this;
                _logger.LogInformation("Facade reference injected into DataGridViewModel (enables preview validation)");
            }
            else
            {
                _logger.LogWarning("DataGridViewModel not available - FilterFlyoutService and Facade not injected");
            }
        }

        // Obtain ThemeService (always available)
        _themeService = serviceProvider.GetRequiredService<Features.Color.ThemeService>();

        // Obtain ColumnSchemaService (always available)
        _columnSchemaService = serviceProvider.GetRequiredService<Features.Schema.ColumnSchemaService>();

        // Obtain feature modules via DI
        _columns = serviceProvider.GetRequiredService<Columns.IDataGridColumns>();
        _editing = serviceProvider.GetRequiredService<Editing.IDataGridEditing>();
        _filtering = serviceProvider.GetRequiredService<Filtering.IDataGridFiltering>();
        _selection = serviceProvider.GetRequiredService<Selection.IDataGridSelection>();
        _sorting = serviceProvider.GetRequiredService<Sorting.IDataGridSorting>();
        _configuration = serviceProvider.GetRequiredService<Configuration.IDataGridConfiguration>();
        _rows = serviceProvider.GetRequiredService<Rows.IDataGridRows>();
        _batch = serviceProvider.GetRequiredService<Batch.IDataGridBatch>();
        _io = serviceProvider.GetRequiredService<IO.IDataGridIO>();
        _clipboard = serviceProvider.GetRequiredService<Clipboard.IDataGridClipboard>();
        _search = serviceProvider.GetRequiredService<Search.IDataGridSearch>();
        _validation = serviceProvider.GetRequiredService<Validation.IDataGridValidation>();
        _performance = serviceProvider.GetRequiredService<Performance.IDataGridPerformance>();
        _theming = serviceProvider.GetRequiredService<Theming.IDataGridTheming>();
        _notifications = serviceProvider.GetRequiredService<Notifications.IDataGridNotifications>();
        _autoRowHeight = serviceProvider.GetRequiredService<AutoRowHeight.IDataGridAutoRowHeight>();
        _shortcuts = serviceProvider.GetRequiredService<Shortcuts.IDataGridShortcuts>();
        _mvvm = serviceProvider.GetRequiredService<MVVM.IDataGridMVVM>();
        // REMOVED: SmartOperations - replaced by RowManagement feature
        // _smartOperations = serviceProvider.GetRequiredService<SmartOperations.IDataGridSmartOperations>();
        _environment = serviceProvider.GetRequiredService<Environments.IEnvironmentConfiguration>();
        _colors = serviceProvider.GetRequiredService<IDataGridColors>();
        _theme = serviceProvider.GetRequiredService<IDataGridTheme>();

        _logger.LogInformation("AdvancedDataGrid facade initialized with operation mode {OperationMode}", _options.OperationMode);
    }

    /// <summary>
    /// Helper method to check if a feature is enabled
    /// </summary>
    private bool IsFeatureEnabled(GridFeature feature)
    {
        return _options.EnabledFeatures.Contains(feature);
    }

    /// <summary>
    /// Helper method to throw exception if feature is disabled
    /// </summary>
    private void EnsureFeatureEnabled(GridFeature feature, string operationName)
    {
        if (!IsFeatureEnabled(feature))
        {
            var message = $"Feature '{feature}' is disabled. Operation '{operationName}' cannot be executed.";
            _logger.LogWarning(message);
            throw new InvalidOperationException(message);
        }
    }

    #region UI Control Access

    /// <summary>
    /// Gets the UI control for the DataGrid (Interactive mode only).
    /// In Interactive mode, the component manages its own ViewModel and UI control with automatic updates.
    /// </summary>
    /// <returns>The AdvancedDataGridControl instance, or null if in Headless mode</returns>
    /// <exception cref="InvalidOperationException">Thrown when called in Headless mode or when DispatcherQueue is not provided</exception>
    public UIControls.AdvancedDataGridControl? GetUIControl()
    {
        ThrowIfDisposed();

        if (_options.OperationMode != PublicDataGridOperationMode.Interactive)
        {
            _logger.LogWarning("GetUIControl() called in {Mode} mode - UI control is only available in Interactive mode", _options.OperationMode);
            return null;
        }

        if (_dispatcher == null)
        {
            _logger.LogError("GetUIControl() called but DispatcherQueue was not provided - UI control requires DispatcherQueue");
            throw new InvalidOperationException("UI control requires DispatcherQueue. Provide DispatcherQueue in AdvancedDataGridOptions when creating facade.");
        }

        // Get UI control from DI container
        var uiControl = _serviceProvider.GetService<UIControls.AdvancedDataGridControl>();

        if (uiControl == null)
        {
            _logger.LogError("Failed to retrieve UI control from DI container - this should not happen in Interactive mode");
            throw new InvalidOperationException("UI control not registered in DI container. This is an internal error.");
        }

        _logger.LogInformation("UI control retrieved successfully");
        return uiControl;
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes the facade and all its resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing AdvancedDataGrid facade");

            try
            {
                // Dispose handlers first (unsubscribe from events)
                _internalUIUpdateHandler?.Dispose();
                _internalUIOperationHandler?.Dispose();
                _internalUISortHandler?.Dispose();

                // Dispose of service provider if it's disposable
                if (_serviceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
                else if (_serviceProvider is IAsyncDisposable asyncDisposableProvider)
                {
                    await asyncDisposableProvider.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during facade disposal");
            }

            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AdvancedDataGridFacade));
        }
    }

    #endregion

    #region Column Schema Management

    /// <summary>
    /// Defines column schema with type information and validation rules.
    /// BREAKING CHANGE v4.0: Enables typed columns with DataType enforcement.
    /// </summary>
    public async Task<PublicResult> DefineColumnsAsync(
        IEnumerable<ColumnDefinition> columns,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation("Defining column schema...");

            // Validate and normalize schema via ColumnSchemaService
            var result = _columnSchemaService.DefineColumns(columns);
            if (!result.IsSuccess)
            {
                _logger.LogError("Column schema definition failed: {Error}",
                    result.ErrorMessage);
                return PublicResult.Failure(result.ErrorMessage ?? "Unknown error");
            }

            _logger.LogInformation("✓ Column schema defined successfully: {ColumnCount} columns",
                result.Value.Count);

            // Return on background thread (async operation)
            await Task.CompletedTask;

            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during column schema definition");
            return PublicResult.Failure($"Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current column schema (read-only).
    /// Returns empty list if no schema has been defined via DefineColumnsAsync.
    /// </summary>
    public IReadOnlyList<ColumnDefinition> GetColumnSchema()
    {
        ThrowIfDisposed();
        return _columnSchemaService.GetSchema();
    }

    #endregion
}

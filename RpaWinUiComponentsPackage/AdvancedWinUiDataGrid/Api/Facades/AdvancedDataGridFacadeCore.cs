using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
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
    private readonly Features.Color.ThemeService _themeService;
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
    private readonly SmartOperations.IDataGridSmartOperations _smartOperations;

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

    /// <summary>
    /// Smart row management operations (add/delete with minimum rows)
    /// </summary>
    public SmartOperations.IDataGridSmartOperations SmartOperations => _smartOperations;

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

        // Obtain ThemeService (always available)
        _themeService = serviceProvider.GetRequiredService<Features.Color.ThemeService>();

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
        _smartOperations = serviceProvider.GetRequiredService<SmartOperations.IDataGridSmartOperations>();

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
}

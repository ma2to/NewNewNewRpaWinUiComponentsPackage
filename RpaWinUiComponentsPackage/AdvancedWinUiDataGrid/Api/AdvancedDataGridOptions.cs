using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Configuration options for the AdvancedDataGrid component
/// </summary>
public class AdvancedDataGridOptions
{
    /// <summary>
    /// Gets or sets the operation mode (UI or Headless)
    /// </summary>
    public PublicDataGridOperationMode OperationMode { get; set; } = PublicDataGridOperationMode.Interactive;

    /// <summary>
    /// Gets or sets whether the validation alerts column is enabled
    /// </summary>
    public bool EnableValidationAlertsColumn { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the row number column is enabled
    /// </summary>
    public bool EnableRowNumberColumn { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the checkbox column is enabled
    /// </summary>
    public bool EnableCheckboxColumn { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the delete row column is enabled
    /// </summary>
    public bool EnableDeleteRowColumn { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum width for the validation alerts column
    /// </summary>
    public double ValidationAlertsColumnMinWidth { get; set; } = 150.0;

    /// <summary>
    /// Gets or sets whether batch validation is automatically executed during Import/Export/Paste operations
    /// When true: batch validation happens automatically during bulk operations
    /// When false: batch validation only happens on explicit demand via ValidateAllAsync()
    /// </summary>
    public bool EnableBatchValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether real-time validation is enabled during cell editing
    /// When true: validation happens automatically during cell editing
    /// When false: real-time validation is disabled
    /// Note: This setting is only effective when ValidationAutomationMode = Automatic
    /// </summary>
    public bool EnableRealTimeValidation { get; set; } = true;

    /// <summary>
    /// SENIOR FIX: Gets or sets whether validation should stop on first error for each cell.
    /// When true: Stops validating a cell after the first validation error (but continues with other cells)
    /// When false (default): Applies ALL validation rules to each cell and collects all errors
    /// Example: If cell has 3 validation rules and StopOnFirstError=true, stops after first rule fails
    /// Note: This only affects PER-CELL validation - other cells are still validated
    /// </summary>
    public bool ValidationStopOnFirstError { get; set; } = false;

    /// <summary>
    /// Gets or sets the validation automation mode
    /// Automatic (default): Validates automatically on import/paste/edit/row changes
    /// Manual: Validates only via explicit ValidateAllAsync() or similar calls
    /// Note: When set to Manual, EnableBatchValidation and EnableRealTimeValidation are ignored
    /// </summary>
    public ValidationAutomationMode ValidationAutomationMode { get; set; } = ValidationAutomationMode.Automatic;

    /// <summary>
    /// Gets or sets the validation strategy
    /// </summary>
    public PublicValidationStrategy ValidationStrategy { get; set; } = PublicValidationStrategy.OnInput;

    /// <summary>
    /// Gets or sets the set of enabled grid features
    /// Features not in this set will be completely disabled (not available even on demand)
    /// Default: all features are enabled
    /// </summary>
    public HashSet<GridFeature> EnabledFeatures { get; set; } = new()
    {
        GridFeature.Sort,
        GridFeature.Search,
        GridFeature.Filter,
        GridFeature.Import,
        GridFeature.Export,
        GridFeature.Validation,
        GridFeature.CopyPaste,
        GridFeature.CellEdit,
        GridFeature.RowColumnOperations,
        GridFeature.ColumnResize,
        GridFeature.Performance,
        GridFeature.Color,
        GridFeature.Shortcuts,
        GridFeature.SmartOperations,
        GridFeature.AutoRowHeight,
        GridFeature.RowNumbering,
        GridFeature.Selection,
        GridFeature.SpecialColumns,
        GridFeature.UI,
        GridFeature.Security,
        GridFeature.Logging,
        GridFeature.ExceptionHandling,
        GridFeature.Configuration
    };

    /// <summary>
    /// Gets or sets the default timeout for operations
    /// </summary>
    public TimeSpan DefaultOperationTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the batch size for bulk operations
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the batch size for import operations
    /// </summary>
    public int ImportBatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the batch size for export operations
    /// </summary>
    public int ExportBatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum selection size limit
    /// </summary>
    public int MaxSelectionSize { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the estimated row count for performance optimizations
    /// </summary>
    public int EstimatedRowCount { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the grace period before user-inserted empty rows can be automatically deleted.
    /// Cleanup is executed: after import/add/paste/smartdelete + before export.
    /// Default: 3 minutes
    /// Set TimeSpan.MaxValue = never delete user-inserted empty rows
    /// </summary>
    public TimeSpan UserEmptyRowGracePeriod { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Gets or sets whether auto-generated empty rows are automatically deleted (except last row) during cleanup.
    /// Default: true
    /// </summary>
    public bool CleanupAutoGeneratedEmptyRows { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the insert row column is enabled
    /// </summary>
    public bool EnableInsertRowColumn { get; set; } = false;

    /// <summary>
    /// Gets or sets whether parallel processing is enabled for large datasets
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold for enabling parallel processing
    /// </summary>
    public int ParallelProcessingThreshold { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the degree of parallelism
    /// </summary>
    public int? DegreeOfParallelism { get; set; }

    /// <summary>
    /// Gets or sets whether LINQ optimizations are enabled
    /// </summary>
    public bool EnableLinqOptimizations { get; set; } = true;

    /// <summary>
    /// Gets or sets whether caching is enabled
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache expiration time
    /// </summary>
    public TimeSpan CacheExpirationTime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets whether comprehensive logging is enabled
    /// </summary>
    public bool EnableComprehensiveLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum log level for the component
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets whether structured logging is enabled
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether performance metrics logging is enabled
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the auto row height mode (Disabled, Enabled, Auto)
    /// Default: Disabled
    /// </summary>
    public PublicAutoRowHeightMode AutoRowHeightMode { get; set; } = PublicAutoRowHeightMode.Disabled;

    /// <summary>
    /// Gets or sets the minimum row height in pixels
    /// Used when AutoRowHeightMode is enabled
    /// Default: 25.0
    /// </summary>
    public double MinimumRowHeight { get; set; } = 25.0;

    /// <summary>
    /// Gets or sets the maximum row height in pixels
    /// Used when AutoRowHeightMode is enabled
    /// Default: 200.0
    /// </summary>
    public double MaximumRowHeight { get; set; } = 200.0;

    /// <summary>
    /// Gets or sets the minimum column width in pixels
    /// Used when resizing columns via drag & drop
    /// Default: 50.0
    /// </summary>
    public double MinimumColumnWidth { get; set; } = 50.0;

    /// <summary>
    /// Gets or sets the maximum column width in pixels
    /// Used when resizing columns via drag & drop
    /// Default: 500.0
    /// </summary>
    public double MaximumColumnWidth { get; set; } = 500.0;

    /// <summary>
    /// Gets or sets the checkbox border color (hex format: #RRGGBB or #AARRGGBB)
    /// Used for checkbox special column styling
    /// Default: #333333 (DarkGray - clearly visible on white background)
    /// </summary>
    public string CheckboxBorderColor { get; set; } = "#333333";

    /// <summary>
    /// Gets or sets the checkbox border thickness in pixels
    /// Used for checkbox special column styling
    /// Default: 2.0
    /// </summary>
    public double CheckboxBorderThickness { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the checkbox background color (hex format: #RRGGBB or #AARRGGBB)
    /// Used for checkbox special column styling
    /// Default: #FFFFFF (White)
    /// </summary>
    public string CheckboxBackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Gets or sets the minimum checkbox width in pixels
    /// Used for checkbox special column styling
    /// Default: 20.0
    /// </summary>
    public double CheckboxMinWidth { get; set; } = 20.0;

    /// <summary>
    /// Gets or sets the minimum checkbox height in pixels
    /// Used for checkbox special column styling
    /// Default: 20.0
    /// </summary>
    public double CheckboxMinHeight { get; set; } = 20.0;

    // ═══════════════════════════════════════════════════════════════════════════════
    // ZEBRA ROWS CONFIGURATION (FÁZA 6 - Alternate Row Colors)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets or sets whether zebra rows (alternate row background and foreground colors) are enabled
    /// When true: even rows use ZebraRowEvenBackgroundColor/ZebraRowEvenForegroundColor,
    ///            odd rows use ZebraRowOddBackgroundColor/ZebraRowOddForegroundColor
    /// When false: all rows use default cell colors from theme
    /// Default: false
    /// </summary>
    public bool EnableZebraRows { get; set; } = false;

    /// <summary>
    /// Gets or sets the background color for even rows (0, 2, 4, ...) when zebra rows are enabled
    /// Hex format: #RRGGBB or #AARRGGBB
    /// Default: #FFFFFF (white)
    /// </summary>
    public string ZebraRowEvenBackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Gets or sets the background color for odd rows (1, 3, 5, ...) when zebra rows are enabled
    /// Hex format: #RRGGBB or #AARRGGBB
    /// Default: #F5F5F5 (light gray)
    /// </summary>
    public string ZebraRowOddBackgroundColor { get; set; } = "#F5F5F5";

    /// <summary>
    /// Gets or sets the foreground (text) color for even rows (0, 2, 4, ...) when zebra rows are enabled
    /// Hex format: #RRGGBB or #AARRGGBB
    /// Default: #000000 (black)
    /// </summary>
    public string ZebraRowEvenForegroundColor { get; set; } = "#000000";

    /// <summary>
    /// Gets or sets the foreground (text) color for odd rows (1, 3, 5, ...) when zebra rows are enabled
    /// Hex format: #RRGGBB or #AARRGGBB
    /// Default: #000000 (black)
    /// </summary>
    public string ZebraRowOddForegroundColor { get; set; } = "#000000";

    /// <summary>
    /// Gets or sets the initial column definitions
    /// </summary>
    public List<PublicColumnDefinition> InitialColumns { get; set; } = new();

    /// <summary>
    /// Gets or sets custom properties for the grid
    /// </summary>
    public Dictionary<string, object?> CustomProperties { get; set; } = new();

    // ═══════════════════════════════════════════════════════════════════════════════
    // ADAPTIVE STORAGE CONFIGURATION
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ADAPTIVE STORAGE: Automatické prepínanie storage stratégie na základe row count
    /// TRUE (default): AdaptiveRowStore (automatic InMemory ↔ Hybrid switching)
    /// FALSE: InMemoryRowStore (static, legacy)
    /// NOTE: Ignoruje sa ak je nastavený RowStoreFactory (custom factory má prioritu)
    /// </summary>
    public bool UseAdaptiveStorage { get; set; } = true;

    /// <summary>
    /// THRESHOLD: Počet riadkov, od ktorých sa DÁTA prepnú z InMemory na SQLite
    /// Default: 100,000 rows
    /// Reasoning:
    ///   - < 100K: InMemory je rýchlejší (RAM spotreba prijateľná)
    ///   - >= 100K: SQLite šetrí RAM (50 MB → 10 MB) a rýchlejšie Filter/Sort/Search
    /// </summary>
    public int DataStorageThreshold { get; set; } = 100_000;

    /// <summary>
    /// THRESHOLD: Počet riadkov, od ktorých sa VALIDÁCIE prepnú z InMemory na SQLite
    /// Default: 1,000,000 rows
    /// Reasoning:
    ///   - < 1M: InMemory validácie sú rýchlejšie (Dictionary lookup < 1ms)
    ///   - >= 1M: SQLite šetrí RAM (630 MB → 50 MB) ale validačný lookup je pomalší (< 3ms)
    /// NOTE: ValidationService automaticky prepne na WriteValidationResultsAsync() pri >= threshold
    /// </summary>
    public int ValidationStorageThreshold { get; set; } = 1_000_000;

    /// <summary>
    /// DATABASE PATH: Cesta k SQLite databáze pre HybridRowStore
    /// SMART LOGIC (rovnako ako DatabaseLifecycleManager.ResolveDatabasePath):
    ///   - null → C:\Temp\AdvancedDataGrid\grid_{GUID}.db (default temp path)
    ///   - "C:\MyData\" → C:\MyData\grid_{GUID}.db (directory → auto-generate filename)
    ///   - "C:\MyData\my_grid.db" → C:\MyData\my_grid.db (explicit file path)
    ///   - "C:\MyData\file.txt" → ERROR (non-.db extension not allowed)
    /// </summary>
    public string? DatabasePath { get; set; } = null;

    /// <summary>
    /// VIEWPORT CACHE SIZE: Max počet riadkov v RAM viewport cache (HybridRowStore)
    /// Default: 1,000 rows
    /// Reasoning: UI virtualizácia zobrazuje max 1000 rows, zvyšok je v SQLite
    /// </summary>
    public int ViewportCacheSize { get; set; } = 1_000;

    /// <summary>
    /// Gets or sets the row store factory function (internal use only)
    /// </summary>
    internal Func<IServiceProvider, Infrastructure.Persistence.Interfaces.IRowStore>? RowStoreFactory { get; set; }

    /// <summary>
    /// Gets or sets the logger factory (for headless mode)
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// Gets or sets the dispatcher queue (for UI mode)
    /// </summary>
    public DispatcherQueue? DispatcherQueue { get; set; }

    /// <summary>
    /// Enables or disables a specific grid feature
    /// </summary>
    /// <param name="feature">The feature to enable or disable</param>
    /// <param name="enabled">True to enable, false to disable</param>
    public void SetFeatureEnabled(GridFeature feature, bool enabled)
    {
        if (enabled)
        {
            EnabledFeatures.Add(feature);
        }
        else
        {
            EnabledFeatures.Remove(feature);
        }
    }

    /// <summary>
    /// Checks if a specific grid feature is enabled
    /// </summary>
    /// <param name="feature">The feature to check</param>
    /// <returns>True if the feature is enabled, false otherwise</returns>
    public bool IsFeatureEnabled(GridFeature feature)
    {
        return EnabledFeatures.Contains(feature);
    }

    /// <summary>
    /// Enables multiple grid features at once
    /// </summary>
    /// <param name="features">Features to enable</param>
    public void EnableFeatures(params GridFeature[] features)
    {
        foreach (var feature in features)
        {
            EnabledFeatures.Add(feature);
        }
    }

    /// <summary>
    /// Disables multiple grid features at once
    /// </summary>
    /// <param name="features">Features to disable</param>
    public void DisableFeatures(params GridFeature[] features)
    {
        foreach (var feature in features)
        {
            EnabledFeatures.Remove(feature);
        }
    }

    /// <summary>
    /// Creates a copy of this options instance
    /// </summary>
    /// <returns>New options instance with copied values</returns>
    public AdvancedDataGridOptions Clone()
    {
        return new AdvancedDataGridOptions
        {
            OperationMode = this.OperationMode,
            EnableValidationAlertsColumn = this.EnableValidationAlertsColumn,
            EnableRowNumberColumn = this.EnableRowNumberColumn,
            EnableCheckboxColumn = this.EnableCheckboxColumn,
            EnableDeleteRowColumn = this.EnableDeleteRowColumn,
            EnableInsertRowColumn = this.EnableInsertRowColumn,
            ValidationAlertsColumnMinWidth = this.ValidationAlertsColumnMinWidth,
            UserEmptyRowGracePeriod = this.UserEmptyRowGracePeriod,
            CleanupAutoGeneratedEmptyRows = this.CleanupAutoGeneratedEmptyRows,
            EnableBatchValidation = this.EnableBatchValidation,
            EnableRealTimeValidation = this.EnableRealTimeValidation,
            ValidationAutomationMode = this.ValidationAutomationMode,
            ValidationStrategy = this.ValidationStrategy,
            EnabledFeatures = new HashSet<GridFeature>(this.EnabledFeatures),
            DefaultOperationTimeout = this.DefaultOperationTimeout,
            BatchSize = this.BatchSize,
            EnableParallelProcessing = this.EnableParallelProcessing,
            ParallelProcessingThreshold = this.ParallelProcessingThreshold,
            DegreeOfParallelism = this.DegreeOfParallelism,
            EnableLinqOptimizations = this.EnableLinqOptimizations,
            EnableCaching = this.EnableCaching,
            CacheExpirationTime = this.CacheExpirationTime,
            EnableComprehensiveLogging = this.EnableComprehensiveLogging,
            MinimumLogLevel = this.MinimumLogLevel,
            EnableStructuredLogging = this.EnableStructuredLogging,
            EnablePerformanceMetrics = this.EnablePerformanceMetrics,
            AutoRowHeightMode = this.AutoRowHeightMode,
            MinimumRowHeight = this.MinimumRowHeight,
            MaximumRowHeight = this.MaximumRowHeight,
            MinimumColumnWidth = this.MinimumColumnWidth,
            MaximumColumnWidth = this.MaximumColumnWidth,
            CheckboxBorderColor = this.CheckboxBorderColor,
            CheckboxBorderThickness = this.CheckboxBorderThickness,
            CheckboxBackgroundColor = this.CheckboxBackgroundColor,
            CheckboxMinWidth = this.CheckboxMinWidth,
            CheckboxMinHeight = this.CheckboxMinHeight,
            InitialColumns = new List<PublicColumnDefinition>(this.InitialColumns),
            CustomProperties = new Dictionary<string, object?>(this.CustomProperties),
            RowStoreFactory = this.RowStoreFactory,
            LoggerFactory = this.LoggerFactory,
            DispatcherQueue = this.DispatcherQueue
        };
    }

    /// <summary>
    /// Gets a custom property value
    /// </summary>
    /// <typeparam name="T">Type of the property</typeparam>
    /// <param name="key">Property key</param>
    /// <returns>Property value or default if not found</returns>
    public T? GetCustomProperty<T>(string key)
    {
        if (CustomProperties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Sets a custom property value
    /// </summary>
    /// <param name="key">Property key</param>
    /// <param name="value">Property value</param>
    public void SetCustomProperty(string key, object? value)
    {
        CustomProperties[key] = value;
    }

    /// <summary>
    /// Validates the options configuration
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        return BatchSize > 0 &&
               ParallelProcessingThreshold > 0 &&
               DefaultOperationTimeout > TimeSpan.Zero &&
               CacheExpirationTime > TimeSpan.Zero &&
               ValidationAlertsColumnMinWidth > 0 &&
               (DegreeOfParallelism == null || DegreeOfParallelism > 0) &&
               MinimumRowHeight > 0 &&
               MaximumRowHeight > MinimumRowHeight;
    }
}
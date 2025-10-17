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

    /// <summary>
    /// Gets or sets the initial column definitions
    /// </summary>
    public List<PublicColumnDefinition> InitialColumns { get; set; } = new();

    /// <summary>
    /// Gets or sets custom properties for the grid
    /// </summary>
    public Dictionary<string, object?> CustomProperties { get; set; } = new();

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
            ValidationAlertsColumnMinWidth = this.ValidationAlertsColumnMinWidth,
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
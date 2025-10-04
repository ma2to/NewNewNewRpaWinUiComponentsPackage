using System.Data;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public interface for the AdvancedDataGrid facade providing comprehensive data grid functionality
/// </summary>
public interface IAdvancedDataGridFacade : IAsyncDisposable
{
    /// <summary>
    /// Imports data using command pattern with LINQ optimization and validation pipeline
    /// </summary>
    /// <param name="command">Import command with data and configuration</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Import PublicResult with metrics and status</returns>
    Task<ImportResult> ImportAsync(ImportDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports data using command pattern with comprehensive filtering
    /// </summary>
    /// <param name="command">Export command with configuration</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Export PublicResult with metrics and status</returns>
    Task<ExportResult> ExportAsync(ExportDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates all non-empty rows with batched, thread-safe processing
    /// </summary>
    /// <param name="onlyFiltered">Whether to validate only filtered rows</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Validation PublicResult indicating if all rows are valid</returns>
    Task<PublicResult<bool>> ValidateAllAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates all non-empty rows with detailed statistics tracking
    /// Provides performance insights including rule execution times and error rates
    /// </summary>
    /// <param name="onlyFiltered">Whether to validate only filtered rows</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Validation result with detailed rule statistics and performance metrics</returns>
    Task<PublicValidationResultWithStatistics> ValidateAllWithStatisticsAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes validation results to UI (no-op in headless mode)
    /// </summary>
    void RefreshValidationResultsToUI();

    /// <summary>
    /// Manually refreshes UI after operations
    /// Available in both Interactive and Headless modes (if DispatcherQueue is provided)
    /// - Interactive mode: Automatic UI refresh after operations + manual via this method
    /// - Headless mode: NO automatic refresh, ONLY manual via this method
    /// </summary>
    /// <param name="operationType">Type of operation that triggered refresh (default: "ManualRefresh")</param>
    /// <param name="affectedRows">Number of affected rows (default: 0)</param>
    /// <returns>Task representing the asynchronous operation</returns>
    /// <exception cref="InvalidOperationException">Thrown when DispatcherQueue was not provided in options</exception>
    Task RefreshUIAsync(string operationType = "ManualRefresh", int affectedRows = 0);

    #region MVVM Transformations

    /// <summary>
    /// Adapts raw row data to UI-friendly view model for MVVM binding
    /// </summary>
    /// <param name="rowData">Row data to transform</param>
    /// <param name="rowIndex">Row index</param>
    /// <returns>Row view model ready for XAML binding</returns>
    PublicRowViewModel AdaptToRowViewModel(IReadOnlyDictionary<string, object?> rowData, int rowIndex);

    /// <summary>
    /// Adapts multiple rows to view models for MVVM binding
    /// </summary>
    /// <param name="rows">Rows to transform</param>
    /// <param name="startIndex">Starting row index (default: 0)</param>
    /// <returns>Collection of row view models</returns>
    IReadOnlyList<PublicRowViewModel> AdaptToRowViewModels(
        IEnumerable<IReadOnlyDictionary<string, object?>> rows,
        int startIndex = 0);

    /// <summary>
    /// Adapts column definition to UI-friendly view model for MVVM binding
    /// </summary>
    /// <param name="columnDefinition">Column definition to transform</param>
    /// <returns>Column view model ready for XAML binding</returns>
    PublicColumnViewModel AdaptToColumnViewModel(PublicColumnDefinition columnDefinition);

    /// <summary>
    /// Adapts validation errors to UI-friendly view models
    /// This is primarily a convenience method for transforming collections
    /// </summary>
    /// <param name="errors">Validation error view models to transform</param>
    /// <returns>Collection of validation error view models (same as input for compatibility)</returns>
    IReadOnlyList<PublicValidationErrorViewModel> AdaptValidationErrors(
        IReadOnlyList<PublicValidationErrorViewModel> errors);

    #endregion

    #region UI Notification Subscriptions

    /// <summary>
    /// Subscribes to validation refresh notifications
    /// Allows application to receive real-time updates when validation results change
    /// </summary>
    /// <param name="handler">Handler to invoke when validation results are refreshed</param>
    /// <returns>Disposable subscription that unsubscribes when disposed</returns>
    /// <exception cref="InvalidOperationException">Thrown when UiNotificationService is not available</exception>
    IDisposable SubscribeToValidationRefresh(Action<PublicValidationRefreshEventArgs> handler);

    /// <summary>
    /// Subscribes to data refresh notifications
    /// Allows application to receive real-time updates when data changes
    /// </summary>
    /// <param name="handler">Handler to invoke when data is refreshed</param>
    /// <returns>Disposable subscription that unsubscribes when disposed</returns>
    /// <exception cref="InvalidOperationException">Thrown when UiNotificationService is not available</exception>
    IDisposable SubscribeToDataRefresh(Action<PublicDataRefreshEventArgs> handler);

    /// <summary>
    /// Subscribes to operation progress notifications
    /// Allows application to track progress of long-running operations
    /// </summary>
    /// <param name="handler">Handler to invoke when operation progress is updated</param>
    /// <returns>Disposable subscription that unsubscribes when disposed</returns>
    /// <exception cref="InvalidOperationException">Thrown when UiNotificationService is not available</exception>
    IDisposable SubscribeToOperationProgress(Action<PublicOperationProgressEventArgs> handler);

    #endregion

    /// <summary>
    /// Sets clipboard content for copy/paste operations
    /// </summary>
    /// <param name="payload">Payload to store in clipboard</param>
    void SetClipboard(object payload);

    /// <summary>
    /// Gets clipboard content for copy/paste operations
    /// </summary>
    /// <returns>Clipboard content or null if empty</returns>
    object? GetClipboard();

    /// <summary>
    /// Copies selected data to clipboard
    /// </summary>
    /// <param name="command">Copy command with selection data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Copy operation result</returns>
    Task<CopyPasteResult> CopyAsync(CopyDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pastes data from clipboard
    /// </summary>
    /// <param name="command">Paste command with clipboard data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paste operation result</returns>
    Task<CopyPasteResult> PasteAsync(PasteDataCommand command, CancellationToken cancellationToken = default);

    // Column Resize APIs
    /// <summary>
    /// Resizes a column to a specific width
    /// Enforces min/max width constraints from options
    /// </summary>
    /// <param name="columnIndex">Index of the column to resize</param>
    /// <param name="newWidth">New width in pixels</param>
    /// <returns>Actual width applied after constraint enforcement</returns>
    double ResizeColumn(int columnIndex, double newWidth);

    /// <summary>
    /// Starts column resize operation (called when mouse down on column border)
    /// </summary>
    /// <param name="columnIndex">Index of column to resize</param>
    /// <param name="clientX">Initial mouse X position</param>
    void StartColumnResize(int columnIndex, double clientX);

    /// <summary>
    /// Updates column width during drag (called on mouse move)
    /// Debounced for performance
    /// </summary>
    /// <param name="clientX">Current mouse X position</param>
    void UpdateColumnResize(double clientX);

    /// <summary>
    /// Ends column resize operation (called on mouse up)
    /// </summary>
    void EndColumnResize();

    /// <summary>
    /// Gets the current width of a column
    /// </summary>
    /// <param name="columnIndex">Index of the column</param>
    /// <returns>Current width in pixels, or 0 if invalid index</returns>
    double GetColumnWidth(int columnIndex);

    /// <summary>
    /// Checks if a resize operation is currently active
    /// </summary>
    /// <returns>True if resizing, false otherwise</returns>
    bool IsResizing();

    /// <summary>
    /// Starts drag selection operation
    /// </summary>
    /// <param name="row">Starting row index</param>
    /// <param name="col">Starting column index</param>
    void StartDragSelect(int row, int col);

    /// <summary>
    /// Updates drag selection to new position
    /// </summary>
    /// <param name="row">Current row index</param>
    /// <param name="col">Current column index</param>
    void DragSelectTo(int row, int col);

    /// <summary>
    /// Ends drag selection operation
    /// </summary>
    /// <param name="row">Final row index</param>
    /// <param name="col">Final column index</param>
    void EndDragSelect(int row, int col);

    /// <summary>
    /// Selects a specific cell
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="col">Column index</param>
    void SelectCell(int row, int col);

    /// <summary>
    /// Toggles cell selection state
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="col">Column index</param>
    void ToggleCellSelection(int row, int col);

    /// <summary>
    /// Extends selection to specified cell
    /// </summary>
    /// <param name="row">Row index</param>
    /// <param name="col">Column index</param>
    void ExtendSelectionTo(int row, int col);

    // Data access APIs
    /// <summary>
    /// Gets current grid data as read-only dictionary collection
    /// </summary>
    /// <returns>Current data in the grid</returns>
    IReadOnlyList<IReadOnlyDictionary<string, object?>> GetCurrentData();

    /// <summary>
    /// Gets current grid data as DataTable
    /// </summary>
    /// <returns>Current data as DataTable</returns>
    Task<DataTable> GetCurrentDataAsDataTableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets column definitions
    /// </summary>
    /// <returns>Collection of column definitions</returns>
    IReadOnlyList<PublicColumnDefinition> GetColumnDefinitions();

    /// <summary>
    /// Adds a new column definition
    /// </summary>
    /// <param name="PublicColumnDefinition">Column definition to add</param>
    /// <returns>True if added successfully</returns>
    bool AddColumn(PublicColumnDefinition columnDefinition);

    /// <summary>
    /// Removes a column by name
    /// </summary>
    /// <param name="columnName">Name of column to remove</param>
    /// <returns>True if removed successfully</returns>
    bool RemoveColumn(string columnName);

    /// <summary>
    /// Updates an existing column definition
    /// </summary>
    /// <param name="PublicColumnDefinition">Updated column definition</param>
    /// <returns>True if updated successfully</returns>
    bool UpdateColumn(PublicColumnDefinition columnDefinition);

    // Validation APIs
    /// <summary>
    /// Adds a validation rule
    /// </summary>
    /// <param name="rule">Validation rule to add</param>
    /// <returns>PublicResult of the operation</returns>
    Task<PublicResult> AddValidationRuleAsync(IValidationRule rule);

    /// <summary>
    /// Removes validation rules by column names
    /// </summary>
    /// <param name="columnNames">Column names to remove rules for</param>
    /// <returns>PublicResult of the operation</returns>
    Task<PublicResult> RemoveValidationRulesAsync(params string[] columnNames);

    /// <summary>
    /// Removes a validation rule by name
    /// </summary>
    /// <param name="ruleName">Name of rule to remove</param>
    /// <returns>PublicResult of the operation</returns>
    Task<PublicResult> RemoveValidationRuleAsync(string ruleName);

    /// <summary>
    /// Clears all validation rules
    /// </summary>
    /// <returns>PublicResult of the operation</returns>
    Task<PublicResult> ClearAllValidationRulesAsync();

    // Filter APIs
    /// <summary>
    /// Applies a filter to the data
    /// </summary>
    /// <param name="columnName">Column to filter</param>
    /// <param name="operator">Filter operator</param>
    /// <param name="value">Filter value</param>
    /// <returns>Number of rows matching the filter</returns>
    Task<int> ApplyFilterAsync(string columnName, PublicFilterOperator @operator, object? value);

    /// <summary>
    /// Clears all filters
    /// </summary>
    /// <returns>Number of visible rows after clearing filters</returns>
    Task<int> ClearFiltersAsync();

    // Sort APIs
    /// <summary>
    /// Sorts data by single column using command pattern
    /// </summary>
    /// <param name="command">Sort command with data and configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the sort operation with sorted data</returns>
    Task<SortDataResult> SortAsync(SortDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sorts data by multiple columns using command pattern
    /// </summary>
    /// <param name="command">Multi-sort command with columns and configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the sort operation with sorted data</returns>
    Task<SortDataResult> MultiSortAsync(MultiSortDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quick synchronous sort for immediate results
    /// </summary>
    /// <param name="data">Data to sort</param>
    /// <param name="columnName">Column to sort by</param>
    /// <param name="direction">Sort direction</param>
    /// <returns>PublicResult of the sort operation with sorted data</returns>
    SortDataResult QuickSort(IEnumerable<IReadOnlyDictionary<string, object?>> data, string columnName, PublicSortDirection direction = PublicSortDirection.Ascending);

    /// <summary>
    /// Gets sortable columns from data
    /// </summary>
    /// <param name="data">Data to analyze</param>
    /// <returns>List of sortable column names</returns>
    IReadOnlyList<string> GetSortableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data);

    /// <summary>
    /// Sorts data by column (legacy compatibility)
    /// </summary>
    /// <param name="columnName">Column to sort by</param>
    /// <param name="direction">Sort direction</param>
    /// <returns>PublicResult of the sort operation</returns>
    Task<PublicResult> SortByColumnAsync(string columnName, PublicSortDirection direction);

    /// <summary>
    /// Clears all sorting
    /// </summary>
    /// <returns>PublicResult of the operation</returns>
    Task<PublicResult> ClearSortingAsync();

    #region Search Operations

    /// <summary>
    /// Performs basic search operation
    /// </summary>
    /// <param name="command">Search command with data and criteria</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Search PublicResult with matched data</returns>
    Task<SearchDataResult> SearchAsync(SearchDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs advanced search with regex, fuzzy matching, and smart ranking
    /// </summary>
    /// <param name="command">Advanced search command with comprehensive criteria</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Search PublicResult with matched data and statistics</returns>
    Task<SearchDataResult> AdvancedSearchAsync(AdvancedSearchDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs smart search with automatic optimization
    /// </summary>
    /// <param name="command">Smart search command</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Search PublicResult with optimized search strategy</returns>
    Task<SearchDataResult> SmartSearchAsync(SmartSearchDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quick synchronous search for immediate results
    /// </summary>
    /// <param name="data">Data to search</param>
    /// <param name="searchText">Text to search for</param>
    /// <param name="caseSensitive">Case-sensitive search</param>
    /// <returns>Search PublicResult with matched data</returns>
    SearchDataResult QuickSearch(IEnumerable<IReadOnlyDictionary<string, object?>> data, string searchText, bool caseSensitive = false);

    /// <summary>
    /// Validates search criteria
    /// </summary>
    /// <param name="searchCriteria">Search criteria to validate</param>
    /// <returns>Validation result</returns>
    Task<PublicResult> ValidateSearchCriteriaAsync(PublicAdvancedSearchCriteria searchCriteria);

    /// <summary>
    /// Gets searchable columns from data
    /// </summary>
    /// <param name="data">Data to analyze</param>
    /// <returns>List of searchable column names</returns>
    IReadOnlyList<string> GetSearchableColumns(IEnumerable<IReadOnlyDictionary<string, object?>> data);

    #endregion

    // Row management APIs
    /// <summary>
    /// Adds a new row
    /// </summary>
    /// <param name="rowData">Data for the new row</param>
    /// <returns>Index of the added row</returns>
    Task<int> AddRowAsync(IReadOnlyDictionary<string, object?> rowData);

    /// <summary>
    /// Removes a row by index
    /// </summary>
    /// <param name="rowIndex">Index of row to remove</param>
    /// <returns>True if removed successfully</returns>
    Task<bool> RemoveRowAsync(int rowIndex);

    /// <summary>
    /// Updates a row by index
    /// </summary>
    /// <param name="rowIndex">Index of row to update</param>
    /// <param name="rowData">New data for the row</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateRowAsync(int rowIndex, IReadOnlyDictionary<string, object?> rowData);

    /// <summary>
    /// Gets row data by index
    /// </summary>
    /// <param name="rowIndex">Index of row to get</param>
    /// <returns>Row data or null if not found</returns>
    IReadOnlyDictionary<string, object?>? GetRow(int rowIndex);

    /// <summary>
    /// Gets the total number of rows
    /// </summary>
    /// <returns>Total row count</returns>
    int GetRowCount();

    /// <summary>
    /// Gets the number of visible rows (after filtering)
    /// </summary>
    /// <returns>Visible row count</returns>
    int GetVisibleRowCount();

    #region AutoRowHeight API

    /// <summary>
    /// Enables automatic row height calculation with configuration
    /// </summary>
    /// <param name="configuration">Auto row height configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<PublicAutoRowHeightResult> EnableAutoRowHeightAsync(
        PublicAutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates optimal row heights for all rows
    /// </summary>
    /// <param name="progress">Progress reporting callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Row height calculation results</returns>
    Task<IReadOnlyList<PublicRowHeightCalculationResult>> CalculateOptimalRowHeightsAsync(
        IProgress<PublicBatchCalculationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate height for specific row
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <param name="rowData">Row data to measure</param>
    /// <param name="options">Calculation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Row height calculation result</returns>
    Task<PublicRowHeightCalculationResult> CalculateRowHeightAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        PublicRowHeightCalculationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Measure text dimensions for height calculation
    /// </summary>
    /// <param name="text">Text to measure</param>
    /// <param name="fontFamily">Font family</param>
    /// <param name="fontSize">Font size</param>
    /// <param name="maxWidth">Maximum width for wrapping</param>
    /// <param name="textWrapping">Enable text wrapping</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Text measurement result</returns>
    Task<PublicTextMeasurementResult> MeasureTextAsync(
        string text,
        string fontFamily,
        double fontSize,
        double maxWidth,
        bool textWrapping = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply new auto row height configuration
    /// </summary>
    /// <param name="configuration">Configuration to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<PublicAutoRowHeightResult> ApplyAutoRowHeightConfigurationAsync(
        PublicAutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate measurement cache
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cache was invalidated successfully</returns>
    Task<bool> InvalidateHeightCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets auto row height statistics
    /// </summary>
    /// <returns>Statistics for auto row height operations</returns>
    PublicAutoRowHeightStatistics GetAutoRowHeightStatistics();

    /// <summary>
    /// Gets measurement cache statistics
    /// </summary>
    /// <returns>Cache statistics for monitoring</returns>
    PublicCacheStatistics GetCacheStatistics();

    #endregion

    #region Keyboard Shortcuts Operations

    /// <summary>
    /// Executes a predefined shortcut by name
    /// </summary>
    /// <param name="command">Shortcut execution command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the shortcut execution</returns>
    Task<ShortcutDataResult> ExecuteShortcutAsync(ExecuteShortcutDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new keyboard shortcut
    /// </summary>
    /// <param name="shortcut">Shortcut definition to register</param>
    /// <returns>True if registration was successful</returns>
    Task<bool> RegisterShortcutAsync(PublicShortcutDefinition shortcut);

    /// <summary>
    /// Gets all registered shortcuts
    /// </summary>
    /// <returns>List of registered shortcuts</returns>
    IReadOnlyList<PublicShortcutDefinition> GetRegisteredShortcuts();

    #endregion

    #region Smart Row Management Operations

    /// <summary>
    /// Smart add rows with minimum rows management
    /// </summary>
    /// <param name="command">Smart add rows command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the smart add operation</returns>
    Task<SmartOperationDataResult> SmartAddRowsAsync(SmartAddRowsDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Smart delete rows with context-aware logic
    /// </summary>
    /// <param name="command">Smart delete rows command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the smart delete operation</returns>
    Task<SmartOperationDataResult> SmartDeleteRowsAsync(SmartDeleteRowsDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-expand empty row maintenance
    /// </summary>
    /// <param name="command">Auto-expand command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the auto-expand operation</returns>
    Task<SmartOperationDataResult> AutoExpandEmptyRowAsync(AutoExpandEmptyRowDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates row management configuration
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <returns>Validation result</returns>
    Task<PublicResult> ValidateRowManagementConfigurationAsync(PublicRowManagementConfiguration configuration);

    /// <summary>
    /// Gets current row management statistics
    /// </summary>
    /// <returns>Row management statistics</returns>
    PublicRowManagementStatistics GetRowManagementStatistics();

    #endregion

    #region Color Operations

    /// <summary>
    /// Applies color to cells/rows/columns
    /// </summary>
    /// <param name="command">Color application command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the color operation</returns>
    Task<ColorDataResult> ApplyColorAsync(ApplyColorDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies conditional formatting rules
    /// </summary>
    /// <param name="command">Conditional formatting command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the operation</returns>
    Task<ColorDataResult> ApplyConditionalFormattingAsync(ApplyConditionalFormattingDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears color from cells/rows/columns
    /// </summary>
    /// <param name="command">Clear color command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the operation</returns>
    Task<ColorDataResult> ClearColorAsync(ClearColorDataCommand command, CancellationToken cancellationToken = default);

    #endregion

    #region Performance Operations

    /// <summary>
    /// Starts performance monitoring
    /// </summary>
    /// <param name="command">Performance monitoring command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the operation</returns>
    Task<PublicResult> StartPerformanceMonitoringAsync(StartPerformanceMonitoringCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops performance monitoring
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the operation</returns>
    Task<PublicResult> StopPerformanceMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current performance snapshot
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance snapshot data</returns>
    Task<PerformanceSnapshotData> GetPerformanceSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive performance report
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance report with analysis</returns>
    Task<PerformanceReportData> GetPerformanceReportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance statistics
    /// </summary>
    /// <returns>Performance statistics</returns>
    PerformanceStatisticsData GetPerformanceStatistics();

    #endregion

    #region Row/Column/Cell Batch Operations

    /// <summary>
    /// Executes batch cell updates
    /// </summary>
    /// <param name="command">Batch cell update command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the batch operation</returns>
    Task<BatchOperationResult> BatchUpdateCellsAsync(BatchUpdateCellsDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes batch row operations
    /// </summary>
    /// <param name="command">Batch row operations command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the batch operation</returns>
    Task<BatchOperationResult> BatchRowOperationsAsync(BatchRowOperationsDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes batch column operations
    /// </summary>
    /// <param name="command">Batch column operations command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PublicResult of the batch operation</returns>
    Task<BatchOperationResult> BatchColumnOperationsAsync(BatchColumnOperationsDataCommand command, CancellationToken cancellationToken = default);

    #endregion

    #region Cell Edit Operations

    /// <summary>
    /// Begins an edit session for a specific cell
    /// </summary>
    /// <param name="command">Begin edit command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Edit PublicResult with session information</returns>
    Task<CellEditResult> BeginEditAsync(BeginEditDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a cell value with real-time validation
    /// </summary>
    /// <param name="command">Update cell command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Edit PublicResult with validation information</returns>
    Task<CellEditResult> UpdateCellAsync(UpdateCellDataCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current edit session
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Edit result</returns>
    Task<CellEditResult> CommitEditAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the current edit session
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Edit result</returns>
    Task<CellEditResult> CancelEditAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets validation alerts for a specific row
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <returns>Validation alerts string</returns>
    string GetValidationAlerts(int rowIndex);

    /// <summary>
    /// Checks if a row has validation errors
    /// </summary>
    /// <param name="rowIndex">Row index</param>
    /// <returns>True if row has validation errors</returns>
    bool HasValidationErrors(int rowIndex);

    #endregion

    #region Business Presets

    /// <summary>
    /// Creates employee hierarchy sort preset (Department → Position → Salary)
    /// </summary>
    /// <param name="departmentColumn">Department column name</param>
    /// <param name="positionColumn">Position column name</param>
    /// <param name="salaryColumn">Salary column name</param>
    /// <returns>Sort configuration for employee hierarchy</returns>
    PublicSortConfiguration CreateEmployeeHierarchySortPreset(
        string departmentColumn = "Department",
        string positionColumn = "Position",
        string salaryColumn = "Salary");

    /// <summary>
    /// Creates customer priority sort preset (Tier → Value → JoinDate)
    /// </summary>
    /// <param name="tierColumn">Customer tier column name</param>
    /// <param name="valueColumn">Total value column name</param>
    /// <param name="joinDateColumn">Join date column name</param>
    /// <returns>Sort configuration for customer priority</returns>
    PublicSortConfiguration CreateCustomerPrioritySortPreset(
        string tierColumn = "CustomerTier",
        string valueColumn = "TotalValue",
        string joinDateColumn = "JoinDate");

    /// <summary>
    /// Gets responsive row height preset (min: 24px, max: 150px, font: 13px)
    /// </summary>
    /// <returns>Auto row height configuration for responsive design</returns>
    PublicAutoRowHeightConfiguration GetResponsiveHeightPreset();

    /// <summary>
    /// Gets compact row height preset (min: 18px, max: 100px, font: 11px)
    /// </summary>
    /// <returns>Auto row height configuration for compact display</returns>
    PublicAutoRowHeightConfiguration GetCompactHeightPreset();

    /// <summary>
    /// Gets performance row height preset (no wrapping, large cache, timeout)
    /// </summary>
    /// <returns>Auto row height configuration for performance optimization</returns>
    PublicAutoRowHeightConfiguration GetPerformanceHeightPreset();

    #endregion

    #region Theme and Color Management

    /// <summary>
    /// Applies a theme to the grid
    /// </summary>
    /// <param name="theme">Theme configuration to apply</param>
    /// <returns>Result of theme application</returns>
    Task<PublicResult> ApplyThemeAsync(PublicGridTheme theme);

    /// <summary>
    /// Gets the current active theme
    /// </summary>
    /// <returns>Current grid theme</returns>
    PublicGridTheme GetCurrentTheme();

    /// <summary>
    /// Resets theme to default
    /// </summary>
    /// <returns>Result of theme reset</returns>
    Task<PublicResult> ResetThemeToDefaultAsync();

    /// <summary>
    /// Updates specific cell colors without changing entire theme
    /// </summary>
    /// <param name="cellColors">Cell colors to update</param>
    /// <returns>Result of color update</returns>
    Task<PublicResult> UpdateCellColorsAsync(PublicCellColors cellColors);

    /// <summary>
    /// Updates specific row colors without changing entire theme
    /// </summary>
    /// <param name="rowColors">Row colors to update</param>
    /// <returns>Result of color update</returns>
    Task<PublicResult> UpdateRowColorsAsync(PublicRowColors rowColors);

    /// <summary>
    /// Updates specific validation colors without changing entire theme
    /// </summary>
    /// <param name="validationColors">Validation colors to update</param>
    /// <returns>Result of color update</returns>
    Task<PublicResult> UpdateValidationColorsAsync(PublicValidationColors validationColors);

    /// <summary>
    /// Creates a dark theme preset
    /// </summary>
    /// <returns>Dark theme configuration</returns>
    PublicGridTheme CreateDarkTheme();

    /// <summary>
    /// Creates a light theme preset
    /// </summary>
    /// <returns>Light theme configuration</returns>
    PublicGridTheme CreateLightTheme();

    /// <summary>
    /// Creates a high contrast theme preset
    /// </summary>
    /// <returns>High contrast theme configuration</returns>
    PublicGridTheme CreateHighContrastTheme();

    #endregion
}

/// <summary>
/// Interface for validation rules
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Gets the unique identifier for this validation rule
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Gets the name of the validation rule
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Gets the columns this rule depends on
    /// </summary>
    IReadOnlyList<string> DependentColumns { get; }

    /// <summary>
    /// Gets whether this rule is enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the validation timeout
    /// </summary>
    TimeSpan ValidationTimeout { get; }

    /// <summary>
    /// Validates a row (synchronous version for compatibility)
    /// </summary>
    /// <param name="row">Row data to validate</param>
    /// <param name="context">Validation context</param>
    /// <returns>Validation result</returns>
    ValidationResult Validate(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context);

    /// <summary>
    /// Validates a row (asynchronous version)
    /// </summary>
    /// <param name="row">Row data to validate</param>
    /// <param name="context">Validation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateAsync(
        IReadOnlyDictionary<string, object?> row,
        ValidationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Validation context for rules
/// </summary>
public class ValidationContext
{
    /// <summary>
    /// Gets or sets the row index being validated
    /// </summary>
    public int RowIndex { get; set; }

    /// <summary>
    /// Gets or sets the column name being validated
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets all row data for cross-row validation
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>>? AllRows { get; set; }

    /// <summary>
    /// Gets or sets custom validation properties
    /// </summary>
    public Dictionary<string, object?> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the operation ID for tracking
    /// </summary>
    public string? OperationId { get; set; }
}

/// <summary>
/// PublicResult of validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets whether validation passed
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation error message
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the validation severity
    /// </summary>
    public PublicValidationSeverity Severity { get; init; } = PublicValidationSeverity.Error;

    /// <summary>
    /// Gets the affected column
    /// </summary>
    public string? AffectedColumn { get; init; }

    /// <summary>
    /// Creates a successful validation PublicResult
    /// </summary>
    /// <returns>Successful validation result</returns>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation PublicResult
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="severity">Validation severity</param>
    /// <param name="affectedColumn">Affected column name</param>
    /// <returns>Failed validation result</returns>
    public static ValidationResult Error(string errorMessage, PublicValidationSeverity severity = PublicValidationSeverity.Error, string? affectedColumn = null) =>
        new() { IsValid = false, ErrorMessage = errorMessage, Severity = severity, AffectedColumn = affectedColumn };
}
Export je iba do datatable a dictionary. Nic ine (ziaden csv, excel, json,.....)!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!



# KOMPLETNÉ ZADANIE: EXPORT SYSTÉM PRE UPRAVENÝ KOMPONENT

---
# GENERATE MAP for Export documentation (only metadata; content unchanged)
# Use these target paths when generating files from this documentation:
# - Core/ValueObjects/ExportTypes.cs
# - Application/Interfaces/IExportService.cs
# - Application/Services/ExportService.cs
# - Infrastructure/Logging/Interfaces/IExportLogger.cs
# - Infrastructure/Logging/ExportLogger.cs
# - Infrastructure/Services/InternalServiceRegistration.cs  # add Export registrations here
# - /AdvancedWinUiDataGridFacade          # facade output (single file target; no extension)
# - /AdvancedWinUiDataGridFacade/IAdvancedWinUiDataGridFacade  # facade interface
---

## **Cieľ Implementácie:**
Vytvoriť robustnú, optimalizovanú export funkcionalitu pre AdvancedWinUiDataGrid komponent s enterprise-grade architektúrou. Export systém bude integrovať všetky technológie a paradigmy používané v komponente: Clean Architecture, Command Pattern, Hybrid Internal DI, LINQ optimalizácie, a comprehensive logging.

## **Architectural Principles & Export Strategy:**
- **Clean Architecture + Command Pattern**: Export operations implementované ako commands s dedicated handlers
- **Hybrid Internal DI + Functional/OOP**: Export services v internal DI kontajneri s funkcionálnym programovaním
- **SOLID Principles**: Separation of concerns medzi export formats, filtering, a data processing
- **Enterprise Observability**: Comprehensive logging pre všetky export operations
- **Performance Optimized**: LINQ optimalizácie s parallel processing pre veľké datasets
- **Thread Safe**: Concurrent export operations bez data corruption

## **Internal DI Integration & Service Distribution:**
Export services registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a distribuované cez hybrid internal DI systém. ExportService dostáva specialized export logging dependencies cez constructor injection z internal DI kontajnera.

## **Backup Strategy & Implementation Approach:**
### **1. Backup Strategy**
- Vytvoriť .oldbackup_timestamp súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### **2. Implementation Replacement**
- Kompletný refaktoring s rozšíreným export systémom
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## **Export System Architecture:**

### **1. Export Types Definition (ExportTypes.cs)**
```csharp
// Core/ValueObjects/ExportTypes.cs

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// Defines available export formats for data export operations
/// </summary>
public enum ExportFormat
{
    /// <summary>Dictionary format for in-memory data structures</summary>
    Dictionary,

    /// <summary>DataTable format for .NET data operations</summary>
    DataTable
}

/// <summary>
/// Export progress information for tracking export operations
/// </summary>
/// <param name="ProcessedRows">Number of processed rows</param>
/// <param name="TotalRows">Total number of rows to export</param>
/// <param name="ElapsedTime">Time elapsed since export start</param>
/// <param name="CurrentOperation">Description of current export operation</param>
/// <param name="CurrentFormat">Current export format being processed</param>
public record ExportProgress(
    int ProcessedRows,
    int TotalRows,
    TimeSpan ElapsedTime,
    string CurrentOperation = "",
    ExportFormat CurrentFormat = ExportFormat.Dictionary
)
{
    /// <summary>Calculated completion percentage (0-100)</summary>
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;

    /// <summary>Estimated time remaining based on current progress</summary>
    public TimeSpan? EstimatedTimeRemaining => ProcessedRows > 0 && TotalRows > ProcessedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ProcessedRows) / ProcessedRows)
        : null;

    public ExportProgress() : this(0, 0, TimeSpan.Zero, "", ExportFormat.Dictionary) { }
}

/// <summary>
/// Command for exporting data with comprehensive filtering and formatting options
/// </summary>
/// <param name="IncludeValidAlerts">Include validation alerts in export</param>
/// <param name="ExportOnlyChecked">Export only checked/selected rows</param>
/// <param name="ExportOnlyFiltered">Export only currently filtered rows</param>
/// <param name="RemoveAfter">Remove exported rows from source after export</param>
/// <param name="IncludeHeaders">Include column headers in export</param>
/// <param name="Timeout">Timeout for export operation</param>
/// <param name="ExportProgress">Progress reporting callback</param>
/// <param name="Format">Target export format</param>
/// <param name="CorrelationId">Operation correlation ID for logging</param>
public record ExportDataCommand(
    bool IncludeValidAlerts = false,
    bool ExportOnlyChecked = false,
    bool ExportOnlyFiltered = false,
    bool RemoveAfter = false,
    bool IncludeHeaders = true,
    TimeSpan? Timeout = null,
    IProgress<ExportProgress>? ExportProgress = null,
    ExportFormat Format = ExportFormat.Dictionary,
    string? CorrelationId = null
)
{
    /// <summary>Computed property for validation alerts inclusion</summary>
    public bool IncludeValidationAlerts => IncludeValidAlerts;

    /// <summary>Indicates whether any filtering is applied</summary>
    public bool HasFiltering => ExportOnlyChecked || ExportOnlyFiltered;

    /// <summary>Factory method for creating dictionary export command</summary>
    public static ExportDataCommand ToDictionary(
        bool includeValidAlerts = false,
        bool exportOnlyChecked = false,
        bool exportOnlyFiltered = false,
        bool removeAfter = false,
        bool includeHeaders = true,
        TimeSpan? timeout = null,
        IProgress<ExportProgress>? exportProgress = null,
        string? correlationId = null) =>
        new(
            IncludeValidAlerts: includeValidAlerts,
            ExportOnlyChecked: exportOnlyChecked,
            ExportOnlyFiltered: exportOnlyFiltered,
            RemoveAfter: removeAfter,
            IncludeHeaders: includeHeaders,
            Timeout: timeout,
            ExportProgress: exportProgress,
            Format: ExportFormat.Dictionary,
            CorrelationId: correlationId
        );

    /// <summary>Factory method for creating DataTable export command</summary>
    public static ExportDataCommand ToDataTable(
        bool includeValidAlerts = false,
        bool exportOnlyChecked = false,
        bool exportOnlyFiltered = false,
        bool removeAfter = false,
        bool includeHeaders = true,
        TimeSpan? timeout = null,
        IProgress<ExportProgress>? exportProgress = null,
        string? correlationId = null) =>
        new(
            IncludeValidAlerts: includeValidAlerts,
            ExportOnlyChecked: exportOnlyChecked,
            ExportOnlyFiltered: exportOnlyFiltered,
            RemoveAfter: removeAfter,
            IncludeHeaders: includeHeaders,
            Timeout: timeout,
            ExportProgress: exportProgress,
            Format: ExportFormat.DataTable,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Specialized command for DataTable export operations
/// </summary>
/// <param name="IncludeValidAlerts">Include validation alerts in export</param>
/// <param name="ExportOnlyChecked">Export only checked rows</param>
/// <param name="ExportOnlyFiltered">Export only filtered rows</param>
/// <param name="RemoveAfter">Remove rows after export</param>
/// <param name="IncludeHeaders">Include headers in export</param>
/// <param name="Timeout">Operation timeout</param>
/// <param name="Progress">Progress reporting callback</param>
/// <param name="CorrelationId">Operation correlation ID</param>
public record ExportToDataTableCommand(
    bool IncludeValidAlerts = false,
    bool ExportOnlyChecked = false,
    bool ExportOnlyFiltered = false,
    bool RemoveAfter = false,
    bool IncludeHeaders = true,
    TimeSpan? Timeout = null,
    IProgress<ExportProgress>? Progress = null,
    string? CorrelationId = null
) : ExportDataCommand(
    IncludeValidAlerts: IncludeValidAlerts,
    ExportOnlyChecked: ExportOnlyChecked,
    ExportOnlyFiltered: ExportOnlyFiltered,
    RemoveAfter: RemoveAfter,
    IncludeHeaders: IncludeHeaders,
    Timeout: Timeout,
    ExportProgress: Progress,
    Format: ExportFormat.DataTable,
    CorrelationId: CorrelationId
)
{
    /// <summary>Alias for validation alerts inclusion</summary>
    public new bool IncludeValidationAlerts => IncludeValidAlerts;

    /// <summary>Factory method for creating DataTable export command</summary>
    public static new ExportToDataTableCommand ToDataTable(
        bool includeValidAlerts = false,
        bool exportOnlyChecked = false,
        bool exportOnlyFiltered = false,
        bool removeAfter = false,
        bool includeHeaders = true,
        TimeSpan? timeout = null,
        IProgress<ExportProgress>? exportProgress = null,
        string? correlationId = null) =>
        new(
            IncludeValidAlerts: includeValidAlerts,
            ExportOnlyChecked: exportOnlyChecked,
            ExportOnlyFiltered: exportOnlyFiltered,
            RemoveAfter: removeAfter,
            IncludeHeaders: includeHeaders,
            Timeout: timeout,
            Progress: exportProgress,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Result of export operation with comprehensive metrics
/// </summary>
/// <param name="Success">Whether export completed successfully</param>
/// <param name="ExportedRows">Number of successfully exported rows</param>
/// <param name="TotalRows">Total number of rows processed</param>
/// <param name="ExportTime">Time taken for export operation</param>
/// <param name="Format">Export format that was used</param>
/// <param name="DataSize">Size of exported data in bytes/elements</param>
/// <param name="ErrorMessage">Error message if export failed</param>
/// <param name="CorrelationId">Operation correlation ID</param>
public record ExportResult(
    bool Success,
    int ExportedRows,
    int TotalRows,
    TimeSpan ExportTime,
    ExportFormat Format,
    long? DataSize = null,
    string? ErrorMessage = null,
    string? CorrelationId = null
)
{
    public ExportResult() : this(false, 0, 0, TimeSpan.Zero, ExportFormat.Dictionary) { }

    /// <summary>Creates successful export result</summary>
    public static ExportResult CreateSuccess(
        int exportedRows,
        int totalRows,
        TimeSpan exportTime,
        ExportFormat format,
        long? dataSize = null,
        string? correlationId = null) =>
        new(
            Success: true,
            ExportedRows: exportedRows,
            TotalRows: totalRows,
            ExportTime: exportTime,
            Format: format,
            DataSize: dataSize,
            CorrelationId: correlationId
        );

    /// <summary>Creates failed export result</summary>
    public static ExportResult Failure(
        string errorMessage,
        TimeSpan exportTime,
        ExportFormat format,
        string? correlationId = null) =>
        new(
            Success: false,
            ExportedRows: 0,
            TotalRows: 0,
            ExportTime: exportTime,
            Format: format,
            ErrorMessage: errorMessage,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Export validation result for pre-export checks
/// </summary>
/// <param name="IsValid">Whether export configuration is valid</param>
/// <param name="ValidationErrors">List of validation errors</param>
/// <param name="Warnings">List of validation warnings</param>
/// <param name="EstimatedSize">Estimated export data size</param>
/// <param name="EstimatedDuration">Estimated export duration</param>
public record ExportValidationResult(
    bool IsValid,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<string> Warnings,
    long? EstimatedSize = null,
    TimeSpan? EstimatedDuration = null
);
```

### **2. Export Service Interface**
```csharp
// Application/Interfaces/IExportService.cs

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// Service interface for data export operations with comprehensive export functionality
/// </summary>
internal interface IExportService
{
    /// <summary>
    /// Exports data to DataTable using command pattern with LINQ filtering
    /// </summary>
    /// <param name="data">Source data to export</param>
    /// <param name="command">Export command with configuration</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>DataTable with exported data</returns>
    Task<DataTable> ExportToDataTableAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ExportDataCommand command,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Exports data to Dictionary collection using command pattern with LINQ filtering
    /// </summary>
    /// <param name="data">Source data to export</param>
    /// <param name="command">Export command with configuration</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>Dictionary collection with exported data</returns>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExportToDictionaryAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ExportDataCommand command,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Exports data using specialized DataTable command
    /// </summary>
    /// <param name="data">Source data to export</param>
    /// <param name="command">DataTable export command</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>DataTable with exported data</returns>
    Task<DataTable> ExportWithDataTableCommandAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ExportToDataTableCommand command,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Validates export configuration before execution
    /// </summary>
    /// <param name="data">Data to validate for export</param>
    /// <param name="command">Export command to validate</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>Validation result with errors and warnings</returns>
    Task<ExportValidationResult> ValidateExportConfigurationAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ExportDataCommand command,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supported export formats for current data configuration
    /// </summary>
    /// <param name="dataType">Type of data to export</param>
    /// <returns>List of supported export formats</returns>
    IReadOnlyList<ExportFormat> GetSupportedFormats(Type dataType);

    /// <summary>
    /// Estimates export size and duration
    /// </summary>
    /// <param name="data">Data to analyze for export</param>
    /// <param name="format">Target export format</param>
    /// <returns>Estimation result with size and duration</returns>
    Task<(long EstimatedSize, TimeSpan EstimatedDuration)> EstimateExportRequirementsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ExportFormat format);
}
```

### **3. Facade API Integration**
```csharp
// Public API methods pre Facade Pattern (AdvancedWinUiDataGrid.cs)

/// <summary>
/// Exports current grid data to DataTable using command pattern with comprehensive filtering
/// </summary>
/// <param name="includeValidationAlerts">Include validation alerts in export</param>
/// <param name="exportOnlyChecked">Export only checked/selected rows</param>
/// <param name="exportOnlyFiltered">Export only currently filtered rows</param>
/// <param name="includeHeaders">Include column headers in export</param>
/// <param name="removeAfter">Remove exported rows from grid after export</param>
/// <param name="progress">Progress reporting callback</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>DataTable containing exported data</returns>
public async Task<DataTable> ExportToDataTableAsync(
    bool includeValidationAlerts = false,
    bool exportOnlyChecked = false,
    bool exportOnlyFiltered = false,
    bool includeHeaders = true,
    bool removeAfter = false,
    IProgress<ExportProgress>? progress = null,
    cancellationToken cancellationToken = default)
{
    var correlationId = Guid.NewGuid().ToString();
    _logger.LogInformation("Starting DataTable export: includeAlerts={IncludeAlerts}, onlyChecked={OnlyChecked}, onlyFiltered={OnlyFiltered} [CorrelationId: {CorrelationId}]",
        includeValidationAlerts, exportOnlyChecked, exportOnlyFiltered, correlationId);

    var command = ExportDataCommand.ToDataTable(
        includeValidAlerts: includeValidationAlerts,
        exportOnlyChecked: exportOnlyChecked,
        exportOnlyFiltered: exportOnlyFiltered,
        removeAfter: removeAfter,
        includeHeaders: includeHeaders,
        exportProgress: progress,
        correlationId: correlationId
    );

    var currentData = GetCurrentData(); // Facade method to get current grid data
    return await _exportService.ExportToDataTableAsync(currentData, command, cancellationToken);
}

/// <summary>
/// Exports current grid data to Dictionary collection using command pattern
/// </summary>
/// <param name="includeValidationAlerts">Include validation alerts in export</param>
/// <param name="exportOnlyChecked">Export only checked rows</param>
/// <param name="exportOnlyFiltered">Export only filtered rows</param>
/// <param name="includeHeaders">Include headers in export</param>
/// <param name="removeAfter">Remove rows after export</param>
/// <param name="progress">Progress reporting callback</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Dictionary collection with exported data</returns>
public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExportToDictionaryAsync(
    bool includeValidationAlerts = false,
    bool exportOnlyChecked = false,
    bool exportOnlyFiltered = false,
    bool includeHeaders = true,
    bool removeAfter = false,
    IProgress<ExportProgress>? progress = null,
    cancellationToken cancellationToken = default)
{
    var correlationId = Guid.NewGuid().ToString();
    _logger.LogInformation("Starting Dictionary export [CorrelationId: {CorrelationId}]", correlationId);

    var command = ExportDataCommand.ToDictionary(
        includeValidAlerts: includeValidationAlerts,
        exportOnlyChecked: exportOnlyChecked,
        exportOnlyFiltered: exportOnlyFiltered,
        removeAfter: removeAfter,
        includeHeaders: includeHeaders,
        exportProgress: progress,
        correlationId: correlationId
    );

    var currentData = GetCurrentData();
    return await _exportService.ExportToDictionaryAsync(currentData, command, cancellationToken);
}

/// <summary>
/// Exports data using specialized DataTable command for advanced scenarios
/// </summary>
/// <param name="command">DataTable export command with full configuration</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>DataTable with exported data and comprehensive export result</returns>
public async Task<DataTable> ExportWithDataTableCommandAsync(
    ExportToDataTableCommand command,
    cancellationToken cancellationToken = default)
{
    _logger.LogInformation("Starting specialized DataTable export [CorrelationId: {CorrelationId}]", command.CorrelationId);

    var currentData = GetCurrentData();
    return await _exportService.ExportWithDataTableCommandAsync(currentData, command, cancellationToken);
}

/// <summary>
/// Validates export configuration before executing export operation
/// </summary>
/// <param name="format">Target export format</param>
/// <param name="exportOnlyChecked">Whether to export only checked rows</param>
/// <param name="exportOnlyFiltered">Whether to export only filtered rows</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Validation result with errors, warnings, and estimates</returns>
public async Task<ExportValidationResult> ValidateExportConfigurationAsync(
    ExportFormat format,
    bool exportOnlyChecked = false,
    bool exportOnlyFiltered = false,
    cancellationToken cancellationToken = default)
{
    var command = new ExportDataCommand(
        ExportOnlyChecked: exportOnlyChecked,
        ExportOnlyFiltered: exportOnlyFiltered,
        Format: format
    );

    var currentData = GetCurrentData();
    return await _exportService.ValidateExportConfigurationAsync(currentData, command, cancellationToken);
}
```

## **Enhanced Export Service Implementation Pattern:**

### **LINQ Optimizations & Performance Features:**
```csharp
// Application/Services/ExportService.cs - ENHANCED s LINQ optimalizáciami

internal sealed class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly IExportLogger<ExportService> _exportLogger;
    private readonly ICommandLogger<ExportService> _commandLogger;

    public async Task<DataTable> ExportToDataTableAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ExportDataCommand command,
        cancellationToken cancellationToken = default)
    {
        using var scope = _exportLogger.LogCommandOperationStart(command,
            new { format = command.Format, hasFiltering = command.HasFiltering });

        _logger.LogInformation("Starting DataTable export: format={Format}, filtering={HasFiltering} [CorrelationId: {CorrelationId}]",
            command.Format, command.HasFiltering, command.CorrelationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // LINQ Optimization: Count optimization s short-circuit evaluation
            var totalRows = await Task.Run(() =>
                data.TryGetNonEnumeratedCount(out var count) ? count : data.Count(),
                cancellationToken);

            _logger.LogInformation("Export data analysis: {TotalRows} rows available for export", totalRows);

            // LINQ Optimization: Progressive filtering s lazy evaluation
            var filteredData = await ApplyExportFiltersAsync(data, command, cancellationToken);

            // LINQ Optimization: Parallel processing pre veľké datasets
            var useParallel = totalRows > 1000;
            var exportedData = useParallel
                ? await ProcessDataParallelAsync(filteredData, command, cancellationToken)
                : await ProcessDataSequentialAsync(filteredData, command, cancellationToken);

            _exportLogger.LogLINQOptimization("ExportDataProcessing", useParallel, false, stopwatch.Elapsed);

            // DataTable conversion s schema optimization
            var dataTable = await ConvertToDataTableAsync(exportedData, command, cancellationToken);

            var exportedRows = dataTable.Rows.Count;
            _exportLogger.LogExportOperation(command.Format.ToString(), totalRows, exportedRows, stopwatch.Elapsed);

            _logger.LogInformation("DataTable export completed: {ExportedRows}/{TotalRows} rows in {Duration}ms",
                exportedRows, totalRows, stopwatch.ElapsedMilliseconds);

            scope.MarkSuccess(new { exportedRows, format = command.Format });
            return dataTable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DataTable export failed [CorrelationId: {CorrelationId}]", command.CorrelationId);
            scope.MarkError(ex.Message);
            throw;
        }
    }

    private async Task<IEnumerable<IReadOnlyDictionary<string, object?>>> ApplyExportFiltersAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ExportDataCommand command,
        cancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var query = data.AsQueryable();

            // LINQ Optimization: Conditional filtering s short-circuit evaluation
            if (command.ExportOnlyChecked)
            {
                query = query.Where(row => IsRowChecked(row));
                _exportLogger.LogDataConversion("AllData", "CheckedOnly", query.Count(), TimeSpan.Zero);
            }

            if (command.ExportOnlyFiltered)
            {
                query = query.Where(row => IsRowVisible(row));
                _exportLogger.LogDataConversion("CheckedData", "FilteredData", query.Count(), TimeSpan.Zero);
            }

            return query.AsEnumerable();
        }, cancellationToken);
    }

    private async Task<IEnumerable<IReadOnlyDictionary<string, object?>>> ProcessDataParallelAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> data,
        ExportDataCommand command,
        cancellationToken cancellationToken)
    {
        // LINQ Optimization: Parallel processing s partition-based approach
        return await Task.Run(() =>
        {
            var partitioner = Partitioner.Create(data, EnumerablePartitionerOptions.NoBuffering);

            return partitioner.AsParallel()
                .WithCancellation(cancellationToken)
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select((row, index) =>
                {
                    // Progress reporting every 100 rows
                    if (index % 100 == 0 && command.ExportProgress != null)
                    {
                        command.ExportProgress.Report(new ExportProgress(
                            ProcessedRows: index,
                            TotalRows: data.Count(),
                            ElapsedTime: TimeSpan.FromMilliseconds(index * 5), // Estimated
                            CurrentOperation: $"Processing row {index}",
                            CurrentFormat: command.Format
                        ));
                    }

                    return ProcessExportRow(row, command);
                })
                .Where(row => row != null)
                .ToList();
        }, cancellationToken);
    }
}
```

## **Enhanced Export Logging Integration:**

### **IExportLogger Interface:**
```csharp
// Infrastructure/Logging/IExportLogger.cs

internal interface IExportLogger<T> : IOperationLogger<T>
{
    /// <summary>
    /// Logs export operation with comprehensive metrics
    /// </summary>
    void LogExportOperation(string exportFormat, int totalRows, int exportedRows, TimeSpan duration);

    /// <summary>
    /// Logs data conversion during export process
    /// </summary>
    void LogDataConversion(string fromFormat, string toFormat, int rowCount, TimeSpan duration);

    /// <summary>
    /// Logs export filtering operations
    /// </summary>
    void LogExportFiltering(string filterType, int originalCount, int filteredCount, TimeSpan duration);

    /// <summary>
    /// Logs progress reporting during export operations
    /// </summary>
    void LogProgressReporting(string operationType, double progressPercentage, int processedItems, int totalItems);

    /// <summary>
    /// Logs export format-specific operations (DataTable schema, Dictionary serialization, etc.)
    /// </summary>
    void LogFormatOperation(string format, string operation, bool success, TimeSpan duration);

    /// <summary>
    /// Logs data removal operations when RemoveAfter is enabled
    /// </summary>
    void LogDataRemoval(int removedRows, string removalCriteria, TimeSpan duration);
}
```

## **Internal DI Registration:**
```csharp
// Infrastructure/Services/InternalServiceRegistration.cs - Export Module Addition

public static IServiceCollection AddAdvancedWinUiDataGridInternal(this IServiceCollection services)
{
    // ... existing registrations ...

    // Export module services
    services.AddScoped<IExportService, ExportService>();
    services.AddSingleton(typeof(IExportLogger<>), typeof(ExportLogger<>));

    // Export data processors
    services.AddSingleton<IDataTableExportProcessor, DataTableExportProcessor>();
    services.AddSingleton<IDictionaryExportProcessor, DictionaryExportProcessor>();
    services.AddSingleton<IExportFilterProcessor, ExportFilterProcessor>();

    return services;
}
```

## **Performance & Optimization Features:**

### **1. LINQ Query Optimizations**
- **Parallel Processing**: Automatic parallel processing pre datasets > 1000 rows
- **Lazy Evaluation**: Deferred execution pre filtering chains
- **Short-Circuit Evaluation**: Early termination pre empty result sets
- **Memory Efficiency**: Streaming processing pre veľké exports

### **2. Export Format Optimizations**
- **DataTable**: Schema pre-calculation a bulk row insertion
- **Dictionary**: Object pooling pre reduced GC pressure
- **Filtering**: Conditional filter application s performance monitoring

### **3. Progress Reporting & Monitoring**
- **Real-time Progress**: Granular progress reporting každých 100 rows
- **Performance Metrics**: Processing speed a throughput monitoring
- **Memory Usage**: Memory consumption tracking pre large exports

## **Command Pattern Integration:**
Všetky export operations implementované ako commands:
- **ExportDataCommand**: Main export command s flexible filtering
- **ExportToDataTableCommand**: Specialized DataTable export
- **ValidateExportCommand**: Pre-export validation
- **EstimateExportCommand**: Size/duration estimation

Každý command má svoj dedicated handler s comprehensive logging a error handling.

## **Integration s Existing Modules:**
Export systém je navrhnutý pre bezproblémovú integráciu s:
- **Import System**: Paste operations využívajú import validation pipeline
- **Search System**: Import search results s highlighting preservation
- **Validation System**: Automatic validation pre paste operations
- **Filter System**: Copy filtered data s preserve formatting

## **Future Extensions:**
- **Streaming Export**: Large dataset export s memory optimization

### **Logging Levels Usage:**
- **Information**: Successful imports/exports, progress milestones, data statistics
- **Warning**: Partial import failures, format conversion warnings, large dataset warnings
- **Error**: Import/export failures, clipboard access errors, validation failures
- **Critical**: Data corruption detection, system resource exhaustion


### ExportDataCommand (public API)

The export entry accepts an `ExportDataCommand` record. Example shape:

```csharp

public record ExportDataCommand(
    bool IncludeValidAlerts = false,
    bool ExportOnlyChecked = false,
    bool ExportOnlyFiltered = false,
    bool RemoveAfter = false,
    bool IncludeHeaders = true,
    TimeSpan? Timeout = null,
    IProgress<ExportProgress>? ExportProgress = null,
    ExportFormat Format = ExportFormat.Dictionary,
    List<string>? ColumnNames = null, // null = all non-special columns
    string? CorrelationId = null
);

```

Semantics and behaviour:
- `IncludeValidAlerts`: when true, the export includes the special `validAlerts` column content (a textual aggregation of validation messages). Default `false`.
- `ExportOnlyChecked`: when true, export only rows with the `checkboxColumn` set to true. If the checkbox column is not enabled then this flag is ignored and the export defaults to the current selection/filtering rules.
- `ExportOnlyFiltered`: when true, export only the rows that are currently **filtered** by the component's filter API (see `FilterDocumentation.md`). If no filter is active, this behaves as `ExportOnlyFiltered = false` (i.e., export whole dataset).
- Column selection: the export API supports an optional list of column names (excluding special columns). If `null` the export will include **all non-special columns** by default. If provided, only the specified non-special columns are exported (order preserved).
- Validation before export: the export flow MUST call `AreAllNonEmptyRowsValidAsync(onlyFiltered)` **before** proceeding. If validation fails, the export must abort and return an error result.

Example usage:
```csharp
var cmd = new ExportDataCommand(
    IncludeValidAlerts: true,
    ExportOnlyChecked: false,
    ExportOnlyFiltered: true,
    IncludeHeaders: true,
    Format: ExportFormat.Dictionary
);

var result = await _exportService.ExportAsync(cmd, cancellationToken);
```

Notes on filtering:
- `ExportOnlyFiltered` does **not** transmit filter rules into export. It only instructs the export to use the currently applied filter results (the filtered row set produced by `FilterDocumentation.md`). If no filter is applied, `ExportOnlyFiltered` will export the entire dataset.



## Automatic validation before export


**Paste** operations use the same import flow: after paste completes, a **bulk validation** is executed. Bulk validation can validate in parts (to reduce memory spikes) but **UI must only be updated at the end** of the bulk validation pass unless running in *headless mode*.
- In headless mode the component does **not** automatically update UI; instead the public API exposes a method to request UI refresh for validation results — callers may invoke it when appropriate.
- `ValidationProgress` is used to report progress of the validation phase (bulk validation). It should be reported with coarse-grained updates (e.g., per batch) to avoid UI thrashing.



**Headless export behavior:** Even when the UI has not been refreshed (e.g., in headless scenarios where the consumer didn't call `RefreshValidationResultsToUI()`), the export will still include correct `validAlerts` text when `IncludeValidAlerts = true`. This is because export triggers `AreAllNonEmptyRowsValidAsync(...)` prior to serializing rows for export, and the exporter will serialize the validation messages produced by that call into the `validAlerts` output.


The export flow **automatically** invokes validation using the same canonical entry point `AreAllNonEmptyRowsValidAsync(onlyFiltered)` **before** performing the export if validation rules are configured. There is **no** separate `` flag on the public export API — validation is implicit when rules exist.

Behavior and usage:
- Export calls `AreAllNonEmptyRowsValidAsync(onlyFiltered)`, where `onlyFiltered` follows the `ExportOnlyFiltered` semantics documented in `ExportDocumentation.md` (export/validate either the whole dataset or only the filtered subset).
- `AreAllNonEmptyRowsValidAsync` semantics reminder:
  - If **no validation rules** are configured, the method returns success(true) immediately.
  - If validation rules exist and cached validation state covers all non-empty rows for the requested scope (whole dataset or filtered subset), the cached boolean result is returned.
  - If cached state is incomplete for the requested scope, the method performs validation over the requested scope (in batches if needed) and then returns the final boolean result.
- If validation fails (result false), the export must abort and return an appropriate error indicating validation failures; exporter should include validation details if available (for user reporting).
- The validation operation should honor `CancellationToken` and report progress via `ExportProgress` where applicable.

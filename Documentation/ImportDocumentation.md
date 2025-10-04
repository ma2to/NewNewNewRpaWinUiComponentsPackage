Import je iba z datatable a dictionary. Nic ine (ziaden csv, excel, json,.....)!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

# KOMPLETNÉ ZADANIE: IMPORT SYSTÉM PRE UPRAVENÝ KOMPONENT

---
# GENERATE MAP for Import documentation (only metadata; content unchanged)
# Use these target paths when generating files from this documentation:
# - Core/ValueObjects/ImportTypes.cs
# - Application/Interfaces/IImportService.cs
# - Application/Services/ImportService.cs
# - Infrastructure/Logging/Interfaces/IImportLogger.cs
# - Infrastructure/Logging/ImportLogger.cs
# - Infrastructure/Services/InternalServiceRegistration.cs  # add Import registrations here
# - /AdvancedWinUiDataGridFacade          # facade output (single file target; no extension)
# - /AdvancedWinUiDataGridFacade/IAdvancedWinUiDataGridFacade  # facade interface
---

## **Cieľ Implementácie:**
Vytvoriť robustnú, optimalizovanú import funkcionalitu pre AdvancedWinUiDataGrid komponent s enterprise-grade architektúrou. Import systém bude integrovať všetky technológie a paradigmy používané v komponente: Clean Architecture, Command Pattern, Hybrid Internal DI, LINQ optimalizácie, a comprehensive logging.

## **Architectural Principles & Import Strategy:**
- **Clean Architecture + Command Pattern**: Import operations implementované ako commands s dedicated handlers
- **Hybrid Internal DI + Functional/OOP**: Import services v internal DI kontajneri s funkcionálnym programovaním
- **SOLID Principles**: Separation of concerns medzi import modes, validation, a data processing
- **Enterprise Observability**: Comprehensive logging pre všetky import operations
- **Performance Optimized**: LINQ optimalizácie s parallel processing pre veľké datasets
- **Thread Safe**: Concurrent import operations bez data corruption

## **Internal DI Integration & Service Distribution:**
Import services registrované v **`Infrastructure/Services/InternalServiceRegistration.cs`** a distribuované cez hybrid internal DI systém. ImportService dostáva specialized import logging dependencies cez constructor injection z internal DI kontajnera.

## **Backup Strategy & Implementation Approach:**
### **1. Backup Strategy**
- Vytvoriť .oldbackup_timestamp súbory pre všetky modifikované súbory
- Úplne nahradiť staré implementácie - **ŽIADNA backward compatibility**
- Zachovať DI registrácie a interface contracts

### **2. Implementation Replacement**
- Kompletný refaktoring s rozšíreným import systémom
- Bez backward compatibility ale s preservation DI architektúry
- Optimalizované, bezpečné a stabilné riešenie

## **Import System Architecture:**

### **1. Import Types Definition (ImportTypes.cs)**
```csharp
// Core/ValueObjects/ImportTypes.cs

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

/// <summary>
/// Defines import modes for data import operations
/// </summary>
public enum ImportMode
{
    /// <summary>Replace all existing data with imported data</summary>
    Replace,

    /// <summary>Append imported data to existing data</summary>
    Append,

    /// <summary>Insert imported data at specific position</summary>
    Insert,

    /// <summary>Merge imported data with existing data based on key matching</summary>
    Merge
}

/// <summary>
/// Import progress information for tracking import operations
/// </summary>
/// <param name="ValidatedRows">Number of validated rows</param>
/// <param name="TotalRows">Total number of rows to import</param>
/// <param name="ElapsedTime">Time elapsed since import start</param>
/// <param name="CurrentRule">Currently executing validation rule</param>
/// <param name="ValidationErrors">List of validation errors encountered</param>
public record ImportProgress(
    int ValidatedRows,
    int TotalRows,
    TimeSpan ElapsedTime,
    string CurrentRule = "",
    IReadOnlyList<string> ValidationErrors = null
)
{
    /// <summary>Calculated completion percentage (0-100)</summary>
    public double CompletionPercentage => TotalRows > 0 ? (double)ValidatedRows / TotalRows * 100 : 0;

    /// <summary>Estimated time remaining based on current progress</summary>
    public TimeSpan? EstimatedTimeRemaining => ValidatedRows > 0 && TotalRows > ValidatedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ValidatedRows) / ValidatedRows)
        : null;

    public ImportProgress() : this(0, 0, TimeSpan.Zero, "", Array.Empty<string>()) { }
}

/// <summary>
/// Command for importing data using flexible data sources
/// </summary>
/// <param name="DictionaryData">Data as dictionary collection for import</param>
/// <param name="DataTableData">Data as DataTable for import</param>
/// <param name="CheckboxStates">Checkbox states for selective import (row index -> checked state)</param>
/// <param name="StartRow">Starting row for import operation (1-based index)</param>
/// <param name="Mode">Import mode (Replace/Append/Insert/Merge)</param>
/// <param name="Timeout">Timeout for import operation</param>
/// <param name="ValidationProgress">Progress reporting for validation phase</param>
/// <param name="">Whether to validate data before import</param>
/// <param name="CorrelationId">Operation correlation ID for logging</param>
public record ImportDataCommand(
    List<Dictionary<string, object?>>? DictionaryData = null,
    DataTable? DataTableData = null,
    Dictionary<int, bool>? CheckboxStates = null,
    int StartRow = 1,
    ImportMode Mode = ImportMode.Replace,
    TimeSpan? Timeout = null,
    IProgress<ImportProgress>? ValidationProgress = null,
        string? CorrelationId = null
)
{
    /// <summary>Computed property for data access</summary>
    public List<Dictionary<string, object?>>? Data => DictionaryData;

    /// <summary>Indicates whether command contains valid data</summary>
    public bool HasData => DictionaryData?.Count > 0 || DataTableData?.Rows.Count > 0;

    /// <summary>Factory method for creating import command from dictionary data</summary>
    public static ImportDataCommand FromDictionary(
        List<Dictionary<string, object?>> data,
        Dictionary<int, bool>? checkboxStates = null,
        int startRow = 1,
        ImportMode mode = ImportMode.Replace,
        TimeSpan? timeout = null,
        IProgress<ImportProgress>? validationProgress = null,
        string? correlationId = null) =>
        new(
            DictionaryData: data,
            CheckboxStates: checkboxStates,
            StartRow: startRow,
            Mode: mode,
            Timeout: timeout,
            ValidationProgress: validationProgress,
            CorrelationId: correlationId
        );

    /// <summary>Factory method for creating import command from DataTable</summary>
    public static ImportDataCommand FromDataTable(
        DataTable dataTable,
        Dictionary<int, bool>? checkboxStates = null,
        int startRow = 1,
        ImportMode mode = ImportMode.Replace,
        TimeSpan? timeout = null,
        IProgress<ImportProgress>? validationProgress = null,
        string? correlationId = null) =>
        new(
            DataTableData: dataTable,
            CheckboxStates: checkboxStates,
            StartRow: startRow,
            Mode: mode,
            Timeout: timeout,
            ValidationProgress: validationProgress,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Specialized command for DataTable import operations
/// </summary>
/// <param name="DataTable">DataTable containing data to import</param>
/// <param name="CheckboxStates">Checkbox states for selective import</param>
/// <param name="StartRow">Starting row for import operation</param>
/// <param name="Mode">Import mode</param>
/// <param name="Timeout">Operation timeout</param>
/// <param name="Progress">Progress reporting callback</param>
/// <param name="">Whether to validate before import</param>
/// <param name="CorrelationId">Operation correlation ID</param>
public record ImportFromDataTableCommand(
    DataTable? DataTable = null,
    Dictionary<int, bool>? CheckboxStates = null,
    int StartRow = 1,
    ImportMode Mode = ImportMode.Replace,
    TimeSpan? Timeout = null,
    IProgress<ImportProgress>? Progress = null,
        string? CorrelationId = null
) : ImportDataCommand(
    DictionaryData: null,
    DataTableData: DataTable,
    CheckboxStates: CheckboxStates,
    StartRow: StartRow,
    Mode: Mode,
    Timeout: Timeout,
    ValidationProgress: Progress,
    : ,
    CorrelationId: CorrelationId
)
{
    /// <summary>Factory method for creating DataTable import command</summary>
    public static new ImportFromDataTableCommand FromDataTable(
        DataTable dataTable,
        Dictionary<int, bool>? checkboxStates = null,
        int startRow = 1,
        ImportMode mode = ImportMode.Replace,
        TimeSpan? timeout = null,
        IProgress<ImportProgress>? progress = null,
        string? correlationId = null) =>
        new(
            DataTable: dataTable,
            CheckboxStates: checkboxStates,
            StartRow: startRow,
            Mode: mode,
            Timeout: timeout,
            Progress: progress,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Result of import operation with comprehensive metrics
/// </summary>
/// <param name="Success">Whether import completed successfully</param>
/// <param name="ImportedRows">Number of successfully imported rows</param>
/// <param name="SkippedRows">Number of rows skipped during import</param>
/// <param name="TotalRows">Total number of rows processed</param>
/// <param name="ImportTime">Time taken for import operation</param>
/// <param name="ErrorMessages">List of error messages if import failed</param>
/// <param name="WarningMessages">List of warning messages from import</param>
/// <param name="Mode">Import mode that was used</param>
/// <param name="CorrelationId">Operation correlation ID</param>
public record ImportResult(
    bool Success,
    int ImportedRows,
    int SkippedRows,
    int TotalRows,
    TimeSpan ImportTime,
    IReadOnlyList<string> ErrorMessages,
    IReadOnlyList<string> WarningMessages,
    ImportMode Mode,
    string? CorrelationId = null
)
{
    public ImportResult() : this(false, 0, 0, 0, TimeSpan.Zero, Array.Empty<string>(), Array.Empty<string>(), ImportMode.Replace) { }

    /// <summary>Creates successful import result</summary>
    public static ImportResult CreateSuccess(
        int importedRows,
        int totalRows,
        TimeSpan importTime,
        ImportMode mode = ImportMode.Replace,
        string? correlationId = null,
        IReadOnlyList<string>? warnings = null) =>
        new(
            Success: true,
            ImportedRows: importedRows,
            SkippedRows: totalRows - importedRows,
            TotalRows: totalRows,
            ImportTime: importTime,
            ErrorMessages: Array.Empty<string>(),
            WarningMessages: warnings ?? Array.Empty<string>(),
            Mode: mode,
            CorrelationId: correlationId
        );

    /// <summary>Creates failed import result</summary>
    public static ImportResult Failure(
        IReadOnlyList<string> errors,
        TimeSpan importTime,
        ImportMode mode = ImportMode.Replace,
        string? correlationId = null) =>
        new(
            Success: false,
            ImportedRows: 0,
            SkippedRows: 0,
            TotalRows: 0,
            ImportTime: importTime,
            ErrorMessages: errors,
            WarningMessages: Array.Empty<string>(),
            Mode: mode,
            CorrelationId: correlationId
        );
}

/// <summary>
/// Import validation result for pre-import checks
/// </summary>
/// <param name="IsValid">Whether import data is valid</param>
/// <param name="ValidationErrors">List of validation errors</param>
/// <param name="Warnings">List of validation warnings</param>
/// <param name="EstimatedDuration">Estimated import duration</param>
/// <param name="DataQualityScore">Data quality score (0-100)</param>
public record ImportValidationResult(
    bool IsValid,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<string> Warnings,
    TimeSpan? EstimatedDuration = null,
    double? DataQualityScore = null
);
```

### **2. Import Service Interface**
```csharp
// Application/Interfaces/IImportService.cs

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// Service interface for data import operations with comprehensive import functionality
/// </summary>
internal interface IImportService
{
    /// <summary>
    /// Imports data using command pattern with validation pipeline
    /// </summary>
    /// <param name="command">Import command with data and configuration</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>Import result with metrics and status</returns>
    Task<ImportResult> ImportAsync(
        ImportDataCommand command,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Imports data from DataTable using specialized command
    /// </summary>
    /// <param name="command">DataTable import command</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>Import result with metrics and status</returns>
    Task<ImportResult> ImportFromDataTableAsync(
        ImportFromDataTableCommand command,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Validates import data before actual import operation
    /// </summary>
    /// <param name="command">Import command to validate</param>
    /// <param name="cancellationToken">cancellationToken for operation</param>
    /// <returns>Validation result with errors and warnings</returns>
    Task<ImportValidationResult> ValidateImportDataAsync(
        ImportDataCommand command,
        cancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supported import modes for current data configuration
    /// </summary>
    /// <param name="dataType">Type of data to import</param>
    /// <param name="targetSchema">Target schema for validation</param>
    /// <returns>List of supported import modes</returns>
    IReadOnlyList<ImportMode> GetSupportedImportModes(Type dataType, object? targetSchema = null);

    /// <summary>
    /// Estimates import duration and resource requirements
    /// </summary>
    /// <param name="command">Import command to analyze</param>
    /// <returns>Estimation result with duration and resource requirements</returns>
    Task<(TimeSpan EstimatedDuration, long EstimatedMemoryUsage)> EstimateImportRequirementsAsync(
        ImportDataCommand command);
}
```

### **3. Facade API Integration**
```csharp
// Public API methods pre Facade Pattern (AdvancedWinUiDataGrid.cs)

/// <summary>
/// Imports data using command pattern with LINQ optimization and validation pipeline
/// </summary>
/// <param name="data">Data to import as dictionary collection</param>
/// <param name="mode">Import mode (Replace/Append/Insert/Merge)</param>
/// <param name="validateBeforeImport">Whether to validate data before import</param>
/// <param name="progress">Progress reporting callback for validation</param>
/// <param name="startRow">Starting row for import (1-based index)</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Import result with comprehensive metrics</returns>
public async Task<ImportResult> ImportAsync(
    List<Dictionary<string, object?>> data,
    ImportMode mode = ImportMode.Replace,
    bool validateBeforeImport = true,
    IProgress<ImportProgress>? progress = null,
    int startRow = 1,
    cancellationToken cancellationToken = default)
{
    var correlationId = Guid.NewGuid().ToString();
    _logger.LogInformation("Starting data import: {RowCount} rows in {Mode} mode [CorrelationId: {CorrelationId}]",
        data.Count, mode, correlationId);

    var command = ImportDataCommand.FromDictionary(
        data: data,
        mode: mode,
        startRow: startRow,
        validationProgress: progress,
        validateBeforeImport: validateBeforeImport,
        correlationId: correlationId
    );

    return await _importService.ImportAsync(command, cancellationToken);
}

/// <summary>
/// Imports data from DataTable using specialized import command
/// </summary>
/// <param name="dataTable">DataTable containing data to import</param>
/// <param name="mode">Import mode</param>
/// <param name="validateBeforeImport">Whether to validate before import</param>
/// <param name="progress">Progress reporting callback</param>
/// <param name="startRow">Starting row for import</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Import result with metrics and status</returns>
public async Task<ImportResult> ImportFromDataTableAsync(
    DataTable dataTable,
    ImportMode mode = ImportMode.Replace,
    bool validateBeforeImport = true,
    IProgress<ImportProgress>? progress = null,
    int startRow = 1,
    cancellationToken cancellationToken = default)
{
    var correlationId = Guid.NewGuid().ToString();
    _logger.LogInformation("Starting DataTable import: {RowCount} rows in {Mode} mode [CorrelationId: {CorrelationId}]",
        dataTable.Rows.Count, mode, correlationId);

    var command = ImportFromDataTableCommand.FromDataTable(
        dataTable: dataTable,
        mode: mode,
        startRow: startRow,
        progress: progress,
        validateBeforeImport: validateBeforeImport,
        correlationId: correlationId
    );

    return await _importService.ImportFromDataTableAsync(command, cancellationToken);
}

/// <summary>
/// Validates import data configuration before executing import
/// </summary>
/// <param name="data">Data to validate for import</param>
/// <param name="mode">Intended import mode</param>
/// <param name="cancellationToken">cancellationToken for operation</param>
/// <returns>Validation result with errors, warnings, and recommendations</returns>
public async Task<ImportValidationResult> ValidateImportDataAsync(
    List<Dictionary<string, object?>> data,
    ImportMode mode = ImportMode.Replace,
    cancellationToken cancellationToken = default)
{
    var command = ImportDataCommand.FromDictionary(data, mode: mode);
    return await _importService.ValidateImportDataAsync(command, cancellationToken);
}
```

## **Enhanced Import Service Implementation Pattern:**

### **LINQ Optimizations & Performance Features:**
```csharp
// Application/Services/ImportService.cs - ENHANCED s LINQ optimalizáciami

internal sealed class ImportService : IImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly IImportLogger<ImportService> _importLogger;
    private readonly ICommandLogger<ImportService> _commandLogger;

    public async Task<ImportResult> ImportAsync(ImportDataCommand command,
        cancellationToken cancellationToken = default)
    {
        using var scope = _importLogger.LogCommandOperationStart(command,
            new { mode = command.Mode, hasData = command.HasData, validateFirst = command. });

        _logger.LogInformation("Starting import operation: {Mode} mode, validate={Validate} [CorrelationId: {CorrelationId}]",
            command.Mode, command., command.CorrelationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // LINQ Optimization: Count optimization s short-circuit evaluation
            var sourceData = command.DictionaryData ?? ConvertDataTableToDictionary(command.DataTableData);
            var totalRows = sourceData?.Count ?? 0;

            if (totalRows == 0)
            {
                _logger.LogWarning("Import aborted: No data provided [CorrelationId: {CorrelationId}]", command.CorrelationId);
                return ImportResult.Failure(new[] { "No data provided for import" }, stopwatch.Elapsed, command.Mode, command.CorrelationId);
            }

            _logger.LogInformation("Import data analysis: {TotalRows} rows to import", totalRows);

            // Validation phase s progress reporting
            if (command.)
            {
                var validationResult = await ValidateDataWithProgressAsync(sourceData, command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger.LogError("Import validation failed: {ErrorCount} errors [CorrelationId: {CorrelationId}]",
                        validationResult.ValidationErrors.Count, command.CorrelationId);
                    return ImportResult.Failure(validationResult.ValidationErrors, stopwatch.Elapsed, command.Mode, command.CorrelationId);
                }
            }

            // LINQ Optimization: Parallel processing pre veľké datasets
            var useParallel = totalRows > 1000;
            var processedData = useParallel
                ? await ProcessDataParallelAsync(sourceData, command, cancellationToken)
                : await ProcessDataSequentialAsync(sourceData, command, cancellationToken);

            _importLogger.LogLINQOptimization("ImportDataProcessing", useParallel, false, stopwatch.Elapsed);

            // Import operation implementation
            var result = await ExecuteImportOperationAsync(processedData, command, cancellationToken);

            _importLogger.LogImportOperation(command.Mode.ToString(), totalRows, result.ImportedRows, stopwatch.Elapsed);

            _logger.LogInformation("Import completed successfully: {ImportedRows}/{TotalRows} rows in {Duration}ms",
                result.ImportedRows, totalRows, stopwatch.ElapsedMilliseconds);

            scope.MarkSuccess(new { importedRows = result.ImportedRows, mode = result.Mode });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import operation failed for mode {Mode} [CorrelationId: {CorrelationId}]",
                command.Mode, command.CorrelationId);
            scope.MarkError(ex.Message);
            throw;
        }
    }

    private async Task<IEnumerable<Dictionary<string, object?>>> ProcessDataParallelAsync(
        IEnumerable<Dictionary<string, object?>> sourceData,
        ImportDataCommand command,
        cancellationToken cancellationToken)
    {
        // LINQ Optimization: Parallel processing s partition-based approach
        return await Task.Run(() =>
        {
            var partitioner = Partitioner.Create(sourceData, EnumerablePartitionerOptions.NoBuffering);

            return partitioner.AsParallel()
                .WithCancellation(cancellationToken)
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select((row, index) =>
                {
                    // Progress reporting every 100 rows
                    if (index % 100 == 0 && command.ValidationProgress != null)
                    {
                        command.ValidationProgress.Report(new ImportProgress(
                            ValidatedRows: index,
                            TotalRows: sourceData.Count(),
                            ElapsedTime: TimeSpan.FromMilliseconds(index * 10), // Estimated
                            CurrentRule: $"Processing row {index}"
                        ));
                    }

                    return ProcessImportRow(row, command, index);
                })
                .Where(row => row != null)
                .ToList();
        }, cancellationToken);
    }
}
```

## **Enhanced Import Logging Integration:**

### **IImportLogger Interface:**
```csharp
// Infrastructure/Logging/IImportLogger.cs

internal interface IImportLogger<T> : IOperationLogger<T>
{
    /// <summary>
    /// Logs import operation with comprehensive metrics
    /// </summary>
    void LogImportOperation(string importMode, int totalRows, int importedRows, TimeSpan duration);

    /// <summary>
    /// Logs data conversion during import process
    /// </summary>
    void LogDataConversion(string fromFormat, string toFormat, int rowCount, TimeSpan duration);

    /// <summary>
    /// Logs import validation with detailed results
    /// </summary>
    void LogImportValidation(string validationType, bool isValid, int errorCount, string? summary = null);

    /// <summary>
    /// Logs progress reporting during import operations
    /// </summary>
    void LogProgressReporting(string operationType, double progressPercentage, int processedItems, int totalItems);

    /// <summary>
    /// Logs import mode-specific operations (Replace, Append, Insert, Merge)
    /// </summary>
    void LogImportModeOperation(string mode, string operation, bool success, TimeSpan duration);

    /// <summary>
    /// Logs selective import operations when checkbox states are used
    /// </summary>
    void LogSelectiveImport(int selectedRows, int totalRows, string selectionCriteria);
}
```

## **Internal DI Registration:**
```csharp
// Infrastructure/Services/InternalServiceRegistration.cs - Import Module Addition

public static IServiceCollection AddAdvancedWinUiDataGridInternal(this IServiceCollection services)
{
    // ... existing registrations ...

    // Import module services
    services.AddScoped<IImportService, ImportService>();
    services.AddSingleton(typeof(IImportLogger<>), typeof(ImportLogger<>));

    // Import data processors
    services.AddSingleton<IDataTableImportProcessor, DataTableImportProcessor>();
    services.AddSingleton<IDictionaryImportProcessor, DictionaryImportProcessor>();
    services.AddSingleton<IImportValidationProcessor, ImportValidationProcessor>();

    return services;
}
```

## **Performance & Optimization Features:**

### **1. LINQ Query Optimizations**
- **Parallel Processing**: Automatic parallel processing pre datasets > 1000 rows
- **Memory Efficiency**: Streaming processing pre veľké imports
- **Short-Circuit Evaluation**: Early termination pre validation errors
- **Lazy Evaluation**: Deferred execution až po validation

### **2. Import Mode Optimizations**
- **Replace**: Bulk clear + bulk insert s transaction wrapping
- **Append**: Direct append s minimal validation overhead
- **Insert**: Position-aware insertion s index management
- **Merge**: Key-based matching s LINQ Join optimizations

### **3. Validation Pipeline**
- **Progressive Validation**: Row-by-row s early termination
- **Parallel Validation**: Multi-threaded validation pre independent rules
- **Caching**: Validation rule compilation a result caching

## **Command Pattern Integration:**
Všetky import operations implementované ako commands:
- **ImportDataCommand**: Main import command s flexible data sources
- **ImportFromDataTableCommand**: Specialized DataTable import
- **ValidateImportCommand**: Pre-import validation
- **EstimateImportCommand**: Resource estimation

Každý command má svoj dedicated handler s comprehensive logging a error handling.

## **Integration s Existing Modules:**
Import systém je navrhnutý pre bezproblémovú integráciu s:
- **Validation System**: Automatic validation pipeline integration
- **Filter System**: Import filtered data s preserve filter state
- **Search System**: Import search results s highlighting preservation
- **Export System**: Round-trip import/export s data integrity checks

## **Future Extensions:**
- **Rollback Support**: Transaction-based import s rollback capability

### **Logging Levels Usage:**
- **Information**: Successful imports/exports, progress milestones, data statistics
- **Warning**: Partial import failures, format conversion warnings, large dataset warnings
- **Error**: Import/export failures, clipboard access errors, validation failures
- **Critical**: Data corruption detection, system resource exhaustion


### ImportDataCommand (public API)

The import entry accepts an `ImportDataCommand` record. Example shape:

```csharp

public record ImportDataCommand(
    List<Dictionary<string, object?>>? DictionaryData = null,
    DataTable? DataTableData = null,
    Dictionary<int, bool>? CheckboxStates = null,
    int StartRow = 1,
    ImportMode Mode = ImportMode.Replace,
    TimeSpan? Timeout = null,
    IProgress<ImportProgress>? ValidationProgress = null,
    string? CorrelationId = null
);

```

- **Paste** operations use the same import flow: after paste completes, a **bulk validation** is executed. Bulk validation can validate in parts (to reduce memory spikes) but **UI must only be updated at the end** of the bulk validation pass unless running in *headless mode*.
  - In headless mode the component does **not** automatically update UI; instead the public API exposes a method to request UI refresh for validation results — callers may invoke it when appropriate.
- `ValidationProgress` is used to report progress of the validation phase (bulk validation). It should be reported with coarse-grained updates (e.g., per batch) to avoid UI thrashing.

Example flow in `ImportService`:
```csharp
// After import/paste materializes data
if (command.)
{
    // full dataset validation mandated for import
    var result = await _validationService.AreAllNonEmptyRowsValidAsync(false); // false => whole dataset
    if (!result.IsSuccess || !result.Value)
    {
        // mark import as failed/needs-fix, report errors
        return ImportResult.Failure("Validation failed");
    }
}

// continue with post-import operations (indexing, caching, notifications)
```


### Bulk validation & UI update semantics (Import & Paste)

- Bulk validation may run in chunks to avoid blocking or memory pressure. Implementations SHOULD:
  - Validate the entire dataset logically (all rows) but may stream/partition validation work into batches.
  - Aggregate validation results and apply UI updates only once per overall operation (not per batch) **unless** the component is configured to show incremental results.
- **Headless mode**: when the component is created in headless mode it **must not** automatically update UI during validation. Instead, provide a public API method (on `AdvancedDataGridFacade`) that allows callers to request the latest validation results be pushed to the UI when desired. This same API works when used from UI or headless contexts.
  - Example method on facade: `void RefreshValidationResultsToUI();`



## Automatic validation after import / paste


**Paste** operations use the same import flow: after paste completes, a **bulk validation** is executed. Bulk validation can validate in parts (to reduce memory spikes) but **UI must only be updated at the end** of the bulk validation pass unless running in *headless mode*.
- In headless mode the component does **not** automatically update UI; instead the public API exposes a method to request UI refresh for validation results — callers may invoke it when appropriate.
- `ValidationProgress` is used to report progress of the validation phase (bulk validation). It should be reported with coarse-grained updates (e.g., per batch) to avoid UI thrashing.



**Headless export behavior:** If the component is running in headless mode and the consumer does **not** call `RefreshValidationResultsToUI()` after import/paste, it is still safe to immediately perform an export. The export flow itself invokes `AreAllNonEmptyRowsValidAsync(...)` before exporting; when `IncludeValidAlerts = true` the export will include the correct, up-to-date `validAlerts` content produced by validation even if the UI was never refreshed. In other words: exporting immediately after import/paste in headless mode will still produce the expected `validAlerts` values because validation is executed as part of the export pre-check.


After an import or paste operation completes and the data is materialized, the component **automatically** performs validation if **validation rules are configured** in the system (see `ValidationDocumentation.md`). There is **no** `` flag on the public import API — validation is implicit and always executed when rules exist.

Behavior and usage:
- The import/paste flow calls the canonical validation entry `AreAllNonEmptyRowsValidAsync(false)` automatically (false = validate whole dataset) after the data is materialized.
- `AreAllNonEmptyRowsValidAsync` semantics:
  - If **no validation rules** are configured, the method returns `Result<bool>.Success(true)` (everything is considered valid).
  - If validation rules are configured and **all non-empty rows are already validated**, the method returns the cached boolean result (true/false).
  - If validation rules are configured and **not all non-empty rows are validated**, the method performs a full validation pass (may be batched) across all rows (in-memory, cached, on-disk), then returns the validation result (true if all non-empty rows pass, otherwise false).
- Bulk validation is allowed to run in batches to reduce memory pressure. Implementations MUST ensure the final UI update (displaying validation results) is applied only after the overall validation completes unless the component is running in headless mode.
- `ValidationProgress` (if provided on import) receives progress updates for the validation phase (coarse-grained updates recommended).
- The validation operation should honor `CancellationToken` when invoked from import/paste flows where applicable.

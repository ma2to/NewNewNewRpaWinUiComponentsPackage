using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Logging;

/// <summary>
/// Internal specialized logger for import operations
/// Provides detailed logging and performance tracking for data import scenarios
/// </summary>
internal sealed class ImportLogger
{
    private readonly ILogger<ImportLogger> _logger;

    public ImportLogger(ILogger<ImportLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log import operation start
    /// </summary>
    /// <param name="operationId">Unique operation identifier</param>
    /// <param name="dataSource">Source of import data (DataTable, Dictionary, etc.)</param>
    /// <param name="rowCount">Number of rows to import</param>
    /// <param name="importMode">Import mode (Replace, Append, Insert, Merge)</param>
    public void LogImportStart(Guid operationId, string dataSource, int rowCount, string importMode)
    {
        _logger.LogInformation("Import operation started [{OperationId}]: Source={DataSource}, Rows={RowCount}, Mode={ImportMode}",
            operationId, dataSource, rowCount, importMode);
    }

    /// <summary>
    /// Log import progress
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="processedRows">Number of rows processed</param>
    /// <param name="totalRows">Total number of rows</param>
    /// <param name="elapsedTime">Time elapsed since start</param>
    public void LogImportProgress(Guid operationId, int processedRows, int totalRows, TimeSpan elapsedTime)
    {
        var progressPercentage = totalRows > 0 ? (double)processedRows / totalRows * 100 : 0;

        _logger.LogDebug("Import progress [{OperationId}]: {ProcessedRows}/{TotalRows} ({ProgressPercentage:F1}%) - Elapsed: {ElapsedTime}ms",
            operationId, processedRows, totalRows, progressPercentage, elapsedTime.TotalMilliseconds);
    }

    /// <summary>
    /// Log import validation start
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="validationRuleCount">Number of validation rules to apply</param>
    public void LogValidationStart(Guid operationId, int validationRuleCount)
    {
        _logger.LogInformation("Import validation started [{OperationId}]: ValidationRules={ValidationRuleCount}",
            operationId, validationRuleCount);
    }

    /// <summary>
    /// Log validation results
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="validatedRows">Number of rows validated</param>
    /// <param name="validRows">Number of valid rows</param>
    /// <param name="errorCount">Number of validation errors</param>
    /// <param name="validationTime">Time spent on validation</param>
    public void LogValidationResults(Guid operationId, int validatedRows, int validRows, int errorCount, TimeSpan validationTime)
    {
        _logger.LogInformation("Import validation completed [{OperationId}]: Validated={ValidatedRows}, Valid={ValidRows}, Errors={ErrorCount}, Duration={Duration}ms",
            operationId, validatedRows, validRows, errorCount, validationTime.TotalMilliseconds);
    }

    /// <summary>
    /// Log import completion
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="success">Whether import was successful</param>
    /// <param name="importedRows">Number of successfully imported rows</param>
    /// <param name="totalTime">Total operation time</param>
    /// <param name="errorMessage">Error message if failed</param>
    public void LogImportCompletion(Guid operationId, bool success, int importedRows, TimeSpan totalTime, string? errorMessage = null)
    {
        if (success)
        {
            _logger.LogInformation("Import operation completed successfully [{OperationId}]: ImportedRows={ImportedRows}, Duration={Duration}ms",
                operationId, importedRows, totalTime.TotalMilliseconds);
        }
        else
        {
            _logger.LogError("Import operation failed [{OperationId}]: ImportedRows={ImportedRows}, Duration={Duration}ms, Error={ErrorMessage}",
                operationId, importedRows, totalTime.TotalMilliseconds, errorMessage ?? "Unknown error");
        }
    }

    /// <summary>
    /// Log data transformation details
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="sourceFormat">Source data format</param>
    /// <param name="targetFormat">Target data format</param>
    /// <param name="transformedCount">Number of transformed items</param>
    /// <param name="transformationTime">Time spent on transformation</param>
    public void LogDataTransformation(Guid operationId, string sourceFormat, string targetFormat, int transformedCount, TimeSpan transformationTime)
    {
        _logger.LogDebug("Data transformation [{OperationId}]: {SourceFormat} -> {TargetFormat}, Items={TransformedCount}, Duration={Duration}ms",
            operationId, sourceFormat, targetFormat, transformedCount, transformationTime.TotalMilliseconds);
    }

    /// <summary>
    /// Log batch processing information
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="batchNumber">Current batch number</param>
    /// <param name="batchSize">Size of current batch</param>
    /// <param name="batchTime">Time to process batch</param>
    public void LogBatchProcessing(Guid operationId, int batchNumber, int batchSize, TimeSpan batchTime)
    {
        _logger.LogTrace("Batch processed [{OperationId}]: Batch={BatchNumber}, Size={BatchSize}, Duration={Duration}ms",
            operationId, batchNumber, batchSize, batchTime.TotalMilliseconds);
    }

    /// <summary>
    /// Log import performance metrics
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="rowsPerSecond">Processing rate in rows per second</param>
    /// <param name="memoryUsed">Memory used during import (bytes)</param>
    /// <param name="peakMemory">Peak memory usage (bytes)</param>
    public void LogPerformanceMetrics(Guid operationId, double rowsPerSecond, long memoryUsed, long peakMemory)
    {
        _logger.LogInformation("Import performance [{OperationId}]: Rate={RowsPerSecond:F2} rows/sec, Memory={MemoryUsed:N0} bytes, Peak={PeakMemory:N0} bytes",
            operationId, rowsPerSecond, memoryUsed, peakMemory);
    }

    /// <summary>
    /// Log critical import error
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="context">Additional context information</param>
    public void LogCriticalError(Guid operationId, Exception exception, string context)
    {
        _logger.LogCritical(exception, "Critical import error [{OperationId}]: Context={Context}",
            operationId, context);
    }
}
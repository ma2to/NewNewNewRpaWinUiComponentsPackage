using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Logging;

/// <summary>
/// Internal specialized logger for export operations
/// Provides detailed logging and performance tracking for data export scenarios
/// </summary>
internal sealed class ExportLogger
{
    private readonly ILogger<ExportLogger> _logger;

    public ExportLogger(ILogger<ExportLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log export operation start
    /// </summary>
    /// <param name="operationId">Unique operation identifier</param>
    /// <param name="targetFormat">Target export format (DataTable, Dictionary)</param>
    /// <param name="rowCount">Number of rows to export</param>
    /// <param name="columnCount">Number of columns to export</param>
    /// <param name="onlyFiltered">Whether exporting only filtered data</param>
    /// <param name="onlyChecked">Whether exporting only checked rows</param>
    public void LogExportStart(Guid operationId, string targetFormat, int rowCount, int columnCount, bool onlyFiltered, bool onlyChecked)
    {
        _logger.LogInformation("Export operation started [{OperationId}]: Format={TargetFormat}, Rows={RowCount}, Columns={ColumnCount}, OnlyFiltered={OnlyFiltered}, OnlyChecked={OnlyChecked}",
            operationId, targetFormat, rowCount, columnCount, onlyFiltered, onlyChecked);
    }

    /// <summary>
    /// Log export progress
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="processedRows">Number of rows processed</param>
    /// <param name="totalRows">Total number of rows</param>
    /// <param name="elapsedTime">Time elapsed since start</param>
    public void LogExportProgress(Guid operationId, int processedRows, int totalRows, TimeSpan elapsedTime)
    {
        var progressPercentage = totalRows > 0 ? (double)processedRows / totalRows * 100 : 0;

        _logger.LogDebug("Export progress [{OperationId}]: {ProcessedRows}/{TotalRows} ({ProgressPercentage:F1}%) - Elapsed: {ElapsedTime}ms",
            operationId, processedRows, totalRows, progressPercentage, elapsedTime.TotalMilliseconds);
    }

    /// <summary>
    /// Log data filtering applied before export
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="totalRows">Total rows available</param>
    /// <param name="filteredRows">Rows after filtering</param>
    /// <param name="checkedRows">Rows that are checked</param>
    /// <param name="finalRows">Final rows to export after all filters</param>
    public void LogDataFiltering(Guid operationId, int totalRows, int filteredRows, int checkedRows, int finalRows)
    {
        _logger.LogInformation("Export data filtering [{OperationId}]: Total={TotalRows}, Filtered={FilteredRows}, Checked={CheckedRows}, Final={FinalRows}",
            operationId, totalRows, filteredRows, checkedRows, finalRows);
    }

    /// <summary>
    /// Log column selection details
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="availableColumns">Total available columns</param>
    /// <param name="selectedColumns">Columns selected for export</param>
    /// <param name="includeValidationAlerts">Whether validation alerts column is included</param>
    public void LogColumnSelection(Guid operationId, int availableColumns, int selectedColumns, bool includeValidationAlerts)
    {
        _logger.LogDebug("Export column selection [{OperationId}]: Available={AvailableColumns}, Selected={SelectedColumns}, IncludeValidationAlerts={IncludeValidationAlerts}",
            operationId, availableColumns, selectedColumns, includeValidationAlerts);
    }

    /// <summary>
    /// Log data transformation during export
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="sourceFormat">Source data format</param>
    /// <param name="targetFormat">Target export format</param>
    /// <param name="transformedRows">Number of transformed rows</param>
    /// <param name="transformationTime">Time spent on transformation</param>
    public void LogDataTransformation(Guid operationId, string sourceFormat, string targetFormat, int transformedRows, TimeSpan transformationTime)
    {
        _logger.LogDebug("Export data transformation [{OperationId}]: {SourceFormat} -> {TargetFormat}, Rows={TransformedRows}, Duration={Duration}ms",
            operationId, sourceFormat, targetFormat, transformedRows, transformationTime.TotalMilliseconds);
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
        _logger.LogTrace("Export batch processed [{OperationId}]: Batch={BatchNumber}, Size={BatchSize}, Duration={Duration}ms",
            operationId, batchNumber, batchSize, batchTime.TotalMilliseconds);
    }

    /// <summary>
    /// Log validation check before export
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="validationRequired">Whether validation was required</param>
    /// <param name="validationPassed">Whether validation passed</param>
    /// <param name="errorCount">Number of validation errors found</param>
    /// <param name="validationTime">Time spent on validation</param>
    public void LogValidationCheck(Guid operationId, bool validationRequired, bool validationPassed, int errorCount, TimeSpan validationTime)
    {
        _logger.LogInformation("Export validation check [{OperationId}]: Required={ValidationRequired}, Passed={ValidationPassed}, Errors={ErrorCount}, Duration={Duration}ms",
            operationId, validationRequired, validationPassed, errorCount, validationTime.TotalMilliseconds);
    }

    /// <summary>
    /// Log export completion
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="success">Whether export was successful</param>
    /// <param name="exportedRows">Number of successfully exported rows</param>
    /// <param name="exportedColumns">Number of exported columns</param>
    /// <param name="outputSize">Size of exported data (approximate bytes)</param>
    /// <param name="totalTime">Total operation time</param>
    /// <param name="errorMessage">Error message if failed</param>
    public void LogExportCompletion(Guid operationId, bool success, int exportedRows, int exportedColumns, long outputSize, TimeSpan totalTime, string? errorMessage = null)
    {
        if (success)
        {
            _logger.LogInformation("Export operation completed successfully [{OperationId}]: Rows={ExportedRows}, Columns={ExportedColumns}, Size={OutputSize:N0} bytes, Duration={Duration}ms",
                operationId, exportedRows, exportedColumns, outputSize, totalTime.TotalMilliseconds);
        }
        else
        {
            _logger.LogError("Export operation failed [{OperationId}]: Rows={ExportedRows}, Columns={ExportedColumns}, Duration={Duration}ms, Error={ErrorMessage}",
                operationId, exportedRows, exportedColumns, totalTime.TotalMilliseconds, errorMessage ?? "Unknown error");
        }
    }

    /// <summary>
    /// Log export performance metrics
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="rowsPerSecond">Processing rate in rows per second</param>
    /// <param name="memoryUsed">Memory used during export (bytes)</param>
    /// <param name="peakMemory">Peak memory usage (bytes)</param>
    /// <param name="throughputMBps">Data throughput in MB/s</param>
    public void LogPerformanceMetrics(Guid operationId, double rowsPerSecond, long memoryUsed, long peakMemory, double throughputMBps)
    {
        _logger.LogInformation("Export performance [{OperationId}]: Rate={RowsPerSecond:F2} rows/sec, Throughput={ThroughputMBps:F2} MB/s, Memory={MemoryUsed:N0} bytes, Peak={PeakMemory:N0} bytes",
            operationId, rowsPerSecond, throughputMBps, memoryUsed, peakMemory);
    }

    /// <summary>
    /// Log data quality metrics
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="nullCells">Number of null cells exported</param>
    /// <param name="emptyCells">Number of empty cells exported</param>
    /// <param name="validationErrors">Number of validation errors in exported data</param>
    public void LogDataQualityMetrics(Guid operationId, int nullCells, int emptyCells, int validationErrors)
    {
        _logger.LogDebug("Export data quality [{OperationId}]: NullCells={NullCells}, EmptyCells={EmptyCells}, ValidationErrors={ValidationErrors}",
            operationId, nullCells, emptyCells, validationErrors);
    }

    /// <summary>
    /// Log critical export error
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="context">Additional context information</param>
    public void LogCriticalError(Guid operationId, Exception exception, string context)
    {
        _logger.LogCritical(exception, "Critical export error [{OperationId}]: Context={Context}",
            operationId, context);
    }
}
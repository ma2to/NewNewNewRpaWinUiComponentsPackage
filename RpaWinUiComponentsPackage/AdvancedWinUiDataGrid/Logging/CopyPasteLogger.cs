using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Logging;

/// <summary>
/// Internal specialized logger for copy/paste operations
/// Provides detailed logging and performance tracking for clipboard operations
/// </summary>
internal sealed class CopyPasteLogger
{
    private readonly ILogger<CopyPasteLogger> _logger;

    public CopyPasteLogger(ILogger<CopyPasteLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log copy operation start
    /// </summary>
    public void LogCopyStart(Guid operationId, int selectedRows, int selectedColumns, string format)
    {
        _logger.LogInformation("Copy operation started [{OperationId}]: Rows={SelectedRows}, Columns={SelectedColumns}, Format={Format}",
            operationId, selectedRows, selectedColumns, format);
    }

    /// <summary>
    /// Log copy completion
    /// </summary>
    public void LogCopyCompletion(Guid operationId, bool success, int copiedCells, long dataSize, TimeSpan duration)
    {
        if (success)
        {
            _logger.LogInformation("Copy operation completed successfully [{OperationId}]: Cells={CopiedCells}, Size={DataSize:N0} bytes, Duration={Duration}ms",
                operationId, copiedCells, dataSize, duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogError("Copy operation failed [{OperationId}]: Duration={Duration}ms",
                operationId, duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Log paste operation start
    /// </summary>
    public void LogPasteStart(Guid operationId, string format, int targetRow, int targetColumn)
    {
        _logger.LogInformation("Paste operation started [{OperationId}]: Format={Format}, TargetRow={TargetRow}, TargetColumn={TargetColumn}",
            operationId, format, targetRow, targetColumn);
    }

    /// <summary>
    /// Log paste completion
    /// </summary>
    public void LogPasteCompletion(Guid operationId, bool success, int pastedCells, int affectedRows, TimeSpan duration)
    {
        if (success)
        {
            _logger.LogInformation("Paste operation completed successfully [{OperationId}]: Cells={PastedCells}, AffectedRows={AffectedRows}, Duration={Duration}ms",
                operationId, pastedCells, affectedRows, duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogError("Paste operation failed [{OperationId}]: Duration={Duration}ms",
                operationId, duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Log critical copy/paste error
    /// </summary>
    public void LogCriticalError(Guid operationId, Exception exception, string context)
    {
        _logger.LogCritical(exception, "Critical copy/paste error [{OperationId}]: Context={Context}",
            operationId, context);
    }
}

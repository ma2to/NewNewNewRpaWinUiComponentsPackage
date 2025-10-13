using System.Data;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Partial class containing Private Helper Methods + Helper Classes + Placeholder Implementations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Private Helper Methods

    /// <summary>
    /// Centralized UI refresh logic - triggers automatic UI refresh ONLY in Interactive mode
    /// </summary>
    /// <param name="operationType">Type of operation that triggered refresh</param>
    /// <param name="affectedRows">Number of affected rows</param>
    private async Task TriggerUIRefreshIfNeededAsync(string operationType, int affectedRows)
    {
        // Automatický refresh LEN v Interactive mode
        if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
        {
            await _uiNotificationService.NotifyDataRefreshAsync(affectedRows, operationType);
        }
        // V Headless mode → skip (automatický refresh je zakázaný)
    }

    private static long EstimateDataTableSize(DataTable dataTable)
    {
        // Rough estimation of DataTable size in bytes
        return dataTable.Rows.Count * dataTable.Columns.Count * 50L;
    }

    private static long EstimateDictionarySize(IReadOnlyList<IReadOnlyDictionary<string, object?>> dictionaries)
    {
        // Rough estimation of dictionary collection size in bytes
        var avgColumns = dictionaries.FirstOrDefault()?.Count ?? 0;
        return dictionaries.Count * avgColumns * 30L;
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Helper class for accumulating rule statistics during validation
    /// </summary>
    private class RuleStatsAccumulator
    {
        public string RuleName { get; set; } = string.Empty;
        public int ExecutionCount { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public int ErrorsFound { get; set; }
    }

    #endregion
}

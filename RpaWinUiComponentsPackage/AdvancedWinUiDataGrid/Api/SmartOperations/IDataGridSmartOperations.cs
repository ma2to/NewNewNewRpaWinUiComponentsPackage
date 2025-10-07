
namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.SmartOperations;

/// <summary>
/// Public interface for DataGrid smart operations.
/// Provides intelligent automation features like auto-fill, pattern detection, and smart completion.
/// </summary>
public interface IDataGridSmartOperations
{
    /// <summary>
    /// Auto-fills cells based on pattern detection.
    /// </summary>
    /// <param name="startRowIndex">Start row index</param>
    /// <param name="endRowIndex">End row index</param>
    /// <param name="columnName">Column name to auto-fill</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> AutoFillAsync(int startRowIndex, int endRowIndex, string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects and applies patterns in selected cells.
    /// </summary>
    /// <param name="rowIndices">Row indices to analyze</param>
    /// <param name="columnName">Column name to analyze</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with detected pattern information</returns>
    Task<PublicResult<PublicPatternInfo>> DetectPatternAsync(IEnumerable<int> rowIndices, string columnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a formula to selected cells.
    /// </summary>
    /// <param name="rowIndices">Row indices to apply formula to</param>
    /// <param name="columnName">Column name to apply formula to</param>
    /// <param name="formula">Formula expression</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ApplyFormulaAsync(IEnumerable<int> rowIndices, string columnName, string formula, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs smart completion based on existing data.
    /// </summary>
    /// <param name="rowIndex">Row index to complete</param>
    /// <param name="columnName">Column name to complete</param>
    /// <param name="partialValue">Partial value entered</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with completion suggestions</returns>
    Task<PublicResult<IReadOnlyList<string>>> GetCompletionSuggestionsAsync(int rowIndex, string columnName, string partialValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes duplicates from the grid based on column values.
    /// </summary>
    /// <param name="columnNames">Column names to check for duplicates</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with count of removed rows</returns>
    Task<PublicResult<int>> RemoveDuplicatesAsync(IEnumerable<string> columnNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds and highlights anomalies in data.
    /// </summary>
    /// <param name="columnName">Column name to analyze</param>
    /// <param name="sensitivity">Anomaly detection sensitivity (0.0-1.0)</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with anomaly information</returns>
    Task<PublicResult<IReadOnlyList<PublicAnomalyInfo>>> DetectAnomaliesAsync(string columnName, double sensitivity = 0.5, CancellationToken cancellationToken = default);
}

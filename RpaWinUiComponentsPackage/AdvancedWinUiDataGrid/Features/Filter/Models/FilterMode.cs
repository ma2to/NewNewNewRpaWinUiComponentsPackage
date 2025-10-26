namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Models;

/// <summary>
/// Filter mode for column filtering
/// - Checkbox: Filter by selecting values from unique value list (SQL WHERE IN)
/// - Regex: Filter by regex pattern matching (SQL WHERE REGEXP)
/// </summary>
public enum FilterMode
{
    /// <summary>
    /// Checkbox mode: SELECT unique values, filter by SQL WHERE IN (...)
    /// Use case: Filter by specific known values
    /// Example: Show only rows where Status IN ('Active', 'Pending')
    /// </summary>
    Checkbox,

    /// <summary>
    /// Regex mode: Enter regex pattern, filter by SQL WHERE REGEXP
    /// Use case: Advanced pattern matching
    /// Example: Show rows where Email REGEXP '^test.*@example\.com$'
    /// </summary>
    Regex
}

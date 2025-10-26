using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SpecialColumns.Services;

/// <summary>
/// Service for managing 3-state checkbox header logic
/// Handles checkbox column header state calculation and toggle operations
/// States:
/// - true (Checked): All rows are selected
/// - false (Unchecked): No rows are selected
/// - null (Indeterminate): Some rows are selected, some are not
/// </summary>
internal sealed class CheckboxHeaderService
{
    private readonly ILogger<CheckboxHeaderService>? _logger;

    public CheckboxHeaderService(ILogger<CheckboxHeaderService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculate 3-state checkbox header state based on current row selection
    /// </summary>
    /// <param name="rows">All rows in the grid</param>
    /// <returns>
    /// - true: All rows are selected (100%)
    /// - false: No rows are selected (0%)
    /// - null: Partial selection (1-99%)
    /// </returns>
    public bool? GetHeaderCheckState(IEnumerable<DataGridRowViewModel> rows)
    {
        if (rows == null || !rows.Any())
        {
            _logger?.LogDebug("GetHeaderCheckState: No rows available, returning false (unchecked)");
            return false;
        }

        var rowList = rows.ToList();
        var checkedCount = rowList.Count(r => r.IsSelected);
        var totalCount = rowList.Count;

        _logger?.LogDebug(
            "GetHeaderCheckState: {CheckedCount}/{TotalCount} rows selected",
            checkedCount, totalCount);

        if (checkedCount == 0)
        {
            // No rows selected → Empty (Unchecked) ☐
            return false;
        }
        else if (checkedCount == totalCount)
        {
            // All rows selected → Full (Checked) ✓
            return true;
        }
        else
        {
            // Some rows selected → Indeterminate ■
            return null;
        }
    }

    /// <summary>
    /// Handle checkbox header click event
    /// Toggles between Checked (all selected) and Unchecked (none selected)
    /// Skips Indeterminate state on click (Indeterminate is only calculated, not set manually)
    /// </summary>
    /// <param name="header">Checkbox column header</param>
    /// <param name="rows">All rows in the grid</param>
    public void OnHeaderCheckboxClicked(ColumnHeaderViewModel header, IEnumerable<DataGridRowViewModel> rows)
    {
        if (header == null || rows == null)
        {
            _logger?.LogWarning("OnHeaderCheckboxClicked: Null parameters provided");
            return;
        }

        var rowList = rows.ToList();
        var currentState = GetHeaderCheckState(rowList);

        // Cycle logic: Empty → Checked, Checked → Empty, Indeterminate → Checked
        // Simplified: If not all checked → check all, otherwise uncheck all
        var newState = currentState != true;

        _logger?.LogInformation(
            "OnHeaderCheckboxClicked: Changing state from {OldState} to {NewState}",
            StateToString(currentState), newState);

        // Apply new state to all rows
        foreach (var row in rowList)
        {
            row.IsSelected = newState;
        }

        // Update header state
        header.IsCheckboxHeaderChecked = newState;

        _logger?.LogInformation(
            "OnHeaderCheckboxClicked: Updated {RowCount} rows to {NewState}",
            rowList.Count, newState);
    }

    /// <summary>
    /// Update header checkbox state based on current row selection
    /// Should be called after any row selection change
    /// </summary>
    /// <param name="header">Checkbox column header</param>
    /// <param name="rows">All rows in the grid</param>
    public void UpdateHeaderCheckboxState(ColumnHeaderViewModel header, IEnumerable<DataGridRowViewModel> rows)
    {
        if (header == null || rows == null)
        {
            _logger?.LogWarning("UpdateHeaderCheckboxState: Null parameters provided");
            return;
        }

        var newState = GetHeaderCheckState(rows);

        if (header.IsCheckboxHeaderChecked != newState)
        {
            _logger?.LogDebug(
                "UpdateHeaderCheckboxState: Updating header from {OldState} to {NewState}",
                StateToString(header.IsCheckboxHeaderChecked), StateToString(newState));

            header.IsCheckboxHeaderChecked = newState;
        }
    }

    /// <summary>
    /// Helper: Convert nullable bool state to readable string
    /// </summary>
    private string StateToString(bool? state)
    {
        return state switch
        {
            true => "Checked (all selected)",
            false => "Unchecked (none selected)",
            null => "Indeterminate (partial selection)"
        };
    }

    /// <summary>
    /// Check if any rows are selected
    /// </summary>
    public bool HasSelectedRows(IEnumerable<DataGridRowViewModel> rows)
    {
        return rows?.Any(r => r.IsSelected) ?? false;
    }

    /// <summary>
    /// Get count of selected rows
    /// </summary>
    public int GetSelectedRowCount(IEnumerable<DataGridRowViewModel> rows)
    {
        return rows?.Count(r => r.IsSelected) ?? 0;
    }

    /// <summary>
    /// Get percentage of selected rows (0-100)
    /// </summary>
    public double GetSelectionPercentage(IEnumerable<DataGridRowViewModel> rows)
    {
        if (rows == null || !rows.Any())
            return 0;

        var rowList = rows.ToList();
        var checkedCount = rowList.Count(r => r.IsSelected);
        var totalCount = rowList.Count;

        return totalCount > 0 ? (checkedCount * 100.0 / totalCount) : 0;
    }
}

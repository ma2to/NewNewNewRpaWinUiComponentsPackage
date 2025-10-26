using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIControls.Menus;

/// <summary>
/// SENIOR IMPLEMENTATION: Context menu for row operations (Insert Above, Insert Below, Delete)
/// Excel-like behavior for row management via right-click
/// Supports multi-row selection (insert/delete multiple rows at once)
/// </summary>
internal sealed class RowContextMenu
{
    /// <summary>
    /// Fired when user requests to insert rows above selected rows
    /// </summary>
    public event EventHandler<InsertRowsEventArgs>? InsertRowsAboveRequested;

    /// <summary>
    /// Fired when user requests to insert rows below selected rows
    /// </summary>
    public event EventHandler<InsertRowsEventArgs>? InsertRowsBelowRequested;

    /// <summary>
    /// Fired when user requests to delete selected rows
    /// </summary>
    public event EventHandler<DeleteRowsEventArgs>? DeleteRowsRequested;

    /// <summary>
    /// Creates context menu for row operations
    /// Excel-like: Insert Above/Below, Delete
    /// Adapts text based on number of selected rows (e.g., "Insert 3 Rows Above")
    /// </summary>
    /// <param name="selectedRowIndices">Indices of selected rows</param>
    /// <param name="selectedRowIds">IDs of selected rows</param>
    /// <returns>Configured MenuFlyout</returns>
    public MenuFlyout CreateRowContextMenu(
        IReadOnlyList<int> selectedRowIndices,
        IReadOnlyList<string> selectedRowIds)
    {
        var menu = new MenuFlyout();

        if (selectedRowIndices == null || selectedRowIndices.Count == 0)
        {
            // No selection - return empty menu
            return menu;
        }

        // ===== INSERT ABOVE =====
        var insertAboveItem = new MenuFlyoutItem
        {
            Text = selectedRowIndices.Count > 1
                ? $"Insert {selectedRowIndices.Count} Rows Above"
                : "Insert Row Above",
            Icon = new FontIcon { Glyph = "\uE710" } // Add icon
        };
        insertAboveItem.Click += (s, e) =>
        {
            InsertRowsAboveRequested?.Invoke(this, new InsertRowsEventArgs
            {
                ReferenceRowIndex = selectedRowIndices.Min(),
                RowCount = selectedRowIndices.Count,
                SelectedRowIds = selectedRowIds
            });
        };
        menu.Items.Add(insertAboveItem);

        // ===== INSERT BELOW =====
        var insertBelowItem = new MenuFlyoutItem
        {
            Text = selectedRowIndices.Count > 1
                ? $"Insert {selectedRowIndices.Count} Rows Below"
                : "Insert Row Below",
            Icon = new FontIcon { Glyph = "\uE710" } // Add icon
        };
        insertBelowItem.Click += (s, e) =>
        {
            InsertRowsBelowRequested?.Invoke(this, new InsertRowsEventArgs
            {
                ReferenceRowIndex = selectedRowIndices.Max(),
                RowCount = selectedRowIndices.Count,
                SelectedRowIds = selectedRowIds
            });
        };
        menu.Items.Add(insertBelowItem);

        // ===== SEPARATOR =====
        menu.Items.Add(new MenuFlyoutSeparator());

        // ===== DELETE ROWS =====
        var deleteItem = new MenuFlyoutItem
        {
            Text = selectedRowIndices.Count > 1
                ? $"Delete {selectedRowIndices.Count} Rows"
                : "Delete Row",
            Icon = new FontIcon { Glyph = "\uE74D" }, // Delete icon
            Foreground = new SolidColorBrush(Colors.Red) // Warning color for destructive action
        };
        deleteItem.Click += (s, e) =>
        {
            DeleteRowsRequested?.Invoke(this, new DeleteRowsEventArgs
            {
                RowIndices = selectedRowIndices,
                RowIds = selectedRowIds
            });
        };
        menu.Items.Add(deleteItem);

        return menu;
    }
}

/// <summary>
/// Event args for insert rows request
/// Contains reference row index, count, and selected row IDs
/// </summary>
public class InsertRowsEventArgs : EventArgs
{
    /// <summary>
    /// Reference row index for insertion (insert above/below this index)
    /// </summary>
    public int ReferenceRowIndex { get; init; }

    /// <summary>
    /// Number of rows to insert
    /// </summary>
    public int RowCount { get; init; }

    /// <summary>
    /// IDs of selected rows (for context)
    /// </summary>
    public IReadOnlyList<string> SelectedRowIds { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Event args for delete rows request
/// Contains row indices and IDs to delete
/// </summary>
public class DeleteRowsEventArgs : EventArgs
{
    /// <summary>
    /// Indices of rows to delete
    /// </summary>
    public IReadOnlyList<int> RowIndices { get; init; } = Array.Empty<int>();

    /// <summary>
    /// IDs of rows to delete
    /// </summary>
    public IReadOnlyList<string> RowIds { get; init; } = Array.Empty<string>();
}

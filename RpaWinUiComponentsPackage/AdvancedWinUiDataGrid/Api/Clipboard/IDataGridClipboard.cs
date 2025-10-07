using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Clipboard;

/// <summary>
/// Public interface for DataGrid clipboard operations.
/// Provides copy, cut, and paste functionality with format support.
/// </summary>
public interface IDataGridClipboard
{
    /// <summary>
    /// Copies selected cells to clipboard.
    /// </summary>
    /// <param name="includeHeaders">Whether to include column headers</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> CopyAsync(bool includeHeaders = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuts selected cells to clipboard (copy + delete).
    /// </summary>
    /// <param name="includeHeaders">Whether to include column headers</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> CutAsync(bool includeHeaders = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pastes clipboard content to grid.
    /// </summary>
    /// <param name="startRowIndex">Start row index for paste</param>
    /// <param name="startColumnName">Start column name for paste</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with count of pasted cells</returns>
    Task<PublicResult<int>> PasteAsync(int startRowIndex, string startColumnName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if clipboard has compatible data.
    /// </summary>
    /// <returns>True if clipboard has pasteable data</returns>
    bool CanPaste();

    /// <summary>
    /// Gets clipboard content as text.
    /// </summary>
    /// <returns>Clipboard text content</returns>
    Task<string> GetClipboardTextAsync();

    /// <summary>
    /// Sets clipboard content from text.
    /// </summary>
    /// <param name="text">Text to set</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SetClipboardTextAsync(string text, CancellationToken cancellationToken = default);
}

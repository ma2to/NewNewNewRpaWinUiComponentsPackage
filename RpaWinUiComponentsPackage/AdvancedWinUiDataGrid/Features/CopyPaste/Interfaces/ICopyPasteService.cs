using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;

/// <summary>
/// Service interface for copy/paste operations with clipboard management
/// Implements Singleton lifetime per DI_DECISIONS.md - globally shared clipboard semantics
/// THREAD-SAFE: All operations must be thread-safe with immutable snapshots
/// </summary>
internal interface ICopyPasteService
{
    /// <summary>
    /// Copies selected data to clipboard
    /// </summary>
    /// <param name="command">Copy command with selection data</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Copy operation result</returns>
    Task<CopyPasteResult> CopyToClipboardAsync(
        CopyDataCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pastes data from clipboard to grid
    /// CRITICAL: Paste calls AreAllNonEmptyRowsValidAsync after completion
    /// </summary>
    /// <param name="command">Paste command with target location</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Paste operation result</returns>
    Task<CopyPasteResult> PasteFromClipboardAsync(
        PasteDataCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets clipboard content - thread-safe with immutable snapshots
    /// </summary>
    /// <param name="payload">Payload to store in clipboard</param>
    void SetClipboard(object payload);

    /// <summary>
    /// Gets clipboard content - thread-safe with immutable snapshots
    /// </summary>
    /// <returns>Clipboard content or null if empty</returns>
    object? GetClipboard();


    /// <summary>
    /// Estimates clipboard requirements for copy operation
    /// </summary>
    /// <param name="selectedData">Data to copy</param>
    /// <param name="format">Target clipboard format</param>
    /// <returns>Estimated clipboard size and processing time</returns>
    (long EstimatedSize, TimeSpan EstimatedProcessingTime) EstimateClipboardRequirements(
        IEnumerable<IReadOnlyDictionary<string, object?>> selectedData,
        ClipboardFormat format);
}
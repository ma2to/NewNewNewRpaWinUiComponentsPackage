using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;
using System.Collections.Specialized;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Services;

/// <summary>
/// Orchestrates automatic row height adjustments by listening to grid events.
/// ARCHITECTURE: Decouples UI from AutoRowHeightService - no manual calls needed.
/// PROFESSIONAL: Service-based approach following separation of concerns.
/// QUALITY: Only adjusts row heights when AutoRowHeight is enabled (via public API).
/// </summary>
internal sealed class AutoRowHeightOrchestrator : IDisposable
{
    private readonly IAutoRowHeightService _autoRowHeightService;
    private readonly ILogger<AutoRowHeightOrchestrator> _logger;
    private readonly DataGridViewModel _viewModel;
    private bool _disposed;

    public AutoRowHeightOrchestrator(
        IAutoRowHeightService autoRowHeightService,
        DataGridViewModel viewModel,
        ILogger<AutoRowHeightOrchestrator> logger)
    {
        _autoRowHeightService = autoRowHeightService ?? throw new ArgumentNullException(nameof(autoRowHeightService));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // ✅ PROFESSIONAL: Subscribe to Rows.CollectionChanged (separation of concerns)
        // REASON: Rows is ObservableCollection - CollectionChanged fires when rows are added/removed/reset
        _viewModel.Rows.CollectionChanged += OnRowsCollectionChanged;

        _logger.LogInformation("AutoRowHeightOrchestrator initialized and subscribed to Rows.CollectionChanged");
    }

    /// <summary>
    /// Handles Rows collection changes - triggers row height adjustments if enabled.
    /// QUALITY: Checks _autoRowHeightService.IsAutoRowHeightEnabled() before adjusting (respects public API setting).
    /// </summary>
    private async void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // ✅ CRITICAL: Only adjust row heights if AutoRowHeight is ENABLED via public API
        // USER REQUIREMENT: AutoRowHeight má fungovať iba ak je zapnutý
        if (!_autoRowHeightService.IsAutoRowHeightEnabled())
        {
            _logger.LogTrace("AutoRowHeight is DISABLED - skipping row height adjustment");
            return;
        }

        _logger.LogDebug("Rows collection changed (Action={Action}), adjusting heights for affected rows", e.Action);

        try
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    // New rows added - adjust heights for new rows only
                    if (e.NewItems != null)
                    {
                        foreach (DataGridRowViewModel row in e.NewItems)
                        {
                            await AdjustRowHeightSafeAsync(row);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    // Collection reset (full reload) - adjust heights for all visible rows
                    _logger.LogDebug("Rows collection RESET - adjusting heights for all {Count} visible rows", _viewModel.Rows.Count);
                    foreach (var row in _viewModel.Rows)
                    {
                        await AdjustRowHeightSafeAsync(row);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    // Row replaced - adjust height for new row
                    if (e.NewItems != null)
                    {
                        foreach (DataGridRowViewModel row in e.NewItems)
                        {
                            await AdjustRowHeightSafeAsync(row);
                        }
                    }
                    break;

                // Remove and Move actions don't require height adjustment
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting row heights after collection changed");
        }
    }

    /// <summary>
    /// Safely adjusts row height with error handling.
    /// </summary>
    private async Task AdjustRowHeightSafeAsync(DataGridRowViewModel row)
    {
        if (string.IsNullOrEmpty(row.RowId))
        {
            _logger.LogTrace("Skipping row height adjustment - RowId is null/empty for RowIndex={RowIndex}", row.RowIndex);
            return;
        }

        try
        {
            var result = await _autoRowHeightService.AdjustRowHeightAsync(row.RowId, CancellationToken.None);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to adjust row height for rowId {RowId}: {Error}",
                    row.RowId, result.ErrorMessage);
            }
            else
            {
                _logger.LogTrace("Adjusted row height for rowId {RowId}: {Height}px",
                    row.RowId, result.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception adjusting row height for rowId {RowId}", row.RowId);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _viewModel.Rows.CollectionChanged -= OnRowsCollectionChanged;
        _disposed = true;

        _logger.LogInformation("AutoRowHeightOrchestrator disposed");
    }
}

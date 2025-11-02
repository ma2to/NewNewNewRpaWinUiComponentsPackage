using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

/// <summary>
/// Internal handler for automatic sort operations triggered by header clicks.
/// Subscribes to SortRequested event from DataGridViewModel and calls facade sort API.
/// ACTIVE in all operation modes (Interactive, Headless, Readonly) - sorting is always allowed.
///
/// ARCHITECTURE:
/// - ViewModel fires SortRequested event when user clicks header sort menu
/// - This handler receives event and calls IAdvancedDataGridFacade.Sorting.SortByColumnAsync()
/// - SortService performs actual sorting in IRowStore (reorders __rowNumber)
/// - InternalUIUpdateHandler detects row changes and triggers UI refresh automatically
///
/// THREAD SAFETY: Event handlers use async void pattern (fire-and-forget).
/// </summary>
internal sealed class InternalUISortHandler : IDisposable
{
    private readonly ILogger<InternalUISortHandler> _logger;
    private readonly DataGridViewModel _viewModel;
    private readonly IAdvancedDataGridFacade _facade;
    private bool _isDisposed;

    /// <summary>
    /// Creates internal sort handler and subscribes to ViewModel.SortRequested event.
    /// PROFESSIONAL: Automatically handles sort operations without application code intervention.
    /// </summary>
    /// <param name="facade">Facade API for sort operations</param>
    /// <param name="viewModel">ViewModel that fires SortRequested event</param>
    /// <param name="logger">Optional logger for diagnostics and troubleshooting</param>
    public InternalUISortHandler(
        IAdvancedDataGridFacade facade,
        DataGridViewModel viewModel,
        ILogger<InternalUISortHandler>? logger = null)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // ✅ Subscribe to SortRequested event
        _viewModel.SortRequested += OnSortRequested;
        _logger.LogInformation("InternalUISortHandler activated - subscribed to SortRequested event");
    }

    /// <summary>
    /// ✅ PROFESSIONAL FIX: Handles sort requests with multi-sort support (Shift key detection).
    /// Click = Single-sort (SortByColumnAsync - replaces all existing sort criteria)
    /// Shift+Click = Multi-sort (SortByMultipleColumnsAsync - adds/updates column in existing criteria)
    /// Special handling for "None" direction (clear sort = restore original __rowNumber order).
    /// </summary>
    private async void OnSortRequested(object? sender, SortRequestedEventArgs args)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot handle sort request - handler is disposed");
            return;
        }

        try
        {
            _logger.LogInformation("AUTO-SORT: Sort requested for column '{ColumnName}', direction '{SortDirection}', ShiftKey={Shift}",
                args.ColumnName, args.SortDirection, args.IsShiftKeyPressed);

            // ✅ SPECIAL CASE: "None" direction means clear sort (restore original order)
            if (args.SortDirection.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("AUTO-SORT: Clear sort requested - using ClearSortingAsync()");

                // Use dedicated clear sort API instead of sorting by __rowNumber
                var clearResult = await _facade.Sorting.ClearSortingAsync(CancellationToken.None);

                if (clearResult.IsSuccess)
                {
                    _logger.LogInformation("✅ AUTO-SORT: Sort cleared successfully");
                }
                else
                {
                    _logger.LogError("❌ AUTO-SORT: Clear sort failed - {ErrorMessage}", clearResult.ErrorMessage);
                }
                return;
            }

            // ✅ Convert Direction string to PublicSortDirection enum
            var publicDirection = args.SortDirection.Equals("Ascending", StringComparison.OrdinalIgnoreCase)
                ? PublicSortDirection.Ascending
                : PublicSortDirection.Descending;

            // ✅ PROFESSIONAL FIX: Multi-sort vs Single-sort logic
            if (args.IsShiftKeyPressed)
            {
                // MULTI-SORT MODE: Add/update column in existing sort criteria
                _logger.LogInformation("AUTO-SORT (MULTI-SORT): Building multi-column sort criteria");

                // Get current sort descriptors
                var currentDescriptors = _facade.Sorting.GetCurrentSortDescriptors().ToList();

                // Remove column if already exists (will be re-added with new direction)
                currentDescriptors.RemoveAll(d => d.ColumnName.Equals(args.ColumnName, StringComparison.OrdinalIgnoreCase));

                // Add new/updated descriptor
                currentDescriptors.Add(new PublicSortDescriptor
                {
                    ColumnName = args.ColumnName,
                    Direction = publicDirection
                });

                _logger.LogInformation("AUTO-SORT (MULTI-SORT): Sorting by {Count} columns: {Columns}",
                    currentDescriptors.Count,
                    string.Join(", ", currentDescriptors.Select(d => $"{d.ColumnName} {d.Direction}")));

                // Call multi-sort API
                var result = await _facade.Sorting.SortByMultipleColumnsAsync(currentDescriptors, CancellationToken.None);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("✅ AUTO-SORT (MULTI-SORT): Sort completed successfully");

                    // ✅ PROBLEM 1 FIX: Restore column header sort indicators after multi-sort
                    // ReplaceAllRowsAsync invalidates viewport cache → UI refresh clears indicators
                    // Solution: Explicitly restore visual sort state from sort descriptors
                    foreach (var descriptor in currentDescriptors)
                    {
                        var header = _viewModel.ColumnHeaders.FirstOrDefault(h =>
                            h.ColumnName.Equals(descriptor.ColumnName, StringComparison.OrdinalIgnoreCase));
                        if (header != null)
                        {
                            var directionString = descriptor.Direction == PublicSortDirection.Ascending
                                ? "Ascending"
                                : "Descending";
                            header.SortDirection = directionString;
                            _logger.LogDebug("Restored sort indicator for column {ColumnName}: {Direction}",
                                descriptor.ColumnName, directionString);
                        }
                    }
                    _logger.LogInformation("✅ PROBLEM 1 FIX: Restored {Count} column header sort indicators",
                        currentDescriptors.Count);

                    // ✅ PROFESSIONAL FIX: Force UI refresh to ensure sort indicators are visible
                    // REASON: ReplaceAllRowsAsync triggers UI virtualization refresh which may clear indicators
                    // SOLUTION: Yield control to UI thread after setting indicators to ensure binding updates
                    await Task.Delay(1);
                }
                else
                {
                    _logger.LogError("❌ AUTO-SORT (MULTI-SORT): Sort failed - {ErrorMessage}", result.ErrorMessage);
                }
            }
            else
            {
                // SINGLE-SORT MODE: Replace all existing sort criteria with this column
                _logger.LogInformation("AUTO-SORT (SINGLE-SORT): Sorting by single column '{ColumnName}' ({Direction})",
                    args.ColumnName, publicDirection);

                // Call single-column sort API (clears all other sort criteria)
                var result = await _facade.Sorting.SortByColumnAsync(args.ColumnName, publicDirection, CancellationToken.None);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("✅ AUTO-SORT (SINGLE-SORT): Sort completed successfully for column '{ColumnName}'",
                        args.ColumnName);

                    // ✅ PROBLEM 1 FIX: Restore column header sort indicator after single-sort
                    var header = _viewModel.ColumnHeaders.FirstOrDefault(h =>
                        h.ColumnName.Equals(args.ColumnName, StringComparison.OrdinalIgnoreCase));
                    if (header != null)
                    {
                        var directionString = publicDirection == PublicSortDirection.Ascending
                            ? "Ascending"
                            : "Descending";
                        header.SortDirection = directionString;
                        _logger.LogDebug("✅ PROBLEM 1 FIX: Restored sort indicator for column {ColumnName}: {Direction}",
                            args.ColumnName, directionString);

                        // ✅ PROFESSIONAL FIX: Force UI refresh to ensure sort indicator is visible
                        await Task.Delay(1);
                    }
                }
                else
                {
                    _logger.LogError("❌ AUTO-SORT (SINGLE-SORT): Sort failed - {ErrorMessage}", result.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ AUTO-SORT: Exception during sort operation for column '{ColumnName}'", args.ColumnName);
        }
    }

    /// <summary>
    /// Disposes handler and unsubscribes from SortRequested event.
    /// PROFESSIONAL: Clean resource management following IDisposable pattern.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _viewModel.SortRequested -= OnSortRequested;
        _logger.LogInformation("InternalUISortHandler deactivated - unsubscribed from SortRequested event");

        _isDisposed = true;
    }
}

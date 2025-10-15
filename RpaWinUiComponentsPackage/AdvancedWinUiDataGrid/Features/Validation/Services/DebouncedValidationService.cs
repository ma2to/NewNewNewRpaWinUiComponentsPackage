using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Services;

/// <summary>
/// PERFORMANCE OPTIMIZATION: Debounced validation service for rapid operations.
/// Instead of validating synchronously after EVERY delete (blocks UI for 50-100ms),
/// schedules validation to run after a delay (500ms), cancelling previous pending validations.
///
/// Example: User deletes 10 rows rapidly
/// - WITHOUT debounce: 10 validations × 100ms = 1000ms blocking time
/// - WITH debounce: 1 validation after 500ms = 100ms total (10x faster)
/// </summary>
internal sealed class DebouncedValidationService : IDisposable
{
    private readonly ILogger<DebouncedValidationService> _logger;
    private readonly IValidationService _validationService;
    private readonly UiNotificationService? _uiNotificationService;
    private readonly SemaphoreSlim _validationLock = new(1, 1);
    private Timer? _debounceTimer;
    private bool _disposed;
    private int _pendingValidationCount;

    public DebouncedValidationService(
        IValidationService validationService,
        ILogger<DebouncedValidationService> logger,
        UiNotificationService? uiNotificationService = null)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uiNotificationService = uiNotificationService;
    }

    /// <summary>
    /// Schedules validation to run after specified delay.
    /// Cancels any previously scheduled validation (debounce).
    /// Non-blocking - returns immediately.
    /// </summary>
    /// <param name="operationName">Name of operation that triggered validation</param>
    /// <param name="delayMs">Delay in milliseconds before validation runs (default 500ms)</param>
    public void ScheduleValidation(string operationName, int delayMs = 500)
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot schedule validation - service is disposed");
            return;
        }

        // Cancel previous timer (debounce)
        _debounceTimer?.Dispose();

        // Increment pending count
        Interlocked.Increment(ref _pendingValidationCount);

        _logger.LogDebug("Validation scheduled for {OperationName} with {Delay}ms debounce (pending count: {Count})",
            operationName, delayMs, _pendingValidationCount);

        // Schedule new validation after delay
        _debounceTimer = new Timer(
            async _ => await ExecuteValidationAsync(operationName),
            null,
            delayMs,
            Timeout.Infinite);
    }

    /// <summary>
    /// Executes validation asynchronously in background.
    /// Thread-safe with semaphore to prevent concurrent validations.
    /// </summary>
    private async Task ExecuteValidationAsync(string operationName)
    {
        if (_disposed)
            return;

        // Try to acquire validation lock (non-blocking)
        if (!await _validationLock.WaitAsync(0))
        {
            _logger.LogDebug("Validation already running - skipping duplicate for {OperationName}", operationName);
            return;
        }

        try
        {
            _logger.LogInformation("Starting debounced validation for {OperationName} (processed {Count} pending validations)",
                operationName, _pendingValidationCount);

            // Reset pending count
            Interlocked.Exchange(ref _pendingValidationCount, 0);

            // Run validation in background (don't block caller)
            var validationResult = await _validationService.AreAllNonEmptyRowsValidAsync(
                onlyFiltered: false,
                onlyChecked: false,
                cancellationToken: CancellationToken.None);

            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("Debounced validation found issues for {OperationName}: {Error}",
                    operationName, validationResult.ErrorMessage);
            }
            else
            {
                _logger.LogInformation("Debounced validation successful for {OperationName}", operationName);
            }

            // Notify UI that validation completed (if UI notification service available)
            if (_uiNotificationService != null && validationResult.IsSuccess)
            {
                var errorCount = validationResult.ErrorMessage?.Contains("error") == true ? 1 : 0;
                _uiNotificationService.NotifyValidationResultsRefresh(errorCount, !validationResult.IsSuccess);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debounced validation failed for {OperationName}", operationName);
        }
        finally
        {
            _validationLock.Release();
        }
    }

    /// <summary>
    /// Cancels any pending validation.
    /// </summary>
    public void CancelPendingValidation()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        Interlocked.Exchange(ref _pendingValidationCount, 0);
        _logger.LogDebug("Cancelled pending validation");
    }

    /// <summary>
    /// Executes validation immediately (bypasses debounce).
    /// Blocks until validation completes.
    /// </summary>
    public async Task<Result<bool>> ValidateNowAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DebouncedValidationService));

        // Cancel pending debounced validation
        CancelPendingValidation();

        await _validationLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Executing immediate validation (bypass debounce)");

            return await _validationService.AreAllNonEmptyRowsValidAsync(
                onlyFiltered: false,
                onlyChecked: false,
                cancellationToken);
        }
        finally
        {
            _validationLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _debounceTimer?.Dispose();
        _validationLock?.Dispose();

        _logger.LogDebug("DebouncedValidationService disposed");
    }
}

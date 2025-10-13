using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Partial class containing Component Initialization & Lifecycle
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Component Initialization & Lifecycle

    /// <summary>
    /// Initialize for UI mode with default configuration
    /// CONVENIENCE: Simplified UI initialization
    /// </summary>
    public async Task<PublicInitializationResult> InitializeForUIAsync(
        PublicInitializationConfiguration? config = null,
        IProgress<PublicInitializationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var lifecycleManager = scope.ServiceProvider.GetRequiredService<IComponentLifecycleManager>();

            // Konvertujeme public config na internal
            var internalConfig = config?.ToInternal() ?? new Configuration.InitializationConfiguration();

            // Vytvoríme progress wrapper
            var internalProgress = InitializationMappings.CreateProgressWrapper(progress);

            // Vytvoríme internal command
            var command = InitializeComponentCommand.ForUI(internalConfig, internalProgress, cancellationToken);

            // Vykonáme inicializáciu
            var internalResult = await lifecycleManager.InitializeAsync(command, cancellationToken);

            // Konvertujeme result na public
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Component initialization (UI mode) failed: {Message}", ex.Message);
            return new PublicInitializationResult
            {
                IsSuccess = false,
                Message = "Initialization failed",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Initialize for headless mode with server optimizations
    /// PERFORMANCE: Optimized for server/background scenarios
    /// </summary>
    public async Task<PublicInitializationResult> InitializeForHeadlessAsync(
        PublicInitializationConfiguration? config = null,
        IProgress<PublicInitializationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var lifecycleManager = scope.ServiceProvider.GetRequiredService<IComponentLifecycleManager>();

            // Konvertujeme public config na internal
            var internalConfig = config?.ToInternal() ?? new Configuration.InitializationConfiguration();

            // Vytvoríme progress wrapper
            var internalProgress = InitializationMappings.CreateProgressWrapper(progress);

            // Vytvoríme internal command
            var command = InitializeComponentCommand.ForHeadless(internalConfig, internalProgress, cancellationToken);

            // Vykonáme inicializáciu
            var internalResult = await lifecycleManager.InitializeAsync(command, cancellationToken);

            // Konvertujeme result na public
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Component initialization (headless mode) failed: {Message}", ex.Message);
            return new PublicInitializationResult
            {
                IsSuccess = false,
                Message = "Initialization failed",
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Graceful component shutdown with cleanup
    /// LIFECYCLE: Proper resource cleanup and disposal
    /// </summary>
    public async Task<bool> ShutdownAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var lifecycleManager = scope.ServiceProvider.GetRequiredService<IComponentLifecycleManager>();

            return await lifecycleManager.ShutdownAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Component shutdown failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Get current initialization status
    /// MONITORING: Runtime initialization state inspection
    /// </summary>
    public PublicInitializationStatus GetInitializationStatus()
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var lifecycleManager = scope.ServiceProvider.GetRequiredService<IComponentLifecycleManager>();

            var internalStatus = lifecycleManager.GetStatus();
            return internalStatus.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get initialization status: {Message}", ex.Message);
            return new PublicInitializationStatus { IsInitialized = false, LastError = ex.Message };
        }
    }

    #endregion
}


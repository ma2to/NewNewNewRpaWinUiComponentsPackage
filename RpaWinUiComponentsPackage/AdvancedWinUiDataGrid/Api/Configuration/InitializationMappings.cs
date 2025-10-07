using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Configuration;

/// <summary>
/// Mapping extensions medzi public a internal Initialization models
/// </summary>
internal static class InitializationMappings
{
    // PUBLIC -> INTERNAL

    /// <summary>
    /// Konvertuje public InitializationConfiguration na internal
    /// </summary>
    internal static InitializationConfiguration ToInternal(this PublicInitializationConfiguration publicConfig)
    {
        return new InitializationConfiguration
        {
            EnableSmartOperations = publicConfig.EnableSmartOperations,
            EnableAdvancedValidation = publicConfig.EnableAdvancedValidation,
            EnablePerformanceOptimizations = publicConfig.EnablePerformanceOptimizations,
            InitializationTimeout = publicConfig.InitializationTimeout
        };
    }

    // INTERNAL -> PUBLIC

    /// <summary>
    /// Konvertuje internal InitializationResult na public
    /// </summary>
    internal static PublicInitializationResult ToPublic(this InitializationResult internalResult)
    {
        return new PublicInitializationResult
        {
            IsSuccess = internalResult.IsSuccess,
            Message = internalResult.Message,
            ErrorMessage = internalResult.ErrorMessage,
            Duration = internalResult.Duration
        };
    }

    /// <summary>
    /// Konvertuje internal InitializationProgress na public
    /// </summary>
    internal static PublicInitializationProgress ToPublic(this InitializationProgress internalProgress)
    {
        return new PublicInitializationProgress
        {
            CompletedSteps = internalProgress.CompletedSteps,
            TotalSteps = internalProgress.TotalSteps,
            CompletionPercentage = internalProgress.CompletionPercentage,
            ElapsedTime = internalProgress.ElapsedTime,
            CurrentOperation = internalProgress.CurrentOperation,
            IsHeadlessMode = internalProgress.IsHeadlessMode,
            EstimatedTimeRemaining = internalProgress.EstimatedTimeRemaining
        };
    }

    /// <summary>
    /// Konvertuje internal InitializationStatus na public
    /// </summary>
    internal static PublicInitializationStatus ToPublic(this InitializationStatus internalStatus)
    {
        return new PublicInitializationStatus
        {
            IsInitialized = internalStatus.IsInitialized,
            IsHeadlessMode = internalStatus.IsHeadlessMode,
            InitializationStartTime = internalStatus.InitializationStartTime,
            InitializationCompletedTime = internalStatus.InitializationCompletedTime,
            InitializationDuration = internalStatus.InitializationDuration,
            LastError = internalStatus.LastError
        };
    }

    /// <summary>
    /// Vytvorí progress wrapper pre mapovanie internal -> public progress events
    /// </summary>
    internal static IProgress<InitializationProgress> CreateProgressWrapper(
        IProgress<PublicInitializationProgress>? publicProgress)
    {
        if (publicProgress == null)
            return new NoOpProgress();

        return new ProgressWrapper(publicProgress);
    }

    private sealed class ProgressWrapper : IProgress<InitializationProgress>
    {
        private readonly IProgress<PublicInitializationProgress> _publicProgress;

        public ProgressWrapper(IProgress<PublicInitializationProgress> publicProgress)
        {
            _publicProgress = publicProgress;
        }

        public void Report(InitializationProgress value)
        {
            _publicProgress.Report(value.ToPublic());
        }
    }

    private sealed class NoOpProgress : IProgress<InitializationProgress>
    {
        public void Report(InitializationProgress value)
        {
            // No-op
        }
    }
}

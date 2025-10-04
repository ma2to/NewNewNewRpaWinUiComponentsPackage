using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Progress;

/// <summary>
/// Internal implementation of progress tracking hub
/// Thread-safe operation progress monitoring with comprehensive logging
/// </summary>
internal sealed class ProgressHub : IProgressHub, IDisposable
{
    private readonly ILogger<ProgressHub> _logger;
    private readonly ConcurrentDictionary<Guid, OperationProgress> _activeOperations;
    private readonly ConcurrentDictionary<Guid, bool> _cancelledOperations;
    private volatile bool _isDisposed;

    public ProgressHub(ILogger<ProgressHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activeOperations = new ConcurrentDictionary<Guid, OperationProgress>();
        _cancelledOperations = new ConcurrentDictionary<Guid, bool>();

        _logger.LogDebug("ProgressHub initialized");
    }

    public void StartOperation(Guid operationId, string operationName, int totalSteps)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot start operation {OperationId} - ProgressHub is disposed", operationId);
            return;
        }

        if (string.IsNullOrWhiteSpace(operationName))
            throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));

        if (totalSteps < 0)
            throw new ArgumentException("Total steps cannot be negative", nameof(totalSteps));

        var progress = new OperationProgress
        {
            OperationId = operationId,
            OperationName = operationName,
            TotalSteps = totalSteps,
            CurrentStep = 0,
            StartTime = DateTime.UtcNow,
            IsCompleted = false,
            IsSuccessful = false,
            IsCancelled = false
        };

        _activeOperations.AddOrUpdate(operationId, progress, (_, _) => progress);

        _logger.LogInformation("Operation started: {OperationName} [{OperationId}] with {TotalSteps} steps",
            operationName, operationId, totalSteps);
    }

    public void UpdateProgress(Guid operationId, int currentStep, string? message = null)
    {
        if (_isDisposed || !_activeOperations.TryGetValue(operationId, out var existingProgress))
        {
            _logger.LogWarning("Cannot update progress for operation {OperationId} - operation not found or hub disposed", operationId);
            return;
        }

        if (currentStep < 0)
        {
            _logger.LogWarning("Invalid current step {CurrentStep} for operation {OperationId}", currentStep, operationId);
            return;
        }

        var updatedProgress = existingProgress with
        {
            CurrentStep = Math.Min(currentStep, existingProgress.TotalSteps),
            CurrentMessage = message
        };

        _activeOperations.TryUpdate(operationId, updatedProgress, existingProgress);

        _logger.LogDebug("Progress updated: {OperationName} [{OperationId}] - {CurrentStep}/{TotalSteps} ({Percentage:F1}%) {Message}",
            updatedProgress.OperationName, operationId, updatedProgress.CurrentStep, updatedProgress.TotalSteps,
            updatedProgress.ProgressPercentage, message ?? "");
    }

    public void CompleteOperation(Guid operationId, bool success, string? message = null)
    {
        if (_isDisposed || !_activeOperations.TryGetValue(operationId, out var existingProgress))
        {
            _logger.LogWarning("Cannot complete operation {OperationId} - operation not found or hub disposed", operationId);
            return;
        }

        var completedProgress = existingProgress with
        {
            CurrentStep = existingProgress.TotalSteps,
            EndTime = DateTime.UtcNow,
            IsCompleted = true,
            IsSuccessful = success,
            CurrentMessage = message
        };

        _activeOperations.TryUpdate(operationId, completedProgress, existingProgress);

        var duration = completedProgress.EndTime?.Subtract(completedProgress.StartTime) ?? TimeSpan.Zero;

        _logger.LogInformation("Operation completed: {OperationName} [{OperationId}] - Success: {Success}, Duration: {Duration}ms {Message}",
            completedProgress.OperationName, operationId, success, duration.TotalMilliseconds, message ?? "");

        // Remove completed operation after a short delay to allow final status checks
        _ = Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(_ =>
        {
            _activeOperations.TryRemove(operationId, out var _);
            _cancelledOperations.TryRemove(operationId, out var _);
        });
    }

    public OperationProgress? GetProgress(Guid operationId)
    {
        return _activeOperations.TryGetValue(operationId, out var progress) ? progress : null;
    }

    public IReadOnlyList<OperationProgress> GetActiveOperations()
    {
        return _activeOperations.Values
            .Where(op => !op.IsCompleted)
            .OrderBy(op => op.StartTime)
            .ToList();
    }

    public void CancelOperation(Guid operationId)
    {
        if (_isDisposed)
        {
            _logger.LogWarning("Cannot cancel operation {OperationId} - ProgressHub is disposed", operationId);
            return;
        }

        _cancelledOperations.TryAdd(operationId, true);

        if (_activeOperations.TryGetValue(operationId, out var existingProgress))
        {
            var cancelledProgress = existingProgress with
            {
                IsCancelled = true,
                IsCompleted = true,
                IsSuccessful = false,
                EndTime = DateTime.UtcNow,
                CurrentMessage = "Operation cancelled"
            };

            _activeOperations.TryUpdate(operationId, cancelledProgress, existingProgress);

            _logger.LogInformation("Operation cancelled: {OperationName} [{OperationId}]",
                existingProgress.OperationName, operationId);
        }
    }

    public bool IsOperationCancelled(Guid operationId)
    {
        return _cancelledOperations.ContainsKey(operationId);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Cancel all active operations
        foreach (var operationId in _activeOperations.Keys.ToList())
        {
            CancelOperation(operationId);
        }

        _activeOperations.Clear();
        _cancelledOperations.Clear();

        _logger.LogDebug("ProgressHub disposed");
    }
}
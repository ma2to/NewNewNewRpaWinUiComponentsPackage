using Microsoft.Extensions.Logging;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Progress;

/// <summary>
/// Progress tracking and reporting hub for long-running operations
/// Provides centralized progress monitoring across all features
/// </summary>
internal interface IProgressHub
{
    /// <summary>
    /// Start tracking a new operation
    /// </summary>
    /// <param name="operationId">Unique operation identifier</param>
    /// <param name="operationName">Human-readable operation name</param>
    /// <param name="totalSteps">Total number of steps in the operation</param>
    void StartOperation(Guid operationId, string operationName, int totalSteps);

    /// <summary>
    /// Update progress for an operation
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="currentStep">Current step number</param>
    /// <param name="message">Optional progress message</param>
    void UpdateProgress(Guid operationId, int currentStep, string? message = null);

    /// <summary>
    /// Complete an operation
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <param name="success">Whether operation completed successfully</param>
    /// <param name="message">Optional completion message</param>
    void CompleteOperation(Guid operationId, bool success, string? message = null);

    /// <summary>
    /// Get current progress for an operation
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <returns>Progress information or null if operation not found</returns>
    OperationProgress? GetProgress(Guid operationId);

    /// <summary>
    /// Get all active operations
    /// </summary>
    /// <returns>List of active operation progress</returns>
    IReadOnlyList<OperationProgress> GetActiveOperations();

    /// <summary>
    /// Cancel an operation
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    void CancelOperation(Guid operationId);

    /// <summary>
    /// Check if an operation is cancelled
    /// </summary>
    /// <param name="operationId">Operation identifier</param>
    /// <returns>True if operation is cancelled</returns>
    bool IsOperationCancelled(Guid operationId);
}

/// <summary>
/// Represents the progress of a long-running operation
/// </summary>
internal sealed record OperationProgress
{
    public Guid OperationId { get; init; }
    public string OperationName { get; init; } = string.Empty;
    public int TotalSteps { get; init; }
    public int CurrentStep { get; init; }
    public string? CurrentMessage { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public bool IsCompleted { get; init; }
    public bool IsSuccessful { get; init; }
    public bool IsCancelled { get; init; }
    public double ProgressPercentage => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;
}
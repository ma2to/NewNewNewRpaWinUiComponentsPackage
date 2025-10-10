using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

/// <summary>
/// Operation scope with automatic timing and disposal tracking
/// Implements RAII pattern for automatic operation time measurement
/// </summary>
internal interface IOperationScope : IDisposable
{
    /// <summary>
    /// Operation name
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Operation start time
    /// </summary>
    DateTime StartTime { get; }

    /// <summary>
    /// Elapsed time since operation start
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Indicator whether operation was completed (success/failure/warning)
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Correlation ID for tracking operation across the system
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Marks operation as successful with optional result
    /// </summary>
    void MarkSuccess(object? result = null);

    /// <summary>
    /// Marks operation as failed with exception
    /// </summary>
    void MarkFailure(Exception exception);

    /// <summary>
    /// Adds warning to operation (operation can continue but with warning)
    /// </summary>
    void MarkWarning(string warning);

    /// <summary>
    /// Updates operation context (adds additional information)
    /// </summary>
    void UpdateContext(object additionalContext);

    /// <summary>
    /// Sets operation result without completing it
    /// </summary>
    void SetResult(object result);
}

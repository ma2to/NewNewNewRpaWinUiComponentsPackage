using System;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

/// <summary>
/// Null implementation of IOperationScope - používa sa keď logging nie je dostupný
/// Zero-overhead implementation pre scenáre bez loggingu
/// </summary>
internal sealed class NullOperationScope : IOperationScope
{
    /// <summary>
    /// Singleton instance pre minimálne alokácie
    /// </summary>
    public static readonly NullOperationScope Instance = new();

    // Súkromný konštruktor pre singleton pattern
    private NullOperationScope() { }

    public string OperationName => string.Empty;
    public DateTime StartTime => DateTime.MinValue;
    public TimeSpan Elapsed => TimeSpan.Zero;
    public bool IsCompleted => true;
    public string? CorrelationId => null;

    // Všetky metódy sú no-op (nič nerobia)
    public void MarkSuccess(object? result = null) { }
    public void MarkFailure(Exception exception) { }
    public void MarkWarning(string warning) { }
    public void UpdateContext(object additionalContext) { }
    public void SetResult(object result) { }
    public void Dispose() { }
}

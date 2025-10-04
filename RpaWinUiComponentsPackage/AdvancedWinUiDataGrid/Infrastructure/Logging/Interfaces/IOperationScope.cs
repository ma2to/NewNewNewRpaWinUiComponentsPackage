using System;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;

/// <summary>
/// Operation scope with automatic timing and disposal tracking
/// Implementuje RAII pattern pre automatické meranie času operácií
/// </summary>
internal interface IOperationScope : IDisposable
{
    /// <summary>
    /// Názov operácie
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Čas začiatku operácie
    /// </summary>
    DateTime StartTime { get; }

    /// <summary>
    /// Uplynulý čas od začiatku operácie
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Indikátor či operácia bola dokončená (success/failure/warning)
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Correlation ID pre tracking operácie naprieč systémom
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Označí operáciu ako úspešnú s voliteľným výsledkom
    /// </summary>
    void MarkSuccess(object? result = null);

    /// <summary>
    /// Označí operáciu ako neúspešnú s exception
    /// </summary>
    void MarkFailure(Exception exception);

    /// <summary>
    /// Pridá warning k operácii (operácia môže pokračovať ale s upozornením)
    /// </summary>
    void MarkWarning(string warning);

    /// <summary>
    /// Aktualizuje context operácie (pridá ďalšie informácie)
    /// </summary>
    void UpdateContext(object additionalContext);

    /// <summary>
    /// Nastaví výsledok operácie bez jej ukončenia
    /// </summary>
    void SetResult(object result);
}

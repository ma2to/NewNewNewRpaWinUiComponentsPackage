namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;

/// <summary>
/// Výsledok inicializácie komponentu
/// Immutable result object s success/failure stavom
/// </summary>
internal sealed record InitializationResult
{
    /// <summary>Indikátor úspešnej inicializácie</summary>
    internal bool IsSuccess { get; init; }

    /// <summary>Správa o výsledku inicializácie</summary>
    internal string Message { get; init; } = string.Empty;

    /// <summary>Chybová správa (ak inicializácia zlyhala)</summary>
    internal string? ErrorMessage { get; init; }

    /// <summary>Trvanie inicializácie</summary>
    internal TimeSpan? Duration { get; init; }

    /// <summary>Exception ktorá spôsobila zlyhanie (ak existuje)</summary>
    internal Exception? Exception { get; init; }

    /// <summary>
    /// Factory metóda pre úspešnú inicializáciu
    /// </summary>
    internal static InitializationResult Success(string message, TimeSpan? duration = null) =>
        new()
        {
            IsSuccess = true,
            Message = message,
            Duration = duration
        };

    /// <summary>
    /// Factory metóda pre zlyhanie inicializácie
    /// </summary>
    internal static InitializationResult Failure(string errorMessage, Exception? exception = null) =>
        new()
        {
            IsSuccess = false,
            Message = "Initialization failed",
            ErrorMessage = errorMessage,
            Exception = exception
        };

    /// <summary>
    /// Factory metóda pre už inicializovaný komponent
    /// </summary>
    internal static InitializationResult AlreadyInitialized() =>
        new()
        {
            IsSuccess = true,
            Message = "Component is already initialized"
        };
}

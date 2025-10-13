namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Configuration for smart row management operations (add/delete with automatic minimum rows).
/// </summary>
public sealed class PublicSmartOperationsConfig
{
    /// <summary>
    /// Minimum number of rows to maintain in the grid (default: 1).
    /// SmartDelete will clear content instead of removing rows when at minimum.
    /// </summary>
    public int MinimumRows { get; init; } = 1;

    /// <summary>
    /// Enable automatic expansion of empty rows (default: true).
    /// When enabled, an empty row is automatically added when the last row is filled.
    /// </summary>
    public bool EnableAutoExpand { get; init; } = true;

    /// <summary>
    /// Enable smart delete logic (default: true).
    /// When enabled, rows are cleared instead of removed when at minimum rows.
    /// </summary>
    public bool EnableSmartDelete { get; init; } = true;

    /// <summary>
    /// Always keep the last row empty (default: true).
    /// Ensures there's always an empty row at the end for data entry.
    /// </summary>
    public bool AlwaysKeepLastEmpty { get; init; } = true;

    /// <summary>
    /// Creates default smart operations configuration.
    /// </summary>
    public static PublicSmartOperationsConfig Default => new();

    /// <summary>
    /// Creates custom smart operations configuration.
    /// </summary>
    public static PublicSmartOperationsConfig Create(
        int minimumRows = 1,
        bool enableAutoExpand = true,
        bool enableSmartDelete = true,
        bool alwaysKeepLastEmpty = true) =>
        new()
        {
            MinimumRows = minimumRows,
            EnableAutoExpand = enableAutoExpand,
            EnableSmartDelete = enableSmartDelete,
            AlwaysKeepLastEmpty = alwaysKeepLastEmpty
        };
}

/// <summary>
/// Result of smart row management operations with statistics.
/// </summary>
public sealed class PublicSmartOperationResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Final row count after operation.
    /// </summary>
    public int FinalRowCount { get; init; }

    /// <summary>
    /// Number of rows processed (added/deleted/cleared).
    /// </summary>
    public int ProcessedRows { get; init; }

    /// <summary>
    /// Statistics about the operation.
    /// </summary>
    public PublicSmartOperationStatistics Statistics { get; init; } = new();

    /// <summary>
    /// Operation duration.
    /// </summary>
    public TimeSpan OperationTime { get; init; }

    /// <summary>
    /// Additional messages about the operation.
    /// </summary>
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    internal static PublicSmartOperationResult Success(
        int finalRowCount,
        int processedRows,
        TimeSpan operationTime,
        PublicSmartOperationStatistics statistics) =>
        new()
        {
            IsSuccess = true,
            FinalRowCount = finalRowCount,
            ProcessedRows = processedRows,
            OperationTime = operationTime,
            Statistics = statistics
        };

    internal static PublicSmartOperationResult Failure(
        string errorMessage,
        TimeSpan operationTime,
        IReadOnlyList<string>? messages = null) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            OperationTime = operationTime,
            Messages = messages ?? Array.Empty<string>()
        };
}

/// <summary>
/// Statistics from smart row management operations.
/// </summary>
public sealed class PublicSmartOperationStatistics
{
    /// <summary>
    /// Number of empty rows created.
    /// </summary>
    public int EmptyRowsCreated { get; init; }

    /// <summary>
    /// Number of rows physically deleted (removed from grid).
    /// </summary>
    public int RowsPhysicallyDeleted { get; init; }

    /// <summary>
    /// Number of rows where content was cleared but row kept.
    /// </summary>
    public int RowsContentCleared { get; init; }

    /// <summary>
    /// Number of rows shifted during operation.
    /// </summary>
    public int RowsShifted { get; init; }

    /// <summary>
    /// Whether minimum rows constraint was enforced.
    /// </summary>
    public bool MinimumRowsEnforced { get; init; }

    /// <summary>
    /// Whether last empty row was maintained.
    /// </summary>
    public bool LastEmptyRowMaintained { get; init; }
}

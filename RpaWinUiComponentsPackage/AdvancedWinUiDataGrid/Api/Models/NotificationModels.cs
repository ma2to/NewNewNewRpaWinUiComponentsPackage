namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public event args for validation refresh notifications
/// </summary>
public sealed class PublicValidationRefreshEventArgs
{
    /// <summary>
    /// Total number of validation results
    /// </summary>
    public int TotalErrors { get; init; }

    /// <summary>
    /// Number of errors found
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Number of warnings found
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Whether there are any errors
    /// </summary>
    public bool HasErrors { get; init; }

    /// <summary>
    /// Time when refresh occurred
    /// </summary>
    public DateTime RefreshTime { get; init; }
}

/// <summary>
/// Public event args for data refresh notifications
/// </summary>
public sealed class PublicDataRefreshEventArgs
{
    /// <summary>
    /// Number of affected rows
    /// </summary>
    public int AffectedRows { get; init; }

    /// <summary>
    /// Number of columns (if applicable)
    /// </summary>
    public int ColumnCount { get; init; }

    /// <summary>
    /// Type of operation that triggered refresh
    /// </summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>
    /// Time when refresh occurred
    /// </summary>
    public DateTime RefreshTime { get; init; }
}

/// <summary>
/// Public event args for operation progress notifications
/// </summary>
public sealed class PublicOperationProgressEventArgs
{
    /// <summary>
    /// Name of the operation
    /// </summary>
    public string OperationName { get; init; } = string.Empty;

    /// <summary>
    /// Number of processed items
    /// </summary>
    public int ProcessedItems { get; init; }

    /// <summary>
    /// Total number of items to process
    /// </summary>
    public int TotalItems { get; init; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public double ProgressPercentage { get; init; }

    /// <summary>
    /// Optional progress message
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Elapsed time since operation started
    /// </summary>
    public TimeSpan ElapsedTime { get; init; }
}

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

    #region Granular Update Metadata (for 10M+ row performance optimization)

    /// <summary>
    /// Indices of rows that were physically deleted from the grid.
    /// Used by InternalUIUpdateHandler for granular RemoveAt() operations instead of full rebuild.
    /// Empty for Scenario A (content clear only) operations.
    /// </summary>
    public IReadOnlyList<int> PhysicallyDeletedIndices { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Indices of rows where content was cleared but row structure remains.
    /// Used by InternalUIUpdateHandler for granular cell value updates instead of full rebuild.
    /// Empty for Scenario B (physical delete) operations.
    /// </summary>
    public IReadOnlyList<int> ContentClearedIndices { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Dictionary of row indices to updated row data (e.g., shifted rows after delete).
    /// Key: Row index, Value: New row data for that index.
    /// Used by InternalUIUpdateHandler for granular cell value updates instead of full rebuild.
    /// </summary>
    public IReadOnlyDictionary<int, IReadOnlyDictionary<string, object?>> UpdatedRowData { get; init; }
        = new Dictionary<int, IReadOnlyDictionary<string, object?>>();

    #endregion
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

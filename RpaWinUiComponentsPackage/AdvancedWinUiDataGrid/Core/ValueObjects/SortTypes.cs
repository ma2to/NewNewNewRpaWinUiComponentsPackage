namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

#region Enums

/// <summary>
/// Sort direction for sorting operations.
/// </summary>
internal enum SortDirection
{
    None = 0,
    Ascending = 1,
    Descending = 2
}

/// <summary>
/// Performance modes for sort operations.
/// </summary>
internal enum SortPerformanceMode
{
    Auto,           // Automatic selection based on data size
    Sequential,     // Single-threaded sorting
    Parallel,       // Multi-threaded sorting
    Optimized       // Advanced optimizations with caching
}

/// <summary>
/// Sort stability mode.
/// </summary>
internal enum SortStability
{
    Stable,         // Preserves relative order of equal elements
    Unstable        // Allows reordering of equal elements for performance
}

#endregion

#region Progress & Context Types

/// <summary>
/// Sort operation progress reporting.
/// Consistent structure with ValidationProgress and ExportProgress.
/// </summary>
internal sealed record SortProgress
{
    internal int ProcessedRows { get; init; }
    internal int TotalRows { get; init; }
    internal double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    internal TimeSpan ElapsedTime { get; init; }
    internal string CurrentOperation { get; init; } = string.Empty;
    internal string? CurrentColumn { get; init; }
    internal SortDirection CurrentDirection { get; init; } = SortDirection.None;

    /// <summary>Estimated time remaining based on current progress rate.</summary>
    internal TimeSpan? EstimatedTimeRemaining => ProcessedRows > 0 && TotalRows > ProcessedRows
        ? TimeSpan.FromTicks(ElapsedTime.Ticks * (TotalRows - ProcessedRows) / ProcessedRows)
        : null;

    public SortProgress() : this(0, 0, TimeSpan.Zero, "", null, SortDirection.None) { }

    public SortProgress(int processedRows, int totalRows, TimeSpan elapsedTime, string currentOperation, string? currentColumn, SortDirection currentDirection)
    {
        ProcessedRows = processedRows;
        TotalRows = totalRows;
        ElapsedTime = elapsedTime;
        CurrentOperation = currentOperation;
        CurrentColumn = currentColumn;
        CurrentDirection = currentDirection;
    }
}

/// <summary>
/// Sort execution context with dependency injection support.
/// Hybrid DI: Provides services for custom sort functions.
/// </summary>
internal sealed record SortContext
{
    internal IServiceProvider? ServiceProvider { get; init; }
    internal IReadOnlyDictionary<string, object?> SortParameters { get; init; } = new Dictionary<string, object?>();
    internal CancellationToken CancellationToken { get; init; } = default;
    internal DateTime ExecutionTime { get; init; } = DateTime.UtcNow;
    internal SortPerformanceMode PerformanceMode { get; init; } = SortPerformanceMode.Auto;
}

#endregion

#region Configuration

/// <summary>
/// Configuration for sorting a single column.
/// Immutable record with factory methods.
/// </summary>
internal sealed record SortColumnConfiguration
{
    internal string ColumnName { get; init; } = string.Empty;
    internal SortDirection Direction { get; init; } = SortDirection.Ascending;
    internal int Priority { get; init; } = 0;
    internal bool IsPrimary { get; init; } = false;
    internal bool IsEnabled { get; init; } = true;
    internal bool CaseSensitive { get; init; } = false;
    internal bool NullsFirst { get; init; } = false;
    internal Func<object?, object?, int>? CustomComparer { get; init; }

    /// <summary>
    /// Creates a basic sort configuration.
    /// </summary>
    internal static SortColumnConfiguration Create(
        string columnName,
        SortDirection direction,
        int priority = 0) =>
        new()
        {
            ColumnName = columnName,
            Direction = direction,
            Priority = priority,
            IsPrimary = priority == 0
        };

    /// <summary>
    /// Creates a sort configuration with custom comparer function.
    /// </summary>
    internal static SortColumnConfiguration WithCustomComparer(
        string columnName,
        SortDirection direction,
        Func<object?, object?, int> comparer,
        int priority = 0) =>
        new()
        {
            ColumnName = columnName,
            Direction = direction,
            Priority = priority,
            IsPrimary = priority == 0,
            CustomComparer = comparer
        };
}

/// <summary>
/// Advanced sort configuration with business rule support.
/// Supports multi-column sorting with performance optimization.
/// </summary>
internal sealed record AdvancedSortConfiguration
{
    internal string ConfigurationName { get; init; } = string.Empty;
    internal IReadOnlyList<SortColumnConfiguration> SortColumns { get; init; } = Array.Empty<SortColumnConfiguration>();
    internal SortPerformanceMode PerformanceMode { get; init; } = SortPerformanceMode.Auto;
    internal SortStability Stability { get; init; } = SortStability.Stable;
    internal bool AllowMultiColumnSort { get; init; } = true;
    internal int MaxSortColumns { get; init; } = 5;
    internal TimeSpan? ExecutionTimeout { get; init; }
    internal bool EnableParallelProcessing { get; init; } = true;
    internal Func<IReadOnlyDictionary<string, object?>, SortContext, object?>? CustomSortKey { get; init; }

    /// <summary>
    /// Creates employee hierarchy sort configuration (Department > Position > Salary).
    /// </summary>
    internal static AdvancedSortConfiguration CreateEmployeeHierarchy(
        string departmentColumn = "Department",
        string positionColumn = "Position",
        string salaryColumn = "Salary") =>
        new()
        {
            ConfigurationName = "EmployeeHierarchy",
            SortColumns = new[]
            {
                SortColumnConfiguration.Create(departmentColumn, SortDirection.Ascending, 0),
                SortColumnConfiguration.Create(positionColumn, SortDirection.Ascending, 1),
                SortColumnConfiguration.Create(salaryColumn, SortDirection.Descending, 2)
            },
            PerformanceMode = SortPerformanceMode.Auto,
            MaxSortColumns = 3
        };

    /// <summary>
    /// Creates customer priority sort configuration (Tier > Value > Join Date).
    /// </summary>
    internal static AdvancedSortConfiguration CreateCustomerPriority(
        string tierColumn = "CustomerTier",
        string valueColumn = "TotalValue",
        string joinDateColumn = "JoinDate") =>
        new()
        {
            ConfigurationName = "CustomerPriority",
            SortColumns = new[]
            {
                SortColumnConfiguration.Create(tierColumn, SortDirection.Ascending, 0),
                SortColumnConfiguration.Create(valueColumn, SortDirection.Descending, 1),
                SortColumnConfiguration.Create(joinDateColumn, SortDirection.Ascending, 2)
            },
            PerformanceMode = SortPerformanceMode.Optimized,
            Stability = SortStability.Stable
        };
}

#endregion

#region Statistics

/// <summary>
/// Sort execution statistics.
/// Performance monitoring and optimization metrics.
/// </summary>
internal sealed record SortStatistics
{
    internal int TotalComparisons { get; init; }
    internal int TotalSwaps { get; init; }
    internal TimeSpan AverageSortTime { get; init; }
    internal bool UsedParallelProcessing { get; init; }
    internal bool UsedObjectPooling { get; init; }
    internal int ObjectPoolHits { get; init; }
    internal int CacheHits { get; init; }
    internal double ComparisonsPerSecond { get; init; }
    internal SortPerformanceMode SelectedMode { get; init; }
}

#endregion

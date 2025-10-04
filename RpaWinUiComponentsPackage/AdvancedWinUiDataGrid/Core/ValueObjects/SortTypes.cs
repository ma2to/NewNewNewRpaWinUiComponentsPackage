namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

#region Enums

/// <summary>
/// Smer triedenia pre sort operácie
/// </summary>
internal enum SortDirection
{
    None = 0,
    Ascending = 1,
    Descending = 2
}

/// <summary>
/// Režimy výkonu pre sort operácie
/// </summary>
internal enum SortPerformanceMode
{
    Auto,           // Automatický výber podľa veľkosti dát
    Sequential,     // Jednovláknové triedenie
    Parallel,       // Viacvláknové triedenie
    Optimized       // Pokročilé optimalizácie s cachingom
}

/// <summary>
/// Stabilita triedenia
/// </summary>
internal enum SortStability
{
    Stable,         // Zachováva relatívne poradie rovnakých elementov
    Unstable        // Povoluje zmenu poradia rovnakých elementov kvôli výkonu
}

#endregion

#region Progress & Context Types

/// <summary>
/// Sort operation progress reporting
/// Konzistentná štruktúra s ValidationProgress a ExportProgress
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

    /// <summary>Odhadovaný zostávajúci čas na základe aktuálneho progresu</summary>
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
/// Sort execution context s DI support
/// Hybrid DI: Poskytuje services pre custom sort functions
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
/// Konfigurácia jedného stĺpca pre triedenie
/// Immutable record s factory methods
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
    /// Vytvorí základnú sort konfiguráciu
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
    /// Vytvorí konfiguráciu s custom comparer
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
/// Advanced sort configuration s business rule support
/// Podporuje multi-column sorting s performance optimization
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
    /// Vytvorí employee hierarchy sort configuration
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
    /// Vytvorí customer priority sort configuration
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
/// Sort execution statistics
/// Performance monitoring a optimization metrics
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

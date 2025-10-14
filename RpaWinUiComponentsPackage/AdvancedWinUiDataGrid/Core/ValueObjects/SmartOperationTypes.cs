using System;
using System.Collections.Generic;
using System.Threading;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

#region Enums

/// <summary>
/// Row operation types for smart management
/// </summary>
internal enum RowOperationType
{
    Add,
    Delete,
    Clear,
    AutoExpand,
    SmartDelete
}

/// <summary>
/// Row state for tracking empty/filled rows
/// </summary>
internal enum RowState
{
    Empty,
    Filled,
    Partial,
    LastEmpty
}

#endregion

#region Progress & Context Types

/// <summary>
/// Row management progress reporting
/// </summary>
internal sealed record RowManagementProgress
{
    internal int ProcessedRows { get; init; }
    internal int TotalRows { get; init; }
    internal double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;
    internal TimeSpan ElapsedTime { get; init; }
    internal string CurrentOperation { get; init; } = string.Empty;
    internal RowOperationType CurrentOperationType { get; init; } = RowOperationType.Add;

    public RowManagementProgress() : this(0, 0, TimeSpan.Zero, "", RowOperationType.Add) { }

    public RowManagementProgress(int processedRows, int totalRows, TimeSpan elapsedTime, string currentOperation, RowOperationType operationType)
    {
        ProcessedRows = processedRows;
        TotalRows = totalRows;
        ElapsedTime = elapsedTime;
        CurrentOperation = currentOperation;
        CurrentOperationType = operationType;
    }
}

/// <summary>
/// Row management configuration
/// </summary>
internal sealed record RowManagementConfiguration
{
    internal int MinimumRows { get; init; } = 1;
    internal bool EnableAutoExpand { get; init; } = true;
    internal bool EnableSmartDelete { get; init; } = true;
    internal bool AlwaysKeepLastEmpty { get; init; } = true;

    internal static RowManagementConfiguration Default => new();

    internal static RowManagementConfiguration Create(
        int minimumRows = 1,
        bool enableAutoExpand = true,
        bool enableSmartDelete = true) =>
        new()
        {
            MinimumRows = minimumRows,
            EnableAutoExpand = enableAutoExpand,
            EnableSmartDelete = enableSmartDelete
        };
}

#endregion

#region Command Objects

/// <summary>
/// Smart add rows command
/// </summary>
internal sealed record SmartAddRowsCommand
{
    internal required IEnumerable<IReadOnlyDictionary<string, object?>> DataToAdd { get; init; }
    internal required RowManagementConfiguration Configuration { get; init; }
    internal bool PreserveRowNumbers { get; init; } = true;
    internal IProgress<RowManagementProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static SmartAddRowsCommand Create(
        IEnumerable<IReadOnlyDictionary<string, object?>> dataToAdd,
        RowManagementConfiguration configuration) =>
        new() { DataToAdd = dataToAdd, Configuration = configuration };
}

/// <summary>
/// Smart delete rows command
/// </summary>
internal sealed record SmartDeleteRowsCommand
{
    internal required IReadOnlyList<int> RowIndexesToDelete { get; init; }
    internal required RowManagementConfiguration Configuration { get; init; }
    internal bool ForcePhysicalDelete { get; init; } = false;
    internal IProgress<RowManagementProgress>? ProgressReporter { get; init; }
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static SmartDeleteRowsCommand Create(
        IReadOnlyList<int> rowIndexesToDelete,
        RowManagementConfiguration configuration) =>
        new() { RowIndexesToDelete = rowIndexesToDelete, Configuration = configuration };
}

/// <summary>
/// Auto-expand empty row command
/// </summary>
internal sealed record AutoExpandEmptyRowCommand
{
    internal required RowManagementConfiguration Configuration { get; init; }
    internal int CurrentRowCount { get; init; }
    internal bool TriggerExpansion { get; init; } = true;
    internal CancellationToken CancellationToken { get; init; } = default;

    internal static AutoExpandEmptyRowCommand Create(
        RowManagementConfiguration configuration,
        int currentRowCount) =>
        new() { Configuration = configuration, CurrentRowCount = currentRowCount };
}

#endregion

#region Result Objects

/// <summary>
/// Row management operation result
/// </summary>
internal sealed record RowManagementResult
{
    internal bool Success { get; init; }
    internal int FinalRowCount { get; init; }
    internal int ProcessedRows { get; init; }
    internal RowOperationType OperationType { get; init; }
    internal TimeSpan OperationTime { get; init; }
    internal IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
    internal RowManagementStatistics Statistics { get; init; } = new();

    #region Granular Update Metadata (for 10M+ row performance optimization)

    /// <summary>
    /// Indices of rows that were physically deleted from storage.
    /// Used for granular UI updates (Remove action) instead of full rebuild.
    /// Empty for Scenario A (clear content only).
    /// </summary>
    internal IReadOnlyList<int> PhysicallyDeletedIndices { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Indices of rows where content was cleared but row structure remains.
    /// Used for granular UI updates (update cell values) instead of full rebuild.
    /// Empty for Scenario B (physical delete).
    /// </summary>
    internal IReadOnlyList<int> ContentClearedIndices { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Dictionary of row indices to updated row data (e.g., shifted rows after delete).
    /// Key: Row index, Value: New row data for that index.
    /// Used for granular UI updates (update cell values) instead of full rebuild.
    /// </summary>
    internal IReadOnlyDictionary<int, IReadOnlyDictionary<string, object?>> UpdatedRowData { get; init; }
        = new Dictionary<int, IReadOnlyDictionary<string, object?>>();

    /// <summary>
    /// Indices of rows that may be affected by this operation for validation purposes.
    /// Used for selective validation instead of validating all rows.
    /// Includes deleted/cleared rows and rows with dependent column values.
    /// </summary>
    internal IReadOnlyList<int> AffectedRowIndices { get; init; } = Array.Empty<int>();

    #endregion

    internal static RowManagementResult CreateSuccess(
        int finalRowCount,
        int processedRows,
        RowOperationType operationType,
        TimeSpan operationTime,
        RowManagementStatistics? statistics = null) =>
        new()
        {
            Success = true,
            FinalRowCount = finalRowCount,
            ProcessedRows = processedRows,
            OperationType = operationType,
            OperationTime = operationTime,
            Statistics = statistics ?? new()
        };

    internal static RowManagementResult CreateFailure(
        RowOperationType operationType,
        IReadOnlyList<string> messages,
        TimeSpan operationTime) =>
        new()
        {
            Success = false,
            OperationType = operationType,
            Messages = messages,
            OperationTime = operationTime
        };

    internal static RowManagementResult Empty => new();
}

/// <summary>
/// Row management statistics
/// </summary>
internal sealed record RowManagementStatistics
{
    internal int EmptyRowsCreated { get; init; }
    internal int RowsPhysicallyDeleted { get; init; }
    internal int RowsContentCleared { get; init; }
    internal int RowsShifted { get; init; }
    internal bool MinimumRowsEnforced { get; init; }
    internal bool LastEmptyRowMaintained { get; init; }
}

#endregion

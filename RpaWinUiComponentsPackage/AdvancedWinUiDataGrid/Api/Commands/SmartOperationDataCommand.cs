using System;
using System.Collections.Generic;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public command for smart add rows operation
/// </summary>
public sealed record SmartAddRowsDataCommand
{
    /// <summary>
    /// Data rows to add
    /// </summary>
    public required IEnumerable<IReadOnlyDictionary<string, object?>> DataToAdd { get; init; }

    /// <summary>
    /// Row management configuration
    /// </summary>
    public required PublicRowManagementConfiguration Configuration { get; init; }

    /// <summary>
    /// Progress reporting callback
    /// </summary>
    public IProgress<PublicRowManagementProgress>? ProgressReporter { get; init; }
}

/// <summary>
/// Public command for smart delete rows operation
/// </summary>
public sealed record SmartDeleteRowsDataCommand
{
    /// <summary>
    /// Indices of rows to delete
    /// </summary>
    public required IReadOnlyList<int> RowIndexesToDelete { get; init; }

    /// <summary>
    /// Row management configuration
    /// </summary>
    public required PublicRowManagementConfiguration Configuration { get; init; }

    /// <summary>
    /// Current row count
    /// </summary>
    public int CurrentRowCount { get; init; }

    /// <summary>
    /// Force physical delete even if below minimum rows
    /// </summary>
    public bool ForcePhysicalDelete { get; init; } = false;
}

/// <summary>
/// Public command for auto-expand empty row operation
/// </summary>
public sealed record AutoExpandEmptyRowDataCommand
{
    /// <summary>
    /// Row management configuration
    /// </summary>
    public required PublicRowManagementConfiguration Configuration { get; init; }

    /// <summary>
    /// Current row count
    /// </summary>
    public int CurrentRowCount { get; init; }
}

/// <summary>
/// Public row management configuration
/// </summary>
public sealed record PublicRowManagementConfiguration
{
    /// <summary>
    /// Minimum number of rows to maintain
    /// </summary>
    public int MinimumRows { get; init; } = 1;

    /// <summary>
    /// Enable automatic expansion of empty rows
    /// </summary>
    public bool EnableAutoExpand { get; init; } = true;

    /// <summary>
    /// Enable smart delete logic
    /// </summary>
    public bool EnableSmartDelete { get; init; } = true;

    /// <summary>
    /// Always keep last row empty
    /// </summary>
    public bool AlwaysKeepLastEmpty { get; init; } = true;
}

/// <summary>
/// Public result from smart row operation
/// </summary>
public sealed record SmartOperationDataResult
{
    /// <summary>
    /// Indicates if operation was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Final row count after operation
    /// </summary>
    public int FinalRowCount { get; init; }

    /// <summary>
    /// Number of rows processed
    /// </summary>
    public int ProcessedRows { get; init; }

    /// <summary>
    /// Operation time
    /// </summary>
    public TimeSpan OperationTime { get; init; }

    /// <summary>
    /// Messages from operation
    /// </summary>
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Statistics from operation
    /// </summary>
    public PublicRowManagementStatistics Statistics { get; init; } = new();
}

/// <summary>
/// Public row management statistics
/// </summary>
public sealed record PublicRowManagementStatistics
{
    /// <summary>
    /// Number of empty rows created
    /// </summary>
    public int EmptyRowsCreated { get; init; }

    /// <summary>
    /// Number of rows physically deleted
    /// </summary>
    public int RowsPhysicallyDeleted { get; init; }

    /// <summary>
    /// Number of rows with content cleared
    /// </summary>
    public int RowsContentCleared { get; init; }

    /// <summary>
    /// Number of rows shifted
    /// </summary>
    public int RowsShifted { get; init; }

    /// <summary>
    /// Whether minimum rows were enforced
    /// </summary>
    public bool MinimumRowsEnforced { get; init; }

    /// <summary>
    /// Whether last empty row was maintained
    /// </summary>
    public bool LastEmptyRowMaintained { get; init; }
}

/// <summary>
/// Public row management progress
/// </summary>
public sealed record PublicRowManagementProgress
{
    /// <summary>
    /// Number of rows processed
    /// </summary>
    public int ProcessedRows { get; init; }

    /// <summary>
    /// Total rows to process
    /// </summary>
    public int TotalRows { get; init; }

    /// <summary>
    /// Completion percentage
    /// </summary>
    public double CompletionPercentage => TotalRows > 0 ? (double)ProcessedRows / TotalRows * 100 : 0;

    /// <summary>
    /// Current operation description
    /// </summary>
    public string CurrentOperation { get; init; } = string.Empty;
}

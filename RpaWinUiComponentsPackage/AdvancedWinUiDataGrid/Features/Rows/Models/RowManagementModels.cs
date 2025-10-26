using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Rows.Models;

/// <summary>
/// Result of a row management operation (delete, insert, update)
/// </summary>
internal sealed class RowManagementResult
{
    public bool Success { get; init; }
    public int FinalRowCount { get; init; }
    public int RowsAffected { get; init; }
    public RowOperationType OperationType { get; init; }
    public TimeSpan Duration { get; init; }
    public RowManagementStatistics Statistics { get; init; } = new();
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();

    public static RowManagementResult CreateSuccess(
        int finalRowCount,
        int rowsAffected,
        RowOperationType operationType,
        TimeSpan duration,
        RowManagementStatistics statistics)
    {
        return new RowManagementResult
        {
            Success = true,
            FinalRowCount = finalRowCount,
            RowsAffected = rowsAffected,
            OperationType = operationType,
            Duration = duration,
            Statistics = statistics,
            Messages = new[] { $"{operationType} operation completed successfully: {rowsAffected} rows affected" }
        };
    }

    public static RowManagementResult CreateFailure(
        RowOperationType operationType,
        IEnumerable<string> errorMessages,
        TimeSpan duration)
    {
        return new RowManagementResult
        {
            Success = false,
            FinalRowCount = 0,
            RowsAffected = 0,
            OperationType = operationType,
            Duration = duration,
            Statistics = new(),
            Messages = errorMessages.ToList()
        };
    }
}

/// <summary>
/// Statistics about row management operation
/// </summary>
internal sealed class RowManagementStatistics
{
    public int RowsDataCleared { get; init; }
    public int RowsDataShifted { get; init; }
    public int EmptyRowsCreated { get; init; }
}

/// <summary>
/// Type of row management operation
/// </summary>
internal enum RowOperationType
{
    Delete,
    Insert,
    Update
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;

/// <summary>
/// APPLICATION INTERFACE: RowNumber management service contract
/// CLEAN ARCHITECTURE: Application layer abstraction for RowNumber operations
/// ENTERPRISE: Professional RowNumber management with async operations
/// </summary>
internal interface IRowNumberService
{
    /// <summary>
    /// APPLICATION: Assign RowNumber to new row asynchronously
    /// ENTERPRISE: Thread-safe row number assignment for concurrent operations
    /// </summary>
    Task<Result<int>> AssignRowNumberAsync(DataRow newRow, IEnumerable<DataRow> existingRows, CancellationToken cancellationToken = default);

    /// <summary>
    /// APPLICATION: Regenerate all RowNumbers after sort operations
    /// ENTERPRISE: Batch RowNumber update with progress reporting
    /// </summary>
    Task<Result<bool>> RegenerateRowNumbersAsync(IList<DataRow> orderedRows, IProgress<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// APPLICATION: Compact RowNumbers after deletion operations
    /// ENTERPRISE: Remove gaps in RowNumber sequence
    /// </summary>
    Task<Result<bool>> CompactRowNumbersAsync(IList<DataRow> rows, CancellationToken cancellationToken = default);

    /// <summary>
    /// APPLICATION: Batch assign RowNumbers for import operations
    /// ENTERPRISE: Efficient batch processing for large datasets
    /// </summary>
    Task<Result<bool>> AssignRowNumbersBatchAsync(IList<DataRow> newRows, int startingRowNumber = 1, IProgress<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// APPLICATION: Get next available RowNumber for new row creation
    /// ENTERPRISE: Thread-safe RowNumber generation
    /// </summary>
    Task<Result<int>> GetNextRowNumberAsync(IEnumerable<DataRow> existingRows, CancellationToken cancellationToken = default);

    /// <summary>
    /// VALIDATION: Validate RowNumber sequence integrity
    /// ENTERPRISE: Data consistency validation with detailed reporting
    /// </summary>
    Task<Result<RowNumberValidationResult>> ValidateRowNumberSequenceAsync(IEnumerable<DataRow> rows, CancellationToken cancellationToken = default);

    /// <summary>
    /// ENTERPRISE: Repair corrupted RowNumber sequence
    /// SMART: Automatic recovery from RowNumber inconsistencies
    /// </summary>
    Task<Result<bool>> RepairRowNumberSequenceAsync(IList<DataRow> rows, CancellationToken cancellationToken = default);

    /// <summary>
    /// STATISTICS: Get RowNumber management statistics
    /// ENTERPRISE: Performance monitoring and diagnostics
    /// </summary>
    Task<Result<RowNumberStatistics>> GetRowNumberStatisticsAsync(IEnumerable<DataRow> rows, CancellationToken cancellationToken = default);
}

/// <summary>
/// VALUE OBJECT: RowNumber management statistics
/// ENTERPRISE: Comprehensive statistics for monitoring and diagnostics
/// </summary>
internal sealed class RowNumberStatistics
{
    public int TotalRows { get; }
    public int MinRowNumber { get; }
    public int MaxRowNumber { get; }
    public int ExpectedMaxRowNumber { get; }
    public int GapCount { get; }
    public int DuplicateCount { get; }
    public bool HasValidSequence { get; }
    public DateTime LastUpdateTime { get; }

    public RowNumberStatistics(
        int totalRows,
        int minRowNumber,
        int maxRowNumber,
        int expectedMaxRowNumber,
        int gapCount,
        int duplicateCount,
        bool hasValidSequence,
        DateTime lastUpdateTime)
    {
        TotalRows = totalRows;
        MinRowNumber = minRowNumber;
        MaxRowNumber = maxRowNumber;
        ExpectedMaxRowNumber = expectedMaxRowNumber;
        GapCount = gapCount;
        DuplicateCount = duplicateCount;
        HasValidSequence = hasValidSequence;
        LastUpdateTime = lastUpdateTime;
    }

    public static RowNumberStatistics Empty => new(0, 0, 0, 0, 0, 0, true, DateTime.MinValue);
}
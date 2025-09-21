using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Application.Services;

/// <summary>
/// APPLICATION SERVICE: RowNumber management with async operations and progress reporting
/// CLEAN ARCHITECTURE: Application layer implementation of RowNumber business logic
/// ENTERPRISE: Professional RowNumber service with comprehensive error handling
/// </summary>
internal sealed class RowNumberService : IRowNumberService
{
    private readonly Core.Services.RowNumberService _coreRowNumberService;
    private readonly object _operationLock = new();

    public RowNumberService()
    {
        _coreRowNumberService = new Core.Services.RowNumberService();
    }

    public async Task<Result<int>> AssignRowNumberAsync(DataRow newRow, IEnumerable<DataRow> existingRows, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (newRow == null)
                return Result<int>.Failure("New row cannot be null");

            if (existingRows == null)
                return Result<int>.Failure("Existing rows collection cannot be null");

            cancellationToken.ThrowIfCancellationRequested();

            lock (_operationLock)
            {
                _coreRowNumberService.AssignRowNumber(newRow, existingRows);
                return Result<int>.Success(newRow.RowNumber);
            }
        }
        catch (OperationCanceledException)
        {
            return Result<int>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to assign row number: {ex.Message}");
        }
    }

    public async Task<Result<bool>> RegenerateRowNumbersAsync(IList<DataRow> orderedRows, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (orderedRows == null)
                return Result<bool>.Failure("Ordered rows collection cannot be null");

            cancellationToken.ThrowIfCancellationRequested();

            lock (_operationLock)
            {
                const int batchSize = 1000;
                int processed = 0;

                for (int i = 0; i < orderedRows.Count; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = orderedRows.Skip(i).Take(batchSize).ToList();

                    for (int j = 0; j < batch.Count; j++)
                    {
                        batch[j].RowNumber = i + j + 1; // 1-based numbering
                    }

                    processed += batch.Count;
                    progress?.Report(processed);

                    // Yield control for long operations
                    if (i % 5000 == 0 && i > 0)
                    {
                        Task.Delay(1, cancellationToken).Wait(cancellationToken);
                    }
                }

                return Result<bool>.Success(true);
            }
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to regenerate row numbers: {ex.Message}");
        }
    }

    public async Task<Result<bool>> CompactRowNumbersAsync(IList<DataRow> rows, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (rows == null)
                return Result<bool>.Failure("Rows collection cannot be null");

            cancellationToken.ThrowIfCancellationRequested();

            lock (_operationLock)
            {
                _coreRowNumberService.CompactRowNumbers(rows);
                return Result<bool>.Success(true);
            }
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to compact row numbers: {ex.Message}");
        }
    }

    public async Task<Result<bool>> AssignRowNumbersBatchAsync(IList<DataRow> newRows, int startingRowNumber = 1, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (newRows == null)
                return Result<bool>.Failure("New rows collection cannot be null");

            if (startingRowNumber < 1)
                return Result<bool>.Failure("Starting row number must be positive");

            cancellationToken.ThrowIfCancellationRequested();

            lock (_operationLock)
            {
                const int batchSize = 1000;
                int processed = 0;

                for (int i = 0; i < newRows.Count; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = newRows.Skip(i).Take(batchSize).ToList();
                    _coreRowNumberService.AssignRowNumbersBatch(batch, startingRowNumber + i);

                    processed += batch.Count;
                    progress?.Report(processed);

                    // Yield control for long operations
                    if (i % 5000 == 0 && i > 0)
                    {
                        Task.Delay(1, cancellationToken).Wait(cancellationToken);
                    }
                }

                return Result<bool>.Success(true);
            }
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to assign row numbers in batch: {ex.Message}");
        }
    }

    public async Task<Result<int>> GetNextRowNumberAsync(IEnumerable<DataRow> existingRows, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (existingRows == null)
                return Result<int>.Failure("Existing rows collection cannot be null");

            cancellationToken.ThrowIfCancellationRequested();

            lock (_operationLock)
            {
                var nextRowNumber = _coreRowNumberService.GetNextRowNumber(existingRows);
                return Result<int>.Success(nextRowNumber);
            }
        }
        catch (OperationCanceledException)
        {
            return Result<int>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to get next row number: {ex.Message}");
        }
    }

    public async Task<Result<Core.Services.RowNumberValidationResult>> ValidateRowNumberSequenceAsync(IEnumerable<DataRow> rows, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (rows == null)
                return Result<Core.Services.RowNumberValidationResult>.Failure("Rows collection cannot be null");

            cancellationToken.ThrowIfCancellationRequested();

            lock (_operationLock)
            {
                var validationResult = _coreRowNumberService.ValidateRowNumberSequence(rows);
                return Result<Core.Services.RowNumberValidationResult>.Success(validationResult);
            }
        }
        catch (OperationCanceledException)
        {
            return Result<Core.Services.RowNumberValidationResult>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<Core.Services.RowNumberValidationResult>.Failure($"Failed to validate row number sequence: {ex.Message}");
        }
    }

    public async Task<Result<bool>> RepairRowNumberSequenceAsync(IList<DataRow> rows, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (rows == null)
                return Result<bool>.Failure("Rows collection cannot be null");

            cancellationToken.ThrowIfCancellationRequested();

            lock (_operationLock)
            {
                _coreRowNumberService.RepairRowNumberSequence(rows);
                return Result<bool>.Success(true);
            }
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to repair row number sequence: {ex.Message}");
        }
    }

    public async Task<Result<RowNumberStatistics>> GetRowNumberStatisticsAsync(IEnumerable<DataRow> rows, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        try
        {
            if (rows == null)
                return Result<RowNumberStatistics>.Failure("Rows collection cannot be null");

            cancellationToken.ThrowIfCancellationRequested();

            var rowList = rows.ToList();
            if (!rowList.Any())
                return Result<RowNumberStatistics>.Success(RowNumberStatistics.Empty);

            var rowNumbers = rowList.Select(r => r.RowNumber).ToList();
            var minRowNumber = rowNumbers.Min();
            var maxRowNumber = rowNumbers.Max();
            var expectedMaxRowNumber = rowList.Count;

            // Calculate gaps
            var gapCount = 0;
            for (int i = 1; i <= expectedMaxRowNumber; i++)
            {
                if (!rowNumbers.Contains(i))
                    gapCount++;
            }

            // Calculate duplicates
            var duplicateCount = rowNumbers.GroupBy(rn => rn)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);

            var hasValidSequence = gapCount == 0 && duplicateCount == 0 &&
                                 minRowNumber == 1 && maxRowNumber == expectedMaxRowNumber;

            var statistics = new RowNumberStatistics(
                rowList.Count,
                minRowNumber,
                maxRowNumber,
                expectedMaxRowNumber,
                gapCount,
                duplicateCount,
                hasValidSequence,
                DateTime.UtcNow);

            return Result<RowNumberStatistics>.Success(statistics);
        }
        catch (OperationCanceledException)
        {
            return Result<RowNumberStatistics>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<RowNumberStatistics>.Failure($"Failed to get row number statistics: {ex.Message}");
        }
    }
}
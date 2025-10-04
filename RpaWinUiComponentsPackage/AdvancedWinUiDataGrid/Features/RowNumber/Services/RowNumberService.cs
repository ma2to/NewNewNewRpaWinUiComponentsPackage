using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowNumber.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.RowNumber.Services;

/// <summary>
/// INTERNAL: Row number management service implementation
/// ENTERPRISE: Thread-safe row numbering with automatic regeneration
/// PERFORMANCE: Optimized for bulk operations with minimal overhead
/// </summary>
internal sealed class RowNumberService : IRowNumberService
{
    private readonly Infrastructure.Persistence.Interfaces.IRowStore _rowStore;
    private readonly ILogger<RowNumberService> _logger;
    private readonly IOperationLogger<RowNumberService> _operationLogger;
    private readonly SemaphoreSlim _regenerationLock = new(1, 1);

    public RowNumberService(
        Infrastructure.Persistence.Interfaces.IRowStore rowStore,
        ILogger<RowNumberService> logger,
        IOperationLogger<RowNumberService>? operationLogger = null)
    {
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Použijeme null pattern ak logger nie je poskytnutý
        _operationLogger = operationLogger ?? NullOperationLogger<RowNumberService>.Instance;

        _logger.LogInformation("RowNumberService initialized");
    }

    /// <summary>
    /// CORE: Regenerate all row numbers with sequential ordering
    /// SMART: Uses CreatedAt as fallback ordering
    /// THREAD-SAFE: Single regeneration at a time
    /// </summary>
    public async Task<bool> RegenerateRowNumbersAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationId = Guid.NewGuid();

        // Začíname regenerate row numbers operáciu - vytvoríme operation scope pre automatické tracking
        using var scope = _operationLogger.LogOperationStart("RegenerateRowNumbersAsync", new
        {
            OperationId = operationId
        });

        _logger.LogInformation("Starting row number regeneration for operation {OperationId}", operationId);

        // Zabezpečíme že len jedna regenerácia beží naraz
        await _regenerationLock.WaitAsync(cancellationToken);
        try
        {
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);

            if (allRows.Count == 0)
            {
                _logger.LogInformation("No rows to regenerate for operation {OperationId}", operationId);

                scope.MarkSuccess(new
                {
                    RenumberedRows = 0,
                    Duration = stopwatch.Elapsed
                });

                return true;
            }

            _logger.LogInformation("Loaded {RowCount} rows for number regeneration for operation {OperationId}",
                allRows.Count, operationId);

            // Zoradíme podľa CreatedAt (fallback) alebo existujúceho RowNumber
            _logger.LogInformation("Sorting rows by CreatedAt for operation {OperationId}", operationId);

            var sortedRows = allRows
                .Select((row, index) => new { Row = row, Index = index })
                .OrderBy(x => GetCreatedAt(x.Row))
                .ThenBy(x => x.Index)
                .ToList();

            // Priraďujeme sekvenčné row numbers (1-based)
            _logger.LogInformation("Assigning new sequential row numbers (1-based) for operation {OperationId}", operationId);

            var updatedRows = sortedRows.Select((item, newRowNumber) =>
            {
                var mutableRow = item.Row.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                mutableRow["__rowNumber"] = newRowNumber + 1; // 1-based
                return (IReadOnlyDictionary<string, object?>)mutableRow;
            }).ToList();

            // Nahradíme všetky riadky s aktualizovanými row numbers
            _logger.LogInformation("Saving {RowCount} rows with new row numbers for operation {OperationId}",
                updatedRows.Count, operationId);

            await _rowStore.ReplaceAllRowsAsync(updatedRows, cancellationToken);

            _logger.LogInformation("Row number regeneration completed in {Duration}ms for operation {OperationId}: " +
                "{Count} rows renumbered",
                stopwatch.ElapsedMilliseconds, operationId, updatedRows.Count);

            scope.MarkSuccess(new
            {
                RenumberedRows = updatedRows.Count,
                Duration = stopwatch.Elapsed
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Row number regeneration failed for operation {OperationId}: {Message}",
                operationId, ex.Message);

            scope.MarkFailure(ex);
            return false;
        }
        finally
        {
            _regenerationLock.Release();
        }
    }

    /// <summary>
    /// UTILITY: Get next available row number
    /// </summary>
    public async Task<int> GetNextRowNumberAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var maxRowNumber = await GetMaxRowNumberAsync(cancellationToken);
            var nextRowNumber = maxRowNumber + 1;

            _logger.LogDebug("Next row number calculated: {NextRowNumber}", nextRowNumber);
            return nextRowNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate next row number");
            return 1; // Default to 1 if calculation fails
        }
    }

    /// <summary>
    /// QUERY: Get maximum row number
    /// </summary>
    public async Task<int> GetMaxRowNumberAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);

            if (allRows.Count == 0)
                return 0;

            var maxRowNumber = allRows
                .Select(row => GetRowNumber(row))
                .DefaultIfEmpty(0)
                .Max();

            _logger.LogTrace("Max row number: {MaxRowNumber}", maxRowNumber);
            return maxRowNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get max row number");
            return 0;
        }
    }

    /// <summary>
    /// VALIDATION: Check if row numbers are valid
    /// </summary>
    public async Task<bool> ValidateRowNumbersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allRows = await _rowStore.GetAllRowsAsync(cancellationToken);

            if (allRows.Count == 0)
                return true;

            var rowNumbers = allRows.Select(GetRowNumber).OrderBy(n => n).ToList();

            // Check for duplicates
            if (rowNumbers.Count != rowNumbers.Distinct().Count())
            {
                _logger.LogWarning("Row numbers validation failed: duplicates detected");
                return false;
            }

            // Check for gaps (should be 1, 2, 3, ...)
            for (int i = 0; i < rowNumbers.Count; i++)
            {
                if (rowNumbers[i] != i + 1)
                {
                    _logger.LogWarning("Row numbers validation failed: gap detected at position {Position}, expected {Expected}, got {Actual}",
                        i + 1, i + 1, rowNumbers[i]);
                    return false;
                }
            }

            _logger.LogDebug("Row numbers validation passed: {Count} rows", rowNumbers.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Row numbers validation failed");
            return false;
        }
    }

    #region Private Helpers

    private static int GetRowNumber(IReadOnlyDictionary<string, object?> row)
    {
        if (row.TryGetValue("__rowNumber", out var value))
        {
            return value switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                string strValue when int.TryParse(strValue, out var parsed) => parsed,
                _ => 0
            };
        }
        return 0;
    }

    private static DateTime GetCreatedAt(IReadOnlyDictionary<string, object?> row)
    {
        if (row.TryGetValue("__createdAt", out var value) && value is DateTime dt)
        {
            return dt;
        }
        return DateTime.MinValue;
    }

    #endregion
}

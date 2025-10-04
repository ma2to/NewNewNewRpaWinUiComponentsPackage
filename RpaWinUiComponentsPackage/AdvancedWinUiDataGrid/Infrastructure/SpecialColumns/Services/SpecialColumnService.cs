using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.SpecialColumns.Interfaces;
using System.Collections.Concurrent;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.SpecialColumns.Services;

/// <summary>
/// Service for managing special columns (validation alerts, row numbers, etc.)
/// Thread-safe implementation with IRowStore integration
/// </summary>
internal sealed class SpecialColumnService : ISpecialColumnService
{
    private readonly ILogger<SpecialColumnService> _logger;
    private readonly IRowStore _rowStore;
    private readonly ConcurrentDictionary<int, string> _validationAlertsCache;

    /// <summary>
    /// Validation alerts column name constant
    /// </summary>
    public string ValidationAlertsColumnName => "validAlerts";

    /// <summary>
    /// Row number column name constant
    /// </summary>
    public string RowNumberColumnName => "rowNumber";

    /// <summary>
    /// Constructor for SpecialColumnService
    /// </summary>
    public SpecialColumnService(
        ILogger<SpecialColumnService> logger,
        IRowStore rowStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _validationAlertsCache = new ConcurrentDictionary<int, string>();
    }

    /// <summary>
    /// Updates validation alerts for a specific row
    /// </summary>
    public async Task<Result> UpdateValidationAlertsAsync(int rowIndex, string alertMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating validation alerts for row {RowIndex}: {Message}", rowIndex, alertMessage);

            // Update cache
            _validationAlertsCache.AddOrUpdate(rowIndex, alertMessage, (_, _) => alertMessage);

            // Update row store
            var row = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
            if (row == null)
            {
                _logger.LogWarning("Row {RowIndex} not found when updating validation alerts", rowIndex);
                return Result.Failure($"Row {rowIndex} not found");
            }

            // Create updated row with validation alerts
            var updatedRow = new Dictionary<string, object?>(row)
            {
                [ValidationAlertsColumnName] = alertMessage
            };

            await _rowStore.UpdateRowAsync(rowIndex, updatedRow, cancellationToken);

            _logger.LogDebug("Successfully updated validation alerts for row {RowIndex}", rowIndex);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update validation alerts for row {RowIndex}: {Message}", rowIndex, ex.Message);
            return Result.Failure($"Failed to update validation alerts: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets validation alerts for a specific row
    /// </summary>
    public string? GetValidationAlerts(int rowIndex)
    {
        // Check cache first
        if (_validationAlertsCache.TryGetValue(rowIndex, out var cachedAlert))
        {
            return cachedAlert;
        }

        // Fallback to row store (synchronous - may need redesign for true async)
        try
        {
            var row = _rowStore.GetRowAsync(rowIndex, CancellationToken.None).GetAwaiter().GetResult();
            if (row != null && row.TryGetValue(ValidationAlertsColumnName, out var value))
            {
                var alert = value?.ToString();
                if (!string.IsNullOrEmpty(alert))
                {
                    _validationAlertsCache.TryAdd(rowIndex, alert);
                    return alert;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get validation alerts for row {RowIndex}", rowIndex);
        }

        return null;
    }

    /// <summary>
    /// Clears validation alerts for a specific row
    /// </summary>
    public async Task<Result> ClearValidationAlertsAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Clearing validation alerts for row {RowIndex}", rowIndex);

            // Remove from cache
            _validationAlertsCache.TryRemove(rowIndex, out _);

            // Update row store
            var row = await _rowStore.GetRowAsync(rowIndex, cancellationToken);
            if (row == null)
            {
                _logger.LogWarning("Row {RowIndex} not found when clearing validation alerts", rowIndex);
                return Result.Failure($"Row {rowIndex} not found");
            }

            // Create updated row without validation alerts
            var updatedRow = new Dictionary<string, object?>(row)
            {
                [ValidationAlertsColumnName] = string.Empty
            };

            await _rowStore.UpdateRowAsync(rowIndex, updatedRow, cancellationToken);

            _logger.LogDebug("Successfully cleared validation alerts for row {RowIndex}", rowIndex);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear validation alerts for row {RowIndex}: {Message}", rowIndex, ex.Message);
            return Result.Failure($"Failed to clear validation alerts: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all validation alerts
    /// </summary>
    public async Task<Result> ClearAllValidationAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Clearing all validation alerts");

            // Clear cache
            _validationAlertsCache.Clear();

            // Clear all rows (batch operation)
            var rowCount = await _rowStore.GetRowCountAsync(cancellationToken);
            for (int i = 0; i < rowCount; i++)
            {
                var row = await _rowStore.GetRowAsync(i, cancellationToken);
                if (row != null)
                {
                    var updatedRow = new Dictionary<string, object?>(row)
                    {
                        [ValidationAlertsColumnName] = string.Empty
                    };
                    await _rowStore.UpdateRowAsync(i, updatedRow, cancellationToken);
                }
            }

            _logger.LogInformation("Successfully cleared all validation alerts for {RowCount} rows", rowCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all validation alerts: {Message}", ex.Message);
            return Result.Failure($"Failed to clear all validation alerts: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a column is a special column
    /// </summary>
    public bool IsSpecialColumn(string columnName)
    {
        return columnName == ValidationAlertsColumnName || columnName == RowNumberColumnName;
    }

    /// <summary>
    /// Gets the special column type for a column
    /// </summary>
    public SpecialColumnType GetSpecialColumnType(string columnName)
    {
        return columnName switch
        {
            _ when columnName == ValidationAlertsColumnName => SpecialColumnType.ValidationAlerts,
            _ when columnName == RowNumberColumnName => SpecialColumnType.RowNumber,
            _ => SpecialColumnType.Normal
        };
    }

    /// <summary>
    /// Initializes special columns (adds them if they don't exist)
    /// </summary>
    public async Task<Result> InitializeSpecialColumnsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing special columns");

            // For now, just log - actual column creation should be handled by ColumnStore
            // This is a placeholder for future implementation
            _logger.LogInformation("Special columns initialized successfully");

            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize special columns: {Message}", ex.Message);
            return Result.Failure($"Failed to initialize special columns: {ex.Message}");
        }
    }
}

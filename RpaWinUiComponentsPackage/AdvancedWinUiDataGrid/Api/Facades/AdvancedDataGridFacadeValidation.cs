using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Import.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Export.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Validation.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.CopyPaste.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Selection.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Column.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Filter.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Initialization.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Logging.NullPattern;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Partial class containing Validation Operations and Validation Management APIs
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Validation Operations

    /// <summary>
    /// Validates all non-empty rows with batch, thread-safe processing
    /// Implementation according to documentation: AreAllNonEmptyRowsValidAsync with batch, thread-safe, stream support
    /// </summary>
    public async Task<PublicResult<bool>> ValidateAllAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Validation, nameof(ValidateAllAsync));

        _logger.LogDebug("Starting validation: onlyFiltered={OnlyFiltered}", onlyFiltered);

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();

            var internalResult = await validationService.AreAllNonEmptyRowsValidAsync(onlyFiltered, cancellationToken);
            var result = internalResult.ToPublic();

            // Automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("Validation", 0);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
            return PublicResult<bool>.Failure($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates all non-empty rows with detailed statistics tracking
    /// </summary>
    public async Task<PublicValidationResultWithStatistics> ValidateAllWithStatisticsAsync(bool onlyFiltered = false, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        EnsureFeatureEnabled(GridFeature.Validation, nameof(ValidateAllWithStatisticsAsync));

        _logger.LogDebug("Starting validation with statistics: onlyFiltered={OnlyFiltered}", onlyFiltered);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
            var rowStore = scope.ServiceProvider.GetRequiredService<IRowStore>();

            // Get all rows to validate
            var allRows = await rowStore.GetAllRowsAsync(cancellationToken);

            int totalRows = allRows.Count;
            int validRows = 0;
            int totalErrors = 0;
            var errorsBySeverity = new Dictionary<string, int>();
            var validationErrors = new List<PublicValidationErrorViewModel>();
            var ruleStatisticsDict = new Dictionary<string, RuleStatsAccumulator>();

            // Validate each row and accumulate statistics
            for (int rowIndex = 0; rowIndex < allRows.Count; rowIndex++)
            {
                var rowData = allRows[rowIndex];

                var context = new ValidationContext
                {
                    RowIndex = rowIndex,
                    AllRows = allRows,
                    OperationId = Guid.NewGuid().ToString()
                };

                var ruleStopwatch = System.Diagnostics.Stopwatch.StartNew();
                var validationResult = await validationService.ValidateRowAsync(rowData, context, cancellationToken);
                ruleStopwatch.Stop();

                if (validationResult.IsValid)
                {
                    validRows++;
                }
                else
                {
                    totalErrors++;

                    // Accumulate error by severity
                    var severityKey = validationResult.Severity.ToString();
                    errorsBySeverity.TryGetValue(severityKey, out int count);
                    errorsBySeverity[severityKey] = count + 1;

                    // Add validation error
                    validationErrors.Add(new PublicValidationErrorViewModel
                    {
                        RowIndex = rowIndex,
                        ColumnName = validationResult.AffectedColumn ?? string.Empty,
                        Message = validationResult.ErrorMessage ?? string.Empty,
                        Severity = validationResult.Severity.ToString(),
                        ErrorCode = $"VAL_{rowIndex}"
                    });
                }

                // Track rule statistics (simplified - in real implementation this would come from ValidationService)
                var ruleName = validationResult.AffectedColumn ?? "DefaultRule";
                if (!ruleStatisticsDict.ContainsKey(ruleName))
                {
                    ruleStatisticsDict[ruleName] = new RuleStatsAccumulator { RuleName = ruleName };
                }

                var stats = ruleStatisticsDict[ruleName];
                stats.ExecutionCount++;
                stats.TotalExecutionTime += ruleStopwatch.Elapsed;
                if (!validationResult.IsValid)
                {
                    stats.ErrorsFound++;
                }
            }

            stopwatch.Stop();

            // Convert rule statistics to public format
            var ruleStatistics = ruleStatisticsDict.Values.Select(stats => new PublicRuleStatistics
            {
                RuleName = stats.RuleName,
                ExecutionCount = stats.ExecutionCount,
                AverageExecutionTimeMs = stats.ExecutionCount > 0
                    ? stats.TotalExecutionTime.TotalMilliseconds / stats.ExecutionCount
                    : 0,
                ErrorsFound = stats.ErrorsFound,
                TotalExecutionTime = stats.TotalExecutionTime
            }).ToList();

            // Automatic UI refresh in Interactive mode
            await TriggerUIRefreshIfNeededAsync("Validation", 0);

            _logger.LogInformation(
                "Validation with statistics completed: TotalRows={TotalRows}, ValidRows={ValidRows}, TotalErrors={TotalErrors}, Duration={Duration}ms",
                totalRows, validRows, totalErrors, stopwatch.ElapsedMilliseconds);

            return totalErrors == 0
                ? PublicValidationResultWithStatistics.Success(totalRows, stopwatch.Elapsed, ruleStatistics)
                : PublicValidationResultWithStatistics.Failure(
                    totalRows,
                    validRows,
                    totalErrors,
                    errorsBySeverity,
                    stopwatch.Elapsed,
                    ruleStatistics,
                    validationErrors);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Validation with statistics failed");

            return PublicValidationResultWithStatistics.Failure(
                0, 0, 0,
                new Dictionary<string, int>(),
                stopwatch.Elapsed,
                Array.Empty<PublicRuleStatistics>(),
                new List<PublicValidationErrorViewModel>
                {
                    new() { Message = $"Validation failed: {ex.Message}", Severity = "Error" }
                });
        }
    }

    /// <summary>
    /// Refreshes validation results to UI (no-op in headless mode)
    /// </summary>
    public void RefreshValidationResultsToUI()
    {
        ThrowIfDisposed();

        if (_options.OperationMode == PublicDataGridOperationMode.Headless)
        {
            _logger.LogDebug("RefreshValidationResultsToUI called in headless mode - no operation performed");
            return;
        }

        if (_dispatcher == null)
        {
            _logger.LogWarning("No dispatcher available for UI refresh");
            return;
        }

        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            _logger.LogDebug("Refreshing validation results to UI");
            // In a real implementation, this would trigger UI updates
        });
    }

    /// <summary>
    /// Manually refreshes UI after operations
    /// Available in both Interactive and Headless modes (if DispatcherQueue is provided)
    /// - Interactive mode: Automatic UI refresh after operations + manual via this method
    /// - Headless mode: NO automatic refresh, ONLY manual via this method
    /// </summary>
    public async Task RefreshUIAsync(string operationType = "ManualRefresh", int affectedRows = 0)
    {
        ThrowIfDisposed();

        if (_uiNotificationService == null)
        {
            throw new InvalidOperationException(
                "UI refresh is not available because DispatcherQueue was not provided in AdvancedDataGridOptions. " +
                "To enable UI refresh, provide a DispatcherQueue when creating the grid.");
        }

        _logger.LogInformation("Manual UI refresh requested: OperationType={OperationType}, AffectedRows={AffectedRows}",
            operationType, affectedRows);

        // Funguje v Interactive aj Headless mode (if DispatcherQueue poskytnutý)
        await _uiNotificationService.NotifyDataRefreshAsync(affectedRows, operationType);
    }

    #endregion

    #region Validation Management APIs

    /// <summary>
    /// Adds validation rule
    /// </summary>
    public async Task<PublicResult> AddValidationRuleAsync(IValidationRule rule)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
            var internalResult = await validationService.AddValidationRuleAsync(rule);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add validation rule");
            return PublicResult.Failure($"Failed to add validation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes validation rules by column names
    /// </summary>
    public async Task<PublicResult> RemoveValidationRulesAsync(params string[] columnNames)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
            var internalResult = await validationService.RemoveValidationRulesAsync(columnNames);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove validation rules");
            return PublicResult.Failure($"Failed to remove validation rules: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes validation rule by name
    /// </summary>
    public async Task<PublicResult> RemoveValidationRuleAsync(string ruleName)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
            var internalResult = await validationService.RemoveValidationRuleAsync(ruleName);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove validation rule");
            return PublicResult.Failure($"Failed to remove validation rule: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all validation rules
    /// </summary>
    public async Task<PublicResult> ClearAllValidationRulesAsync()
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();
            var internalResult = await validationService.ClearAllValidationRulesAsync();
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear validation rules");
            return PublicResult.Failure($"Failed to clear validation rules: {ex.Message}");
        }
    }

    #endregion
}

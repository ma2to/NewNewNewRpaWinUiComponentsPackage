using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.SmartAddDelete.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Infrastructure.Persistence.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.UIAdapters.WinUI;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.SmartOperations;

/// <summary>
/// Public implementation of smart row management operations.
/// Wraps internal ISmartOperationService and provides public API.
/// </summary>
internal sealed class DataGridSmartOperations : IDataGridSmartOperations
{
    private readonly ILogger<DataGridSmartOperations>? _logger;
    private readonly ISmartOperationService _smartOperationService;
    private readonly IRowStore _rowStore;
    private readonly UiNotificationService? _uiNotificationService;
    private readonly AdvancedDataGridOptions _options;
    private PublicSmartOperationsConfig _currentConfig;

    public DataGridSmartOperations(
        ISmartOperationService smartOperationService,
        IRowStore rowStore,
        AdvancedDataGridOptions options,
        UiNotificationService? uiNotificationService = null,
        ILogger<DataGridSmartOperations>? logger = null)
    {
        _smartOperationService = smartOperationService ?? throw new ArgumentNullException(nameof(smartOperationService));
        _rowStore = rowStore ?? throw new ArgumentNullException(nameof(rowStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _uiNotificationService = uiNotificationService;
        _logger = logger;
        _currentConfig = PublicSmartOperationsConfig.Default;
    }

    public async Task<PublicSmartOperationResult> SmartDeleteRowsAsync(
        IEnumerable<int> rowIndices,
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = config ?? _currentConfig;
            _logger?.LogInformation("SmartDeleteRowsAsync called for {Count} rows", rowIndices.Count());

            // Convert public config to internal
            var internalConfig = MapToInternalConfig(cfg);

            // Create internal command
            var currentRowCount = _rowStore.GetRowCount();
            var command = SmartDeleteRowsInternalCommand.Create(
                rowIndices.ToList(),
                internalConfig,
                currentRowCount);

            // Execute via internal service
            var result = await _smartOperationService.SmartDeleteRowsAsync(command, cancellationToken);

            // Trigger UI refresh if needed
            await TriggerUIRefreshIfNeededAsync("SmartDeleteRows", result.ProcessedRows);

            // Convert result to public
            return MapToPublicResult(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SmartDeleteRowsAsync failed");
            return PublicSmartOperationResult.Failure(
                $"Smart delete failed: {ex.Message}",
                TimeSpan.Zero,
                new[] { ex.Message });
        }
    }

    public async Task<PublicSmartOperationResult> SmartDeleteRowAsync(
        int rowIndex,
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        return await SmartDeleteRowsAsync(new[] { rowIndex }, config, cancellationToken);
    }

    public async Task<PublicSmartOperationResult> SmartAddRowsAsync(
        IEnumerable<IReadOnlyDictionary<string, object?>> rowsData,
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = config ?? _currentConfig;
            _logger?.LogInformation("SmartAddRowsAsync called for {Count} rows", rowsData.Count());

            // Convert public config to internal
            var internalConfig = MapToInternalConfig(cfg);

            // Create internal command
            var command = SmartAddRowsInternalCommand.Create(rowsData, internalConfig);

            // Execute via internal service
            var result = await _smartOperationService.SmartAddRowsAsync(command, cancellationToken);

            // Trigger UI refresh if needed
            await TriggerUIRefreshIfNeededAsync("SmartAddRows", result.ProcessedRows);

            // Convert result to public
            return MapToPublicResult(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SmartAddRowsAsync failed");
            return PublicSmartOperationResult.Failure(
                $"Smart add failed: {ex.Message}",
                TimeSpan.Zero,
                new[] { ex.Message });
        }
    }

    public async Task<PublicSmartOperationResult> AutoExpandEmptyRowAsync(
        PublicSmartOperationsConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = config ?? _currentConfig;
            _logger?.LogInformation("AutoExpandEmptyRowAsync called");

            // Convert public config to internal
            var internalConfig = MapToInternalConfig(cfg);

            // Create internal command
            var currentRowCount = _rowStore.GetRowCount();
            var command = AutoExpandEmptyRowInternalCommand.Create(internalConfig, currentRowCount);

            // Execute via internal service
            var result = await _smartOperationService.AutoExpandEmptyRowAsync(command, cancellationToken);

            // Trigger UI refresh if needed
            await TriggerUIRefreshIfNeededAsync("AutoExpandEmptyRow", result.ProcessedRows);

            // Convert result to public
            return MapToPublicResult(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AutoExpandEmptyRowAsync failed");
            return PublicSmartOperationResult.Failure(
                $"Auto-expand failed: {ex.Message}",
                TimeSpan.Zero,
                new[] { ex.Message });
        }
    }

    public PublicSmartOperationsConfig GetCurrentConfig()
    {
        return _currentConfig;
    }

    public Task<PublicResult> UpdateConfigAsync(PublicSmartOperationsConfig config)
    {
        try
        {
            _logger?.LogInformation("Updating smart operations config: MinRows={MinRows}", config.MinimumRows);
            _currentConfig = config;
            return Task.FromResult(PublicResult.Success());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UpdateConfigAsync failed");
            return Task.FromResult(PublicResult.Failure($"Config update failed: {ex.Message}"));
        }
    }

    #region Private Mapping Methods

    private RowManagementConfiguration MapToInternalConfig(PublicSmartOperationsConfig publicConfig)
    {
        return new RowManagementConfiguration
        {
            MinimumRows = publicConfig.MinimumRows,
            EnableAutoExpand = publicConfig.EnableAutoExpand,
            EnableSmartDelete = publicConfig.EnableSmartDelete,
            AlwaysKeepLastEmpty = publicConfig.AlwaysKeepLastEmpty
        };
    }

    private PublicSmartOperationResult MapToPublicResult(RowManagementResult internalResult)
    {
        if (!internalResult.Success)
        {
            return PublicSmartOperationResult.Failure(
                string.Join(", ", internalResult.Messages),
                internalResult.OperationTime,
                internalResult.Messages);
        }

        var publicStats = new PublicSmartOperationStatistics
        {
            EmptyRowsCreated = internalResult.Statistics.EmptyRowsCreated,
            RowsPhysicallyDeleted = internalResult.Statistics.RowsPhysicallyDeleted,
            RowsContentCleared = internalResult.Statistics.RowsContentCleared,
            RowsShifted = internalResult.Statistics.RowsShifted,
            MinimumRowsEnforced = internalResult.Statistics.MinimumRowsEnforced,
            LastEmptyRowMaintained = internalResult.Statistics.LastEmptyRowMaintained
        };

        return PublicSmartOperationResult.Success(
            internalResult.FinalRowCount,
            internalResult.ProcessedRows,
            internalResult.OperationTime,
            publicStats);
    }

    private async Task TriggerUIRefreshIfNeededAsync(string operationType, int affectedRows)
    {
        // Automatic refresh ONLY in Interactive mode
        if (_options.OperationMode == PublicDataGridOperationMode.Interactive && _uiNotificationService != null)
        {
            await _uiNotificationService.NotifyDataRefreshAsync(affectedRows, operationType);
        }
        // In Readonly/Headless mode → skip (automatic refresh is disabled)
    }

    #endregion
}

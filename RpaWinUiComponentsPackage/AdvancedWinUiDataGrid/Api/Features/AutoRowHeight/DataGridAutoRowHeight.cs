using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.AutoRowHeight.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.AutoRowHeight;

/// <summary>
/// Internal implementation of DataGrid auto row height operations.
/// Delegates to internal auto row height service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridAutoRowHeight : IDataGridAutoRowHeight
{
    private readonly ILogger<DataGridAutoRowHeight>? _logger;
    private readonly IAutoRowHeightService _autoRowHeightService;

    public DataGridAutoRowHeight(
        IAutoRowHeightService autoRowHeightService,
        ILogger<DataGridAutoRowHeight>? logger = null)
    {
        _autoRowHeightService = autoRowHeightService ?? throw new ArgumentNullException(nameof(autoRowHeightService));
        _logger = logger;
    }

    public async Task<PublicResult> EnableAutoRowHeightAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Enabling auto row height via AutoRowHeight module");

            var internalResult = await _autoRowHeightService.EnableAutoRowHeightAsync(cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "EnableAutoRowHeight failed in AutoRowHeight module");
            throw;
        }
    }

    public async Task<PublicResult> DisableAutoRowHeightAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Disabling auto row height via AutoRowHeight module");

            var internalResult = await _autoRowHeightService.DisableAutoRowHeightAsync(cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DisableAutoRowHeight failed in AutoRowHeight module");
            throw;
        }
    }

    public async Task<PublicResult<double>> AdjustRowHeightAsync(int rowIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Adjusting row height for row {RowIndex} via AutoRowHeight module", rowIndex);

            var internalResult = await _autoRowHeightService.AdjustRowHeightAsync(rowIndex, cancellationToken);
            return new PublicResult<double>
            {
                IsSuccess = internalResult.IsSuccess,
                ErrorMessage = internalResult.ErrorMessage,
                Value = internalResult.Value
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AdjustRowHeight failed in AutoRowHeight module");
            throw;
        }
    }

    public async Task<PublicResult> AdjustAllRowHeightsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Adjusting all row heights via AutoRowHeight module");

            var internalResult = await _autoRowHeightService.AdjustAllRowHeightsAsync(cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "AdjustAllRowHeights failed in AutoRowHeight module");
            throw;
        }
    }

    public PublicResult SetMinRowHeight(double minHeight)
    {
        try
        {
            _logger?.LogInformation("Setting minimum row height to {MinHeight} via AutoRowHeight module", minHeight);

            var internalResult = _autoRowHeightService.SetMinRowHeight(minHeight);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetMinRowHeight failed in AutoRowHeight module");
            throw;
        }
    }

    public PublicResult SetMaxRowHeight(double maxHeight)
    {
        try
        {
            _logger?.LogInformation("Setting maximum row height to {MaxHeight} via AutoRowHeight module", maxHeight);

            var internalResult = _autoRowHeightService.SetMaxRowHeight(maxHeight);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SetMaxRowHeight failed in AutoRowHeight module");
            throw;
        }
    }

    public bool IsAutoRowHeightEnabled()
    {
        try
        {
            return _autoRowHeightService.IsAutoRowHeightEnabled();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "IsAutoRowHeightEnabled check failed in AutoRowHeight module");
            throw;
        }
    }

    public double GetMinRowHeight()
    {
        try
        {
            return _autoRowHeightService.GetMinRowHeight();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetMinRowHeight failed in AutoRowHeight module");
            throw;
        }
    }

    public double GetMaxRowHeight()
    {
        try
        {
            return _autoRowHeightService.GetMaxRowHeight();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetMaxRowHeight failed in AutoRowHeight module");
            throw;
        }
    }
}

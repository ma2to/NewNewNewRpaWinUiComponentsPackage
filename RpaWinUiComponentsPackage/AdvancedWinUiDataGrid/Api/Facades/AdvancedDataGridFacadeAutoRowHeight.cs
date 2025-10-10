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
/// Partial class containing AutoRowHeight Implementation
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region AutoRowHeight Implementation

    public async Task<PublicAutoRowHeightResult> EnableAutoRowHeightAsync(
        PublicAutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogInformation("Enabling auto row height with configuration: MinHeight={MinHeight}, MaxHeight={MaxHeight}",
                configuration.MinimumRowHeight, configuration.MaximumRowHeight);

            var internalConfig = configuration.ToInternal();
            var internalResult = await autoRowHeightService.EnableAutoRowHeightAsync(internalConfig, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable auto row height");
            return new PublicAutoRowHeightResult(false, ex.Message, null, null);
        }
    }

    public async Task<IReadOnlyList<PublicRowHeightCalculationResult>> CalculateOptimalRowHeightsAsync(
        IProgress<PublicBatchCalculationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogInformation("Starting optimal row height calculation");

            // Create progress wrapper if provided
            IProgress<Features.AutoRowHeight.Interfaces.BatchCalculationProgress>? internalProgress = null;
            if (progress != null)
            {
                internalProgress = new Progress<Features.AutoRowHeight.Interfaces.BatchCalculationProgress>(p =>
                {
                    progress.Report(new PublicBatchCalculationProgress(
                        p.ProcessedRows,
                        p.TotalRows,
                        p.ElapsedTime.TotalMilliseconds,
                        p.CurrentOperation
                    ));
                });
            }

            var internalResults = await autoRowHeightService.CalculateOptimalRowHeightsAsync(internalProgress, cancellationToken);
            return internalResults.ToPublicList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate optimal row heights");
            return Array.Empty<PublicRowHeightCalculationResult>();
        }
    }

    public async Task<PublicRowHeightCalculationResult> CalculateRowHeightAsync(
        int rowIndex,
        IReadOnlyDictionary<string, object?> rowData,
        PublicRowHeightCalculationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogDebug("Calculating row height for row {RowIndex}", rowIndex);

            var internalOptions = options.ToInternal();
            var internalResult = await autoRowHeightService.CalculateRowHeightAsync(rowIndex, rowData, internalOptions, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate row height for row {RowIndex}", rowIndex);
            return new PublicRowHeightCalculationResult(rowIndex, 0, false, ex.Message, null);
        }
    }

    public async Task<PublicTextMeasurementResult> MeasureTextAsync(
        string text,
        string fontFamily,
        double fontSize,
        double maxWidth,
        bool textWrapping = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogDebug("Measuring text: length={TextLength}, font={FontFamily}, size={FontSize}",
                text?.Length ?? 0, fontFamily, fontSize);

            var internalResult = await autoRowHeightService.MeasureTextAsync(text ?? string.Empty, fontFamily, fontSize, maxWidth, textWrapping, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to measure text");
            // Return fallback measurement
            return new PublicTextMeasurementResult(maxWidth, fontSize * 1.5, text ?? string.Empty, fontFamily, fontSize, false);
        }
    }

    public async Task<PublicAutoRowHeightResult> ApplyAutoRowHeightConfigurationAsync(
        PublicAutoRowHeightConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogInformation("Applying auto row height configuration: MinHeight={MinHeight}, MaxHeight={MaxHeight}",
                configuration.MinimumRowHeight, configuration.MaximumRowHeight);

            var internalConfig = configuration.ToInternal();
            var internalResult = await autoRowHeightService.ApplyConfigurationAsync(internalConfig, cancellationToken);
            return internalResult.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply auto row height configuration");
            return new PublicAutoRowHeightResult(false, ex.Message, null, null);
        }
    }

    public async Task<bool> InvalidateHeightCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            _logger.LogInformation("Invalidating auto row height cache");

            return await autoRowHeightService.InvalidateHeightCacheAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate height cache");
            return false;
        }
    }

    public PublicAutoRowHeightStatistics GetAutoRowHeightStatistics()
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            var internalStats = autoRowHeightService.GetStatistics();
            return internalStats.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get auto row height statistics");
            return new PublicAutoRowHeightStatistics(0, 0, 0, 0, 0, 0, 0);
        }
    }

    public PublicCacheStatistics GetCacheStatistics()
    {
        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var autoRowHeightService = scope.ServiceProvider.GetRequiredService<IAutoRowHeightService>();

            var internalStats = autoRowHeightService.GetCacheStatistics();
            return internalStats.ToPublic();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache statistics");
            return new PublicCacheStatistics(0, 0, 0, 0, 0, 0, 0);
        }
    }

    #endregion
}


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
/// Partial class containing Color Operations
/// </summary>
public sealed partial class AdvancedDataGridFacade : IAdvancedDataGridFacade
{
    #region Color Operations

    public async Task<ColorDataResult> ApplyColorAsync(ApplyColorDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var colorService = scope.ServiceProvider.GetRequiredService<Features.Color.Interfaces.IColorService>();

            var colorConfig = new Core.ValueObjects.ColorConfiguration
            {
                Mode = (Core.ValueObjects.ColorMode)command.Mode,
                BackgroundColor = command.BackgroundColor,
                ForegroundColor = command.ForegroundColor,
                RowIndex = command.RowIndex,
                ColumnIndex = command.ColumnIndex,
                ColumnName = command.ColumnName
            };

            var applyCommand = Features.Color.Commands.ApplyColorCommand.Create(colorConfig);
            var result = await colorService.ApplyColorAsync(applyCommand, cancellationToken);

            return new ColorDataResult(result.Success, result.AffectedCells, result.Duration, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply color");
            return new ColorDataResult(false, 0, TimeSpan.Zero, ex.Message);
        }
    }

    public async Task<ColorDataResult> ApplyConditionalFormattingAsync(ApplyConditionalFormattingDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var colorService = scope.ServiceProvider.GetRequiredService<Features.Color.Interfaces.IColorService>();

            var rules = command.Rules.Select(r => new Core.ValueObjects.ConditionalFormatRule
            {
                ColumnName = r.ColumnName,
                Rule = (Core.ValueObjects.ConditionalFormattingRule)r.Rule,
                Value = r.Value,
                ColorConfig = new Core.ValueObjects.ColorConfiguration
                {
                    BackgroundColor = r.BackgroundColor,
                    ForegroundColor = r.ForegroundColor
                }
            }).ToList();

            var applyCommand = Features.Color.Commands.ApplyConditionalFormattingCommand.Create(rules);
            var result = await colorService.ApplyConditionalFormattingAsync(applyCommand, cancellationToken);

            return new ColorDataResult(result.Success, result.AffectedCells, result.Duration, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply conditional formatting");
            return new ColorDataResult(false, 0, TimeSpan.Zero, ex.Message);
        }
    }

    public async Task<ColorDataResult> ClearColorAsync(ClearColorDataCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            using var scope = ServiceRegistration.CreateOperationScope(_serviceProvider);
            var colorService = scope.ServiceProvider.GetRequiredService<Features.Color.Interfaces.IColorService>();

            var clearCommand = new Features.Color.Commands.ClearColorCommand
            {
                Mode = (Core.ValueObjects.ColorMode)command.Mode,
                RowIndex = command.RowIndex,
                ColumnIndex = command.ColumnIndex,
                ColumnName = command.ColumnName
            };

            var result = await colorService.ClearColorAsync(clearCommand, cancellationToken);

            return new ColorDataResult(result.Success, result.AffectedCells, result.Duration, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear color");
            return new ColorDataResult(false, 0, TimeSpan.Zero, ex.Message);
        }
    }

    #endregion
}


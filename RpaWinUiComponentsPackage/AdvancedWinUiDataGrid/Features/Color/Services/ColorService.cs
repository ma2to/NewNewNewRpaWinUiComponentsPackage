using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Commands;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Color.Services;

/// <summary>
/// Service for color management with conditional formatting support
/// </summary>
internal sealed class ColorService : IColorService
{
    private readonly ILogger<ColorService> _logger;
    private readonly ConcurrentDictionary<string, ColorConfiguration> _colorMappings = new();
    private readonly ConcurrentDictionary<string, ConditionalFormatRule> _conditionalRules = new();

    public ColorService(ILogger<ColorService> logger)
    {
        _logger = logger;
    }

    public async Task<ColorResult> ApplyColorAsync(ApplyColorCommand command, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var key = GenerateKey(command.ColorConfig);
            _colorMappings[key] = command.ColorConfig;

            _logger.LogInformation("Color applied: mode={Mode}, affectedCells={Count}",
                command.ColorConfig.Mode, 1);

            sw.Stop();
            return ColorResult.CreateSuccess(1, sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply color");
            return ColorResult.CreateFailure(ex.Message);
        }
    }

    public async Task<ColorResult> ApplyConditionalFormattingAsync(ApplyConditionalFormattingCommand command, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var affectedCells = 0;
            foreach (var rule in command.Rules)
            {
                var key = $"conditional_{rule.ColumnName}_{rule.Rule}";
                _conditionalRules[key] = rule;
                affectedCells++;
            }

            _logger.LogInformation("Conditional formatting applied: rules={RuleCount}", command.Rules.Count);

            sw.Stop();
            return ColorResult.CreateSuccess(affectedCells, sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply conditional formatting");
            return ColorResult.CreateFailure(ex.Message);
        }
    }

    public async Task<ColorResult> ClearColorAsync(ClearColorCommand command, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var cleared = 0;
            var keysToRemove = new List<string>();

            foreach (var kvp in _colorMappings)
            {
                var shouldRemove = command.Mode switch
                {
                    ColorMode.Cell => kvp.Value.RowIndex == command.RowIndex && kvp.Value.ColumnIndex == command.ColumnIndex,
                    ColorMode.Row => kvp.Value.RowIndex == command.RowIndex,
                    ColorMode.Column => kvp.Value.ColumnName == command.ColumnName,
                    _ => false
                };

                if (shouldRemove)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _colorMappings.TryRemove(key, out _);
                cleared++;
            }

            _logger.LogInformation("Color cleared: mode={Mode}, cleared={Count}", command.Mode, cleared);

            sw.Stop();
            return ColorResult.CreateSuccess(cleared, sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear color");
            return ColorResult.CreateFailure(ex.Message);
        }
    }

    public IReadOnlyDictionary<string, ColorConfiguration> GetColoredCells()
    {
        return _colorMappings;
    }

    public async Task<Result> ValidateColorConfigurationAsync(ColorConfiguration config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(config.BackgroundColor) && string.IsNullOrEmpty(config.ForegroundColor))
        {
            return Result.Failure("At least one color must be specified");
        }

        return Result.Success();
    }

    private string GenerateKey(ColorConfiguration config)
    {
        return config.Mode switch
        {
            ColorMode.Cell => $"cell_{config.RowIndex}_{config.ColumnIndex}",
            ColorMode.Row => $"row_{config.RowIndex}",
            ColorMode.Column => $"column_{config.ColumnName}",
            _ => Guid.NewGuid().ToString()
        };
    }
}

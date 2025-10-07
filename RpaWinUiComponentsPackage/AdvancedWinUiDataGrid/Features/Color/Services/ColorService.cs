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
    private readonly ConcurrentDictionary<string, string> _elementStateColors = new();
    private bool _zebraRowsEnabled = false;
    private string _evenRowColor = "#FFFFFF";
    private string _oddRowColor = "#F9F9F9";

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

    public async Task SetElementStatePropertyColorAsync(string element, string state, string property, string color, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var key = $"{element}_{state}_{property}";
            _elementStateColors[key] = color;
            _logger.LogInformation("Set color for {Element}.{State}.{Property} = {Color}", element, state, property, color);
        }, cancellationToken);
    }

    public async Task<string> GetElementStatePropertyColorAsync(string element, string state, string property, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var key = $"{element}_{state}_{property}";
            if (_elementStateColors.TryGetValue(key, out var color))
            {
                return color;
            }
            return GetDefaultColorAsync(element, state, property, cancellationToken).Result;
        }, cancellationToken);
    }

    public async Task ClearElementStatePropertyColorAsync(string element, string state, string property, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            var key = $"{element}_{state}_{property}";
            _elementStateColors.TryRemove(key, out _);
            _logger.LogInformation("Cleared color for {Element}.{State}.{Property}", element, state, property);
        }, cancellationToken);
    }

    public async Task<string> GetDefaultColorAsync(string element, string state, string property, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            // Default colors based on element, state, property
            return (element, state, property) switch
            {
                ("Cell", "Normal", "BackgroundColor") => "#FFFFFF",
                ("Cell", "Normal", "TextColor") => "#000000",
                ("Cell", "Selected", "BackgroundColor") => "#0078D4",
                ("Cell", "Selected", "TextColor") => "#FFFFFF",
                ("Cell", "ValidationFailed", "BackgroundColor") => "#FFEBEE",
                ("Cell", "ValidationFailed", "TextColor") => "#D32F2F",
                ("Cell", "ValidationFailed", "BorderColor") => "#F44336",
                ("Row", "Normal", "BackgroundColor") => "#FFFFFF",
                ("RowAlternate", "Normal", "BackgroundColor") => "#F9F9F9",
                ("Header", "Normal", "BackgroundColor") => "#F5F5F5",
                ("Header", "Normal", "TextColor") => "#000000",
                _ => "#FFFFFF"
            };
        }, cancellationToken);
    }

    public async Task EnableZebraRowsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _zebraRowsEnabled = true;
            _logger.LogInformation("Zebra rows enabled");
        }, cancellationToken);
    }

    public async Task DisableZebraRowsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _zebraRowsEnabled = false;
            _logger.LogInformation("Zebra rows disabled");
        }, cancellationToken);
    }

    public bool IsZebraRowsEnabled()
    {
        return _zebraRowsEnabled;
    }

    public async Task SetZebraRowColorsAsync(string evenRowColor, string oddRowColor, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _evenRowColor = evenRowColor;
            _oddRowColor = oddRowColor;
            _logger.LogInformation("Zebra row colors set: even={EvenColor}, odd={OddColor}", evenRowColor, oddRowColor);
        }, cancellationToken);
    }

    public (string evenRowColor, string oddRowColor) GetZebraRowColors()
    {
        return (_evenRowColor, _oddRowColor);
    }

    public async Task ResetZebraRowColorsToDefaultAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _evenRowColor = "#FFFFFF";
            _oddRowColor = "#F9F9F9";
            _logger.LogInformation("Zebra row colors reset to default");
        }, cancellationToken);
    }

    // Old API compatibility methods
    public async Task SetCellBackgroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default)
    {
        await SetElementStatePropertyColorAsync($"Cell_{rowIndex}_{columnName}", "Normal", "BackgroundColor", color, cancellationToken);
    }

    public async Task SetCellForegroundColorAsync(int rowIndex, string columnName, string color, CancellationToken cancellationToken = default)
    {
        await SetElementStatePropertyColorAsync($"Cell_{rowIndex}_{columnName}", "Normal", "TextColor", color, cancellationToken);
    }

    public async Task SetRowBackgroundColorAsync(int rowIndex, string color, CancellationToken cancellationToken = default)
    {
        await SetElementStatePropertyColorAsync($"Row_{rowIndex}", "Normal", "BackgroundColor", color, cancellationToken);
    }

    public async Task ClearCellColorsAsync(int rowIndex, string columnName, CancellationToken cancellationToken = default)
    {
        await ClearElementStatePropertyColorAsync($"Cell_{rowIndex}_{columnName}", "Normal", "BackgroundColor", cancellationToken);
        await ClearElementStatePropertyColorAsync($"Cell_{rowIndex}_{columnName}", "Normal", "TextColor", cancellationToken);
    }

    public async Task ClearAllColorsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _elementStateColors.Clear();
            _logger.LogInformation("Cleared all element state colors");
        }, cancellationToken);
    }
}

using Microsoft.Extensions.Logging;
using System.Text.Json;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Services;

/// <summary>
/// Internal service for DataGrid configuration management
/// </summary>
internal sealed class DataGridConfigurationService : IDataGridConfiguration
{
    private readonly ILogger<DataGridConfigurationService>? _logger;
    private PublicDataGridConfiguration _currentConfiguration;
    private readonly Dictionary<string, PublicDataGridConfiguration> _presets;

    public DataGridConfigurationService(ILogger<DataGridConfigurationService>? logger = null)
    {
        _logger = logger;
        _currentConfiguration = PublicDataGridConfiguration.Default;
        _presets = new Dictionary<string, PublicDataGridConfiguration>();
    }

    public async Task ApplyConfigurationAsync(PublicDataGridConfiguration config, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _currentConfiguration = config ?? throw new ArgumentNullException(nameof(config));
            _logger?.LogInformation("Applied configuration");
        }, cancellationToken);
    }

    public async Task SaveConfigurationPresetAsync(string presetName, PublicDataGridConfiguration config, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _presets[presetName] = config ?? throw new ArgumentNullException(nameof(config));
            _logger?.LogInformation("Saved configuration preset '{PresetName}'", presetName);
        }, cancellationToken);
    }

    public async Task<PublicDataGridConfiguration> LoadConfigurationPresetAsync(string presetName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (_presets.TryGetValue(presetName, out var config))
            {
                _currentConfiguration = config;
                _logger?.LogInformation("Loaded configuration preset '{PresetName}'", presetName);
                return config;
            }
            throw new InvalidOperationException($"Preset '{presetName}' not found");
        }, cancellationToken);
    }

    public PublicDataGridConfiguration GetCurrentConfiguration()
    {
        return _currentConfiguration;
    }

    public async Task ResetToDefaultConfigurationAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _currentConfiguration = PublicDataGridConfiguration.Default;
            _logger?.LogInformation("Reset to default configuration");
        }, cancellationToken);
    }

    public async Task ExportConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(_currentConfiguration, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger?.LogInformation("Exported configuration to '{FilePath}'", filePath);
    }

    public async Task<PublicDataGridConfiguration> ImportConfigurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var config = JsonSerializer.Deserialize<PublicDataGridConfiguration>(json);
        if (config != null)
        {
            _currentConfiguration = config;
            _logger?.LogInformation("Imported configuration from '{FilePath}'", filePath);
            return config;
        }
        throw new InvalidOperationException($"Failed to import configuration from '{filePath}'");
    }

    public IReadOnlyList<string> GetAvailablePresets()
    {
        return _presets.Keys.ToList();
    }

    public async Task DeleteConfigurationPresetAsync(string presetName, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (_presets.Remove(presetName))
            {
                _logger?.LogInformation("Deleted configuration preset '{PresetName}'", presetName);
            }
            else
            {
                throw new InvalidOperationException($"Preset '{presetName}' not found");
            }
        }, cancellationToken);
    }
}

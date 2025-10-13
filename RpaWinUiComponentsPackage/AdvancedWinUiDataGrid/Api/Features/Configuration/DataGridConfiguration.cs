using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Api.Mappings;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Interfaces;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

/// <summary>
/// Internal implementation of DataGrid configuration operations.
/// Delegates to internal configuration service and provides mapping between public and internal models.
/// </summary>
internal sealed class DataGridConfiguration : IDataGridConfiguration
{
    private readonly ILogger<DataGridConfiguration>? _logger;
    private readonly Features.Configuration.Interfaces.IDataGridConfiguration _configurationService;

    public DataGridConfiguration(
        Features.Configuration.Interfaces.IDataGridConfiguration configurationService,
        ILogger<DataGridConfiguration>? logger = null)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger;
    }

    public async Task<PublicResult> SaveConfigurationPresetAsync(string presetName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Saving configuration preset '{PresetName}' via Configuration module", presetName);

            var currentConfig = _configurationService.GetCurrentConfiguration();
            await _configurationService.SaveConfigurationPresetAsync(presetName, currentConfig, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SaveConfigurationPreset failed in Configuration module");
            return PublicResult.Failure($"Save failed: {ex.Message}");
        }
    }

    public async Task<PublicResult> LoadConfigurationPresetAsync(string presetName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Loading configuration preset '{PresetName}' via Configuration module", presetName);

            var internalConfig = await _configurationService.LoadConfigurationPresetAsync(presetName, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "LoadConfigurationPreset failed in Configuration module");
            return PublicResult.Failure($"Load failed: {ex.Message}");
        }
    }

    public IReadOnlyList<string> GetAvailablePresets()
    {
        try
        {
            return _configurationService.GetAvailablePresets();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetAvailablePresets failed in Configuration module");
            return Array.Empty<string>();
        }
    }

    public async Task<PublicResult> DeleteConfigurationPresetAsync(string presetName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Deleting configuration preset '{PresetName}' via Configuration module", presetName);

            await _configurationService.DeleteConfigurationPresetAsync(presetName, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DeleteConfigurationPreset failed in Configuration module");
            return PublicResult.Failure($"Delete failed: {ex.Message}");
        }
    }

    public async Task<PublicResult<string>> ExportConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Exporting configuration via Configuration module");

            var json = await _configurationService.ExportConfigurationAsJsonAsync(cancellationToken);
            return PublicResult<string>.Success(json);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExportConfiguration failed in Configuration module");
            return PublicResult<string>.Failure($"Export failed: {ex.Message}");
        }
    }

    public async Task<PublicResult> ImportConfigurationAsync(string jsonConfig, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Importing configuration via Configuration module");

            var config = await _configurationService.ImportConfigurationFromJsonAsync(jsonConfig, cancellationToken);
            await _configurationService.ApplyConfigurationAsync(config, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ImportConfiguration failed in Configuration module");
            return PublicResult.Failure($"Import failed: {ex.Message}");
        }
    }

    public PublicDataGridConfiguration GetCurrentConfiguration()
    {
        try
        {
            return _configurationService.GetCurrentConfiguration();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetCurrentConfiguration failed in Configuration module");
            return PublicDataGridConfiguration.Default;
        }
    }

    public async Task<PublicResult> ApplyConfigurationAsync(PublicDataGridConfiguration configuration, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Applying configuration via Configuration module");

            await _configurationService.ApplyConfigurationAsync(configuration, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ApplyConfiguration failed in Configuration module");
            return PublicResult.Failure($"Apply failed: {ex.Message}");
        }
    }

    public async Task<PublicResult> ResetToDefaultConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Resetting to default configuration via Configuration module");

            await _configurationService.ResetToDefaultConfigurationAsync(cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ResetToDefaultConfiguration failed in Configuration module");
            return PublicResult.Failure($"Reset failed: {ex.Message}");
        }
    }
}

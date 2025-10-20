using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Environments;

/// <summary>
/// Internal implementation of Environment Configuration operations.
/// Delegates to internal environment configuration service and provides mapping between public and internal models.
/// Acts as adapter/facade between public API and internal implementation.
/// </summary>
internal sealed class EnvironmentConfiguration : IEnvironmentConfiguration
{
    private readonly ILogger<EnvironmentConfiguration> _logger;
    private readonly Features.Configuration.Interfaces.IEnvironmentConfiguration _environmentService;

    public EnvironmentConfiguration(
        Features.Configuration.Interfaces.IEnvironmentConfiguration environmentService,
        ILogger<EnvironmentConfiguration>? logger = null)
    {
        _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
        _logger = logger ?? NullLogger<EnvironmentConfiguration>.Instance;
    }

    public async Task<PublicResult> RegisterEnvironmentsAsync(
        IReadOnlyList<PublicEnvironmentConfig> environments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Registering {EnvironmentCount} environments via Environment module",
                environments.Count);

            // Convert public models to internal models
            var internalEnvironments = new Dictionary<string, EnvironmentConfig>();
            foreach (var publicEnv in environments)
            {
                internalEnvironments[publicEnv.Name] = MapToInternal(publicEnv);
            }

            var container = new EnvironmentConfigurationContainer
            {
                Environments = internalEnvironments,
                ActiveEnvironmentName = null
            };

            await _environmentService.RegisterEnvironmentsAsync(container, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RegisterEnvironments failed in Environment module");
            return PublicResult.Failure($"Registration failed: {ex.Message}");
        }
    }

    public async Task<PublicResult<PublicEnvironmentConfig>> LoadEnvironmentConfigAsync(
        string environmentName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading environment '{EnvironmentName}' via Environment module", environmentName);

            var internalConfig = await _environmentService.LoadEnvironmentConfigAsync(environmentName, cancellationToken);
            var publicConfig = MapToPublic(internalConfig);

            return PublicResult<PublicEnvironmentConfig>.Success(publicConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoadEnvironmentConfig failed in Environment module");
            return PublicResult<PublicEnvironmentConfig>.Failure($"Load failed: {ex.Message}");
        }
    }

    public async Task<PublicResult<PublicEnvironmentConfig>> DetectAndLoadEnvironmentAsync(
        IEnumerable<PublicEnvironmentDetectionRule> detectionRules,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Detecting and loading environment via Environment module");

            // Convert public detection rules to internal
            var internalRules = detectionRules.Select(r => new EnvironmentDetectionRule
            {
                EnvironmentName = r.EnvironmentName,
                Condition = r.Condition
            });

            var internalConfig = await _environmentService.DetectAndLoadEnvironmentAsync(internalRules, cancellationToken);
            var publicConfig = MapToPublic(internalConfig);

            return PublicResult<PublicEnvironmentConfig>.Success(publicConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DetectAndLoadEnvironment failed in Environment module");
            return PublicResult<PublicEnvironmentConfig>.Failure($"Auto-detection failed: {ex.Message}");
        }
    }

    public async Task<PublicResult> ExportEnvironmentConfigAsync(
        string environmentName,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting environment '{EnvironmentName}' to '{FilePath}' via Environment module",
                environmentName, filePath);

            await _environmentService.ExportEnvironmentConfigAsync(environmentName, filePath, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportEnvironmentConfig failed in Environment module");
            return PublicResult.Failure($"Export failed: {ex.Message}");
        }
    }

    public async Task<PublicResult<string>> ExportEnvironmentConfigAsJsonAsync(
        string environmentName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting environment '{EnvironmentName}' as JSON via Environment module",
                environmentName);

            var json = await _environmentService.ExportEnvironmentConfigAsJsonAsync(environmentName, cancellationToken);
            return PublicResult<string>.Success(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportEnvironmentConfigAsJson failed in Environment module");
            return PublicResult<string>.Failure($"Export failed: {ex.Message}");
        }
    }

    public async Task<PublicResult<PublicEnvironmentConfig>> ImportEnvironmentConfigAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Importing environment from '{FilePath}' via Environment module", filePath);

            var internalConfig = await _environmentService.ImportEnvironmentConfigAsync(filePath, cancellationToken);
            var publicConfig = MapToPublic(internalConfig);

            return PublicResult<PublicEnvironmentConfig>.Success(publicConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ImportEnvironmentConfig failed in Environment module");
            return PublicResult<PublicEnvironmentConfig>.Failure($"Import failed: {ex.Message}");
        }
    }

    public async Task<PublicResult<PublicEnvironmentConfig>> ImportEnvironmentConfigFromJsonAsync(
        string jsonConfig,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Importing environment from JSON via Environment module");

            var internalConfig = await _environmentService.ImportEnvironmentConfigFromJsonAsync(jsonConfig, cancellationToken);
            var publicConfig = MapToPublic(internalConfig);

            return PublicResult<PublicEnvironmentConfig>.Success(publicConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ImportEnvironmentConfigFromJson failed in Environment module");
            return PublicResult<PublicEnvironmentConfig>.Failure($"Import failed: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting available environments via Environment module");
            return await _environmentService.GetAvailableEnvironmentsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAvailableEnvironments failed in Environment module");
            return Array.Empty<string>();
        }
    }

    public async Task<PublicResult> SetActiveEnvironmentAsync(
        string environmentName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Setting active environment to '{EnvironmentName}' via Environment module",
                environmentName);

            await _environmentService.SetActiveEnvironmentAsync(environmentName, cancellationToken);
            return PublicResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetActiveEnvironment failed in Environment module");
            return PublicResult.Failure($"Set active failed: {ex.Message}");
        }
    }

    public async Task<PublicResult<PublicEnvironmentConfig?>> LoadActiveEnvironmentAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading active environment via Environment module");

            var internalConfig = await _environmentService.LoadActiveEnvironmentAsync(cancellationToken);

            if (internalConfig == null)
            {
                return PublicResult<PublicEnvironmentConfig?>.Success(null);
            }

            var publicConfig = MapToPublic(internalConfig);
            return PublicResult<PublicEnvironmentConfig?>.Success(publicConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoadActiveEnvironment failed in Environment module");
            return PublicResult<PublicEnvironmentConfig?>.Failure($"Load active failed: {ex.Message}");
        }
    }

    public PublicEnvironmentConfig? GetActiveEnvironment()
    {
        try
        {
            var internalConfig = _environmentService.GetActiveEnvironment();

            if (internalConfig == null)
            {
                return null;
            }

            return MapToPublic(internalConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveEnvironment failed in Environment module");
            return null;
        }
    }

    public T? GetSetting<T>(string settingKey)
    {
        try
        {
            return _environmentService.GetSetting<T>(settingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSetting failed in Environment module for key '{SettingKey}'", settingKey);
            return default;
        }
    }

    // PRIVATE HELPER METHODS - Mapping between public and internal models

    private static EnvironmentConfig MapToInternal(PublicEnvironmentConfig publicConfig)
    {
        return new EnvironmentConfig
        {
            Name = publicConfig.Name,
            Settings = new Dictionary<string, object>(publicConfig.Settings),
            CreatedAt = publicConfig.CreatedAt,
            Author = publicConfig.Author,
            Description = publicConfig.Description
        };
    }

    private static PublicEnvironmentConfig MapToPublic(EnvironmentConfig internalConfig)
    {
        return new PublicEnvironmentConfig
        {
            Name = internalConfig.Name,
            Settings = internalConfig.Settings,
            CreatedAt = internalConfig.CreatedAt,
            Author = internalConfig.Author,
            Description = internalConfig.Description
        };
    }
}

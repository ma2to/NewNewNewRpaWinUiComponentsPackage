using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Services;

/// <summary>
/// Internal service for Environment Configuration management.
/// Manages environment-specific settings with persistent storage support.
/// Config is loaded at application startup and remains read-only during runtime (NO hot-reload).
/// Thread-safe implementation.
/// </summary>
internal sealed class EnvironmentConfigurationService : IEnvironmentConfiguration
{
    private readonly ILogger<EnvironmentConfigurationService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _persistentStoragePath;

    private EnvironmentConfigurationContainer? _container;
    private EnvironmentConfig? _activeEnvironment;

    public EnvironmentConfigurationService(ILogger<EnvironmentConfigurationService>? logger = null)
    {
        _logger = logger ?? NullLogger<EnvironmentConfigurationService>.Instance;

        // Persistent storage path: %TEMP%/AdvancedDataGrid/environment_config.json
        var tempPath = Path.GetTempPath();
        var appFolder = Path.Combine(tempPath, "AdvancedDataGrid");

        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
            _logger.LogDebug("Created AdvancedDataGrid config directory: {Path}", appFolder);
        }

        _persistentStoragePath = Path.Combine(appFolder, "environment_config.json");
        _logger.LogDebug("Environment config storage path: {Path}", _persistentStoragePath);
    }

    public async Task<EnvironmentConfig> LoadEnvironmentConfigAsync(
        string environmentName,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            EnsureContainerIsRegistered();

            if (!_container!.Environments.TryGetValue(environmentName, out var config))
            {
                _logger.LogError("Environment '{EnvironmentName}' not found", environmentName);
                throw new InvalidOperationException($"Environment '{environmentName}' not found");
            }

            _activeEnvironment = config;
            _logger.LogInformation("Loaded environment configuration: '{EnvironmentName}'", environmentName);

            // Persist active environment name to storage
            await SaveActiveEnvironmentNameAsync(environmentName, cancellationToken);

            return config;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<EnvironmentConfig> DetectAndLoadEnvironmentAsync(
        IEnumerable<EnvironmentDetectionRule> detectionRules,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            EnsureContainerIsRegistered();

            var rulesList = detectionRules.ToList();
            _logger.LogInformation("Detecting environment using {RuleCount} detection rules", rulesList.Count);

            foreach (var rule in rulesList)
            {
                try
                {
                    if (rule.Condition())
                    {
                        _logger.LogInformation("Detection rule matched: '{EnvironmentName}'", rule.EnvironmentName);

                        // Release lock before calling LoadEnvironmentConfigAsync (which acquires lock)
                        _lock.Release();
                        try
                        {
                            return await LoadEnvironmentConfigAsync(rule.EnvironmentName, cancellationToken);
                        }
                        finally
                        {
                            await _lock.WaitAsync(cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Detection rule for '{EnvironmentName}' failed: {Message}",
                        rule.EnvironmentName, ex.Message);
                }
            }

            _logger.LogError("No detection rule matched - unable to automatically detect environment");
            throw new InvalidOperationException("No detection rule matched - unable to automatically detect environment");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ExportEnvironmentConfigAsync(
        string environmentName,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            EnsureContainerIsRegistered();

            if (!_container!.Environments.TryGetValue(environmentName, out var config))
            {
                _logger.LogError("Environment '{EnvironmentName}' not found for export", environmentName);
                throw new InvalidOperationException($"Environment '{environmentName}' not found");
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogInformation("Exported environment '{EnvironmentName}' to '{FilePath}'",
                environmentName, filePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> ExportEnvironmentConfigAsJsonAsync(
        string environmentName,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            EnsureContainerIsRegistered();

            if (!_container!.Environments.TryGetValue(environmentName, out var config))
            {
                _logger.LogError("Environment '{EnvironmentName}' not found for export", environmentName);
                throw new InvalidOperationException($"Environment '{environmentName}' not found");
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Exported environment '{EnvironmentName}' as JSON string", environmentName);

            return json;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<EnvironmentConfig> ImportEnvironmentConfigAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return await ImportEnvironmentConfigFromJsonAsync(json, cancellationToken);
    }

    public async Task<EnvironmentConfig> ImportEnvironmentConfigFromJsonAsync(
        string jsonConfig,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            EnsureContainerIsRegistered();

            var config = JsonSerializer.Deserialize<EnvironmentConfig>(jsonConfig);
            if (config == null)
            {
                _logger.LogError("Failed to deserialize environment configuration JSON");
                throw new InvalidOperationException("Failed to deserialize environment configuration JSON");
            }

            // Add or overwrite environment in container
            _container = _container! with
            {
                Environments = new Dictionary<string, EnvironmentConfig>(_container.Environments)
                {
                    [config.Name] = config
                }
            };

            _logger.LogInformation("Imported environment '{EnvironmentName}'", config.Name);
            return config;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make it properly async
        EnsureContainerIsRegistered();
        return _container!.Environments.Keys.ToList();
    }

    public async Task SetActiveEnvironmentAsync(
        string environmentName,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            EnsureContainerIsRegistered();

            if (!_container!.Environments.ContainsKey(environmentName))
            {
                _logger.LogError("Environment '{EnvironmentName}' not found", environmentName);
                throw new InvalidOperationException($"Environment '{environmentName}' not found");
            }

            // Save to persistent storage (requires restart to take effect)
            await SaveActiveEnvironmentNameAsync(environmentName, cancellationToken);

            _logger.LogInformation(
                "Set active environment to '{EnvironmentName}' - restart required for changes to take effect",
                environmentName);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<EnvironmentConfig?> LoadActiveEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            EnsureContainerIsRegistered();

            var activeEnvironmentName = await LoadActiveEnvironmentNameAsync(cancellationToken);

            if (string.IsNullOrEmpty(activeEnvironmentName))
            {
                _logger.LogInformation("No active environment set");
                return null;
            }

            if (!_container!.Environments.TryGetValue(activeEnvironmentName, out var config))
            {
                _logger.LogWarning("Active environment '{EnvironmentName}' not found in registered environments",
                    activeEnvironmentName);
                return null;
            }

            _activeEnvironment = config;
            _logger.LogInformation("Loaded active environment: '{EnvironmentName}'", activeEnvironmentName);

            return config;
        }
        finally
        {
            _lock.Release();
        }
    }

    public EnvironmentConfig? GetActiveEnvironment()
    {
        return _activeEnvironment;
    }

    public async Task RegisterEnvironmentsAsync(
        EnvironmentConfigurationContainer container,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _logger.LogInformation("Registered {EnvironmentCount} environments: {EnvironmentNames}",
                container.Environments.Count,
                string.Join(", ", container.Environments.Keys));

            // If container has active environment set, load it
            if (!string.IsNullOrEmpty(container.ActiveEnvironmentName))
            {
                if (container.Environments.TryGetValue(container.ActiveEnvironmentName, out var config))
                {
                    _activeEnvironment = config;
                    _logger.LogInformation("Set active environment from container: '{EnvironmentName}'",
                        container.ActiveEnvironmentName);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public T? GetSetting<T>(string settingKey)
    {
        if (_activeEnvironment == null)
        {
            _logger.LogWarning("No active environment - cannot get setting '{SettingKey}'", settingKey);
            return default;
        }

        if (!_activeEnvironment.Settings.TryGetValue(settingKey, out var value))
        {
            _logger.LogDebug("Setting '{SettingKey}' not found in active environment '{EnvironmentName}'",
                settingKey, _activeEnvironment.Name);
            return default;
        }

        try
        {
            // Handle type conversion
            if (value is T typedValue)
            {
                return typedValue;
            }

            // Try to convert using JsonSerializer (handles complex types and type conversions)
            var json = JsonSerializer.Serialize(value);
            var result = JsonSerializer.Deserialize<T>(json);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert setting '{SettingKey}' to type {TargetType}",
                settingKey, typeof(T).Name);
            return default;
        }
    }

    // PRIVATE HELPER METHODS

    private void EnsureContainerIsRegistered()
    {
        if (_container == null)
        {
            throw new InvalidOperationException(
                "Environment configuration container not registered - call RegisterEnvironmentsAsync first");
        }
    }

    private async Task SaveActiveEnvironmentNameAsync(string environmentName, CancellationToken cancellationToken)
    {
        try
        {
            var data = new { ActiveEnvironmentName = environmentName };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_persistentStoragePath, json, cancellationToken);

            _logger.LogDebug("Saved active environment name to persistent storage: '{EnvironmentName}'",
                environmentName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save active environment to persistent storage: {Message}",
                ex.Message);
        }
    }

    private async Task<string?> LoadActiveEnvironmentNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_persistentStoragePath))
            {
                _logger.LogDebug("Persistent storage file not found: {Path}", _persistentStoragePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_persistentStoragePath, cancellationToken);
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (data != null && data.TryGetValue("ActiveEnvironmentName", out var value))
            {
                var environmentName = value.ToString();
                _logger.LogDebug("Loaded active environment name from persistent storage: '{EnvironmentName}'",
                    environmentName);
                return environmentName;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load active environment from persistent storage: {Message}",
                ex.Message);
            return null;
        }
    }
}

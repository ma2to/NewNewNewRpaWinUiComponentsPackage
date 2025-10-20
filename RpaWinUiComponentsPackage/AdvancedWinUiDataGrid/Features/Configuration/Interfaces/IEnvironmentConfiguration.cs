using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Models;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Interfaces;

/// <summary>
/// Internal interface for Environment Configuration management.
/// Manages environment-specific settings (page size, log level, database paths, etc.).
/// Config is loaded at application startup and remains read-only during runtime (NO hot-reload).
/// </summary>
internal interface IEnvironmentConfiguration
{
    /// <summary>
    /// Load configuration for a specific environment by name.
    /// This sets the active environment and applies its settings.
    /// </summary>
    /// <param name="environmentName">Name of the environment to load (e.g., "ProductionEU")</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The loaded environment configuration</returns>
    /// <exception cref="InvalidOperationException">If environment name does not exist</exception>
    Task<EnvironmentConfig> LoadEnvironmentConfigAsync(string environmentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Automatically detect and load environment based on detection rules.
    /// First matching rule wins (rules are evaluated in order).
    /// </summary>
    /// <param name="detectionRules">Collection of detection rules with conditions</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The loaded environment configuration</returns>
    /// <exception cref="InvalidOperationException">If no detection rule matches</exception>
    Task<EnvironmentConfig> DetectAndLoadEnvironmentAsync(
        IEnumerable<EnvironmentDetectionRule> detectionRules,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export environment configuration to JSON file.
    /// Exports a specific environment's settings.
    /// </summary>
    /// <param name="environmentName">Name of the environment to export</param>
    /// <param name="filePath">File path where JSON will be saved</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <exception cref="InvalidOperationException">If environment name does not exist</exception>
    Task ExportEnvironmentConfigAsync(
        string environmentName,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export environment configuration as JSON string.
    /// Exports a specific environment's settings.
    /// </summary>
    /// <param name="environmentName">Name of the environment to export</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>JSON string representation of the environment config</returns>
    /// <exception cref="InvalidOperationException">If environment name does not exist</exception>
    Task<string> ExportEnvironmentConfigAsJsonAsync(
        string environmentName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import environment configuration from JSON file.
    /// Overwrites existing environment if name matches, or adds new environment.
    /// </summary>
    /// <param name="filePath">File path to JSON file</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The imported environment configuration</returns>
    Task<EnvironmentConfig> ImportEnvironmentConfigAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import environment configuration from JSON string.
    /// Overwrites existing environment if name matches, or adds new environment.
    /// </summary>
    /// <param name="jsonConfig">JSON string representation of environment config</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The imported environment configuration</returns>
    Task<EnvironmentConfig> ImportEnvironmentConfigFromJsonAsync(
        string jsonConfig,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of all available environment names.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Read-only list of environment names</returns>
    Task<IReadOnlyList<string>> GetAvailableEnvironmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set active environment (saves to persistent storage, requires application restart to take effect).
    /// IMPORTANT: This only MARKS the environment as active for next startup - it does NOT reload config.
    /// Application must be restarted for the new environment to take effect.
    /// </summary>
    /// <param name="environmentName">Name of the environment to set as active</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <exception cref="InvalidOperationException">If environment name does not exist</exception>
    Task SetActiveEnvironmentAsync(
        string environmentName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load the active environment configuration from persistent storage.
    /// Used at application startup to load the environment marked as active.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The active environment configuration, or null if no active environment is set</returns>
    Task<EnvironmentConfig?> LoadActiveEnvironmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the currently loaded active environment configuration.
    /// Returns null if no environment has been loaded yet.
    /// </summary>
    /// <returns>Currently active environment config, or null</returns>
    EnvironmentConfig? GetActiveEnvironment();

    /// <summary>
    /// Register a new environment configuration container (from builder).
    /// This is typically called at application startup to register all environments.
    /// </summary>
    /// <param name="container">Environment configuration container with all defined environments</param>
    Task RegisterEnvironmentsAsync(EnvironmentConfigurationContainer container, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific setting value from the active environment.
    /// </summary>
    /// <typeparam name="T">Type of the setting value</typeparam>
    /// <param name="settingKey">Setting key (e.g., "PageSize", "LogLevel", "DatabasePath")</param>
    /// <returns>Setting value, or default(T) if not found</returns>
    T? GetSetting<T>(string settingKey);
}


namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

/// <summary>
/// Public interface for DataGrid configuration operations.
/// Provides configuration management, presets, and settings persistence.
/// </summary>
public interface IDataGridConfiguration
{
    /// <summary>
    /// Saves current grid configuration as a preset.
    /// </summary>
    /// <param name="presetName">Preset name</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> SaveConfigurationPresetAsync(string presetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a configuration preset.
    /// </summary>
    /// <param name="presetName">Preset name to load</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> LoadConfigurationPresetAsync(string presetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available configuration presets.
    /// </summary>
    /// <returns>Collection of preset names</returns>
    IReadOnlyList<string> GetAvailablePresets();

    /// <summary>
    /// Deletes a configuration preset.
    /// </summary>
    /// <param name="presetName">Preset name to delete</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> DeleteConfigurationPresetAsync(string presetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports current configuration to JSON.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result with JSON string</returns>
    Task<PublicResult<string>> ExportConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports configuration from JSON.
    /// </summary>
    /// <param name="jsonConfig">JSON configuration string</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ImportConfigurationAsync(string jsonConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current grid configuration.
    /// </summary>
    /// <returns>Current configuration</returns>
    PublicDataGridConfiguration GetCurrentConfiguration();

    /// <summary>
    /// Applies a configuration to the grid.
    /// </summary>
    /// <param name="configuration">Configuration to apply</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ApplyConfigurationAsync(PublicDataGridConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets configuration to defaults.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Result of the operation</returns>
    Task<PublicResult> ResetToDefaultConfigurationAsync(CancellationToken cancellationToken = default);
}


namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Interfaces;

/// <summary>
/// Internal interface for DataGrid configuration management
/// </summary>
internal interface IDataGridConfiguration
{
    /// <summary>
    /// Apply configuration (use at initialization only)
    /// </summary>
    Task ApplyConfigurationAsync(PublicDataGridConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save configuration preset
    /// </summary>
    Task SaveConfigurationPresetAsync(string presetName, PublicDataGridConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load configuration preset
    /// </summary>
    Task<PublicDataGridConfiguration> LoadConfigurationPresetAsync(string presetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current configuration
    /// </summary>
    PublicDataGridConfiguration GetCurrentConfiguration();

    /// <summary>
    /// Reset to default configuration
    /// </summary>
    Task ResetToDefaultConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Export configuration to file
    /// </summary>
    Task ExportConfigurationAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export configuration as JSON string
    /// </summary>
    Task<string> ExportConfigurationAsJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Import configuration from file
    /// </summary>
    Task<PublicDataGridConfiguration> ImportConfigurationAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Import configuration from JSON string
    /// </summary>
    Task<PublicDataGridConfiguration> ImportConfigurationFromJsonAsync(string jsonConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available preset names
    /// </summary>
    IReadOnlyList<string> GetAvailablePresets();

    /// <summary>
    /// Delete configuration preset
    /// </summary>
    Task DeleteConfigurationPresetAsync(string presetName, CancellationToken cancellationToken = default);
}

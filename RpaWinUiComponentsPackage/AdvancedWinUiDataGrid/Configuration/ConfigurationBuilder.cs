using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Common;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

/// <summary>
/// ENTERPRISE: Fluent configuration API builder
/// </summary>
internal sealed class ConfigurationBuilder
{
    private readonly AdvancedDataGridOptions _options = new();
    private AdvancedGridConfiguration _advancedConfig = AdvancedGridConfiguration.Default;

    /// <summary>
    /// Set operation mode
    /// </summary>
    public ConfigurationBuilder WithOperationMode(PublicDataGridOperationMode mode)
    {
        _options.OperationMode = mode;
        return this;
    }

    /// <summary>
    /// Enable/disable special columns
    /// </summary>
    public ConfigurationBuilder WithSpecialColumns(
        bool validationAlerts = true,
        bool rowNumber = false,
        bool checkbox = false,
        bool deleteRow = false)
    {
        _options.EnableValidationAlertsColumn = validationAlerts;
        _options.EnableRowNumberColumn = rowNumber;
        _options.EnableCheckboxColumn = checkbox;
        _options.EnableDeleteRowColumn = deleteRow;
        return this;
    }

    /// <summary>
    /// Configure performance settings
    /// </summary>
    public ConfigurationBuilder WithPerformance(
        int batchSize = 1000,
        bool enableParallel = true,
        int parallelThreshold = 1000)
    {
        _options.BatchSize = batchSize;
        _options.EnableParallelProcessing = enableParallel;
        _options.ParallelProcessingThreshold = parallelThreshold;
        return this;
    }

    /// <summary>
    /// Configure logging
    /// </summary>
    public ConfigurationBuilder WithLogging(
        bool enabled = true,
        LogLevel minLevel = LogLevel.Information)
    {
        _options.EnableComprehensiveLogging = enabled;
        _options.MinimumLogLevel = minLevel;
        return this;
    }

    /// <summary>
    /// Configure validation settings
    /// </summary>
    public ConfigurationBuilder WithValidation(
        bool enableBatch = true,
        bool enableRealTime = true)
    {
        _options.EnableBatchValidation = enableBatch;
        _options.EnableRealTimeValidation = enableRealTime;
        return this;
    }

    /// <summary>
    /// Enable or disable a specific grid feature
    /// </summary>
    public ConfigurationBuilder WithFeature(GridFeature feature, bool enabled = true)
    {
        _options.SetFeatureEnabled(feature, enabled);
        return this;
    }

    /// <summary>
    /// Enable multiple grid features at once
    /// </summary>
    public ConfigurationBuilder WithFeatures(params GridFeature[] features)
    {
        _options.EnableFeatures(features);
        return this;
    }

    /// <summary>
    /// Disable multiple grid features at once
    /// </summary>
    public ConfigurationBuilder WithoutFeatures(params GridFeature[] features)
    {
        _options.DisableFeatures(features);
        return this;
    }

    /// <summary>
    /// Configure UI settings
    /// </summary>
    public ConfigurationBuilder WithUI(
        UIMode mode = UIMode.Interactive,
        ThemeMode theme = ThemeMode.Light)
    {
        _advancedConfig = _advancedConfig with
        {
            UIMode = mode,
            ThemeMode = theme
        };
        return this;
    }

    /// <summary>
    /// Configure security settings
    /// </summary>
    public ConfigurationBuilder WithSecurity(
        SecurityLevel level = SecurityLevel.Standard,
        bool enableInputValidation = true)
    {
        _advancedConfig = _advancedConfig with
        {
            SecurityLevel = level,
            EnableInputValidation = enableInputValidation
        };
        return this;
    }

    /// <summary>
    /// Build and validate configuration
    /// </summary>
    public (AdvancedDataGridOptions Options, AdvancedGridConfiguration Advanced) Build()
    {
        var optionsValidation = ConfigurationValidator.Validate(_options);
        if (!optionsValidation.IsValid)
        {
            throw new InvalidOperationException(
                $"Invalid configuration: {string.Join(", ", optionsValidation.Errors)}");
        }

        var advancedValidation = ConfigurationValidator.Validate(_advancedConfig);
        if (!advancedValidation.IsValid)
        {
            throw new InvalidOperationException(
                $"Invalid advanced configuration: {string.Join(", ", advancedValidation.Errors)}");
        }

        return (_options.Clone(), _advancedConfig);
    }
}

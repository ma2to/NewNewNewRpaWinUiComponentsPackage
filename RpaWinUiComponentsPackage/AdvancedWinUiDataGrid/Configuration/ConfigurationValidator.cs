namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Configuration;

/// <summary>
/// ENTERPRISE: Configuration validation service
/// </summary>
internal static class ConfigurationValidator
{
    /// <summary>
    /// Validate AdvancedDataGridOptions configuration
    /// </summary>
    public static ValidationResult Validate(AdvancedDataGridOptions options)
    {
        var errors = new List<string>();

        if (options.BatchSize <= 0)
            errors.Add("BatchSize must be greater than 0");

        if (options.MaxSelectionSize <= 0)
            errors.Add("MaxSelectionSize must be greater than 0");

        if (options.DefaultOperationTimeout <= TimeSpan.Zero)
            errors.Add("DefaultOperationTimeout must be positive");

        if (options.MinimumRowHeight <= 0)
            errors.Add("MinimumRowHeight must be greater than 0");

        if (options.MaximumRowHeight <= options.MinimumRowHeight)
            errors.Add("MaximumRowHeight must be greater than MinimumRowHeight");

        if (options.ParallelProcessingThreshold <= 0)
            errors.Add("ParallelProcessingThreshold must be greater than 0");

        return errors.Any()
            ? ValidationResult.Failed(errors)
            : ValidationResult.Passed();
    }

    /// <summary>
    /// Validate AdvancedGridConfiguration
    /// </summary>
    public static ValidationResult Validate(AdvancedGridConfiguration configuration)
    {
        var errors = new List<string>();

        if (configuration.CacheExpiration <= TimeSpan.Zero)
            errors.Add("CacheExpiration must be positive");

        if (configuration.MaxConcurrentOperations <= 0)
            errors.Add("MaxConcurrentOperations must be greater than 0");

        if (configuration.LoggingConfiguration.PerformanceThreshold <= TimeSpan.Zero)
            errors.Add("PerformanceThreshold must be positive");

        return errors.Any()
            ? ValidationResult.Failed(errors)
            : ValidationResult.Passed();
    }
}

/// <summary>
/// Validation result value object
/// </summary>
internal sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ValidationResult Passed() => new() { IsValid = true };
    public static ValidationResult Failed(IEnumerable<string> errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}

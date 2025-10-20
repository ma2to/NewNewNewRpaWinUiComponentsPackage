using System.Text.Json.Serialization;

namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid.Features.Configuration.Models;

/// <summary>
/// Internal model for environment detection rules.
/// Defines conditions for automatically selecting an environment based on runtime conditions.
/// </summary>
internal record EnvironmentDetectionRule
{
    /// <summary>Name of the environment to select if condition matches</summary>
    public required string EnvironmentName { get; init; }

    /// <summary>
    /// Condition function that returns true if this environment should be selected.
    /// Not serialized - must be set programmatically.
    /// </summary>
    [JsonIgnore]
    public Func<bool> Condition { get; init; } = () => false;
}

/// <summary>
/// Internal model for environment configuration.
/// Represents a single environment with its settings (page size, log level, database path, etc.).
/// </summary>
internal record EnvironmentConfig
{
    /// <summary>Environment name (e.g., "LocalDev", "ProductionEU")</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Environment settings as key-value pairs.
    /// Flexible design allows any custom settings.
    /// Example: { "PageSize": 1000, "LogLevel": "Warning", "DatabasePath": "D:\\Production\\eu.db" }
    /// </summary>
    public required Dictionary<string, object> Settings { get; init; }

    /// <summary>Metadata: When this environment config was created</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Metadata: Author or creator of this environment config</summary>
    public string? Author { get; init; }

    /// <summary>Metadata: Description of this environment</summary>
    public string? Description { get; init; }
}

/// <summary>
/// Internal model for environment configuration container.
/// Holds all defined environments and tracks which one is currently active.
/// </summary>
internal record EnvironmentConfigurationContainer
{
    /// <summary>
    /// All defined environments, keyed by environment name.
    /// Example: { "LocalDev": {...}, "ProductionEU": {...}, "ProductionUS": {...} }
    /// </summary>
    public required Dictionary<string, EnvironmentConfig> Environments { get; init; }

    /// <summary>
    /// Name of the currently active environment.
    /// Null if no environment has been loaded yet.
    /// </summary>
    public string? ActiveEnvironmentName { get; init; }

    /// <summary>
    /// Currently loaded environment configuration.
    /// Null if no environment has been loaded yet.
    /// </summary>
    [JsonIgnore]
    public EnvironmentConfig? ActiveEnvironment =>
        ActiveEnvironmentName != null && Environments.TryGetValue(ActiveEnvironmentName, out var env)
            ? env
            : null;
}

/// <summary>
/// Internal builder for creating environment configurations with fluent API.
/// Provides a type-safe way to define multiple environments programmatically.
/// </summary>
internal sealed class EnvironmentConfigurationBuilder
{
    private readonly Dictionary<string, EnvironmentConfig> _environments = new();

    /// <summary>
    /// Adds a new environment with the specified name and settings.
    /// </summary>
    /// <param name="name">Environment name (must be unique)</param>
    /// <param name="settings">Settings as anonymous object or dictionary</param>
    /// <param name="author">Optional: Author or creator of this environment</param>
    /// <param name="description">Optional: Description of this environment</param>
    /// <returns>This builder instance for method chaining</returns>
    /// <exception cref="ArgumentException">If environment name is null or empty</exception>
    /// <exception cref="InvalidOperationException">If environment with this name already exists</exception>
    public EnvironmentConfigurationBuilder AddEnvironment(
        string name,
        object settings,
        string? author = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Environment name cannot be null or empty", nameof(name));

        if (_environments.ContainsKey(name))
            throw new InvalidOperationException($"Environment '{name}' already exists");

        // Convert anonymous object or dictionary to Dictionary<string, object>
        var settingsDict = settings switch
        {
            Dictionary<string, object> dict => dict,
            _ => ConvertAnonymousObjectToDictionary(settings)
        };

        _environments[name] = new EnvironmentConfig
        {
            Name = name,
            Settings = settingsDict,
            CreatedAt = DateTime.UtcNow,
            Author = author,
            Description = description
        };

        return this;
    }

    /// <summary>
    /// Builds the environment configuration container from all added environments.
    /// </summary>
    /// <param name="activeEnvironmentName">Optional: Set which environment is active by default</param>
    /// <returns>Immutable environment configuration container</returns>
    /// <exception cref="InvalidOperationException">If no environments have been added</exception>
    public EnvironmentConfigurationContainer Build(string? activeEnvironmentName = null)
    {
        if (_environments.Count == 0)
            throw new InvalidOperationException("At least one environment must be defined");

        if (activeEnvironmentName != null && !_environments.ContainsKey(activeEnvironmentName))
            throw new InvalidOperationException($"Active environment '{activeEnvironmentName}' does not exist");

        return new EnvironmentConfigurationContainer
        {
            Environments = new Dictionary<string, EnvironmentConfig>(_environments),
            ActiveEnvironmentName = activeEnvironmentName
        };
    }

    /// <summary>
    /// Converts anonymous object to Dictionary using reflection.
    /// Supports nested objects and common types.
    /// </summary>
    private static Dictionary<string, object> ConvertAnonymousObjectToDictionary(object obj)
    {
        var dict = new Dictionary<string, object>();

        foreach (var prop in obj.GetType().GetProperties())
        {
            var value = prop.GetValue(obj);
            if (value != null)
            {
                dict[prop.Name] = value;
            }
        }

        return dict;
    }
}

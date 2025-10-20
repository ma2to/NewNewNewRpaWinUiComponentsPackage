namespace RpaWinUiComponentsPackage.AdvancedWinUiDataGrid;

/// <summary>
/// Public model for environment detection rule.
/// Defines conditions for automatically selecting an environment based on runtime conditions.
/// </summary>
public class PublicEnvironmentDetectionRule
{
    /// <summary>
    /// Name of the environment to select if condition matches (e.g., "LocalDev", "ProductionEU")
    /// </summary>
    public string EnvironmentName { get; init; } = "";

    /// <summary>
    /// Condition function that returns true if this environment should be selected.
    /// Example: () => Environment.MachineName == "MY-DEV-PC"
    /// </summary>
    public Func<bool> Condition { get; init; } = () => false;
}

/// <summary>
/// Public model for environment configuration.
/// Represents a single environment with its settings (page size, log level, database path, etc.).
/// </summary>
public class PublicEnvironmentConfig
{
    /// <summary>
    /// Environment name (e.g., "LocalDev", "ProductionEU", "ProductionUS")
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Environment settings as key-value pairs.
    /// Flexible design allows any custom settings.
    /// Example: { "PageSize" = 1000, "LogLevel" = "Warning", "DatabasePath" = "D:\\Production\\eu.db" }
    /// </summary>
    public IReadOnlyDictionary<string, object> Settings { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// When this environment config was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Author or creator of this environment config
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Description of this environment
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Public builder for creating environment configurations with fluent API.
/// Provides a type-safe way to define multiple environments programmatically.
/// </summary>
/// <example>
/// <code>
/// var envConfig = new PublicEnvironmentConfigurationBuilder()
///     .AddEnvironment("LocalDev", new {
///         PageSize = 10,
///         LogLevel = "Debug",
///         DatabasePath = "C:\\Dev\\local.db"
///     })
///     .AddEnvironment("ProductionEU", new {
///         PageSize = 1000,
///         LogLevel = "Warning",
///         DatabasePath = "D:\\Production\\eu.db"
///     })
///     .Build();
/// </code>
/// </example>
public class PublicEnvironmentConfigurationBuilder
{
    private readonly List<PublicEnvironmentConfig> _environments = new();

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
    public PublicEnvironmentConfigurationBuilder AddEnvironment(
        string name,
        object settings,
        string? author = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Environment name cannot be null or empty", nameof(name));

        if (_environments.Any(e => e.Name == name))
            throw new InvalidOperationException($"Environment '{name}' already exists");

        // Convert anonymous object or dictionary to Dictionary<string, object>
        var settingsDict = settings switch
        {
            Dictionary<string, object> dict => dict,
            IReadOnlyDictionary<string, object> roDict => new Dictionary<string, object>(roDict),
            _ => ConvertAnonymousObjectToDictionary(settings)
        };

        _environments.Add(new PublicEnvironmentConfig
        {
            Name = name,
            Settings = settingsDict,
            CreatedAt = DateTime.UtcNow,
            Author = author,
            Description = description
        });

        return this;
    }

    /// <summary>
    /// Builds a read-only list of all added environments.
    /// </summary>
    /// <returns>Read-only list of environment configurations</returns>
    /// <exception cref="InvalidOperationException">If no environments have been added</exception>
    public IReadOnlyList<PublicEnvironmentConfig> Build()
    {
        if (_environments.Count == 0)
            throw new InvalidOperationException("At least one environment must be defined");

        return _environments.AsReadOnly();
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

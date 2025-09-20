using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Interfaces;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Services;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// PUBLIC EXTENSION: Dependency injection registration for AdvancedWinUiLogger
/// DI PATTERN: Clean registration of internal services
/// ENTERPRISE: Professional service registration for consumers
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// PUBLIC API: Register AdvancedWinUiLogger services with dependency injection
    /// MAIN REGISTRATION: Single method to register all component services
    /// CLEAN API: Consumers only see this extension method, not internal types
    /// </summary>
    public static IServiceCollection AddAdvancedWinUiLogger(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Register internal service interface with implementation
        // This registration is internal - consumers don't see it in IntelliSense
        services.AddScoped<IAdvancedLoggerService, AdvancedLoggerService>();

        // Register the public facade
        // This uses the DI constructor of AdvancedLoggerFacade
        services.AddScoped<AdvancedLoggerFacade>(serviceProvider =>
        {
            var loggerService = serviceProvider.GetRequiredService<IAdvancedLoggerService>();
            var logger = serviceProvider.GetService<ILogger<AdvancedLoggerFacade>>();

            return new AdvancedLoggerFacade(loggerService, logger);
        });

        return services;
    }

    /// <summary>
    /// PUBLIC API: Register AdvancedWinUiLogger with preconfigured settings
    /// CONVENIENCE: Quick setup with common configuration
    /// </summary>
    public static IServiceCollection AddAdvancedWinUiLogger(
        this IServiceCollection services,
        string logDirectory,
        string baseFileName = "application")
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new ArgumentException("Log directory cannot be null or whitespace", nameof(logDirectory));
        }

        // Register main services
        services.AddAdvancedWinUiLogger();

        // Register pre-configured logger configuration as singleton
        var configuration = LoggerConfiguration.CreateMinimal(logDirectory, baseFileName);
        services.AddSingleton(configuration);

        return services;
    }

    /// <summary>
    /// PUBLIC API: Register AdvancedWinUiLogger with custom configuration
    /// ADVANCED REGISTRATION: For scenarios requiring specific configuration
    /// </summary>
    public static IServiceCollection AddAdvancedWinUiLogger(
        this IServiceCollection services,
        LoggerConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        // Validate configuration
        var validationResult = configuration.Validate();
        if (validationResult.IsFailure)
        {
            throw new ArgumentException($"Invalid logger configuration: {validationResult.Error}", nameof(configuration));
        }

        // Register main services
        services.AddAdvancedWinUiLogger();

        // Register the provided configuration
        services.AddSingleton(configuration);

        return services;
    }

    /// <summary>
    /// PUBLIC API: Register AdvancedWinUiLogger for high-performance scenarios
    /// PERFORMANCE REGISTRATION: Optimized for high-throughput logging
    /// </summary>
    public static IServiceCollection AddAdvancedWinUiLoggerHighPerformance(
        this IServiceCollection services,
        string logDirectory,
        string baseFileName = "application")
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new ArgumentException("Log directory cannot be null or whitespace", nameof(logDirectory));
        }

        // Register main services
        services.AddAdvancedWinUiLogger();

        // Register high-performance configuration
        var configuration = LoggerConfiguration.CreateHighPerformance(logDirectory, baseFileName);
        services.AddSingleton(configuration);

        return services;
    }

    /// <summary>
    /// PUBLIC API: Register AdvancedWinUiLogger for development scenarios
    /// DEVELOPMENT REGISTRATION: Optimized for development and debugging
    /// </summary>
    public static IServiceCollection AddAdvancedWinUiLoggerDevelopment(
        this IServiceCollection services,
        string logDirectory,
        string baseFileName = "dev")
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new ArgumentException("Log directory cannot be null or whitespace", nameof(logDirectory));
        }

        // Register main services
        services.AddAdvancedWinUiLogger();

        // Register development configuration
        var configuration = LoggerConfiguration.CreateDevelopment(logDirectory, baseFileName);
        services.AddSingleton(configuration);

        return services;
    }

    /// <summary>
    /// PUBLIC API: Register AdvancedWinUiLogger services as singletons
    /// PERFORMANCE REGISTRATION: When thread-safety allows singleton usage
    /// </summary>
    public static IServiceCollection AddAdvancedWinUiLoggerSingleton(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Register internal service interface as singleton
        services.AddSingleton<IAdvancedLoggerService, AdvancedLoggerService>();

        // Register the public facade as singleton
        services.AddSingleton<AdvancedLoggerFacade>(serviceProvider =>
        {
            var loggerService = serviceProvider.GetRequiredService<IAdvancedLoggerService>();
            var logger = serviceProvider.GetService<ILogger<AdvancedLoggerFacade>>();

            return new AdvancedLoggerFacade(loggerService, logger);
        });

        return services;
    }

    /// <summary>
    /// PUBLIC API: Check if AdvancedWinUiLogger services are registered
    /// UTILITY: Help consumers verify registration status
    /// </summary>
    public static bool IsAdvancedWinUiLoggerRegistered(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services.Any(serviceDescriptor =>
            serviceDescriptor.ServiceType == typeof(AdvancedLoggerFacade));
    }

    /// <summary>
    /// PUBLIC API: Remove AdvancedWinUiLogger services from registration
    /// UTILITY: For testing or reconfiguration scenarios
    /// </summary>
    public static IServiceCollection RemoveAdvancedWinUiLogger(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Remove facade registration
        var facadeDescriptors = services
            .Where(s => s.ServiceType == typeof(AdvancedLoggerFacade))
            .ToList();

        foreach (var descriptor in facadeDescriptors)
        {
            services.Remove(descriptor);
        }

        // Remove internal service registrations
        var internalDescriptors = services
            .Where(s => s.ServiceType == typeof(IAdvancedLoggerService))
            .ToList();

        foreach (var descriptor in internalDescriptors)
        {
            services.Remove(descriptor);
        }

        // Remove configuration registration
        var configDescriptors = services
            .Where(s => s.ServiceType == typeof(LoggerConfiguration))
            .ToList();

        foreach (var descriptor in configDescriptors)
        {
            services.Remove(descriptor);
        }

        return services;
    }

    /// <summary>
    /// PUBLIC API: Create logger configuration builder for custom setup
    /// BUILDER PATTERN: Fluent configuration creation
    /// </summary>
    public static LoggerConfigurationBuilder CreateLoggerConfiguration(
        this IServiceCollection services,
        string logDirectory)
    {
        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new ArgumentException("Log directory cannot be null or whitespace", nameof(logDirectory));
        }

        return new LoggerConfigurationBuilder(logDirectory);
    }
}

/// <summary>
/// PUBLIC BUILDER: Fluent configuration builder for logger setup
/// BUILDER PATTERN: Simplifies complex configuration creation
/// </summary>
public sealed class LoggerConfigurationBuilder
{
    private readonly string _logDirectory;
    private string _baseFileName = "application";
    private LogLevel _minLogLevel = LogLevel.Information;
    private bool _enableStructuredLogging = true;
    private bool _enableBackgroundLogging = true;
    private int _bufferSize = 1000;
    private TimeSpan _flushInterval = TimeSpan.FromSeconds(5);

    internal LoggerConfigurationBuilder(string logDirectory)
    {
        _logDirectory = logDirectory;
    }

    /// <summary>Set base file name for log files</summary>
    public LoggerConfigurationBuilder WithBaseFileName(string baseFileName)
    {
        _baseFileName = baseFileName ?? throw new ArgumentNullException(nameof(baseFileName));
        return this;
    }

    /// <summary>Set minimum log level</summary>
    public LoggerConfigurationBuilder WithMinLogLevel(LogLevel minLogLevel)
    {
        _minLogLevel = minLogLevel;
        return this;
    }

    /// <summary>Enable or disable structured logging</summary>
    public LoggerConfigurationBuilder WithStructuredLogging(bool enable)
    {
        _enableStructuredLogging = enable;
        return this;
    }

    /// <summary>Enable or disable background logging</summary>
    public LoggerConfigurationBuilder WithBackgroundLogging(bool enable)
    {
        _enableBackgroundLogging = enable;
        return this;
    }

    /// <summary>Set internal buffer size</summary>
    public LoggerConfigurationBuilder WithBufferSize(int bufferSize)
    {
        if (bufferSize <= 0)
            throw new ArgumentException("Buffer size must be greater than 0", nameof(bufferSize));

        _bufferSize = bufferSize;
        return this;
    }

    /// <summary>Set flush interval for buffered entries</summary>
    public LoggerConfigurationBuilder WithFlushInterval(TimeSpan flushInterval)
    {
        if (flushInterval <= TimeSpan.Zero)
            throw new ArgumentException("Flush interval must be greater than zero", nameof(flushInterval));

        _flushInterval = flushInterval;
        return this;
    }

    /// <summary>Build the logger configuration</summary>
    public LoggerConfiguration Build()
    {
        return new LoggerConfiguration
        {
            LogDirectory = _logDirectory,
            BaseFileName = _baseFileName,
            MinLogLevel = _minLogLevel,
            EnableStructuredLogging = _enableStructuredLogging,
            EnableBackgroundLogging = _enableBackgroundLogging,
            BufferSize = _bufferSize,
            FlushInterval = _flushInterval
        };
    }
}
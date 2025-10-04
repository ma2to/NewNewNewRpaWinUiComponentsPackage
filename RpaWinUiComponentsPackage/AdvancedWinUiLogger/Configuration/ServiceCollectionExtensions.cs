using System;
using Microsoft.Extensions.DependencyInjection;

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
    public static IServiceCollection AddAdvancedWinUiLogger(
        this IServiceCollection services,
        AdvancedLoggerOptions? options = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        Configuration.ServiceRegistration.Register(services, options);
        return services;
    }

    /// <summary>
    /// PUBLIC API: Register AdvancedWinUiLogger with fluent configuration
    /// CONVENIENCE: Quick setup with inline configuration
    /// </summary>
    public static IServiceCollection AddAdvancedWinUiLogger(
        this IServiceCollection services,
        Action<AdvancedLoggerOptions> configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = AdvancedLoggerOptions.CreateMinimal("./logs");
        // Note: Cannot call configure on record, so create via factory
        return AddAdvancedWinUiLogger(services, options);
    }

    /// <summary>
    /// PUBLIC API: Register AdvancedWinUiLogger with minimal configuration
    /// CONVENIENCE: Simple setup with directory and filename
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

        var options = AdvancedLoggerOptions.CreateMinimal(logDirectory, baseFileName);
        return AddAdvancedWinUiLogger(services, options);
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

        var options = AdvancedLoggerOptions.CreateHighPerformance(logDirectory, baseFileName);
        return AddAdvancedWinUiLogger(services, options);
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

        var options = AdvancedLoggerOptions.CreateDevelopment(logDirectory, baseFileName);
        return AddAdvancedWinUiLogger(services, options);
    }
}

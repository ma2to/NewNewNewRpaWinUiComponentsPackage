using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Entities;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// ENTERPRISE COMPONENT: Advanced WinUI Logger with comprehensive logging capabilities
/// PUBLIC API: Main entry point for all logging operations
/// CLEAN ARCHITECTURE: Facade pattern providing unified access to logging functionality
/// SENIOR ARCHITECTURE: 20+ years of enterprise logging best practices
/// </summary>
public static class AdvancedWinUiLogger
{
    #region Factory Methods - Core Logger Creation

    /// <summary>
    /// FACTORY: Create minimal file logger for simple scenarios
    /// ENTERPRISE: Quick setup with sensible defaults for development
    /// USAGE: AdvancedWinUiLogger.CreateSimpleLogger(@"C:\Logs", "MyApp")
    /// </summary>
    /// <param name="logDirectory">Directory where log files will be stored</param>
    /// <param name="baseFileName">Base name for log files (without extension)</param>
    /// <returns>Result containing ILogger instance or error information</returns>
    public static Result<ILogger> CreateSimpleLogger(string logDirectory, string baseFileName = "application")
    {
        return LoggerApi.CreateFileLogger(logDirectory, baseFileName);
    }

    /// <summary>
    /// FACTORY: Create high-performance logger for production environments
    /// ENTERPRISE: Optimized for high-throughput scenarios with advanced features
    /// USAGE: AdvancedWinUiLogger.CreateProductionLogger(@"C:\Logs\Production", "MyEnterpriseApp")
    /// </summary>
    /// <param name="logDirectory">Directory where log files will be stored</param>
    /// <param name="baseFileName">Base name for log files (without extension)</param>
    /// <returns>Result containing high-performance ILogger instance or error information</returns>
    public static Result<ILogger> CreateProductionLogger(string logDirectory, string baseFileName = "application")
    {
        return LoggerApi.CreateHighPerformanceLogger(logDirectory, baseFileName);
    }

    /// <summary>
    /// FACTORY: Create development logger with detailed debugging capabilities
    /// ENTERPRISE: Full logging with trace-level details for development scenarios
    /// USAGE: AdvancedWinUiLogger.CreateDevelopmentLogger(@"C:\Logs\Dev", "MyApp-Dev")
    /// </summary>
    /// <param name="logDirectory">Directory where log files will be stored</param>
    /// <param name="baseFileName">Base name for log files (without extension)</param>
    /// <returns>Result containing development-optimized ILogger instance or error information</returns>
    public static Result<ILogger> CreateDevelopmentLogger(string logDirectory, string baseFileName = "dev")
    {
        return LoggerApi.CreateDevelopmentLogger(logDirectory, baseFileName);
    }

    /// <summary>
    /// FACTORY: Create logger with fully customized configuration
    /// ENTERPRISE: Complete control over all logging aspects and behaviors
    /// USAGE: AdvancedWinUiLogger.CreateCustomLogger(myConfiguration)
    /// </summary>
    /// <param name="configuration">Complete logger configuration with all settings</param>
    /// <returns>Result containing custom-configured ILogger instance or error information</returns>
    public static Result<ILogger> CreateCustomLogger(LoggerConfiguration configuration)
    {
        return LoggerApi.CreateCustomLogger(configuration);
    }

    #endregion

    #region Environment-Specific Logger Creation

    /// <summary>
    /// ENVIRONMENT: Create logger optimized for specific deployment environment
    /// ENTERPRISE: Environment-aware configuration with best practices
    /// USAGE: AdvancedWinUiLogger.CreateEnvironmentLogger("Production", @"C:\Logs", "MyApp")
    /// </summary>
    /// <param name="environment">Environment name (Development, Staging, Production, Testing)</param>
    /// <param name="logDirectory">Directory where log files will be stored</param>
    /// <param name="baseFileName">Base name for log files (without extension)</param>
    /// <returns>Result containing environment-optimized ILogger instance or error information</returns>
    public static Result<ILogger> CreateEnvironmentLogger(string environment, string logDirectory, string baseFileName = "application")
    {
        var configResult = LoggerApi.CreateConfigurationForEnvironment(environment, logDirectory, baseFileName);
        if (configResult.IsFailure)
            return Result<ILogger>.Failure($"Failed to create environment configuration: {configResult.Error}");

        return LoggerApi.CreateCustomLogger(configResult.Value);
    }

    #endregion

    #region Configuration Management

    /// <summary>
    /// CONFIGURATION: Create minimal configuration for quick setup
    /// ENTERPRISE: Sensible defaults for rapid development starts
    /// USAGE: var config = AdvancedWinUiLogger.CreateMinimalConfiguration(@"C:\Logs", "MyApp");
    /// </summary>
    /// <param name="logDirectory">Directory where log files will be stored</param>
    /// <param name="baseFileName">Base name for log files (without extension)</param>
    /// <returns>Result containing minimal LoggerConfiguration or error information</returns>
    public static Result<LoggerConfiguration> CreateMinimalConfiguration(string logDirectory, string baseFileName = "application")
    {
        try
        {
            var config = LoggerConfiguration.CreateMinimal(logDirectory, baseFileName);
            var validationResult = config.Validate();

            if (validationResult.IsFailure)
                return Result<LoggerConfiguration>.Failure($"Generated minimal configuration is invalid: {validationResult.Error}");

            return Result<LoggerConfiguration>.Success(config);
        }
        catch (Exception ex)
        {
            return Result<LoggerConfiguration>.Failure($"Failed to create minimal configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// CONFIGURATION: Create high-performance configuration for enterprise scenarios
    /// ENTERPRISE: Optimized settings for production environments with high throughput
    /// USAGE: var config = AdvancedWinUiLogger.CreateHighPerformanceConfiguration(@"C:\Logs", "EnterpriseApp");
    /// </summary>
    /// <param name="logDirectory">Directory where log files will be stored</param>
    /// <param name="baseFileName">Base name for log files (without extension)</param>
    /// <returns>Result containing high-performance LoggerConfiguration or error information</returns>
    public static Result<LoggerConfiguration> CreateHighPerformanceConfiguration(string logDirectory, string baseFileName = "application")
    {
        try
        {
            var config = LoggerConfiguration.CreateHighPerformance(logDirectory, baseFileName);
            var validationResult = config.Validate();

            if (validationResult.IsFailure)
                return Result<LoggerConfiguration>.Failure($"Generated high-performance configuration is invalid: {validationResult.Error}");

            return Result<LoggerConfiguration>.Success(config);
        }
        catch (Exception ex)
        {
            return Result<LoggerConfiguration>.Failure($"Failed to create high-performance configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// VALIDATION: Validate configuration for production readiness
    /// ENTERPRISE: Comprehensive validation with production-specific checks
    /// USAGE: var isValid = AdvancedWinUiLogger.ValidateProductionConfiguration(myConfig);
    /// </summary>
    /// <param name="configuration">Configuration to validate for production use</param>
    /// <returns>Result indicating validation success or detailed error information</returns>
    public static Result<bool> ValidateProductionConfiguration(LoggerConfiguration configuration)
    {
        return LoggerApi.ValidateProductionConfiguration(configuration);
    }

    #endregion

    #region File Management Operations

    /// <summary>
    /// FILE MANAGEMENT: Manually rotate current log file
    /// ENTERPRISE: Force rotation for maintenance or operational needs
    /// USAGE: await AdvancedWinUiLogger.RotateLogFileAsync(@"C:\Logs\current.log");
    /// </summary>
    /// <param name="currentFilePath">Path to current log file to rotate</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result indicating rotation success or detailed error information</returns>
    public static async Task<Result<bool>> RotateLogFileAsync(string currentFilePath, CancellationToken cancellationToken = default)
    {
        return await LoggerApi.RotateLogFileAsync(currentFilePath, cancellationToken);
    }

    /// <summary>
    /// MAINTENANCE: Clean up old log files based on retention policies
    /// ENTERPRISE: Automated cleanup for storage management and compliance
    /// USAGE: await AdvancedWinUiLogger.CleanupOldLogsAsync(@"C:\Logs", maxAgeDays: 30, maxFileCount: 100);
    /// </summary>
    /// <param name="logDirectory">Directory containing log files to clean up</param>
    /// <param name="maxAgeDays">Maximum age in days before files are deleted</param>
    /// <param name="maxFileCount">Maximum number of files to retain</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result containing number of files cleaned up or error information</returns>
    public static async Task<Result<int>> CleanupOldLogsAsync(
        string logDirectory,
        int maxAgeDays = 30,
        int maxFileCount = 10,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.CleanupOldLogsAsync(logDirectory, maxAgeDays, maxFileCount, cancellationToken);
    }

    /// <summary>
    /// DISCOVERY: Get information about log files in specified directory
    /// ENTERPRISE: File inventory and analysis for monitoring and management
    /// USAGE: var logFiles = await AdvancedWinUiLogger.GetLogFilesAsync(@"C:\Logs");
    /// </summary>
    /// <param name="logDirectory">Directory to scan for log files</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result containing list of LogFileInfo objects or error information</returns>
    public static async Task<Result<IReadOnlyList<LogFileInfo>>> GetLogFilesAsync(
        string logDirectory,
        CancellationToken cancellationToken = default)
    {
        return await LoggerApi.GetLogFilesAsync(logDirectory, cancellationToken);
    }

    #endregion

    #region Health and Diagnostics

    /// <summary>
    /// HEALTH: Check overall logger system health
    /// ENTERPRISE: Health monitoring for operational oversight and alerting
    /// USAGE: var isHealthy = await AdvancedWinUiLogger.CheckHealthAsync();
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result indicating health status or detailed error information</returns>
    public static async Task<Result<bool>> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return await LoggerApi.CheckHealthAsync(cancellationToken);
    }

    /// <summary>
    /// TELEMETRY: Get comprehensive performance metrics
    /// ENTERPRISE: Performance monitoring and operational insights
    /// USAGE: var metrics = AdvancedWinUiLogger.GetPerformanceMetrics();
    /// </summary>
    /// <returns>Result containing performance metrics object or error information</returns>
    public static Result<object> GetPerformanceMetrics()
    {
        return LoggerApi.GetPerformanceMetrics();
    }

    #endregion

    #region Component Information

    /// <summary>
    /// INFO: Get current logger component version
    /// ENTERPRISE: Version tracking for compatibility and support
    /// USAGE: var version = AdvancedWinUiLogger.GetVersion();
    /// </summary>
    /// <returns>Version string in semantic versioning format</returns>
    public static string GetVersion()
    {
        return LoggerApi.GetVersion();
    }

    /// <summary>
    /// INFO: Get list of supported logger features
    /// ENTERPRISE: Feature discovery for capability detection
    /// USAGE: var features = AdvancedWinUiLogger.GetSupportedFeatures();
    /// </summary>
    /// <returns>Read-only list of supported feature descriptions</returns>
    public static IReadOnlyList<string> GetSupportedFeatures()
    {
        return LoggerApi.GetSupportedFeatures();
    }

    /// <summary>
    /// INFO: Get configuration schema version for compatibility checking
    /// ENTERPRISE: Schema version tracking for configuration compatibility
    /// USAGE: var schemaVersion = AdvancedWinUiLogger.GetConfigurationSchemaVersion();
    /// </summary>
    /// <returns>Configuration schema version string</returns>
    public static string GetConfigurationSchemaVersion()
    {
        return LoggerApi.GetConfigurationSchemaVersion();
    }

    #endregion

    #region Convenience Methods for Common Scenarios

    /// <summary>
    /// CONVENIENCE: Quick setup for Windows application logging
    /// ENTERPRISE: One-line setup for typical Windows desktop applications
    /// USAGE: var logger = AdvancedWinUiLogger.QuickSetupForWindowsApp("MyWinUIApp");
    /// </summary>
    /// <param name="applicationName">Name of the application for log file naming</param>
    /// <param name="logLevel">Minimum log level (default: Information)</param>
    /// <returns>Result containing configured ILogger instance or error information</returns>
    public static Result<ILogger> QuickSetupForWindowsApp(string applicationName, LogLevel logLevel = LogLevel.Information)
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDirectory = System.IO.Path.Combine(appDataPath, applicationName, "Logs");

            var configResult = CreateMinimalConfiguration(logDirectory, applicationName);
            if (configResult.IsFailure)
                return Result<ILogger>.Failure($"Failed to create configuration: {configResult.Error}");

            var config = configResult.Value.WithMinLogLevel(logLevel);
            return CreateCustomLogger(config);
        }
        catch (Exception ex)
        {
            return Result<ILogger>.Failure($"Quick setup failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// CONVENIENCE: Quick setup for enterprise service logging
    /// ENTERPRISE: One-line setup for Windows services and enterprise applications
    /// USAGE: var logger = AdvancedWinUiLogger.QuickSetupForEnterpriseService("MyEnterpriseService");
    /// </summary>
    /// <param name="serviceName">Name of the service for log file naming</param>
    /// <param name="logLevel">Minimum log level (default: Warning for production)</param>
    /// <returns>Result containing enterprise-configured ILogger instance or error information</returns>
    public static Result<ILogger> QuickSetupForEnterpriseService(string serviceName, LogLevel logLevel = LogLevel.Warning)
    {
        try
        {
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var logDirectory = System.IO.Path.Combine(programDataPath, serviceName, "Logs");

            var configResult = CreateHighPerformanceConfiguration(logDirectory, serviceName);
            if (configResult.IsFailure)
                return Result<ILogger>.Failure($"Failed to create configuration: {configResult.Error}");

            var config = configResult.Value.WithMinLogLevel(logLevel).ForProduction();
            return CreateCustomLogger(config);
        }
        catch (Exception ex)
        {
            return Result<ILogger>.Failure($"Enterprise setup failed: {ex.Message}", ex);
        }
    }

    #endregion
}
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.API;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Models.Configuration;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Models.Results;

// 🎯 ROOT PUBLIC API: Single entry point for AdvancedWinUiLogger component
// CLEAN API DESIGN: One namespace, minimal using statements required
namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger;

/// <summary>
/// 🚀 ADVANCED WINUI LOGGER: Enterprise-grade file logging component
///
/// 🎯 SINGLE USING STATEMENT: using RpaWinUiComponentsPackage.AdvancedWinUiLogger;
///
/// ✅ CLEAN ARCHITECTURE: Follows clean architecture principles with clear separation of concerns
/// ✅ SOLID PRINCIPLES: Single responsibility, open/closed, Liskov substitution, interface segregation, dependency inversion
/// ✅ FUNCTIONAL/HYBRID: Functional approach with monadic error handling, OOP where beneficial
/// ✅ FILE-ONLY LOGGING: Pure file-based logging without UI components
/// ✅ ENTERPRISE FEATURES: Automatic rotation, size limits, background logging, performance monitoring
/// ✅ THREAD-SAFE: Concurrent logging support with proper synchronization
/// ✅ FAIL-SAFE: Graceful error handling with external logger fallback
/// ✅ MICROSOFT INTEGRATION: Full Microsoft.Extensions.Logging.ILogger interface compatibility
///
/// 📁 COMPONENT STRUCTURE:
/// ├── Models/           - Domain entities, configuration, results
/// ├── Interfaces/       - Contracts and abstractions
/// ├── Services/         - Business logic and infrastructure
/// ├── API/             - Implementation layer
/// ├── Core/            - Functional utilities and shared types
/// └── AdvancedWinUiLogger.cs - This root public API
///
/// 💡 USAGE PATTERNS:
///
/// // 🔧 BASIC FILE LOGGER:
/// var logger = AdvancedWinUiLogger.CreateFileLogger(
///     externalLogger: appLogger,
///     logDirectory: @"C:\MyApp\Logs",
///     baseFileName: "MyApp",
///     maxFileSizeMB: 50);
///
/// logger.LogInformation("🚀 Application started");
/// logger.LogError("❌ Something went wrong");
///
/// // 🔧 CONFIGURATION-BASED APPROACH:
/// var options = LoggerOptions.Production(@"C:\Logs", "MyApp");
/// var logger = AdvancedWinUiLogger.CreateFileLogger(options);
///
/// // 🔧 COMPONENT-BASED APPROACH (Advanced):
/// var component = AdvancedWinUiLogger.CreateComponent(
///     logDirectory: @"C:\Logs",
///     baseFileName: "MyApp",
///     maxFileSizeMB: 100);
///
/// await component.RotateLogsAsync();
/// var files = await component.GetLogFilesAsync();
/// await component.CleanupOldLogsAsync(30);
///
/// // 🔧 SAFE ERROR HANDLING:
/// var result = AdvancedWinUiLogger.CreateFileLoggerSafe(
///     externalLogger: null,
///     logDirectory: @"C:\Logs",
///     baseFileName: "Test",
///     maxFileSizeMB: 10);
///
/// if (result.IsSuccess)
/// {
///     var logger = result.Value;
///     // Use logger safely
/// }
/// else
/// {
///     Console.WriteLine($"Logger creation failed: {result.ErrorMessage}");
/// }
///
/// 🎯 KEY FEATURES:
/// - Automatic file rotation based on size limits
/// - Configurable retention policies for old log files
/// - High-performance async logging with batching
/// - Structured logging support with parameters
/// - Thread-safe concurrent access
/// - Clean functional error handling with Result<T> monads
/// - Zero-configuration defaults with extensive customization options
/// - Integration with Microsoft.Extensions.Logging ecosystem
/// - Production-ready enterprise features
/// </summary>
public static class AdvancedWinUiLogger
{
    #region Core Logger Creation - Primary API

    /// <summary>
    /// 🎯 PRIMARY API: Create enterprise file logger with automatic rotation
    ///
    /// This is the main entry point for creating a professional file logger.
    /// Provides all essential features with clean parameter-based configuration.
    ///
    /// FEATURES:
    /// ✅ Automatic file rotation based on size
    /// ✅ Thread-safe concurrent logging
    /// ✅ External logger integration for audit trails
    /// ✅ Directory auto-creation with permission validation
    /// ✅ Configurable file naming and retention
    /// ✅ Full Microsoft.Extensions.Logging.ILogger compatibility
    ///
    /// USAGE EXAMPLES:
    ///
    /// // Enterprise application with rotation:
    /// var logger = AdvancedWinUiLogger.CreateFileLogger(
    ///     externalLogger: serviceProvider.GetLogger<MyApp>(),
    ///     logDirectory: @"C:\ProgramData\MyCompany\MyApp\Logs",
    ///     baseFileName: "MyApp",
    ///     maxFileSizeMB: 50);
    ///
    /// // Simple development logger:
    /// var devLogger = AdvancedWinUiLogger.CreateFileLogger(
    ///     externalLogger: null,
    ///     logDirectory: Path.GetTempPath(),
    ///     baseFileName: "DevTest",
    ///     maxFileSizeMB: 5);
    ///
    /// // High-volume production system:
    /// var prodLogger = AdvancedWinUiLogger.CreateFileLogger(
    ///     externalLogger: systemLogger,
    ///     logDirectory: @"D:\HighVolumeLogs",
    ///     baseFileName: "Production",
    ///     maxFileSizeMB: 200);
    ///
    /// // Unlimited size (use with caution):
    /// var auditLogger = AdvancedWinUiLogger.CreateFileLogger(
    ///     externalLogger: complianceLogger,
    ///     logDirectory: @"C:\AuditLogs",
    ///     baseFileName: "Audit",
    ///     maxFileSizeMB: null);
    /// </summary>
    /// <param name="externalLogger">
    /// Optional external logger for internal operations logging.
    /// Can be null - logger will work without external logging.
    /// If provided, internal operations (startup, rotation, errors) are logged.
    /// </param>
    /// <param name="logDirectory">
    /// Absolute path to directory for log files.
    /// Directory will be created if it doesn't exist.
    /// Examples: @"C:\MyApp\Logs", @"D:\ApplicationData\Logs"
    /// </param>
    /// <param name="baseFileName">
    /// Base name for log files without extension (.log is added automatically).
    /// Examples: "MyApplication", "DataProcessor", "WebAPI"
    /// </param>
    /// <param name="maxFileSizeMB">
    /// Maximum file size in megabytes before rotation.
    /// null = no rotation (file grows unlimited - use with caution).
    /// Typical values: 5MB (dev), 50MB (production), 100MB+ (high-volume)
    /// </param>
    /// <returns>
    /// ILogger implementation optimized for file logging.
    /// Supports full Microsoft.Extensions.Logging interface.
    /// Thread-safe with automatic resource cleanup.
    /// </returns>
    /// <exception cref="ArgumentException">Invalid logDirectory or baseFileName</exception>
    /// <exception cref="DirectoryNotFoundException">Cannot create logDirectory</exception>
    /// <exception cref="UnauthorizedAccessException">Insufficient write permissions</exception>
    public static ILogger CreateFileLogger(
        ILogger? externalLogger,
        string logDirectory,
        string baseFileName,
        int? maxFileSizeMB) =>
        LoggerAPI.CreateFileLogger(externalLogger, logDirectory, baseFileName, maxFileSizeMB);

    #endregion

    #region Configuration-Based API

    /// <summary>
    /// 🔧 CONFIGURATION API: Create file logger using LoggerOptions
    ///
    /// Modern configuration-based approach using LoggerOptions object.
    /// Provides better IntelliSense support, type safety, and extensibility.
    ///
    /// USAGE PATTERNS:
    ///
    /// // Using factory methods:
    /// var logger = AdvancedWinUiLogger.CreateFileLogger(
    ///     LoggerOptions.Production(@"C:\Logs", "MyApp"));
    ///
    /// var devLogger = AdvancedWinUiLogger.CreateFileLogger(
    ///     LoggerOptions.Debug(@"C:\DevLogs", "Debug"));
    ///
    /// // Custom configuration:
    /// var options = LoggerOptions.Create(@"C:\Logs", "MyApp") with
    /// {
    ///     MaxFileSizeBytes = 25 * 1024 * 1024, // 25 MB
    ///     MaxFileCount = 20,
    ///     EnablePerformanceMonitoring = true
    /// };
    /// var logger = AdvancedWinUiLogger.CreateFileLogger(options);
    /// </summary>
    /// <param name="options">
    /// LoggerOptions configuration object containing all logger settings.
    /// Use LoggerOptions.Create(), .Debug(), or .Production() factory methods.
    /// </param>
    /// <returns>ILogger implementation configured according to options</returns>
    /// <exception cref="ArgumentNullException">Options cannot be null</exception>
    /// <exception cref="ArgumentException">Invalid configuration values</exception>
    public static ILogger CreateFileLogger(LoggerOptions options) =>
        LoggerAPI.CreateFileLogger(options);

    /// <summary>
    /// 🔧 ENHANCED CONFIGURATION API: Create file logger with external logger and options
    ///
    /// Combines configuration convenience with external logger support.
    /// Best for complex scenarios requiring audit trails and chained logging.
    ///
    /// USAGE:
    /// var appLogger = serviceProvider.GetService<ILogger<MyApp>>();
    /// var options = LoggerOptions.Production(@"C:\Logs", "MyApp");
    /// var fileLogger = AdvancedWinUiLogger.CreateFileLogger(appLogger, options);
    /// </summary>
    /// <param name="externalLogger">Optional external logger for audit trail</param>
    /// <param name="options">Complete logger configuration options</param>
    /// <returns>ILogger implementation with external logging support</returns>
    /// <exception cref="ArgumentNullException">Options cannot be null</exception>
    public static ILogger CreateFileLogger(ILogger? externalLogger, LoggerOptions options) =>
        LoggerAPI.CreateFileLogger(externalLogger, options);

    #endregion

    #region Safe Result-Based API

    /// <summary>
    /// 🛡️ SAFE API: Create file logger with explicit error handling
    ///
    /// Returns LoggerResult<ILogger> for functional error handling patterns.
    /// Use when you need explicit control over error scenarios without exceptions.
    ///
    /// USAGE:
    /// var result = AdvancedWinUiLogger.CreateFileLoggerSafe(
    ///     appLogger, @"C:\Logs", "MyApp", 50);
    ///
    /// if (result.IsSuccess)
    /// {
    ///     var logger = result.Value;
    ///     logger.LogInformation("Logger created successfully");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Logger creation failed: {result.ErrorMessage}");
    ///     // Handle error gracefully without exceptions
    /// }
    ///
    /// // Functional composition:
    /// var loggerResult = AdvancedWinUiLogger.CreateFileLoggerSafe(null, @"C:\Logs", "Test", 10)
    ///     .OnSuccess(logger => logger.LogInformation("Logger ready"))
    ///     .OnFailure(error => Console.WriteLine($"Failed: {error}"));
    /// </summary>
    /// <param name="externalLogger">Optional external logger</param>
    /// <param name="logDirectory">Log directory path</param>
    /// <param name="baseFileName">Base file name</param>
    /// <param name="maxFileSizeMB">Max file size in MB</param>
    /// <returns>LoggerResult containing ILogger or error information</returns>
    public static LoggerResult<ILogger> CreateFileLoggerSafe(
        ILogger? externalLogger,
        string logDirectory,
        string baseFileName,
        int? maxFileSizeMB) =>
        LoggerAPI.CreateFileLoggerSafe(externalLogger, logDirectory, baseFileName, maxFileSizeMB);

    #endregion

    #region Component-Based API

    /// <summary>
    /// 🚀 COMPONENT API: Create advanced logger component with extended functionality
    ///
    /// Creates a component-based logger with programmatic control over all operations.
    /// Suitable for applications requiring runtime configuration changes and monitoring.
    ///
    /// ADVANCED FEATURES:
    /// - Runtime log directory changes
    /// - Manual rotation control
    /// - File monitoring and statistics
    /// - Programmatic cleanup operations
    /// - Configuration management
    ///
    /// USAGE:
    /// var component = AdvancedWinUiLogger.CreateComponent(
    ///     logDirectory: @"C:\Logs",
    ///     baseFileName: "MyApp",
    ///     maxFileSizeMB: 100,
    ///     logger: appLogger);
    ///
    /// // Advanced operations:
    /// await component.RotateLogsAsync();
    /// var files = await component.GetLogFilesAsync();
    /// await component.CleanupOldLogsAsync(30);
    /// await component.SetLogDirectoryAsync(@"D:\NewLogs");
    ///
    /// // Monitor statistics:
    /// Console.WriteLine($"Total log size: {component.TotalLogSizeMB:F2} MB");
    /// Console.WriteLine($"Current directory: {component.LogDirectory}");
    /// </summary>
    /// <param name="logDirectory">Directory for log files</param>
    /// <param name="baseFileName">Base name for log files</param>
    /// <param name="maxFileSizeMB">Maximum file size before rotation</param>
    /// <param name="maxBackupFiles">Maximum number of backup files to keep</param>
    /// <param name="logger">Optional external logger for component operations</param>
    /// <returns>LoggerAPIComponent with advanced management capabilities</returns>
    public static LoggerAPIComponent CreateComponent(
        string logDirectory,
        string baseFileName = "application",
        int maxFileSizeMB = 10,
        int maxBackupFiles = 5,
        ILogger? logger = null) =>
        LoggerAPIComponent.CreateFileLogger(logDirectory, baseFileName, maxFileSizeMB, maxBackupFiles, logger);

    /// <summary>
    /// 🚀 COMPONENT API: Create from configuration options
    ///
    /// Creates component using LoggerOptions for consistency with other APIs.
    ///
    /// USAGE:
    /// var options = LoggerOptions.Production(@"C:\Logs", "MyApp");
    /// var component = AdvancedWinUiLogger.CreateComponent(options, appLogger);
    /// </summary>
    /// <param name="options">Logger configuration options</param>
    /// <param name="logger">Optional external logger</param>
    /// <returns>LoggerAPIComponent configured according to options</returns>
    public static LoggerAPIComponent CreateComponent(LoggerOptions options, ILogger? logger = null) =>
        LoggerAPIComponent.FromOptions(options, logger);

    /// <summary>
    /// 🚀 COMPONENT API: Create headless component for background operations
    ///
    /// Creates lightweight component for background services and automated operations.
    ///
    /// USAGE:
    /// var headless = AdvancedWinUiLogger.CreateHeadlessComponent(backgroundLogger);
    /// await headless.SetLogDirectoryAsync(@"C:\BackgroundLogs");
    /// await headless.RotateLogsAsync();
    /// </summary>
    /// <param name="logger">Optional external logger</param>
    /// <returns>Headless logger component</returns>
    public static LoggerAPIComponent CreateHeadlessComponent(ILogger? logger = null) =>
        LoggerAPIComponent.CreateHeadless(logger);

    #endregion
}
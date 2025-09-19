using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Entities;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Application.Interfaces;

/// <summary>
/// APPLICATION INTERFACE: Primary logger service contract
/// CLEAN ARCHITECTURE: Application layer service abstraction
/// ENTERPRISE: Comprehensive logging service with session management
/// </summary>
internal interface ILoggerService : ILogger, IDisposable
{
    /// <summary>
    /// INITIALIZATION: Start logging session with configuration
    /// ENTERPRISE: Session-based logging with full lifecycle management
    /// </summary>
    Task<Result<Guid>> StartSessionAsync(LoggerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// LIFECYCLE: Stop specific logging session
    /// ENTERPRISE: Graceful session termination with resource cleanup
    /// </summary>
    Task<Result<bool>> StopSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// LIFECYCLE: Stop all active logging sessions
    /// ENTERPRISE: Bulk session management for application shutdown
    /// </summary>
    Task<Result<int>> StopAllSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// LOGGING: Write structured log entry with rich metadata
    /// ENTERPRISE: Advanced logging with contextual information
    /// </summary>
    Task<Result<bool>> LogAsync(LogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// LOGGING: Write multiple log entries in batch operation
    /// PERFORMANCE: Optimized batch logging for high-throughput scenarios
    /// </summary>
    Task<Result<int>> LogBatchAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// PERSISTENCE: Force flush all pending log entries to storage
    /// RELIABILITY: Ensure data persistence before critical operations
    /// </summary>
    Task<Result<int>> FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// CONFIGURATION: Update logger configuration at runtime
    /// ENTERPRISE: Dynamic configuration updates without restart
    /// </summary>
    Task<Result<bool>> UpdateConfigurationAsync(LoggerConfiguration newConfiguration, CancellationToken cancellationToken = default);

    /// <summary>
    /// QUERY: Get current logger configuration
    /// MONITORING: Configuration inspection for debugging and auditing
    /// </summary>
    Result<LoggerConfiguration> GetCurrentConfiguration();

    /// <summary>
    /// QUERY: Get current session status
    /// MONITORING: Real-time session information for operational insights
    /// </summary>
    Result<SessionStatus> GetSessionStatus();

    /// <summary>
    /// QUERY: Get status of all managed sessions
    /// MONITORING: Comprehensive session overview
    /// </summary>
    Result<IReadOnlyList<SessionStatus>> GetAllSessionStatuses();

    /// <summary>
    /// HEALTH: Check if logger service is healthy and operational
    /// MONITORING: Health check for service monitoring systems
    /// </summary>
    Task<Result<bool>> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// METRICS: Get comprehensive service metrics
    /// TELEMETRY: Performance and operational metrics collection
    /// </summary>
    Result<LoggerServiceMetrics> GetMetrics();
}

/// <summary>
/// APPLICATION INTERFACE: File management service contract
/// ENTERPRISE: Advanced file operations and lifecycle management
/// </summary>
internal interface IFileManagementService
{
    /// <summary>
    /// ROTATION: Perform log file rotation
    /// ENTERPRISE: Complete rotation workflow with validation and cleanup
    /// </summary>
    Task<Result<RotationResult>> RotateLogFileAsync(string currentFilePath, LoggerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// CLEANUP: Clean up old log files based on retention policies
    /// MAINTENANCE: Automated file lifecycle management
    /// </summary>
    Task<Result<int>> CleanupOldFilesAsync(string directoryPath, LoggerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// ARCHIVING: Archive log files for long-term storage
    /// ENTERPRISE: File archiving with compression and organization
    /// </summary>
    Task<Result<string>> ArchiveLogFileAsync(string filePath, LoggerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// QUERY: Get information about log files in directory
    /// DISCOVERY: File inventory and analysis
    /// </summary>
    Task<Result<IReadOnlyList<LogFileInfo>>> GetLogFilesAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// VALIDATION: Check if rotation is needed
    /// BUSINESS LOGIC: Rotation criteria evaluation
    /// </summary>
    Task<Result<bool>> ShouldRotateAsync(string filePath, LoggerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// VALIDATION: Validate file and directory permissions
    /// SECURITY: Access control validation
    /// </summary>
    Task<Result<bool>> ValidatePermissionsAsync(string directoryPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// APPLICATION INTERFACE: Configuration management service contract
/// ENTERPRISE: Configuration lifecycle and validation management
/// </summary>
internal interface IConfigurationService
{
    /// <summary>
    /// VALIDATION: Validate configuration against business rules
    /// ENTERPRISE: Comprehensive configuration validation
    /// </summary>
    Result<bool> ValidateConfiguration(LoggerConfiguration configuration);

    /// <summary>
    /// VALIDATION: Validate configuration for specific environment
    /// ENVIRONMENT: Context-aware validation with environment-specific rules
    /// </summary>
    Result<bool> ValidateForEnvironment(LoggerConfiguration configuration, string environment);

    /// <summary>
    /// TRANSFORMATION: Apply environment-specific defaults
    /// CONFIGURATION: Environment-aware configuration enhancement
    /// </summary>
    Result<LoggerConfiguration> ApplyEnvironmentDefaults(LoggerConfiguration baseConfiguration, string environment);

    /// <summary>
    /// PERSISTENCE: Save configuration to persistent storage
    /// CONFIGURATION: Configuration state management
    /// </summary>
    Task<Result<bool>> SaveConfigurationAsync(LoggerConfiguration configuration, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// PERSISTENCE: Load configuration from persistent storage
    /// CONFIGURATION: Configuration state restoration
    /// </summary>
    Task<Result<LoggerConfiguration>> LoadConfigurationAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// FACTORY: Create configuration for common scenarios
    /// CONVENIENCE: Pre-configured setups for typical use cases
    /// </summary>
    Result<LoggerConfiguration> CreateConfiguration(string scenario, string logDirectory, string baseFileName = "application");
}

/// <summary>
/// VALUE OBJECT: Logger service metrics for monitoring and telemetry
/// TELEMETRY: Comprehensive service performance and operational data
/// </summary>
internal sealed record LoggerServiceMetrics
{
    /// <summary>Total number of log entries processed</summary>
    public long TotalEntriesProcessed { get; init; }

    /// <summary>Total bytes written to log files</summary>
    public long TotalBytesWritten { get; init; }

    /// <summary>Number of active sessions</summary>
    public int ActiveSessions { get; init; }

    /// <summary>Number of rotation operations performed</summary>
    public int RotationOperations { get; init; }

    /// <summary>Number of cleanup operations performed</summary>
    public int CleanupOperations { get; init; }

    /// <summary>Average entries per second throughput</summary>
    public double AverageEntriesPerSecond { get; init; }

    /// <summary>Average MB per second throughput</summary>
    public double AverageMBPerSecond { get; init; }

    /// <summary>Number of errors encountered</summary>
    public long ErrorCount { get; init; }

    /// <summary>Number of warnings encountered</summary>
    public long WarningCount { get; init; }

    /// <summary>Current memory usage in bytes</summary>
    public long CurrentMemoryUsage { get; init; }

    /// <summary>Service uptime</summary>
    public TimeSpan Uptime { get; init; }

    /// <summary>Last health check timestamp</summary>
    public DateTime LastHealthCheck { get; init; }

    /// <summary>Indicates if service is healthy</summary>
    public bool IsHealthy { get; init; }

    /// <summary>Service start timestamp</summary>
    public DateTime ServiceStartTime { get; init; }

    /// <summary>Last operation timestamp</summary>
    public DateTime LastOperationTime { get; init; }
}
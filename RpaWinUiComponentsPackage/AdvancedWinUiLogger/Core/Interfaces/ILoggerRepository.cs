using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.ValueObjects;
using RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Entities;

namespace RpaWinUiComponentsPackage.AdvancedWinUiLogger.Core.Interfaces;

/// <summary>
/// CORE INTERFACE: Repository contract for logger persistence operations
/// CLEAN ARCHITECTURE: Domain layer interface for infrastructure abstraction
/// ENTERPRISE: Comprehensive logging data management contract
/// </summary>
internal interface ILoggerRepository
{
    /// <summary>
    /// PERSISTENCE: Write single log entry to persistent storage
    /// ENTERPRISE: Atomic write operation with result handling
    /// </summary>
    Task<Result<bool>> WriteLogEntryAsync(LogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// PERSISTENCE: Write multiple log entries in batch operation
    /// PERFORMANCE: Optimized batch writing for high throughput scenarios
    /// </summary>
    Task<Result<int>> WriteBatchAsync(IEnumerable<LogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// FILE MANAGEMENT: Initialize log file for writing
    /// ENTERPRISE: File preparation and validation
    /// </summary>
    Task<Result<string>> InitializeLogFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// FILE MANAGEMENT: Rotate current log file
    /// ENTERPRISE: File rotation with comprehensive result tracking
    /// </summary>
    Task<Result<RotationResult>> RotateLogFileAsync(string currentFilePath, string newFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// FILE MANAGEMENT: Get information about log file
    /// QUERY: File metadata retrieval
    /// </summary>
    Task<Result<LogFileInfo>> GetLogFileInfoAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// FILE MANAGEMENT: Get list of all managed log files in directory
    /// QUERY: Directory-level file discovery and analysis
    /// </summary>
    Task<Result<IReadOnlyList<LogFileInfo>>> GetLogFilesInDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// CLEANUP: Delete old log files based on criteria
    /// ENTERPRISE: Automated cleanup with comprehensive reporting
    /// </summary>
    Task<Result<int>> CleanupOldFilesAsync(string directoryPath, int maxAgeDays, int maxFileCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// ARCHIVING: Archive log file (compress or move)
    /// ENTERPRISE: File archiving for long-term storage
    /// </summary>
    Task<Result<string>> ArchiveLogFileAsync(string filePath, string archiveDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// VALIDATION: Check if log file is currently in use
    /// CONCURRENCY: File lock detection for safe operations
    /// </summary>
    Task<Result<bool>> IsFileInUseAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// VALIDATION: Ensure log directory exists and is accessible
    /// INFRASTRUCTURE: Directory preparation and validation
    /// </summary>
    Task<Result<bool>> EnsureDirectoryExistsAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// QUERY: Get current log file size
    /// PERFORMANCE: Fast size checking for rotation decisions
    /// </summary>
    Task<Result<long>> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// FLUSH: Force flush all pending writes to disk
    /// RELIABILITY: Ensure data persistence
    /// </summary>
    Task<Result<bool>> FlushAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// CORE INTERFACE: Logger session management contract
/// ENTERPRISE: Session lifecycle and state management
/// </summary>
internal interface ILoggerSessionManager
{
    /// <summary>
    /// SESSION: Create new logging session
    /// ENTERPRISE: Session initialization with configuration
    /// </summary>
    Result<LoggerSession> CreateSession(LoggerConfiguration configuration);

    /// <summary>
    /// SESSION: Get existing session by ID
    /// QUERY: Session retrieval and access
    /// </summary>
    Result<LoggerSession> GetSession(Guid sessionId);

    /// <summary>
    /// SESSION: Get currently active session
    /// QUERY: Active session access
    /// </summary>
    Result<LoggerSession> GetActiveSession();

    /// <summary>
    /// SESSION: Stop and cleanup session
    /// LIFECYCLE: Session termination and resource cleanup
    /// </summary>
    Result<bool> StopSession(Guid sessionId);

    /// <summary>
    /// SESSION: Stop all active sessions
    /// LIFECYCLE: Bulk session termination
    /// </summary>
    Result<int> StopAllSessions();

    /// <summary>
    /// QUERY: Get status of all sessions
    /// MONITORING: Session overview for operational insights
    /// </summary>
    Result<IReadOnlyList<SessionStatus>> GetAllSessionStatuses();
}

/// <summary>
/// CORE INTERFACE: File rotation service contract
/// ENTERPRISE: Advanced file rotation and lifecycle management
/// </summary>
internal interface IFileRotationService
{
    /// <summary>
    /// ROTATION: Check if file needs rotation based on configuration
    /// BUSINESS LOGIC: Rotation criteria evaluation
    /// </summary>
    Task<Result<bool>> ShouldRotateAsync(string filePath, LoggerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// ROTATION: Perform file rotation operation
    /// ENTERPRISE: Complete rotation workflow with result tracking
    /// </summary>
    Task<Result<RotationResult>> RotateFileAsync(string currentFilePath, LoggerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// CLEANUP: Clean up old rotated files
    /// MAINTENANCE: Automated file lifecycle management
    /// </summary>
    Task<Result<int>> CleanupRotatedFilesAsync(string directoryPath, LoggerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// GENERATION: Generate next rotation file name
    /// UTILITY: Consistent file naming for rotation sequences
    /// </summary>
    Result<string> GenerateRotatedFileName(string originalFilePath, DateTime rotationTime);

    /// <summary>
    /// VALIDATION: Validate rotation operation feasibility
    /// PRECONDITION: Pre-rotation validation and safety checks
    /// </summary>
    Task<Result<bool>> ValidateRotationAsync(string filePath, string targetDirectory, CancellationToken cancellationToken = default);
}

/// <summary>
/// CORE INTERFACE: Configuration validation service
/// ENTERPRISE: Configuration validation and business rules
/// </summary>
internal interface IConfigurationValidator
{
    /// <summary>
    /// VALIDATION: Validate logger configuration
    /// BUSINESS RULES: Comprehensive configuration validation
    /// </summary>
    Result<bool> ValidateConfiguration(LoggerConfiguration configuration);

    /// <summary>
    /// VALIDATION: Validate configuration for specific environment
    /// ENVIRONMENT: Context-specific validation rules
    /// </summary>
    Result<bool> ValidateForEnvironment(LoggerConfiguration configuration, string environment);

    /// <summary>
    /// VALIDATION: Check configuration compatibility with system
    /// SYSTEM: System resource and capability validation
    /// </summary>
    Task<Result<bool>> ValidateSystemCompatibilityAsync(LoggerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// VALIDATION: Validate directory permissions and accessibility
    /// SECURITY: Access control and permission validation
    /// </summary>
    Task<Result<bool>> ValidateDirectoryAccessAsync(string directoryPath, CancellationToken cancellationToken = default);
}